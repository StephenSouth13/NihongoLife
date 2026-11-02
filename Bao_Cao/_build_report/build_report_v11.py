"""Bao_Cao v10 -> v11: the report rewritten after the owner's review of 02/11/2026.
Removes the blank lines, brings every figure and number up to date and adds the new sections, each with pictures from
the game. The format stays: Times New Roman 13/18, line spacing 1.5, Word fields for the contents and lists.
Run: python build_report_v11.py <v10.docx> <out.docx>  then the Word step fills the fields and exports the PDF.
"""
import copy
import glob
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

import docx
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Emu, Pt
from docx.table import Table
from docx.text.paragraph import Paragraph
from PIL import Image

SRC, OUT = sys.argv[1], sys.argv[2]
BAO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
RES = os.path.abspath(os.path.join(BAO, '..', 'NihongoLife', 'Assets', 'NihongoLife', 'Resources'))
LOGS = r'D:\VTC_Academy\NihongoLife\_work\logs'
TMP = os.path.join(BAO, '_build_report', 'img_v11')
os.makedirs(TMP, exist_ok=True)
TEXT_WIDTH_CM = 16.0

doc = docx.Document(SRC)
body = doc.element.body


def W(tag):
    return qn('w:' + tag)


def blocks():
    return list(body.iterchildren())


def paragraphs():
    return [Paragraph(e, doc) for e in blocks() if e.tag == W('p')]


def find(prefix, style=None):
    for p in paragraphs():
        if p.text.strip().startswith(prefix) and (style is None or p.style.name == style):
            return p
    raise KeyError(prefix)


def chapter_end(number):
    """The element before which a chapter's new sections go: the next chapter's title."""
    return find(f'CHƯƠNG {number + 1}.', 'Heading 1')._p


# ─────────── cleaning ───────────

def set_text(p, text):
    runs = p.runs
    runs[0].text = text
    for r in runs[1:]:
        r.text = ''


def replace_everywhere(old, new):
    all_paragraphs = paragraphs() + [c for t in doc.tables for row in t.rows for cell in row.cells for c in cell.paragraphs]
    n = 0
    for p in all_paragraphs:
        if old in p.text and p.runs:  # the contents is a field: Word writes it again
            for r in p.runs:
                if old in r.text:
                    r.text = r.text.replace(old, new)
                    n += 1
            if old in p.text:
                set_text(p, p.text.replace(old, new))
                n += 1
    return n


def cell_set(table, row_label, text):
    for row in table.rows:
        if row.cells[0].text.strip() == row_label:
            set_text(row.cells[1].paragraphs[0], text)
            return
    raise KeyError(row_label)


def drop_blank_lines():
    """Empty paragraphs were used as spacers: spacing now belongs to the paragraphs themselves."""
    removed = 0
    items = blocks()
    for i, e in enumerate(items):
        if e.tag != W('p'):
            continue
        p = Paragraph(e, doc)
        if p.text.strip() or e.xpath('.//w:drawing') or e.xpath('.//w:sectPr') or e.xpath('.//w:br') or e.xpath('.//w:fldChar'):
            continue
        following = items[i + 1] if i + 1 < len(items) else None
        previous = items[i - 1] if i else None
        if following is None or following.tag == W('sectPr'):
            continue
        if following.tag == W('tbl') and previous is not None and previous.tag == W('tbl'):
            continue  # Word joins two tables that touch
        body.remove(e)
        removed += 1
    for i, e in enumerate(blocks()):
        if e.tag == W('tbl') and i + 1 < len(blocks()) and blocks()[i + 1].tag == W('p'):
            Paragraph(blocks()[i + 1], doc).paragraph_format.space_before = Pt(8)
    return removed


# ─────────── new content ───────────

def counter(kind, chapter):
    found = 0
    for p in paragraphs():
        m = re.match(rf'{kind} {chapter}\.(\d+)\.', p.text.strip())
        if m:
            found = max(found, int(m.group(1)))
    return found + 1


