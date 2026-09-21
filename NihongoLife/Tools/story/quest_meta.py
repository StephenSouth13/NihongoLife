import re

B = r"D:\VTC_Academy\NihongoLife\NihongoLife\NihongoLife\Assets\NihongoLife"

# id -> metadata. Requirements mirror the campaign order so the journal never contradicts the story flow.
Q = {
    "scenario.intro.arrival": dict(
        file="scenario_intro_arrival", type="main", giver="", reward=0, know=0, req=[], unlock=["scenario.street.first_talk"], coop=False,
        vi="Bạn vừa đến Hibari-chō với tư cách du học sinh. Hãy bước ra phố và làm quen với nơi mình sẽ sống.",
        en="You have just arrived in Hibari-cho as an international student. Step out and get to know your new neighbourhood.",
        ja="留学生としてひばり町に着きました。外に出て、これから住む町に慣れましょう。",
        wvi="Sakura-dōri (con phố chính của khu phố)", wen="Sakura-dori (the main street)", wja="さくら通り"),
    "scenario.street.first_talk": dict(
        file="scenario_street_first_talk", type="main", giver="npc_neighbor_1", reward=200, know=0, req=["scenario.intro.arrival"], unlock=["scenario.house1.greeting"], coop=False,
        vi="Một người hàng xóm thân thiện chào bạn trên phố. Hãy đáp lại bằng tiếng Nhật và luyện phát âm.",
        en="A friendly neighbour greets you on the street. Answer in Japanese and practise your pronunciation.",
        ja="親切な隣人が通りで声をかけてくれます。日本語で答えて、発音も練習しましょう。",
        wvi="Sakura-dōri, gần nhà bạn", wen="Sakura-dori, near your home", wja="さくら通り（家の近く）"),
    "scenario.house1.greeting": dict(
        file="scenario_house1_greeting", type="main", giver="npc_neighbor_1", reward=100, know=0, req=["scenario.street.first_talk"], unlock=["scenario.school.self_intro"], coop=False,
        vi="Tanaka mời bạn ghé nhà. Chào hỏi đúng phép lịch sự khi đến nhà người khác.",
        en="Tanaka invites you over. Greet properly when you visit someone's home.",
        ja="田中さんが家に招いてくれました。人の家を訪ねるときの挨拶をしましょう。",
        wvi="Nhà Tanaka (sát vách)", wen="Tanaka's house (next door)", wja="田中さんの家（隣）"),
    "scenario.school.self_intro": dict(
        file="scenario_school_self_intro", type="main", giver="npc_teacher_morita", reward=300, know=0, req=["scenario.house1.greeting"], unlock=["scenario.konbini.buy_onigiri"], coop=True,
        vi="Buổi học đầu tiên tại ひばり日本語学院. Tự giới thiệu với cô Morita và bạn Kim: tên, quê hương, sở thích.",
        en="Your first class at Hibari Nihongo Gakuin. Introduce yourself to Ms. Morita and Kim: name, home country, hobby.",
        ja="ひばり日本語学院での最初の授業。森田先生とキムさんに、名前・出身・趣味を自己紹介しましょう。",
        wvi="ひばり日本語学院, lớp さくら", wen="Hibari Nihongo Gakuin, Sakura class", wja="ひばり日本語学院 さくらクラス"),
    "scenario.konbini.buy_onigiri": dict(
        file="scenario_konbini_buy_onigiri", type="main", giver="npc_cashier", reward=400, know=0, req=["scenario.school.self_intro"], unlock=["scenario.house2.lostcat", "scenario.house3.garbage", "scenario.konbini.evening_shift"], coop=False,
        vi="Bạn đói rồi. Vào ひばりコンビニ, tìm onigiri và nước uống, rồi thanh toán bằng tiếng Nhật.",
        en="You are hungry. Go into Hibari Konbini, find an onigiri and a drink, and pay in Japanese.",
        ja="お腹がすきました。ひばりコンビニでおにぎりと飲み物を探し、日本語でお会計をしましょう。",
        wvi="ひばりコンビニ (Sakura-dōri)", wen="Hibari Konbini (Sakura-dori)", wja="ひばりコンビニ（さくら通り）"),
    "scenario.house2.lostcat": dict(
        file="scenario_house2_lostcat", type="community", giver="npc_neighbor_2", reward=300, know=40, req=["scenario.konbini.buy_onigiri"], unlock=["scenario.neighborhood.cat_followup"], coop=False,
        vi="Suzuki lo lắng vì chú mèo bị lạc. Hãy hỏi thăm và nghe cô ấy tả con mèo bằng tiếng Nhật.",
        en="Suzuki is worried about her lost cat. Ask about it and listen to her describe the cat in Japanese.",
        ja="鈴木さんは迷子の猫を心配しています。声をかけて、猫の特徴を日本語で聞きましょう。",
        wvi="Nhà Suzuki", wen="Suzuki's house", wja="鈴木さんの家"),
    "scenario.house3.garbage": dict(
        file="scenario_house3_garbage", type="community", giver="npc_neighbor_3", reward=200, know=40, req=["scenario.konbini.buy_onigiri"], unlock=["scenario.neighborhood.recycling_morning"], coop=False,
        vi="Sato, trưởng khu phố, giải thích quy tắc đổ rác. Hỏi đúng cách để không làm phiền hàng xóm.",
        en="Sato, the neighbourhood head, explains the garbage rules. Ask the right way so you do not bother your neighbours.",
        ja="町内会長の佐藤さんがゴミの出し方を説明してくれます。近所に迷惑をかけないよう、正しく聞きましょう。",
        wvi="Nhà Sato (trưởng khu phố)", wen="Sato's house (neighbourhood head)", wja="佐藤さんの家（町内会長）"),
    "scenario.restaurant.sushi_dining": dict(
        file="scenario_restaurant_sushi_dining", type="main", giver="npc_sushi_staff", reward=0, know=60, req=["scenario.konbini.buy_onigiri"], unlock=["scenario.station.buy_ticket"], coop=True,
        vi="Lần đầu ăn ở nhà hàng sushi: chào hỏi, gọi món, ăn, tính tiền. Học cách nói chuyện ở quán ăn Nhật.",
        en="Your first meal at a sushi restaurant: greet, order, eat and pay. Learn how to talk in a Japanese restaurant.",
        ja="初めての寿司店：挨拶、注文、食事、お会計。日本のレストランでの話し方を学びます。",
        wvi="ひばり寿司 (cổng vào quán ở khu phố)", wen="Hibari Sushi (entrance in town)", wja="ひばり寿司"),
    "scenario.restaurant.order_ramen": dict(
        file="scenario_restaurant_order_ramen", type="side", giver="npc_neighbor_1", reward=0, know=60, req=["scenario.konbini.buy_onigiri"], unlock=["scenario.town.summer_festival"], coop=False,
        vi="Tanaka dẫn bạn tới quán ramen của ông Yamada. Gọi món và trả tiền theo cách khác với nhà hàng sushi.",
        en="Tanaka takes you to Mr. Yamada's ramen shop. Order and pay in a more casual way than at the sushi restaurant.",
        ja="田中さんが山田さんのラーメン店に案内してくれます。寿司店よりカジュアルな注文と支払いを学びます。",
        wvi="やまだ食堂 (quán ramen)", wen="Yamada Shokudo (ramen shop)", wja="やまだ食堂"),
    "scenario.neighborhood.cat_followup": dict(
        file=None, type="community", giver="npc_neighbor_2", reward=400, know=50, req=["scenario.house2.lostcat"], unlock=["scenario.town.summer_festival"], coop=False,
        vi="Bạn thấy manh mối về chú mèo. Báo cho Suzuki và tả vị trí bằng tiếng Nhật.",
        en="You spotted a clue about the cat. Tell Suzuki and describe the place in Japanese.",
        ja="猫の手がかりを見つけました。鈴木さんに伝え、場所を日本語で説明しましょう。",
        wvi="Nhà Suzuki", wen="Suzuki's house", wja="鈴木さんの家"),
    "scenario.neighborhood.recycling_morning": dict(
        file=None, type="community", giver="npc_neighbor_3", reward=300, know=50, req=["scenario.house3.garbage"], unlock=["scenario.town.summer_festival"], coop=False,
        vi="Sáng thứ hai là ngày thu gom rác tái chế. Phân loại đúng và hỏi Sato khi chưa chắc.",
        en="Monday morning is recycling day. Sort correctly and ask Sato when you are unsure.",
        ja="月曜の朝はリサイクルの日。正しく分別し、迷ったら佐藤さんに聞きましょう。",
        wvi="Điểm tập kết rác gần nhà Sato", wen="The collection point near Sato's house", wja="佐藤さんの家の近くのゴミ置き場"),
    "scenario.konbini.evening_shift": dict(
        file=None, type="career", giver="npc_cashier", reward=1200, know=50, req=["scenario.konbini.buy_onigiri"], unlock=[], coop=False,
        vi="Ito nhờ bạn phụ ca tối ở cửa hàng. Học cách nói với khách khi hết hàng và gợi ý món khác.",
        en="Ito asks you to help with the evening shift. Learn what to say when an item is out of stock and how to suggest another.",
        ja="伊藤さんに夜のシフトを頼まれました。品切れのときの言い方と、別の商品のすすめ方を学びます。",
        wvi="ひばりコンビニ", wen="Hibari Konbini", wja="ひばりコンビニ"),
    "scenario.station.buy_ticket": dict(
        file="scenario_station_buy_ticket", type="main", giver="npc_station_staff", reward=300, know=80, req=["scenario.restaurant.sushi_dining"], unlock=["scenario.town.summer_festival"], coop=False,
        vi="Hỏi đường và mua vé tại ga Hibari với nhân viên Kimura.",
        en="Ask for directions and buy a ticket at Hibari Station from Kimura.",
        ja="ひばり駅で木村さんに道を聞き、切符を買いましょう。",
        wvi="ひばり駅 (ga Hibari)", wen="Hibari Station", wja="ひばり駅"),
    "scenario.town.summer_festival": dict(
        file="scenario_town_summer_festival", type="main", giver="npc_neighbor_3", reward=1000, know=150, req=["scenario.station.buy_ticket", "scenario.neighborhood.cat_followup", "scenario.neighborhood.recycling_morning", "scenario.restaurant.order_ramen"], unlock=[], coop=True,
        vi="Lễ hội mùa hè của khu phố. Những người bạn bạn đã giúp đỡ đều có mặt để cảm ơn bạn.",
        en="The neighbourhood summer festival. The people you helped are all there to thank you.",
        ja="町の夏祭り。これまで助けた人たちが、あなたにお礼を言いに集まります。",
        wvi="ひばり神社 (đền Hibari)", wen="Hibari Shrine", wja="ひばり神社"),
}


