# Bộ marketing NihongoLife

Anh Huy mở đúng một chỗ: thư mục tuần. Copy caption, đăng ảnh, không cần đụng code game.

Lịch bắt đầu **thứ Hai 26/10/2026**, hết **chủ Nhật 17/01/2027**.

## Cách đăng một bài Facebook

1. Mở `Tuan-XX/README.md` xem chủ đề tuần và việc của Long, Huy, Dũng.
2. Vào thư mục bài. Mở `caption.txt`, thay các mã link rồi copy.
3. Đăng `fb-1080.png` (vuông) hoặc `fb-1350.png` (dọc 4:5). Story dùng `story-1920.png` nếu thư mục có file đó.
4. Bài nào có `CAN_QUAY.md` thì ảnh tĩnh đăng được, video thì chưa. Đừng ghép cảnh giả.

Mã link nằm ở `LINKS.md`. Trang đội ngũ đã có URL thật. Link chơi, fanpage, Discord, Zalo chưa được bịa trong bộ này.

## Trong thư mục

| Chỗ | Việc |
|---|---|
| `Brand/` | Màu lấy từ UI game, font Montserrat (OFL), cách dùng logo, giọng văn |
| `KeHoach/` | Kế hoạch 12 tuần (Markdown + PDF) và lịch Excel từng ngày |
| `Tuan-01` … `Tuan-12` | Bài sẵn caption và ảnh |
| `PressKit/` | Mô tả Việt / Anh, tờ sự thật, chỗ trống tiểu sử |
| `Templates/` | HTML, CSS và script dựng lại ảnh cùng PDF |
| `Video/` | Video không nằm trong git. File nặng để ổ D |

## Dựng lại ảnh và PDF

Từ gốc repo:

```bash
python3 Marketing/Templates/regenerate.py
```

Script đọc `Templates/posts.py` và `KeHoach/KeHoach_Marketing_12_Tuan.md`, ghi đè caption, ảnh PNG, Excel và PDF. Sửa câu ở `posts.py` rồi chạy lại. Sửa tay `caption.txt` sẽ mất nếu chạy lại script.

Chỉ PDF: `--only-pdf`. Chỉ lịch và chữ, không render ảnh: `--skip-images`.

## Luật khi viết thêm bài

- Không bịa lượt chơi, điểm, sao, review.
- Không gắn logo JLPT, IELTS, British Council, Japan Foundation. Nói «luyện theo dạng đề».
- Voice, chat realtime, phòng video: ghi **bản máy tính** cho tới khi web chạy được và đã kiểm tra.
- Nhạc trailer chỉ dùng bài CC0 đã ghi trong `NihongoLife/Docs/AUDIO_CREDITS.md`, hoặc bài khác có giấy phép chép vào `Bao_Cao/marketing/CREDITS.md`.
- Không đưa secret, khoá Supabase, Agora App ID, đề thi bản quyền vào thư mục này.
- Không sửa code gameplay từ bộ marketing.

Brief giao việc nằm ở `NihongoLife/Docs/GROK_TASK_TRAILER_MARKETING.md`. Trailer và demo gameplay chưa quay trong đợt này vì máy làm bộ kit không chạy Unity. Danh sách cảnh quay nằm cạnh từng bài và trong `Video/README.md`.
