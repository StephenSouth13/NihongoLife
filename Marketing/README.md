# Bộ marketing NihongoLife

Anh Huy mở đúng một chỗ: thư mục tuần. Copy caption, đăng ảnh, không cần đụng code game.

Lịch bắt đầu **thứ Hai 26/10/2026**, hết **chủ Nhật 17/01/2027**.

## Cách đăng một bài Facebook

1. Mở `Tuan-XX/README.md` xem chủ đề tuần và việc của Long, Huy, Dũng.
2. Vào thư mục bài. Đọc `chi-tiet.md` (giờ, ba hook, trả lời, cách đo). Mở `caption.txt`, thay các mã link rồi copy.
3. Đăng `fb-1080.png` (vuông) hoặc `fb-1350.png` (dọc 4:5). Story dùng `story-1920.png` nếu thư mục có file đó.
4. Bài nào có `CAN_QUAY.md` thì ảnh tĩnh đăng được, video thì chưa. Đừng ghép cảnh giả.

Mã link nằm ở `LINKS.md`. Trang đội ngũ đã có URL thật. Link chơi, fanpage, Discord, Zalo chưa được bịa trong bộ này.

## Trong thư mục

| Chỗ | Việc |
|---|---|
| `HinhAnh/` | Ảnh nguồn đã lọc. Nhân vật cũ không nằm ở đây. Chỉ mục trong `HinhAnh/README.md` |
| `Brand/` | Màu lấy từ UI game, font Montserrat (OFL), cách dùng logo, giọng văn |
| `KeHoach/` | Kế hoạch 12 tuần: Markdown, PDF, Word (`.docx`) và lịch Excel. Sheet `Nam` là năm sau đó |
| `KeHoachNam/` | Kế hoạch 18/01/2027–16/01/2028, bốn mùa, sự kiện, checklist. Có bản Word. Sẽ sửa thường xuyên |
| `TuKhoa/` | Ngân hàng từ khoá: Markdown và Word. Cốt lõi, đuôi dài, Facebook, TikTok, YouTube, Google |
| `Tuan-01` … `Tuan-12` | Bài sẵn caption và ảnh |
| `PressKit/` | Mô tả Việt / Anh, tờ sự thật, chỗ trống tiểu sử |
| `Templates/` | HTML, CSS và script dựng lại ảnh cùng PDF |
| `Video/` | Video không nằm trong git. File nặng để ổ D |

## Dựng lại ảnh và PDF

Từ gốc repo:

```bash
python3 Marketing/Templates/regenerate.py
```

Script đọc `Templates/posts.py`, `chi_tiet.py`, `tukhoa.py`, `nam.py` và kế hoạch 12 tuần, rồi ghi đè caption, `chi-tiet.md`, ảnh PNG, Excel, PDF 12 tuần, PDF năm, `TuKhoa/Ngan_Hang_Tu_Khoa.md` và checklist trong `KeHoachNam/`. Sửa câu ở các file Python đó rồi chạy lại. Sửa tay caption, chi tiết bài hoặc checklist sẽ mất nếu chạy lại script. File `KeHoachNam/KeHoach_Nam_2026-2027.md` viết tay; script chỉ in lại PDF.

Chỉ PDF: `--only-pdf`. Chỉ lịch và chữ, không render ảnh: `--skip-images`.

Bản Word (bìa, mục lục, bảng): `python3 Marketing/Templates/docx_kit.py`. Font trong file là Montserrat. Máy chưa có font thì cài file trong `Brand/fonts/`.

## Luật khi viết thêm bài

- Không bịa lượt chơi, điểm, sao, review.
- Không gắn logo JLPT, IELTS, British Council, Japan Foundation. Nói «luyện theo dạng đề».
- Voice, chat realtime, phòng video: ghi **bản máy tính** cho tới khi web chạy được và đã kiểm tra.
- Nhạc trailer chỉ dùng bài CC0 đã ghi trong `NihongoLife/Docs/AUDIO_CREDITS.md`, hoặc bài khác có giấy phép chép vào `Bao_Cao/marketing/CREDITS.md`.
- Không đưa secret, khoá Supabase, Agora App ID, đề thi bản quyền vào thư mục này.
- Không sửa code gameplay từ bộ marketing.

Brief giao việc nằm ở `NihongoLife/Docs/GROK_TASK_TRAILER_MARKETING.md`. Trailer và demo gameplay chưa quay trong đợt này vì máy làm bộ kit không chạy Unity. Danh sách cảnh quay nằm cạnh từng bài và trong `Video/README.md`.
