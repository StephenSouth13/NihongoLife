"""Imports the music & art content (Grok's research pack, nihongo-music-art.zip) into Resources.

Usage: python Tools/music/import_group_b.py <extracted pack>/music
Writes (UTF-8, JsonUtility-friendly — no dictionaries):
  Resources/Music/songs.json            22 melodies (notes in quarter-note beats, MIDI numbers)
  Resources/Music/guitar.json           42 chord shapes + progressions
  Resources/Music/Theory/theory_<lang>.json   24 lessons × vi/en/ja/ru with quizzes
  Resources/Art/artworks.json + Resources/Art/Images/<id>.jpg (≤ 640 px, for the gallery)
Note data, chords and lessons are CC0 (original); paintings are CC0 (Met Open Access) or public domain (Commons).
"""
import json
import os
import re
import sys

from PIL import Image

SRC = sys.argv[1]
HERE = os.path.dirname(os.path.abspath(__file__))
RES = os.path.normpath(os.path.join(HERE, '..', '..', 'Assets', 'NihongoLife', 'Resources'))


def load(path):
    with open(os.path.join(SRC, path), encoding='utf-8') as f:
        return json.load(f)


SYMBOLS = re.compile(r'\s?\(?[𝄀-𝇿]\)?')  # musical symbols (𝄞 𝄢): not in the game's fonts


def clean(text):
    return SYMBOLS.sub('', text) if isinstance(text, str) else text


def save(path, data):
    full = os.path.join(RES, path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=1)


# Songs
index = load('songs-notes/index.json')
songs = []
for entry in index['songs']:
    s = load('songs-notes/' + entry['file'])
    songs.append({k: s.get(k) for k in ('id', 'title', 'title_vi', 'title_ja', 'title_ru', 'composer', 'key', 'time_signature',
                                         'tempo_bpm', 'difficulty', 'level', 'instrument', 'notes_licence')}
                 | {'notes': [{'midi': n['midi'], 'note': n['note'], 'start': n['start'], 'duration': n['duration']} for n in s['notes']]})
save('Music/songs.json', {'schema': 'nihongolife.songs.v1', 'songs': songs})

# Guitar
g = load('guitar-chords.json')
chords = []
for name, c in g['chords'].items():
    chords.append({'name': name, 'frets': c['frets'], 'fingers': c.get('fingers') or [0] * 6, 'category': c.get('category', ''),
                   'baseFret': c.get('base_fret', 1), 'midi': [m if m is not None else -1 for m in c['midi']]})
progressions = []
for p in g['progressions']:
    for key, seq in p['keys'].items():
        progressions.append({'id': p['id'] + '_' + key, 'name': p['name'], 'roman': p.get('roman', ''), 'key': key, 'chords': seq,
                             'genre': p.get('genre', '')})
save('Music/guitar.json', {'schema': 'nihongolife.guitar.v1', 'tuningMidi': g['tuning_midi'], 'chords': chords, 'progressions': progressions})

# Theory
for lang in ('vi', 'en', 'ja', 'ru'):
    t = load(f'theory/music-theory.{lang}.json')
    lessons = []
    for l in t['lessons']:
        lessons.append({'id': l['id'], 'order': l['order'], 'level': l['level'], 'minutes': l.get('minutes', 5), 'title': clean(l['title']),
                        'intro': clean(l['intro']), 'points': [clean(x) for x in l.get('points', [])], 'practiceSongs': l.get('practice_songs', []),
                        'quiz': [{'id': q['id'], 'q': clean(q['q']), 'options': [clean(o) for o in q['options']], 'answer': q['answer'], 'explain': clean(q.get('explain', ''))} for q in l.get('quiz', [])]})
    save(f'Music/Theory/theory_{lang}.json', {'schema': 'nihongolife.musictheory.v1', 'language': lang, 'lessons': lessons})

# Art
art = load('art/artworks.json')
out = []
img_dir = os.path.join(RES, 'Art', 'Images')
os.makedirs(img_dir, exist_ok=True)
# Wikimedia Commons titles arrive as raw multilingual label dumps ("Polish:  Mona Lisa Mona Lisatitle QS:P1476,…"):
# the English title and the holding museum are given here instead.
COMMONS = {
    'Mona_Lisa': ('Mona Lisa', 'Musée du Louvre, Paris'),
    'Meisje_met_de_parel': ('Girl with a Pearl Earring', 'Mauritshuis, The Hague'),
    'Tsunami_by_hokusai': ('The Great Wave off Kanagawa (神奈川沖浪裏)', ''),
    'Starry_Night': ('The Starry Night', 'Museum of Modern Art, New York'),
    'The_Scream': ('The Scream', 'National Museum of Norway, Oslo'),
    'Impression_soleil_levant': ('Impression, Sunrise', 'Musée Marmottan Monet, Paris'),
    'Las_Meninas': ('Las Meninas', 'Museo del Prado, Madrid'),
    'nascita_di_Venere': ('The Birth of Venus', 'Uffizi Gallery, Florence'),
    'Night_Watch': ('The Night Watch', 'Rijksmuseum, Amsterdam'),
    'Sunflowers_VGM': ('Sunflowers', 'Van Gogh Museum, Amsterdam'),
    'Klimt_016': ('The Kiss', 'Belvedere, Vienna'),
    'jeuner_sur_l_herbe': ('Luncheon on the Grass', "Musée d'Orsay, Paris"),
    'Wanderer_above': ('Wanderer above the Sea of Fog', 'Hamburger Kunsthalle'),
    'Creation_of_Adam': ('The Creation of Adam', 'Sistine Chapel, Vatican City'),
    'Self_Portrait': ('Self-Portrait', "Musée d'Orsay, Paris"),
}


def tidy_date(text):
    text = re.sub(r'^First publication:\s*', '', text or '').strip()
    text = re.sub(r'between (?:circa )?(\d{3,4}) and (?:circa )?(\d{3,4})', r'\1–\2', text)
    if text.startswith('circa '): text = 'c. ' + text[6:]
    return text


images = {os.path.splitext(f)[0]: f for f in os.listdir(os.path.join(SRC, 'art', 'images'))}
for a in art:
    key = os.path.splitext(os.path.basename(a.get('file') or ''))[0]
    safe = re.sub(r'[^A-Za-z0-9_-]', '_', a['id'])[:48]
    im = Image.open(os.path.join(SRC, 'art', 'images', images[key])).convert('RGB')
    im.thumbnail((640, 640))
    im.save(os.path.join(img_dir, safe + '.jpg'), quality=82)
    title, museum, credit = a['title'], a.get('museum') or '', a.get('suggested_credit', '')
    if a['id'].startswith('commons-'):
        match = next((v for k, v in COMMONS.items() if k in a['id']), None)
        if match is None: raise SystemExit('no clean title for ' + a['id'])
        title, museum = match
        credit = f"{a.get('artist', '')}, {title}. Public domain, via Wikimedia Commons."
    out.append({'id': safe, 'title': title, 'artist': a.get('artist', ''), 'date': tidy_date(a.get('date', '')), 'medium': a.get('medium') or '',
                'museum': museum, 'licence': a.get('licence', ''), 'credit': credit,
                'sourceUrl': a.get('source_url', '')})
save('Art/artworks.json', {'schema': 'nihongolife.artworks.v1', 'artworks': out})
print(f'{len(songs)} songs, {len(chords)} chords, {len(progressions)} progressions, 4 × {len(lessons)} lessons, {len(out)} paintings')
