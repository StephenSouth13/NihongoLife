# -*- coding: utf-8 -*-
"""Generates Assets/NihongoLife/Resources/Exams/jlpt_n5_mock_1.asset — a short N5 practice test
covering Vocabulary, Grammar/Reading and (text-simulated) Listening. Run from anywhere:
    python Tools/exam/gen_jlpt_n5_mock1.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from exam_lib import Exam, Section, Passage, Question, Choice

OUT = os.path.join(os.path.dirname(__file__), "..", "..",
                    "Assets", "NihongoLife", "Resources", "Exams", "jlpt_n5_mock_1.asset")


def mc(id, ja_word, reading, choices, correct, tags=()):
    return Question(
        id=id, type="mc",
        prompt_ja=ja_word, prompt_vi=f"「{ja_word}」の読み方はどれですか。",
        prompt_en=f"「{ja_word}」の読み方はどれですか。",
        choices=[Choice(vi=c, en=c, ja=c) for c in choices],
        correct=correct,
        explanation_vi=f"正解: {choices[correct]}",
        explanation_en=f"Correct reading: {choices[correct]}",
        tags=list(tags), points=1,
    )


vocab_questions = [
    mc("n5v_q1", "先生", "せんせい", ["せんせい", "せんせえ", "せいせん", "せんせ"], 0, ("kanji", "n5")),
    mc("n5v_q2", "今日", "きょう", ["きょう", "きにち", "こんび", "きゅう"], 0, ("kanji", "n5")),
    mc("n5v_q3", "水", "みず", ["みず", "みす", "すい", "みづ"], 0, ("kanji", "n5")),
    mc("n5v_q4", "学校", "がっこう", ["がっこう", "がくこう", "かっこう", "がこう"], 0, ("kanji", "n5")),
    mc("n5v_q5", "友達", "ともだち", ["ともだち", "ゆうだち", "とうだち", "ともたち"], 0, ("kanji", "n5")),
    mc("n5v_q6", "食べます", "たべます", ["たべます", "たへます", "だべます", "たべまつ"], 0, ("kanji", "n5")),
]

grammar_questions = [
    Question(
        id="n5g_q1", type="mc",
        prompt_ja="わたし（　）がくせいです。", prompt_en="わたし（　）がくせいです。",
        choices=[Choice("は", "は", "は"), Choice("を", "を", "を"), Choice("に", "に", "に"), Choice("で", "で", "で")],
        correct=0, explanation_vi="Trợ từ「は」đánh dấu chủ đề câu.", explanation_en="「は」marks the topic of the sentence.",
        tags=["grammar", "n5", "particle"],
    ),
    Question(
        id="n5g_q2", type="mc",
        prompt_ja="きのう ともだち（　）あいました。", prompt_en="きのう ともだち（　）あいました。",
        choices=[Choice("に", "に", "に"), Choice("を", "を", "を"), Choice("は", "は", "は"), Choice("の", "の", "の")],
        correct=0, explanation_vi="「～に あいます」= gặp ai đó.", explanation_en="「～に あいます」means \"to meet (someone)\".",
        tags=["grammar", "n5", "particle"],
    ),
    Question(
        id="n5g_q3", type="mc",
        prompt_ja="この ほんは たなかさん（　）です。", prompt_en="この ほんは たなかさん（　）です。",
        choices=[Choice("の", "の", "の"), Choice("に", "に", "に"), Choice("で", "で", "で"), Choice("へ", "へ", "へ")],
        correct=0, explanation_vi="「～の」thể hiện sở hữu: sách của Tanaka.", explanation_en="「～の」shows possession: Tanaka's book.",
        tags=["grammar", "n5", "particle"],
    ),
    Question(
        id="n5g_q4", type="mc",
        prompt_ja="まいにち コーヒー（　）のみます。", prompt_en="まいにち コーヒー（　）のみます。",
        choices=[Choice("を", "を", "を"), Choice("に", "に", "に"), Choice("は", "は", "は"), Choice("で", "で", "で")],
        correct=0, explanation_vi="「を」đánh dấu tân ngữ trực tiếp của động từ.", explanation_en="「を」marks the direct object of the verb.",
        tags=["grammar", "n5", "particle"],
    ),
]

reading_passage = Passage(
    id="n5_passage_intro",
    title_vi="Bài đọc: Tự giới thiệu", title_en="Reading: Self-introduction", title_ja="読み物：自己紹介",
    body_ja="わたしはグエンです。ベトナムから来ました。今、日本語学校で勉強しています。"
            "毎日、朝8時に学校へ行きます。授業は9時から12時までです。午後はアルバイトをします。"
            "土曜日と日曜日は休みです。休みの日はともだちと買い物に行きます。",
    body_reading="わたしはグエンです。べとなむからきました。いま、にほんごがっこうでべんきょうしています。"
                 "まいにち、あさ8じにがっこうへいきます。じゅぎょうは9じから12じまでです。ごごはあるばいとをします。"
                 "どようびとにちようびはやすみです。やすみのひはともだちとかいものにいきます。",
    body_en="I am Nguyen. I came from Vietnam. Now I am studying at a Japanese language school. "
            "Every day I go to school at 8 in the morning. Classes run from 9 to 12. In the afternoon I do a part-time job. "
            "Saturday and Sunday are days off. On my days off I go shopping with friends.",
    body_vi="Tôi là Nguyễn. Tôi đến từ Việt Nam. Hiện tại tôi đang học ở trường tiếng Nhật. "
            "Mỗi ngày tôi đến trường lúc 8 giờ sáng. Giờ học từ 9 giờ đến 12 giờ. Buổi chiều tôi đi làm thêm. "
            "Thứ Bảy và Chủ Nhật là ngày nghỉ. Vào ngày nghỉ tôi đi mua sắm cùng bạn bè.",
)

reading_questions = [
    Question(
        id="n5r_q1", type="mc", passage_id="n5_passage_intro",
        prompt_ja="グエンさんは どこから 来ましたか。", prompt_en="グエンさんは どこから 来ましたか。",
        choices=[Choice("ベトナム", "ベトナム", "ベトナム"), Choice("にほん", "にほん", "にほん"),
                 Choice("かんこく", "かんこく", "かんこく"), Choice("ちゅうごく", "ちゅうごく", "ちゅうごく")],
        correct=0, explanation_vi="Bài đọc ghi rõ: 「ベトナムから来ました」.", explanation_en="The passage says: 「ベトナムから来ました」(came from Vietnam).",
        tags=["reading", "n5"],
    ),
    Question(
        id="n5r_q2", type="mc", passage_id="n5_passage_intro",
        prompt_ja="じゅぎょうは 何時からですか。", prompt_en="じゅぎょうは 何時からですか。",
        choices=[Choice("8時", "8時", "8時"), Choice("9時", "9時", "9時"),
                 Choice("12時", "12時", "12時"), Choice("午後", "午後", "午後")],
        correct=1, explanation_vi="「授業は9時から12時までです」nghĩa là học từ 9 giờ.", explanation_en="「授業は9時から12時までです」means classes run from 9.",
        tags=["reading", "n5"],
    ),
]

listening_passage = Passage(
    id="n5_listen_library",
    title_vi="Hội thoại: Hỏi đường", title_en="Conversation: Asking for directions", title_ja="会話：道を聞く",
    body_reading="おとこ：すみません、としょかんは どこですか。\n"
                 "おんな：としょかんですか。まっすぐ 行って、右に 曲がって ください。\n"
                 "おとこ：わかりました。ありがとうございます。",
    body_ja="男：すみません、図書館はどこですか。\n女：図書館ですか。まっすぐ行って、右に曲がってください。\n男：わかりました。ありがとうございます。",
    body_en="Man: Excuse me, where is the library?\nWoman: The library? Go straight, then turn right.\nMan: I understand. Thank you.",
    body_vi="Nam: Xin lỗi, thư viện ở đâu ạ?\nNữ: Thư viện à? Đi thẳng rồi rẽ phải.\nNam: Tôi hiểu rồi. Cảm ơn chị.",
    max_plays=2,
)

listening_questions = [
    Question(
        id="n5l_q1", type="mc", passage_id="n5_listen_library",
        prompt_ja="としょかんは どちらに ありますか。", prompt_en="としょかんは どちらに ありますか。",
        choices=[Choice("右", "右", "右"), Choice("左", "左", "左"), Choice("まっすぐ", "まっすぐ", "まっすぐ"), Choice("うしろ", "うしろ", "うしろ")],
        correct=0, explanation_vi="Người phụ nữ nói: 「まっすぐ行って、右に曲がってください」.", explanation_en="The woman says: 「まっすぐ行って、右に曲がってください」(go straight, then turn right).",
        tags=["listening", "n5"],
    ),
    Question(
        id="n5l_q2", type="mc", passage_id="n5_listen_library",
        prompt_ja="おとこの人は さいごに 何と言いましたか。", prompt_en="おとこの人は さいごに 何と言いましたか。",
        choices=[Choice("ありがとうございます", "ありがとうございます", "ありがとうございます"), Choice("すみません", "すみません", "すみません"),
                 Choice("こんにちは", "こんにちは", "こんにちは"), Choice("さようなら", "さようなら", "さようなら")],
        correct=0, explanation_vi="Câu cuối cùng của người đàn ông là 「ありがとうございます」.", explanation_en="The man's last line is 「ありがとうございます」(thank you).",
        tags=["listening", "n5"],
    ),
]

exam = Exam(
    id="exam.jlpt.n5.mock1",
    exam_type="jlpt", level="N5",
    title_vi="JLPT N5 - Đề luyện tập 1", title_en="JLPT N5 - Practice Test 1", title_ja="JLPT N5 模擬テスト1",
    description_vi="Đề luyện tập ngắn cho N5: chữ Hán/từ vựng, ngữ pháp/đọc hiểu và nghe hiểu (nghe được mô phỏng bằng văn bản).",
    description_en="A short N5 practice set: Vocabulary/Kanji, Grammar/Reading and Listening (listening is text-simulated).",
    sections=[
        Section("n5_vocab", "jlpt_vocab", "文字・語彙", "Vocabulary / Kanji", "文字・語彙",
                time_limit_seconds=300, score_scale_max=60, questions=vocab_questions),
        Section("n5_grammar_reading", "jlpt_grammar_reading", "文法・読解", "Grammar / Reading", "文法・読解",
                time_limit_seconds=600, score_scale_max=60,
                passages=[reading_passage], questions=grammar_questions + reading_questions),
        Section("n5_listening", "jlpt_listening", "聴解", "Listening", "聴解",
                time_limit_seconds=300, score_scale_max=60,
                passages=[listening_passage], questions=listening_questions),
    ],
    jlpt_total_pass=80, jlpt_section_pass=19,
)

if __name__ == "__main__":
    exam.write(OUT, name="jlpt_n5_mock_1")
    print("Wrote", os.path.abspath(OUT))
