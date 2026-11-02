#!/usr/bin/env python3
"""Turns the channel's public playlists into video_courses.json: courses, sections and lessons for the game's course library.

Input: a JSON of playlists (name -> {playlist, videos: [{id, title, seconds}]}) read from YouTube's public pages.
Output: Assets/NihongoLife/Resources/Video/video_courses.json (UTF-8, ensure_ascii=False).
Run from the Unity project folder:  python Tools/video/build_courses.py <playlists.json>
"""
import json
import re
import sys

COURSES = [
    {'id': 'russian', 'lang': 'ru', 'color': '#C8455A', 'lists': ['russian'],
     'titleVi': 'Tiếng Nga từ con số 0', 'titleEn': 'Russian from zero', 'titleJa': 'ゼロからのロシア語',
     'blurbVi': 'Bảng chữ cái, phát âm, giao tiếp hằng ngày và ngữ pháp. Có cả bài giáo viên thật và bài AI.',
     'blurbEn': 'The alphabet, pronunciation, everyday conversation and grammar, taught by real teachers and AI lessons.',
     'blurbJa': '文字・発音・日常会話・文法。先生の授業とAIレッスンで学びます。'},
    {'id': 'english', 'lang': 'en', 'color': '#3F7FD6', 'lists': ['english-basic', 'english'],
     'titleVi': 'Tiếng Anh A1 đến B2', 'titleEn': 'English A1 to B2', 'titleJa': '英語 A1〜B2',
     'blurbVi': 'Lộ trình theo cấp độ với bài nghe, ngữ pháp và tình huống giao tiếp thực tế.',
     'blurbEn': 'A levelled path with listening, grammar and real-life conversations.',
     'blurbJa': 'レベル別に、リスニング・文法・実践会話を学びます。'},
    {'id': 'japanese', 'lang': 'ja', 'color': '#E0A02F', 'lists': ['japanese', 'japanese-game'],
     'titleVi': 'Tiếng Nhật: kana, mẫu câu, JLPT', 'titleEn': 'Japanese: kana, patterns, JLPT', 'titleJa': '日本語：かな・文型・JLPT',
     'blurbVi': 'Bảng chữ cái, ngữ pháp trọng tâm N5–N4 và luyện nghe mẫu câu.',
     'blurbEn': 'The kana, key N5–N4 grammar and sentence listening practice.',
     'blurbJa': 'かな、N5〜N4の重要文法、文型のリスニング。'},
    {'id': 'game', 'lang': 'all', 'color': '#2FA88F', 'lists': ['game', 'gameplay'],
     'titleVi': 'Khám phá NihongoLife', 'titleEn': 'Discover NihongoLife', 'titleJa': 'NihongoLife ガイド',
     'blurbVi': 'Trailer, các khu vực và mẹo chơi từ kênh chính thức.',
     'blurbEn': 'The trailer, the areas and playing tips from the official channel.',
     'blurbJa': '公式チャンネルのトレーラーと遊び方。'},
]

SECTION = re.compile(r'^(.*?)(?:\s*[–\-:]?\s*(?:Bài|Lesson|Урок)\s*\d+|\s+\d+\s*:)', re.I)
CLEAN = re.compile(r'\s*\|\s*NihongoLife\s*$', re.I)


def section_of(title):
    head = title.split('|')[0].strip()
    found = SECTION.match(head)
    if found and found.group(1).strip():
        return found.group(1).strip(' –-:')
    return head.split(':')[0].split('–')[0].strip() if (':' in head or '–' in head) else None


def number_of(title):
    found = re.search(r'(?:Bài|Lesson|Урок)?\s*(\d+)\s*[:|]', title.split('|')[0] + ':')
    return int(found.group(1)) if found else 0


LEVELS = ['Bảng chữ cái', 'Phát âm', 'từ con số 0', 'A1', 'A2', 'B1', 'B2', 'giao tiếp']
WORDS = [('Tiếng Nga', 'Russian', 'ロシア語'), ('Tiếng Anh', 'English', '英語'), ('Tiếng Nhật', 'Japanese', '日本語'),
         ('Bảng chữ cái', 'alphabet', '文字'), ('Phát âm', 'pronunciation', '発音'), ('giao tiếp', 'conversation', '会話'),
         ('từ con số 0', 'from zero', 'ゼロから'), ('Bài bổ sung', 'Extra lessons', '追加レッスン'), (' đến ', ' to ', '〜'),
         ('kana, mẫu câu', 'kana, patterns', 'かな・文型')]


def level_of(title):
    for i, word in enumerate(LEVELS):
        if word in title:
            return i
    return 99 if title == 'Bài bổ sung' else 50


def translated(title):
    en, ja = title, title
    for vi, e, j in WORDS:
        en, ja = en.replace(vi, e), ja.replace(vi, j)
    return en, ja


def build(playlists, course):
    sections, order = {}, []
    for name in course['lists']:
        for video in playlists[name]['videos']:
            title = CLEAN.sub('', video['title']).strip()
            key = section_of(title) or course['titleVi']
            if key not in sections:
                sections[key] = []
                order.append(key)
            sections[key].append({'id': video['id'], 'title': title, 'seconds': video['seconds']})
    # Small leftovers join a shared last section; the channel lists newest first, a course reads from lesson 1.
    result, extras = [], []
    for key in order:
        lessons = sorted(sections[key], key=lambda v: number_of(v['title']))
        if len(lessons) < 2:
            extras += lessons
        else:
            result.append({'title': key, 'lessons': lessons})
    if extras:
        result.append({'title': 'Bài bổ sung', 'lessons': extras})
    result.sort(key=lambda s: level_of(s['title']))
    for s in result:
        s['titleEn'], s['titleJa'] = translated(s['title'])
    out = {k: v for k, v in course.items() if k != 'lists'}
    out['sections'] = result
    return out


def main():
    playlists = json.load(open(sys.argv[1], encoding='utf-8'))
    data = {'schema': 'nihongolife.video_courses.v1', 'channelUrl': 'https://www.youtube.com/@NihongoLifeGame',
            'courses': [build(playlists, c) for c in COURSES]}
    with open('Assets/NihongoLife/Resources/Video/video_courses.json', 'w', encoding='utf-8', newline='') as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    for c in data['courses']:
        print(c['id'], len(c['sections']), [(s['title'][:34], len(s['lessons'])) for s in c['sections']])


if __name__ == '__main__':
    main()
