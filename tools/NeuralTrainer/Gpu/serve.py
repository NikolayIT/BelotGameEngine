"""Training-only batched inference on loopback; no game state or hidden-card inputs."""

import argparse
import hashlib
import json
from pathlib import Path
import socket
import socketserver
import struct
import threading

import numpy as np
import torch

import fit

MAGIC = 0x314E4E47


def receive(stream, size):
    result = bytearray(size)
    view = memoryview(result)
    while view:
        count = stream.recv_into(view)
        if count == 0:
            raise EOFError("Connection closed")
        view = view[count:]
    return result


@torch.inference_mode()
def choose(model, features, rotation):
    legal = features[:, 32:64] > 0
    if not legal.any(dim=1).all():
        raise ValueError("A decision has no legal cards")
    return choose_unchecked(model, features, rotation)


def choose_unchecked(model, features, rotation):
    legal = features[:, 32:64] > 0
    values = model(features).masked_fill(~legal, -torch.inf)
    # The C# policy breaks equal values in physical card order, before suit rotation.
    return torch.roll(values, rotation * 8, dims=1).argmax(1).to(torch.int32)


class GraphPolicy:
    """Static padded batches avoid repeated Python/driver kernel-launch overhead."""
    def __init__(self, models):
        self.models = models
        self.graphs = {}
        self.lock = threading.Lock()

    @torch.inference_mode()
    def choose(self, tag, features, rotation):
        count = len(features)
        bucket = 1 << (count - 1).bit_length()
        key = (tag, rotation, bucket)
        # No other server CUDA work may overlap capture or overwrite static buffers.
        with self.lock:
            if key not in self.graphs:
                inputs = torch.zeros(bucket, 600, device="cuda")
                stream = torch.cuda.Stream()
                stream.wait_stream(torch.cuda.current_stream())
                with torch.cuda.stream(stream):
                    for _ in range(3):
                        choose_unchecked(self.models[tag], inputs, rotation)
                torch.cuda.current_stream().wait_stream(stream)
                graph = torch.cuda.CUDAGraph()
                with torch.cuda.graph(graph):
                    output = choose_unchecked(self.models[tag], inputs, rotation)
                self.graphs[key] = inputs, output, graph
            inputs, output, graph = self.graphs[key]
            inputs[:count].copy_(torch.from_numpy(features))
            graph.replay()
            # Rows are independent: padded rows cannot affect the requested outputs.
            return output[:count].cpu().numpy().copy()


class Handler(socketserver.BaseRequestHandler):
    def handle(self):
        self.request.settimeout(120)
        self.request.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        try:
            handshake = receive(self.request, 132)
            accepted = struct.unpack_from("<i", handshake)[0] == MAGIC and handshake[4:] == self.server.hashes
            self.request.sendall(struct.pack("<ii", MAGIC, 0 if accepted else 1))
            if not accepted:
                return
            while True:
                magic, tag, rotation, count, width = struct.unpack("<5i", receive(self.request, 20))
                if magic != MAGIC or tag not in (1, 2, 3) or not 0 <= rotation <= 3 or not 1 <= count <= 4096 or width != 600:
                    raise ValueError("Invalid inference request")
                data = receive(self.request, count * width * 4)
                features = np.frombuffer(data, dtype="<f4").reshape(count, width)
                if not np.isfinite(features).all():
                    raise ValueError("Non-finite features")
                if not (features[:, 32:64] > 0).any(axis=1).all():
                    raise ValueError("A decision has no legal cards")
                if self.server.policy is None:
                    tensor = torch.from_numpy(features).to(self.server.device)
                    choices = choose(self.server.models[tag], tensor, rotation).cpu().numpy()
                else:
                    choices = self.server.policy.choose(tag, features, rotation)
                choices = choices.astype("<i4")
                self.request.sendall(struct.pack("<ii", MAGIC, count) + choices.tobytes())
        except EOFError:
            pass
        except (ValueError, OSError) as error:
            print(json.dumps(dict(client_error=str(error))), flush=True)


class Server(socketserver.ThreadingTCPServer):
    daemon_threads = True
    allow_reuse_address = True


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--in", dest="input", required=True)
    parser.add_argument("--port", type=int, default=18731)
    parser.add_argument("--device", choices=("cpu", "cuda"), default="cuda")
    parser.add_argument("--graphs", action="store_true")
    args = parser.parse_args()
    if args.graphs and args.device != "cuda":
        parser.error("--graphs requires CUDA")
    fit.disable_power_throttling()
    torch.set_num_threads(1)
    torch.use_deterministic_algorithms(True)
    device = torch.device(args.device)
    paths = [Path(args.input) / (name + ".bin") for name in fit.NAMES]
    models = {tag: fit.read_network(path, tag).to(device).eval() for tag, path in enumerate(paths) if tag}
    hashes = b"".join(hashlib.sha256(path.read_bytes()).digest() for path in paths)
    with Server(("127.0.0.1", args.port), Handler) as server:
        server.models, server.hashes, server.device = models, hashes, device
        server.policy = GraphPolicy(models) if args.graphs else None
        print(json.dumps(dict(listening=f"127.0.0.1:{args.port}", torch=torch.__version__,
                              device=str(device), graphs=args.graphs, hashes=hashes.hex())), flush=True)
        server.serve_forever()