def y(v):
    return '"%s"' % v.replace('"', '\\"')


def block(m):
    lines = [
        "  questType: %s" % m["type"],
        "  giverNpcId: %s" % (m["giver"] or '""'),
        "  briefingVi: %s" % y(m["vi"]),
        "  briefingEn: %s" % y(m["en"]),
        "  briefingJa: %s" % y(m["ja"]),
        "  locationHintVi: %s" % y(m["wvi"]),
        "  locationHintEn: %s" % y(m["wen"]),
        "  locationHintJa: %s" % y(m["wja"]),
        "  rewardYen: %d" % m["reward"],
        "  branchId: %s" % {"main": "main", "side": "side", "community": "community", "career": "career.retail"}[m["type"]],
        "  requiredKnowledge: %d" % m["know"],
    ]
    lines.append("  requiredScenarioIds:" + ("" if m["req"] else " []"))
    lines += ["  - %s" % r for r in m["req"]]
    lines.append("  unlockScenarioIds:" + ("" if m["unlock"] else " []"))
    lines += ["  - %s" % r for r in m["unlock"]]
    lines.append("  repeatable: 0")
    lines.append("  supportsCoOp: %d" % (1 if m["coop"] else 0))
    lines.append("  minPlayers: %d" % (2 if m["coop"] else 1))
    lines.append("  maxPlayers: 2")
    return "\n".join(lines) + "\n"


