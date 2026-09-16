# Team Tasks — Nihongo Life

Phân công công việc giữa 3 "nhân sự" AI: **Claude** (nội dung + quản lý/tổng hợp), **Codex**, **Antigravity** (2 nhánh kỹ thuật độc lập). Mỗi task card dưới đây viết đủ ngữ cảnh để dán thẳng làm prompt cho Codex/Antigravity mà không cần giải thích lại từ đầu, vì Claude không thể nhắn trực tiếp cho 2 công cụ đó trong phiên này (không nằm trong danh sách agent có thể `SendMessage`).

## Vai trò

| Ai | Phụ trách | Vì sao |
|---|---|---|
| **Claude** | Nội dung cốt truyện (3 scenario còn thiếu), sửa `chapterIndex`, review/tích hợp code từ Codex & Antigravity vào đúng convention, cập nhật `STORY_BIBLE.md`/`TODO_CHECKLIST.md` | Đã có toàn bộ ngữ cảnh cốt truyện + kiến trúc scenario trong phiên này |
| **Codex** | TASK-A: Tổng quát hoá pipeline auto-import nhân vật (`CharacterBuilder.cs`) | Việc thuần Editor-tooling C#, tự chứa, không cần biết cốt truyện |
| **Antigravity** | TASK-B: Hệ thống trailer code-driven (camera director + shot list) | Việc thuần dựng hệ thống mới, tự chứa, không đụng gameplay hiện có |

Nếu bạn thấy hợp hơn thì đổi chéo Codex/Antigravity — 2 task độc lập với nhau nên hoán đổi không ảnh hưởng gì.

---

## TASK-A (Codex) — Auto-import pipeline cho nhân vật/asset mới

**Bối cảnh đã có sẵn**: `Assets/NihongoLife/Scripts/Editor/CharacterBuilder.cs` (menu `NihongoLife/Characters/Build Character System`) đã tự động hoá gần hết việc "biến FBX thô thành nhân vật dùng được":
- Cấu hình ModelImporter sang Humanoid (`ConfigureModel`).
- Cấu hình animation clip (idle/walk/talk/bow/point) từ Mixamo.
- Generate `Assets/NihongoLife/Animations/NL_Humanoid.controller` với đủ state/trigger (`Speed`, `IsTalking`, `Bow`, `Point` — khớp `CharacterAnimationController.cs`).
- Tạo prefab visual trong `Assets/NihongoLife/Prefabs/Characters`.

**Vấn đề**: 4 nhân vật đang bị hardcode theo đường dẫn cố định (`REMY_PATH`, `ELIZABETH_PATH`, `LILLY_PATH`, `EMINEM_PATH`). Muốn thêm NPC mới (vd. `npc_ramen_owner`, `npc_station_staff` — xem `STORY_BIBLE.md` mục 3) phải sửa tay code này mỗi lần.

**Yêu cầu**:
1. Viết lại/bổ sung thành pipeline quét thư mục thay vì hardcode path: quét toàn bộ `Assets/ThirdParty/Mixamo/Characters/*/source/*.fbx`, với quy ước đặt tên **tên thư mục con = `speakerId`** (vd. thư mục `npc_ramen_owner/source/xxx.fbx`).
2. Với mỗi nhân vật chưa có prefab tương ứng trong `Assets/NihongoLife/Prefabs/Characters/NL_<speakerId>.prefab`, tự động: `ConfigureModel` → gán chung `NL_Humanoid.controller` → `CreateVisualPrefab` (tái dùng logic đã có, không viết lại từ đầu — đọc kỹ file trước khi sửa).
3. Bỏ qua (skip, log rõ ràng) nhân vật đã có prefab rồi, không build lại đè lên tuỳ chỉnh thủ công đã làm trong Inspector.
4. Giữ nguyên `[MenuItem("NihongoLife/Characters/Build Character System")]` làm entry point chính; có thể thêm `[MenuItem("NihongoLife/Characters/Scan New Characters")]` riêng nếu tách logic quét khỏi logic build 4 nhân vật gốc — miễn không phá hành vi cũ.
5. Không tự ý đổi animation set mặc định (idle/walk/talk/bow/point) trừ khi asset FBX thiếu clip tương ứng — trường hợp đó log warning, dùng animation cue gần nhất làm fallback thay vì crash.

**Không làm**: không cần tự động đặt prefab vào scene hay gán `npcId` trên `NPCController` — bước đó vẫn thủ công (kéo prefab vào scene + set field), vì cần quyết định vị trí/scenario cụ thể.

