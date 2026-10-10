# Tiến độ và deadline đề xuất

Cơ sở lập kế hoạch: trạng thái repo và tài liệu đến ngày 2026-10-10. Các ngày dưới đây là **mốc đề xuất**, chưa phải cam kết phát hành.

| ID | Hạng mục | Trạng thái | Tiến độ tham chiếu | Deadline đề xuất | Tiêu chí đóng |
|---|---|---:|---:|---:|---|
| P0-01 | Ổn định input/camera/cursor và click-to-move | Đang làm | 65% | 2026-10-12 | Điều khiển chuột/phím không xung đột; regression Play Mode đạt |
| P0-02 | Chạy full campaign và ghi lỗi theo scenario | Đang làm | 60% | 2026-10-14 | Hoàn thành toàn tuyến, có log và danh sách lỗi phân mức |
| P0-03 | Regression toàn bộ zone, cổng, spawn, collider | Đang làm | 70% | 2026-10-15 | Vào/ra mọi zone đúng vị trí; không kẹt collider |
| P0-04 | Ổn định tuyến ga ↔ Đảo Xanh/Midori Island | Đang làm | 65% | 2026-10-16 | Đi, quay về, lưu trạng thái và HUD hoạt động đúng |
| P0-05 | Hoàn tất/kiểm chứng engine IELTS Listening | Đang làm | 55% | 2026-10-17 | Audio, timer, submit, scoring, result và persistence đạt |
| P0-06 | Kiểm chứng save/load và offline fallback | Chưa đóng | 50% | 2026-10-18 | Tiến trình, quest, inventory, exam khôi phục đúng; mất mạng không chặn game |
| P1-01 | Hoàn thiện UI responsive 1024×600, 1366×768, 1920×1080 | Đang làm | 75% | 2026-10-19 | Không tràn/cắt text, dialog và HUD |
| P1-02 | Cân bằng nhu cầu sống, stamina và nhịp ngày/đêm | Đang làm | 70% | 2026-10-20 | Tham số được chốt, state transition và feedback rõ |
| P1-03 | Hoàn thiện nội dung JLPT/IELTS và review ngôn ngữ | Đang làm | 60% | 2026-10-22 | Đủ bộ câu hỏi mục tiêu, đáp án/giải thích được review |
| P1-04 | Kiểm chứng Supabase auth/sync/leaderboard | Chưa đóng | 55% | 2026-10-23 | Test tài khoản thật, conflict/fallback và quyền truy cập đạt |
| P1-05 | Kiểm chứng Gemini pronunciation/NPC/Writing | Chưa đóng | 60% | 2026-10-24 | Có timeout, error handling, giới hạn chi phí và log hợp lệ |
| P1-06 | Kiểm chứng Agora trên hai máy | Chưa làm | 35% | 2026-10-25 | Join/leave/reconnect/audio-video trên hai thiết bị đạt |
| P2-01 | Tối ưu hiệu năng và kiểm tra build mục tiêu | Chưa đóng | 45% | 2026-10-27 | Chốt FPS/memory/load-time và không có lỗi P0 |
| P2-02 | UAT vòng 1 + triage | Chưa làm | 10% | 2026-10-29 | Test case BA được ký, lỗi có owner và due date |
| P2-03 | Release candidate + checklist bàn giao | Chưa làm | 5% | 2026-10-31 | Build RC, release notes, known issues và tài liệu vận hành đầy đủ |

## Quy tắc cập nhật

- Chỉ chuyển sang **Hoàn thành** khi tiêu chí đóng đạt và có bằng chứng kiểm thử tương ứng.
- Tiến độ phần trăm là ước lượng quản trị, không thay thế kết quả test.
- Nếu một hạng mục trễ, cập nhật lý do, tác động, owner và deadline mới; không xóa lịch sử.
- Mốc P0 phải hoàn tất trước UAT; P1 chưa đạt cần được ghi rõ thành known issue và Product Owner chấp thuận.
