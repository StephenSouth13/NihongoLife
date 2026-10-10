# -*- coding: utf-8 -*-
"""Generates Resources/Exams/jlpt_n5_mock_2.asset — a full-length JLPT N5 practice test written for NihongoLife
(original questions, not copied from any real JLPT paper), in the current official N5 layout:

  言語知識（文字・語彙） 20 min  もんだい1 漢字読み 7 · 2 表記 5 · 3 文脈規定 6 · 4 言い換え類義 3
  言語知識（文法）・読解 40 min  もんだい1 文法形式 9 · 2 文の組み立て 4 · 3 文章の文法 4 · 4 短文 2 · 5 中文 2 · 6 情報検索 1
  聴解                   30 min  もんだい1 課題理解 7 · 2 ポイント理解 6 · 3 発話表現 5 · 4 即時応答 6

Scoring divisions (得点区分) as for N5: 言語知識（文字・語彙・文法）・読解 0–120 (min 38) + 聴解 0–60 (min 19),
pass 80/180. The listening audio is synthesised with Microsoft Edge neural voices (ja-JP-NanamiNeural narrator/woman,
ja-JP-KeitaNeural man) — the section title says so. Audio files: Assets/NihongoLife/Audio/JLPT/n5_mock2/*.mp3 (+ .meta
with a stable GUID so the asset can reference them without an Editor step).

    python Tools/exam/gen_jlpt_n5_mock2.py          # writes audio (missing files only) + the asset
"""
import hashlib
import os
import random
import subprocess
import sys
import tempfile
import time

sys.path.insert(0, os.path.dirname(__file__))
from exam_lib import Exam, Section, Passage, Question, Choice

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "NihongoLife", "Resources", "Exams", "jlpt_n5_mock_2.asset")
AUDIO_DIR = os.path.join(ROOT, "Assets", "NihongoLife", "Audio", "JLPT", "n5_mock2")


def opts(*texts):
    return [Choice(t, t, t) for t in texts]


def shuffled(qid, choices, correct):
    """Stable per-question order so the right answer is not always in the same place."""
    order = list(range(len(choices)))
    random.Random(qid).shuffle(order)
    return [choices[i] for i in order], order.index(correct)


def mcq(qid, mondai, instr_ja, instr_vi, sentence, choices, correct, why_vi, passage_id="", tags=()):
    choices, correct = shuffled(qid, choices, correct)
    return Question(
        id=qid, type="mc", passage_id=passage_id, mondai=mondai,
        prompt_ja=f"{instr_ja}\n{sentence}", prompt_vi=f"{instr_vi}\n{sentence}", prompt_en=f"{instr_ja}\n{sentence}",
        choices=opts(*choices), correct=correct,
        explanation_vi=f"Đáp án: {choices[correct]}. {why_vi}", explanation_en=f"Answer: {choices[correct]}.",
        tags=["n5", mondai] + list(tags),
    )


# ═══════════════════════ 言語知識（文字・語彙） ═══════════════════════

M1 = ("【　】の ことばは ひらがなで どう かきますか。", "Từ trong 【 】 đọc (viết bằng hiragana) thế nào?")
M2 = ("【　】の ことばは どう かきますか。", "Từ trong 【 】 viết bằng chữ nào?")
M3 = ("（　）に なにを いれますか。", "Điền từ thích hợp vào （ ）.")
M4 = ("【　】の ぶんと だいたい おなじ いみの ぶんは どれですか。", "Chọn câu có nghĩa gần giống câu trong 【 】.")

