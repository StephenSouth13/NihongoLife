"""Diagrams for the 2026-10 Capstone report (v4): phases, Midori Island, farming, jobs, quests, economy,
HUD/cursor policy, architecture, use case, data model, test results. Same visual language as diagrams.py."""
import math
from PIL import Image, ImageDraw

from diagrams import OUT, INK, GOLD, RED, MUTED, LINE, PAPER, SOFT, GREEN, font, wrap, arrow, flow, node
from diagrams2 import canvas, centered, dashed, label_on, sequence, state_diagram, table_box, BLUE

TEAL = "#2E7D6B"
CREAM = "#FBF6EA"


def sub_color(fill):
    h = fill.lstrip("#")
    r, g, b = int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)
    return "#4A5568" if (0.299 * r + 0.587 * g + 0.114 * b) > 150 else "#DCE3EE"


def box(d, b, title, sub=None, fill=PAPER, border=INK, txt=INK, r=22, ts=28, ss=22):
    d.rounded_rectangle(b, r, fill=fill, outline=border, width=4)
    x1, y1, x2, y2 = b
    lines = wrap(d, title, font(ts, True), x2 - x1 - 30)
    subs = wrap(d, sub, font(ss), x2 - x1 - 30) if sub else []
    h = len(lines) * (ts + 8) + len(subs) * (ss + 7)
    y = (y1 + y2) / 2 - h / 2
    for ln in lines:
        d.text(((x1 + x2) / 2, y), ln, font=font(ts, True), fill=txt, anchor="ma"); y += ts + 8
    for ln in subs:
        d.text(((x1 + x2) / 2, y), ln, font=font(ss), fill=sub_color(fill), anchor="ma"); y += ss + 7


def mid(b, side):
    x1, y1, x2, y2 = b
    return {"r": (x2, (y1 + y2) / 2), "l": (x1, (y1 + y2) / 2), "b": ((x1 + x2) / 2, y2), "t": ((x1 + x2) / 2, y1)}[side]


# ─────────── 7 phases ───────────

def phases():
    W, H = 2600, 900
    img, d = canvas(W, H, "Tiến trình phát triển theo 7 Phase", "Mỗi Phase chỉ được ghi “hoàn thành” khi có kiểm thử Play Mode và ảnh chụp minh chứng")
    P = [("1", "HUD & khung UI", "HUD dùng chung, nút ×, ảnh vật phẩm, thể lực"),
         ("2", "Điều khiển & camera", "ESC đóng cửa sổ trên cùng, O cài đặt, Tab hồ sơ 3D, camera"),
         ("3", "Luyện thi", "Engine IELTS local, Listening 40 câu; JLPT/IELTS rút gọn có nhãn phạm vi"),
         ("4", "Tuyến tàu", "Hibari ↔ Gakuen-mae ↔ Minato ↔ Midori; vé, soát vé, chiều về"),
         ("5", "Đảo Midori", "Trồng trọt, chăn nuôi, cửa hàng, sổ tay từ vựng JA/EN"),
         ("6", "Đồng bộ hình ảnh", "Hướng chữ 3D, chồng lớp UI, nhãn tên NPC"),
         ("7", "Đời sống & sự nghiệp", "HUD gọn, chuột khoá + Ctrl, việc làm, nhiệm vụ N, level")]
    bw, gap = 330, 30
    left = (W - (7 * bw + 6 * gap)) / 2
    y = 260
    for i, (n, t, s) in enumerate(P):
        x = left + i * (bw + gap)
        b = (x, y, x + bw, y + 470)
        d.rounded_rectangle(b, 26, fill=PAPER, outline=INK, width=4)
        d.rounded_rectangle((x, y, x + bw, y + 110), 26, fill=INK)
        d.rectangle((x, y + 80, x + bw, y + 110), fill=INK)
        d.text((x + bw / 2, y + 55), f"PHASE {n}", font=font(30, True), fill=GOLD, anchor="mm")
        centered(d, x + bw / 2, y + 175, wrap(d, t, font(28, True), bw - 40), font(28, True), INK, 36)
        centered(d, x + bw / 2, y + 320, wrap(d, s, font(22), bw - 44), font(22), MUTED, 30)
        d.rounded_rectangle((x + 60, y + 410, x + bw - 60, y + 450), 18, fill="#E7F6EE", outline=GREEN, width=3)
        d.text((x + bw / 2, y + 430), "đã kiểm thử", font=font(21, True), fill="#1D6B47", anchor="mm")
        if i < 6:
            arrow(d, [(x + bw + 4, y + 235), (x + bw + gap - 4, y + 235)], color=GOLD, width=5, head=14)
    d.text((W / 2, 800), "Phase 5 (Midori) và Phase 7 (việc làm, nhiệm vụ, level) là trọng tâm của đợt hoàn thiện tháng 10/2026",
           font=font(24), fill=MUTED, anchor="mm")
    img.save(OUT / "d30_phases.png")


