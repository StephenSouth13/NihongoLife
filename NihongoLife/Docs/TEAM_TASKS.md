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
- ~~Nếu Realtime Broadcast (`chat_message`) không nhận được ở client khác... báo lại cho Claude~~ **Đã tự kiểm tra và loại trừ (2026-09-24)**: `SupabaseRealtimeClient.JoinChannel()` không set `"private": true` trong config, nghĩa là dùng kênh broadcast công khai — không cần policy `realtime.messages`, không phải cấu hình còn thiếu. Nếu chat vẫn không nhận được ở client khác thì là bug code, không phải backend.
- 6 field cũ `supabaseHost/postgresPort/databaseName/userName/passwordEnvironmentKey/requireSsl` trong `GameControlDatabase.cs` là tàn dư từ hướng tiếp cận Postgres-direct-connection cũ, không còn dùng (đã xác nhận `SupabaseClient` chỉ dùng REST qua `supabaseProjectUrl`/`supabaseAnonKey`) — an toàn để bỏ qua, không cần dọn trong task này.

**CẬP NHẬT (2026-09-24, đổi sang project Supabase mới)**: chủ dự án tự tạo 1 project Supabase khác (`pkxrtvlerjjiignmlebx`, ngoài tài khoản mà tool Supabase của Claude truy cập được) và yêu cầu chuyển sang dùng project này thay cho `nihongolife` (`kcxhynckzlkrkomnmucp`). Claude đã:
- Điền lại `supabaseProjectUrl`/`supabaseAnonKey` trong `NihongoLifeControlDatabase.asset` trỏ sang project mới.
- Dựng lại y hệt schema cũ (8 bảng + 25 RLS policy + index) sang project mới bằng kết nối Postgres trực tiếp (psql, connection string chủ dự án cung cấp) — đã xác minh lại số bảng/policy khớp 100% với project cũ.
- Xác nhận: không có trigger nào trên `auth.users` (profiles/player_progress/leaderboard được client tự insert lần đầu, không tự sinh qua trigger — đúng như policy `*_upsert_own` đã thiết kế), không có Storage bucket nào được dùng (`avatar_url` chỉ là text field).
- Chủ dự án đã tự bật Anonymous Sign-Ins cho project mới qua Dashboard.
- **Chưa làm** (ngoài khả năng của Claude với project này): lịch sử "Migrations" trên Dashboard sẽ không hiện bản ghi vì áp schema bằng kết nối trực tiếp thay vì Supabase CLI/Management API — chỉ là vấn đề hiển thị, không ảnh hưởng schema thật.
- Project `nihongolife` (`kcxhynckzlkrkomnmucp`) cũ coi như không dùng nữa — không cần dọn, chỉ cần biết là project đang chạy đã đổi.

---

## TASK-E (Codex) — Bug thật từ playtest: camera hội thoại, hướng di chuyển, giật lag, checklist nhiệm vụ trống

**Bối cảnh**: Chủ dự án playtest `90_TestSandbox` (scenario `street.first_talk`) và báo 4 vấn đề bằng ảnh chụp Play Mode thật. Claude đã đọc code liên quan và xác nhận/khoanh vùng như dưới — Codex verify lại bằng cách chạy thật trong Play Mode (Claude không chạy được Unity), đừng đoán mò thêm khi không cần.

### E1. Camera lúc đối thoại quá đơn giản
`Assets/NihongoLife/Scripts/Camera/ThirdPersonCameraController.cs` dòng 141-150 (`UpdateConversationCamera`) — **đã có** cơ chế camera riêng khi nói chuyện (`SetConversationTarget` gọi khi bắt đầu dialogue), không phải hoàn toàn thiếu, nhưng chỉ có **đúng 1 góc máy cố định** (offset forward 2.4 + side 0.65), không có over-the-shoulder đẹp, không zoom/cắt cảnh theo diễn biến. Cải thiện: thêm easing mượt hơn khi vào/ra góc hội thoại (hiện chỉ có `SmoothDamp` cho position, rotation dùng `Slerp` tốc độ cố định 12f — dễ giật nếu camera đang di chuyển nhanh lúc bắt đầu hội thoại), và cân nhắc thêm 1-2 biến thể góc máy (vd. hơi lệch trái/phải tuỳ vị trí NPC) thay vì luôn 1 công thức y hệt.

### E2. Nghi vấn "di chuyển không theo hướng chuột"
`Assets/NihongoLife/Scripts/Player/PlayerController.cs` dòng 103-104: di chuyển ĐÃ tính theo `_mainCamera.transform.forward/right` (camera-relative, đúng chuẩn third-person) — không phải world-relative. `Assets/NihongoLife/Scripts/Camera/ThirdPersonCameraController.cs` dòng 73-79: camera ĐÃ xoay theo `Mouse.current.delta` mỗi frame. Về lý thuyết 2 hệ thống này khớp nhau đúng. Nghi vấn cụ thể cần verify trong Play Mode (Claude không kiểm tra được):
- `PlayerController.Awake()` dòng 50 cache `_mainCamera = UnityEngine.Camera.main` **một lần duy nhất** — nếu lúc đó `Camera.main` trỏ nhầm camera (vd. có camera khác đang tag "MainCamera" tại thời điểm scene load, hoặc `ThirdPersonCameraController` chưa kịp gắn tag), player sẽ di chuyển theo hướng của SAI camera dù camera hiển thị đúng hướng mắt nhìn. Có fallback tự fetch lại nếu null (dòng 98-101) nhưng KHÔNG fallback nếu nó khác null nhưng sai camera.
- Kiểm tra xem có >1 GameObject nào đang mang tag "MainCamera" trong `90_TestSandbox` không (vd. camera preview menu còn sót, hoặc `Main Camera` mặc định của Unity chưa bị gỡ).

### E3. Animation giật/lag
Đã có 1 phần fix liên quan trong commit "Update menu" (`CharacterAnimationController.cs`: tắt `ApplyPresentationMotion` khi có Animator thật, thêm `iKOnFeet`, normalize Speed) — nhưng nếu vẫn còn giật sau đó thì đây là vấn đề **hiệu năng thật** (frame rate/GC/quá nhiều Update), cần Unity Profiler để xác định, không đoán được từ đọc code. Việc của Codex: mở Window → Analysis → Profiler, chơi thử trong `90_TestSandbox`, xem CPU spike do đâu (thường nghi: NavMesh recalculation của nhiều NPC patrol cùng lúc, hoặc quá nhiều `FindFirstObjectByType`/`GetComponent` gọi trong `Update()` thay vì cache 1 lần — nhiều chỗ trong codebase đang làm vậy, vd. `SupabaseOnlineWorldService`, `OnlineWorldBootstrap.Update()` dòng 34-51 gọi `GameServices.TryGet` + `FindWithTag` mỗi frame).

