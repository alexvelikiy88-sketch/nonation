"""Offline synthetic tests: these do not download or claim real Unity import."""
import struct
import unittest

from fetch_textures import git_blob_hash, parse_tga_header, resize_tga_box


def fixture(w=4, h=2, color=(12, 34, 56, 255)):
    header = bytearray(18)
    header[2] = 2
    struct.pack_into('<HHBB', header, 12, w, h, 32, 0x20)
    return bytes(header) + bytes(color) * (w * h)


class TextureTests(unittest.TestCase):
    def test_git_blob_hash_known_vector(self):
        self.assertEqual(git_blob_hash(b'test\n'), '9daeafb9864cf43055ae93beb0afd6c7d144bfa4')

    def test_resize_keeps_bgra_and_orientation(self):
        texture = fixture()
        resized = resize_tga_box(texture, 64)
        self.assertEqual(resized, texture)
        # A 128x64 fixture is reduced to 64x32; homogeneous pixels remain identical.
        data = fixture(128, 64)
        smaller = resize_tga_box(data, 64)
        self.assertEqual(parse_tga_header(smaller), (64, 32, 18, 0x20))
        self.assertEqual(smaller[18:34], bytes((12, 34, 56, 255)) * 4)
        self.assertEqual(len(smaller), 18 + 64 * 32 * 4)

    def test_box_average(self):
        blob = bytearray(fixture(128, 64))
        blob[18:22] = bytes((0, 0, 0, 0))
        blob[22:26] = bytes((100, 100, 100, 200))
        blob[18 + 128 * 4:22 + 128 * 4] = bytes((200, 200, 200, 255))
        blob[22 + 128 * 4:26 + 128 * 4] = bytes((100, 100, 100, 255))
        reduced = resize_tga_box(bytes(blob), 64)
        self.assertEqual(list(reduced[18:22]), [100, 100, 100, 178])

    def test_bad_headers_fail_closed(self):
        with self.assertRaisesRegex(ValueError, 'header missing'):
            parse_tga_header(b'abc')
        corrupt = bytearray(fixture())
        corrupt[2] = 10  # RLE not supported
        with self.assertRaisesRegex(ValueError, 'uncompressed'):
            resize_tga_box(bytes(corrupt), 64)
        with self.assertRaisesRegex(ValueError, 'Truncated'):
            resize_tga_box(fixture(64, 64)[:100], 64)
        with self.assertRaisesRegex(ValueError, 'max_edge'):
            resize_tga_box(fixture(), 8)


if __name__ == '__main__':
    unittest.main()