class Section:
    """Writes new blocks just before an element."""

    def __init__(self, chapter):
        self.chapter = chapter
        self.before = chapter_end(chapter)
        self.figure = counter('Hình', chapter)
        self.table = counter('Bảng', chapter)

    def _para(self, style):
        p = doc.add_paragraph(style=style)
        self.before.addprevious(p._p)
        return p

    def h2(self, text):
        return self._para('Heading 2').add_run(text)

    def h3(self, text):
        return self._para('Heading 3').add_run(text)

    def p(self, text):
        para = self._para('Normal')
        para.add_run(text)
        para.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        return para

    def bullets(self, items):
        for item in items:
            self._para('List Bullet').add_run(item)

    def picture(self, path, cols):
        """A copy sized for the page: 16:9 pictures stay wide, nothing taller than a third of a page."""
        im = Image.open(path).convert('RGB')
        if os.sep + 'body' + os.sep in path or '/body/' in path:
            w, h = im.size
            im = im.crop((int(w * 0.14), int(h * 0.08), int(w * 0.86), int(h * 0.8)))
        name = os.path.join(TMP, re.sub(r'\W+', '_', os.path.relpath(path, BAO)) + '.jpg')
        if im.width > 1280:
            im = im.resize((1280, round(im.height * 1280 / im.width)), Image.LANCZOS)
        im.save(name, quality=86)
        return name, im.width / im.height

    def figures(self, items, caption, cols=2):
        """items: (path, label). One picture stands alone; several sit in a borderless grid with (a), (b) labels."""
        if len(items) == 1:
            path, aspect = self.picture(items[0][0], 1)
            width = min(14.0, 8.6 * aspect)
            para = self._para('Normal')
            para.alignment = WD_ALIGN_PARAGRAPH.CENTER
            para.paragraph_format.keep_with_next = True
            para.add_run().add_picture(path, width=Cm(width))
        else:
            rows = (len(items) + cols - 1) // cols
            cell_width = (TEXT_WIDTH_CM - 0.3 * cols) / cols
            table = doc.add_table(rows=rows * 2, cols=cols)
            table.alignment = 1
            self.before.addprevious(table._tbl)
            for i, (path, label) in enumerate(items):
                r, c = i // cols, i % cols
                top = table.cell(r * 2, c).paragraphs[0]
                top.alignment = WD_ALIGN_PARAGRAPH.CENTER
                top.paragraph_format.keep_with_next = True
                top.paragraph_format.line_spacing = 1.0
                file, aspect = self.picture(path, cols)
                top.add_run().add_picture(file, width=Cm(min(cell_width, 6.2 * aspect)))
                under = table.cell(r * 2 + 1, c).paragraphs[0]
                under.alignment = WD_ALIGN_PARAGRAPH.CENTER
                under.paragraph_format.keep_with_next = True
                under.paragraph_format.line_spacing = 1.0
                under.add_run(f'({chr(97 + i)}) {label}')
        cap = self._para('ChuThichHinh')
        cap.add_run(f'Hình {self.chapter}.{self.figure}. ').bold = True
        cap.add_run(caption)
        self.figure += 1

    def grid(self, caption, header, rows, widths):
        cap = self._para('ChuThichBang')
        cap.add_run(f'Bảng {self.chapter}.{self.table}. ').bold = True
        cap.add_run(caption)
        cap.paragraph_format.keep_with_next = True
        self.table += 1
        model = next(t for t in doc.tables if len(t.columns) == 3 and t.rows[0].cells[0].text.startswith('Công việc'))
        table = doc.add_table(rows=1 + len(rows), cols=len(header))
        self.before.addprevious(table._tbl)
        props = copy.deepcopy(model._tbl.tblPr)
        table._tbl.replace(table._tbl.tblPr, props)
        total = sum(widths)
        grid = table._tbl.tblGrid
        for col, w in zip(grid.findall(W('gridCol')), widths):
            col.set(W('w'), str(round(9072 * w / total)))
        for r, values in enumerate([header] + rows):
            row = table.rows[r]
            row_pr = row._tr.get_or_add_trPr()
            cant = OxmlElement('w:cantSplit')
            row_pr.append(cant)
            if r == 0:
                row_pr.append(OxmlElement('w:tblHeader'))
            for c, value in enumerate(values):
                cell = row.cells[c]
                cell.width = Emu(round(9072 * widths[c] / total * 635))
                para = cell.paragraphs[0]
                para.paragraph_format.line_spacing = 1.15
                para.paragraph_format.space_after = Pt(2)
                run = para.add_run(str(value))
                run.bold = r == 0
        after = self._para('Normal')
        after.paragraph_format.space_after = Pt(0)
        after.paragraph_format.line_spacing = 0.5
        return table


# ─────────── facts from the project ───────────

def results(tag):
    path = os.path.join(LOGS, f'tests_{tag}.xml')
    if not os.path.exists(path):
        return None
    root = ET.parse(path).getroot()
    return {k: int(root.attrib.get(k, 0)) for k in ('total', 'passed', 'failed', 'skipped', 'inconclusive')}


def load(path):
    return json.load(open(os.path.join(RES, path), encoding='utf-8'))


EDIT, PLAY = results(os.environ.get('NL_EDIT_TAG', 'final_edit')), results(os.environ.get('NL_PLAY_TAG', 'final_play'))
catalog = load('Fashion/outfits.json')
illness = load('Health/illnesses.json')['illnesses']
fish = load('Island/island_catalog.json')['fish']
courses = load('Video/video_courses.json')['courses']
business = load('Progression/business.json')
progression = load('Progression/progression.json')
wage = {q['id']: q['rewards']['yen'] for q in progression['quests'] if q['id'].startswith('job_')}


def S(path):
    return os.path.join(BAO, path)


# ─────────── the existing text, brought up to date ───────────

print('blank lines removed', drop_blank_lines())


