"""NihongoLife — Capstone report v4 (10/2026). python build_report_v4.py → ../NihongoLife_Bao_Cao_Capstone_v4.docx
Content reflects only features implemented and verified in the repository (tests of 10/10/2026)."""
from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

P = Path(__file__).parent
OUT = P.parent / "NihongoLife_Bao_Cao_Capstone_v4.docx"

NAVY, GOLD, RED, MUTED, TEXT, SOFT, LINE, TEAL = "17305E", "B8862B", "B23A31", "5B6B82", "1E293B", "F2F5FA", "C9D3E2", "2E7D6B"
BODY, JP = "Times New Roman", "Yu Gothic"
CONTENT_CM = 16.0          # A4 21 cm − 3 cm − 2 cm
CONTENT_W = int(CONTENT_CM / 2.54 * 1440)

doc = Document()


# ═════════════════════════ styles ═════════════════════════

def set_font(obj, name=BODY, size=None, color=None, bold=None, italic=None, east=JP):
    f = obj.font
    f.name = name
    if size: f.size = Pt(size)
    if color: f.color.rgb = RGBColor.from_string(color)
    if bold is not None: f.bold = bold
    if italic is not None: f.italic = italic
    el = obj.element if hasattr(obj, "element") else obj._element
    rpr = el.get_or_add_rPr()
    fonts = rpr.find(qn("w:rFonts"))
    if fonts is None:
        fonts = OxmlElement("w:rFonts"); rpr.append(fonts)
    for k in ("w:ascii", "w:hAnsi", "w:cs"): fonts.set(qn(k), name)
    fonts.set(qn("w:eastAsia"), east)
    for k in ("w:asciiTheme", "w:hAnsiTheme", "w:eastAsiaTheme", "w:cstheme"):
        if fonts.get(qn(k)) is not None: del fonts.attrib[qn(k)]


def style(name, size, color=TEXT, before=0, after=6, line=1.3, bold=None, italic=None, align=None, base=None, kind=1):
    st = doc.styles[name] if name in [s.name for s in doc.styles] else doc.styles.add_style(name, kind)
    if base: st.base_style = doc.styles[base]
    set_font(st, BODY, size, color, bold, italic)
    pf = st.paragraph_format
    pf.space_before, pf.space_after, pf.line_spacing = Pt(before), Pt(after), line
    if align is not None: pf.alignment = align
    return st


style("Normal", 13, TEXT, 0, 6, 1.3, align=WD_ALIGN_PARAGRAPH.JUSTIFY)
doc.styles["Normal"].paragraph_format.first_line_indent = Cm(1.0)
for name, size, col, before, after in [("Heading 1", 16, NAVY, 0, 12), ("Heading 2", 14, NAVY, 14, 6), ("Heading 3", 13, TEAL, 10, 4)]:
    st = style(name, size, col, before, after, 1.2, bold=True, italic=(name == "Heading 3"), align=WD_ALIGN_PARAGRAPH.LEFT)
    st.paragraph_format.keep_with_next = True
    st.paragraph_format.first_line_indent = Cm(0)
style("Caption", 11, MUTED, 4, 12, 1.15, italic=True, align=WD_ALIGN_PARAGRAPH.CENTER).paragraph_format.first_line_indent = Cm(0)
style("TableCaption", 11, NAVY, 10, 4, 1.15, bold=True, align=WD_ALIGN_PARAGRAPH.CENTER, base="Normal").paragraph_format.first_line_indent = Cm(0)
style("Cell", 11, TEXT, 1, 1, 1.15, base="Normal", align=WD_ALIGN_PARAGRAPH.LEFT).paragraph_format.first_line_indent = Cm(0)
style("CellHead", 11, "FFFFFF", 1, 1, 1.15, bold=True, base="Normal", align=WD_ALIGN_PARAGRAPH.LEFT).paragraph_format.first_line_indent = Cm(0)
style("Bullet", 13, TEXT, 0, 3, 1.3, base="Normal", align=WD_ALIGN_PARAGRAPH.JUSTIFY)
doc.styles["Bullet"].paragraph_format.first_line_indent = Cm(-0.5)
doc.styles["Bullet"].paragraph_format.left_indent = Cm(1.0)
style("Note", 12, NAVY, 0, 0, 1.25, base="Normal", align=WD_ALIGN_PARAGRAPH.JUSTIFY).paragraph_format.first_line_indent = Cm(0)
style("Front", 13, TEXT, 0, 6, 1.3, base="Normal", align=WD_ALIGN_PARAGRAPH.CENTER).paragraph_format.first_line_indent = Cm(0)
for n, ind in (("TOC 1", 0), ("TOC 2", 0.6), ("TOC 3", 1.2)):
    st = style(n, 13 if n == "TOC 1" else 12.5, NAVY if n == "TOC 1" else TEXT, 3 if n == "TOC 1" else 0, 1, 1.15, bold=(n == "TOC 1"), base="Normal", align=WD_ALIGN_PARAGRAPH.LEFT)
    st.paragraph_format.left_indent = Cm(ind); st.paragraph_format.first_line_indent = Cm(0)
style("Table of Figures", 12.5, TEXT, 0, 2, 1.15, base="Normal", align=WD_ALIGN_PARAGRAPH.LEFT).paragraph_format.first_line_indent = Cm(0)


# ═════════════════════════ helpers ═════════════════════════

def shade(cell, fill):
    tcpr = cell._tc.get_or_add_tcPr()
    e = OxmlElement("w:shd"); e.set(qn("w:val"), "clear"); e.set(qn("w:color"), "auto"); e.set(qn("w:fill"), fill); tcpr.append(e)


def cell_margins(tbl, top=70, bottom=70, left=120, right=120):
    m = OxmlElement("w:tblCellMar")
    for k, v in (("top", top), ("left", left), ("bottom", bottom), ("right", right)):
        e = OxmlElement(f"w:{k}"); e.set(qn("w:w"), str(v)); e.set(qn("w:type"), "dxa"); m.append(e)
    tbl._tbl.tblPr.append(m)


def borders(tbl, color=LINE, inside=True, size=4):
    b = OxmlElement("w:tblBorders")
    for k in ["top", "left", "bottom", "right", "insideH", "insideV"]:
        e = OxmlElement(f"w:{k}")
        if k in ("top", "left", "bottom", "right") or inside:
            e.set(qn("w:val"), "single"); e.set(qn("w:sz"), str(size)); e.set(qn("w:color"), color)
        else:
            e.set(qn("w:val"), "nil")
        b.append(e)
    tbl._tbl.tblPr.append(b)


def fixed_table(rows, cols, widths):
    t = doc.add_table(rows=rows, cols=cols)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    pr = t._tbl.tblPr
    w = pr.find(qn("w:tblW")); w.set(qn("w:type"), "dxa"); w.set(qn("w:w"), str(sum(widths)))
    lay = OxmlElement("w:tblLayout"); lay.set(qn("w:type"), "fixed"); pr.append(lay)
    grid = t._tbl.tblGrid
    for g in list(grid): grid.remove(g)
    for wd in widths:
        g = OxmlElement("w:gridCol"); g.set(qn("w:w"), str(wd)); grid.append(g)
    for r in t.rows:
        for j, c in enumerate(r.cells):
            c.width = Cm(widths[j] / 1440 * 2.54)
    return t


def scale(widths):
    s = sum(widths)
    out = [round(w * CONTENT_W / s) for w in widths]
    out[-1] = CONTENT_W - sum(out[:-1])
    return out


fig_no = {}
tab_no = {}
chapter = [0]


def para(text, st=None, align=None, bold=False, italic=False, color=None, size=None, indent=True):
    p = doc.add_paragraph(style=st)
    if not indent: p.paragraph_format.first_line_indent = Cm(0)
    if align is not None: p.alignment = align
    parts = text.split("**")
    for i, chunk in enumerate(parts):
        if not chunk: continue
        r = p.add_run(chunk)
        if i % 2 == 1 or bold: r.bold = True
        if italic: r.italic = True
        if color: r.font.color.rgb = RGBColor.from_string(color)
        if size: r.font.size = Pt(size)
    return p


def bullets(items):
    for it in items:
        para("– " + it, "Bullet")


def table(headers, rows, widths=None, caption=None, first_bold=True, font_size=None):
    if caption:
        tab_no[chapter[0]] = tab_no.get(chapter[0], 0) + 1
        cp = para(f"Bảng {chapter[0]}.{tab_no[chapter[0]]}. {caption}", "TableCaption")
        cp.paragraph_format.keep_with_next = True
    widths = scale(widths or [1] * len(headers))
    t = fixed_table(len(rows) + 1, len(headers), widths)
    borders(t, LINE, True); cell_margins(t)
    for i, vals in enumerate([headers] + rows):
        trpr = t.rows[i]._tr.get_or_add_trPr()
        trpr.append(OxmlElement("w:cantSplit"))
        if i == 0: trpr.append(OxmlElement("w:tblHeader"))
        for j, v in enumerate(vals):
            c = t.rows[i].cells[j]
            c.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            p = c.paragraphs[0]; p.style = doc.styles["CellHead" if i == 0 else "Cell"]
            r = p.add_run(str(v))
            if font_size: r.font.size = Pt(font_size)
            if i == 0: shade(c, NAVY)
            else:
                if i % 2 == 0: shade(c, "F6F8FB")
                if j == 0 and first_bold: r.bold = True; r.font.color.rgb = RGBColor.from_string(NAVY)
    sp = doc.add_paragraph(); sp.paragraph_format.space_after = Pt(4); sp.paragraph_format.first_line_indent = Cm(0)
    return t


def figure(path, caption, width_cm=15.5):
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.first_line_indent = Cm(0)
    p.paragraph_format.space_before = Pt(6); p.paragraph_format.space_after = Pt(0); p.paragraph_format.keep_with_next = True
    p.add_run().add_picture(str(P / path), width=Cm(width_cm))
    fig_no[chapter[0]] = fig_no.get(chapter[0], 0) + 1
    para(f"Hình {chapter[0]}.{fig_no[chapter[0]]}. {caption}", "Caption")


def fig_pair(a, b, width_cm=7.8):
    t = fixed_table(1, 2, scale([1, 1])); borders(t, "FFFFFF", False); cell_margins(t, 30, 30, 40, 40)
    t.rows[0]._tr.get_or_add_trPr().append(OxmlElement("w:cantSplit"))
    for k, (path, cap) in enumerate((a, b)):
        c = t.rows[0].cells[k]
        pp = c.paragraphs[0]; pp.alignment = WD_ALIGN_PARAGRAPH.CENTER; pp.paragraph_format.first_line_indent = Cm(0)
        pp.add_run().add_picture(str(P / path), width=Cm(width_cm))
        fig_no[chapter[0]] = fig_no.get(chapter[0], 0) + 1
        cp = c.add_paragraph(f"Hình {chapter[0]}.{fig_no[chapter[0]]}. {cap}", style="Caption")
    sp = doc.add_paragraph(); sp.paragraph_format.space_after = Pt(2); sp.paragraph_format.first_line_indent = Cm(0)


