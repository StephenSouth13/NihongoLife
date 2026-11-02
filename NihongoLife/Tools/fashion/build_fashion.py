#!/usr/bin/env python3
"""The Hibari Mode clothes shop: outfits, the shop-floor Japanese, the fashion career and its shift.

Writes Assets/NihongoLife/Resources/Fashion/outfits.json and fashion.json and merges the career, the industry and the
quest into Resources/Progression. Run from the Unity project folder after changing anything here:
    python Tools/fashion/build_fashion.py
"""
import json
import os

RES = os.path.join('Assets', 'NihongoLife', 'Resources')

# Accessories, in units of the face's width. Head parts start at the mouth (x right, y up, z forward);
# chest and back parts start at the upper-chest bone. Every shape takes the outfit's colour for that part.
PARTS = {
    'cap': [
        {'bone': 'crown', 'shape': 'sphere', 'pos': [0, -0.56, -0.04], 'scale': [2.14, 1.16, 2.14]},
        {'bone': 'crown', 'shape': 'sphere', 'pos': [0, -0.88, 0.96], 'scale': [1.5, 0.12, 1.1]},
    ],
    'beanie': [
        {'bone': 'crown', 'shape': 'sphere', 'pos': [0, -0.62, -0.02], 'scale': [2.24, 1.46, 2.24]},
    ],
    'strawhat': [
        {'bone': 'crown', 'shape': 'sphere', 'pos': [0, -0.8, 0], 'scale': [3.2, 0.14, 3.2]},
        {'bone': 'crown', 'shape': 'sphere', 'pos': [0, -0.46, 0], 'scale': [2.1, 1.0, 2.1]},
    ],
    'visor': [
        {'bone': 'brow', 'shape': 'sphere', 'pos': [0, 0, 0], 'scale': [1.98, 0.2, 1.98]},
        {'bone': 'brow', 'shape': 'sphere', 'pos': [0, -0.04, 0.92], 'scale': [1.4, 0.1, 1.0]},
    ],
    'headband': [
        {'bone': 'brow', 'shape': 'sphere', 'pos': [0, 0, 0], 'scale': [1.98, 0.22, 1.98]},
    ],
    'headphones': [
        {'bone': 'crown', 'shape': 'sphere', 'pos': [0, -0.5, 0], 'scale': [2.2, 0.22, 0.34]},
        {'bone': 'brow', 'shape': 'sphere', 'pos': [-1.02, -0.34, 0], 'scale': [0.34, 0.72, 0.72]},
        {'bone': 'brow', 'shape': 'sphere', 'pos': [1.02, -0.34, 0], 'scale': [0.34, 0.72, 0.72]},
    ],
    'glasses': [
        {'bone': 'head', 'shape': 'sphere', 'pos': [-0.2, 0.3, 0.5], 'scale': [0.3, 0.2, 0.04]},
        {'bone': 'head', 'shape': 'sphere', 'pos': [0.2, 0.3, 0.5], 'scale': [0.3, 0.2, 0.04]},
        {'bone': 'head', 'shape': 'cube', 'pos': [0, 0.31, 0.52], 'scale': [0.16, 0.03, 0.03]},
    ],
    'scarf': [
        {'bone': 'chest', 'shape': 'sphere', 'pos': [0, 0.22, 0.02], 'scale': [0.86, 0.3, 0.76]},
        {'bone': 'chest', 'shape': 'cube', 'pos': [0.16, -0.06, 0.34], 'scale': [0.2, 0.56, 0.06]},
    ],
    'tie': [
        {'bone': 'chest', 'shape': 'cube', 'pos': [0, 0.14, 0.36], 'scale': [0.2, 0.14, 0.06]},
        {'bone': 'chest', 'shape': 'cube', 'pos': [0, -0.26, 0.37], 'scale': [0.2, 0.68, 0.05]},
    ],
    'bowtie': [
        {'bone': 'chest', 'shape': 'sphere', 'pos': [-0.13, 0.13, 0.33], 'scale': [0.24, 0.15, 0.08]},
        {'bone': 'chest', 'shape': 'sphere', 'pos': [0.13, 0.13, 0.33], 'scale': [0.24, 0.15, 0.08]},
    ],
    'backpack': [
        {'bone': 'chest', 'shape': 'cube', 'pos': [0, -0.14, -0.42], 'scale': [0.72, 0.84, 0.3]},
        {'bone': 'chest', 'shape': 'cube', 'pos': [-0.25, 0.1, 0.0], 'scale': [0.09, 0.09, 0.82]},
        {'bone': 'chest', 'shape': 'cube', 'pos': [0.25, 0.1, 0.0], 'scale': [0.09, 0.09, 0.82]},
    ],
}

