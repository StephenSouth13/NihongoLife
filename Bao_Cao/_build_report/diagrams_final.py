"""Diagrams of the final report (2026-11-08): the player's journey, a day in the game, the use cases, a lesson in the
classroom, the languages and the Phú Quốc roadmap. Same colours as diagrams.py / diagrams_v4.py, with larger text: the
report prints them 2600 px wide on 16 cm, so text is 30-46 px (about 7-10 pt on paper).
Run: python diagrams_final.py  ->  diag/d50..d55_*.png"""
from PIL import Image, ImageDraw

from diagrams import OUT, INK, GOLD, RED, MUTED, PAPER, SOFT, GREEN, font, wrap, arrow
from diagrams2 import centered, dashed, sequence, BLUE
from diagrams_v4 import box, TEAL

PALE_GOLD, PALE_TEAL, PALE_RED, PALE_BLUE = "#FDF3DC", "#E3F2EE", "#FDECEA", "#E8EEFA"
TS, SS = 38, 30  # box title / subtitle


def canvas(W, H, title, sub=None):
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)
    d.text((60, 30), title, font=font(58, True), fill=INK)
    if sub:
        d.text((60, 108), sub, font=font(32), fill=MUTED)
    return img, d


def label(d, x, y, text, size=30, color=INK, bg=PAPER):
    f = font(size, True)
    w = d.textlength(text, font=f)
    d.rectangle((x - w / 2 - 8, y - size * 0.7, x + w / 2 + 8, y + size * 0.7), fill=bg)
    d.text((x, y), text, font=f, fill=color, anchor="mm")


def stick(d, cx, cy, name, sub=None, color=INK):
    d.ellipse((cx - 28, cy - 116, cx + 28, cy - 60), outline=color, width=6)
    d.line([(cx, cy - 60), (cx, cy + 10)], fill=color, width=6)
    d.line([(cx - 54, cy - 32), (cx + 54, cy - 32)], fill=color, width=6)
    d.line([(cx, cy + 10), (cx - 42, cy + 82)], fill=color, width=6)
    d.line([(cx, cy + 10), (cx + 42, cy + 82)], fill=color, width=6)
    d.text((cx, cy + 122), name, font=font(38, True), fill=color, anchor="mm")
    if sub:
        d.text((cx, cy + 166), sub, font=font(28), fill=MUTED, anchor="mm")


def mid(b, side):
    x1, y1, x2, y2 = b
    return {"l": (x1, (y1 + y2) / 2), "r": (x2, (y1 + y2) / 2), "t": ((x1 + x2) / 2, y1), "b": ((x1 + x2) / 2, y2)}[side]


def diamond(d, cx, cy, w, h, text, fill=PALE_GOLD):
    d.polygon([(cx, cy - h / 2), (cx + w / 2, cy), (cx, cy + h / 2), (cx - w / 2, cy)], fill=fill, outline=INK, width=4)
    centered(d, cx, cy, wrap(d, text, font(34, True), w * 0.6), font(34, True), INK, 42)


def band(d, y1, y2, text, color):
    d.rounded_rectangle((40, y1, 2560, y2), 26, fill=color)
    d.text((70, y1 + 14), text, font=font(32, True), fill=MUTED)


