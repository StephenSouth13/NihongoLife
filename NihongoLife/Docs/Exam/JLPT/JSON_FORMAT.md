# Chuẩn Cấu Trúc JSON Đề Thi JLPT / IELTS (NihongoLife)

Để Import thành công đề thi (từ PDF) vào Game NihongoLife thông qua tool `NihongoLife -> Exam -> Import Exam from JSON`, bạn cần yêu cầu AI (ChatGPT/Claude) chuyển đổi PDF theo ĐÚNG định dạng JSON sau.

Lưu ý: 
- `tags`: Dùng để lưu trữ dạng bài (Mondai 1, Mondai 5, Ngữ pháp, Từ vựng). Đây là keyword **cực kỳ quan trọng** để AI sau này phân tích điểm mạnh/yếu của thí sinh.
- `correctChoiceIndex`: Bắt đầu từ `0` (Đáp án A = 0, B = 1, C = 2, D = 3).
- `type`: Xem quy ước ở dưới.

### Quy ước các Enum (Số nguyên)
**ExamType**: 
- `0`: JLPT
- `1`: IELTS

**ExamSectionType**:
- `0`: JlptVocabulary (Từ vựng/Chữ Hán)
- `1`: JlptGrammarReading (Ngữ pháp/Đọc hiểu)
- `2`: JlptListening (Nghe hiểu)

**ExamQuestionType**:
- `0`: MultipleChoice (Trắc nghiệm 4 đáp án)
- `1`: TrueFalseNotGiven (IELTS)
- `2`: FillBlank
- `3`: Essay (Viết luận - tự luận)
- `4`: SpeakingPrompt (Ghi âm tự luận)

---

### Mẫu JSON Prompt cho AI
Copy nguyên văn mẫu JSON này ném cho ChatGPT/Claude kèm với file PDF:

```json
{
  "id": "JLPT_N4_2024_07",
  "examType": 0,
  "level": "N4",
  "learnerLevel": "Elementary",
  "titleJa": "N4 2024年 7月",
  "titleVi": "Đề thi thật N4 Tháng 7/2024",
  "descriptionVi": "Đề thi N4 tiêu chuẩn",
  "jlptTotalPassScore": 90,
  "jlptSectionPassScore": 19,
  "sections": [
    {
      "id": "sec_vocab_n4_2024",
      "type": 0,
      "titleJa": "言語知識（文字・語彙）",
      "titleVi": "Kiến thức ngôn ngữ (Từ vựng)",
      "timeLimitSeconds": 1800,
      "scoreScaleMax": 60,
      "passages": [],
      "questions": [
        {
          "id": "q1_vocab",
          "type": 0,
          "passageId": "",
          "promptJa": "Mondai 1: 漢字の読み方...",
          "promptVi": "Câu 1: Cách đọc Kanji của từ gạch chân...",
          "choices": [
            { "textJa": "あ" },
            { "textJa": "い" },
            { "textJa": "う" },
            { "textJa": "え" }
          ],
          "correctChoiceIndex": 1,
          "explanationVi": "Chữ Hán này được đọc là 'i'...",
          "tags": ["Mondai1_Kanji", "Vocabulary"],
          "points": 1
        }
      ]
    },
    {
      "id": "sec_reading_n4_2024",
      "type": 1,
      "titleJa": "読解",
      "titleVi": "Đọc hiểu",
      "timeLimitSeconds": 3600,
      "scoreScaleMax": 60,
      "passages": [
        {
          "id": "pass_read_1",
          "titleJa": "問題 4",
          "bodyReading": "これはテストの文章です..."
        }
      ],
      "questions": [
        {
          "id": "q1_read",
          "type": 0,
          "passageId": "pass_read_1",
          "promptJa": "筆者が言いたいことは何ですか。",
          "promptVi": "Tác giả muốn nói điều gì?",
          "choices": [
            { "textJa": "Đáp án 1" },
            { "textJa": "Đáp án 2" }
          ],
          "correctChoiceIndex": 0,
          "tags": ["Mondai4_Dokkai", "Reading"],
          "points": 2
        }
      ]
    }
  ]
}
```
