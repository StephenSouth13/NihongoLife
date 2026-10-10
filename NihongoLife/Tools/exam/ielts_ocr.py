# -*- coding: utf-8 -*-
"""OCR a local Cambridge IELTS PDF page by page with the built-in Windows OCR engine (en-US).

Licensed material stays local: input PDFs and every output live under NihongoLife/LocalContent/ (gitignored, never
imported by Unity, never shipped). Output: LocalContent/IELTS/_sources/<book>/pNNN.txt (+ pNNN.png for checking).

    python Tools/exam/ielts_ocr.py "Cambridge IELTS 14.pdf" [first last]
    python Tools/exam/ielts_ocr.py --columns cam12 first last   → pNNN.col.txt: two-column passages read column by column
    python Tools/exam/ielts_ocr.py --flat cam13 first last      → re-OCR pNNN.txt after flat-field correction (faded margins)
"""
import asyncio
import os
import re
import sys

import fitz
from winsdk.windows.graphics.imaging import BitmapDecoder
from winsdk.windows.media.ocr import OcrEngine
from winsdk.windows.storage.streams import DataWriter, InMemoryRandomAccessStream

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SOURCES = os.path.join(ROOT, "LocalContent", "IELTS", "_sources")


async def ocr_png(engine, png_bytes):
    stream = InMemoryRandomAccessStream()
    writer = DataWriter(stream)
    writer.write_bytes(png_bytes)
    await writer.store_async()
    stream.seek(0)
    decoder = await BitmapDecoder.create_async(stream)
    bitmap = await decoder.get_software_bitmap_async()
    result = await engine.recognize_async(bitmap)
    # Keep the reading order of lines and a rough left indent so form layouts stay legible.
    lines = []
    for line in result.lines:
        words = list(line.words)
        x = min(w.bounding_rect.x for w in words) if words else 0
        y = min(w.bounding_rect.y for w in words) if words else 0
        h = max(w.bounding_rect.height for w in words) if words else 0
        lines.append((y, x, h, line.text))
    lines.sort(key=lambda t: (round(t[0] / 12), t[1]))
    # A vertical gap clearly larger than the usual line pitch is a paragraph break: keep it as an empty line.
    pitches = sorted(b[0] - a[0] for a, b in zip(lines, lines[1:]) if b[0] - a[0] > 5)
    pitch = pitches[len(pitches) // 2] if pitches else 40
    out = []
    for k, (y, x, h, text) in enumerate(lines):
        if k > 0 and y - lines[k - 1][0] > pitch * 1.55:
            out.append("")
        out.append(("  " * int(x // 120)) + text)
    return "\n".join(out)


def _layout(lines):
    """(y, x, h, text) lines in reading order → text with an empty line wherever the vertical gap is a paragraph break."""
    pitches = sorted(b[0] - a[0] for a, b in zip(lines, lines[1:]) if b[0] - a[0] > 5)
    pitch = pitches[len(pitches) // 2] if pitches else 40
    out = []
    for k, (y, x, h, text) in enumerate(lines):
        if k > 0 and y - lines[k - 1][0] > pitch * 1.55:
            out.append("")
        out.append(text)
    return out


async def ocr_columns(engine, png_bytes, width):
    """Two-column page: full-width lines above the columns, then the left column, then the right column, then the
    full-width lines below (page number). A line belongs to a column when it lies entirely on one side of the middle."""
    stream = InMemoryRandomAccessStream()
    writer = DataWriter(stream)
    writer.write_bytes(png_bytes)
    await writer.store_async()
    stream.seek(0)
    decoder = await BitmapDecoder.create_async(stream)
    bitmap = await decoder.get_software_bitmap_async()
    result = await engine.recognize_async(bitmap)
    mid = width / 2
    full, left, right = [], [], []
    for line in result.lines:
        words = list(line.words)
        if not words:
            continue
        x0 = min(w.bounding_rect.x for w in words)
        x1 = max(w.bounding_rect.x + w.bounding_rect.width for w in words)
        y = min(w.bounding_rect.y for w in words)
        h = max(w.bounding_rect.height for w in words)
        item = (y, x0, h, line.text)
        (right if x0 > mid - 20 else left if x1 < mid + width * 0.1 else full).append(item)
    head = re.compile(r"^\s*(Test \d+|Reading|Listening|\d{1,3})\s*$")
    # The columns start at the right column's first line (the running head aside); left-hand lines above it are the
    # page heading and the passage introduction.
    top = min([l[0] for l in right if not head.match(l[3])], default=10 ** 9) - 10
    above = [l for l in full + left if l[0] < top]
    left = [l for l in left if l[0] >= top]
    below = [l for l in full if l[0] >= top]
    for col in (above, left, right, below):
        col.sort(key=lambda t: (round(t[0] / 12), t[1]))
    out = _layout(above) + [""] + _layout(left) + ["<col>"] + _layout(right) + [""] + _layout(below)  # <col>: column turn
    return "\n".join(out)


def flat_png(path):
    """The page with its uneven lighting removed (divided by a heavily blurred copy), so text faded by the scan's
    gutter shadow reads as black on white."""
    import io
    import numpy as np
    from PIL import Image, ImageFilter
    gray = Image.open(path).convert("L")
    background = np.asarray(gray.filter(ImageFilter.GaussianBlur(30)), dtype=np.float32) + 1
    ratio = np.asarray(gray, dtype=np.float32) / background  # ~1 on paper, lower on ink
    flat = Image.fromarray(np.clip((ratio - 0.55) / 0.4 * 255, 0, 255).astype(np.uint8))
    buf = io.BytesIO()
    flat.save(buf, "PNG")
    return buf.getvalue()


async def flat(book, first, last):
    engine = OcrEngine.try_create_from_user_profile_languages()
    out = os.path.join(SOURCES, book)
    for p in range(first, last + 1):
        text = await ocr_png(engine, flat_png(os.path.join(out, f"p{p:03d}.png")))
        with open(os.path.join(out, f"p{p:03d}.txt"), "w", encoding="utf-8") as fh:
            fh.write(text)
    print(f"{book}: flat-field OCR {first}-{last}")


async def columns(book, first, last):
    engine = OcrEngine.try_create_from_user_profile_languages()
    from PIL import Image
    out = os.path.join(SOURCES, book)
    for p in range(first, last + 1):
        png = os.path.join(out, f"p{p:03d}.png")
        with open(png, "rb") as fh:
            data = fh.read()
        text = await ocr_columns(engine, data, Image.open(png).size[0])
        with open(os.path.join(out, f"p{p:03d}.col.txt"), "w", encoding="utf-8") as fh:
            fh.write(text)
    print(f"{book}: columns {first}-{last}")


async def main(pdf_name, first=None, last=None):
    pdf = os.path.join(SOURCES, pdf_name)
    book = os.path.splitext(pdf_name)[0].replace("Cambridge IELTS ", "cam")
    out = os.path.join(SOURCES, book)
    os.makedirs(out, exist_ok=True)
    engine = OcrEngine.try_create_from_user_profile_languages()
    doc = fitz.open(pdf)
    first = first or 1
    last = min(last or len(doc), len(doc))
    for p in range(first, last + 1):
        txt = os.path.join(out, f"p{p:03d}.txt")
        if os.path.exists(txt):
            continue
        pix = doc[p - 1].get_pixmap(dpi=200)
        png = pix.tobytes("png")
        with open(os.path.join(out, f"p{p:03d}.png"), "wb") as fh:
            fh.write(png)
        text = await ocr_png(engine, png)
        with open(txt, "w", encoding="utf-8") as fh:
            fh.write(text)
    print(f"{book}: pages {first}-{last} → {out}")


if __name__ == "__main__":
    args = sys.argv[1:]
    if args[0] == "--flat":
        asyncio.run(flat(args[1], int(args[2]), int(args[3])))
        sys.exit()
    if args[0] == "--columns":
        asyncio.run(columns(args[1], int(args[2]), int(args[3])))
        sys.exit()
    asyncio.run(main(args[0], int(args[1]) if len(args) > 1 else None, int(args[2]) if len(args) > 2 else None))