def journey():
    img, d = canvas(2600, 2060, "Hành trình của người chơi", "Từ lúc mở game tới ba kết thúc; khách mở cả thị trấn, tài khoản theo lộ trình và vào được online")
    band(d, 170, 560, "1 · Bắt đầu", SOFT)
    band(d, 590, 980, "2 · Vòng lặp mỗi ngày", PALE_TEAL)
    band(d, 1010, 1400, "3 · Phát triển", PALE_GOLD)
    band(d, 1430, 2030, "4 · Mục tiêu dài hạn và cộng đồng", PALE_BLUE)
    a = (80, 250, 520, 500); box(d, a, "Mở game", "máy tính, trình duyệt, điện thoại", fill=GOLD, ts=TS, ss=SS)
    b = (590, 250, 1050, 500); box(d, b, "Menu chính", "ngôn ngữ, nhân vật, điều khiển", ts=TS, ss=SS)
    diamond(d, 1270, 375, 330, 240, "Đăng nhập?")
    c1 = (1560, 225, 2520, 360); box(d, c1, "Khách (offline)", "cả thị trấn mở, lưu trên máy", ts=TS, ss=SS)
    c2 = (1560, 395, 2520, 530); box(d, c2, "Tài khoản đã đăng ký", "lộ trình, cốt truyện, online", fill=INK, txt="#FFFFFF", ts=TS, ss=SS)
    arrow(d, [mid(a, "r"), mid(b, "l")]); arrow(d, [mid(b, "r"), (1105, 375)])
    d.line([(1435, 375), (1490, 375)], fill=INK, width=5)
    arrow(d, [(1490, 375), (1490, 292), mid(c1, "l")]); label(d, 1490, 200, "không")
    arrow(d, [(1490, 375), (1490, 462), mid(c2, "l")]); label(d, 1440, 520, "có")
    row = [("Thị trấn Hibari", "bản đồ M, bảng tin, nhiệm vụ N"), ("Hội thoại tiếng Nhật", "cách đọc, nghĩa, 1–4 lựa chọn"),
           ("Đúng: thưởng · Sai: giải thích", "thử lại, không mất tiến trình"), ("Nhu cầu, sức khoẻ", "ăn uống, ngủ, khám bệnh")]
    boxes = []
    for i, (t, s) in enumerate(row):
        x = 80 + i * 620
        bb = (x, 670, x + 560, 940); boxes.append(bb)
        box(d, bb, t, s, fill=PAPER if i != 1 else TEAL, txt=INK if i != 1 else "#FFFFFF", ts=TS, ss=SS)
        if i: arrow(d, [mid(boxes[i - 1], "r"), mid(bb, "l")])
    d.line([mid(c1, "r"), (2545, 292), (2545, 575), (360, 575)], fill=MUTED, width=4)
    d.line([mid(c2, "b"), (2040, 575)], fill=MUTED, width=4)
    arrow(d, [(360, 575), mid(boxes[0], "t")], color=MUTED, width=4)
    arrow(d, [mid(boxes[3], "b"), (2200, 995), (360, 995), (360, 1080)], color=INK, width=4)
    row2 = [("Yên · XP · trí tuệ", "từ nhiệm vụ, bài học, ca làm, bài thi"), ("Mở khu vực mới", "100 sushi · 160 ga · 240 Game Center · 320 Midori"),
            ("Cấp 10: nghề nghiệp", "10 ngành × 6 bậc, phỏng vấn tiếng Nhật"), ("Lớp học, thi thử", "N5→N1, IELTS, TORFL, video")]
    boxes2 = []
    for i, (t, s) in enumerate(row2):
        x = 80 + i * 620
        bb = (x, 1080, x + 560, 1360); boxes2.append(bb)
        box(d, bb, t, s, fill=GOLD if i == 0 else PAPER, ts=TS, ss=SS)
        if i: arrow(d, [mid(boxes2[i - 1], "r"), mid(bb, "l")])
    e = (80, 1510, 1250, 1740); box(d, e, "Cốt truyện 9 chương, N5 → N1", "trường học, lễ hội, chọn nghề, Phú Quốc, kỳ thi N3, đàn anh, hai quê hương", ts=TS, ss=SS)
    f = (80, 1800, 1250, 1990); box(d, f, "3 kết thúc ở chương 9", "ở lại Hibari-chō · về Việt Nam · sống giữa hai nước", fill=GOLD, ts=TS, ss=SS)
    arrow(d, [mid(boxes2[3], "b"), (2040, 1415), (665, 1415), mid(e, "t")]); arrow(d, [mid(e, "b"), mid(f, "t")])
    g = (1390, 1510, 2520, 1990); box(d, g, "Thế giới online (chỉ tài khoản)", "gặp người thật, chat khu vực, bạn bè, voice khu vực (giữ T), gọi riêng, phòng luyện nói với giáo viên, bảng xếp hạng", fill=INK, txt="#FFFFFF", ts=TS + 4, ss=SS + 2)
    arrow(d, [(2520, 462), (2575, 462), (2575, 1750), mid(g, "r")], color=BLUE, width=4, dash=True)
    img.save(OUT / "d50_player_journey.png")


