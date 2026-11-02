# -*- coding: utf-8 -*-
# Usage: python gen.py <Assets/NihongoLife dir> <Docs dir>. Writes Resources/Story/storyline.json, merges story quests
# into Resources/Progression/progression.json and Russian lines into ru_table_extra.json, writes Docs/STORY.md.
import json, sys, os, io
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from content import TIERS, LANG, SPEAKERS, MAIN, ARCS
from content_scenes import CHAPTER_SCENES, TIER_SCENES, OWNER_CALLBACK, BOND_THRESHOLD
SPEAKERS = dict(SPEAKERS, narrator=("ナレーション", "Dẫn truyện", "Narrator", "Рассказчик"))
A, DOCS = sys.argv[1], sys.argv[2]
P = os.path.join(A, 'Resources', 'Progression')
careers = json.load(io.open(os.path.join(P, 'careers.json'), encoding='utf-8'))
prog = json.load(io.open(os.path.join(P, 'progression.json'), encoding='utf-8'))
rupath = os.path.join(A, 'Resources', 'Content', 'Localization', 'ru_table_extra.json')
ru = json.load(io.open(rupath, encoding='utf-8'))
C = {c['id']: c for c in careers['careers']}
K = dict(LANG)
RU = {}
def r(en, rus): RU[en] = rus

EV = {  # event: (en, vi, ja, ru) with {n}
 'serve': ("Serve {n} guests", "Phục vụ {n} khách", "お客さまを{n}組接客する", "Обслужите гостей: {n}"),
 'triage': ("Triage {n} patients", "Phân loại {n} bệnh nhân", "患者さんを{n}人トリアージする", "Проведите сортировку пациентов: {n}"),
 'checkout': ("Ring up {n} customers", "Tính tiền cho {n} khách", "{n}人のお会計をする", "Пробейте покупки клиентам: {n}"),
 'cook': ("Cook {n} dishes", "Nấu {n} món", "料理を{n}品作る", "Приготовьте блюда: {n}"),
 'harvest': ("Harvest {n} crops", "Thu hoạch {n} cây", "作物を{n}回収穫する", "Соберите урожай: {n}"),
 'perform': ("Play {n} songs on stage", "Biểu diễn {n} bài", "ステージで{n}曲演奏する", "Сыграйте песни на сцене: {n}"),
 'mix': ("Play {n} DJ sets", "Chơi {n} set DJ", "DJセットを{n}回かける", "Отыграйте DJ-сеты: {n}"),
 'paint': ("Finish {n} paintings", "Hoàn thành {n} bức tranh", "絵を{n}枚仕上げる", "Закончите картины: {n}"),
 'fold': ("Tidy {n} clothes tables", "Gấp gọn {n} bàn quần áo", "テーブルを{n}つたたむ", "Сложите одежду на столах: {n}"),
 'learn': ("Finish {n} lessons", "Học xong {n} bài", "レッスンを{n}回終える", "Пройдите уроки: {n}"),
 'order': ("Order a meal in Japanese", "Gọi món bằng tiếng Nhật", "日本語で注文する", "Закажите еду по-японски"),
}
def ev_text(e, n):
    t = EV[e]; out = [x.replace('{n}', str(n)) for x in t]
    r(out[0], out[3]); return out
def talk_text(sp, report):
    ja, vi, en, rus = SPEAKERS[sp]
    if report: t = ("Report to " + en, "Báo cáo " + vi, ja + "に報告する", "Доложите: " + rus)
    else: t = ("Talk to " + en, "Nói chuyện với " + vi, ja + "と話す", "Поговорите: " + rus)
    r(t[0], t[3]); return t