CATEGORIES = [
    ('casual', 'Thường ngày', 'Everyday', 'ふだん着', 'Повседневное'),
    ('street', 'Đường phố', 'Street', 'ストリート', 'Уличный стиль'),
    ('office', 'Công sở', 'Office', 'オフィス', 'Офис'),
    ('sport', 'Thể thao', 'Sport', 'スポーツ', 'Спорт'),
    ('summer', 'Mùa hè', 'Summer', '夏', 'Лето'),
    ('winter', 'Mùa đông', 'Winter', '冬', 'Зима'),
    ('festival', 'Lễ hội', 'Festival', 'お祭り', 'Праздник'),
]

# id, category, vi, en, ja, ru, price, top, bottom, [(part, colour)]
OUTFITS = [
    ('casual_white', 'casual', 'Sơ mi trắng, quần bò', 'White shirt and jeans', '白シャツとジーンズ', 'Белая рубашка и джинсы', 4800, '#F2F0EA', '#3E5C86', []),
    ('casual_knit', 'casual', 'Áo len kem, kính tròn', 'Cream jumper and glasses', 'クリーム色のセーターとめがね', 'Кремовый свитер и очки', 5200, '#E6D3B3', '#6B4F3A', [('glasses', '#2B2B30')]),
    ('casual_mint', 'casual', 'Bộ bạc hà dạo phố', 'Mint day out', 'ミントのおでかけ服', 'Мятный комплект', 5600, '#9FD8C4', '#EDE6D6', [('cap', '#EDE6D6')]),
    ('street_black', 'street', 'Toàn đen đường phố', 'All-black street', 'オールブラック', 'Весь в чёрном', 9800, '#1F2126', '#2C2F36', [('cap', '#16171B')]),
    ('street_neon', 'street', 'Neon Akihabara', 'Akihabara neon', 'アキバネオン', 'Неон Акихабары', 11800, '#7B3FF2', '#22242B', [('headphones', '#F24FA8')]),
    ('street_bomber', 'street', 'Bomber đỏ, ba lô', 'Red bomber and backpack', '赤いボンバーとリュック', 'Красный бомбер и рюкзак', 12500, '#B8322F', '#23252B', [('backpack', '#2A2C33')]),
    ('office_navy', 'office', 'Vest xanh than, cà vạt đỏ', 'Navy suit, red tie', '紺のスーツ', 'Тёмно-синий костюм', 18000, '#1F2C4A', '#1F2C4A', [('tie', '#B33A3A')]),
    ('office_grey', 'office', 'Vest xám, kính', 'Grey suit and glasses', 'グレーのスーツとめがね', 'Серый костюм и очки', 18000, '#6A6F78', '#4C5058', [('tie', '#1F2C4A'), ('glasses', '#1E1E22')]),
    ('office_blouse', 'office', 'Sơ mi trắng, khăn lụa', 'White shirt and silk scarf', '白シャツとスカーフ', 'Белая рубашка и шёлковый платок', 12000, '#FFFFFF', '#2A2D33', [('scarf', '#3F7FD6')]),
    ('sport_track', 'sport', 'Đồ thể thao xanh', 'Blue tracksuit', '青いジャージ', 'Синий спортивный костюм', 7500, '#2F6FD0', '#1B2A4A', [('headband', '#FFFFFF')]),
    ('sport_tennis', 'sport', 'Tennis trắng', 'Tennis whites', 'テニスウェア', 'Белая теннисная форма', 7800, '#F7F7F2', '#F7F7F2', [('visor', '#2FA88F')]),
    ('summer_beach', 'summer', 'Đi biển, mũ rơm', 'Beach day, straw hat', '海の服とむぎわら帽子', 'Пляжный комплект и шляпа', 6200, '#4FC1D9', '#F2C14E', [('strawhat', '#E3C98A')]),
    ('summer_linen', 'summer', 'Vải lanh mùa hè', 'Summer linen', '夏のリネン', 'Летний лён', 6800, '#EFE3C8', '#C9B48F', [('strawhat', '#D8BE82')]),
    ('winter_camel', 'winter', 'Áo khoác nâu, khăn đỏ', 'Camel coat, red scarf', 'キャメルのコートと赤いマフラー', 'Пальто и красный шарф', 16000, '#B98A54', '#33302C', [('scarf', '#B33A3A'), ('beanie', '#3B3631')]),
    ('winter_snow', 'winter', 'Bộ đi tuyết đỏ', 'Red snow set', '赤いスノーウェア', 'Красный зимний комплект', 14500, '#C23B3B', '#3A3F4A', [('beanie', '#F2F0EA'), ('scarf', '#F2F0EA')]),
    ('festival_indigo', 'festival', 'Lễ hội màu chàm', 'Indigo festival', '藍色のお祭り服', 'Индиговый праздничный', 15000, '#2A3F73', '#2A3F73', [('headband', '#FFFFFF')]),
    ('festival_red', 'festival', 'Lễ hội đỏ vàng', 'Red and gold festival', '赤と金のお祭り服', 'Красно-золотой праздничный', 15000, '#B33A3A', '#EED9A8', [('headband', '#EED9A8')]),
    ('festival_gold', 'festival', 'Dạ hội vàng, nơ đen', 'Gold gala, black bow tie', '金のパーティー服', 'Золотой вечерний', 48000, '#C8A24A', '#1C1C20', [('bowtie', '#16171B')]),
]