# ─────────── World + train line ───────────

def world_v2():
    W, H = 2950, 1500
    img, d = canvas(W, H, "Thế giới NihongoLife: thành phố trung tâm, các khu vực và tuyến tàu tới Đảo Midori",
                    "Khu vực được nạp chồng (additive) lên thành phố; đi tàu thì chuyển thẳng giữa các khu vực (TransferZone)")
    hub = (980, 560, 1620, 860)
    box(d, hub, "90_TestSandbox · Hibari-chō", "Thành phố trung tâm: HUD, dịch vụ, người chơi, konbini Hibari Mart, việc làm thêm ở konbini", fill=GOLD, border=GOLD, txt=INK, ts=30)
    zones = {
        "home": ((120, 260, 640, 470), "45_HomeBedroom", "Phòng trọ: ngủ, tủ lạnh, góc học"),
        "school": ((120, 620, 640, 830), "40_HIBARICLASS", "Lớp học, bàn thi JLPT/IELTS"),
        "game": ((120, 980, 640, 1190), "50_GameCenter", "Kana Match, quầy đổi quà"),
        "station": ((1960, 260, 2480, 470), "20_StationDistrict", "Ga Hibari: Kimura, máy bán vé, sân ga 2"),
        "sushi": ((1960, 620, 2480, 830), "30_SushiRestaurant", "Nhà hàng + việc phục vụ bàn"),
        "midori": ((1960, 980, 2480, 1190), "60_MidoriIsland", "Nông trại, chuồng thú, cửa hàng Midori, cô Hana"),
    }
    for k, (b, t, s) in zones.items():
        box(d, b, t, s, fill=INK if k != "midori" else TEAL, border=INK if k != "midori" else TEAL, txt="#FFFFFF", ts=27)
    for k in ("home", "school", "game"):
        arrow(d, [mid(zones[k][0], "r"), mid(hub, "l")], color=MUTED, width=4)
        arrow(d, [mid(hub, "l"), mid(zones[k][0], "r")], color=MUTED, width=4)
    for k in ("station", "sushi"):
        arrow(d, [mid(hub, "r"), mid(zones[k][0], "l")], color=MUTED, width=4)
    # train line
    st = zones["station"][0]; su = zones["sushi"][0]; mi = zones["midori"][0]
    x = st[2] + 40
    d.line([(x, mid(st, "r")[1]), (x, mid(mi, "r")[1])], fill=RED, width=8)
    for b, lbl in [(st, "Hibari"), (su, "Minato ¥320"), (mi, "Midori ¥450")]:
        y = mid(b, "r")[1]
        d.ellipse((x - 16, y - 16, x + 16, y + 16), fill=PAPER, outline=RED, width=6)
        d.line([(b[2], y), (x - 16, y)], fill=RED, width=4)
        d.text((x + 30, y), lbl, font=font(24, True), fill=RED, anchor="lm")
    gy = (mid(st, "r")[1] + mid(su, "r")[1]) / 2
    d.ellipse((x - 12, gy - 12, x + 12, gy + 12), fill=RED)
    d.text((x + 30, gy), "Gakuen-mae ¥180", font=font(22, True), fill=RED, anchor="lm")
    label_on(d, 1900, 1260, "Tuyến tàu sân ga 2 · chiều về từ đảo: máy bán vé → soát vé ở cửa tàu → ga Hibari", 22, RED)
    d.text((W / 2, 1400), "Cổng ScenePortal đưa về đúng cửa ở thành phố; SceneZoneVisibility ẩn thành phố khi ở trong khu vực",
           font=font(23), fill=MUTED, anchor="mm")
    img.save(OUT / "d31_world_v2.png")


# ─────────── Farming ───────────

