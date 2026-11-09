#!/usr/bin/env python3
"""Builds Resources/Progression/career_tasks.json (the ladder: tasks per level, tasks a shift counts) and
Resources/Progression/CareerTasks/<career>.json (that career's work tasks, more than 300 different ones; only the current
career's file is loaded). Every path from level 1 to the top takes more than 300 tasks.

A shift counts its own work as tasks (shiftTasks, half outside working hours) and then hands the player a few task cards
(cardsPerShift): a line in Japanese at the workplace, a question, three answers. A card answered right the first time is
one more task. Vocabulary and situations: career_tasks_data.py. Run from the Unity project folder:
    python Tools/story/career_tasks.py
"""
import itertools
import json
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from career_tasks_data import CAREERS  # noqa: E402

PROJECT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
OUT = os.path.join(PROJECT, 'Assets', 'NihongoLife', 'Resources', 'Progression', 'career_tasks.json')
TASK_DIR = os.path.join(PROJECT, 'Assets', 'NihongoLife', 'Resources', 'Progression', 'CareerTasks')
CAREERS_JSON = os.path.join(PROJECT, 'Assets', 'NihongoLife', 'Resources', 'Progression', 'careers.json')

SHIFT_TASKS, OFF_HOURS_TASKS, CARDS_PER_SHIFT = 4, 2, 2
TASKS_PER_LEVEL = [32, 48, 64, 82, 104, 120]  # tasks to fill the bar at level 1, 2, ... (330 from level 1 to level 6, about 55 shifts)

DIGITS = ['', 'いち', 'に', 'さん', 'よん', 'ご', 'ろく', 'なな', 'はち', 'きゅう']
HUNDREDS = ['', 'ひゃく', 'にひゃく', 'さんびゃく', 'よんひゃく', 'ごひゃく', 'ろっぴゃく', 'ななひゃく', 'はっぴゃく', 'きゅうひゃく']
THOUSANDS = ['', 'せん', 'にせん', 'さんぜん', 'よんせん', 'ごせん', 'ろくせん', 'ななせん', 'はっせん', 'きゅうせん']
TIMES = ['いちじ', 'にじ', 'さんじ', 'よじ', 'ごじ', 'ろくじ', 'しちじ', 'はちじ', 'くじ', 'じゅうじ', 'じゅういちじ', 'じゅうにじ']
COUNTS = [('ひとつ', 1), ('ふたつ', 2), ('みっつ', 3), ('よっつ', 4), ('いつつ', 5)]
PRICES = [100, 120, 150, 180, 200, 250, 280, 300, 350, 400, 480, 500, 600, 680, 800, 980, 1000, 1200, 1500, 3000]
VERBS = [('もってきて', 'Mang {0} đến', 'Bring the {0}', '持ってくる'), ('かたづけて', 'Cất {0} đi', 'Put the {0} away', '片付ける'),
         ('かぞえて', 'Đếm {0}', 'Count the {0}', '数える'), ('かくにんして', 'Kiểm tra {0}', 'Check the {0}', '確認する'),
         ('ならべて', 'Xếp {0} ngay ngắn', 'Line up the {0}', '並べる'), ('さがして', 'Tìm {0}', 'Look for the {0}', '探す')]
PLACES = [('いりぐちの ちかく', 'gần lối vào', 'near the entrance'), ('おくの たな', 'trên kệ phía trong', 'on the back shelf'),
          ('つくえの うえ', 'trên bàn', 'on the desk'), ('つくえの した', 'dưới bàn', 'under the desk'), ('まどの そば', 'cạnh cửa sổ', 'by the window')]
KINDS = {
    'find': ('Hỏi đồ', 'A request', 'おたずね'), 'count': ('Lấy đủ số lượng', 'Getting the amount', '数量'),
    'pair': ('Nhận yêu cầu', 'Taking an order', '注文'), 'verb': ('Việc sếp giao', 'A task from the boss', '指示'),
    'place': ('Tìm chỗ để đồ', 'Finding things', '場所'), 'price': ('Tính tiền', 'At the till', '会計'),
    'time': ('Lịch làm việc', 'The schedule', 'スケジュール'), 'situation': ('Giao tiếp', 'Speaking', '会話'),
}
TIER = {'find': 1, 'count': 1, 'verb': 2, 'place': 2, 'price': 3, 'pair': 3, 'time': 4}


