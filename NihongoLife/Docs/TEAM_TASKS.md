# Team Tasks — Nihongo Life

Phân công công việc giữa 3 "nhân sự" AI: **Claude** (nội dung + quản lý/tổng hợp), **Codex**, **Antigravity** (2 nhánh kỹ thuật độc lập). Mỗi task card dưới đây viết đủ ngữ cảnh để dán thẳng làm prompt cho Codex/Antigravity mà không cần giải thích lại từ đầu, vì Claude không thể nhắn trực tiếp cho 2 công cụ đó trong phiên này (không nằm trong danh sách agent có thể `SendMessage`).

## Vai trò

| Ai | Phụ trách | Vì sao |
|---|---|---|
| **Claude** | Nội dung cốt truyện (3 scenario còn thiếu), sửa `chapterIndex`, review/tích hợp code từ Codex & Antigravity vào đúng convention, cập nhật `STORY_BIBLE.md`/`TODO_CHECKLIST.md` | Đã có toàn bộ ngữ cảnh cốt truyện + kiến trúc scenario trong phiên này |
| **Codex** | TASK-A: Tổng quát hoá pipeline auto-import nhân vật (`CharacterBuilder.cs`) | Việc thuần Editor-tooling C#, tự chứa, không cần biết cốt truyện |
| **Antigravity** | TASK-B: Hệ thống trailer code-driven (camera director + shot list) | Việc thuần dựng hệ thống mới, tự chứa, không đụng gameplay hiện có |

Nếu bạn thấy hợp hơn thì đổi chéo Codex/Antigravity — 2 task độc lập với nhau nên hoán đổi không ảnh hưởng gì.

**Cập nhật (2026-09-17)**: Chủ dự án không muốn auto-build khi mở Unity và cũng không muốn quay lại menu `NihongoLife/...`. Đã bỏ hệ `AutoBuildOnLoad`/`[InitializeOnLoad]`. Từ giờ build theo từng bước có chủ đích: gọi trực tiếp từng hàm build từ script tạm, test, hoặc batch step rõ ràng, ví dụ `CharacterBuilder.BuildCharacterSystem()` rồi mới tới `TrailerSceneBuilder.BuildTrailerSceneSafe()` khi thật sự cần.

**Không còn auto-installer/editor setup cho scene gameplay**: không commit script kiểu `GameplayAreaInstaller`/`SafeGameplayAreaInstaller`, không để bất kỳ editor auto-run nào tự mở và ghi `90_TestSandbox.unity`. Ramen/station phải được chỉnh trực tiếp trong scene thật hoặc bằng script tạm không commit.

Nếu về sau cần force reset thật (hiếm khi cần), gọi trực tiếp `CharacterBuilder.ForceRebuildLegacyCharacters()` / `TrailerSceneBuilder.ForceRebuildTrailerScene()` qua 1 script tạm hoặc Test Runner — không auto-run và không còn menu để bấm nữa.

---

## TASK-A (Codex) — Auto-import pipeline cho nhân vật/asset mới

**Bối cảnh đã có sẵn**: `Assets/NihongoLife/Scripts/Editor/CharacterBuilder.cs` đã tự động hoá gần hết việc "biến FBX thô thành nhân vật dùng được":
- Cấu hình ModelImporter sang Humanoid (`ConfigureModel`).
- Cấu hình animation clip (idle/walk/talk/bow/point) từ Mixamo.
- Generate `Assets/NihongoLife/Animations/NL_Humanoid.controller` với đủ state/trigger (`Speed`, `IsTalking`, `Bow`, `Point` — khớp `CharacterAnimationController.cs`).
- Tạo prefab visual trong `Assets/NihongoLife/Prefabs/Characters`.

**Vấn đề**: 4 nhân vật đang bị hardcode theo đường dẫn cố định (`REMY_PATH`, `ELIZABETH_PATH`, `LILLY_PATH`, `EMINEM_PATH`). Muốn thêm NPC mới (vd. `npc_ramen_owner`, `npc_station_staff` — xem `STORY_BIBLE.md` mục 3) phải sửa tay code này mỗi lần.

**Yêu cầu**:
1. Viết lại/bổ sung thành pipeline quét thư mục thay vì hardcode path: quét toàn bộ `Assets/ThirdParty/Mixamo/Characters/*/source/*.fbx`, với quy ước đặt tên **tên thư mục con = `speakerId`** (vd. thư mục `npc_ramen_owner/source/xxx.fbx`).
2. Với mỗi nhân vật chưa có prefab tương ứng trong `Assets/NihongoLife/Prefabs/Characters/NL_<speakerId>.prefab`, tự động: `ConfigureModel` → gán chung `NL_Humanoid.controller` → `CreateVisualPrefab` (tái dùng logic đã có, không viết lại từ đầu — đọc kỹ file trước khi sửa).
3. Bỏ qua (skip, log rõ ràng) nhân vật đã có prefab rồi, không build lại đè lên tuỳ chỉnh thủ công đã làm trong Inspector.
4. Không thêm lại `[MenuItem]`/menu bar `NihongoLife/...` và không thêm auto-build khi mở Unity. Entry point chính là gọi trực tiếp `CharacterBuilder.BuildCharacterSystem()` từ test, batch step, hoặc script tạm không commit.
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

