# -*- coding: utf-8 -*-
"""OCR a local Cambridge IELTS PDF page by page with the built-in Windows OCR engine (en-US).

Licensed material stays local: input PDFs and every output live under NihongoLife/LocalContent/ (gitignored, never
imported by Unity, never shipped). Output: LocalContent/IELTS/_sources/<book>/pNNN.txt (+ pNNN.png for checking).

    python Tools/exam/ielts_ocr.py "Cambridge IELTS 14.pdf" [first last]
"""
import asyncio
import os
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
    asyncio.run(main(args[0], int(args[1]) if len(args) > 1 else None, int(args[2]) if len(args) > 2 else None))
