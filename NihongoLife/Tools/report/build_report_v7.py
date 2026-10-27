"""Builds Bao_Cao/NihongoLife_Bao_Cao_Capstone_v7.docx from v6: new sections in the right chapters, v6 styles reused.

Usage:  python Tools/report/build_report_v7.py <date dd/mm/yyyy> <play-mode result e.g. 100/101> <edit-mode result e.g. 123/123>
then    python Tools/report/layout_v7.py <docx> <chapter prefixes that flow on>
then    powershell -File Tools/report/word_update.ps1 -Docx <docx> -Pdf <pdf>   (Word updates TOC / lists, exports PDF)
"""
import copy
import os
import sys

import docx
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn
from docx.shared import Cm, Pt

BAO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..', 'Bao_Cao'))
SRC = os.path.join(BAO, 'NihongoLife_Bao_Cao_Capstone_v6.docx')
OUT = os.path.join(BAO, 'NihongoLife_Bao_Cao_Capstone_v7.docx')
DATE = sys.argv[1] if len(sys.argv) > 1 else '27/10/2026'
PLAY = sys.argv[2] if len(sys.argv) > 2 else None
EDIT = sys.argv[3] if len(sys.argv) > 3 else '123/123'

d = docx.Document(SRC)
body = d.element.body


def P(path):
    return os.path.join(BAO, path)


def heading(prefix):
    for p in d.paragraphs:
        if p.style.name.startswith('Heading') and p.text.strip().startswith(prefix):
            return p
    raise KeyError(prefix)


def para(prefix, style=None):
    for p in d.paragraphs:
        if p.text.strip().startswith(prefix) and (style is None or p.style.name == style):
            return p
    raise KeyError(prefix)


def is_heading_el(el):
    if el.tag != qn('w:p'):
        return False
    st = el.find('.//' + qn('w:pStyle'))
    return st is not None and st.get(qn('w:val')) in ('Heading1', 'Heading2')


def end_of_section(head_par):
    """The element right after the section's last element (the next heading or sectPr)."""
    el = head_par._p.getnext()
    while el is not None and not is_heading_el(el) and el.tag != qn('w:sectPr'):
        el = el.getnext()
    return el


class Cursor:
    """Inserts new elements one after another before a reference element."""

    def __init__(self, before):
        self.before = before

    def put(self, el):
        self.before.addprevious(el)
        return el

    def p(self, text='', style='Normal', align=None, bold_prefix=None):
        par = d.add_paragraph(style=style)
        if bold_prefix:
            par.add_run(bold_prefix).bold = True
        if text:
            par.add_run(text)
        if align is not None:
            par.alignment = align
        self.put(par._p)
        return par

    def h2(self, text):
        return self.p(text, 'Heading 2')

    def bullets(self, items):
        for it in items:
            if isinstance(it, tuple):
                self.p(it[1], 'Bullet', bold_prefix='– ' + it[0] + ': ')
            else:
                self.p('– ' + it, 'Bullet')

    def caption(self, text, style='Caption'):
        c = self.p(text, style)
        if style == 'Caption':
            c.alignment = WD_ALIGN_PARAGRAPH.CENTER
        return c

    def figure(self, image, caption, width_cm=14.5):
        par = d.add_paragraph(style='Front')
        par.alignment = WD_ALIGN_PARAGRAPH.CENTER
        par.paragraph_format.keep_with_next = True
        par.add_run().add_picture(P(image), width=Cm(width_cm))
        self.put(par._p)
        self.caption(caption)

    def figures2(self, items, width_cm=7.9):
        """Two pictures side by side, each with its caption (saves half a page per pair)."""
        t = d.add_table(rows=1, cols=2)
        t.autofit = False
        tblPr = t._tbl.tblPr
        borders = tblPr.find(qn('w:tblBorders'))
        if borders is not None:
            tblPr.remove(borders)
        tblPr.append(tblPr.makeelement(qn('w:jc'), {qn('w:val'): 'center'}))
        for i, (image, caption) in enumerate(items):
            cell = t.rows[0].cells[i]
            cell.width = Cm(8.25)
            p0 = cell.paragraphs[0]
            p0.style = d.styles['Front']
            p0.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p0.paragraph_format.space_after = Pt(2)
            p0.add_run().add_picture(P(image), width=Cm(width_cm))  # the row cannot split: picture and caption stay together
            cap = cell.add_paragraph(caption, style='Caption')
            cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        trPr = t.rows[0]._tr.get_or_add_trPr()
        trPr.append(trPr.makeelement(qn('w:cantSplit'), {}))
        self.put(t._tbl)

    def table(self, caption, headers, rows, widths_cm):
        self.caption(caption, 'TableCaption').paragraph_format.keep_with_next = True
        self.put(make_table(headers, rows, widths_cm))
        spacer = d.add_paragraph(style='Normal')
        spacer.paragraph_format.space_after = Pt(0)
        spacer.paragraph_format.space_before = Pt(0)
        self.put(spacer._p)


