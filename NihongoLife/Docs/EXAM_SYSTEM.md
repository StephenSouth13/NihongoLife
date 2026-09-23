# Hệ thống luyện thi JLPT / IELTS

Trạng thái: **kiến trúc + UI + 2 đề mẫu đã viết xong, chưa từng chạy Play Mode** (Unity đang mở/rebuild ở máy chủ dự án trong lúc phần này được viết — xem `Docs/TEAM_TASKS.md` TASK-K). Đọc mục "Giới hạn trung thực" bên dưới trước khi quảng bá tính năng này là "chấm điểm JLPT/IELTS thật".

## Vì sao có tính năng này

Chủ dự án yêu cầu (2026-09-23): mở rộng game thành nơi luyện thi JLPT và IELTS hoàn chỉnh, không chỉ là life-sim học từ vựng rời rạc. Hệ thống này độc lập với hệ scenario/quest cốt truyện — vào bất cứ lúc nào qua phím **K** hoặc nút trong menu, không cần hoàn thành cốt truyện trước.

## Kiến trúc

```
ExamModels.cs        ExamType, ExamSectionType, ExamQuestionType, ExamPassage, ExamChoice,
                      ExamQuestion, ExamSection, ExamDefinition (ScriptableObject)
ExamResultDto.cs      ExamSectionResult, ExamQuestionRecord, ExamAttemptRecord (lưu vào
                      PlayerProgressDto.examAttempts — tự động qua local + cloud save có sẵn),
                      ExamAttemptResult (kết quả 1 lượt thi, truyền cho màn kết quả)
ExamRepository.cs     IGameService — load mọi ExamDefinition trong Resources/Exams
ExamGradingService.cs IGameService — chấm Essay/Speaking bằng Gemini (có rubric fallback khi
                      chưa cấu hình Gemini)
ExamManager.cs        IGameService, singleton Instance — engine chạy 1 lượt thi: điều hướng
                      câu hỏi, khoá từng phần sau khi nộp, đếm giờ, chấm điểm, lưu kết quả
ExamCenterPopup.cs    UI chọn đề (tab JLPT/IELTS), xem điểm cao nhất, bấm Bắt đầu (phím K)
ExamPlayUI.cs         UI làm bài: câu hỏi/đáp án theo từng loại, bảng câu hỏi, đồng hồ đếm
                      giờ, màn kết quả + xem lại đáp án
```

Đăng ký ở `AppRoot.InitializeServices()` bước "9. Exam Center" — độc lập hoàn toàn với `ScenarioCampaignManager`.

## Data-driven: thêm đề thi mới không cần sửa code

Một `ExamDefinition` là 1 file `.asset` trong `Assets/NihongoLife/Resources/Exams/`. `ExamRepository` tự động `Resources.LoadAll<ExamDefinition>("Exams")` — thả file mới vào là `ExamCenterPopup` tự hiện thêm thẻ đề thi, không cần đụng code UI.

Vì cấu trúc lồng sâu (sections → passages/questions → choices) khó viết tay bằng YAML mà không lỗi, dùng generator Python giống `Tools/story/scen_lib.py`:

- `Tools/exam/exam_lib.py` — thư viện dựng `Exam`/`Section`/`Passage`/`Question`/`Choice` rồi `exam.write(path, name)` ra đúng định dạng Unity YAML (thuần LF, không CRLF — bài học rút ra từ sự cố hỏng file `90_TestSandbox.unity` do lẫn 1 dòng `\n` giữa file CRLF).
- `Tools/exam/gen_jlpt_n5_mock1.py` → `Resources/Exams/jlpt_n5_mock_1.asset`
- `Tools/exam/gen_ielts_academic_practice1.py` → `Resources/Exams/ielts_academic_practice_1.asset`

Thêm đề mới: viết 1 file `gen_*.py` mới dùng `exam_lib`, chạy `python Tools/exam/gen_xxx.py`, tạo `.meta` cho file `.asset` mới (2 dòng `fileFormatVersion`/`guid` theo `NativeFormatImporter`, xem file `.meta` cạnh 2 đề mẫu). Không cần mở Unity Editor để thêm đề — đúng tinh thần AGENTS.md "không cần bước setup thủ công".

## Nội dung đã có

