#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Bản Word của kế hoạch 12 tuần, kế hoạch năm và ngân hàng từ khoá.

Chạy từ gốc repo:
    python3 Marketing/Templates/docx_kit.py
"""

from __future__ import annotations

import re
import sys
from datetime import timedelta
from pathlib import Path

from docx import Document
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Mm, Pt, RGBColor

sys.path.insert(0, str(Path(__file__).resolve().parent))
from chi_tiet import WHEN  # noqa: E402
from posts import POSTS, WEEKS  # noqa: E402
from regenerate import DOW, START, calendar_rows  # noqa: E402
from tukhoa import HASHTAG, SLUG_THEME, THEMES  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
MKT = ROOT / "Marketing"

NAVY = "111721"
INK = "1F1405"
GOLD = "F7BD3D"
GOLD_DEEP = "A97812"
CREAM = "FBF6EC"
LINE = "E4D7B8"
WHITE = "FFFFFF"
MUTED = "3E4C63"
CHECK_FONT = "DejaVu Sans"

CONTENT_TWIPS = 9978  # A4, lề 17 mm hai bên
TAB_TWIPS = CONTENT_TWIPS - 220


def _rfonts(rPr, east="Noto Sans CJK JP"):
    rFonts = rPr.find(qn("w:rFonts"))
    if rFonts is None:
        rFonts = OxmlElement("w:rFonts")
        rPr.append(rFonts)
    rFonts.set(qn("w:ascii"), "Montserrat")
    rFonts.set(qn("w:hAnsi"), "Montserrat")
    rFonts.set(qn("w:cs"), "Montserrat")
    rFonts.set(qn("w:eastAsia"), east)
    lang = rPr.find(qn("w:lang"))
    if lang is None:
        lang = OxmlElement("w:lang")
        rPr.append(lang)
    lang.set(qn("w:val"), "vi-VN")
    lang.set(qn("w:eastAsia"), "ja-JP")


def style_run(run, size, color, bold=False, font=None):
    run.bold = bold
    run.italic = False
    run.font.size = Pt(size)
    run.font.color.rgb = RGBColor.from_string(color)
    run.font.name = font or "Montserrat"
    rPr = run._element.get_or_add_rPr()
    if font:
        rFonts = rPr.find(qn("w:rFonts"))
        if rFonts is None:
            rFonts = OxmlElement("w:rFonts")
            rPr.append(rFonts)
        for attr in ("w:ascii", "w:hAnsi", "w:cs", "w:eastAsia"):
            rFonts.set(qn(attr), font)
    else:
        _rfonts(rPr)
    # Không giãn dòng cố định: dấu tiếng Việt bị cắt nếu snap vào lưới.
    snap = rPr.find(qn("w:snapToGrid"))
    if snap is None:
        # snap lives on pPr, not rPr
        pass


def no_snap(paragraph):
    pPr = paragraph._p.get_or_add_pPr()
    snap = pPr.find(qn("w:snapToGrid"))
    if snap is None:
        snap = OxmlElement("w:snapToGrid")
        pPr.append(snap)
    snap.set(qn("w:val"), "0")


def shade_cell(cell, hex_color):
    tcPr = cell._tc.get_or_add_tcPr()
    for old in tcPr.findall(qn("w:shd")):
        tcPr.remove(old)
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), hex_color)
    tcPr.append(shd)


def cell_margins(cell, top=50, bottom=50, left=80, right=80):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = OxmlElement("w:tcMar")
    for name, val in (("top", top), ("left", left), ("bottom", bottom), ("right", right)):
        node = OxmlElement(f"w:{name}")
        node.set(qn("w:w"), str(val))
        node.set(qn("w:type"), "dxa")
        tcMar.append(node)
    tcPr.append(tcMar)


def cell_valign(cell, val="center"):
    tcPr = cell._tc.get_or_add_tcPr()
    v = OxmlElement("w:vAlign")
    v.set(qn("w:val"), val)
    tcPr.append(v)


def cell_borders(cell, color=LINE, sz="4"):
    tcPr = cell._tc.get_or_add_tcPr()
    borders = OxmlElement("w:tcBorders")
    for edge in ("top", "left", "bottom", "right"):
        el = OxmlElement(f"w:{edge}")
        el.set(qn("w:val"), "single")
        el.set(qn("w:sz"), sz)
        el.set(qn("w:space"), "0")
        el.set(qn("w:color"), color)
        borders.append(el)
    tcPr.append(borders)


def set_table_widths(table, widths):
    table.autofit = False
    table.allow_autofit = False
    tbl = table._tbl
    tblPr = tbl.tblPr
    tblW = tblPr.find(qn("w:tblW"))
    if tblW is None:
        tblW = OxmlElement("w:tblW")
        tblPr.append(tblW)
    total = sum(widths)
    tblW.set(qn("w:w"), str(total))
    tblW.set(qn("w:type"), "dxa")
    layout = tblPr.find(qn("w:tblLayout"))
    if layout is None:
        layout = OxmlElement("w:tblLayout")
        tblPr.append(layout)
    layout.set(qn("w:type"), "fixed")
    grid = tbl.find(qn("w:tblGrid"))
    if grid is not None:
        for child in list(grid):
            grid.remove(child)
    else:
        grid = OxmlElement("w:tblGrid")
        tblPr.addnext(grid)
    for w in widths:
        gc = OxmlElement("w:gridCol")
        gc.set(qn("w:w"), str(w))
        grid.append(gc)
    for row in table.rows:
        tr = row._tr
        trPr = tr.get_or_add_trPr()
        if trPr.find(qn("w:tblHeader")) is None and row is table.rows[0]:
            trPr.append(OxmlElement("w:tblHeader"))
        for cell, w in zip(row.cells, widths):
            tc = cell._tc
            tcPr = tc.get_or_add_tcPr()
            tcW = tcPr.find(qn("w:tcW"))
            if tcW is None:
                tcW = OxmlElement("w:tcW")
                tcPr.append(tcW)
            tcW.set(qn("w:w"), str(w))
            tcW.set(qn("w:type"), "dxa")


def paragraph_border(paragraph, color=GOLD, sz="12"):
    pPr = paragraph._p.get_or_add_pPr()
    pBdr = OxmlElement("w:pBdr")
    bottom = OxmlElement("w:bottom")
    bottom.set(qn("w:val"), "single")
    bottom.set(qn("w:sz"), sz)
    bottom.set(qn("w:space"), "6")
    bottom.set(qn("w:color"), color)
    pBdr.append(bottom)
    pPr.append(pBdr)


def keep_next(paragraph):
    pPr = paragraph._p.get_or_add_pPr()
    if pPr.find(qn("w:keepNext")) is None:
        pPr.append(OxmlElement("w:keepNext"))


def soften(text: str) -> str:
    text = text.replace("\u00a0", " ")
    return re.sub(r"(https?://\S+)", lambda m: m.group(1).replace("/", "/\u200b"), text)


TOKEN = re.compile(r"(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))")


def add_inline(paragraph, text, size=11, color=INK, bold=False):
    for part in TOKEN.split(soften(text)):
        if not part:
            continue
        if part.startswith("**") and part.endswith("**") and len(part) >= 4:
            run = paragraph.add_run(part[2:-2])
            style_run(run, size, color, True)
        elif part.startswith("`") and part.endswith("`") and len(part) >= 2:
            run = paragraph.add_run(part[1:-1])
            style_run(run, max(size - 0.5, 9), NAVY, False)
            rPr = run._element.get_or_add_rPr()
            shd = OxmlElement("w:shd")
            shd.set(qn("w:val"), "clear")
            shd.set(qn("w:fill"), CREAM)
            rPr.append(shd)
        elif part.startswith("["):
            match = re.match(r"\[([^\]]+)\]\(([^)]+)\)", part)
            if match:
                add_link(paragraph, match.group(1), match.group(2), size)
            else:
                run = paragraph.add_run(part)
                style_run(run, size, color, bold)
        else:
            run = paragraph.add_run(part)
            style_run(run, size, color, bold)


def add_link(paragraph, text, url, size):
    if not url.startswith("http"):
        run = paragraph.add_run(text)
        style_run(run, size, NAVY, True)
        return
    part = paragraph.part
    r_id = part.relate_to(
        url,
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink",
        is_external=True,
    )
    hyperlink = OxmlElement("w:hyperlink")
    hyperlink.set(qn("r:id"), r_id)
    run = OxmlElement("w:r")
    rPr = OxmlElement("w:rPr")
    _rfonts(rPr)
    b = OxmlElement("w:b")
    rPr.append(b)
    color = OxmlElement("w:color")
    color.set(qn("w:val"), GOLD_DEEP)
    rPr.append(color)
    sz = OxmlElement("w:sz")
    sz.set(qn("w:val"), str(int(size * 2)))
    rPr.append(sz)
    run.append(rPr)
    t = OxmlElement("w:t")
    t.set(qn("xml:space"), "preserve")
    t.text = text
    run.append(t)
    hyperlink.append(run)
    paragraph._p.append(hyperlink)


def is_real_image(path: Path) -> bool:
    if not path.is_file() or path.stat().st_size < 800:
        return False
    head = path.read_bytes()[:40]
    return not head.startswith(b"version https://git-lfs") and head[:2] != b"PK"


class Builder:
    def __init__(self, footer_label: str):
        self.doc = Document()
        self.footer_label = footer_label
        self.headings: list[tuple[int, str]] = []
        self._setup_styles()
        self._setup_defaults()

    def _setup_defaults(self):
        styles = self.doc.styles.element
        defaults = styles.find(qn("w:docDefaults"))
        if defaults is None:
            defaults = OxmlElement("w:docDefaults")
            styles.append(defaults)
        rpr_default = defaults.find(qn("w:rPrDefault"))
        if rpr_default is None:
            rpr_default = OxmlElement("w:rPrDefault")
            defaults.append(rpr_default)
        rPr = rpr_default.find(qn("w:rPr"))
        if rPr is None:
            rPr = OxmlElement("w:rPr")
            rpr_default.append(rPr)
        _rfonts(rPr)
        settings = self.doc.settings.element
        update = settings.find(qn("w:updateFields"))
        if update is None:
            update = OxmlElement("w:updateFields")
            settings.append(update)
        update.set(qn("w:val"), "true")

    def _setup_styles(self):
        normal = self.doc.styles["Normal"]
        normal.font.name = "Montserrat"
        normal.font.size = Pt(11)
        normal.font.color.rgb = RGBColor.from_string(INK)
        normal.font.bold = False
        normal.font.italic = False
        _rfonts(normal.element.get_or_add_rPr())
        pf = normal.paragraph_format
        pf.space_after = Pt(8)
        pf.space_before = Pt(0)
        pf.line_spacing = 1.12
        no_snap(normal.element) if False else None
        pPr = normal.element.get_or_add_pPr()
        snap = OxmlElement("w:snapToGrid")
        snap.set(qn("w:val"), "0")
        pPr.append(snap)

        specs = {
            "Heading 1": (22, NAVY, True, 0, 10),
            "Heading 2": (15, NAVY, True, 12, 4),
            "Heading 3": (12, GOLD_DEEP, True, 10, 3),
        }
        for name, (size, color, bold, before, after) in specs.items():
            style = self.doc.styles[name]
            style.font.name = "Montserrat"
            style.font.size = Pt(size)
            style.font.bold = bold
            style.font.italic = False
            style.font.color.rgb = RGBColor.from_string(color)
            _rfonts(style.element.get_or_add_rPr())
            style.paragraph_format.space_before = Pt(before)
            style.paragraph_format.space_after = Pt(after)
            style.paragraph_format.line_spacing = 1.08
            style.paragraph_format.page_break_before = name == "Heading 1"
            pPr = style.element.get_or_add_pPr()
            if pPr.find(qn("w:keepNext")) is None:
                pPr.append(OxmlElement("w:keepNext"))
            if pPr.find(qn("w:snapToGrid")) is None:
                snap = OxmlElement("w:snapToGrid")
                snap.set(qn("w:val"), "0")
                pPr.append(snap)

    def _section_page(self, section):
        section.page_width = Mm(210)
        section.page_height = Mm(297)
        section.header.is_linked_to_previous = False
        section.footer.is_linked_to_previous = False

    def cover(self, kicker, title_lines, subtitle, meta_left, meta_right, image: Path):
        # Document() đôi khi tạo một đoạn trống. Giữ nó thì bìa tràn sang trang 2.
        if self.doc.paragraphs:
            first = self.doc.paragraphs[0]
            if not first.text:
                first._element.getparent().remove(first._element)
        section = self.doc.sections[0]
        self._section_page(section)
        section.top_margin = Mm(11)
        section.bottom_margin = Mm(11)
        section.left_margin = Mm(12)
        section.right_margin = Mm(12)
        section.header_distance = Mm(0)
        section.footer_distance = Mm(0)
        section.different_first_page_header_footer = False

        table = self.doc.add_table(rows=1, cols=1)
        table.alignment = WD_TABLE_ALIGNMENT.CENTER
        cell = table.cell(0, 0)
        shade_cell(cell, "0B0F17")
        cell_margins(cell, 160, 160, 200, 200)
        cell_valign(cell, "center")
        cell_borders(cell, "0B0F17", "2")
        usable = 16838 - 2 * (int(Mm(11)) // 635)
        tr = table.rows[0]._tr
        trPr = tr.get_or_add_trPr()
        trHeight = OxmlElement("w:trHeight")
        trHeight.set(qn("w:val"), str(usable - 80))
        trHeight.set(qn("w:hRule"), "exact")
        trPr.append(trHeight)
        set_table_widths(table, [CONTENT_TWIPS + 400])

        inner = cell.paragraphs[0]
        inner.alignment = WD_ALIGN_PARAGRAPH.CENTER
        inner.paragraph_format.space_after = Pt(2)
        no_snap(inner)
        run = inner.add_run("NL")
        style_run(run, 14, "1F1405", True)
        # gold chip behind NL via shading on the run
        rPr = run._element.get_or_add_rPr()
        shd = OxmlElement("w:shd")
        shd.set(qn("w:val"), "clear")
        shd.set(qn("w:fill"), GOLD)
        rPr.append(shd)

        def center_line(text, size, color, bold, before=0, after=2):
            p = cell.add_paragraph()
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p.paragraph_format.space_before = Pt(before)
            p.paragraph_format.space_after = Pt(after)
            p.paragraph_format.line_spacing = 1.05
            no_snap(p)
            run = p.add_run(text)
            style_run(run, size, color, bold)
            return p

        center_line(kicker.upper(), 11, GOLD, True, before=10, after=2)
        center_line("NIHONGO LIFE", 28, WHITE, True, before=0, after=0)
        center_line("日本語ライフ", 13, GOLD, True, before=0, after=8)
        for i, line in enumerate(title_lines):
            center_line(line, 30, WHITE, True, before=0, after=0 if i < len(title_lines) - 1 else 6)
        sub = cell.add_paragraph()
        sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
        sub.paragraph_format.space_after = Pt(8)
        no_snap(sub)
        add_inline(sub, subtitle, 12, "F2F5FA")

        if is_real_image(image):
            pic_p = cell.add_paragraph()
            pic_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            pic_p.paragraph_format.space_before = Pt(4)
            pic_p.paragraph_format.space_after = Pt(6)
            no_snap(pic_p)
            run = pic_p.add_run()
            run.add_picture(str(image), width=Cm(14.6))

        center_line(meta_left, 11, GOLD, True, before=4, after=0)
        center_line(meta_right, 10, "C7D1E0", False, before=0, after=0)

        # Header/footer của phần bìa để trống.
        section.header.paragraphs[0].text = ""
        section.footer.paragraphs[0].text = ""

    def start_body(self):
        from docx.enum.section import WD_SECTION

        section = self.doc.add_section(WD_SECTION.NEW_PAGE)
        self._section_page(section)
        section.top_margin = Mm(18)
        section.bottom_margin = Mm(16)
        section.left_margin = Mm(17)
        section.right_margin = Mm(17)
        section.header_distance = Mm(7)
        section.footer_distance = Mm(7)
        pg = OxmlElement("w:pgNumType")
        pg.set(qn("w:start"), "1")
        section._sectPr.append(pg)

        header = section.header.paragraphs[0]
        header.alignment = WD_ALIGN_PARAGRAPH.LEFT
        header.paragraph_format.space_after = Pt(2)
        no_snap(header)
        paragraph_border(header, GOLD, "10")
        tab = OxmlElement("w:tabs")
        t = OxmlElement("w:tab")
        t.set(qn("w:val"), "right")
        t.set(qn("w:pos"), str(TAB_TWIPS))
        tab.append(t)
        header._p.get_or_add_pPr().append(tab)
        left = header.add_run("NIHONGO LIFE")
        style_run(left, 9, NAVY, True)
        header.add_run("\t")
        right = header.add_run(self.footer_label)
        style_run(right, 9, GOLD_DEEP, True)

        footer = section.footer.paragraphs[0]
        footer.paragraph_format.space_before = Pt(2)
        no_snap(footer)
        pPr = footer._p.get_or_add_pPr()
        pBdr = OxmlElement("w:pBdr")
        top = OxmlElement("w:top")
        top.set(qn("w:val"), "single")
        top.set(qn("w:sz"), "8")
        top.set(qn("w:space"), "6")
        top.set(qn("w:color"), GOLD)
        pBdr.append(top)
        pPr.append(pBdr)
        tabs = OxmlElement("w:tabs")
        tab = OxmlElement("w:tab")
        tab.set(qn("w:val"), "right")
        tab.set(qn("w:pos"), str(TAB_TWIPS))
        tabs.append(tab)
        pPr.append(tabs)
        run = footer.add_run("The Next Generation  ·  " + self.footer_label)
        style_run(run, 8, MUTED, False)
        footer.add_run("\t")
        self._page_field(footer)

    def _page_field(self, paragraph):
        run = paragraph.add_run("Trang ")
        style_run(run, 8, NAVY, True)
        for kind, text in (("begin", None), ("instr", " PAGE "), ("end", None)):
            r = paragraph.add_run()
            style_run(r, 8, NAVY, True)
            if kind == "instr":
                instr = OxmlElement("w:instrText")
                instr.set(qn("xml:space"), "preserve")
                instr.text = text
                r._r.append(instr)
            else:
                fld = OxmlElement("w:fldChar")
                fld.set(qn("w:fldCharType"), kind)
                r._r.append(fld)

    def toc(self, entries: list[tuple[int, str, int | None]]):
        title = self.doc.add_paragraph()
        title.paragraph_format.space_after = Pt(4)
        title.paragraph_format.page_break_before = False
        no_snap(title)
        paragraph_border(title, GOLD, "12")
        run = title.add_run("Mục lục")
        style_run(run, 22, NAVY, True)
        note = self.doc.add_paragraph()
        note.paragraph_format.space_after = Pt(8)
        no_snap(note)
        add_inline(
            note,
            "Mục lục bám theo đề mục. Mở bằng Word, chuột phải vào mục lục và chọn cập nhật trường nếu anh thêm đề mục.",
            10,
            MUTED,
        )
        # Trường TOC để Word tự cập nhật. Kết quả lưu sẵn bên dưới để LibreOffice in ra đủ dòng.
        holder = self.doc.add_paragraph()
        no_snap(holder)
        holder.paragraph_format.space_after = Pt(0)
        r = holder.add_run()
        fld = OxmlElement("w:fldChar")
        fld.set(qn("w:fldCharType"), "begin")
        fld.set(qn("w:dirty"), "true")
        r._r.append(fld)
        r2 = holder.add_run()
        instr = OxmlElement("w:instrText")
        instr.set(qn("xml:space"), "preserve")
        instr.text = r' TOC \o "1-2" \h \z \u '
        r2._r.append(instr)
        r3 = holder.add_run()
        sep = OxmlElement("w:fldChar")
        sep.set(qn("w:fldCharType"), "separate")
        r3._r.append(sep)

        for level, text, page in entries:
            p = self.doc.add_paragraph()
            no_snap(p)
            p.paragraph_format.space_before = Pt(1)
            p.paragraph_format.space_after = Pt(1)
            p.paragraph_format.line_spacing = 1.05
            p.paragraph_format.left_indent = Cm(0.45 if level == 2 else 0)
            pPr = p._p.get_or_add_pPr()
            tabs = OxmlElement("w:tabs")
            tab = OxmlElement("w:tab")
            tab.set(qn("w:val"), "right")
            tab.set(qn("w:leader"), "dot")
            tab.set(qn("w:pos"), str(TAB_TWIPS))
            tabs.append(tab)
            pPr.append(tabs)
            add_inline(p, text, 12 if level == 1 else 10.5, NAVY, level == 1)
            p.add_run("\t")
            num = p.add_run("" if page is None else str(page))
            style_run(num, 11 if level == 1 else 10, GOLD_DEEP, True)

        end_p = self.doc.add_paragraph()
        no_snap(end_p)
        end_p.paragraph_format.space_after = Pt(0)
        r4 = end_p.add_run()
        end = OxmlElement("w:fldChar")
        end.set(qn("w:fldCharType"), "end")
        r4._r.append(end)

    def h1(self, text):
        p = self.doc.add_paragraph(style="Heading 1")
        no_snap(p)
        paragraph_border(p, GOLD, "10")
        add_inline(p, text, 22, NAVY, True)
        self.headings.append((1, text))
        return p

    def h2(self, text, page_break=False):
        p = self.doc.add_paragraph(style="Heading 2")
        no_snap(p)
        if page_break:
            p.paragraph_format.page_break_before = True
        add_inline(p, text, 15, NAVY, True)
        self.headings.append((2, text))
        return p

    def h3(self, text):
        p = self.doc.add_paragraph(style="Heading 3")
        no_snap(p)
        add_inline(p, text, 12, GOLD_DEEP, True)
        return p

    def body(self, text, size=11, space_after=8):
        p = self.doc.add_paragraph()
        no_snap(p)
        p.paragraph_format.space_after = Pt(space_after)
        p.paragraph_format.line_spacing = 1.12
        add_inline(p, text, size, INK, False)
        return p

    def kicker(self, text):
        p = self.doc.add_paragraph()
        no_snap(p)
        p.paragraph_format.space_before = Pt(0)
        p.paragraph_format.space_after = Pt(2)
        run = p.add_run(text.upper())
        style_run(run, 10, GOLD_DEEP, True)
        return p

    def quote(self, text):
        p = self.doc.add_paragraph()
        no_snap(p)
        p.paragraph_format.left_indent = Cm(0.3)
        p.paragraph_format.space_before = Pt(4)
        p.paragraph_format.space_after = Pt(8)
        pPr = p._p.get_or_add_pPr()
        pBdr = OxmlElement("w:pBdr")
        left = OxmlElement("w:left")
        left.set(qn("w:val"), "single")
        left.set(qn("w:sz"), "16")
        left.set(qn("w:space"), "8")
        left.set(qn("w:color"), GOLD)
        pBdr.append(left)
        pPr.append(pBdr)
        shd = OxmlElement("w:shd")
        shd.set(qn("w:val"), "clear")
        shd.set(qn("w:fill"), CREAM)
        pPr.append(shd)
        add_inline(p, text, 12, NAVY, True)
        return p

    def bullets(self, items, numbered=False):
        for i, item in enumerate(items, 1):
            p = self.doc.add_paragraph()
            no_snap(p)
            p.paragraph_format.left_indent = Cm(0.55)
            p.paragraph_format.first_line_indent = Cm(-0.4)
            p.paragraph_format.space_after = Pt(3)
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.line_spacing = 1.08
            mark = f"{i}." if numbered else "•"
            run = p.add_run(mark + "  ")
            style_run(run, 11, GOLD_DEEP, True)
            add_inline(p, item, 11, INK)

    def image(self, path: Path, caption: str):
        if not is_real_image(path):
            self.body(f"Ảnh «{caption}» chưa có pixel trong bản này (`{path.name}`). Không ghép cảnh khác.")
            return
        from PIL import Image as PILImage

        with PILImage.open(path) as im:
            px_w, px_h = im.size
        disp_w = 16.2
        if px_w and disp_w * px_h / px_w > 10.8:
            disp_w = 10.8 * px_w / px_h
        p = self.doc.add_paragraph()
        no_snap(p)
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.space_before = Pt(4)
        p.paragraph_format.space_after = Pt(2)
        keep_next(p)
        run = p.add_run()
        run.add_picture(str(path), width=Cm(disp_w))
        cap = self.doc.add_paragraph()
        no_snap(cap)
        cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        cap.paragraph_format.space_after = Pt(8)
        add_inline(cap, caption, 9, MUTED)

    def table(self, headers, rows, compact=False):
        if not headers:
            return
        widths = _column_widths(headers, CONTENT_TWIPS)
        table = self.doc.add_table(rows=1 + len(rows), cols=len(headers))
        table.alignment = WD_TABLE_ALIGNMENT.CENTER
        table.style = "Table Grid"
        set_table_widths(table, widths)
        size = 9 if compact or len(headers) >= 5 else 10
        for c, header in enumerate(headers):
            cell = table.rows[0].cells[c]
            shade_cell(cell, NAVY)
            cell_borders(cell, "2A3344")
            cell_margins(cell, 40, 40, 70, 70)
            cell_valign(cell)
            p = cell.paragraphs[0]
            p.paragraph_format.space_after = Pt(0)
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.line_spacing = 1.05
            no_snap(p)
            # clear default run
            if p.runs:
                p.runs[0].text = ""
            add_inline(p, header, size, GOLD, True)
        for r, row in enumerate(rows):
            fill = CREAM if r % 2 else WHITE
            for c, value in enumerate(row):
                cell = table.rows[r + 1].cells[c]
                shade_cell(cell, fill)
                cell_borders(cell)
                cell_margins(cell, 36, 36, 70, 70)
                cell_valign(cell)
                p = cell.paragraphs[0]
                p.paragraph_format.space_after = Pt(0)
                p.paragraph_format.space_before = Pt(0)
                p.paragraph_format.line_spacing = 1.05
                no_snap(p)
                text = value.strip()
                if text in ("[ ]", "☐"):
                    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
                    run = p.add_run("☐")
                    style_run(run, 12, NAVY, False, font=CHECK_FONT)
                else:
                    add_inline(p, text, size, INK, False)
        spacer = self.doc.add_paragraph()
        spacer.paragraph_format.space_after = Pt(4)
        spacer.paragraph_format.space_before = Pt(2)
        no_snap(spacer)
        return table

    def save(self, path: Path):
        props = self.doc.core_properties
        props.author = "The Next Generation"
        props.last_modified_by = "The Next Generation"
        props.title = "NihongoLife — " + self.footer_label
        props.subject = self.footer_label
        props.category = "Marketing"
        props.language = "vi-VN"
        path.parent.mkdir(parents=True, exist_ok=True)
        self.doc.save(path)


def _column_widths(headers, total):
    weights = []
    for header in headers:
        h = header.lower()
        if header in {"#", "Xong"} or h in {"hạn"}:
            weights.append(0.7)
        elif h in {"giờ", "tuần", "quý"}:
            weights.append(0.85)
        elif h in {"ngày", "người", "kênh", "mùa"}:
            weights.append(1.15)
        elif "việc" in h or "hook" in h or "ghi" in h or "không" in h or "từ khoá" in h or "hashtag" in h:
            weights.append(2.3)
        else:
            weights.append(1.5)
    scale = total / sum(weights)
    widths = [max(700, int(w * scale)) for w in weights]
    drift = total - sum(widths)
    widths[-1] += drift
    return widths


def parse_md(text: str, base: Path):
    lines = text.splitlines()
    blocks = []
    i = 0
    while i < len(lines):
        line = lines[i].rstrip()
        stripped = line.strip()
        if not stripped:
            i += 1
            continue
        if stripped.startswith("<p class=\"kicker\">"):
            blocks.append(("kicker", re.sub(r"<[^>]+>", "", stripped)))
            i += 1
            continue
        if stripped.startswith("<div class=\"pagebreak\""):
            blocks.append(("break", None))
            i += 1
            continue
        if stripped.startswith("#### "):
            blocks.append(("h", (3, stripped[5:].strip())))
            i += 1
            continue
        if stripped.startswith("### "):
            blocks.append(("h", (3, stripped[4:].strip())))
            i += 1
            continue
        if stripped.startswith("## "):
            blocks.append(("h", (2, stripped[3:].strip())))
            i += 1
            continue
        if stripped.startswith("# "):
            blocks.append(("h", (1, stripped[2:].strip())))
            i += 1
            continue
        if stripped.startswith("!["):
            match = re.match(r"!\[([^\]]*)\]\(([^)]+)\)", stripped)
            if match:
                blocks.append(("image", (match.group(1), (base / match.group(2)).resolve())))
            i += 1
            continue
        if stripped.startswith("|"):
            rows = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                raw = [c.strip() for c in lines[i].strip().strip("|").split("|")]
                rows.append(raw)
                i += 1
            data = [r for r in rows if not re.fullmatch(r"[:\-\s|]+", "|".join(r))]
            # separator row is --- 
            data = [r for r in rows if not all(re.fullmatch(r":?-{2,}:?", c.replace(" ", "")) for c in r)]
            if data:
                blocks.append(("table", (data[0], data[1:])))
            continue
        if stripped.startswith("> "):
            quote = []
            while i < len(lines) and lines[i].strip().startswith(">"):
                quote.append(lines[i].strip().lstrip(">").strip())
                i += 1
            blocks.append(("quote", " ".join(quote)))
            continue
        if re.match(r"\d+\.\s", stripped) or stripped.startswith("- "):
            numbered = bool(re.match(r"\d+\.\s", stripped))
            items = []
            while i < len(lines):
                s = lines[i].strip()
                if numbered and re.match(r"\d+\.\s", s):
                    items.append(re.sub(r"^\d+\.\s+", "", s))
                    i += 1
                elif not numbered and s.startswith("- "):
                    items.append(s[2:].strip())
                    i += 1
                else:
                    break
            blocks.append(("ol" if numbered else "ul", items))
            continue
        para = [stripped]
        i += 1
        while i < len(lines) and lines[i].strip() and not _block_start(lines[i].strip()):
            para.append(lines[i].strip())
            i += 1
        blocks.append(("p", " ".join(para)))
    return blocks


def _block_start(stripped: str) -> bool:
    return stripped.startswith(("#", "|", ">", "- ", "![", "<")) or bool(re.match(r"\d+\.\s", stripped))


def _visual_level(level: int, role: str) -> int:
    # Kế hoạch dùng ## làm chương (sang trang). README sự kiện dùng # làm chương, ## nằm cùng trang.
    if role == "plan":
        return 1 if level <= 2 else 2
    if role == "readme":
        return 1 if level == 1 else 3
    if role == "nested":
        return 2 if level == 1 else 3
    return min(level, 3)


def render_blocks(b: Builder, blocks, role="plan", skip_leading_h1=False):
    skipped = False
    for kind, payload in blocks:
        if kind == "break":
            continue
        if kind == "h":
            level, text = payload
            if skip_leading_h1 and level == 1 and not skipped:
                skipped = True
                continue
            visual = _visual_level(level, role)
            if visual <= 1:
                b.h1(text)
            elif visual == 2:
                b.h2(text)
            else:
                b.h3(text)
            continue
        elif kind == "kicker":
            b.kicker(payload)
        elif kind == "p":
            b.body(payload)
        elif kind == "quote":
            b.quote(payload)
        elif kind == "ul":
            b.bullets(payload, numbered=False)
        elif kind == "ol":
            b.bullets(payload, numbered=True)
        elif kind == "image":
            alt, path = payload
            b.image(path, alt)
        elif kind == "table":
            headers, rows = payload
            width = max(len(headers), max((len(r) for r in rows), default=0))
            headers = headers + [""] * (width - len(headers))
            norm = [(r + [""] * (width - len(r)))[:width] for r in rows]
            b.table(headers, norm, compact=width >= 5)


def theme_by_id(tid):
    for theme in THEMES:
        if theme["id"] == tid:
            return theme
    raise KeyError(tid)


def add_post_summaries(b: Builder):
    b.h1("Tóm tắt 36 bài Facebook")
    b.body(
        "Mỗi dòng là một bài trong thư mục tuần. Giờ là giờ Việt Nam. Từ khoá lấy từ chủ đề của bài trong ngân hàng `TuKhoa`. Caption đầy đủ, ba câu mở và cách trả lời nằm ở `chi-tiet.md`."
    )
    by_week = {}
    for post in POSTS:
        by_week.setdefault(post["week"], []).append(post)
    themes = {w["n"]: w for w in WEEKS}
    for n in range(1, 13):
        week = themes[n]
        start = START + timedelta(days=(n - 1) * 7)
        end = start + timedelta(days=6)
        b.h2(f"Bài tuần {n} · {start.strftime('%d/%m')}–{end.strftime('%d/%m')} · {week['theme']}")
        rows = []
        for post in by_week[n]:
            day = START + timedelta(days=(n - 1) * 7 + post["dow"])
            theme = theme_by_id(SLUG_THEME[post["slug"]])
            keys = ", ".join(theme["core"][:2])
            rows.append([
                f"{DOW[day.weekday()]} {day.strftime('%d/%m/%Y')}",
                WHEN[post["dow"]][0],
                post["slug"],
                post["hook"],
                f"{theme['ten']}: {keys}",
            ])
        b.table(["Ngày", "Giờ", "Bài", "Hook", "Từ khoá"], rows, compact=True)


def add_calendar(b: Builder):
    b.h1("Lịch đăng cả đợt")
    b.body(
        "Đủ kênh trong file Excel, từ thứ Hai 26/10/2026 đến chủ nhật 17/01/2027. Trạng thái là việc phải làm, không phải số đã đạt. Bài Facebook đăng khi đã thay mã link."
    )
    current = None
    bucket = []

    def flush(week_no):
        if not bucket:
            return
        week = next(w for w in WEEKS if w["n"] == week_no)
        b.h2(f"Lịch tuần {week_no} · {week['theme']}")
        b.table(["Ngày", "Giờ", "Kênh", "Việc", "Người", "Trạng thái"], bucket, compact=True)
        bucket.clear()

    for row in calendar_rows():
        if current is None:
            current = row["week"]
        if row["week"] != current:
            flush(current)
            current = row["week"]
        bucket.append([
            row["date"].strftime("%d/%m/%Y"),
            row["time"],
            row["channel"],
            row["hook"],
            row["owner"],
            row["status"],
        ])
    if current is not None:
        flush(current)


def add_keyword_summary(b: Builder):
    b.h1("Từ khoá dùng trong 12 tuần")
    b.body(
        "Bảng này là lối tắt. Bản đủ từng kênh nằm ở `Marketing/TuKhoa/Ngan_Hang_Tu_Khoa.docx`. Hashtag có chữ JLPT hoặc IELTS chỉ đăng kèm câu: luyện trong game, không phải kết quả chính thức."
    )
    rows = []
    for theme in THEMES:
        rows.append([
            theme["ten"],
            ", ".join(theme["core"][:3]),
            theme["tt"],
            HASHTAG[theme["id"]],
        ])
    b.table(["Chủ đề", "Cốt lõi", "Câu TikTok", "Hashtag"], rows, compact=True)


def add_persona_table(blocks):
    out = []
    i = 0
    while i < len(blocks):
        out.append(blocks[i])
        kind, payload = blocks[i]
        if kind == "h" and payload[0] == 3 and str(payload[1]).startswith("Khoa,"):
            # paragraph theo sau
            if i + 1 < len(blocks) and blocks[i + 1][0] == "p":
                i += 1
                out.append(blocks[i])
            out.append(("h", (3, "Bảng chân dung")))
            out.append(("p", "Ba người này là giả định để chọn bài, không phải học viên thật và không phải lời chứng thực."))
            out.append((
                "table",
                (
                    ["Chân dung", "Việc họ cần", "Kênh", "Bài hợp"],
                    [
                        ["Minh, 17, giả định", "N5, ngại nói sai trước lớp", "TikTok, nhóm trường, Discord", "Một câu hội thoại. Chọn sai vẫn được chữa."],
                        ["Lan, 21, giả định", "IELTS và tiếng Nhật. Sợ nhầm điểm game với band", "Facebook, YouTube, nhóm luyện đề", "Luyện trong lớp Hibari. Voice chỉ hẹn trên máy tính."],
                        ["Khoa, 24, giả định", "Sắp sang Nhật, gọi món, mua vé, hỏi đường", "Nhóm đi Nhật, Zalo, video dài", "Cơm nắm, vé 320 yên tới Minato, một đĩa sushi."],
                    ],
                ),
            ))
        i += 1
    return out


def build_twelve(toc):
    b = Builder("Kế hoạch 12 tuần")
    b.cover(
        "Dành cho anh Huy",
        ["Kế hoạch", "12 tuần"],
        "Học tiếng Nhật bằng cách sống ở Nhật.",
        "26.10.2026  —  17.01.2027",
        "Cộng đồng mở  ·  VTC Academy",
        ROOT / "docs/readme/horizon_dusk.jpg",
    )
    b.start_body()
    b.toc(toc)
    src = MKT / "KeHoach" / "KeHoach_Marketing_12_Tuan.md"
    blocks = add_persona_table(parse_md(src.read_text(encoding="utf-8"), src.parent))
    # Phần mở không có đề mục trong Markdown. Thêm một đề mục để mục lục trỏ tới.
    b.h1("Mở đầu")
    render_blocks(b, blocks)
    add_post_summaries(b)
    add_calendar(b)
    add_keyword_summary(b)
    return b


def build_year(toc):
    b = Builder("Kế hoạch một năm")
    b.cover(
        "Sau 12 tuần  ·  sẽ sửa thường xuyên",
        ["Kế hoạch", "một năm"],
        "Từ 18/01/2027 đến 16/01/2028. Sửa sau mỗi tháng và sau mỗi sự kiện.",
        "18.01.2027  —  16.01.2028",
        "Cộng đồng mở  ·  VTC Academy",
        ROOT / "docs/readme/horizon_day.jpg",
    )
    b.start_body()
    b.toc(toc)
    src = MKT / "KeHoachNam" / "KeHoach_Nam_2026-2027.md"
    blocks = parse_md(src.read_text(encoding="utf-8"), src.parent)
    b.h1("Mở đầu")
    render_blocks(b, blocks)
    b.h1("Checklist theo mùa")
    b.body("Ô vuông là việc chưa làm. Anh đánh dấu trên bản in hoặc trong Word. Hạn trễ thì ghi ngày thật, không xoá dòng.")
    for folder in ("Mua-Xuan", "Mua-He", "Mua-Thu", "Mua-Dong"):
        readme = MKT / "KeHoachNam" / folder / "README.md"
        check = MKT / "KeHoachNam" / folder / "checklist.md"
        render_blocks(b, parse_md(readme.read_text(encoding="utf-8"), readme.parent), role="nested")
        b.h3("Việc cần làm")
        render_blocks(b, _without_title(parse_md(check.read_text(encoding="utf-8"), check.parent)), role="readme")
    b.h1("Checklist theo sự kiện")
    b.body(
        "Ngày JLPT là cửa sổ đội tự chuẩn bị, không phải lịch thi chính thức, không có logo kỳ thi. Mỗi sự kiện một trang việc."
    )
    order = [
        "Tet-Nguyen-Dan", "Mua-Hoa-Anh-Dao", "JLPT-Thang-7", "Tanabata", "Obon",
        "Khai-Giang", "Ngay-Viet-Nam-Nhat-Ban", "Halloween", "JLPT-Thang-12",
        "Giang-Sinh", "Moc-Cap-Nhat-Game",
    ]
    for folder in order:
        base = MKT / "KeHoachNam" / "SuKien" / folder
        readme_blocks = parse_md((base / "README.md").read_text(encoding="utf-8"), base)
        # Đưa tên sự kiện lên Heading 1 để mỗi sự kiện sang trang mới.
        if readme_blocks and readme_blocks[0][0] == "h" and readme_blocks[0][1][0] == 1:
            b.h2(readme_blocks[0][1][1])
            render_blocks(b, readme_blocks[1:], role="nested")
        else:
            render_blocks(b, readme_blocks, role="nested")
        b.h3("Việc cần làm")
        check_blocks = parse_md((base / "checklist.md").read_text(encoding="utf-8"), base)
        render_blocks(b, _without_title(check_blocks), role="readme")
    return b


def build_keywords(toc):
    b = Builder("Ngân hàng từ khoá")
    b.cover(
        "Viết bài mới thì lấy ở đây",
        ["Ngân hàng", "từ khoá"],
        "Cốt lõi, đuôi dài, Facebook, TikTok, YouTube và câu người học gõ trên Google.",
        "Dùng kèm kế hoạch 12 tuần và kế hoạch năm",
        "Không phải số liệu  ·  không logo kỳ thi",
        ROOT / "docs/readme/dialogue.jpg",
    )
    b.start_body()
    b.toc(toc)
    b.h1("Cách dùng")
    b.body(
        "Anh Huy lấy từ đây khi viết bài mới. Từ khoá không phải số liệu. Hashtag có chữ JLPT hoặc IELTS chỉ đăng kèm câu: luyện trong game, không phải kết quả chính thức, không có logo kỳ thi."
    )
    b.body("Kênh: Facebook là nơi đăng chính. TikTok là một câu móc. YouTube chỉ khi đã có video xem hết. Google là cách người học gõ tìm, không phải quảng cáo đang chạy.")
    b.bullets([
        "Chọn một chủ đề. Đừng nhồi bảy chủ đề vào một caption.",
        "Một từ cốt lõi trong câu đầu. Một đuôi dài trong thân bài.",
        "Hashtag để cuối. Giữ bộ của chủ đề, thêm tối đa ba tag của riêng bài.",
        "UTM: `utm_source` là kênh, `utm_campaign` là `tuan-XX` hoặc mã sự kiện năm.",
    ], numbered=True)
    for theme in THEMES:
        b.h1(theme["ten"])
        b.h3("Cốt lõi")
        b.body(", ".join(theme["core"]))
        b.h3("Đuôi dài")
        b.bullets(theme["longtail"])
        b.h3("English")
        b.body(", ".join(theme["en"]))
        b.h3("日本語")
        b.body(", ".join(theme["ja"]))
        b.h3("Google")
        b.bullets(theme["google"])
        b.table(
            ["Kênh", "Câu để dùng"],
            [
                ["Facebook", theme["fb"]],
                ["TikTok, hai giây đầu", theme["tt"]],
                ["YouTube, tiêu đề", theme["yt_title"]],
                ["YouTube, mô tả", theme["yt_desc"]],
                ["Hashtag Việt / Anh / Nhật", HASHTAG[theme["id"]]],
            ],
        )
    return b


def _without_title(blocks):
    return [block for block in blocks if not (block[0] == "h" and block[1][0] == 1)]


def assign_pages(entries, pdf_path: Path):
    import subprocess

    text = subprocess.check_output(["pdftotext", "-layout", str(pdf_path), "-"], text=True, errors="replace")
    pages = text.split("\f")
    lines = []
    for index, page in enumerate(pages, 1):
        for line in page.splitlines():
            s = " ".join(line.split())
            if s:
                lines.append((index, s))
    pos = 0
    for idx, (_, s) in enumerate(lines):
        if s.startswith("Mở đầu") and s.count(".") < 6:
            pos = idx
            break
    out = []
    for level, title in entries:
        found = None
        for idx in range(pos, len(lines)):
            physical, s = lines[idx]
            if s.count(".") >= 8:
                continue
            if s == title or s.startswith(title):
                found = physical
                pos = idx + 1
                break
        printed = None if found is None else max(1, found - 1)
        out.append((level, title, printed))
    return out


def main():
    jobs = [
        (build_twelve, MKT / "KeHoach" / "KeHoach_Marketing_12_Tuan.docx"),
        (build_year, MKT / "KeHoachNam" / "KeHoach_Nam_2026-2027.docx"),
        (build_keywords, MKT / "TuKhoa" / "Ngan_Hang_Tu_Khoa.docx"),
    ]
    for factory, path in jobs:
        print("building", path.name, flush=True)
        pdf = Path("/tmp") / (path.stem + ".probe.pdf")
        toc: list[tuple[int, str, int | None]] = []
        final = None
        for attempt in range(3):
            final = factory(toc)
            final.save(path)
            export_pdf(path, pdf)
            measured = assign_pages(final.headings, pdf)
            known = sum(1 for _, _, page in measured if page is not None)
            print(f"  pass {attempt + 1}", path.name, "pages-known", known, "/", len(measured), flush=True)
            if measured == toc:
                break
            toc = measured
        missing = [t for _, t, p in toc if p is None]
        if missing:
            print("  missing", missing[:10], flush=True)
        pdf.unlink(missing_ok=True)
        print("wrote", path, "headings", len(final.headings), flush=True)


def export_pdf(docx: Path, pdf: Path):
    import subprocess
    import tempfile

    with tempfile.TemporaryDirectory(prefix="nlword-") as tmp:
        subprocess.check_call(
            [
                "soffice", "--headless", "--norestore", "--convert-to", "pdf",
                "--outdir", tmp, str(docx),
            ],
            stdout=subprocess.DEVNULL,
        )
        produced = Path(tmp) / (docx.stem + ".pdf")
        pdf.parent.mkdir(parents=True, exist_ok=True)
        produced.replace(pdf)


if __name__ == "__main__":
    main()
