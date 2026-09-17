# Story Bible — Nihongo Life

> **Tóm tắt 1 dòng**: Một học viên nước ngoài đến sống ở Hibari-chō suốt một mùa xuân-hè, dần được cả khu phố nhớ mặt qua từng việc nhỏ (chào hỏi, mua đồ, giúp hàng xóm, ăn ramen, đi tàu), rồi cùng những học viên thật khác (chơi online) góp mặt vào lễ hội mùa hè cuối cùng — không có villain, chỉ có cảm giác "được thuộc về".

Tài liệu này định nghĩa cốt truyện xuyên suốt cho các scenario N5, để mọi scenario mới/cũ dùng chung một thế giới, một dàn nhân vật và một mạch cảm xúc thay vì là các lát cắt rời rạc như hiện tại. Đây là tài liệu sáng tạo (content), không thay đổi kiến trúc engine mô tả trong `ARCHITECTURE.md` / `SCENARIO_SYSTEM.md`.

## 1. Premise

Người chơi vừa chuyển đến sống ở một khu phố nhỏ hư cấu tên **ひばり町 (Hibari-chō — "phố Chim Sơn Ca")** để đi học/đi làm tại Nhật, theo diện **"Chương trình lưu trú giao lưu văn hoá Hibari"** (Hibari Cultural Exchange Residency) — một lý do nền nhẹ nhàng, không cần kể chi tiết trong dialogue, nhưng quan trọng vì nó giải thích **tự nhiên vì sao nhiều học viên thật (người chơi khác) cùng "sống" trong cùng một Hibari-chō**: họ đều là học viên khác trong cùng chương trình, xuất hiện qua hệ thống online (xem mục 11). Không có phản diện, không có drama lớn — mục tiêu xuyên suốt là **hòa nhập vào khu phố**: từ một người lạ chưa quen ai, đến khi được cả xóm nhớ mặt và mời tham gia lễ hội mùa hè cuối game.

Câu chuyện trải dài đúng 1 mùa: bắt đầu vào **đầu mùa xuân** (lúc mới chuyển tới, hoa nở, không khí mới mẻ hợp với "khởi đầu") và kết ở **lễ hội mùa hè** (Chapter 4) — khung thời gian ngắn, rõ ràng, giúp người chơi cảm nhận được "hành trình có điểm đầu điểm cuối" dù mỗi chapter học độc lập.

Tông truyện: **slice-of-life nhẹ nhàng** (kiểu Animal Crossing/Shizuku no Hibi), phù hợp trình độ N5, không tạo áp lực kịch tính lệch tông với mục đích e-learning. Chất lượng "cuốn" đến từ **sự nhất quán và các chi tiết callback nhỏ** (NPC nhớ bạn, nhắc lại chuyện cũ, môi trường phản ánh đúng mùa/thời gian) — không phải từ kịch tính lớn.

### 1.1 Địa danh cố định trong Hibari-chō

Dùng nhất quán tên địa danh sau trong mọi dialogue/mô tả từ giờ trở đi, thay vì mô tả chung chung "con phố"/"cửa hàng" — giúp thế giới có cảm giác thật và nhất quán giữa các chapter:

| Địa danh | Vai trò | Xuất hiện |
|---|---|---|
| **さくら通り (Sakura-dōri)** | Con đường chính của khu phố, nơi gặp Tanaka lần đầu | Ch1 |
| **ひばりコンビニ (Hibari Konbini)** | Cửa hàng tiện lợi | Ch2 |
| **ひばり神社 (Hibari Jinja — đền Hibari)** | Ngôi đền nhỏ đầu phố; Suzuki hay nhắc mèo thích lảng vảng ở đây | Nhắc ở Ch3 (lostcat), là nơi tổ chức lễ hội Ch4 |
| **やまだ食堂 (Yamada Shokudō)** | Quán ăn của Yamada, bán ramen | Ch3 |
| **ひばり駅 (Hibari Eki — ga Hibari)** | Nhà ga khu phố | Ch4 |

## 11. Đa người chơi trong cốt truyện (Online E-Learning)