def farm_loop():
    steps = [("Nhận / mua hạt", "Bộ khởi đầu, cửa hàng Midori (P) hoặc ca làm của cô Hana", "start"),
             ("Xới đất", "cuốc 1,6 s · xẻng 2,6 s · tay không 7 s", "step"),
             ("Gieo hạt", "chọn loại hạt có trong balo · 1,2 s", "step"),
             ("Tưới nước", "cần bình tưới · 1,5 s", "ui"),
             ("Cây lớn theo thời gian", "secondsPerStage trong catalog, tính bằng giờ UTC", "step"),
             ("Khát nước", "mỗi giai đoạn cần tưới lại (3 giai đoạn)", "step"),
             ("Chín → thu hoạch", "1,4 s · sản lượng theo catalog", "ui"),
             ("Vào balo", "bán ở cửa hàng, cho thú ăn hoặc giao cho nhiệm vụ", "end")]
    flow("d32_farm_loop", "Quy trình trồng trọt trên Đảo Midori", steps, per=4,
         branches={3: ("Không có bình tưới", "không mang nước được → mua bình ở cửa hàng"), 1: ("Không có cuốc", "vẫn xới được bằng tay, chậm gấp ~4 lần")}, W=2100)


def farm_state():
    S = {"un": ("Chưa xới", "cỏ, đất cứng", (380, 470), "start"),
         "til": ("Đã xới", "chờ gieo hạt", (1100, 470), "normal"),
         "water": ("Cần tưới", "giai đoạn 0–2 chưa được tưới", (1820, 470), "hot"),
         "grow": ("Đang lớn", "đếm theo stageStartTicks", (1820, 1000), "normal"),
         "ready": ("Chín", "giai đoạn 3 — thu hoạch được", (1100, 1000), "good"),
         }
    T = [("un", "til", "Xới (Till)", 26), ("til", "water", "Gieo (Plant)", 26), ("water", "grow", "Tưới (Water)", 26),
         ("grow", "water", "hết giờ, stage < 3", 26), ("grow", "ready", "hết giờ, stage = 3", 0),
         ("ready", "un", "Thu hoạch (Harvest)", 0), ("water", "til", "Dọn ô (Clear)", -0.001)]
    img, d, B = state_diagram("d33", "Máy trạng thái một ô ruộng (FarmPlot)", "Trạng thái lưu trong FarmPlotRecord nên cây vẫn lớn khi người chơi rời đảo", S, T, W=2300, H=1300)
    arrow(d, [(92, img.height / 2), (B["un"][0] - 6, 470)], color=INK, width=4)
    img.save(OUT / "d33_farm_state.png")


# ─────────── Jobs / quests ───────────

def job_sequence():
    L = [("P", "Người chơi"), ("TJ", "TaskJournalUI (N)"), ("QS", "QuestService"), ("JS", "JobStation"), ("TA", "TimedAction"),
         ("QC", "JobQuizCard"), ("NPC", "NPCController (chị Ito)"), ("INV", "PlayerInventory")]
    M = [("P", "TJ", "N → chọn “Làm thêm ở Hibari Mart” → Nhận ca", "call"),
         ("TJ", "QS", "Accept(job_konbini_shift)", "call"),
         ("QS", "TJ", "trạng thái active, theo dõi trên HUD", "ret"),
         ("P", "JS", "F ở kho → lấy thùng hàng", "call"),
         ("JS", "TA", "Run(\"Đang bê thùng hàng…\", 1,2 s)", "call"),
         ("P", "JS", "F ở kệ (đang cầm thùng)", "call"),
         ("JS", "QS", "Raise(\"restock\", \"konbini_shelf\")", "call"),
         ("P", "JS", "F với khách → câu hỏi tiếng Nhật", "call"),
         ("JS", "QC", "Show(câu hỏi, 4 lựa chọn)", "call"),
         ("QC", "P", "sai → giải thích, không tính", "ret"),
         ("QC", "QS", "đúng → Raise(\"assist\")", "call"),
         ("P", "NPC", "báo cáo với chị Ito", "call"),
         ("NPC", "QS", "Raise(\"talk\", \"npc_cashier\")", "call"),
         ("QS", "QS", "đánh dấu rewardClaimed, lưu", "self"),
         ("QS", "INV", "AddYen(600) · AddExp(40) · AddKnowledge(6)", "call")]
    sequence("d34_seq_job", "Sơ đồ tuần tự — một ca làm thêm ở konbini", "Mỗi bước là một hành động thật; lương chỉ trả sau khi trạng thái đã được đánh dấu và lưu",
             L, M, frames=[(9, 10, "alt  [trả lời sai | đúng]", 10)], W=2700)