# A data table of the report used as the template (header row + one body row).
TEMPLATE = next(t for t in d.tables if t.rows[0].cells[0].text.strip() == 'Mã')


def set_cell_text(cell_el, text):
    ps = cell_el.findall(qn('w:p'))
    for extra in ps[1:]:
        cell_el.remove(extra)
    p = ps[0]
    runs = p.findall(qn('w:r'))
    rpr = copy.deepcopy(runs[0].find(qn('w:rPr'))) if runs and runs[0].find(qn('w:rPr')) is not None else None
    for r in runs:
        p.remove(r)
    for h in p.findall(qn('w:hyperlink')):
        p.remove(h)
    r = p.makeelement(qn('w:r'), {})
    if rpr is not None:
        r.append(rpr)
    t = r.makeelement(qn('w:t'), {})
    t.text = text
    t.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
    r.append(t)
    p.append(r)


def make_table(headers, rows, widths_cm):
    tbl = copy.deepcopy(TEMPLATE._tbl)
    trs = tbl.findall(qn('w:tr'))
    head_tr, body_tr = trs[0], trs[1]
    for tr in trs:
        tbl.remove(tr)
    twips = [int(w / 2.54 * 1440) for w in widths_cm]
    grid = tbl.find(qn('w:tblGrid'))
    for g in list(grid):
        grid.remove(g)
    for w in twips:
        grid.append(grid.makeelement(qn('w:gridCol'), {qn('w:w'): str(w)}))
    tblW = tbl.find(qn('w:tblPr')).find(qn('w:tblW'))
    if tblW is not None:
        tblW.set(qn('w:w'), str(sum(twips)))
        tblW.set(qn('w:type'), 'dxa')

    def row(template, values):
        tr = copy.deepcopy(template)
        tcs = tr.findall(qn('w:tc'))
        for tc in tcs:
            tr.remove(tc)
        for i, v in enumerate(values):
            tc = copy.deepcopy(tcs[min(i, len(tcs) - 1)])
            tcw = tc.find(qn('w:tcPr')).find(qn('w:tcW'))
            if tcw is not None:
                tcw.set(qn('w:w'), str(twips[i]))
                tcw.set(qn('w:type'), 'dxa')
            set_cell_text(tc, v)
            tr.append(tc)
        return tr

    tbl.append(row(head_tr, headers))
    for r in rows:
        tbl.append(row(body_tr, r))
    return tbl


def rewrite(prefix, pairs):
    par = para(prefix)
    text = par.text
    for old, new in pairs:
        assert old in text, (prefix, old)
        text = text.replace(old, new)
    runs = par.runs
    runs[0].text = text
    for r in runs[1:]:
        r.text = ''


LEFT = WD_ALIGN_PARAGRAPH.LEFT

# ─────────── 0. Cover, thanks, summary ───────────
cover = para('TP. Hồ Chí Minh, tháng 10 năm 2026 · Bản 6', 'Front')
for r in cover.runs:
    r.text = ''
cover.runs[0].text = 'TP. Hồ Chí Minh, tháng 10 năm 2026 · Bản 7 (cập nhật %s)' % DATE
rewrite('TP. Hồ Chí Minh, ngày 23 tháng 10 năm 2026', [('ngày 23 tháng 10', 'ngày %s tháng 10' % DATE.split('/')[0])])
rewrite('Sản phẩm được xây dựng trên Unity 6000.3', [
    ('với 9 scene trong bản phát hành', 'với 10 scene chơi'),
    ('và sáu khu vực nạp chồng (ga, nhà hàng sushi, lớp học, phòng trọ, Game Center, Đảo Midori)',
     'và bảy khu vực nạp chồng (ga, nhà hàng sushi, lớp học, phòng trọ, Game Center, Đảo Midori, phòng khám Hibari) cùng bản dựng đầu của Đảo Phú Quốc')])
summary3 = para('Quá trình phát triển chia thành 7 Phase').text
rewrite('Quá trình phát triển chia thành 7 Phase', [(summary3[summary3.find('Kiểm thử:'):], 'Kiểm thử bản v7: EditMode %s bài, Play Mode %s bài chạy.' % (EDIT, PLAY or '—'))])
c = Cursor(para('Quá trình phát triển chia thành 7 Phase')._p.getnext())
c.p('Bản v7 (%s) thêm nghề nghiệp từ cấp 10 (năm nghề, sáu bậc, chín cột mốc; nghề Y tế theo lộ trình đào tạo thật ở Nhật), tám bệnh có nguyên nhân và cách chữa đúng thực hành y khoa, ngoại hình thay đổi theo tuổi, phòng khám có người dân đến khám, sổ tay năm chương, năm bản nhạc gốc tạo bằng code và phần mở rộng Đảo Phú Quốc (bản đồ từ toạ độ OpenStreetMap thật, nghề Nhà hàng & dịch vụ tại nhà hàng Gánh Ngon).' % DATE)