Đây là **e-learning online**, không phải single-player thuần — hệ thống chat/presence/co-op/bạn bè/xếp hạng đã có sẵn trong code (xem `online_multiplayer_scaffolding` memory), chỉ cần khớp đúng ý nghĩa cốt truyện thay vì để trống ý nghĩa fiction:

- **Người chơi khác xuất hiện trong Hibari-chō như những học viên khác cùng chương trình** — không phải NPC, mà là presence thật (`SupabaseOnlineWorldService`/`RemotePlayerAvatar`). Không cần lời thoại giải thích điều này (phá nhịp slice-of-life) — người chơi tự hiểu khi thấy nhân vật khác đi lại trong cùng khu phố.
- **Kênh chat "town" = bảng tin khu phố (Hibari-chō Board).** Khung fiction: đây là nơi cư dân mới trao đổi, thực hành viết tiếng Nhật ngắn với nhau — không cần thay đổi code, chỉ cần khi có UI copy/placeholder text cho ô chat, dùng đúng khung này (vd. placeholder "Nhắn gì đó cho khu phố...", không phải "type a message" chung chung).
- **Co-op = "học cùng bạn" (study buddy).** `CoopParticipant.assigned_speaker` đã cho phép 2 người chơi thật nhận 2 vai thoại khác nhau trong cùng 1 scenario — đề xuất áp dụng cho các scenario có đúng 2 vai rõ ràng, tự nhiên nhất là:
  - `scenario.restaurant.order_ramen`: 1 người chơi vai khách, 1 người chơi tạm vai Yamada (đọc lại lời NPC) — luyện cả nghe lẫn nói 2 chiều.
  - `scenario.station.buy_ticket`: tương tự với Kimura.
  - Không áp dụng co-op cho `scenario.intro.arrival`/`town.summer_festival` (thuần narration, không có structure 2 vai rõ ràng).
- **Leaderboard = "reputation trong khu phố"**, không cần đổi tên hiển thị nhưng khi Sato/Tanaka nói chuyện ở lễ hội cuối (Ch4), có thể lồng 1 câu nhẹ nhàng ghi nhận tiến bộ (đã có sẵn hook: `progress.level`/`completedScenarios.Count` hiển thị trong `MainMenuUI.DisplayProfileStats`) — không cần thêm cơ chế mới, chỉ là văn phong khi viết node cuối game nên gợi ý "khu phố đã thấy bạn tiến bộ" thay vì chỉ nói chung chung.

**Việc không nên làm**: không viết NPC nào đại diện "người chơi khác" bằng dialogue node cứng — vì đó là chỗ của presence/avatar thật, viết cứng vào sẽ xung đột ý nghĩa và không tận dụng được multiplayer thật.

## 2. Nhân vật chính

- Ẩn danh, không có thoại/lời thoại riêng — học viên tự chiếu bản thân vào nhân vật.
- Không NPC nào gọi tên nhân vật chính trong dialogue (tránh phải nhánh theo `display_name` của `PlayerProfile`, việc này không cần thiết cho N5).
- `PlayerStatus.PlayerName` ("Học viên Nihongo") và `PlayerProfile.display_name` chỉ dùng cho UI hồ sơ/điểm số, không xuất hiện trong lời NPC.

## 3. Dàn nhân vật cố định (Cast)

Nguyên tắc: **mỗi NPC xuất hiện lại ở nhiều scenario**, giữ nguyên `speakerId`, tính cách nhất quán. Không tạo NPC dùng một lần rồi bỏ.

