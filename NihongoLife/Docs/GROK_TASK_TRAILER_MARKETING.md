# TASK-M (Grok) — Trailer, demo gameplay và kế hoạch marketing theo tuần

> Người giao: Claude (theo yêu cầu của anh Long, trưởng nhóm). Đọc **hết** file này và `AGENTS.md` ở gốc repo trước khi làm.
> Mục tiêu: 3 sản phẩm marketing **đẹp, đúng sự thật, quay từ game thật**. Không sửa gameplay.

---

## 0. Bối cảnh bạn cần biết

**Nihongo Life** là game mô phỏng cuộc sống ở khu phố Hibari-chō (Nhật Bản) để học tiếng Nhật (và IELTS tiếng Anh) qua tình huống thật.
- **Đối tượng:** người Việt 16–25 tuổi học tiếng Nhật (JLPT N5–N3) hoặc luyện IELTS; sinh viên; người chuẩn bị du học hoặc đi làm ở Nhật.
- **Điểm mạnh nhất (ưu tiên số 1 trong mọi video và bài đăng):**
  1. **Online giao tiếp:** đi dạo cùng người chơi khác, chat khu vực/nhóm/riêng, kết bạn, co-op quest, voice push-to-talk, phòng Speaking 1-1 với giáo viên (video Agora).
  2. **Thi IELTS và JLPT ngay trong game:** đề Listening/Reading/Writing/Speaking, chấm điểm, lịch sử bài thi, trí tuệ tăng theo điểm.
- **Thế giới:** cốt truyện mở đầu và lộ trình mở bản đồ theo "trí tuệ".
  - Các địa điểm: Hibari Mart → Sushi Hibari → Ga → Game Center → đảo Midori.
  - Hoạt động: làm thêm (konbini, sushi), nông trại và câu cá trên đảo, phòng trọ (học, ngủ), Game Center 6 trò học chữ (UFO catcher, Kana Rain…), giao thông có đèn và vạch qua đường.
- **Chế độ chơi:** Khách = chơi ngoại tuyến full map. Tài khoản đăng ký = cốt truyện theo lộ trình + online.
- **Nền tảng:** PC (Windows) và **web (WebGL)**. Website đội: https://website-the-next-generation-o323.vercel.app/team
- **Đội ngũ:** The Next Generation (VTC Academy), **3 nhà sáng lập**: Quách Thành Long (Trưởng nhóm · Lập trình web & game), Nguyễn Lê Gia Huy, Lương Nguyễn Tấn Dũng (Nhân vật, animation & gameplay). **Chỉ ghi 3 người này** ở mọi credit.
- **Thành tích:** Giải Triển vọng (Unique Project 2025 Award, VTC Academy). Ảnh có trong `NihongoLife/Assets/NihongoLife/Resources/MenuGallery/01_team_award_2025.jpg`.
- Thuật ngữ tiếng Việt trong game: **"trí tuệ"** (không dùng "kiến thức").

## 1. Luật bắt buộc (vi phạm = làm lại)

1. **Không sửa gameplay, cảnh hay prefab.** Chỉ được sửa:
   - `Assets/NihongoLife/TrailerReel/TrailerReel.cs` (assembly trailer);
   - hai test quay phim `Tests/PlayMode/TrailerRecordingTests.cs`, `Tests/PlayMode/DemoRecordingTests.cs`;
   - file mới trong `Bao_Cao/marketing/` và `Docs/marketing/`.
   Gặp lỗi game khi quay thì **ghi vào `Docs/TEAM_TASKS.md`**, không tự vá gameplay.
2. **Mọi cảnh quay là game thật.** Không mockup, không ghép UI giả, không chèn tính năng chưa có. Tính năng chỉ có trên PC (voice/video Agora khi chưa cấu hình web) thì ghi rõ "PC".
3. **Không bịa số liệu, review, lời chứng thực, logo đối tác hay giải thưởng.** Không giả danh tổ chức hoặc người thật. Logo JLPT/IELTS/British Council/Japan Foundation **không được dùng**; chỉ được ghi "luyện thi theo dạng đề JLPT/IELTS".
4. **Nhạc và âm thanh:** dùng nhạc trong game (CC0, xem `Docs/AUDIO_CREDITS.md`) hoặc nhạc CC0/royalty-free có giấy phép rõ ràng, ghi nguồn vào `Bao_Cao/marketing/CREDITS.md`. Lồng tiếng bằng TTS: edge-tts giọng `vi-VN-HoaiMyNeural` / `vi-VN-NamMinhNeural` (cách làm giống `_work/voice_gen.py`).
5. **Không ghi secret** (Supabase key, Agora App ID, mật khẩu) vào file nào. Tài liệu Cambridge/JLPT gốc chỉ để trên máy.
6. **Unity chỉ mở project một lần:** đóng Unity Editor trước khi chạy batch. File lớn (khung hình, video thô) đặt ở **ổ D:** (`D:/VTC_Academy/NihongoLife/_work/marketing/`). Ổ C: đã đầy.
7. Xem lại **từng ảnh kiểm tra** (contact sheet) trước khi báo xong: không HUD lỗi, không nhân vật xuyên tường, không nhấp nháy, không chữ tràn, không màn đen dài.