# ─────────── 1. Chapter 5: clinic, Phú Quốc ───────────
c = Cursor(end_of_section(heading('5.4.')))
c.h2('5.5. Phòng khám Hibari và người dân đi khám (bản v7)')
c.p('Phòng khám Hibari (ひばりクリニック, scene 35_HibariClinic, nội khoa – nhi khoa) được dựng lại bằng ClinicBuilder.BuildInterior: lối đi màu bạc hà từ cửa tới quầy tiếp nhận, thảm khu chờ, tay vịn gỗ, góc trẻ em (キッズコーナー), bể cá, tranh treo và đèn ấm. Lệnh này chỉ dựng lại scene phòng khám, không chạm tới thị trấn.')
c.p('ClinicPatientFlow cho người dân tự đến khám theo đúng quy trình một phòng khám Nhật: vào cửa → 受付 (đưa thẻ bảo hiểm, nói triệu chứng) → ngồi ghế chờ → bảng gọi số và lời gọi 「N番の方、診察室へどうぞ」 → 診察室 gặp bác sĩ Mori → お薬 → ra về. Tám người dân (có ông bà, trẻ em) nằm trong clinic.json; người lớn tuổi có tóc bạc, râu và đi chậm. Mỗi lúc chỉ một người trong phòng khám và người dân không có collider nên không chắn đường người chơi.')
c.figures2([('clinic/10_townspeople_waiting.png', 'Hình 5.7. Phòng chờ: người dân ngồi chờ, bảng gọi số'),
            ('clinic/12_consultation.png', 'Hình 5.8. Bệnh nhân vào phòng khám, bác sĩ hỏi bệnh')])
c.h2('5.6. Phần mở rộng Đảo Phú Quốc (bản dựng đầu, v7)')
c.p('Đảo Phú Quốc (đặc khu Phú Quốc, tỉnh An Giang) là phần mở rộng đầu tiên lấy bối cảnh Việt Nam: đảo lớn nhất nước, khoảng 575 km², dài ~50 km Bắc – Nam; trung tâm là Dương Đông ở bờ Tây, An Thới ở mũi Nam, dãy "99 ngọn" với Núi Chúa 603 m ở phía Bắc. Dữ liệu bản đồ (Resources/PhuQuoc/phu_quoc_map.json) gồm 31 địa điểm thật — chợ đêm Dinh Cậu, nhà thùng nước mắm, vườn tiêu, sân bay PQC, Bãi Trường, Hàm Ninh, Bãi Sao, Bãi Khem, di tích Nhà tù Phú Quốc, cáp treo Hòn Thơm, Bãi Dài, Gành Dầu, Vườn quốc gia… — toạ độ tra từ OpenStreetMap (giấy phép ODbL); không dùng Google Maps và không dùng tên, logo thương hiệu khi chưa được phép.')
c.p('Toàn đảo không thể dựng tỉ lệ 1:1 (đi bộ mất hàng giờ, số thực mất độ chính xác ở xa gốc toạ độ, WebGL không tải nổi), nên bản đồ dùng tỉ lệ 1:100 theo phương ngang và 1:50 theo phương đứng. Mọi điểm được chiếu từ toạ độ thật quanh nhà hàng Gánh Ngon (PhuQuocMap.ToWorld) nên vị trí tương đối giữ đúng địa lý — bài kiểm thử khẳng định An Thới ở phía Nam Dương Đông, Hàm Ninh ở bờ Đông, Gành Dầu ở Tây Bắc. Scene 65_PhuQuocIsland do PhuQuocIslandBuilder dựng từ dữ liệu với tài nguyên CC0 (Kenney, ambientCG, Poly Haven). Đây là bản dựng đầu: đường bờ biển vẽ tay theo bản đồ (sai số ~1 km); khu phố Dương Đông và nội thất nhà hàng sẽ được dựng chi tiết ở tỉ lệ 1:1 cục bộ trong đợt sau.')
c.figure('phuquoc/01_island_overview_crop.png', 'Hình 5.9. Đảo Phú Quốc (bản dựng đầu): đường bờ biển thật, dãy núi phía Bắc, 31 điểm mốc', 7.6)

# ─────────── 2. Chapter 6: look by age; health ───────────
c = Cursor(end_of_section(heading('6.5.')))
c.h2('6.6. Ngoại hình theo tuổi (bản v7)')
c.p('Tuổi của người chơi tăng mỗi sinh nhật trong game; CharacterLook cho ngoại hình đổi theo tuổi trên cả tám model Kids:')
c.bullets([('Tóc', 'mười một màu, đổi trên GPU bằng shader Hidden/NihongoLife/HairRecolor với mặt nạ vùng tóc mà CharacterLookBuilder nướng sẵn cho từng model; tự bạc dần từ 45 tuổi.'),
           ('Râu', 'lún phún, ria mép, râu dê, quai nón, râu dài; mở theo tuổi (18–40) và chỉ có ở model nam. Râu là các khối gắn vào xương đầu nên theo mọi animation.'),
           ('Nếp nhăn', 'trán, rãnh mũi – má từ 50 tuổi; vết chân chim từ 65 tuổi.'),
           ('Nhịp cơ thể', 'từ 55 tuổi đi chậm dần (tới ×0,72) và nói chậm dần (tốc độ chữ hội thoại tới ×0,6); NPC có thể đặt nhịp riêng.')])