| speakerId | Tên | Vai trò | Tính cách ngắn gọn | Xuất hiện | Arc nhỏ xuyên suốt |
|---|---|---|---|---|---|
| `npc_neighbor_1` | Tanaka | Hàng xóm sát vách, người bạn đầu tiên, "hướng dẫn viên" không chính thức của khu phố | Thân thiện, hay chủ động bắt chuyện | Ch1 (street, house1), dẫn dắt sang Ch2, xuất hiện lại ở lễ hội Ch4 | Từ "người lạ tốt bụng chỉ đường" → "bạn thân đầu tiên" → ở lễ hội, chính Tanaka là người giới thiệu người chơi với cả khu phố như "bạn tôi" — khép lại vòng tròn từ người lạ thành người trong nhóm. |
| `npc_neighbor_2` | Suzuki | Hàng xóm nuôi mèo | Hơi đãng trí, ấm áp, hay cảm ơn quá mức | Ch3 (lostcat), quay lại Ch4 báo tin đã tìm được mèo | Mối lo nhỏ (mèo lạc) được gieo ở Ch3, giải quyết nhẹ nhàng ở Ch4 — không cần người chơi trực tiếp tìm ra mèo, chỉ cần đã quan tâm hỏi han là đủ để Suzuki nhớ ơn. |
| `npc_neighbor_3` | Sato | Trưởng khu phố (chōnaikai), phụ trách quy tắc sinh hoạt | Nghiêm túc nhưng tốt bụng, thích giải thích quy tắc | Ch3 (garbage), là người đứng ra tổ chức lễ hội ở Ch4 | Từ "người canh giữ luật lệ" (hơi xa cách) → người đầu tiên công nhận công khai sự tiến bộ của người chơi trước cả khu phố ở lễ hội — phần thưởng cảm xúc cho việc tuân thủ quy tắc nhỏ nhặt trước đó. |
| `npc_cashier` | Ito | Nhân viên cửa hàng tiện lợi | Lịch sự kiểu công việc (敬語 chuẩn mực) | Ch2, xuất hiện nền ở lễ hội Ch4 | Đại diện cho "giao dịch lịch sự kiểu Nhật" — ở lễ hội chỉ cần 1 câu chào nhận ra mặt quen, không cần arc lớn, đủ để không bị "dùng 1 lần rồi bỏ". |
| `npc_ramen_owner` | Yamada | Chủ quán ramen được Tanaka giới thiệu | Xuề xòa, nhiệt tình, hay mời thêm | Ch3 (ramen), bán hàng ở lễ hội Ch4 | Ban đầu là "chủ quán do Tanaka giới thiệu" → tới lễ hội đã tự nhận ra người chơi mà không cần Tanaka giới thiệu lại — dấu hiệu rõ ràng nhất cho thấy người chơi đã thực sự thành cư dân quen mặt. |
| `npc_station_staff` | Kimura | Nhân viên nhà ga | Chuyên nghiệp, nói nhanh, chuẩn văn phong hướng dẫn | Ch4 (station), có thể xuất hiện thoáng qua ở lễ hội | NPC "công việc" cuối cùng gặp trước lễ hội — đại diện thử thách nhỏ cuối "phải tự lo được việc hành chính/di chuyển" trước khi được đón nhận vào cộng đồng ở lễ hội. |

## 4. Cấu trúc chương (Main Quest)

Khớp với 4 chương curriculum trong `CONTENT_GUIDE.md`. Cột "Trạng thái" phản ánh đúng asset hiện có trong `Resources/Scenarios/`.

### Mở màn — `scenario.intro.arrival` (chapterIndex 0)

Cảnh dẫn nhập thuần narration trước Chapter 1, chạy tự động khi bấm "Start" (đã set `activeScenarioId` trong `GameControlDatabase` trỏ tới đây). Không có NPC, không rẽ nhánh thật (ngoại lệ so với mục 10 — đây là cinematic mở đầu, không phải bài học), chỉ 4 dòng narration + 1 lựa chọn xác nhận để chuyển sang `scenario.street.first_talk`. File: `Resources/Scenarios/scenario_intro_arrival.asset`.

### Chapter 1 — はじめまして (Chuyển đến Hibari-chō)
1. `scenario.street.first_talk` *(đã có)* — Gặp Tanaka lần đầu trên phố, được hỏi "đi đâu vậy".
2. `scenario.house1.greeting` *(đã có)* — Đến chào Tanaka chính thức tại nhà, tự giới thiệu bản thân. **Callback**: câu mở đầu nên nhắc đã gặp nhau ngoài đường lúc nãy, thay vì mở màn như người lạ hoàn toàn.
- Kết chương: Tanaka rủ ra cửa hàng tiện lợi gần đó → mở khóa Chapter 2.

