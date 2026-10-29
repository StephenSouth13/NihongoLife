"""Adds the music-shop careers (musician, producer / DJ, painter) to careers.json and their shifts and career tasks to
progression.json. Idempotent: existing entries with the same ids are replaced. UTF-8 in and out.

Run from the Unity project folder:  python Tools/music/add_arts_careers.py
"""
import json
import os

ROOT = os.path.join('Assets', 'NihongoLife', 'Resources', 'Progression')
SCENE = '55_MusicShop'
OWNER = 'npc_music_owner'
LOC = {'locationVi': 'Tiệm nhạc & tranh Hibiki', 'locationEn': 'Hibiki Music & Art', 'locationJa': 'ひびき楽器店'}
GIVER = {'giverNpc': OWNER, 'giverNameVi': 'Chú Hibiki (chủ tiệm)', 'giverNameEn': 'Mr Hibiki (owner)', 'giverNameJa': 'ひびき店長'}


def lvl(vi, en, ja, salary, shifts, knowledge):
    return {'titleVi': vi, 'titleEn': en, 'titleJa': ja, 'salary': salary, 'shiftsToPromote': shifts, 'knowledgeToPromote': knowledge}


def ok(ja, vi, en, branch=None):
    c = {'ja': ja, 'vi': vi, 'en': en, 'correct': True}
    if branch: c['branch'] = branch
    return c


def no(ja, vi, en, hint_vi, hint_en):
    return {'ja': ja, 'vi': vi, 'en': en, 'hintVi': hint_vi, 'hintEn': hint_en}


def ask(level, ja, reading, vi, en, choices, branch=None):
    i = {'toLevel': level, 'lineJa': ja, 'readingJa': reading, 'meaningVi': vi, 'meaningEn': en, 'choices': choices}
    if branch: i['branch'] = branch
    return i


def act(ja, vi, en, performance, reputation=0, mood=None):
    c = {'ja': ja, 'vi': vi, 'en': en, 'performance': performance}
    if reputation: c['reputation'] = reputation
    if mood: c['mood'] = mood
    return c


def event(id, ja, reading, vi, en, choices):
    return {'id': id, 'lineJa': ja, 'readingJa': reading, 'meaningVi': vi, 'meaningEn': en, 'choices': choices}


