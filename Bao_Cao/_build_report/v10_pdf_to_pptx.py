"""Rebuilds NihongoLife_Thuyet_Trinh_Capstone_v9 as an editable .pptx from its PDF (the .pptx was lost; the PDF is the
latest deck): backgrounds, cards, lines, pictures and text become native PowerPoint objects at the same positions, every
title goes into the slide's title placeholder, all text is Roboto (Yu Gothic only for Japanese characters, which Roboto
does not have), bullets are real bullets, video links stay clickable.
Run: python pdf_to_pptx.py <in.pdf> <out.pptx>
"""
import io
import re
import sys
import fitz
from lxml import etree
from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_CONNECTOR, MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.oxml.ns import qn
from pptx.util import Emu, Pt

SRC, OUT = sys.argv[1], sys.argv[2]
LATIN, EAST = 'Roboto', 'Yu Gothic'
PT = 12700
A = '{http://schemas.openxmlformats.org/drawingml/2006/main}'

pdf = fitz.open(SRC)
prs = Presentation()
prs.slide_width = Emu(int(pdf[0].rect.width * PT))
prs.slide_height = Emu(int(pdf[0].rect.height * PT))
TITLE_ONLY = prs.slide_layouts[5]

# The layout must not bring anything of its own: placeholders for date, footer and number are dropped.
for layout in (TITLE_ONLY,):
    for ph in list(layout.placeholders):
        if ph.placeholder_format.type is not None and ph.placeholder_format.idx != 0:
            ph._element.getparent().remove(ph._element)


def rgb(c):
    return RGBColor(*[int(round(v * 255)) for v in c])


def color_int(v):
    return RGBColor((v >> 16) & 255, (v >> 8) & 255, v & 255)


def e(v):
    return Emu(int(round(v * PT)))


image_cache = {}


def image_stream(xref):
    if xref in image_cache:
        return io.BytesIO(image_cache[xref])
    info = pdf.extract_image(xref)
    smask = info.get('smask', 0)
    pix = fitz.Pixmap(pdf, xref)
    if pix.colorspace is not None and pix.colorspace.n > 3:
        pix = fitz.Pixmap(fitz.csRGB, pix)
    if smask:
        mask = fitz.Pixmap(pdf, smask)
        pix = fitz.Pixmap(pix, mask)
    # Screenshots as JPEG (the deck stays light); logos, icons and anything transparent stay PNG.
    data = pix.tobytes('jpeg', jpg_quality=88) if not smask and not pix.alpha and pix.width >= 700 else pix.tobytes('png')
    image_cache[xref] = data
    return io.BytesIO(data)


def is_oval(d):
    r = d['rect']
    kinds = [i[0] for i in d['items']]
    return kinds.count('c') >= 4 and kinds.count('l') == 0 or (kinds.count('c') >= 4 and abs(r.width - r.height) < 1 and kinds.count('l') <= 1)


def set_run_font(run, size, bold, italic, color):
    run.font.size = Pt(round(size, 1))
    run.font.bold = bold
    run.font.italic = italic
    run.font.color.rgb = color
    r_pr = run._r.get_or_add_rPr()
    for tag in ('latin', 'ea', 'cs'):
        old = r_pr.find(A + tag)
        if old is not None:
            r_pr.remove(old)
    # PowerPoint draws "×", "·", "–" with the East Asian font: only real Japanese text gets it.
    japanese = re.search('[\u3040-\u30ff\u3400-\u9fff\uff00-\uffef]', run.text) is not None
    for tag, face in (('latin', LATIN), ('ea', EAST if japanese else LATIN), ('cs', LATIN)):
        el = etree.SubElement(r_pr, A + tag)
        el.set('typeface', face)


def is_bullet(span):
    return span['text'].strip() in ('•', '') and 'OpenSymbol' in span['font']


def spans_of(line):
    return [s for s in line['spans'] if s['text']]


def style(s):
    return (round(s['size'], 1), 'Bold' in s['font'], 'Italic' in s['font'], s['color'])


def merged_runs(spans):
    """Adjacent spans of one style become one run; CJK spans (another font in the PDF) join their neighbours."""
    runs = []
    for s in spans:
        k = style(s)
        if runs and runs[-1][0] == k:
            runs[-1][1] += s['text']
        else:
            runs.append([k, s['text']])
    return runs


def fill_paragraph(par, line_groups, pitch):
    """line_groups: wrapped lines of one paragraph (kept as line breaks so the deck looks exactly like the PDF)."""
    for n, spans in enumerate(line_groups):
        if n:
            par.add_line_break()
        for (size, bold, italic, color), text in merged_runs(spans):
            run = par.add_run()
            run.text = text.rstrip() if n < len(line_groups) - 1 else text
            set_run_font(run, size, bold, italic, color_int(color))
    if pitch:
        par.line_spacing = Pt(round(pitch, 1))


