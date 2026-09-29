"""Verify all 16 native Google Play captures and write a manifest only on success.

Uses only the Python standard library. PNG pixels must match the RGB hash recorded
by capture.py before encoding. The raw framebuffer is not retained by capture.py,
so its recorded SHA-256 is provenance, not a separately reverified input.
"""

import argparse
from datetime import datetime, timezone
import hashlib
import json
import math
from pathlib import Path
import re
import struct
import sys
import zlib

ROOT = Path(__file__).resolve().parents[2]
SIZE = (1080, 1920)
LOCALES = ("bg", "en-US")
CATEGORIES = (
    "01-gameplay", "02-bidding", "03-hint", "04-declarations",
    "05-result", "06-setup", "07-statistics", "08-rules",
)
SIGNATURE = b"\x89PNG\r\n\x1a\n"
SHA256_PATTERN = re.compile(r"[0-9a-f]{64}\Z")


class VerificationError(ValueError):
    """The screenshots or their capture evidence failed a required check."""


def require(condition, message):
    if not condition:
        raise VerificationError(message)


def paeth(left, above, upper_left):
    prediction = left + above - upper_left
    distances = (abs(prediction - left), abs(prediction - above), abs(prediction - upper_left))
    if distances[0] <= distances[1] and distances[0] <= distances[2]:
        return left
    return above if distances[1] <= distances[2] else upper_left


def decode_rgb_png(data, expected_size=SIZE):
    """Validate chunk framing/CRCs and decode all five lossless PNG row filters."""
    require(data.startswith(SIGNATURE), "Invalid PNG signature")
    cursor = len(SIGNATURE)
    seen_header = False
    seen_end = False
    seen_data = False
    compressed = bytearray()
    width = height = 0
    while cursor < len(data):
        require(cursor + 12 <= len(data), "Truncated PNG chunk")
        length = struct.unpack_from(">I", data, cursor)[0]
        kind = data[cursor + 4:cursor + 8]
        end = cursor + 12 + length
        require(end <= len(data), "PNG chunk length exceeds file size")
        payload = data[cursor + 8:cursor + 8 + length]
        crc = struct.unpack_from(">I", data, cursor + 8 + length)[0]
        require(zlib.crc32(kind + payload) == crc, f"CRC mismatch in {kind!r}")
        require(kind in (b"IHDR", b"IDAT", b"IEND"), f"Unexpected capture PNG chunk {kind!r}")
        require(seen_header or kind == b"IHDR", "IHDR must be the first chunk")

        if kind == b"IHDR":
            require(not seen_header and length == 13, "Invalid or duplicate IHDR")
            width, height, depth, color, compression, filtering, interlace = struct.unpack(">IIBBBBB", payload)
            require((width, height) == expected_size, f"Wrong dimensions: {width}x{height}; expected {expected_size}")
            require((depth, color) == (8, 2), "PNG must be opaque 24-bit RGB (no alpha or palette)")
            require((compression, filtering, interlace) == (0, 0, 0), "Unsupported PNG encoding")
            seen_header = True
        elif kind == b"IDAT":
            seen_data = True
            compressed.extend(payload)
        else:
            require(length == 0 and seen_data, "Invalid IEND or missing IDAT")
            require(end == len(data), "Trailing bytes after IEND")
            seen_end = True
        cursor = end

    require(seen_header and seen_data and seen_end, "Incomplete PNG")
    stride = width * 3
    expected_length = height * (stride + 1)
    decoder = zlib.decompressobj()
    try:
        rows = decoder.decompress(bytes(compressed), expected_length + 1)
    except zlib.error as error:
        raise VerificationError(f"Invalid PNG zlib data: {error}") from error
    require(len(rows) == expected_length, "Decoded PNG pixel length does not match dimensions")
    require(decoder.eof and not decoder.unused_data and not decoder.unconsumed_tail, "Incomplete or extra compressed PNG data")

    rgb = bytearray()
    previous = bytearray(stride)
    for row_number in range(height):
        offset = row_number * (stride + 1)
        filtering = rows[offset]
        require(filtering <= 4, f"Invalid row filter {filtering}")
        row = bytearray(rows[offset + 1:offset + 1 + stride])
        if filtering:
            for column in range(stride):
                left = row[column - 3] if column >= 3 else 0
                above = previous[column]
                upper_left = previous[column - 3] if column >= 3 else 0
                if filtering == 1:
                    prediction = left
                elif filtering == 2:
                    prediction = above
                elif filtering == 3:
                    prediction = (left + above) // 2
                else:
                    prediction = paeth(left, above, upper_left)
                row[column] = (row[column] + prediction) & 255
        rgb.extend(row)
        previous = row
    return bytes(rgb)