def merge_field_ends():
    """A contents list ends in an empty paragraph holding the field's end: on a full page it alone made a blank page."""
    merged = 0
    items = blocks()
    for i, e in enumerate(items):
        if e.tag == W('p') and not Paragraph(e, doc).text.strip() and e.xpath('.//w:fldChar[@w:fldCharType="end"]') and items[i + 1].tag == W('p'):
            title = items[i + 1]
            at = 1 if title.pPr is not None else 0
            for run in e.xpath('./w:r'):
                for mark in run.xpath('./w:lastRenderedPageBreak'):
                    run.remove(mark)
                title.insert(at, run)
                at += 1
            body.remove(e)
            merged += 1
    return merged


print('field ends merged', merge_field_ends())
for section in doc.sections:
    for part in (section.header, section.footer, section.first_page_header):
        for p in part.paragraphs:
            for r in p.runs:
                if 'Capstone v10' in r.text:  # setting .text wipes a run's field marks, and the page number with them
                    r.text = r.text.replace('Capstone v10', 'Capstone v11')
for old, new in [('9 nghề nghiệp', '10 nghề nghiệp'), ('chị Ito', 'anh Ito'), ('Chị Ito', 'Anh Ito'), ('Nhấn E ở bàn', 'Nhấn F ở bàn'), ('9 tuyến nghề', '10 tuyến nghề'), ('Chín tuyến nghề', 'Mười tuyến nghề'), ('chín tuyến nghề', 'mười tuyến nghề'),
                 ('một trong 9 nghề', 'một trong 10 nghề'), ('9 nghề, ca làm thật', '10 nghề, ca làm thật'), ('9 nghề, 6 cấp', '10 nghề, 6 cấp'), ('(yên, XP, kiến thức)', '(yên, XP, trí tuệ)'), ('kiến thức là chỉ số riêng', 'trí tuệ là chỉ số riêng'), ('cấp và kiến thức tối thiểu', 'cấp và trí tuệ tối thiểu'), ('cổng kiến thức', 'cổng trí tuệ'), ('tiền, kiến thức, ca làm', 'tiền, trí tuệ, ca làm'), ('Game Center 14 máy', 'Game Center 14 máy, trong đó có ném bóng rổ và máy gắp thú điều khiển tay')]:
    print(old, replace_everywhere(old, new))

numbers = next(t for t in doc.tables if len(t.columns) == 2 and 'Hiện trạng trong repo' in t.rows[0].cells[1].text)
set_text(numbers.rows[0].cells[1].paragraphs[0], 'Hiện trạng trong repo (03/11/2026)')
cell_set(numbers, 'Cốt truyện', '9 chương chính (N5 → N1), 3 kết thúc ở chương 9; 10 tuyến nghề × 5 bậc truyện')
cell_set(numbers, 'Thế giới', 'Hibari-chō, tuyến tàu 5 ga, đảo Midori có biển bơi được, cửa hàng thời trang Hibari Mode, đảo Phú Quốc (OSM, 1 đơn vị = 100 m, 31 điểm xe tour)')
cell_set(numbers, 'Nhân vật', f'8 nhân vật Kids chơi được, {len(catalog["outfits"])} bộ trang phục hoàn chỉnh; NPC cùng họ Kids mặc đồng phục theo nghề')
total_lessons = sum(len(s['lessons']) for c in courses for s in c['sections'])
cell_set(numbers, 'Học tập', f'40 bài học × 4 ngôn ngữ, 20 bộ luyện chứng chỉ, từ điển 2.234 mục; English Foundations 20 unit; lộ trình tiếng Nga 6 chặng / 53 bước; thư viện {total_lessons} bài video theo khoá')
if EDIT and PLAY:
    cell_set(numbers, 'Chất lượng', f'{EDIT["passed"]}/{EDIT["total"]} EditMode và {PLAY["passed"]}/{PLAY["total"]} Play Mode test đạt (03/11/2026); 16 lần kiểm tra bố cục điện thoại, 0 lỗi')

set_text(find('Thanh 10 lối tắt gập vào ngăn kéo'), 'Mười lối tắt nằm trong một khay icon mở từ một nút duy nhất, trên cả máy tính và điện thoại (mục 10.5); thẻ trạng thái thu còn 80%; cảnh báo không đè lên joystick.')
set_text(find('Một số test phụ thuộc thứ tự chạy'), 'Một số test phụ thuộc mạng (chấm bài bằng AI thật) có thể thất bại khi mất kết nối; hiện tượng nhấp nháy khi vào game đã được đo lại ở mục 12.4.')
set_text(find('Cuộc gọi voice/video giữa hai người thật'), 'Luồng cuộc gọi đã kiểm trên máy chủ thật bằng tài khoản thử (mục 9.5), nhưng âm thanh và hình ảnh giữa hai thiết bị thật chưa có kiểm thử tự động.')
set_text(find('Voice/video hai người thật và voice trên web'), 'Âm thanh và hình ảnh giữa hai thiết bị thật, voice trên web chưa có kiểm thử tự động; bản iOS chưa có; bản Android chưa phát hành.')
set_text(find('Một số tính năng còn ở mức lõi'), 'Một số tính năng còn ở mức lõi: máy gắp thú là mô phỏng 2D, trận câu cá mập chưa có thao tác riêng, thuê nhân công chưa có hình 3D, trang phục chỉ đổi màu và phụ kiện chứ chưa đổi dáng áo, xe máy/ô tô của người chơi chưa làm.')

