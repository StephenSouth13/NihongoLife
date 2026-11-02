# Story — Nihongo Life

> Generated from `Assets/NihongoLife/Resources/Story/storyline.json` by `Tools/story/gen.py`; edit the content there, rerun, then run the EditMode tests `StorylineTests`.

The world and cast are defined in `STORY_BIBLE.md` (Hibari-chō, the N5 scenarios). This file is the long story that runs on top of it: a **main storyline** of 9 chapters and **10 career arcs**, each with five stages from trainee to owner/specialist. Every chapter and stage is a real quest in `progression.json` (category `story`), so the Task Journal (N), quest tracking, rewards and the quest HUD handle it with no extra system.

## Unlock rules

- **Language level** comes from Trí tuệ (knowledge): JLPT N5 ≥ 0, JLPT N4 ≥ 150, JLPT N3 ≥ 400, JLPT N2 ≥ 800, JLPT N1 ≥ 1400. A quest's `minKnowledge` is the threshold of its JLPT level; the journal shows what is missing.
- **Main chapters** unlock in order (`requiresQuests`) and at a player level (`minLevel`).
- **Career arcs** open after chapter 5 (Choosing a career). Each stage needs the career (`career`) at its career level (`careerLevel`, from careers.json), its JLPT level and the stage before it:

| Stage | Career level | JLPT | Knowledge |
|---|---|---|---|
| Trainee (Thực tập) | 1 | N5 | 0 |
| Staff (Nhân viên) | 2 | N4 | 150 |
| Senior (Nhân viên chính) | 3 | N3 | 400 |
| Manager (Quản lý) | 5 | N2 | 800 |
| Owner / specialist (Chủ / chuyên gia) | 6 | N1 | 1400 |

Career levels 4–6 come from the branch chosen at the level-4 interview (careers.json `branches`), so the manager and owner/specialist stages apply to either branch (e.g. Medicine: resident → specialty trainee → specialist and clinic director, or the pharmacist branch).

## Main storyline

### 1. A new life in Hibari-chō — ひばり町での新生活 (`story_ch01_arrival`, JLPT N5, level 1)

You arrive as an exchange student. Meet your neighbour Tanaka on Sakura-dōri.

- **Tanaka**: ようこそ、ひばり町へ！となりの田中です。 — *Welcome to Hibari-chō! I'm Tanaka, from next door.*
- **Tanaka**: わからないことがあったら、いつでも聞いてね。 — *If anything is unclear, just ask me any time.*
- **Tanaka**: まずは学校と駅の場所を覚えましょう。 — *First, let's learn the way to the school and the station.*

*Choice* — What would you like to do first in Hibari-chō?
  - I want to study Japanese properly. → Good for you. Ms Morita is strict but kind. (`story_choice_arrival_study`, affinity +5)
  - I want to make friends with the people here. → Then let's start by eating something good on the shopping street! (`story_choice_arrival_friends`, affinity +8)
  - I want to find a part-time job. → Ms Ito at the konbini is looking for help. (`story_choice_arrival_work`, affinity +4)
*Ending*
  - **Narrator**: Your days in Hibari-chō begin here.

### 2. First day at Hibari School — ひばり学院の初日 (`story_ch02_first_class`, JLPT N5, level 1)

Introduce yourself in Sakura class and finish three lessons.

- **Ms Morita**: はじめまして。担任の森田です。 — *Nice to meet you. I'm Morita, your homeroom teacher.*
- **Ms Morita**: まちがえても大丈夫。話すことがいちばん大切です。 — *Mistakes are fine. Speaking is what matters most.*
- **Ms Morita**: キムです！いっしょにがんばりましょう！ — *I'm Kim! Let's do our best together!*

*Choice* — How will you introduce yourself?
  - Nice to meet you all. I look forward to working with you. → Very polite, well done. (`story_choice_first_class_polite`, affinity +6)
  - I like cooking. I want to cook Vietnamese food for everyone. → Kim likes cooking too. Talk to her later. (`story_choice_first_class_hobby`, affinity +5)
  - Sorry, my Japanese is still poor… → It's fine. Everyone is the same. Let's do our best together. (`story_choice_first_class_nervous`, affinity +8)
