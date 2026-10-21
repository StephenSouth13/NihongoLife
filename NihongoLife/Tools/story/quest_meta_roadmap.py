# -*- coding: utf-8 -*-
"""Quest metadata for the map roadmap (2026-10-21): the two gateway quests (Game Center, Midori Island) and the
knowledge requirements that line up with LocationUnlocks (Sushi 100, Station 160, Game Center 240, Island 320, festival
380). Same block format as quest_meta.py, which predates these quests; run after regenerating either quest.
Also inserts the new quests into the campaign order of the control database."""
import os
import re

B = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'NihongoLife')

FESTIVAL_REQ = ["scenario.station.buy_ticket", "scenario.island.midori_trip", "scenario.neighborhood.cat_followup",
                "scenario.neighborhood.recycling_morning", "scenario.restaurant.order_ramen"]

Q = {
    "scenario_arcade_kim_invite": dict(
        type="main", giver="npc_classmate_kim", reward=300, know=240, req=["scenario.station.buy_ticket"], unlock=["scenario.island.midori_trip"],
        vi="Sau giờ học, Kim rủ bạn ra Game Center. Rủ rê, đếm số lần (〜回), đếm vé (〜まい) và đổi quà bằng tiếng Nhật.",
        en="After class Kim invites you to the Game Center. Invite, count times (~kai) and tickets (~mai), and swap for a prize in Japanese.",
        ja="放課後、キムさんがゲームセンターにさそってくれます。〜回・〜まいで数えて、けいひんを日本語でもらいましょう。",
        wvi="Lớp さくら (gặp Kim), rồi Trung tâm trò chơi Hibari", wen="Sakura class (meet Kim), then Game Center Hibari", wja="さくらクラス → ゲームセンター"),
    "scenario_island_midori_trip": dict(
        type="main", giver="npc_neighbor_1", reward=500, know=320, req=["scenario.arcade.kim_invite"], unlock=["scenario.town.summer_festival"],
        vi="Tanaka mời bạn ra ruộng của bà anh ấy trên đảo Midori. Đi tàu, ngắm biển, học xới đất, gieo hạt, tưới nước.",
        en="Tanaka invites you to his grandmother's field on Midori Island. Take the train, watch the sea, learn to till, sow and water.",
        ja="田中さんがみどり島のおばあちゃんの畑にさそってくれます。電車で行って、たがやす・まく・水をやるを学びます。",
        wvi="Gặp Tanaka ở phố, rồi Đảo Xanh", wen="Meet Tanaka in town, then Midori Island", wja="町で田中さん → みどり島"),
}
KNOW = {"scenario_restaurant_sushi_dining": 100, "scenario_station_buy_ticket": 160, "scenario_town_summer_festival": 380}


def y(v):
    return '"%s"' % v.replace('"', '\\"')


def block(m):
    lines = ["  questType: %s" % m["type"], "  giverNpcId: %s" % m["giver"],
             "  briefingVi: %s" % y(m["vi"]), "  briefingEn: %s" % y(m["en"]), "  briefingJa: %s" % y(m["ja"]),
             "  locationHintVi: %s" % y(m["wvi"]), "  locationHintEn: %s" % y(m["wen"]), "  locationHintJa: %s" % y(m["wja"]),
             "  rewardYen: %d" % m["reward"], "  branchId: main", "  requiredKnowledge: %d" % m["know"],
             "  requiredScenarioIds:"] + ["  - %s" % r for r in m["req"]] + ["  unlockScenarioIds:"] + ["  - %s" % r for r in m["unlock"]] + \
            ["  repeatable: 0", "  supportsCoOp: 0", "  minPlayers: 1", "  maxPlayers: 2"]
    return "\n".join(lines) + "\n"


KEYS = ["questType", "giverNpcId", "briefingVi", "briefingEn", "briefingJa", "locationHintVi", "locationHintEn", "locationHintJa",
        "rewardYen", "branchId", "requiredKnowledge", "requiredScenarioIds", "unlockScenarioIds", "repeatable", "supportsCoOp", "minPlayers", "maxPlayers"]


def load(name):
    p = os.path.join(B, "Resources", "Scenarios", name + ".asset")
    raw = open(p, "rb").read().decode("utf-8")
    return p, raw.replace("\r\n", "\n"), "\r\n" in raw


def save(p, s, crlf):
    open(p, "wb").write((s.replace("\n", "\r\n") if crlf else s).encode("utf-8"))


for name, m in Q.items():
    p, s, crlf = load(name)
    for k in KEYS:
        s = re.sub(r"^  %s:.*\n(?:  - .*\n)*" % k, "", s, flags=re.M)
    anchor = re.search(r"^  chapterIndex: .*\n", s, flags=re.M)
    s = s[:anchor.end()] + block(m) + s[anchor.end():]
    save(p, s, crlf)
    print("meta", name)

for name, know in KNOW.items():
    p, s, crlf = load(name)
    s, n = re.subn(r"^  requiredKnowledge: \d+$", "  requiredKnowledge: %d" % know, s, flags=re.M)
    assert n == 1, name
    if name == "scenario_town_summer_festival":
        s = re.sub(r"^  requiredScenarioIds:.*\n(?:  - .*\n)*", "  requiredScenarioIds:\n" + "".join("  - %s\n" % r for r in FESTIVAL_REQ), s, flags=re.M)
    if name == "scenario_station_buy_ticket":
        s = re.sub(r"^  unlockScenarioIds:.*\n(?:  - .*\n)*", "  unlockScenarioIds:\n  - scenario.arcade.kim_invite\n", s, flags=re.M)
    save(p, s, crlf)
    print("knowledge", name, know)

# Campaign order: the two new quests before the festival.
p = os.path.join(B, "Resources", "Control", "NihongoLifeControlDatabase.asset")
raw = open(p, "rb").read().decode("utf-8")
crlf = "\r\n" in raw
s = raw.replace("\r\n", "\n")
if "scenario.arcade.kim_invite" not in s:
    s = s.replace("  - scenario.station.buy_ticket\n  - scenario.town.summer_festival\n",
                  "  - scenario.station.buy_ticket\n  - scenario.arcade.kim_invite\n  - scenario.island.midori_trip\n  - scenario.town.summer_festival\n")
    assert "scenario.arcade.kim_invite" in s
save(p, s, crlf)
print("campaign ok")