# ─────────── Chapter 4 ───────────

s = Section(4)
s.h2('4.7. Cửa hàng thời trang Hibari Mode (56_FashionStore)')
s.p('Hibari Mode là khu mới cạnh tiệm nhạc Hibiki, cửa vào nằm ngay trên phố chính. Bên trong có bảy giá treo theo nhóm trang phục, ba bàn gấp đồ, các ma-nơ-canh mặc đúng những bộ đang bán, ba phòng thử đồ, gương lớn, quầy thu ngân và anh Ren, người quản lý. Quần áo treo trên giá lấy màu trực tiếp từ dữ liệu outfits.json, nên thứ người chơi nhìn thấy trên giá chính là thứ họ mua được.')
s.p('Khu này được dựng bằng FashionStoreBuilder, đăng ký đầy đủ như mọi khu khác: danh mục địa điểm, gói tải riêng trên web (nlzone-fashion), điểm xuất hiện, cổng ra phố, bản đồ tầng và ảnh màn tải.')
s.figures([(S('fashion/01_entrance.jpg'), 'Nhìn từ cửa vào'), (S('fashion/02_floor_west.jpg'), 'Giá treo và ma-nơ-canh'), (S('fashion/03_floor_east.jpg'), 'Khu trang phục theo mùa'), (S('fashion/04_fitting_till.jpg'), 'Phòng thử đồ và quầy')], 'Không gian cửa hàng Hibari Mode, ảnh chụp trong game')

# ─────────── Chapter 5 ───────────

s = Section(5)
s.h2('5.7. Bơi và biển')
s.p('Trước đây người chơi đi xuống nước là rơi khỏi bản đồ. Biển và hồ bơi nay là bề mặt nước thật (WaterSurface): sâu quá thắt lưng thì nhân vật nổi lên, thân nghiêng theo tư thế bơi sải, chạy thành bơi nhanh, đứng yên thì đạp nước. Chạm bờ nhân vật tự leo lên. Ra quá 45 m có lời cảnh báo, quá 60 m dòng nước đưa người chơi quay lại gần bờ, nên không ai bị lạc ngoài khơi.')
s.figures([(S('swim/midori_1_tread.png'), 'Đạp nước tại chỗ'), (S('swim/midori_2_stroke.png'), 'Bơi sải vào bờ')], 'Bơi ở đảo Midori')
s.h2('5.8. Câu cá nhiều loài theo kỹ năng')
s.p(f'Danh sách cá tăng lên {len(fish)} loài, dùng gói mô hình 52 loài có sẵn của dự án. Mỗi loài có bậc kỹ năng: người mới chỉ gặp cá thường, câu nhiều mới gặp cá hiếm, cao nhất là cá mập và cá mập yêu tinh. Tỉ lệ xuất hiện đọc từ island_catalog.json nên có thể chỉnh mà không sửa mã. Trận kéo cá hiện dùng chung một thao tác cho mọi loài.')
s.grid('Số loài cá theo bậc kỹ năng (nguồn: island_catalog.json)', ['Bậc kỹ năng', 'Số loài', 'Ví dụ'], [
    [f'{lv}', sum(1 for f in fish if f.get('skill', 0) == lv), ', '.join(f['id'].replace('fish_', '') for f in fish if f.get('skill', 0) == lv)] for lv in sorted({f.get('skill', 0) for f in fish})], [2, 1.2, 8])
s.h2('5.9. Game Center: ném bóng rổ và máy gắp thú')
s.p('Rà soát Game Center cho thấy nhiều máy trùng trò và máy bỏ không. Mỗi máy nay gắn một trò riêng; các máy còn lại có người đang chơi và biển 使用中, nên không còn máy chết. Hai trò được làm lại theo hướng mô phỏng: ném bóng rổ dùng cú vuốt có lực và quỹ đạo vật lý, bóng phải qua vành mới tính điểm; máy gắp thú để người chơi tự lái càng bằng phím hoặc giữ nút, độ chặt của càng phụ thuộc độ lệch so với thú, thú có thể tuột giữa đường.')
s.figures([(S('arcade/hoop_shot_vi.jpg'), 'Ném bóng rổ'), (S('arcade/ufo_catcher_vi.jpg'), 'Máy gắp thú')], 'Hai trò mô phỏng ở Game Center')
s.h2('5.10. Bệnh hiện ra trên cơ thể nhân vật')
s.p(f'Mỗi bệnh trong {len(illness)} bệnh của sổ tay nay có một cử chỉ riêng lặp lại khi nhân vật đang rảnh: hắt hơi, ho, ôm trán khi sốt, quạt tay khi say nắng, ôm đầu, ôm bụng, xoa chỗ đau. Cử chỉ đi kèm bong bóng lời nói tiếng Nhật và viền màn hình theo màu bệnh. Cử chỉ không bao giờ chen ngang lúc đang ăn, đang bơi hay đang làm việc.')
order = sorted(illness, key=lambda i: i['id'])
s.figures([(S(f'health/body/{i["id"]}.jpg'), i['vi'].split(' (')[0]) for i in order[:12]], 'Nhân vật khi mắc bệnh, ảnh chụp trực tiếp trong phòng khám của game', cols=4)
s.figures([(S(f'health/body/{i["id"]}.jpg'), i['vi'].split(' (')[0]) for i in order[12:]], 'Nhân vật khi mắc bệnh (tiếp theo)', cols=4)