## TASK-C (Codex) — Wire ramen shop & station vào scene gameplay thật (90_TestSandbox)

**TRẠNG THÁI (2026-09-16)**: Không dùng/không commit `GameplayAreaInstaller` hoặc auto-installer scene. Nếu TASK-C còn thiếu object trong `90_TestSandbox.unity`, hãy chỉnh trực tiếp scene thật theo kiểu additive, không rebuild sandbox và không thêm menu/editor setup phải bấm.

**Bối cảnh**: `scenario_restaurant_order_ramen.asset` và `scenario_station_buy_ticket.asset` (đã viết, xem `STORY_BIBLE.md`) mỗi cái có 1 node `GoToArea` (`nodeType: 3`) cần trigger thật trong scene mới chạy được:
- Ramen: `targetAreaId: ramen_shop_entrance`.
- Station: `targetAreaId: station_entrance`.

Sau khi vào đúng area, các node `Dialogue` tiếp theo (`speakerId: npc_ramen_owner` / `npc_station_staff`) tự chạy qua `DialogueManager` — **không cần** cơ chế `TalkToNPC`/tương tác trực tiếp để scenario chạy được, nên việc bắt buộc chỉ là 2 trigger area. Đặt NPC Yamada/Kimura đứng trong shop chỉ là polish hình ảnh, không bắt buộc cho logic.

**CẢNH BÁO QUAN TRỌNG — đọc trước khi code**: `SceneBuilder.cs` đang chứa hàm dựng toàn bộ `90_TestSandbox.unity` (cửa hàng konbini, kệ, quầy thu ngân...) và **3 `[MenuItem]` liên quan đều đã bị comment out** (`// [MenuItem("NihongoLife/Rebuild Gameplay Sandbox")]` dòng 80) — rõ ràng là cố ý vô hiệu hoá để không ai lỡ tay bấm lại và ghi đè scene đã được chỉnh tay rất nhiều. **Tuyệt đối không gọi lại `BuildAllScenes`/hàm rebuild sandbox hiện có, không sửa để bật lại menu đó.** Đây đúng loại lỗi chủ dự án vừa bị (xem lịch sử task TASK-A/B: rebuild đè mất đồ đã chỉnh tay) — lặp lại ở `90_TestSandbox` sẽ nghiêm trọng hơn nhiều vì đây là scene gameplay chính.

**Yêu cầu**: Chỉnh `90_TestSandbox.unity` theo kiểu **cộng dồn (additive)**, không đụng vào hàm dựng scene hiện có và không commit editor menu/tool setup riêng:
1. Mở/chỉnh `90_TestSandbox.unity` hiện có (giữ nguyên toàn bộ nội dung đã có), **không** gọi `NewScene`, không rebuild sandbox.
2. Kiểm tra trước khi tạo: nếu đã có GameObject tên `RamenShopArea`/`StationArea` trong scene rồi thì **skip/giữ nguyên, không tạo trùng** — giống hệt pattern `skipIfExists` đã dùng ở TASK-A/B.
3. Nếu chưa có, thêm mới (không xoá/đổi gì object cũ):
   - 1 trigger area cho `ramen_shop_entrance`: dùng `ScenarioAreaTrigger` (`Assets/NihongoLife/Scripts/Interaction/ScenarioAreaTrigger.cs`) trên 1 BoxCollider `isTrigger=true`, `areaId = "ramen_shop_entrance"`, đặt cạnh vị trí trống trong scene (tự chọn toạ độ không đè lên object khác, ví dụ cách khu konbini hiện có vài mét).
   - 1 trigger area cho `station_entrance` tương tự, `areaId = "station_entrance"`.
   - Tham khảo đúng pattern `CreateCashier()` trong `SceneBuilder.cs` (dòng ~760, đã có sẵn ví dụ NPCController + prefab instantiate + fallback primitive) để đặt NPC Yamada (`npcId: npc_ramen_owner`) và Kimura (`npcId: npc_station_staff`) đứng cạnh 2 khu vực trên — dùng `NL_Guide`/`NL_Neighbor` prefab tạm (giống cách mình vừa map trong `TrailerSceneBuilder.CastPrefabOverrides`) làm placeholder, kèm log warning nhắc đây là model tạm.