## 2. Công cụ đã có (dùng lại, đừng viết lại)

| Công cụ | Dùng để | Cách chạy |
|---|---|---|
| `TrailerReel.cs` | Trailer quay trong game: chuỗi `Shot(id, giây, body, ja, vi)`, letterbox, phụ đề JA/VI, cắt qua màn đen, chạy theo game time | được gọi từ `TrailerRecordingTests` |
| `TrailerRecordingTests.Trailer_RecordFrames` | Xuất khung hình 1920×1080 JPG ở 30 fps | `Unity -batchmode -projectPath <project> -runTests -testPlatform PlayMode -testFilter Trailer_RecordFrames -testResults <xml> -logFile <log>` với `NL_FRAME_DIR=D:/.../frames_trailer` |
| `DemoRecordingTests.Demo_RecordFrames` | Demo gameplay có HUD, con trỏ, phím bấm, `timeline.tsv` để ghép tiếng | `-testFilter Demo_RecordFrames`, `NL_DEMO_DIR=...`, `NL_DEMO_VO=` (file "id\tgiây") |
| Kịch bản cũ | Tham khảo, làm bản mới tốt hơn | `Bao_Cao/NihongoLife_Kich_Ban_Trailer_v2.pdf`, `Bao_Cao/NihongoLife_Kich_Ban_Demo_v2.pdf`, video cũ `Bao_Cao/NihongoLife_Trailer_v3.mp4`, `Bao_Cao/NihongoLife_Gameplay_Demo.mp4` |
| Test có sẵn để tham khảo cách dựng cảnh | Chat/online, thi, Speaking, đảo, Game Center | `OnlineSocialPlayModeTests`, `IeltsProductivePlayModeTests`, `SpeakingLivePlayModeTests`, `IslandPlayModeTests`, `ArcadeGamesPlayModeTests`, `StreetTrafficPlayModeTests` |
| ffmpeg | Ghép khung hình, chèn chữ, nhạc, xuất video | có trên máy |

Ghép video mẫu (30 fps, chất lượng cao):
```bash
ffmpeg -framerate 30 -i frames/%05d.jpg -i music.wav -i vo.wav \
  -filter_complex "[1:a]volume=0.55[m];[2:a]volume=1.0[v];[m][v]amix=inputs=2:duration=first,loudnorm=I=-14:TP=-1.5:LRA=11[a]" \
  -map 0:v -map "[a]" -c:v libx264 -preset slow -crf 17 -pix_fmt yuv420p -c:a aac -b:a 192k -movflags +faststart out.mp4
```
Bản dọc 9:16: crop hoặc quay lại với camera dọc (đừng chỉ phóng to làm vỡ hình). Kiểm tra bằng contact sheet: `ffmpeg -i out.mp4 -vf "fps=1,scale=480:-1,tile=6x5" sheet.jpg`.

## 3. Sản phẩm 1 — Trailer v4 "đỉnh" (bắt buộc)

**Xuất ra `Bao_Cao/marketing/`:**
- `NihongoLife_Trailer_v4_16x9.mp4`: 75–90 giây, 1920×1080, 30 fps (60 fps nếu đủ mượt).
- `NihongoLife_Teaser_30s_16x9.mp4`.
- `NihongoLife_Teaser_15s_9x16.mp4`: 1080×1920 cho TikTok, Reels, Shorts; chữ to, móc câu trong 2 giây đầu.
- `NihongoLife_Trailer_v4.srt` (VI) và `NihongoLife_Trailer_v4.en.srt` (EN).
- `Kich_Ban_Trailer_v4.md`: kịch bản theo từng shot (giây, cảnh, góc máy, phụ đề JA/VI, âm thanh).
- `thumbnail_16x9.png`, `thumbnail_9x16.png` (chụp từ game, chữ to, tương phản cao).

