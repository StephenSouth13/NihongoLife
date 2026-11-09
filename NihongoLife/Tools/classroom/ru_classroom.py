"""Russian lines of the classroom hub (StudyHubPopup, the classroom desk), merged into
Resources/Content/Localization/ru_table_extra.json. Run from the Unity project folder: python Tools/classroom/ru_classroom.py"""
import json
import os

RU = {
    'Classroom': 'Класс',
    '40 lessons, the alphabet, grammar, certificates and a placement test.': '40 уроков, алфавит, грамматика, сертификаты и тест на уровень.',
    '{0} video lessons with a curriculum, in the game or on YouTube.': '{0} видеоуроков по программе — в игре или на YouTube.',
    'JLPT mock tests': 'Пробные тесты JLPT',
    'IELTS mock tests': 'Пробные тесты IELTS',
    'Tests in the real format, marked and kept in your history.': 'Тесты в настоящем формате, с оценкой и историей попыток.',
    'Vietnamese': 'Вьетнамский',
    'Study: lessons, videos, mock tests': 'Учиться: уроки, видео, пробные тесты',
    'VIDEO ({0})': 'ВИДЕО ({0})',
    '12 tenses': '12 времён',
    "Ms. Morita's class · desk at the front — press F for lessons, videos and mock tests": 'Класс госпожи Мориты · стол впереди — F: уроки, видео, пробные тесты',
    'Pick a language, then a lesson, a video or a mock test.': 'Выберите язык, затем урок, видео или пробный тест.',
}

path = os.path.join('Assets', 'NihongoLife', 'Resources', 'Content', 'Localization', 'ru_table_extra.json')
raw = open(path, encoding='utf-8').read()
table = json.loads(raw)
entries = {e['en']: e for e in table['entries']}
for en, ru in RU.items():
    entries[en] = {'en': en, 'ru': ru}
table['entries'] = list(entries.values())
table['count'] = len(table['entries'])
with open(path, 'w', encoding='utf-8', newline='\n') as f:
    json.dump(table, f, ensure_ascii=False, indent=2)
    f.write('\n')
print(f'{len(RU)} Russian lines merged, {table["count"]} in the extra table')
