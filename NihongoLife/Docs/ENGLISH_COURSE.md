# English Course — từ nền tảng đến luyện thi

NihongoLife có hai nhánh tiếng Anh bổ trợ nhau:

1. **English Foundations**: học tiếng Anh tổng quát theo CEFR A1–C1 bằng mẫu câu, ngữ pháp, điền từ, đọc hiểu và nghe.
2. **IELTS Practice**: làm đề Listening, Reading, Writing và Speaking theo giao diện luyện tập/thi thử.

## Starter curriculum trong build

`EnglishCourseCatalog` chứa **20 unit tự soạn, 60 câu**, phủ ma trận **5 cấp CEFR × 4 kỹ năng**. Đây là nội dung seed để người chơi luôn có thể học offline; không trích từ sách IELTS hoặc giáo trình thương mại.

| Quyển / chặng | Listening | Speaking | Reading | Writing |
|---|---|---|---|---|
| A1 · First Steps | Lời chào, tên, số | Giới thiệu, yêu cầu lịch sự | Biển báo, tin nhắn | `be`, hiện tại đơn |
| A2 · Everyday Life | Thông báo, giờ, địa điểm | Mua sắm, hỏi đường, hẹn gặp | Email, hướng dẫn | Giới từ, lượng từ, present perfect cơ bản |
| B1 · Independent User | Cuộc gọi, phỏng vấn | Kinh nghiệm, ý kiến, bất đồng lịch sự | Ý chính và nghĩa theo ngữ cảnh | Kể chuyện, mệnh đề quan hệ |
| B2 · Confident Communicator | Thái độ và hàm ý | Thảo luận, thuyết trình, làm rõ | Báo cáo và lập luận | Từ nối, collocation |
| C1 · Academic Explorer | Bài giảng và stance | Hedging, inversion, nhấn mạnh | Đọc phản biện | Thesis, synthesis, viết cô đọng |

`EnglishPracticeUI` render từng câu, bốn lựa chọn, phản hồi đúng/sai, giải thích và tổng kết. Exam Center có bộ lọc Nghe/Nói/Đọc/Viết và card ghi rõ cấp độ, kỹ năng, trọng tâm.

### Audio nghe

- `Tools/english/audio_manifest.json` là script lời thoại tự soạn.
- `Tools/english/generate_audio.ps1` tạo 15 WAV offline vào `LocalContent/English/audio/`.
- `EnglishAudioLibrary` ưu tiên audio local trên desktop; nếu thiếu, WebGL dùng Web Speech API; transcript luôn có để hỗ trợ tiếp cận.
- `LocalContent/English/` đã được `.gitignore`, vì vậy 15 WAV (~5,45 MB hiện tại) không đi vào commit.
- Bản production nên thay giọng hệ thống bằng audio người thật hoặc audio có giấy phép, nén OGG/MP3 và phân phối qua CDN theo `EXAM_CONTENT_PIPELINE.md`.

Tạo lại audio local:

```powershell
powershell -ExecutionPolicy Bypass -File NihongoLife/Tools/english/generate_audio.ps1
```

## Nguồn tham khảo phương pháp

Lộ trình tham khảo mô tả CEFR theo kỹ năng của British Council LearnEnglish, cấu trúc khóa beginner của VOA Learning English và kho tài liệu bốn kỹ năng của American English. Nội dung câu hỏi, passage và lời thoại trong game do dự án tự soạn; không sao chép các bài nguồn.

## Kho IELTS cục bộ

Package sách có bản quyền nằm trong `LocalContent/IELTS/<package>/`, bị Git ignore và không được đưa vào build công khai. Mỗi đề Reading/Listening cần:

- `test.json` theo schema `nihongolife.ielts.v1`;
- `key.json` tách khỏi đề;
- đúng một widget và một đáp án cho từng câu 1–40;
- Listening có audio theo từng part, đường dẫn tương đối không thoát khỏi package;
- provenance/source rõ ràng và `localOnly: true` với tài liệu không được phát hành.

Các đề tự soạn được phép phát hành đặt trong `StreamingAssets/IELTS`; nội dung lấy từ Cambridge hoặc nguồn thương mại chỉ dùng cục bộ khi chủ dự án có quyền sử dụng.

## Mở rộng tiếp theo

- Chuyển catalog lớn sang package JSON có version, thay vì tăng kích thước class seed.
- Lưu mastery riêng theo kỹ năng và CEFR cho từng tài khoản.
- Bổ sung audio người thật có checksum, transcript và tốc độ nghe.
- Thêm bài viết/nói English tổng quát trước khi đưa người học sang rubric IELTS.
- Dùng placement test để đề xuất điểm bắt đầu, không khóa người chơi khỏi nội dung dễ hơn.