c.p('Bảng ngoại hình mở bằng cách chạm chân dung trên HUD hoặc nút "Ngoại hình" ở màn chọn nhân vật; mọi thay đổi hiện ngay trên nhân vật. Hành khách lớn tuổi trên tàu và người dân ở phòng khám dùng cùng hệ thống.')
c.figures2([('character-look/03_kids_80_grey_long.png', 'Hình 6.9. Tám model ở 80 tuổi: tóc bạc, râu dài, nếp nhăn'),
            ('character-look/06_panel_30_beard.png', 'Hình 6.10. Bảng ngoại hình: màu tóc, râu mở theo tuổi')])
c.h2('6.7. Sức khoẻ và bệnh tật như ngoài đời (bản v7)')
c.p('Hệ thống bệnh (Health/PlayerIllness, dữ liệu Resources/Health/illnesses.json) biến sức khoẻ thành một phần của đời sống: người chơi mắc bệnh do chính những gì mình làm, cảm nhận bệnh qua cơ thể và chữa theo đúng thực hành y khoa. Mỗi bệnh nhân hệ số lên tốc độ đi, hồi thể lực, buồn ngủ, khát, mất máu và hiệu suất làm việc; màn hình có viền màu theo bệnh, nhân vật hắt hơi 「ハクション！」 và HUD hiện tên bệnh.')
c.table('Bảng 6.1. Tám bệnh trong game và cách chữa đúng',
        ['Bệnh', 'Nguyên nhân trong game', 'Cách chữa (như ngoài đời)'],
        [['Cảm lạnh 風邪', 'Ướt mưa lâu', 'Nghỉ ngơi, giữ ấm; thuốc cảm chỉ làm dịu; kháng sinh không có tác dụng'],
         ['Cúm インフルエンザ', 'Nơi đông người mùa thu – đông; máu thấp dễ mắc gấp đôi', 'Khám sớm: thuốc kháng virus hiệu quả nhất trong 48 giờ đầu; ở nhà; tiêm phòng để phòng'],
         ['Đau mỏi cơ 筋肉痛', 'Hai ca làm/ngày hoặc chạy hơn 3 phút', 'Nghỉ, vận động nhẹ, miếng dán; ngủ một giấc'],
         ['Kiệt sức 過労', 'Ba ca làm/ngày', 'Ngủ đủ, có ngày nghỉ; không thuốc nào thay được'],
         ['Say nắng 熱中症', 'Nắng trưa khi thiếu nước', 'Vào chỗ mát, làm mát, bù nước điện giải; phòng khám truyền dịch'],
         ['Đầy bụng 胃もたれ', 'Ăn khi đã no', 'Ngừng ăn, uống nước ấm; thuốc dạ dày làm dịu'],
         ['Dị ứng phấn hoa 花粉症', 'Ngoài trời mùa xuân, không khẩu trang', 'Thuốc kháng dị ứng (có thể gây buồn ngủ); đeo khẩu trang'],
         ['Đau đầu 頭痛', 'Buồn ngủ đầy quá lâu', 'Ngủ; thuốc giảm đau chỉ tạm thời']],
        [4.3, 4.7, 7.4])
c.p('Nguyên tắc điều trị được giữ đúng thực tế: thuốc không kê đơn ở kệ くすり của Hibari Mart chỉ giảm triệu chứng và rút ngắn bệnh một phần; bác sĩ điều trị theo bệnh (cúm được rút ngắn 80% nếu khám trong 48 giờ đầu, 30% nếu muộn; say nắng truyền dịch là khỏi); giấc ngủ chữa cảm, đau mỏi, kiệt sức và đau đầu. Đi làm khi đang bị cúm bị trừ danh tiếng vì ở Nhật người bệnh được yêu cầu ở nhà (học sinh nghỉ đến khi đủ 5 ngày từ lúc phát bệnh và 2 ngày sau khi hết sốt). Tiêm phòng cúm ở phòng khám cho miễn dịch khoảng 30 ngày trong game; khẩu trang bảo vệ một ngày.')
c.figures2([('health/01_flu_fever.png', 'Hình 6.11. Bị cúm: thông báo, chip HUD và viền sốt'),
            ('health/04_konbini_medicine.png', 'Hình 6.12. Kệ くすり ở Hibari Mart: bảy món không kê đơn')])