vocab = [
    mcq("n5m2_v01", "m1", *M1, "【毎朝】 コーヒーを のみます。", ["まいあさ", "まいにち", "まいばん", "まえあさ"], 0, "毎朝 = mỗi sáng."),
    mcq("n5m2_v02", "m1", *M1, "えきの 【北】に こうえんが あります。", ["きた", "みなみ", "ひがし", "にし"], 0, "北 = phía bắc (きた)."),
    mcq("n5m2_v03", "m1", *M1, "この かばんは 【高い】です。", ["たかい", "ひくい", "やすい", "おおい"], 0, "高い = cao, đắt (たかい)."),
    mcq("n5m2_v04", "m1", *M1, "【来週】 しけんが あります。", ["らいしゅう", "らいしゅ", "きしゅう", "こんしゅう"], 0, "来週 = tuần sau; 今週 là こんしゅう."),
    mcq("n5m2_v05", "m1", *M1, "ふゆやすみに 【外国】へ いきたいです。", ["がいこく", "がいごく", "そとくに", "かいこく"], 0, "外国 = nước ngoài (がいこく)."),
    mcq("n5m2_v06", "m1", *M1, "【父】は ぎんこうで はたらいて います。", ["ちち", "はは", "あに", "あね"], 0, "父 = bố (khi nói về bố mình)."),
    mcq("n5m2_v07", "m1", *M1, "つくえの 【上】に ほんが あります。", ["うえ", "した", "なか", "よこ"], 0, "上 = trên (うえ)."),
    mcq("n5m2_v08", "m2", *M2, "【しろい】 くつを かいました。", ["白い", "百い", "日い", "目い"], 0, "しろい = 白い (trắng)."),
    mcq("n5m2_v09", "m2", *M2, "まいばん 【てれび】を みます。", ["テレビ", "テレビー", "テルビ", "デレビ"], 0, "Từ ngoại lai viết katakana: テレビ."),
    mcq("n5m2_v10", "m2", *M2, "なつやすみに 【やま】に のぼりました。", ["山", "川", "出", "木"], 0, "やま = 山 (núi); 川 là かわ."),
    mcq("n5m2_v11", "m2", *M2, "【ごご】 3じに あいましょう。", ["午後", "午前", "牛後", "千後"], 0, "ごご = 午後 (buổi chiều); 午前 là ごぜん."),
    mcq("n5m2_v12", "m2", *M2, "この 【へや】は ひろいです。", ["部屋", "部家", "都屋", "剖屋"], 0, "へや = 部屋 (căn phòng)."),
    mcq("n5m2_v13", "m3", *M3, "さむいですから、まどを （　）ください。", ["しめて", "あけて", "けして", "つけて"], 0, "Trời lạnh nên đóng (しめる) cửa sổ."),
    mcq("n5m2_v14", "m3", *M3, "のどが かわいたので、みずを （　）。", ["のみました", "たべました", "はきました", "ききました"], 0, "Khát nước → uống (のむ)."),
    mcq("n5m2_v15", "m3", *M3, "わたしの へやは せまいですが、（　）です。", ["きれい", "せまい", "ちいさい", "くらい"], 0, "「〜が」 nối hai ý trái ngược: hẹp nhưng sạch đẹp."),
    mcq("n5m2_v16", "m3", *M3, "「いってきます。」「（　）。」", ["いってらっしゃい", "おかえりなさい", "ただいま", "いただきます"], 0, "Đáp lại 「いってきます」 là 「いってらっしゃい」."),
    mcq("n5m2_v17", "m3", *M3, "えきまで あるいて 10（　）です。", ["ぷん", "じ", "かい", "ほん"], 0, "10ぷん = 10 phút."),
    mcq("n5m2_v18", "m3", *M3, "あしたは やすみですから、（　）まで ねます。", ["おそく", "はやく", "すこし", "ちかく"], 0, "おそくまで ねる = ngủ đến muộn."),
    mcq("n5m2_v19", "m4", *M4, "【この へやは くらいです。】", ["この へやは あかるく ありません。", "この へやは あかるいです。", "この へやは ひろく ありません。", "この へやは しずかです。"], 0, "くらい (tối) = không sáng (あかるくない)."),
    mcq("n5m2_v20", "m4", *M4, "【わたしは まいばん ふろに はいります。】", ["わたしは よる いつも ふろに はいります。", "わたしは あさ いつも ふろに はいります。", "わたしは よる ときどき ふろに はいります。", "わたしは あさ ときどき ふろに はいります。"], 0, "まいばん = mỗi tối = よる いつも."),
    mcq("n5m2_v21", "m4", *M4, "【たなかさんは りょうりが じょうずです。】", ["たなかさんは りょうりを つくるのが じょうずです。", "たなかさんは りょうりが へたです。", "たなかさんは りょうりが きらいです。", "たなかさんは りょうりを あまり つくりません。"], 0, "りょうりが じょうず = nấu ăn giỏi."),
]

# ═══════════════════════ 言語知識（文法）・読解 ═══════════════════════

G1 = ("（　）に 何を 入れますか。", "Điền vào （ ） phương án đúng.")
G2 = ("★ に 入る ものは どれですか。", "Sắp xếp các phần thành câu đúng; chọn phần đứng ở vị trí ★.")
G3 = ("ぶんしょうの いみを かんがえて、（　）に 入る ものを えらんで ください。", "Đọc đoạn văn, chọn từ điền vào chỗ trống.")
RD = ("しつもんに こたえて ください。", "Đọc và trả lời câu hỏi.")