# ─────────── Chapter 6 ───────────

s = Section(6)
s.h2('6.6. Ngành Thời trang')
s.p('Thời trang là ngành nghề thứ sáu, gồm một nghề có sáu bậc và hai nhánh từ bậc 4: Stylist và Nhà thiết kế. Công việc là ca thật ở Hibari Mode: gấp gọn ba bàn quần áo, tư vấn cỡ và màu cho hai khách, chăm sóc khách ở phòng thử đồ, tính tiền hai món rồi báo cáo với anh Ren. Mọi câu hỏi đều bằng tiếng Nhật, trả lời sai được giải thích và làm lại. Nghề có đủ bảy buổi phỏng vấn thăng chức, ba sự kiện nơi làm việc và tuyến truyện năm bậc như các nghề khác.')
career = next(c for c in load('Progression/careers.json')['careers'] if c['id'] == 'fashion')
ladder = [[l['titleVi'], l['titleJa'], f'¥{l["salary"]}'] for l in career['levels']]
for branch in career['branches']:
    ladder += [[f'{branch["nameVi"]}: {l["titleVi"]}', l['titleJa'], f'¥{l["salary"]}'] for l in branch['levels']]
s.grid('Thang bậc ngành Thời trang (nguồn: careers.json)', ['Chức danh', 'Tiếng Nhật', 'Lương mỗi ca'], ladder, [6, 4, 2])
s.figures([(S('fashion/20_quiz_FashionAdvise.jpg'), 'Tư vấn cỡ áo'), (S('fashion/20_quiz_FashionRegister.jpg'), 'Tính tiền ở quầy')], 'Ca làm ở Hibari Mode bằng tiếng Nhật')
s.h2('6.7. Mua sắm và mặc đồ hoàn chỉnh')
s.p(f'Cửa hàng bán {len(catalog["outfits"])} bộ trong {len(catalog["categories"])} nhóm, từ ¥{min(o["price"] for o in catalog["outfits"]):,} đến ¥{max(o["price"] for o in catalog["outfits"]):,}. Chọn một bộ là nhân vật mặc thử ngay, camera vòng ra trước mặt nhân vật; đóng cửa sổ mà chưa mua thì đồ tự trả về. Bộ đã mua lưu theo tài khoản, thay được ở mọi nơi từ bảng ngoại hình, và người chơi khác nhìn thấy khi online.')
s.p('Về kỹ thuật, CharacterVariantBuilder đo từng khối màu quần áo trên atlas của tám nhân vật. Một bộ đổi toàn bộ các khối đó, không chỉ màu chính; phụ kiện (mũ, kính, khăn, cà vạt, ba lô) là các khối hình gắn vào xương đầu và xương ngực, nên đi theo mọi hoạt ảnh. Mũ neo theo đỉnh đầu đo được của từng mô hình để vừa với mọi kiểu tóc.')
s.figures([(p, l) for p, l in [(S('fashion/10_kid_haruto_casual_mint.jpg'), 'Haruto · bạc hà'), (S('fashion/10_kid_hina_summer_beach.jpg'), 'Hina · đi biển'), (S('fashion/10_kid_mio_street_bomber.jpg'), 'Mio · bomber đỏ'), (S('fashion/10_kid_riku_winter_camel.jpg'), 'Riku · mùa đông'),
           (S('fashion/10_kid_sakura_sport_tennis.jpg'), 'Sakura · tennis'), (S('fashion/10_kid_sota_office_navy.jpg'), 'Sota · vest'), (S('fashion/10_kid_yui_street_neon.jpg'), 'Yui · neon'), (S('fashion/10_kid_yuto_casual_knit.jpg'), 'Yuto · áo len')]], 'Tám nhân vật trong tám bộ trang phục', cols=4)
s.figures([(S('fashion/05_try_on.jpg'), 'Thử đồ')], 'Cửa sổ thử đồ: nhân vật mặc ngay bộ đang chọn')
s.h2('6.8. Kinh tế sau đợt cân bằng lại')
s.p('Đánh giá thực tế cho thấy tiền tiêu thì dễ mà kiếm thì khó. Thưởng các ca làm được nhân 2,5 lần, nhiệm vụ nông trại, phòng khám, học tập và hằng ngày cũng tăng; mọi nhiệm vụ đều trả tiền và kinh nghiệm rõ ràng trên thông báo. Giá trị nằm trong progression.json, kiểm thử đối chiếu đúng số tiền nhận được.')
s.grid('Tiền công một ca sau khi cân bằng (nguồn: progression.json)', ['Ca làm', 'Trước', 'Hiện tại'], [
    ['Hibari Mart', '¥600', f'¥{wage["job_konbini_shift"]:,}'], ['Sushi Hibari', '¥700', f'¥{wage["job_sushi_shift"]:,}'], ['Nông trại Midori', '¥500', f'¥{wage["job_farm_shift"]:,}'],
    ['Phòng khám Hibari', '¥750', f'¥{wage["job_clinic_shift"]:,}'], ['Hibari Mode', 'chưa có', f'¥{wage["job_fashion_shift"]:,}']], [5, 2, 2])