**Mạch cảm xúc gợi ý** (sửa nếu có ý hay hơn, nhưng giữ 2 trụ cột ở giữa):
1. **Móc câu (0–5 s):** "Học tiếng Nhật… bằng cách *sống* ở Nhật." Flycam phố Hibari lúc hoàng hôn, đèn đường bật, tàu chạy.
2. **Đặt chân (5–15 s):** nhân vật bước ra phố, qua vạch qua đường có đèn "piyo piyo", vào konbini, chào 「いらっしゃいませ」.
3. **Trụ cột 1 — Online giao tiếp (15–40 s):** người chơi khác xuất hiện, chat khu vực, kết bạn, rủ nhóm co-op, voice push-to-talk (giữ T), phòng Speaking với giáo viên. Phụ đề: "Nói chuyện với người thật".
4. **Trụ cột 2 — Thi IELTS & JLPT (40–60 s):** phòng thi, đồng hồ đếm, nghe/đọc/viết, kết quả band/điểm, "trí tuệ +N", bản đồ mở khu mới (thẻ "KHU VỰC MỚI ĐÃ MỞ").
5. **Thế giới sống (60–75 s):** cắt nhanh theo nhịp nhạc qua sushi, ga, đảo Midori (nông trại, câu cá), Game Center (UFO catcher thú nhồi bông mới), đêm, mưa.
6. **Kết (75–90 s):** logo NIHONGO LIFE 日本語ライフ, "Chơi miễn phí trên web và PC", nút/URL website, "The Next Generation · VTC Academy", huy hiệu "Giải Triển vọng — Unique Project 2025".

**Tiêu chuẩn đẹp:**
- Nhịp cắt theo phách nhạc; mỗi shot ≤ 3 s ở đoạn cao trào.
- Camera dolly/crane mượt (dùng `Mathf.SmoothStep`); không giật, không xuyên vật.
- Ánh sáng giờ vàng và đêm có đèn. Letterbox 2.35:1 cho bản 16:9.
- Phụ đề hai tầng: JA nhỏ ở trên, VI to ở dưới, font NotoSansJP; không che mặt nhân vật.
- Âm: nhạc + tiếng phố + 1–2 câu thoại tiếng Nhật thật trong game. Mức âm −14 LUFS.
- **Không** hiện cửa sổ Unity, chữ debug, HUD lỗi, khoảng đen trên 0,5 s.

## 4. Sản phẩm 2 — Demo gameplay v3 có thuyết minh (bắt buộc)

**Xuất ra `Bao_Cao/marketing/`:**
- `NihongoLife_Gameplay_Demo_v3.mp4`: 3–5 phút, 1920×1080, có HUD và con trỏ/phím bấm.
- Phụ đề `.srt` VI và EN.
- `Kich_Ban_Demo_v3.md`: lời thuyết minh từng đoạn, kèm thời lượng.

**Mạch** (quay bằng `DemoRecordingTests`, chỉnh luồng trong file test đó):
1. Menu → chọn nhân vật → **Khách** (ngoại tuyến, full map).
2. Phố, bản đồ (M), vào konbini mua đồ, hội thoại N5.
3. Đăng nhập tài khoản → **lộ trình mở bản đồ** (Sổ nhiệm vụ N, thẻ mở khu mới).
4. **Online:** chat, kết bạn, phòng co-op, voice (ghi "PC" nếu cần).
5. **Thi:** một đề JLPT và một phần IELTS (Reading/Listening). Kết quả và trí tuệ tăng.
6. Làm thêm (konbini/sushi), đảo Midori (nông trại, câu cá), Game Center, về phòng ngủ.
7. Kết: website, đội ngũ, kêu gọi chơi thử.

Thuyết minh: giọng Việt tự nhiên, ngắn gọn, nói **lợi ích cho người học**, không đọc tên tính năng khô khan.

## 5. Sản phẩm 3 — Kế hoạch marketing 12 tuần "siêu đẹp" (bắt buộc)

**Xuất ra:**
- `Docs/marketing/KE_HOACH_MARKETING_12_TUAN.md` (bản nguồn).
- `Bao_Cao/marketing/Ke_Hoach_Marketing_12_Tuan.pdf`: thiết kế đẹp, có bìa, màu thương hiệu (vàng `#F2B233`, hồng neon Game Center, xanh đêm `#0B0F16`), biểu đồ, ảnh chụp game. Có thể dựng HTML rồi in PDF.
- `Bao_Cao/marketing/Lich_Noi_Dung_12_Tuan.xlsx`: lịch đăng bài từng ngày, cột: Ngày | Kênh | Định dạng | Chủ đề | Hook | Asset | CTA | Người làm | KPI | Trạng thái.

