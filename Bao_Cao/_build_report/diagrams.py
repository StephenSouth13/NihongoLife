"""Workflow / architecture diagrams for the NihongoLife report and deck (PIL, 2x resolution)."""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).parent / "diag"
OUT.mkdir(exist_ok=True)
F = "C:/Windows/Fonts/"
INK, GOLD, RED, MUTED, LINE, PAPER, SOFT, GREEN = "#14213D", "#F2B233", "#D94F45", "#5B6B82", "#C9D2E0", "#FFFFFF", "#EEF2F8", "#2E9E6B"


def font(size, bold=False):
    return ImageFont.truetype(F + ("segoeuib.ttf" if bold else "segoeui.ttf"), size)


def jfont(size):
    return ImageFont.truetype(F + "YuGothB.ttc", size)


def wrap(d, text, f, width):
    words, lines, cur = text.split(), [], ""
    for w in words:
        t = (cur + " " + w).strip()
        if d.textlength(t, font=f) <= width:
            cur = t
        else:
            if cur:
                lines.append(cur)
            cur = w
    if cur:
        lines.append(cur)
    return lines


def arrow(d, pts, color=INK, width=5, head=18, dash=False):
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        if dash:
            L = math.hypot(x2 - x1, y2 - y1)
            n = max(1, int(L // 22))
            for i in range(0, n, 2):
                a, b = i / n, min(1, (i + 1) / n)
                d.line([(x1 + (x2 - x1) * a, y1 + (y2 - y1) * a), (x1 + (x2 - x1) * b, y1 + (y2 - y1) * b)], fill=color, width=width)
        else:
            d.line([(x1, y1), (x2, y2)], fill=color, width=width)
    (x1, y1), (x2, y2) = pts[-2], pts[-1]
    ang = math.atan2(y2 - y1, x2 - x1)
    p = [(x2, y2), (x2 - head * math.cos(ang - 0.45), y2 - head * math.sin(ang - 0.45)),
         (x2 - head * math.cos(ang + 0.45), y2 - head * math.sin(ang + 0.45))]
    d.polygon(p, fill=color)


STYLE = {
    "start": (GOLD, GOLD, INK),
    "step": (PAPER, INK, INK),
    "ui": (INK, INK, "#FFFFFF"),
    "check": ("#FDECEA", RED, RED),
    "end": (GREEN, GREEN, "#FFFFFF"),
}


def node(d, box, title, sub=None, kind="step", num=None):
    x1, y1, x2, y2 = box
    fill, border, txt = STYLE[kind]
    r = (y2 - y1) // 2 if kind in ("start", "end") else 22
    d.rounded_rectangle(box, r, fill=fill, outline=border, width=4)
    ft, fs = font(30, True), font(23)
    w = x2 - x1 - 40
    lines = wrap(d, title, ft, w)
    sublines = wrap(d, sub, fs, w) if sub else []
    h = len(lines) * 38 + len(sublines) * 30
    y = (y1 + y2) / 2 - h / 2
    for ln in lines:
        d.text(((x1 + x2) / 2, y), ln, font=ft, fill=txt, anchor="ma"); y += 38
    subcol = {"ui": "#C8D3E6", "start": INK, "end": "#E3F6EC", "check": MUTED}.get(kind, MUTED)
    for ln in sublines:
        d.text(((x1 + x2) / 2, y), ln, font=fs, fill=subcol, anchor="ma"); y += 30
    if num is not None:
        cx, cy = x1 + 4, y1 + 4
        d.ellipse((cx - 24, cy - 24, cx + 24, cy + 24), fill=GOLD if kind != "start" else INK, outline=PAPER, width=4)
        d.text((cx, cy), str(num), font=font(24, True), fill=INK if kind != "start" else GOLD, anchor="mm")


def flow(name, title, steps, per=4, branches=None, W=2000):
    """steps: list of (title, sub, kind). branches: {index: (text, sub)} drawn below that node."""
    branches = branches or {}
    bw, bh, gx, gy = 380, 170, 110, 120
    rows = math.ceil(len(steps) / per)
    branch_rows = {i // per for i in branches}
    extra = 210
    H = 150 + rows * bh + (rows - 1) * gy + sum(extra for r in range(rows) if r in branch_rows) + 70
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)
    d.text((60, 50), title, font=font(40, True), fill=INK)
    left = (W - (per * bw + (per - 1) * gx)) / 2
    y = 150
    boxes = []
    for r in range(rows):
        for c in range(per):
            i = r * per + c
            if i >= len(steps):
                break
            x = left + c * (bw + gx)
            boxes.append((x, y, x + bw, y + bh))
        y += bh + gy + (extra if r in branch_rows else 0)
    for i in range(len(boxes) - 1):
        a, b = boxes[i], boxes[i + 1]
        if abs(a[1] - b[1]) < 1:
            arrow(d, [(a[2] + 6, (a[1] + a[3]) / 2), (b[0] - 8, (b[1] + b[3]) / 2)])
        else:
            midy = b[1] - gy / 2
            arrow(d, [((a[0] + a[2]) / 2, a[3] + 6), ((a[0] + a[2]) / 2, midy), ((b[0] + b[2]) / 2, midy), ((b[0] + b[2]) / 2, b[1] - 8)], color=MUTED)
    for i, (t, s, k) in enumerate(steps):
        node(d, boxes[i], t, s, k, num=i + 1)
    for i, (t, s) in branches.items():
        a = boxes[i]
        bx = (a[0] - 10, a[3] + 70, a[2] + 10, a[3] + 70 + 130)
        arrow(d, [((a[0] + a[2]) / 2, a[3] + 6), ((a[0] + a[2]) / 2, bx[1] - 6)], color=RED, dash=True)
        node(d, bx, t, s, "check")
    img.save(OUT / f"{name}.png")
    return img


def core_loop():
    W, H = 2000, 1250
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)
    d.text((60, 50), "Vòng lặp cốt lõi của NihongoLife", font=font(40, True), fill=INK)
    cx, cy, R = W / 2, 680, 430
    steps = [("Khám phá", "Hibari-chō 3D"), ("Nhận mục tiêu", "QuestLog / HUD"), ("Tương tác", "NPC · đồ vật · cổng"),
             ("Chọn câu tiếng Nhật", "Dialogue N5"), ("Phản hồi & sửa lỗi", "Nhánh sai → thử lại"), ("Tiến bộ", "XP · Knowledge · mở khóa")]
    d.ellipse((cx - R, cy - R, cx + R, cy + R), outline=LINE, width=6)
    pts = []
    for i in range(len(steps)):
        a = -math.pi / 2 + i * 2 * math.pi / len(steps)
        pts.append((cx + R * math.cos(a), cy + R * math.sin(a)))
    for i in range(len(steps)):
        a1 = -math.pi / 2 + i * 2 * math.pi / len(steps) + 0.28
        a2 = -math.pi / 2 + (i + 1) * 2 * math.pi / len(steps) - 0.28
        arc = [(cx + R * math.cos(a1 + (a2 - a1) * t / 12), cy + R * math.sin(a1 + (a2 - a1) * t / 12)) for t in range(13)]
        arrow(d, arc, color=GOLD, width=10, head=30)
    for i, ((t, s), (x, y)) in enumerate(zip(steps, pts)):
        node(d, (x - 210, y - 85, x + 210, y + 85), t, s, "ui" if i % 2 == 0 else "step", num=i + 1)
    d.ellipse((cx - 190, cy - 190, cx + 190, cy + 190), fill=GOLD)
    d.text((cx, cy - 70), "日本語", font=jfont(70), fill=INK, anchor="mm")
    d.text((cx, cy + 20), "Học bằng", font=font(34, True), fill=INK, anchor="mm")
    d.text((cx, cy + 65), "cách sống", font=font(34, True), fill=INK, anchor="mm")
    img.save(OUT / "d01_core_loop.png")


def screen_map():
    W, H = 2000, 1300
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)
    d.text((60, 50), "Bản đồ màn hình (UI screen map)", font=font(40, True), fill=INK)
    boot = (80, 170, 430, 300)
    menu = (80, 420, 430, 560)
    char = (80, 700, 430, 840)
    hud = (80, 980, 430, 1120)
    node(d, boot, "00_Bootstrap", "AppRoot · GameServices", "start")
    node(d, menu, "01_MainMenu", "MainMenuUI", "ui")
    node(d, char, "Chọn nhân vật", "PlayableCharacterCatalog", "step")
    node(d, hud, "Gameplay HUD", "HUDUI · Hibari-chō", "end")
    for a, b in [(boot, menu), (menu, char), (char, hud)]:
        arrow(d, [((a[0] + a[2]) / 2, a[3] + 6), ((b[0] + b[2]) / 2, b[1] - 8)])
    menu_items = [("Cách chơi", "GuidePopup"), ("Cài đặt", "SettingsUI"), ("Về tôi", "AboutPopup"), ("Hồ sơ", "PlayerProfileUI"),
                  ("Xếp hạng", "LeaderboardUI"), ("Bạn bè · Online", "FriendsUI · AuthUI")]
    hud_items = [("Hội thoại", "DialogueManager"), ("Nhiệm vụ [J]", "QuestLogPopup"), ("Bản đồ [M]", "WorldMapUI"),
                 ("Balo [B]", "InventoryUI"), ("Nhân vật [Tab]", "StatusUI"), ("Luyện thi [K]", "ExamCenterPopup"),
                 ("Cửa hàng", "ShopUI"), ("Nhà hàng", "RestaurantMenuUI"), ("Co-op · Chat", "CoopLobbyUI")]
    def fan(src, items, x0, y0, cols, bw=420, bh=110, gx=40, gy=34):
        for i, (t, s) in enumerate(items):
            c, r = i % cols, i // cols
            b = (x0 + c * (bw + gx), y0 + r * (bh + gy), x0 + c * (bw + gx) + bw, y0 + r * (bh + gy) + bh)
            node(d, b, t, s, "step")
        top = y0 - 30
        arrow(d, [(src[2] + 6, (src[1] + src[3]) / 2), (x0 - 40, (src[1] + src[3]) / 2), (x0 - 40, top), (x0 - 10, top)], color=GOLD, width=6)
        d.line([(x0 - 40, top), (x0 + cols * (bw + gx) - gx, top)], fill=GOLD, width=6)
    fan(menu, menu_items, 560, 380, 3)
    fan(hud, hud_items, 560, 760, 3)
    d.text((560, 296), "Popup từ Menu chính", font=font(26, True), fill=MUTED)
    d.text((560, 676), "Popup trong gameplay (đóng bằng ESC / X, quay lại đúng ngữ cảnh)", font=font(26, True), fill=MUTED)
    img.save(OUT / "d02_screen_map.png")