### E4. Checklist nhiệm vụ (góc trên trái, "Nhiệm vụ") hiển thị trống
`Assets/NihongoLife/Scripts/UI/HUDUI.cs` — code populate coi hợp lý trên giấy:
- `UpdateObjectivesDisplay()` (dòng ~692-718) đọc `ScenarioManager.Instance.Objectives`, build checklist, gán `objectivesText.text`. Được gọi ở `Start()` dòng 113, và lại qua event `OnScenarioStarted`/`OnObjectiveStateChanged` (dòng 75-76).
- `Assets/NihongoLife/Scripts/Editor/SceneBuilder.cs` dòng 814 + 873 tạo object "ObjectivesList" và wire đúng vào field `objectivesText` qua `SetRef`.
- Nhưng ảnh chụp thật cho thấy panel "Nhiệm vụ" trống rỗng. Nghi vấn cần Codex verify trực tiếp trong Inspector lúc Play:
  1. Click GameObject HUD trong Hierarchy lúc đang Play → xem field `Objectives Text` trên component `HUDUI` có bị "None" (chưa gán) không — nếu `90_TestSandbox` hiện tại không phải bản do đúng hàm `CreateGameUI` trong `SceneBuilder.cs` sinh ra (đã bị chỉnh tay nhiều lần qua các session trước), reference này có thể đã đứt.
  2. Nếu field vẫn còn gán đúng, thêm tạm 1 `Debug.Log($"Objectives count: {ScenarioManager.Instance.Objectives.Count}")` trong `UpdateObjectivesDisplay()` để xem lúc `Start()` chạy, list đã có dữ liệu chưa (nghi vấn: `ScenarioManager` load scenario ở `Start()`/`Awake()` khác thứ tự với `HUDUI.Start()`, dẫn tới đọc list rỗng lúc gọi đầu tiên — nếu đúng vậy, fix bằng cách gọi lại `UpdateObjectivesDisplay()` trong `UpdateScenarioInfo` cho chắc thay vì chỉ dựa vào event, hoặc double check `ScenarioManager` có invoke `OnScenarioStarted` sau khi `_objectives` đã populate xong chưa (nhìn thứ tự code trong `ScenarioManager.cs` dòng ~101-104).

### Đã tự fix (không cần Codex làm lại)
Claude đã sửa `HUDUI.cs` (`RenderPolishedPlayerPanels`, panel "Hồ sơ học viên" bên phải) — trước đó bị hardcode cứng `"Tên: Remy"` và `"Mục tiêu: trò chuyện với người trên phố"` bất kể scenario nào đang chạy. Giờ đọc tên thật từ `IProgressRepository`/`IAuthService` và mục tiêu thật từ `ScenarioManager.Instance.Objectives`. Không đụng lại phần này.

### E5. Konbini bên trong trông như "mockup trống" (đã xác nhận với chủ dự án — không phải hỏi nữa)
Chủ dự án xác nhận: đã bước vào bên trong konbini thật (không phải cảnh đường phố ở ảnh trước) và thấy nó trống, giống mockup chưa có asset.

**Đã verify bằng cách grep GUID trực tiếp trong `90_TestSandbox.unity` — KHÔNG phải bug thiếu asset**: `Shelf_Food`, `Shelf_Drinks`, `Onigiri`, `CashierNPC`, cửa kính đều tồn tại trong scene, và GUID của cả 5 prefab liên quan (`shelf.prefab`, `counter.prefab`, `store_fridge.prefab`, `food_apple.prefab`, `food_bottle.prefab` — toàn bộ đều có file thật trong `Assets/NihongoLife/Prefabs/Furniture/` và `Prefabs/Food/`) đều xuất hiện nhiều lần trong scene, tức đã được instantiate thật, không phải reference bị đứt. `store_fridge.prefab` tồn tại trên đĩa và được reference trong scene nhưng **không thấy lệnh gọi `CreateItem`/tương đương cho fridge trong `FillKonbiniInterior`/hàm dựng shop hiện tại** (chỉ có 2 shelf + 3 item + counter + cashier) — nghĩa là scene hiện tại đã được ai đó bổ sung thêm fridge bằng tay ngoài code, nhưng tổng thể vẫn rất ít đồ so với 1 gian phòng cửa hàng đầy đủ.

**Kết luận**: đây không phải bug kỹ thuật (thiếu asset/reference đứt) mà là **thiếu nội dung trang trí + có thể ánh sáng trong nhà quá tối/phẳng** khiến 1 phòng lớn với chỉ 2 kệ + vài vật phẩm trông trống. Việc cho Codex (không phải audit thêm, mà làm trực tiếp, additive, đúng tinh thần TASK-C — không rebuild toàn bộ shop):
1. Thêm thêm 1-2 vị trí `CreateShelf`/`CreateItem` mới (tái dùng nguyên hàm có sẵn trong `SceneBuilder.cs` dòng 689-757) trực tiếp vào `90_TestSandbox.unity` hiện có — thêm vật phẩm mới (vd. bánh, cà phê lon) lấp khoảng trống, không xoá gì cũ.
2. Kiểm tra ánh sáng bên trong konbini khi `StoreCameraZone`/`SetIndoorMode(true)` kích hoạt (`ThirdPersonCameraController.cs` dòng 121-127) — xem có set riêng ambient light/point light cho khu vực trong nhà không, hay đang dùng chung ánh sáng ngoài trời khiến bên trong tối/phẳng.
3. Không cần hỏi lại chủ dự án về việc này nữa — đã xác nhận rõ trong hội thoại.

---

## TASK-F (Codex) — Shop UI mua hàng kiểu danh sách (click chọn, thay thế nhặt đồ 3D)

**Quyết định đã chốt với chủ dự án (2026-09-17)**: thêm UI mua hàng kiểu danh sách (như shop RPG) — tương tác kệ/quầy hiện bảng liệt kê món + giá, bấm mua trực tiếp bằng UI thay vì phải đi nhặt vật phẩm 3D ngoài đời.

**Khuyến nghị của Claude (chủ dự án đã biết trade-off, xem lại nếu muốn đổi)**: chỉ áp dụng UI mua hàng kiểu này cho việc mua sắm **tự do/lặp lại**, không áp dụng lên scenario `scenario.konbini.buy_onigiri` (bài học N5 chính thức đang luyện mẫu câu `〜をください` qua `DialogueChoice`) — nếu thay luôn cả bài học đó bằng UI bấm chọn thì phần luyện nói/chọn câu tiếng Nhật ở bước mua hàng sẽ mất, không còn giá trị học tập. Nếu chủ dự án muốn thay luôn cả bài học, báo lại rõ ràng trước khi Codex đụng vào node `CollectItem` của scenario đó.

**Cơ chế đã có sẵn, tái dùng — không viết lại từ đầu**:
- `Assets/NihongoLife/Scripts/Interaction/InteractiveItem.cs` dòng 26-50 (`Interact`): đã xử lý sẵn đăng ký với `ScenarioManager.OnItemInteracted`, thêm vào `PlayerInventory`, ẩn vật phẩm — **UI mua hàng nên gọi lại đúng hàm `item.Interact(player)` này** cho từng item được mua, không viết logic riêng, để tương thích ngược với mọi scenario đang track vật phẩm qua `CollectItem`/`InspectItem` node.
- `Assets/NihongoLife/Scripts/Player/PlayerInventory.cs`: `SpendYen(int amount)` dòng 95, `AddItem(...)` dòng 50, `Yen` property dòng 27 — dùng để trừ tiền khi bấm mua (lưu ý: `InteractiveItem.Interact` KHÔNG tự trừ tiền, hiện tại việc trừ tiền đang nằm ở bước hội thoại tại quầy thu ngân — UI mua hàng mới phải tự gọi `SpendYen` khi xác nhận mua, kiểm tra đủ tiền trước khi cho mua).
- `Assets/NihongoLife/Scripts/UI/HUDUI.cs` đã có pattern tạo panel UI runtime nhất quán (`CreateInfoPanel`, `StyleInfoPanel`) — nên viết `ShopUI` theo đúng phong cách này (dựng bằng code lúc `Start()`, không cần dựng tay trong Scene — đúng convention toàn bộ project).

**Yêu cầu**:
1. Tạo `ShopUI.cs` (namespace `NihongoLife.UI`): panel liệt kê item — mỗi dòng hiện `displayNameJa` (kèm đọc nếu có), `displayNameEn`/vi, `priceYen`, nút "Mua". Bấm mua → kiểm tra `PlayerInventory.Instance.Yen >= item.PriceYen` → nếu đủ: `SpendYen` rồi `item.Interact(player)`; nếu không đủ: hiện cảnh báo nhẹ, không trừ tiền.
2. Tạo component mới (vd. `ShopCounter.cs`, implement `IInteractable` giống `InteractiveItem`) đặt tại quầy — khi tương tác (E), tự tìm tất cả `InteractiveItem` đang active trong khu vực cửa hàng (vd. `GetComponentsInChildren` từ 1 root "ShopArea", hoặc liệt kê thủ công qua Inspector), mở `ShopUI` với danh sách đó.
3. **Không đụng vào node `CollectItem`/`node_find_onigiri` trong `scenario_konbini_buy_onigiri.asset`** — vật phẩm onigiri/nước/trà trong scenario đó vẫn giữ nguyên cơ chế nhặt 3D + hội thoại thanh toán như cũ. `ShopCounter`/`ShopUI` mới chỉ nên gắn vào vật phẩm/khu vực KHÔNG thuộc scenario đang chạy (vd. chỉ kích hoạt khi không có `ScenarioManager.Instance.CurrentScenario` đang yêu cầu nhặt tay item đó — hoặc đơn giản nhất: thêm lối vào ShopUI ở 1 vị trí riêng trong konbini, tách biệt hoàn toàn khỏi 3 vật phẩm bài học).

**Acceptance criteria**:
- Chơi hết `scenario.konbini.buy_onigiri` bằng đúng cách cũ (nhặt tay + hội thoại) vẫn hoạt động y hệt, không bị ShopUI can thiệp.
- Có thể mở `ShopUI` ở khu vực mua sắm tự do, mua được item, trừ đúng tiền, item vào túi đồ.
- Không đủ tiền thì không mua được, có phản hồi rõ ràng cho người chơi.

## TASK-G (Codex) — Play Mode kiểm chứng & hoàn thiện nhà hàng ひばり寿司 (zone `30_SushiRestaurant`)

**Bối cảnh**: Claude đã thêm dữ liệu + hệ thống thực đơn và scenario, và đặt sẵn bảng thực đơn trong scene, **chưa chạy Play Mode lần nào**. Tuân thủ `AGENTS.md`: không thêm `[MenuItem]`, không tạo scene mới, không xếp môi trường mới đè lên cũ, chỉnh trực tiếp scene/prefab/asset.

**Đã có (check-first — đừng làm lại)**
- `Scripts/Data/RestaurantMenuDefinition.cs` (+ `JapaneseNumber.cs`), `Scripts/Interaction/RestaurantMenuBoard.cs` (bảng xem trên tường), `Scripts/Interaction/RestaurantTable.cs` (vòng gọi món tại bàn), `Scripts/UI/RestaurantMenuUI.cs` (thực đơn + màn gọi món + phụ đề nhân viên).
- `Resources/Restaurants/menu_sushi_hibari.asset` (24 món có `hungerRestore`/`thirstRestore`/`servingVi`/`servedModel`, 6 nhóm, 4 đặc sản 名物, 18 cụm từ, 8 nghi thức, 3 câu nhân viên `serviceLines`).
- `Resources/Scenarios/scenario_restaurant_sushi_dining.asset` (74 node, có trong `campaignScenarioIds`, không có node `GoToArea`).
- Scene `30_SushiRestaurant.unity`: `RestaurantMenuBoard_Sushi` (x≈504.2, z≈-8.62); `DiningTableService_A` (496.2, 0, -2.4) và `DiningTableService_B` (503.8, 0, -2.4) là **object phục vụ không có hình**, đặt chồng lên `DiningTable_A/B` (prefab instance của Codex, mặt bàn cao ≈ 0.87 m) — mỗi object có trigger (2.4 x 1.6 x 4.2) + `RestaurantTable` + con `ServeAnchor` (local y 0.87). Model bày món chỉ dùng Sushi Restaurant Kit.
- Nếu đổi vị trí/kích thước bàn `DiningTable_*`, phải dời `DiningTableService_*` và `ServeAnchor` theo (không được tạo bàn thứ hai).

**Việc cần làm**
1. Play Mode ở `30_SushiRestaurant`: tới gần bảng thực đơn → prompt「メニューを見る」→ E mở `RestaurantMenuUI` (4 tab, ESC đóng, nhân vật không kẹt khoá input).
2. Tới `DiningTable_A/B` → prompt「注文する」→ E: thấy nút - / + ở mỗi món, tab 注文, tổng tiền/ví, nút 注文する; đặt món → phụ đề nhân viên (青木さん) hiện đủ Nhật/furigana/dịch → chờ → món hiện trên mặt bàn (không chìm xuống bàn, không trùng TableBowl/TableBottle) → E いただきます (no/khát tăng ở HUD) → E お会計 (ví trừ đúng tổng, có phụ đề). Thử: không chọn món, không đủ tiền, 2 bàn cùng lúc, đặt lại sau khi trả tiền. Thử thêm: ăn xong rồi đi ra cửa (`ExitToCity`) khi chưa trả tiền → phải bị chặn kèm phụ đề nhân viên; chip thông báo vàng "!" giữa phía trên màn hình hiện đúng số tiền/trạng thái, không đè lên HUD khác (nếu đè thì dời `anchoredPosition` trong `BuildBillChip` của `RestaurantMenuUI.cs`); nhắc lại sau 25 s; trả tiền xong thì chip biến mất và ra cửa bình thường.
3. Chỉnh trực tiếp nếu sai: `servedModelSize`/`servedModelEuler`/`plateModelSize` trong asset thực đơn (hình bày món), `serveAnchor` và trigger của `DiningTableService_*` (vị trí), `slotSpacing` (khoảng cách đĩa). Ghi lại giá trị đã chỉnh.
4. Model bày món hiện đang tạm (ví dụ unagi/ikura/hotate dùng chung hình nigiri khác, tráng miệng và đồ uống dùng bowl/bottle): thay bằng model đúng nếu có trong project, không nhập thêm nếu không cần.
5. ~~Đặt 2 NPC npc_sushi_staff (Aoki)... npc_sushi_chef (Ota)~~ **Đã thêm (2026-09-24)** bằng `GameplayZoneSceneBuilder.AddSushiStaff()` (chạy qua batch mode, additive — không đụng `DiningTable_*`/serve anchor đã chỉnh tay, xác nhận bằng git diff không mất object nào cũ): Aoki đứng gần cửa vào (origin +(0,0.05,-5)), Ota đứng sau quầy giữa quầy sushi và bàn bếp (origin +(0,0.05,5)) — **đang dùng tạm prefab `NL_Neighbor`/`NL_Guide`** (chưa có model riêng, đã log warning). Cả hai chỉ đứng yên, chưa có patrol/đi bưng món — việc Aoki đi tới bàn khi món sẵn sàng vẫn còn treo, chưa làm.
6. Quyết định gating scenario: hiện chạy ở bất kỳ đâu. Nếu muốn ép vào quán mới chơi, thêm trigger khu vực trong zone và node `GoToArea` — **phải giữ nhánh cũ chạy được**, báo Claude trước khi đổi graph.
7. Chơi hết các nhánh scenario (đúng / kém tự nhiên / sai → sửa → thử lại), ghi lỗi hiển thị hoặc node treo.
8. Trang trí thêm nếu còn trống: chỉ dùng asset có sẵn; **disable/xoá** thứ bị thay thế, không xếp chồng.

**Ranh giới**: không sửa nội dung tiếng Nhật trong scenario/menu (Claude sở hữu nội dung); nếu thấy lỗi thì ghi vào báo cáo. Không đổi tên field serialized của `RestaurantMenuDefinition` (asset đang dùng GUID script 5e1a7c30…).

**Acceptance criteria**: mục 1-3 và 7 có bằng chứng Play Mode (mô tả hoặc ảnh), console không lỗi mới, không thêm menu Editor.

## TASK-H (Codex) — Kiểm chứng HUD cứng + responsive trong Unity (`90_TestSandbox`)

**Bối cảnh**: Claude đã ghi HUD mới trực tiếp vào `90_TestSandbox.unity` bằng tay (chưa mở Unity nên **chưa nhìn thấy kết quả**). Đổi này thay cho việc dựng/đặt lại HUD lúc chạy (`RepairRuntimeLayout` đã bỏ). Mở scene sẽ có hộp thoại "scene đã bị đổi bên ngoài": chọn **Reload**. Tuân thủ `AGENTS.md` và `Docs/DEVELOPMENT.md` mục "HUD & Responsive UI Standard".

**Đã có (check-first — đừng làm lại)**
- Scripts: `HudCanvasFitter`, `HudSafeArea`, `HudFitRect`, `HudVitalsCard`, `HudNotificationTray` trong `Scripts/UI`.
- Scene: `Canvas` (CanvasScaler đã đổi sang Expand + `HudCanvasFitter`) > `SafeArea` > `HUDPanel` > `HudRightColumn` (`VitalsCard` + `NotificationTray` với 3 `NotificationChip_*`). `WalletText` cũ đã tắt (ví nằm trong `VitalsCard`). Vị trí/kích thước MissionPanel, DialoguePanel, InventoryPanel, CharacterPanel đã được ghi cứng vào scene theo đúng giá trị mà code runtime cũ từng áp.
- `RestaurantMenuUI` đăng thông báo hóa đơn qua `HudNotificationTray` (không còn chip tự dựng).

**Việc cần làm**
1. Mở `90_TestSandbox`, kiểm tra Console không có lỗi khi Reload; Scene view: cột phải trên có thẻ chỉ số, không lỗi missing script/reference.
2. Play Mode ở 1920x1080: thẻ chỉ số cập nhật (Lv, ví, 4 thanh; thử ăn/uống để thấy thanh tăng; hạ dưới 25% thì đỏ nhấp nháy); mở túi (B) và hồ sơ (Tab) — panel nằm dưới thẻ, đúng vị trí, chữ không tràn; mở hội thoại; mission panel góc trái trên.
3. Đổi Game view sang các độ phân giải trong Standard mục 7 (và bật `forceTouchLayout` để thử cảm ứng): không panel nào tràn màn hình / đè lên nhau. Chỉnh `HudFitRect` (`maxWidthFraction`, `maxHeightFraction`, `minScale`) và kích thước thẻ trong scene nếu cần; chữ nhỏ nhất vẫn đọc được.
4. Ở `30_SushiRestaurant`: gọi món rồi ăn xong đi ra cửa → chip "Hóa đơn" hiện trong `NotificationTray` dưới thẻ chỉ số (nháy vàng khi chờ thanh toán), bấm được khi con trỏ tự do, biến mất sau khi trả tiền.
5. Chat panel (tự tạo lúc chạy) và `TutorialUI`/`WorldMapUI`/`PlayerProfileUI` có đè lên cột phải không; nếu có thì dời/ẩn theo quy tắc neo cạnh, không thêm menu Editor.
6. Bản build WebGL thử trên điện thoại thật hoặc trình giả lập trình duyệt (DevTools) nếu có thể; ghi lại nếu `Screen.dpi` trả về giá trị lạ làm chữ quá to/nhỏ (`HudCanvasFitter` dùng nó để tính chiều cao CSS).

**Ranh giới**: không tạo scene mới; không dựng lại HUD bằng code lúc chạy; không đổi tên field serialized của `HudVitalsCard`/`HudNotificationTray`/`HudFitRect` (scene đang tham chiếu theo tên); nếu thẻ chỉ số cần thêm hàng mới (ví dụ vệ sinh/WC) thì báo Claude vì cần `PlayerStatus` hỗ trợ trước.

**Acceptance criteria**: mục 1-4 có bằng chứng Play Mode (mô tả hoặc ảnh ở ít nhất 1920x1080 và 1 độ phân giải điện thoại), console không lỗi mới.

## TASK-I (Codex) — Đưa cốt truyện vào thế giới thật (theo `Docs/PROJECT_AUDIT.md` mục 4, P0)

**Thứ tự campaign hiện hành** (đã ghi vào `NihongoLifeControlDatabase.asset`): intro → street → house1 → school → konbini → lostcat → garbage → sushi_dining → cat_followup → recycling → evening_shift. `restaurant.order_ramen`, `station.buy_ticket`, `town.summer_festival` **chưa** nằm trong campaign vì thiếu NPC/khu vực trong scene; khi dựng xong từng phần thì báo Claude để đưa vào.

**Việc cần làm (không tạo scene mới, không thêm menu Editor, ghi trực tiếp vào scene)**
1. Play thử toàn bộ campaign theo thứ tự trên, ghi lại node treo, lỗi hiển thị, lỗi va chạm, lỗi mục tiêu.
2. `30_SushiRestaurant`: đặt NPC `npc_sushi_staff` (Aoki) và `npc_sushi_chef` (Ota) bằng pipeline nhân vật hiện có; bỏ đầu bếp sinh lúc chạy trong `SushiRestaurantRuntime` (chỉ giữ 1 đầu bếp, dùng lời thoại từ scenario/menu asset).
3. Ghi phần môi trường sinh lúc chạy (`SushiRestaurantRuntime`, `StationMetroEnvironment`, `RuntimeCollisionRepair`, `StoreLayoutStabilizer`) vào scene rồi bỏ đoạn sinh; kiểm tra collider, cửa, spawn.
4. Quán ramen: thêm khu vực `ramen_shop_entrance` + NPC `npc_ramen_owner` (Yamada). Nhà ga: khu vực `station_entrance` + NPC `npc_station_staff` (Kimura). Sau đó báo Claude.
5. Quy chuẩn ánh sáng chung cho menu / sandbox / zone (giờ trong ngày, sương, ambient), kèm ảnh so sánh.

**Ranh giới**: không sửa nội dung tiếng Nhật trong scenario; không đổi `speakerId`/tên khu vực đã dùng trong scenario (nếu cần đổi thì báo Claude).

## TASK-J (Codex) — Kiểm chứng khung nhiệm vụ và co-op online (xem `Docs/QUEST_AND_ONLINE_PLAN.md`)

**Đã có (check-first)**: trường quest mới trong `ScenarioDefinition` (đã điền cho cả 14 quest), thưởng tiền khi hoàn thành (`ScenarioManager`), nhật ký nhiệm vụ `QuestLogPopup` (phím J, gắn trong `HUDUI.EnsureQuestLog`), quest `street.first_talk` viết lại 28 node, `CoopScenarioController` được tạo khi có phiên co-op (`CoopScenarioController.EnsureFor`), lượt chọn luân phiên (host trước), `CoopLobbyUI` tự chọn quest co-op.

**Việc cần làm (không tạo scene mới, không thêm menu Editor)**
1. Play Mode: mở J ở gameplay (không mở được lúc đang hội thoại), 3 tab, Bắt đầu nhiệm vụ / Chỉ đường, đóng bằng J/ESC/X, nhân vật không kẹt khoá di chuyển, con trỏ trở lại đúng.
2. Chơi hết `street.first_talk` (28 node: chọn đúng, kém, sai → sửa → thử lại), kiểm tra mục tiêu đổi trạng thái, luyện nói (V) ở node `n_practice`, màn kết quả hiện `+kiến thức` và `+¥200`, nhật ký chuyển quest sang tab Đã xong.
3. Online: chờ bạn bật Anonymous Sign-Ins (Supabase Dashboard). Sau đó thử 2 bản chạy: tạo phòng ở máy A, vào phòng ở máy B, bắt đầu `school.self_intro`; kiểm tra node đồng bộ, lượt (chip "Đến lượt bạn"), người không có lượt bấm chọn thì không có tác dụng, node không bị chuyển lặp, kết thúc phiên trở lại solo.
4. **Story flags và node Branch** (mới): chơi `intro` chọn "ちょっと不安です" rồi chơi `house1` — Tanaka phải nói câu an ủi tương ứng; chơi lostcat có/không hỏi どんな猫ですか rồi tới lễ hội — Suzuki phải đổi lời; kiểm tra Console có log `[StoryFlags] Set ...` và `Branch ... -> true/false`; đóng game, mở lại: cờ còn (lưu local), thử đăng nhập cloud nếu đã bật.
5. Chơi từng quest theo thứ tự campaign (14 quest): ghi node treo, chữ Nhật hiển thị lỗi, hội thoại nhảy sai node, mục tiêu không đổi trạng thái, `TalkToNPC` không kích hoạt được (NPC Tanaka/Suzuki/Sato phải có mặt trong sandbox).
6. Sửa hoặc ghi lại lỗi; nếu cần đổi giao thức broadcast thì báo Claude trước.
7. **Lỗi biên dịch chặn mọi thứ ở trên**: `Assets/NihongoLife/Scripts/UI/WorldMapUI.cs:94` — `position + new(0, -103)` không hợp lệ trong C# (không thể suy ra kiểu của `new(...)` qua toán tử `+`). Đây không phải file nội dung/scenario nên Claude không tự sửa; Codex sửa bằng cách viết rõ kiểu, ví dụ `position + new Vector2(0, -103)`, rồi báo lại khi Unity mở được (mục 1-6 ở trên đều cần Unity biên dịch được trước).

**Ranh giới**: không sửa nội dung tiếng Nhật/Việt trong scenario; không đổi tên field serialized mới của `ScenarioDefinition`; không thêm bảng Supabase mới khi chưa báo.

## TASK-K (Codex) — Play Mode kiểm chứng hệ thống luyện thi JLPT/IELTS mới

**Bối cảnh**: Claude vừa viết xong toàn bộ hệ thống luyện thi JLPT/IELTS (xem `Docs/EXAM_SYSTEM.md` để hiểu kiến trúc + giới hạn trung thực trước khi test) — độc lập với hệ scenario/quest, vào bằng phím **K** hoặc `ExamCenterPopup`. **Chưa chạy Play Mode lần nào**: lúc viết, `Library/ScriptAssemblies` thiếu DLL TextMeshPro/InputSystem/UGUI (Unity đang mở/rebuild ở máy khác), nên không tự kiểm tra biên dịch offline được. Đây là việc code hoàn toàn mới, rủi ro lỗi biên dịch/logic cao hơn bình thường — đọc kỹ code trước khi chỉ chạy thử.

**Đã có (check-first — đừng viết lại)**:
- `Scripts/Exam/ExamModels.cs`, `ExamResultDto.cs`, `ExamGradingService.cs`, `ExamRepository.cs`, `ExamManager.cs`.
- `Scripts/UI/ExamCenterPopup.cs` (chọn đề, phím K), `Scripts/UI/ExamPlayUI.cs` (làm bài + kết quả).
- `AppRoot.cs` mục "9. Exam Center" đã đăng ký 3 service (`ExamRepository`, `ExamGradingService`, `ExamManager`).
- `HUDUI.cs` đã gọi `EnsureExamCenter()` để gắn `ExamCenterPopup` vào HUD.
- `PlayerProgressDto.examAttempts` + `SupabaseProgressRepository` đã cộng dồn merge lịch sử lượt thi (local + cloud).
- 2 đề mẫu: `Resources/Exams/jlpt_n5_mock_1.asset`, `Resources/Exams/ielts_academic_practice_1.asset` (sinh bằng `Tools/exam/gen_*.py`, đã tự kiểm tra YAML hợp lệ bằng PyYAML — nhưng chưa qua Unity Editor).

**Việc cần làm**:
1. **Trước hết: xác nhận project biên dịch được** (mở Unity, xem Console không có lỗi CS ở 7 file mới). Đây là bước biên dịch đầu tiên của toàn bộ hệ thống — nhiều khả năng có lỗi nhỏ (tên field/thứ tự tham số) cần Codex tự sửa vì Claude không tự chạy Unity được.
2. Play Mode: mở HUD, nhấn **K** → `ExamCenterPopup` hiện, có 2 tab JLPT/IELTS, mỗi tab có 1 thẻ đề (đề kia đang trống nếu tab chưa có đề gì tương ứng — hiện tại mỗi tab đúng 1 đề).
3. Bấm "Bắt đầu" đề JLPT N5:
   - Phần Vocabulary: chọn đáp án, bấm Sau/Trước, bảng số câu đổi màu đúng khi đã trả lời.
   - Phần Grammar/Reading: bài đọc hiện bên trái, câu hỏi bên phải; câu ngữ pháp không có bài đọc thì panel câu hỏi chiếm full chiều ngang (không có khoảng trống bên trái).
   - Phần Listening: nút "Nghe (2)" giảm dần khi bấm, hết lượt thì nút mờ/không bấm được.
   - Nộp từng phần (nút "Nộp phần này") hoặc để hết giờ tự khoá — sau khi khoá, Trước/Sau/palette của phần đó không cho sửa nữa.
   - Nộp xong phần cuối → màn kết quả hiện ĐẠT/CHƯA ĐẠT + điểm từng phần + danh sách xem lại đáp án (đúng/sai + giải thích).
4. Bấm "Đóng" ở màn kết quả, mở lại `ExamCenterPopup` → thấy "Điểm cao nhất" cập nhật đúng.
5. Thử đề IELTS: đặc biệt kiểm tra phần Writing (gõ essay, đếm từ chạy đúng, đóng/mở lại câu vẫn giữ nội dung đã gõ) và Speaking (bấm ghi âm, nói vài giây, bấm dừng, trạng thái đổi thành "Đã ghi âm xong"). Nếu chưa cấu hình Gemini (`GameControlDatabase.enableGeminiConversation`/API key env var), xác nhận màn "Đang chấm..." vẫn chạy xong và trả về feedback kiểu "chấm tạm, chưa phải AI thật" thay vì bị treo vô hạn.
6. Đóng `ExamPlayUI` giữa chừng một lượt thi (chưa nộp hết), mở lại qua `ExamCenterPopup` → xác nhận **tiếp tục đúng câu đang làm**, không bị reset về đầu (đây là hành vi cố ý, xem `ExamCenterPopup.StartExam`).
7. Kiểm tra HUD/gameplay khác không bị ảnh hưởng: phím K không trùng phím nào khác đang dùng (J = quest log, W/A/S/D = di chuyển/tutorial).

**Ranh giới**: không sửa nội dung câu hỏi/đáp án tiếng Nhật/Anh trong 2 đề mẫu (nội dung học thuật thuộc Claude) — nếu thấy sai thì ghi lại báo cáo. Có thể sửa lỗi biên dịch/logic C# tự do. Không đổi tên field serialized trong `ExamModels.cs`/`ExamResultDto.cs` (2 file `.asset` đang tham chiếu theo tên field) nếu không báo Claude trước — 2 file này là ScriptableObject asset, đổi tên field mà không có `[FormerlySerializedAs]` sẽ làm mất dữ liệu đã lưu.

**Acceptance criteria**: mục 1-6 có bằng chứng Play Mode (mô tả hoặc ảnh), Console không có lỗi mới liên quan tới `NihongoLife.Exam`/`ExamCenterPopup`/`ExamPlayUI`.

## Việc của Claude (song song, không chờ Codex/Antigravity)

- Đang xác nhận với chủ dự án về việc tạo Supabase project riêng cho NihongoLife (tài khoản hiện chỉ có 1 project không liên quan tên `hrm_crm`) — sau khi có project sẽ tạo 8 bảng + RLS rồi điền vào `GameControlDatabase`.
- Tiếp tục viết sâu nội dung cốt truyện (mở rộng `STORY_BIBLE.md`, thêm chi tiết/nhánh cho các chapter).

1. Sửa `chapterIndex` cho `house1_greeting` (3→1) theo `STORY_BIBLE.md` mục 5.
2. Viết `scenario.restaurant.order_ramen`, `scenario.station.buy_ticket`, `scenario.town.summer_festival` theo nguyên tắc rẽ nhánh sâu ở `STORY_BIBLE.md` mục 10 (nhiều nhánh, hệ quả khác nhau, dùng đa dạng `animationCue`).
3. Sau khi TASK-A/TASK-B có kết quả: review code theo convention hiện có (service locator, ScriptableObject data-driven, namespace `NihongoLife.*`), đảm bảo không phá test hiện có, cập nhật `TODO_CHECKLIST.md`.

## TASK-L (Codex) — Play Mode kiểm chứng zone mới ひばり日本語学院 (`40_HibariSchool`)

**Bối cảnh (2026-09-24)**: theo yêu cầu trực tiếp của chủ dự án, Claude vừa dựng zone `40_HibariSchool.unity` bằng `GameplayZoneSceneBuilder.BuildHibariSchool()` (chạy thật qua Unity batch mode, biên dịch sạch — 0 lỗi CS, chỉ có 3 warning CS0414 cũ không liên quan) và nối cổng vào từ thành phố bằng `GameplayZoneSceneBuilder.AddCityPortals()` (cũng chạy thật, `git diff` xác nhận chỉ thêm mới, không xoá gì trong `90_TestSandbox.unity`, file vẫn thuần LF). **Chưa mở Unity Editor bằng GUI, chưa Play Mode** — mọi thứ dưới đây chỉ được xác nhận bằng batch mode + đọc trực tiếp YAML.

**Đã có (check-first — đừng dựng lại)**:
- Zone `40_HibariSchool.unity`: 1 phòng học (sàn/tường/header hiệu "ひばり日本語学院") dùng khối màu đơn giản như Station/Sushi; bảng đen (`blackboardbig`), bàn giáo viên (`table`), 6 bộ bàn-ghế học sinh (`desk`+`chairtable`), tủ sách (`shelf`), tủ đồ (`locker`) — toàn bộ lấy từ `Assets/ThirdParty/StylooClassroomAssetPack GLTF & FBX/.../classroom/FBX`.
- 2 NPC: `TeacherMorita` (`npc_teacher_morita`) và `ClassmateKim` (`npc_classmate_kim`) — **đang dùng tạm prefab `NL_Guide`/`NL_Neighbor`** (log warning rõ ràng khi build, chưa có model riêng cho Morita/Kim).
- Cổng ra vào: `Spawn_school_entrance` trong zone, `SchoolPortal`/`Spawn_city_school_return` trong `90_TestSandbox` (đặt tại toạ độ thành phố `(18, 1, -4)` — **chưa xác nhận bằng mắt là không đè lên object nào khác**, đây là việc đầu tiên cần kiểm).
- **Cố ý KHÔNG** thêm node `GoToArea` vào `scenario_school_self_intro.asset` — scenario vẫn chạy dialogue-anywhere như trước (xem `STORY_BIBLE.md` mục 4.5). Zone này thuần là bổ sung hình ảnh.

**Việc cần làm**:
1. Mở Unity Editor bằng GUI (không batch mode) một lần, xác nhận Console sạch khi load lại toàn bộ project.
2. Play Mode ở `90_TestSandbox`: xác nhận cổng `SchoolPortal` (toạ độ `(18, 1, -4)`) hiện rõ, không chồng lên nhà/NPC/đường nào khác; đi vào cổng → chuyển scene `40_HibariSchool` tại `school_entrance`, không lỗi.
3. Trong `40_HibariSchool`: xác nhận va chạm tường/bàn/ghế đúng (không đi xuyên), Morita và Kim đứng đúng vị trí không lọt xuống sàn/xuyên bàn, ánh sáng đủ nhìn rõ (2 `ClassroomLight_A/B` + `ZoneLighting`), bảng đen/bàn giáo viên không chồng lấn bàn học sinh.
4. Tương tác (E) với Morita và Kim — xác nhận thoại fallback hiện đúng (câu tiếng Nhật đã set: Morita "自己紹介の練習をしましょう。", Kim "こんにちは。同じクラスですね。") khi Gemini không phản hồi/chưa cấu hình.
5. Đi ra cổng `ExitToCity` trong trường → xác nhận quay lại đúng `city_school_return` trong thành phố, không kẹt.
6. Kiểm tra `WorldMapUI` (phím M): bản đồ tổng thành phố có pin "SCHOOL" mới, mở bản đồ khi đang đứng trong `40_HibariSchool` hiện đúng sơ đồ riêng (bảng đen/bàn học sinh/lối ra).

**Ranh giới**: không sửa nội dung tiếng Nhật/thoại fallback (thuộc Claude); nếu cần đổi toạ độ cổng/NPC để hết chồng lấn thì cứ chỉnh trực tiếp trong `GameplayZoneSceneBuilder.cs` rồi chạy lại `BuildHibariSchool()`/`AddCityPortals()` qua batch mode (không dựng tay trong Editor, để lần sau chạy lại vẫn ra kết quả giống hệt — đúng tinh thần data/code-driven của file này). Không thêm `GoToArea` vào scenario nếu chưa báo Claude.

**Acceptance criteria**: mục 2-5 có bằng chứng Play Mode (mô tả hoặc ảnh), Console không có lỗi mới liên quan tới `HibariSchool`/`GameplayZoneSceneBuilder`.

## TASK-M (Codex) — Play Mode kiểm chứng điều khiển cảm ứng mới + audit tương tác/NPC toàn game (2026-09-24)

**Bối cảnh**: chủ dự án hỏi trực tiếp 3 việc: (1) scene/tương tác nào còn thiếu/lỗi, (2) chó mèo động vật và NPC bán hàng/người đi phố/bán vé/giáo viên đã tương tác đủ chưa, (3) UI/menu hoàn chỉnh và không có `[MenuItem]` rác. Claude đã audit tĩnh (đọc code + YAML scene trực tiếp, không chạy được Unity GUI) và trả lời + sửa một phần — bảng dưới đây là kết quả audit, task này là để Codex xác nhận bằng Play Mode và xử lý phần còn lại.

**Đã xác nhận bằng audit tĩnh (đọc trực tiếp 8 file scene + toàn bộ Scripts/)**:
- `[MenuItem]` — **0 kết quả trong toàn bộ `Scripts/`**, sạch, không có menu rác nào trên thanh Tools của Unity.
- NPC theo scene (đếm `npcId:` trực tiếp trong YAML): `90_TestSandbox` có 5 (Tanaka/Suzuki/Sato/Lilly-guide/cashier), `20_StationDistrict` có 1 (Kimura), `40_HibariSchool` có 2 (Morita/Kim, Claude vừa thêm), `30_SushiRestaurant` **có 2 (Aoki/Ota, Claude vừa thêm hôm nay)**. `01_MainMenu`/`99_*`: 0 (đúng, không cần NPC).
- **Không có ambient/filler NPC nào** — chỉ 1 `NPCStreetPatrol` trong toàn bộ project (Tanaka), nghĩa là "người đi phố" ngoài các nhân vật chính không tồn tại. Đây là giới hạn thật, không phải bug.
- **Chó/mèo/động vật**: pack `Ultimate Animated Animals` có sẵn trong `Assets/ThirdParty/` (Alpaca, Bull, Cow, Deer, Donkey, Fox, Horse, Husky, ShibaInu, Stag, Wolf) nhưng **chưa đặt một con nào vào bất kỳ scene nào**. Quan trọng: **không có model mèo nào trong project** (chỉ Husky/ShibaInu là chó) — scenario `house2.lostcat` (con mèo Mike của Suzuki) hiện **hoàn toàn chỉ là hội thoại**, không có mèo 3D, và đúng ra không cần vì thiết kế node graph không có `CollectItem`/`InspectItem` cho con mèo. Việc thêm chó trang trí vào thành phố Claude **chưa dám tự làm** vì không xem được Editor để tránh đặt xuyên tường/nhà đã dựng tay rất nhiều lần — cần Codex đặt trực tiếp trong Editor.
- **Bán vé tàu**: không qua hội thoại NPC — `StationTravelController`/`TicketMachine_A` (`StationTravelInteractable`, `StationAction.BuyTicket`) đã có sẵn và hoạt động độc lập với Kimura (đúng thiết kế, không phải thiếu).
- **`ShopUI`/`ShopCounter`** (TASK-F, mua hàng tự do): code đầy đủ và đúng (`ShopCounter.Interact()` gọi `ShopUI.GetOrCreate()` chính xác) nhưng **`ShopCounter` chưa được gắn vào bất kỳ GameObject nào trong bất kỳ scene nào** — tính năng này viết xong nhưng người chơi không bao giờ gặp được. Cần Codex chọn 1 vị trí trong `90_TestSandbox` (khu chợ/market, tách biệt khỏi 3 vật phẩm bài học konbini theo đúng ranh giới TASK-F) và gắn `ShopCounter` + vài `InteractiveItem` mẫu.
- **Điều khiển cảm ứng (mobile)**: `MobileJoystick`/`MobileActionButton` (`Scripts/UI/MobileGameControls.cs`) đã viết từ trước nhưng **chưa từng được đặt vào HUD** — input plumbing (`GameInputService.SetMobileMove/SetMobileButton`, `PlayerController` đọc đúng) đã hoạt động, chỉ thiếu UI hiển thị. Claude vừa thêm `HUDUI.EnsureMobileControls()` (joystick góc dưới-trái, nút Interact/Jump góc dưới-phải, chỉ hiện khi `HudCanvasFitter.IsTouchLayout()` true) — **compile sạch, chưa Play Mode/chưa test cảm ứng thật**.
- **Menu/panel khác vẫn chỉ dùng phím tắt, không có nút chạm**: Inventory (B), bản đồ (M), nhật ký nhiệm vụ (J), trung tâm thi (K), hồ sơ (Tab), cài đặt (Esc) — trên thiết bị cảm ứng thật (không bàn phím) người chơi sẽ **không mở được các menu này**. Chưa sửa trong lượt này — cần quyết định thêm 1 thanh icon HUD cho các menu này hay chấp nhận giới hạn "chỉ chơi được bằng bàn phím/gamepad thật" trước mắt.

**Việc cần làm**:
1. Mở `90_TestSandbox`, `30_SushiRestaurant`, `40_HibariSchool` — Play Mode xác nhận Aoki/Ota/Morita/Kim đứng đúng chỗ, không xuyên sàn/tường, tương tác (E) ra đúng thoại fallback đã set.
2. Bật `forceTouchLayout` (hoặc build thử trên thiết bị cảm ứng/DevTools) ở `HudCanvasFitter` trên Canvas chính → xác nhận joystick di chuyển được nhân vật, 2 nút Interact/Jump bấm được, không đè lên `OnlineChatPanel` khi panel đó đang mở (cả hai đang neo cùng góc dưới-trái — nếu đè, dời `EnsureMobileControls()` trong `HUDUI.cs`).
3. Quyết định + báo lại: có cần thêm nút chạm cho Inventory/Map/Quest/Exam/Character/Settings không, hay chấp nhận giới hạn hiện tại.
4. Gắn `ShopCounter` vào 1 vị trí thật trong `90_TestSandbox` theo đúng ranh giới TASK-F.
5. Nếu muốn có chó trang trí trong thành phố: chọn vị trí trống thật (Codex nhìn được Editor), dùng model `Husky`/`ShibaInu` từ `Ultimate Animated Animals`, chỉ cần đứng/animation idle, không cần AI phức tạp.
6. Ramen shop (Yamada) và quán trọn vẹn cho `scenario.restaurant.order_ramen` **vẫn chưa có scene** — đây là 1 scene mới hoàn toàn, theo đúng AGENTS.md Claude cần chủ dự án duyệt tên/vị trí scene cụ thể trước (giống cách `40_HibariSchool` đã được duyệt) trước khi ai dựng, kể cả Codex.

**Ranh giới**: không đụng nội dung tiếng Nhật/thoại fallback (thuộc Claude); nếu cần đổi toạ độ NPC/nút UI thì sửa trực tiếp trong `GameplayZoneSceneBuilder.cs`/`HUDUI.cs` rồi chạy lại qua batch mode hoặc Editor, không dựng tay rồi quên đồng bộ code.

**Acceptance criteria**: mục 1-2 có bằng chứng Play Mode; mục 3 có câu trả lời rõ ràng (làm hay không làm thêm nút chạm); Console không lỗi mới liên quan `MobileGameControls`/`AddSushiStaff`.

## Cập nhật (2026-09-24, chủ dự án tự làm + Claude hoàn thiện) — Phòng riêng `45_HomeBedroom` + lia camera vào zone

**Bối cảnh**: chủ dự án tự tạo `45_HomeBedroom.unity` + `HomeBedroomRuntime.cs` (dựng phòng lúc Play, giống lỗi cũ của `SushiRestaurantRuntime`). Claude đã:
- Bake toàn bộ nội thất (sàn/tường/giường/bàn học/thảm/điểm nghỉ `BedRestPoint`) trực tiếp vào `45_HomeBedroom.unity` qua `GameplayZoneSceneBuilder.BuildHomeBedroom()` (batch mode thật, biên dịch sạch). Sửa `HomeBedroomRuntime.cs` bỏ phần dựng phòng lúc Play, chỉ giữ bảng trạng thái (năng lượng/nghỉ/kiến thức/tiền) — đúng loại runtime UI được chấp nhận.
- Thêm cổng `HomeBedroomPortal` trong `90_TestSandbox` (toạ độ `(-18,1,-4)`, đối xứng với `SchoolPortal` ở `(18,1,-4)`) + cổng ra khỏi phòng về thành phố.
- Điền `WorldLocationCatalog.cs` còn thiếu entry `45_HomeBedroom` (trước đó sẽ hiện tên scene thô thay vì "Phòng riêng" trên HUD).
- **Mới**: `Scripts/Camera/ZoneEntrancePan.cs` — lia camera thiết lập cảnh khi vào zone (dùng lại đúng `ThirdPersonCameraController.SetOrbit()`/`IsLocked` sẵn có, không tạo camera/AudioListener thứ 2 để tránh xung đột). Đã gắn vào `HomeBedroom_YourRoom` — mỗi lần vào phòng, camera lia từ yaw -55° sang 55° trong 2.4s (khoá di chuyển tạm thời), rồi trả lại điều khiển bình thường. **Chưa Play Mode kiểm chứng góc quay/tốc độ có mượt không** — cần chỉnh `panDuration`/`startYaw`/`endYaw`/`pitch`/`distance` trực tiếp trên component trong Inspector nếu cảm thấy chưa đẹp.
- `HibariClassroomRuntime.cs` (chủ dự án tự viết) đã kiểm tra — đúng chuẩn AGENTS.md (chỉ decorate/gắn tương tác lên geometry đã bake, không tự sinh môi trường), không cần sửa.

**Việc cần Codex Play Mode xác nhận**: vào `90_TestSandbox`, đi tới cổng `HomeBedroomPortal` (x=-18) → vào phòng, xem cảnh lia camera có mượt/đẹp không, nội thất không xuyên sàn/tường, `BedRestPoint` tương tác (F) được, ra cổng về đúng thành phố. Nếu muốn lia camera cho cả School/Sushi/Station, chỉ cần thêm `root.AddComponent<ZoneEntrancePan>();` vào các hàm `Build*()` tương ứng trong `GameplayZoneSceneBuilder.cs`, không cần viết lại gì.

## Trạng thái

- [ ] TASK-A (Codex) — chưa giao
- [ ] TASK-B (Antigravity) — chưa giao
- [ ] TASK-K (Codex) — chưa giao. Hệ thống luyện thi JLPT/IELTS (Claude viết xong kiến trúc + UI + 2 đề mẫu, chưa Play Mode) — xem `Docs/EXAM_SYSTEM.md`.
- [ ] TASK-L (Codex) — chưa giao. Zone trường học `40_HibariSchool` vừa dựng bằng batch mode, chưa Play Mode.
- [ ] TASK-M (Codex) — chưa giao. Điều khiển cảm ứng mới + audit NPC/tương tác/động vật toàn game, xem chi tiết ở trên.
- [x] Nội dung 3 scenario mới (Claude) — đã viết xong dạng draft: `scenario_restaurant_order_ramen.asset`, `scenario_station_buy_ticket.asset`, `scenario_town_summer_festival.asset` (rẽ nhánh thật theo mục 10 `STORY_BIBLE.md`, có nhánh sai/nhánh sửa sai). **Chưa mở Unity để playtest/verify** — xem việc còn thiếu bên dưới trước khi coi là xong.
- [x] Sửa `chapterIndex` `house1_greeting` (3→1) theo mục 5 `STORY_BIBLE.md`.

### Việc còn thiếu để 3 scenario mới chạy được thật (không chỉ là data)

- 2 NPC mới cần dựng nhân vật thật: `npc_ramen_owner` (Yamada), `npc_station_staff` (Kimura) — dùng đúng pipeline TASK-A khi Codex hoàn thành, quy ước tên thư mục FBX phải khớp 2 `speakerId` này.
- 2 khu vực trigger mới chưa tồn tại trong scene, cần dựng: `ramen_shop_entrance` (`targetAreaId` trong `scenario_restaurant_order_ramen`), `station_entrance` (`targetAreaId` trong `scenario_station_buy_ticket`). Việc này thuộc phạm vi dựng scene (`SceneBuilder.cs`/thủ công trong Editor), chưa gán cho ai — Claude sẽ làm khi có model NPC từ TASK-A, hoặc báo lại nếu muốn giao riêng.
- Cần chạy Unity Editor để mở từng scenario, kiểm tra node graph không lỗi tham chiếu, rồi playtest thật (đúng quy tắc trong `TODO_CHECKLIST.md`: không tick hoàn thành chỉ vì đã viết code/data).

## Cập nhật (2026-10-07, Claude) — Phòng trọ `45_HomeBedroom` + thành phố/siêu thị `90_TestSandbox`

**Phân công đã chốt với chủ dự án:** Claude giữ `45_HomeBedroom` + `Scripts/Home`. Codex **không sửa** các file này; nếu cần thay đổi, ghi yêu cầu tại đây.

**Phòng trọ** — dựng lại bằng `Scripts/Editor/HomeBedroomBuilder.cs` (chạy `-batchmode -executeMethod NihongoLife.EditorTools.HomeBedroomBuilder.Build`, không có MenuItem, chạy lại không bị chồng):
- Căn hộ 1K bằng Kenney furniture kit, gồm giường, bàn học, bếp mini, tủ lạnh, TV, tủ quần áo, poster ひらがな và cửa げんかん ra phố.
- Tương tác: `BedroomRestInteractable` (ngủ → 07:00, hồi năng lượng, lưu game), `StudyDeskInteractable` (ôn 5 từ N5, +Kiến thức, 3 lượt/ngày), `HomeNeedsStation` (bồn rửa uống nước / tủ lạnh dùng đồ đã mua), `RoomLightSwitch`.
- `NL_Humanoid.controller` có thêm state `Sit`/`Lay`. Clip `Remy@Sitting`, `Remy@Laying Nodding` đã chuyển sang Humanoid (trước là Generic nên bị T-pose).
- Test: `Tests/PlayMode/BedroomPlayModeTests.cs`, ảnh trong `Bao_Cao/bedroom-regression/`.

**Thành phố** — `Scripts/Editor/CityTownBuilder.cs` (chạy bằng `-executeMethod NihongoLife.EditorTools.CityTownBuilder.Build`):
- Đã xóa 61 object gốc bị nhân bản (DoorMat/FacadeTrim/WarmWindow/Collision_Footprint…) và object `Visual` mồ côi (nhân vật T-pose lạc sang phòng ngủ).
- Siêu thị ひばりマート là tòa nhà thật (`Town_Konbini`), có kệ, quầy và **NPC thu ngân `npc_cashier` (Ito)**. NPC này trước đó bị mất nên nhiệm vụ konbini bị kẹt.
- Đã tắt `StreetBuilding_N_4` (khối 19 m trùm lên lô siêu thị) và `KonbiniStoreAsset` cũ (có 3 tường vô hình giữa lối đi). Lô đất trống làm quảng trường `Town_Plaza`.
- 4 cổng `AdditiveZonePortals/*` giữ nguyên tên và component, chỉ dời tới mặt tiền thật (`Town_Destinations`), spawn trở về đặt ngay trước cửa. Thêm cột chỉ đường `Town_Signposts`. `WorldMapUI.CityMap()` vẽ theo tọa độ thật.
- ⚠️ **Ảnh hưởng tới Codex:** `NL_Cashier.prefab` (cả `Prefabs/` và `Resources/`) đã dựng lại từ mô hình Lilly có xương + tạp dề, vì mô hình Elizabeth cũ không có xương. Nhân viên ở `20_StationDistrict` và đầu bếp sushi dùng prefab này nên giờ đã có animation. Vật liệu `NL_Guide__*_URP.mat` đã được nối lại texture.
- Test: `Tests/PlayMode/CityTownPlayModeTests.cs`, ảnh trong `Bao_Cao/city-regression/`. Toàn bộ Play Mode 5/5, EditMode 10/10 (07/10/2026).

## Cập nhật (2026-10-07, Claude) — Hội thoại, siêu thị, Balo/Bản đồ, ga `20_StationDistrict`

**UI dùng chung (mọi scene nên dùng lại, không tự dựng UI riêng):**
- `Scripts/UI/NLUi.cs`: bộ dựng UI theo layout group (Panel, Label, Button, Pill, màu `NLUi.Ink/Card/Gold…`). `CreateCanvas` lồng trong canvas khác sẽ tự stretch và bật `overrideSorting`.
- `Scripts/UI/DialogueView.cs`: hộp thoại duy nhất, tự giãn theo nội dung; phím 1–4, ↑↓, Enter, Esc để rời cuộc nói chuyện. `HUDUI` tự tạo và chuyển `DisplayDialogue`/`HideDialogue` sang view này.
- `DialogueManager.StartConversation(nodes, startId, onFinished)` dùng cho trò chuyện phụ, **không** đẩy bước scenario. `CancelDialogue()` để thoát. Trước đây nói chuyện phiếm làm nhảy node nhiệm vụ.
- `NPCController`: nếu scenario không xử lý, NPC có component `INpcService` sẽ nhận `HandleInteract(player)`.

**Siêu thị:** `Scripts/Shop/` (`KonbiniCatalog` 16 món + `KonbiniBasket`, `KonbiniShelf`, `KonbiniClerk`) và `UI/KonbiniShopUI.cs`. `CityTownBuilder` gắn 4 kệ `Shelf_*` và `KonbiniClerk` cho Ito. Scenario konbini vẫn nhận `onigiri`/`water`/`tea` qua `OnItemInteracted`.

**Balo / Bản đồ:** `UI/InventoryWindow.cs` thay panel túi cũ; `UI/WorldMapUI.cs` viết lại, API public giữ nguyên.

**⚠️ Ga `20_StationDistrict` (phạm vi Codex) — Claude đã viết lại `World/StationTravelController.cs`** vì người chơi không lên được tàu:
- Tuyến ひばり → ミナト, ¥320, sân ga 2, vé `train_ticket_minato`.
- Tàu tự vào ga khi người chơi qua cổng và giữ cửa tới khi lên tàu; chạy 18 s; tới nơi tự xuống.
- Objective `obj_ticket`, `obj_platform`, `obj_board` chỉ được hoàn thành khi `scenario.station.buy_ticket` đang chạy.
- API: `PurchaseTicket()`, `HasTicket`, `GatePassed`, `IsOnboard`, `IsRiding`, `IsTrainBoarding`, `IsTicketMachineOpen`. HUD mới là `StationTravelHUD`, gồm thẻ tuyến, 4 bước, thông báo và máy bán vé.
- `StationTravelInteractable` có thêm getter `Action`.
- Nếu Codex sửa ga, hãy giữ các API này và chạy lại `GameplayUiPlayModeTests.Station_FullTripToMinato`.

**Sửa chung:**
- `SceneFlowController` reset trạng thái zone khi load scene Single. Trước đây sau khi về menu/load save trong zone, `EnterZone` bị chặn.
- `TMP Settings.asset` thêm `NotoSansJP SDF` làm fallback toàn dự án. Trước đây chữ Nhật trên bảng giờ tàu, nhãn nhân viên và màn tải hiện ô vuông.
- Nút `ToggleNPCNames` dời sang trái ví tiền (trước bị đè lên số ¥).

**Test:** `Tests/PlayMode/GameplayUiPlayModeTests.cs` (City_DialogueShopBagMap, Station_FullTripToMinato, IntroScenario_GoesHomeBeforeTadaima), ảnh trong `Bao_Cao/ui-regression/`. Play Mode 8/8, EditMode 10/10.

## Cập nhật (2026-10-07 tối, Claude) — HUD mọi scene, tàu tới nơi thật, cổng có biển

**Chạy độc lập một zone:** bấm Play trong `20/30/40/45` giờ sẽ tự nạp `90_TestSandbox` làm nền rồi `EnterZone` vào đúng zone (`StandaloneZoneBootstrap`). Nhờ vậy zone có đủ HUD, balo, bản đồ, dịch vụ, và cửa ra phố hoạt động. Cách cũ (nhân vật trơn) chỉ còn là dự phòng khi thành phố không có trong Build Settings.

**HUD:** `UI/StatusDock.cs` — thẻ chỉ số (体 Thể lực, 元 Năng lượng, 食 No, 水 Khát, Kiến thức, ¥, nơi đang đứng) + thanh nút `BagButton`/`CharacterButton`/`MapButton`/`QuestButton`/`ExamButton`/`SettingsButton` + cửa sổ Nhân vật mới (Tab). Nút `MapButton` cũ đã thay bằng nút cùng tên trong thanh nút.

**Cổng:** mỗi `ScenePortal` tự gắn `PortalBeacon` (biển nổi "駅前 · Khu nhà ga [F] Đi vào", lối ra "出口 · Ra phố"). Không sửa scene.

**Ga (phạm vi Codex) — tuyến ひばり → がくえんまえ ¥180 → ミナト ¥320, sân ga 2:**
- Kimura (`npc_station_staff`) có `StationStaffService : IPriorityNpcService`, chạy trước scenario. Kimura hỏi đi đâu và **bán vé**. Máy bán vé vẫn bán cả 2 ga.
- Tàu dừng ở mỗi ga. Tới ga trên vé → `SceneFlowController.TransferZone()`:
  - Gakuen-mae → cổng `40_ HIBARICLASS`;
  - Minato → cổng `30_SushiRestaurant`.
  - Không còn đưa về sân ga.
- Scenario `scenario.station.buy_ticket` chỉ nhận objective (`obj_arrive/ask/ticket/platform/board`). Tới Minato → `TransitionToNode("n_recap")`.
- Toa tàu dựng lại bằng `Editor/StationTrainBuilder.cs` (`-executeMethod NihongoLife.EditorTools.StationTrainBuilder.Build`, chỉ đụng `TrainCarriageInterior`, toa dời ra z+260 để cửa sổ không nhìn thấy sảnh ga):
  - ghế băng, kính, cửa, tay vịn, giá hành lý, LED つぎは, quảng cáo;
  - hành khách ngồi (`SeatedPassenger`, `TrainPassengerTalk`);
  - cảnh ngoài nhiều lớp (`TrainWindowScenery`);
  - `TrainWindowCamera` (phím Q).
- `INpcService` mới: `IPriorityNpcService`. `DialogueManager.StartConversation` callback giờ trả về id node cuối, hoặc id "lệnh" như `buy:minato` (trước trả `null` nên mọi nhánh thưởng không chạy).

**Test:** `GameplayUiPlayModeTests` (Station_FullTripToMinato, Station_GakuenMaeTicketGoesToSchool, Hud_StatusDockButtonsAndPortalSigns), `BedroomPlayModeTests.BedroomStandalone_BootsThroughCityAndLeaves`.

## Cập nhật (2026-10-08, Claude) — Game Center `50_GameCenter` + Kana Match (vertical slice)

**Scene mới đã được chủ dự án duyệt:** `Assets/NihongoLife/Scenes/50_GameCenter.unity`, dựng bằng `Scripts/Editor/GameCenterBuilder.cs` (`-executeMethod NihongoLife.EditorTools.GameCenterBuilder.Build`).
- Asset: `ThirdParty/Minigame/kenney_mini-arcade`.
- Tối ưu WebGL: 1 vật liệu colormap chung, static batching (trừ chữ TMP), tắt bóng.
- Cổng vào gắn vào `StreetBuilding_S_4` trong `90_TestSandbox`: `Town_Destinations/Entrance_GameCenter`, `AdditiveZonePortals/GameCenterPortal`, `Spawn_city_game_center_return`. Builder chỉ thêm hoặc thay đúng các object này.

**Kiến trúc** `Assets/NihongoLife/MiniGames/` (asmdef `NihongoLife.MiniGames`):
- Lõi: `IMiniGame`, `MiniGameController`, `MiniGameLauncher`, `MiniGameDefinition`, `MiniGameResult`, `KanaPairSet` (5 loại cặp).
- Kết quả đi vào `ScoringManager` (sourceId `minigame.<id>`), `LearningMasteryManager.RegisterUsage` và `PlayerStatus.AddExp`/`AddKnowledge`. Không có lưu trữ riêng.
- Dữ liệu ở `MiniGames/Data/*.asset`; ảnh từ vựng ở `Resources/MiniGames/Words`.
- Word Shooter và Order Rush: mới có định nghĩa dữ liệu (`playable = false`), máy hiện "じゅんびちゅう". Làm tiếp sau khi Kana Match ổn định.

**Font:** `NotoSansJP SDF` lấy mẫu lại ở 56pt, padding 6, atlas 2048 (`Editor/FontAtlasUpgrader.cs`).
- Trước đó: 90pt, atlas 1024, chỉ khoảng 81 ký tự mỗi trang. Ký tự tràn sang trang 2 bị mất trên chữ TMP 3D (biển hiệu).
- Test `FontAtlasDiagnosticTests` giữ cho lỗi này không quay lại.

**Test:** `GameCenterPlayModeTests.GameCenter_KanaMatchVerticalSlice` đi qua đủ 14 điểm kiểm chứng của spec.

⚠️ **`NPC_StationStaff_Kimura` đã bị xoá khỏi `20_StationDistrict`** trong commit `fe1ad2f1` (2026-10-07 23:35). Quầy vé vẫn bán được qua `StationTicketClerk`, nhưng không còn nhân viên hiển thị. Test `Station_FullTripToMinato` cần Kimura nên đang lỗi. Chờ chủ dự án xác nhận: khôi phục hay bỏ hẳn.

## Cập nhật (2026-10-08 sáng, Claude) — ga, HUD, Game Center, lớp học, trailer, nợ kỹ thuật

- **Quầy vé ga:** `StationTicketClerk` giờ là Kimura duy nhất. Model tĩnh không xương (bị chồng với NPC) đã thay bằng `NL_Cashier` có xương, kèm `NPCController` + `StationStaffService`. Builder: `Editor/StationClerkBuilder.cs`.
- **HUD đồng bộ** (`HUDUI.UnifyHudStyle`):
  - bảng nhiệm vụ, lời nhắc `[ F ]` và nút tên nhân vật dùng kiểu NLUi; ẩn ví trùng;
  - mục tiêu hiển thị ●/○/×;
  - HUD trên tàu: bảng LED, đồng hồ, sơ đồ tuyến, chip vé/cửa, nút ngắm cảnh.
- **Chỉ đường:** `UI/WaypointGuide.cs`; bản đồ gọi khi chọn địa điểm, có thêm `WorldMapUI.GuideTo(type)`.
- **Game Center** (`GameCenterBuilder`):
  - biển neon theo khu, poster, đèn rọi màu;
  - quầy quà `MiniGames/Core/PrizeCounter.cs` (vé `game_ticket`);
  - Kana Match: đếm ngược, combo, điểm trực tiếp, ★, kỷ lục theo bộ, vé thưởng.
- **Lớp học** `40_ HIBARICLASS`, dựng lại bằng `Editor/ClassroomBuilder.cs` (bộ Styloo):
  - bàn thi `School/ClassroomExamDesk.cs` mở đề JLPT/IELTS;
  - `School/ClassroomRuntime.cs`: banner chào, nghi thức thi (đèn dịu, đèn rọi), kết quả, pháo giấy, cô Morita nhận xét, bảng đen ghi điểm cao nhất.
- **Trailer:**
  - Hệ thống cũ (`TrailerDirector`, `TrailerSceneBuilder`, 8 shot asset) đã xoá.
  - `TrailerReel/TrailerReel.cs` (asmdef `NihongoLife.Trailer`) quay trên scene thật; `99_Trailer` chỉ còn host (`Editor/TrailerReelSceneBuilder.cs`).
  - Nhạc tự tổng hợp: `Resources/Audio/Trailer/nihongolife_theme.ogg`.
  - Video: `Bao_Cao/NihongoLife_Trailer.mp4` (1080p30, khoảng 67 s), ghi bằng test `[Explicit]` `TrailerRecordingTests.Trailer_RecordFrames`, sau đó ghép bằng ffmpeg.
- **Nợ kỹ thuật đã xử lý:**
  - Supabase `voice_lines` 404 chỉ kiểm tra 1 lần/phiên, ghi info thay vì cảnh báo.
  - `StandaloneZoneBootstrap` không còn tạo nhân vật thừa khi Play từ zone.
  - `ScenarioValidationTests` chạy validator có sẵn trên cả 14 kịch bản: tất cả hợp lệ.
- **Test:** PlayMode 14/14 (+1 explicit), EditMode 11/11.

## Cập nhật (2026-10-08 trưa, Claude) — nút ×, đường chân trời, ảnh vật phẩm, thể lực, dáng đi

- **Nút × dùng chung:** `NLUi.CloseButton(parent, font, onClick)` — ô đỏ góc trên phải, nằm ngoài layout.
  - Đã gắn vào: mini-game (`MiniGameController.CloseOrAbort`: đang chơi thì dừng, ở bảng kết quả thì đóng), hộp thoại (chỉ hiện khi `CanLeave`), cửa hàng konbini, balo, cửa sổ nhân vật, máy bán vé ga.
  - Các nút chữ "とじる · Esc" / "Tab · Đóng" cũ đã thay; Esc/Tab vẫn hoạt động.
- **Đường chân trời** `90_TestSandbox` (ảnh hưởng asset dùng chung, Codex lưu ý):
  - Root mới `Horizon_Backdrop` (có `SceneZoneVisibility`), dựng bằng `Editor/HorizonBuilder.cs`. Chạy lại sẽ thay root cũ, không chồng lên.
  - Gồm: dải đất hai bên phố, khu nhà ngoại ô có cửa sổ (lặp mỗi 100 m theo phố vô tận), vòng đồi, dải sương nhuộm theo màu fog.
  - Runtime `World/HorizonBackdrop.cs`: bám camera; cửa sổ sáng dần khi trời tối.
  - Không có collider. Chống rơi vẫn do `WorldBoundsGuard` + `WorldFallSafety` sẵn có.
  - Asset mới: `Materials/Horizon/*`, `Models/Horizon/*`.
- **Ảnh vật phẩm thật:** `Resources/Items/<itemId>.png`, nạp bằng `UI/ItemIcons.cs` (cửa hàng, giỏ, balo).
  - 13 món vẽ minh hoạ: onigiri ×3, nước, trà, nước cam, sữa, cà phê lon, senbei, Pocky, melonpan, karaage, nikuman, cùng sandwich trứng.
  - Vé và quà cũng vẽ minh hoạ.
  - Kem, oden, snack render từ Kenney (`Editor/ItemIconRenderer.cs` chỉ đụng 3 món này).
  - Item mới chưa có ảnh vẫn hiện chữ Hán như cũ.
- **Thể lực:** chạy nhanh tốn 14/s và không hồi khi đang chạy (hồi sau 1,2 s nghỉ).
  - Hết thể lực thì bị khoá chạy tới khi hồi đủ 25; HUD hiện "Hết sức".
  - Đói/khát giảm nhanh hơn khi đi/chạy.
  - File: `Player/PlayerStatus.cs`, `PlayerController.cs`, `UI/StatusDock.cs`.
- **Dáng đi:** `Player/PostureStabilizer.cs` (do PlayerController tự thêm) kéo cột sống/ngực/cổ về trục đứng sau Animator để bớt lắc hông. Bỏ qua khi ngồi/nằm.
- **Test mới:** `WorldFeelPlayModeTests.City_HorizonStaminaPosture`, ảnh lưu ở `Bao_Cao/world-regression`.
  - Test UI và Game Center kiểm tra thêm nút × và ảnh cơm nắm.

## Cập nhật (2026-10-08 chiều, Claude) — hoàn tất phần Codex còn dở, báo cáo Capstone

Phiên Codex 08/10 dừng giữa chừng vì hết lượt dùng. Các mục còn dở đã làm xong:
- **Gemini 404:** `gemini-2.5-flash` không còn mở cho tài khoản hiện tại (Codex đã phát hiện; gọi lại vẫn 404). Đã đổi sang `gemini-flash-latest` (gọi thử trả 200) ở:
  - `Resources/Control/NihongoLifeControlDatabase.asset`;
  - mặc định trong `GameControlDatabase.cs`;
  - fallback trong `ExamGradingService.cs` và `SpeechPracticeController.cs`.
- **Ảnh cơm nắm** (ảnh chụp do Codex nhúng) thu về 512 px (≈0,4 MB thay vì ≈2 MB/ảnh). Các món khác vẫn là ảnh minh hoạ/render.
- **Đã xoá** `Assets/InitTestScene7a3ed5f2-….unity`: scene thừa do test runner sinh ra và bị commit nhầm.
- **Slide riêng "Dịch vụ trực tuyến"** (Supabase · Gemini · Agora, kèm hiện trạng thật), là slide 32. Slide 22–24 giữ nguyên số.
- **Báo cáo** `Bao_Cao/NihongoLife_Bao_Cao_Capstone.docx` (75 trang, bản PDF kèm theo):
  - thêm 3 chương: Ga tàu, Game Center & mini-game, Quy trình phát triển;
  - thêm 13 sơ đồ UML: use case, lớp, 5 tuần tự, 2 máy trạng thái, ERD Supabase, triển khai, chuyến tàu, quy trình nhóm;
  - ảnh UI mới;
  - bảng kiểm thử và bảng lỗi cập nhật.
- **Test (08/10 chiều):** PlayMode 16/16 (+1 explicit), EditMode 11/11.

## Cập nhật (2026-10-09 tối, Claude) — Stage A: ESC/O/Tab, camera kiểu Sims, HUD nhu cầu, sửa ga

### Phân chia file với Codex (Phase 7) — tránh ghi đè nhau

| Việc | Người làm | File được phép sửa | Kiểm tra khi ghép |
|---|---|---|---|
| Icon 2D từ model 3D, atlas, manifest, tối ưu ảnh | Codex | `Scripts/Editor/AssetPipeline/**`, `Generated/AssetPipeline/**`, `Docs/AssetPipeline/**` | Claude nối icon vào `UI/ItemIcons.cs` |
| Runtime UI, input, camera, HUD, vé tàu, thi | Claude | `Scripts/UI/**`, `Scripts/Core/**`, `Scripts/Camera/**`, `Scripts/World/**`, `Scripts/Exam/**`, scene, builder | PlayMode + ảnh chụp |

- **Unity batchmode: mỗi lúc chỉ một người chạy.** Nếu Unity đang mở project thì chờ, không mở thêm instance.
- Codex không sửa scene hoặc script gameplay khi chưa có xác nhận.

### Đã làm và kiểm chứng bằng Play Mode
- **Ga: Kimura không phản hồi.** Khi Play từ ga, game nạp phố trước và lời dẫn mở đầu (`n_open`) ở lại trạng thái "đang mở" nhưng không hiện, nên Kimura bỏ qua mọi lần nhấn F.
  - Sửa: mỗi lần chuyển khu, `SceneFlowController.LockPlayer` gọi `DialogueManager.SuspendForTravel()` để tạm dừng hội thoại đang mở (giữ bước truyện, nhấn R để tiếp).
  - Test `StationStandalonePlayModeTests`: nhấn F qua đường input thật thì hội thoại với Kimura mở.
- **Nhân vật lơ lửng.** CharacterController luôn giữ đáy capsule cao hơn sàn đúng bằng skinWidth (8 cm), mà model được vẽ từ transform.
  - Sửa: `PlayerController.Awake` nâng `center.y` thêm skinWidth. Đo được khoảng hở 0,000 m.
- **ESC.** Trước đây 13 script tự đọc phím ESC; HUD mở Cài đặt khi không thấy cửa sổ nào đang mở (mà HUD không biết DialogueView, cửa hàng, máy vé, mini-game, bài thi).
  - Mới: `UI/UiModalStack.cs`. Mỗi cửa sổ đăng ký một lần ("đang mở?", "đóng"); ESC chỉ đóng cửa sổ mở gần nhất, không mở Cài đặt.
  - **O** mở Cài đặt (`GameInputId.Settings`); nút HUD ghi "O".
  - Bài thi: ESC hỏi "Tạm rời bài thi?" trước khi đóng.
  - Test `ModalInputPlayModeTests`.
- **Camera / chuột kiểu Sims:** chuột luôn hiện (click-to-move và nút HUD dùng được).
  - Giữ chuột phải hoặc chuột giữa rồi kéo để xoay quanh nhân vật; lăn chuột để zoom.
  - Không xoay camera khi đang mở cửa sổ hoặc khi trỏ chuột nằm trên UI.
  - Đã bỏ mọi chỗ khoá con trỏ (`CursorLockMode.Locked`).
- **Tab — hồ sơ nhân vật:** model 3D thật của người chơi (`UI/ProfilePreview.cs`, clone "Visual" lên sân khấu riêng ở layer 30), kéo chuột để xoay, cùng các chỉ số nhu cầu.
- **HUD nhu cầu:** thanh chạy mượt; dưới 50 % chuyển màu hổ phách, dưới 20 % nhấp nháy đỏ và hiện "Đói!", "Khát!".
  - HUD tự ẩn khi đang thi hoặc chơi mini-game (overlay đăng ký `immersive`).
- **Đề thi có bản quyền** (sách và audio Cambridge, đề JLPT PDF) đã bỏ khỏi git (`.gitignore`); file vẫn giữ ở máy. Lịch sử GitHub cũ vẫn còn: chủ dự án tự quyết định có xoá khỏi lịch sử hay không.

## Cập nhật (2026-10-10, Claude) — Stage B: engine IELTS, Listening kiểm chứng đầu tiên

### Phân chia file (cập nhật)

| Việc | Người làm | File được phép sửa | Phụ thuộc | Kiểm tra khi ghép |
|---|---|---|---|---|
| Engine + UI thi IELTS/JLPT | Claude | `Scripts/Exam/Ielts/**`, `Scripts/UI/IeltsTestUI.cs`, `UI/ExamCenterPopup.cs`, test `Ielts*` | — | PlayMode E2E |
| Mở rộng `validate_exam.py` cho schema `nihongolife.ielts.v1` (test.json + key.json: số câu liền mạch, mỗi câu có key, `set` đối xứng, file audio tồn tại) | Codex | `Tools/asset_pipeline/**`, `Docs/AssetPipeline/**` | schema trong `Scripts/Exam/Ielts/IeltsModels.cs` (chỉ đọc) | Claude chạy validator trước mỗi lần nhập đề |
| Gán icon từ bộ 438 thumbnail vào item | Codex đề xuất, Claude duyệt và nối vào `ItemIcons` | `Docs/AssetPipeline/Reports/**` | danh sách item được duyệt | ảnh cửa hàng/balo |

- **Không đưa nội dung đề có bản quyền vào repo:** câu hỏi, đáp án, audio, ảnh chụp màn hình bài thi chỉ nằm trong `NihongoLife/LocalContent/` (đã gitignore, Unity không import, không vào bản build). Test E2E đọc đáp án từ `key.json` lúc chạy.

### Đã làm
- **Gói local Cambridge 14 Test 1 Listening** (`LocalContent/IELTS/cambridge14_test1/`, có `INVENTORY.md`):
  - Chép tay từ bản PDF scan (máy không có OCR) và đối chiếu từng dòng với ảnh trang.
  - Cả 40 đáp án được đối chiếu với audioscript.
  - 4 file audio khớp đúng 4 phần: kiểm tra bằng tên file và nhận dạng giọng nói offline của Windows.
- **Engine** `Scripts/Exam/Ielts/`:
  - Gồm schema (`IeltsModels`), thư viện tải gói và audio lúc chạy (`IeltsLibrary`), bộ chấm (`IeltsGrader`), lưu bài đang làm và lịch sử (`IeltsAttemptStore`).
  - Bộ chấm: phần "(…)" trong key là tuỳ chọn; giới hạn số từ được kiểm tra; cặp "IN EITHER ORDER" chấm theo cặp; band chỉ là ước tính.
- **UI** `UI/IeltsTestUI.cs`: màn hình toàn màn, có 2 chế độ.
  - Luyện tập: phát, dừng, tua tự do.
  - Thi: mỗi phần phát một lần, không dừng, tự sang phần sau; hết 2 phút kiểm tra thì tự nộp.
  - Ô điền nằm ngay trong form; điều hướng câu 1–40; tự lưu và làm tiếp được; nộp bài có xác nhận; trang xem lại từng câu.
  - ESC và × hỏi trước khi rời; HUD ẩn, phím tắt bị khoá trong lúc thi.
- Exam Center (tab IELTS) hiện thẻ cho gói local, với các nút Tiếp tục / Luyện tập / Thi thử.
- Test: `IeltsGraderTests` (5, dữ liệu giả lập) và `IeltsListeningPlayModeTests` (E2E; tự bỏ qua nếu máy không có gói local).

## Cập nhật (2026-10-10, Claude) — Phase 4 tuyến tàu ↔ Đảo Xanh, Phase 5 `60_MidoriIsland`

| Việc | Người làm | File được phép sửa | Kiểm tra khi ghép |
|---|---|---|---|
| Đảo Xanh: scene, nông trại, động vật, cửa hàng, UI Midori, tàu về | Claude | `Scripts/Island/**`, `Scripts/Editor/IslandBuilder.cs`, `Resources/Island/**`, `Generated/Island/**`, `Materials/Island/**`, `Scenes/60_MidoriIsland.unity`, test `Island*` | `IslandPlayModeTests` (2 test) + ảnh `Bao_Cao/island-regression` |
| Icon item Đảo Xanh | Codex vẽ thumbnail, Claude duyệt và chép vào `Resources/Items` | Codex: `Generated/AssetPipeline/**` (chỉ đọc với Claude) | ảnh cửa hàng/balo |

### Đã làm
- **Tuyến tàu** (`World/StationTravelController.cs`): thêm ga cuối `みどりじま` (¥450, bằng `ticketPrice` trong catalog).
  - Kimura có lựa chọn "みどりじまへ いきたいです"; máy bán vé tự liệt kê ga này.
  - Chiều về: máy bán vé `IslandTicketKiosk` → soát vé ở cửa tàu `IslandTrainDoor` → màn hình đang chạy → tới `20_StationDistrict`.
  - Không thu tiền 2 lần. Nếu hết tiền và không có gì để bán, người chơi được tặng một vé hỗ trợ, nên không bao giờ kẹt trên đảo.
- **Dữ liệu** `Resources/Island/island_catalog.json`: giá, thời gian lớn, sản lượng, từ vựng JA/EN/VI, thức ăn của thú. Script không viết cứng con số nào.
- **Lưu tiến độ** trong `PlayerProgressDto.island` (local + `progress_json` trên cloud), gồm ô ruộng, từ đã học, thú đã quen, thành tích, ngôn ngữ đang học. Cây lớn theo dấu thời gian UTC nên vẫn lớn khi người chơi rời đảo.
- **Scene** `60_MidoriIsland` (đã được duyệt), dựng bằng `IslandBuilder.Build`, không dùng `[MenuItem]`.
  - Gồm ga, quảng trường, 6 ô ruộng, chuồng 4 thú (bò, alpaca, lừa, ngựa) và chó Shiba quanh quảng trường, cửa hàng cạnh ga, điểm ngắm cảnh, 6 biển dạy tên địa điểm.
  - Bờ biển có tường vô hình, kèm cơ chế đưa người chơi về ga nếu rơi khỏi đảo.
  - Chỉ có một mặt trời: mặt trời của thành phố bị tắt khi đảo đang được nạp.
- **UI Midori** (`Island/IslandUI.cs`): thanh công cụ (đổi ngôn ngữ học JA⇄EN, sổ tay, P = cửa hàng), banner chào mừng, thẻ ô ruộng có chọn hạt, thẻ thú, cửa hàng 4 tab, sổ tay tiến độ, toast.
  - Tab Thời trang **để trống có giải thích**, vì dự án chưa có mô hình quần áo.
  - Các cửa sổ đều đăng ký `UiModalStack` (ESC đóng được).

### Ảnh hưởng tài sản dùng chung
- `IslandBuilder` bật `loopTime` cho các clip Idle/Walk/Eating của FBX Quaternius `Cow`, `Alpaca`, `Donkey`, `Horse`, `ShibaInu`. Chó Shiba ngoài thành phố dùng chung file này nên giờ đi/đứng lặp mượt hơn.
- `GameInputId.Shop` (phím P) được thêm vào **cuối** enum, nên các giá trị đã lưu không bị lệch.
- `NPCNameplateSystem` không tạo nhãn thứ hai nữa nếu NPC đã có `Nameplate` dựng sẵn (trước đây Kimura bị chồng 2 nhãn).
- Thẻ đề luyện có sẵn trong Exam Center giờ ghi rõ: "đề rút gọn tự soạn · N câu · không phải đề chính thức". Writing/Speaking chấm bằng AI, cần mạng, chỉ là ước tính.

### Chưa làm / cần quyết định
- Gói `Unity_6_Animals_Free_v2.3.unitypackage` (ithappy) **chưa import**, vì gói kèm demo HDRP; 5 thú Quaternius đã đủ hoạt ảnh.
- Bình tưới không có model 3D nào trong dự án. Icon bình tưới do Claude vẽ (PIL), không lấy từ Codex.
- Đồ công nghệ (laptop, TV) hiện chỉ là đồ sưu tầm, chưa đặt được vào phòng trọ.

## Cập nhật (2026-10-10, Claude) — HUD gọn, chuột khoá + Ctrl, Sổ nhiệm vụ (N), việc làm thêm, tiến trình theo dữ liệu

| Việc | Người làm | File được phép sửa | Kiểm tra khi ghép |
|---|---|---|---|
| HUD, con trỏ, camera, Sổ nhiệm vụ, việc làm thêm, tiến trình | Claude | `Scripts/UI/{StatusDock,HudFeed,HudGraphics,CursorDirector,TaskJournalUI,TimedAction,JobQuizCard}.cs`, `Scripts/Progression/**`, `Scripts/Core/ControlSettings.cs`, `Scripts/Editor/JobSiteBuilder.cs`, `Resources/Progression/**`, `Docs/PROGRESSION_GUIDE.md` | `LifeLoopPlayModeTests` (3), `ProgressionCatalogTests`, `ProgressMergeTests` |
| Icon món sushi | Codex render, Claude duyệt và chép 4 icon nigiri (`sushi_maguro/salmon/ebi/tamago`) vào `Resources/Items` | chỉ đọc `Generated/AssetPipeline/**` | thẻ gọi món |

### Đã làm
- **HUD:**
  - Bỏ thanh phím tắt cố định (B/Tab/M/J/K/O) trên desktop; vẫn giữ trên màn hình cảm ứng.
  - Ô trạng thái gọn ở góc dưới trái: chân dung, cấp, ¥, 5 vòng nhu cầu. Chip cảnh báo chỉ hiện khi nhu cầu < 20%.
  - Góc trên trái chỉ còn một mục tiêu đang theo dõi.
  - Thông báo gom vào `HudFeed`: giữa phía trên, cùng mục tiêu thì cập nhật tại chỗ, tạm ẩn khi có cửa sổ mở.
  - Một làn gợi ý duy nhất ở dưới giữa: ưu tiên `[F]` hơn "Tiếp hội thoại · R"; ẩn khi đang làm việc có thời gian hoặc có cửa sổ mở.
  - Dòng debug "Press V…" chuyển vào feed.
  - Đã kiểm tra không chồng nhau ở 1920×1080, 1600×900, 1366×768, 1280×720.
- **Chuột và camera:**
  - `CursorDirector` là nơi duy nhất quyết định con trỏ.
  - Mặc định khoá chuột và xoay camera bằng chuột. Giữ Ctrl để hiện con trỏ (camera dừng). Mở cửa sổ thì con trỏ tự hiện. Mất focus thì nhả chuột.
  - Kiểu "Click để đi" (như Sims) vẫn chọn được trong Cài đặt.
  - Cài đặt có thêm độ nhạy, đảo trục Y, kiểu điều khiển và bảng đủ phím (thêm N, P, Ctrl, E).
- **Tiến trình theo dữ liệu:**
  - `Resources/Progression/progression.json` chứa ngưỡng cấp, 3 việc làm thêm, nhiệm vụ nông trại, học tập, hằng ngày. Có kiểm tra dữ liệu tự động (`ProgressionCatalogTests`).
  - `progress.xp` giờ là **tổng XP**; cấp tính từ bảng. Bản lưu cũ được nâng lên, không mất cấp.
  - XP và Kiến thức đã tách riêng (trước đây `AddExp` cộng luôn vào Kiến thức).
- **Việc làm thêm** (không còn nhận lương chỉ bằng một nút bấm):
  - Konbini: bê thùng hàng → xếp 3 kệ → chỉ đường 2 khách → tính tiền → báo cáo chị Ito (¥600).
  - Sushi: nhận 2 order tiếng Nhật → lấy đúng món ở quầy bếp → bưng đúng bàn → báo cáo anh Aoki (¥700).
  - Đảo Xanh: cô Hana giao hạt; xới, gieo, tưới, cho thú ăn → báo cáo (¥500).
  - Trả lời sai không được tính. Lương trả đúng một lần. Huỷ ca thì không có lương. Mỗi lúc chỉ làm một ca.
- **Làm nông có thời gian:**
  - Thanh tiến trình cho mọi thao tác; Esc để huỷ.
  - Xới bằng cuốc 1,6 s, bằng xẻng 2,6 s, bằng tay 7 s. Không có bình tưới thì không tưới được.
- **Sổ nhiệm vụ (N; J cũ cũng mở sổ này):** gồm cốt truyện, làm thêm, nông trại, học tập, hằng ngày. Có nhận, huỷ, theo dõi; hiện thưởng thật từ dữ liệu.

### Ảnh hưởng chung
- `90_TestSandbox` có thêm root `JobSite_Konbini` (riêng biệt, `CityTownBuilder` không xoá). `30_SushiRestaurant` có thêm `JobSite_Sushi`.
- Biển `RegisterPrompt` chỉ được xoay nếu đang quay sai phía.
- `SupabaseProgressRepository.MergeProgress` trước đây làm rơi `inventory`, `island`, `quests` khi đồng bộ đăng nhập. Đã sửa và có test.
- `GameInputId.Journal` và `FreeCursor` được thêm vào cuối enum.

### Còn mở
- Mới kiểm thử phần gộp bản lưu cloud bằng unit test; chưa chạy với Supabase thật.
- WebGL: pointer lock chỉ được cấp sau một lần click vào canvas. Trước đó vẫn kéo chuột phải để xoay camera. Chưa chạy thử bản build WebGL.
- 438 icon của Codex chỉ dùng phần đã duyệt từng cái (8 + 4). Chưa gắn hàng loạt.

## Cập nhật (2026-10-10 chiều, Claude) — Báo cáo Capstone v4, trailer v2, video demo, kịch bản

Claude nhận tiếp phần Codex đang làm dở: báo cáo, trailer, demo và kịch bản. Các bản nháp của Codex trong `Bao_Cao/` được giữ nguyên, không ghi đè.

| Việc | Người làm | File được phép sửa |
|---|---|---|
| Báo cáo v4, trailer v2, demo, kịch bản v2 | Claude | `Bao_Cao/_build_report/**`, `Bao_Cao/_build_video/**`, `TrailerReel/TrailerReel.cs`, `Tests/PlayMode/{TrailerRecordingTests,DemoRecordingTests}.cs` |

### Đã làm
- **Báo cáo:** `Bao_Cao/NihongoLife_Bao_Cao_Capstone_v4.docx` + `.pdf` (55 trang). File gốc giữ nguyên.
- **Trailer:** `Bao_Cao/NihongoLife_Trailer_v2.mp4` (84,6 s, 1080p30, −15 LUFS).
  - Ghi trong game qua `TrailerReel`, gồm 17 cảnh: làm thêm, sổ nhiệm vụ, tàu tới Đảo Midori, làm nông, vật nuôi, bán nông sản.
  - Lời dẫn tiếng Nhật bằng TTS AI. Nhạc tự tổng hợp.
  - Trailer cũ vẫn được giữ.
- **Demo:** `Bao_Cao/NihongoLife_Gameplay_Demo.mp4`.
  - `DemoRecordingTests.Demo_RecordFrames` (Explicit) vừa ghi hình vừa kiểm tra vòng lặp: thành phố → konbini → ga → Đảo Midori → về Hibari.
  - Hiện con trỏ và phím vừa bấm. Thuyết minh tiếng Việt bằng TTS kèm phụ đề.
  - Dựng bằng `Bao_Cao/_build_video/mix_video.py`.
- **Kịch bản:** `Bao_Cao/NihongoLife_Kich_Ban_Trailer_v2.docx` và `NihongoLife_Kich_Ban_Demo_v2.docx` (+ PDF). Timecode và ảnh lấy từ chính bản ghi.
- **Sửa Cài đặt:** núm thanh trượt cao 56 px che chữ "Nhạc nền / Hiệu ứng / Độ nhạy chuột". Đã sửa: `SettingsUI.CreateSlider` đặt `handle.sizeDelta.y = 0`.
- `TrailerRecordingTests.FrameFolder` đọc biến `NL_FRAME_DIR` (ổ C: đầy, ghi khung hình sang D:).

### Lưu ý
- Ở batchmode, `WaitForEndOfFrame` không bao giờ chạy tiếp. Bộ ghi phải tự gọi `camera.Render()` sau `yield return null`.
- Mỗi lúc chỉ một tiến trình Unity được mở project. Đã có lần Codex và Claude cùng chạy bản ghi demo; theo quyết định của người dùng, Claude làm tiếp phần video.

## Cập nhật (2026-10-10 tối, Claude) — Animation làm việc, dụng cụ có độ bền, câu cá Đảo Midori

### Đã làm
- **Animation làm việc:**
  - 11 state mới trong `NL_Humanoid` (`Work_*`, `Fish_*`) từ clip Remy. Cấu hình bằng `WorkAnimationSetup.Run`, chạy qua `-executeMethod`, không dùng MenuItem.
  - `TimedAction.Run(..., pose, prop)` phát clip thật và cho nhân vật cầm dụng cụ ở xương tay phải.
- **Dụng cụ có độ bền** (dữ liệu trong `island_catalog.json`):
  - Không còn làm bằng tay. Xới đất cần cuốc hoặc xẻng, tưới cần bình tưới.
  - Độ bền giảm sau mỗi lần dùng xong (`IslandTools`, lưu ở `IslandRecord.toolWear`); hỏng thì công cụ bị lấy khỏi túi đồ.
- **Câu cá:**
  - Cầu câu ở bờ bắc đảo (do `IslandBuilder` dựng) với `FishingSpot`.
  - 4 loài cá, cần câu bán giá ¥300; câu được cá thì nhận đúng 1 lần.
  - Bán cá ở tab Bán. Thêm quest `farm_first_catch`.

### Ảnh hưởng chung
- `NL_Humanoid.controller`: thêm state.
- `ThirdPartyAssetIntegrator.ConfigureMixamoImports` bỏ qua clip do `WorkAnimationSetup` quản lý.
- `QuestService`: mục tiêu nhận dạng tiền tố `fish_*`. Sự kiện mới `fish`.
- `TimedAction` tự huỷ khi đổi scene.
- `CharacterAnimationController`: thêm `PlayWork`, `HoldProp`. Bow/Point bị bỏ qua khi đang làm việc.
- Scene `60_MidoriIsland` đã dựng lại.

### Còn mở
- Không có model bình tưới, nên lúc tưới nhân vật chỉ làm động tác, không cầm gì.
- Video demo đã dựng trước thay đổi này, vẫn có cảnh "xới bằng tay". `DemoRecordingTests` và lời thuyết minh đã cập nhật; cần ghi hình lại nếu muốn video khớp.
- `CharacterHumanoidAuditPlayModeTests` (của Codex) lỗi: clip "Run" của Devion có AnimationEvent `Footsteps` mà không có component nhận.

## Cập nhật (2026-10-11 sáng, Claude) — Tô sáng trong bài thi, Sổ tay, giấy ở Hibari Mart

### Đã làm
- **Tô sáng (highlight) khi làm bài** — `UI/TextHighlighter.cs`:
  - Thanh công cụ trên header IELTS và JLPT: Tô sáng (3 màu), Xoá tô (tẩy), Xoá hết, → Sổ, Sổ tay.
  - Kéo qua chữ để tô (tiếng Anh tự bắt trọn từ, tiếng Nhật giữ đúng ký tự kéo qua); bấm vào chỗ đã tô bằng tẩy để xoá.
  - Bút tắt thì chữ không bắt chuột, cuộn và trả lời vẫn như cũ.
  - IELTS lưu highlight trong `IeltsAttempt.highlights` (mở lại bài vẫn còn). JLPT giữ trong phiên chơi.
- **Sổ tay** (phím **L**, nút "Sổ tay" trên HUD và trong bài thi) — `Notebook/NotebookService.cs`, `UI/NotebookUI.cs`:
  - Sổ gáy xoắn, trang giấy kẻ dòng, kéo được để di chuyển; luôn hiện trên cả màn thi.
  - Bắt đầu với 6 trang, mỗi trang tối đa 600 ký tự, tối đa 99 trang. Tẩy trang cần bấm 2 lần.
  - Lưu trong `PlayerProgressDto.notebook`; khi gộp với cloud thì lấy bản sửa gần nhất.
  - "→ Sổ" chép các đoạn đã tô sáng sang sổ.
- **Giấy ở Hibari Mart** — tab mới `ぶんぼうぐ · Văn phòng phẩm` (`KonbiniSection.Stationery`):
  - 5 / 12 / 30 trang, giá ¥120 / ¥260 / ¥580.
  - Trả tiền xong số trang cộng thẳng vào sổ, không vào túi đồ.

### Ảnh hưởng chung
- `PlayerController`: không di chuyển hay nhảy khi đang gõ vào ô nhập chữ (`UiModalStack.IsTyping`).
- HUD dock thêm nút L. Thanh nút cảm ứng tự co theo số nút.
- `IeltsTestUI.PaperText` gắn highlighter cho chữ trong `RenderPart`.
- Test: `NotebookHighlightPlayModeTests`. Ảnh chụp ở `Bao_Cao/notebook-regression`; ảnh có đề Cambridge chỉ lưu local.