# ─────────── 3. Chapter 8: careers ───────────
c = Cursor(end_of_section(heading('8.8.')))
c.h2('8.9. Nghề nghiệp từ cấp 10 và tuyến phát triển (bản v7)')
c.p('Từ cấp 10 người chơi chọn một trong năm nghề — Bán lẻ, Ẩm thực, Nông nghiệp, Nhà hàng & dịch vụ (Phú Quốc), Y tế — theo mô hình nghề của The Sims 3/4: sáu bậc, lịch làm theo ngày trong tuần, thanh hiệu suất, phỏng vấn thăng chức bằng tiếng Nhật và rẽ nhánh ở bậc 4. Công việc thật vẫn là ca làm thêm sẵn có; nghề cộng thêm lương, chức danh và sự kiện ở nơi làm. Cửa sổ nghề (CareerUI) có sáu thẻ: Tổng quan, Lộ trình (sơ đồ cây có lương và yêu cầu từng bậc), Lịch làm (tuần hiện tại, ngày đã làm và vắng), Hồ sơ (lương đã nhận, lịch sử thăng chức, nghề cũ), Cột mốc và Các nghề (lưới ba cột, đổi nghề có xác nhận).')
c.p('Tuyến phát triển gồm chín cột mốc trong careers.json (初出勤, はじめての昇進, 皆勤の一週間, がんばり屋, 臨機応変, 自分の道, たよれる人, 多才, キャリアの頂点), mỗi cột mốc thưởng yen và trí tuệ một lần và được giữ khi đổi nghề. Thêm nghề, bậc, phỏng vấn hay cột mốc chỉ cần sửa JSON; CareerCatalogTests kiểm tra dữ liệu.', align=LEFT)
c.figures2([('career/07_overview.png', 'Hình 8.13. Thẻ Tổng quan: hiệu suất, mục tiêu tiếp theo'),
            ('career/08_path.png', 'Hình 8.14. Thẻ Lộ trình: cây sáu bậc, hai nhánh')])
c.h2('8.10. Nghề Y tế theo lộ trình đào tạo thật ở Nhật')
c.p('Ở Nhật không thể từ điều dưỡng lên thẳng bác sĩ; nghề Y tế vì vậy đi đúng các bước đào tạo thật (thời gian được rút gọn), và mỗi lần thăng chức là một câu hỏi y khoa thật bằng tiếng Nhật — ví dụ bệnh nhân bị cảm xin kháng sinh, câu trả lời đúng là 「かぜは ウイルスなので、こうせいぶっしつは ききません」.')
c.table('Bảng 8.3. Lộ trình nghề Y tế: ngoài đời và trong game',
        ['Bậc', 'Ngoài đời thật', 'Trong game'],
        [['1–2', 'Làm thêm ở cơ sở y tế', '医療事務 (lễ tân y vụ) → 看護助手 (trợ lý điều dưỡng)'],
         ['3', 'Trường y hoặc trường dược 6 năm', '医療系の学生 — sinh viên y/dược vừa học vừa làm'],
         ['4', 'Thi quốc gia: 医師国家試験 / 薬剤師国家試験', 'Chọn nhánh: 研修医 (bác sĩ nội trú) hoặc 薬剤師'],
         ['5', 'Nội trú ≥ 2 năm → đào tạo chuyên khoa ~3 năm', '専攻医 / 管理薬剤師'],
         ['6', 'Bác sĩ chuyên khoa, mở hoặc điều hành phòng khám', '専門医・院長 / 薬局長']],
        [1.3, 7.0, 8.1])
c.h2('8.11. Nghề Nhà hàng & dịch vụ tại Gánh Ngon (Phú Quốc)')
c.p('Nghề thứ năm (hospitality) làm việc ở nhà hàng Gánh Ngon, 113 Trần Hưng Đạo, Dương Đông — thông tin do chủ nhà hàng cung cấp: hải sản nướng BBQ, bò nướng, lẩu gà sầu riêng, sầu riêng chiên, cơm niêu buổi trưa 10:00–13:00, quầy pub/cocktail, chín phòng VIP, mở cửa 10:00–23:00. Nhà hàng đón du khách nhiều nước nên ca làm luyện cả tiếng Nhật lẫn tiếng Anh: khách Nhật được trả lời bằng tiếng Nhật, khách nói tiếng Anh được trả lời bằng tiếng Anh.')
c.p('Dữ liệu (Resources/PhuQuoc/ganh_ngon.json) gồm chín món có tên Việt – Anh – Nhật, 18 câu giao tiếp phục vụ (いらっしゃいませ, ご注文は…, お会計…) và sáu đoạn hội thoại theo vị trí: phục vụ bàn (cặp du khách Nhật, cơm niêu buổi trưa), lễ tân (đặt phòng VIP bằng tiếng Anh, đặt bàn bằng tiếng Nhật), bếp và quầy pub. Mỗi vị trí (GanhNgonSpot: lễ tân, bốn bàn, quầy bếp, quầy pub, quản lý ca chị Mai) mở một đoạn hội thoại; trả lời đúng ghi nhận sự kiện order/serve cho ca job_ganhngon_shift và thanh hiệu suất nghề. Không có giá món (không bịa giá), không có món từ động vật hoang dã; tên nhân viên là nhân vật hư cấu.')
c.table('Bảng 8.4. Nấc thang nghề Nhà hàng & dịch vụ',
        ['Bậc', 'Nhánh Bếp', 'Nhánh Quản lý'],
        [['1–3', 'Phục vụ tập sự 見習いホール → Phục vụ / lễ tân ホール・受付 → Trưởng ca シフトリーダー (chung)', ''],
         ['4', 'Phụ bếp 調理補助', 'Phó quản lý 副支配人'],
         ['5', 'Đầu bếp chính 調理師', 'Quản lý nhà hàng 支配人'],
         ['6', 'Bếp trưởng 料理長', 'Tổng quản lý 総支配人']],
        [1.3, 8.1, 7.0])

