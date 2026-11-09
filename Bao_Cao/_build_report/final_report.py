"""Final report pass (2026-11-08, after the owner's review): adds the idea and meaning of the game (1.6), the player's
journey and use-case diagrams (3.5), every area seen from above (4.5.x, 4.7), the Phú Quốc development (7.5), the
languages (10.1), the official links (13.3) and the last test round (12.5); then renumbers every figure per chapter and
the references to them. Edits the report in place; then v10_update_fields.ps1 refreshes the lists and exports the PDF.
Usage (from Bao_Cao/_build_report): python diagrams_final.py; python final_report.py ../NihongoLife_Bao_Cao_Capstone.docx
Safe to run once: it stops when the sections it adds are already there."""
import copy
import os
import re
import sys

from docx import Document
from docx.oxml.ns import qn
from docx.shared import Cm
from PIL import Image, ImageDraw, ImageFont

DOCX = sys.argv[1]
HERE = os.path.dirname(os.path.abspath(__file__))
BAO = os.path.normpath(os.path.join(HERE, '..'))
DIAG = os.path.join(HERE, 'diag')
TMP = os.path.join(HERE, 'img_final')
os.makedirs(TMP, exist_ok=True)

doc = Document(DOCX)
body = [p for p in doc.paragraphs if not p.style.name.lower().startswith('toc')]
if any(p.text.startswith('1.6. Ý tưởng và ý nghĩa') for p in body):
    sys.exit('already applied')


# ─────────── pictures ───────────

def font(size, bold=False):
    return ImageFont.truetype('C:/Windows/Fonts/' + ('segoeuib.ttf' if bold else 'segoeui.ttf'), size)