s.p(f'Công ty của người chơi được tính theo tuần game. Mỗi loại hình có tiền thuê, điện nước và vật tư riêng, trừ sau mỗi {business["weekDays"]} ngày game cùng lương nhân viên. Nhân viên làm ra doanh thu theo ngày; phần lợi nhuận nằm trong công ty cho đến khi chủ rút về ví. Thiếu tiền trả chi phí thì danh tiếng giảm, không làm ví âm.')
s.grid('Chi phí cố định mỗi tuần theo loại hình (nguồn: business.json)', ['Loại hình', 'Thuê', 'Điện nước', 'Vật tư', 'Hệ số doanh thu'], [
    [k['nameVi'], f'¥{k["weeklyRent"]:,}', f'¥{k["weeklyUtilities"]:,}', f'¥{k["weeklySupplies"]:,}', f'×{k["revenueFactor"]}'] for k in business['kinds']], [4, 2, 2, 2, 2.4])
s.p('Người chơi chọn một ngành chính. Làm đúng ngành được trọn lương và kinh nghiệm; làm việc thuộc ngành khác chỉ nhận 50% cả hai, thông báo ghi rõ "trái ngành −50%". Quy tắc này khiến việc chọn nghề là một quyết định thật, đúng tinh thần định hướng nghề nghiệp của đồ án.')

# ─────────── Chapter 8 ───────────

s = Section(8)
s.h2('8.13. Thư viện khoá học video')
s.p(f'Trình phát video cũ được thay bằng thư viện theo mô hình các nền tảng học trực tuyến. Công cụ build_courses.py đọc danh sách phát công khai của kênh @NihongoLifeGame và chia thành {len(courses)} khoá, {total_lessons} bài, tách theo chương và theo ngôn ngữ. Trang khoá học có giáo trình, thời lượng, tiến độ từng bài và nút học tiếp; trên bản web, video YouTube phát ngay trong khung bài học.')
s.grid('Các khoá trong thư viện (nguồn: video_courses.json)', ['Khoá', 'Số chương', 'Số bài', 'Thời lượng'], [
    [c['titleVi'], len(c['sections']), sum(len(x['lessons']) for x in c['sections']), f'{sum(l["seconds"] for x in c["sections"] for l in x["lessons"]) / 3600:.1f} giờ'.replace('.', ',')] for c in courses], [6, 1.6, 1.4, 2])
s.figures([(S('video-courses/01_catalogue.png'), 'Danh mục khoá học'), (S('video-courses/02_course_page.png'), 'Trang khoá tiếng Nga')], 'Thư viện khoá học video trong game')

# ─────────── Chapter 9 ───────────

s = Section(9)
s.h2('9.5. Biểu cảm, trang phục và kết quả thử trên máy chủ thật')
s.p('Biểu cảm trước đây tạo mới bong bóng chữ mỗi lần bấm nên gây giật. Bong bóng nay được dùng lại, mỗi lần gọi chỉ mất 5 đến 6 ms; biểu cảm gửi qua kênh của khu vực nên người chơi khác thấy ngay trên nhân vật của mình. Trang phục đang mặc đi kèm trạng thái hiện diện. Khi máy chủ từ chối một cập nhật hiện diện, client chờ 15 giây rồi gửi lại, thay vì mất trạng thái.')
s.p('Công cụ online_test.py đăng nhập sáu tài khoản thử nghiệm vào máy chủ thật và chạy trọn các luồng. Tin nhắn cộng đồng, nhóm và riêng được kiểm thêm bằng Social_CommunityGroupPrivateE2E trong Play Mode.')
s.grid('Kết quả thử trực tiếp trên Supabase, 02/11/2026 (nguồn: Tools/online_playtest)', ['Nhóm kiểm tra', 'Kết quả'], [
    ['Đăng nhập 6 tài khoản, khách ẩn danh bị chặn', 'Đạt'], ['5 client cùng phòng: presence, chat, vị trí, rời phòng', 'Đạt'], ['Gọi giáo viên: đổ chuông, nhận, cấp token, kết thúc, từ chối người ngoài', '24/24'],
    ['Thông báo và từ chối yêu cầu', '11/11'], ['Biểu cảm và trang phục của người khác hiển thị', 'Đạt (OnlineSocialPlayModeTests)']], [8, 3])

# ─────────── Chapter 10 ───────────