# ─────────── 4. Chapter 10: handbook ───────────
c = Cursor(end_of_section(heading('10.7.')))
c.h2('10.8. Sổ tay (Handbook) dạng sách (bản v7)')
c.p('Cẩm nang địa điểm được nâng thành cuốn sổ tay (HandbookUI, phím Y, nút "Sổ tay" trên HUD): một cuốn sách mở với bìa vải xanh, hai trang giấy kem, gáy khâu chỉ vàng và ruy-băng đánh dấu năm chương — 案内 Giới thiệu (game, điều khiển, cơ thể, học tập, việc làm, online, mẹo sống ở Nhật), 場所 Địa điểm (ảnh và câu hữu ích của từng nơi; nút cẩm nang trên bản đồ mở thẳng chương này), 健康 Sức khoẻ (tình trạng hiện tại, tám bệnh với nguyên nhân – biểu hiện – cách chữa – phòng ngừa, tủ thuốc, con đường trở thành bác sĩ), 仕事 Nghề nghiệp và 音楽 Âm nhạc (nghe thử, ghim một bài phát mọi nơi). Trang trái là mục lục của chương, trang phải là nội dung; toàn bộ nội dung đến từ JSON nên thêm trang không cần sửa giao diện.')
c.figures2([('handbook/01_intro.png', 'Hình 10.10. Sổ tay: chương Giới thiệu'),
            ('handbook/04_heatstroke.png', 'Hình 10.11. Chương Sức khoẻ: say nắng và cách sơ cứu')])
c.figures2([('handbook/05_doctor_path.png', 'Hình 10.12. Con đường trở thành bác sĩ ở Nhật'),
            ('handbook/08_music.png', 'Hình 10.13. Chương Âm nhạc: nghe thử và ghim bài')])

# ─────────── 5. Chapter 12: music ───────────
c = Cursor(end_of_section(heading('12.3.')))
c.h2('12.4. Âm nhạc gốc tạo bằng code (bản v7)')
c.p('Năm bản nhạc mới được soạn và tổng hợp hoàn toàn bằng code (Tools/audio/compose_music.py: numpy, scipy, ffmpeg), không dùng mẫu âm thanh của bên thứ ba. Mỗi bản là một vòng lặp liền mạch: các nốt được tổng hợp một lần rồi đặt vào ba bản sao, giữ bản giữa để điểm lặp mang đúng phần ngân của ô nhịp trước. Soundscape chọn bài theo hoàn cảnh, và Resources/Audio/music.json liệt kê toàn bộ 15 bài cho chương Âm nhạc của sổ tay.')
c.table('Bảng 12.3. Năm bản nhạc gốc của bản v7',
        ['Bài', 'Nhạc cụ, điệu thức', 'Phát khi'],
        [['クリニック · Phòng chờ yên tĩnh', 'Marimba, pad; ngũ cung Fa trưởng, 76 bpm', 'Ở phòng khám'],
         ['あめの ひ · Ngày mưa', 'Electric piano lo-fi, chổi trống, 68 bpm', 'Ngoài phố khi mưa'],
         ['おかゆ · Mau khoẻ nhé', 'Hộp nhạc ru ngủ nhịp 3/4, 60 bpm', 'Ở nhà khi đang ốm'],
         ['さくらみち · Đường hoa anh đào', 'Koto, sáo; âm giai Yo, 92 bpm', 'Sáng mùa xuân ngoài phố'],
         ['いそがしい シフト · Ca làm bận rộn', 'Shamisen, taiko; âm giai In, 112 bpm', 'Giờ làm ở nơi làm của nghề']],
        [5.4, 6.3, 4.7])

