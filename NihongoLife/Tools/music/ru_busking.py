"""Russian lines for the player's own guitar (bought at the Hibiki counter, worn on the back, busking in the town), the
air-hockey match rules and the island's boarding sign, merged into Resources/Content/Localization/ru_table_extra.json.
Run from the Unity project folder after changing those English strings:  python Tools/music/ru_busking.py
"""
import json
import os

RU = {
    # The guitar at the counter
    'Hibiki Music': 'Музыкальный магазин «Хибики»',
    'Yes, please ({0}).': 'Да, пожалуйста ({0}).',
    'No, thank you.': 'Нет, спасибо.',
    'There is a second-hand guitar ({0}). Will you buy it? Wear it on your back and busk in the town.':
        'Есть подержанная гитара ({0}). Купите? Носите её за спиной и играйте на улицах города.',
    'Answer the owner:': 'Ответьте хозяину:',
    'Not enough money ({0}).': 'Не хватает денег ({0}).',
    'Bought a guitar': 'Покупка гитары',
    'You own a guitar! It rides on your back. In the town open the bag (B) → Play to busk.':
        'Теперь у вас есть гитара! Она за спиной. В городе откройте сумку (B) → «Играть», чтобы выступить на улице.',
    'Your guitar': 'Ваша гитара',
    'Your own guitar, worn on your back. In the town press Play (E) to busk — passers-by give tips.':
        'Ваша гитара, за спиной. В городе нажмите «Играть» (E) — уличное выступление, прохожие дают чаевые.',
    'Play (E)': 'Играть (E)',
    'Busk outside, in the town (in the shop, use the stage).': 'Уличные выступления — снаружи, в городе (в магазине есть сцена).',
    # Busking
    'Busking tips': 'Чаевые за уличное выступление',
    '{0} people stopped to listen — tips {1}. +1 fan.': '{0} чел. остановились послушать — чаевые {1}. +1 поклонник.',
    'Too slow — people walk on. Try 80–125%.': 'Слишком медленно — люди проходят мимо. Попробуйте 80–125%.',
    'Too fast — nobody can follow. Try 80–125%.': 'Слишком быстро — никто не успевает. Попробуйте 80–125%.',
    'Busking!': 'Уличное выступление!',
    'Busking — pick a song': 'Уличное выступление — выберите песню',
    'Your character plays your guitar where you stand. At the end of a song people within about 12 m tip you if the tempo is {0}–{1}% — the bigger the crowd, the more. Try the station, the plaza or the shops.':
        'Персонаж играет на вашей гитаре прямо здесь. В конце песни люди в радиусе ~12 м дают чаевые, если темп {0}–{1}% — чем больше слушателей, тем больше. Попробуйте у станции, на площади или у магазинов.',
    # Air hockey
    'Mouse / arrows / WASD steer the green mallet. Hit the puck into the robot\'s red goal. First to 5 goals WINS; let 3 into your green goal (3 hearts) and you LOSE. Time up: more goals wins.':
        'Мышь / стрелки / WASD управляют зелёной битой. Забейте шайбу в красные ворота робота. Кто первым забьёт 5 — ПОБЕДА; пропустите 3 в свои зелёные ворота (3 сердца) — ПОРАЖЕНИЕ. Время вышло: побеждает тот, у кого больше голов.',
    'YOUR GOAL': 'ВАШИ ВОРОТА',
    'YOU': 'ВЫ',
    'First to {0} goals wins': 'Побеждает тот, кто первым забьёт {0}',
    'You win {0} – {1}': 'Вы победили {0} – {1}',
    'The robot wins {1} – {0}': 'Робот победил {1} – {0}',
    'A draw {0} – {1}': 'Ничья {0} – {1}',
    # Midori Island: the way home
    'Train home to Hibari': 'Поезд домой, в Хибари',
    'Board': 'Сесть в поезд',
    'Board (ticket from the machine on the right)': 'Сесть в поезд (билет — в автомате справа)',
    # Phone HUD
    'Tools': 'Инструменты',
    # Gestures
    'Carry a dish on a tray': 'Нести блюдо на подносе',
    'Guitar on the back': 'Гитара за спиной',
    'Play an arcade machine': 'Играть на игровом автомате',
}

path = os.path.join('Assets', 'NihongoLife', 'Resources', 'Content', 'Localization', 'ru_table_extra.json')
table = json.load(open(path, encoding='utf-8'))
entries = {e['en']: e for e in table['entries']}
for en, ru in RU.items():
    entries[en] = {'en': en, 'ru': ru}
table['entries'] = list(entries.values())
table['count'] = len(table['entries'])
with open(path, 'w', encoding='utf-8', newline='\n') as f:
    json.dump(table, f, ensure_ascii=True, indent=2)  # the table's own style: escaped, so any code page reads it
    f.write('\n')
print(f'{len(RU)} Russian lines merged, {table["count"]} in the extra table')