*Ending*
  - **Narrator**: Kim is waving at you.
  - **Kim**: Tell me about your cooking later! _(if `story_choice_first_class_hobby`)_

### 3. Neighbours and sushi — ご近所とお寿司 (`story_ch03_neighbours`, JLPT N5, level 3)

Tanaka takes you to Sushi Hibari. Order on your own in Japanese.

- **Aoki**: いらっしゃいませ！田中さんのお友だちですね。 — *Welcome! You're Tanaka's friend, aren't you?*
- **Aoki**: ご注文がお決まりでしたら、どうぞ。 — *Whenever you're ready to order, go ahead.*
- **Aoki**: また来てくださいね。次はおすすめを出しますよ。 — *Please come again. Next time I'll bring you our special.*

*Choice* — Aoki asks what you would like.
  - Omakase, please. → Brave! Today's tuna is the best. (`story_choice_neighbours_omakase`, affinity +8)
  - What fish is this? → That's horse mackerel. I like people who ask. (`story_choice_neighbours_ask`, affinity +6)
  - I'd like to eat with Mr Tanaka. → Nice. Neighbours matter. (`story_choice_neighbours_share`, affinity +5)
*Ending*
  - **Narrator**: Behind the noren, the neighbourhood is laughing.

### 4. The summer festival — 夏祭り (`story_ch04_festival`, JLPT N5, level 6)

Mr Sato invites you to help at the shrine festival. The whole street now knows your face.

- **Mr Sato**: 今年の夏祭り、手伝ってくれませんか。 — *Would you help with this year's summer festival?*
- **Mr Sato**: みなさん、この人はもう町の仲間です。 — *Everyone, this person is one of our neighbourhood now.*
- **Mr Sato**: ありがとう。ここはあなたの町ですよ。 — *Thank you. This is your town too.*

*Choice* — How will you help at the festival?
  - I'll dance the Bon Odori! → Great, join the circle! (`story_choice_festival_dance`, affinity +6)
  - I'll help at a stall. → The takoyaki stall is swamped. That helps. (`story_choice_festival_stall`, affinity +8)
  - I'll clean up afterwards. → That's what we value most. People notice. (`story_choice_festival_clean`, affinity +10)
*Ending*
  - **Narrator**: Fireworks open across the night sky.
  - **Kim**: Beautiful. Let's watch together next year too. _(if `story_bond_npc_classmate_kim`)_

### 5. Choosing a career — 仕事を選ぶ (`story_ch05_career_choice`, JLPT N4, level 10)

With your visa changed to work, pick a career in the Career app and start as a trainee.

- **Tanaka**: ビザ、おめでとう！これからは働けるね。 — *Congratulations on the visa! You can work now.*
- **Tanaka**: 店、病院、農場、フーコックのレストラン……何がしたい？ — *A shop, the clinic, the farm, the restaurant in Phú Quốc… what do you want to do?*
- **Tanaka**: どの仕事も、日本語が上手になるほど上に行けるよ。 — *In any job, the better your Japanese, the higher you go.*

*Choice* — What kind of work do you want to live by?
  - I want work that helps people. → The clinic or a shop would suit you. (`story_choice_career_choice_people`, affinity +6)
  - I want to cook, make music, create things. → The craftsman's road is hard but fun. (`story_choice_career_choice_craft`, affinity +6)
  - I want work that links Japan and Vietnam. → Then you should meet Mai on Phú Quốc. (`story_choice_career_choice_bridge`, affinity +8)
*Ending*
  - **Tanaka**: You can pick your path under Careers.

### 6. A bridge to Phú Quốc — フーコックへの架け橋 (`story_ch06_phu_quoc`, JLPT N4, level 10)

Mai at Gánh Ngon needs a Japanese speaker for her guests from Japan. Visit the island.

- **Mai**: 日本からのお客さまが増えているんです。 — *More and more guests are coming from Japan.*
- **Mai**: あなたの日本語で、お店を手伝ってくれませんか。 — *Could you help the restaurant with your Japanese?*
- **Mai**: ようこそフーコックへ。海もごはんも最高ですよ！ — *Welcome to Phú Quốc. The sea and the food are the best!*