def quest_state():
    S = {"locked": ("Chưa mở", "thiếu cấp, kiến thức hoặc nhiệm vụ trước", (380, 470), "normal"),
         "avail": ("Có thể nhận", "hiện trong sổ nhiệm vụ", (1150, 470), "start"),
         "active": ("Đang làm", "sự kiện gameplay cộng tiến độ", (1920, 470), "hot"),
         "done": ("Hoàn thành", "trả thưởng đúng một lần", (1920, 1000), "good"),
         "cool": ("Chờ ca sau", "repeatable + cooldownMinutes", (1150, 1000), "normal")}
    T = [("locked", "avail", "đủ yêu cầu", 26), ("avail", "active", "Accept", 26), ("active", "avail", "Abandon (không thưởng)", 26),
         ("active", "done", "mọi mục tiêu đạt", 0), ("done", "cool", "nếu lặp lại", 0), ("cool", "avail", "hết thời gian chờ", 0)]
    img, d, B = state_diagram("d35", "Vòng đời một nhiệm vụ / ca làm (QuestService)", "Định nghĩa trong progression.json; trạng thái người chơi trong PlayerProgressDto.quests", S, T, W=2350, H=1300)
    arrow(d, [(92, img.height / 2), (B["locked"][0] - 6, 470)], color=INK, width=4)
    img.save(OUT / "d35_quest_state.png")


