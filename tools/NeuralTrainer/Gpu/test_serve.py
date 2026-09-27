"""Verify masked decisions, physical tie order, and the C#-compatible binary protocol."""

import socket
import struct
import threading
import unittest

import numpy as np
import torch

import fit
import serve


class ServeTests(unittest.TestCase):
    def model(self):
        model = fit.Network(1, 1, (600, 32))
        with torch.no_grad():
            model.layers[0].weight.zero_()
            model.layers[0].bias.zero_()
            model.layers[0].bias[31] = 100  # Illegal, so must never win.
        return model

    def features(self):
        features = torch.zeros(2, 600)
        features[:, 32] = 1
        features[:, 32 + 17] = 1
        return features

    def test_legal_mask_and_physical_tie_order(self):
        model, features = self.model(), self.features()
        self.assertEqual(serve.choose(model, features, 0).tolist(), [0, 0])
        self.assertEqual(serve.choose(model, features, 2).tolist(), [1, 1])
        with self.assertRaises(ValueError):
            serve.choose(model, torch.zeros_like(features), 0)

    def test_binary_round_trip_and_weight_hash_rejection(self):
        with serve.Server(("127.0.0.1", 0), serve.Handler) as server:
            server.models = {1: self.model()}
            server.hashes = b"a" * 128
            server.device = "cpu"
            server.policy = None
            worker = threading.Thread(target=server.serve_forever, daemon=True)
            worker.start()
            try:
                for valid in (False, True):
                    with socket.create_connection(server.server_address, timeout=5) as client:
                        client.sendall(struct.pack("<i", serve.MAGIC) + (server.hashes if valid else b"b" * 128))
                        magic, status = struct.unpack("<ii", serve.receive(client, 8))
                        self.assertEqual((magic, status), (serve.MAGIC, 0 if valid else 1))
                        if valid:
                            client.sendall(struct.pack("<5i", serve.MAGIC, 1, 2, 2, 600))
                            client.sendall(self.features().numpy().astype("<f4").tobytes())
                            self.assertEqual(struct.unpack("<ii", serve.receive(client, 8)), (serve.MAGIC, 2))
                            np.testing.assert_array_equal(np.frombuffer(serve.receive(client, 8), dtype="<i4"), [1, 1])
            finally:
                server.shutdown()
                worker.join(timeout=5)

    @unittest.skipUnless(torch.cuda.is_available(), "CUDA graph check requires CUDA")
    def test_graph_batches_match_eager_with_changing_counts_and_inputs(self):
        torch.manual_seed(41)
        model = fit.Network(1, 1, (600, 32, 16, 32)).cuda().eval()
        policy = serve.GraphPolicy({1: model})
        for count, rotation in ((3, 0), (4, 0), (2, 2), (3, 0), (7, 3)):
            features = torch.randn(count, 600)
            features[:, 32] = 1
            expected = serve.choose(model, features.cuda(), rotation).cpu().numpy()
            actual = policy.choose(1, features.numpy(), rotation)
            np.testing.assert_array_equal(actual, expected)


if __name__ == "__main__":
    unittest.main()