*Choice* — What will you do first at Gánh Ngon?
  - I'll translate the menu into Japanese. → Thank you! Japanese guests will love it. (`story_choice_phu_quoc_menu`, affinity +10)
  - I'll help in the kitchen. → Then let's start with filleting fish. (`story_choice_phu_quoc_kitchen`, affinity +6)
  - I'll look around the island first. → Be back by sunset. (`story_choice_phu_quoc_beach`, affinity +3)
*Ending*
  - **Narrator**: In the sea breeze, a new workplace awaits.
  - **Mai**: The Japanese menu is already a hit. _(if `story_choice_phu_quoc_menu`)_

### 7. Exam season: N3 — 試験シーズン：N3 (`story_ch07_n3_exam`, JLPT N3, level 14)

Morita-sensei prepares the class for the JLPT N3. Ten lessons or practice tests.

- **Ms Morita**: N3からは、新聞の見出しも読めるようになりますよ。 — *From N3 on you'll be able to read newspaper headlines.*
- **Ms Morita**: 仕事で使う日本語も、試験で練習しましょう。 — *Let's practise the Japanese you use at work in the exam too.*
- **Ms Morita**: キムさんと一緒に、毎日少しずつ。 — *A little every day, together with Kim.*

*Choice* — What will you do the day before the exam?
  - Review grammar once more. → Diligent. But do go to bed early. (`story_choice_n3_exam_review`, affinity +5)
  - Hold a study group with Kim and the others. → Teaching each other is the best study. (`story_choice_n3_exam_group`, affinity +7)
  - Rest well. → That's important preparation too. (`story_choice_n3_exam_rest`, affinity +4)
*Ending*
  - **Narrator**: On results day, you find your name.
  - **Narrator**: Kim passed too! Everyone celebrates. _(if `story_choice_n3_exam_group`)_

### 8. From student to mentor — 学生から先輩へ (`story_ch08_mentor`, JLPT N2, level 18)

New students arrive in Sakura class. Morita-sensei asks you to welcome them as Tanaka once welcomed you.

- **Ms Morita**: 今度はあなたが先輩ですね。 — *This time you're the senior student.*
- **Ms Morita**: 新しい学生に、町を案内してあげてください。 — *Please show the new students around town.*
- **Ms Morita**: あなたを見ていると、教師になってよかったと思います。 — *Watching you, I'm glad I became a teacher.*

*Choice* — What do you want to tell the new students?
  - That mistakes are fine. → Those are the words I said to you on day one. (`story_choice_mentor_mistakes`, affinity +10)
  - To talk with the townspeople. → Hibari-chō raised you. (`story_choice_mentor_town`, affinity +6)
  - That work makes your Japanese grow. → Words from experience are strong. (`story_choice_mentor_work`, affinity +6)
*Ending*
  - **Narrator**: New voices ring through the classroom.

### 9. Home in two countries — 二つのふるさと (`story_ch09_homecoming`, JLPT N1, level 22)

At the next summer festival Tanaka gives the speech — about you. Your career, your Japanese, your friends in Hibari and Phú Quốc.

- **Tanaka**: 最初は道もわからなかったのにね。 — *At first you didn't even know the way.*
- **Tanaka**: 今では日本とベトナムをつなぐ人だ。 — *Now you're someone who connects Japan and Vietnam.*
- **Tanaka**: おかえり。ここも、あなたのふるさとだよ。 — *Welcome home. This is your hometown too.*

*Choice* — Where will you live from now on?
  - I'll stay in Hibari-chō. → I'm glad. We stay neighbours. (`story_choice_homecoming_japan`, affinity +10)
  - I'll go home to Vietnam and be a bridge to Japan. → I'll miss you, but I'm proud. (`story_choice_homecoming_vietnam`, affinity +8)
  - I'll live between both countries. → Two hometowns. That's so you. (`story_choice_homecoming_both`, affinity +8)
*Ending*
  - **Narrator**: A letter from Ms Morita: you are the student I'm proudest of.
  - **Kim**: Kim: Let's cook together again. _(if `story_bond_npc_classmate_kim`)_
  - **Mai**: Mai: Gánh Ngon is always your home. _(if `story_bond_npc_ganhngon_manager`)_
  - **Aoki**: Aoki: The noren is always open for you. _(if `story_bond_npc_sushi_staff`)_
  - **Narrator**: The story of Hibari-chō does not end. A new day begins.