KEYS = ["questType", "giverNpcId", "briefingVi", "briefingEn", "briefingJa", "locationHintVi", "locationHintEn", "locationHintJa",
        "rewardYen", "branchId", "requiredKnowledge", "requiredScenarioIds", "unlockScenarioIds", "repeatable", "supportsCoOp", "minPlayers", "maxPlayers", "speakerRoles"]

for sid, m in Q.items():
    if not m["file"]:
        continue
    p = B + r"\Resources\Scenarios\%s.asset" % m["file"]
    raw = open(p, "rb").read().decode("utf-8")
    crlf = "\r\n" in raw
    s = raw.replace("\r\n", "\n")
    # drop any existing lines/list items for these keys (top-level scalar or list)
    for k in KEYS:
        s = re.sub(r"^  %s:.*\n(?:  - .*\n)*" % k, "", s, flags=re.M)
    anchor = re.search(r"^  chapterIndex: .*\n", s, flags=re.M)
    assert anchor, p
    s = s[:anchor.end()] + block(m) + s[anchor.end():]
    open(p, "wb").write((s.replace("\n", "\r\n") if crlf else s).encode("utf-8"))

print("assets ok")

# built-in (code) scenarios
p = B + r"\Scripts\Scenario\BuiltInStoryScenarioCatalog.cs"
raw = open(p, "rb").read().decode("utf-8")
crlf = "\r\n" in raw
s = raw.replace("\r\n", "\n")
assert "ApplyQuest(" not in s