def callout(title, text, fill="FFF7E6", border="E6C27A"):
    t = fixed_table(1, 1, [CONTENT_W]); borders(t, border, False, 8); cell_margins(t, 140, 140, 200, 200)
    c = t.rows[0].cells[0]; shade(c, fill)
    p = c.paragraphs[0]; p.style = doc.styles["Note"]
    r = p.add_run(title + "  "); r.bold = True; r.font.color.rgb = RGBColor.from_string(GOLD)
    p.add_run(text)
    sp = doc.add_paragraph(); sp.paragraph_format.space_after = Pt(2); sp.paragraph_format.first_line_indent = Cm(0)


def field(par, instr, placeholder=""):
    r = par.add_run(); f1 = OxmlElement("w:fldChar"); f1.set(qn("w:fldCharType"), "begin"); r._r.append(f1)
    r = par.add_run(); it = OxmlElement("w:instrText"); it.set(qn("xml:space"), "preserve"); it.text = f" {instr} "; r._r.append(it)
    r = par.add_run(); f2 = OxmlElement("w:fldChar"); f2.set(qn("w:fldCharType"), "separate"); r._r.append(f2)
    par.add_run(placeholder)
    r = par.add_run(); f3 = OxmlElement("w:fldChar"); f3.set(qn("w:fldCharType"), "end"); r._r.append(f3)


def h1(text, numbered=True, letter=0):
    if numbered:
        chapter[0] += 1
        text = f"CHƯƠNG {chapter[0]}. {text.upper()}"
    else:
        chapter[0] = letter
    p = doc.add_paragraph(text, style="Heading 1")
    p.paragraph_format.page_break_before = True
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER if not numbered else WD_ALIGN_PARAGRAPH.LEFT
    pb = p._p.get_or_add_pPr(); bdr = OxmlElement("w:pBdr"); bt = OxmlElement("w:bottom")
    bt.set(qn("w:val"), "single"); bt.set(qn("w:sz"), "8"); bt.set(qn("w:space"), "6"); bt.set(qn("w:color"), GOLD); bdr.append(bt); pb.append(bdr)
    return p


sub = [0, 0]


def h2(text):
    sub[0] += 1; sub[1] = 0
    return doc.add_paragraph(f"{chapter[0]}.{sub[0]}. {text}", style="Heading 2")


def h3(text):
    sub[1] += 1
    return doc.add_paragraph(f"{chapter[0]}.{sub[0]}.{sub[1]}. {text}", style="Heading 3")


def new_chapter(title):
    sub[0] = sub[1] = 0
    h1(title)


def section_break(start_page=None, fmt=None):
    s = doc.add_section(WD_SECTION.NEW_PAGE)
    s.header.is_linked_to_previous = False
    s.footer.is_linked_to_previous = False
    sectPr = s._sectPr
    pg = OxmlElement("w:pgNumType")
    if fmt: pg.set(qn("w:fmt"), fmt)
    if start_page: pg.set(qn("w:start"), str(start_page))
    sectPr.append(pg)
    return s


def page_setup(s):
    s.page_width, s.page_height = Cm(21), Cm(29.7)
    s.top_margin, s.bottom_margin = Cm(2.0), Cm(2.0)
    s.left_margin, s.right_margin = Cm(3.0), Cm(2.0)
    s.header_distance = s.footer_distance = Cm(1.0)


def header_footer(s, header_text=True):
    for p in s.header.paragraphs: p.text = ""
    for p in s.footer.paragraphs: p.text = ""
    if header_text:
        hp = s.header.paragraphs[0]; hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT; hp.paragraph_format.first_line_indent = Cm(0)
        r = hp.add_run("Báo cáo đồ án Capstone — NihongoLife"); set_font(r, BODY, 10, MUTED, italic=True)
        pb = hp._p.get_or_add_pPr(); bdr = OxmlElement("w:pBdr"); bt = OxmlElement("w:bottom")
        bt.set(qn("w:val"), "single"); bt.set(qn("w:sz"), "4"); bt.set(qn("w:space"), "2"); bt.set(qn("w:color"), LINE); bdr.append(bt); pb.append(bdr)
    fp = s.footer.paragraphs[0]; fp.alignment = WD_ALIGN_PARAGRAPH.CENTER; fp.paragraph_format.first_line_indent = Cm(0)
    field(fp, "PAGE", "1")
    for run in fp.runs: set_font(run, BODY, 11, NAVY)


# ═════════════════════════ cover ═════════════════════════
s0 = doc.sections[0]; page_setup(s0)
s0.left_margin = Cm(2.5); s0.right_margin = Cm(2.0)
pg = OxmlElement("w:pgBorders"); pg.set(qn("w:offsetFrom"), "page")
for side in ("top", "left", "bottom", "right"):
    e = OxmlElement(f"w:{side}"); e.set(qn("w:val"), "thinThickSmallGap"); e.set(qn("w:sz"), "24"); e.set(qn("w:space"), "24"); e.set(qn("w:color"), NAVY); pg.append(e)
s0._sectPr.append(pg)


def cover_line(text, size, color=NAVY, bold=True, after=4, before=0, font=BODY):
    p = doc.add_paragraph(style="Front"); p.paragraph_format.space_after = Pt(after); p.paragraph_format.space_before = Pt(before)
    r = p.add_run(text); set_font(r, font, size, color, bold)
    return p


cover_line("HỌC VIỆN CÔNG NGHỆ THÔNG TIN & THIẾT KẾ VTC ACADEMY", 14, NAVY, True, 2, 10)
cover_line("─────── ✦ ───────", 12, GOLD, False, 10)
lp = doc.add_paragraph(style="Front"); lp.add_run().add_picture(str(P / "shots/vtc-logo.png"), width=Cm(5.2)); lp.paragraph_format.space_after = Pt(8)
cover_line("BÁO CÁO ĐỒ ÁN TỐT NGHIỆP", 18, NAVY, True, 2)
cover_line("PROJECT 3 – CAPSTONE", 13, MUTED, True, 14)
cover_line("NIHONGOLIFE", 40, NAVY, True, 0)
cover_line("ひばり町で、日本語と暮らそう", 16, GOLD, False, 6, 0, JP)
cover_line("GAME MÔ PHỎNG CUỘC SỐNG 3D KẾT HỢP HỌC TIẾNG NHẬT N5", 14, TEXT, True, 4)
cover_line("Đời sống – học tập – việc làm thêm – nông trại Đảo Midori", 12.5, MUTED, False, 14)
pp = doc.add_paragraph(style="Front"); pp.add_run().add_picture(str(P / "img/isl_overview.jpg"), width=Cm(11.5)); pp.paragraph_format.space_after = Pt(10)
info = [("Ngành", "Công nghệ thông tin"), ("Chuyên ngành", "Lập trình game"), ("Giảng viên hướng dẫn", "Nguyễn Ngọc Chấn"),
        ("Học viên thực hiện", "Quách Thành Long"), ("MSSV", "124010124034"), ("Lớp", "K24GD03")]
it = fixed_table(6, 2, [int(5.0 / 2.54 * 1440), int(7.0 / 2.54 * 1440)]); borders(it, "FFFFFF", False); cell_margins(it, 30, 30, 80, 80)
for k, (lab, val) in enumerate(info):
    a, b = it.rows[k].cells
    pa = a.paragraphs[0]; pa.style = doc.styles["Cell"]; x = pa.add_run(lab + ":"); set_font(x, BODY, 13, MUTED)
    pb = b.paragraphs[0]; pb.style = doc.styles["Cell"]; y = pb.add_run(val); set_font(y, BODY, 13, NAVY, True)
cover_line("TP. Hồ Chí Minh, tháng 10 năm 2026", 13, TEXT, False, 0, 14)

# ═════════════════════════ front matter (roman) ═════════════════════════
s1 = section_break(1, "lowerRoman"); page_setup(s1); header_footer(s1, header_text=False)
pgb = s1._sectPr.find(qn("w:pgBorders"))
if pgb is not None: s1._sectPr.remove(pgb)


first_front = [True]


def front_title(text, in_toc=True):
    p = doc.add_paragraph(text, style="Heading 1" if in_toc else "Normal")
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    if not in_toc:
        p.paragraph_format.first_line_indent = Cm(0); p.paragraph_format.space_after = Pt(12)
        for r in p.runs: set_font(r, BODY, 16, NAVY, True)
    if not first_front[0]: p.paragraph_format.page_break_before = True
    first_front[0] = False
    return p


front_title("LỜI CẢM ƠN")
for t in [
    "Em xin chân thành cảm ơn thầy Nguyễn Ngọc Chấn, giảng viên hướng dẫn, đã định hướng đề tài, góp ý về phạm vi và cách đánh giá sản phẩm trong suốt quá trình thực hiện đồ án. Những nhận xét của thầy giúp em giữ đồ án tập trung vào giá trị học tập thay vì chạy theo số lượng tính năng.",
    "Em cảm ơn quý thầy cô Học viện Công nghệ Thông tin & Thiết kế VTC Academy đã trang bị nền tảng về lập trình, thiết kế trò chơi và quy trình phát triển phần mềm — những kiến thức được vận dụng trực tiếp trong NihongoLife.",
    "Em cũng cảm ơn các tác giả tài nguyên mở (Quaternius, Kenney, KayKit, Mixamo) và cộng đồng Unity. Trong quá trình phát triển, em sử dụng các trợ lý lập trình AI (Claude, Codex) như công cụ hỗ trợ; mọi quyết định thiết kế, duyệt kết quả và chịu trách nhiệm về sản phẩm thuộc về em.",
    "Do thời gian và kinh nghiệm còn hạn chế, báo cáo khó tránh khỏi thiếu sót. Em rất mong nhận được ý kiến đóng góp của quý thầy cô.",
]:
    para(t)
para("TP. Hồ Chí Minh, ngày 10 tháng 10 năm 2026", align=WD_ALIGN_PARAGRAPH.RIGHT, italic=True, indent=False)
para("Học viên thực hiện", align=WD_ALIGN_PARAGRAPH.RIGHT, bold=True, indent=False)
p = para("Quách Thành Long", align=WD_ALIGN_PARAGRAPH.RIGHT, indent=False); p.paragraph_format.space_before = Pt(36)

front_title("NHẬN XÉT CỦA GIẢNG VIÊN HƯỚNG DẪN")
for _ in range(16):
    para("." * 118, indent=False, color="9AA5B4")
para("TP. Hồ Chí Minh, ngày ...... tháng ...... năm 2026", align=WD_ALIGN_PARAGRAPH.RIGHT, italic=True, indent=False)
para("Giảng viên hướng dẫn", align=WD_ALIGN_PARAGRAPH.RIGHT, bold=True, indent=False)
para("(Ký và ghi rõ họ tên)", align=WD_ALIGN_PARAGRAPH.RIGHT, italic=True, indent=False)