musician = {
    'id': 'musician', 'nameVi': 'Nhạc công', 'nameEn': 'Musician', 'nameJa': 'ミュージシャン',
    'descriptionVi': 'Biểu diễn trên sân khấu tiệm nhạc Hibiki: nhân vật đàn cho khán giả, bạn giữ nhịp vừa phải (80–125%) để được vỗ tay và có người hâm mộ — rồi thành nghệ sĩ piano hoặc ca sĩ – nhạc sĩ.',
    'descriptionEn': 'Play the stage of the Hibiki music shop: your character performs and you keep the tempo right (80–125%) for applause and fans — then become a pianist or a singer-songwriter.',
    'descriptionJa': 'ひびき楽器店のステージで演奏。テンポ80〜125%で拍手とファンを集めて、ピアニストかシンガーソングライターへ。',
    'workplaceVi': 'Sân khấu tiệm nhạc Hibiki', 'workplaceEn': 'The Hibiki music shop stage', 'workplaceJa': 'ひびき楽器店のステージ',
    'shiftQuest': 'job_music_gig', 'bossNameJa': 'ひびき店長', 'bossNameVi': 'Chú Hibiki',
    'workStart': 17, 'workEnd': 22, 'workDays': [4, 5, 6, 0], 'branchAtLevel': 4,
    'levels': [lvl('Nhạc công tập sự', 'Trainee musician', '見習いミュージシャン', 240, 2, 0),
               lvl('Nhạc công quán cà phê', 'Café musician', 'カフェ演奏者', 400, 3, 120),
               lvl('Nhạc công phòng thu', 'Session musician', 'スタジオミュージシャン', 600, 4, 260)],
    'branches': [
        {'id': 'pianist', 'nameVi': 'Nghệ sĩ piano', 'nameEn': 'Pianist', 'nameJa': 'ピアニストコース',
         'descriptionVi': 'Đệm đàn, độc tấu rồi hoà nhạc: từ nghệ sĩ piano lên giám đốc âm nhạc.',
         'descriptionEn': 'Accompanying, recitals, then concerts: from pianist to music director.',
         'descriptionJa': '伴奏、リサイタル、コンサート。ピアニストから音楽監督へ。',
         'levels': [lvl('Nghệ sĩ piano', 'Pianist', 'ピアニスト', 800, 4, 380),
                    lvl('Nghệ sĩ piano hoà nhạc', 'Concert pianist', 'コンサートピアニスト', 1100, 5, 520),
                    lvl('Giám đốc âm nhạc', 'Music director', '音楽監督', 1500, 0, 0)]},
        {'id': 'songwriter', 'nameVi': 'Ca sĩ – nhạc sĩ', 'nameEn': 'Singer-songwriter', 'nameJa': 'シンガーソングライターコース',
         'descriptionVi': 'Tự viết và hát bài của mình: ra mắt hãng đĩa rồi đi lưu diễn.',
         'descriptionEn': 'Write and sing your own songs: a label debut, then a tour.',
         'descriptionJa': '自分の曲を作って歌う。メジャーデビューからツアーへ。',
         'levels': [lvl('Ca sĩ – nhạc sĩ', 'Singer-songwriter', 'シンガーソングライター', 780, 4, 360),
                    lvl('Nghệ sĩ hãng đĩa lớn', 'Major-label artist', 'メジャーアーティスト', 1080, 5, 500),
                    lvl('Nghệ sĩ lưu diễn', 'Touring headliner', 'ツアーアーティスト', 1480, 0, 0)]},
    ],
    'interviews': [
        ask(2, 'ほんばんの まえに なにを しますか。', 'Honban no mae ni nani o shimasu ka.', 'Trước giờ diễn bạn làm gì?', 'What do you do before the show?', [
            ok('チューニングと リハーサルを します。', 'Lên dây đàn và tập dượt.', 'Tune up and rehearse.'),
            no('なにも しません。', 'Không làm gì.', 'Nothing.', 'Đàn lệch dây và chưa dượt thì dễ hỏng buổi diễn.', 'An untuned instrument and no run-through spoil a show.'),
            no('おきゃくさんに あやまって おきます。', 'Xin lỗi khán giả trước.', 'Apologise to the audience in advance.', 'Chuẩn bị kỹ thì không cần xin lỗi.', 'Prepare well and there is nothing to apologise for.')]),
        ask(3, 'ちょさくけんの ある きょくを みせで えんそうする とき、なにが ひつようですか。', 'Chosakuken no aru kyoku o mise de ensō suru toki, nani ga hitsuyō desu ka.',
            'Diễn bài còn bản quyền ở cửa hàng thì cần gì?', 'Playing a copyrighted song in a shop: what is needed?', [
            ok('おみせが JASRAC などに きょかを とります。', 'Cửa hàng xin phép (trả phí) qua JASRAC hoặc tổ chức quản lý quyền.', 'The venue gets a licence from JASRAC or a similar society.'),
            no('なにも いりません。', 'Không cần gì.', 'Nothing.', 'Biểu diễn công khai bài còn bản quyền cần giấy phép; bài đã hết bản quyền (như Beethoven) thì không.', 'Public performance of a copyrighted song needs a licence; public-domain music (Beethoven) does not.'),
            no('がくふを かえば じゆうです。', 'Mua bản nhạc là được tự do.', 'Buying the sheet music is enough.', 'Mua bản nhạc không kèm quyền biểu diễn công khai.', 'Sheet music does not include public-performance rights.')]),
        ask(4, 'これから どの みちに すすみたいですか。', 'Kore kara dono michi ni susumitai desu ka.', 'Từ giờ bạn muốn đi theo hướng nào?', 'Which way would you like to go now?', [
            ok('ピアノを きわめたいです。', 'Tôi muốn theo đuổi piano tới cùng.', 'I want to master the piano.', 'pianist'),
            ok('じぶんの きょくを つくって うたいたいです。', 'Tôi muốn tự sáng tác và hát.', 'I want to write and sing my own songs.', 'songwriter')]),
        ask(5, 'ペダルを ふみすぎると どう なりますか。', 'Pedaru o fumisugiru to dō narimasu ka.', 'Đạp pedal ngân quá nhiều thì sao?', 'What happens if you hold the sustain pedal too much?', [
            ok('おとが にごります。コードが かわったら ふみかえます。', 'Tiếng bị đục; đổi hợp âm thì nhấc pedal đạp lại.', 'The sound gets muddy; change the pedal when the chord changes.'),
            no('おとが おおきく なるだけです。', 'Chỉ to tiếng hơn thôi.', 'It is only louder.', 'Pedal ngân giữ mọi dây rung nên các hợp âm chồng lên nhau.', 'The pedal keeps every string ringing, so chords blur together.')], 'pianist'),
        ask(6, 'コンサートの まえに きんちょうしたら どうしますか。', 'Konsāto no mae ni kinchō shitara dō shimasu ka.', 'Hồi hộp trước buổi hoà nhạc thì làm sao?', 'Nervous before a concert: what do you do?', [
            ok('ふかこきゅうして、いつもの れんしゅうを しんじます。', 'Hít thở sâu và tin vào những buổi tập.', 'Breathe deeply and trust my practice.'),
            no('コンサートを キャンセルします。', 'Huỷ buổi hoà nhạc.', 'Cancel the concert.', 'Hồi hộp là bình thường; huỷ phút chót làm mất lòng khán giả và ban tổ chức.', 'Nerves are normal; a last-minute cancellation lets everyone down.')], 'pianist'),
        ask(5, 'メロディーと コードが あわない とき、どうしますか。', 'Merodī to kōdo ga awanai toki, dō shimasu ka.', 'Giai điệu và hợp âm không khớp thì làm sao?', 'The melody and the chords clash: what do you do?', [
            ok('つよい はくで コードの なかの おとを つかいます。', 'Dùng nốt thuộc hợp âm ở phách mạnh.', 'Use chord tones on the strong beats.'),
            no('きに しません。', 'Kệ nó.', 'Ignore it.', 'Nốt ngoài hợp âm ở phách mạnh nghe chỏi; đặt nốt hợp âm ở đó.', 'Non-chord notes on strong beats sound wrong; put chord tones there.')], 'songwriter'),
        ask(6, 'ツアーで のどを まもるには どうしますか。', 'Tsuā de nodo o mamoru ni wa dō shimasu ka.', 'Đi lưu diễn thì giữ giọng thế nào?', 'How do you look after your voice on tour?', [
            ok('よく ねて、みずを のんで、ウォームアップします。', 'Ngủ đủ, uống nước và khởi động giọng.', 'Sleep well, drink water and warm up.'),
            no('まいにち おおごえで れんしゅうしつづけます。', 'Ngày nào cũng gào thật to để luyện.', 'Keep practising at full volume every day.', 'Hát gắng quá sức làm tổn thương dây thanh; cần nghỉ ngơi.', 'Straining hurts the vocal folds; they need rest.')], 'songwriter'),
    ],
    'events': [
        event('mus_string', 'ライブちゅうに ギターの げんが きれました！', 'Raibu-chū ni gitā no gen ga kiremashita!', 'Đang diễn thì đứt dây đàn guitar!', 'A guitar string snapped mid-show!', [
            act('おちついて ほかの げんで つづけます。', 'Bình tĩnh chơi tiếp bằng các dây còn lại.', 'Stay calm and carry on with the other strings.', 10, 2, 'Proud'),
            act('スペアの ギターに かえます。', 'Đổi sang đàn dự phòng.', 'Switch to the spare guitar.', 8, 1),
            act('えんそうを やめて かえります。', 'Bỏ dở rồi về.', 'Stop and go home.', -12, -2, 'Embarrassed')]),
        event('mus_request', 'おきゃくさんが しらない きょくを リクエストしました。', 'Okyakusan ga shiranai kyoku o rikuesuto shimashita.', 'Khách yêu cầu một bài bạn không biết.', 'A guest requests a song you do not know.', [
            act('にている きょくを ていあんします。', 'Gợi ý một bài tương tự.', 'Suggest a similar song.', 8, 2, 'Confident'),
            act('みみコピーで ためしに ひいて みます。', 'Thử chơi theo tai.', 'Try to play it by ear.', 4, 1),
            act('きこえない ふりを します。', 'Giả vờ không nghe thấy.', 'Pretend not to hear.', -6)]),
    ],
}

