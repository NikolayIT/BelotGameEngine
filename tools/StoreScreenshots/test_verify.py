"""Synthetic PNG/evidence regression tests; no device or real screenshots needed.

Run: python -B -m unittest discover -s tools/StoreScreenshots -p test_verify.py -v
"""

import contextlib
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch
import zlib

SPEC = importlib.util.spec_from_file_location("store_screenshot_verify", Path(__file__).with_name("verify.py"))
verify = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(verify)


def chunk(kind, payload):
    return struct.pack(">I", len(payload)) + kind + payload + struct.pack(">I", zlib.crc32(kind + payload))


def make_png(width, height, pixels, *, depth=8, color=2, compressed=None, level=1):
    if compressed is None:
        stride = width * 3
        rows = b"".join(b"\0" + pixels[offset:offset + stride] for offset in range(0, len(pixels), stride))
        compressed = zlib.compress(rows, level)
    return (
        verify.SIGNATURE
        + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, depth, color, 0, 0, 0))
        + chunk(b"IDAT", compressed)
        + chunk(b"IEND", b"")
    )


class PngDecoderTests(unittest.TestCase):
    # Fixed RGB and independently calculated filter bytes exercise None, Sub,
    # Up, Average and all three Paeth predictors without an encoder round-trip.
    PIXELS = bytes((
        10, 20, 30, 40, 50, 60, 70, 80, 90,
        11, 22, 33, 44, 55, 66, 77, 88, 99,
        9, 40, 80, 7, 3, 1, 250, 0, 100,
        19, 3, 9, 100, 60, 50, 44, 33, 22,
        250, 0, 30, 2, 100, 70, 60, 10, 255,
    ))
    FILTERED = bytes((
        0, 10, 20, 30, 40, 50, 60, 70, 80, 90,
        1, 11, 22, 33, 33, 33, 33, 33, 33, 33,
        2, 254, 18, 47, 219, 204, 191, 173, 168, 1,
        3, 15, 239, 225, 87, 57, 45, 125, 3, 203,
        4, 231, 253, 21, 8, 40, 20, 58, 206, 205,
    ))

    def test_all_five_row_filters_decode_to_known_pixels(self):
        data = make_png(3, 5, self.PIXELS, compressed=zlib.compress(self.FILTERED))
        self.assertEqual(verify.decode_rgb_png(data, (3, 5)), self.PIXELS)

    def test_paeth_ties_follow_the_png_predictor_order(self):
        self.assertEqual(verify.paeth(0, 3, 2), 0)  # Left ties upper-left.
        self.assertEqual(verify.paeth(3, 0, 2), 0)  # Above ties upper-left.

    def test_multiple_idat_chunks_form_one_complete_stream(self):
        packed = zlib.compress(self.FILTERED)
        data = (
            verify.SIGNATURE
            + chunk(b"IHDR", struct.pack(">IIBBBBB", 3, 5, 8, 2, 0, 0, 0))
            + chunk(b"IDAT", packed[:7])
            + chunk(b"IDAT", packed[7:])
            + chunk(b"IEND", b"")
        )
        self.assertEqual(verify.decode_rgb_png(data, (3, 5)), self.PIXELS)

    def test_native_sized_opaque_rgb_pixels_are_unchanged(self):
        pixels = bytes((20, 40, 60)) * (1080 * 1920)
        self.assertEqual(verify.decode_rgb_png(make_png(1080, 1920, pixels)), pixels)

    def test_alpha_palette_grayscale_and_sixteen_bit_images_are_rejected(self):
        for depth, color in ((8, 6), (8, 4), (8, 3), (8, 0), (16, 2)):
            with self.subTest(depth=depth, color=color):
                with self.assertRaisesRegex(verify.VerificationError, "opaque 24-bit RGB"):
                    verify.decode_rgb_png(make_png(3, 5, self.PIXELS, depth=depth, color=color), (3, 5))

    def test_wrong_dimensions_are_rejected_by_default(self):
        with self.assertRaisesRegex(verify.VerificationError, "Wrong dimensions"):
            verify.decode_rgb_png(make_png(3, 5, self.PIXELS))

    def test_corrupted_chunk_crc_is_rejected(self):
        data = bytearray(make_png(3, 5, self.PIXELS))
        data[41] ^= 1  # Mutate the IDAT payload without updating its CRC.
        with self.assertRaisesRegex(verify.VerificationError, "CRC mismatch"):
            verify.decode_rgb_png(data, (3, 5))

    def test_truncated_or_incomplete_pngs_are_rejected(self):
        data = make_png(3, 5, self.PIXELS)
        for broken in (data[:4], data[:10], data[:40], data[:-1], data[:-12]):
            with self.subTest(length=len(broken)):
                with self.assertRaises(verify.VerificationError):
                    verify.decode_rgb_png(broken, (3, 5))

    def test_invalid_truncated_or_extra_zlib_data_is_rejected(self):
        packed = zlib.compress(self.FILTERED)
        for broken in (b"not zlib", packed[:-1], packed + b"trailing", packed + packed):
            with self.subTest(compressed=broken):
                with self.assertRaises(verify.VerificationError):
                    verify.decode_rgb_png(make_png(3, 5, self.PIXELS, compressed=broken), (3, 5))

    def test_wrong_decoded_length_and_unknown_filters_are_rejected(self):
        for rows in (self.FILTERED[:-1], self.FILTERED + b"extra", b"\5" + self.FILTERED[1:]):
            with self.subTest(rows=rows):
                with self.assertRaises(verify.VerificationError):
                    verify.decode_rgb_png(make_png(3, 5, self.PIXELS, compressed=zlib.compress(rows)), (3, 5))

    def test_trailing_file_bytes_and_transparency_chunks_are_rejected(self):
        data = make_png(3, 5, self.PIXELS)
        for broken in (data + b"extra", data[:-12] + chunk(b"tRNS", b"\0" * 6) + data[-12:]):
            with self.subTest(data=broken):
                with self.assertRaises(verify.VerificationError):
                    verify.decode_rgb_png(broken, (3, 5))


class CaptureEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.fixtures = []
        pairs = ((locale, category) for locale in verify.LOCALES for category in verify.CATEGORIES)
        for index, (locale, category) in enumerate(pairs):
            pixels = bytes((index, index + 20, index + 40)) * (1080 * 1920)
            data = make_png(1080, 1920, pixels)
            metadata = {
                "file": f"store/google-play/screenshots/{locale}/{category}.png",
                "locale": locale,
                "serial": "127.0.0.1:5575",
                "package": "com.nksolutions.belot",
                "width": 1080,
                "height": 1920,
                "format": "24-bit RGB PNG, opaque",
                "sha256": hashlib.sha256(data).hexdigest(),
                "rgb_sha256": hashlib.sha256(pixels).hexdigest(),
                "raw_sha256": hashlib.sha256(b"synthetic-raw-" + bytes((index,))).hexdigest(),
                "captured_at_unix": 1790000000,
                "size_bytes": len(data),
            }
            cls.fixtures.append((locale, category, data, metadata))

    def setUp(self):
        directory = tempfile.TemporaryDirectory(prefix="belot-png-tests-")
        self.addCleanup(directory.cleanup)
        self.screenshots = Path(directory.name) / "screenshots"
        self.evidence = Path(directory.name) / "evidence"
        self.evidence.mkdir()
        for index, fixture in enumerate(self.fixtures):
            self.write_fixture(index, fixture[2], fixture[3])

    def paths(self, index):
        locale, category, _, _ = self.fixtures[index]
        return self.screenshots / locale / f"{category}.png", self.evidence / f"{locale}-{category}.json"

    def write_fixture(self, index, data, metadata):
        image, evidence = self.paths(index)
        image.parent.mkdir(parents=True, exist_ok=True)
        image.write_bytes(data)
        evidence.write_text(json.dumps(metadata), encoding="utf-8")

    def test_all_sixteen_distinct_captures_produce_the_expected_manifest(self):
        manifest = verify.verify_all(self.screenshots, self.evidence)
        self.assertEqual(manifest["screenshot_count"], 16)
        self.assertEqual(manifest["dimensions"], [1080, 1920])
        self.assertEqual(manifest["locales"], ["bg", "en-US"])
        for locale in verify.LOCALES:
            captures = [capture for capture in manifest["screenshots"] if capture["locale"] == locale]
            self.assertEqual([capture["category"] for capture in captures], list(verify.CATEGORIES))
        for capture in manifest["screenshots"]:
            self.assertEqual(capture["decoded_rgb_sha256"], capture["rgb_sha256"])

    def test_missing_category_or_evidence_is_rejected(self):
        for path in self.paths(0):
            with self.subTest(path=path.name):
                original = path.read_bytes()
                path.unlink()
                with self.assertRaisesRegex(verify.VerificationError, "Missing"):
                    verify.verify_all(self.screenshots, self.evidence)
                path.write_bytes(original)

    def test_unexpected_category_is_rejected(self):
        (self.screenshots / "bg" / "09-extra.PNG").write_bytes(self.fixtures[0][2])
        with self.assertRaisesRegex(verify.VerificationError, "Unexpected screenshot categories/locales"):
            verify.verify_all(self.screenshots, self.evidence)

    def test_identical_file_hashes_are_rejected(self):
        metadata = dict(self.fixtures[1][3])
        metadata.update({key: self.fixtures[0][3][key] for key in ("sha256", "rgb_sha256", "size_bytes")})
        self.write_fixture(1, self.fixtures[0][2], metadata)
        with self.assertRaisesRegex(verify.VerificationError, "Duplicate file hash"):
            verify.verify_all(self.screenshots, self.evidence)

    def test_identical_pixels_with_different_png_compression_are_rejected(self):
        pixels = bytes((0, 20, 40)) * (1080 * 1920)
        data = make_png(1080, 1920, pixels, level=9)
        self.assertNotEqual(data, self.fixtures[0][2])
        metadata = dict(self.fixtures[1][3])
        metadata.update(sha256=hashlib.sha256(data).hexdigest(),
                        rgb_sha256=hashlib.sha256(pixels).hexdigest(), size_bytes=len(data))
        self.write_fixture(1, data, metadata)
        with self.assertRaisesRegex(verify.VerificationError, "Duplicate pixel hash"):
            verify.verify_all(self.screenshots, self.evidence)

    def test_mismatched_pixel_hash_file_hash_or_capture_origin_is_rejected(self):
        for key, value in (("rgb_sha256", "1" * 64), ("sha256", "1" * 64),
                           ("serial", "other-device"), ("package", "other.app"),
                           ("width", 540), ("size_bytes", 1)):
            with self.subTest(key=key):
                metadata = dict(self.fixtures[0][3])
                metadata[key] = value
                self.write_fixture(0, self.fixtures[0][2], metadata)
                with self.assertRaisesRegex(verify.VerificationError, "Capture evidence mismatch: " + key):
                    verify.verify_capture(self.screenshots, self.evidence, "bg", "01-gameplay")

    def test_malformed_raw_hash_or_timestamp_is_rejected(self):
        for key, value in (("raw_sha256", "bad"), ("raw_sha256", None),
                           ("captured_at_unix", 0), ("captured_at_unix", float("nan")),
                           ("captured_at_unix", True)):
            with self.subTest(key=key, value=value):
                metadata = dict(self.fixtures[0][3])
                metadata[key] = value
                self.write_fixture(0, self.fixtures[0][2], metadata)
                with self.assertRaises(verify.VerificationError):
                    verify.verify_capture(self.screenshots, self.evidence, "bg", "01-gameplay")