### Chapter 2 — コンビニ
1. `scenario.konbini.buy_onigiri` *(đã có)* — Lần đầu tự mua đồ ăn, gặp nhân viên konbini.
- Kết chương: Tanaka nhắc "khu này còn có quán ramen ngon" → mở khóa Chapter 3.

### Chapter 3 — 近所とレストラン (Xóm giềng & Nhà hàng)
1. `scenario.house2.lostcat` *(đã có)* — Suzuki nhờ tìm mèo lạc. **Callback**: Suzuki có thể nhắc "đã thấy bạn nói chuyện với Tanaka hôm trước".
2. `scenario.house3.garbage` *(đã có)* — Sato hướng dẫn luật đổ rác.
3. `scenario.restaurant.order_ramen` *(MỚI — cần viết)* — Gọi món tại quán Yamada, Tanaka dẫn đường và giới thiệu.
- Kết chương: Sato nhắc tới "lễ hội mùa hè sắp tới của khu phố" → mở khóa Chapter 4.

### Chapter 4 — 駅と夏祭り (Nhà ga & Lễ hội mùa hè)
1. `scenario.station.buy_ticket` *(MỚI — cần viết)* — Hỏi đường và mua vé tại ga, với Kimura.
2. `scenario.town.summer_festival` *(MỚI — cảnh kết)* — Toàn bộ NPC cũ (Tanaka, Suzuki đã tìm được mèo, Sato, Yamada, nhân viên konbini) xuất hiện lại tại lễ hội, cảm ơn/ghi nhận người chơi đã hòa nhập vào khu phố. Đây là màn kết game, mang tính tổng kết cảm xúc, không cần learning target mới.

## 5. Vấn đề dữ liệu cần xử lý trước khi viết nội dung mới

`house1_greeting`, `house2_lostcat`, `house3_garbage` hiện đang gán `chapterIndex: 3` trong asset, dù nội dung thuộc về Chapter 1 (house1) và mạch "xóm giềng" của Chapter 3 (house2/house3) theo bảng trên. Đây là điểm lệch giữa dữ liệu hiện có và cấu trúc truyện đề xuất — cần cập nhật lại field `chapterIndex`:
- `house1_greeting`: 3 → **1**
- `house2_lostcat`, `house3_garbage`: giữ **3** (đúng với vị trí "xóm giềng" trong bible này)

Việc này chỉ sửa metadata, không đổi node/dialogue.

## 6. Quy tắc viết tiếp (Continuity Rules)

1. **Không NPC dùng một lần.** NPC mới chỉ được thêm nếu có khả năng quay lại ít nhất ở màn kết Chapter 4.
2. **Callback bằng lời thoại cứng.** Vì các scenario chạy tuyến tính theo chapter, không cần engine hỗ trợ rẽ nhánh theo lịch sử — chỉ cần dòng thoại đầu mỗi scenario mới nhắc lại 1 chi tiết từ scenario trước (tên người, sự kiện, địa điểm).
3. **speakerId cố định.** Luôn tái sử dụng đúng `speakerId` đã định nghĩa ở mục 3, không tạo id mới cho cùng một nhân vật.
4. **Giữ nguyên văn phong lịch sự trung tính** (đúng mức độ 敬語 phù hợp N5) — không thêm xung đột/hiểu lầm kịch tính.
5. **Không đặt lời thoại cho nhân vật chính.** Mọi phản hồi của người chơi đi qua `DialogueChoice`, không qua node kể chuyện có `speakerId` của player.

## 7. Đề xuất kỹ thuật đi kèm (không tự triển khai — cần xác nhận riêng)

- `PlayerProgressDto.completedScenarios` đã lưu sẵn danh sách scenario đã hoàn thành. Có thể tận dụng để thêm 1 field điều kiện nhỏ trên node (vd. `requiredCompletedScenarioId`) cho phép NPC thực sự phản ứng khác nếu người chơi đã/chưa hoàn thành scenario trước đó (vd. Suzuki nói khác nếu đã tìm ra mèo ở chapter khác trước khi gặp lại ở lễ hội). Đây là cải tiến engine nhỏ, đề xuất làm sau khi nội dung Chapter 3/4 đã có, không bắt buộc cho bản đầu vì truyện hiện tại tuyến tính.