## Career arcs

### Restaurant & service (Phú Quốc) — レストラン・サービス（フーコック） (Gánh Ngon Restaurant, Phú Quốc, boss: Mai)

**trainee** · career level 1 · JLPT N5 · `story_hospitality_trainee` — Greet guests and serve your first tables.

- いらっしゃいませ、は笑顔で言ってね。 — *Say irasshaimase with a smile.*
- お水とおしぼりを先に出します。 — *Water and a cold towel come first.*
- よくできました。明日も来てね。 — *Well done. Come again tomorrow.*

**staff** · career level 2 · JLPT N4 · `story_hospitality_staff` — Take orders from a Japanese tour group and explain the dishes.

- 今日は日本の団体さんが十二名です。 — *Today we have a Japanese group of twelve.*
- パクチーが苦手な方がいるか、聞いてください。 — *Ask whether anyone dislikes coriander.*
- ガイドさんがあなたをほめていましたよ。 — *The tour guide praised you.*

**senior** · career level 3 · JLPT N3 · `story_hospitality_senior` — Lead the evening shift and handle a complaint politely.

- 今夜はあなたがシフトリーダーよ。 — *Tonight you're the shift lead.*
- お客さまが「遅い」と言ったら、まず「申し訳ございません」。 — *If a guest says it's slow, start with mōshiwake gozaimasen.*
- 新人の子たちにも教えてあげてね。 — *Teach the new staff too.*

**manager** · career level 5 · JLPT N2 · `story_hospitality_manager` — Plan the rota, the menu and a partnership with a Japanese travel agency.

- このお店の鍵、あなたに預けます。 — *I'm trusting you with the keys to this place.*
- 旅行会社とのメール、敬語で書けますか。 — *Can you write the travel agency email in keigo?*
- 売り上げ、先月より二割も上がったわ！ — *Sales are up twenty percent on last month!*

**owner** · career level 6 · JLPT N1 · `story_hospitality_owner` — Open a second Gánh Ngon by the beach, a bridge between Vietnamese food and Japanese guests.

- 二号店のオーナーは、あなたしかいない。 — *There's no one but you to own the second restaurant.*
- 日本の雑誌にも紹介されるそうよ。 — *I hear a Japanese magazine will feature it.*
- 最初の日のエプロン、まだ持ってる？ — *Do you still have the apron from your first day?*

### Medicine — 医療 (Hibari Clinic, boss: Dr Mori)

**trainee** · career level 1 · JLPT N5 · `story_medical_trainee` — Check patients in and ask for their insurance card.

- 保険証をお願いします、から始めましょう。 — *Start with: your insurance card, please.*
- 問診票は、ゆっくり説明してあげてください。 — *Explain the questionnaire slowly.*
- 受付は病院の顔ですよ。 — *Reception is the face of the clinic.*

**staff** · career level 2 · JLPT N4 · `story_medical_staff` — Take temperatures and blood pressure and ask where it hurts.

- どこが痛いですか、と聞いてから測ります。 — *Ask where it hurts, then measure.*
- 外国の患者さんには、あなたの通訳が助かります。 — *Your interpreting helps our foreign patients a lot.*
- 数字は二回確認してね。 — *Check the numbers twice.*

**senior** · career level 3 · JLPT N3 · `story_medical_senior` — Pass the CBT practice and present a case to Dr Mori.

- 医学部の勉強は六年。焦らずにいきましょう。 — *Medical school is six years. Don't rush.*
- 症例発表は、主訴から順番に。 — *Present the case in order, starting with the chief complaint.*
- いい質問ですね。それが医者の目です。 — *Good question. That's a doctor's eye.*

**manager** · career level 5 · JLPT N2 · `story_medical_manager` — After the national exam and residency, lead the morning clinic as a specialty trainee.

- 国家試験合格、おめでとう。 — *Congratulations on passing the national exam.*
- 今日から、午前の外来をお願いします。 — *From today the morning clinic is yours.*
- 説明は、患者さんの言葉で。 — *Explain things in the patient's words.*

