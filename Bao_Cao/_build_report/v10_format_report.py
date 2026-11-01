"""Bao_Cao v9 -> v10: same content, formal format.
Times New Roman, 13 pt body / 18 pt chapter titles (only those two sizes), line spacing 1.5, black text, real bullet
lists, and real Word fields for the table of contents and the lists of figures and tables.
Run: python format_report.py <in.docx> <out.docx>   then update_fields.ps1 fills the fields through Word.
"""
import copy
import re
import sys
import docx
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn

SRC, OUT = sys.argv[1], sys.argv[2]
TNR = 'Times New Roman'
EAST = 'Yu Gothic'  # Times New Roman has no kana / kanji; only those characters fall back to this
BODY, TITLE = 26, 36  # half-points
LINE = 360  # 1.5
TEXT_W = 9071

doc = docx.Document(SRC)
body = doc.element.body
styles_el = doc.styles.element


def W(tag):
    return qn('w:' + tag)


def el(tag, **attrs):
    e = OxmlElement('w:' + tag)
    for k, v in attrs.items():
        e.set(W(k), str(v))
    return e


def fonts():
    return el('rFonts', ascii=TNR, hAnsi=TNR, cs=TNR, eastAsia=EAST)


def style_by_id(sid):
    for s in styles_el.findall(W('style')):
        if s.get(W('styleId')) == sid:
            return s
    return None


def rewrite_style(sid, ppr_xml, sz, bold=False, italic=False):
    """Replaces a paragraph style's pPr and rPr, keeping its name, base and links."""
    s = style_by_id(sid)
    for t in ('pPr', 'rPr'):
        old = s.find(W(t))
        if old is not None:
            s.remove(old)
    ppr = parse_xml(f'<w:pPr {nsdecls("w")}>{ppr_xml}</w:pPr>')
    rpr = el('rPr')
    rpr.append(fonts())
    rpr.append(el('b', val='1' if bold else '0'))
    rpr.append(el('bCs', val='1' if bold else '0'))
    rpr.append(el('i', val='1' if italic else '0'))
    rpr.append(el('iCs', val='1' if italic else '0'))
    rpr.append(el('color', val='000000'))
    rpr.append(el('sz', val=sz))
    rpr.append(el('szCs', val=sz))
    s.append(ppr)
    s.append(rpr)
    return s


def new_style(sid, name, based, ppr_xml, sz, bold=False, italic=False, ui=39):
    if style_by_id(sid) is None:
        s = parse_xml(
            f'<w:style {nsdecls("w")} w:type="paragraph" w:styleId="{sid}"><w:name w:val="{name}"/>'
            f'<w:basedOn w:val="{based}"/><w:next w:val="Normal"/><w:uiPriority w:val="{ui}"/><w:unhideWhenUsed/></w:style>')
        styles_el.append(s)
    return rewrite_style(sid, ppr_xml, sz, bold, italic)


# ─────────── Styles ───────────
dd = styles_el.find(W('docDefaults'))
rpr = dd.find(W('rPrDefault')).find(W('rPr'))
for child in list(rpr):
    rpr.remove(child)
rpr.append(fonts())
rpr.append(el('sz', val=BODY))
rpr.append(el('szCs', val=BODY))
rpr.append(el('lang', val='vi-VN', eastAsia='ja-JP', bidi='ar-SA'))
ppr = dd.find(W('pPrDefault')).find(W('pPr'))
for child in list(ppr):
    ppr.remove(child)
ppr.append(el('spacing', before=0, after=120, line=LINE, lineRule='auto'))

rewrite_style('Normal', f'<w:spacing w:before="0" w:after="120" w:line="{LINE}" w:lineRule="auto"/><w:jc w:val="both"/>', BODY)
rewrite_style('Heading1',
              f'<w:keepNext/><w:keepLines/><w:pageBreakBefore/><w:spacing w:before="0" w:after="240" w:line="{LINE}" w:lineRule="auto"/>'
              '<w:jc w:val="center"/><w:outlineLvl w:val="0"/>', TITLE, bold=True)
rewrite_style('Heading2',
              f'<w:keepNext/><w:keepLines/><w:spacing w:before="240" w:after="120" w:line="{LINE}" w:lineRule="auto"/>'
              '<w:jc w:val="left"/><w:outlineLvl w:val="1"/>', BODY, bold=True)
