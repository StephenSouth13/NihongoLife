<div align="center">

<img src="docs/readme/teaser.gif" alt="Teaser 6 giây: phố Hibari-chō trong trailer v4b" width="100%"/>

<br/>

[Trailer v4b · 720p · 67 giây](docs/readme/preview_v4b_720p.mp4) · [Kênh YouTube](https://www.youtube.com/channel/UCAKp-_EsasxPY2oa-4CzWtQ)

<br/>

Music: "Ishikari Lore" by Kevin MacLeod (incompetech.com), licensed under CC BY 4.0

<br/>

[![Unity](https://img.shields.io/badge/Unity-6000.3.12f1-222222?logo=unity&logoColor=white)](NihongoLife/ProjectSettings/ProjectVersion.txt)
[![WebGL](https://img.shields.io/badge/WebGL-template%20sẵn-1F3C88)](NihongoLife/Assets/WebGLTemplates/NihongoLife)
[![License: MIT](https://img.shields.io/badge/License-MIT-F7BD3D?style=flat&labelColor=111721)](LICENSE)
[![PRs welcome](https://img.shields.io/badge/PRs-welcome-2E9E6B)](#đóng-góp)

**Học tiếng Nhật bằng cách sống ở Nhật.**

Game 3D mô phỏng cuộc sống ở khu phố Hibari-chō (ひばり町), cho người Việt luyện tiếng Nhật trong việc hàng ngày.

[Trong game](#trong-game) · [Chơi trên web](#chơi-trên-web) · [Bắt đầu](#bắt-đầu-nhanh) · [Điều khiển](#điều-khiển) · [Kiến trúc](#kiến-trúc) · [Lộ trình](#lộ-trình) · [Đóng góp](#đóng-góp) · [English](#english)

</div>

---

## NihongoLife là gì

Bạn vào phố, chào hàng xóm, mua cơm nắm, hỏi giá vé, ngồi lớp, gọi sushi, tan học thì ra Game Center, và khi lộ trình tới thì ra đảo Midori. Chọn sai trong hội thoại thì được giải thích và chọn lại.

Người chơi là tám bé trong `PlayableCharacterCatalog`: **Haruto** (mặc định), Yui, Sota, Hina, Riku, Mio, Yuto, Sakura. Prefab nằm ở `Resources/Characters/Kids/NL_kid_*`. Save cũ còn id `student` được chuyển sang Haruto.

Giao diện có tiếng Việt, English và 日本語. Chữ tiến trình trên UI tiếng Việt là **trí tuệ**.

<p align="center"><img src="docs/readme/v4b_town.jpg" width="88%" alt="Phố Hibari-chō nhìn từ trên cao"/>
<br/><sub>Phố Hibari-chō trong trailer v4b. Dòng chữ: «Học tiếng Nhật bằng cách sống.»</sub></p>

## Trong game

### Hội thoại

Một khung dùng chung: câu tiếng Nhật, cách đọc và nghĩa tiếng Việt, tuỳ chế độ hướng dẫn, luyện hoặc kiểm tra. Chọn bằng phím số hoặc chuột. Chọn sai thì được giải thích và chọn lại.

<p align="center"><img src="docs/readme/v4b_station_dialogue.jpg" width="85%" alt="Kimura hỏi ga đến tại Ga Hibari"/>
<br/><sub>Ga Hibari · Kimura: 「いらっしゃいませ。どちらまでですか。」 Trên hình: ga Minato, ga Gakuen-mae, Đảo Xanh, một câu cộc lốc, và lời từ chối.</sub></p>

### Nói chuyện online

Chat khu vực, thẻ người chơi, kết bạn và chặn có trong game. Giữ **T** để nói (push-to-talk). Voice và phòng Speaking đi qua Agora. Điền `AGORA_APP_ID` trong `secrets.json` thì dịch vụ mới bật. Chưa cấu hình thì game vẫn chơi ngoại tuyến. Trailer chỉ có thẻ chữ về học cùng nhau; không có cảnh hai người chơi.

### Luyện theo dạng JLPT và IELTS

Trong lớp có bài dạng JLPT và dạng IELTS (đọc, nghe, viết, nói). Điểm trong game không phải kết quả chính thức, và repo không gắn logo của kỳ thi. Gói đề IELTS có bản quyền để ở `LocalContent/IELTS/`, không đưa vào git. Trailer v4b không có màn hình đề thi.

### Phố, ga, đảo

Hibari-chō có phòng trọ, konbini, ga, quán sushi, trường, Game Center. Đảo Midori có luống và chỗ câu. Với tài khoản, `LocationUnlocks` mở sushi ở **100** trí tuệ (sau konbini), ga ở **160** (sau sushi), Game Center ở **240** (sau tàu), đảo ở **320** (sau Game Center). Hibari Mart và trường mở từ đầu. Phòng trọ mở khi cốt truyện đến nơi.

Sáu máy Game Center, đúng tên trong game: Kana Match, Word Shooter, Order Rush, Kana Rain, UFO Catcher, Basket Count.

<table>
<tr>
<td width="50%"><img src="docs/readme/v4b_job.jpg" alt="Ca làm thêm: khách hỏi sữa ở đâu"/><br/><sub>Làm thêm ở Hibari Mart. Khách: 「すみません、ぎゅうにゅうは どこですか。」</sub></td>
<td width="50%"><img src="docs/readme/v4b_train.jpg" alt="Tàu tới Đảo Midori"/><br/><sub>«Tàu tới Đảo Midori» / 「みどり島行きの電車」. Vé: Gakuen-mae ¥180, Minato ¥320, đảo ¥450, sân số 2.</sub></td>
</tr>
<tr>
<td width="50%"><img src="docs/readme/v4b_aoki.jpg" alt="Aoki chào khách tại quán sushi"/><br/><sub>Aoki: 「いらっしゃいませ。何名様ですか。」 Bảng tuyển phục vụ ¥700/ca.</sub></td>
<td width="50%"><img src="docs/readme/v4b_bedroom.jpg" alt="Phòng của bạn"/><br/><sub>Phòng của bạn / Your Bedroom / わたしの部屋.</sub></td>
</tr>
<tr>
<td width="50%"><img src="docs/readme/v4b_kana.jpg" alt="Kana Match, thẻ hiragana"/><br/><sub>Kana Match · hiragana. Trên hình: lượt 3, cặp 2/8, 225 điểm, x2.</sub></td>
<td width="50%"><img src="docs/readme/v4b_farm.jpg" alt="Xới đất trên đảo Midori"/><br/><sub>Nông trại Đảo Xanh. Ô chưa xới; câu 「つちをたがやします」.</sub></td>
</tr>
</table>

### Tâm trạng và danh tiếng

`SocialStanding` giữ tâm trạng (vui, ngại, tự tin, căng, mệt, hứng, tự hào, bực, hoặc trung tính), danh tiếng và tai tiếng. Mỗi chỉ số tối đa 100. Tâm trạng đổi hệ số XP một chút và đổi câu NPC nói. Phần này chạy cả khi chơi khách, không nhìn cấp tài khoản.

### Chơi khách, không khoá cấp

Khách chơi ngoại tuyến và cả phố mở, vì `PlayerSessionService.OpenWorld`. Tài khoản đã đăng ký đi lộ trình trí tuệ và vào được phần online. Ngủ trong phòng đưa đồng hồ tới sáng hôm sau, mặc định 7:00.

## Chơi trên web

Chưa có URL chơi công khai trong repo. Khi bản WebGL được host, điền URL thật vào mục này. Đừng điền một địa chỉ giả.

Mẫu trang WebGL nằm ở `NihongoLife/Assets/WebGLTemplates/NihongoLife`. Xem trailer và kênh ở đầu trang.

## Bắt đầu nhanh

Cần Unity **6000.3.12f1** (`NihongoLife/ProjectSettings/ProjectVersion.txt`).

```bash
git clone https://github.com/StephenSouth13/NihongoLife.git
cd NihongoLife
```

Mở thư mục `NihongoLife/` bằng Unity Hub (đó là project, không phải gốc repo). Mở `Assets/NihongoLife/Scenes/00_Bootstrap.unity` và bấm Play. Không có bước dựng scene bằng menu editor. Builder dựng lại khu bằng `Unity -batchmode -executeMethod`, không bằng `[MenuItem]`.

Secrets không vào git. Copy file mẫu rồi điền giá trị của mình. Dòng còn dấu `<...>` bị bỏ qua, nên file mẫu để nguyên thì game chơi ngoại tuyến.

```bash
cp NihongoLife/LocalSecrets/secrets.example.json NihongoLife/LocalSecrets/secrets.json
```

Các khoá: `NIHONGOLIFE_SUPABASE_URL`, `NIHONGOLIFE_SUPABASE_ANON_KEY` (anon key, không phải service role), `AGORA_APP_ID`, `NIHONGOLIFE_GEMINI_API_KEY`. Biến môi trường cùng tên ghi đè file.

## Điều khiển

Phím mặc định trong `GameInputService`. Đổi trong màn hình cài đặt.

| Việc | Phím |
|---|---|
| Đi | W A S D |
| Chạy | Shift trái |
| Nhảy | Space |
| Tương tác | F |
| Balo | B |
| Bản đồ | M |
| Dùng vật phẩm | C |
| Biểu cảm | E |
| Hội thoại / chat | Enter |
| Nói, giữ để nói | T |
| Voice | V |
| Nhật ký | N |
| Bạn bè | U |
| Cửa hàng | P |
| Cài đặt | O |
| Nhân vật | Tab |
| Thả đồ | G |
| Tạm dừng | Esc |
| Xoay camera | dấu phẩy / chấm |
| Đổi mục tiêu tương tác | Q |
| Con trỏ | Ctrl trái |
| Tay cầm | Cột gamepad cạnh phím, trong cùng file |

Trên máy cảm ứng và trên WebGL khi trình duyệt báo con trỏ thô, `TouchControlOverlay` hiện joystick, vùng nhìn, và nút nhảy, tương tác, chạy, bản đồ, balo, chat, giữ để nói, cài đặt. Bàn phím vẫn dùng được.

Khi đang trên tàu, **Q** mở ngắm cảnh qua cửa sổ.

## Kiến trúc

Nội dung (kịch bản, câu thoại) tách khỏi máy chạy. `00_Bootstrap` giữ `GameServices`. Cảnh chơi hỏi dịch vụ qua interface, nên save local và save Supabase đổi được mà không viết lại hội thoại. Sơ đồ dưới đây bám `NihongoLife/Docs/ARCHITECTURE.md`.

```mermaid
graph TD
    Boot[00_Bootstrap] --> Services[GameServices]
    Services --> Scenes[SceneFlowController]
    Services --> Audio[AudioService]
    Services --> Save[LocalProgressRepository]
    Services --> Scenarios[LocalScenarioRepository]
    Town[Cảnh phố] --> Scenario[ScenarioManager]
    Scenario --> Dialogue[DialogueManager]
    Scenario --> Score[ScoringManager]
    Scenario --> Mastery[LearningMasteryManager]
    Secrets[secrets.json] -.-> Supabase
    Secrets -.-> Agora
    Save -.-> Supabase
```

`DialogueManager` có ba chế độ: hướng dẫn, luyện, kiểm tra. Chế độ quyết định có hiện cách đọc và nghĩa Việt hay không.

## Lộ trình

Repo chưa ghi ngày phát hành. Việc đội đang theo dõi nằm ở `NihongoLife/Docs/TEAM_TASKS.md`.

Mốc trí tuệ **100 / 160 / 240 / 320** là luật mở cửa của tài khoản trong game: sushi, ga, Game Center, đảo Midori. Khách không đi lộ trình này.

## Đóng góp

Pull request được chào. Đọc `AGENTS.md` trước khi sửa cảnh hoặc gameplay: không thêm `[MenuItem]` cho bước dựng, không chồng một môi trường mới lên môi trường đang có, và Play phải vào đúng trạng thái đã lưu trong scene.

Không commit `secrets.json`, key, hay đề thi có bản quyền.

Ba nhà sáng lập, The Next Generation, VTC Academy:

- Quách Thành Long — trưởng nhóm, web và game
- Nguyễn Lê Gia Huy — nhân vật, animation và gameplay
- Lương Nguyễn Tấn Dũng — nhân vật, animation và gameplay

Trang đội: https://website-the-next-generation-o323.vercel.app/team

Giải Triển vọng, Unique Project 2025, VTC Academy.

<img src="NihongoLife/Assets/NihongoLife/Resources/MenuGallery/01_team_award_2025.jpg" alt="Giấy khen Giải Triển vọng 2025" width="70%"/>

## Giấy phép

Mã nguồn là [MIT](LICENSE). Nhạc và tiếng môi trường trong game là CC0, ghi ở `NihongoLife/Docs/AUDIO_CREDITS.md`. Ảnh và model bên thứ ba giữ giấy phép của gói đó.

Music: "Ishikari Lore" by Kevin MacLeod (incompetech.com), licensed under CC BY 4.0

Track của trailer v4b là CC BY 4.0, khác quy ước CC0 của nhạc trong game.

---

## English

NihongoLife is a 3D life-sim in the Hibari-chō neighbourhood. Daily errands are Japanese practice: greet a neighbour, buy onigiri, ask a fare, sit an in-game paper, order sushi, play a kana machine, and later farm on Midori island.

The playable cast is eight kids. Haruto is the default. A guest plays offline with the whole town open. A registered account follows the knowledge route (trí tuệ: sushi 100, station 160, Game Center 240, Midori island 320) and can go online. Chat, friends, block, and push-to-talk (hold T) are in the game. Agora, Supabase and Gemini stay off until `NihongoLife/LocalSecrets/secrets.json` is filled from `secrets.example.json`.

In-game JLPT-style and IELTS-style practice is not an official score, and this repo does not use those organisations' logos.

Open `NihongoLife/` in Unity 6000.3.12f1 and press Play. There is no public play URL in the repo yet. The WebGL host template is `NihongoLife/Assets/WebGLTemplates/NihongoLife`. The approved trailer is `docs/readme/preview_v4b_720p.mp4` (720p, 67s). YouTube: https://www.youtube.com/channel/UCAKp-_EsasxPY2oa-4CzWtQ

Three founders: Quách Thành Long (lead, web and game), Nguyễn Lê Gia Huy and Lương Nguyễn Tấn Dũng (characters, animation and gameplay). The Next Generation, VTC Academy. Prospect Award, Unique Project 2025.

Code license: MIT. In-game music is CC0 (`NihongoLife/Docs/AUDIO_CREDITS.md`). The trailer track is separate: Music: "Ishikari Lore" by Kevin MacLeod (incompetech.com), licensed under CC BY 4.0.