**owner** · career level 6 · JLPT N1 · `story_medical_owner` — Become a specialist and take over Hibari Clinic, with care in Japanese, Vietnamese and English.

- このクリニックを、あなたに任せたい。 — *I want to leave this clinic to you.*
- 三つの言葉で診られる院長は、町の宝です。 — *A director who treats in three languages is a treasure for the town.*
- 受付から始めた日を、忘れないでね。 — *Don't forget the day you started at reception.*

### Retail & service — 小売・サービス (Hibari Mart, main street, boss: Ms Ito)

**trainee** · career level 1 · JLPT N5 · `story_retail_trainee` — Stock shelves and ring up customers.

- レジ袋はご利用ですか、と聞いてね。 — *Ask whether they'd like a bag.*
- 温めますか、も忘れずに。 — *Don't forget: shall I heat it up?*
- 初日なのに上手！ — *Great for a first day!*

**staff** · career level 2 · JLPT N4 · `story_retail_staff` — Remember the regulars and handle deliveries.

- 佐藤さんはいつも朝の新聞よ。 — *Mr Sato always buys the morning paper.*
- 宅配便の伝票、書き方を教えるね。 — *Let me show you the delivery slip.*
- お客さまの名前を覚えると、店が温かくなる。 — *Knowing names makes a shop warm.*

**senior** · career level 3 · JLPT N3 · `story_retail_senior` — Train a new clerk and close the register without errors.

- 新人さんに、あいさつから教えてあげて。 — *Teach the newcomer, starting with greetings.*
- レジ締め、一円も合わせてね。 — *Close the register to the last yen.*
- あなたがいると安心。 — *With you here I can relax.*

**manager** · career level 5 · JLPT N2 · `story_retail_manager` — Plan orders for the festival week and manage the staff.

- 祭りの週は、おにぎりが三倍売れるの。 — *Festival week sells three times the onigiri.*
- 発注は、天気予報も見て決めて。 — *Check the weather forecast when ordering.*
- 店長代理、よろしくね。 — *I'm counting on you, deputy manager.*

**owner** · career level 6 · JLPT N1 · `story_retail_owner` — Open a Hibari Mart that also sells Vietnamese goods, or become the chain's buyer.

- 新しい店、あなたにお願いしたいの。 — *I'd like you to take the new store.*
- フォーやバインミーの棚、いいアイデアね。 — *A shelf for phở and bánh mì — nice idea.*
- 最初のレジ、覚えてる？ — *Remember your first register?*

### Food & restaurant — 飲食 (Sushi Hibari, boss: Aoki)

**trainee** · career level 1 · JLPT N5 · `story_food_trainee` — Keep the dishes moving and learn the restaurant greetings.

- 洗い場が止まると、店が止まる。 — *If the sink stops, the restaurant stops.*
- 「いらっしゃいませ」は大きな声で！ — *Shout irasshaimase!*
- よし、明日はホールだ。 — *Good, tomorrow you're on the floor.*

**staff** · career level 2 · JLPT N4 · `story_food_staff` — Take orders and repeat them back correctly.

- ご注文を繰り返します、が基本だ。 — *Repeating the order is the basics.*
- わさび抜き、聞き逃すなよ。 — *Don't miss 'no wasabi'.*
- お客さんが笑ってたぞ。 — *The customers were smiling.*

**senior** · career level 3 · JLPT N3 · `story_food_senior` — Run the lunch rush and seat a group of eight.

- 八名様、カウンターは無理だな。 — *Eight guests won't fit at the counter.*
- 座敷にご案内して。 — *Show them to the tatami room.*
- ホールはお前に任せた。 — *The floor is yours.*

**manager** · career level 5 · JLPT N2 · `story_food_manager` — Run the restaurant's books or stand behind the counter.

- 数字を見るのも、魚を見るのも同じ目だ。 — *Reading numbers and reading fish take the same eye.*
- 仕入れの電話、敬語で頼む。 — *Use keigo when you call the suppliers.*
- いい顔になってきたな。 — *You're starting to look the part.*

**owner** · career level 6 · JLPT N1 · `story_food_owner` — Take over the restaurant, or open your own counter.