grammar = [
    mcq("n5m2_g01", "m1", *G1, "わたしは まいにち でんしゃ（　）がっこうへ 行きます。", ["で", "に", "を", "が"], 0, "で chỉ phương tiện: đi bằng tàu."),
    mcq("n5m2_g02", "m1", *G1, "つくえの 下に ねこ（　）います。", ["が", "を", "で", "へ"], 0, "〜に 〜が います: có (con gì) ở đâu."),
    mcq("n5m2_g03", "m1", *G1, "A「この ケーキ、（　）ですか。」\nB「300えんです。」", ["いくら", "いくつ", "どれ", "なん"], 0, "Hỏi giá: いくらですか."),
    mcq("n5m2_g04", "m1", *G1, "日本語は むずかしいです（　）、おもしろいです。", ["が", "から", "と", "や"], 0, "が nối hai vế trái ngược: khó nhưng thú vị."),
    mcq("n5m2_g05", "m1", *G1, "きのうは あたま（　）いたかったです。", ["が", "を", "に", "で"], 0, "〜が いたい: (chỗ nào) đau."),
    mcq("n5m2_g06", "m1", *G1, "しゅくだいを（　）から、あそびに 行きます。", ["して", "する", "した", "しない"], 0, "Vて から = sau khi làm V."),
    mcq("n5m2_g07", "m1", *G1, "この みせは（　）、やすいです。", ["しずかで", "しずかだ", "しずかの", "しずかに"], 0, "Tính từ な nối câu bằng で: しずかで やすい."),
    mcq("n5m2_g08", "m1", *G1, "あした ともだちと えいがを みに（　）。", ["行きます", "見ます", "買います", "会います"], 0, "Vます(bỏ ます)に 行く: đi để làm gì."),
    mcq("n5m2_g09", "m1", *G1, "A「いっしょに おちゃを のみませんか。」\nB「ええ、（　）。」", ["のみましょう", "のみません", "のみました", "のんで いません"], 0, "Nhận lời mời 〜ませんか → 〜ましょう."),
    mcq("n5m2_g10", "m2", *G2, "あそこ ＿＿ ＿＿ ★ ＿＿ やまださんです。", ["いる", "で", "人が", "ないて"], 0, "あそこで ないて いる 人が やまださんです → ★ = いる."),
    mcq("n5m2_g11", "m2", *G2, "わたしは ＿＿ ＿＿ ★ ＿＿ すきです。", ["のが", "あまい", "たべる", "ものを"], 2, "わたしは あまい ものを たべる のが すきです → ★ = たべる."),
    mcq("n5m2_g12", "m2", *G2, "きのう ＿＿ ＿＿ ★ ＿＿ かいました。", ["セーターを", "ははと", "あかい", "デパートで"], 2, "きのう ははと デパートで あかい セーターを かいました → ★ = あかい."),
    mcq("n5m2_g13", "m2", *G2, "それは ＿＿ ＿＿ ★ ＿＿ です。", ["ほん", "かった", "わたしが", "きのう"], 1, "それは わたしが きのう かった ほん です → ★ = かった."),
]

essay_ja = ("わたしは 先週の 日よう日、ともだちと うみへ 行きました。うみは とても きれい （35）。"
            "わたしたちは およいだり、すなで あそんだり （36）。うみは とおいですから、でんしゃ （37） 2じかん かかりました。"
            "とても たのしかったです。こんどは かぞく （38） 行きたいです。")
reading_passages = [
    Passage("p_m3", title_ja="もんだい3　ぶんしょう", title_vi="もんだい3 · Đoạn văn của du học sinh", title_en="もんだい3",
            body_ja=essay_ja, body_vi=essay_ja, body_en=essay_ja),
    Passage("p_m4a", title_ja="もんだい4 (1)　メモ", title_vi="もんだい4 (1) · Lời nhắn", title_en="もんだい4 (1)",
            body_ja="マリアさんへ\nきょうは 5じに かえります。れいぞうこに ケーキが あります。たべても いいですよ。"
                    "でも、ぎゅうにゅうは のまないで ください。あしたの あさ つかいます。\nはは",
            body_vi="マリアさんへ\nきょうは 5じに かえります。れいぞうこに ケーキが あります。たべても いいですよ。"
                    "でも、ぎゅうにゅうは のまないで ください。あしたの あさ つかいます。\nはは"),
    Passage("p_m4b", title_ja="もんだい4 (2)　メール", title_vi="もんだい4 (2) · Thư điện tử", title_en="もんだい4 (2)",
            body_ja="やまださん\nあしたの じゅぎょうは 10じからでしたが、11じからに なりました。"
                    "きょうしつも 3がいから 5かいに かわりました。きょうかしょを わすれないで ください。\n田中",
            body_vi="やまださん\nあしたの じゅぎょうは 10じからでしたが、11じからに なりました。"
                    "きょうしつも 3がいから 5かいに かわりました。きょうかしょを わすれないで ください。\n田中"),
    Passage("p_m5", title_ja="もんだい5　わたしの へや", title_vi="もんだい5 · Phòng của tôi", title_en="もんだい5",
            body_ja="わたしは 先月から アパートに すんで います。へやは 一つだけで、あまり ひろく ありませんが、"
                    "まどが 大きくて あかるいです。えきから あるいて 5ふんですから、とても べんりです。\n"
                    "でも、ちかくに スーパーが ありません。かいものに 行く ときは、バスに のります。ときどき たいへんです。"
                    "らいねんは スーパーの ちかくの へやに ひっこしたいです。",
            body_vi="わたしは 先月から アパートに すんで います。へやは 一つだけで、あまり ひろく ありませんが、"
                    "まどが 大きくて あかるいです。えきから あるいて 5ふんですから、とても べんりです。\n"
                    "でも、ちかくに スーパーが ありません。かいものに 行く ときは、バスに のります。ときどき たいへんです。"
                    "らいねんは スーパーの ちかくの へやに ひっこしたいです。"),
    Passage("p_m6", title_ja="もんだい6　ひばりスポーツセンター　プール", title_vi="もんだい6 · Bảng giờ mở cửa hồ bơi", title_en="もんだい6",
            body_ja="月・水・金　9:00〜17:00　おとな 500えん／こども 200えん\n"
                    "火・木　　13:00〜21:00　おとな 500えん／こども 200えん\n"
                    "土・日　　9:00〜21:00　おとな 700えん／こども 300えん\n"
                    "※ まいつき さいごの 月よう日は やすみです。",
            body_vi="月・水・金　9:00〜17:00　おとな 500えん／こども 200えん\n"
                    "火・木　　13:00〜21:00　おとな 500えん／こども 200えん\n"
                    "土・日　　9:00〜21:00　おとな 700えん／こども 300えん\n"
                    "※ まいつき さいごの 月よう日は やすみです。"),
]