- **`exam.jlpt.n5.mock1`** — N5, 3 phần: 文字・語彙 (6 câu đọc kanji), 文法・読解 (4 câu ngữ pháp điền trợ từ + 1 bài đọc ngắn + 2 câu hiểu bài đọc), 聴解 (1 hội thoại mô phỏng bằng văn bản + 2 câu, giới hạn 2 lượt "nghe"). Ngưỡng đậu dùng đúng chuẩn N5 thật: mỗi phần ≥19/60, tổng ≥80/180.
- **`exam.ielts.academic.practice1`** — Academic, 4 kỹ năng theo đúng thứ tự thật: Listening (hội thoại mô phỏng, 1 lượt nghe), Reading (1 bài + 3 câu True/False/Not Given + 2 trắc nghiệm), Writing (Task 2, tối thiểu 250 từ), Speaking (Part 2 cue card, ghi âm).

Cả hai là **đề luyện tập rút gọn** (ít câu hơn đề thi thật) để demo toàn bộ luồng — không phải đề thi đầy đủ chuẩn thời lượng thật.

## Cách làm bài (ExamPlayUI)

- Điều hướng theo câu hỏi trong phần hiện tại (Trước/Sau + bảng số câu, màu xanh = đã trả lời, vàng = câu hiện tại); không quay lại phần đã nộp.
- Mỗi phần có đồng hồ đếm giờ riêng (nếu `timeLimitSeconds > 0`); hết giờ tự khoá phần và chuyển tiếp — giống thi thật.
- Loại câu hỏi:
  - **MultipleChoice / TrueFalseNotGiven**: bấm chọn, đổi được cho tới khi nộp phần.
  - **FillBlank**: gõ, so khớp không phân biệt hoa/thường/khoảng trắng với danh sách đáp án chấp nhận.
  - **Essay**: khung nhập nhiều dòng + đếm từ; nội dung được lưu khi chuyển câu/nộp phần (không lưu từng phím gõ, tránh việc UI tự vẽ lại làm mất focus khi đang gõ).
  - **SpeakingPrompt**: ghi âm bằng microphone (`Microphone.Start/End`, tối đa 120 giây), không phát lại qua AI mà gửi thẳng file WAV cho Gemini để lấy transcript + chấm nội dung.
- Đóng màn hình giữa chừng rồi mở lại (qua `ExamCenterPopup`) sẽ **tiếp tục** đúng lượt thi đang làm, không mất dữ liệu — `ExamManager` là service sống suốt phiên chơi, không phụ thuộc UI đang mở hay đóng.

## Giới hạn trung thực (đọc trước khi quảng bá tính năng)

1. **Đây không phải điểm JLPT/IELTS chính thức**, không liên kết với Japan Foundation, British Council, IDP hay Cambridge. `ExamAttemptRecord.estimatedBand`/`passed` là mô hình chấm điểm riêng của dự án, ghi rõ trong `ExamModels.cs`/`ExamResultDto.cs` và hiển thị lại cho người chơi ở màn kết quả (`ExamPlayUI.ShowResults`).
2. **Speaking chỉ chấm được nội dung**, không chấm được phát âm/ngữ điệu/độ trôi chảy thật — vì AI chỉ nhận được văn bản chuyển từ giọng nói (transcript), không "nghe" được cách nói. Đã ghi rõ trong prompt gửi AI, trong `ExamGradingService` và hiển thị cảnh báo ngay trên màn ghi âm.
3. **Khi chưa cấu hình Gemini** (`GameControlDatabase.enableGeminiConversation`/`allowGeminiDirectClientCalls`/biến môi trường API key), Essay/Speaking rơi về "chấm tạm" dựa trên độ dài bài viết — không phải đánh giá chất lượng thật, có ghi rõ trong feedback trả về cho người chơi.
4. **Nghe hiểu (Listening) đang mô phỏng bằng văn bản**, chưa có audio thật — người chơi đọc transcript thay vì nghe. Giới hạn "số lượt nghe" (`maxPlays`) vẫn được áp dụng để giữ kỷ luật luyện tập giống thi thật, nhưng đây là mô phỏng, không phải audio thật.
5. **Chưa chạy Play Mode lần nào** — xem TASK-K trong `Docs/TEAM_TASKS.md`. Đã kiểm tra: build YAML của 2 file `.asset` hợp lệ (PyYAML parse thành công, không lệch `passageId`/`correctChoiceIndex`), guid `.meta` không trùng file nào khác trong project, code đã được review thủ công kỹ theo đúng API thật của từng lớp — nhưng **chưa được trình biên dịch Unity xác nhận** (lúc viết phần này, `Library/ScriptAssemblies` của máy chủ dự án đang thiếu DLL TextMeshPro/InputSystem/UGUI vì Unity Editor đang mở/rebuild, nên không tự kiểm tra biên dịch offline được).

## Việc còn lại

Xem TASK-K trong `Docs/TEAM_TASKS.md`.