def outfits():
    return {
        'schema': 'nihongolife.outfits.v1',
        'parts': [{'id': k, 'pieces': v} for k, v in PARTS.items()],
        'categories': [{'id': c[0], 'vi': c[1], 'en': c[2], 'ja': c[3], 'ru': c[4]} for c in CATEGORIES],
        'outfits': [{'id': o[0], 'category': o[1], 'vi': o[2], 'en': o[3], 'ja': o[4], 'ru': o[5], 'price': o[6], 'top': o[7], 'bottom': o[8],
                     'wear': [{'part': p, 'color': c} for p, c in o[9]]} for o in OUTFITS],
    }


FLOOR = {
    'schema': 'nihongolife.fashion_floor.v1',
    # A customer wants a size or a colour; the staff answer with the right word.
    'requests': [
        {'lineJa': 'この シャツの Mサイズは ありますか。', 'reading': 'kono shatsu no emu saizu wa arimasu ka', 'vi': 'Áo này có cỡ M không?', 'en': 'Do you have this shirt in medium?',
         'answerJa': 'はい、Mサイズは こちらです。', 'answerVi': 'Dạ có, cỡ M đây ạ.', 'answerEn': 'Yes, here is the medium.',
         'wrongJa': ['Lサイズは ありません。', 'おにあいです。'], 'wrongVi': ['Không có cỡ L.', 'Hợp với quý khách lắm.'], 'wrongEn': ['We have no large.', 'It suits you.']},
        {'lineJa': 'もう少し 小さいのは ありますか。', 'reading': 'mō sukoshi chiisai no wa arimasu ka', 'vi': 'Có cái nhỏ hơn một chút không?', 'en': 'Do you have a slightly smaller one?',
         'answerJa': 'Sサイズを お持ちします。', 'answerVi': 'Tôi mang cỡ S đến ạ.', 'answerEn': 'I will bring the small.',
         'wrongJa': ['Lサイズを お持ちします。', 'いらっしゃいませ。'], 'wrongVi': ['Tôi mang cỡ L đến ạ.', 'Xin chào quý khách.'], 'wrongEn': ['I will bring the large.', 'Welcome.']},
        {'lineJa': 'これの 青は ありますか。', 'reading': 'kore no ao wa arimasu ka', 'vi': 'Cái này có màu xanh không?', 'en': 'Do you have this in blue?',
         'answerJa': '青は あちらの たなに ございます。', 'answerVi': 'Màu xanh ở kệ đằng kia ạ.', 'answerEn': 'The blue ones are on that shelf.',
         'wrongJa': ['赤は 売り切れです。', '千円です。'], 'wrongVi': ['Màu đỏ hết hàng rồi.', '1.000 yên ạ.'], 'wrongEn': ['Red is sold out.', 'That is ¥1,000.']},
        {'lineJa': 'この ジャケットは あらえますか。', 'reading': 'kono jaketto wa araemasu ka', 'vi': 'Áo khoác này giặt được không?', 'en': 'Can this jacket be washed?',
         'answerJa': 'はい、せんたくきで あらえます。', 'answerVi': 'Dạ được, giặt máy được ạ.', 'answerEn': 'Yes, it is machine washable.',
         'wrongJa': ['はい、あたたかいです。', 'しちゃくしつは あちらです。'], 'wrongVi': ['Dạ, ấm lắm ạ.', 'Phòng thử đồ ở đằng kia.'], 'wrongEn': ['Yes, it is warm.', 'The fitting room is over there.']},
    ],
    # Fitting-room manners.
    'fittings': [
        {'lineJa': 'これを 着てみても いいですか。', 'reading': 'kore o kite mite mo ii desu ka', 'vi': 'Tôi mặc thử cái này được không?', 'en': 'May I try this on?',
         'answerJa': 'はい、しちゃくしつは こちらです。', 'answerVi': 'Dạ được, phòng thử đồ ở đây ạ.', 'answerEn': 'Of course, the fitting room is this way.',
         'wrongJa': ['ありがとうございました。', '三千円です。'], 'wrongVi': ['Cảm ơn quý khách.', '3.000 yên ạ.'], 'wrongEn': ['Thank you for shopping.', 'That is ¥3,000.']},
        {'lineJa': 'サイズは どうでしょうか。…少し 大きいです。', 'reading': 'saizu wa dō deshō ka… sukoshi ōkii desu', 'vi': 'Cỡ thế nào ạ? …Hơi rộng một chút.', 'en': 'How is the size? …A little big.',
         'answerJa': '一つ 小さいサイズを お持ちしましょうか。', 'answerVi': 'Để tôi mang cỡ nhỏ hơn một số nhé?', 'answerEn': 'Shall I bring one size smaller?',
         'wrongJa': ['一つ 大きいサイズを お持ちしましょうか。', 'またの ご来店を お待ちしております。'], 'wrongVi': ['Để tôi mang cỡ lớn hơn nhé?', 'Hẹn gặp lại quý khách.'], 'wrongEn': ['Shall I bring one size bigger?', 'We hope to see you again.']},
        {'lineJa': 'どうですか。にあいますか。', 'reading': 'dō desu ka. niaimasu ka', 'vi': 'Thế nào? Có hợp không?', 'en': 'How is it? Does it suit me?',
         'answerJa': 'とても よく おにあいです。', 'answerVi': 'Rất hợp với quý khách ạ.', 'answerEn': 'It suits you very well.',
         'wrongJa': ['売り切れです。', 'ふくろは いりますか。'], 'wrongVi': ['Hết hàng rồi ạ.', 'Quý khách có cần túi không?'], 'wrongEn': ['It is sold out.', 'Do you need a bag?']},
    ],
}