grammar += [
    mcq("n5m2_g14", "m3", *G3, "（35）", ["でした", "です", "だった", "じゃありません"], 0, "Cả đoạn ở thì quá khứ lịch sự: きれいでした.", passage_id="p_m3"),
    mcq("n5m2_g15", "m3", *G3, "（36）", ["しました", "ありました", "いきました", "なりました"], 0, "〜たり 〜たり します: liệt kê hoạt động.", passage_id="p_m3"),
    mcq("n5m2_g16", "m3", *G3, "（37）", ["で", "に", "を", "と"], 0, "で chỉ phương tiện: でんしゃで.", passage_id="p_m3"),
    mcq("n5m2_g17", "m3", *G3, "（38）", ["と", "を", "が", "へ"], 0, "〜と 行く: đi cùng ai.", passage_id="p_m3"),
    mcq("n5m2_g18", "m4", *RD, "マリアさんは 何を しては いけませんか。", ["ぎゅうにゅうを のむ", "ケーキを たべる", "5じに かえる", "あさごはんを つくる"], 0, "「ぎゅうにゅうは のまないで ください」: không được uống sữa.", passage_id="p_m4a"),
    mcq("n5m2_g19", "m4", *RD, "あしたの じゅぎょうは どう なりましたか。", ["11じから 5かいの きょうしつで あります", "10じから 5かいの きょうしつで あります", "11じから 3がいの きょうしつで あります", "10じから 3がいの きょうしつで あります"], 0, "Giờ đổi sang 11 giờ, phòng chuyển lên tầng 5.", passage_id="p_m4b"),
    mcq("n5m2_g20", "m5", *RD, "この 人の へやは どんな へやですか。", ["ひろく ないが、あかるい へや", "ひろくて あかるい へや", "くらいが、ひろい へや", "せまくて くらい へや"], 0, "あまり ひろく ありませんが、… あかるいです.", passage_id="p_m5"),
    mcq("n5m2_g21", "m5", *RD, "この 人は どうして ひっこしたいですか。", ["ちかくに スーパーが ないから", "えきから とおいから", "へやが くらいから", "へやが せまいから"], 0, "Gần nhà không có siêu thị, đi mua sắm phải đi xe buýt.", passage_id="p_m5"),
    mcq("n5m2_g22", "m6", *RD, "リンさんは 土よう日の ゆうがた 6じに、こどもと 二人で プールに 行きます。リンさんは おとなです。ぜんぶで いくら はらいますか。",
        ["700えん", "900えん", "1000えん", "1200えん"], 2, "Thứ bảy: người lớn 700 + trẻ em 300 = 1000 yên.", passage_id="p_m6"),
]

# ═══════════════════════ 聴解 ═══════════════════════
# Each item: (id, mondai, situation_vi, script lines [(speaker, text)], question_ja (spoken), choices, correct, why_vi)
# Speakers: N = narrator, F = woman, M = man.

L1 = "もんだい1では、はじめに しつもんを きいて ください。それから はなしを きいて、もんだいようしの 1から4の なかから、いちばん いい ものを 一つ えらんで ください。"
L2 = "もんだい2では、はじめに しつもんを きいて ください。それから はなしを きいて、もんだいようしの 1から4の なかから、いちばん いい ものを 一つ えらんで ください。"
L3 = "もんだい3では、ぶんを きいて ください。やじるしの 人は 何と いいますか。1から3の なかから、いちばん いい ものを 一つ えらんで ください。"
L4 = "もんだい4は、ぶんを きいて、1から3の なかから、いちばん いい へんじを 一つ えらんで ください。"