def text_blocks(page):
    """Lines grouped into blocks: a block is one column of text whose lines follow at one line pitch."""
    out = []
    for b in page.get_text('dict')['blocks']:
        if 'lines' not in b:
            continue
        lines = []
        for l in b['lines']:
            sp = spans_of(l)
            if sp and any(s['text'].strip() for s in sp):
                lines.append({'bbox': fitz.Rect(l['bbox']), 'spans': sp, 'base': sp[0]['origin'][1]})
        lines.sort(key=lambda l: (round(l['base']), l['bbox'].x0))
        # A bullet glyph can come as its own line at the same height: join it to the text beside it.
        joined = []
        for l in lines:
            near = joined and (l['bbox'].x0 - joined[-1]['bbox'].x1 < 14 or all(is_bullet(x) for x in joined[-1]['spans']))
            if joined and abs(joined[-1]['base'] - l['base']) < 2 and near:
                joined[-1]['spans'] += l['spans']
                joined[-1]['bbox'] |= l['bbox']
            else:
                joined.append(l)
        if joined:
            out.append(joined)
    # Split blocks whose pitch jumps (two text pieces the PDF put in one block).
    result = []
    for lines in out:
        group = [lines[0]]
        for l in lines[1:]:
            prev = group[-1]
            size = prev['spans'][0]['size']
            gap = l['base'] - prev['base']
            if gap < 2 or gap > size * 1.45 or abs(l['bbox'].x0 - prev['bbox'].x0) > 30:
                result.append(group)
                group = [l]
            else:
                group.append(l)
        result.append(group)
    return result


def container_of(rect, drawings):
    best = None
    for d in drawings:
        r = d['rect']
        if r.width > 900 or d.get('fill') is None:
            continue
        if r.x0 - 1 <= rect.x0 and r.x1 + 1 >= rect.x1 and r.y0 - 1 <= rect.y0 and r.y1 + 1 >= rect.y1:
            if best is None or r.get_area() < best.get_area():
                best = r
    return best


def add_block(slide, lines, drawings, title_shape=None):
    rect = fitz.Rect(lines[0]['bbox'])
    for l in lines:
        rect |= l['bbox']
    bullets = all(True for _ in lines) and any(is_bullet(l['spans'][0]) for l in lines)
    pitches = [b['base'] - a['base'] for a, b in zip(lines, lines[1:])]
    pitch = pitches[0] if pitches else None
    box_rect = fitz.Rect(rect)
    align = PP_ALIGN.LEFT
    host = container_of(rect, drawings)
    if host is not None and not bullets:
        centred = all(abs((l['bbox'].x0 + l['bbox'].x1) / 2 - (host.x0 + host.x1) / 2) < 3 for l in lines)
        if centred and rect.width < host.width * 0.95:
            align = PP_ALIGN.CENTER
            box_rect = fitz.Rect(host.x0, rect.y0, host.x1, rect.y1)
    if title_shape is not None:
        shape = title_shape
        shape.left, shape.top, shape.width, shape.height = e(box_rect.x0), e(box_rect.y0), e(max(box_rect.width + 12, 400)), e(box_rect.height)
    else:
        shape = slide.shapes.add_textbox(e(box_rect.x0), e(box_rect.y0), e(box_rect.width + (12 if align == PP_ALIGN.LEFT else 0)), e(box_rect.height))
    tf = shape.text_frame
    tf.word_wrap = False
    tf.margin_left = tf.margin_right = tf.margin_top = tf.margin_bottom = 0
    tf.vertical_anchor = MSO_ANCHOR.TOP
    body_pr = tf._txBody.find(A + 'bodyPr')
    for child in list(body_pr):
        body_pr.remove(child)
    etree.SubElement(body_pr, A + 'noAutofit')
    # Paragraphs: a bullet starts a new one; other lines are line breaks inside the paragraph.
    groups = []
    for l in lines:
        sp = l['spans']
        if is_bullet(sp[0]):
            rest = [dict(s) for s in sp[1:]]
            if rest:
                rest[0]['text'] = rest[0]['text'].lstrip()  # the bullet's own indent spaces the text
            groups.append([rest])
        elif groups and not bullets:
            groups[-1].append(sp)
        elif groups:
            groups[-1].append(sp)
        else:
            groups.append([sp])
    first = True
    for g in groups:
        par = tf.paragraphs[0] if first else tf.add_paragraph()
        first = False
        par.alignment = align
        fill_paragraph(par, g, pitch)
        if bullets:
            p_pr = par._p.get_or_add_pPr()
            text_x = min(s['bbox'][0] for s in g[0])
            indent = max(text_x - rect.x0, 18)
            p_pr.set('marL', str(int(indent * PT)))
            p_pr.set('indent', str(-int(indent * PT)))
            bu = etree.SubElement(p_pr, A + 'buChar')
            bu.set('char', '•')
    return shape


TEXT_ONLY_LINK = re.compile(r'^https?://')

