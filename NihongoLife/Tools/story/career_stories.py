"""Career milestone stories: a short scene at the workplace before each promotion interview.

Each promotion (level 2, 3, 4 shared; 5 and 6 per branch) has one story: a few lines, a question, three answers in
Japanese. The best answer (polite, thoughtful) gives trí tuệ and reputation; an okay one a little reputation; a poor
one costs performance, so the bar is no longer full and the interview waits for the next shift.

Writes NihongoLife/Assets/NihongoLife/Resources/Progression/career_stories.json (read by Progression/CareerStories.cs).
Content lives in career_stories_a.py (retail, food, farm, hospitality), _b.py (medical, chef, musician),
_c.py (producer, painter, fashion). Run: python Tools/story/career_stories.py
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'NihongoLife', 'Resources', 'Progression', 'career_stories.json')
CAREERS = os.path.join(ROOT, 'Assets', 'NihongoLife', 'Resources', 'Progression', 'careers.json')

EFFECTS = {
    'best': {'knowledge': 5, 'reputation': 4, 'performance': 0, 'mood': 'Proud'},
    'ok': {'knowledge': 0, 'reputation': 1, 'performance': 0, 'mood': ''},
    'poor': {'knowledge': 0, 'reputation': -2, 'performance': -20, 'mood': 'Embarrassed'},
}

STORIES = []


def S(career, level, branch, title, cast, lines, ask, choices):
    """One story. title (vi, en, ja); cast {id: (vi, en, ja)}; lines [(speaker, ja, vi, en)]; ask (speaker, ja, vi, en);
    choices [(kind, ja, vi, en, reply)] with kind best / ok / poor and reply (speaker, ja, vi, en).
    Speakers: 'boss' (the career's boss), 'narrator', or an id of the cast."""
    STORIES.append(dict(career=career, level=level, branch=branch or '', title=title, cast=cast, lines=lines, ask=ask, choices=choices))


def line(t):
    speaker, ja, vi, en = t
    return {'speaker': speaker, 'ja': ja, 'vi': vi, 'en': en}


def build():
    sys.path.insert(0, HERE)
    sys.modules.setdefault('career_stories', sys.modules[__name__])  # run as a script: the content files add to this STORIES
    import career_stories_a  # noqa: F401  (each file calls S)
    import career_stories_b  # noqa: F401
    import career_stories_c  # noqa: F401
    careers = {c['id']: c for c in json.load(open(CAREERS, encoding='utf-8-sig'))['careers']}
    out, errors = [], []
    for s in STORIES:
        sid = f"{s['career']}_{s['level']}" + (f"_{s['branch']}" if s['branch'] else '')
        kinds = sorted(c[0] for c in s['choices'])
        if kinds != ['best', 'ok', 'poor']:
            errors.append(f'{sid}: answers must be one best, one ok, one poor ({kinds})')
        speakers = {'boss', 'narrator', *s['cast']}
        for t in [*s['lines'], s['ask'], *[c[4] for c in s['choices']]]:
            if t[0] not in speakers:
                errors.append(f'{sid}: unknown speaker {t[0]}')
            if not all(x.strip() for x in t[1:]):
                errors.append(f'{sid}: empty text in {t}')
        out.append({
            'id': sid, 'career': s['career'], 'toLevel': s['level'], 'branch': s['branch'],
            'titleVi': s['title'][0], 'titleEn': s['title'][1], 'titleJa': s['title'][2],
            'cast': [{'id': k, 'vi': v[0], 'en': v[1], 'ja': v[2]} for k, v in s['cast'].items()],
            'lines': [line(t) for t in s['lines']], 'ask': line(s['ask']),
            'choices': [{'id': c[0], 'kind': c[0], 'ja': c[1], 'vi': c[2], 'en': c[3], 'reply': line(c[4]), **EFFECTS[c[0]]} for c in s['choices']],
        })
    # Every promotion of every career has its story: levels 2..branchAtLevel shared, the branch levels per branch.
    have = {(o['career'], o['toLevel'], o['branch']) for o in out}
    for cid, c in careers.items():
        shared = len(c['levels'])
        for lvl in range(2, shared + 2):
            if (cid, lvl, '') not in have:
                errors.append(f'{cid}: no story for level {lvl}')
        for b in c['branches']:
            for k in range(2, len(b['levels']) + 1):
                if (cid, shared + k, b['id']) not in have:
                    errors.append(f"{cid}: no story for level {shared + k} ({b['id']})")
    if len({o['id'] for o in out}) != len(out):
        errors.append('duplicate story ids')
    if errors:
        raise SystemExit('\n'.join(errors))
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        json.dump({'schema': 'nihongolife.career_stories.v1', 'stories': out}, f, ensure_ascii=False, indent=1)
    print(len(out), 'stories ->', OUT)


if __name__ == '__main__':
    build()