producer = {
    'id': 'producer', 'nameVi': 'DJ & nhà sản xuất', 'nameEn': 'DJ & producer', 'nameJa': 'DJ・プロデューサー',
    'descriptionVi': 'Chơi DJ ở tiệm nhạc Hibiki: chọn bài tiếp theo khớp nhịp (BPM) và hoà âm (vòng Camelot) — rồi thành DJ sân khấu lớn hoặc nhà sản xuất âm nhạc.',
    'descriptionEn': 'DJ at the Hibiki music shop: pick the next record by tempo (BPM) and key (the Camelot wheel) — then headline clubs or produce music.',
    'descriptionJa': 'ひびき楽器店でDJ。BPMとキー（カメロット）で次の曲をえらび、クラブDJか音楽プロデューサーへ。',
    'workplaceVi': 'Bàn DJ tiệm nhạc Hibiki', 'workplaceEn': 'The Hibiki DJ booth', 'workplaceJa': 'ひびき楽器店のDJブース',
    'shiftQuest': 'job_dj_set', 'bossNameJa': 'DJ ソウタ', 'bossNameVi': 'DJ Sōta',
    'workStart': 18, 'workEnd': 23, 'workDays': [4, 5, 6], 'branchAtLevel': 4,
    'levels': [lvl('Phụ âm thanh', 'Sound assistant', '音響アシスタント', 260, 2, 0),
               lvl('DJ mở màn', 'Opening DJ', 'オープニングDJ', 420, 3, 120),
               lvl('DJ thường trú', 'Resident DJ', 'レジデントDJ', 620, 4, 260)],
    'branches': [
        {'id': 'club_dj', 'nameVi': 'DJ sân khấu', 'nameEn': 'Club DJ', 'nameJa': 'クラブDJコース',
         'descriptionVi': 'Đọc sàn nhảy, giữ năng lượng cả đêm: DJ khách mời, DJ chính rồi DJ lễ hội.',
         'descriptionEn': 'Read the floor and keep the energy all night: guest, headliner, festival DJ.',
         'descriptionJa': 'フロアを読んで一晩もりあげる。ゲストDJからフェスDJへ。',
         'levels': [lvl('DJ khách mời', 'Guest DJ', 'ゲストDJ', 800, 4, 380),
                    lvl('DJ chính', 'Headliner DJ', 'ヘッドライナーDJ', 1100, 5, 520),
                    lvl('DJ lễ hội', 'Festival DJ', 'フェスDJ', 1500, 0, 0)]},
        {'id': 'producer', 'nameVi': 'Nhà sản xuất âm nhạc', 'nameEn': 'Music producer', 'nameJa': 'プロデューサーコース',
         'descriptionVi': 'Làm beat, phối khí, master: từ người làm nhạc tới chủ hãng đĩa.',
         'descriptionEn': 'Beats, arrangement, mastering: from track maker to label head.',
         'descriptionJa': 'トラック、アレンジ、マスタリング。トラックメイカーからレーベル代表へ。',
         'levels': [lvl('Người làm nhạc', 'Track maker', 'トラックメイカー', 780, 4, 360),
                    lvl('Nhà sản xuất âm nhạc', 'Music producer', '音楽プロデューサー', 1080, 5, 500),
                    lvl('Chủ hãng đĩa', 'Label head', 'レーベル代表', 1480, 0, 0)]},
    ],
    'interviews': [
        ask(2, 'BPM とは なんですか。', 'BPM to wa nan desu ka.', 'BPM là gì?', 'What is BPM?', [
            ok('いっぷんかんの はくの かずです。', 'Số phách trong một phút.', 'Beats per minute.'),
            no('おとの おおきさの たんいです。', 'Đơn vị âm lượng.', 'A unit of loudness.', 'Âm lượng đo bằng dB; BPM là nhịp độ.', 'Loudness is in dB; BPM is the tempo.'),
            no('きょくの ながさです。', 'Độ dài bài hát.', 'The length of the song.', 'BPM là số phách mỗi phút, không phải độ dài.', 'BPM counts beats per minute, not the length.')]),
        ask(3, 'つぎの きょくの キーが あわない とき、どうしますか。', 'Tsugi no kyoku no kī ga awanai toki, dō shimasu ka.', 'Bài tiếp theo lệch giọng thì làm sao?', 'The next record is in a clashing key: what do you do?', [
            ok('カメロットで となりの キーの きょくを えらびます。', 'Chọn bài giọng liền kề trên vòng Camelot.', 'Pick a record in a neighbouring key on the Camelot wheel.'),
            no('おおきい おとで むりやり まぜます。', 'Vặn to rồi trộn bừa.', 'Turn it up and force the mix.', 'Hai giọng chỏi nhau nghe rất khó chịu, to hơn lại càng rõ.', 'Clashing keys sound bad, and louder makes it worse.')]),
        ask(4, 'これから どこで はたらきたいですか。', 'Kore kara doko de hatarakitai desu ka.', 'Từ giờ bạn muốn làm ở đâu?', 'Where would you like to work from now on?', [
            ok('クラブで みんなを おどらせたいです。', 'Tôi muốn làm cả sàn nhảy cùng nhảy.', 'In clubs, making everyone dance.', 'club_dj'),
            ok('スタジオで きょくを つくりたいです。', 'Tôi muốn làm nhạc trong phòng thu.', 'In the studio, making tracks.', 'producer')]),
        ask(5, 'フロアの おきゃくさんが へって きました。', 'Furoa no okyakusan ga hette kimashita.', 'Khách trên sàn đang vãn dần.', 'The dance floor is emptying.', [
            ok('フロアを みて きょくの エネルギーを かえます。', 'Nhìn sàn và đổi năng lượng của bài.', 'Watch the floor and change the energy.'),
            no('よていどおり ながしつづけます。', 'Cứ phát đúng danh sách định sẵn.', 'Keep playing the planned list.', 'DJ giỏi đọc phản ứng của khách chứ không chỉ theo danh sách.', 'A good DJ reads the room, not just the list.')], 'club_dj'),
        ask(6, 'おとが おおきすぎると いわれました。', 'Oto ga ōkisugiru to iwaremashita.', 'Có người phàn nàn nhạc quá to.', 'Someone says the music is too loud.', [
            ok('おんりょうを さげて、みみせんを すすめます。', 'Giảm âm lượng và gợi ý dùng nút tai.', 'Turn it down and suggest earplugs.'),
            no('もっと あげます。', 'Vặn to thêm.', 'Turn it up more.', 'Tiếng quá lớn kéo dài gây tổn thương thính lực vĩnh viễn.', 'Long exposure to very loud sound damages hearing for good.')], 'club_dj'),
        ask(5, 'ほかの ひとの きょくを サンプルに つかう とき、ひつようなのは？', 'Hoka no hito no kyoku o sanpuru ni tsukau toki, hitsuyō na no wa?', 'Lấy mẫu (sample) nhạc người khác thì cần gì?', 'Sampling someone else\'s record: what do you need?', [
            ok('けんりしゃの きょか（クリアランス）です。', 'Sự cho phép của chủ quyền (clearance).', 'Permission from the rights holders (clearance).'),
            no('みじかければ じゆうです。', 'Ngắn thì dùng tự do.', 'Short samples are free.', 'Không có quy tắc "ngắn là được": mẫu ngắn vẫn cần xin phép.', 'There is no "short is fine" rule: even short samples need clearance.')], 'producer'),
        ask(6, 'マスタリングで いちばん だいじなのは？', 'Masutaringu de ichiban daiji na no wa?', 'Điều quan trọng nhất khi master là gì?', 'What matters most in mastering?', [
            ok('いろいろな スピーカーで きいて バランスを たしかめます。', 'Nghe trên nhiều loa để kiểm tra độ cân bằng.', 'Check the balance on many speakers.'),
            no('とにかく おとを おおきく します。', 'Cứ làm cho thật to.', 'Make it as loud as possible.', 'Các nền tảng nghe nhạc tự cân âm lượng; nén quá tay chỉ làm mất độ động.', 'Streaming services normalise loudness; over-compression just kills the dynamics.')], 'producer'),
    ],
    'events': [
        event('dj_silence', 'ミキサーの おとが きゅうに きえました！', 'Mikisā no oto ga kyū ni kiemashita!', 'Bàn mixer đột nhiên mất tiếng!', 'The mixer suddenly goes silent!', [
            act('おちついて ケーブルと ボリュームを たしかめます。', 'Bình tĩnh kiểm tra dây cáp và âm lượng.', 'Calmly check the cables and the faders.', 10, 2, 'Confident'),
            act('あわてて ぜんぶの ボタンを おします。', 'Hoảng loạn bấm hết mọi nút.', 'Panic and press every button.', -8, 0, 'Tense')]),
        event('dj_request', 'よっぱらった おきゃくさんが しつこく リクエストしています。', 'Yopparatta okyakusan ga shitsukoku rikuesuto shite imasu.', 'Một vị khách say cứ nằng nặc đòi bài.', 'A drunk guest keeps pestering you for a song.', [
            act('ていねいに ことわって、スタッフに つたえます。', 'Từ chối lịch sự và báo nhân viên.', 'Decline politely and tell the staff.', 8, 2),
            act('どなって おいかえします。', 'Quát đuổi đi.', 'Shout them away.', -10, -2, 'Embarrassed')]),
    ],
}