LOC = {  # scene: (en, vi, ja, ru)
 '56_FashionStore': ("Hibari Mode", "Hibari Mode", "ひばりモード", "Hibari Mode"),
 '90_TestSandbox': ("Hibari-chō", "Hibari-chō", "ひばり町", "Хибари-тё"),
 '40_ HIBARICLASS': ("Hibari School, Sakura class", "Trường Hibari, lớp Sakura", "ひばり学院さくらクラス", "Школа Хибари, класс Сакура"),
 '30_SushiRestaurant': ("Sushi Hibari", "Sushi Hibari", "ひばり寿司", "«Суси Хибари»"),
 '35_HibariClinic': ("Hibari Clinic", "Phòng khám Hibari", "ひばりクリニック", "Клиника Хибари"),
 '60_MidoriIsland': ("Midori Island farm", "Nông trại đảo Midori", "みどり島の農園", "Ферма на острове Мидори"),
 '65_PhuQuocIsland': ("Gánh Ngon Restaurant, Phú Quốc", "Nhà hàng Gánh Ngon, Phú Quốc", "ガンゴン（フーコック）", "Ресторан «Ганьнгон», Фукуок"),
 '55_MusicShop': ("The Hibiki music shop", "Tiệm nhạc Hibiki", "ひびき楽器店", "Музыкальный магазин «Хибики»"),
}
for v in LOC.values(): r(v[0], v[3])
for sp, (ja, vi, en, rus) in SPEAKERS.items(): r(en, rus)
JNOTE = ("Language level: JLPT {0}.", "Trình độ tiếng: JLPT {0}.", "日本語レベル：JLPT {0}。", "Уровень языка: JLPT {0}.")

def line(sp, t, flag=''):
    r(t[2], t[3]); d = dict(speaker=sp, ja=t[0], vi=t[1], en=t[2], ru=t[3])
    if flag: d['flag'] = flag
    return d
def lines(sp, ls): return [line(sp, t) for t in ls]
def ending_line(e):  # (speaker, ja, vi, en, ru[, flag condition])
    return line(e[0], e[1:5], e[5] if len(e) > 5 else '')
def cutscene(sp, spec, flag_prefix, ending, extra_ending=()):
    """Cutscene after the quest's lines: the boss asks, the player answers (flag + affinity), the boss replies, then the ending."""
    choices = []
    for oid, text, reply, aff in spec['options']:
        r(text[2], text[3])
        choices.append(dict(id=oid, ja=text[0], vi=text[1], en=text[2], ru=text[3], flag=flag_prefix + oid, affinity=aff, reply=line(sp, reply)))
    return dict(prompt=line(sp, spec['prompt']), choices=choices, ending=list(ending) + list(extra_ending))

def quest(qid, title, desc, jlpt, sp, scene, objectives, requires, min_level, reward, career=None, clevel=1):
    en_desc = desc[0] + " " + JNOTE[0].format(jlpt)
    r(title[0], title[3]); r(en_desc, desc[3] + " " + JNOTE[3].format(jlpt))
    ja, vi, en, rus = SPEAKERS[sp]; loc = LOC[scene]
    q = dict(id=qid, category='story', titleVi=title[1], titleEn=title[0], titleJa=title[2],
             descriptionVi=desc[1] + " " + JNOTE[1].format(jlpt), descriptionEn=en_desc, descriptionJa=desc[2] + JNOTE[2].format(jlpt),
             giverNpc=sp, giverNameVi=vi, giverNameEn=en, giverNameJa=ja,
             locationVi=loc[1], locationEn=loc[0], locationJa=loc[2], scene=scene,
             minLevel=min_level, minKnowledge=K[jlpt], requiresQuests=requires, repeatable=False, cooldownMinutes=0,
             grantOnAccept=[], objectives=objectives, rewards=reward)
    if career: q['career'] = career; q['careerLevel'] = clevel
    return q

def obj(oid, e, target, n, text, after=False):
    return dict(id=oid, event=e, target=target, count=n, afterAll=after, textVi=text[1], textEn=text[0], textJa=text[2])

story = dict(schema='nihongolife.story.v1',
    languageLevels=[dict(id=i, knowledge=k) for i, k in LANG],
    tiers=[dict(id=t[0], careerLevel=t[1], jlpt=t[2], nameEn=t[4], nameVi=t[5], nameJa=t[6], nameRu=t[7]) for t in TIERS],
    speakers=[dict(id=k, nameJa=v[0], nameVi=v[1], nameEn=v[2], nameRu=v[3]) for k, v in SPEAKERS.items()],
    chapters=[], arcs=[], bondThreshold=BOND_THRESHOLD)