def daily():
    img, d = canvas(2600, 1640, "Một ngày của nhân vật (sơ đồ hoạt động)", "Nhu cầu và sức khoẻ quyết định việc tiếp theo; mỗi việc là một tình huống giao tiếp")
    d.ellipse((60, 330, 120, 390), fill=INK)
    w = (170, 270, 650, 450); box(d, w, "Thức dậy", "phòng trọ: giường, tủ lạnh, bàn học", ts=TS, ss=SS)
    arrow(d, [(120, 360), mid(w, "l")])
    diamond(d, 960, 360, 440, 260, "Đói, khát hay mệt?")
    arrow(d, [mid(w, "r"), (740, 360)])
    k = (1310, 200, 1960, 345); box(d, k, "Hibari Mart, Sushi Hibari", "mua, gọi món bằng tiếng Nhật", ts=TS - 4, ss=SS - 2)
    s = (1310, 380, 1960, 525); box(d, s, "Ốm: phòng khám, thuốc", "tiếp nhận → khám → nhận thuốc", fill=PALE_RED, ts=TS - 4, ss=SS - 2)
    arrow(d, [(1180, 360), (1240, 360), (1240, 272), mid(k, "l")]); arrow(d, [(1240, 360), (1240, 452), mid(s, "l")])
    label(d, 1240, 230, "có")
    acts = [("Đi học", "bài, video, thi thử", TEAL), ("Đi làm", "ca làm, lương, thăng chức", GOLD), ("Khám phá", "ga tàu, Midori, Phú Quốc", BLUE), ("Giải trí", "Game Center, nhạc, tranh", RED)]
    bar_y = 680
    d.rectangle((260, bar_y - 7, 2340, bar_y + 7), fill=INK)
    arrow(d, [(960, 490), (960, bar_y - 9)]); label(d, 1030, 590, "không")
    arrow(d, [mid(k, "r"), (2040, 272), (2040, bar_y - 9)], color=MUTED, width=4); d.line([mid(s, "r"), (2040, 452)], fill=MUTED, width=4)
    boxes = []
    for i, (t, sub, c) in enumerate(acts):
        x = 260 + i * 530
        bb = (x, 780, x + 480, 1010); boxes.append(bb)
        box(d, bb, t, sub, fill=c, txt="#FFFFFF" if c != GOLD else INK, ts=TS + 4, ss=SS)
        arrow(d, [((bb[0] + bb[2]) / 2, bar_y + 7), mid(bb, "t")])
    d.rectangle((260, 1100 - 7, 2340, 1100 + 7), fill=INK)
    for bb in boxes: arrow(d, [mid(bb, "b"), ((bb[0] + bb[2]) / 2, 1091)])
    m = (260, 1200, 1180, 1400); box(d, m, "Tiền, trí tuệ, XP tăng", "ngân hàng, công ty chốt sổ mỗi 7 ngày", ts=TS, ss=SS)
    sl = (1420, 1200, 2340, 1400); box(d, sl, "Ngủ 8 giờ", "hồi sức khoẻ, sang ngày mới, lưu tiến trình", fill=INK, txt="#FFFFFF", ts=TS, ss=SS)
    arrow(d, [(720, 1107), mid(m, "t")]); arrow(d, [mid(m, "r"), mid(sl, "l")])
    arrow(d, [mid(sl, "r"), (2500, 1300), (2500, 175), (410, 175), (410, 270)], color=MUTED, width=4, dash=True)
    label(d, 2500, 760, "ngày mới", 30, MUTED)
    d.text((260, 1470), "Ngất khi sức khoẻ về 0: tỉnh lại trên giường phòng khám.", font=font(32), fill=MUTED)
    d.text((260, 1520), "Tải lại bản lưu không bao giờ làm ngất (PlayerStatus.WakeSafely).", font=font(32), fill=MUTED)
    img.save(OUT / "d51_daily_loop.png")