INDUSTRY = {
    'id': 'fashion', 'color': '#D2558C',
    'nameVi': 'Thời trang', 'nameEn': 'Fashion', 'nameJa': 'ファッション', 'nameRu': 'Мода',
    'blurbVi': 'Hibari Mode: gấp đồ, tư vấn cỡ và màu, phòng thử đồ, thu ngân. Lên cao thành stylist hoặc nhà thiết kế.',
    'blurbEn': 'Hibari Mode: folding, advising on sizes and colours, the fitting room, the till. Later a stylist or a designer.',
    'blurbJa': 'ひばりモード：たたみ、サイズと色の案内、試着室、レジ。のちにスタイリストかデザイナーへ。',
    'blurbRu': 'Hibari Mode: укладка одежды, советы по размеру и цвету, примерочная, касса. Затем стилист или дизайнер.',
    'careers': ['fashion'],
}


LOOK = {'career': 'fashion', 'industry': 'fashion', 'variant': 'fashion_riku', 'gesture': 'fold_clothes', 'alt': 'think', 'prop': ''}


def level(vi, en, ja, salary, shifts, knowledge):
    return {'titleVi': vi, 'titleEn': en, 'titleJa': ja, 'salary': salary, 'shiftsToPromote': shifts, 'knowledgeToPromote': knowledge}