def progression_data():
    W, H = 2600, 1400
    img, d = canvas(W, H, "Tiến trình hướng dữ liệu: một nguồn sự thật cho nhiệm vụ, phần thưởng và cấp độ",
                    "Sửa JSON → chạy ProgressionCatalogTests → sổ nhiệm vụ, HUD và phần thưởng cập nhật, không sửa script")
    data = (100, 230, 700, 560)
    box(d, data, "Resources/Progression/progression.json", "levels.xpToNext · quests[] (job, farm, learning, daily) · objectives · rewards · grantOnAccept · cooldown", fill=GOLD, border=GOLD, txt=INK, ts=26)
    cat = (100, 700, 700, 960)
    box(d, cat, "ProgressionCatalog", "Load() · Validate(): trùng id, sự kiện lạ, thưởng âm, vòng phụ thuộc, vật phẩm không tồn tại", ts=26)
    arrow(d, [mid(data, "b"), mid(cat, "t")])
    qs = (1000, 560, 1600, 860)
    box(d, qs, "QuestService", "Accept · Abandon · Track · Raise(event, target) · Complete → thưởng một lần", fill=INK, border=INK, txt="#FFFFFF", ts=30)
    arrow(d, [mid(cat, "r"), (900, mid(cat, "r")[1]), (900, 760), mid(qs, "l")])
    srcs = [("FarmPlot", "till · plant · water · harvest"), ("IslandAnimal", "feed · pet"), ("Cửa hàng", "buy · sell"),
            ("NPCController / JobGiver", "talk (báo cáo)"), ("JobStation", "restock · assist · checkout · order · serve"), ("IslandState", "learn (từ mới)")]
    for i, (t, s) in enumerate(srcs):
        b = (1000 + (i % 3) * 210 - 0, 200 + (i // 3) * 170, 1000 + (i % 3) * 210 + 190, 350 + (i // 3) * 170)
    for i, (t, s) in enumerate(srcs):
        x = 960 + i * 270
        b = (x - 0, 1050, x + 250, 1250)
        box(d, b, t, s, fill=SOFT, border=BLUE, ts=22, ss=19)
        arrow(d, [mid(b, "t"), (mid(b, "t")[0], 960), (1300, 960), mid(qs, "b")], color=BLUE, width=3, head=14)
    outs = [((1900, 230, 2500, 400), "PlayerProgressDto.quests", "trạng thái, tiến độ, rewardClaimed, cooldown"),
            ((1900, 470, 2500, 640), "PlayerInventory · PlayerStatus", "¥ vào ví chung · XP → cấp · kiến thức riêng"),
            ((1900, 710, 2500, 880), "HUD + Sổ nhiệm vụ (N)", "chip mục tiêu theo dõi · danh sách · phần thưởng")]
    for b, t, s in outs:
        box(d, b, t, s, fill=PAPER, border=GREEN, ts=24, ss=20)
        arrow(d, [mid(qs, "r"), (1750, mid(qs, "r")[1]), (1750, mid(b, "l")[1]), mid(b, "l")], color=GREEN, width=4)
    label_on(d, 1300, 1310, "Mỗi nơi phát sinh hành động chỉ gọi QuestService.Raise(...) — QuestService quyết định mục tiêu nào được cộng", 22, MUTED)
    img.save(OUT / "d36_progression_data.png")


def economy():
    W, H = 2600, 1300
    img, d = canvas(W, H, "Dòng tiền trong game: một ví ¥ duy nhất", "Mọi nguồn thu và khoản chi đi qua PlayerInventory.Yen; không có tiền tệ thứ hai")
    wallet = (1050, 520, 1550, 800)
    d.ellipse(wallet, fill=GOLD, outline=GOLD)
    centered(d, 1300, 660, ["Ví ¥", "PlayerInventory.Yen"], font(34, True), INK, 46)
    ins = [("Làm thêm konbini", "+¥600 / ca"), ("Phục vụ sushi", "+¥700 / ca"), ("Phụ việc nông trại", "+¥500 / ca"),
           ("Bán nông sản", "cà rốt ¥35 · cà chua ¥50 · ngô ¥65 · bí ¥90"), ("Thưởng cốt truyện", "rewardYen của scenario")]
    outs = [("Đồ ăn, nước (konbini)", "giá theo KonbiniCatalog"), ("Vé tàu", "¥180 · ¥320 · ¥450"), ("Hạt giống, dụng cụ", "hạt ¥40–100 · cuốc ¥200"),
            ("Nhà hàng sushi", "trả theo hoá đơn"), ("Đồ công nghệ", "laptop ¥1.200 · TV ¥1.500")]
    for i, (t, s) in enumerate(ins):
        b = (90, 200 + i * 200, 690, 350 + i * 200)
        box(d, b, t, s, fill="#E7F6EE", border=GREEN, txt="#1D6B47", ts=26, ss=21)
        arrow(d, [mid(b, "r"), (mid(wallet, "l")[0] - 10, 660 + (i - 2) * 40)], color=GREEN, width=4)
    for i, (t, s) in enumerate(outs):
        b = (1910, 200 + i * 200, 2510, 350 + i * 200)
        box(d, b, t, s, fill="#FDECEA", border=RED, txt=RED, ts=26, ss=21)
        arrow(d, [(mid(wallet, "r")[0] + 10, 660 + (i - 2) * 40), mid(b, "l")], color=RED, width=4)
    centered(d, 1300, 1000, ["Chống trùng: lương chỉ trả khi rewardClaimed chưa đặt; mua thất bại thì hoàn tiền;",
                             "ca làm không yêu cầu cấp/kiến thức nên người chơi mới luôn có cách kiếm tiền đầu tiên."], font(24), MUTED, 36)
    img.save(OUT / "d37_economy.png")


# ─────────── HUD & cursor ───────────

def hud_layout():
    W, H = 2600, 1600
    img, d = canvas(W, H, "Chính sách bố cục HUD (1920×1080 tham chiếu, kiểm tra tới 1280×720)",
                    "Mỗi vùng màn hình có một chủ; thông báo không bao giờ đè lên gợi ý tương tác hay cửa sổ")
    S = (240, 200, 2360, 1393)
    d.rectangle(S, fill="#D8E3D3", outline=INK, width=5)
    def zone(b, t, s, fill=INK, txt="#FFFFFF"):
        box(d, b, t, s, fill=fill, border=fill, txt=txt, ts=26, ss=20, r=18)
    zone((270, 230, 860, 380), "Mục tiêu đang theo dõi", "1 dòng · N mở sổ nhiệm vụ")
    zone((1000, 230, 1600, 330), "Thanh khu vực", "bước đi tàu / thanh công cụ đảo", fill=TEAL)
    zone((1000, 350, 1600, 470), "Thông báo (HudFeed)", "tối đa 3 · cùng mục tiêu cập nhật tại chỗ", fill=BLUE)
    zone((2060, 230, 2330, 320), "Tên NPC", "bật/tắt", fill=MUTED)
    zone((1750, 520, 2330, 1000), "Thẻ ngữ cảnh", "ô ruộng, con vật — bên phải, không che nhân vật", fill="#3D5A4C")
    zone((960, 600, 1640, 1000), "Cửa sổ trung tâm", "Sổ nhiệm vụ, cửa hàng, cài đặt, câu hỏi khách — con trỏ tự hiện", fill="#24324A")
    zone((970, 1160, 1630, 1270), "Làn gợi ý duy nhất", "[F] > Tiếp hội thoại · R > gợi ý; ẩn khi đang làm việc", fill=GOLD, txt=INK)
    zone((270, 1180, 860, 1360), "Ô trạng thái gọn", "chân dung, cấp, ¥, 5 vòng nhu cầu; số liệu chi tiết ở Tab")
    zone((270, 1050, 760, 1140), "Cảnh báo nhu cầu", "chỉ khi < 20%", fill=RED)
    d.text((W / 2, 1460), "Đã bỏ thanh phím B/Tab/M/J/K/O cố định trên desktop — danh sách phím nằm trong Cài đặt → Điều khiển; màn hình cảm ứng vẫn giữ nút.",
           font=font(23), fill=MUTED, anchor="mm")
    img.save(OUT / "d38_hud_layout.png")


def cursor_state():
    S = {"look": ("Chơi (khoá chuột)", "con trỏ ẩn, rê chuột xoay camera", (420, 470), "start"),
         "ctrl": ("Giữ Ctrl", "con trỏ hiện, camera dừng", (1250, 470), "normal"),
         "ui": ("Cửa sổ / hội thoại", "con trỏ hiện, nhân vật đứng yên", (1250, 1000), "normal"),
         "lost": ("Mất focus", "nhả chuột (alt-tab, đổi tab trình duyệt)", (2050, 470), "hot"),
         "sims": ("Kiểu Click để đi", "con trỏ luôn hiện; kéo chuột phải để xoay", (2050, 1000), "good")}
    T = [("look", "ctrl", "nhấn giữ Ctrl", 26), ("ctrl", "look", "thả Ctrl (bỏ 1 khung delta)", 26),
         ("look", "ui", "Tab · N · B · O · F với NPC", 26), ("ui", "look", "Esc / × đóng", 26),
         ("look", "lost", "cửa sổ mất focus", 0, 0.3), ("lost", "look", "quay lại", -0.001, 0.7),
         ("look", "sims", "Cài đặt → kiểu điều khiển", 0, 0.62)]
    img, d, B = state_diagram("d39", "Chế độ con trỏ và camera (CursorDirector)", "Một thành phần duy nhất quyết định trạng thái con trỏ, chạy sau mọi script khác trong khung hình", S, T, W=2500, H=1300)
    arrow(d, [(92, img.height / 2), (B["look"][0] - 6, 470)], color=INK, width=4)
    img.save(OUT / "d39_cursor_state.png")


# ─────────── Architecture v2 ───────────

def architecture_v2():
    W, H = 2600, 1700
    img, d = canvas(W, H, "Kiến trúc phân lớp của NihongoLife (10/2026)", "Lớp trên chỉ phụ thuộc lớp dưới; dịch vụ tìm qua GameServices; nội dung nằm trong dữ liệu")
    layers = [
        ("Trình bày (UI)", INK, ["HUDUI", "StatusDock", "TaskJournalUI", "IslandUI", "SettingsUI", "DialogueView", "IeltsTestUI", "HudFeed · TimedAction"]),
        ("Gameplay", BLUE, ["ScenarioManager", "QuestService", "JobStation · JobGiver", "FarmPlot · IslandAnimal", "StationTravelController", "RestaurantTable", "MiniGame (Kana Match)", "PlayerController"]),
        ("Dịch vụ (GameServices)", TEAL, ["SceneFlowController", "GameInputService", "CursorDirector", "UiModalStack", "IProgressRepository", "AudioService", "OnlineWorldService", "ExamRepository"]),
        ("Dữ liệu & nội dung", GOLD, ["ScenarioDefinition ×14", "progression.json", "island_catalog.json", "RestaurantMenuDefinition", "KonbiniCatalog", "ExamDefinition", "Gói IELTS local (ngoài Assets)", "Resources/Items"]),
        ("Nền tảng", MUTED, ["Unity 6000.3 · URP", "Input System", "TextMeshPro", "Supabase (auth, progress_json, chat)", "Gemini (chấm viết/nói)", "Agora (gọi thoại)"]),
    ]
    y = 200
    for name, col, items in layers:
        d.rounded_rectangle((80, y, W - 80, y + 250), 24, fill=PAPER, outline=col, width=5)
        d.rounded_rectangle((80, y, 420, y + 250), 24, fill=col)
        d.rectangle((380, y, 420, y + 250), fill=col)
        centered(d, 250, y + 125, wrap(d, name, font(30, True), 300), font(30, True), "#FFFFFF" if col != GOLD else INK, 40)
        cols = 4
        cw = (W - 80 - 460 - 30 * (cols - 1)) / cols
        for i, it in enumerate(items):
            r, c = divmod(i, cols)
            x = 460 + c * (cw + 30)
            yy = y + 30 + r * 105
            d.rounded_rectangle((x, yy, x + cw, yy + 85), 16, fill=SOFT, outline=LINE, width=3)
            d.text((x + cw / 2, yy + 42), it, font=font(23, True), fill=INK, anchor="mm")
        y += 290
    img.save(OUT / "d40_architecture_v2.png")


def use_case_v2():
    W, H = 2600, 1700
    img, d = canvas(W, H, "Sơ đồ use case tổng quát", "Người học là tác nhân chính; tác giả nội dung chỉnh dữ liệu và chạy builder; dịch vụ ngoài là tác nhân phụ")
    sysb = (520, 200, 2080, 1640)
    d.rounded_rectangle(sysb, 30, outline=INK, width=5, fill=PAPER)
    d.text((1300, 240), "Hệ thống NihongoLife", font=font(30, True), fill=INK, anchor="mm")
    ucs = ["Chọn nhân vật, bắt đầu", "Hội thoại học tập theo scenario", "Mua sắm ở konbini", "Ăn ở nhà hàng sushi",
           "Mua vé, đi tàu", "Trồng trọt trên Đảo Midori", "Chăm sóc, cho thú ăn", "Nhận và làm ca làm thêm",
           "Theo dõi nhiệm vụ (N)", "Luyện thi JLPT / IELTS", "Chơi Kana Match", "Chỉnh cài đặt, phím, camera",
           "Lưu / đồng bộ tiến trình", "Chat, học cùng bạn (online)"]
    pos = []
    for i, u in enumerate(ucs):
        c, r = divmod(i, 7)
        cx = 960 + c * 680
        cy = 340 + r * 182
        e = (cx - 290, cy - 62, cx + 290, cy + 62)
        d.ellipse(e, fill=SOFT, outline=BLUE, width=4)
        centered(d, cx, cy, wrap(d, u, font(23, True), 480), font(23, True), INK, 30)
        pos.append((cx, cy))
    def actor(cx, cy, name):
        d.ellipse((cx - 30, cy - 120, cx + 30, cy - 60), outline=INK, width=6)
        d.line([(cx, cy - 60), (cx, cy + 20)], fill=INK, width=6)
        d.line([(cx - 50, cy - 30), (cx + 50, cy - 30)], fill=INK, width=6)
        d.line([(cx, cy + 20), (cx - 40, cy + 80)], fill=INK, width=6); d.line([(cx, cy + 20), (cx + 40, cy + 80)], fill=INK, width=6)
        centered(d, cx, cy + 120, wrap(d, name, font(25, True), 360), font(25, True), INK, 32)
    actor(280, 860, "Người học")
    for (cx, cy) in pos[:7]:
        d.line([(330, 840), (cx - 292, cy)], fill=MUTED, width=2)
    for (cx, cy) in pos[7:13]:
        d.line([(330, 840), (cx - 292, cy)], fill=LINE, width=2)
    actor(2330, 600, "Tác giả nội dung")
    d.text((2330, 760), "sửa progression.json,\nscenario, catalog;\nchạy builder", font=font(21), fill=MUTED, anchor="ma", align="center")
    actor(2330, 1250, "Dịch vụ ngoài")
    d.text((2330, 1410), "Supabase · Gemini\n· Agora", font=font(21), fill=MUTED, anchor="ma", align="center")
    for i in (12, 13, 9):
        cx, cy = pos[i]
        dashed(d, (cx + 292, cy), (2270, 1230), BLUE, 3)
    img.save(OUT / "d41_use_case_v2.png")


def erd_v2():
    W, H = 2600, 1500
    img, d = canvas(W, H, "Mô hình dữ liệu tiến trình người chơi (PlayerProgressDto)", "Một đối tượng JSON lưu cục bộ và đồng bộ lên Supabase (player_progress.progress_json)")
    root = table_box(d, 960, 190, 680, "PlayerProgressDto", [("PK playerId", "string"), ("level · xp (tổng)", "int"), ("knowledge", "int"), ("yen", "int"),
                                                              ("health · energy · hunger · thirst", "float"), ("trackedQuestId", "string"), ("activeScenarioId", "string")], INK)
    T = {
        "inv": table_box(d, 80, 220, 600, "InventoryEntry[] (balo)", [("itemId", "string"), ("quantity", "int"), ("priceYen", "int"), ("useType · food · drink", "…")], BLUE),
        "q": table_box(d, 80, 700, 600, "QuestStateRecord[]", [("FK questId → progression.json", "string"), ("status", "active|completed"), ("progress", "int[]"),
                                                             ("rewardClaimed", "bool"), ("cooldownUntilTicks", "long"), ("timesCompleted", "int")], RED),
        "car": table_box(d, 80, 1180, 600, "CareerRecord[]", [("roleId (= id ca làm)", "string"), ("totalShifts", "int"), ("reputation", "int")], BLUE),
        "isl": table_box(d, 1920, 220, 600, "IslandRecord", [("targetLanguage", "ja|en"), ("words", "string[]"), ("animalsMet", "string[]"), ("harvested · sold · earned", "int"), ("starterKitGiven", "bool")], TEAL),
        "plot": table_box(d, 1920, 760, 600, "FarmPlotRecord[]", [("PK plotId", "string"), ("tilled", "bool"), ("cropId", "string"), ("stage 0–3", "int"), ("watered", "bool"), ("stageStartTicks", "long")], TEAL),
        "exam": table_box(d, 1000, 900, 600, "ExamAttemptRecord[] · MasteryRecord[]", [("examId / targetId", "string"), ("điểm, band, thời gian", "…"), ("mức thành thạo", "int")], GOLD),
    }
    for k, side_r, side_k in [("inv", "l", "r"), ("q", "l", "r"), ("car", "l", "r")]:
        p1, p2 = mid(root, side_r), mid(T[k], side_k)
        d.line([p1, (p1[0] - 120, p1[1]), (p1[0] - 120, p2[1]), p2], fill=INK, width=3)
    p1, p2 = mid(root, "r"), mid(T["isl"], "l"); d.line([p1, (p1[0] + 100, p1[1]), (p1[0] + 100, p2[1]), p2], fill=INK, width=3)
    p1, p2 = mid(T["isl"], "b"), mid(T["plot"], "t"); d.line([p1, p2], fill=TEAL, width=3)
    p1, p2 = mid(root, "b"), mid(T["exam"], "t"); d.line([p1, p2], fill=INK, width=3)
    label_on(d, 1300, 1440, "Gộp online/offline: ví và balo theo bản cloud; đảo theo bản tiến xa hơn; nhiệm vụ gộp theo id, bản đã nhận thưởng thắng", 22, MUTED)
    img.save(OUT / "d42_erd_v2.png")


def tests_chart():
    W, H = 2600, 1250
    img, d = canvas(W, H, "Kết quả kiểm thử tự động (10/10/2026, Unity 6000.3.12f1, batchmode)", "EditMode 22/22 đạt · Play Mode 24/24 đạt, 1 bỏ qua (ghi trailer, chỉ chạy khi cần quay)")
    rows = [("Play Mode — thành phố, konbini, bản đồ", 3), ("Play Mode — ga tàu, đi tàu, đến trường", 3), ("Play Mode — phòng trọ, chuyển khu, mở đầu", 4),
            ("Play Mode — Đảo Midori (luồng 17 bước, hiệu năng)", 2), ("Play Mode — việc làm, HUD, con trỏ (LifeLoop)", 3),
            ("Play Mode — Game Center, lớp học, IELTS", 4), ("Play Mode — hội thoại, cảm xúc, ESC, thế giới", 5),
            ("EditMode — kịch bản, IELTS, tiến trình, gộp bản lưu", 22)]
    maxv = 22
    x0, x1 = 980, 2420
    for i, (t, v) in enumerate(rows):
        y = 220 + i * 118
        d.text((x0 - 30, y + 38), t, font=font(25, True if "EditMode" in t else False), fill=INK, anchor="rm")
        w = (x1 - x0) * v / maxv
        d.rounded_rectangle((x0, y + 10, x0 + w, y + 70), 12, fill=GREEN if "EditMode" not in t else BLUE)
        d.text((x0 + w + 16, y + 40), f"{v}/{v} đạt", font=font(25, True), fill=INK, anchor="lm")
    img.save(OUT / "d43_tests.png")


if __name__ == "__main__":
    for f in (phases, world_v2, farm_loop, farm_state, job_sequence, quest_state, progression_data, economy, hud_layout,
              cursor_state, architecture_v2, use_case_v2, erd_v2, tests_chart):
        f(); print("ok", f.__name__)
