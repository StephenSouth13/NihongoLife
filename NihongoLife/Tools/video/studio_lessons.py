#!/usr/bin/env python3
"""Builds the NihongoLife Studio lesson videos: original lessons that play inside the game on every platform.

For each lesson of studio_content.py: slides drawn with Pillow (960x540), Japanese read by a Japanese neural voice and
explained by a Vietnamese one (edge-tts), joined with ffmpeg into one small H.264 / AAC mp4.
Output:
  Assets/StreamingAssets/Videos/Studio/<id>.mp4 and <id>.jpg (thumbnail)
  Assets/NihongoLife/Resources/Video/studio_courses.json (the course for VideoCourseCatalog, UTF-8)
Work files go to D:/VTC_Academy/NihongoLife/_work/studio. Re-run safe: a lesson is rebuilt only when its content changed
(or with --force). Usage, from the Unity project folder: python Tools/video/studio_lessons.py [--force] [lesson_id ...]
"""
import asyncio
import hashlib
import json
import os
import re
import subprocess
import sys

import edge_tts
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from studio_content import KANA_ROWS, PHRASES  # noqa: E402

PROJECT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
OUT = os.path.join(PROJECT, 'Assets', 'StreamingAssets', 'Videos', 'Studio')
JSON_OUT = os.path.join(PROJECT, 'Assets', 'NihongoLife', 'Resources', 'Video', 'studio_courses.json')
WORK = 'D:/VTC_Academy/NihongoLife/_work/studio'
W, H = 960, 540
JA_VOICE, VI_VOICE = 'ja-JP-NanamiNeural', 'vi-VN-HoaiMyNeural'
FONT_JA = 'C:/Windows/Fonts/YuGothB.ttc'
FONT_UI = 'C:/Windows/Fonts/segoeuib.ttf'
FONT_UI_R = 'C:/Windows/Fonts/segoeui.ttf'
NAVY, PANEL, GOLD, INK, MUTED, TEAL = (18, 24, 38), (30, 38, 58), (242, 183, 56), (244, 246, 250), (160, 172, 192), (47, 168, 143)


def font(path, size):
    return ImageFont.truetype(path, size)


def fit(draw, text, path, size, width):
    while size > 14 and draw.textlength(text, font=font(path, size)) > width:
        size -= 2
    return font(path, size)


CJK = re.compile(r'([　-ヿ一-鿿＀-￯]+)')


def runs(text, path, size):
    """Text split into runs drawn with the Latin font or, for kana and kanji, the Japanese one (Segoe has no kana)."""
    ja = font(FONT_JA, size)
    latin = font(path, size) if path != FONT_JA else ja
    return [(part, ja if CJK.fullmatch(part) else latin) for part in CJK.split(text) if part]


def width(draw, text, path, size):
    return sum(draw.textlength(t, font=f) for t, f in runs(text, path, size))


def write(draw, xy, text, path, size, fill, anchor='lm', max_width=None):
    if max_width:
        while size > 14 and width(draw, text, path, size) > max_width:
            size -= 2
    x, y = xy
    total = width(draw, text, path, size)
    x -= total / 2 if anchor[0] == 'm' else total if anchor[0] == 'r' else 0
    for t, f in runs(text, path, size):
        draw.text((x, y), t, font=f, fill=fill, anchor='l' + anchor[1])
        x += draw.textlength(t, font=f)


def frame(lesson_no, total, title_vi):
    im = Image.new('RGB', (W, H), NAVY)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((24, 24, W - 24, H - 24), 28, fill=PANEL)
    d.rectangle((24, H - 70, W - 24, H - 24), fill=(24, 31, 48))
    d.text((48, H - 47), 'NihongoLife Studio', font=font(FONT_UI, 20), fill=GOLD, anchor='lm')
    write(d, (W - 48, H - 47), f'{title_vi} · {lesson_no}/{total}', FONT_UI_R, 18, MUTED, 'rm')
    return im, d


