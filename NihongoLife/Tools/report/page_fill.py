"""Prints the pages of a PDF filled less than 72 % (headers / footers ignored)."""
import sys

import fitz

d = fitz.open(sys.argv[1])
h = d[0].rect.height
for i, p in enumerate(d):
    blocks = [b for b in p.get_text('blocks') if b[3] < h - 70 and b[1] > 60]
    imgs = [im['bbox'] for im in p.get_image_info() if im['bbox'][3] < h - 40]
    ys = [b[3] for b in blocks] + [b[3] for b in imgs]
    fill = (max(ys) if ys else 0) / h
    if fill < 0.72:
        print(i + 1, round(fill, 2), (blocks[0][4][:46].replace('\n', ' ') if blocks else ''))
print('pages', len(d))
