# Hệ thống luyện thi JLPT / IELTS

Trạng thái hiện tại: engine thi, UI, lưu kết quả, JLPT N5 mock 1–2 và runtime package IELTS Reading/Listening đã có trong project. Đây là hệ thống luyện tập của NihongoLife, không phải kỳ thi hoặc bảng điểm chính thức của Japan Foundation, British Council, IDP hay Cambridge.

## Kiến trúc chung

- `ExamDefinition` là ScriptableObject data-driven trong `Resources/Exams`; `ExamRepository` tự nạp toàn bộ đề.
- `ExamManager` quản lý lượt thi, timer từng phần, câu trả lời, khoá phần đã nộp, chấm điểm và lưu `examAttempts` qua hồ sơ local/cloud hiện hữu.
- `ExamCenterPopup` mở bằng bàn thi hoặc <kbd>K</kbd>, chia tab JLPT/IELTS, hiện điểm cao và cho làm tiếp lượt đang dở.
- `ExamPlayUI` render trắc nghiệm, True/False/Not Given, điền trống, bài viết và speaking prompt; đóng UI không xoá lượt thi trong phiên.

## JLPT N5

Project có `jlpt_n5_mock_1.asset` và `jlpt_n5_mock_2.asset`. Mock 2 là bản nâng cấp chính: 67 câu, chia 言語知識（文字・語彙・文法）・読解 và 聴解, có 24 file MP3 trong `Audio/JLPT/n5_mock2`, ẩn script lúc làm và chỉ hiện lại khi review. Audio nghe hiểu chỉ phát một lượt.

Điểm được quy đổi về 120 + 60 = 180. Điều kiện đậu mô phỏng là tổng từ 80/180 và đạt điểm sàn từng division. Báo cáo sau thi chỉ ra nhóm もんだい yếu để người học biết phần cần ôn lại.

`JlptExamTests` kiểm tra dữ liệu/đáp án và `JlptMockPlayModeTests` chạy luồng UI, audio, nộp bài, save, điểm division và báo cáo điểm yếu.

## IELTS Reading và Listening

NihongoLife hỗ trợ package schema `nihongolife.ielts.v1` được nạp lúc chạy từ `LocalContent/IELTS/<package>/`. Mỗi package gồm `test.json`, `key.json`, audio OGG và ảnh sơ đồ nếu có. Thư mục này bị Git ignore để nội dung sách có bản quyền không bị phát hành cùng mã nguồn.

- Reading/Listening hỗ trợ đủ 40 câu và nhiều kiểu group/widget.
- Chế độ luyện tập cho phép pause/seek audio; chế độ thi không cho pause/seek và tự chuyển part khi audio kết thúc.
- Reading có timer 60 phút và tự nộp khi hết giờ.
- Đáp án được chuẩn hoá hoa/thường, khoảng trắng, dấu gạch; hỗ trợ đáp án thay thế và giới hạn số từ.
- Lượt đang làm được lưu để tiếp tục; lúc thi, UI khoá di chuyển và các hotkey/HUD gây xao nhãng.
- `IeltsGrader` chấm raw 0–40 và quy đổi band riêng cho Academic Reading và Listening.

`IeltsReadingPlayModeTests`, `IeltsListeningPlayModeTests` và các test grader kiểm tra package, đủ widget/key, timer, audio, resume, submit, raw/band và phục hồi trạng thái người chơi. Nếu máy không có package cục bộ, test IELTS package sẽ được bỏ qua có chủ đích.

## Writing và Speaking

Đề seed Academic Practice 1 vẫn có Writing/Speaking trong engine chung. Gemini có thể hỗ trợ phản hồi nội dung khi được cấu hình; fallback chỉ dựa trên quy tắc đơn giản và phải được ghi là điểm luyện tập. Không tuyên bố hệ thống chấm chính xác phát âm, ngữ điệu hoặc band IELTS chính thức.

## Thêm nội dung

- JLPT: dùng `Tools/exam/exam_lib.py` và các generator `gen_jlpt_*.py` để tạo YAML Unity nhất quán.
- IELTS: dùng `Tools/exam/ielts_ocr.py` để OCR nguồn người dùng có quyền sử dụng; `ielts_pkg.py` dựng passage, question group, audio/ảnh, `test.json` và `key.json`, đồng thời kiểm tra mỗi câu 1–40 có đúng một widget và một đáp án.
- Quy tắc lưu trữ, audio/CDN và phát hành package nằm trong [EXAM_CONTENT_PIPELINE.md](EXAM_CONTENT_PIPELINE.md).