front_title("TÓM TẮT ĐỒ ÁN")
for t in [
    "NihongoLife là trò chơi mô phỏng cuộc sống 3D giúp người Việt mới bắt đầu học tiếng Nhật (trình độ N5) luyện giao tiếp trong ngữ cảnh. Người chơi nhập vai một du học sinh vừa đến khu phố Hibari-chō: chào hỏi hàng xóm, mua đồ ở cửa hàng tiện lợi, gọi món ở nhà hàng sushi, mua vé tàu, học ở trường Nhật ngữ, làm thêm để kiếm tiền và trồng trọt, chăn nuôi trên Đảo Midori. Mỗi hoạt động là một tình huống giao tiếp có mục tiêu, có phản hồi khi chọn sai và có thể luyện lại.",
    "Sản phẩm được xây dựng trên Unity 6000.3 (URP) với khoảng 200 script runtime (≈37.000 dòng C#), 11 scene, 14 kịch bản học tập dạng dữ liệu, hệ thống việc làm thêm và nhiệm vụ hướng dữ liệu, kinh tế dùng một ví ¥ duy nhất, trung tâm luyện thi JLPT/IELTS và mini-game Kana Match. Dịch vụ trực tuyến (Supabase, Gemini, Agora) là tuỳ chọn; trò chơi chạy đầy đủ ở chế độ offline.",
    "Quá trình phát triển chia thành 7 Phase, kết thúc bằng đợt hoàn thiện tháng 10/2026: Đảo Midori với vòng lặp trồng trọt có thời gian và dụng cụ, ba công việc làm thêm có thao tác thật, Sổ nhiệm vụ (phím N), cấp độ tính từ dữ liệu, HUD gọn theo cảm hứng The Sims 4 và Sons of the Forest, cùng chế độ điều khiển chuột khoá/Ctrl. Kết quả kiểm thử tự động ngày 10/10/2026: 22/22 bài EditMode và 24/24 bài Play Mode đạt.",
]:
    para(t)
para("**Từ khoá:** game giáo dục, học tiếng Nhật N5, mô phỏng cuộc sống, Unity, học theo tình huống, thiết kế hướng dữ liệu.", indent=False)

front_title("MỤC LỤC", in_toc=False)
p = doc.add_paragraph(); p.paragraph_format.first_line_indent = Cm(0)
field(p, 'TOC \\o "1-2" \\h \\z \\u', "Nhấn chuột phải → Update Field để cập nhật mục lục.")
front_title("DANH MỤC HÌNH ẢNH", in_toc=False)
p = doc.add_paragraph(); p.paragraph_format.first_line_indent = Cm(0)
field(p, 'TOC \\h \\z \\t "Caption,1"', "Cập nhật trường để hiện danh mục hình.")
front_title("DANH MỤC BẢNG", in_toc=False)
p = doc.add_paragraph(); p.paragraph_format.first_line_indent = Cm(0)
field(p, 'TOC \\h \\z \\t "TableCaption,1"', "Cập nhật trường để hiện danh mục bảng.")
front_title("DANH MỤC TỪ VIẾT TẮT VÀ THUẬT NGỮ")
table(["Từ / thuật ngữ", "Giải thích"], [
    ["N5", "Cấp độ thấp nhất của kỳ thi năng lực tiếng Nhật JLPT"],
    ["JLPT", "Japanese-Language Proficiency Test — kỳ thi năng lực tiếng Nhật"],
    ["IELTS", "International English Language Testing System"],
    ["URP", "Universal Render Pipeline của Unity"],
    ["HUD", "Head-Up Display — lớp giao diện hiển thị khi đang chơi"],
    ["NPC", "Non-Player Character — nhân vật do máy điều khiển"],
    ["Scenario", "Kịch bản học tập: chuỗi node hội thoại, lựa chọn và mục tiêu"],
    ["Scene additive", "Scene được nạp chồng lên scene đang chạy (khu vực trên nền thành phố)"],
    ["Play Mode / EditMode test", "Kiểm thử tự động chạy trong game thật / trong trình soạn thảo"],
    ["Builder", "Script Editor chạy ở chế độ batch để dựng và lưu scene, không cần thao tác tay"],
    ["Konbini", "Cửa hàng tiện lợi kiểu Nhật (ひばりマート trong game)"],
    ["XP / Kiến thức", "Điểm kinh nghiệm (cấp độ) / điểm học tập — hai chỉ số tách biệt"],
], [3.2, 9], caption=None)

# ═════════════════════════ main content (arabic) ═════════════════════════
s2 = section_break(1, "decimal"); page_setup(s2); header_footer(s2, header_text=True)

# ── Chapter 1
new_chapter("Giới thiệu đề tài")
h2("Bối cảnh và vấn đề")
para("Số người Việt học tiếng Nhật tăng mạnh nhờ nhu cầu du học và làm việc, nhưng phần lớn người mới học gặp chung một khó khăn: thuộc từ vựng và mẫu câu trên giấy nhưng lúng túng khi phải phản hồi trong một tình huống thật — hỏi đường ở nhà ga, gọi món, trả tiền ở cửa hàng. Bài học truyền thống tách rời kiến thức khỏi địa điểm, người đối thoại và mục đích giao tiếp, nên người học ít có cơ hội luyện phản xạ chọn câu đúng trong ngữ cảnh.")
para("Các ứng dụng học ngôn ngữ phổ biến tập trung vào thẻ từ và bài tập ngắn. Chúng hiệu quả cho ghi nhớ nhưng thiếu “cuộc sống” để vận dụng. Ngược lại, game mô phỏng cuộc sống có thế giới sống động nhưng hiếm khi gắn với mục tiêu học tập có thể đo lường.")
h2("Mục tiêu đồ án")
bullets(["Xây dựng một khu phố Nhật Bản 3D nơi mọi hoạt động thường ngày là một tình huống giao tiếp tiếng Nhật N5 có mục tiêu, phản hồi và luyện lại.",
         "Tạo vòng lặp đời sống có ý nghĩa: làm thêm kiếm tiền, chi tiêu cho nhu cầu, trồng trọt và chăn nuôi, mở rộng cuộc sống qua cấp độ và kiến thức.",
         "Thiết kế hệ thống nội dung hướng dữ liệu để thêm kịch bản, nhiệm vụ, phần thưởng mà không phải sửa mã nguồn.",
         "Bảo đảm chất lượng bằng kiểm thử tự động chạy thao tác thật trong game và ảnh chụp minh chứng cho từng tính năng."])
h2("Đối tượng và phạm vi")
para("Người dùng chính là người Việt bắt đầu học tiếng Nhật, chưa vững kana và mẫu câu cơ bản. Giao diện hỗ trợ ba ngôn ngữ hiển thị (Việt, Anh, Nhật); riêng Đảo Midori cho phép chọn ngôn ngữ đang học là tiếng Nhật hoặc tiếng Anh, tiếng Việt luôn là ngôn ngữ trợ giúp khi người chơi bấm xem nghĩa.")
table(["Trong phạm vi", "Ngoài phạm vi (ghi rõ để tránh hiểu nhầm)"], [
    ["Giao tiếp tình huống N5, kana, từ vựng đời sống", "Ngữ pháp trung cấp trở lên, kanji ngoài phạm vi N5"],
    ["7 khu vực chơi + Đảo Midori, tuyến tàu 4 ga", "Thế giới mở quy mô lớn, xe cộ"],
    ["Ba công việc làm thêm, kinh tế một ví ¥", "Mô phỏng doanh nghiệp, quản lý nhân sự"],
    ["Luyện thi JLPT/IELTS rút gọn; Listening IELTS đầy đủ dạng gói cục bộ", "Ngân hàng đề chính thức đầy đủ trong bản phát hành"],
    ["Online tuỳ chọn: đăng nhập, lưu cloud, chat", "Máy chủ game thời gian thực nhiều người"],
], [1, 1], caption="Phạm vi đồ án")
h2("Phương pháp thực hiện")
para("Đồ án được phát triển lặp theo 7 Phase. Mỗi vòng gồm: xác định yêu cầu từ ảnh chụp lỗi hoặc mô tả tính năng, hiện thực, dựng scene bằng builder chạy batch (không thao tác tay trong Editor), kiểm thử Play Mode điều khiển nhân vật như người chơi, chụp ảnh tự động và chỉ ghi nhận hoàn thành khi có bằng chứng. Nội dung (kịch bản, giá cả, nhiệm vụ, thực đơn) nằm trong dữ liệu để người thiết kế chỉnh mà không phải biên dịch lại logic.")
h2("Cấu trúc báo cáo")
para("Chương 2 trình bày cơ sở lý thuyết và các trò chơi tham khảo. Chương 3 phân tích yêu cầu. Chương 4 tóm tắt quá trình 7 Phase. Chương 5 đến 9 mô tả thế giới, gameplay, Đảo Midori, kinh tế – việc làm – nhiệm vụ và hệ thống học tập. Chương 10 phân tích UI/UX. Chương 11 và 12 trình bày kiến trúc, dữ liệu và quy trình. Chương 13 báo cáo kiểm thử; Chương 14 kết luận.")
figure("img/hud_city.jpg", "Khu phố Hibari-chō trong gameplay: ô trạng thái gọn, biển chỉ đường và cổng khu vực")