def yen(n):
    out = THOUSANDS[n // 1000] + HUNDREDS[n // 100 % 10]
    tens, ones = n // 10 % 10, n % 10
    out += ('' if tens == 0 else ('じゅう' if tens == 1 else DIGITS[tens] + 'じゅう')) + DIGITS[ones]
    return out


assert yen(350) == 'さんびゃくごじゅう' and yen(3000) == 'さんぜん' and yen(680) == 'ろっぴゃくはちじゅう' and yen(1200) == 'せんにひゃく'
assert yen(980) == 'きゅうひゃくはちじゅう' and yen(800) == 'はっぴゃく' and yen(120) == 'ひゃくにじゅう'


def word(item):
    return item[0]


def choice(vi, en, ja, correct, hint_vi='', hint_en=''):
    return {'vi': vi, 'en': en, 'ja': ja, 'correct': correct, 'hintVi': hint_vi, 'hintEn': hint_en}


def task(career, kind, n, speaker, ja, meaning_vi, meaning_en, ask, choices, rng, level=None):
    rng.shuffle(choices)
    vi, en, jk = KINDS[kind]
    return {'id': f'{career}_{kind}_{n:03d}', 'kind': kind, 'kindVi': vi, 'kindEn': en, 'kindJa': jk, 'minLevel': level or TIER[kind],
            'speaker': speaker, 'ja': ja, 'meaningVi': meaning_vi, 'meaningEn': meaning_en,
            'askVi': ask[0], 'askEn': ask[1], 'askJa': ask[2], 'choices': choices}


def build(career, data):
    rng = random.Random(career)
    items = data['items']
    out = []
    others = lambda it, k=2: rng.sample([x for x in items if x is not it], k)  # noqa: E731
    wrong = ('Nghe lại tên món đồ trong câu.', 'Listen again for the name of the thing.')
    n = 0
    for it in items:
        for line, mvi, men in [(f'すみません、{word(it)}は どこですか。', f'Xin lỗi, {it[2]} ở đâu?', f'Excuse me, where is the {it[3]}?'),
                               (f'{word(it)}は ありますか。', f'Có {it[2]} không?', f'Do you have {it[3]}?')]:
            n += 1
            ch = [choice(it[2], it[3], word(it), True)] + [choice(o[2], o[3], word(o), False, *wrong) for o in others(it)]
            out.append(task(career, 'find', n, 'speaker', line, mvi, men, ('Người này đang tìm gì?', 'What are they looking for?', '何を探していますか。'), ch, rng))
    n = 0
    for it in items:
        for kana, count in COUNTS:
            n += 1
            nums = [count] + rng.sample([c for _, c in COUNTS if c != count], 2)
            ch = [choice(str(c), str(c), f'{c}つ', c == count, 'ひとつ 1 · ふたつ 2 · みっつ 3 · よっつ 4 · いつつ 5', 'ひとつ 1 · ふたつ 2 · みっつ 3 · よっつ 4 · いつつ 5') for c in nums]
            out.append(task(career, 'count', n, 'speaker', f'{word(it)}を {kana} おねがいします。', f'Cho tôi {count} {it[2]}.', f'{count} {it[3]}, please.',
                            (f'Cần bao nhiêu {it[2]}?', f'How many {it[3]}?', 'いくつですか。'), ch, rng))
    n = 0
    for a, b in itertools.combinations(items, 2):
        n += 1
        c, d = rng.sample([x for x in items if x is not a and x is not b], 2)
        ch = [choice(f'{a[2]} và {b[2]}', f'{a[3]} and {b[3]}', f'{word(a)}と{word(b)}', True),
              choice(f'{a[2]} và {c[2]}', f'{a[3]} and {c[3]}', f'{word(a)}と{word(c)}', False, *wrong),
              choice(f'{d[2]} và {b[2]}', f'{d[3]} and {b[3]}', f'{word(d)}と{word(b)}', False, *wrong)]
        out.append(task(career, 'pair', n, 'speaker', f'{word(a)}と {word(b)}を おねがいします。', f'Cho tôi {a[2]} và {b[2]}.', f'{a[3]} and {b[3]}, please.',
                        ('Người này cần những gì?', 'What do they need?', '何が必要ですか。'), ch, rng))
    n = 0
    for it in items:
        for v in VERBS:
            n += 1
            ov = rng.choice([x for x in VERBS if x is not v])
            oi = others(it, 1)[0]
            ch = [choice(v[1].format(it[2]), v[2].format(it[3]), f'{word(it)}を{v[3]}', True),
                  choice(ov[1].format(it[2]), ov[2].format(it[3]), f'{word(it)}を{ov[3]}', False, 'Nghe kỹ động từ ở cuối câu.', 'Listen for the verb at the end.'),
                  choice(v[1].format(oi[2]), v[2].format(oi[3]), f'{word(oi)}を{v[3]}', False, *wrong)]
            out.append(task(career, 'verb', n, 'boss', f'{word(it)}を {v[0]}ください。', v[1].format(it[2]) + ' giúp nhé.', v[2].format(it[3]) + ', please.',
                            ('Sếp bảo bạn làm gì?', 'What does the boss ask you to do?', '何をしますか。'), ch, rng))
    n = 0
    for it in items:
        for p in PLACES:
            n += 1
            op = rng.sample([x for x in PLACES if x is not p], 2)
            ch = [choice(p[1], p[2], p[0], True)] + [choice(o[1], o[2], o[0], False, 'うえ trên · した dưới · ちかく gần · そば cạnh · おく phía trong', 'うえ on · した under · ちかく near · そば beside · おく at the back') for o in op]
            out.append(task(career, 'place', n, 'boss', f'{word(it)}は {p[0]}に あります。', f'{it[2]} ở {p[1]}.', f'The {it[3]} is {p[2]}.',
                            (f'{it[2][0].upper() + it[2][1:]} ở đâu?', f'Where is the {it[3]}?', 'どこにありますか。'), ch, rng))
    n = 0
    for price in PRICES:
        n += 1
        near = sorted({x for x in PRICES if x != price}, key=lambda x: abs(x - price))[:4]
        nums = [price] + rng.sample(near, 2)
        ch = [choice(f'{x:,} yên'.replace(',', '.'), f'{x:,} yen', f'{x}円', x == price, 'ひゃく 100 · せん 1000 · じゅう 10', 'ひゃく 100 · せん 1000 · じゅう 10') for x in nums]
        out.append(task(career, 'price', n, 'speaker', f'ぜんぶで {yen(price)}えんです。', f'Tổng cộng {price:,} yên.'.replace(',', '.'), f'That is {price:,} yen in total.',
                        ('Bao nhiêu tiền?', 'How much is it?', 'いくらですか。'), ch, rng))
    n = 0
    for h, kana in enumerate(TIMES, 1):
        n += 1
        nums = [h] + rng.sample([x for x in range(1, 13) if x != h], 2)
        ch = [choice(f'{x} giờ', f'{x} o\'clock', TIMES[x - 1], x == h, 'よじ là 4 giờ, しちじ là 7 giờ, くじ là 9 giờ.', 'よじ is 4, しちじ is 7, くじ is 9.') for x in nums]
        out.append(task(career, 'time', n, 'boss', f'あしたは {kana}から はじめます。', f'Mai bắt đầu từ {h} giờ.', f'Tomorrow we start at {h}.',
                        ('Mai mấy giờ bắt đầu?', 'What time do we start tomorrow?', '何時からですか。'), ch, rng))
    for n, s in enumerate(data['situations'], 1):
        svi, sen, good, bad1, bad2, hvi, hen = s
        ch = [choice(good, good, good, True)] + [choice(b, b, b, False, hvi, hen) for b in (bad1, bad2)]
        out.append(task(career, 'situation', n, 'speaker', s[2], svi, sen, (svi + ' Bạn nói gì?', sen + ' What do you say?', 'なんと言いますか。'), ch, rng, level=1 + (n - 1) % 5))
        out[-1]['ja'] = ''  # the situation is the question; the Japanese is in the answers
    return out


def main():
    careers = json.load(open(CAREERS_JSON, encoding='utf-8'))['careers']
    result = {'schema': 'nihongolife.career_tasks.v1', 'shiftTasks': SHIFT_TASKS, 'offHoursTasks': OFF_HOURS_TASKS,
              'cardsPerShift': CARDS_PER_SHIFT, 'tasksPerLevel': TASKS_PER_LEVEL, 'careers': []}
    os.makedirs(TASK_DIR, exist_ok=True)
    for c in careers:
        cid = c['id']
        assert cid in CAREERS, f'{cid}: add its vocabulary to career_tasks_data.py'
        tasks = build(cid, CAREERS[cid])
        assert len({t['id'] for t in tasks}) == len(tasks)
        assert len(tasks) >= 300, (cid, len(tasks))
        for t in tasks:
            assert sum(ch['correct'] for ch in t['choices']) == 1, t['id']
            assert len({ch['vi'] for ch in t['choices']}) == 3, t['id']
        # the ladder: every path from level 1 to the top takes more than 300 tasks
        for branch in c['branches']:
            rungs = [l['shiftsToPromote'] for l in c['levels'] + branch['levels']]
            promotions = min(len(rungs), sum(1 for r in rungs if r > 0), len(c['levels']) + max(len(b['levels']) for b in c['branches']) - 1)
            assert sum(TASKS_PER_LEVEL[:promotions]) >= 300, (cid, branch['id'], promotions)
        result['careers'].append({'id': cid, 'count': len(tasks)})
        with open(os.path.join(TASK_DIR, cid + '.json'), 'w', encoding='utf-8', newline=chr(10)) as f:
            json.dump({'id': cid, 'speakerVi': CAREERS[cid]['speaker'][0], 'speakerEn': CAREERS[cid]['speaker'][1],
                       'speakerJa': CAREERS[cid]['speaker'][2], 'tasks': tasks}, f, ensure_ascii=False, separators=(',', ':'))
        print(cid, len(tasks), 'tasks')
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(result, f, ensure_ascii=False, indent=1)
    print('->', OUT, os.path.getsize(OUT) // 1024, 'KB')


if __name__ == '__main__':
    main()
