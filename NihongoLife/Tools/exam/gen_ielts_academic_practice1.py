# -*- coding: utf-8 -*-
"""Generates Assets/NihongoLife/Resources/Exams/ielts_academic_practice_1.asset — one Reading passage
(True/False/Not Given + MCQ), one simulated Listening conversation, a Writing Task 2 essay and a
Speaking Part 2 cue card. Run from anywhere:
    python Tools/exam/gen_ielts_academic_practice1.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from exam_lib import Exam, Section, Passage, Question, Choice

OUT = os.path.join(os.path.dirname(__file__), "..", "..",
                    "Assets", "NihongoLife", "Resources", "Exams", "ielts_academic_practice_1.asset")

reading_passage = Passage(
    id="ielts_passage_gardens",
    title_vi="Bài đọc: Vườn cộng đồng", title_en="Reading: Community Gardens",
    body_en="Community gardens have become increasingly popular in cities around the world. These shared plots of "
            "land allow residents, especially those living in apartments without private outdoor space, to grow "
            "their own vegetables, fruits, and flowers. Beyond the practical benefit of fresh produce, community "
            "gardens create opportunities for neighbours to meet and cooperate, often strengthening a sense of "
            "belonging within the local area. Studies have also linked gardening activities to reduced stress and "
            "improved mental well-being. However, community gardens are not without challenges: disputes over "
            "shared resources such as water and tools are common, and long waiting lists mean that not everyone "
            "who wants a plot can obtain one quickly.",
    body_vi="Vườn cộng đồng ngày càng phổ biến ở các thành phố trên khắp thế giới. Những mảnh đất chung này cho phép "
            "cư dân, đặc biệt là những người sống trong căn hộ không có không gian ngoài trời riêng, tự trồng rau, "
            "trái cây và hoa. Ngoài lợi ích thực tế là có nông sản tươi, vườn cộng đồng còn tạo cơ hội để hàng xóm "
            "gặp gỡ và hợp tác, thường củng cố cảm giác gắn kết trong khu vực. Các nghiên cứu cũng cho thấy hoạt "
            "động làm vườn giúp giảm căng thẳng và cải thiện sức khỏe tinh thần. Tuy nhiên, vườn cộng đồng cũng có "
            "thách thức: tranh chấp về tài nguyên chung như nước và dụng cụ là chuyện thường gặp, và danh sách chờ "
            "dài khiến không phải ai muốn có một mảnh đất cũng có thể nhận được ngay.",
)

reading_questions = [
    Question(
        id="ielts_r_q1", type="tfng", passage_id="ielts_passage_gardens",
        prompt_en="Community gardens exist only in large cities.",
        choices=[Choice("True", "True"), Choice("False", "False"), Choice("Not Given", "Not Given")],
        correct=2,
        explanation_vi="Bài đọc nói \"around the world\" nhưng không đề cập đến quy mô thành phố.",
        explanation_en="The passage says \"around the world\" but never specifies city size, so this is Not Given.",
        tags=["reading", "tfng"],
    ),
    Question(
        id="ielts_r_q2", type="tfng", passage_id="ielts_passage_gardens",
        prompt_en="Gardening has been linked to lower stress levels.",
        choices=[Choice("True", "True"), Choice("False", "False"), Choice("Not Given", "Not Given")],
        correct=0,
        explanation_vi="Bài đọc nói rõ: \"linked gardening activities to reduced stress\".",
        explanation_en="The passage explicitly states gardening is \"linked ... to reduced stress\".",
        tags=["reading", "tfng"],
    ),
    Question(
        id="ielts_r_q3", type="tfng", passage_id="ielts_passage_gardens",
        prompt_en="Disputes over shared resources never occur in community gardens.",
        choices=[Choice("True", "True"), Choice("False", "False"), Choice("Not Given", "Not Given")],
        correct=1,
        explanation_vi="Bài đọc nói ngược lại: \"disputes ... are common\".",
        explanation_en="The passage contradicts this: \"disputes ... are common\".",
        tags=["reading", "tfng"],
    ),
    Question(
        id="ielts_r_q4", type="mc", passage_id="ielts_passage_gardens",
        prompt_en="According to the passage, what is one social benefit of community gardens?",
        choices=[Choice("They increase property values", "They increase property values"),
                 Choice("They help neighbours meet and cooperate", "They help neighbours meet and cooperate"),
                 Choice("They reduce city traffic", "They reduce city traffic"),
                 Choice("They provide free tools", "They provide free tools")],
        correct=1,
        explanation_vi="Bài đọc nói vườn cộng đồng \"create opportunities for neighbours to meet and cooperate\".",
        explanation_en="The passage says gardens \"create opportunities for neighbours to meet and cooperate\".",
        tags=["reading", "mc"],
    ),
    Question(
        id="ielts_r_q5", type="mc", passage_id="ielts_passage_gardens",
        prompt_en="What problem does the passage mention regarding community gardens?",
        choices=[Choice("They require expensive land", "They require expensive land"),
                 Choice("Long waiting lists for plots", "Long waiting lists for plots"),
                 Choice("They are illegal in many countries", "They are illegal in many countries"),
                 Choice("They only grow flowers", "They only grow flowers")],
        correct=1,
        explanation_vi="Bài đọc nói \"long waiting lists mean that not everyone ... can obtain one quickly\".",
        explanation_en="The passage says \"long waiting lists mean that not everyone ... can obtain one quickly\".",
        tags=["reading", "mc"],
    ),
]

listening_passage = Passage(
    id="ielts_listen_hotel",
    title_vi="Hội thoại: Đặt phòng khách sạn", title_en="Conversation: Booking a hotel room",
    body_en="Woman: Good afternoon, Lakeside Hotel, how can I help you?\n"
            "Man: Hi, I'd like to book a room for two nights, please, from the 10th to the 12th of June.\n"
            "Woman: Certainly. Would you prefer a single or a double room?\n"
            "Man: A double room, please, with a view of the lake if possible.\n"
            "Woman: We do have one available. That will be 90 pounds per night, so 180 pounds in total. "
            "Could I have your name, please?\n"
            "Man: Yes, it's James Carter.",
    body_vi="Nữ: Chào buổi chiều, khách sạn Lakeside, tôi có thể giúp gì cho anh?\n"
            "Nam: Chào chị, tôi muốn đặt phòng hai đêm, từ ngày 10 đến ngày 12 tháng 6.\n"
            "Nữ: Vâng ạ. Anh muốn phòng đơn hay phòng đôi?\n"
            "Nam: Cho tôi phòng đôi, nếu có thể nhìn ra hồ.\n"
            "Nữ: Chúng tôi có một phòng như vậy. Giá 90 bảng một đêm, tổng cộng 180 bảng. Cho tôi xin tên anh?\n"
            "Nam: Vâng, tôi là James Carter.",
    max_plays=1,
)

listening_questions = [
    Question(
        id="ielts_l_q1", type="mc", passage_id="ielts_listen_hotel",
        prompt_en="What type of room does the man book?",
        choices=[Choice("Single", "Single"), Choice("Double", "Double"), Choice("Twin", "Twin"), Choice("Family", "Family")],
        correct=1,
        explanation_vi="Người đàn ông nói: \"A double room, please\".",
        explanation_en="The man says: \"A double room, please\".",
        tags=["listening", "mc"],
    ),
    Question(
        id="ielts_l_q2", type="fill", passage_id="ielts_listen_hotel",
        prompt_en="What is the total cost mentioned for the stay?",
        accepted=["180 pounds", "£180", "180"],
        explanation_vi="Nhân viên nói: \"90 pounds per night, so 180 pounds in total\".",
        explanation_en="The receptionist says: \"90 pounds per night, so 180 pounds in total\".",
        tags=["listening", "fill"],
    ),
    Question(
        id="ielts_l_q3", type="mc", passage_id="ielts_listen_hotel",
        prompt_en="What is the man's name?",
        choices=[Choice("James Carter", "James Carter"), Choice("James Cartier", "James Cartier"),
                 Choice("Jason Carter", "Jason Carter"), Choice("James Carker", "James Carker")],
        correct=0,
        explanation_vi="Người đàn ông nói: \"it's James Carter\".",
        explanation_en="The man says: \"it's James Carter\".",
        tags=["listening", "mc"],
    ),
]

writing_question = Question(
    id="ielts_w_task2", type="essay",
    prompt_en="Writing Task 2: Some people believe that university education should be free for all students, "
              "while others think students should pay tuition fees. Discuss both views and give your own opinion.",
    task_vi="Thảo luận cả hai quan điểm và nêu ý kiến riêng của bạn. Viết ít nhất 250 từ.",
    task_en="Discuss both views and give your own opinion. Write at least 250 words.",
    min_words=250, time_limit=2400,
    explanation_vi="Được chấm bởi AI (hoặc quy tắc dự phòng khi chưa cấu hình AI) theo 4 tiêu chí IELTS Writing: Task Response, Coherence and Cohesion, Lexical Resource, Grammatical Range and Accuracy.",
    explanation_en="Graded by AI (or a transparent fallback rubric when AI is not configured) against the four IELTS Writing criteria: Task Response, Coherence and Cohesion, Lexical Resource, Grammatical Range and Accuracy.",
    tags=["writing", "task2"],
)

speaking_question = Question(
    id="ielts_s_part2", type="speaking",
    prompt_en="Speaking Part 2: Describe a skill you would like to learn in the future.",
    task_vi="Bạn nên nói về: kỹ năng đó là gì, tại sao bạn muốn học, bạn sẽ học như thế nào, và kỹ năng này sẽ giúp ích cho bạn ra sao trong tương lai. Bạn có 1 phút chuẩn bị và nên nói trong 1-2 phút.",
    task_en="You should say: what the skill is, why you want to learn it, how you would learn it, and explain how "
            "this skill would help you in the future. You have 1 minute to prepare and should speak for 1-2 minutes.",
    time_limit=120,
    explanation_vi="Điểm Speaking chỉ dựa trên nội dung bản ghi âm được chuyển thành văn bản — không thể đánh giá phát âm hay ngữ điệu qua bản ghi.",
    explanation_en="The Speaking score reflects transcript content only — pronunciation and intonation cannot be judged from a recording's transcript.",
    tags=["speaking", "part2"],
)

exam = Exam(
    id="exam.ielts.academic.practice1",
    exam_type="ielts", level="Academic",
    title_vi="IELTS Academic - Đề luyện tập 1", title_en="IELTS Academic - Practice Test 1",
    description_vi="Đề luyện tập ngắn: Đọc (1 bài + Đúng/Sai/Không có thông tin + trắc nghiệm), Nghe (mô phỏng bằng văn bản), Viết (Task 2) và Nói (Part 2).",
    description_en="A short practice set: Reading (1 passage + True/False/Not Given + MCQ), Listening (text-simulated), Writing (Task 2) and Speaking (Part 2).",
    sections=[
        Section("ielts_listening", "ielts_listening", "Listening", "Listening",
                time_limit_seconds=480, score_scale_max=60,
                passages=[listening_passage], questions=listening_questions),
        Section("ielts_reading", "ielts_reading", "Reading", "Reading",
                time_limit_seconds=1200, score_scale_max=60,
                passages=[reading_passage], questions=reading_questions),
        Section("ielts_writing", "ielts_writing", "Writing", "Writing",
                time_limit_seconds=2400, score_scale_max=9,
                questions=[writing_question]),
        Section("ielts_speaking", "ielts_speaking", "Speaking", "Speaking",
                time_limit_seconds=300, score_scale_max=9,
                questions=[speaking_question]),
    ],
)

if __name__ == "__main__":
    exam.write(OUT, name="ielts_academic_practice_1")
    print("Wrote", os.path.abspath(OUT))