for t in TIERS: r(t[4], t[7])
quests = []
prev = []
for i, (cid, jlpt, minlvl, (e, tgt, n), sp, scene, title, summ, ls) in enumerate(MAIN, 1):
    qid = 'story_ch%02d_%s' % (i, cid)
    if e == 'talk': objs = [obj('talk', 'talk', tgt, 1, talk_text(tgt, False))]
    else: objs = [obj(e, e, tgt, n, ev_text(e, n)), obj('report', 'talk', sp, 1, talk_text(sp, True), True)]
    quests.append(quest(qid, title, summ, jlpt, sp, scene, objs, prev, minlvl, dict(yen=300 * i, xp=60 + 20 * i, knowledge=15 + 5 * i)))
    story['chapters'].append(dict(id=cid, order=i, questId=qid, jlpt=jlpt, minLevel=minlvl, speaker=sp, scene=scene,
        titleEn=title[0], titleVi=title[1], titleJa=title[2], titleRu=title[3],
        summaryEn=summ[0], summaryVi=summ[1], summaryJa=summ[2], summaryRu=summ[3], lines=lines(sp, ls),
        cutscene=cutscene(sp, CHAPTER_SCENES[cid], 'story_choice_%s_' % cid, [ending_line(e) for e in CHAPTER_SCENES[cid]['ending']])))
    prev = [qid]
CHOICE = 'story_ch05_career_choice'
assert any(q['id'] == CHOICE for q in quests)
assert set(CHAPTER_SCENES) == {c['id'] for c in story['chapters']}, 'every chapter needs a cutscene'
for career, (sp, scene, (e, tgt), stages) in ARCS.items():
    assert career in C, career
    arc = dict(career=career, speaker=sp, scene=scene, stages=[])
    prev = [CHOICE]
    for k, (tier, (title, goal, ls)) in enumerate(zip(TIERS, stages)):
        tid, clevel, jlpt = tier[0], tier[1], tier[2]
        assert clevel <= len(C[career]['levels']) + max(len(b['levels']) for b in C[career]['branches'])
        qid = 'story_%s_%s' % (career, tid)
        n = 2 + k
        objs = [obj('work', e, tgt, n, ev_text(e, n)), obj('report', 'talk', sp, 1, talk_text(sp, True), True)]
        quests.append(quest(qid, title, goal, jlpt, sp, scene, objs, prev, 10, dict(yen=600 + 500 * k, xp=80 + 40 * k, knowledge=20 + 10 * k), career, clevel))
        arc['stages'].append(dict(tier=tid, careerLevel=clevel, jlpt=jlpt, questId=qid,
            titleEn=title[0], titleVi=title[1], titleJa=title[2], titleRu=title[3],
            goalEn=goal[0], goalVi=goal[1], goalJa=goal[2], goalRu=goal[3], lines=lines(sp, ls),
            cutscene=cutscene(sp, TIER_SCENES[tid], 'story_%s_%s_' % (career, tid), [line('narrator', TIER_SCENES[tid]['ending'])],
                [line(sp, t, 'story_%s_trainee_%s' % (career, o)) for o, t in OWNER_CALLBACK.items()] if tid == 'owner' else ())))
        prev = [qid]
    story['arcs'].append(arc)

ids = {q['id'] for q in quests}
prog['quests'] = [q for q in prog['quests'] if q['id'] not in ids and not q['id'].startswith('story_')] + quests
os.makedirs(os.path.join(A, 'Resources', 'Story'), exist_ok=True)
io.open(os.path.join(A, 'Resources', 'Story', 'storyline.json'), 'w', encoding='utf-8', newline='\n').write(json.dumps(story, ensure_ascii=False, indent=2) + '\n')
io.open(os.path.join(P, 'progression.json'), 'w', encoding='utf-8', newline='\n').write(json.dumps(prog, ensure_ascii=False, indent=2) + '\n')
have = {e['en'] for e in ru['entries']}
added = 0
for en, rus in RU.items():
    if en not in have: ru['entries'].append(dict(en=en, ru=rus)); added += 1
if 'count' in ru: ru['count'] = len(ru['entries'])
io.open(rupath, 'w', encoding='utf-8', newline='\n').write(json.dumps(ru, ensure_ascii=True, indent=2) + '\n')