def architecture():
    W, H = 2000, 1250
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)
    d.text((60, 50), "Kiến trúc phân lớp", font=font(40, True), fill=INK)
    layers = [
        ("Trình bày (UI)", INK, ["MainMenuUI", "HUDUI", "QuestLogPopup", "ShopUI", "RestaurantMenuUI", "ExamCenterPopup", "ExamPlayUI", "SettingsUI"]),
        ("Gameplay & Engine kịch bản", "#2B3F66", ["ScenarioManager", "DialogueManager", "ScoringManager", "ExamManager", "PlayerController", "PlayerStatus", "PlayerInventory"]),
        ("Dịch vụ (GameServices)", "#3D5A80", ["SceneFlowController", "IProgressRepository", "IAudioService", "GameSettingsService", "GameInputService", "ScenarioCampaignManager", "ExamRepository"]),
        ("Dữ liệu (ScriptableObject)", "#98692A", ["ScenarioDefinition ×14", "ExamDefinition ×2", "RestaurantMenu", "CharacterCatalog", "MenuAbout"]),
        ("Hạ tầng tùy chọn", "#5B6B82", ["Supabase (auth · cloud save · chat)", "Gemini (chấm Writing/Speaking)", "Agora (voice)"]),
    ]
    y = 140
    for name, col, items in layers:
        d.rounded_rectangle((60, y, W - 60, y + 190), 26, fill=SOFT, outline=LINE, width=3)
        d.rounded_rectangle((60, y, 470, y + 190), 26, fill=col)
        for i, ln in enumerate(wrap(d, name, font(32, True), 360)):
            d.text((265, y + 95 - 20 * (len(wrap(d, name, font(32, True), 360)) - 1) + 40 * i), ln, font=font(32, True), fill="#FFFFFF", anchor="mm")
        x = 500
        for it in items:
            tw = d.textlength(it, font=font(25, True)) + 44
            if x + tw > W - 80:
                break
            d.rounded_rectangle((x, y + 60, x + tw, y + 130), 35, fill=PAPER, outline=col, width=3)
            d.text((x + tw / 2, y + 95), it, font=font(25, True), fill=INK, anchor="mm")
            x += tw + 18
        y += 215
        if y < 1150:
            arrow(d, [(W - 140, y - 30), (W - 140, y - 5)], color=GOLD, width=6)
    img.save(OUT / "d08_architecture.png")