# ─────────── 6. Chapter 13: tests ───────────
c = Cursor(end_of_section(heading('13.7.')))
c.h2('13.8. Kiểm thử đợt v7 (%s)' % DATE)
c.p('Đợt v7 thêm 9 bài kiểm thử mới và mở rộng các bài sẵn có. Bộ EditMode đạt %s bài; bộ Play Mode (Unity 6000.3.12f1, batchmode) đạt %s bài chạy, các bài còn lại được bỏ qua có chủ đích (Explicit hoặc cần thiết bị thật). Bài duy nhất chưa đạt là chấm IELTS bằng AI thật, phụ thuộc mạng và khoá dịch vụ. Các bài lỗi trong lần chạy đầu do ổ đĩa đầy hoặc mạng Realtime chập chờn đều đạt khi chạy lại riêng.' % (EDIT, PLAY or '—'))
c.table('Bảng 13.4. Bài kiểm thử mới của bản v7',
        ['Bài kiểm thử', 'Kiểm tra gì'],
        [['IllnessCatalogTests (EditMode)', 'Tám bệnh hợp lệ, khớp triệu chứng phòng khám; cách chữa đúng thực tế (kháng sinh, 48 giờ, truyền dịch); thuốc có trên kệ konbini; danh sách nhạc trỏ đúng tệp'],
         ['IllnessPlayModeTests', 'Mắc cảm khi ướt, thuốc chỉ giảm triệu chứng, ngủ khỏi; cúm làm chậm và giảm hiệu suất, khám sớm rút ngắn; ca làm gây đau mỏi/kiệt sức; tiêm phòng; sổ tay đủ năm chương; nghe thử và ghim nhạc'],
         ['CharacterLookPlayModeTests', 'Tóc đổi màu trên tám model, râu chỉ ở model nam và mở theo tuổi, nếp nhăn, đi/nói chậm theo tuổi; bảng ngoại hình trên người chơi'],
         ['ClinicPlayModeTests (mở rộng)', 'Người dân vào chờ, được gọi số, gặp bác sĩ, nhận thuốc rồi về; không chắn người chơi'],
         ['CareerPlayModeTests (mở rộng)', 'Sáu thẻ của cửa sổ nghề, cột mốc được thưởng, đổi nghề có xác nhận'],
         ['CareerCatalogTests (mở rộng)', 'Năm nghề; cột mốc hợp lệ; dữ liệu sai bị từ chối'],
         ['PhuQuocRestaurantTests (EditMode, 7 bài)', 'Dữ liệu Gánh Ngon và bản đồ hợp lệ; vị trí tương đối đúng địa lý; không giá bịa, không món hoang dã'],
         ['PhuQuocIslandPlayModeTests', 'Scene 65 có đủ 31 điểm mốc, đảo có collider, tám điểm phục vụ của Gánh Ngon']],
        [5.6, 10.8])

# ─────────── 7. Chapter 14: YouTube ───────────
c = Cursor(end_of_section(heading('14.3.')))
c.p('Kênh YouTube @NihongoLifeGame (https://www.youtube.com/@NihongoLifeGame) là kênh chính thức của dự án; mười video của kênh được xem ngay trong game qua trình phát video tích hợp (Resources/Video/nihongolife_videos.json).', bold_prefix='Kênh YouTube. ', align=LEFT)
c.table('Bảng 14.3. Video trên kênh YouTube @NihongoLifeGame',
        ['Video', 'Loại', 'Thời lượng'],
        [['Thi thử IELTS trong game: Writing 8.5 – Speaking 9.0', 'Video', '4:45'],
         ['Thi thử JLPT N5 trong game đạt 180/180 điểm — full bài', 'Video', '3:18'],
         ['Một ngày ở phố Hibari: Konbini, tàu điện, đảo Midori', 'Video', '2:12'],
         ['Mở khóa bản đồ Hibari từng nơi một', 'Video', '0:57'],
         ['8 nhân vật NihongoLife – bạn chọn ai?', 'Shorts', '0:47'],
         ['Làm thêm ở Konbini Nhật trong game! コンビニのアルバイト', 'Shorts', '0:33'],
         ['Đi tàu điện ra đảo Midori 電車に乗ります', 'Shorts', '0:37'],
         ['Làm nông = học tiếng Nhật! みずを あげます', 'Shorts', '0:37'],
         ['Game Center học Hiragana! かなマッチ Kana Match', 'Shorts', '0:29'],
         ['Câu cá ở đảo Midori – học từ まぐろ', 'Shorts', '0:25']],
        [11.4, 2.4, 2.6])