def use_cases():
    img, d = canvas(2600, 1880, "Sơ đồ use case", "Online và phòng học với giáo viên chỉ dành cho tài khoản đã đăng ký")
    sysb = (520, 180, 2060, 1840)
    d.rounded_rectangle(sysb, 30, outline=INK, width=5)
    d.text((1290, 222), "Hệ thống NihongoLife", font=font(42, True), fill=INK, anchor="mm")
    left = ["Chọn ngôn ngữ, nhân vật", "Khám phá 13 khu vực", "Hội thoại học tập", "Làm ca, thăng chức",
            "Học bài, video, thi thử", "Khám bệnh, mua thuốc", "Mua sắm, ngân hàng", "Nông trại, câu cá, bơi"]
    right = ["Chat khu vực, emoji", "Bạn bè, nhóm", "Voice, gọi riêng", "Luyện nói với giáo viên", "Bảng xếp hạng", "Lưu đám mây"]
    E = {}
    for i, t in enumerate(left):
        cy = 320 + i * 190
        e = (560, cy - 72, 1270, cy + 72); E[t] = e
        d.ellipse(e, fill=SOFT, outline=BLUE, width=4)
        centered(d, (e[0] + e[2]) / 2, cy, wrap(d, t, font(34, True), 600), font(34, True), INK, 40)
    for i, t in enumerate(right):
        cy = 400 + i * 230
        e = (1320, cy - 76, 2020, cy + 76); E[t] = e
        d.ellipse(e, fill=PALE_BLUE, outline=INK, width=4)
        centered(d, (e[0] + e[2]) / 2, cy, wrap(d, t, font(34, True), 590), font(34, True), INK, 40)
    stick(d, 240, 560, "Khách", "chơi offline")
    stick(d, 240, 1270, "Tài khoản", "đăng ký email")
    arrow(d, [(240, 1150), (240, 790)], color=INK, width=4, head=26)  # generalisation: an account is also a player
    d.polygon([(240, 760), (218, 800), (262, 800)], fill=PAPER, outline=INK)
    for t in left: d.line([(300, 520), (E[t][0], (E[t][1] + E[t][3]) / 2)], fill="#7C8AA5", width=2)
    for t in right: d.line([(300, 1240), (E[t][0] + 40, (E[t][1] + E[t][3]) / 2)], fill="#9AA6BA", width=2)
    stick(d, 2330, 640, "Giáo viên", "lớp video")
    stick(d, 2330, 1340, "Dịch vụ ngoài", "Supabase, Agora")
    dashed(d, (2272, 700), (E["Luyện nói với giáo viên"][2], 1090), BLUE, 3)
    for t in ("Chat khu vực, emoji", "Voice, gọi riêng", "Lưu đám mây", "Bảng xếp hạng"):
        dashed(d, (2272, 1310), (E[t][2], (E[t][1] + E[t][3]) / 2), BLUE, 3)
    img.save(OUT / "d52_use_case_final.png")


def lesson_sequence():
    L = [("P", "Người chơi"), ("HUB", "Lớp học (StudyHubPopup)"), ("SCH", "Trường học (CoursePopup)"),
         ("VID", "Video bài giảng"), ("EX", "Thi thử"), ("PR", "Trí tuệ, lưu")]
    M = [("P", "HUB", "F ở bàn đầu lớp hoặc phím `", "call"),
         ("HUB", "P", "chọn ngôn ngữ: Nhật, Anh, Nga, Việt", "ret"),
         ("HUB", "SCH", "OpenSchool(ngôn ngữ)", "call"),
         ("SCH", "VID", "nút VIDEO của bài", "call"),
         ("VID", "P", "phát trong game / YouTube", "ret"),
         ("P", "SCH", "làm bài: chọn, nối, sắp xếp, gõ", "call"),
         ("SCH", "PR", "bài đạt: cộng trí tuệ", "call"),
         ("P", "EX", "OpenExams(): JLPT, IELTS, TORFL", "call"),
         ("EX", "PR", "điểm, nhận xét, lịch sử", "call"),
         ("PR", "P", "mở khu vực, điều kiện thăng chức", "ret")]
    sequence("d53_seq_lesson", "Tuần tự: một buổi học trong lớp", "Chọn ngôn ngữ trước, rồi bài học, video, thi thử của ngôn ngữ đó",
             L, M, frames=[(7, 8, "opt  [thi thử]", None)], W=1500)