# ── Chapter 2
new_chapter("Cơ sở lý thuyết và tham khảo thiết kế")
h2("Học ngôn ngữ theo tình huống")
para("Lý thuyết học tập tình huống (situated learning) của Lave và Wenger cho rằng kiến thức được hình thành và ghi nhớ tốt nhất khi gắn với hoạt động và cộng đồng nơi nó được sử dụng [2]. Giả thuyết đầu vào của Krashen nhấn mạnh vai trò của đầu vào dễ hiểu, hơi vượt trình độ hiện tại (i+1) [1]. NihongoLife hiện thực hai ý tưởng này bằng cách đặt mỗi câu tiếng Nhật trong một việc cụ thể — mua cơm nắm, hỏi giá vé, nhận order — và luôn cung cấp cách đọc, nghĩa tiếng Việt theo yêu cầu thay vì hiển thị sẵn.")
h2("Phản hồi và tải nhận thức")
para("Hattie và Timperley chỉ ra phản hồi hiệu quả nhất khi trả lời được “tôi đang ở đâu, cần đi tiếp thế nào” [4]. Vì vậy khi người chơi chọn sai, trò chơi không chỉ báo sai mà giải thích lý do (ví dụ: “からあげ (gà rán) không nằm ở kệ đó”) và cho thử lại; câu trả lời sai không được tính điểm hay tiền. Theo lý thuyết tải nhận thức của Sweller [5], giao diện được tối giản trong lúc chơi: chỉ một mục tiêu được theo dõi, một gợi ý tương tác tại một thời điểm; thông tin chi tiết nằm trong cửa sổ người chơi chủ động mở.")
h2("Trò chơi hoá có kiểm soát")
para("Deterding và cộng sự định nghĩa gamification là dùng yếu tố thiết kế trò chơi trong bối cảnh không phải trò chơi [6]. NihongoLife đi theo hướng ngược lại: một trò chơi thực thụ mà phần thưởng gắn với hành vi học. Tiền chỉ có được từ việc làm có thao tác thật hoặc bán nông sản tự trồng; XP (cấp độ) và Kiến thức là hai chỉ số tách biệt để tránh việc mọi hoạt động bị quy về một con số chung. Trạng thái dòng chảy (flow) của Csikszentmihalyi [7] là cơ sở để giữ độ khó vừa phải: việc làm không yêu cầu cấp độ ban đầu, công cụ tốt hơn giúp làm nhanh hơn chứ không khoá nội dung.")
h2("Tham khảo thiết kế trải nghiệm: The Sims 4")
para("The Sims 4 (Maxis, 2014) là chuẩn mực của thể loại mô phỏng cuộc sống [9]. Đồ án tham khảo ba điểm: (1) nhu cầu nhân vật được trình bày trực quan và chỉ trở nên nổi bật khi xuống thấp; (2) chân dung nhân vật và trạng thái luôn ở một góc cố định, không che thế giới; (3) phản hồi tương tác rõ ràng ngay tại nơi hành động diễn ra. NihongoLife chuyển hoá thành ô trạng thái gọn ở góc dưới trái gồm chân dung, cấp độ, ¥ và năm vòng nhu cầu; cảnh báo chỉ xuất hiện khi một nhu cầu dưới 20%; số liệu chi tiết nằm trong cửa sổ Hồ sơ (Tab). Đồ án không sao chép đồ hoạ hay bố cục của The Sims.")
h2("Tham khảo thiết kế trải nghiệm: Sons of the Forest")
para("Sons of the Forest (Endnight Games, phát hành bản 1.0 năm 2024) theo đuổi giao diện tối giản: phần lớn thông tin sinh tồn hiển thị qua vật thể trong thế giới như đồng hồ đeo tay của nhân vật, kho đồ được bày ra như đồ vật thật, và màn hình gần như không có thanh công cụ cố định [10]. Đồ án học hai nguyên tắc: ưu tiên biểu tượng dạng vòng nhỏ gọn cho chỉ số, và bỏ các nút phím tắt thường trực để thế giới 3D chiếm trọn màn hình. Khác với trò chơi sinh tồn, NihongoLife vẫn giữ nhãn chữ song ngữ vì mục tiêu học tập.")
h2("Tham khảo vòng lặp nông trại")
para("Vòng lặp xới – gieo – tưới – chờ – thu hoạch – bán của Stardew Valley [11] được dùng làm tham chiếu cho Đảo Midori, nhưng rút gọn cho mục tiêu học: mỗi thao tác đi kèm một câu hành động tiếng Nhật (つちを たがやします — xới đất) và tên vật nuôi, cây trồng được ghi vào sổ tay từ vựng.")
table(["Trò chơi tham khảo", "Điều học được", "Cách áp dụng trong NihongoLife"], [
    ["The Sims 4", "Nhu cầu trực quan, chân dung cố định, phản hồi tại chỗ", "Ô trạng thái vòng nhu cầu; cảnh báo khi < 20%; Tab cho số liệu chi tiết"],
    ["Sons of the Forest", "HUD tối giản, ít thanh công cụ cố định", "Bỏ thanh phím tắt; một làn gợi ý; con trỏ ẩn khi chơi, giữ Ctrl để dùng chuột"],
    ["Stardew Valley", "Vòng lặp nông trại theo thời gian", "Cây lớn theo giờ thực, cần tưới mỗi giai đoạn, dụng cụ quyết định tốc độ"],
], [2.2, 3.5, 4.6], caption="Các trò chơi tham khảo và cách chuyển hoá")

# ── Chapter 3
new_chapter("Phân tích yêu cầu")
h2("Tác nhân")
para("Người học là tác nhân chính, tương tác với thế giới, cửa sổ chức năng và bài luyện. Tác giả nội dung chỉnh kịch bản, catalog và tệp tiến trình rồi chạy builder để cập nhật scene. Các dịch vụ trực tuyến (Supabase, Gemini, Agora) là tác nhân phụ, không bắt buộc để chơi.")
figure("diag/d41_use_case_v2.png", "Sơ đồ use case tổng quát")
h2("Yêu cầu chức năng")
table(["Mã", "Yêu cầu", "Tiêu chí chấp nhận"], [
    ["F01", "Hội thoại học tập theo kịch bản", "Chọn sai có giải thích và chọn lại; tiến trình lưu theo node"],
    ["F02", "Mua sắm ở konbini, ăn ở nhà hàng", "Giỏ hàng, thanh toán trừ đúng tiền một lần, vật phẩm vào balo"],
    ["F03", "Đi tàu giữa 4 ga", "Mua vé, soát vé, lên tàu, đến đúng khu vực, chiều về từ Đảo Midori"],
    ["F04", "Trồng trọt, chăn nuôi trên Đảo Midori", "Xới, gieo, tưới, lớn theo thời gian, thu hoạch, bán; cho thú ăn"],
    ["F05", "Việc làm thêm", "Nhận ca, hoàn thành thao tác thật, báo cáo, nhận lương đúng một lần"],
    ["F06", "Sổ nhiệm vụ (N), cấp độ, kiến thức", "Nhận/huỷ/theo dõi; thưởng từ dữ liệu; XP và kiến thức tách biệt"],
    ["F07", "Luyện thi JLPT/IELTS, Kana Match", "Làm bài, chấm, lưu lịch sử; nhãn phạm vi trung thực"],
    ["F08", "Cài đặt", "Âm lượng, ngôn ngữ, độ nhạy chuột, đảo trục Y, kiểu điều khiển, đổi phím"],
    ["F09", "Lưu và đồng bộ", "Lưu cục bộ; đồng bộ cloud khi đăng nhập không mất balo, đảo, nhiệm vụ"],
], [0.8, 3.6, 5.6], caption="Yêu cầu chức năng chính")
h2("Yêu cầu phi chức năng")
table(["Nhóm", "Yêu cầu"], [
    ["Hiệu năng", "Khung hình ổn định trên máy học sinh; đảo Midori đo trung bình 1,4 ms/khung trong Editor"],
    ["Khả dụng", "HUD không chồng lớp ở 1920×1080, 1600×900, 1366×768, 1280×720; mọi cửa sổ đóng được bằng × và Esc"],
    ["Toàn vẹn dữ liệu", "Không trùng phần thưởng, không trừ tiền hai lần, không lưu hai ví"],
    ["Bảo trì", "Nội dung hướng dữ liệu; scene dựng bằng builder; không dùng lệnh menu thủ công"],
    ["Bản quyền", "Đề thi có bản quyền chỉ lưu cục bộ, không đưa vào repository hay bản phát hành"],
    ["Ngoại tuyến", "Chơi đầy đủ khi không có mạng; dịch vụ online lỗi thì tự chuyển offline"],
], [2.2, 8], caption="Yêu cầu phi chức năng")
h2("Đặc tả use case tiêu biểu")
table(["Thành phần", "UC05 — Làm một ca làm thêm ở konbini"], [
    ["Tác nhân", "Người học"],
    ["Tiền điều kiện", "Không đang làm ca khác; ca konbini không trong thời gian chờ"],
    ["Luồng chính", "Mở Sổ nhiệm vụ (N) → Nhận ca → lấy thùng hàng trong kho → xếp 3 kệ → chỉ đường đúng cho 2 khách → tính đúng tiền 1 lần → báo cáo chị Ito → nhận ¥600, 40 XP, 6 kiến thức"],
    ["Luồng thay thế", "Trả lời sai: hiển thị giải thích, không tính; huỷ ca: không lương, trạng thái về “có thể nhận”"],
    ["Hậu điều kiện", "Lương trả đúng một lần; ca tiếp theo mở sau 5 phút; số ca đã làm tăng 1"],
], [2.4, 8], caption="Đặc tả use case làm thêm")

# ── Chapter 4
new_chapter("Quá trình phát triển qua 7 Phase")
para("Sản phẩm được phát triển lặp. Mỗi Phase có phạm vi được thống nhất trước và chỉ được ghi nhận hoàn thành khi các bài kiểm thử Play Mode tương ứng đạt và có ảnh chụp minh chứng.")
figure("diag/d30_phases.png", "Bảy Phase phát triển")
table(["Phase", "Nội dung chính", "Bằng chứng kiểm thử"], [
    ["1. HUD & khung UI", "HUD dùng chung mọi khu vực, nút × cho mọi cửa sổ, ảnh vật phẩm, thể lực và chạy", "Hud_StatusDock, City_DialogueShopBagMap, WorldFeel"],
    ["2. Điều khiển & camera", "Ngăn xếp cửa sổ (Esc đóng cửa sổ trên cùng), O mở Cài đặt, hồ sơ 3D (Tab), sửa nhân vật lơ lửng", "ModalInput, StationStandalone"],
    ["3. Luyện thi", "Engine IELTS đọc gói cục bộ, Listening 40 câu hai chế độ; đề rút gọn có nhãn phạm vi", "IeltsListening (E2E), IeltsGraderTests"],
    ["4. Tuyến tàu", "Bốn ga Hibari → Gakuen-mae → Minato → Midori, chiều về từ đảo, không trừ tiền hai lần", "Station_FullTripToMinato, MidoriIsland_FullAcceptanceFlow"],
    ["5. Đảo Midori", "Trồng trọt, chăn nuôi, cửa hàng, sổ tay từ vựng, ngôn ngữ học JA/EN", "MidoriIsland_* (2 bài, luồng 17 bước)"],
    ["6. Đồng bộ hình ảnh", "Hướng chữ 3D, nhãn tên NPC không trùng, biển hiệu không bị che", "Ảnh chụp island/lifeloop"],
    ["7. Đời sống & sự nghiệp", "HUD gọn, chuột khoá + Ctrl, việc làm, Sổ nhiệm vụ (N), cấp độ dữ liệu, gộp bản lưu", "LifeLoop (3 bài), ProgressionCatalogTests, ProgressMergeTests"],
], [2.6, 5, 3.6], caption="Tóm tắt 7 Phase và bằng chứng")
callout("Nguyên tắc ghi nhận.", "Không có Phase nào được báo cáo hoàn thành chỉ dựa trên mô tả hay ảnh dựng sẵn. Mọi tính năng trong báo cáo này đều có trong mã nguồn ở commit a1c9f99b (nhánh main, 10/10/2026) và được bài kiểm thử tự động chạm tới.")