listening = [
    # ── もんだい1 課題理解 ──
    ("l01", "m1", "Ở văn phòng.", [("N", "かいしゃで 女の人と 男の人が 話して います。男の人は はじめに 何を しますか。"),
        ("F", "たなかさん、かいぎの まえに、この しりょうを 5まい コピーして ください。"), ("M", "はい、わかりました。いすも ならべましょうか。"),
        ("F", "いすは もう ならべました。あ、コピーの あとで、おちゃを かって きて ください。"), ("M", "はい。")],
     "男の人は はじめに 何を しますか。", ["コピーを する", "いすを ならべる", "おちゃを かう", "かいぎに 出る"], 0, "Việc đầu tiên là photo tài liệu; mua trà để sau."),
    ("l02", "m1", "Trong lớp học.", [("N", "クラスで 先生が 話して います。学生は あした 何を もって いきますか。"),
        ("F", "みなさん、あしたは テストです。きょうかしょの 20ページから 25ページまで べんきょうして ください。"),
        ("M", "先生、じしょを もって きても いいですか。"), ("F", "じしょは だめです。えんぴつと けしゴムを もって きて ください。")],
     "学生は あした 何を もって いきますか。", ["えんぴつと けしゴム", "じしょと えんぴつ", "きょうかしょと じしょ", "けしゴムと じしょ"], 0, "Không được mang từ điển; mang bút chì và tẩy."),
    ("l03", "m1", "Ở cửa hàng hoa quả.", [("N", "みせで 男の人と 店の人が 話して います。男の人は ぜんぶで いくら はらいますか。"),
        ("M", "すみません、この りんごは いくらですか。"), ("F", "一つ 150えんです。三つで 400えんです。"),
        ("M", "じゃあ、三つ ください。それと、みかんも 五つ ください。"), ("F", "みかんは 一つ 50えんです。")],
     "男の人は ぜんぶで いくら はらいますか。", ["650えん", "450えん", "700えん", "400えん"], 0, "3 quả táo 400 yên + 5 quả quýt × 50 = 250 → 650 yên."),
    ("l04", "m1", "Hẹn đi xem phim.", [("N", "女の人と 男の人が 話して います。二人は 何時に どこで あいますか。"),
        ("F", "あしたの えいが、何時に あいましょうか。えいがは 3じからです。"), ("M", "じゃあ、2じ半に えきの まえで あいましょう。"),
        ("F", "えきは 人が おおいですから、えいがかんの まえに しませんか。"), ("M", "いいですよ。じゃあ、2じ45ふんに えいがかんの まえで。")],
     "二人は 何時に どこで あいますか。", ["2じ45ふんに えいがかんの まえ", "2じ半に えきの まえ", "3じに えいがかんの まえ", "2じ半に えいがかんの まえ"], 0, "Đổi chỗ hẹn sang trước rạp, giờ 2:45."),
    ("l05", "m1", "Hỏi đường.", [("N", "男の人と 女の人が 話して います。ゆうびんきょくは どこですか。"),
        ("M", "すみません、ゆうびんきょくは どこですか。"),
        ("F", "この みちを まっすぐ 行って、二つめの かどを 右に まがって ください。ゆうびんきょくは ぎんこうの となりです。")],
     "ゆうびんきょくは どこですか。", ["二つめの かどを 右、ぎんこうの となり", "一つめの かどを 右、ぎんこうの となり", "二つめの かどを 左、ぎんこうの となり", "二つめの かどを 右、ぎんこうの まえ"], 0, "Góc thứ hai rẽ phải, cạnh ngân hàng."),
    ("l06", "m1", "Ở nhà.", [("N", "うちで お母さんと 男の子が 話して います。男の子は このあと まず 何を しますか。"),
        ("F", "ゆうごはんの まえに、しゅくだいを して ね。"), ("M", "でも、いま テレビを 見たいよ。"),
        ("F", "だめ。テレビは ゆうごはんの あとで 見ても いいです。"), ("M", "はあい。")],
     "男の子は このあと まず 何を しますか。", ["しゅくだいを する", "テレビを 見る", "ゆうごはんを たべる", "ねる"], 0, "Phải làm bài tập trước bữa tối."),
    ("l07", "m1", "Chọn quà sinh nhật.", [("N", "男の人と 女の人が 話して います。男の人は キムさんに 何を あげますか。"),
        ("M", "キムさんの たんじょうびに 何を あげましょうか。"), ("F", "キムさんは ほんが すきですよ。"),
        ("M", "ほんは このあいだ あげました。"), ("F", "じゃあ、おかしは どうですか。キムさんは あまい ものが すきですから。"), ("M", "そうですね。そう しましょう。")],
     "男の人は キムさんに 何を あげますか。", ["おかし", "ほん", "はな", "CD"], 0, "Sách đã tặng rồi; chọn bánh kẹo vì Kim thích đồ ngọt."),
    # ── もんだい2 ポイント理解 ──
    ("l08", "m2", "Nghỉ học hôm qua.", [("N", "男の人と 女の人が 話して います。女の人は どうして がっこうを やすみましたか。"),
        ("M", "きのう どうして がっこうを やすみましたか。かぜですか。"), ("F", "いいえ、かぜじゃ ありません。ははが びょうきで、びょういんへ 行きました。")],
     "女の人は どうして がっこうを やすみましたか。", ["おかあさんが びょうきだったから", "かぜを ひいたから", "あたまが いたかったから", "びょういんで はたらいて いるから"], 0, "Vì mẹ bị ốm nên đưa mẹ đi viện."),
    ("l09", "m2", "Kỳ nghỉ hè.", [("N", "女の人と 男の人が 話して います。男の人は なつやすみに どこへ 行きましたか。"),
        ("F", "たけしさんは なつやすみに どこへ 行きましたか。"),
        ("M", "おきなわへ 行く つもりでしたが、たいふうで ひこうきが とびませんでした。それで、ほっかいどうへ 行きました。")],
     "男の人は なつやすみに どこへ 行きましたか。", ["ほっかいどう", "おきなわ", "きょうと", "どこへも 行きませんでした"], 0, "Định đi Okinawa nhưng máy bay không bay, nên đi Hokkaido."),
    ("l10", "m2", "Đôi giày mới.", [("N", "女の人と 男の人が 話して います。男の人は くつを いくらで かいましたか。"),
        ("F", "その くつ、いいですね。いくらでしたか。"), ("M", "8000えんでしたが、きのうは はんぶんの ねだんでしたよ。")],
     "男の人は くつを いくらで かいましたか。", ["4000えん", "8000えん", "2000えん", "6000えん"], 0, "Giá 8000 yên, hôm qua giảm một nửa → 4000 yên."),
    ("l11", "m2", "Giờ thức dậy.", [("N", "男の人と 女の人が 話して います。女の人は 日よう日に 何時ごろ おきますか。"),
        ("M", "まいあさ 何時に おきますか。"), ("F", "へいじつは 6じですが、土よう日と 日よう日は 8じごろ おきます。")],
     "女の人は 日よう日に 何時ごろ おきますか。", ["8じごろ", "6じ", "7じ", "9じ"], 0, "Ngày thường 6 giờ, cuối tuần khoảng 8 giờ."),
    ("l12", "m2", "Tìm nhà hàng.", [("N", "女の人と 男の人が 話して います。きょう あいて いる レストランは どこですか。"),
        ("F", "すみません、この ちかくに レストランは ありますか。"),
        ("M", "えきの まえに ありますよ。でも、きょうは 水よう日ですから、やすみです。ホテルの 中の レストランは あいて いますよ。")],
     "きょう あいて いる レストランは どこですか。", ["ホテルの 中", "えきの まえ", "えきの 中", "デパートの 中"], 0, "Nhà hàng trước ga nghỉ thứ tư; nhà hàng trong khách sạn mở."),
    ("l13", "m2", "Chiếc ô.", [("N", "男の人と 女の人が 話して います。女の人は かさを だれに もらいましたか。"),
        ("M", "その かさ、すてきですね。"), ("F", "ありがとう。たんじょうびに あねに もらいました。")],
     "女の人は かさを だれに もらいましたか。", ["おねえさん", "おかあさん", "ともだち", "おにいさん"], 0, "あね = chị gái mình → おねえさん."),
    # ── もんだい3 発話表現 (situation shown as text; options spoken) ──
    ("l14", "m3", "Bạn bước vào nhà một người bạn. Bạn nói gì?", [("N", "ともだちの うちに 入ります。何と いいますか。"),
        ("F", "1、おじゃまします。"), ("F", "2、いってきます。"), ("F", "3、おかえりなさい。")], "", ["1", "2", "3"], 0, "Khi vào nhà người khác: おじゃまします."),
    ("l15", "m3", "Bạn muốn hỏi giáo viên một câu. Bạn nói gì?", [("N", "先生に しつもんが あります。何と いいますか。"),
        ("M", "1、しつもんしても いいですか。"), ("M", "2、しつもんしましょう。"), ("M", "3、しつもんしましたか。")], "", ["1", "2", "3"], 0, "Xin phép: 〜ても いいですか."),
    ("l16", "m3", "Bạn sắp ăn cơm. Trước khi ăn bạn nói gì?", [("N", "これから ごはんを たべます。何と いいますか。"),
        ("F", "1、ごちそうさまでした。"), ("F", "2、いただきます。"), ("F", "3、おやすみなさい。")], "", ["1", "2", "3"], 1, "Trước khi ăn: いただきます; ăn xong: ごちそうさまでした."),
    ("l17", "m3", "Bạn va phải một người trên đường. Bạn nói gì?", [("N", "みちで 人に ぶつかりました。何と いいますか。"),
        ("M", "1、どういたしまして。"), ("M", "2、ありがとう。"), ("M", "3、すみません。")], "", ["1", "2", "3"], 2, "Xin lỗi: すみません."),
    ("l18", "m3", "Ở cửa hàng, bạn muốn xem chiếc áo sơ mi đỏ. Bạn nói gì?", [("N", "みせで あかい シャツが 見たいです。何と いいますか。"),
        ("F", "1、あかい シャツを 見せて ください。"), ("F", "2、あかい シャツを 見て ください。"), ("F", "3、あかい シャツは いかがですか。")], "", ["1", "2", "3"], 0, "見せて ください = cho tôi xem; 見て ください = hãy xem."),
    # ── もんだい4 即時応答 ──
    ("l19", "m4", "Nghe câu nói và chọn câu đáp phù hợp.", [("M", "おげんきですか。"),
        ("F", "1、はい、げんきです。"), ("F", "2、はい、おげんきです。"), ("F", "3、いいえ、げんきでした。")], "", ["1", "2", "3"], 0, "Nói về mình không dùng お: げんきです."),
    ("l20", "m4", "Nghe câu nói và chọn câu đáp phù hợp.", [("F", "ごはん、もう たべましたか。"),
        ("M", "1、はい、たべません。"), ("M", "2、いいえ、まだです。"), ("M", "3、はい、まだです。")], "", ["1", "2", "3"], 1, "もう〜ましたか → いいえ、まだです."),
    ("l21", "m4", "Nghe câu nói và chọn câu đáp phù hợp.", [("M", "これ、だれの かさですか。"),
        ("F", "1、わたしのです。"), ("F", "2、わたしは かさです。"), ("F", "3、かさが あります。")], "", ["1", "2", "3"], 0, "だれの → わたしのです."),
    ("l22", "m4", "Nghe câu nói và chọn câu đáp phù hợp.", [("F", "どうぞ、すわって ください。"),
        ("M", "1、はい、すわって ください。"), ("M", "2、ありがとう ございます。"), ("M", "3、どういたしまして。")], "", ["1", "2", "3"], 1, "Được mời ngồi → cảm ơn."),
    ("l23", "m4", "Nghe câu nói và chọn câu đáp phù hợp.", [("M", "しゅうまつは 何を しましたか。"),
        ("F", "1、えいがを 見ました。"), ("F", "2、えいがを 見ます。"), ("F", "3、えいがが すきです。")], "", ["1", "2", "3"], 0, "Hỏi quá khứ → trả lời quá khứ."),
    ("l24", "m4", "Nghe câu nói và chọn câu đáp phù hợp.", [("F", "あしたの パーティー、来ますか。"),
        ("M", "1、はい、来ました。"), ("M", "2、はい、行きます。"), ("M", "3、はい、行って ください。")], "", ["1", "2", "3"], 1, "Nói về việc mình đến chỗ người kia: 行きます."),
]