s = Section(10)
s.h2('10.5. Dock phím tắt, đóng mở HUD và chọn chế độ chơi')
s.p('Toàn bộ lối tắt nay nằm trong một khay mở từ một nút icon, giống nhau trên máy tính và điện thoại: mỗi ô có icon, tên theo ngôn ngữ đang dùng và phím tương ứng trên máy tính. Ba nút tròn đóng mở riêng thẻ nhân vật, thanh nhiệm vụ và gợi ý nút F; lựa chọn được ghi nhớ trên thiết bị. Khi cần màn hình rộng, người chơi cất cả ba chỉ trong một lần chạm.')
s.p('Nút "Nút cảm ứng: Tự động" khó hiểu được đổi thành "Chế độ chơi: Điện thoại" hoặc "Máy tính", kèm icon tương ứng; chế độ tự nhận ghi rõ thiết bị nó tìm thấy. Người chơi có thể chơi kiểu điện thoại trên máy tính cảm ứng, hoặc dùng bàn phím trên máy bảng.')
s.h2('10.6. Trang web và nhận diện')
s.p('Bản web có logo thật ở màn tải và menu chính, favicon và ảnh chia sẻ dựng từ logo của game, chữ màn tải theo ngôn ngữ trình duyệt. Nút Fullscreen bằng chữ che cảnh được thay bằng icon nhỏ ở góc, có nhãn theo ngôn ngữ. Dòng thông tin học viên ở menu có nền tối để đọc được trên ảnh thị trấn.')
web = [(S('web-release/p_pc_1_menu.png'), 'Menu chính có logo'), (S('web-release/p_pc_6_town.png'), 'Trong thị trấn'), (S('web-release/p_phone_1_menu.png'), 'Menu trên điện thoại'), (S('web-release/p_phone_6_town.png'), 'HUD trên điện thoại')]
web = [w for w in web if os.path.exists(w[0])]
if web:
    s.figures(web, 'Bản web sau đợt nâng cấp, ảnh chụp từ trình duyệt')

# ─────────── Chapter 13 ───────────

s = Section(13)
s.h2('13.6. Video demo đầy đủ chức năng')
s.p('Bản demo dài 7 phút 40 giây đi qua toàn bộ chức năng chính trong một lượt, ghi trực tiếp từ game bằng DemoRecordingTests.Tour_RecordFrames: mọi thao tác chạy qua logic thật của trò chơi, không dựng cảnh riêng. Lời dẫn tiếng Việt do giọng đọc tổng hợp thần kinh của Microsoft đọc, kịch bản viết theo văn nói; nhạc nền là nhạc gốc của từng khu; phụ đề gắn sẵn. Tệp video: Bao_Cao/NihongoLife_Demo_Day_Du.mp4.')
s.grid('Các chương trong video demo', ['Chương', 'Thời điểm'], [['Menu, ngôn ngữ, chế độ chơi', '00:11'], ['Thị trấn, phím tắt, HUD, bản đồ', '00:38'], ['Làm thêm ở Hibari Mart', '01:23'], ['Mua sắm và ca làm ở Hibari Mode', '02:16'], ['Bệnh, sổ tay, phòng khám', '03:14'], ['Quán sushi', '04:00'], ['Trường, khoá học video, luyện thi', '04:26'], ['Game Center', '04:58'], ['Âm nhạc và phòng tranh', '05:35'], ['Đảo Xanh: câu cá, bơi', '05:59'], ['Đảo Phú Quốc', '06:35'], ['Nghề nghiệp, ngân hàng', '06:52'], ['Chơi online', '07:14']], [8, 2])
s.figures([(S('demo-tour/menu.jpg'), 'Menu chính'), (S('demo-tour/konbini.jpg'), 'Hibari Mart'), (S('demo-tour/fashion.jpg'), 'Thử đồ'), (S('demo-tour/heat.jpg'), 'Say nắng'),
           (S('demo-tour/courses.jpg'), 'Khoá học video'), (S('demo-tour/fishing.jpg'), 'Câu cá')], 'Một số khung hình trong video demo', cols=3)

# ─────────── Chapter 12 ───────────

if EDIT and PLAY:
    results_table = next(t for t in doc.tables if t.rows[0].cells[0].text.startswith('Nhóm kiểm thử'))
    cell_set(results_table, 'EditMode toàn bộ', f'{EDIT["passed"]}/{EDIT["total"]} đạt (03/11/2026)')