# ── Chapter 5
new_chapter("Thế giới game và cốt truyện")
h2("Hibari-chō và các khu vực")
para("Thế giới xoay quanh thành phố trung tâm Hibari-chō (scene 90_TestSandbox). Các khu vực — phòng trọ, lớp học, Game Center, ga tàu, nhà hàng sushi và Đảo Midori — là scene riêng được nạp chồng lên thành phố. Cách tổ chức này giữ HUD, balo, người chơi và các dịch vụ liên tục giữa các khu vực, đồng thời cho phép mở thẳng một khu vực trong Editor để kiểm tra (StandaloneZoneBootstrap tự khởi động qua thành phố).")
figure("diag/d31_world_v2.png", "Thế giới game: thành phố trung tâm, khu vực và tuyến tàu")
h2("Nhân vật và mạch truyện")
para("Người chơi là du học sinh Việt Nam đến Hibari Nihongo Gakuin vào mùa xuân. Mười bốn kịch bản dẫn từ ngày đầu chào hỏi, đến khi tự đi tàu, gọi món, học ở lớp và hoà nhập với khu phố. Mỗi kịch bản là một ScenarioDefinition gồm các node hội thoại, lựa chọn, mục tiêu và cờ truyện; tiến trình lưu theo node nên đóng hội thoại giữa chừng không làm mất mạch.")
fig_pair(("img/bedroom.jpg", "Phòng trọ — điểm bắt đầu mỗi ngày"), ("img/map.jpg", "Bản đồ (M) với vị trí thật của các địa điểm"))
h2("Tuyến tàu")
para("Ga Hibari có nhân viên Kimura và máy bán vé. Người chơi chọn ga đến, trả đúng giá (Gakuen-mae ¥180, Minato ¥320, Midori ¥450), qua cửa soát vé, chờ tàu ở sân ga số 2 và đi qua từng ga với thông báo tiếng Nhật. Đến nơi, vé được thu và người chơi xuất hiện ở đúng khu vực. Chiều về từ Đảo Midori dùng máy bán vé và cửa tàu trên đảo; nếu người chơi hết tiền và không còn gì để bán, nhân viên tặng một vé hỗ trợ để tránh kẹt lại trên đảo.")
fig_pair(("img/station_staff.jpg", "Hỏi vé với nhân viên Kimura"), ("img/on_train.jpg", "Trên tàu: bảng LED báo ga tiếp theo"))

# ── Chapter 6
new_chapter("Gameplay cốt lõi và hệ thống đời sống")
h2("Vòng lặp chính")
para("Vòng lặp cốt lõi gồm: khám phá khu phố → nhận mục tiêu → tương tác (F) → chọn câu tiếng Nhật → nhận phản hồi, sửa lỗi → tiến bộ (kiến thức, cấp độ, tiền) → mở hoạt động mới. Đợt hoàn thiện tháng 10 bổ sung vòng lặp kinh tế: làm thêm → có tiền → mua thức ăn, vé tàu, hạt giống → duy trì nhu cầu và mở rộng nông trại.")
figure("diag/d01_core_loop.png", "Vòng lặp cốt lõi học – chơi", 12.5)
h2("Hội thoại học tập")
para("Hộp hội thoại hiển thị tên người nói, câu tiếng Nhật, cách đọc, romaji (ở chế độ hướng dẫn) và nghĩa tiếng Việt. Ba chế độ học điều chỉnh mức hỗ trợ: Hướng dẫn hiện đủ, Luyện tập ẩn romaji, Kiểm tra ẩn cả cách đọc và nghĩa. Người chơi có thể tạm rời hội thoại bằng × hoặc Esc và tiếp tục bằng phím R.")
fig_pair(("img/dialogue.jpg", "Hộp hội thoại song ngữ"), ("img/emote.jpg", "Vòng biểu cảm (giữ E)"))
h2("Mua sắm và nhà hàng")
para("Konbini ひばりマート có bốn kệ theo nhóm hàng; mỗi món có chữ Nhật, cách đọc, nghĩa và ảnh. Người chơi cho món vào giỏ, thanh toán ở quầy với chị Ito và nhận lời cảm ơn bằng tiếng Nhật. Tiền chỉ bị trừ một lần; nếu balo đầy, món không nhận được sẽ được hoàn tiền. Ở nhà hàng sushi, người chơi xem thực đơn, gọi món, ăn và trả tiền theo hoá đơn.")
fig_pair(("img/shop_onigiri.jpg", "Kệ cơm nắm với ảnh và giá"), ("img/checkout.jpg", "Thanh toán ở quầy"))
h2("Nhu cầu, thể lực và balo")
para("Nhân vật có năm chỉ số: sức khoẻ, thể lực, no, khát và tỉnh táo. Chạy tiêu hao thể lực; hết sức thì không chạy được cho tới khi hồi đủ. Đồ ăn uống hồi nhu cầu; ngủ ở phòng trọ hồi tỉnh táo và thể lực. Balo có 16 ô, hiển thị ảnh vật phẩm và cho dùng hoặc bỏ đồ.")
fig_pair(("img/bag.jpg", "Balo với ảnh vật phẩm"), ("img/profile.jpg", "Hồ sơ nhân vật (Tab): cấp độ, tiền, kiến thức, nhu cầu"))
figure("diag/d19_state_stamina.png", "Máy trạng thái thể lực", 14)

# ── Chapter 7
new_chapter("Đảo Midori: trồng trọt và chăn nuôi")
h2("Tổng quan")
para("Đảo Midori (みどりじま, scene 60_MidoriIsland) là khu vực mới nhất, đến bằng tàu từ ga Hibari (vé ¥450). Đảo được dựng hoàn toàn bằng builder từ các bộ tài nguyên Farm Buildings, Ultimate Crops (Quaternius) và Survival Kit (Kenney): ga, quảng trường, sáu ô ruộng, chuồng thú với bò, alpaca, lừa và ngựa, chú chó Shiba quanh quảng trường, cửa hàng Midori cạnh ga, điểm ngắm cảnh và sáu biển chỉ đường dạy tên địa điểm. Bờ biển có tường vô hình và cơ chế đưa người chơi về ga nếu rơi khỏi đảo.")
figure("img/isl_overview.jpg", "Toàn cảnh Đảo Midori: ga, ruộng, chuồng thú, cửa hàng và điểm ngắm cảnh")
fig_pair(("img/isl_arrival.jpg", "Đến đảo: thanh công cụ, lời chào và quà khởi đầu"), ("img/isl_travel.jpg", "Màn hình tàu chạy khi trở về Hibari"))
h2("Vòng lặp trồng trọt")
para("Mỗi ô ruộng đi qua các trạng thái: chưa xới → đã xới → cần tưới → đang lớn → (cần tưới lại) → chín → thu hoạch. Thời gian lớn tính theo dấu thời gian UTC lưu trong bản lưu, nên cây vẫn lớn khi người chơi rời đảo. Bốn loại cây có thông số trong catalog: cà rốt 20 giây/giai đoạn, cà chua 30 giây, ngô 40 giây, bí ngô 50 giây; mỗi lần thu hoạch được 2 quả.")
figure("diag/d32_farm_loop.png", "Quy trình trồng trọt", 15.5)
figure("diag/d33_farm_state.png", "Máy trạng thái một ô ruộng", 14)
h2("Dụng cụ và thao tác có thời gian")
para("Theo góp ý từ chủ dự án, việc làm nông phải hợp lý: thiếu dụng cụ thì vẫn làm được nhưng rất chậm, hoặc không làm được nếu không có cách nào khác. Mọi thao tác hiển thị thanh tiến trình, nhân vật đứng yên trong lúc làm và có thể dừng bằng Esc (không tính kết quả). Thao tác chỉ chạy một lần tại một thời điểm, nên bấm liên tục không làm cộng dồn.")
table(["Thao tác", "Có dụng cụ", "Không có dụng cụ"], [
    ["Xới đất", "Cuốc 1,6 s · xẻng 2,6 s", "Bằng tay 7 s (chậm ~4 lần)"],
    ["Gieo hạt", "1,2 s", "—"],
    ["Tưới nước", "Bình tưới 1,5 s", "Không thể: không có gì để mang nước"],
    ["Thu hoạch", "1,4 s", "—"],
    ["Dọn ô", "Xẻng 1,8 s · cuốc 2,5 s", "Bằng tay 6 s"],
    ["Cho thú ăn", "1,0 s (cần cà rốt hoặc ngô)", "—"],
], [2.5, 3.5, 4], caption="Thời gian thao tác theo dụng cụ (FarmActionTimes)")
fig_pair(("img/dig_hand.jpg", "Xới đất bằng tay khi chưa có cuốc"), ("img/isl_seed.jpg", "Thẻ ô ruộng: chọn hạt và câu hành động"))
fig_pair(("img/isl_crop.jpg", "Cây đang lớn trên ô đã xới"), ("img/isl_ready.jpg", "Cây chín, sẵn sàng thu hoạch"))
h2("Chăn nuôi")
para("Các con vật dùng mô hình có hoạt ảnh thật (đi, đứng, ăn) từ bộ Ultimate Animated Animals của Quaternius. Chúng đi lại trong chuồng, quay về phía người chơi khi được bắt chuyện. Thẻ con vật hiển thị tên theo ngôn ngữ đang học, tiếng kêu (モー cho bò) và cho phép cho ăn bằng nông sản hoặc vuốt ve. Gặp đủ năm con vật mở thành tích “どうぶつの ともだち”.")
figure("img/isl_animal.jpg", "Thẻ con vật: tên, tiếng kêu, cho ăn và vuốt ve")
h2("Cửa hàng Midori và sổ tay")
para("Cửa hàng mở bằng F ở quầy hoặc phím P ở bất kỳ đâu trên đảo, gồm bốn mục: Nông nghiệp (hạt giống, dụng cụ), Thời trang, Công nghệ (laptop, TV là đồ sưu tầm) và Bán nông sản. Mục Thời trang để trống kèm lời giải thích vì dự án chưa có mô hình quần áo phù hợp — đồ án chọn minh bạch thay vì bán món không tồn tại. Sổ tay tiến độ ghi số lần thu hoạch, nông sản đã bán, tiền kiếm được, từ đã học và thành tích.")
fig_pair(("img/isl_shop.jpg", "Cửa hàng Midori — mục Nông nghiệp"), ("img/isl_notebook.jpg", "Sổ tay Đảo Xanh: thống kê, từ vựng, thành tích"))
h2("Ngôn ngữ học: tiếng Nhật hoặc tiếng Anh")
para("Trên đảo, người chơi chọn ngôn ngữ đang học là tiếng Nhật (mặc định) hoặc tiếng Anh; tên cây, con vật, dụng cụ và câu hành động đổi theo lựa chọn. Tiếng Việt là ngôn ngữ trợ giúp, chỉ hiện khi bấm “Nghĩa?”. Trò chơi không tạo âm thanh giả cho từ vựng: không có giọng đọc nào được tổng hợp nếu dự án không có file âm thanh thật.")
figure("img/isl_english.jpg", "Cửa hàng khi chuyển ngôn ngữ học sang tiếng Anh")