for idx, page in enumerate(pdf):
    slide = prs.slides.add_slide(TITLE_ONLY)
    title = slide.shapes.title
    drawings = page.get_drawings()
    # Background: the first full-page fill that is not the white page itself.
    bg = None
    shapes = []
    for d in drawings:
        r = d['rect']
        if r.width > 950 and r.height > 530:
            if d.get('fill') is not None:
                bg = d['fill']
            continue
        shapes.append(d)
    if bg is not None:
        f = slide.background.fill
        f.solid()
        f.fore_color.rgb = rgb(bg)
    # Draw in PDF order: shapes and pictures interleave by sequence number.
    for d in shapes:
        r = d['rect']
        kinds = [i[0] for i in d['items']]
        if d.get('fill') is None and d.get('color') is not None and r.height < 2 or r.width < 2:
            line = slide.shapes.add_connector(MSO_CONNECTOR.STRAIGHT, e(r.x0), e(r.y0), e(r.x1), e(r.y1))
            line.line.color.rgb = rgb(d['color'])
            line.line.width = Pt(d.get('width') or 1)
            continue
        if d.get('fill') is None and d.get('color') is None:
            continue
        if is_oval(d):
            shp = slide.shapes.add_shape(MSO_SHAPE.OVAL, e(r.x0), e(r.y0), e(r.width), e(r.height))
        elif 'c' in kinds or len(kinds) > 8:
            shp = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, e(r.x0), e(r.y0), e(r.width), e(r.height))
            first_curve = next((i for i in d['items'] if i[0] == 'c'), None)
            radius = 8
            if first_curve is not None:
                radius = max(abs(first_curve[1].x - first_curve[4].x), abs(first_curve[1].y - first_curve[4].y))
            shp.adjustments[0] = min(0.5, radius / min(r.width, r.height))
        else:
            shp = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, e(r.x0), e(r.y0), e(r.width), e(r.height))
        if d.get('fill') is not None:
            shp.fill.solid()
            shp.fill.fore_color.rgb = rgb(d['fill'])
        else:
            shp.fill.background()
        if d.get('color') is not None:
            shp.line.color.rgb = rgb(d['color'])
            shp.line.width = Pt(d.get('width') or 1)
        else:
            shp.line.fill.background()
        shp.shadow.inherit = False
    links = [l for l in page.get_links() if l.get('uri')]
    for im in page.get_image_info(xrefs=True):
        b = fitz.Rect(im['bbox'])
        if not im['xref']:
            continue
        pic = slide.shapes.add_picture(image_stream(im['xref']), e(b.x0), e(b.y0), e(b.width), e(b.height))
        for l in links:
            if fitz.Rect(l['from']).intersects(b) and (fitz.Rect(l['from']) & b).get_area() > b.get_area() * 0.5:
                pic.click_action.hyperlink.address = l['uri']
    # Text: the largest text at the top is the title.
    blocks = text_blocks(page)
    # (Big stat numbers are 54 pt: the title is the top text of 36–44 pt.)
    titles = [ls for ls in blocks if 36 <= ls[0]['spans'][0]['size'] <= 44]
    biggest = min(titles, key=lambda ls: ls[0]['base']) if titles else None
    placed = False
    for ls in blocks:
        s = None
        if ls is biggest and not placed:
            s = add_block(slide, ls, shapes, title)
            placed = True
        else:
            s = add_block(slide, ls, shapes)
        for l in links:
            lr = fitz.Rect(l['from'])
            for par in s.text_frame.paragraphs:
                for run in par.runs:
                    if ls and lr.intersects(ls[0]['bbox']) and 'youtu' in run.text:
                        run.hyperlink.address = l['uri']
    if not placed:
        title._element.getparent().remove(title._element)

# Theme fonts: anything added later in PowerPoint is Roboto too.
master_part = prs.slide_master.part
theme_part = next(r.target_part for r in master_part.rels.values() if r.reltype.endswith('/theme'))
theme = etree.fromstring(theme_part.blob)
for tag in ('majorFont', 'minorFont'):
    font = theme.find('.//' + A + tag)
    font.find(A + 'latin').set('typeface', LATIN)
    font.find(A + 'ea').set('typeface', EAST)
    font.find(A + 'cs').set('typeface', LATIN)
for tag in ('hlink', 'folHlink'):  # links keep the deck's gold, readable on the dark slides
    node = theme.find('.//' + A + tag)
    for child in list(node):
        node.remove(child)
    etree.SubElement(node, A + 'srgbClr').set('val', 'F7BD3D')
theme_part._blob = etree.tostring(theme, xml_declaration=True, encoding='UTF-8', standalone=True)

prs.core_properties.title = 'NihongoLife — Thuyết trình đồ án tốt nghiệp'
prs.core_properties.author = 'Quách Thành Long'
prs.save(OUT)
print('slides', len(prs.slides))
