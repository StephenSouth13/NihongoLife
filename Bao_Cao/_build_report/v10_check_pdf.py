import collections
import io
import sys
import fitz
from PIL import Image

path = sys.argv[1]
d = fitz.open(path)
sizes = collections.Counter()
fonts = collections.Counter()
small = []
for i, p in enumerate(d):
    for b in p.get_text('dict')['blocks']:
        for l in b.get('lines', []):
            for s in l['spans']:
                t = s['text'].strip()
                if not t:
                    continue
                sizes[round(s['size'], 1)] += len(t)
                fonts[s['font']] += len(t)
                if round(s['size'], 1) not in (13.0, 18.0):
                    small.append((i + 1, round(s['size'], 1), t[:50]))
print('pages', d.page_count)
print('sizes', sizes.most_common())
print('fonts', fonts.most_common())
print('other sizes', small[:15])
blank = [i + 1 for i, p in enumerate(d) if len(p.get_text().strip()) < 12 and not p.get_images()]
print('blank pages', blank)
short = [(i + 1, len(p.get_text())) for i, p in enumerate(d) if len(p.get_text()) < 500 and not p.get_images()]
print('thin pages', short)


def sheet(pages, name, dpi=55, cols=6):
    ims = [Image.open(io.BytesIO(d[n].get_pixmap(dpi=dpi).tobytes('png'))) for n in pages if n < d.page_count]
    w, h = ims[0].size
    rows = (len(ims) + cols - 1) // cols
    s = Image.new('RGB', (cols * w, rows * h), 'white')
    for k, im in enumerate(ims):
        s.paste(im, ((k % cols) * w, (k // cols) * h))
    s.save(name)


base = path.rsplit('.', 1)[0]
n = d.page_count
for k in range(0, n, 24):
    sheet(range(k, k + 24), f'{base}_sheet{k // 24}.png')
