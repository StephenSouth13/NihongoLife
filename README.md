<div align="center">

<img src="docs/readme/hero.png" alt="NihongoLife — ひばり町で、日本語と暮らそう" width="100%"/>

<br/>

[![Unity](https://img.shields.io/badge/Unity-6000.3.12f1-000000?logo=unity&logoColor=white)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-.NET-512BD4?logo=csharp&logoColor=white)](#-kiến-trúc)
[![URP](https://img.shields.io/badge/Render-URP-1F3C88)](#-kiến-trúc)
[![Supabase](https://img.shields.io/badge/Supabase-online-3FCF8E?logo=supabase&logoColor=white)](#-dịch-vụ-trực-tuyến)
[![Gemini](https://img.shields.io/badge/Gemini-AI-8E75B2?logo=googlegemini&logoColor=white)](#-dịch-vụ-trực-tuyến)
[![Agora](https://img.shields.io/badge/Agora-RTC-099DFD)](#-dịch-vụ-trực-tuyến)
[![Play Mode](https://img.shields.io/badge/Play_Mode-16%2F16_passed-2E9E6B)](#-kiểm-thử)
[![EditMode](https://img.shields.io/badge/EditMode-11%2F11_passed-2E9E6B)](#-kiểm-thử)
[![License: MIT](https://img.shields.io/badge/License-MIT-F2B233.svg)](LICENSE)

**Học tiếng Nhật bằng cách sống ở Nhật.**
*Learn Japanese by living in Japan · 日本で暮らして、日本語を学ぼう*

[Gameplay](#-trải-nghiệm-trong-game) · [Bắt đầu](#-bắt-đầu-nhanh) · [Điều khiển](#-điều-khiển) · [Kiến trúc](#-kiến-trúc) · [Kiểm thử](#-kiểm-thử) · [Tài liệu](#-tài-liệu)

</div>

---

<div align="center">
<img src="docs/readme/gif_city.gif" alt="Bay qua phố Hibari-chō rồi đi bộ trên phố" width="80%"/>
<br/><sub><i>Phố Hibari-chō: khu phố Nhật thu nhỏ, nơi mọi bài học diễn ra.</i></sub>
</div>

## 🌸 NihongoLife là gì?

**NihongoLife** là game 3D mô phỏng cuộc sống cho người Việt bắt đầu học tiếng Nhật trình độ **N5**. Bạn vào vai một du học sinh vừa tới **ひばり町 (Hibari-chō)**: chào hàng xóm, mua cơm nắm ở konbini, hỏi đường, đi tàu tới trường, gọi sushi, làm bài thi thử… Mỗi việc trong ngày là một tình huống giao tiếp có thật, và **nói sai cũng không sao**: chọn sai sẽ có giải thích và cơ hội chọn lại.

<table>
<tr>
<td align="center" width="25%"><h3>14</h3>kịch bản N5<br/><sub>từ ngày đầu tới lễ hội mùa hè</sub></td>
<td align="center" width="25%"><h3>7</h3>khu vực chơi được<br/><sub>phố · konbini · phòng trọ · ga · sushi · trường · Game Center</sub></td>
<td align="center" width="25%"><h3>3</h3>ngôn ngữ giao diện<br/><sub>Tiếng Việt · English · 日本語</sub></td>
<td align="center" width="25%"><h3>5</h3>nhóm kỹ năng được chấm<br/><sub>từ vựng · ngữ pháp · nghe · phản xạ · hoàn thành</sub></td>
</tr>
</table>

---

## 🎮 Trải nghiệm trong game

### 💬 Hội thoại N5 có phản hồi

Một khung hội thoại dùng chung cho mọi nơi: câu tiếng Nhật hiện dần, kèm **cách đọc, romaji và nghĩa tiếng Việt** (tuỳ chế độ Hướng dẫn / Luyện tập / Kiểm tra). Chọn đáp án bằng phím 1–4 hoặc chuột. Chọn sai thì NPC giải thích và cho thử lại. Nút **×** đóng hội thoại bất cứ lúc nào mà không mất bước truyện; nhấn **R** để tiếp tục.

<p align="center"><img src="docs/readme/dialogue.jpg" width="85%" alt="Hộp thoại dùng chung"/></p>

### 🏪 Mua sắm ở ひばりマート

Đi tới từng kệ, xem **ảnh thật của từng món**, chọn số lượng, bỏ vào giỏ rồi ra quầy レジ. Chị Ito tính tiền, hỏi có cần túi không, và bạn trả lời bằng tiếng Nhật.

<table>
<tr>
<td width="50%"><img src="docs/readme/shop.jpg" alt="Kệ cơm nắm"/><p align="center"><sub>Kệ cơm nắm: ảnh, tên Nhật, giá ¥</sub></p></td>
<td width="50%"><img src="docs/readme/checkout.jpg" alt="Quầy thanh toán"/><p align="center"><sub>Quầy レジ: giỏ hàng có ảnh, tổng tiền</sub></p></td>
</tr>
</table>

<p align="center"><img src="docs/readme/items.png" width="80%" alt="Ảnh vật phẩm"/><br/><sub>Mỗi món có ảnh riêng, dùng chung cho cửa hàng, giỏ hàng và balo.</sub></p>

### 🚃 Đi tàu như người Nhật

Hỏi anh Kimura ở quầy vé (¥320, sân số 2), mua vé, qua cổng soát vé, chờ tàu và lên tàu. Trên tàu có bảng LED, sơ đồ tuyến và hành khách để bắt chuyện; nhấn **Q** để ngắm cảnh qua cửa sổ. Xuống ở Gakuen-mae để tới trường hoặc ở Minato để ăn sushi.

<table>
<tr>
<td width="33%"><img src="docs/readme/station.jpg" alt="Quầy vé"/><p align="center"><sub>Hỏi giá ở quầy vé</sub></p></td>
<td width="33%"><img src="docs/readme/train.jpg" alt="Trên tàu"/><p align="center"><sub>HUD trên tàu</sub></p></td>
<td width="33%"><img src="docs/readme/window.jpg" alt="Ngắm cảnh"/><p align="center"><sub>Q · ngắm cảnh</sub></p></td>
</tr>
</table>

### 🏫 Lớp học Hibari và luyện thi JLPT / IELTS

Ngồi vào bàn thi trong lớp, chọn đề **JLPT N5** hoặc **IELTS**, làm bài có đồng hồ đếm ngược. Nộp bài xong, lớp tắt đèn, pháo giấy bay và cô Morita nhận xét kết quả.

<table>
<tr>
<td width="33%"><img src="docs/readme/exam_desk.jpg" alt="Bàn thi"/><p align="center"><sub>Bàn thi · F</sub></p></td>
<td width="33%"><img src="docs/readme/exam_centre.jpg" alt="Chọn đề"/><p align="center"><sub>Chọn đề JLPT / IELTS</sub></p></td>
<td width="33%"><img src="docs/readme/teacher.jpg" alt="Nhận xét"/><p align="center"><sub>Cô Morita nhận xét</sub></p></td>
</tr>
</table>

### 🕹️ Game Center: Kana Match

Lật thẻ ghép cặp **hiragana, katakana, kanji N5, đồ ăn và từ vựng**. Có đếm ngược 3-2-1, combo, điểm trực tiếp, sao và vé thưởng; đổi vé lấy quà ở quầy của Aoi. Kết quả được ghi vào cùng hồ sơ học tập với các kịch bản.

<table>
<tr>
<td width="55%"><img src="docs/readme/gif_kana.gif" alt="Kana Match"/></td>
<td width="45%"><img src="docs/readme/kana_result.jpg" alt="Kết quả"/><p align="center"><sub>Bảng kết quả: sao, độ chính xác, vé</sub></p><img src="docs/readme/prize.jpg" alt="Quầy quà"/><p align="center"><sub>Quầy đổi quà</sub></p></td>
</tr>
</table>

### 🏠 Phòng trọ, chỉ số sống và hơn thế

<table>
<tr>
<td width="33%"><img src="docs/readme/bedroom.jpg" alt="Phòng trọ"/><p align="center"><sub>Phòng trọ 1K: học, nấu, ngủ</sub></p></td>
<td width="33%"><img src="docs/readme/study.jpg" alt="Học bài"/><p align="center"><sub>Thẻ học bài ở bàn</sub></p></td>
<td width="33%"><img src="docs/readme/morning.jpg" alt="Buổi sáng"/><p align="center"><sub>Ngủ dậy lúc 07:00</sub></p></td>
</tr>
<tr>
<td width="33%"><img src="docs/readme/bag.jpg" alt="Balo"/><p align="center"><sub>Balo [B]: ăn, bỏ, xem thể trạng</sub></p></td>
<td width="33%"><img src="docs/readme/map.jpg" alt="Bản đồ"/><p align="center"><sub>Bản đồ [M] + chỉ đường</sub></p></td>
<td width="33%"><img src="docs/readme/emote.jpg" alt="Biểu cảm"/><p align="center"><sub>Giữ E: vòng biểu cảm có câu Nhật</sub></p></td>
</tr>
</table>

**Chỉ số sống chạy như thật.** Chạy (Shift) tốn **14 thể lực/giây** và không hồi khi đang chạy. Hết thể lực thì bị khoá chạy tới khi hồi đủ 25 điểm, và HUD nháy đỏ "Hết sức". Đi và chạy làm đói, khát nhanh hơn; ăn uống và ngủ để hồi.

### 🌅 Thế giới liền mạch

Không còn mép bản đồ hay khoảng đen ở chân trời: khu nhà ngoại ô, đồi và sương đổi màu theo giờ trong ngày, cửa sổ sáng đèn khi trời tối.

<p align="center">
<img src="docs/readme/horizon_day.jpg" width="32%" alt="Ban ngày"/>
<img src="docs/readme/horizon_dusk.jpg" width="32%" alt="Hoàng hôn"/>
<img src="docs/readme/horizon_night.jpg" width="32%" alt="Ban đêm"/>
</p>

Nhân vật đi **thẳng lưng, bước rộng bằng hông** nhờ `PostureStabilizer` chỉnh xương sau Animator. Đo trên chu kỳ đi thật: vai lệch 6,3° → 2,1°, khoảng cách hai bàn chân hẹp nhất 6 cm → 15 cm.

<p align="center"><img src="docs/readme/posture.jpg" width="80%" alt="Dáng đi trước và sau"/></p>

---

## 🚀 Bắt đầu nhanh

**Yêu cầu:** Windows 10/11, **Unity 6000.3.12f1** (URP).

```bash
git clone https://github.com/StephenSouth13/NihongoLife.git
```

1. Mở thư mục **`NihongoLife/`** (thư mục Unity project bên trong repo) bằng Unity Hub.
2. Mở scene `Assets/NihongoLife/Scenes/00_Bootstrap.unity` và bấm **Play**. Cũng có thể Play thẳng từ bất kỳ khu nào (phòng trọ, ga…): game tự nạp thành phố làm nền nên luôn có HUD và lối ra.
3. *(Tuỳ chọn)* Bật AI bằng cách đặt khoá Gemini vào biến môi trường, rồi mở lại Unity:

   ```powershell
   setx NIHONGOLIFE_GEMINI_API_KEY "<khoá-của-bạn>"
   ```

> [!NOTE]
> Không có mạng hay khoá API thì game vẫn chạy đầy đủ ở chế độ offline: lưu cục bộ, NPC dùng câu soạn sẵn, bảng xếp hạng báo "chưa kết nối".

## ⌨️ Điều khiển

| Phím | Chức năng | Phím | Chức năng |
|:---:|---|:---:|---|
| <kbd>W</kbd><kbd>A</kbd><kbd>S</kbd><kbd>D</kbd> | Di chuyển | <kbd>Shift</kbd> | Chạy (tốn thể lực) |
| <kbd>F</kbd> | Nói chuyện / tương tác | <kbd>1</kbd>–<kbd>4</kbd> · <kbd>Enter</kbd> | Chọn đáp án · tiếp tục |
| <kbd>E</kbd> *(giữ)* | Vòng biểu cảm | <kbd>R</kbd> | Tiếp hội thoại đã tạm đóng |
| <kbd>B</kbd> | Balo | <kbd>Tab</kbd> | Nhân vật |
| <kbd>M</kbd> | Bản đồ | <kbd>J</kbd> | Nhiệm vụ |
| <kbd>K</kbd> | Luyện thi | <kbd>V</kbd> | Luyện phát âm (AI) |
| <kbd>Q</kbd> | Ngắm cảnh trên tàu | <kbd>F8</kbd> | Lớp học video |
| <kbd>Esc</kbd> / **×** | Đóng cửa sổ · cài đặt | 🖱️ | Xoay camera, bấm nút |

## 🗺️ Thế giới

<p align="center"><img src="docs/readme/d11_world_hub.png" width="85%" alt="Bản đồ khu vực"/></p>

| Scene | Khu vực |
|---|---|
| `00_Bootstrap` → `01_MainMenu` | Khởi động, chọn ngôn ngữ và nhân vật |
| `90_TestSandbox` | Phố Hibari-chō, konbini ひばりマート, đường chân trời |
| `20_StationDistrict` | Ga Hibari, tàu tới Gakuen-mae và Minato |
| `30_SushiRestaurant` | Nhà hàng Sushi Hibari |
| `40_ HIBARICLASS` | Trường Nhật ngữ Hibari, bàn thi |
| `45_HomeBedroom` | Phòng trọ Hibari Heights |
| `50_GameCenter` | Game Center, Kana Match, quầy quà |

Mọi khu vực được nạp thêm (additive) trên nền thành phố và có lối ra cố định về đúng cửa đã vào.

---

## 🧩 Kiến trúc

<p align="center"><img src="docs/readme/d08_architecture.png" width="85%" alt="Kiến trúc phân lớp"/></p>

- **Kịch bản theo dữ liệu.** Mỗi bài học là một `ScenarioDefinition` (ScriptableObject): node, lựa chọn, mục tiêu, điều kiện mở khoá. Thêm bài mới không cần viết code gameplay mới.
- **Service locator.** `AppRoot` đăng ký các dịch vụ (lưu, âm thanh, chuyển scene, online) vào `GameServices`. Mỗi dịch vụ có bản local và bản cloud dùng chung interface.
- **Chấm điểm thống nhất.** `ScoringManager` gom điểm từ hội thoại, bài thi và mini-game theo 5 nhóm kỹ năng; `LearningMasteryManager` theo dõi mức thuộc từng mục.
- **Khung mini-game dùng chung.** `MiniGameController` cùng interface `IMiniGame`: trò mới chỉ cần hiện thực `Begin` / `Abort` / `Finished`.
- **Mọi thứ dựng bằng builder.** Scene và prefab được tạo bằng editor builder chạy batchmode, lưu thẳng vào asset; mở project và bấm Play là đúng trạng thái cuối.

<details>
<summary><b>📐 Sơ đồ lớp</b></summary>
<br/>
<img src="docs/readme/d13_class.png" alt="Sơ đồ lớp"/>
</details>

<details>
<summary><b>👤 Sơ đồ use case</b></summary>
<br/>
<img src="docs/readme/d12_use_case.png" alt="Use case"/>
</details>

<details>
<summary><b>🔁 Sơ đồ tuần tự: hội thoại học tập và Kana Match</b></summary>
<br/>
<img src="docs/readme/d14_seq_dialogue.png" alt="Tuần tự hội thoại"/>
<br/><br/>
<img src="docs/readme/d16_seq_minigame.png" alt="Tuần tự mini-game"/>
</details>

<details>
<summary><b>⚡ Máy trạng thái thể lực và vòng lặp cốt lõi</b></summary>
<br/>
<img src="docs/readme/d19_state_stamina.png" alt="Máy trạng thái thể lực"/>
<br/><br/>
<img src="docs/readme/d01_core_loop.png" alt="Vòng lặp cốt lõi"/>
</details>

### ☁️ Dịch vụ trực tuyến

<p align="center"><img src="docs/readme/d22_deployment.png" width="85%" alt="Sơ đồ triển khai"/></p>

| Dịch vụ | Dùng cho | Trạng thái |
|---|---|---|
| **Supabase** | Đăng nhập, lưu tiến trình cloud, hồ sơ, xếp hạng, bạn bè, chat, co-op | ✅ Có mã đầy đủ, tự chuyển offline khi thiếu cấu hình |
| **Google Gemini** | Chấm phát âm <kbd>V</kbd>, NPC trả lời bằng AI, chấm IELTS Writing | ✅ Dùng `gemini-flash-latest`, đã gọi thử thành công |
| **Agora RTC** | Lớp học video <kbd>F8</kbd> cùng giáo viên | 🟡 Đã nối SDK; cuộc gọi giữa hai máy chưa kiểm chứng |

<details>
<summary><b>🗄️ Mô hình dữ liệu Supabase</b></summary>
<br/>
<img src="docs/readme/d18_erd.png" alt="ERD"/>
</details>

### 📁 Cấu trúc thư mục

```text
NihongoLife/                         ← repo
├── NihongoLife/                     ← Unity project (mở thư mục này)
│   ├── Assets/NihongoLife/
│   │   ├── Scenes/                  00_Bootstrap … 50_GameCenter, 90_TestSandbox
│   │   ├── Scripts/                 Core · Scenario · Dialogue · Player · UI · World · Exam · School · Home …
│   │   ├── MiniGames/               Core (IMiniGame, MiniGameController) · KanaMatch · Data
│   │   ├── Resources/               Scenarios (14) · Exams · Items (ảnh vật phẩm) · Control
│   │   └── Tests/                   PlayMode (13 bộ) · EditMode
│   └── Docs/                        ARCHITECTURE · STORY_BIBLE · SCENARIO_SYSTEM · TEAM_TASKS …
├── Bao_Cao/                         báo cáo Capstone, slide, trailer, ảnh kiểm thử
└── docs/readme/                     hình ảnh cho README này
```

---

## ✅ Kiểm thử

Mỗi bộ kiểm thử Play Mode **điều khiển nhân vật bằng thao tác thật** (đi, nhấn F, chọn đáp án, bấm nút) và **tự chụp ảnh từng bước** vào `Bao_Cao/*-regression/` để người duyệt xem lại.

| Bộ kiểm thử | Kiểm tra |
|---|---|
| `GameplayUiPlayModeTests` | Hội thoại + nút ×, konbini → giỏ → thu ngân → balo, bản đồ, HUD, biểu cảm, hai chuyến tàu |
| `CompletionAuditPlayModeTests` | Chạy thật qua bộ điều khiển, cạn/hồi thể lực, đóng và tiếp lời dẫn truyện |
| `WorldFeelPlayModeTests` | Đường chân trời ngày/chiều/tối, thể lực, dáng đi thẳng lưng |
| `GameCenterPlayModeTests` | Kana Match trọn vòng, đổi quà |
| `SchoolPlayModeTests` | Bàn thi, chọn JLPT/IELTS, làm bài, nhận xét |
| `BedroomPlayModeTests` · `CityTown` · `ZoneTransition` · `Scenario` | Phòng trọ, lối vào các khu, chuyển khu hai chiều, chạy kịch bản |

**Kết quả gần nhất (08/10/2026):** Play Mode **16/16** · EditMode **11/11** · hơn 150 ảnh tự chụp.

<details>
<summary><b>Chạy kiểm thử và builder bằng dòng lệnh</b></summary>

```bash
# Đóng Unity Editor trước khi chạy batchmode
UNITY="C:/Program Files/Unity/Hub/Editor/6000.3.12f1/Editor/Unity.exe"

"$UNITY" -batchmode -projectPath NihongoLife -runTests -testPlatform PlayMode -testResults playmode.xml
"$UNITY" -batchmode -projectPath NihongoLife -runTests -testPlatform EditMode -testResults editmode.xml

# Dựng lại một khu vực (lưu thẳng vào scene, không cần thao tác menu)
"$UNITY" -batchmode -projectPath NihongoLife -executeMethod NihongoLife.EditorTools.GameCenterBuilder.Build -logFile build.log
```

| Builder (`NihongoLife.EditorTools.*`) | Dựng |
|---|---|
| `CityTownBuilder.Build` | Phố Hibari-chō, konbini, cổng khu vực |
| `HomeBedroomBuilder.Build` | Phòng trọ |
| `StationTrainBuilder.Build` · `StationClerkBuilder.Build` | Ga, tàu, quầy vé |
| `ClassroomBuilder.Build` | Lớp học, bàn thi |
| `GameCenterBuilder.Build` | Game Center |
| `HorizonBuilder.Build` | Đường chân trời |
| `ItemIconRenderer.Render` | Ảnh render vật phẩm |
| `TrailerReelSceneBuilder.Build` | Scene quay trailer |

</details>

## 🤝 Quy trình làm việc

<p align="center"><img src="docs/readme/d24_dev_workflow.png" width="80%" alt="Quy trình nhóm"/></p>

Quy tắc của repo (xem [`AGENTS.md`](AGENTS.md)): không dùng lệnh `[MenuItem]`; không tạo scene mới khi chưa được duyệt; lưu nội dung hoàn chỉnh thẳng vào scene/prefab; không chồng môi trường mới lên môi trường cũ; chỉ coi một scene là xong khi đã kiểm thử hình ảnh và va chạm trong Play Mode.

## 📚 Tài liệu

| | |
|---|---|
| 📘 **Báo cáo Capstone** | [`Bao_Cao/NihongoLife_Bao_Cao_Capstone.pdf`](Bao_Cao/NihongoLife_Bao_Cao_Capstone.pdf) · 75 trang, 13 sơ đồ UML |
| 🎞️ **Slide thuyết trình** | [`Bao_Cao/NihongoLife_Thuyet_Trinh.pptx`](Bao_Cao/NihongoLife_Thuyet_Trinh.pptx) |
| 🎬 **Trailer** | [`Bao_Cao/NihongoLife_Trailer.mp4`](Bao_Cao/NihongoLife_Trailer.mp4) · 1080p, 67 giây |
| 🧭 **Thiết kế** | [`ARCHITECTURE`](NihongoLife/Docs/ARCHITECTURE.md) · [`STORY_BIBLE`](NihongoLife/Docs/STORY_BIBLE.md) · [`SCENARIO_SYSTEM`](NihongoLife/Docs/SCENARIO_SYSTEM.md) · [`EXAM_SYSTEM`](NihongoLife/Docs/EXAM_SYSTEM.md) · [`ROADMAP`](NihongoLife/Docs/ROADMAP.md) |

## 🙏 Ghi công

[Unity](https://unity.com/) · [Kenney](https://kenney.nl/) · [Quaternius](https://quaternius.com/) · [KayKit](https://kaylousberg.itch.io/) · [Mixamo](https://www.mixamo.com/) · [Noto Sans JP](https://fonts.google.com/noto/specimen/Noto+Sans+JP) · [Supabase](https://supabase.com/) · [Google Gemini](https://ai.google.dev/) · [Agora](https://www.agora.io/)

<div align="center">
<br/>

**Quách Thành Long** · K24GD03 · Project 3 Capstone · VTC Academy · 2026
<br/>
Giảng viên hướng dẫn: Nguyễn Ngọc Chấn

<sub>Phát hành theo giấy phép <a href="LICENSE">MIT</a>.</sub>

<br/>

<i>ひばり町で、また会いましょう。 · Hẹn gặp lại ở Hibari-chō.</i>

</div>