**Bonus fix bắt buộc (rủi ro đã xác nhận trong code, không phải giả định)**: `NPCStreetPatrol.cs` gọi `_animation.SetSpeed(_smoothVelocity.magnitude)` với tốc độ di chuyển thật (~1.15 m/s, dao động 0.88x–1.12x theo `_speedPersonality`), nhưng `GenerateAnimatorController()` trong `CharacterBuilder.cs` chỉ tạo transition Idle↔Walk theo ngưỡng `Speed`, **không calibrate tốc độ phát của Walk state** theo tốc độ di chuyển thật của clip Mixamo. Đây chính là nguyên nhân gây hiện tượng chân trượt/"cà nhắc" mà chủ dự án lo ngại. Khi generate/tổng quát hoá animator controller, thêm bước set `walkState.speed` = tỉ lệ giữa tốc độ di chuyển thật (đọc từ `NPCStreetPatrol.walkSpeed`, để hằng số/field chung thay vì hardcode) và tốc độ bước chân vốn có của clip Walk, để foot speed khớp tốc độ di chuyển thật thay vì fix cứng bằng 1.

**Acceptance criteria**:
- Thêm 1 thư mục FBX Mixamo mới đúng convention → chạy menu → có prefab mới trong `Prefabs/Characters` mà không phải sửa code.
- Chạy lại trên nhân vật cũ (Remy/Elizabeth/Lilly/Eminem) không tạo trùng/không lỗi.
- Không phá vỡ `EditMode`/`PlayMode` test hiện có (`ScenarioEngineTests`, xem `DEVELOPMENT.md`).
- Cho NPC chạy patrol thật trong `90_TestSandbox`, quan sát bằng mắt: chân không trượt/lê rõ rệt so với tốc độ di chuyển.

---

## TASK-B (Antigravity) — Hệ thống Trailer code-driven

**Bối cảnh**: Chủ dự án muốn 1 đoạn trailer chất lượng cao nhưng **không muốn tự tay keyframe animation**. Tin tốt: `CharacterAnimationController.cs` (`Assets/NihongoLife/Scripts/Core/`) đã animate nhân vật hoàn toàn bằng code — breathing, walk bob, talk sway, look-at, và 2 gesture trigger (`Bow`, `Point`) — nên trailer có thể dựng hoàn toàn bằng script + số liệu, không cần vẽ animation.

**Chưa có trong project**: package Timeline/Cinemachine chưa được cài (`Packages/manifest.json` không có `com.unity.timeline`/`com.unity.cinemachine`). Ưu tiên **không thêm dependency mới** nếu không cần thiết — dùng cách tiếp cận nhẹ, nhất quán với style hiện có của project (data-driven ScriptableObject, giống `ScenarioDefinition`).

**Yêu cầu — thiết kế data-driven giống `ScenarioDefinition`**:
1. Tạo `TrailerShotDefinition` (ScriptableObject, namespace gợi ý `NihongoLife.Trailer`), mỗi shot gồm: thứ tự, vị trí/góc camera đầu-cuối (hoặc tham chiếu 2 `Transform` đặt sẵn trong scene), thời lượng, easing (dùng `AnimationCurve` là đủ, không cần Cinemachine), `focusTarget` (Transform NPC để camera look-at), `animationCueToTrigger` (gọi lại đúng `TriggerBow()`/`TriggerPoint()`/`SetTalking(true)` có sẵn), text overlay 2 dòng (JP + EN/VI, tái dùng field pattern `textJa`/`textEn` như `ScenarioDefinition` cho nhất quán), và cờ "fade to black" giữa các shot.
2. Tạo `TrailerDirector` (MonoBehaviour) đọc danh sách `TrailerShotDefinition`, chạy tuần tự bằng `Coroutine`/`Lerp` giữa 2 điểm camera theo `AnimationCurve`, gọi trigger animation, hiện overlay UI (dùng UI hiện có kiểu `DialogueManager` nếu tiện tái sử dụng).
3. Tạo scene riêng `Assets/NihongoLife/Scenes/99_Trailer.unity` (hoặc thư mục tương đương theo cấu trúc scene hiện có mô tả trong `DEVELOPMENT.md`) dùng lại môi trường/nhân vật đã build (Kenney assets + prefab từ TASK-A) để dàn 5-8 shot mẫu.
4. Gợi ý nội dung 6-8 shot mẫu (điền placeholder, nội dung thật sẽ do Claude/chủ dự án chỉnh sau khi có model thật):
   - Toàn cảnh phố Hibari-chō (establishing shot).
   - Cận cảnh Tanaka vẫy tay chào (`Point`/`Bow` trigger).
   - Nhân vật chính bước vào konbini.
   - Cận cảnh bát ramen bốc khói tại quán Yamada.
   - Suzuki ôm mèo, cảm ơn (`Bow`).
   - Toàn cảnh ga tàu.
   - Cảnh lễ hội mùa hè quy tụ NPC (dùng chung block-out).
   - Logo/tên game "Nihongo Life" full màn hình kết thúc.