def choice(ja, vi, en, correct=False, hint_vi=None, hint_en=None, branch=None):
    c = {'ja': ja, 'vi': vi, 'en': en}
    if correct:
        c['correct'] = True
    if hint_vi:
        c['hintVi'], c['hintEn'] = hint_vi, hint_en
    if branch:
        c['branch'] = branch
    return c


def event(id_, ja, reading, vi, en, choices):
    return {'id': id_, 'lineJa': ja, 'readingJa': reading, 'meaningVi': vi, 'meaningEn': en, 'choices': choices}


CAREER = {
    'id': 'fashion',
    'nameVi': 'Thời trang', 'nameEn': 'Fashion', 'nameJa': 'ファッション',
    'descriptionVi': 'Làm ở Hibari Mode: gấp đồ, tư vấn cỡ và màu bằng tiếng Nhật, đưa khách vào phòng thử, tính tiền. Lên cao thành stylist hoặc nhà thiết kế.',
    'descriptionEn': 'Work at Hibari Mode: fold the stock, advise on sizes and colours in Japanese, look after the fitting room, run the till. Later become a stylist or a designer.',
    'descriptionJa': 'ひばりモードでたたみ、サイズと色の案内、試着室、レジ。将来はスタイリストかデザイナーへ。',
    'workplaceVi': 'Hibari Mode, phố chính', 'workplaceEn': 'Hibari Mode, main street', 'workplaceJa': 'ひばりモード（商店街）',
    'shiftQuest': 'job_fashion_shift', 'bossNameJa': 'れん店長', 'bossNameVi': 'Anh Ren',
    'workStart': 10, 'workEnd': 18, 'workDays': [2, 3, 4, 5, 6], 'branchAtLevel': 4,
    'levels': [
        level('Nhân viên phụ kho', 'Stock assistant', 'ストックスタッフ', 260, 2, 0),
        level('Nhân viên bán hàng', 'Sales assistant', '販売スタッフ', 420, 3, 120),
        level('Tư vấn thời trang', 'Fashion adviser', 'ファッションアドバイザー', 620, 4, 260),
    ],
    'branches': [
        {'id': 'stylist', 'nameVi': 'Stylist', 'nameEn': 'Stylist', 'nameJa': 'スタイリストコース',
         'descriptionVi': 'Chọn đồ cho khách, cho buổi chụp ảnh và cho sân khấu.', 'descriptionEn': 'Dress customers, photo shoots and stages.', 'descriptionJa': 'お客さま、撮影、舞台の服を選ぶ。',
         'levels': [level('Stylist', 'Stylist', 'スタイリスト', 800, 4, 370), level('Stylist trưởng', 'Lead stylist', 'チーフスタイリスト', 1000, 5, 520), level('Giám đốc hình ảnh', 'Image director', 'イメージディレクター', 1320, 6, 700)]},
        {'id': 'designer', 'nameVi': 'Nhà thiết kế', 'nameEn': 'Designer', 'nameJa': 'デザイナーコース',
         'descriptionVi': 'Vẽ mẫu, chọn vải, làm bộ sưu tập của riêng mình.', 'descriptionEn': 'Sketch, choose fabrics, make your own collection.', 'descriptionJa': 'デザインを描き、布を選び、自分のコレクションを作る。',
         'levels': [level('Trợ lý thiết kế', 'Design assistant', 'デザインアシスタント', 780, 4, 370), level('Nhà thiết kế', 'Designer', 'デザイナー', 980, 5, 520), level('Giám đốc sáng tạo', 'Creative director', 'クリエイティブディレクター', 1350, 6, 700)]},
    ],
    'interviews': [
        {'toLevel': 2, 'lineJa': 'おきゃくさまが「少し 大きい」と 言いました。なんと 言いますか。', 'readingJa': 'Okyakusama ga “sukoshi ōkii” to iimashita. Nan to iimasu ka.',
         'meaningVi': 'Khách nói "hơi rộng". Bạn nói gì?', 'meaningEn': 'A customer says "a little big". What do you say?',
         'choices': [choice('一つ 小さいサイズを お持ちします。', 'Tôi mang cỡ nhỏ hơn ạ.', 'I will bring one size smaller.', True),
                     choice('やせて ください。', 'Quý khách gầy đi đi.', 'Please lose weight.', hint_vi='Không bao giờ nói vậy với khách.', hint_en='Never say that to a customer.'),
                     choice('知りません。', 'Tôi không biết.', 'I do not know.', hint_vi='Hãy đề nghị mang cỡ khác: お持ちします.', hint_en='Offer another size: お持ちします.')]},
        {'toLevel': 3, 'lineJa': 'おきゃくさまに 色を すすめて ください。', 'readingJa': 'Okyakusama ni iro o susumete kudasai.',
         'meaningVi': 'Hãy gợi ý màu cho khách.', 'meaningEn': 'Suggest a colour to the customer.',
         'choices': [choice('こちらの 紺は いかがでしょうか。', 'Quý khách thấy màu xanh than này thế nào ạ?', 'How about this navy?', True),
                     choice('紺を 買いなさい。', 'Mua màu xanh than đi.', 'Buy the navy.', hint_vi='Là mệnh lệnh. Dùng いかがでしょうか.', hint_en='That is an order. Use いかがでしょうか.'),
                     choice('紺は きらいです。', 'Tôi ghét màu xanh than.', 'I hate navy.', hint_vi='Không nói sở thích của mình với khách.', hint_en='Your own taste does not matter here.')]},
        {'toLevel': 4, 'lineJa': 'しょうらい、どんな 仕事を したいですか。', 'readingJa': 'Shōrai, donna shigoto o shitai desu ka.',
         'meaningVi': 'Sau này bạn muốn làm công việc nào?', 'meaningEn': 'What work do you want to do in the future?',
         'choices': [choice('お客さまに 合う 服を えらぶ 仕事が したいです。', 'Tôi muốn chọn đồ hợp với từng khách.', 'I want to choose clothes that suit each customer.', True, branch='stylist'),
                     choice('自分で 服を デザインしたいです。', 'Tôi muốn tự thiết kế quần áo.', 'I want to design clothes myself.', True, branch='designer')]},
        {'toLevel': 5, 'branch': 'stylist', 'lineJa': 'ざっしの さつえいで、モデルの 服が 合いません。どうしますか。', 'readingJa': 'Zasshi no satsuei de, moderu no fuku ga aimasen. Dō shimasu ka.',
         'meaningVi': 'Trong buổi chụp tạp chí, đồ của người mẫu không vừa. Bạn làm gì?', 'meaningEn': 'At a magazine shoot the clothes do not fit the model. What do you do?',
         'choices': [choice('すぐに 別の サイズを 用意します。', 'Tôi chuẩn bị ngay cỡ khác.', 'I get another size ready at once.', True),
                     choice('そのまま さつえいします。', 'Cứ chụp như vậy.', 'We shoot as it is.', hint_vi='Khách hàng sẽ không hài lòng.', hint_en='The client will not be happy.')]},
        {'toLevel': 6, 'branch': 'stylist', 'lineJa': 'ブランドの イメージを どう 変えますか。', 'readingJa': 'Burando no imēji o dō kaemasu ka.',
         'meaningVi': 'Bạn sẽ đổi hình ảnh thương hiệu thế nào?', 'meaningEn': 'How will you change the brand image?',
         'choices': [choice('お客さまの 声を 聞いて、少しずつ 変えます。', 'Nghe khách hàng rồi đổi từng bước.', 'I listen to customers and change it step by step.', True),
                     choice('全部 すぐに 変えます。', 'Đổi hết ngay lập tức.', 'I change everything at once.', hint_vi='Thương hiệu cần sự tin cậy lâu dài.', hint_en='A brand needs lasting trust.')]},
        {'toLevel': 5, 'branch': 'designer', 'lineJa': 'この 布は 高すぎます。どうしますか。', 'readingJa': 'Kono nuno wa takasugimasu. Dō shimasu ka.',
         'meaningVi': 'Loại vải này đắt quá. Bạn làm gì?', 'meaningEn': 'This fabric costs too much. What do you do?',
         'choices': [choice('にた 布を さがして、ねだんを くらべます。', 'Tìm loại vải tương tự rồi so giá.', 'I look for a similar fabric and compare prices.', True),
                     choice('ねだんを 見ないで 買います。', 'Mua luôn không xem giá.', 'I buy it without checking the price.', hint_vi='Nhà thiết kế cũng phải giữ ngân sách.', hint_en='A designer has a budget too.')]},
        {'toLevel': 6, 'branch': 'designer', 'lineJa': '新しい コレクションの テーマを 教えて ください。', 'readingJa': 'Atarashii korekushon no tēma o oshiete kudasai.',
         'meaningVi': 'Hãy cho biết chủ đề bộ sưu tập mới.', 'meaningEn': 'Tell us the theme of the new collection.',
         'choices': [choice('ひばり町の 四季を テーマに しました。', 'Tôi lấy bốn mùa của phố Hibari làm chủ đề.', 'The four seasons of Hibari town.', True),
                     choice('テーマは ありません。', 'Không có chủ đề.', 'There is no theme.', hint_vi='Người đứng đầu phải có ý tưởng rõ ràng.', hint_en='A director needs a clear idea.')]},
    ],
    'events': [
        event('fashion_sale', 'セールで お店が こんでいます。', 'Sēru de omise ga konde imasu.', 'Cửa hàng đông vì đợt giảm giá.', 'The shop is crowded for the sale.', [
            {'ja': 'レジを 手伝います。', 'vi': 'Tôi ra phụ thu ngân.', 'en': 'I help at the till.', 'performance': 15, 'reputation': 3, 'mood': 'Proud'},
            {'ja': 'たたむのを 続けます。', 'vi': 'Tôi tiếp tục gấp đồ.', 'en': 'I keep folding.', 'performance': 6, 'reputation': 1},
            {'ja': '休けいに 行きます。', 'vi': 'Tôi đi nghỉ.', 'en': 'I go on a break.', 'performance': -10, 'reputation': -3, 'mood': 'Embarrassed'},
        ]),
        event('fashion_stain', '新しい シャツに しみが あります。', 'Atarashii shatsu ni shimi ga arimasu.', 'Chiếc áo mới có vết bẩn.', 'There is a stain on a new shirt.', [
            {'ja': 'すぐに 新しいのと 交換します。', 'vi': 'Đổi ngay chiếc mới.', 'en': 'I swap it for a new one at once.', 'performance': 15, 'reputation': 3, 'mood': 'Proud'},
            {'ja': '店長に 報告します。', 'vi': 'Báo với quản lý.', 'en': 'I tell the manager.', 'performance': 8, 'reputation': 1},
            {'ja': 'そのまま たなに もどします。', 'vi': 'Cứ để lại lên kệ.', 'en': 'I put it back on the shelf.', 'performance': -10, 'reputation': -3, 'mood': 'Embarrassed'},
        ]),
        event('fashion_return', 'この シャツを 返品したいのですが……', 'Kono shatsu o henpin shitai no desu ga…', 'Tôi muốn trả lại chiếc áo này…', 'I would like to return this shirt…', [
            {'ja': 'レシートは お持ちでしょうか。', 'vi': 'Quý khách có hoá đơn không ạ?', 'en': 'Do you have the receipt?', 'performance': 15, 'reputation': 3, 'mood': 'Proud'},
            {'ja': '店長を 呼びます。', 'vi': 'Tôi gọi quản lý.', 'en': 'I will call the manager.', 'performance': 8, 'reputation': 1},
            {'ja': '返品は できません。', 'vi': 'Không trả được.', 'en': 'No returns.', 'performance': -10, 'reputation': -3, 'mood': 'Embarrassed'},
        ]),
    ],
}