**Nội dung tối thiểu:**
1. **Mục tiêu và KPI:** lượt chơi web, tài khoản đăng ký, DAU/WAU, tỉ lệ làm bài thi, follower, CTR. Đặt **mốc thực tế cho nhóm sinh viên không có ngân sách lớn**, có 3 mức: tối thiểu / mục tiêu / vượt.
2. **Chân dung người dùng (3 persona):** học sinh/sinh viên học JLPT; người luyện IELTS; người sắp sang Nhật làm việc hoặc du học. Mỗi persona ghi nỗi đau, kênh họ dùng, thông điệp phù hợp.
3. **Định vị và thông điệp:** một câu định vị, 3 trụ thông điệp (Sống như ở Nhật · Nói chuyện với người thật · Thi thử JLPT/IELTS miễn phí), giọng thương hiệu.
4. **Kênh:** TikTok, Facebook (fanpage + group học tiếng Nhật/IELTS), YouTube (Shorts + video dài), Discord cộng đồng, Zalo, CLB tiếng Nhật các trường, VTC Academy, itch.io / trang web. Mỗi kênh ghi vai trò, tần suất, định dạng.
5. **Trụ nội dung (content pillars)** và **20+ ý tưởng bài đăng cụ thể**, kèm hook 2 giây đầu. Ví dụ: "1 phút học gọi món ở quán sushi", "Thử thách: qua JLPT N5 trong game", "Người lạ chat tiếng Nhật với mình".
6. **Lịch 12 tuần.** Mỗi tuần một bảng: mục tiêu tuần, nội dung chính, asset cần sản xuất, kênh, KPI, việc của từng người (Long / Huy / Dũng). Gợi ý giai đoạn:
   - Tuần 1–2: chuẩn bị (thương hiệu, trang, cộng đồng Discord, teaser).
   - Tuần 3–4: teaser + beta kín (mời 50–100 người học thử, thu phản hồi).
   - Tuần 5–6: **ra mắt bản web** (trailer v4, livestream chơi cùng, bài PR trường/CLB).
   - Tuần 7–9: tăng trưởng (thử thách JLPT/IELTS hằng tuần, bảng xếp hạng, UGC, hợp tác giáo viên/creator nhỏ).
   - Tuần 10–12: giữ chân và mở rộng (sự kiện theo mùa trong game, cập nhật nội dung, chuẩn bị itch.io/Steam).
7. **Ngân sách 3 mức:** 0 đồng / ~2 triệu / ~10 triệu VND, với chi tiêu cụ thể (boost bài, quà thử thách, in standee hội chợ trường).
8. **Đo lường:** công cụ (Google Analytics trên web, UTM link, bảng theo dõi tuần), nghi thức review mỗi thứ Hai.
9. **Rủi ro và cách xử lý:** game lỗi trên máy yếu, ít người online cùng lúc (giải pháp: giờ chơi chung cố định), bản quyền nhạc và tên thương hiệu, an toàn chat (chặn/báo cáo).
10. **Kit truyền thông:** mô tả game ngắn (50 chữ), trung bình (150 chữ), dài (300 chữ) bằng VI/EN/JA; bộ ảnh chụp; logo; thông tin đội.

**Tiêu chuẩn "hay":** mọi ý tưởng phải **làm được ngay bằng tính năng đang có**, có hook cụ thể và CTA rõ. Không viết chung chung kiểu "tăng nhận diện thương hiệu" mà không có hành động.

## 6. Nghiệm thu (tự kiểm trước khi báo xong)

- [ ] Ba video 16:9 / 30s / 9:16 xem hết không lỗi hình, tiếng; contact sheet đã soát (lưu `Bao_Cao/marketing/check/`).
- [ ] Trailer có đủ 2 trụ cột **Online giao tiếp** và **Thi IELTS/JLPT**, chiếm ít nhất 40% thời lượng.
- [ ] Demo v3 chạy đúng luồng thật, thuyết minh khớp hình, có phụ đề VI/EN.
- [ ] Kế hoạch 12 tuần: PDF đẹp + Markdown + Excel lịch đăng, đủ 10 mục ở phần 5.
- [ ] Credit chỉ có 3 nhà sáng lập; nhạc/âm thanh có nguồn trong `CREDITS.md`; không secret, không logo/tổ chức bên ngoài.
- [ ] Không file gameplay, cảnh, prefab nào bị sửa (`git diff --stat` chỉ gồm các đường dẫn cho phép ở mục 1).
- [ ] Thêm một mục ngày giờ vào `Docs/TEAM_TASKS.md`: đã làm gì, file nào, lỗi game phát hiện khi quay.
- [ ] Commit nhỏ, rõ ràng. Push lên `main` khi anh Long đồng ý.

## 7. Thứ tự làm gợi ý

1. Xem trailer v3 + demo cũ + kịch bản v2. Viết `Kich_Ban_Trailer_v4.md` và `Kich_Ban_Demo_v3.md`.
2. Sửa `TrailerReel.cs` theo kịch bản mới → quay → ghép → soát → làm bản 30s và 9:16.
3. Sửa `DemoRecordingTests.cs` → tạo thuyết minh TTS → quay → ghép → phụ đề.
4. Viết kế hoạch 12 tuần, dùng ảnh và video vừa làm làm asset minh hoạ → PDF + Excel.
5. Tự nghiệm thu theo mục 6 rồi báo lại anh Long, kèm danh sách file.