# ── Chapter 8
new_chapter("Kinh tế, việc làm thêm và hệ thống nhiệm vụ")
h2("Một ví ¥ duy nhất")
para("Mọi khoản thu và chi đi qua PlayerInventory.Yen. Không có tiền tệ thứ hai, không có ví riêng cho nông trại. Nguồn thu gồm lương ca làm, bán nông sản và thưởng cốt truyện; khoản chi gồm đồ ăn, vé tàu, hạt giống, dụng cụ, nhà hàng và đồ công nghệ. Người chơi mới luôn có cách kiếm tiền đầu tiên vì các ca làm không yêu cầu cấp độ hay kiến thức.")
figure("diag/d37_economy.png", "Dòng tiền trong game")
h2("Ba công việc làm thêm")
para("Hệ thống nghề cũ (trả lương khi bấm một nút) đã bị loại bỏ. Mỗi ca làm hiện nay gồm các bước là thao tác thật trong thế giới, kết thúc bằng việc báo cáo với người giao việc để nhận lương.")
table(["Công việc", "Người giao việc", "Các bước", "Lương"], [
    ["Phụ việc ở ひばりマート", "Chị Ito (thu ngân)", "Bê thùng hàng → xếp 3 kệ → chỉ đúng kệ cho 2 khách hỏi bằng tiếng Nhật → tính đúng tổng tiền 1 lần → báo cáo", "¥600, 40 XP, 6 KT"],
    ["Phục vụ ở ひばり寿司", "Anh Aoki", "Nghe khách gọi món ở 2 bàn → lấy đúng món ở quầy bếp → mang tới đúng bàn → báo cáo", "¥700, 45 XP, 6 KT"],
    ["Phụ việc nông trại", "Cô Hana (Đảo Midori)", "Nhận 2 túi hạt → xới 2 ô → gieo 2 hạt → tưới 2 lần → cho 1 con vật ăn → báo cáo", "¥500, 40 XP, 4 KT"],
], [2.4, 2.2, 5.6, 2], caption="Ba công việc làm thêm (giá trị lấy từ progression.json)", font_size=10.5)
fig_pair(("img/customer_q.jpg", "Khách hỏi chỗ bán trà: chọn đúng kệ"), ("img/register_q.jpg", "Quầy thu ngân: tính tổng tiền bằng tiếng Nhật"))
fig_pair(("img/sushi_order.jpg", "Nhận order ở nhà hàng sushi"), ("img/farm_paid.jpg", "Cô Hana trả lương sau ca nông trại"))
h2("Chống gian lận và trùng lặp")
bullets(["Câu trả lời sai được giải thích nhưng không cộng tiến độ.",
         "Mỗi kệ chỉ được xếp một lần trong một ca; mỗi bàn chỉ phục vụ một lần.",
         "Phần thưởng chỉ trả khi cờ rewardClaimed chưa đặt; trạng thái được đánh dấu và lưu trước khi cộng tiền.",
         "Mỗi lúc chỉ làm một ca; huỷ ca không có lương; ca tiếp theo mở sau thời gian chờ (5 phút).",
         "Thao tác có thời gian chỉ chạy một lần, bấm liên tục không cộng dồn."])
figure("diag/d34_seq_job.png", "Sơ đồ tuần tự một ca làm ở konbini", 16)
h2("Sổ nhiệm vụ (phím N)")
para("Sổ nhiệm vụ là nơi duy nhất tập hợp cốt truyện, việc làm, nhiệm vụ nông trại, học tập và việc hằng ngày. Cột trái lọc theo loại và chia Đang làm / Có thể nhận / Đã xong; cột phải hiện mô tả, người giao việc, địa điểm, yêu cầu, từng mục tiêu với tiến độ thật và phần thưởng. Người chơi nhận việc, huỷ hoặc chọn theo dõi; mục tiêu đang theo dõi hiện thành một dòng ở góc trên trái màn hình.")
fig_pair(("img/journal.jpg", "Sổ nhiệm vụ — chi tiết ca làm ở konbini"), ("img/journal_active.jpg", "Sau khi nhận ca: tiến độ từng mục tiêu"))
figure("diag/d35_quest_state.png", "Vòng đời một nhiệm vụ", 14)
h2("Tiến trình hướng dữ liệu")
para("Toàn bộ nhiệm vụ, phần thưởng và ngưỡng cấp độ nằm trong một tệp duy nhất: Assets/NihongoLife/Resources/Progression/progression.json. Định nghĩa (chỉ đọc) tách biệt khỏi trạng thái người chơi (PlayerProgressDto.quests). Mã gameplay chỉ báo sự kiện, ví dụ QuestService.Raise(\"water\", \"carrot\"); QuestService quyết định mục tiêu nào được cộng. Bài kiểm thử ProgressionCatalogTests tự phát hiện id trùng, sự kiện không hỗ trợ, thiếu bản dịch, thưởng âm, vòng phụ thuộc và vật phẩm không tồn tại.")
figure("diag/d36_progression_data.png", "Kiến trúc tiến trình hướng dữ liệu")
table(["Nhiệm vụ", "Loại", "Mục tiêu", "Thưởng"], [
    ["Vụ mùa đầu tiên", "Nông trại", "Thu hoạch 2 lần, bán 2 nông sản", "¥150, 35 XP, 3 KT"],
    ["Sổ tay từ vựng Đảo Xanh", "Học tập", "Học 8 từ mới trên đảo", "30 XP, 12 KT"],
    ["Bữa ăn hôm nay", "Hằng ngày", "Mua 1 món ở konbini bằng tiền tự kiếm", "10 XP, 1 KT (mỗi 24 giờ)"],
], [3, 2, 4.2, 2.8], caption="Các nhiệm vụ không phải việc làm trong dữ liệu hiện tại")
h2("Cấp độ, kinh nghiệm và kiến thức")
para("XP biểu thị kinh nghiệm chung và quyết định cấp độ; Kiến thức biểu thị tiến bộ ngôn ngữ; Tiền biểu thị sức mua; Nhu cầu là trạng thái ngắn hạn. Bản lưu giữ tổng XP, cấp độ được tính lại từ bảng xpToNext (100, 160, 240, 340, …). Bản lưu cũ được nâng cấp để không mất cấp. Trước đợt hoàn thiện, việc cộng XP tự động cộng cả kiến thức; hai chỉ số nay đã tách hẳn.")

# ── Chapter 9
new_chapter("Hệ thống học tập và khảo thí")
h2("Học qua kịch bản")
para("Mười bốn kịch bản N5 bao phủ chào hỏi, tự giới thiệu, mua sắm, hỏi đường, đi tàu, gọi món và sinh hoạt lớp học. Mỗi lựa chọn đúng/sai được ScoringManager ghi nhận theo kỹ năng; LearningMasteryManager theo dõi mức thành thạo của từng từ/mẫu câu và việc làm thêm cũng ghi nhận lượt dùng từ vựng (ví dụ tên món ăn, số tiền).")
h2("Trung tâm luyện thi JLPT và IELTS")
para("Trung tâm luyện thi (phím K hoặc bàn thi trong lớp học) có hai tab JLPT và IELTS. Đồ án ghi nhãn phạm vi trung thực cho từng đề: đề có sẵn là đề rút gọn tự soạn, không phải đề chính thức; phần Writing/Speaking IELTS chấm bằng AI (cần mạng, chỉ mang tính ước tính).")
table(["Nội dung", "Hiện trạng"], [
    ["JLPT N5 — đề luyện tập 1", "Đề rút gọn tự soạn: từ vựng/kanji, ngữ pháp/đọc hiểu, nghe; có chấm, lưu kết quả tốt nhất"],
    ["IELTS Academic — đề luyện tập 1", "Đề rút gọn tự soạn đủ 4 kỹ năng; Writing/Speaking chấm bằng Gemini khi có mạng"],
    ["IELTS Listening đầy đủ (gói cục bộ)", "Engine đọc gói đề từ thư mục LocalContent nằm ngoài Assets: 40 câu, 4 phần, audio, hai chế độ Luyện tập/Thi, tự lưu, xem lại từng câu"],
    ["IELTS Reading/Writing/Speaking đầy đủ, JLPT chính thức", "Chưa có — không công bố là tính năng"],
], [4, 7], caption="Phạm vi luyện thi")
callout("Bản quyền đề thi.", "Đề Cambridge IELTS dùng để kiểm chứng engine chỉ được lưu cục bộ trên máy phát triển (thư mục LocalContent được gitignore), không có trong repository, không nằm trong bản build. Báo cáo không trích nội dung câu hỏi của đề này.")
fig_pair(("img/exam_centre.jpg", "Trung tâm luyện thi trong lớp học"), ("img/exam_started.jpg", "Làm bài JLPT N5 rút gọn"))
para("Bộ chấm IELTS xử lý đúng quy ước của đáp án in: phần trong ngoặc là tuỳ chọn, giới hạn số từ được kiểm tra, câu “theo thứ tự bất kỳ” chấm theo cặp, band chỉ là ước tính theo bảng quy đổi công bố. Năm bài EditMode với dữ liệu giả lập kiểm tra các quy tắc này.")
h2("Mini-game Kana Match")
para("Game Center có máy Kana Match: lật thẻ ghép cặp hiragana–romaji, katakana–romaji, kanji N5–cách đọc, từ–hình và Nhật–Việt, có đếm giờ, điểm và vé đổi quà ở quầy.")
fig_pair(("img/gc_board.jpg", "Kana Match: bàn thẻ"), ("img/gc_result.jpg", "Kết quả và vé thưởng"))