painter = {
    'id': 'painter', 'nameVi': 'Hoạ sĩ', 'nameEn': 'Painter', 'nameJa': '画家',
    'descriptionVi': 'Vẽ tranh theo đơn ở góc giá vẽ tiệm Hibiki (tô theo số theo các kiệt tác), giao tranh cho khách — rồi thành hoạ sĩ minh hoạ hoặc hoạ sĩ triển lãm.',
    'descriptionEn': 'Paint commissions at the Hibiki shop\'s easel (paint-by-numbers after the masterpieces), deliver them — then become an illustrator or an exhibiting artist.',
    'descriptionJa': 'ひびき楽器店のイーゼルで依頼の絵をかいて納品。イラストレーターか個展作家へ。',
    'workplaceVi': 'Góc giá vẽ tiệm Hibiki', 'workplaceEn': 'The Hibiki shop easel', 'workplaceJa': 'ひびき楽器店のイーゼル',
    'shiftQuest': 'job_art_commission', 'bossNameJa': 'ひびき店長', 'bossNameVi': 'Chú Hibiki',
    'workStart': 9, 'workEnd': 17, 'workDays': [1, 2, 3, 4, 5], 'branchAtLevel': 4,
    'levels': [lvl('Phụ việc xưởng vẽ', 'Studio assistant', 'アトリエアシスタント', 250, 2, 0),
               lvl('Hoạ sĩ vẽ theo đơn', 'Commission painter', '受注画家', 410, 3, 120),
               lvl('Hoạ sĩ chuyên nghiệp', 'Professional painter', 'プロの画家', 610, 4, 260)],
    'branches': [
        {'id': 'illustrator', 'nameVi': 'Hoạ sĩ minh hoạ', 'nameEn': 'Illustrator', 'nameJa': 'イラストレーターコース',
         'descriptionVi': 'Tranh cho sách, game, quảng cáo: từ hoạ sĩ minh hoạ lên giám đốc nghệ thuật.',
         'descriptionEn': 'Pictures for books, games and ads: from illustrator to art director.',
         'descriptionJa': '本・ゲーム・広告の絵。イラストレーターからアートディレクターへ。',
         'levels': [lvl('Hoạ sĩ minh hoạ', 'Illustrator', 'イラストレーター', 790, 4, 370),
                    lvl('Hoạ sĩ minh hoạ nổi tiếng', 'Popular illustrator', '人気イラストレーター', 1090, 5, 510),
                    lvl('Giám đốc nghệ thuật', 'Art director', 'アートディレクター', 1490, 0, 0)]},
        {'id': 'fine_art', 'nameVi': 'Hoạ sĩ triển lãm', 'nameEn': 'Exhibiting artist', 'nameJa': '個展作家コース',
         'descriptionVi': 'Tranh của chính mình: triển lãm cá nhân, gallery đại diện, rồi được bảo tàng sưu tập.',
         'descriptionEn': 'Your own work: solo shows, a gallery that represents you, then museum collections.',
         'descriptionJa': '自分の作品で個展、ギャラリー専属、そして美術館収蔵へ。',
         'levels': [lvl('Hoạ sĩ triển lãm cá nhân', 'Solo-show artist', '個展画家', 770, 4, 350),
                    lvl('Hoạ sĩ của gallery', 'Gallery artist', 'ギャラリー専属画家', 1070, 5, 490),
                    lvl('Hoạ sĩ được bảo tàng sưu tập', 'Museum-collected artist', '美術館収蔵画家', 1470, 0, 0)]},
    ],
    'interviews': [
        ask(2, 'あおと きいろを まぜると なにいろに なりますか。', 'Ao to kiiro o mazeru to nani-iro ni narimasu ka.', 'Trộn xanh lam với vàng ra màu gì?', 'Blue and yellow make which colour?', [
            ok('みどりです。', 'Xanh lá.', 'Green.'),
            no('むらさきです。', 'Tím.', 'Purple.', 'Tím là đỏ + xanh lam.', 'Purple is red + blue.'),
            no('オレンジです。', 'Cam.', 'Orange.', 'Cam là đỏ + vàng.', 'Orange is red + yellow.')]),
        ask(3, 'ふでを つかった あと、どうしますか。', 'Fude o tsukatta ato, dō shimasu ka.', 'Dùng cọ xong thì làm gì?', 'What do you do after using a brush?', [
            ok('すぐに あらって、けを ととのえて かわかします。', 'Rửa ngay, vuốt lông cọ rồi phơi khô.', 'Wash it at once, shape the bristles and let it dry.'),
            no('そのまま おいて おきます。', 'Để nguyên đó.', 'Leave it as it is.', 'Màu khô lại trong lông cọ làm hỏng cọ.', 'Paint drying in the bristles ruins the brush.')]),
        ask(4, 'これから どんな えを かきたいですか。', 'Kore kara donna e o kakitai desu ka.', 'Từ giờ bạn muốn vẽ tranh thế nào?', 'What would you like to paint from now on?', [
            ok('ほんや ゲームの イラストを かきたいです。', 'Tôi muốn vẽ minh hoạ cho sách và game.', 'Illustrations for books and games.', 'illustrator'),
            ok('てんらんかいで じぶんの さくひんを みせたいです。', 'Tôi muốn trưng bày tác phẩm của mình ở triển lãm.', 'My own work, in exhibitions.', 'fine_art')]),
        ask(5, 'クライアントが しめきりを はやめて ほしいと いいました。', 'Kuraianto ga shimekiri o hayamete hoshii to iimashita.', 'Khách hàng muốn rút ngắn hạn nộp.', 'The client wants the deadline moved earlier.', [
            ok('できる ひにちと ないようを はっきり そうだんします。', 'Trao đổi rõ ngày có thể giao và phạm vi công việc.', 'Agree clearly on a possible date and scope.'),
            no('なにも いわずに てぬきします。', 'Im lặng làm ẩu cho kịp.', 'Say nothing and cut corners.', 'Làm ẩu mất uy tín; thoả thuận lại mới chuyên nghiệp.', 'Cutting corners costs your name; renegotiating is professional.')], 'illustrator'),
        ask(6, 'ほかの ひとの イラストを なぞって だしても いいですか。', 'Hoka no hito no irasuto o nazotte dashite mo ii desu ka.', 'Đồ lại tranh minh hoạ của người khác rồi nộp được không?', 'May you trace someone else\'s illustration and hand it in?', [
            ok('いいえ。むだんの トレースは ちょさくけんの しんがいです。', 'Không. Đồ lại khi chưa xin phép là xâm phạm bản quyền.', 'No. Tracing without permission infringes copyright.'),
            no('すこし かえれば だいじょうぶです。', 'Sửa một chút là được.', 'Fine if you change a little.', 'Sửa chút ít vẫn là tác phẩm phái sinh — vẫn cần xin phép.', 'Small changes still make a derivative work — permission is still needed.')], 'illustrator'),
        ask(5, 'さくひんの ねだんを きめる とき、だいじなのは？', 'Sakuhin no nedan o kimeru toki, daiji na no wa?', 'Định giá tác phẩm thì cần cân nhắc gì?', 'What matters when you price a work?', [
            ok('サイズ、ざいりょう、じかん、これまでの うれた ねだんです。', 'Kích thước, vật liệu, thời gian và giá đã bán trước đây.', 'Size, materials, time and past sale prices.'),
            no('きぶんで きめます。', 'Tuỳ hứng.', 'Whatever I feel like.', 'Giá thất thường làm người sưu tập mất lòng tin.', 'Erratic prices make collectors lose trust.')], 'fine_art'),
        ask(6, 'わかい さっかに なにを アドバイスしますか。', 'Wakai sakka ni nani o adobaisu shimasu ka.', 'Bạn khuyên các hoạ sĩ trẻ điều gì?', 'What do you advise young artists?', [
            ok('まいにち スケッチを つづける ことです。', 'Mỗi ngày đều ký hoạ.', 'Sketch every day.'),
            no('うれる えだけ かけば いいです。', 'Chỉ vẽ tranh dễ bán là đủ.', 'Only paint what sells.', 'Tay nghề đến từ luyện tập đều đặn, không chỉ chạy theo thị trường.', 'Skill comes from steady practice, not only from the market.')], 'fine_art'),
    ],
    'events': [
        event('art_spill', 'ふでを あらう みずを キャンバスに こぼしました！', 'Fude o arau mizu o kyanbasu ni koboshimashita!', 'Bạn làm đổ nước rửa cọ lên toan!', 'You spilled the brush water on the canvas!', [
            act('すぐに かみで すいとって、かわいてから なおします。', 'Thấm ngay bằng giấy, khô rồi sửa.', 'Blot it at once with paper and fix it once dry.', 10, 1, 'Confident'),
            act('そのまま うえから ぬります。', 'Cứ tô đè lên.', 'Paint straight over it.', -8, 0, 'Tense')]),
        event('art_critic', 'おきゃくさんが「この いろは へんだ」と いいました。', 'Okyakusan ga "kono iro wa hen da" to iimashita.', 'Khách nói: "Màu này lạ quá."', 'A customer says: "This colour looks odd."', [
            act('ありがとうございます。どこが きに なりますか。', 'Cảm ơn ạ. Anh/chị thấy chỗ nào chưa ổn?', 'Thank you. Which part bothers you?', 8, 2, 'Happy'),
            act('おこって いいかえします。', 'Nổi cáu cãi lại.', 'Snap back angrily.', -10, -2, 'Embarrassed')]),
    ],
}