rewrite_style('Heading3',
              f'<w:keepNext/><w:keepLines/><w:spacing w:before="180" w:after="120" w:line="{LINE}" w:lineRule="auto"/>'
              '<w:jc w:val="left"/><w:outlineLvl w:val="2"/>', BODY, bold=True, italic=True)
rewrite_style('Caption', f'<w:keepLines/><w:spacing w:before="60" w:after="180" w:line="{LINE}" w:lineRule="auto"/><w:jc w:val="center"/>', BODY)
rewrite_style('Header', '<w:spacing w:before="0" w:after="0" w:line="240" w:lineRule="auto"/><w:jc w:val="right"/>', BODY, italic=True)
rewrite_style('Footer', '<w:spacing w:before="0" w:after="0" w:line="240" w:lineRule="auto"/><w:jc w:val="center"/>', BODY)
tab = f'<w:tabs><w:tab w:val="right" w:leader="dot" w:pos="{TEXT_W}"/></w:tabs>'
sp = f'<w:spacing w:before="0" w:after="60" w:line="{LINE}" w:lineRule="auto"/><w:jc w:val="left"/>'
# The right indent makes long entries wrap before the page number column.
sp_ = f'<w:spacing w:before="0" w:after="60" w:line="{LINE}" w:lineRule="auto"/>'
new_style('TOC1', 'toc 1', 'Normal', tab + sp_ + '<w:ind w:right="567"/><w:jc w:val="left"/>', BODY, bold=True)
new_style('TOC2', 'toc 2', 'Normal', tab + sp_ + '<w:ind w:left="284" w:right="567"/><w:jc w:val="left"/>', BODY)
new_style('TOC3', 'toc 3', 'Normal', tab + sp_ + '<w:ind w:left="567" w:right="567"/><w:jc w:val="left"/>', BODY, italic=True)
new_style('TOC4', 'toc 4', 'Normal', tab + sp_ + '<w:ind w:right="567"/><w:jc w:val="left"/>', BODY)
cap = f'<w:keepLines/><w:spacing w:before="60" w:after="180" w:line="{LINE}" w:lineRule="auto"/><w:jc w:val="center"/>'
new_style('ChuThichHinh', 'ChuThichHinh', 'Caption', cap, BODY)
new_style('ChuThichBang', 'ChuThichBang', 'Caption', cap, BODY)
# The word "MỤC LỤC" looks like a chapter title but stays out of its own table.
new_style('TOCHeading', 'TOC Heading', 'Heading1',
          f'<w:keepNext/><w:pageBreakBefore/><w:spacing w:before="0" w:after="240" w:line="{LINE}" w:lineRule="auto"/><w:jc w:val="center"/><w:outlineLvl w:val="9"/>',
          TITLE, bold=True)

# ─────────── A real bullet list ───────────
numbering = doc.part.numbering_part.element
ids = [int(a.get(W('abstractNumId'))) for a in numbering.findall(W('abstractNum'))]
abstract_id = max(ids) + 1
num_id = max(int(n.get(W('numId'))) for n in numbering.findall(W('num'))) + 1
abstract = parse_xml(
    f'<w:abstractNum {nsdecls("w")} w:abstractNumId="{abstract_id}"><w:multiLevelType w:val="hybridMultilevel"/>'
    '<w:lvl w:ilvl="0"><w:start w:val="1"/><w:numFmt w:val="bullet"/><w:lvlText w:val="&#8226;"/><w:lvlJc w:val="left"/>'
    '<w:pPr><w:ind w:left="567" w:hanging="283"/></w:pPr>'
    f'<w:rPr><w:rFonts w:ascii="{TNR}" w:hAnsi="{TNR}" w:cs="{TNR}" w:hint="default"/><w:color w:val="000000"/></w:rPr></w:lvl></w:abstractNum>')
first_num = numbering.find(W('num'))
first_num.addprevious(abstract)
numbering.append(parse_xml(f'<w:num {nsdecls("w")} w:numId="{num_id}"><w:abstractNumId w:val="{abstract_id}"/></w:num>'))
s = style_by_id('ListBullet')
old = s.find(W('pPr'))
if old is not None:
    s.remove(old)