if __name__ == "__main__":
    core_loop()
    screen_map()
    architecture()
    flow("d03_menu_flow", "Workflow: Khởi động → Menu → Chọn nhân vật → Vào game", [
        ("Khởi động", "00_Bootstrap · AppRoot", "start"),
        ("Menu chính", "MainMenuUI · chọn VI/EN/JP", "ui"),
        ("Bấm “Bắt đầu”", "OnStartClicked", "step"),
        ("Duyệt nhân vật", "< > · preview 3D", "ui"),
        ("Nhập tên", "kiểm tra rỗng / độ dài", "step"),
        ("Xác nhận", "ConfirmCharacterAndStart", "step"),
        ("Tải khu phố", "SceneFlowController · spawn", "step"),
        ("Gameplay HUD", "nhiệm vụ đầu tiên", "end"),
    ], per=4, branches={4: ("Tên không hợp lệ", "giữ màn hình · báo lỗi")})
    flow("d04_dialogue_flow", "Workflow: Hội thoại học tập (DialogueManager)", [
        ("Tiếp cận NPC", "nhắc [F] Tương tác", "start"),
        ("Mở hội thoại", "người nói · câu Nhật", "ui"),
        ("Hỗ trợ theo chế độ", "Guided · Practice · Assessment", "step"),
        ("Chọn câu trả lời", "1 / 2 / 3 hoặc chuột", "ui"),
        ("Chấm điểm", "ScoringManager · 5 nhóm", "step"),
        ("Rẽ nhánh & lưu cờ", "setFlags · Branch", "step"),
        ("Hoàn thành mục tiêu", "objective · XP", "step"),
        ("Kết quả tình huống", "ScoreBreakdown", "end"),
    ], per=4, branches={4: ("Chọn sai", "NPC sửa nhẹ nhàng → thử lại")})
    flow("d05_shop_flow", "Workflow: Mua sắm tại konbini (ShopUI)", [
        ("Nhận mục tiêu", "ví dụ: mua onigiri", "start"),
        ("Đến cửa hàng", "GoToArea", "step"),
        ("Xem kệ hàng", "InspectItem", "step"),
        ("Mở ShopUI", "danh mục · giá ¥", "ui"),
        ("Chọn món", "số lượng", "ui"),
        ("Thanh toán", "hội thoại tại quầy", "step"),
        ("Cập nhật Yen + Balo", "CollectItem", "step"),
        ("Hoàn thành", "objective ✓", "end"),
    ], per=4, branches={5: ("Không đủ Yen", "giữ nguyên tiền & balo")})
    flow("d06_restaurant_flow", "Workflow: Nhà hàng sushi Hibari (RestaurantMenuUI)", [
        ("Vào quán", "いらっしゃいませ", "start"),
        ("Nhận chỗ ngồi", "RestaurantTable", "step"),
        ("Mở thực đơn", "7 nhóm món", "ui"),
        ("Xem chi tiết món", "nguyên liệu · dị ứng", "ui"),
        ("Gọi món", "giỏ hàng · xác nhận", "step"),
        ("Dùng bữa", "いただきます", "step"),
        ("Tính tiền", "お会計お願いします", "step"),
        ("Rời quán", "ごちそうさまでした", "end"),
    ], per=4, branches={6: ("Chưa thanh toán", "chưa được rời quán")})
    flow("d07_exam_flow", "Workflow: Trung tâm luyện thi JLPT / IELTS", [
        ("Mở luyện thi", "phím K · ExamCenterPopup", "start"),
        ("Chọn tab", "JLPT / IELTS", "ui"),
        ("Chọn đề", "ExamRepository", "ui"),
        ("Bắt đầu lượt", "ExamManager.StartAttempt", "step"),
        ("Trả lời câu hỏi", "bảng số · Trước / Sau", "ui"),
        ("Nộp phần", "khóa đáp án", "step"),
        ("Chấm điểm", "ExamGradingService", "step"),
        ("Kết quả", "ExamAttemptRecord", "end"),
    ], per=4, branches={5: ("Hết giờ phần thi", "tự động khóa & chuyển")})
    flow("d09_scenario_pipeline", "Luồng thực thi kịch bản (Scenario engine)", [
        ("ScenarioDefinition", "Resources/Scenarios", "start"),
        ("Repository", "IScenarioRepository", "step"),
        ("ScenarioManager", "kiểm tra requiredKnowledge", "ui"),
        ("Node", "Dialogue · GoToArea · Collect", "step"),
        ("DialogueManager", "lời thoại & lựa chọn", "ui"),
        ("ScoringManager", "ScoreEvent", "step"),
        ("Objective", "hoàn thành mục tiêu", "step"),
        ("Save / Progress", "XP · Knowledge · flags", "end"),
    ], per=4)
    print("ok", sorted(p.name for p in OUT.iterdir()))