class CommandLineTests(unittest.TestCase):
    def test_checked_in_evidence_default_and_both_option_names(self):
        for arguments, expected in (
            ([], verify.ROOT / "store/google-play/capture-evidence"),
            (["--evidence-dir", "custom"], Path("custom")),
            (["--evidence", "legacy"], Path("legacy")),
        ):
            with self.subTest(arguments=arguments):
                with patch.object(sys, "argv", ["verify.py", "--check-only", *arguments]), \
                        patch.object(verify, "verify_all", return_value={}) as check, \
                        contextlib.redirect_stdout(io.StringIO()):
                    self.assertEqual(verify.main(), 0)
                self.assertEqual(check.call_args.args[1], expected)

    def test_failed_verification_does_not_overwrite_an_existing_manifest(self):
        with tempfile.TemporaryDirectory(prefix="belot-manifest-tests-") as directory:
            manifest = Path(directory) / "manifest.json"
            manifest.write_text("existing manifest", encoding="utf-8")
            with patch.object(sys, "argv", ["verify.py", "--manifest", str(manifest)]), \
                    patch.object(verify, "verify_all", side_effect=verify.VerificationError("missing category")), \
                    contextlib.redirect_stderr(io.StringIO()):
                self.assertEqual(verify.main(), 1)
            self.assertEqual(manifest.read_text(encoding="utf-8"), "existing manifest")

    def test_check_only_does_not_write_a_manifest(self):
        with tempfile.TemporaryDirectory(prefix="belot-manifest-tests-") as directory:
            manifest = Path(directory) / "manifest.json"
            with patch.object(sys, "argv", ["verify.py", "--check-only", "--manifest", str(manifest)]), \
                    patch.object(verify, "verify_all", return_value={}), \
                    contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(verify.main(), 0)
            self.assertFalse(manifest.exists())


if __name__ == "__main__":
    unittest.main()