4. Lưu chính scene đang mở/đang chỉnh, không tạo scene mới.
5. **Không thêm menu/tool phải bấm trong `NihongoLife/...` cho game setup.** Scene gameplay thật phải có sẵn trigger/NPC cần thiết sau khi chỉnh, hoặc nếu cần script hỗ trợ thì chỉ dùng script tạm thời không commit. Game không được phụ thuộc vào việc người làm phải nhớ bấm menu editor riêng.

**Acceptance criteria**:
- Không còn menu/editor tool mới kiểu `NihongoLife/Scenes/Add ...` để setup ramen/station; mở scene là đã có đúng 1 `RamenShopArea` và đúng 1 `StationArea`.
- Mở `90_TestSandbox`, xác nhận toàn bộ nội dung konbini/shop cũ (kệ, quầy, cửa) còn nguyên, không bị dịch chuyển hay mất.
- Vào Play Mode, load `scenario.restaurant.order_ramen`, đi tới vị trí `ramen_shop_entrance` → objective `obj_enter_shop` hoàn thành, dialogue Yamada tự chạy tiếp.
- Tương tự cho `scenario.station.buy_ticket` với `station_entrance`.

## TASK-D (Codex) — Audit hệ thống chat/online multiplayer đã có sẵn (chưa từng chạy thật)

**Phát hiện quan trọng (2026-09-16)**: Chủ dự án tưởng game chưa có chat/online giữa người chơi, nhưng thật ra **đã có sẵn gần như đầy đủ code**, chỉ chưa từng nối với backend thật nên chưa ai thấy nó chạy. Đọc kỹ trước khi code thêm bất cứ gì — tránh viết trùng:

- `Assets/NihongoLife/Scripts/Core/OnlineWorldService.cs` — interface `IOnlineWorldService` + `LocalOnlineWorldService` (giả lập local khi chưa có backend).
- `Assets/NihongoLife/Scripts/Core/SupabaseOnlineWorldService.cs` — bản thật: presence (thấy người chơi khác) + chat qua Supabase Realtime Broadcast, có `SendChatMessage`, `OnChatMessageReceived`, lưu lịch sử chat, persist vào bảng `chat_messages`.
- `Assets/NihongoLife/Scripts/Core/SupabaseRealtimeClient.cs` — WebSocket client cho Presence/Broadcast/Postgres Changes.
- `Assets/NihongoLife/Scripts/Core/OnlineWorldBootstrap.cs` — tự connect người chơi local khi vào game, publish vị trí mỗi 0.2s.
- `Assets/NihongoLife/Scripts/UI/HUDUI.cs` (dòng ~45, ~209-270, ~500-563) — **UI chat đã dựng sẵn hoàn chỉnh**: panel, ô nhập, lịch sử tin nhắn, phím Enter để mở/gửi.
- `Assets/NihongoLife/Scripts/Player/RemotePlayerManager.cs` + `RemotePlayerAvatar.cs` — hiển thị người chơi khác trong thế giới.
- `Assets/NihongoLife/Scripts/Core/CoopSessionService.cs`, `Scripts/Scenario/CoopScenarioController.cs`, `Scripts/UI/CoopLobbyUI.cs` — hệ thống chơi chung 1 scenario (co-op).
- `Assets/NihongoLife/Scripts/Core/FriendService.cs` + `Scripts/UI/FriendsUI.cs` — kết bạn.
- `Assets/NihongoLife/Scripts/Core/LeaderboardService.cs` + `Scripts/UI/LeaderboardUI.cs` — bảng xếp hạng.
- `Assets/NihongoLife/Scripts/Core/SupabaseAuthService.cs` + `Scripts/UI/AuthUI.cs` — đăng nhập.
- `Assets/NihongoLife/Scripts/Core/AppRoot.cs` dòng ~79-138: đã **wire đúng** — nếu `SupabaseClient.IsConfigured` (có `supabaseProjectUrl`/`supabaseAnonKey` trong `GameControlDatabase`) thì dùng `SupabaseOnlineWorldService` (thật), không thì tự fallback `LocalOnlineWorldService` (giả lập). Hiện tại 2 field đó đang rỗng (`GameControlDatabase.cs` dòng 68-69) → toàn bộ hệ thống đang chạy chế độ giả lập local, chưa ai test qua backend thật bao giờ.

**Lý do quan trọng**: đoạn code này viết ra nhưng **chưa từng chạy với backend thật**, nên rất có thể có bug tiềm ẩn (parse JSON sai field, race condition khi reconnect, RLS-mismatch giả định sai cấu trúc bảng...). Claude sẽ tự tạo Supabase project + 8 bảng cần thiết riêng (không phải việc của Codex, đang chờ chủ dự án xác nhận) — 8 bảng code đang gọi tới: `coop_sessions`, `coop_participants`, `friendships`, `leaderboard`, `player_progress`, `profiles`, `scenario_scores`, `chat_messages`.

