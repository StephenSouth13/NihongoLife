"""Adds English to scenario assets written by gen_*.py.

gen_*.py (scen_lib.py) writes the Vietnamese text into the titleEn / textEn / descriptionEn keys. This script moves
each Vietnamese value to titleVi / textVi / descriptionVi and adds the English translation from en/<quest>.json
(one entry per Vietnamese value, in file order). Run it right after regenerating an asset:

    python i18n/apply_en.py ../../Assets/NihongoLife/Resources/Scenarios/scenario_house1_greeting.asset

If you change a quest's Vietnamese lines, update its en/<quest>.json in the same order (the script stops when the
counts differ). Assets that were already converted are left as they are.
"""
import json, os, re, sys

VI = re.compile('[ăâđêôơưáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵĂÂĐÊÔƠƯ]')
KEY = re.compile(r'^(\s*-?\s*)(titleEn|textEn|descriptionEn): (.*)$')
VI_KEY = {'titleEn': 'titleVi', 'textEn': 'textVi', 'descriptionEn': 'descriptionVi'}
HERE = os.path.dirname(os.path.abspath(__file__))


def quote(s):
    return '"' + s.replace('\\', '\\\\').replace('"', '\\"') + '"'


def convert(path):
    name = os.path.basename(path)[len('scenario_'):-len('.asset')]
    raw = open(path, encoding='utf-8', newline='').read()
    if re.search(r'^\s*-?\s*textVi: ', raw, re.M):
        print(name, 'already has textVi, skipped'); return
    en = json.load(open(os.path.join(HERE, 'en', name + '.json'), encoding='utf-8'))
    crlf = '\r\n' in raw
    lines = raw.replace('\r\n', '\n').split('\n')
    out, k, i = [], 0, 0
    while i < len(lines):
        m = KEY.match(lines[i])
        if not m:
            out.append(lines[i]); i += 1; continue
        value, j, block = m.group(3), i + 1, [lines[i]]
        while j < len(lines) and lines[j].startswith(' ' * (len(m.group(1)) + 2)) and not re.match(r'^\s*-?\s*\w+:', lines[j]):
            value += ' ' + lines[j].strip(); block.append(lines[j]); j += 1
        if not VI.search(value):
            out.extend(block); i = j; continue
        prefix = m.group(1)
        block[0] = prefix + VI_KEY[m.group(2)] + ': ' + block[0][len(prefix) + len(m.group(2)) + 2:]
        out.extend(block)
        out.append(' ' * len(prefix) + m.group(2) + ': ' + quote(en[k]))
        k += 1; i = j
    if k != len(en):
        sys.exit(f'{name}: {k} Vietnamese values but {len(en)} English lines in en/{name}.json; nothing written')
    text = '\n'.join(out)
    open(path, 'w', encoding='utf-8', newline='').write(text.replace('\n', '\r\n') if crlf else text)
    print(name, k, 'lines translated')


for p in sys.argv[1:]:
    convert(p)