# ── Chapter 10
new_chapter("Phân tích UI/UX")
h2("Chính sách bố cục màn hình")
para("Đợt hoàn thiện tháng 10 xuất phát từ ảnh chụp thực tế cho thấy nhiều lớp phủ cùng lúc: bảng nhiệm vụ lớn ở góc trái, thanh nút phím tắt cố định ở đáy, gợi ý “Tiếp hội thoại · R” chồng lên gợi ý tương tác. Giải pháp là một chính sách bố cục áp dụng cho mọi khu vực, trong đó mỗi vùng màn hình chỉ có một chủ.")
figure("diag/d38_hud_layout.png", "Chính sách bố cục HUD")
table(["Mức ưu tiên", "Loại thông tin", "Cách hiển thị"], [
    ["1", "Cửa sổ chặn (xác nhận, bài thi)", "Cửa sổ trung tâm, con trỏ hiện, nhân vật đứng yên"],
    ["2", "Hội thoại đang mở", "Hộp hội thoại; HUD mờ đi"],
    ["3", "Tương tác ngữ cảnh [F]", "Làn gợi ý duy nhất ở giữa đáy"],
    ["4", "Gợi ý phụ (Tiếp hội thoại · R)", "Chỉ hiện khi làn gợi ý trống và không có cửa sổ"],
    ["5", "Thông báo", "HudFeed ở giữa phía trên, tối đa 3 thẻ, tạm ẩn khi có cửa sổ"],
], [1.6, 4, 5], caption="Thứ tự ưu tiên thông tin")
fig_pair(("img/hud_1280.jpg", "HUD ở 1280×720: không chồng lớp"), ("img/restock.jpg", "Thanh tiến trình thao tác trong làn gợi ý"))
h2("Điều khiển chuột hai chế độ")
para("Trước đây con trỏ luôn hiện và phải giữ chuột phải để xoay camera, làm trải nghiệm kém nhập vai. Thành phần CursorDirector nay là nơi duy nhất quyết định trạng thái con trỏ: khi chơi, con trỏ bị khoá và ẩn, rê chuột xoay camera; giữ Ctrl để tạm dùng con trỏ với camera dừng; mở cửa sổ (Tab, N, B, O, hội thoại) thì con trỏ tự hiện; cửa sổ mất focus thì nhả chuột. Kiểu “Click để đi” như The Sims vẫn chọn được trong Cài đặt. Trên WebGL, trình duyệt chỉ cấp khoá chuột sau một lần click; trước đó vẫn kéo chuột phải để xoay.")
figure("diag/d39_cursor_state.png", "Chế độ con trỏ và camera", 14.5)
h2("Cài đặt và khả năng tiếp cận")
para("Cài đặt gồm âm lượng nhạc nền và hiệu ứng, ngôn ngữ giao diện (Việt/Anh/Nhật), độ nhạy chuột, đảo trục Y, kiểu điều khiển và bảng đổi phím đầy đủ (kể cả N, P, Ctrl, E). Mọi cửa sổ có nút × và đóng được bằng Esc theo thứ tự cửa sổ trên cùng trước.")
figure("img/settings.jpg", "Cài đặt: âm thanh, ngôn ngữ, camera và bảng phím", 13)
h2("Đánh giá theo nguyên tắc khả dụng")
table(["Nguyên tắc (Nielsen [16])", "Hiện thực trong NihongoLife"], [
    ["Hiển thị trạng thái hệ thống", "Vòng nhu cầu, chip mục tiêu, thanh tiến trình thao tác, thông báo tiến độ ca làm"],
    ["Người dùng kiểm soát", "× và Esc ở mọi cửa sổ; huỷ ca, dừng thao tác; tạm rời hội thoại và tiếp tục bằng R"],
    ["Phòng lỗi", "Không cho làm hai ca; thiếu tiền/balo đầy báo rõ; trả lời sai không mất tiền"],
    ["Nhận biết hơn nhớ", "Bảng phím trong Cài đặt; gợi ý [F] theo ngữ cảnh; thẻ ô ruộng ghi thời gian theo dụng cụ"],
    ["Tối giản", "Bỏ thanh phím tắt, một mục tiêu trên HUD, chi tiết trong cửa sổ khi người chơi yêu cầu"],
], [3.5, 7], caption="Đối chiếu với nguyên tắc khả dụng")

# ── Chapter 11
new_chapter("Kiến trúc hệ thống và dữ liệu")
h2("Kiến trúc phân lớp")
para("Hệ thống chia năm lớp: Trình bày, Gameplay, Dịch vụ, Dữ liệu – nội dung và Nền tảng. Scene 00_Bootstrap khởi tạo AppRoot; GameServices là service locator cho phép gameplay tìm dịch vụ qua interface (IProgressRepository, SceneFlowController, GameInputService…). Giao tiếp giữa gameplay và tiến trình dùng sự kiện (QuestService.Raise) nên một hệ thống mới không cần biết nhiệm vụ nào đang mở.")
figure("diag/d40_architecture_v2.png", "Kiến trúc phân lớp")
h2("Luồng chuyển khu vực")
para("SceneFlowController quản lý ba thao tác: vào khu vực (EnterZone), chuyển thẳng giữa hai khu vực khi đi tàu (TransferZone) và về thành phố (ExitZone). Trong lúc chuyển, người chơi bị khoá điều khiển, hội thoại kể chuyện được tạm treo và màn hình mờ dần; khu vực mới đặt người chơi tại điểm SceneSpawnPoint tương ứng.")
figure("diag/d23_seq_zone.png", "Sơ đồ tuần tự chuyển khu vực", 15.5)
h2("Mô hình dữ liệu và lưu trữ")
para("Toàn bộ tiến trình người chơi là một đối tượng PlayerProgressDto được tuần tự hoá JSON: chỉ số, ví, balo, nghề nghiệp, trạng thái nhiệm vụ, dữ liệu Đảo Midori (ô ruộng, từ vựng, thành tích), kết quả thi và mức thành thạo. Bản lưu cục bộ dùng LocalProgressRepository; khi đăng nhập, SupabaseProgressRepository đồng bộ trường progress_json.")
figure("diag/d42_erd_v2.png", "Mô hình dữ liệu tiến trình người chơi")
h2("Đồng bộ online/offline")
para("Khi đăng nhập, bản cục bộ và bản cloud được gộp. Kiểm tra trong đợt hoàn thiện phát hiện hàm gộp cũ bỏ sót balo, dữ liệu đảo và nhiệm vụ. Hàm gộp hiện tại: ví và balo cùng lấy theo bản cloud để không nhân đôi tài sản; dữ liệu đảo lấy bản tiến xa hơn; nhiệm vụ gộp theo id, bản đã nhận thưởng nhiều lần hơn được giữ nên phần thưởng đã nhận ở máy này không thể nhận lại ở máy khác. Hai bài EditMode (ProgressMergeTests) kiểm tra các quy tắc này; việc thử với dự án Supabase thật chưa được thực hiện.")
table(["Dịch vụ", "Dùng cho", "Hiện trạng đã kiểm chứng"], [
    ["Supabase Auth + PostgREST", "Đăng nhập, progress_json, bảng xếp hạng, bạn bè, co-op", "Mã nguồn hoàn chỉnh; thiếu cấu hình thì tự chạy offline"],
    ["Supabase Realtime", "Vị trí người chơi khác, chat theo kênh", "Có mã kết nối; chưa thử tải nhiều người"],
    ["Google Gemini", "Chấm phát âm, chấm IELTS Writing/Speaking", "Đã đổi model phù hợp tài khoản hiện tại; khoá đọc từ biến môi trường"],
    ["Agora RTC", "Lớp học video (F8)", "Đã nối SDK; cuộc gọi thật giữa hai máy chưa kiểm thử tự động"],
], [3, 4, 4.5], caption="Hiện trạng dịch vụ trực tuyến")

# ── Chapter 12
new_chapter("Pipeline nội dung, công cụ và quy trình")
h2("Builder thay cho thao tác tay")
para("Quy tắc của repository không cho phép lệnh menu Editor ([MenuItem]) hay bước thiết lập thủ công: mở project và nhấn Play phải dùng ngay trạng thái hoàn chỉnh. Mỗi khu vực có một builder chạy bằng dòng lệnh Unity -batchmode -executeMethod, dựng và lưu scene. Builder mới của đợt này gồm IslandBuilder (toàn bộ Đảo Midori, prefab giai đoạn cây, bộ điều khiển hoạt ảnh con vật) và JobSiteBuilder (các trạm làm việc ở konbini và nhà hàng, gỡ các điểm nghề cũ).")
table(["Builder", "Kết quả"], [
    ["CityTownBuilder", "Thành phố Hibari-chō, konbini, quầy thu ngân, biển chỉ đường"],
    ["StationClerkBuilder · StationTrainBuilder", "Ga tàu, nhân viên Kimura, tàu và toa"],
    ["ClassroomBuilder · HomeBedroomBuilder", "Lớp học với bàn thi; phòng trọ"],
    ["GameCenterBuilder", "Game Center, Kana Match, lối vào trên phố"],
    ["IslandBuilder", "Đảo Midori: địa hình, ruộng, chuồng thú, cửa hàng, ga, cô Hana"],
    ["JobSiteBuilder", "Trạm làm việc konbini và sushi; xoá điểm nghề trả lương một nút"],
], [4.2, 7], caption="Các builder chính")
h2("Pipeline tài nguyên và icon")
para("Codex xây dựng pipeline tạo icon từ mô hình 3D (438 ảnh hợp lệ từ 599 ứng viên, 17 ảnh cần duyệt thêm). Đồ án không gắn hàng loạt: chỉ những icon được xem trực tiếp và khớp đúng vật phẩm mới được đưa vào Resources/Items — 8 icon cho nông sản, dụng cụ và đồ công nghệ, 4 icon nigiri cho thẻ gọi món. Icon túi hạt giống, bình tưới và vé tàu về Hibari được vẽ riêng do không có mô hình tương ứng.")
h2("Phối hợp nhóm")
table(["Thành viên", "Vai trò"], [
    ["Quách Thành Long", "Chủ dự án: định hướng, giao yêu cầu bằng ảnh chụp lỗi, duyệt kết quả, commit"],
    ["Claude (trợ lý AI)", "Gameplay, UI, khu vực, Đảo Midori, việc làm, kiểm thử Play Mode"],
    ["Codex (trợ lý AI)", "Pipeline tài nguyên/icon, kiểm tra độc lập, công cụ dữ liệu đề thi"],
], [3.5, 7.5], caption="Phân công trong nhóm")
figure("diag/d24_dev_workflow.png", "Quy trình một vòng phát triển", 14.5)