- のれん、お前に譲る。 — *I'm handing the noren to you.*
- 洗い場から始めた奴は、強い。 — *Those who start at the sink are strong.*
- ベトナムの魚も、握ってみろ。 — *Try making sushi with Vietnamese fish too.*

### Chef — 料理人 (Sushi Hibari's kitchen, boss: Ota)

**trainee** · career level 1 · JLPT N5 · `story_chef_trainee` — Wash the rice and learn knife safety.

- 米は三年、と言われる。 — *They say rice takes three years.*
- 包丁は、使ったらすぐ洗う。 — *Wash the knife right after use.*
- 手を見せろ。……よし。 — *Show me your hands… good.*

**staff** · career level 2 · JLPT N4 · `story_chef_staff` — Prepare fish and garnish for service.

- 仕込みで、味の半分が決まる。 — *Prep decides half the flavour.*
- 大根のつま、細く。 — *Cut the daikon thin.*
- 悪くない。 — *Not bad.*

**senior** · career level 3 · JLPT N3 · `story_chef_senior` — Pass the cook's licence and cook the staff meal.

- 免許、おめでとう。 — *Congratulations on your licence.*
- まかないで、皆を笑わせろ。 — *Make everyone smile with the staff meal.*
- このフォー、店で出してもいい。 — *This phở could go on the menu.*

**manager** · career level 5 · JLPT N2 · `story_chef_manager` — Run the kitchen when Ota is away and create a seasonal dish.

- 今日は俺がいない。厨房はお前だ。 — *I'm away today. The kitchen is yours.*
- 季節の一品、考えておけ。 — *Think up a seasonal dish.*
- ……うまい。 — *…Delicious.*

**owner** · career level 6 · JLPT N1 · `story_chef_owner` — Lead the kitchen: sushi or pastry, your name on the menu.

- 料理長、と呼ばれる気分はどうだ。 — *How does 'head chef' feel?*
- お前の味は、二つの国の味だ。 — *Your flavour is the flavour of two countries.*
- 米を研いだ日を忘れるな。 — *Don't forget the day you washed rice.*

### Farming — 農業 (Midori Island farm, boss: Hana)

**trainee** · career level 1 · JLPT N5 · `story_farm_trainee` — Till, plant and water with Hana.

- 土は生きているのよ。 — *The soil is alive.*
- 水は朝のうちにね。 — *Water in the morning.*
- いい手つき！ — *Nice hands!*

**staff** · career level 2 · JLPT N4 · `story_farm_staff` — Bring in the harvest and sort it for market.

- 大きさで三つに分けて。 — *Sort into three sizes.*
- 傷のあるのは、ジャム用。 — *Bruised ones are for jam.*
- 市場の人も喜ぶわ。 — *The market will be pleased.*

**senior** · career level 3 · JLPT N3 · `story_farm_senior` — Plan a season of crops and sell at the market.

- 何を植えるか、あなたが決めて。 — *You decide what to plant.*
- 値段の交渉も、日本語でね。 — *Negotiate prices in Japanese too.*
- もう立派な農家ね。 — *You're a real farmer now.*

**manager** · career level 5 · JLPT N2 · `story_farm_manager` — Lead the helpers and supply Sushi Hibari and Gánh Ngon.

- ひばり寿司が、うちの大葉をほしいって。 — *Sushi Hibari wants our shiso.*
- フーコックにはハーブを送りましょう。 — *Let's send herbs to Phú Quốc.*
- みんな、あなたの指示を待ってるわ。 — *Everyone is waiting for your call.*

**owner** · career level 6 · JLPT N1 · `story_farm_owner` — Run your own organic farm or ranch on Midori Island.

- この畑、あなたに譲るわ。 — *I'm giving this field to you.*
- ベトナムの野菜も育ててみて。 — *Try growing Vietnamese vegetables too.*
- 最初の畝、まだ覚えてる？ — *Remember your first furrow?*

### Musician — ミュージシャン (The Hibiki music shop stage, boss: Hibiki)

**trainee** · career level 1 · JLPT N5 · `story_musician_trainee` — Play your first song on the café stage.