def world_hub():
    W, H = 2000, 1150
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)
    d.text((60, 50), "Thế giới Hibari-chō: thành phố trung tâm + các khu vực", font=font(40, True), fill=INK)
    cx, cy = W / 2, 640
    hub = (cx - 330, cy - 150, cx + 330, cy + 150)
    zones = [
        ("Ga Hibari", "20_StationDistrict", "StationPortal · cổng Ga Hibari", (1640, 330), "ui"),
        ("Sushi Hibari", "30_SushiRestaurant", "SushiPortal · rèm noren", (360, 330), "ui"),
        ("Trường Nhật ngữ", "40_HIBARICLASS", "SchoolPortal · cổng trường", (1640, 960), "ui"),
        ("Phòng trọ", "45_HomeBedroom", "HomeBedroomPortal · Hibari Heights", (360, 960), "ui"),
    ]
    for t, scene, via, (x, y), k in zones:
        arrow(d, [((hub[0] + hub[2]) / 2 + (x - cx) * 0.38, cy + (y - cy) * 0.42), (x - (x - cx) * 0.12, y - (y - cy) * 0.18)], color=GOLD, width=8, head=26)
    node(d, hub, "90_TestSandbox · Hibari-chō", "Hibari Mart · quảng trường · cột chỉ đường", "start")
    for t, scene, via, (x, y), k in zones:
        node(d, (x - 290, y - 105, x + 290, y + 105), f"{t}  ({scene})", via, k)
    node(d, (cx - 250, 190, cx + 250, 300), "00_Bootstrap → 01_MainMenu", "chọn nhân vật → vào thành phố", "step")
    arrow(d, [(cx, 304), (cx, hub[1] - 8)], color=INK, width=6)
    d.text((cx, H - 60), "Vào khu: ScenePortal (nạp additive, ẩn thành phố)   ·   Ra khu: cửa/cổng ExitToCity → spawn trước đúng cửa", font=font(26, True), fill=MUTED, anchor="mm")
    img.save(OUT / "d11_world_hub.png")


world_hub()