def cs(v):
    return '"%s"' % v.replace('\\', '\\\\').replace('"', '\\"')


def call(sid, chapter):
    m = Q[sid]
    req = ", ".join(cs(x) for x in m["req"])
    unl = ", ".join(cs(x) for x in m["unlock"])
    return ("            ApplyQuest(scenario, %s, %s, %d, %d, new[] { %s }, new[] { %s },\n"
            "                %s, %s, %s,\n                %s, %s, %s);\n") % (
        cs(m["type"]), cs(m["giver"]), m["reward"], m["know"], req, unl,
        cs(m["vi"]), cs(m["en"]), cs(m["ja"]), cs(m["wvi"]), cs(m["wen"]), cs(m["wja"]))


for sid, marker, chapter in (
    ("scenario.neighborhood.cat_followup", "            scenario.startNodeId = \"node_find_suzuki\";\n", 3),
    ("scenario.neighborhood.recycling_morning", "            scenario.startNodeId = \"node_find_sato\";\n", 3),
    ("scenario.konbini.evening_shift", "            scenario.startNodeId = \"node_find_ito\";\n", 3),
):
    assert marker in s, marker
    s = s.replace(marker, call(sid, chapter) + marker, 1)

s = s.replace('Create("scenario.neighborhood.cat_followup", 4,', 'Create("scenario.neighborhood.cat_followup", 3,', 1)
s = s.replace('Create("scenario.neighborhood.recycling_morning", 5,', 'Create("scenario.neighborhood.recycling_morning", 3,', 1)
s = s.replace('Create("scenario.konbini.evening_shift", 6,', 'Create("scenario.konbini.evening_shift", 3,', 1)

helper = """        private static void ApplyQuest(ScenarioDefinition scenario, string questType, string giverNpcId, int rewardYen, int requiredKnowledge,
            string[] requiredScenarioIds, string[] unlockScenarioIds, string briefingVi, string briefingEn, string briefingJa,
            string locationVi, string locationEn, string locationJa)
        {
            scenario.questType = questType;
            scenario.giverNpcId = giverNpcId;
            scenario.rewardYen = rewardYen;
            scenario.requiredKnowledge = requiredKnowledge;
            scenario.requiredScenarioIds = new List<string>(requiredScenarioIds);
            scenario.unlockScenarioIds = new List<string>(unlockScenarioIds);
            scenario.branchId = questType == "career" ? "career.retail" : questType;
            scenario.briefingVi = briefingVi;
            scenario.briefingEn = briefingEn;
            scenario.briefingJa = briefingJa;
            scenario.locationHintVi = locationVi;
            scenario.locationHintEn = locationEn;
            scenario.locationHintJa = locationJa;
        }

"""
marker = "        private static ScenarioDefinition Create(string id, int chapter"
s = s.replace(marker, helper + marker, 1)
open(p, "wb").write((s.replace("\n", "\r\n") if crlf else s).encode("utf-8"))
print("built-ins ok")