5. **Không cần** thêm Cinemachine ngay — nếu thấy custom Lerp không đủ mượt, có thể đề xuất thêm `com.unity.cinemachine` như bước 2 (ghi rõ lý do trong PR), Claude sẽ duyệt trước khi merge dependency mới.

**Rủi ro cần tránh (chân đi cà nhắc)**: chủ dự án lo ngại animation thuần code dễ gây chân trượt/cà nhắc — lo ngại này đúng và đã xác nhận là bug có thật trong hệ thống patrol hiện tại (xem ghi chú "Bonus fix bắt buộc" ở TASK-A). Để trailer không dính lỗi tương tự hoặc tệ hơn:
- **Không tự chế logic đi bộ mới cho NPC trong trailer.** Nếu 1 shot cần NPC di chuyển, tái dùng nguyên `NPCStreetPatrol`/`NavMeshAgent` đang chạy sẵn trong scene (đã chạy thật ở gameplay, sẽ tự hưởng lợi khi Codex sửa lỗi calibrate tốc độ ở TASK-A) — không viết code di chuyển transform NPC riêng cho trailer.
- Phần "code-driven, không cần biết animation" trong task này chỉ áp dụng cho **camera** (vị trí/góc/thời lượng/easing) và **trigger gesture có sẵn** (`Bow`/`Point`/`SetTalking`), không áp dụng cho việc tự tạo chuyển động chân mới.
- Ưu tiên shot camera pan/dolly/zoom quanh NPC đứng yên hoặc đang patrol tự nhiên, thay vì đạo diễn NPC đi theo đường mới riêng cho trailer.

**Acceptance criteria**:
- Mở scene `99_Trailer`, bấm Play → trailer tự chạy hết danh sách shot không cần thao tác tay.
- Đổi thứ tự/thời lượng/nội dung overlay của 1 shot chỉ bằng cách sửa `TrailerShotDefinition` asset trong Inspector, không cần sửa code.
- Không phá vỡ scene/script gameplay hiện có (`90_TestSandbox`, `ScenarioManager`, v.v.) — hệ thống trailer phải độc lập hoàn toàn.

---

## Việc của Claude (song song, không chờ Codex/Antigravity)

1. Sửa `chapterIndex` cho `house1_greeting` (3→1) theo `STORY_BIBLE.md` mục 5.
2. Viết `scenario.restaurant.order_ramen`, `scenario.station.buy_ticket`, `scenario.town.summer_festival` theo nguyên tắc rẽ nhánh sâu ở `STORY_BIBLE.md` mục 10 (nhiều nhánh, hệ quả khác nhau, dùng đa dạng `animationCue`).
3. Sau khi TASK-A/TASK-B có kết quả: review code theo convention hiện có (service locator, ScriptableObject data-driven, namespace `NihongoLife.*`), đảm bảo không phá test hiện có, cập nhật `TODO_CHECKLIST.md`.

## Trạng thái

- [ ] TASK-A (Codex) — chưa giao
- [ ] TASK-B (Antigravity) — chưa giao
- [x] Nội dung 3 scenario mới (Claude) — đã viết xong dạng draft: `scenario_restaurant_order_ramen.asset`, `scenario_station_buy_ticket.asset`, `scenario_town_summer_festival.asset` (rẽ nhánh thật theo mục 10 `STORY_BIBLE.md`, có nhánh sai/nhánh sửa sai). **Chưa mở Unity để playtest/verify** — xem việc còn thiếu bên dưới trước khi coi là xong.
- [x] Sửa `chapterIndex` `house1_greeting` (3→1) theo mục 5 `STORY_BIBLE.md`.

### Việc còn thiếu để 3 scenario mới chạy được thật (không chỉ là data)

- 2 NPC mới cần dựng nhân vật thật: `npc_ramen_owner` (Yamada), `npc_station_staff` (Kimura) — dùng đúng pipeline TASK-A khi Codex hoàn thành, quy ước tên thư mục FBX phải khớp 2 `speakerId` này.
- 2 khu vực trigger mới chưa tồn tại trong scene, cần dựng: `ramen_shop_entrance` (`targetAreaId` trong `scenario_restaurant_order_ramen`), `station_entrance` (`targetAreaId` trong `scenario_station_buy_ticket`). Việc này thuộc phạm vi dựng scene (`SceneBuilder.cs`/thủ công trong Editor), chưa gán cho ai — Claude sẽ làm khi có model NPC từ TASK-A, hoặc báo lại nếu muốn giao riêng.
- Cần chạy Unity Editor để mở từng scenario, kiểm tra node graph không lỗi tham chiếu, rồi playtest thật (đúng quy tắc trong `TODO_CHECKLIST.md`: không tick hoàn thành chỉ vì đã viết code/data).