def title_slide(lesson, no, total):
    im, d = frame(no, total, lesson['vi'])
    d.rounded_rectangle((48, 48, W - 48, H - 94), 22, fill=(36, 46, 70))
    d.text((W // 2, 150), lesson['ja'], font=fit(d, lesson['ja'], FONT_JA, 64, W - 160), fill=GOLD, anchor='mm')
    write(d, (W // 2, 240), lesson['vi'], FONT_UI, 44, INK, 'mm', W - 160)
    write(d, (W // 2, 300), lesson['en'], FONT_UI_R, 26, MUTED, 'mm', W - 160)
    d.rounded_rectangle((W // 2 - 170, 352, W // 2 + 170, 404), 26, fill=GOLD)
    d.text((W // 2, 378), f'Bài {no} · NihongoLife Studio', font=font(FONT_UI, 22), fill=NAVY, anchor='mm')
    return im


def kana_slide(lesson, no, total, item, k, n):
    letter, romaji, word, kanji, vi, en = item
    im, d = frame(no, total, lesson['vi'])
    d.rounded_rectangle((52, 52, 392, 446), 26, fill=(250, 248, 242))
    d.text((222, 222), letter, font=font(FONT_JA, 220), fill=NAVY, anchor='mm')
    d.text((222, 396), romaji, font=font(FONT_UI, 40), fill=(200, 90, 60), anchor='mm')
    x = 430
    d.text((x, 92), f'{k}/{n}', font=font(FONT_UI_R, 22), fill=MUTED, anchor='lm')
    d.text((x, 170), word, font=fit(d, word, FONT_JA, 72, W - x - 60), fill=INK, anchor='lm')
    if kanji and kanji != word:
        d.text((x, 248), kanji, font=fit(d, kanji, FONT_JA, 44, W - x - 60), fill=GOLD, anchor='lm')
    write(d, (x, 322), vi, FONT_UI, 36, INK, 'lm', W - x - 60)
    write(d, (x, 372), en, FONT_UI_R, 24, MUTED, 'lm', W - x - 60)
    for i in range(n):
        d.ellipse((x + i * 26, 420, x + i * 26 + 14, 434), fill=GOLD if i < k else (70, 80, 104))
    return im


def phrase_slide(lesson, no, total, item, k, n):
    ja, romaji, vi, en, note = item
    im, d = frame(no, total, lesson['vi'])
    d.text((60, 86), f'{k}/{n}', font=font(FONT_UI_R, 22), fill=MUTED, anchor='lm')
    d.rounded_rectangle((52, 116, W - 52, 268), 22, fill=(250, 248, 242))
    d.text((W // 2, 176), ja, font=fit(d, ja, FONT_JA, 64, W - 150), fill=NAVY, anchor='mm')
    d.text((W // 2, 238), romaji, font=fit(d, romaji, FONT_UI_R, 26, W - 150), fill=(200, 90, 60), anchor='mm')
    write(d, (W // 2, 318), vi, FONT_UI, 40, INK, 'mm', W - 120)
    write(d, (W // 2, 362), en, FONT_UI_R, 24, MUTED, 'mm', W - 120)
    if note:
        d.rounded_rectangle((120, 392, W - 120, 438), 20, fill=(36, 46, 70))
        write(d, (W // 2, 415), note, FONT_UI_R, 22, TEAL, 'mm', W - 280)
    return im


def end_slide(lesson, no, total, items, kind):
    im, d = frame(no, total, lesson['vi'])
    write(d, (W // 2, 92), 'Ôn lại · おさらい', FONT_UI, 34, GOLD, 'mm')
    texts = [i[0] if kind == 'kana' else i[0] for i in items]
    if kind == 'kana':
        cell = min(140, (W - 120) // len(texts))
        x0 = (W - cell * len(texts)) // 2
        for i, t in enumerate(texts):
            d.rounded_rectangle((x0 + i * cell + 6, 150, x0 + (i + 1) * cell - 6, 150 + cell - 12), 18, fill=(250, 248, 242))
            d.text((x0 + i * cell + cell // 2, 150 + (cell - 12) // 2), t, font=font(FONT_JA, int(cell * 0.6)), fill=NAVY, anchor='mm')
            d.text((x0 + i * cell + cell // 2, 150 + cell + 16), items[i][1], font=font(FONT_UI, 22), fill=MUTED, anchor='mm')
    else:
        for i, t in enumerate(texts):
            d.text((W // 2, 150 + i * 46), t, font=fit(d, t, FONT_JA, 32, W - 140), fill=INK, anchor='mm')
    d.text((W // 2, 428), 'Luyện tiếp trong lớp học Hibari và ngoài thị trấn!', font=font(FONT_UI_R, 24), fill=TEAL, anchor='mm')
    return im


async def say(text, voice, path, rate='-8%'):
    if os.path.exists(path) and os.path.getsize(path) > 0:
        return
    if voice == VI_VOICE:  # the Vietnamese voice cannot read kana: the Japanese voice says those
        text = re.sub(r'[぀-ヿ一-鿿]+', '', text).replace(' .', '.').strip()
    # The service drops a request now and then, and refuses a few short sentences outright ("Nghĩa là biển."):
    # later attempts say the same thing in slightly other words.
    base = text.rstrip('.')
    tries = [(voice, text)] * 2
    if voice == VI_VOICE:
        alt = [text.replace(' là ', ' là, '), base + ' nhé.', base.replace(' là ', ' là, ') + ' nhé.', 'Nghĩa tiếng Việt: ' + base.split(' là ')[-1] + '.']
        tries += [(VI_VOICE, t) for t in alt] + [('vi-VN-NamMinhNeural', t) for t in [text] + alt]
    for attempt, (v, t) in enumerate(tries):
        try:
            await edge_tts.Communicate(t, v, rate=rate).save(path + '.part')
            os.replace(path + '.part', path)
            return
        except Exception:
            await asyncio.sleep(1 + attempt)
    # Still refused: a short silence keeps the slide (the words are on screen) instead of stopping the build.
    print('TTS refused, silent:', voice, repr(text))
    run(['ffmpeg', '-y', '-f', 'lavfi', '-i', 'anullsrc=r=24000:cl=mono', '-t', '0.8', '-c:a', 'libmp3lame', '-b:a', '48k', path])


def run(args):
    subprocess.run(args, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def duration(path):
    out = subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', path], capture_output=True, text=True).stdout
    return float(out.strip() or 0)


def lessons():
    all_ = []
    for (lid, vi, en, ja, items) in KANA_ROWS:
        all_.append({'id': lid, 'vi': vi, 'en': en, 'ja': ja, 'kind': 'kana', 'items': items})
    for (lid, vi, en, ja, items) in PHRASES:
        all_.append({'id': lid, 'vi': vi, 'en': en, 'ja': ja, 'kind': 'phrase', 'items': items})
    return all_


def script(lesson):
    """Slides as (kind, item index, [(voice, text), ...])."""
    out = [('title', -1, [(VI_VOICE, f"Bài học: {lesson['vi']}."), (JA_VOICE, lesson['ja'])])]
    for k, item in enumerate(lesson['items']):
        if lesson['kind'] == 'kana':
            letter, romaji, word, kanji, vi, en = item
            lines = [(JA_VOICE, letter), (JA_VOICE, f'{word}。{word}。'), (VI_VOICE, f'Từ này có nghĩa là {vi}.')]
            if letter == 'ー':
                lines = [(VI_VOICE, 'Dấu gạch dài kéo dài nguyên âm phía trước.'), (JA_VOICE, f'{word}。{word}。'), (VI_VOICE, f'nghĩa là {vi}.')]
            elif letter == 'を':
                lines = [(JA_VOICE, 'を'), (VI_VOICE, 'Chữ này đọc là "ô", dùng để chỉ tân ngữ.'), (JA_VOICE, f'{kanji}。'), (VI_VOICE, 'nghĩa là đọc sách.')]
        else:
            ja, romaji, vi, en, note = item
            lines = [(JA_VOICE, ja), (VI_VOICE, f'{vi}.'), (JA_VOICE, ja)]
        out.append(('item', k, lines))
    review = '、'.join(i[0] for i in lesson['items']) if lesson['kind'] == 'kana' else '。'.join(i[0] for i in lesson['items'][:3])
    out.append(('end', -1, [(VI_VOICE, 'Cùng ôn lại nhé.'), (JA_VOICE, review + '。')]))
    return out


async def build(lesson, no, total, force):
    lid = lesson['id']
    key = hashlib.sha1(json.dumps([lesson, no, total, 'v2'], ensure_ascii=False).encode('utf-8')).hexdigest()[:12]
    mp4, jpg = os.path.join(OUT, lid + '.mp4'), os.path.join(OUT, lid + '.jpg')
    stamp = os.path.join(WORK, lid, 'key.txt')
    if not force and os.path.exists(mp4) and os.path.exists(stamp) and open(stamp).read() == key:
        return duration(mp4)
    work = os.path.join(WORK, lid)
    os.makedirs(work, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    segments = []
    for s, (kind, idx, lines) in enumerate(script(lesson)):
        if kind == 'title':
            img = title_slide(lesson, no, total)
        elif kind == 'end':
            img = end_slide(lesson, no, total, lesson['items'], lesson['kind'])
        elif lesson['kind'] == 'kana':
            img = kana_slide(lesson, no, total, lesson['items'][idx], idx + 1, len(lesson['items']))
        else:
            img = phrase_slide(lesson, no, total, lesson['items'][idx], idx + 1, len(lesson['items']))
        png = os.path.join(work, f's{s:02d}.png')
        img.save(png)
        if s == 0:
            img.resize((480, 270), Image.LANCZOS).save(jpg, quality=88)
        clips = []
        for c, (voice, text) in enumerate(lines):
            path = os.path.join(work, f's{s:02d}_{c}_{hashlib.sha1((voice + text).encode("utf-8")).hexdigest()[:8]}.mp3')
            await say(text, voice, path, '-18%' if voice == JA_VOICE else '-5%')
            clips.append(path)
        # the slide's sound: its clips with short pauses, then a breath before the next slide
        inputs, chain = [], []
        for i, clip in enumerate(clips):
            inputs += ['-i', clip]
            chain.append(f'[{i}:a]aresample=44100,aformat=channel_layouts=mono,apad=pad_dur=0.45[a{i}]')
        concat = ''.join(f'[a{i}]' for i in range(len(clips)))
        wav = os.path.join(work, f's{s:02d}.wav')
        run(['ffmpeg', '-y', *inputs, '-filter_complex', ';'.join(chain) + f';{concat}concat=n={len(clips)}:v=0:a=1,apad=pad_dur=0.5[out]', '-map', '[out]', wav])
        seg = os.path.join(work, f's{s:02d}.mp4')
        d = duration(wav)
        run(['ffmpeg', '-y', '-loop', '1', '-framerate', '24', '-i', png, '-i', wav, '-t', f'{d:.2f}',
             '-vf', f'fade=t=in:st=0:d=0.25,fade=t=out:st={max(0, d - 0.25):.2f}:d=0.25,format=yuv420p',
             '-c:v', 'libx264', '-preset', 'medium', '-tune', 'stillimage', '-crf', '27', '-r', '24',
             '-c:a', 'aac', '-b:a', '64k', '-ar', '44100', '-ac', '1', seg])
        segments.append(seg)
    lst = os.path.join(work, 'list.txt')
    with open(lst, 'w', encoding='utf-8') as f:
        for seg in segments:
            f.write(f"file '{seg}'\n")
    os.makedirs(OUT, exist_ok=True)
    run(['ffmpeg', '-y', '-f', 'concat', '-safe', '0', '-i', lst, '-c', 'copy', '-movflags', '+faststart', mp4])
    with open(stamp, 'w') as f:
        f.write(key)
    return duration(mp4)


async def main():
    force = '--force' in sys.argv
    only = [a for a in sys.argv[1:] if not a.startswith('--')]
    all_ = lessons()
    rows = []
    for no, lesson in enumerate(all_, 1):
        if only and lesson['id'] not in only:
            seconds = duration(os.path.join(OUT, lesson['id'] + '.mp4')) if os.path.exists(os.path.join(OUT, lesson['id'] + '.mp4')) else 0
        else:
            seconds = await build(lesson, no, len(all_), force)
            print(f"{lesson['id']}: {seconds:.0f} s")
        rows.append((lesson, round(seconds)))
    kana = [r for r in rows if r[0]['kind'] == 'kana']
    phrases = [r for r in rows if r[0]['kind'] == 'phrase']

    def lesson_json(lesson, seconds):
        return {'id': 'studio_' + lesson['id'], 'title': f"{lesson['vi']} · {lesson['ja']}", 'seconds': seconds,
                'file': f"Videos/Studio/{lesson['id']}.mp4", 'thumb': f"Videos/Studio/{lesson['id']}.jpg"}

    course = {
        'id': 'studio', 'lang': 'ja', 'color': '#D9534F',
        'titleVi': 'NihongoLife Studio: tiếng Nhật trong game', 'titleEn': 'NihongoLife Studio: Japanese in the game', 'titleJa': 'NihongoLife スタジオ：日本語',
        'blurbVi': 'Bài giảng riêng của game, phát ngay trong game: chữ kana, chào hỏi, số đếm, konbini, nhà ga, phòng khám.',
        'blurbEn': "The game's own lessons, played right in the game: kana, greetings, numbers, the konbini, the station, the clinic.",
        'blurbJa': 'ゲーム内で再生できるオリジナル授業：かな・あいさつ・数字・コンビニ・駅・クリニック。',
        'sections': [
            {'title': 'Bảng chữ cái kana', 'titleEn': 'The kana', 'titleJa': 'かな', 'lessons': [lesson_json(*r) for r in kana]},
            {'title': 'Tiếng Nhật trong thị trấn', 'titleEn': 'Japanese around town', 'titleJa': '町の日本語', 'lessons': [lesson_json(*r) for r in phrases]},
        ],
    }
    with open(JSON_OUT, 'w', encoding='utf-8', newline='\n') as f:
        json.dump({'courses': [course]}, f, ensure_ascii=False, indent=2)
    total = sum(s for _, s in rows)
    size = sum(os.path.getsize(os.path.join(OUT, l['id'] + '.mp4')) for l, _ in rows if os.path.exists(os.path.join(OUT, l['id'] + '.mp4')))
    print(f'{len(rows)} lessons, {total // 60} min {total % 60} s, {size / 1048576:.1f} MB -> {JSON_OUT}')


if __name__ == '__main__':
    asyncio.run(main())
