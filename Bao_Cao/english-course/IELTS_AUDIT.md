# IELTS local package audit — 2026-10-25

Phạm vi: `NihongoLife/LocalContent/IELTS` (gitignored, không phát hành).

## Kết quả

- 48 package có `test.json` và `key.json`.
- 24 package Listening, mỗi đề 4 part và phủ đủ câu 1–40.
- 24 package Academic Reading, mỗi đề 3 passage và phủ đủ câu 1–40.
- Cả 48 package có đúng 40 số câu trong widget và 40 số câu trong answer key.
- Không có đường dẫn audio Listening được tham chiếu nhưng bị thiếu.
- Tất cả package được đánh dấu `localOnly: true`.

| Nguồn local | Test | Listening | Reading | Trạng thái |
|---|---|---:|---:|---|
| Cambridge 12 | 5–8 | 4 | 4 | Đạt kiểm tra cấu trúc |
| Cambridge 13 | 1–4 | 4 | 4 | Đạt kiểm tra cấu trúc |
| Cambridge 14 | 1–4 | 4 | 4 | Đạt kiểm tra cấu trúc |
| Cambridge 15 | 1–4 | 4 | 4 | Đạt kiểm tra cấu trúc |
| Cambridge 16 | 1–4 | 4 | 4 | Đạt kiểm tra cấu trúc |
| Cambridge 17 | 1–4 | 4 | 4 | Đạt kiểm tra cấu trúc |

## Ranh giới bản quyền

Audit chỉ ghi metadata và tính toàn vẹn; không sao chép câu hỏi, passage, đáp án hoặc audio vào báo cáo/Git. Các package này chỉ được dùng cục bộ khi chủ dự án có quyền sử dụng. Bộ đề phát hành công khai phải là nội dung tự soạn hoặc có giấy phép phân phối phù hợp.

## Kiểm tra đã thực hiện

Với mỗi package: parse JSON, đếm `parts`, hợp nhất dải `group.from..group.to`, kiểm tra số câu trong `key.answers`, kiểm tra file audio tương đối tồn tại và đọc cờ `localOnly`.