# Docs/STORY.md
md = ["# Story — Nihongo Life", "",
 "> Generated from `Assets/NihongoLife/Resources/Story/storyline.json` by `Tools/story/gen.py`; edit the content there, rerun, then run the EditMode tests `StorylineTests`.", "",
 "The world and cast are defined in `STORY_BIBLE.md` (Hibari-chō, the N5 scenarios). This file is the long story that runs on top of it: a **main storyline** of %d chapters and **%d career arcs**, each with five stages from trainee to owner/specialist. Every chapter and stage is a real quest in `progression.json` (category `story`), so the Task Journal (N), quest tracking, rewards and the quest HUD handle it with no extra system." % (len(MAIN), len(ARCS)), "",
 "## Unlock rules", "",
 "- **Language level** comes from Trí tuệ (knowledge): " + ", ".join("JLPT %s ≥ %d" % (i, k) for i, k in LANG) + ". A quest's `minKnowledge` is the threshold of its JLPT level; the journal shows what is missing.",
 "- **Main chapters** unlock in order (`requiresQuests`) and at a player level (`minLevel`).",
 "- **Career arcs** open after chapter 5 (Choosing a career). Each stage needs the career (`career`) at its career level (`careerLevel`, from careers.json), its JLPT level and the stage before it:", "",
 "| Stage | Career level | JLPT | Knowledge |", "|---|---|---|---|"]
for t in TIERS: md.append("| %s (%s) | %d | %s | %d |" % (t[4], t[5], t[1], t[2], t[3]))
md += ["", "Career levels 4–6 come from the branch chosen at the level-4 interview (careers.json `branches`), so the manager and owner/specialist stages apply to either branch (e.g. Medicine: resident → specialty trainee → specialist and clinic director, or the pharmacist branch).", "", "## Main storyline", ""]
for c in story['chapters']:
    md += ["### %d. %s — %s (`%s`, JLPT %s, level %d)" % (c['order'], c['titleEn'], c['titleJa'], c['questId'], c['jlpt'], c['minLevel']), "", c['summaryEn'], ""]
    for l in c['lines']: md.append("- **%s**: %s — *%s*" % (SPEAKERS[l['speaker']][2], l['ja'], l['en']))
    sc = c["cutscene"]; md += ["", "*Choice* — %s" % sc['prompt']['en']]
    for ch in sc['choices']: md.append("  - %s → %s (`%s`, affinity +%d)" % (ch['en'], ch['reply']['en'], ch['flag'], ch['affinity']))
    md.append("*Ending*")
    for l in sc['ending']: md.append("  - **%s**: %s%s" % (SPEAKERS[l['speaker']][2], l['en'], " _(if `%s`)_" % l['flag'] if l.get('flag') else ""))
    md.append("")
md += ["## Career arcs", ""]
for a in story['arcs']:
    cc = C[a['career']]
    md += ["### %s — %s (%s, boss: %s)" % (cc['nameEn'], cc['nameJa'], cc['workplaceEn'], SPEAKERS[a['speaker']][2]), ""]
    for s in a['stages']:
        md += ["**%s** · career level %d · JLPT %s · `%s` — %s" % (s['tier'], s['careerLevel'], s['jlpt'], s['questId'], s['goalEn']), ""]
        for l in s['lines']: md.append("- %s — *%s*" % (l['ja'], l['en']))
        md.append("")
md += ["## Cutscenes, choices and relationships", "",
 "When a story quest is completed, `StoryScenes` plays its scene in the normal dialogue window: the quest's lines, the boss's question, the player's answer (a story flag + affinity with that person, `SocialStanding`), the reply and the ending. Ending lines with a flag show only when that flag is set, so earlier answers come back later (e.g. the owner stage recalls the trainee answer, the last chapter thanks the people you bonded with). Affinity of %d or more with someone sets `story_bond_<speaker>`. Career answers are remembered per tier (`story_<career>_<tier>_<answer>`):" % BOND_THRESHOLD, ""]
for tid, spec in TIER_SCENES.items():
    md.append("- **%s** — %s: " % (tid, spec['prompt'][2]) + "; ".join("%s (`%s`)" % (o[1][2], o[0]) for o in spec['options']))
md += ["", "Scenes are not played while `StoryDirector.Bypass` is on (tests). A scene plays once (`story_scene_<quest>`); the journal shows the lines at any time.", ""]
md += ["## Languages", "", "Every line carries Japanese, Vietnamese, English and Russian (`ja/vi/en/ru`). Quest titles, descriptions and objectives are in progression.json (vi/en/ja) with Russian in `ru_table_extra.json`.", ""]
io.open(os.path.join(DOCS, 'STORY.md'), 'w', encoding='utf-8', newline='\n').write("\n".join(md))
print('quests', len(quests), 'ru added', added, 'total quests', len(prog['quests']))