INSTR = {"m1": L1, "m2": L2, "m3": L3, "m4": L4}
VOICE = {"N": ("ja-JP-NanamiNeural", "-8%", "-6Hz"), "F": ("ja-JP-NanamiNeural", "+0%", "+0Hz"), "M": ("ja-JP-KeitaNeural", "+0%", "+0Hz")}


def guid_for(path):
    return hashlib.md5(("nihongolife:" + os.path.relpath(path, ROOT).replace("\\", "/")).encode()).hexdigest()


def tts(text, speaker, out):
    voice, rate, pitch = VOICE[speaker]
    for attempt in range(6):
        r = subprocess.run(["edge-tts", "--voice", voice, f"--rate={rate}", f"--pitch={pitch}", "--text", text, "--write-media", out],
                           capture_output=True)
        if r.returncode == 0 and os.path.exists(out) and os.path.getsize(out) > 1000:
            return
        time.sleep(2 + attempt * 2)
    raise RuntimeError("TTS failed: " + text)


def build_audio(item):
    """Narration as on the real test: the question first (もんだい1/2), the talk, the question again, answer time."""
    qid, mondai, _, script, question, _, _, _ = item
    out = os.path.join(AUDIO_DIR, f"n5m2_{qid}.mp3")
    if os.path.exists(out):
        return out
    os.makedirs(AUDIO_DIR, exist_ok=True)
    number = int(qid[1:])
    parts = [("N", f"{number}ばん。")]
    if mondai in ("m1", "m2") and question:
        parts += [("N", question)]
    parts += script
    if mondai in ("m1", "m2") and question:
        parts += [("N", question)]
    with tempfile.TemporaryDirectory() as tmp:
        files = []
        for k, (speaker, text) in enumerate(parts):
            f = os.path.join(tmp, f"{k:02d}.mp3")
            tts(text, speaker, f)
            files.append(f)
        silence = os.path.join(tmp, "gap.mp3")
        end = os.path.join(tmp, "end.mp3")
        subprocess.run(["ffmpeg", "-y", "-v", "error", "-f", "lavfi", "-i", "anullsrc=r=24000:cl=mono", "-t", "0.6", "-q:a", "4", silence], check=True)
        subprocess.run(["ffmpeg", "-y", "-v", "error", "-f", "lavfi", "-i", "anullsrc=r=24000:cl=mono", "-t", "4" if mondai in ("m1", "m2") else "2", "-q:a", "4", end], check=True)
        listing = os.path.join(tmp, "list.txt")
        with open(listing, "w", encoding="utf-8") as fh:
            for f in files:
                fh.write(f"file '{f}'\nfile '{silence}'\n")
            fh.write(f"file '{end}'\n")
        subprocess.run(["ffmpeg", "-y", "-v", "error", "-f", "concat", "-safe", "0", "-i", listing,
                        "-ar", "24000", "-ac", "1", "-b:a", "64k", out], check=True)
    return out