s = Section(12)
s.h2('12.4. Đợt rà soát ngày 02/11/2026')
s.p('Sau khi chơi thử, chủ dự án gửi danh sách lỗi kèm ảnh chụp. Mỗi lỗi được tái hiện, tìm nguyên nhân gốc, sửa và thêm kiểm thử để không tái phát. Bảng dưới đây ghi lại những lỗi chính.')
s.grid('Lỗi đã sửa và nguyên nhân gốc', ['Hiện tượng', 'Nguyên nhân gốc', 'Cách xử lý', 'Kiểm thử'], [
    ['Quán sushi không bấm F được, không ra được cửa', 'Mô hình món ăn trỏ vào đối tượng Prefab cũ, Instantiate ném lỗi, bàn kẹt ở trạng thái chuẩn bị', 'Sửa 24 món, thêm kiểm tra toàn dự án', 'Scene Integrity, Sushi Flow'],
    ['Tự dưng bị trừ tiền', 'Nút chuột trái là phím tấn công, bấm giao diện thành phạt ¥500', 'Đưa tấn công sang phím X', 'Player Safety'],
    ['Rơi khỏi bản đồ khi ra cửa', 'Khe hở quanh cửa các khu', 'Chặn cửa, lùa nước kiểm tra lỗ hở từ điểm xuất hiện', 'Zone Leak'],
    ['Nhảy vô tận', 'Không giới hạn số lần và độ cao', 'Giới hạn 0,85 m, cách nhau 1 giây, chỉ từ mặt đất', 'Player Safety'],
    ['Xuống nước là rơi map', 'Biển là khối có tường bao', 'Bề mặt nước và hệ thống bơi', 'Swim'],
    ['Biển màu nâu', 'Hàm sương mù trong shader tô màu sương lên nước', 'Bỏ sương trong shader biển, thêm ánh sáng môi trường', 'Swim'],
    ['Biểu cảm giật', 'Tạo mới đối tượng và chữ mỗi lần bấm', 'Dùng lại bong bóng, làm nóng sẵn', 'Smoothness'],
    ['Nhân viên nam tên chị Ito', 'Sai dữ liệu', 'Đổi thành anh Ito ở mọi nguồn', 'Rà văn bản'],
    ['Nửa bộ đồ không đổi màu', 'Chỉ đổi một khối màu của áo', 'Đo mọi khối màu của quần áo', 'Fashion Store'],
    ['Tay che trong đầu chibi', 'Tư thế tay theo tỉ lệ người lớn', 'Đặt lại tay cạnh đầu, trước miệng', 'Ảnh chụp từng bệnh'],
    ['Cử chỉ bệnh chen ngang lúc ăn, bơi', 'Không kiểm tra cử chỉ đang chạy', 'Chỉ phát khi nhân vật rảnh', 'Sushi Flow, Swim'],
    ['Dải trắng che màn hình khi ra khỏi cửa hàng', 'Camera sau lưng nhân vật kẹt trong mái hiên', 'Ra phố nhìn dọc vỉa hè', 'Fashion Store'],
    ['Tên cử chỉ tiếng Nhật sai', 'Dữ liệu ghi bằng bảng mã không phải UTF-8', 'Sửa 10 tên, thêm kiểm tra chữ Nhật', 'Gesture Library'],
], [3.2, 4, 3.4, 2.2])
if EDIT and PLAY:
    s.p(f'Vòng kiểm thử toàn phần cuối cùng chạy ngày 03/11/2026 trên đúng mã nguồn của bản phát hành. EditMode đạt {EDIT["passed"]}/{EDIT["total"]} ca. Play Mode có {PLAY["total"]} ca: {PLAY["passed"]} đạt, {PLAY["skipped"]} ca bỏ qua có chủ đích vì chỉ dùng để quay video hoặc chụp ảnh khi gọi riêng, {PLAY["inconclusive"]} ca không kết luận được vì máy chạy test thiếu tệp ghi âm mẫu và vì camera gốc không chạy khi build target là WebGL, {PLAY["failed"]} ca không đạt. Ca không đạt là chấm Writing bằng Gemini thật. Nguyên nhân đầu tiên nằm ở chính ca kiểm thử: chấm AI đang tắt mặc định mà ca này chưa bật. Sau khi sửa, ca này gọi được tới Gemini nhưng dịch vụ báo quá tải hoặc hết thời gian chờ, kể cả khi gọi trực tiếp ngoài game, nên chưa thể chạy trọn; trò chơi xử lý đúng khi đó: thử lại một lần, giữ nguyên bài làm và báo người học chấm lại sau. Vì vậy luồng chấm Writing bằng AI thật chưa được xác nhận lại trong đợt này.')
s.p('Kiểm thử trên trình duyệt thật được chạy lại sau đợt sửa, đi qua 12 khu và các cửa sổ HUD ở cấu hình máy tính, cùng đường vào game trên màn hình điện thoại giả lập: không có lỗi Unity hay lỗi trình duyệt, không thiếu script, không có vật liệu hỏng, tốc độ từ 58 đến 60 khung hình mỗi giây. Hiện tượng màn hình chớp sáng tối khi mới vào thị trấn được đo trên bản phát hành bằng 382 ảnh chụp: sau ba khung chuyển dần từ màn hình tải, độ sáng trung bình giữ trong khoảng 145 đến 150 trên thang 255, không còn bước nhảy lớn. Phép đo này có giới hạn: ảnh chụp đạt khoảng 14 khung một giây trên trình duyệt không giao diện, nên một lần chớp ngắn trên máy người chơi vẫn có thể lọt qua.')

for p in paragraphs():  # a replacement across runs left the whole caption in its bold first run
    m = re.match(r'(Hình|Bảng) \d+\.\d+\. ', p.text)
    if m and p.style.name.startswith('ChuThich') and p.runs[0].text == p.text:
        rest = p.text[m.end():]
        p.runs[0].text = m.group(0)
        p.add_run(rest).bold = False
doc.save(OUT)
print('saved', OUT)