def quest(id, vi, en, ja, dvi, den, dja, objectives, rewards, cooldown, career=None, level=None):
    q = {'id': id, 'category': 'job', 'titleVi': vi, 'titleEn': en, 'titleJa': ja, 'descriptionVi': dvi, 'descriptionEn': den, 'descriptionJa': dja}
    q.update(GIVER)
    q.update(LOC)
    q.update({'scene': SCENE, 'minLevel': 1, 'minKnowledge': 0, 'requiresQuests': [], 'repeatable': True, 'cooldownMinutes': cooldown,
              'grantOnAccept': [], 'objectives': objectives, 'rewards': rewards})
    if career: q.update({'career': career, 'careerLevel': level})
    return q


def obj(id, evt, target, count, vi, en, ja, after=False):
    return {'id': id, 'event': evt, 'target': target, 'count': count, 'afterAll': after, 'textVi': vi, 'textEn': en, 'textJa': ja}


REPORT = obj('report', 'talk', OWNER, 1, 'Báo cáo chú Hibiki ở quầy để nhận công', 'Report to Mr Hibiki at the counter to get paid', '店長に報告', True)
quests = [
    quest('job_music_gig', 'Biểu diễn ở tiệm nhạc Hibiki', 'Gig at the Hibiki music shop', 'ひびき楽器店で ライブ',
          'Chú Hibiki cần người chơi nhạc buổi tối: lên sân khấu, diễn 2 bài với nhịp 80–125% bản gốc để khán giả thích, rồi báo lại.',
          'Mr Hibiki needs an evening musician: take the stage, play 2 songs at 80–125% of their tempo so the audience enjoys them, then report.',
          'ステージでテンポ80〜125%で2曲演奏して、店長に報告。',
          [obj('perform', 'perform', 'music_stage', 2, 'Biểu diễn tốt 2 bài trên sân khấu', 'Play 2 songs well on the stage', 'ステージで2曲'), REPORT],
          {'yen': 650, 'xp': 45, 'knowledge': 8}, 5),
    quest('job_dj_set', 'Chơi DJ ở tiệm Hibiki', 'DJ set at the Hibiki shop', 'ひびき楽器店で DJ',
          'DJ Sōta cần người giữ bàn: chọn đúng 4 bài nối tiếp — nhịp chênh không quá 6%, giọng liền kề trên vòng Camelot.',
          'DJ Sōta needs someone on the decks: pick 4 records in a row that mix — tempo within 6%, neighbouring keys on the Camelot wheel.',
          'BPMとキーが合う曲を4回つなげて、店長に報告。',
          [obj('mix', 'mix', 'music_dj', 4, 'Nối đúng 4 bài ở bàn DJ', 'Mix 4 records correctly at the booth', 'DJで4曲つなぐ'), REPORT],
          {'yen': 650, 'xp': 45, 'knowledge': 7}, 5),
    quest('job_art_commission', 'Vẽ tranh theo đơn', 'A painting commission', '絵の 依頼',
          'Một vị khách đặt tranh chép theo một kiệt tác: tô xong một bức ở giá vẽ rồi giao cho chú Hibiki.',
          'A customer orders a copy of a masterpiece: finish one painting at the easel and deliver it to Mr Hibiki.',
          'イーゼルで1枚かいて、店長に納品。',
          [obj('paint', 'paint', 'art_easel', 1, 'Vẽ xong 1 bức tranh ở giá vẽ', 'Finish 1 painting at the easel', 'イーゼルで1枚'), REPORT],
          {'yen': 700, 'xp': 45, 'knowledge': 8}, 5),
    quest('career_musician_recital', 'Đêm độc tấu', 'A recital night', 'リサイタルの夜',
          'Bậc 3 nghề Nhạc công: một đêm diễn riêng — 4 bài tốt liên tiếp.', 'Musician career level 3: your own night — 4 good songs.', 'ミュージシャン3：4曲のリサイタル。',
          [obj('perform', 'perform', 'music_stage', 4, 'Biểu diễn tốt 4 bài', 'Play 4 songs well', '4曲演奏'), REPORT],
          {'yen': 1200, 'xp': 80, 'knowledge': 14}, 30, 'musician', 3),
    quest('career_producer_party', 'Tiệc cuối tuần', 'The weekend party', '週末パーティー',
          'Bậc 2 nghề DJ: giữ sàn cả buổi tiệc — nối đúng 8 bài.', 'DJ career level 2: hold the party — mix 8 records.', 'DJ2：8曲つなぐ。',
          [obj('mix', 'mix', 'music_dj', 8, 'Nối đúng 8 bài', 'Mix 8 records', '8曲つなぐ'), REPORT],
          {'yen': 1100, 'xp': 70, 'knowledge': 12}, 30, 'producer', 2),
    quest('career_painter_exhibit', 'Chuẩn bị triển lãm nhỏ', 'A small exhibition', '小さな展示',
          'Bậc 4 nghề Hoạ sĩ: vẽ 3 bức cho góc triển lãm của tiệm.', 'Painter career level 4: paint 3 works for the shop\'s exhibition corner.', '画家4：3枚かいて展示。',
          [obj('paint', 'paint', 'art_easel', 3, 'Vẽ xong 3 bức', 'Finish 3 paintings', '3枚かく'), REPORT],
          {'yen': 1900, 'xp': 110, 'knowledge': 20}, 60, 'painter', 4),
]


def load(name):
    with open(os.path.join(ROOT, name), encoding='utf-8') as f:
        return json.load(f)


def save(name, data):
    with open(os.path.join(ROOT, name), 'w', encoding='utf-8', newline='\n') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')


careers = load('careers.json')
new = [musician, producer, painter]
careers['careers'] = [c for c in careers['careers'] if c['id'] not in {c['id'] for c in new}] + new
save('careers.json', careers)

progression = load('progression.json')
ids = {q['id'] for q in quests}
progression['quests'] = [q for q in progression['quests'] if q['id'] not in ids] + quests
save('progression.json', progression)
print(f"careers: {[c['id'] for c in careers['careers']]}; quests +{len(quests)}")