- 緊張しても大丈夫。 — *It's fine to be nervous.*
- お客さんの顔を見て弾いて。 — *Play looking at the audience.*
- 拍手、聞こえた？ — *Did you hear the applause?*

**staff** · career level 2 · JLPT N4 · `story_musician_staff` — Play weekly sets and talk with the audience in Japanese.

- 曲の間に、少し話してみて。 — *Say a few words between songs.*
- ベトナムの歌も聞きたいな。 — *I'd love to hear a Vietnamese song.*
- ファンができたね。 — *You've got fans.*

**senior** · career level 3 · JLPT N3 · `story_musician_senior` — Record for other artists and read charts fast.

- スタジオは時間との勝負だ。 — *The studio is a race against time.*
- 一回で決めよう。 — *Let's nail it in one take.*
- プロの音になった。 — *That's a pro sound.*

**manager** · career level 5 · JLPT N2 · `story_musician_manager` — Lead a recital or a tour show.

- ホールが満席だ！ — *The hall is sold out!*
- インタビューも日本語でね。 — *The interview's in Japanese too.*
- 胸を張って。 — *Chin up.*

**owner** · career level 6 · JLPT N1 · `story_musician_owner` — Headline the summer festival in Hibari and Phú Quốc.

- トリはお前だ。 — *You're closing the show.*
- 二つの町が歌ってる。 — *Two towns are singing.*
- 最初のカフェ、忘れるなよ。 — *Don't forget the first café.*

### DJ & producer — DJ・プロデューサー (The Hibiki DJ booth, boss: Hibiki)

**trainee** · career level 1 · JLPT N5 · `story_producer_trainee` — Set up the booth and play the warm-up.

- ケーブルは色で覚えろ。 — *Learn the cables by colour.*
- 最初は小さい音から。 — *Start quiet.*
- フロアが揺れてる。 — *The floor is moving.*

**staff** · career level 2 · JLPT N4 · `story_producer_staff` — Open the night and read the crowd.

- 客の空気を読め。 — *Read the room.*
- 日本語でマイクも入れて。 — *Get on the mic in Japanese.*
- いい流れだ。 — *Nice flow.*

**senior** · career level 3 · JLPT N3 · `story_producer_senior` — Hold a residency and make your first track.

- 毎週金曜はお前の夜だ。 — *Every Friday is your night.*
- 自分の曲、作ってみろ。 — *Try making your own track.*
- いいビートだ。 — *Good beat.*

**manager** · career level 5 · JLPT N2 · `story_producer_manager` — Headline a club night or produce for an artist.

- 契約書、ちゃんと読めるか。 — *Can you read the contract properly?*
- 敬語で交渉しろ。 — *Negotiate in keigo.*
- 名前が売れてきたな。 — *Your name is getting known.*

**owner** · career level 6 · JLPT N1 · `story_producer_owner` — Play the beach festival in Phú Quốc or start your own label.

- ビーチのステージ、決まったぞ。 — *The beach stage is booked.*
- レーベルの名前は？ — *What's the label called?*
- ケーブル係だったのにな。 — *You used to carry the cables.*

### Painter — 画家 (The Hibiki shop easel, boss: Hibiki)

**trainee** · career level 1 · JLPT N5 · `story_painter_trainee` — Prepare canvases and finish your first painting.

- 筆はていねいに洗ってね。 — *Wash the brushes carefully.*
- 見たものを、そのまま描いて。 — *Paint what you see.*
- 色がいいね。 — *Nice colours.*

**staff** · career level 2 · JLPT N4 · `story_painter_staff` — Paint for customers and discuss what they want in Japanese.

- お客さまの話を、最後まで聞いて。 — *Hear the customer out.*
- 締め切りは守ること。 — *Meet the deadline.*
- 喜んでたよ。 — *They were delighted.*

**senior** · career level 3 · JLPT N3 · `story_painter_senior` — Finish a series and price your work.

- シリーズで描くと、物語になる。 — *A series becomes a story.*
- 値段は自信の数字だ。 — *A price is a number for confidence.*
- プロの絵だ。 — *That's professional work.*

**manager** · career level 5 · JLPT N2 · `story_painter_manager` — Hold a solo show or art-direct a project.