def verify_capture(screenshots, evidence, locale, category):
    relative = f"{locale}/{category}.png"
    path = screenshots / relative
    metadata_path = evidence / f"{locale}-{category}.json"
    require(path.is_file(), f"Missing screenshot: {relative}")
    require(metadata_path.is_file(), f"Missing capture evidence: {metadata_path.name}")
    data = path.read_bytes()
    rgb = decode_rgb_png(data)
    png_hash = hashlib.sha256(data).hexdigest()
    rgb_hash = hashlib.sha256(rgb).hexdigest()
    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    require(isinstance(metadata, dict), "Capture evidence must be an object")
    expected = {
        "file": f"store/google-play/screenshots/{relative}",
        "locale": locale,
        "serial": "127.0.0.1:5575",
        "package": "com.nksolutions.belot",
        "width": SIZE[0],
        "height": SIZE[1],
        "format": "24-bit RGB PNG, opaque",
        "sha256": png_hash,
        "rgb_sha256": rgb_hash,
        "size_bytes": len(data),
    }
    for key, value in expected.items():
        require(metadata.get(key) == value, f"Capture evidence mismatch: {key}")
    raw_hash = metadata.get("raw_sha256")
    require(isinstance(raw_hash, str) and SHA256_PATTERN.fullmatch(raw_hash), "Missing or malformed raw framebuffer hash")
    captured_at = metadata.get("captured_at_unix")
    require(isinstance(captured_at, (int, float)) and not isinstance(captured_at, bool)
            and math.isfinite(captured_at) and captured_at > 0, "Invalid capture timestamp")
    return {**metadata, "category": category, "decoded_rgb_sha256": rgb_hash}


def verify_all(screenshots, evidence):
    expected = {f"{locale}/{category}.png" for locale in LOCALES for category in CATEGORIES}
    actual = {path.relative_to(screenshots).as_posix() for path in screenshots.rglob("*")
              if path.is_file() and path.suffix.lower() == ".png"}
    errors = []
    if actual - expected:
        errors.append("Unexpected screenshot categories/locales: " + ", ".join(sorted(actual - expected)))
    captures = []
    file_hashes = {}
    pixel_hashes = {}
    for locale in LOCALES:
        for category in CATEGORIES:
            name = f"{locale}/{category}.png"
            try:
                capture = verify_capture(screenshots, evidence, locale, category)
                for hashes, key, label in ((file_hashes, "sha256", "file"), (pixel_hashes, "rgb_sha256", "pixel")):
                    digest = capture[key]
                    require(digest not in hashes, f"Duplicate {label} hash: {name} and {hashes.get(digest)}")
                    hashes[digest] = name
                captures.append(capture)
            except (OSError, ValueError, struct.error) as error:
                errors.append(f"{name}: {error}")
    require(not errors, "\n".join(errors))
    return {
        "verified_at_utc": datetime.now(timezone.utc).isoformat(),
        "screenshot_count": len(captures),
        "locales": list(LOCALES),
        "categories": list(CATEGORIES),
        "dimensions": list(SIZE),
        "format": "24-bit RGB PNG, opaque",
        "verification": "PNG framing, CRC, complete lossless decode, unique file/pixel hashes, and decoded RGB equals capture evidence",
        "raw_framebuffer_note": "Raw framebuffers are not retained; their recorded hashes are not independently reverified.",
        "screenshots": captures,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--screenshots", type=Path, default=ROOT / "store/google-play/screenshots")
    parser.add_argument("--evidence-dir", "--evidence", dest="evidence", type=Path,
                        default=ROOT / "store/google-play/capture-evidence",
                        help="Capture metadata directory (defaults to checked-in evidence)")
    parser.add_argument("--manifest", type=Path, default=ROOT / "store/google-play/screenshots/manifest.json")
    parser.add_argument("--check-only", action="store_true", help="Validate without writing a manifest")
    options = parser.parse_args()
    try:
        manifest = verify_all(options.screenshots, options.evidence)
        if not options.check_only:
            options.manifest.parent.mkdir(parents=True, exist_ok=True)
            options.manifest.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    except (OSError, ValueError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        return 1
    print("PASS: all 16 screenshots are complete, unique, opaque RGB 1080x1920 PNGs and match their capture RGB hashes.")
    if not options.check_only:
        print(f"Manifest: {options.manifest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
