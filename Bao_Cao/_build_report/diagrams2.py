"""UML diagrams for the round-7 report: use case, class, sequence, state, ERD, deployment, workflows.
Same visual language as diagrams.py (navy/gold/red, Segoe UI, PIL at 2x)."""
import math
from PIL import Image, ImageDraw

from diagrams import OUT, INK, GOLD, RED, MUTED, LINE, PAPER, SOFT, GREEN, font, wrap, arrow, flow

BLUE = "#1F3C88"


def canvas(W, H, title, sub=None):
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)
    d.text((60, 46), title, font=font(42, True), fill=INK)
    if sub:
        d.text((60, 104), sub, font=font(26), fill=MUTED)
    return img, d


def centered(d, cx, cy, lines, f, fill, lh):
    y = cy - (len(lines) - 1) * lh / 2
    for ln in lines:
        d.text((cx, y), ln, font=f, fill=fill, anchor="mm")
        y += lh


def dashed(d, p1, p2, color, width=3, dash=14):
    (x1, y1), (x2, y2) = p1, p2
    L = math.hypot(x2 - x1, y2 - y1)
    n = max(1, int(L // dash))
    for i in range(0, n, 2):
        a, b = i / n, min(1, (i + 1) / n)
        d.line([(x1 + (x2 - x1) * a, y1 + (y2 - y1) * a), (x1 + (x2 - x1) * b, y1 + (y2 - y1) * b)], fill=color, width=width)


def label_on(d, x, y, text, size=22, color=MUTED, bg=PAPER):
    f = font(size)
    w = d.textlength(text, font=f)
    d.rectangle((x - w / 2 - 8, y - size * 0.7, x + w / 2 + 8, y + size * 0.7), fill=bg)
    d.text((x, y), text, font=f, fill=color, anchor="mm")


# ─────────── Use case ───────────

def stick(d, cx, cy, name, sub=None, color=INK):
    d.ellipse((cx - 26, cy - 110, cx + 26, cy - 58), outline=color, width=6)
    d.line([(cx, cy - 58), (cx, cy + 10)], fill=color, width=6)
    d.line([(cx - 52, cy - 32), (cx + 52, cy - 32)], fill=color, width=6)
    d.line([(cx, cy + 10), (cx - 40, cy + 80)], fill=color, width=6)
    d.line([(cx, cy + 10), (cx + 40, cy + 80)], fill=color, width=6)
    d.text((cx, cy + 112), name, font=font(28, True), fill=color, anchor="mm")
    if sub:
        d.text((cx, cy + 148), sub, font=font(22), fill=MUTED, anchor="mm")


def system_actor(d, box, name, sub):
    x1, y1, x2, y2 = box
    d.rounded_rectangle(box, 18, fill=SOFT, outline=BLUE, width=4)
    d.text(((x1 + x2) / 2, y1 + 34), "«hệ thống ngoài»", font=font(20), fill=MUTED, anchor="mm")
    d.text(((x1 + x2) / 2, y1 + 74), name, font=font(28, True), fill=BLUE, anchor="mm")
    d.text(((x1 + x2) / 2, y1 + 112), sub, font=font(20), fill=MUTED, anchor="mm")


def use_case():
    cases = [("Chọn ngôn ngữ & nhân vật", None), ("Khám phá phố & bản đồ [M]", None), ("Theo dõi nhiệm vụ [J]", None),
             ("Hội thoại N5 với NPC", None), ("Biểu cảm & câu chào [E]", None), ("Ăn, uống, ngủ (chỉ số sống)", None),
             ("Mua sắm ở konbini", None), ("Gọi món ở nhà hàng sushi", None), ("Mua vé & đi tàu", None),
             ("Chơi mini-game Kana Match", None), ("Đổi vé lấy quà", None),
             ("Luyện thi JLPT N5 / IELTS", "gm"), ("Luyện phát âm [V]", "gm"), ("Trò chuyện AI với NPC", "gm"),
             ("Đăng nhập & hồ sơ", "sb"), ("Bạn bè, chat, xếp hạng", "sb"), ("Co-op theo kịch bản", "sb"),
             ("Lớp học video [F8]", "ag")]
    step, top = 126, 300
    W, H = 2600, top + len(cases) * step + 120
    img, d = canvas(W, H, "Sơ đồ use case tổng quát — NihongoLife", "18 use case; ba dịch vụ ngoài chỉ tham gia ở các ca trực tuyến (viền xanh)")
    sys_box = (560, 190, 1780, H - 70)
    d.rounded_rectangle(sys_box, 30, outline=INK, width=5)
    d.text(((sys_box[0] + sys_box[2]) / 2, sys_box[1] + 42), "Hệ thống: game NihongoLife (Unity 6)", font=font(30, True), fill=INK, anchor="mm")
    cx, ew, eh = 1170, 640, 100
    actor = (250, H / 2)
    ys = [top + 40 + i * step for i in range(len(cases))]
    for y in ys:
        d.line([(actor[0] + 70, actor[1] - 40), (cx - ew / 2, y)], fill="#AEB9CC", width=3)
    ext = {"gm": ("Google Gemini", "gemini-flash-latest"), "sb": ("Supabase", "Auth · REST · Realtime"), "ag": ("Agora RTC", "kênh lớp học video")}
    for key, (name, sub) in ext.items():
        idx = [i for i, (_, k) in enumerate(cases) if k == key]
        my = sum(ys[i] for i in idx) / len(idx)
        box = (2050, my - 85, 2480, my + 85)
        for i in idx:
            d.line([(cx + ew / 2, ys[i]), (box[0], my)], fill="#9FB3D6", width=3)
        system_actor(d, box, name, sub)
    groups = [(0, 5, "Đời sống & khám phá"), (6, 10, "Dịch vụ trong phố"), (11, 17, "Học tập & trực tuyến")]
    for i0, i1, lab in groups:
        y0, y1 = ys[i0] - eh / 2 - 8, ys[i1] + eh / 2 + 8
        d.line([(sys_box[0] + 60, y0), (sys_box[0] + 60, y1)], fill=GOLD, width=6)
        tmp = Image.new("RGBA", (int(y1 - y0), 40), (0, 0, 0, 0))
        ImageDraw.Draw(tmp).text((tmp.width / 2, 20), lab, font=font(22, True), fill="#9A6A00", anchor="mm")
        tmp = tmp.rotate(90, expand=True)
        img.paste(tmp, (int(sys_box[0] + 14), int(y0)), tmp)
    for (t, k), y in zip(cases, ys):
        online = k is not None
        d.ellipse((cx - ew / 2, y - eh / 2, cx + ew / 2, y + eh / 2), fill=SOFT if online else PAPER, outline=BLUE if online else INK, width=4)
        d.text((cx, y), t, font=font(27, True), fill=INK, anchor="mm")
    stick(d, actor[0], actor[1], "Người chơi", "học viên N5")
    img.save(OUT / "d12_use_case.png")


# ─────────── Class diagram ───────────

def class_box(d, x, y, w, name, fields, methods, stereo=None, accent=INK):
    fh, mh = 30, 30
    head = 66 if stereo else 50
    h = head + 16 + len(fields) * fh + 16 + len(methods) * mh + 12
    d.rectangle((x, y, x + w, y + h), fill=PAPER, outline=accent, width=4)
    d.rectangle((x, y, x + w, y + head), fill=accent)
    if stereo:
        d.text((x + w / 2, y + 18), stereo, font=font(19), fill="#C8D3E6", anchor="mm")
        d.text((x + w / 2, y + 46), name, font=font(26, True), fill="#FFFFFF", anchor="mm")
    else:
        d.text((x + w / 2, y + head / 2), name, font=font(26, True), fill="#FFFFFF", anchor="mm")
    yy = y + head + 10
    for f_ in fields:
        d.text((x + 16, yy), f_, font=font(21), fill=INK); yy += fh
    d.line([(x, yy + 6), (x + w, yy + 6)], fill=LINE, width=3)
    yy += 16
    for m in methods:
        d.text((x + 16, yy), m, font=font(21), fill=BLUE); yy += mh
    return (x, y, x + w, y + h)


def class_diagram():
    W, H = 2800, 2000
    img, d = canvas(W, H, "Sơ đồ lớp — các thành phần cốt lõi", "Rút gọn: chỉ ghi trường và phương thức chính (198 script runtime + 8 script mini-game)")
    w = 540
    cols = [60, 760, 1460, 2160]
    rows = [180, 600, 1040, 1500]
    B = {}
    B["AppRoot"] = class_box(d, cols[0], rows[0], w, "AppRoot", ["- services: GameServices"], ["+ Awake(): đăng ký dịch vụ", "+ DontDestroyOnLoad"])
    B["GameServices"] = class_box(d, cols[1], rows[0], w, "GameServices", ["- registry: Dictionary<Type,object>"], ["+ Register<T>(svc)", "+ Get<T>() / TryGet<T>()"], "«static»")
    B["SceneFlow"] = class_box(d, cols[2], rows[0], w, "SceneFlowController", ["+ IsLoading: bool"], ["+ EnterZone(scene, spawn)", "+ ExitZone() / TransferZone()"])
    B["Settings"] = class_box(d, cols[3], rows[0], w, "GameSettingsService", ["+ Language: VI | EN | JP"], ["+ SetLanguage(lang)", "+ event LanguageChanged"])
    B["Scenario"] = class_box(d, cols[0], rows[1], w, "ScenarioManager", ["+ CurrentNode: ScenarioNode", "- active: ScenarioDefinition"], ["+ StartScenario(def)", "+ SetPlayerInputLocked(b)"])
    B["Dialogue"] = class_box(d, cols[1], rows[1], w, "DialogueManager", ["+ IsOpen, CanLeave: bool", "+ IsStoryPaused: bool"], ["+ StartConversation(nodes)", "+ CancelDialogue() / ResumeDialogue()"])
    B["View"] = class_box(d, cols[2], rows[1], w, "DialogueView", ["- choices: List<Image>", "- closeButton (×), resumeButton"], ["+ Show(data, canLeave)", "+ Pick(i) / Continue() / Leave()"])
    B["Scoring"] = class_box(d, cols[3], rows[1], w, "ScoringManager", ["+ Events: List<ScoreEvent>"], ["+ Submit(source, category, score)", "+ Summary(scenarioId)"])
    B["Posture"] = class_box(d, cols[0], rows[2], w, "PostureStabilizer", ["- spine, chest, neck: float", "- stanceWidening = 4°"], ["- LateUpdate(): thẳng lưng"])
    B["Player"] = class_box(d, cols[1], rows[2], w, "PlayerController", ["+ InputLocked: bool", "- runEnergyCostPerSecond = 14"], ["- HandleMovement()", "+ Setup(): thêm thành phần"])
    B["Status"] = class_box(d, cols[2], rows[2], w, "PlayerStatus", ["+ CurrentEnergy, Hunger, Thirst", "+ IsExhausted, CanSprint"], ["+ ReportActivity(moving, sprint)", "+ ConsumeEnergy(x) / Sleep(h)"])
    B["Inventory"] = class_box(d, cols[3], rows[2], w, "PlayerInventory", ["+ Yen: int", "+ Items: List<InventoryItem>"], ["+ AddItem(id, n) / RemoveItem()", "+ TrySpendYen(amount)"])
    B["MiniCtl"] = class_box(d, cols[0], rows[3], w, "MiniGameController", ["- game: IMiniGame", "+ LastResult: MiniGameResult"], ["+ Launch(launcher, player)", "+ CloseOrAbort()"])
    B["IMini"] = class_box(d, cols[1], rows[3], w, "IMiniGame", [], ["+ Begin(def, root)", "+ Abort()", "+ event Finished(result)"], "«interface»", BLUE)
    B["Kana"] = class_box(d, cols[2], rows[3], w, "KanaMatchGame", ["+ State: Phase", "+ Cards: List<Card>"], ["+ StartRound(set)", "+ Pick(index)"])
    B["Shop"] = class_box(d, cols[3], rows[3], w, "KonbiniShopUI", ["- basket: KonbiniBasket", "- ItemIcons (ảnh món)"], ["+ AddSelectedToBasket()", "+ Pay(): bool"])

    def P(k, side, t=0.5):
        x1, y1, x2, y2 = B[k]
        return {"r": (x2, y1 + (y2 - y1) * t), "l": (x1, y1 + (y2 - y1) * t), "b": (x1 + (x2 - x1) * t, y2), "t": (x1 + (x2 - x1) * t, y1)}[side]

    def path(pts, text=None, at=None, kind="dep", tcolor=MUTED):
        if kind == "real":
            for q1, q2 in zip(pts, pts[1:]):
                dashed(d, q1, q2, BLUE, 4)
            (x1, y1), (x2, y2) = pts[-2], pts[-1]
            ang = math.atan2(y2 - y1, x2 - x1)
            d.polygon([(x2, y2), (x2 - 28 * math.cos(ang - 0.45), y2 - 28 * math.sin(ang - 0.45)), (x2 - 28 * math.cos(ang + 0.45), y2 - 28 * math.sin(ang + 0.45))], fill=PAPER, outline=BLUE)
        elif kind == "comp":
            for q1, q2 in zip(pts, pts[1:]):
                d.line([q1, q2], fill=INK, width=4)
            (x1, y1), (x2, y2) = pts[0], pts[1]
            ang = math.atan2(y2 - y1, x2 - x1)
            d.polygon([(x1, y1), (x1 + 20 * math.cos(ang - 0.5), y1 + 20 * math.sin(ang - 0.5)), (x1 + 40 * math.cos(ang), y1 + 40 * math.sin(ang)), (x1 + 20 * math.cos(ang + 0.5), y1 + 20 * math.sin(ang + 0.5))], fill=INK)
        else:
            arrow(d, pts, color=INK, width=4, head=18)
        if text:
            x, y = at if at else ((pts[0][0] + pts[-1][0]) / 2, (pts[0][1] + pts[-1][1]) / 2)
            label_on(d, x, y, text, 21, tcolor)

    def hlink(a, b, text):  # adjacent boxes in one row, label above the arrow
        p1, p2 = P(a, "r", 0.3), P(b, "l", 0.3)
        path([p1, p2])
        label_on(d, (p1[0] + p2[0]) / 2, p1[1] - 28, text, 20)

    hlink("AppRoot", "GameServices", "đăng ký")
    hlink("GameServices", "SceneFlow", "cung cấp")
    hlink("SceneFlow", "Settings", "ngôn ngữ")
    hlink("Scenario", "Dialogue", "node")
    hlink("Dialogue", "View", "hiển thị")
    hlink("View", "Scoring", "chấm")
    hlink("Status", "Inventory", "hồi chỉ số")
    hlink("Player", "Status", "vận động")
    # composition: PlayerController ◆— PostureStabilizer
    p1, p2 = P("Player", "l", 0.3), P("Posture", "r", 0.3)
    path([p1, p2], kind="comp"); label_on(d, (p1[0] + p2[0]) / 2, p1[1] - 28, "thêm", 20)
    # services resolve
    p1 = P("Scenario", "t", 0.5); p2 = P("GameServices", "b", 0.2)
    path([p1, (p1[0], p2[1] + 70), (p2[0], p2[1] + 70), p2]); label_on(d, (p1[0] + p2[0]) / 2, p2[1] + 70, "lấy dịch vụ", 20)
    # Player locked by scenario
    p1 = P("Player", "t", 0.5); p2 = P("Scenario", "b", 0.7)
    path([p1, (p1[0], p1[1] - 70), (p2[0], p1[1] - 70), p2]); label_on(d, (p1[0] + p2[0]) / 2, p1[1] - 70, "khoá input", 20)
    # mini-game
    p1, p2 = P("MiniCtl", "r", 0.35), P("IMini", "l", 0.35)
    path([p1, p2], kind="comp"); label_on(d, (p1[0] + p2[0]) / 2, p1[1] - 28, "1", 22, INK)
    p1, p2 = P("Kana", "l", 0.35), P("IMini", "r", 0.35)
    path([p1, p2], kind="real"); label_on(d, (p1[0] + p2[0]) / 2, p1[1] - 28, "hiện thực", 20)
    p1 = P("MiniCtl", "t", 0.7); p2 = P("Player", "b", 0.3)
    path([p1, (p1[0], p1[1] - 60), (p2[0], p1[1] - 60), p2]); label_on(d, (p1[0] + p2[0]) / 2 + 40, p1[1] - 60, "khoá / khôi phục", 20)
    p1, p2 = P("Shop", "t", 0.5), P("Inventory", "b", 0.5)
    path([p1, p2]); label_on(d, p1[0] + 150, (p1[1] + p2[1]) / 2, "TrySpendYen + AddItem", 20)
    yb = H - 70
    p1 = P("MiniCtl", "b", 0.5); x_out = W - 40; p2 = P("Scoring", "r", 0.6)
    path([p1, (p1[0], yb), (x_out, yb), (x_out, p2[1]), p2])
    label_on(d, 1400, yb, "kết quả mini-game → ScoringManager (Vocabulary, ResponseAccuracy)", 21)
    img.save(OUT / "d13_class.png")


# ─────────── Sequence diagrams ───────────

def sequence(name, title, sub, lifelines, msgs, frames=(), W=2600):
    """msgs: (from, to, text, kind) kind in call|ret|self|note. frames: (first_msg, last_msg, label, else_at or None)."""
    n = len(lifelines)
    left, right = 170, W - 170
    xs = [left + i * (right - left) / (n - 1) for i in range(n)]
    X = {k: xs[i] for i, (k, _) in enumerate(lifelines)}
    row = 82
    top = 330
    H = top + len(msgs) * row + 140
    img, d = canvas(W, H, title, sub)
    head_w = min(300, (right - left) / (n - 1) - 24)
    for k, label in lifelines:
        x = X[k]
        lines = wrap(d, label, font(23, True), head_w - 20)
        bh = 40 + 30 * len(lines)
        actor = k == "P"
        if actor:
            d.ellipse((x - 18, 170, x + 18, 206), outline=INK, width=5)
            d.line([(x, 206), (x, 246)], fill=INK, width=5); d.line([(x - 30, 222), (x + 30, 222)], fill=INK, width=5)
            d.line([(x, 246), (x - 24, 280)], fill=INK, width=5); d.line([(x, 246), (x + 24, 280)], fill=INK, width=5)
            d.text((x, 302), label, font=font(23, True), fill=INK, anchor="mm")
        else:
            d.rounded_rectangle((x - head_w / 2, 300 - bh, x + head_w / 2, 300), 12, fill=INK)
            centered(d, x, 300 - bh / 2, lines, font(23, True), "#FFFFFF", 30)
        dashed(d, (x, 318), (x, H - 60), "#9AA6BA", 3, 12)
    ys = [top + 40 + i * row for i in range(len(msgs))]
    for first, last, lab, else_at in frames:
        y1, y2 = ys[first] - 52, ys[last] + 30
        xs_in = [X[m[0]] for m in msgs[first:last + 1]] + [X[m[1]] for m in msgs[first:last + 1]]
        x1, x2 = min(xs_in) - 120, max(xs_in) + 120
        x1, x2 = max(30, x1), min(W - 30, x2)
        d.rectangle((x1, y1, x2, y2), outline=GOLD, width=4)
        tw = d.textlength(lab, font=font(22, True)) + 30
        d.polygon([(x1, y1), (x1 + tw, y1), (x1 + tw, y1 + 26), (x1 + tw - 14, y1 + 40), (x1, y1 + 40)], fill=GOLD)
        d.text((x1 + 12, y1 + 6), lab, font=font(22, True), fill=INK)
        if else_at is not None:
            ye = ys[else_at] - 46
            dashed(d, (x1, ye), (x2, ye), GOLD, 3)
            d.text((x1 + 14, ye + 4), "[ngược lại]", font=font(21, True), fill="#9A6A00")
    for i, (a, b, text, kind) in enumerate(msgs):
        y = ys[i]
        xa, xb = X[a], X[b]
        if kind == "self":
            d.line([(xa, y - 14), (xa + 70, y - 14), (xa + 70, y + 18)], fill=INK, width=4)
            arrow(d, [(xa + 70, y + 18), (xa + 6, y + 18)], color=INK, width=4, head=14)
            tw2 = d.textlength(text, font=font(22))
            d.rectangle((xa + 78, y - 14, xa + 90 + tw2, y + 18), fill=PAPER)
            d.text((xa + 84, y + 2), text, font=font(22), fill=INK, anchor="lm")
            continue
        color = INK if kind == "call" else MUTED
        if kind == "ret":
            dashed(d, (xa, y), (xb, y), color, 3)
            arrow(d, [(xb + (12 if xb < xa else -12), y), (xb, y)], color=color, width=3, head=14)
        else:
            arrow(d, [(xa, y), (xb + (6 if xb < xa else -6), y)], color=color, width=4, head=16)
        f = font(22) if kind == "call" else font(21)
        tx = (xa + xb) / 2
        tw = d.textlength(text, font=f)
        span = abs(xb - xa)
        if tw > span - 20:
            tx = min(xa, xb) + tw / 2 + 10
        tw2 = d.textlength(text, font=f)
        d.rectangle((tx - tw2 / 2 - 6, y - 38, tx + tw2 / 2 + 6, y - 7), fill=PAPER)
        d.text((tx, y - 22), text, font=f, fill=INK if kind == "call" else MUTED, anchor="mm")
    img.save(OUT / f"{name}.png")


def seq_dialogue():
    L = [("P", "Người chơi"), ("ID", "InteractionDetector"), ("NPC", "NPCController"), ("DM", "DialogueManager"),
         ("SM", "ScenarioManager"), ("DV", "DialogueView"), ("SC", "ScoringManager"), ("SV", "SaveService")]
    M = [("P", "ID", "F gần NPC (ưu tiên theo hướng nhìn)", "call"),
         ("ID", "NPC", "Interact(player)", "call"),
         ("NPC", "DM", "StartDialogue(node)", "call"),
         ("DM", "SM", "SetPlayerInputLocked(true)", "call"),
         ("DM", "DV", "Show(data, canLeave)", "call"),
         ("DV", "P", "câu Nhật · cách đọc · nghĩa · 1–4 lựa chọn", "ret"),
         ("P", "DV", "chọn đáp án (phím 1–4 / chuột)", "call"),
         ("DV", "DM", "ChoiceSelected(i)", "call"),
         ("DM", "SM", "Advance(choice)", "call"),
         ("SM", "SC", "Submit(kỹ năng, đúng/sai)", "call"),
         ("SM", "DM", "node tiếp theo", "ret"),
         ("SM", "DM", "node phản hồi + gợi ý, cho chọn lại", "ret"),
         ("P", "DV", "× hoặc Esc", "call"),
         ("DV", "DM", "Leave(): giữ node, IsStoryPaused", "call"),
         ("P", "DV", "R · Tiếp hội thoại", "call"),
         ("DM", "DV", "Show(node đang dừng)", "call"),
         ("SM", "SV", "SaveProgress(node, cờ truyện)", "call")]
    sequence("d14_seq_dialogue", "Sơ đồ tuần tự — hội thoại học tập", "Từ lúc nhấn F tới khi lưu tiến trình; đóng hội thoại không làm mất bước truyện",
             L, M, frames=[(10, 11, "alt  [đúng]", 11), (12, 15, "opt  [người chơi tạm rời]", None)])


def seq_checkout():
    L = [("P", "Người chơi"), ("SH", "KonbiniShelf"), ("UI", "KonbiniShopUI"), ("BK", "KonbiniBasket"),
         ("ITO", "NPC Ito (thu ngân)"), ("INV", "PlayerInventory"), ("DM", "DialogueManager")]
    M = [("P", "SH", "F ở kệ", "call"),
         ("SH", "UI", "Open(section)", "call"),
         ("UI", "P", "lưới món: ảnh, tên Nhật, giá ¥", "ret"),
         ("P", "UI", "chọn món, số lượng", "call"),
         ("UI", "BK", "Add(id, qty)", "call"),
         ("P", "UI", "× / Esc đóng kệ", "call"),
         ("P", "ITO", "F ở quầy thu ngân", "call"),
         ("ITO", "UI", "OpenCheckout()", "call"),
         ("UI", "P", "giỏ hàng có ảnh, tổng tiền", "ret"),
         ("P", "UI", "Thanh toán (harau)", "call"),
         ("UI", "INV", "TrySpendYen(total)", "call"),
         ("UI", "INV", "AddItem(id, n) cho từng món", "call"),
         ("UI", "BK", "Clear()", "call"),
         ("UI", "DM", "Ito cảm ơn, hỏi có cần túi", "call"),
         ("UI", "P", "báo thiếu Yen, giữ nguyên giỏ", "ret")]
    sequence("d15_seq_checkout", "Sơ đồ tuần tự — mua hàng ở Hibari Mart", "Chọn món ở kệ, thanh toán ở quầy với chị Ito",
             L, M, frames=[(11, 14, "alt  [đủ Yen]", 14)])


def seq_minigame():
    L = [("P", "Người chơi"), ("L", "MiniGameLauncher"), ("C", "MiniGameController"), ("K", "KanaMatchGame"),
         ("SC", "ScoringManager"), ("LM", "LearningMastery"), ("INV", "PlayerInventory"), ("PC", "PrizeCounter")]
    M = [("P", "L", "F ở máy Kana Match", "call"),
         ("L", "C", "Launch(launcher, player)", "call"),
         ("C", "C", "khoá di chuyển, đổi camera", "self"),
         ("C", "K", "Begin(definition, root)", "call"),
         ("K", "P", "chọn bộ: hiragana / katakana / từ", "ret"),
         ("P", "K", "StartRound(set)", "call"),
         ("K", "K", "đếm 3-2-1, lật thẻ, combo", "self"),
         ("P", "C", "× / Esc → CloseOrAbort()", "call"),
         ("C", "K", "Abort() → kết quả “dừng giữa chừng”", "call"),
         ("K", "C", "Finished(result)", "call"),
         ("C", "SC", "Submit(Vocabulary, độ chính xác)", "call"),
         ("C", "LM", "tăng mastery các cặp đã thuộc", "call"),
         ("C", "INV", "AddItem(game_ticket, vé thưởng)", "call"),
         ("C", "P", "bảng kết quả: sao, điểm, vé", "ret"),
         ("P", "C", "× / Tiếp tục → Close()", "call"),
         ("P", "PC", "đổi vé ở quầy Aoi", "call"),
         ("PC", "INV", "trừ vé, thêm quà", "call")]
    sequence("d16_seq_minigame", "Sơ đồ tuần tự — mini-game Kana Match", "Khung mini-game dùng chung: mọi trò chơi mới chỉ cần hiện thực IMiniGame",
             L, M, frames=[(7, 9, "alt  [người chơi bấm ×]", 9)])


def seq_ai():
    L = [("P", "Người chơi"), ("SP", "SpeechPracticeController"), ("MIC", "Microphone"), ("GC", "GeminiConversationService"),
         ("API", "Gemini API (gemini-flash-latest)"), ("NPC", "NPCController"), ("DV", "DialogueView")]
    M = [("P", "SP", "V: bắt đầu ghi câu đang học", "call"),
         ("SP", "MIC", "Microphone.Start()", "call"),
         ("P", "SP", "V lần nữa / hết giờ", "call"),
         ("SP", "API", "generateContent(audio + câu mẫu)", "call"),
         ("API", "SP", "phiên âm, điểm, góp ý (HTTP 200)", "ret"),
         ("SP", "DV", "hiện kết quả phát âm", "call"),
         ("SP", "DV", "báo lỗi, giữ luyện tập offline", "call"),
         ("NPC", "GC", "RequestNpcReply(npc, ý định)", "call"),
         ("GC", "API", "generateContent(vai NPC, N5)", "call"),
         ("API", "GC", "câu trả lời tiếng Nhật", "ret"),
         ("GC", "NPC", "AiNpcReply hoặc câu soạn sẵn", "ret"),
         ("NPC", "DV", "hiển thị lượt thoại", "call")]
    sequence("d17_seq_ai", "Sơ đồ tuần tự — luyện phát âm và NPC dùng AI", "Khoá API đọc từ biến môi trường NIHONGOLIFE_GEMINI_API_KEY; thiếu khoá thì game vẫn chạy offline",
             L, M, frames=[(4, 6, "alt  [API trả 200]", 6)])


def seq_zone():
    L = [("P", "Người chơi"), ("POR", "ScenePortal / cửa"), ("SF", "SceneFlowController"), ("SMG", "SceneManager"),
         ("ZV", "SceneZoneVisibility"), ("PC", "PlayerController"), ("HUD", "HUD (StatusDock)")]
    M = [("P", "POR", "đi vào cổng / F ở cửa", "call"),
         ("POR", "SF", "EnterZone(scene, spawnId)", "call"),
         ("SF", "PC", "LockPlayer(true)", "call"),
         ("SF", "SMG", "LoadSceneAsync(zone, Additive)", "call"),
         ("SF", "ZV", "ẩn root thành phố", "call"),
         ("SF", "PC", "MovePlayerToSpawn(spawnId)", "call"),
         ("SF", "HUD", "cập nhật nơi đang đứng", "call"),
         ("SF", "PC", "LockPlayer(false)", "call"),
         ("P", "POR", "ra cửa ExitToCity", "call"),
         ("POR", "SF", "ExitZone()", "call"),
         ("SF", "SMG", "UnloadSceneAsync(zone)", "call"),
         ("SF", "ZV", "hiện lại thành phố", "call"),
         ("SF", "PC", "spawn trước đúng cửa đã vào", "call")]
    sequence("d23_seq_zone", "Sơ đồ tuần tự — chuyển khu vực", "Thành phố luôn là nền; mỗi khu nạp thêm (additive) và có lối ra cố định",
             L, M)


# ─────────── State machines ───────────

def state_diagram(name, title, sub, states, trans, W=2400, H=1300):
    img, d = canvas(W, H, title, sub)
    B = {}
    for k, (label, desc, (cx, cy), kind) in states.items():
        w, h = 420, 150
        box = (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)
        fill, border, txt = {"normal": (PAPER, INK, INK), "hot": ("#FDECEA", RED, RED), "good": ("#E7F6EE", GREEN, "#1D6B47"), "start": (SOFT, BLUE, BLUE)}[kind]
        d.rounded_rectangle(box, 40, fill=fill, outline=border, width=5)
        d.text((cx, cy - 26), label, font=font(30, True), fill=txt, anchor="mm")
        centered(d, cx, cy + 24, wrap(d, desc, font(22), w - 50), font(22), MUTED, 28)
        B[k] = box
    for tr in trans:
        a, b, text, bend = tr[:4]
        frac = tr[4] if len(tr) > 4 else 0.5
        A, Bb = B[a], B[b]
        ca = ((A[0] + A[2]) / 2, (A[1] + A[3]) / 2)
        cb = ((Bb[0] + Bb[2]) / 2, (Bb[1] + Bb[3]) / 2)

        def edge(box, towards):
            cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
            dx, dy = towards[0] - cx, towards[1] - cy
            sx = (box[2] - box[0]) / 2 / max(1e-6, abs(dx)) if dx else 1e9
            sy = (box[3] - box[1]) / 2 / max(1e-6, abs(dy)) if dy else 1e9
            s = min(sx, sy)
            return (cx + dx * s, cy + dy * s)
        off = bend
        nx, ny = -(cb[1] - ca[1]), cb[0] - ca[0]
        L = math.hypot(nx, ny) or 1
        nx, ny = nx / L * off, ny / L * off
        pa = edge(A, (cb[0] + nx, cb[1] + ny)); pb = edge(Bb, (ca[0] + nx, ca[1] + ny))
        pa = (pa[0] + nx, pa[1] + ny); pb = (pb[0] + nx, pb[1] + ny)
        arrow(d, [pa, pb], color=INK, width=4, head=18)
        label_on(d, pa[0] + (pb[0] - pa[0]) * frac, pa[1] + (pb[1] - pa[1]) * frac, text, 21, INK)
    d.ellipse((60, H / 2 - 16, 92, H / 2 + 16), fill=INK)
    return img, d, B


def state_stamina():
    S = {"idle": ("Đứng yên", "hồi 9 thể lực/giây", (420, 470), "start"),
         "walk": ("Đi bộ", "hồi 4/giây; đói, khát ×1,5", (1200, 470), "normal"),
         "run": ("Chạy (Shift)", "−14 thể lực/giây, không hồi; đói ×4, khát ×5", (1980, 470), "hot"),
         "breath": ("Lấy hơi", "1,2 giây sau khi thả Shift chưa hồi", (1980, 1000), "normal"),
         "tired": ("Kiệt sức", "thể lực = 0: Shift chỉ đi bộ, HUD đỏ “Hết sức”", (1200, 1000), "hot"),
         "rest": ("Ngủ / ăn uống", "Sleep(): đầy thể lực; ăn: +No/Khát/Năng lượng", (420, 1000), "good")}
    T = [("idle", "walk", "WASD", 26), ("walk", "idle", "thả phím", 26),
         ("walk", "run", "giữ Shift & CanSprint", 26), ("run", "walk", "thả Shift", 26),
         ("run", "breath", "dừng chạy", 0), ("breath", "walk", "sau 1,2 s", 0, 0.28),
         ("run", "tired", "thể lực = 0", 0, 0.28), ("tired", "walk", "hồi ≥ 25", 0),
         ("rest", "idle", "xong", 0), ("idle", "rest", "giường / đồ ăn", -0.001)]
    img, d, B = state_diagram("d19", "Máy trạng thái thể lực (PlayerStatus)", "Chạy tiêu hao thật; hết sức thì bị khoá chạy tới khi hồi đủ 25 điểm", S, T)
    arrow(d, [(92, img.height / 2), (B["idle"][0] - 6, 470)], color=INK, width=4)
    img.save(OUT / "d19_state_stamina.png")


def state_minigame():
    S = {"idle": ("Rảnh", "chưa mở máy", (330, 420), "start"),
         "pick": ("ChoosingSet", "chọn bộ kana / từ vựng", (930, 420), "normal"),
         "count": ("Countdown", "3 · 2 · 1", (1530, 420), "normal"),
         "play": ("Playing", "lật 2 thẻ; combo, điểm trực tiếp", (2100, 760), "normal"),
         "res": ("Resolving", "khớp: giữ thẻ · sai: lật lại", (1530, 1100), "normal"),
         "result": ("Result", "sao, độ chính xác, vé thưởng", (930, 1100), "good"),
         "abort": ("Aborted", "× / Esc: kết quả “dừng giữa chừng”", (330, 1100), "hot")}
    T = [("idle", "pick", "F · Launch()", 0), ("pick", "count", "StartRound(set)", 0), ("count", "play", "hết đếm", 0),
         ("play", "res", "chọn thẻ thứ 2", 26), ("res", "play", "IsBusy = false", 26), ("res", "result", "hết cặp", 0),
         ("pick", "abort", "×", 0, 0.25), ("abort", "result", "hiện bảng", 0), ("result", "idle", "× / Tiếp tục · Close()", 0, 0.72)]
    img, d, B = state_diagram("d20", "Máy trạng thái mini-game Kana Match", "Phase của KanaMatchGame và MiniGameController", S, T, H=1300)
    arrow(d, [(92, img.height / 2), (B["idle"][0] + 40, B["idle"][3] + 4)], color=INK, width=4)
    # play -> abort (any running state can abort)
    p = (B["play"][0] + 60, B["play"][3])
    dashed(d, p, (p[0] - 160, 1240), RED, 4)
    d.line([(p[0] - 160, 1240), (330, 1240)], fill=RED, width=4)
    arrow(d, [(330, 1240), (330, B["abort"][3] + 4)], color=RED, width=4)
    label_on(d, 1200, 1240, "× / Esc trong lúc chơi → Abort()", 21, RED)
    img.save(OUT / "d20_state_minigame.png")


# ─────────── ERD ───────────

def table_box(d, x, y, w, name, cols, accent=INK, note=None):
    rh = 36
    h = 60 + rh * len(cols) + 14
    d.rectangle((x, y, x + w, y + h), fill=PAPER, outline=accent, width=4)
    d.rectangle((x, y, x + w, y + 60), fill=accent)
    d.text((x + w / 2, y + 30), name, font=font(26, True), fill="#FFFFFF", anchor="mm")
    yy = y + 72
    for c, t in cols:
        key = c.startswith("PK ") or c.startswith("FK ")
        tag, colname = (c[:2], c[3:]) if key else ("", c)
        if tag:
            d.text((x + 16, yy), tag, font=font(19, True), fill=GOLD if tag == "PK" else RED)
        d.text((x + 64, yy), colname, font=font(22, True) if tag == "PK" else font(22), fill=INK)
        d.text((x + w - 16, yy), t, font=font(20), fill=MUTED, anchor="ra")
        yy += rh
    if note:
        d.text((x + w / 2, y + h + 26), note, font=font(20), fill=MUTED, anchor="mm")
    return (x, y, x + w, y + h)


def erd():
    W, H = 2600, 1800
    img, d = canvas(W, H, "Mô hình dữ liệu — Supabase (Postgres) và bản lưu cục bộ", "Tên bảng và cột lấy từ các service trong mã nguồn; auth.users do Supabase Auth quản lý")
    T = {}
    T["users"] = table_box(d, 1060, 190, 480, "auth.users", [("PK id", "uuid"), ("email", "text"), ("created_at", "timestamptz")], BLUE)
    T["profiles"] = table_box(d, 80, 190, 560, "profiles", [("PK id → users.id", "uuid"), ("display_name", "text"), ("avatar_url", "text"), ("level", "int"), ("xp", "int"), ("current_chapter", "int"), ("bio", "text")])
    T["progress"] = table_box(d, 1960, 190, 560, "player_progress", [("PK FK user_id", "uuid"), ("completed_scenarios", "text[]"), ("progress_json", "jsonb"), ("updated_at", "timestamptz")])
    T["leader"] = table_box(d, 80, 760, 560, "leaderboard", [("FK user_id", "uuid"), ("display_name", "text"), ("total_xp", "int"), ("scenarios_completed", "int"), ("average_score", "float")])
    T["friends"] = table_box(d, 720, 760, 560, "friendships", [("PK id", "uuid"), ("FK requester_id", "uuid"), ("FK addressee_id", "uuid"), ("status", "pending|accepted|blocked")])
    T["chat"] = table_box(d, 1360, 760, 560, "chat_messages", [("FK user_id", "uuid"), ("sender_name", "text"), ("channel_id", "text"), ("text", "text"), ("created_at", "timestamptz")])
    T["coop"] = table_box(d, 2000, 760, 520, "coop_sessions", [("PK id", "uuid"), ("FK host_user_id", "uuid"), ("scenario_id", "text"), ("status", "waiting|active|done"), ("max_players", "int")])
    T["part"] = table_box(d, 2000, 1290, 520, "coop_participants", [("PK id", "uuid"), ("FK session_id", "uuid"), ("FK user_id", "uuid"), ("role", "host|participant"), ("assigned_speaker", "text")])
    T["local"] = table_box(d, 80, 1300, 1000, "Bản lưu cục bộ: PlayerProgressDto (JSON)", [
        ("playerId, displayName, level, xp", ""), ("health, energy, hunger, thirst, sleepiness", "float"), ("yen, knowledge", "int"),
        ("activeScenarioId, activeScenarioNodeId", "string"), ("completedScenarios, storyFlags, examAttempts", "list")], GOLD)
    d.text((80, 1610), "Realtime (không lưu bảng):", font=font(24, True), fill=INK)
    for i, t in enumerate(["presence: vị trí x, y, z của người chơi khác", "broadcast chat theo kênh", "voice_lines: bảng tuỳ chọn, chưa tạo → giọng đọc dùng file local"]):
        d.text((80, 1654 + i * 36), "· " + t, font=font(22), fill=MUTED)

    def mid(box, side):
        x1, y1, x2, y2 = box
        return {"r": (x2, (y1 + y2) / 2), "l": (x1, (y1 + y2) / 2), "b": ((x1 + x2) / 2, y2), "t": ((x1 + x2) / 2, y1)}[side]

    def card(x, y, t):
        d.text((x, y), t, font=font(22, True), fill=RED)

    for k, side_u, side_t in [("profiles", "l", "r"), ("progress", "r", "l")]:
        p1, p2 = mid(T["users"], side_u), mid(T[k], side_t)
        d.line([p1, p2], fill=INK, width=3)
        card(p1[0] + (-30 if side_u == "l" else 12), p1[1] - 34, "1"); card(p2[0] + (12 if side_t == "r" else -30), p2[1] - 34, "1")
    bus = 690
    ub = mid(T["users"], "b")
    d.line([ub, (ub[0], bus)], fill=INK, width=3)
    card(ub[0] + 12, ub[1] + 6, "1")
    tops = [mid(T[k], "t") for k in ("leader", "friends", "chat", "coop")]
    d.line([(tops[0][0], bus), (tops[-1][0], bus)], fill=INK, width=3)
    for (x, y), c in zip(tops, ["1", "n", "n", "n"]):
        d.line([(x, bus), (x, y)], fill=INK, width=3)
        card(x + 12, y - 36, c)
    p1, p2 = mid(T["coop"], "b"), mid(T["part"], "t")
    d.line([p1, p2], fill=INK, width=3); card(p1[0] + 12, p1[1] + 6, "1"); card(p2[0] + 12, p2[1] - 36, "n")
    lr = mid(T["local"], "r"); pb = mid(T["progress"], "b")
    path = [lr, (1960, lr[1]), (1960, 470), (pb[0], 470), (pb[0], pb[1] + 4)]
    for q1, q2 in zip(path, path[1:]):
        dashed(d, q1, q2, GOLD, 4)
    label_on(d, 1500, lr[1], "đồng bộ khi đăng nhập: upsert progress_json", 21, "#9A6A00")
    img.save(OUT / "d18_erd.png")


# ─────────── Deployment ───────────

def deployment():
    W, H = 2600, 1500
    img, d = canvas(W, H, "Sơ đồ triển khai", "Một bản build Windows; dịch vụ trực tuyến đều tuỳ chọn và có đường lui offline")

    def nodebox(box, title, items, accent=INK, fill=PAPER):
        x1, y1, x2, y2 = box
        d.polygon([(x1, y1 + 30), (x1 + 30, y1), (x2 + 30, y1), (x2 + 30, y2 - 30), (x2, y2), (x1, y2)], fill="#DDE3EE")
        d.rectangle((x1, y1 + 30, x2, y2), fill=fill, outline=accent, width=4)
        d.polygon([(x1, y1 + 30), (x1 + 30, y1), (x2 + 30, y1), (x2, y1 + 30)], outline=accent, fill="#E8ECF4")
        d.line([(x2, y1 + 30), (x2 + 30, y1)], fill=accent, width=3)
        d.text((x1 + 24, y1 + 50), title, font=font(28, True), fill=accent)
        yy = y1 + 100
        for it in items:
            if it.startswith("[") and it.endswith("]"):
                d.rounded_rectangle((x1 + 24, yy - 6, x2 - 24, yy + 46), 10, fill=SOFT, outline=LINE, width=2)
                d.text((x1 + 40, yy + 20), it[1:-1], font=font(22, True), fill=INK, anchor="lm")
                yy += 62
            else:
                d.text((x1 + 30, yy), "· " + it, font=font(22), fill=MUTED); yy += 36

    pc = (80, 190, 1100, 1380)
    nodebox(pc, "Máy người chơi · Windows 10/11", [
        "[NihongoLife.exe — Unity 6000.3, URP]", "[Scenes: 00_Bootstrap, 01_MainMenu, 90_TestSandbox, 20/30/40/45/50]",
        "[Resources: 14 kịch bản, đề thi, ảnh vật phẩm]", "[Bản lưu JSON (persistentDataPath)]",
        "Microphone: luyện phát âm [V]", "Camera / micro: lớp học video [F8]", "Biến môi trường NIHONGOLIFE_GEMINI_API_KEY"])
    sb = (1500, 190, 2480, 640)
    nodebox(sb, "Supabase (cloud)", ["[Auth: email / ẩn danh]", "[PostgREST: profiles, player_progress, …]", "[Realtime: presence, chat]"], BLUE, "#F6F8FC")
    gm = (1500, 740, 2480, 1030)
    nodebox(gm, "Google Gemini API", ["[generateContent · gemini-flash-latest]", "chấm phát âm, NPC AI, chấm bài IELTS"], BLUE, "#F6F8FC")
    ag = (1500, 1130, 2480, 1380)
    nodebox(ag, "Agora RTC", ["[kênh nihongolife-classroom]", "token do giáo viên cấp; App ID công khai"], BLUE, "#F6F8FC")
    for (box, txt) in [(sb, "HTTPS REST + WebSocket"), (gm, "HTTPS (khoá API)"), (ag, "UDP/RTC (Agora SDK)")]:
        y = (box[1] + box[3]) / 2 + 15
        arrow(d, [(pc[2] + 4, y), (box[0] - 6, y)], color=INK, width=5)
        label_on(d, (pc[2] + box[0]) / 2, y - 30, txt, 22, INK)
    d.text((W / 2, H - 50), "Thiếu mạng hoặc thiếu khoá: game chạy offline, lưu cục bộ, NPC dùng câu soạn sẵn, bảng xếp hạng báo “chưa kết nối”.", font=font(24, True), fill=MUTED, anchor="mm")
    img.save(OUT / "d22_deployment.png")


# ─────────── Workflows (activity style via diagrams.flow) ───────────

def train_flow():
    flow("d21_train_flow", "Workflow chuyến tàu Ga Hibari → Minato", [
        ("Vào ga", "cổng phía phố, HUD đổi nơi đứng", "start"),
        ("Hỏi Kimura", "nhân viên ga: giá ¥320, sân số 2", "ui"),
        ("Mua vé", "tại quầy hoặc máy bán vé", "step"),
        ("Qua cổng soát vé", "thiếu vé → cổng chặn", "check"),
        ("Chờ tàu ở sân 2", "bảng LED, đồng hồ, cửa chắn", "step"),
        ("Lên tàu, ngồi", "HUD trên tàu: sơ đồ tuyến, chip vé", "ui"),
        ("Ngắm cảnh / trò chuyện", "Q: camera cửa sổ · F: hành khách", "step"),
        ("Dừng Gakuen-mae", "¥180 · xuống để tới trường", "step"),
        ("Tới Minato", "ra ga, đi tiếp tới Sushi Hibari", "end")],
        per=3, branches={2: ("Không đủ Yen", "báo lỗi, không trừ tiền")}, W=2000)


def dev_workflow():
    flow("d24_dev_workflow", "Quy trình làm việc và kiểm chứng của nhóm", [
        ("Yêu cầu chủ dự án", "ảnh chụp lỗi + mô tả", "start"),
        ("Thẻ TEAM_TASKS", "file:dòng, phạm vi, người phụ trách", "step"),
        ("Code + Builder", "không [MenuItem]; builder chạy batchmode", "step"),
        ("Lưu thẳng vào scene/prefab", "mở project, bấm Play là đúng", "ui"),
        ("PlayMode + EditMode test", "thao tác thật, kiểm va chạm", "step"),
        ("Ảnh chụp tự động", "Bao_Cao/*-regression", "ui"),
        ("Xem ảnh, đối chiếu yêu cầu", "lỗi hình → sửa lại", "check"),
        ("Codex kiểm tra độc lập", "chạy lại test, audit", "step"),
        ("Cập nhật tài liệu", "TEAM_TASKS · slide · báo cáo", "end")],
        per=3, branches={6: ("Chưa đạt", "quay lại bước 3")}, W=2000)


if __name__ == "__main__":
    use_case(); class_diagram()
    seq_dialogue(); seq_checkout(); seq_minigame(); seq_ai(); seq_zone()
    state_stamina(); state_minigame(); erd(); deployment(); train_flow(); dev_workflow()
    print("ok")
