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
    'To Hibari': 'В Хибари',
    'Alphabet, pronunciation, A1 → B2 and TORFL mock tests with answer keys.': 'Алфавит, произношение, A1 → B2 и пробные тесты ТРКИ с ответами.',
    'Max test preset: On': 'Режим макс. теста: вкл.',
    'Max test preset: Off': 'Режим макс. теста: выкл.',
    'Ticket needed': 'Нужен билет',
    'Board': 'Сесть в поезд',
    'Board (ticket from the machine on the right)': 'Сесть в поезд (билет — в автомате справа)',
    # Phone HUD
    'Tools': 'Инструменты',
    # Gestures
    'Carry a dish on a tray': 'Нести блюдо на подносе',
    'Guitar on the back': 'Гитара за спиной',
    'Play an arcade machine': 'Играть на игровом автомате',
    # Scene prices
    '/piece': '/шт.',
    '/shift': '/смена',
    '/show': '/выступление',
    'Server wanted': 'Требуется официант',
    # Course library
    '{0} h {1} min': '{0} ч {1} мин',
    '{0} lessons · {1}': '{0} уроков · {1}',
    '{0} lessons · {1} watched': 'Уроков: {0} · просмотрено: {1}',
    '{0} video lessons in courses: Russian, English and Japanese, with a curriculum and progress.':
        'Видеоуроков в курсах: {0}. Русский, английский и японский, с программой и прогрессом.',
    '{0}/{1} done': 'Готово {0}/{1}',
    'All courses': 'Все курсы',
    'Next lesson': 'Следующий урок',
    'Play lesson': 'Смотреть урок',
    # Companies
    '{0}: costs {1} a week · this week income {2}, wages {3} · {4} days to the weekly accounts':
        '{0}: расходы {1} в неделю · за эту неделю доход {2}, зарплаты {3} · до недельного отчёта дней: {4}',
    'Company profit': 'Прибыль компании',
    'Kind: {0}. Fixed costs {1} a week, staff revenue x{2}. Press the middle button to change it.':
        'Тип: {0}. Постоянные расходы {1} в неделю, доход от персонала x{2}. Нажмите среднюю кнопку, чтобы изменить.',
    'Taken from the company': 'Снято со счёта компании',
    "The company is {0} short for this week's costs. Reputation drops.":
        'Компании не хватает {0} на расходы этой недели. Репутация падает.',
    'To my wallet': 'В мой кошелёк',
    'Weekly report: income {0}, wages {1}, costs {2}, to your wallet {3}.':
        'Недельный отчёт: доход {0}, зарплаты {1}, расходы {2}, в ваш кошелёк {3}.',
    # Careers
    'Make this my field': 'Сделать это своей сферой',
    'outside your field −50%': 'не по специальности −50%',
    'Outside your field: 50% pay and experience.': 'Не по специальности: 50% оплаты и опыта.',
    'Your field of work': 'Ваша сфера работы',
    # Swimming
    'The current carries you back towards the shore.': 'Течение относит вас обратно к берегу.',
    'You are far out. Swim back to the shore.': 'Вы заплыли слишком далеко. Плывите к берегу.',
    # Clothes shop
    'Your wardrobe': 'Ваш гардероб',
    'In your wallet: {0} · pick a set to try it on': 'В кошельке: {0} · выберите комплект, чтобы примерить',
    'Mine': 'Моё',
    'Nothing yet. Visit Hibari Mode on the main street.': 'Пока пусто. Загляните в Hibari Mode на главной улице.',
    "The character's own clothes": 'Своя одежда персонажа',
    'Try on': 'Примерить',
    'Wearing': 'Надето',
    'Wear': 'Надеть',
    'In your wardrobe': 'В гардеробе',
    'Buy': 'Купить',
    'Bought: {0}': 'Куплено: {0}',
    'That outfit is not available.': 'Этого комплекта нет.',
    'Not enough money: {0} needed.': 'Не хватает денег: нужно {0}.',
    'Clothes': 'Одежда',
    'Mirror: your wardrobe': 'Зеркало: ваш гардероб',
    'Browse: {0}': 'Смотреть: {0}',
    'Browse the clothes': 'Смотреть одежду',
    'My wardrobe': 'Мой гардероб',
    'HIBARI MODE SHIFT': 'СМЕНА В HIBARI MODE',
    'Folding the clothes…': 'Складываем одежду…',
    'by hand': 'вручную',
    "That does not answer the customer. Listen again.": 'Это не ответ на вопрос покупателя. Послушайте ещё раз.',
    'What do you answer?': 'Что вы ответите?',
    ' — The customer found what they wanted.': ' — Покупатель нашёл то, что искал.',
    ' — The customer takes this one.': ' — Покупатель берёт этот.',
    'These two, please.': 'Вот эти два, пожалуйста.',
    'Add it up again: ¥{0} + ¥{1}.': 'Сложите ещё раз: ¥{0} + ¥{1}.',
    'On the till: ¥{0} and ¥{1}. What is the total?': 'На кассе: ¥{0} и ¥{1}. Какая сумма?',
    'Tidy the clothes table': 'Сложить одежду на столе',
    'Help the customer': 'Помочь покупателю',
    'Customer at the fitting room': 'Покупатель у примерочной',
    'Ring up at the till': 'Пробить на кассе',
    'Hibari Mode (clothes)': 'Hibari Mode (одежда)',
    'HIBARI MODE FLOOR MAP': 'ПЛАН HIBARI MODE',
    'Everyday · street': 'Повседневное · уличное',
    'Office · sport': 'Офис · спорт',
    'Summer · winter · festival': 'Лето · зима · праздник',
    'Fitting rooms · mirror': 'Примерочные · зеркало',
    'Till · Ren': 'Касса · Рэн',
    'Enter the clothes shop': 'Войти в магазин одежды',
    'Play on: Phone': 'Играть: телефон',
    'Play on: Computer': 'Играть: компьютер',
    'Play on: Phone (detected)': 'Играть: телефон (определён)',
    'Play on: Computer (detected)': 'Играть: компьютер (определён)',
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
