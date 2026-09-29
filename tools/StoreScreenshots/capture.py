"""Capture actual Android pixels as a lossless, opaque Google Play PNG.

Only the explicitly authorized BlueStacks Test instance is supported here.
No resizing, cropping, retouching, text overlays or generated UI are used.
"""

import argparse
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import time
import zlib

ROOT = Path(__file__).resolve().parents[2]
ADB = Path(r"C:\Program Files\BlueStacks_nxt\HD-Adb.exe")
SERIAL = "127.0.0.1:5575"
PACKAGE = "com.nksolutions.belot"


def adb(*args):
    config = Path(r"C:\ProgramData\BlueStacks_nxt\bluestacks.conf").read_text(encoding="utf8")
    assert 'bst.instance.Pie64_2.display_name="Test"' in config
    assert 'bst.instance.Pie64_2.status.adb_port="5575"' in config
    return subprocess.check_output([str(ADB), "-s", SERIAL, *args], timeout=30)


def png_chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))


def capture(locale, name):
    assert locale in ("bg", "en-US")
    assert name in (
        "01-gameplay", "02-bidding", "03-hint", "04-declarations",
        "05-result", "06-setup", "07-statistics", "08-rules",
    )
    raw = adb("exec-out", "screencap")
    width, height, pixel_format = struct.unpack("<III", raw[:12])
    assert (width, height, pixel_format) == (1080, 1920, 1), (width, height, pixel_format)
    header_length = len(raw) - width * height * 4
    assert header_length in (12, 16), header_length
    rgba = raw[header_length:]
    assert all(value == 255 for value in rgba[3::4]), "The capture must be fully opaque."
    rgb = bytearray(width * height * 3)
    for channel in range(3):
        rgb[channel::3] = rgba[channel::4]
    rows = b"".join(b"\0" + rgb[offset:offset + width * 3] for offset in range(0, len(rgb), width * 3))
    png = b"\x89PNG\r\n\x1a\n"
    png += png_chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
    png += png_chunk(b"IDAT", zlib.compress(rows, 9))
    png += png_chunk(b"IEND", b"")
    output = ROOT / "store" / "google-play" / "screenshots" / locale / (name + ".png")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(png)
    evidence = ROOT / "artifacts" / "store-screenshots-20260929"
    evidence.mkdir(parents=True, exist_ok=True)
    metadata = {
        "file": output.relative_to(ROOT).as_posix(),
        "locale": locale,
        "captured_at_unix": time.time(),
        "serial": SERIAL,
        "package": PACKAGE,
        "width": width,
        "height": height,
        "format": "24-bit RGB PNG, opaque",
        "sha256": hashlib.sha256(png).hexdigest(),
        "raw_sha256": hashlib.sha256(raw).hexdigest(),
        "rgb_sha256": hashlib.sha256(rgb).hexdigest(),
        "size_bytes": len(png),
    }
    (evidence / (locale + "-" + name + ".json")).write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf8")
    print(json.dumps(metadata))
    return output


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("locale", choices=("bg", "en-US"))
    parser.add_argument("name")
    options = parser.parse_args()
    capture(options.locale, options.name)
