# Đăng NihongoLife lên itch.io (bản WebGL)

## 1. Dựng và đóng gói

```powershell
# Unity phải đang đóng. Dựng bản web (≈15–20 phút): zone bundles, nhạc/giọng tải theo nhu cầu, Brotli + Decompression Fallback.
& "C:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe" -batchmode -quit -buildTarget WebGL `
  -projectPath "D:\VTC_Academy\NihongoLife\NihongoLife\NihongoLife" `
  -executeMethod NihongoLife.Editor.ReleaseBuild.BuildWebGL -logFile "D:\VTC_Academy\NihongoLife\_work\logs\webgl.log"
# Kiểm tra giới hạn itch.io và nén thành zip để upload:
python Tools\release\package_itch.py
```

`package_itch.py` lấy `..\Build\Release\WebGL`, kiểm tra giới hạn HTML5 của itch.io và ghi
`..\Build\Release\NihongoLife_itch_<ngày>.zip` (index.html ở gốc zip):
- ≤ 1.000 file (thư mục cũng tính), ≤ 500 MB sau khi giải nén, ≤ 200 MB mỗi file, đường dẫn ≤ 240 ký tự.
- Bỏ `_headers`, `.htaccess`, `vercel.json` (itch.io không đọc; không cần `Content-Encoding` vì Decompression Fallback bật).

**Trực tuyến trên web.** Trình duyệt không đọc được `LocalSecrets/secrets.json`. Vì vậy mỗi lần build WebGL (cả bằng cửa sổ Build lẫn `ReleaseBuild.BuildWebGL`) sẽ ghi `nl-config.js` cạnh `index.html`.
- File chỉ chứa giá trị công khai, lấy từ `secrets.json` (hoặc biến môi trường): Supabase URL, anon key, Agora App ID.
- Không bao giờ chứa Gemini key. Nếu lỡ điền service key, bản build sẽ dừng với lỗi.
- Thiếu file này thì bản web báo «Máy chủ chưa được cấu hình» và mọi nút trực tuyến không chạy. Nhớ zip cả file này.

## 2. Cài đặt trang (Edit game)
- **Kind of project:** HTML · upload zip, tick **This file will be played in the browser**.
- **Embed options:** Viewport 1280 × 720 · ✓ Mobile friendly (landscape) · ✓ Fullscreen button · ✓ Automatically start on page load: **tắt** (để trang tải nhẹ) · SharedArrayBuffer: **không cần**.
- **Classification:** Game · Genre *Simulation* / *Educational* · Tags: `japanese`, `language-learning`, `educational`, `life-simulation`, `jlpt`, `3d`, `cozy`, `vietnam`.
- **Languages:** Vietnamese, English, Japanese, Russian.
- **Community:** Comments bật. **Visibility:** Draft → kiểm tra trên máy thật → Public.
- Ảnh bìa 630 × 500 và ảnh chụp: lấy từ `Bao_Cao/` (train-route, town-life, bank, clinic) và `Assets/NihongoLife/Resources/UI/Loading/`.

## 3. Mô tả trang

**Tiếng Việt** — *NihongoLife: sống ở Nhật, học tiếng Nhật.* Bạn là học sinh mới đến thị trấn Hibari. Mua vé tàu, gọi món sushi, đi khám bệnh, làm thêm ở konbini, chọn nghề (bán lẻ, ẩm thực, đầu bếp, nông nghiệp, y tế, nhà hàng) — mọi việc đều bằng tiếng Nhật, có nghĩa tiếng Việt bên cạnh. Ôn JLPT / IELTS ở trường, đi tàu ra cảng Minato, đảo Xanh và bay tới Phú Quốc. Ngân hàng cho vay khi hết tiền, phòng khám chữa bệnh đúng như ngoài đời, sổ tay giải thích mọi thứ.

**English** — *NihongoLife: live in Japan, learn Japanese.* You are a new student in the town of Hibari. Buy a train ticket, order sushi, see a doctor, work a konbini shift, build a career (retail, food, chef, farming, medicine, hospitality) — all in Japanese, with the meaning beside it. Practise for the JLPT and IELTS, ride the line to Minato harbour, Midori Island and fly to Phú Quốc. A bank for hard times, a clinic that treats illness the way doctors really do, and a handbook that explains it all.

**日本語** — 『NihongoLife』は、ひばり町で暮らしながら日本語を学ぶゲームです。切符を買う、お寿司を注文する、クリニックで診てもらう、コンビニでアルバイト、仕事を選んで昇進——すべて日本語で。JLPT・IELTSの練習、電車で港町ミナトやみどり島へ、そして空港からフーコック島へ。

**Русский** — *NihongoLife: живи в Японии, учи японский.* Вы — новый ученик в городке Хибари. Купите билет на поезд, закажите суши, сходите к врачу, поработайте в магазине у дома, постройте карьеру — всё по-японски, с переводом рядом. Подготовка к JLPT и IELTS, поездки в порт Минато, на остров Мидори и перелёт на Фукуок.

**Điều khiển / Controls:** WASD đi · Shift chạy · F tương tác · Q đổi người · M bản đồ · B túi · Y sổ tay · Esc menu. Trên điện thoại: cần gạt và nút cảm ứng.
Bản web lưu tiến trình trong trình duyệt (IndexedDB). Chơi online cần đăng ký tài khoản.

## 4. Ghi công (dán vào cuối trang)
- Mô hình 3D: Kenney (CC0) — City Kit Roads/Suburban/Commercial, Food Kit, Furniture Kit, Pirate Kit, Nature Kit, Mini Arcade; Quaternius Ultimate packs (CC0); KayKit City Builder Bits (CC0); Styloo Classroom pack; nhân vật Kids (Unity Asset Store, New Chac); hoạt ảnh Mixamo (Adobe).
- Vật liệu / bầu trời: ambientCG Ground054, Poly Haven Kloofendal sky (CC0).
- Hình minh hoạ từ điển: **Twemoji © Twitter, Inc. và cộng sự, CC-BY 4.0** (https://creativecommons.org/licenses/by/4.0/).
- Tranh trong phòng tranh: The Metropolitan Museum of Art Open Access (CC0) và Wikimedia Commons (phạm vi công cộng). Giai điệu: tác phẩm thuộc phạm vi công cộng, dữ liệu nốt CC0.
- Bản đồ Phú Quốc: dữ liệu **© OpenStreetMap contributors** (ODbL, https://www.openstreetmap.org/copyright).
- Font: Noto Sans JP (SIL Open Font License 1.1).
- Âm nhạc: CC0 từ OpenGameArt (Tozan, Holizna, SubspaceAudio, Tarush Singhal, Thimras, Wolfgang_, Spring Spring, Kenney) và nhạc gốc NihongoLife — chi tiết `Docs/AUDIO_CREDITS.md`. Giọng đọc: Microsoft Edge neural TTS.
- Gọi thoại: Agora RTC SDK.

> Trước khi Public: đối chiếu lại điều khoản của các gói Asset Store (Kids, Devion) và Styloo — dùng trong game được, nhưng không phân phối lại file gốc.
