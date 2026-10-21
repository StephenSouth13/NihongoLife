# -*- coding: utf-8 -*-
"""Write a quest's English next to its Vietnamese (for quests generated after 2026-10-21).

Use V(vi, en) wherever scen_lib expects the Vietnamese text: it returns the Vietnamese and remembers the English.
After S.write(...), call write_en(asset_path, quest_key) to produce i18n/en/<quest_key>.json in the order
i18n/apply_en.py reads (every Vietnamese value of titleEn / descriptionEn / textEn in file order), then run apply_en.
"""
import json
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
VI = re.compile('[ăâđêôơưáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵĂÂĐÊÔƠƯ]')
KEY = re.compile(r'^(\s*-?\s*)(titleEn|textEn|descriptionEn): (.*)$')
_EN = {}


def V(vi, en):
    _EN[vi] = en
    return vi


def write_en(asset_path, quest_key):
    out = []
    for line in open(asset_path, encoding='utf-8').read().split('\n'):
        m = KEY.match(line)
        if not m or not VI.search(m.group(3)):
            continue
        vi = json.loads(m.group(3))
        if vi not in _EN:
            raise SystemExit('no English for: ' + vi)
        out.append(_EN[vi])
    path = os.path.join(HERE, 'i18n', 'en', quest_key + '.json')
    json.dump(out, open(path, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('en lines', len(out), '->', path)