## 8. Để trở thành một game E-learning hoàn chỉnh

Ngoài phần cốt truyện, đối chiếu với `TODO_CHECKLIST.md`, các việc sau ảnh hưởng trực tiếp tới trải nghiệm truyện và nên ưu tiên theo thứ tự:

1. **Viết nội dung 2 scenario còn thiếu** (`restaurant.order_ramen`, `station.buy_ticket`) — hiện là lỗ hổng lớn nhất, đã nằm trong Roadmap Phase 2.
2. **Viết cảnh kết `town.summer_festival`** — hiện game không có màn kết, học xong Chapter 4 sẽ hụt hẫng nếu không có cảnh tổng kết dùng lại toàn bộ NPC.
3. **Sửa `chapterIndex`** theo mục 5 để thứ tự chương hiển thị đúng mạch truyện.
4. **Thay NPC capsule placeholder bằng model thật + animation** (đã có trong checklist P1) — nhân vật lặp lại xuyên suốt càng cần diện mạo nhất quán để người chơi nhận ra ("à, lại là Tanaka").
5. **Audio thoại** cho các câu callback quan trọng (mở đầu mỗi chapter) trước, vì đây là các điểm cảm xúc chính của mạch truyện.

## 10. Chiều sâu tương tác & Animation (cập nhật theo phản hồi 2026-09-16)

Phản hồi từ chủ dự án: ưu tiên **ít scenario hơn nhưng sâu hơn**, thay vì dàn trải nhiều scenario ngắn/nông như 5 asset hiện có (đa số chỉ có 1 choice, không rẽ nhánh thật). Áp dụng cho `restaurant.order_ramen`, `station.buy_ticket`, `town.summer_festival` và mọi scenario viết mới sau này:

- **Rẽ nhánh thật, có hệ quả khác nhau** — mỗi node choice quan trọng nên có ≥2-3 lựa chọn dẫn tới `nextNodeId` khác nhau (không hội tụ về cùng 1 node ngay sau đó), với phản ứng NPC khác nhau và `scoreModifiers` khác nhau. Ví dụ ramen: gọi món đúng cách lịch sự vs. gọi trống không → Yamada phản ứng khác, dẫn tới nhánh hội thoại tiếp theo khác nhau, tối thiểu 1 nhánh có thể "yêu cầu lại" (recovery path) thay vì fail thẳng.
- **Ưu tiên chiều sâu hơn số lượng** — thà 3 scenario mới thật sự nhiều nhánh còn hơn 5-6 scenario ngắn kiểu cũ.
- **Tận dụng animation nhiều hơn thoại suông** — mỗi node quan trọng nên gắn `animationCue`. Bộ cue hiện engine hỗ trợ (`CharacterAnimationController`): `talk` (mặc định khi nói), `bow`, `point`. Nếu cần thêm cue mới (vd. `wave`, `surprised`, `laugh`, `look_menu`) thì đó là một hạng mục kỹ thuật riêng (thêm Animator trigger + xử lý trong `DialogueManager`/`CharacterAnimationController`) — xem `TEAM_TASKS.md`.
- Việc này không cần animation tay: `CharacterAnimationController` đã animate hoàn toàn bằng code (breathing, walk bob, talk sway, look-at, bow/point trigger) — không cần keyframe thủ công, chỉ cần thêm trigger param mới nếu muốn thêm cue.

## 9. Definition of Done cho nội dung truyện

- Mỗi scenario mới có ít nhất 1 dòng callback nhắc lại nhân vật/sự kiện trước đó.
- `speakerId` khớp bảng ở mục 3, không tạo id trùng ý nghĩa.
- `chapterIndex` khớp bảng ở mục 4/5.
- Không có nhân vật nào chỉ xuất hiện đúng 1 lần trong toàn bộ 4 chapter (trừ NPC nền không tên trong lễ hội).
- Cảnh kết Chapter 4 có mặt (trực tiếp hoặc được nhắc tên) toàn bộ NPC chính đã gặp trước đó.