# ─────────── 8. Chapter 15 ───────────
c = Cursor(para('NihongoLife đã trở thành một trò chơi mô phỏng cuộc sống có thể chơi trọn vòng')._p.getnext())
c.p('Bản v7 mở rộng vòng chơi sang giai đoạn trưởng thành: người chơi chọn nghề từ cấp 10 và thăng tiến qua phỏng vấn tiếng Nhật, già đi theo năm tháng với ngoại hình và nhịp cơ thể thay đổi, mắc bệnh và chữa bệnh đúng như ngoài đời, đọc mọi hướng dẫn trong một cuốn sổ tay, nghe nhạc gốc của riêng game và bước đầu đặt chân tới Đảo Phú Quốc.')
c = Cursor(end_of_section(heading('15.2.')))
c.bullets(['Mảng y tế có giá trị giáo dục thật: tám bệnh với nguyên nhân, hiệu ứng và cách chữa đối chiếu nguồn y tế công khai; nghề Y tế đi đúng lộ trình đào tạo bác sĩ, dược sĩ ở Nhật.',
           'Hệ thống nghề nghiệp kiểu The Sims hướng dữ liệu (năm nghề, sáu bậc, chín cột mốc) và cuốn sổ tay năm chương lấy nội dung hoàn toàn từ JSON.',
           'Bản đồ Phú Quốc dựng từ toạ độ OpenStreetMap thật, giữ đúng vị trí tương đối giữa 31 địa điểm.'])
c = Cursor(end_of_section(heading('15.4.')))
c.bullets(['Hoàn thiện Phú Quốc: khu Dương Đông tỉ lệ 1:1 (Trần Hưng Đạo, chợ đêm, Dinh Cậu), nội thất Gánh Ngon, khách NPC đến ăn theo mô hình phòng khám, đường đi từ Hibari (sân bay PQC) và ảnh chụp kiểm chứng.',
           'Thêm bệnh theo mùa và tình huống cấp cứu (gọi 119) cho mảng y tế.'])

# ─────────── 9. References ───────────
c = Cursor(para('[19] Tài liệu nội bộ dự án')._p.getnext())
for ref in [
        '[20] AMR Clinical Reference Center, National Center for Global Health and Medicine (2023). かぜ（感冒）の説明 — 抗微生物薬適正使用の手引き. https://amr.ncgm.go.jp',
        '[21] 厚生労働省 (Ministry of Health, Labour and Welfare). 熱中症が疑われる人を見かけたら. https://www.mhlw.go.jp',
        '[22] 学校保健安全法施行規則 第19条 — インフルエンザの出席停止期間 (tóm tắt của Trung tâm y tế Đại học Oita). https://www.hc.oita-u.ac.jp',
        '[23] 内閣官房 (Cabinet Secretariat) (2023). 花粉症対策 — 有病率（2019年全国調査）. https://www.cas.go.jp',
        '[24] 厚生労働省. 新たな専門医の仕組み — 専門研修と19基本領域. https://www.mhlw.go.jp',
        '[25] Wikipedia. Phú Quốc. https://en.wikipedia.org/wiki/Phú_Quốc (truy cập %s).' % DATE,
        '[26] OpenStreetMap contributors. Nominatim — toạ độ các địa điểm ở Phú Quốc, giấy phép ODbL. https://nominatim.openstreetmap.org',
        '[27] Cổng thông tin điện tử đặc khu Phú Quốc. https://phuquoc.angiang.gov.vn',
        '[28] Kenney (kenney.nl), ambientCG, Poly Haven — tài nguyên 3D, vân bề mặt và bầu trời giấy phép CC0 (Docs/PHU_QUOC_ASSETS.md).',
        '[29] Tài liệu nội bộ dự án: Docs/HEALTH_SOURCES.md, Docs/AUDIO_CREDITS.md, Docs/PHU_QUOC.md, Resources/Health/illnesses.json, Resources/Progression/careers.json (truy cập %s).' % DATE]:
    c.p(ref)

# ─────────── 10. Appendix B: two more pairs after the table that holds Hình B.12 ───────────
cap_el = next(p for p in body.iter(qn('w:p')) if ''.join(t.text or '' for t in p.iter(qn('w:t'))).startswith('Hình B.12.'))
holder = cap_el
while holder.getparent() is not body:
    holder = holder.getparent()
c = Cursor(holder.getnext())
c.figures2([('career/11_milestones.png', 'Hình B.13. Cột mốc nghề nghiệp'),
            ('handbook/07_medical_career.png', 'Hình B.14. Sổ tay: nghề Y tế')])
c.figures2([('character-look/04_kids_30_blonde_goatee.png', 'Hình B.15. 30 tuổi: tóc vàng, râu dê'),
            ('clinic/11_number_called.png', 'Hình B.16. Bảng gọi số ở phòng khám')])

# The YouTube table sits in 14.3, before v6's tables 14.3 and 14.4: renumber those (captions and mentions).
for t_el in body.iter(qn('w:t')):
    if t_el.text and 'YouTube @NihongoLifeGame' not in t_el.text:
        t_el.text = t_el.text.replace('Bảng 14.4', 'Bảng 14.§5').replace('Bảng 14.3', 'Bảng 14.§4').replace('§', '')

# Lists of figures / tables a touch tighter so each ends on a full page.
d.styles['toc 1'].paragraph_format.space_after = Pt(2)

d.core_properties.title = 'NihongoLife — Báo cáo đồ án Capstone (Bản 7)'
d.save(OUT)
print('saved', OUT)