def languages():
    img, d = canvas(2600, 1500, "Các ngôn ngữ trong NihongoLife", "Giao diện đổi lúc nào cũng được; ngôn ngữ học chọn trong Lớp học")
    cols = [("Giao diện (4)", BLUE, [("Tiếng Việt", "mặc định, tiền ₫"), ("English", "tiếng Anh, tiền yên"), ("Tiếng Nhật", "kana, kanji"), ("Русский", "tiếng Nga")]),
            ("Ngôn ngữ học", TEAL, [("Tiếng Nhật (chính)", "cốt truyện N5 → N1, 40 bài"), ("Tiếng Anh", "40 bài, 34 video, IELTS"),
                                    ("Tiếng Nga", "53 bước, 40 bài, 64 video"), ("Tiếng Việt", "40 bài cho người nước ngoài")]),
            ("Luyện thi (đề tự soạn)", GOLD, [("JLPT N5 → N1", "đọc, nghe, từ vựng, ngữ pháp"), ("IELTS", "4 kỹ năng, AI chấm tuỳ chọn"),
                                              ("TORFL", "định dạng thi tiếng Nga"), ("Chứng nhận trong game", "không phải chứng chỉ chính thức")])]
    for i, (head, c, items) in enumerate(cols):
        x = 70 + i * 850
        d.rounded_rectangle((x, 180, x + 800, 300), 22, fill=c)
        d.text((x + 400, 240), head, font=font(46, True), fill="#FFFFFF" if c != GOLD else INK, anchor="mm")
        for j, (t, s) in enumerate(items):
            y = 340 + j * 270
            box(d, (x, y, x + 800, y + 240), t, s, fill=PAPER, border=c, ts=42, ss=34)
    d.text((70, 1430), "Nguồn: Resources/Course, video_courses.json, Docs/ENGLISH_COURSE.md, Docs/RUSSIAN_PATH.md.", font=font(30), fill=MUTED)
    img.save(OUT / "d54_languages.png")


def phu_quoc_roadmap():
    img, d = canvas(2600, 1720, "Phú Quốc: đã làm và hướng phát triển", "Dữ liệu OpenStreetMap, tỉ lệ 1:100; quán Gánh Ngon là nơi làm của nghề Nhà hàng & dịch vụ")
    phases = [("Giai đoạn 1 · đã có (10–11/2026)", GREEN, PALE_TEAL,
               ["Đảo dựng từ 31 điểm OpenStreetMap", "Dương Đông: hẻm, chợ, chợ đêm, biển, hồ bơi", "Gánh Ngon: 9 món, 18 mẫu câu, 3 thứ tiếng",
                "Nghề Nhà hàng 6 bậc, khách Nhật", "Xe tour 31 điểm, chương 6 cốt truyện"]),
              ("Giai đoạn 2 · ba khu", GOLD, PALE_GOLD,
               ["Bắc: nghỉ dưỡng, Bãi Dài, Gành Dầu", "Trung tâm: Dương Đông chi tiết 1:1", "Nam: An Thới, Bãi Sao, cáp treo Hòn Thơm",
                "Bờ biển từ dữ liệu OSM"]),
              ("Giai đoạn 3 · nghề, cộng đồng", BLUE, PALE_BLUE,
               ["Hướng dẫn viên tiếng Nhật, lễ tân resort, giáo viên", "Dẫn đoàn khách Nhật tham quan", "Đặc sản: nước mắm, tiêu, ngọc trai",
                "Gặp bạn bè online ở đảo"])]
    for i, (head, c, pale, items) in enumerate(phases):
        x = 70 + i * 850
        d.rounded_rectangle((x, 180, x + 800, 1580), 26, fill=pale, outline=c, width=5)
        d.rounded_rectangle((x, 180, x + 800, 320), 26, fill=c)
        centered(d, x + 400, 250, wrap(d, head, font(40, True), 740), font(40, True), "#FFFFFF" if c != GOLD else INK, 48)
        y = 370
        for t in items:
            d.ellipse((x + 40, y + 14, x + 66, y + 40), fill=c)
            for ln in wrap(d, t, font(36), 680):
                d.text((x + 90, y), ln, font=font(36), fill=INK); y += 48
            y += 44
        if i < 2: arrow(d, [(x + 805, 880), (x + 845, 880)], color=INK, width=6, head=20)
    d.text((70, 1630), "Nguồn: Docs/PHU_QUOC.md, Docs/Expansion/PhuQuoc/README.md. Tên thương hiệu chỉ dùng khi được phép.", font=font(30), fill=MUTED)
    img.save(OUT / "d55_phuquoc_roadmap.png")


if __name__ == "__main__":
    for f in (journey, daily, use_cases, lesson_sequence, languages, phu_quoc_roadmap):
        f()
        print("ok", f.__name__)