def meaning_picture():
    tiles = [('loop', f'{BAO}/_build_slides/assets/loop.jpg', '学', 'Học qua tình huống thật', '#2E8B7A'),
             ('career', f'{BAO}/career/04_interview.png', '職', 'Định hướng nghề nghiệp', '#F2B33D'),
             ('bridge', f'{BAO}/phuquoc/02_ganh_ngon.png', '橋', 'Cầu nối Việt – Nhật', '#D9483B'),
             ('life', f'{BAO}/clinic/05_doctor.png', '生', 'Kỹ năng sống và văn hoá', '#4C6FD6')]
    W, H = 1100, 700
    sheet = Image.new('RGB', (W * 2 + 30, H * 2 + 30), 'white')
    jp = ImageFont.truetype('C:/Windows/Fonts/YuGothB.ttc', 120)
    for i, (_, src, kanji, label, color) in enumerate(tiles):
        im = Image.open(src).convert('RGB')
        scale = max(W / im.width, (H - 110) / im.height)
        im = im.resize((int(im.width * scale), int(im.height * scale)))
        im = im.crop(((im.width - W) // 2, (im.height - (H - 110)) // 2, (im.width - W) // 2 + W, (im.height - (H - 110)) // 2 + H - 110))
        tile = Image.new('RGB', (W, H), color)
        tile.paste(im, (0, 0))
        d = ImageDraw.Draw(tile)
        d.ellipse((30, 30, 230, 230), fill=color, outline='white', width=8)
        d.text((130, 124), kanji, font=jp, fill='white', anchor='mm')
        d.text((W / 2, H - 55), label, font=font(52, True), fill='white' if color != '#F2B33D' else '#14213D', anchor='mm')
        sheet.paste(tile, ((i % 2) * (W + 30), (i // 2) * (H + 30)))
    path = os.path.join(TMP, 'meaning.jpg')
    sheet.save(path, quality=90)
    return path


def map_pair(name):
    a = Image.open(f'{BAO}/maps/map_{name}.png').convert('RGB')
    b = Image.open(f'{BAO}/maps/aerial_{name}.png').convert('RGB')
    sheet = Image.new('RGB', (a.width + b.width + 24, a.height), 'white')
    sheet.paste(a, (0, 0))
    sheet.paste(b, (a.width + 24, 0))
    sheet.thumbnail((2400, 2400))
    path = os.path.join(TMP, f'map_{name}.jpg')
    sheet.save(path, quality=88)
    return path


# ─────────── document helpers ───────────

def find(prefix, style=None):
    for p in doc.paragraphs:
        if p.style.name.lower().startswith('toc'):
            continue
        if p.text.startswith(prefix) and (style is None or p.style.name == style):
            return p
    raise KeyError(prefix)


def section_end(heading):
    """The last paragraph before the next heading of the same or a higher level."""
    level = int(heading.style.name.split()[-1])
    el = heading._p
    last = el
    nxt = el.getnext()
    while nxt is not None:
        if nxt.tag == qn('w:p'):
            style = nxt.find(qn('w:pPr'))
            sid = style.find(qn('w:pStyle')).get(qn('w:val')) if style is not None and style.find(qn('w:pStyle')) is not None else ''
            if sid.startswith('Heading') and int(sid[-1]) <= level:
                break
        last = nxt
        nxt = nxt.getnext()
    return last


class Cursor:
    """Inserts paragraphs one after another, after an anchor element."""

    def __init__(self, anchor):
        self.at = anchor

    def _add(self, style, text=None, bold_prefix=None):
        p = doc.add_paragraph(style=style)
        if bold_prefix:
            p.add_run(bold_prefix).bold = True
        if text:
            p.add_run(text)
        self.at.addnext(p._p)
        self.at = p._p
        return p

    def heading(self, text, level):
        return self._add(f'Heading {level}', text)

    def para(self, text):
        return self._add('Normal', text)

    def bullets(self, items):
        for t in items:
            self._add('List Bullet', t)

    def figure(self, path, caption, width_cm=16.0):
        p = self._add('Normal')
        pf = p.paragraph_format
        pf.keep_with_next = True
        pf.alignment = 1
        pf.space_before, pf.space_after = Cm(0.2), Cm(0.1)
        pf.line_spacing = 1.0
        pf.first_line_indent = Cm(0)
        im = Image.open(path)
        height = width_cm * im.height / im.width
        if height > 19.0:  # a tall diagram keeps to the page
            width_cm *= 19.0 / height
        p.add_run().add_picture(path, width=Cm(width_cm))
        self._add('ChuThichHinh', caption, bold_prefix='Hình 0.0. ')


# ─────────── 1.6 idea and meaning ───────────

c = Cursor(section_end(find('1.5. NihongoLife qua các con số', 'Heading 2')))
c.heading('1.6. Ý tưởng và ý nghĩa của trò chơi', 2)
c.para('Ý tưởng của NihongoLife là “sống ở Nhật để học tiếng Nhật”. Người chơi vào vai một bạn trẻ Việt Nam vừa chuyển tới khu phố Hibari-chō. '
       'Không có màn hình bài học tách rời: mua cơm nắm ở konbini, mua vé tàu, khai bệnh ở phòng khám, phỏng vấn xin lên chức đều là những tình huống giao tiếp có thật, '
       'câu nói xuất hiện đúng lúc người chơi cần dùng. Ý tưởng này trả lời trực tiếp vấn đề ở mục 1.1: người học thuộc từ trên giấy nhưng không dám nói ngoài đời.')
c.para('NihongoLife là một game giáo dục định hướng. Bốn ý nghĩa của trò chơi được thể hiện bằng bốn hình ảnh tượng trưng trong game (Hình 0.0 ngay dưới):')
c.bullets([
    'Học qua tình huống thật (学): mỗi câu tiếng Nhật có cách đọc và nghĩa, gắn với một việc cụ thể; trả lời sai được giải thích và chọn lại, không mất tiến trình. '
    'Điểm trí tuệ tích luỹ mở khu vực mới và chương truyện mới, nên tiến bộ hiện ra ngay trong thế giới chứ không chỉ là một con số.',
    'Định hướng nghề nghiệp (職): 10 ngành, mỗi ngành 6 bậc, bậc 4 chọn nhánh; mỗi lần thăng chức là một buổi phỏng vấn tiếng Nhật với người quản lý. '
    'Người chơi thử nghề trước khi chọn, thấy rõ trình độ ngoại ngữ mở ra vị trí cao hơn, và học kính ngữ, cách cư xử nơi làm việc ở Nhật.',
    'Cầu nối Việt – Nhật (橋): người chuẩn bị du học, đi làm ở Nhật làm quen trước với đời sống ở đó; ở Phú Quốc, quán Gánh Ngon đón đoàn khách Nhật, '
    'nên tiếng Nhật được dùng ngay trên quê hương. Cốt truyện kết thúc bằng lựa chọn nơi sống: ở lại Hibari-chō, về Việt Nam làm cầu nối, hay sống giữa hai nước.',
    'Kỹ năng sống và văn hoá (生): phép lịch sự nơi công cộng (chào hỏi, đi tàu, đổ rác đúng ngày, いただきます trước bữa ăn), chăm sóc sức khoẻ đúng y khoa '
    '(16 bệnh, thuốc không kê đơn chỉ làm dịu, kháng sinh không trị cảm cúm) và quản lý tiền (lương, chi tiêu, ngân hàng, công ty).',
])
c.figure(meaning_picture(), 'Bốn ý nghĩa của NihongoLife và hình ảnh tượng trưng, ảnh chụp trong game', 15.5)
c.para('Đối tượng hướng tới: người học tiếng Nhật từ con số 0 (N5) tới N1; người chuẩn bị du học hoặc đi làm ở Nhật; người luyện IELTS và tiếng Nga (TORFL) trên cùng nền tảng; '
       'giáo viên dạy nói qua phòng học video. Khách chơi offline có cả thị trấn để khám phá; tài khoản đã đăng ký đi theo lộ trình và học cùng người thật khi online.')
c.para('Điểm khác với ứng dụng học từ vựng hay khoá học video thông thường: trình độ ngôn ngữ (trí tuệ) gắn trực tiếp với những gì người chơi được làm trong thế giới '
       '(khu vực mở ra, chương truyện, phỏng vấn thăng chức), nên động lực học đến từ nhu cầu sống và làm việc trong game. Việc học không dừng ở trò chơi: '
       'video bài giảng, bài thi thử theo định dạng JLPT, IELTS, TORFL và phòng luyện nói với giáo viên nối trò chơi với việc học thật.')

# ─────────── 3.5 player journey and diagrams ───────────

c = Cursor(section_end(find('3.4. Đặc tả các ca sử dụng chính', 'Heading 2')))
c.heading('3.5. Luồng người chơi và các sơ đồ', 2)
c.para('Các sơ đồ dưới đây mô tả trò chơi từ góc nhìn người chơi: hành trình tổng thể từ lúc mở game tới kết thúc cốt truyện, một ngày chơi, các ca sử dụng '
       'và một buổi học trong lớp. Sơ đồ được vẽ lại từ hành vi thật của game (tên lớp và màn hình đúng như trong mã nguồn).')
c.figure(os.path.join(DIAG, 'd50_player_journey.png'), 'Hành trình của người chơi: bắt đầu, vòng lặp mỗi ngày, phát triển và mục tiêu dài hạn')
c.para('Hành trình có bốn tầng. Bắt đầu: người chơi mở game trên máy tính, trình duyệt hoặc điện thoại, chọn ngôn ngữ giao diện và nhân vật; chơi khách thì cả thị trấn mở ngay, '
       'đăng nhập tài khoản thì đi theo lộ trình và vào được online. Vòng lặp mỗi ngày: dạo thị trấn, nhận nhiệm vụ, hội thoại tiếng Nhật, đúng được thưởng, sai được giải thích; '
       'nhu cầu ăn uống, ngủ, sức khoẻ quyết định việc tiếp theo. Phát triển: yên, XP và trí tuệ mở khu vực mới (100 trí tuệ mở quán sushi, 160 mở ga tàu, 240 mở Game Center, '
       '320 mở đảo Midori), cấp 10 mở nghề nghiệp, lớp học và thi thử nâng trình độ. Mục tiêu dài hạn: chín chương cốt truyện theo N5 tới N1 với ba kết thúc, và thế giới online.')
c.figure(os.path.join(DIAG, 'd51_daily_loop.png'), 'Một ngày của nhân vật (sơ đồ hoạt động)')
c.para('Một ngày bắt đầu ở phòng trọ. Nếu đói, khát hay mệt, người chơi tới Hibari Mart hoặc Sushi Hibari; nếu ốm thì tới phòng khám (tiếp nhận, phiếu hỏi bệnh, khám, '
       'trả tiền, nhận thuốc). Sau đó người chơi chọn đi học, đi làm, khám phá hay giải trí; tiền, trí tuệ và XP tăng lên; ngủ 8 giờ hồi sức khoẻ, sang ngày mới và lưu tiến trình.')
c.figure(os.path.join(DIAG, 'd52_use_case_final.png'), 'Sơ đồ use case: khách, tài khoản, giáo viên và dịch vụ ngoài')
c.para('Khách và tài khoản dùng chung toàn bộ phần chơi; tài khoản (đăng ký bằng email) có thêm các ca sử dụng online. Máy chủ cũng kiểm tra điều kiện này '
       '(Edge Functions từ chối người dùng ẩn danh, cơ sở dữ liệu dùng hàm is_registered), nên không chỉ dựa vào phía máy khách.')
c.figure(os.path.join(DIAG, 'd53_seq_lesson.png'), 'Sơ đồ tuần tự: một buổi học trong lớp')
c.para('Bàn đầu lớp ở trường Hibari và phím ` mở cùng một cửa sổ Lớp học. Người chơi chọn ngôn ngữ trước, sau đó chỉ thấy bài học, video và thi thử của ngôn ngữ đó; '
       'nút VIDEO của mỗi bài mở đúng video YouTube của bài; bài đạt cộng trí tuệ, và trí tuệ là điều kiện mở khu vực cũng như phỏng vấn thăng chức.')

# ─────────── every area from above ───────────

AREAS = [
    ('4.5.2.', '90_testsandbox', 'thị trấn Hibari-chō', 'phố chính Sakura-dōri chạy ngang giữa thị trấn, hai làn xe có vạch dừng và vạch qua đường; phố bên chạy Bắc – Nam tới trường và Game Center; '
     'hẻm sau, bãi đỗ xe và công viên ở phía Bắc; dãy cửa hàng phía Nam gồm phòng khám, tiệm nhạc Hibiki, Hibari Mode và Game Center.'),
    ('4.5.3.', '20_stationdistrict', 'ga Hibari', 'sảnh có máy bán vé ở một đầu, dãy cổng soát vé là lối duy nhất xuống sân ga, hai đường ray với sân ga dài, cầu nối về phía thành phố.'),
    ('4.5.4.', '40_hibariclass', 'trường Nhật ngữ Hibari', 'bảng đen và bàn cô Morita ở đầu lớp, bốn dãy bàn học sinh (ghế đo từ mô hình bàn ghế để nhân vật ngồi đúng chỗ), cửa ra phố ở cuối lớp.'),
    ('4.5.5.', '30_sushirestaurant', 'nhà hàng Sushi Hibari', 'quầy lễ tân ở cửa, quầy đầu bếp Ota phía trong, khu bàn ăn ở giữa; mỗi bàn có thực đơn hình.'),
    ('4.5.6.', '35_hibariclinic', 'phòng khám Hibari', 'quầy tiếp nhận và phòng chờ ở phía trước, phòng khám bác sĩ và khu giường nghỉ phía sau, quầy nhận thuốc cạnh lối ra; người dân đi theo tuyến khám, mỗi lần một người trong phòng bác sĩ.'),
    ('4.5.7.', '45_homebedroom', 'phòng trọ', 'giường và bàn học dọc tường, bếp và tủ lạnh cạnh cửa, thảm và bàn thấp ở giữa phòng.'),
    ('4.5.8.', '50_gamecenter', 'Game Center', 'các dãy máy luyện chữ xếp theo hàng, khu ném bóng rổ và máy gắp thú ở hai bên, quầy đổi quà gần lối ra.'),
    ('4.5.9.', '55_musicshop', 'Hibiki Music & Art', 'một nửa là phòng tranh 3D treo 39 tác phẩm trên tường, nửa còn lại là tiệm nhạc: đàn piano, tường guitar, sân khấu và bàn DJ, giá vẽ, quầy chú Hibiki.'),
    ('4.5.10.', '60_midoriisland', 'đảo Midori', 'đảo tròn với ga tàu ở bờ Nam, hai con đường chữ thập chia đảo thành khu ruộng, khu chuồng thú và bến câu cá; bãi cát bao quanh để bơi.'),
    ('4.5.11.', '65_phuquocisland', 'Phú Quốc (khu Dương Đông)', 'khu trung tâm Dương Đông quanh quán Gánh Ngon: phố, hẻm, chợ, khu nghỉ dưỡng có hồ bơi và bờ biển; cả đảo được dựng từ toạ độ OpenStreetMap ở tỉ lệ 1:100.'),
    ('4.7.', '56_fashionstore', 'cửa hàng Hibari Mode', 'các khu trang phục thường ngày, công sở, thể thao, theo mùa và lễ hội; phòng thử đồ có gương ở góc, quầy anh Ren cạnh lối vào.'),
]
for prefix, name, title, layout in AREAS:
    heading = next(p for p in doc.paragraphs if p.style.name.startswith('Heading') and p.text.startswith(prefix))
    c = Cursor(section_end(heading))
    c.para(f'Nhìn từ trên cao: {layout}')
    c.figure(map_pair(name), f'Bản đồ {title} nhìn thẳng từ trên xuống (trái) và góc nhìn chim bay (phải), chụp trong game')

# ─────────── 7.5 Phú Quốc development ───────────

c = Cursor(section_end(find('7.4. Tỉ lệ bản đồ và phép chiếu toạ độ', 'Heading 2')))
c.heading('7.5. Quá trình phát triển và hướng mở rộng', 2)
c.para('Phú Quốc được phát triển theo từng giai đoạn. Ngày 26/10/2026 chủ dự án quyết định làm Phú Quốc như một bản mở rộng lớn; ngày 27/10/2026 phần này được khởi động lại '
       'trong game với dữ liệu thật: toạ độ 31 địa điểm tra từ OpenStreetMap, phép chiếu equirectangular quanh quán Gánh Ngon, tỉ lệ 1 đơn vị = 100 m. '
       'Scene 65_PhuQuocIsland chỉ được dựng bằng PhuQuocIslandBuilder từ phu_quoc_map.json, không sửa tay, nên mỗi lần cập nhật dữ liệu là dựng lại cả đảo.')
c.figure(os.path.join(DIAG, 'd55_phuquoc_roadmap.png'), 'Phú Quốc: phần đã có trong game và hai giai đoạn mở rộng')
c.para('Người chơi tới Phú Quốc bằng tàu từ ga Hibari tới ga Sân bay rồi bay sang đảo; màn đến chào bằng câu フーコックに ようこそ！. Trên đảo người chơi:')
c.bullets([
    'Làm ở quán Gánh Ngon (113 Trần Hưng Đạo, Dương Đông): 9 món thật của quán, 18 mẫu câu, hội thoại Việt / Anh / Nhật với khách; ca làm GanhNgonShift là việc của nghề Nhà hàng & dịch vụ.',
    'Thăng tiến 6 bậc từ thực tập tới chủ quán: phục vụ bàn đầu tiên, nhận món của đoàn khách Nhật, dẫn ca tối và xử lý phàn nàn lịch sự, lên kế hoạch hợp tác với công ty du lịch Nhật, mở quán thứ hai cạnh biển.',
    'Dạo phố Dương Đông, chợ và chợ đêm, bơi ở biển và hồ bơi khu nghỉ dưỡng, đi xe tour qua 31 điểm của đảo; chương 6 của cốt truyện (“Cầu nối tới Phú Quốc”, N4) diễn ra ở đây.',
])
c.para('Giai đoạn 2 chia đảo thành ba khu như định hướng của chủ dự án:')
c.bullets([
    'Bắc đảo (nghỉ dưỡng, vui chơi): Bãi Dài, Gành Dầu, Rạch Vẹm, Vườn quốc gia Phú Quốc và Núi Chúa (603 m).',
    'Trung tâm Dương Đông, làm chi tiết ở tỉ lệ 1:1 quanh Trần Hưng Đạo, Bạch Đằng, chợ đêm, Dinh Cậu, chùa Sùng Hưng, nhà thùng nước mắm và vườn tiêu.',
    'Nam đảo: An Thới, cảng An Thới, Bãi Sao, Bãi Khem, Di tích Nhà tù Phú Quốc, ga cáp treo Hòn Thơm; bờ Đông có làng chài Hàm Ninh, Suối Tranh, chùa Hộ Quốc.',
])
c.para('Giai đoạn 3 thêm nghề và cộng đồng: hướng dẫn viên tiếng Nhật dẫn đoàn khách qua các điểm tham quan, lễ tân khu nghỉ dưỡng, bán hàng, giáo viên; nhiệm vụ về đặc sản thật '
       '(nước mắm có chỉ dẫn địa lý, tiêu, ngọc trai); gặp bạn bè online ở Phú Quốc. Nghề mới chỉ cần thêm dữ liệu vào careers.json và một trạm làm việc, không phải viết hệ thống mới.')
c.para('Ràng buộc khi mở rộng: không lấy dữ liệu hay ảnh 3D từ Google Maps (điều khoản cấm), dùng OpenStreetMap và ghi “© OpenStreetMap contributors”; tên và logo thương mại '
       '(các tập đoàn du lịch, khu vui chơi) không dùng khi chưa có phép, địa danh công cộng giữ nguyên; trên web, Phú Quốc là một gói tải riêng và được giải phóng khi người chơi rời đảo.')

# ─────────── 10.1 languages ───────────

c = Cursor(section_end(find('10.1. Bốn ngôn ngữ và tiền tệ', 'Heading 2')))
c.para('NihongoLife phân biệt ba loại ngôn ngữ (Hình 0.0 ngay dưới). Ngôn ngữ giao diện (tiếng Việt, tiếng Anh, tiếng Nhật, tiếng Nga) đổi được bất cứ lúc nào ở menu hoặc Cài đặt, '
       'đổi cả chữ lẫn đơn vị tiền. Ngôn ngữ học chọn trong Lớp học: tiếng Nhật là ngôn ngữ chính của thế giới game (hội thoại với người dân, cốt truyện N5 tới N1); '
       'tiếng Anh có English Foundations và luyện IELTS; tiếng Nga có lộ trình 53 bước, 64 video và luyện TORFL; tiếng Việt dành cho người nước ngoài. '
       'Mỗi ngôn ngữ học có 40 bài với bảng chữ, ngữ pháp, chứng chỉ và bài kiểm tra trình độ. Đề luyện thi do dự án tự soạn theo định dạng kỳ thi, chứng nhận trong game không phải chứng chỉ chính thức.')
c.figure(os.path.join(DIAG, 'd54_languages.png'), 'Ngôn ngữ giao diện, ngôn ngữ học và các kỳ thi luyện trong NihongoLife')

# ─────────── 13.3 official links ───────────

c = Cursor(section_end(find('13.3. Kênh truyền thông', 'Heading 2')))
c.para('Đường dẫn chính thức của sản phẩm:')
c.bullets([
    'Trang web giới thiệu NihongoLife: https://webservice.gzv.one/store/demo/nihongolife-edu-game-theme',
    'Kênh YouTube @NihongoLifeGame: https://www.youtube.com/channel/UCAKp-_EsasxPY2oa-4CzWtQ',
    'Mã nguồn: https://github.com/StephenSouth13/NihongoLife',
])

# ─────────── 12.5 last round ───────────

c = Cursor(section_end(find('12.4. Đợt rà soát ngày 02/11/2026', 'Heading 2')))
c.heading('12.5. Đợt hoàn thiện ngày 08/11/2026', 2)
c.bullets([
    'Kiểm thử toàn phần: EditMode 339/339 đạt; Play Mode 242 ca: 222 đạt ở lượt đầu, 13 ca quay video và chụp ảnh chỉ chạy khi gọi riêng, 3 ca không kết luận được vì phụ thuộc bên ngoài '
    '(file ghi âm mẫu, máy chủ Gemini quá tải, camera thật khi build cho WebGL); 4 ca lỗi đều do test (trạng thái test trước để lại, canh thời gian, mất mạng giữa chừng), đã sửa và chạy lại đạt cả 4.',
    'Điện thoại: trên web, mỗi khu vực là một gói tải về và trước đây không bao giờ được giải phóng; đi qua vài khu, bộ nhớ trình duyệt tăng thêm hàng trăm MB tới khi điện thoại đóng tab. '
    'Nay khu đã rời trả lại gói của nó (SceneContentLoader.ReleaseArea), điện thoại mặc định đồ hoạ nhẹ (tỉ lệ điểm ảnh 1) và texture giảm một bậc khi GPU không đọc được DXT. Đo giả lập điện thoại: số gói giữ trong bộ nhớ luôn là 2 (thị trấn và khu đang ở), heap WebAssembly đứng yên khi đi tiếp qua Phú Quốc, Midori, phòng khám thay vì tăng thêm mỗi khu.',
    'Rò bộ nhớ khi đi lại nhiều khu: nhãn chữ có viền nhân bản material cho từng nhãn (nay dùng chung qua TmpOutline.Apply), và bộ dịch chữ của scene giữ nhãn của mọi khu đã rời (nay dọn mỗi lần nạp scene). '
    'Test ZoneMemoryPlayModeTests đi 4 vòng tiệm nhạc, Sushi, Phú Quốc: số texture, mesh, material, âm thanh và bộ nhớ C# đứng yên. Trên bản web giả lập điện thoại (CPU chậm 4 lần, không có nén DXT), 12 lần vào khu, heap WebAssembly giữ 628 MB từ vòng 2 tới vòng 4; '
    'kiểm tra web toàn bộ 13 khu không lỗi, không thiếu script, luôn giữ tối đa 2 gói.',
    'Ảnh bản đồ từ trên cao của 11 khu (DemoRecordingTests.Maps_TopDown), sơ đồ hành trình người chơi, một ngày chơi, use case, buổi học và lộ trình Phú Quốc.',
    'Video Con đường sự nghiệp (10 ngành, bậc 1 tới bậc 6, giọng nam, nhạc nền) và video demo đầy đủ chức năng được quay lại trực tiếp trong game.',
])

# ─────────── renumber the figures per chapter, and their references ───────────

chapter = 0
counts = {}
mapping = {}
new_index = []
for p in doc.paragraphs:
    if p.style.name.lower().startswith('toc'):
        continue
    if p.style.name == 'Heading 1':
        m = re.match(r'CHƯƠNG (\d+)\.', p.text)
        chapter = int(m.group(1)) if m else chapter
        continue
    if p.style.name != 'ChuThichHinh':
        continue
    m = re.match(r'Hình (\d+)\.(\d+)\. ', p.text)
    if not m:
        continue
    counts[chapter] = counts.get(chapter, 0) + 1
    new = f'{chapter}.{counts[chapter]}'
    old = f'{m.group(1)}.{m.group(2)}'
    if old != '0.0':
        mapping[old] = new
    new_index.append(new)
    run = p.runs[0]
    assert run.text.startswith('Hình '), run.text
    run.text = f'Hình {new}. '

# The new paragraphs that point at "Hình 0.0 ngay dưới" take the number of the next figure after them.
pending = []
for p in doc.paragraphs:
    if p.style.name.lower().startswith('toc'):
        continue
    if 'Hình 0.0' in p.text and p.style.name != 'ChuThichHinh':
        pending.append(p)
    elif p.style.name == 'ChuThichHinh' and pending:
        number = re.match(r'Hình (\d+\.\d+)\. ', p.text).group(1)
        for q in pending:
            for r in q.runs:
                r.text = r.text.replace('Hình 0.0', 'Hình ' + number)
        pending = []

ref = re.compile(r'(Hình) (\d+\.\d+)(?!\.\d)')
changed = 0
for p in doc.paragraphs:
    if p.style.name.lower().startswith('toc') or p.style.name == 'ChuThichHinh':
        continue
    for r in p.runs:
        if 'Hình ' in r.text:
            new_text = ref.sub(lambda m: f'{m.group(1)} {mapping.get(m.group(2), m.group(2))}', r.text)
            if new_text != r.text:
                r.text = new_text
                changed += 1
for t in doc.tables:
    for row in t.rows:
        for cell in row.cells:
            for p in cell.paragraphs:
                for r in p.runs:
                    new_text = ref.sub(lambda m: f'{m.group(1)} {mapping.get(m.group(2), m.group(2))}', r.text)
                    if new_text != r.text:
                        r.text = new_text
                        changed += 1

assert not any('Hình 0.0' in p.text for p in doc.paragraphs if not p.style.name.lower().startswith('toc')), 'a figure kept its placeholder number'
doc.save(DOCX)
print('figures per chapter:', counts)
print('renumbered', sum(1 for k, v in mapping.items() if k != v), 'figures;', changed, 'references updated')
