"""Build a Windows-compatible multi-size .ico from src/Chronolog/Assets/chronolog.png."""
from __future__ import annotations

import io
import os
import struct
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "src" / "Chronolog" / "Assets"
SRC = ASSETS / "chronolog.png"
ICO = ROOT / "assets" / "installer.ico"
APP_ICO = ASSETS / "chronolog.ico"
LARGE = ASSETS / "chronolog-large.png"


def rgba_to_bmp_dib(img: Image.Image) -> bytes:
    """ICO BMP DIB: BITMAPINFOHEADER + bottom-up BGRA XOR + AND mask."""
    w, h = img.size
    pixels = img.load()
    xor = bytearray()
    for y in range(h - 1, -1, -1):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            xor += bytes((b, g, r, a))

    row_bytes = ((w + 31) // 32) * 4
    and_mask = bytes(row_bytes * h)

    header = struct.pack(
        "<IiiHHIIiiII",
        40,  # biSize
        w,
        h * 2,  # XOR + AND
        1,
        32,
        0,
        len(xor) + len(and_mask),
        0,
        0,
        0,
        0,
    )
    return header + xor + and_mask


def main() -> None:
    src = Image.open(SRC).convert("RGBA")
    print(f"source {SRC} {src.size} {src.mode}")

    bmp_sizes = [16, 24, 32, 48, 64, 128]
    entries: list[tuple[int, int, int, bytes]] = []

    for sz in bmp_sizes:
        im = src.resize((sz, sz), Image.Resampling.NEAREST)
        entries.append((sz, sz, 32, rgba_to_bmp_dib(im)))

    im256 = src.resize((256, 256), Image.Resampling.NEAREST)
    buf = io.BytesIO()
    im256.save(buf, format="PNG")
    entries.append((256, 256, 32, buf.getvalue()))
    im256.save(LARGE)

    count = len(entries)
    offset = 6 + 16 * count
    directory = bytearray()
    payload = bytearray()

    for w, h, bpp, data in entries:
        wb = 0 if w >= 256 else w
        hb = 0 if h >= 256 else h
        directory += struct.pack(
            "<BBBBHHII",
            wb,
            hb,
            0,
            0,
            1,
            bpp,
            len(data),
            offset,
        )
        payload += data
        offset += len(data)

    header = struct.pack("<HHH", 0, 1, count)
    blob = header + directory + payload
    ICO.parent.mkdir(parents=True, exist_ok=True)
    ICO.write_bytes(blob)
    APP_ICO.write_bytes(blob)
    print(f"wrote {ICO} ({ICO.stat().st_size} bytes, {count} images)")
    print(f"wrote {APP_ICO}")

    raw = ICO.read_bytes()
    c = struct.unpack_from("<H", raw, 4)[0]
    for i in range(c):
        off = 6 + i * 16
        w, h, _, _, pl, bpp, size, o = struct.unpack_from("<BBBBHHII", raw, off)
        w = 256 if w == 0 else w
        kind = "PNG" if raw[o : o + 4] == b"\x89PNG" else "BMP"
        print(f"  {w}x{w} {kind} {size}B")


if __name__ == "__main__":
    main()