s.append(parse_xml(f'<w:pPr {nsdecls("w")}><w:numPr><w:numId w:val="{num_id}"/></w:numPr><w:spacing w:before="0" w:after="80" w:line="{LINE}" w:lineRule="auto"/><w:jc w:val="both"/></w:pPr>'))
old = s.find(W('rPr'))
if old is not None:
    s.remove(old)

# ─────────── Paragraphs ───────────


def set_ppr_child(p, child):
    """Inserts in the order the schema expects for the few children used here."""
    ppr = p.get_or_add_pPr()
    order = ['pStyle', 'keepNext', 'keepLines', 'pageBreakBefore', 'widowControl', 'numPr', 'pBdr', 'shd', 'tabs', 'spacing', 'ind', 'jc', 'outlineLvl', 'rPr', 'sectPr']
    name = child.tag.split('}')[1]
    old = ppr.find(child.tag)
    if old is not None:
        ppr.remove(old)
    rank = order.index(name)
    for i, c in enumerate(list(ppr)):
        cname = c.tag.split('}')[1]
        if cname in order and order.index(cname) > rank:
            ppr.insert(i, child)
            return
    ppr.append(child)


def in_table(p):
    return next((True for a in p.iterancestors() if a.tag == W('tbl')), False)


def has_picture(p):
    return p.find('.//' + W('drawing')) is not None


def text_of(p):
    return ''.join(t.text or '' for t in p.iter(W('t')))


children = list(body)
first_section_end = next(i for i, k in enumerate(children) if k.tag == W('p') and k.find(W('pPr') + '/' + W('sectPr')) is not None)
cover = set(children[:first_section_end + 1])

stats = {'bullets': 0, 'pics': 0}
for p in list(body.iter(W('p'))):
    ppr = p.find(W('pPr'))
    pstyle = ppr.find(W('pStyle')).get(W('val')) if ppr is not None and ppr.find(W('pStyle')) is not None else None
    heading = pstyle in ('Heading1', 'Heading2', 'Heading3')
    # Runs: size and colour come from the styles.
    for rpr in p.iter(W('rPr')):
        big = rpr.find(W('sz')) is not None and int(rpr.find(W('sz')).get(W('val'))) >= 30
        for t in ('sz', 'szCs', 'color'):
            old = rpr.find(W(t))
            if old is not None:
                rpr.remove(old)
        if heading:
            for t in ('b', 'bCs', 'i', 'iCs'):
                old = rpr.find(W(t))
                if old is not None:
                    rpr.remove(old)
        if big and p in cover:
            rpr.append(el('sz', val=TITLE))
            rpr.append(el('szCs', val=TITLE))
    if ppr is None:
        ppr = p.get_or_add_pPr()
    for t in ('pBdr', 'spacing'):
        old = ppr.find(W(t))
        if old is not None:
            ppr.remove(old)

    picture = has_picture(p)
    table = in_table(p)
    # Hand-typed "• " becomes a real list item.
    runs = p.findall(W('r'))
    if runs and (runs[0].findtext(W('t')) or '').strip() == '•':
        p.remove(runs[0])
        for t in ('ind', 'jc'):
            old = ppr.find(W(t))
            if old is not None:
                ppr.remove(old)
        set_ppr_child(p, el('pStyle', val='ListBullet'))
        stats['bullets'] += 1
        continue
    if table:
        # Table text: same size and 1.5 lines, nothing between paragraphs but a little air in the cell.
        set_ppr_child(p, el('spacing', before=20, after=20, line=240 if picture else LINE, lineRule='auto'))
    elif picture:
        # A line holding a picture stays single: 1.5 would add half the picture's height above it.
        set_ppr_child(p, el('spacing', before=120, after=60, line=240, lineRule='auto'))
        set_ppr_child(p, el('keepNext'))
        stats['pics'] += 1
    elif pstyle is None and p not in cover:
        jc = ppr.find(W('jc'))
        if jc is not None and jc.get(W('val')) == 'left':
            ppr.remove(jc)
    if p in cover:
        set_ppr_child(p, el('spacing', before=60 if picture else 0, after=160 if picture else 80, line=240 if picture else LINE, lineRule='auto'))