**Yêu cầu cho Codex — CHỈ audit + fix bug đọc thấy, KHÔNG tự bịa Supabase URL/key, KHÔNG tự tạo project**:
1. Đọc hết các file liệt kê ở trên, liệt kê rõ: field/kiểu dữ liệu mỗi REST call hoặc broadcast payload đang giả định (dùng để Claude đối chiếu khi tạo schema thật cho khớp 100%, tránh lệch tên cột).
2. Tìm và sửa bug logic đọc thấy được qua code review tĩnh (không cần chạy): null-check thiếu, race condition rõ ràng, JSON field name không khớp giữa nơi gửi và nơi nhận (vd so sánh field trong `SendChatMessage` với field trong `HandleRemoteChatMessage`), event không unsubscribe gây leak (theo đúng tinh thần `TODO_CHECKLIST.md` mục "huỷ event subscription đúng cách").
3. Kiểm tra `HUDUI` chat panel: UI đã dựng nhưng có bind đúng `_onlineWorld` chưa, có handle trường hợp `_onlineWorld == null` (chưa có GameServices) không bị NullReferenceException không.
4. Viết lại danh sách rõ ràng: **cái gì chắc chắn hoạt động ngay khi có Supabase thật**, **cái gì cần Claude tạo thêm bảng/RLS mới hoạt động**, **cái gì nghi có bug cần fix trước khi test** — trả lời thẳng vào cuối task này trong `TEAM_TASKS.md` hoặc file mới `Docs/ONLINE_AUDIT.md`.

**Không làm**: không tự điền `supabaseProjectUrl`/`supabaseAnonKey`, không tự tạo Supabase project, không tự sửa schema — phần backend là của Claude.

**CẬP NHẬT (2026-09-16, đã xong phần backend)**: Claude đã tạo xong Supabase project thật `nihongolife` (region ap-southeast-1), apply migration đủ 8 bảng (`profiles`, `player_progress`, `scenario_scores`, `chat_messages`, `coop_sessions`, `coop_participants`, `friendships`, `leaderboard`) kèm RLS policy cho từng bảng (đã chạy security advisor, không có cảnh báo), và đã điền `supabaseProjectUrl` + `supabaseAnonKey` + bật `enableOnlineSync: 1` vào `Assets/NihongoLife/Resources/Control/NihongoLifeControlDatabase.asset`. Nghĩa là **giờ có thể test thật** (không chỉ audit tĩnh nữa) — nhắc lưu ý khi audit/test:
- `chat_messages.user_id` cố tình để kiểu `text` (không phải `uuid` FK) vì người chơi có thể chat mà chưa đăng nhập (dùng `SystemInfo.deviceUniqueIdentifier`) — không phải bug, đừng "sửa" thành uuid.
- Các bảng còn lại (`profiles`, `player_progress`, `coop_*`, `friendships`, `leaderboard`) đều yêu cầu người chơi đã đăng nhập thật qua `SupabaseAuthService`/`AuthUI` (RLS check `auth.uid()`) — nếu test mà chưa đăng nhập, các tính năng đó sẽ fail có chủ đích (không phải lỗi).
- Nếu Realtime Broadcast (`chat_message`) không nhận được ở client khác dù đã connect — khả năng do Supabase Realtime Authorization cho channel cần thêm policy trên `realtime.messages` (tính năng mới của Supabase), báo lại cho Claude thay vì tự đoán sửa, vì đây là cấu hình phía backend.
- 6 field cũ `supabaseHost/postgresPort/databaseName/userName/passwordEnvironmentKey/requireSsl` trong `GameControlDatabase.cs` là tàn dư từ hướng tiếp cận Postgres-direct-connection cũ, không còn dùng (đã xác nhận `SupabaseClient` chỉ dùng REST qua `supabaseProjectUrl`/`supabaseAnonKey`) — an toàn để bỏ qua, không cần dọn trong task này.

## Việc của Claude (song song, không chờ Codex/Antigravity)

- Đang xác nhận với chủ dự án về việc tạo Supabase project riêng cho NihongoLife (tài khoản hiện chỉ có 1 project không liên quan tên `hrm_crm`) — sau khi có project sẽ tạo 8 bảng + RLS rồi điền vào `GameControlDatabase`.
- Tiếp tục viết sâu nội dung cốt truyện (mở rộng `STORY_BIBLE.md`, thêm chi tiết/nhánh cho các chapter).

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