def objective(id_, event_, target, count, vi, en, ja, after=False):
    return {'id': id_, 'event': event_, 'target': target, 'count': count, 'afterAll': after, 'textVi': vi, 'textEn': en, 'textJa': ja}


QUEST = {
    'id': 'job_fashion_shift', 'category': 'job',
    'titleVi': 'Làm ở Hibari Mode', 'titleEn': 'Shift at Hibari Mode', 'titleJa': 'ひばりモードで はたらく',
    'descriptionVi': 'Phụ anh Ren: gấp đồ trên bàn, tư vấn cỡ và màu, đưa khách vào phòng thử, tính tiền rồi báo cáo.',
    'descriptionEn': 'Help Ren: fold the tables, advise on sizes and colours, look after the fitting room, run the till, then report.',
    'descriptionJa': 'たたみ、サイズと色の案内、試着室、レジのあと、店長に報告。',
    'giverNpc': 'npc_fashion_manager', 'giverNameVi': 'Anh Ren (quản lý)', 'giverNameEn': 'Ren (manager)', 'giverNameJa': 'れん店長',
    'locationVi': 'Hibari Mode', 'locationEn': 'Hibari Mode', 'locationJa': 'ひばりモード', 'scene': '56_FashionStore',
    'minLevel': 1, 'minKnowledge': 0, 'requiresQuests': [], 'repeatable': True, 'cooldownMinutes': 5, 'grantOnAccept': [],
    'objectives': [
        objective('fold', 'fold', 'fashion_table', 3, 'Gấp gọn 3 bàn quần áo', 'Tidy 3 clothes tables', 'テーブルを 3つ たたむ'),
        objective('advise', 'advise', 'fashion_customer', 2, 'Tư vấn cỡ và màu cho 2 khách', 'Advise 2 customers on sizes and colours', 'お客さま 2人に 案内'),
        objective('fit', 'fit', 'fashion_fitting', 1, 'Chăm sóc khách ở phòng thử đồ', 'Look after a customer at the fitting room', '試着室の お客さま'),
        objective('checkout', 'checkout', 'fashion_register', 1, 'Tính tiền ở quầy', 'Ring up at the till', 'レジで 会計'),
        objective('talk', 'talk', 'npc_fashion_manager', 1, 'Báo cáo với anh Ren', 'Report to Ren', 'れん店長に 報告', True),
    ],
    'rewards': {'yen': 1900, 'xp': 45, 'knowledge': 8},
}


def write(path, data):
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')


def upsert(items, item):
    for i, old in enumerate(items):
        if old['id'] == item['id']:
            items[i] = item
            return
    items.append(item)


def main():
    folder = os.path.join(RES, 'Fashion')
    os.makedirs(folder, exist_ok=True)
    write(os.path.join(folder, 'outfits.json'), outfits())
    write(os.path.join(folder, 'fashion.json'), FLOOR)

    p = os.path.join(RES, 'Progression', 'careers.json')
    data = json.load(open(p, encoding='utf-8'))
    upsert(data['careers'], CAREER)
    write(p, data)

    p = os.path.join(RES, 'Progression', 'career_industries.json')
    data = json.load(open(p, encoding='utf-8'))
    upsert(data['industries'], INDUSTRY)
    data['looks'] = [l for l in data['looks'] if l['career'] != 'fashion'] + [LOOK]
    write(p, data)

    p = os.path.join(RES, 'Progression', 'progression.json')
    data = json.load(open(p, encoding='utf-8'))
    upsert(data['quests'], QUEST)
    write(p, data)
    print(len(OUTFITS), 'outfits, career and shift written')


if __name__ == '__main__':
    main()
