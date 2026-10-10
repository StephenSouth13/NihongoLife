# Phạm vi và yêu cầu BA

## 1. In scope

| Nhóm | Năng lực |
|---|---|
| Onboarding | Chọn ngôn ngữ/nhân vật, bắt đầu game, hướng dẫn điều khiển |
| World navigation | Di chuyển, camera, bản đồ, waypoint, cổng zone và tàu |
| Learning | Scenario, hội thoại, đáp án, feedback, retry, mastery và scoring |
| Daily-life gameplay | Konbini, sushi, phòng trọ, chỉ số sống, hành trang |
| Assessment | JLPT/IELTS, kết quả, nhận xét, lưu lịch sử |
| Mini-game | Kana Match, điểm/combo/sao/vé thưởng |
| Progression | Quest, story flags, unlock, save/load và hồ sơ |
| Online/AI | Supabase, Gemini, Agora với fallback phù hợp |
| Quality | Play Mode, collision, responsive UI, hiệu năng và regression evidence |

## 2. Ngoài phạm vi hoặc cần quyết định riêng

- Nội dung vượt quá N5 nếu chưa có learning design và tiêu chí chấm tương ứng.
- Multiplayer/co-op phát hành thật khi chưa hoàn tất kiểm thử nhiều máy và chính sách an toàn.
- Scene Unity mới khi Product Owner chưa phê duyệt chính xác scene đó.
- Thay thế toàn bộ môi trường hiện có bằng một hierarchy chồng lên môi trường cũ.

## 3. Actor chính

| Actor | Mục tiêu |
|---|---|
| Người học/người chơi | Khám phá, học, làm nhiệm vụ, thi và theo dõi tiến bộ |
| NPC | Cung cấp ngữ cảnh, hội thoại, phản hồi và nhiệm vụ |
| Giáo viên/content author | Soạn scenario, câu hỏi, learning target và phản hồi |
| Product Owner/BA | Quản lý phạm vi, acceptance criteria, ưu tiên và release readiness |
| Dịch vụ cloud | Xác thực, lưu/sync, leaderboard, AI feedback và RTC |

## 4. Yêu cầu phi chức năng

- **Usability:** thao tác chính có prompt; cửa sổ có cách đóng; text không bị cắt ở các độ phân giải mục tiêu.
- **Reliability:** offline-first cho vòng chơi cốt lõi; lỗi dịch vụ ngoài có thông báo và fallback.
- **Performance:** không có giật kéo dài khi di chuyển, hội thoại hoặc chuyển zone; cần chốt target FPS theo thiết bị.
- **Data integrity:** save có versioning; sync tránh ghi đè tiến trình mới hơn; kết quả thi có tính truy vết.
- **Localization:** chuỗi người dùng không hard-code ngoài cơ chế đã chấp thuận; tiếng Nhật cần bước review ngôn ngữ.
- **Security/privacy:** không lưu API key trong repo; dữ liệu tài khoản và bài làm tuân theo quyền truy cập tối thiểu.
- **Maintainability:** nội dung học ưu tiên data-driven; thay đổi scene/prefab phải được lưu trực tiếp vào asset hoàn chỉnh.

## 5. Definition of Done cấp tính năng

- [ ] Yêu cầu, actor, precondition và acceptance criteria đã rõ.
- [ ] Happy path và các lỗi dự kiến đã được xử lý.
- [ ] UI/localization không bị cắt ở độ phân giải mục tiêu.
- [ ] Save/load hoặc reset state đã được kiểm tra nếu tính năng có dữ liệu.
- [ ] Play Mode visual và collision test đã chạy nếu ảnh hưởng scene/gameplay.
- [ ] Regression liên quan đã chạy và có ảnh/log làm bằng chứng.
- [ ] Tài liệu BA, timeline và sơ đồ liên quan được cập nhật.