- 個展のあいさつ、考えた？ — *Have you written the opening speech?*
- フーコックの海の絵、人気だよ。 — *Your Phú Quốc sea paintings are popular.*
- 堂々としてね。 — *Stand tall.*

**owner** · career level 6 · JLPT N1 · `story_painter_owner` — A museum collects your painting of two hometowns.

- 美術館から電話だ。 — *The museum called.*
- ひばりとフーコック、両方が入ってる。 — *Both Hibari and Phú Quốc are in it.*
- 最初の筆、まだある？ — *Still have your first brush?*

### Fashion — ファッション (Hibari Mode, main street, boss: Ren)

**trainee** · career level 1 · JLPT N5 · `story_fashion_trainee` — Fold the tables and learn the sizes and colours in Japanese.

- たたみ方は、お店の顔だよ。 — *How you fold is the face of the shop.*
- Sサイズ、Mサイズ、言ってみて。 — *Say S size, M size for me.*
- いいね、きれいにそろった。 — *Nice, nice and even.*

**staff** · career level 2 · JLPT N4 · `story_fashion_staff` — Greet customers and look after the fitting rooms.

- 声をかけるタイミングが大事だよ。 — *When you speak to them matters.*
- 試着室はこちらです、って言ってみて。 — *Try saying: the fitting room is this way.*
- お客さま、笑ってたね。 — *The customer smiled.*

**senior** · career level 3 · JLPT N3 · `story_fashion_senior` — Build a whole outfit for a customer and explain your choice.

- 服だけじゃなく、その人を見て。 — *Look at the person, not only the clothes.*
- 色の理由を、日本語で言えるかな。 — *Can you give the reason for a colour in Japanese?*
- いい提案だった。 — *That was a good suggestion.*

**manager** · career level 5 · JLPT N2 · `story_fashion_manager` — Dress a magazine shoot or show your first sketches.

- 撮影は明日の朝だ。 — *The shoot is tomorrow morning.*
- 敬語で打ち合わせしよう。 — *Let us hold the meeting in keigo.*
- ひばりの春、という感じだ。 — *That is spring in Hibari.*

**owner** · career level 6 · JLPT N1 · `story_fashion_owner` — Show a collection that joins Hibari and Phú Quốc.

- ショーのあいさつ、考えた？ — *Have you written the words for the show?*
- 二つの町の色が入ってるね。 — *The colours of both towns are in it.*
- たたみ方から始まったのにな。 — *And it all began with folding.*

## Cutscenes, choices and relationships

When a story quest is completed, `StoryScenes` plays its scene in the normal dialogue window: the quest's lines, the boss's question, the player's answer (a story flag + affinity with that person, `SocialStanding`), the reply and the ending. Ending lines with a flag show only when that flag is set, so earlier answers come back later (e.g. the owner stage recalls the trainee answer, the last chapter thanks the people you bonded with). Affinity of 20 or more with someone sets `story_bond_<speaker>`. Career answers are remembered per tier (`story_<career>_<tier>_<answer>`):

- **trainee** — How was your first week?: I took notes so I wouldn't make mistakes. (`careful`); I enjoyed talking with customers. (`cheerful`); Honestly, I'm tired. (`tired`)
- **staff** — A customer complains. What do you do?: Apologise sincerely first. (`apologize`); Listen to the end. (`listen`); Call a senior right away. (`boss`)
- **senior** — The new trainee is close to tears.: Go over the steps together. (`teach`); Tell them about my own mistakes. (`story`); I'll cover for them today. (`cover`)
- **manager** — Sales are down. What will you change?: Ask the staff for ideas. (`team`); Create a new menu or service. (`new`); Go and ask the regulars. (`guests`)
- **owner** — What do you want to leave behind through this work?: I want to raise the next people. (`people`); I want to keep doing the very best work. (`quality`); I want to link Japan and Vietnam. (`bridge`)

Scenes are not played while `StoryDirector.Bypass` is on (tests). A scene plays once (`story_scene_<quest>`); the journal shows the lines at any time.

## Languages

Every line carries Japanese, Vietnamese, English and Russian (`ja/vi/en/ru`). Quest titles, descriptions and objectives are in progression.json (vi/en/ja) with Russian in `ru_table_extra.json`.