# ── Chapter 13
new_chapter("Kiểm thử và đánh giá")
h2("Chiến lược kiểm thử")
para("Kiểm thử Play Mode chạy game thật ở chế độ batch: nạp scene, điều khiển nhân vật bằng đường vào thật của hệ thống (GameInputService, InteractionDetector), bấm nút giao diện, chờ thao tác có thời gian và chụp ảnh. EditMode kiểm tra dữ liệu và logic không cần scene. Một Phase chỉ được ghi nhận khi bài tương ứng đạt.")
figure("diag/d43_tests.png", "Kết quả kiểm thử tự động ngày 10/10/2026")
table(["Bộ kiểm thử", "Nội dung chính", "Kết quả"], [
    ["MidoriIsland_FullAcceptanceFlow", "17 bước: ga → tàu → đảo → mua hạt → xới → gieo → tưới → lớn → thu hoạch → bán → đổi ngôn ngữ → cho thú ăn → rời đảo → quay lại → tiến trình còn nguyên → về Hibari", "Đạt (≈79 s)"],
    ["MidoriIsland_PerformanceAndLayout", "Khởi động thẳng từ scene đảo, đi bộ tới mọi khu, một mặt trời, đo khung hình", "Đạt"],
    ["City_HudCursorJournalKonbiniShiftAndSpend", "HUD ở 4 độ phân giải, con trỏ/Ctrl, O/Esc, Tab, N, ca konbini đầy đủ, trả lời sai, lương một lần, tiêu tiền, lưu/tải", "Đạt"],
    ["Sushi_CancelThenFullServingShift", "Huỷ ca (không lương), ca phục vụ đầy đủ, mang sai bàn bị từ chối", "Đạt"],
    ["Island_FarmShiftHarvestSellLanguageAndReturn", "Ca nông trại, tay không chậm hơn cuốc, bấm liên tục tính một lần, bán, JA/EN, về Hibari", "Đạt"],
    ["ProgressionCatalogTests · ProgressMergeTests", "Kiểm tra dữ liệu tiến trình; gộp bản lưu không mất balo/đảo/nhiệm vụ, không nhận thưởng hai lần", "Đạt (6 bài)"],
    ["Các bộ trước đó", "Thành phố, ga tàu, phòng trọ, lớp học, Game Center, IELTS Listening, hội thoại, Esc", "Đạt"],
], [4.2, 5.4, 1.6], caption="Các bộ kiểm thử tiêu biểu", font_size=10.5)
h2("Lỗi phát hiện và đã sửa trong đợt hoàn thiện")
table(["Lỗi", "Nguyên nhân", "Cách sửa"], [
    ["7 thành phần của Đảo Midori mất khỏi scene", "Nhiều lớp MonoBehaviour chung một file — Unity chỉ tuần tự hoá lớp trùng tên file", "Tách mỗi lớp ra file riêng, dựng lại scene"],
    ["Thẻ ô ruộng hiện trạng thái cũ sau khi tưới", "Ghi nhớ giai đoạn chỉ cập nhật trong Update", "Ghi nhận giai đoạn mỗi lần dựng thẻ"],
    ["Gợi ý “Tiếp hội thoại” chồng gợi ý [F]", "Hai thành phần tự hiển thị độc lập", "Làn gợi ý duy nhất có thứ tự ưu tiên"],
    ["Hàm gộp bản lưu online bỏ balo, đảo, nhiệm vụ", "Tạo đối tượng mới chỉ với một số trường", "Gộp đầy đủ theo quy tắc, thêm bài kiểm thử"],
    ["XP tự cộng vào kiến thức", "AddExp cộng cả Knowledge", "Tách hai chỉ số; cấp độ tính từ tổng XP"],
    ["Nhãn tên Kimura bị chồng hai lần", "Hệ thống nhãn tạo thêm nhãn cho NPC đã có nhãn dựng sẵn", "Dùng lại nhãn có sẵn"],
], [3.6, 3.8, 3.6], caption="Một số lỗi tiêu biểu đã sửa", font_size=10.5)
h2("Hiệu năng")
para("Đo trong Editor ở chế độ batch trên máy phát triển, camera nhìn bao quát Đảo Midori trong 4 giây: thời gian khung trung bình 1,4 ms, phân vị 95% là 3,7 ms, tối đa 9,5 ms, 248 renderer, khoảng 590 nghìn tam giác. Số tam giác tăng đáng kể sau khi thêm mô hình nhân vật cô Hana; với bản WebGL nên thay bằng mô hình nhẹ hơn. Số liệu Editor chỉ mang tính tham khảo, chưa thay thế được đo trên bản build.")
h2("Hạn chế của việc kiểm thử")
bullets(["Chưa chạy thử bản build WebGL và chưa đo hiệu năng trên bản build.",
         "Đồng bộ Supabase, gọi Agora giữa hai máy và micro thật chưa có kiểm thử tự động.",
         "Bài kiểm thử chạy ở chế độ khách: tiến trình giữ trong bộ nhớ; lưu xuống đĩa khi đăng nhập được kiểm tra bằng EditMode, chưa bằng Play Mode.",
         "Một số ảnh chụp trong cửa hàng tiện lợi có góc camera từ phía sau do vị trí camera của bài kiểm thử, không phản ánh góc nhìn khi chơi."])

# ── Chapter 14
new_chapter("Kết luận và hướng phát triển")
h2("Kết quả đạt được")
para("NihongoLife đã trở thành một trò chơi mô phỏng cuộc sống có thể chơi trọn vòng: người học đến khu phố, giao tiếp bằng tiếng Nhật trong các tình huống thường ngày, làm thêm để kiếm tiền, chi tiêu cho nhu cầu và đi lại, trồng trọt và chăn nuôi trên Đảo Midori, theo dõi tiến bộ qua cấp độ, kiến thức và sổ nhiệm vụ. Kiến trúc hướng dữ liệu cho phép mở rộng nhiệm vụ, phần thưởng và giá trị cân bằng mà không sửa mã nguồn; mọi tính năng trong báo cáo có kiểm thử tự động đi kèm.")
h2("Đóng góp")
bullets(["Một mô hình game giáo dục gắn từng hoạt động đời sống với mục tiêu giao tiếp N5, phản hồi có giải thích và không thưởng cho câu trả lời sai.",
         "Hệ thống việc làm thêm với thao tác thật thay cho trả lương theo nút bấm, cùng cơ chế chống trùng phần thưởng.",
         "Chính sách bố cục HUD và điều khiển chuột rút ra từ phân tích The Sims 4 và Sons of the Forest, được kiểm chứng ở bốn độ phân giải.",
         "Quy trình phát triển có kiểm chứng: builder thay thao tác tay, kiểm thử Play Mode điều khiển nhân vật thật, ảnh chụp minh chứng."])
h2("Hạn chế")
bullets(["Nội dung mới dừng ở N5; số kịch bản và loại công việc còn ít.",
         "Chưa có giọng đọc tiếng Nhật thật cho từ vựng trên đảo và câu hỏi của khách.",
         "Mục Thời trang trống vì chưa có mô hình quần áo; đồ công nghệ mới là đồ sưu tầm.",
         "Dịch vụ online và bản WebGL chưa được kiểm thử đầy đủ."])
h2("Hướng phát triển")
bullets(["Thêm kịch bản N4, công việc mới (ga tàu, lớp học) và chuỗi nhiệm vụ cốt truyện gắn với Đảo Midori.",
         "Thu âm hoặc cấp phép giọng đọc thật cho từ vựng; luyện phát âm cho câu trong ca làm.",
         "Trang trí phòng trọ bằng đồ công nghệ và thời trang đã mua.",
         "Kiểm thử bản WebGL, đồng bộ Supabase thật và đo hiệu năng trên máy cấu hình thấp."])

# ── References
h1("TÀI LIỆU THAM KHẢO", numbered=False)
refs = [
    "Krashen, S. D. (1982). Principles and Practice in Second Language Acquisition. Oxford: Pergamon Press.",
    "Lave, J., & Wenger, E. (1991). Situated Learning: Legitimate Peripheral Participation. Cambridge University Press.",
    "Gee, J. P. (2003). What Video Games Have to Teach Us About Learning and Literacy. New York: Palgrave Macmillan.",
    "Hattie, J., & Timperley, H. (2007). The Power of Feedback. Review of Educational Research, 77(1), 81–112.",
    "Sweller, J. (1988). Cognitive Load During Problem Solving: Effects on Learning. Cognitive Science, 12(2), 257–285.",
    "Deterding, S., Dixon, D., Khaled, R., & Nacke, L. (2011). From Game Design Elements to Gamefulness: Defining “Gamification”. Proceedings of MindTrek ’11, 9–15.",
    "Csikszentmihalyi, M. (1990). Flow: The Psychology of Optimal Experience. New York: Harper & Row.",
    "Schell, J. (2019). The Art of Game Design: A Book of Lenses (3rd ed.). CRC Press.",
    "Maxis / Electronic Arts. (2014). The Sims 4 [Trò chơi điện tử, PC].",
    "Endnight Games / Newnight. (2024). Sons of the Forest (phiên bản 1.0) [Trò chơi điện tử, PC].",
    "ConcernedApe. (2016). Stardew Valley [Trò chơi điện tử, PC].",
    "Nielsen, J. (1994). 10 Usability Heuristics for User Interface Design. Nielsen Norman Group.",
    "The Japan Foundation & Japan Educational Exchanges and Services. Japanese-Language Proficiency Test — Test Sections and Test Time. https://www.jlpt.jp",
    "British Council, IDP: IELTS Australia & Cambridge University Press & Assessment. IELTS Test Format. https://www.ielts.org",
    "Unity Technologies. (2026). Unity 6 Manual — Universal Render Pipeline, Input System, Test Framework. https://docs.unity3d.com",
    "Supabase. (2026). Supabase Documentation — Auth, Database, Realtime. https://supabase.com/docs",
    "Gamma, E., Helm, R., Johnson, R., & Vlissides, J. (1994). Design Patterns: Elements of Reusable Object-Oriented Software. Addison-Wesley.",
    "Tài liệu nội bộ dự án: Docs/STORY_BIBLE.md, Docs/TEAM_TASKS.md, Docs/PROGRESSION_GUIDE.md, Docs/AssetPipeline/Preview/STAGE_C0_HANDOFF.md (truy cập 10/10/2026).",
]
for i, r in enumerate(refs, 1):
    p = para(f"[{i}] {r}", indent=False); p.paragraph_format.left_indent = Cm(0.9); p.paragraph_format.first_line_indent = Cm(-0.9)
    p.alignment = WD_ALIGN_PARAGRAPH.LEFT

# ── Appendices
h1("PHỤ LỤC A. HƯỚNG DẪN CHỈNH DỮ LIỆU TIẾN TRÌNH", numbered=False, letter="A")
para("Tệp duy nhất cần chỉnh: Assets/NihongoLife/Resources/Progression/progression.json. Sau khi chỉnh, chạy bài EditMode ProgressionCatalogTests để kiểm tra dữ liệu.", align=WD_ALIGN_PARAGRAPH.LEFT)
table(["Muốn làm gì", "Chỉnh ở đâu"], [
    ["Đổi lương / XP / kiến thức của một việc", "Trường rewards của nhiệm vụ: { \"yen\", \"xp\", \"knowledge\" }"],
    ["Đổi ngưỡng lên cấp", "levels.xpToNext — phần tử thứ i là XP cần để lên từ cấp i+1"],
    ["Thêm nhiệm vụ mới", "Thêm một phần tử vào quests với id, category, objectives và rewards"],
    ["Cho làm lại sau một thời gian", "repeatable: true và cooldownMinutes"],
    ["Phát đồ khi nhận việc", "grantOnAccept: [{ \"itemId\", \"quantity\" }]"],
    ["Mục tiêu “báo cáo với chủ”", "objective event \"talk\", target là npcId, afterAll: true"],
], [4, 7], caption=None)
para("Các sự kiện được hỗ trợ: till, plant, water, harvest, feed, pet, buy, sell, talk, learn, restock, assist, checkout, order, serve. Hướng dẫn đầy đủ nằm trong Docs/PROGRESSION_GUIDE.md.")

h1("PHỤ LỤC B. THƯ VIỆN ẢNH TRÒ CHƠI", numbered=False, letter="B")
para("Ảnh được chụp tự động bởi các bài kiểm thử Play Mode (1600×900 hoặc 1920×1080).")
for a, b in [(("img/menu.jpg", "Menu chính"), ("img/guide.jpg", "Hướng dẫn cách chơi")),
             (("img/ticket_machine.jpg", "Máy bán vé với ga Midori"), ("img/arrived_school.jpg", "Đến trường Hibari bằng tàu")),
             (("img/classroom_desk.jpg", "Bàn thi trong lớp học"), ("img/exam_result.jpg", "Kết quả và lời nhận xét của giáo viên")),
             (("img/gc_entrance.jpg", "Lối vào Game Center"), ("img/gc_match.jpg", "Kana Match: ghép cặp")),
             (("img/isl_fashion.jpg", "Mục Thời trang để trống có giải thích"), ("img/isl_sell.jpg", "Bán nông sản")),
             (("img/isl_onboard.jpg", "Lên tàu đi Đảo Midori"), ("img/isl_back.jpg", "Trở về ga Hibari"))]:
    fig_pair(a, b)

doc.save(OUT)
print("saved", OUT)