# Picture tables stay with the caption below them; rows never split across pages.
for tbl in body.iter(W('tbl')):
    pics = tbl.find('.//' + W('drawing')) is not None
    if pics:
        for p in tbl.iter(W('p')):
            set_ppr_child(p, el('keepNext'))
    for tr in tbl.findall(W('tr')):
        trpr = tr.find(W('trPr'))
        if trpr is None:
            trpr = el('trPr')
            tr.insert(0, trpr)
        if trpr.find(W('cantSplit')) is None:
            trpr.insert(0, el('cantSplit'))
    # Plain borders, neutral fills.
    borders = tbl.find(W('tblPr')).find(W('tblBorders'))
    if borders is not None:
        for b in borders:
            if b.get(W('val')) not in (None, 'none', 'nil'):
                b.set(W('color'), '000000')
                b.set(W('sz'), '4')
    for shd in tbl.iter(W('shd')):
        fill = shd.get(W('fill'))
        if fill == '17305E':
            shd.set(W('fill'), 'D9D9D9')
        elif fill in ('F1F4F9', 'FFF7E6'):
            shd.set(W('fill'), 'F2F2F2')
for shd in body.iter(W('shd')):
    if shd.get(W('fill')) in ('F1F4F9', 'FFF7E6'):
        shd.set(W('fill'), 'F2F2F2')
    elif shd.get(W('fill')) == '17305E':
        shd.set(W('fill'), 'D9D9D9')

# Table header rows stay with their first data row; columns are widened so no word breaks in the middle.
from PIL import ImageFont
_reg = ImageFont.truetype('C:/Windows/Fonts/times.ttf', 130)
_bold = ImageFont.truetype('C:/Windows/Fonts/timesbd.ttf', 130)


def twips(word, bold):
    return (_bold if bold else _reg).getlength(word) / 10 * 20


widened = 0
for tbl in body.iter(W('tbl')):
    if tbl.find('.//' + W('drawing')) is not None:
        continue
    for tr in tbl.findall(W('tr')):
        trpr = tr.find(W('trPr'))
        if trpr is not None and trpr.find(W('tblHeader')) is not None:
            for p in tr.iter(W('p')):
                set_ppr_child(p, el('keepNext'))
    grid = tbl.find(W('tblGrid'))
    cols = [int(g.get(W('w'))) for g in grid.findall(W('gridCol'))]
    if len(cols) < 2:
        continue
    need = [0] * len(cols)
    for tr in tbl.findall(W('tr')):
        i = 0
        for tc in tr.findall(W('tc')):
            span = tc.find(W('tcPr') + '/' + W('gridSpan'))
            if span is None and i < len(cols):
                for r in tc.iter(W('r')):
                    bold = r.find(W('rPr') + '/' + W('b')) is not None and r.find(W('rPr') + '/' + W('b')).get(W('val')) not in ('0', 'false')
                    for t in r.findall(W('t')):
                        for word in (t.text or '').split():
                            need[i] = max(need[i], twips(word, bold) + 260)
            i += int(span.get(W('val'))) if span is not None else 1
    new = list(cols)
    for i in range(len(new)):
        short = need[i] - new[i]
        if short > 0:
            donor = max((j for j in range(len(new)) if j != i), key=lambda j: new[j] - need[j])
            give = min(short, new[donor] - need[donor])
            if give > 0:
                new[i] += give
                new[donor] -= give
    if new != cols:
        widened += 1
        new = [int(round(v)) for v in new]
        for g, v in zip(grid.findall(W('gridCol')), new):
            g.set(W('w'), str(v))
        for tr in tbl.findall(W('tr')):
            i = 0
            for tc in tr.findall(W('tc')):
                tcpr = tc.find(W('tcPr'))
                span = tcpr.find(W('gridSpan')) if tcpr is not None else None
                n = int(span.get(W('val'))) if span is not None else 1
                tcw = tcpr.find(W('tcW')) if tcpr is not None else None
                if tcw is not None:
                    tcw.set(W('w'), str(sum(new[i:i + n])))
                    tcw.set(W('type'), 'dxa')
                i += n
stats['widened'] = widened

# References: long paths justified into wide gaps, so they sit flush left.
for p in body.findall(W('p')):
    if re.match(r'^\[\d+\]', text_of(p)):
        set_ppr_child(p, el('jc', val='left'))