def ensure_meta(path):
    meta = path + ".meta"
    if not os.path.exists(meta):
        with open(meta, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(f"fileFormatVersion: 2\nguid: {guid_for(path)}\n")
    for line in open(meta, encoding="utf-8"):
        if line.startswith("guid:"):
            return line.split()[1]
    raise RuntimeError("No guid in " + meta)


listen_passages, listen_questions = [], []
for item in listening:
    qid, mondai, situation, script, question, choices, correct, why = item
    path = build_audio(item)
    guid = ensure_meta(path)
    transcript = "\n".join(("" if s == "N" else ("女：" if s == "F" else "男：")) + t for s, t in script)
    number = int(qid[1:])
    listen_passages.append(Passage(f"p_{qid}", title_ja=f"もんだい{mondai[1]}　{number}ばん", title_vi=f"もんだい{mondai[1]} · Câu {number} — {situation}",
                                   title_en=f"もんだい{mondai[1]} · {number}", body_ja=transcript, body_vi=transcript, body_en=transcript,
                                   max_plays=1, audio_guid=guid))
    if mondai in ("m1", "m2"):  # printed options (spoken ones in もんだい3/4 keep their 1-2-3 order)
        choices, correct = shuffled(qid, choices, correct)
    prompt = question if question else "えを 見ながら きいて ください。" if mondai == "m3" else "ぶんを きいて、へんじを えらんで ください。"
    listen_questions.append(Question(
        id=f"n5m2_{qid}", type="mc", passage_id=f"p_{qid}", mondai=mondai,
        prompt_ja=f"{number}ばん　{prompt}", prompt_vi=f"Câu {number}. {situation}\n{prompt}", prompt_en=f"{number}. {prompt}",
        choices=opts(*choices), correct=correct, explanation_vi=f"Đáp án: {choices[correct]}. {why}", explanation_en=f"Answer: {choices[correct]}.",
        tags=["n5", mondai, "listening"]))

exam = Exam(
    id="jlpt_n5_mock_2", exam_type="jlpt", level="N5",
    title_vi="JLPT N5 — Đề thi thử đầy đủ số 2", title_en="JLPT N5 — Full mock test 2", title_ja="JLPT N5 もぎしけん 2",
    description_vi="Đề tự soạn theo đúng cấu trúc N5 hiện hành (67 câu, 90 phút). Phần nghe dùng giọng đọc AI. Điểm quy đổi tuyến tính để luyện tập.",
    description_en="Original practice test in the current N5 layout (67 questions, 90 minutes). Listening uses AI voices.",
    jlpt_total_pass=80, jlpt_section_pass=19,
    score_groups=[("gengo", "言語知識（文字・語彙・文法）・読解", "Kiến thức ngôn ngữ · Đọc hiểu", 120, 38),
                  ("choukai", "聴解", "Nghe hiểu", 60, 19)],
    sections=[
        Section("n5m2_vocab", "jlpt_vocab", "Từ vựng (文字・語彙) · 20 phút", "Vocabulary · 20 min", "言語知識（文字・語彙）", 1200, 60,
                questions=vocab, score_group="gengo",
                mondai=[("m1", "もんだい1　漢字読み", "đọc chữ Hán"), ("m2", "もんだい2　表記", "cách viết"),
                        ("m3", "もんだい3　文脈規定", "từ hợp ngữ cảnh"), ("m4", "もんだい4　言い換え類義", "câu đồng nghĩa")]),
        Section("n5m2_grammar", "jlpt_grammar_reading", "Ngữ pháp · Đọc hiểu (文法・読解) · 40 phút", "Grammar · Reading · 40 min", "言語知識（文法）・読解", 2400, 60,
                passages=reading_passages, questions=grammar, score_group="gengo",
                mondai=[("m1", "もんだい1　文法形式", "chọn ngữ pháp"), ("m2", "もんだい2　文の組み立て", "sắp xếp câu ★"),
                        ("m3", "もんだい3　文章の文法", "ngữ pháp trong đoạn văn"), ("m4", "もんだい4　内容理解（短文）", "đọc hiểu đoạn ngắn"),
                        ("m5", "もんだい5　内容理解（中文）", "đọc hiểu đoạn vừa"), ("m6", "もんだい6　情報検索", "tìm thông tin")]),
        Section("n5m2_listening", "jlpt_listening", "Nghe hiểu (聴解) · 30 phút · giọng đọc AI", "Listening · 30 min · AI voices", "聴解", 1800, 60,
                passages=listen_passages, questions=listen_questions, score_group="choukai",
                mondai=[("m1", "もんだい1　課題理解", "hiểu yêu cầu"), ("m2", "もんだい2　ポイント理解", "nắm ý chính"),
                        ("m3", "もんだい3　発話表現", "chọn câu nói"), ("m4", "もんだい4　即時応答", "đáp lời ngay")]),
    ],
)

if __name__ == "__main__":
    counts = {s.id: len(s.questions) for s in exam.sections}
    assert counts == {"n5m2_vocab": 21, "n5m2_grammar": 22, "n5m2_listening": 24}, counts
    for s in exam.sections:
        for qn in s.questions:
            assert 0 <= qn.correct < len(qn.choices), qn.id
    exam.write(OUT, name="jlpt_n5_mock_2")
    print("wrote", OUT, counts)
