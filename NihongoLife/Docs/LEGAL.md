# NihongoLife — Pháp lý & ghi công / Legal & credits

## Người chịu trách nhiệm / Responsible person

**Quách Thành Long — người chịu trách nhiệm chính và đóng góp nhiều nhất** cho dự án NihongoLife
(main responsible person and top contributor), trưởng nhóm The Next Generation · VTC Academy
(https://github.com/StephenSouth13).

Đồng sáng lập / co-founders: Nguyễn Lê Gia Huy, Lương Nguyễn Tấn Dũng.

## Mã nguồn / Code

Mã nguồn của NihongoLife phát hành theo [MIT License](../../LICENSE). Tài nguyên bên thứ ba giữ giấy phép riêng như dưới đây.

## Tài nguyên CC BY (cần ghi công) / CC BY assets

| Tài nguyên | Tác giả | Giấy phép | Dùng ở |
|---|---|---|---|
| Twemoji graphics (fork jdecked/twemoji), 478 PNG 72×72 | Copyright 2020 Twitter, Inc and other contributors | CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/ | `Resources/Content/Illustrations/` (hình minh họa từ điển) |

**Video trong game / In-game videos:** hiện tất cả video trong `Resources/Video/nihongolife_videos.json` là video do nhóm tự quay
(`"license": "own"`) và các ô video Lộ trình tiếng Nga (`ru_path.json`) để trống, chỉ nhận link từ kênh @NihongoLifeGame.
**Hiện không dùng video CC BY nào của bên thứ ba.** Nếu thêm video CC BY, bắt buộc điền `attribution` (tên tác giả, tiêu đề,
link gốc, giấy phép) trong JSON và bổ sung vào bảng trên.

## Tài nguyên CC0 / phạm vi công cộng / CC0 & public domain

| Tài nguyên | Tác giả / nguồn | Giấy phép |
|---|---|---|
| Kenney kits (City Kit Roads, City Kit Suburban, Food Kit, Furniture Kit, Impact Sounds, Interface Sounds, 85 jingles) | Kenney — https://kenney.nl | CC0 1.0 |
| KayKit City Builder Bits | Kay Lousberg — https://kaylousberg.com | CC0 1.0 |
| Sushi Restaurant Kit, Train Pack, Ultimate Animated Animals, Ultimate Food Pack, Ultimate House Interior Pack | Quaternius — https://quaternius.com | CC0 1.0 |
| Tài nguyên Phú Quốc (`Assets/ThirdParty/PhuQuocCC0`) | xem `Docs/PHU_QUOC_ASSETS.md` | CC0 1.0 |
| Nhạc nền & âm thanh môi trường (OpenGameArt: Tozan, Holizna, SubspaceAudio, Tarush Singhal, Thimras, Wolfgang_, Spring Spring) | xem `Docs/AUDIO_CREDITS.md` | CC0 1.0 |
| 24 bức tranh — The Metropolitan Museum of Art Open Access | https://www.metmuseum.org | CC0 1.0 |
| 15 bức tranh — Wikimedia Commons | xem `Resources/Art/artworks.json` | Phạm vi công cộng |
| Giai điệu dân ca / cổ điển trong phòng nhạc (dữ liệu nốt do NihongoLife soạn) | — | Phạm vi công cộng; dữ liệu CC0 |

Danh sách chi tiết: [`CREDITS.md`](CREDITS.md), [`AUDIO_CREDITS.md`](AUDIO_CREDITS.md).

## Tài nguyên có giấy phép khác / Other licences

- Unity 6 (URP), TextMesh Pro — điều khoản Unity.
- Mixamo animations — điều khoản Adobe Mixamo.
- Devion Games, Styloo Classroom Asset Pack — giấy phép của nhà cung cấp.
- Material Symbols icons — Apache 2.0 (`Resources/UI/Icons/LICENSE_MaterialSymbols_Apache2.txt`).
- Giọng đọc cốt truyện tạo bằng Microsoft Edge neural TTS.

## Nhãn hiệu / Trademark disclaimer

IELTS, JLPT và TORFL (ТРКИ) là nhãn hiệu của các chủ sở hữu tương ứng (IELTS: British Council, IDP: IELTS Australia và
Cambridge University Press & Assessment; JLPT: Japan Foundation và JEES; TORFL: Bộ Giáo dục Liên bang Nga và các trung tâm
khảo thí được ủy quyền). NihongoLife **không liên kết, không được tài trợ hay xác nhận** bởi các tổ chức này. Toàn bộ đề luyện
tập, bài mẫu và điểm số trong game là **nội dung không chính thức**, chỉ để luyện tập, không thay thế kỳ thi thật.

IELTS, JLPT and TORFL are trademarks of their respective owners. NihongoLife is not affiliated with or endorsed by them; all
practice content, model answers and scores in the game are unofficial and for practice only.

## Quyền riêng tư: bản ghi giọng nói / Privacy: voice recordings

- Bản ghi phần thi Nói (Speaking) được lưu **cục bộ trên máy người chơi** tại
  `Application.persistentDataPath/exam_submissions/<exam>/<thời gian>/` (file WAV + `manifest.json`). Game không tự tải các
  file này lên máy chủ. Người chơi có thể xóa thư mục này bất cứ lúc nào.
- Luyện phát âm (SpeechPractice) chỉ gửi âm thanh tới dịch vụ nhận dạng giọng nói (Google Gemini API) khi người chơi/nhà phát
  triển **chủ động bật** tính năng và cung cấp khóa API riêng; mặc định tắt.
- Voice recordings for Speaking practice are stored locally on the player's device and are never uploaded automatically.