# Empty paragraphs: thin spacers after a table; none right before a new chapter (it spilled onto a blank page).
kids = list(body)
removed = 0
for i, k in enumerate(kids):
    if k.tag != W('p') or k in cover or text_of(k).strip() or k.find('.//' + W('drawing')) is not None or k.find('.//' + W('sectPr')) is not None:
        continue
    nxt = kids[i + 1] if i + 1 < len(kids) else None
    ps = nxt.find(W('pPr') + '/' + W('pStyle')) if nxt is not None and nxt.tag == W('p') else None
    if nxt is None or nxt.tag == W('sectPr') or (ps is not None and ps.get(W('val')) == 'Heading1'):
        body.remove(k)
        removed += 1
    else:
        set_ppr_child(k, el('spacing', before=0, after=0, line=240, lineRule='auto'))
stats['empties_removed'] = removed

# Page-break-only paragraphs: Heading 1 now starts a page by itself (a kept break would leave a blank page).
for p in list(body.findall(W('p'))):
    br = [b for b in p.iter(W('br')) if b.get(W('type')) == 'page']
    if br and not text_of(p).strip():
        body.remove(p)

# ─────────── Fields: table of contents, figures, tables ───────────


def field(instr, placeholder):
    p = el('p')
    def run(child):
        r = el('r')
        r.append(child)
        p.append(r)
    begin = el('fldChar', fldCharType='begin')
    begin.set(W('dirty'), 'true')
    run(begin)
    it = el('instrText')
    it.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
    it.text = f' {instr} '
    run(it)
    run(el('fldChar', fldCharType='separate'))
    t = el('t')
    t.text = placeholder
    run(t)
    run(el('fldChar', fldCharType='end'))
    return p


def replace_section(title, instr, placeholder):
    kids = list(body)
    start = next(i for i, k in enumerate(kids) if k.tag == W('p') and text_of(k).strip() == title)
    end = next(i for i in range(start + 1, len(kids)) if kids[i].tag == W('p') and kids[i].find(W('pPr') + '/' + W('pStyle')) is not None
               and kids[i].find(W('pPr') + '/' + W('pStyle')).get(W('val')) == 'Heading1')
    for k in kids[start + 1:end]:
        if k.find(W('pPr') + '/' + W('sectPr')) is not None:
            raise SystemExit('section break inside ' + title)
        body.remove(k)
    kids[start].addnext(field(instr, placeholder))
    return kids[start]


toc_heading = replace_section('MỤC LỤC', r'TOC \o "1-3" \h \z', 'Mục lục')
toc_heading.find(W('pPr')).find(W('pStyle')).set(W('val'), 'TOCHeading')
replace_section('DANH MỤC HÌNH', r'TOC \h \z \t "ChuThichHinh,4"', 'Danh mục hình')
replace_section('DANH MỤC BẢNG', r'TOC \h \z \t "ChuThichBang,4"', 'Danh mục bảng')

# Each kind of caption has its own style: the two lists collect by style, so editing a caption updates its list.
counts = {'Hinh': 0, 'Bang': 0}
for p in body.iter(W('p')):
    ps = p.find(W('pPr') + '/' + W('pStyle'))
    if ps is None or ps.get(W('val')) != 'Caption':
        continue
    label = text_of(p)
    kind = 'Hinh' if label.startswith('Hình') else 'Bang' if label.startswith('Bảng') else None
    if kind is None:
        continue
    counts[kind] += 1
    ps.set(W('val'), 'ChuThich' + kind)

# Cover and running header.
for t in body.iter(W('t')):
    if t.text and 'Bản 9 (cập nhật' in t.text:
        t.text = t.text.replace('Bản 9 (cập nhật', 'Bản 10 (cập nhật')
for sec in doc.sections:
    for part in (sec.header, sec.footer):
        for p in part.paragraphs:
            for rpr in p._p.iter(W('rPr')):
                for t in ('sz', 'szCs', 'color', 'i'):
                    old = rpr.find(W(t))
                    if old is not None:
                        rpr.remove(old)
            for t in p._p.iter(W('t')):
                if t.text:
                    t.text = t.text.replace('Capstone v9', 'Capstone v10')

doc.save(OUT)
print(stats, 'captions', counts)
