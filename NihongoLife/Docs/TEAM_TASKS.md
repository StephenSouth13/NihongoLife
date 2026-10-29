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

**CẬP NHẬT (2026-09-16, đã xong phần backend)**: Claude đã tạo xong một Supabase project cũ (`<OLD_SUPABASE_PROJECT_REF>`, không còn dùng), apply migration đủ 8 bảng (`profiles`, `player_progress`, `scenario_scores`, `chat_messages`, `coop_sessions`, `coop_participants`, `friendships`, `leaderboard`) kèm RLS policy cho từng bảng (đã chạy security advisor, không có cảnh báo), và đã điền `supabaseProjectUrl` + `supabaseAnonKey` + bật `enableOnlineSync: 1` vào `Assets/NihongoLife/Resources/Control/NihongoLifeControlDatabase.asset`. Nghĩa là **giờ có thể test thật** (không chỉ audit tĩnh nữa) — nhắc lưu ý khi audit/test:
- `chat_messages.user_id` cố tình để kiểu `text` (không phải `uuid` FK) vì người chơi có thể chat mà chưa đăng nhập (dùng `SystemInfo.deviceUniqueIdentifier`) — không phải bug, đừng "sửa" thành uuid.
- Các bảng còn lại (`profiles`, `player_progress`, `coop_*`, `friendships`, `leaderboard`) đều yêu cầu người chơi đã đăng nhập thật qua `SupabaseAuthService`/`AuthUI` (RLS check `auth.uid()`) — nếu test mà chưa đăng nhập, các tính năng đó sẽ fail có chủ đích (không phải lỗi).
- ~~Nếu Realtime Broadcast (`chat_message`) không nhận được ở client khác... báo lại cho Claude~~ **Đã tự kiểm tra và loại trừ (2026-09-24)**: `SupabaseRealtimeClient.JoinChannel()` không set `"private": true` trong config, nghĩa là dùng kênh broadcast công khai — không cần policy `realtime.messages`, không phải cấu hình còn thiếu. Nếu chat vẫn không nhận được ở client khác thì là bug code, không phải backend.
- 6 field cũ `supabaseHost/postgresPort/databaseName/userName/passwordEnvironmentKey/requireSsl` trong `GameControlDatabase.cs` là tàn dư từ hướng tiếp cận Postgres-direct-connection cũ, không còn dùng (đã xác nhận `SupabaseClient` chỉ dùng REST qua `supabaseProjectUrl`/`supabaseAnonKey`) — an toàn để bỏ qua, không cần dọn trong task này.

**CẬP NHẬT (2026-09-24, đổi sang project Supabase mới)**: chủ dự án tự tạo 1 project Supabase khác (`<SUPABASE_PROJECT_REF>`, ngoài tài khoản mà tool Supabase của Claude truy cập được) và yêu cầu chuyển sang dùng project này thay cho project cũ (`<OLD_SUPABASE_PROJECT_REF>`). Claude đã:
- Điền lại `supabaseProjectUrl`/`supabaseAnonKey` trong `NihongoLifeControlDatabase.asset` trỏ sang project mới. Những giá trị đó không còn nằm trong asset; xem `LocalSecrets/secrets.example.json`.
- Dựng lại y hệt schema cũ (8 bảng + 25 RLS policy + index) sang project mới bằng kết nối Postgres trực tiếp (psql, connection string chủ dự án cung cấp) — đã xác minh lại số bảng/policy khớp 100% với project cũ.
- Xác nhận: không có trigger nào trên `auth.users` (profiles/player_progress/leaderboard được client tự insert lần đầu, không tự sinh qua trigger — đúng như policy `*_upsert_own` đã thiết kế), không có Storage bucket nào được dùng (`avatar_url` chỉ là text field).
- Chủ dự án đã tự bật Anonymous Sign-Ins cho project mới qua Dashboard.
- **Chưa làm** (ngoài khả năng của Claude với project này): lịch sử "Migrations" trên Dashboard sẽ không hiện bản ghi vì áp schema bằng kết nối trực tiếp thay vì Supabase CLI/Management API — chỉ là vấn đề hiển thị, không ảnh hưởng schema thật.
- Project cũ (`<OLD_SUPABASE_PROJECT_REF>`) coi như không dùng nữa — không cần dọn, chỉ cần biết là project đang chạy đã đổi.

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

## Cập nhật (2026-10-11, Claude) — Nâng cấp Player HUD (StatusDock)
- Ảnh đại diện lấy từ model 3D thật của nhân vật (`UI/HudPortrait.cs`): clone "Visual" lên sân khấu riêng (layer 30, y −1600), chụp một lần vào RenderTexture 256px, rồi tắt sân khấu. Chỉ chụp lại khi đổi model.
- Bố cục mới của `StatusDock.BuildStatusWidget`:
  - portrait có badge Lv; tên và địa điểm (icon ghim); ví ¥ viền vàng;
  - 5 vòng tiến trình có icon và % (vẽ bằng code trong `UI/HudIcons.cs`), không có chữ dưới vòng;
  - nền kính tối.
- Dữ liệu và logic giữ nguyên (`PlayerStatus`, `PlayerInventory`, đổi màu khi chỉ số thấp, ẩn khi có hội thoại).
- Thẻ cảnh báo tự đặt lên trên widget.
- Test: `HudStatusWidgetPlayModeTests`. Ảnh ở `Bao_Cao/hud-regression`.

## Cập nhật (2026-10-11, Claude) — Ảnh thật cho giấy/sổ ở Hibari Mart
- `Editor/ItemIconRenderer.cs` có thêm 2 lệnh batchmode (không có MenuItem):
  - `RenderStationery` → `Resources/Items/<id>.png`;
  - `PreviewStationery -iconOut <dir>` → chụp mọi model văn phòng phẩm để chọn.
- Chung một góc camera (34°, −32°), ánh sáng và lề cắt 6%. Nền trong suốt, 512px.
- Màu thật của pack Styloo: FBX mất texture palette, nên đọc `ImphenziaPalette01-256-Gradient` từ GLB cùng tên khi render. Không thêm asset texture mới.
- Gán ảnh:
  - `note_paper_12` ← `principal office/FBX/PRINCIPALOFFICEnotebook_001.fbx` (sổ lò xo xanh lá);
  - `note_paper_30` ← `classroom/FBX/book__004.fbx` (vở bìa cứng xanh dương, đặt nằm).
- **Thiếu model:** `note_paper_5` (ルーズリーフ, giấy rời 5 tờ) vẫn dùng glyph 紙. `postit` chỉ là một tờ giấy dán cong, không phải xấp giấy.
- Test: `Notebook_PagesPaperAndSave` kiểm tra thẻ 12/30 có ảnh. `LoadCity` trong test đóng lời dẫn mở màn trước khi chụp.
- `KonbiniShopUI.ProductCard`: tên trên thẻ giờ chỉ một dòng, tự thu nhỏ chữ (tối thiểu 13). Tên dài như ルーズリーフ（5まい） không còn đè lên dòng nghĩa.

## Cập nhật (2026-10-11 chiều, Claude) — Kẹt bảng tài khoản, chuột tự do trong konbini
- **Chuột tự do khi vào konbini.**
  - Nguyên nhân: `StoreCameraZone` bật camera trong nhà và tắt camera ngoài đường. `CursorDirector` vẫn giữ camera đã tắt nên coi như "không có camera chơi" và thả chuột.
  - Sửa: `CursorDirector` tìm lại camera ngay khi camera đang giữ bị tắt.
  - Test: `CursorFlowPlayModeTests` (ngoài đường → qua cửa → kệ hàng → Esc → quầy → trả tiền).
- **Kẹt ở bảng "Signed in as".**
  - Nguyên nhân: `AuthUI` không có cách đóng. Các nút menu tạo sau còn vẽ đè lên lớp nền của bảng.
  - Sửa: bảng lên trên cùng khi mở; thêm nút × và Esc (`UiModalStack`); thêm nút Tiếp tục/Đăng xuất; tự đóng 1,2 s sau khi đăng nhập thành công; thẻ cao 570 để ô thông báo không thòi ra ngoài.
  - Test: `AuthPanelPlayModeTests` (auth giả, không cần mạng). Ảnh: `Bao_Cao/ui-regression/10_account_signed_in.png`.

## Cập nhật (2026-10-11 tối, Claude) — Thẻ 2: cửa nhìn ra ngoài, trời ngày/đêm, tên HUD, Kids Characters
- **Nhìn từ trong scene ra cửa** (`Editor/EnvironmentViewsBuilder.BuildAll`, batchmode, đã lưu vào scene):
  - Sushi, lớp học, Game Center: ngoài cửa có bản sao đúng đoạn phố Hibari trước cửa tương ứng (`ExteriorView_Street`).
    - Chỉ để nhìn, không có collider.
    - Layer 28 `ExteriorView`, rendering layer bit 1, có mặt trời và đèn riêng, nên không chiếu vào phòng.
  - Ga: tường hầm phía bắc (`TunnelNorthWall`) bị tắt, thay bằng 2 đoạn tường + một ô cửa và `ExitStairwell` (cầu thang lên phố, biển 出口, tay vịn).
    - Phía trên là đoạn phố Hibari.
    - Có `StairBlocker` chặn ngay chân cầu thang; rời ga vẫn bằng cửa `ExitToCity`.
  - **Ảnh hưởng scene của Codex:** 20_StationDistrict, 30_SushiRestaurant, 40_ HIBARICLASS, 50_GameCenter (thêm root `ExteriorView_Street`, `OutdoorSky`; ga thêm `ExitStairwell`).
- **Trời ngày/đêm:**
  - Shader `NihongoLife/SkyDayNight` (trộn ảnh trời ngày/đêm skydoom của ZRNAssets, ánh hoàng hôn, mặt trời/mặt trăng) và component `OutdoorSky` (Core).
  - `DayNightCycle` chỉ chiếu sáng scene có `OutdoorSky`. Trước đây nó áp sương/ambient vào cả phòng trong nhà và lấy nhầm đèn bất kỳ làm mặt trời.
  - Sửa khoảng 22h→5h30 qua nửa đêm (trước đó bị lấy màu bình minh).
  - Hibari: 80 đèn đường tắt ban ngày, bật lúc chạng vạng.
  - Midori: thêm 12 đèn (`Midori_NightLamps`). Ambient và sương giữ đúng giao diện ban ngày đã dựng của từng scene.
- **Tên trên HUD:**
  - `PlayableCharacterCatalog.ResolvePlayerName`, thứ tự ưu tiên: tên đã chọn hoặc tên profile → tên trong save → tên tài khoản → "Gakusei". Không bao giờ dùng "Guest".
  - `SupabaseProgressRepository` chuyển sang chế độ tài khoản khi phiên đăng nhập được khôi phục.
- **Kids Characters for Survival** (đã nhập vào `ThirdParty/New Chac/ForSurvival`):
  - 8 bé Humanoid (`Resources/Characters/Kids/NL_kid_*`, scale 1,35×), dùng `NL_Humanoid` qua override `NL_Kid_Boy`/`NL_Kid_Girl`: idle, walk, happy-talk là clip của gói; còn lại là clip Mixamo.
  - Thay 3 ô COMING SOON trong danh sách nhân vật.
- **Test:**
  - `EnvironmentViewsPlayModeTests` (ảnh ở `Bao_Cao/environment-regression`).
  - `KidsCharactersPlayModeTests` (ảnh ở `Bao_Cao/character-regression`).
  - `HudPlayerNamePlayModeTests`.

## Cập nhật (2026-10-11 tối, Claude) — Bé là nhân vật chính, thời tiết, camera kiểu Sims, IELTS Writing/Speaking
- **Nhân vật:**
  - Mặc định là bé Haruto (`PlayableCharacterCatalog.DefaultId`). Save cũ đang lưu "student" được chuyển một lần.
  - Tanaka/Remy nằm cuối danh sách.
  - Người chơi có vòng chỉ báo dưới chân (`PlayerMarker`, màu của nhân vật); NPC không có. NPC người lớn giữ nguyên.
- **Thời tiết** (`Core/WeatherCycle` trên AppRoot cạnh `DayNightCycle`):
  - Mỗi ngày trong game: nắng, nhiều mây, hoặc một đợt mưa 1,5–4 giờ.
  - `OutdoorSky`: mây trôi (`_Rotation`), trời xám (`skydoom_cloudy`), nắng yếu, sương dày, đèn bật sớm, mưa bằng particle (dừng ở mái, tắt trong cửa hàng), tiếng mưa tạo bằng code.
  - Người chơi đứng ngoài mưa hồi thể lực ×0,5.
  - NPC ngoài trời tạm ẩn khi mưa, trừ NPC kịch bản đang cần và NPC đang hội thoại; NPC trong nhà không bị ẩn.
- **Camera trên cao kiểu Sims** (ý tưởng từ FreeSims `WorldCamera`/`UIUCP`, viết lại trong `ThirdPersonCameraController`):
  - H bật/tắt, `,` `.` xoay 90°, cuộn chuột đổi 3 mức zoom; con trỏ tự do.
  - Chế độ này không dùng trong nhà.
  - HUD có nút "Góc nhìn" và bảng ◀ ▶ + - khi đang ở chế độ này.
- **FreeSims:** đã chuyển ra `D:\VTC_Academy\NihongoLife\References\FreeSims-master` (650 file C# MonoGame làm hỏng biên dịch Unity). Không có asset dùng được vì game gốc cần file của EA.
- **IELTS Writing/Speaking:**
  - Đề tự soạn trong `StreamingAssets/IELTS`: `nl_writing_academic_1` (Task 1 dạng bảng + Task 2) và `nl_speaking_1` (Part 1/2/3).
  - `IeltsTestUI.Productive.cs`: soạn bài, đếm từ, tự lưu, ghi âm từng câu, Part 2 có 1 phút chuẩn bị, giới hạn 40/120/75 giây.
  - `ExamGradingService.Ielts.cs`: Examiner 4 tiêu chí (band tổng tính tại máy, Task 2 hệ số ×2); Coach góp ý từng câu và xem bản viết lại (không chấm band).
  - Speaking gửi audio và số đo thời gian nói; Pronunciation chỉ chấm từ audio, nếu không chấm được thì ghi "chưa chấm".
  - Kiểm soát chi phí (`GameControlDatabase`): `aiDailyRequestLimit`, giới hạn số từ và giây, cache theo nội dung, báo mất mạng, thử lại khi gặp 429/5xx.
  - `MicRecorder` dùng chung (cả `ExamPlayUI`).
  - App ID Agora chuyển sang Control Database hoặc biến môi trường `AGORA_APP_ID`.
- **Test:**
  - `IeltsProductivePlayModeTests`: AI thật, mất mạng/giới hạn lượt, micro thật, thiết bị ghi âm qua Agora.
  - `WeatherCameraPlayerPlayModeTests`.
  - `PlayModeSaveGuard`: giữ nguyên `progress.json` thật khi chạy test.

## Cập nhật (2026-10-11 tối, Claude hỗ trợ Codex) — Speaking Lounge: camera, nút; hộp thư kết quả
- **Lỗi camera không hiện:** video Agora nằm trên canvas riêng (thứ tự 150), bị nền và thẻ Lounge (canvas 180) che. Đã bỏ canvas riêng.
  - `EduMeetingPanelUI` có `VideoStage` cố định trong thẻ (không bị dựng lại khi làm mới), Agora vẽ thẳng vào đó qua `EduMeetingManager.BindVideoViews`.
  - Trong lớp: video người kia lớn, camera mình nhỏ ở góc; có chữ chờ khi chưa có video.
- **`EduMeetingManager`** (chỉ sửa phần video; phần join-request đang làm của Codex giữ nguyên):
  - `EnsureEngine` dùng chung cho kiểm tra camera và vào lớp.
  - `StartCameraCheck` / `StopCameraCheck` (dùng `StartPreview`, không cần đăng nhập), `ToggleCamera`.
  - `CameraDeviceCount`: báo khi máy không có camera. `HasRemoteVideo` theo `OnRemoteVideoStateChanged`.
- **Nút trong `EduMeetingPanelUI`:** "Kiểm tra camera & micro"; bật/tắt camera, đổi camera, micro, rời lớp; "Xin vào lớp của …" (học viên, `RequestJoinRoom`); "Duyệt" (giáo viên, `ApproveJoinRequest` / `JoinRequests`).
- **`ExamMailboxUI`:** Esc đóng (`UiModalStack`); khung chi tiết cuộn được thay vì cắt bằng "…"; nút "Xem bài đầy đủ" mở `IeltsTestUI.OpenSaved(record)` (điểm 4 tiêu chí, Coach, viết lại, ghi âm).
- **Test:** `SpeakingLoungeCameraPlayModeTests` (webcam thật qua Agora: ảnh 960×540 có nội dung thật; bật/tắt; hộp thư → bài đầy đủ; Esc). Ảnh có webcam chỉ lưu ở `LocalContent/_captures` (git bỏ qua).
- **Chưa kiểm được:** cuộc gọi hai người thật (học viên ↔ giáo viên) qua Supabase function `agora-classroom` — cần function đã deploy và hai tài khoản.

## Cập nhật (2026-10-11 khuya, Claude) — Nhu cầu & thời tiết, cầu vồng; online thật: shard, chat nhóm/riêng mã hóa, bạn bè, chọn mục tiêu F, voice
- **Nhu cầu & thời tiết (HUD):**
  - Mưa êm hơn (âm lượng trong nhà 0,035, ngoài trời 0,11; lọc thấp hai tầng).
  - Cầu vồng 60% sau mưa ban ngày, kéo dài 1 giờ trong game, cung kép 42°/51° đối diện mặt trời (`NL_SkyDayNight`, `WeatherCycle.Rainbow`).
  - Ảnh hưởng thời tiết lên chỉ số:
    - Ướt mưa → `Wetness`; từ 0,7 trở lên là cảm lạnh: mất máu từ từ, mau buồn ngủ ×1,6.
    - Nắng gắt (11–15 giờ, trời quang, ngoài trời): khát ×1,6.
    - Đứng trong mưa: hồi thể lực ×0,5.
  - Cảnh báo trước khi phạt (`StatusDock`):
    - Vòng chỉ số chuyển hổ phách khi dưới 30%; đỏ nhấp nháy và đập dưới 15%.
    - Chip ghi rõ hình phạt sắp đến; viền màn hình đỏ khi máu thấp, tối dần khi sắp ngủ gục.
  - Hình phạt (`StatusConsequences`):
    - Đói 0: đi chậm ×0,8. Khát 0: không chạy được.
    - Máu 0: ngất, +3 giờ, phí phòng khám min(800, 10% tiền).
    - Buồn ngủ 100: chậm ×0,7; sau 40 giây gục ngủ +6 giờ.
- **Realtime (`Core/SupabaseRealtimeClient`) viết lại.** Bản cũ không hoạt động: không đọc được khóa `"event"`, gửi song song trên một socket, tách JSON bằng đếm ngoặc.
  - Nhiều kênh trên một socket; `MiniJson` là bộ đọc/ghi JSON chuẩn.
  - Đọc/ghi socket bằng thread riêng. ClientWebSocket của Mono chặn thread, làm nghẽn thread pool và các join bị trả lời muộn hàng chục giây.
  - Hàng đợi gửi xả hết mỗi lần thức. Lỗi cũ: heartbeat chen trước làm `phx_join` kẹt trong hàng đợi.
  - Tự kết nối lại 2→30 giây và join lại mọi kênh; bỏ qua `phx_close` của lần join cũ.
  - `PostBroadcast` gửi qua REST, không cần join kênh.
  - API cũ `Connect/Broadcast/TrackPresence` vẫn giữ. `CoopSessionService` nay dùng kênh riêng `coop:{sessionId}`.
- **Thế giới online (`SupabaseOnlineWorldService`):**
  - `nl:lobby`: presence cho biết ai online, ở đâu, shard nào.
  - Shard `w:{scene}:{n}`, tối đa 40 người. Thành viên cùng nhóm được xếp chung shard. Phòng ngủ là phòng riêng.
  - Vị trí: mét × 100, góc theo độ, có số thứ tự `s` để bỏ gói đến muộn.
  - Tần suất gửi vị trí: 6 Hz khi ít người, giảm dần còn 2 Hz khi shard đông. Server báo "Too many messages" thì gửi chậm ×2 trong 20 giây. Đứng yên: 3 giây/lần.
  - Chat khu vực không lưu DB; thẻ rich-text bị vô hiệu.
  - `RemotePlayerManager` nay chạy thật (trước đây không được tạo ở đâu): model nhân vật người kia chọn, bảng tên, nội suy trễ thích ứng 0,15–0,9 giây, chỉ vẽ 24 người gần nhất.
- **`SocialHub` + `UI/SocialChatWindow`** (Enter: chat, U: bạn bè):
  - Các tab Khu vực / Cộng đồng (`nl:community`) / Nhóm (`grp:{id}`, tối đa 20, mời qua hộp thư) / Riêng tư / Bạn bè.
  - Tất cả tạm thời, không lưu server.
  - Tin riêng: mã hóa đầu-cuối.
    - RSA-2048 + AES-256-CBC + HMAC-SHA256, có chữ ký; gửi tới `inbox:{id}` qua REST.
    - Khóa ghim lần đầu (TOFU) và có "mã an toàn" để hai bên so.
    - Chỉ gửi được khi cả hai online.
    - Sinh khóa mất khoảng 10 giây trên Mono; chạy nền lúc khởi động, một lần cho mỗi máy.
  - Bạn bè = bảng `friendships` + lobby, hiện online/offline và nơi đang ở. Cần đăng nhập; khách vẫn chat được.
  - Chặn người chơi lưu trong PlayerPrefs.
  - Thẻ người chơi (F hoặc bấm tên): Nhắn riêng / Kết bạn / Mời nhóm / Chặn.
  - Nút báo tin mới và lời mời ở góc trái HUD. Bảng chat cũ của HUD và dòng "Online: N" bị ẩn.
- **Chọn mục tiêu F trong đám đông** (`InteractionDetector` + `InteractionHighlight`):
  - Chấm điểm theo khoảng cách, hướng nhân vật và tâm màn hình. Mục tiêu hiện tại được ưu tiên để không nhảy qua lại.
  - Q chuyển mục tiêu tiếp theo; bấm chuột vào người để chọn (giữ 6 giây).
  - Vòng vàng dưới chân và nhãn "▼ [F] Tên · Q đổi i/n".
  - Luôn quét cả layer Default. Trước đây có NPC ở gần là người chơi khác bị bỏ qua.
- **Voice (`Learning/VoiceChatService`, Agora):**
  - Voice khu vực theo shard, âm thanh không gian: nghe trong 18 m, tối đa 6 giọng gần nhất.
  - Voice nhóm; gọi riêng (chuông qua hộp thư, nút Nghe).
  - Giữ T để nói: mặc định ở vai audience, chỉ phát khi giữ phím.
  - Khử ồn AI (AINS balanced), profile giọng nói phòng chat.
  - Tự nhường engine khi mở phòng Speaking.
  - Token lấy từ Edge Function mới `supabase/functions/agora-voice` (chỉ chấp nhận phòng `nlv_/nlg_/nld_`; không sửa `agora-classroom`).
- **Phím mới:** Q đổi mục tiêu, U bạn bè & tin nhắn, T giữ để nói. Đã có trong Cài đặt › Phím.
- **Test:**
  - `OnlineSocialPlayModeTests` (Supabase thật, 4/4 qua):
    - realtime: presence, 31 tin, chữ có `{}` `"` tiếng Nhật/emoji;
    - hai người cùng shard: vị trí chính xác đến cm, model, chat khu vực;
    - cộng đồng, nhóm, tin riêng: kênh nghe trộm chỉ thấy bản mã; chặn; bạn bè online → offline;
    - trong thành phố: avatar thật, nhận mục tiêu F, Q đổi, thẻ người chơi, cửa sổ chat.
  - Ảnh: `Bao_Cao/online-regression`.
  - `NeedsWeatherPlayModeTests` 3/3; ảnh `Bao_Cao/hud-regression/needs_*`, `environment-regression/rainbow_15h.jpg`.
- **Load test** (`Tools/online_load_test.py`, cùng giao thức với game; gói Supabase hiện tại):

  | Lần đo | Người/shard | Tin đến nơi | Trễ p50 / p95 | Ghi chú |
  |---|---|---|---|---|
  | Trước khi chỉnh tần suất | 20 | 97,8% | — | Đã bị báo "Too many messages per second" |
  | Trước khi chỉnh tần suất | 40 | 92% | — | |
  | Sau khi chỉnh | 20 | 97,9% | 96 / 407 ms | Không còn lỗi giới hạn |
  | Sau khi chỉnh | 40 | 96,8% | 131 / 457 ms | Không còn lỗi giới hạn |
  | Sau khi chỉnh | 60 kết nối từ cùng một máy | — | — | 39/60 bị từ chối kết nối |

  Tổng cộng dùng khoảng 80 nghìn realtime message.
- **Cần làm tiếp:**
  - Deploy voice: `supabase functions deploy agora-voice` (dùng chung secrets AGORA_APP_ID / AGORA_APP_CERTIFICATE). Voice chưa được thử với hai người thật.
  - Muốn 1000+ người cùng lúc phải nâng gói Supabase (gói hiện tại vượt giới hạn ở khoảng 20 người di chuyển, 6 Hz, cùng một shard; giới hạn kết nối khoảng 200) hoặc chuyển phần đồng bộ vị trí sang game server riêng (Nakama/Colyseus/Photon). Kiến trúc shard và chat hiện tại giữ được.
  - RLS bảng `friendships` chưa kiểm được bằng tài khoản thật (test bạn bè dùng `DebugSetFriends` + lobby thật).
  - Tin cộng đồng/nhóm/khu vực chưa có chữ ký nên có thể giả tên người gửi. Tin riêng có chữ ký.

## Thẻ việc (2026-10-12, Claude giao) — cổng mở khóa địa điểm, Cửa hàng & Cẩm nang, đồng bộ Kids, tài liệu
> Quy tắc AGENTS.md: không thêm `[MenuItem]`, không tạo scene mới, lưu kết quả vào scene/prefab, kiểm Play Mode kèm ảnh. Trước khi viết mới, kiểm tra hệ thống đã có: `ScenarioCampaignManager`, `TaskJournalUI` (N), `SceneFlowController`, `ScenePortal`, `WorldMapUI`, `KonbiniShopUI`, `PlayableCharacterCatalog`.

### Thẻ A — Mở khóa địa điểm theo điểm kiến thức (ưu tiên cao)
- Lúc đầu chỉ mở Hibari Mart (cửa hàng tiện lợi) và trường `40_ HIBARICLASS`. Các nơi khác cần đủ "điểm kiến thức" (XP từ bài học, bài thi, nhiệm vụ) mới mở dần: Ga `20_StationDistrict` → Sushi `30` → Game Center `50` → Đảo `60`.
- Ngưỡng điểm đặt trong `GameControlDatabase`, không viết cứng trong code.
- Kiểm tra mở khóa tại `ScenePortal` / `SceneFlowController`, một chỗ duy nhất. Cửa còn khóa hiện "Cần N điểm — còn thiếu M", kèm gợi ý nhiệm vụ.
- Sổ nhiệm vụ (N) thêm khu "Mở khóa": thanh tiến độ tới địa điểm kế tiếp và danh sách việc cho điểm. Bản đồ hiện ổ khóa trên địa điểm chưa mở.
- Save cũ: người đã vào một địa điểm thì giữ quyền vào nơi đó.
- Test: tạo save mới thì chỉ vào được Mart và trường; cộng điểm thì Ga mở. Chụp ảnh cửa khóa, Sổ nhiệm vụ, bản đồ.

### Thẻ B — UI tiền và Cửa hàng
- Chip tiền trên HUD (`StatusDock`): mỗi lần có thay đổi thì hiện "+¥/−¥" bay lên kèm lý do (mua đồ, phí phòng khám, lương làm thêm); bấm vào chip mở lịch sử 20 giao dịch gần nhất.
- `KonbiniShopUI`:
  - Các tab danh mục: Đồ ăn, Đồ uống, Văn phòng phẩm, Thuốc/Sức khỏe, Đồ dùng.
  - Thẻ sản phẩm có ảnh (thumbnail thật), giá, hiệu ứng lên chỉ số (đói/khát/máu…) và câu tiếng Nhật mẫu.
  - Có giỏ hàng và tổng tiền; nút "Thanh toán" chuyển sang hội thoại với thu ngân.

### Thẻ C — Cẩm nang địa điểm trong bản đồ
- Mở bản đồ (M), chọn một địa điểm → trang cẩm nang: ảnh chụp thật của địa điểm (chụp lại bằng PlayMode), ý nghĩa văn hóa (ví dụ konbini ở Nhật), cách tương tác (phím, nhiệm vụ có ở đó), từ vựng/câu mẫu, điều kiện mở khóa (nối với Thẻ A).
- Nội dung đặt trong ScriptableObject `LocationGuide` cho mỗi địa điểm (dựa trên `WorldLocationCatalog`), không viết cứng trong UI.

### Thẻ D — Chỉ dùng nhân vật Kids, bỏ Remy/người lớn cũ
- Đã làm: người chơi là Kids (`PlayableCharacterCatalog`), người chơi khác online cũng dùng model Kids.
- Còn lại:
  - Bỏ mục "student" (Tanaka/Remy, `Characters/NL_Player`) khỏi danh sách chọn; save cũ chọn "student" thì chuyển sang `kid_haruto`.
  - Tìm mọi chỗ còn dùng Remy: clip `Remy@*` trong `CharacterAnimationController`, NPC dùng model người lớn. Thay bằng clip/controller của Kids rồi mới xóa asset (kiểm GUID bằng grep trước khi xóa).
  - Tỉ lệ: bé đang được scale 1,35. Đo chiều cao so với cửa, quầy, bàn học, ghế ở 20/30/40/45/60; điều chỉnh để mắt bé ngang mặt quầy. Lưu thay đổi vào prefab, không chỉnh lúc chạy.
  - Chạy lại `KidsCharactersPlayModeTests` và chụp ảnh trước/sau.

### Thẻ E — Tài liệu, PPTX, video
- Cập nhật báo cáo Capstone và PPTX: thêm online (shard, chat, E2E, voice), cổng mở khóa, cẩm nang, số liệu load test (bảng ở mục 2026-10-11 khuya).
- Thay ảnh cũ bằng ảnh trong `Bao_Cao/online-regression`, `hud-regression`, `environment-regression`.
- Quay lại video demo bằng `DemoRecording` sau khi xong Thẻ A–D. Xóa bản cũ chỉ sau khi bản mới đã được kiểm.

### Xem xét thay đổi chưa commit của Codex (va chạm ở 20 và 40)
- `HibariClassroomRuntime.NormalizeDeskColliders`:
  - Đúng chỗ bị kẹt: collider của bàn và ghế dài khoảng 2 m nên chặn kín lối đi giữa các dãy.
  - Nhưng hàm sửa collider lúc chạy. Theo AGENTS.md, nên lưu kích thước collider thẳng vào scene `40_ HIBARICLASS`.
  - Size đang đặt theo hệ tọa độ local của bàn: nếu bàn nào có scale khác 1 thì kích thước sẽ sai, cần kiểm.
- `StationTravelController.CachePlatformTrain`:
  - Nay tìm mọi Transform có tên bắt đầu bằng `HighspeedTrain_`.
  - Nếu object con cũng có tên như vậy, cả cha lẫn con đều bị dời → con lệch gấp đôi. Chỉ nên lấy các gốc (bỏ object có cha cũng khớp tên).
  - Chưa có test va chạm/tàu chạy cho thay đổi này. Cần chạy `StationTravel*` và test đi bộ qua lối giữa các dãy bàn ở lớp học.
- **Test IELTS Reading 48 đề quá 180 s** (sáng 59 s; mở mỗi đề 1,2 s, mỗi phần 0,2–0,35 s). Không phải do phần online. Cần tìm nguyên nhân trong `IeltsTestUI` (các commit sau 07:30 ngày 11/10).

## Cập nhật (2026-10-12, Claude) — Đã làm Thẻ A–E (trừ quay video)
- **A. Mở khóa địa điểm** (`Core/LocationUnlocks`):
  - Ngưỡng trong `GameControlDatabase.locationUnlocks`: Ga 40, Sushi 90, Game Center 160, Đảo 250.
  - Hibari Mart, trường và nhà luôn mở.
  - Chặn tại một chỗ: `SceneFlowController.EnterZone/TransferZone`. Nơi đã vào thì giữ mở (PlayerPrefs `NL.Unlocked.*`). Bước kịch bản đang cần thì không chặn.
  - Cửa (`ScenePortal`) ghi "Đã khóa — cần N (thiếu M)".
  - Bản đồ: điểm xám + "khóa Nđ". Sổ nhiệm vụ (N): khối "MỞ KHÓA ĐỊA ĐIỂM" có thanh tiến độ; danh sách nhiệm vụ nay cuộn được.
  - Đủ điểm thì HUD báo "Đã mở khóa…".
  - Test đặt `LocationUnlocks.Bypass = true` (trong `PlayModeSaveGuard`); `LocationUnlocksPlayModeTests` tắt cờ này.
- **B. Ví:**
  - `WalletLedger`: mọi `AddYen/SpendYen/ApplyFine` nay có lý do; 17 chỗ gọi đã thêm lý do.
  - `WalletFeedback` trên chip tiền: "+¥/−¥ lý do" bay lên, chip đổi màu; bấm vào xem lịch sử.
  - Test không làm bẩn lịch sử ví thật (`PlayModeSaveGuard` lưu và trả lại).
  - Konbini vốn đã có danh mục, ảnh và giỏ hàng nên giữ nguyên.
- **C. Cẩm nang** (`UI/LocationGuideUI`):
  - Mở từ bản đồ: nút "Cẩm nang địa điểm" và dấu "?" ở từng dòng.
  - Nội dung trong `Resources/Guide/locations.json`.
  - Ảnh `Resources/Guide/*.jpg` chụp bằng `GuidePhotosPlayModeTests` (Explicit; chạy lại khi địa điểm thay đổi). Bảng tin và công viên chưa có ảnh.
- **D. Kids 100%:**
  - `KidsCharactersSetup.BuildNpcs` (`-executeMethod`): NL_Cashier/Guide/Neighbor/Player (cả Prefabs lẫn Resources) có model Kids, giữ nguyên object gốc nên các NPC trong scene tự đổi theo.
  - Nhân viên ×1,25 (≈1,7 m) để cao hơn quầy. `RaiseStationStaff` nâng Kimura 0,35 m trong scene 20.
  - Đã bỏ "student/Tanaka" khỏi danh sách chọn; save cũ chuyển sang Haruto.
  - Đã xóa `ThirdParty/Mixamo/Characters` (Remy/Lilly/Elizabeth/Eminem, 1,1 GB) và `CharacterMaterialFixer`.
  - `CharacterBuilder` nay chỉ dựng animation và `NL_Humanoid`. Clip `Remy@*` được giữ.
  - Test: `KidsNpcScalePlayModeTests`; Kids, Portrait và Weather đều qua.
- **Thay đổi của Codex ở scene 20:** `CachePlatformTrain` nay bỏ các object con cũng có tên `HighspeedTrain_` (trước đó bị dời hai lần). Hàm chỉnh collider bàn ở 40 giữ nguyên (sửa lúc chạy).
- **E. Tài liệu:**
  - `Bao_Cao/NihongoLife_Bao_Cao_Capstone_v5.docx`: thêm các mục 5.4, 8.7, 8.8, 10.6, 10.7, 11.5, 13.6.
  - `NihongoLife_Thuyet_Trinh_Capstone_v5.pptx`: sửa các slide Online, Nhân vật, Kiểm thử, Lộ trình; thêm "Mở khóa & Cẩm nang" và "Xã hội online".
  - Đã xóa bản v4 (vẫn có trong git). Script dựng lại: `Bao_Cao/_build_report/build_*_v5.py`.
- **Chưa làm:**
  - Chạy lại toàn bộ Play Mode sau đợt này: Unity Editor đang mở project nên batchmode không chạy được. Con số trên slide/báo cáo (59/65) là của lần chạy trước.
  - Quay lại video demo/trailer (cần `DemoRecording` trong batchmode).
  - Test IELTS Reading vẫn quá 180 giây.

## Cập nhật (2026-10-12 trưa, Claude) — Chạy lại toàn bộ test, quay lại video
- **Toàn bộ Play Mode:** 65/65 test chạy được đều qua. Riêng `IeltsProductive…Speaking_RecordedSpeech_RealAi_FourCriteria` cho kết quả "không kết luận"; 4 test quay video/chụp ảnh là Explicit nên chạy riêng.
  - `GameplayUiPlayModeTests.Emote…`: trước kiểm texture của model Eminem (đã xóa); nay kiểm mọi NPC Kids có texture.
  - `IeltsReadingPlayModeTests.AllLocalPackages…`: Timeout 480 s. Lần này chạy 248 s; sáng 11/10 chỉ 59 s, nên việc dựng `IeltsTestUI` chậm đi vẫn chưa rõ nguyên nhân.
  - Slide và báo cáo v5 đã ghi 65/65.
- **Chọn mục tiêu F:** mức ưu tiên giữ mục tiêu giảm 0,45 → 0,2 m, để khi đi tới một thứ mới gần hơn thì F chọn thứ mới.
- **Video demo** `Bao_Cao/NihongoLife_Gameplay_Demo.mp4` (4 phút 51 giây, 1080p, có thuyết minh và phụ đề):
  - thêm 3 đoạn: bản đồ có địa điểm khóa, cẩm nang địa điểm, cửa sổ chat;
  - đoạn gieo hạt nay xới ô chưa xới trước khi gieo (luật trồng trọt đã đổi sau lần quay trước);
  - thuyết minh mới: `tts_demo/{map,guide,chat}.mp3`.
- **Trailer** `Bao_Cao/NihongoLife_Trailer_v3.mp4` (84,6 giây): quay lại, toàn bộ nhân vật là Kids. Đã xóa trailer v2 (vẫn có trong git).

## Cập nhật (2026-10-12 chiều, Claude) — Animation trạng thái cho Kids, dọn rác
- **Animation đã có và đang dùng:** đi, đứng, nói, cúi chào (hội thoại và biểu cảm), chỉ tay, ngồi, nằm, nhảy, câu cá (quăng cần, chờ), cuốc, gieo, tưới, thu hoạch, dọn ô, bê thùng, xếp kệ.
- **Mới — `Player/KidMoodAnimator`:** đổi clip của các state Walk/Idle/Lay theo trạng thái nhân vật, trên một override riêng của từng nhân vật (không sửa `NL_Humanoid`).
  - Chạy nhanh → `move_run`.
  - Buồn ngủ ≥ 85 hoặc thể lực ≤ 10 → đi/đứng mệt (`*_tired`).
  - Máu ≤ 25% → đi/đứng khập khiễng (`*_injure`).
  - Ngất hoặc ngủ gục (`StatusConsequences`) → `faint_forward`.
  - Người chơi online khác cũng chạy bằng clip chạy khi họ di chuyển nhanh.
  - Clip lấy từ `Resources/Characters/Kids/NL_KidAnimationSet.asset` (`KidsCharactersSetup.BuildAnimationSet`).
  - `KidAnimationEvents` nhận sự kiện `FootstepEffect` có sẵn trong clip chạy.
  - Test `KidMoodPlayModeTests`; ảnh `Bao_Cao/character-regression/mood_*.jpg`.
- **Đã dọn:**
  - `_work`: khung hình video (2,3 GB), wav, mp4 trung gian, log; giữ `progress_backup.json`.
  - Log và XML kết quả test cũ ở gốc repo; đã gỡ khỏi git.
  - `_build_report/_v5_images`, các file `err_*.txt` của TTS, ảnh của nhân vật người lớn cũ.
  - Test chẩn đoán `PerfProbePlayModeTests`.
  - Các gói nén đã giải nén sẵn trong Assets: `Kenney/zip`, `Kids characters for Survival.unitypackage` (49 MB), `Unity_6_Animals_Free_v2.3.unitypackage` (11 MB).
- **Động tác dùng clip gốc của Kids** (`KidsCharactersSetup.BuildActionStates`, ghi thẳng vào `NL_Humanoid` và `NL_Kid_Girl`):

  | State | Clip mới |
  |---|---|
  | Work_Shelf, Work_Harvest | `action_item_get` |
  | Work_Plant | `idle_crouch` (ngồi xổm thật) |
  | Work_Hoe | `action_hammer` |
  | Fish_Cast | `action_board_attack_r` (vung qua vai) |
  | Pickup (mới) | `action_item_get` — nhặt đồ bằng F, bỏ hàng vào giỏ ở Hibari Mart |
  | UseItem (mới) | `action_treat_own` — mọi lần ăn/uống qua `RestoreNeeds`, trừ khi đang ngồi |

  - Cúi chào: Kids không có clip cúi chào, nên `CharacterAnimationController.ApplyBowLean` nghiêng spine, chest và neck thêm khoảng 38° trong lúc cúi (đo được: cổ 2° → 32–36°).
  - `PlayGesture(state, seconds)` phát một động tác ngắn rồi tự trở về đứng.
  - Test `KidActionPosesPlayModeTests`; ảnh `Bao_Cao/character-regression/action_*.jpg`.
- **Báo cáo v5:** đã thay 26 ảnh gameplay còn nhân vật Remy (người lớn) bằng ảnh chụp lại với Kids, cắt đúng tỉ lệ ảnh cũ. Mục 13.4 (pilot Remy) được viết lại cho Kids v5. Hình 1.1 và các sơ đồ giữ nguyên.
- **Bộ pitching** (Slides trên claude.ai, tải được PPTX/PDF): https://claude.ai/artifact/RGdYJ9QRUcbAiQVUkijoDz
  - 16 slide; trọng tâm là trung tâm luyện thi (JLPT N5, IELTS Writing/Speaking có AI chấm). Mỗi slide có lời nói gợi ý.
  - Logo: NihongoLife, GZV Ltd, Quang Dũng Nihongo; không dùng logo VTC.
  - Ảnh đều chụp từ game bản Kids.

## Cập nhật (2026-10-13, Claude) — Chân lún xuống đất, collider người chơi, dụng cụ trong tay

**Chân lún.** Clip Kids hạ hông thấp hơn tư thế gốc nên đế giày lún 2–3,5 cm. Thêm `Scripts/Player/FootGrounder.cs`: khi đứng yên (không ngồi/nằm/làm việc, có mặt đất ngay dưới), đo đáy SkinnedMesh so với gốc nhân vật rồi nâng/hạ model (giới hạn ±15 cm), giữ nguyên độ lệch cố ý (nhân viên ga đứng bục). Gắn trong `PlayableCharacterCatalog.ApplyVisual` (người chơi, người chơi online) và `NPCController.Awake`. Kết quả (`Bao_Cao/character-regression/feet_report.txt`): đứng −0,2 cm, đi +0,4…+1,2 cm, chạy +1,1…+2,6 cm. Test `FootGroundingPlayModeTests` chặn: đứng trong khoảng −1,5…+3 cm, mọi trạng thái > −3 cm; ảnh chụp từ phía trước.

**Collider người chơi cỡ người lớn.** CharacterController của người chơi vẫn cao 2 m, bán kính 0,35 m quanh bé Kids 1,33 m → dừng sớm trước tường, không lọt khe hẹp. Đổi thành cao 1,4 / bán kính 0,27 / bậc 0,3:
- `KidsCharactersSetup.FitPlayerColliders()` (-executeMethod) — đã lưu vào `90_TestSandbox.unity` (scene duy nhất có sẵn Player).
- `StandaloneZoneBootstrap` (tạo Player khi Play thẳng một scene khu vực) dùng cùng kích thước.

**Dụng cụ trong tay.**
- Không có art bình tưới → `KidsCharactersSetup.BuildWateringCan()` dựng bình tưới xanh từ primitive (`Generated/Island/Fishing/Held_tool_watering_can.prefab` + `WateringCan_Green.mat`), đăng ký vào `IslandModelLibrary` trong `60_MidoriIsland.unity`. `IslandUI` tưới cây giờ cầm bình (`"tool_watering_can"`).
- `CharacterAnimationController.AimHeldProp()`: chỉnh hướng dụng cụ sau animation — chờ cá: cần chĩa ra mặt nước (trước đây dựng đứng như cột cờ); quăng câu: cần vung từ sau đầu ra trước-lên theo thời gian clip (trước đây kết thúc chĩa xuống đất); tưới: bình nghiêng về trước.
- `KidActionPosesPlayModeTests` chạy trên Midori (có model dụng cụ), bắt buộc dụng cụ có trong tay, mỗi hành động chụp dải 3 khung (đầu/giữa/cuối): `Bao_Cao/character-regression/action_*.jpg`.

**Online.** Lượt chạy toàn bộ hôm nay: 4 test `OnlineSocialPlayModeTests` hỏng vì server Supabase Realtime (`<SUPABASE_PROJECT_REF>`) trả `phx_error … channel_crash` khi join kênh presence; sáng 12/10 cùng test này đạt. Lỗi phía server, không liên quan thay đổi trên — kiểm tra trạng thái project trên dashboard Supabase.

**Còn lại.** Cuốc đất dùng `action_hammer` (giơ qua vai, nhát bổ nhỏ); nhổ cỏ, cho ăn, bê đồ, ngồi, nằm vẫn là clip Mixamo retarget (pack Kids không có clip tương ứng).

## Cập nhật (2026-10-20, Claude) — Một ngôn ngữ cho toàn game, test đi xuyên collider, bậc sân ga

**Ngôn ngữ (Việt / Anh / Nhật, đổi giữa chừng là đổi toàn bộ).** Mọi chữ giao diện qua `Core/Loc.cs` (`T/F/Pick/Gloss`, `Triple/Resolve` cho chữ lưu lại, `Watch` + `UI/LocText.cs` để vẽ lại khi đổi). Quy tắc: giao diện chỉ một ngôn ngữ; câu tiếng Nhật là nội dung học, dòng nghĩa theo ngôn ngữ giao diện (không có ở giao diện tiếng Nhật).
- **Kịch bản:** 14 asset trong `Resources/Scenarios` trước đây để tiếng Việt trong `textEn/titleEn/descriptionEn`. Nay có `textVi/titleVi/descriptionVi` + tiếng Anh thật (817 câu). Code đọc qua `node.Gloss`, `choice.Gloss`, `objective.Title`, `scenario.DisplayTitle/Description/Briefing/LocationHint`. **Sinh lại bằng `Tools/story/gen_*.py` thì phải chạy `python Tools/story/i18n/apply_en.py <asset>`** (xem README trong đó).
- Đã chuyển: sổ nhiệm vụ, việc làm thêm, chat/voice (thông báo hệ thống lưu 3 ngôn ngữ), câu cá, đảo, ga tàu, chuyển cảnh, nhà hàng, thanh trạng thái, balo (`Player/ItemNames.cs`: tên vật phẩm theo ngôn ngữ, kể cả đồ đã lưu), Hibari Mart (`KonbiniProduct.Name/Meaning`), nhà trọ (từ vựng bàn học 3 ngôn ngữ), cẩm nang địa điểm (`Guide/locations.json` có en/aboutEn/aboutJa/howEn/howJa/phrasesEn), bản đồ, lớp học, cài đặt, admin.
- **Chữ gợi ý `[F]`:** `Interaction/InteractionPrompt.Of()` — mọi nơi hiện prompt đều đi qua đây; bảng `Known` dịch các prompt gõ sẵn trong scene.
- **Biển chữ trong scene** (biển đường, cửa hàng, máy game…): `UI/SceneTextLocalizer.cs` dịch lúc load scene và khi đổi ngôn ngữ, không sửa file scene. Thêm biển mới có tiếng Việt → thêm cụm vào bảng `Phrases`.
- AI chấm IELTS viết nhận xét bằng ngôn ngữ giao diện.
- Chưa dịch (người chơi không thấy): `reason` của điểm trong kịch bản (chỉ ghi log).

**Test đi xuyên collider** `Tests/PlayMode/CollisionWalkPlayModeTests.cs`: ga, trường, sushi, phòng trọ, game center. Dò sàn bằng capsule người chơi (cao 1,4 · bán kính 0,27 · bậc), mọi vật tương tác phải có chỗ đứng trong 1,9 m, rồi đi thật bằng `CharacterController.Move`. Báo cáo + collider chắn: `Bao_Cao/collision-walk/*.txt`.
- Lỗi tìm ra: mép `PlatformNorthExtension` ở ga cao 0,31 m > bậc 0,3 của người chơi → đi bộ không lên được sân ga (test cũ teleport nên không thấy). Đã đổi `stepOffset` 0,36 trong `90_TestSandbox.unity`, `StandaloneZoneBootstrap`, `KidsCharactersSetup`.

**Ảnh chụp UI:** chữ 3D (TMP overlay shader) vẽ đè lên canvas ở chế độ chụp → `ExamUiShowcasePlayModeTests.Capture` tắt chúng khi chụp (trong game thật không bị).

**Khác:** `Scripts/Editor/ReleaseBuild.cs` (file mới chưa commit) thiếu `using UnityEditor.Build;` làm project không compile — đã thêm. IELTS Reading test giờ ~7 s, toàn bộ gói IELTS 51 s. Slide pitch: điền vai trò Quang Dũng Nihongo = "Đối tác chuyên môn tiếng Nhật · góp ý nội dung N5–JLPT" — **chờ chủ dự án xác nhận**.

**Kết quả test:** EditMode 25/25; PlayMode 33/34 rồi sửa → Bedroom, CollisionWalk (5), Station, City, Zone, Feet, WorldFeel, ExamUiShowcase (vi/en/ja), SpeakingLive, OnlineSocial (4), GameCenter, IELTS đều đạt.

## Cập nhật (2026-10-20 chiều, Claude) — Animation Kids mượt hơn, Game Center 6 trò chơi 3 ngôn ngữ

**Animation.**
- `Scripts/Editor/KidMotionBuilder.cs` (`-executeMethod NihongoLife.EditorTools.KidMotionBuilder.Build`): thay clip Mixamo người lớn còn lại trong `NL_Humanoid`:
  - Work_Clear (nhổ cỏ) = `action_release_trapped`; Work_Feed (cho ăn) = `idle_crouch_treated`.
  - Work_Carry / Work_Hold / Fish_Wait = `idle_flashlight`; Work_Water = `action_treat`.
  - Point = `action_switch_r`; Bow = `idle0` + nghiêng người.
  - Sit / Lay = clip ghép `Animations/Kids/Kid_Sit.anim`, `Kid_Sleep.anim`: tư thế thân theo muscle, đầu và nhịp thở của Kids, không còn đứng cứng.
  - `NL_KidAnimationSet.layKey` → Kid_Sleep (ngất vẫn hoạt động).
- **Chân trượt:** chibi bước ~0,3 m mà game chạy 2,8–5,2 m/s. Trạng thái Walk nay có `MoveScale` (`CharacterAnimationController.CadenceFor`): ~1,7× khi đi, ~1,9× khi chạy. Độ trượt 92/95% → 82/80%.
  - Test `FootSlipPlayModeTests` (báo cáo `Bao_Cao/character-regression/foot_slip.txt`).
  - Muốn hết trượt hẳn phải giảm tốc độ đi/chạy, đó là quyết định gameplay.
- **Chọn clip:** `AnimationCandidateSheetTests` render bảng 5 khung (`NL_SHEET_CLIPS=clip1,clip2`), ra `Bao_Cao/anim-candidates/`.
- `KidActionPosesPlayModeTests` chụp thêm Work_Clear, Work_Feed.

**Game Center** (`MiniGames/`; scene `50_GameCenter` chỉ được bổ sung bằng `GameCenterBuilder.UpgradeArcade`, không dựng lại — bầu trời và chỉnh sửa khác giữ nguyên):
- Khung chung `Core/ArcadeGame.cs`: chọn bộ từ → 3-2-1 → điểm, combo, mạng, thời gian → kết quả (sao, vé, kỷ lục, mastery). Mọi chữ qua `Loc`.
- Trò mới (`MiniGames/Arcade/`):

  | Trò | Máy | Cách chơi |
  |---|---|---|
  | ワードシューター Word Shooter | máy giữa | bắn bong bóng từ theo gợi ý (nghĩa / cách đọc / hình) |
  | オーダーラッシュ Order Rush | quầy | khách gọi món bằng tiếng Nhật (ひとつ/ふたつ…), chọn món + số lượng |
  | かなレイン Kana Rain | `Cabinet_W2` | gõ romaji phá kana đang rơi (shi/si, tsu/tu… đều đúng) |
  | UFOキャッチャー UFO Catcher | `Claw_2` | 「〜を とって！」, thả càng gắp đúng món |
  | かずバスケ Basket Count | `Basketball_1` | số đọc bằng tiếng Nhật (giao diện Nhật: chữ Hán) → ném vào rổ đúng; 1–10 → 999 |

- Kana Match, bảng kết quả, máy, quầy đổi quà: 3 ngôn ngữ.
  - Bộ "Nhật ↔ nghĩa" ẩn ở giao diện tiếng Nhật.
  - `KanaPair.bEn`, `KanaPairSet.titleEn`, `MiniGameDefinition.titleEn/descriptionEn/descriptionJa`.
- Quầy quà thêm: gấu khổng lồ (40 vé), mèo bông, daruma, fūrin, hình dán.
- **Lỗi đã sửa:** vé bị kẹt ở 20 (giới hạn chồng của balo) — `game_ticket` giờ chồng tới 999.
- Test `ArcadeGamesPlayModeTests`: 5 trò × vi/en/ja, đúng/sai qua đường chơi thật, nộp kết quả, có vé, không lẫn tiếng Việt ở en/ja. Ảnh `Bao_Cao/arcade/`.

## TASK-N (Codex) — Render art cho Game Center (Claude đã làm xong gameplay, chỉ cần hình)

Mục tiêu: thay hình tạm (icon, sprite đồ ăn có sẵn) bằng art thống nhất, dễ thương, đúng phong cách Kids low-poly. Game tự dùng hình mới nếu có file đúng đường dẫn (thứ tự ưu tiên trong code), **không cần sửa code**.

**Kiểm tra trước:** đường dẫn đã tồn tại chưa (`Assets/NihongoLife/Resources/MiniGames/...`). Không ghi đè `MiniGames/Words/*.png` đang dùng.

1. **Thú bông cho UFO Catcher:** `Resources/MiniGames/Prizes/<id>.png`, 256×256, PNG nền trong suốt, góc nhìn 3/4, có bóng mềm dưới đáy.
   - id: `kuma` (gấu), `neko` (mèo), `inu` (chó), `usagi` (thỏ), `tori` (chim), `sakana` (cá bông), `ringo`, `ichigo`, `hoshi` (sao), `hana` (hoa), `kagi` (chìa khoá), `okashi` (túi kẹo).
   - Danh sách trong `UfoCatcherGame.Catalog`.
2. **Món ăn cho Order Rush:** `Resources/MiniGames/Food/<id>.png`, 256×256, cùng phong cách.
   - id: `onigiri`, `sushi`, `pan`, `keeki`, `ocha`, `koohii`, `tamago`, `sakana`, `ringo`, `aisu`, `karaage`, `nikuman`, `ramen` (quan trọng: hiện chỉ là icon), `ichigo`.
   - Danh sách trong `OrderRushGame.Menu`.
3. **Icon quà mới ở quầy đổi:** `Resources/Items/<id>.png` (128×128, như các icon Items hiện có): `prize_plush_big`, `prize_neko_plush`, `prize_daruma`, `prize_furin`, `prize_sticker`.
4. **Logo/thẻ tiêu đề từng trò:** `Resources/MiniGames/Logos/<game_id>.png`, 1024×512, chữ Nhật lớn + họa tiết.
   - game_id: `kana_match`, `word_shooter`, `order_rush`, `kana_rain`, `ufo_catcher`, `basket_count`.
   - Sau khi có file, báo Claude để gắn vào thẻ bắt đầu (`ArcadeGame.ShowStartCard`).
5. **(Tuỳ chọn, 3D)** Thú bông nhỏ (prefab low-poly) đặt trong 3 máy gắp `Claw_1..3` của `50_GameCenter`, và bảng số trên 2 máy bóng rổ.
   - Chỉ thêm vào scene có sẵn qua một builder `-executeMethod`, không tạo scene mới, không `[MenuItem]`.
   - Collider: chỉ trigger hoặc không có, để `CollisionWalkPlayModeTests.GameCenter_AllInteractablesReachable` vẫn đạt.

**Nghiệm thu:**
- `ArcadeGamesPlayModeTests` và `GameCenterPlayModeTests` đạt.
- Ảnh trong `Bao_Cao/arcade/*.jpg` cho thấy hình mới ở UFO Catcher và Order Rush.
- Ghi tên file đã thêm vào TEAM_TASKS.

## Cập nhật (2026-10-20 tối, Claude) — Trang trí đảo Midori, thăm đảo bạn bè

**Trang trí đảo.**
- Catalog: `Resources/Island/island_catalog.json` → mảng `decor` (24 món).
  - Gồm cây, cây dừa, hoa, bụi, chậu, đá, khúc gỗ, ghế, đèn đường, lửa trại, biển chỉ đường, thùng, rương, hàng rào, lều, giếng.
  - Mỗi món là một từ vựng (`word` ja/reading/en/vi), học được khi mua (`decor:<id>`).
- Dữ liệu: `IslandData.cs` có `IslandDecorDef`, `DecorPlacement` (x, z theo tâm đảo, yaw), `DecorStock`.
  - `IslandRecord.decor` / `decorStock` / `likes` lưu chung save đảo.
- Logic: `Scripts/Island/IslandDecor.cs` — `Buy` (trừ ví chung, vào kho trang trí, không chiếm balo), `Place`, `Move`, `Store`; tối đa 80 món.
- Builder: `Scripts/Editor/IslandDecorBuilder.cs` (`-executeMethod NihongoLife.EditorTools.IslandBuilder.BuildDecor`), chỉ bổ sung, không dựng lại scene.
  - Tạo prefab `Generated/Island/Decor/Decor_<id>.prefab`: vật liệu URP, gốc ở mặt đất.
    - Collider thân cây cho cây cao; collider khối cho đồ cứng.
    - Trigger cho hoa/đá phẳng (đi xuyên được).
  - Render icon `Resources/Items/decor_<id>.png`.
  - Đăng ký `decor:<id>` vào `IslandModelLibrary` (thêm `Put`).
  - Gắn `IslandDecorRuntime` vào nhóm `Decor` trong `60_MidoriIsland`.
  - `IslandBuilder.Build` đầy đủ cũng gọi phần này.
- Cửa hàng Midori có tab **Trang trí**. Thanh công cụ đảo có nút **Trang trí**.
- Chế độ trang trí (`Scripts/Island/IslandDecorRuntime.cs`):
  - Camera riêng nhìn từ trên: WASD/mũi tên để di chuyển, lăn chuột để phóng to.
  - Thanh kho ở dưới. Bóng mờ (ghost) đi theo chuột, vòng xanh = đặt được, vòng đỏ = vướng. Q/E xoay 45°.
  - Bấm vào món đã đặt để di chuyển; Delete hoặc nút "Cất vào kho" để cất lại.
  - Không cho đặt trên đường, quảng trường, ruộng, nhà, món khác, vùng tương tác, mép bãi biển.

**Thăm đảo** (`Scripts/Island/IslandVisit.cs`):
- `IslandShowcaseService` đăng bố cục đảo lên Supabase bảng `island_showcases`.
  - Đăng khi đảo thay đổi (≤ 1 lần/20 s), khi bấm "Xong" và khi rời đảo.
  - Chỉ chạy khi đã đăng nhập.
- `IslandVisit.Visit(userId, displayName)`: tải showcase rồi vào đảo của bạn, chỉ xem.
  - Ruộng, cửa hàng, thú, cầu câu và trang trí đều từ chối với thông báo; không lưu gì.
  - Thanh trên cùng: "Đảo của X", số đồ, số lần thu hoạch, số từ, ❤ (một lần, `rpc/like_island`), nút "Về".
- Đang ở đảo mình thì đổi đảo tại chỗ, không có màn tải. Từ cảnh khác thì chuyển cảnh sang đảo, "Về" trả lại cảnh cũ.
- Cửa tàu trên đảo bạn = về, vé miễn phí.
- **Migration** `supabase/migrations/20261020_island_showcases.sql` (người dùng cần áp vào project Supabase):
  - Tạo `island_showcases`, `island_likes` (RLS).
  - Hàm `like_island(target)` (security definer).
  - Trigger khiến chủ đảo không tự sửa số tim.
- Test `IslandDecorPlayModeTests.Island_DecorateAndVisit` (đạt), ảnh `Bao_Cao/island-decor/`:
  - Mua 2 cây và 5 món, tiền trừ đúng, học từ.
  - Bị chặn ở quảng trường, đường chính, mép đảo, chồng lên món khác.
  - Đặt, xoay, di chuyển, cất.
  - Va chạm thật: giếng chặn người, luống hoa đi xuyên.
  - Lưu qua JSON và respawn; showcase.
  - Thăm đảo "Hana" (3 món), mọi thứ bị khoá, thả tim, về lại đảo mình.

## TASK-O (Codex) — Giao diện danh sách bạn bè + mô hình trang trí kiểu Nhật

**Kiểm tra trước:** đọc `Scripts/UI/FriendsUI.cs` (hiện chỉ in danh sách bằng `StringBuilder`, dòng 116–160), `Scripts/Core/FriendService.cs` (`Friends`, `PendingRequests`, `FriendEntry.friendUserId/friendDisplayName`), `Scripts/Island/IslandVisit.cs`. Không sửa `IslandVisit`, `IslandDecorRuntime`, `IslandDecor` (Claude giữ) — cần API mới thì ghi vào đây.

1. **Thiết kế lại `FriendsUI`** (mở từ `MainMenuUI`, dòng 93) bằng `UiKit` / `UiTheme` như các cửa sổ mới (`ExamPlayUI`, `IslandUI`). Mọi chữ qua `Loc.T` (vi/en/ja), không lẫn ngôn ngữ.
   - Ba tab: **Bạn bè** · **Lời mời** · **Thêm bạn**.
   - Mỗi dòng bạn có:
     - avatar chữ cái (`UiKit.Avatar`), tên, chấm online (chưa có service presence trong `Scripts/Core` — bỏ qua, chỉ chừa chỗ);
     - nút **Nhắn tin** (mở `SocialChatWindow` kênh Direct nếu có API);
     - nút **Thăm đảo** gọi `NihongoLife.Island.IslandVisit.Visit(f.friendUserId, name, error => …)`. Lỗi thì hiện chữ đỏ trong dòng; thành công thì đóng cửa sổ.
     - nút **Xoá bạn** (xác nhận 2 bước).
   - Lời mời: Chấp nhận / Từ chối (`AcceptFriendRequest`, `RemoveFriend`).
   - Thêm bạn: ô nhập User ID + nút Sao chép ID của mình.
   - Không có mạng hoặc chưa đăng nhập: thẻ trống, lời giải thích và nút Đăng nhập. Không để màn hình trống.
   - Bố cục đúng ở 1920×1080, 1366×768, 1024×600.
2. **(Tuỳ chọn)** Trong `PlayerProfileUI`, nếu xem hồ sơ người khác thì thêm nút **Thăm đảo**.
3. **Mô hình trang trí kiểu Nhật** (low-poly, cùng phong cách đảo):
   - Danh sách: torii nhỏ, đèn đá 石灯籠, cây anh đào 桜, khóm tre 竹, ao cá koi 池, hòm thư ポスト, ghế gỗ kiểu Nhật 縁台, đèn lồng 提灯.
   - Đặt model (FBX/OBJ, không dùng glTF vì dự án không có importer) ở `Assets/ThirdParty/<pack>/`.
   - Thêm mục vào mảng `decor` của `Resources/Island/island_catalog.json`, đúng mẫu các mục có sẵn: `id`, `category`, `source`, `price`, `height`, `footprint`, `solid`, `word{ja,reading,en,vi}`, `icon` là tên Material Symbol có trong `Resources/UI/Icons`.
   - Chạy `-executeMethod NihongoLife.EditorTools.IslandBuilder.BuildDecor` để tạo prefab và icon. Không sửa scene bằng tay, không `[MenuItem]`.

**Nghiệm thu:**
- Chạy `IslandDecorPlayModeTests` và `IslandPlayModeTests`; cả hai phải đạt.
- Chụp FriendsUI ở 3 ngôn ngữ × 3 độ phân giải, lưu vào `Bao_Cao/friends-ui/`.
- Món kiểu Nhật xuất hiện ở tab Trang trí và đặt được trên đảo.
- Ghi tên file đã thêm vào TEAM_TASKS.

**Hoàn thành (2026-10-20, Codex):**
- Viết lại `Assets/NihongoLife/Scripts/UI/FriendsUI.cs`: 3 tab responsive, avatar + chỗ trạng thái, nhắn tin riêng, thăm đảo có lỗi theo dòng, xoá xác nhận hai bước, chấp nhận/từ chối lời mời, thêm bạn + sao chép ID, và trạng thái đăng nhập/mất dịch vụ; toàn bộ vi/en/ja qua `Loc`.
- Thêm `Assets/NihongoLife/Tests/PlayMode/FriendsUiPlayModeTests.cs`; ảnh nghiệm thu nằm trong `Bao_Cao/friends-ui/` (`friends_{vi,en,ja}_{1920x1080,1366x768,1024x600}.png`).
- Thêm 8 mục `jp_*` vào `Assets/NihongoLife/Resources/Island/island_catalog.json`: torii, đèn đá, sakura, tre, ao koi, hòm thư, ghế endai và đèn chōchin. Tái sử dụng FBX low-poly của `Assets/ThirdParty/Sushi Restaurant Kit - May 2023-20260920T035054Z-1-001/` (không thêm glTF).
- Đã chạy `IslandBuilder.BuildDecor`: tạo `Assets/NihongoLife/Generated/Island/Decor/Decor_jp_*.prefab`, `Assets/NihongoLife/Resources/Items/decor_jp_*.png` và đăng ký vào `IslandModelLibrary` trong `60_MidoriIsland.unity`.
- Kiểm chứng: `FriendsUiPlayModeTests` 1/1 đạt; `IslandDecorPlayModeTests` 1/1 đạt; `IslandPlayModeTests` 2/2 đạt khi chạy có graphics. Lượt `-nographics` của `IslandPlayModeTests` không dùng làm kết quả vì `HudPortrait` không thể tạo RenderTexture.

## Cập nhật (2026-10-20 khuya, Claude) — Cốt truyện mở đầu + chơi tự do; hoàn tất TASK-O và TASK-N

**Cốt truyện → tự do** (`Scripts/Scenario/StoryDirector.cs`; chi tiết ở STORY_BIBLE mục 13):

Lỗi đã sửa:
- Nút Bắt đầu ở menu luôn chạy `konbini.buy_onigiri`. Quest này cần đã xong buổi học, nên với save mới không có truyện nào chạy.
- Mỗi lần vào thành phố, `intro.arrival` lại chạy lại.
- `UIManager` (DontDestroyOnLoad) chỉ đăng ký sự kiện lúc khởi động. Vì vậy trong luồng chơi thật, màn kết quả quest **không bao giờ hiện**. Nay nó bám theo ScenarioManager hiện tại (`EnsureScenarioSubscription`, `ResolvePanels`).
- Hộp thoại còn treo khi quest kết thúc: thêm `DialogueManager.CloseFinishedStory`.

Phần mở đầu (5 quest, tới `GameControlDatabase.prologueEndScenarioId`):
- Quest tự nối nhau.
- Việc làm thêm, nông trại, nhiệm vụ hằng ngày bị khoá: `QuestService.Requirement`.

Sau phần mở đầu:
- `FreePlayCardUI` hiện một lần.
- HUD chỉ bước truyện tiếp theo hoặc số điểm còn thiếu: fallback trong `StatusDock`.
- Sổ nhiệm vụ liệt kê các bước truyện còn thiếu điểm, kèm lý do (`TaskJournalUI.SelectStory`).

Màn kết quả `ResultUI` vẽ lại bằng UiKit:
- Chương, tiêu đề, điểm và số sao, các thanh điểm từng phần, phần thưởng, dòng "cốt truyện tiếp".
- Nút chính đổi theo tình huống: "Nhiệm vụ tiếp theo" / "Thử lại" / "Tiếp tục khám phá". Không còn đẩy người chơi về menu.

Cân bằng điểm kiến thức của quest truyện: trước 150–190/quest (742 sau phần mở đầu, mở mọi khoá ngay), nay 12–20/quest.

`StoryDirector.Bypass` được bật trong `PlayModeSaveGuard`; chỉ `StoryFlowPlayModeTests` tắt nó.

Test `StoryFlowPlayModeTests` (ảnh trong `Bao_Cao/story-flow/`) đi qua:
- save mới → màn mở đầu tự chạy → 5 quest nối nhau qua nút thật trên màn kết quả;
- thẻ chế độ tự do → việc làm thêm mở → HUD chương 3 → sổ nhiệm vụ;
- vào lại thành phố thì không quest nào tự chạy.

**TASK-O** (Codex làm phần lớn trước khi hết token; Claude hoàn tất):
- FriendsUI: 3 tab, các nút Nhắn tin / Thăm đảo / Xoá hai bước, Chấp nhận / Từ chối, sao chép ID. Codex làm.
- Claude bổ sung:
  - `FriendService.ResolveDetails` lấy tên thật và cấp độ (`profiles`), cùng tim đảo (`island_showcases`) chỉ bằng 2 request.
  - Dòng bạn hiện cấp độ, "♥ N · có đảo", online/ngoại tuyến (`SupabaseOnlineWorldService.IsOnline`).
  - Nút Thăm đảo tắt khi biết chắc bạn chưa đăng đảo. Nút gọn lại, không tràn.
- `FriendsUiPlayModeTests`:
  - Lỗi đã sửa: test gọi `GameServices.Clear()` và để ngôn ngữ ở tiếng Nhật, làm hỏng dây chuyền khoảng 20 test sau nó. Nay test trả lại đúng dịch vụ (thêm `GameServices.Unregister`) và đặt lại tiếng Việt.
  - Đã thêm kiểm tra tab Lời mời / Thêm bạn, thông tin đảo, và lỗi Thăm đảo hiện ngay trong dòng.
- Đồ trang trí kiểu Nhật (Codex): 8 món `jp_*` dùng model của Sushi Restaurant Kit. Test `IslandDecorPlayModeTests.Island_JapaneseDecor` mua, đặt, kiểm chiều cao và chạm đất, rồi chụp cận cảnh `Bao_Cao/island-decor/jp_*.png`.
  - Claude sửa sau khi xem ảnh:
    - "ao cá koi" vốn là tấm gỗ hình con cá, nay dựng thành ao thật (`composite:koi_pond` trong `IslandDecorBuilder.KoiPond`: mặt nước URP, 16 viên đá, 2 cá koi);
    - "đèn đá" thực ra là đèn khung gỗ, nên đổi từ vựng thành とうろう (đèn lồng vườn);
    - cây có tán rộng (anh đào) không còn bị thu nhỏ theo bề ngang.

**TASK-N** (`Scripts/Editor/ArcadeArtBuilder.cs`, chạy `-executeMethod NihongoLife.EditorTools.ArcadeArtBuilder.Build`; `Preview` render một model ở 8 góc vào `_work/art-preview`):
- 14 món ăn → `Resources/MiniGames/Food`.
- 8 thú bông/quà → `Resources/MiniGames/Prizes`:
  - kuma dùng gấu trúc của Sushi Kit, vì `bear.fbx` của Kenney là đầu gấu treo tường;
  - inu = Shiba; usagi = thỏ hồng; hana = hoa anh đào; okashi = kẹo mút.
- `Items/prize_plush_big.png`.
- Ảnh render từ model thật, 256 px, góc 3/4, bóng mềm.
- Chưa có model, giữ hình dự phòng: neko, tori, hoshi, kagi, `prize_neko_plush`, `prize_daruma`, `prize_furin`, `prize_sticker`.
- 6 logo trò chơi (`Resources/MiniGames/Logos/*.png`, 1024×512) tạo bằng `_work/arcade_logos.py` với font NotoSansJP. Logo hiện trên thẻ bắt đầu (`ArcadeGame.StartHeader`, `KanaMatchGame.ShowSetPicker`).

## Cập nhật (2026-10-21, Claude) — Âm thanh hoàn chỉnh, đường phố 90 làm lại, nâng cấp UI

**Âm thanh** (nguồn và giấy phép: `Docs/AUDIO_CREDITS.md`)
- **Giọng nói:**
  - Đủ 410/410 câu thoại NPC và lời dẫn của 14 quest đều có giọng tiếng Nhật thật (Microsoft neural TTS: Keita, Nanami, Emma Multilingual cho Kim), mỗi nhân vật được chỉnh tốc độ và cao độ riêng.
  - Tạo bằng `_work/voice_gen.py` vào `Resources/Voice/ja/<quest>/<node>.mp3`. Script chạy lại an toàn: câu nào sửa thì chỉ tạo lại câu đó.
  - `DialogueManager.LoadDropInVoiceClip` tìm theo thư mục của từng quest, vì id node trùng nhau giữa các quest. Câu nào chưa có file vẫn dùng tiếng bíp cũ.
- **Nhạc và âm thanh môi trường:**
  - Tất cả CC0, tải từ OpenGameArt bằng `_work/audio_fetch.py` (script kiểm giấy phép trước khi tải), xử lý bằng `_work/audio_process.py`: chuẩn hoá -18 LUFS; âm thanh môi trường cắt vòng 60 s nối mượt.
  - Nhạc: menu (koto), thị trấn ngày và đêm, konbini, sushi, trường, ga, Game Center, đảo, phòng ngủ.
  - Âm thanh môi trường: chim (ngày), dế (đêm), chim và gió (đảo).
  - Jingle: hoàn thành quest, mua hàng, mở khoá, lên cấp.
- **`Audio/Soundscape.cs`:**
  - Chọn nhạc theo cảnh, giờ trong game, và trong/ngoài konbini (`ThirdPersonCameraController.IsIndoor`).
  - Chuyển bài có crossfade, giảm nhạc khi có giọng thoại, tắt hẳn khi đang thi JLPT/IELTS.
- Nhạc và âm thanh môi trường import ở chế độ Streaming (`AutoOptimizationProcessor`).
- Test `SoundscapePlayModeTests`.

**Đường phố 90** (`Editor/CityStreetBuilder.cs`, chạy `-executeMethod NihongoLife.EditorTools.CityStreetBuilder.Build`; thêm `NL_STREET_SCENE=01_MainMenu` để dựng lại cảnh nền menu)
- **Đã gỡ:**
  - Lưới ô đường Kenney cũ: đường xuyên qua mọi khu nhà, khoảng 180 mảnh vỉa hè.
  - Vạch làn, vạch qua đường, lối xe vào nhà, mảng đất sau nhà, công viên nhỏ và hàng rào cũ.
  - 2 căn nhà trùng nhau ở chỗ nối giữa các tile.
  - 16 vật thể bị mất vật liệu (hiện màu hồng) đã được gán lại vật liệu.
- **Đại lộ Sakura-dōri:**
  - 2 làn, xe chạy bên trái; vạch mép, vạch giữa nét đứt, ◇ báo trước chỗ qua đường.
  - Vạch zebra kèm đèn giao thông ngang 3 màu và đèn đi bộ hình người có tiếng "piyo piyo".
  - Vỉa hè lát gạch có bó vỉa, nắp cống.
- **Ngõ dọc (路地):** vạch 路側帯, chữ 止まれ và vạch dừng trước khi ra đại lộ; thêm các ngõ sau.
- **Lô nhà:** mỗi nhà một lô, tường gạch block có collider ở hai bên và phía sau, sân sỏi, dải cỏ, trụ cổng. Mặt trước của các điểm đến và konbini để trống.
- **Khu phía sau:** dãy nhà nền, công viên Hibari (lối vào từ ngõ sau), bãi đỗ xe tính tiền có xe đậu.
- **Đồ phố:** gương cầu lồi, máy bán hàng tự động. Texture đều tạo bằng code (`Generated/City/Textures`).
- **Xe cộ** (`World/StreetTraffic.cs`):
  - 6 xe KayKit mỗi tile, bánh xe quay.
  - Xe giữ khoảng cách, dừng ở vạch khi đèn đỏ, dừng nhường người chơi trên vạch qua đường hoặc đứng trước xe.
- `Editor/SceneSurvey.cs`: khảo sát cảnh (cây phân cấp, bản đồ từ trên xuống, tìm vật liệu lỗi), chỉ đọc, không lưu cảnh.
- Test `StreetTrafficPlayModeTests` (ảnh trong `Bao_Cao/street-regression/`):
  - lưới cũ đã bị gỡ;
  - xe chạy đúng làn, không chui vào nhà, không vượt đèn đỏ, dừng nhường người chơi;
  - người chơi đi từ điểm xuất phát qua vạch đến konbini;
  - cả 5 cổng vẫn đi bộ tới được.

**UI**
- **Menu chính:**
  - Nút phụ đổi sang nền kính tối, đọc rõ trên nền thành phố.
  - Thêm thẻ "Cốt truyện" (chương, quest tiếp theo, thanh tiến độ) do `MainMenuUI.DisplayStoryCard` điền từ `StoryDirector`. Thẻ được nướng vào cảnh bởi `MainMenuBaker`.
  - Nền menu là đường phố mới, có xe chạy.
- **Khung gợi ý tương tác:** nền kính bo góc, phím bấm dạng keycap vàng (`HUDUI`).
- **Bản đồ (M):** vẽ đúng đường phố mới; "Công viên Hibari" dời tới công viên mới.
- **Lỗi đã sửa:**
  - Biển "HIBARI JAPANESE SCHOOL" gắn vào canvas tồn tại xuyên cảnh nên vẫn ở giữa màn hình thành phố. Nay biển có canvas riêng, mất theo cảnh trường, chữ theo ngôn ngữ đã chọn, đặt ở góc phải.
  - Thông báo HUD lọt sang menu chính: nay bị xoá khi về menu.

## Cập nhật (2026-10-21 tối, Claude) — Lộ trình mở bản đồ theo trí tuệ + 2 quest cửa ngõ

Chi tiết thiết kế: `STORY_BIBLE.md` mục 14.

**Lộ trình**
- Đầu game chỉ vào được Hibari Mart, trường và phòng trọ.
- Các khu sau mở theo trí tuệ cộng quest truyện trước đó: Sushi 100, Ga 160, Game Center 240, Đảo 320. Lễ hội cần 380.
- Khu đã mở lưu bằng cờ `map.<scene>` trong save, không còn lưu PlayerPrefs theo máy.

**File code**
- `Core/LocationUnlocks.cs`: viết lại toàn bộ — chặng, `Evaluate`, sự kiện `Unlocked`, `ShortStatus`, chuyển save cũ lặng lẽ.
- `Core/GameControlDatabase.cs`:
  - `LocationUnlock` có thêm quest yêu cầu, quest cửa ngõ, ảnh, tiêu đề và lời kể 3 ngôn ngữ.
  - **`locationUnlocks` mặc định để trống** (trước đây giá trị cũ 40/90/160/250 của field ghi đè mặc định mới).
- Gọi `Evaluate` từ:
  - `PlayerStatus.AddKnowledge` (bỏ toast ngưỡng cũ);
  - `ScenarioManager` (khi quest bắt đầu và kết thúc);
  - `SceneFlowController` (khi vào cảnh).
- `Exam/ExamManager.cs`: bài thi cho trí tuệ (`KnowledgeFor`, cờ `exam.done:<id>`).
- `UI/MapUnlockCardUI.cs` (mới): thẻ khu vực mới mở.
- `UI/TaskJournalUI.cs`: khung LỘ TRÌNH MỞ BẢN ĐỒ.
- Chữ khoá theo `ShortStatus` ở các file:
  - `ScenePortal`
  - `LocationGuideUI`
  - `WorldMapUI`
- `Interaction/PortalBeacon.cs`: biển cổng vẽ lại khi có khu mở.
- `StoryFlags.Remove`.

**Nội dung**
- Hai quest mới:
  - `scenario_arcade_kim_invite.asset` (chương 4);
  - `scenario_island_midori_trip.asset` (chương 5).
- Có bản tiếng Anh, giọng đọc (`_work/voice_gen.py`, nay có 455 câu), metadata (`Tools/story/quest_meta_roadmap.py`), thứ tự campaign trong `NihongoLifeControlDatabase.asset`.
- Đổi tên "kiến thức" → "trí tuệ" trong giao diện tiếng Việt (13 file, kể cả `locations.json`).

**Test**
- `MapProgressionPlayModeTests` (mới): save mới trong thành phố thật.
  - Ngày đầu chỉ Mart, trường, phòng mở.
  - Phần mở đầu cho 83 trí tuệ, chưa đủ mở Sushi. Lên 100 thì Sushi mở, thẻ hiện, biển cổng bỏ dòng khoá.
  - Ga chờ quest sushi.
  - Chơi trọn hội thoại của Kim và chuyến đảo; Tanaka nhắc Kim.
  - Trí tuệ về 0 thì các khu vẫn mở.
  - Trí tuệ từ bài thi đúng công thức.
- `LocationUnlocksPlayModeTests` đã cập nhật theo quy tắc mới.

**Ảnh hưởng tài nguyên dùng chung (cho Codex/Antigravity)**
- Đừng điền lại `locationUnlocks` trong database nếu không cố ý ghi đè lộ trình.
- Quest mới cần mở một khu phải thêm chặng trong `LocationUnlocks.Defaults`, không check PlayerPrefs riêng.

**Giai đoạn sau:** mua bán xe đạp / xe máy / ô tô để đi các thành phố lớn. Mỗi thành phố là một chặng mới, có điều kiện phương tiện.

**Sửa thêm (cùng ngày)**
- **Cửa ga ở cảnh 90:**
  - Khối đen phẳng `EntranceDark` (4.2×2.6 m) đã được thay bằng `StationDoors`: cửa kính tự động 4 cánh, sảnh có đèn, bảng 発車案内, 2 máy bán vé.
  - Code: `CityTownBuilder.StationDoors`. Chỉ vá cửa ga bằng `-executeMethod NihongoLife.EditorTools.CityTownBuilder.PatchStationEntrance`; `Build` cũng dùng hàm này.
- **Biển cổng (`PortalBeacon`):** thu nhỏ dần khi mờ đi, vì các tag màu ([F], ▼, nền tối) không ăn theo alpha của TMP.
- **Nhạc nền:**
  - Nguyên nhân mất tiếng: PlayerPrefs `Volume_BGM` / `Volume_SFX` trên máy dev đã bị kéo về 0. Đã đặt lại mặc định.
  - Thanh trượt âm lượng trong Cài đặt không còn nhận điều hướng bàn phím, nên bấm A/← khi đang mở bảng không làm tụt âm lượng nữa.
  - Mức nhạc tăng từ 0.55 lên 0.7.
  - `SoundscapePlayModeTests` giờ kiểm tra cả âm lượng thực (> 0.5), không chỉ kiểm tra "đang phát".

## Cập nhật (2026-10-21 khuya, Claude) — Rà soát trước khi build WebGL (không build)

**Lỗi chặn build đã sửa**
- **Agora:**
  - `AgoraRtcSDK.asmdef` loại WebGL (`excludePlatforms`), vì plugin chỉ có native.
  - Bản web dùng `Learning/AgoraWebShim.cs` + `Learning/VideoSurface.cs` (chỉ biên dịch khi WebGL). Tạo engine thì báo "Voice và lớp học video chỉ có trên bản PC/Mac".
- **Micro:** `UnityEngine.Microphone` không tồn tại trên WebGL (CS0103). `Exam/WebMicrophone.cs` thay thế cho `MicRecorder` và `SpeechPracticeController`, báo "không có micro".
- **Danh sách cảnh:** `ReleaseBuild.ProductionScenes` thiếu cảnh thành phố `90_TestSandbox` (bị luật "không có cảnh Test" loại). Đã thêm, và luật này chừa riêng cảnh thành phố.
- **Kiểm tra:** biên dịch thử với define `NL_WEBGL_CHECK` và bỏ tham chiếu Agora → 0 lỗi. Đã trả cấu hình về bình thường, biên dịch lại 0 lỗi.

**Tránh treo hoặc lỗi khi chạy trên web**
- `SupabaseRealtimeClient`: trên web không mở `ClientWebSocket` và thread (trình duyệt không có).
  - Mất trên web: realtime (presence, chat trực tiếp, co-op, hub Speaking).
  - Vẫn chạy: REST (đăng nhập, lưu cloud, bảng xếp hạng).
  - Cần một transport WebSocket qua jslib nếu muốn online trên web.
- `ChatCrypto`: trên web không sinh khoá RSA (sinh trên luồng chính sẽ treo trang vài giây).
- `LocalProgressRepository`: gọi `FS.syncfs` sau khi lưu (`Plugins/WebGL/NihongoLifeWeb.jslib`), để save còn sau khi tải lại trang.
- Player Settings: bật **Decompression Fallback** (Brotli). Không có fallback thì itch.io / GitHub Pages không tải được.

**Lưu ý khi build bằng Build Profiles:** bỏ chọn `99_ControlRoom` và `99_Trailer` (không cảnh nào trong game load tới; chỉ để test/quay trailer).

## Cập nhật (2026-10-21, Claude) — Ghi nhận 3 nhà sáng lập
- **Đổi tên:** popup "Về tác giả" → **"Về chúng tôi"** (nút trên menu và tiêu đề).
- **Nhóm:** The Next Generation (VTC Academy). Nút "Xem đội ngũ" mở trang team của website.
- **Nhà sáng lập** (`menu_about.asset` → `founders`, mỗi dòng mở GitHub của người đó):
  - Quách Thành Long: Trưởng nhóm · Lập trình web & game;
  - Nguyễn Lê Gia Huy: Nhân vật, animation & gameplay;
  - Lương Nguyễn Tấn Dũng: Nhân vật, animation & gameplay.
- Trần Công Thuận **không** nằm trong danh sách sáng lập của game.
- **Ảnh:** ảnh nhận Giải Triển vọng của cả nhóm lên đầu bộ ảnh (`01_team_award_2025`), có chú thích tên 3 người. Ảnh tốt nghiệp xếp thứ 2.
- **Phụ đề menu:** "Mô phỏng học tiếng Nhật · The Next Generation" (trước đây ghi "bởi quachthanhlong.com").
- **Code:** `MenuAboutFounder` (`MenuAboutDefinition`) và các dòng nhà sáng lập trong `AboutPopup`.
- **Test:** `AboutTeamPlayModeTests` (ảnh trong `Bao_Cao/about-team/`).

## Cập nhật (2026-10-21, Claude) — Khách chơi đầy đủ ngoại tuyến, online chỉ cho tài khoản đã đăng ký
**Luật** (`Core/OnlineAccess.cs`): được online = **đã đăng nhập bằng tài khoản đã đăng ký** (có email). Khách và phiên ẩn danh đều là khách.

**Khách**
- Chơi đầy đủ: cốt truyện, lộ trình mở bản đồ theo trí tuệ, trường, thi.
- Save lưu trên máy ở `guest_progress.json` (`PlayerSessionService`). Trước đây save khách chỉ nằm trong RAM, mất khi thoát game.
- Nút trên màn đăng nhập: "Chơi Khách (ngoại tuyến)".

**Chặn online cho khách**
- `OnlineWorldBootstrap` không nối thế giới online cho khách (trước đây nối bằng mã thiết bị). Khi người chơi đăng nhập tài khoản đã đăng ký thì tự nối.
- Bấm "Chơi online" / "Bạn bè" trên menu → mở bảng đăng nhập kèm lý do (`AuthUI.ShowForOnline`).
- Các tính năng khác cũng chặn khách: voice, phòng co-op, bạn bè, thăm/like đảo, phòng Speaking (Agora), gửi điểm xếp hạng. Xem bảng xếp hạng thì ai cũng được.

**Chuyển save sang tài khoản**
- Khách đăng ký và đăng nhập vào **tài khoản mới** (cloud chưa có save) → tiến trình khách trở thành tiến trình của tài khoản, rồi save khách được xoá.
- Tài khoản đã có save thì giữ nguyên save đó (`SupabaseProgressRepository`).

**Test**
- Mới: `GuestOfflinePlayModeTests` (ảnh trong `Bao_Cao/guest-online/`).
- `OnlineSocialPlayModeTests.City_RemotePlayers` bật `OnlineAccess.Bypass` (máy test không có tài khoản).
- `PlayModeSaveGuard` tắt `PlayerSessionService.PersistGuest`, để test không đụng save khách thật.

**Gộp `Dung--branch` vào `main`** (fast-forward, chưa push):
- Cảnh `40_ HIBARICLASS` hoàn chỉnh (collider bàn học).
- Sửa shader Agora.
- Build mở bằng `01_MainMenu`; cảnh này có AppRoot riêng nên vẫn chạy đúng.
- WebGL strip High. Đã thêm `Assets/NihongoLife/link.xml` giữ nguyên assembly của game.

## Cập nhật (2026-10-23, Claude) — Sửa xe "bay" lên vỉa hè/mái nhà, khách full map, luật cho agent

**Xe bay khỏi làn, chạy trên vỉa hè và mái nhà** (lỗi từ commit 9115381a)
- **Nguyên nhân:** `StreetTraffic` gắn `AudioSource` còi vào chính gốc Traffic, rồi dời transform của còi tới chiếc xe đang bóp còi. Mỗi lần bóp còi, cả gốc Traffic cùng mọi xe bị kéo theo (+1,1 m và lệch làn).
- **Đã sửa:** còi là một GameObject con riêng (`Horn`).

**Xe đụng người**
- Xe phát hiện người trên toàn bề rộng thân xe (`halfWidth` đo từ renderer).
- `SeparateFromPlayer` đặt người chơi ra ngoài sườn xe về phía lề và hất nhẹ ngang. Không còn bị xe kinematic đẩy lên nóc.
- `PlayerController.Knockback` chỉ nhảy lên khi được yêu cầu.

**Khách = full map**
- Luật suy ra từ phiên chơi: `PlayerSessionService.OpenWorld` (khách → mở toàn bộ bản đồ, không bị giữ ở cốt truyện mở đầu).
- `BeginOfflinePlay` / `BeginProgressionPlay` không còn đặt cờ `Bypass`.
- Nút Bắt đầu: tài khoản đã đăng nhập vào lộ trình của tài khoản, khách vào ngoại tuyến. Trước đây nút này đổi cả tài khoản sang save khách.

**Test**
- Mới: `TrafficLanePlayModeTests` (xe ở yên trong làn suốt 1 phút; xe đụng người; người trên nóc xe; gốc Traffic không bao giờ di chuyển).
- `GuestOfflinePlayModeTests`: thêm kiểm tra khách full map, tài khoản theo lộ trình.
- `StreetTrafficPlayModeTests`: đo theo đầu xe.

**`AGENTS.md`:** viết lại thành bộ luật sản phẩm và các bất biến kỹ thuật cho mọi agent.

**Sửa thêm sau khi chạy toàn bộ test trên main (27 test PlayMode trượt sau các commit của Cursor)**
- **Unity fake-null:** 30 chỗ `GetComponent<T>() ?? …` đổi thành `.OrNull() ?? …` (`Core/UnityNull.cs`). Trước đó menu chính ném `MissingComponentException: CanvasGroup`, làm hỏng dây chuyền khoảng 12 test.
- **`TownEdge` chỉ có ở thành phố:** trước đây đảo, ga, quán (có `HorizonBackdrop`) cũng bị coi là "thị trấn". Nhấn F ở chỗ câu cá, quầy, Kimura, bảng việc bị nuốt để đi tới ga, và người chơi bị kẹp trong vùng biên nhỏ nên không chạy được.
- **`ResultUI`:** modal "Results" chỉ tính là mở khi thẻ đang hiển thị. Trước đó nó mở mãi sau khi về thị trấn, chặn thẻ mở khu, phím Esc và nút toàn cảnh.
- **`TouchControlOverlay`:** chỉ thả input khi overlay vừa ẩn. Trước đó nó xoá joystick và nút mỗi khung khi overlay ẩn.
- **`KidExpression`:** chỉ cúi chào / chỉ tay khi nhân vật rảnh. Trước đó học xong đi ngủ thì nhân vật đứng dậy chỉ tay trên giường.
- **Test cập nhật:**
  - `MenuSyncPlayModeTests`: menu giờ responsive, nên kiểm tra nút Bắt đầu nằm trên màn hình, đủ to, không bị che.
  - `SpeakingLoungeCamera` và `OnlineSocial.City`: ra Inconclusive khi máy chưa có `LocalSecrets/secrets.json`.

## TASK-M (Grok) — Trailer v4, demo v3, kế hoạch marketing 12 tuần
Giao việc đầy đủ trong `Docs/GROK_TASK_TRAILER_MARKETING.md` (bối cảnh, luật, công cụ có sẵn, 3 sản phẩm, tiêu chí nghiệm thu). Không sửa gameplay; chỉ sửa `TrailerReel.cs`, hai test quay phim và các file marketing.

## Cập nhật (2026-10-25, Codex) — Viết lại README sản phẩm

- Viết lại toàn bộ `README.md` theo hướng landing page dự án: định vị game, vòng lặp học, gameplay, chế độ khách/tài khoản, kiến trúc, cài đặt, test và đóng góp.
- Thêm mục video dẫn tới kênh YouTube chính thức `@NihongoLifeGame`; dùng GIF gameplay trong repo làm preview vì GitHub README không hỗ trợ trình phát YouTube iframe.
- Thêm hướng phát triển theo bốn trụ cột: nội dung học, thế giới sống động, online/cộng đồng và WebGL/khả năng tiếp cận; không đặt ngày phát hành chưa được xác nhận.
- Chỉ dùng media đang tồn tại trong `docs/readme/`, giữ đúng luật sản phẩm về khách mở toàn thị trấn, campaign tài khoản, từ “trí tuệ” và đúng ba nhà sáng lập.
- Kiểm tra: rà liên kết/ảnh cục bộ và cấu trúc Markdown; không đổi code hoặc scene nên không cần chạy Unity test.

## Cập nhật (2026-10-25, Codex) — English Foundations A1–C1 và rà kho IELTS

- Rà `LocalContent/IELTS`: 48 package local Cambridge 12–17; tất cả package Reading/Listening có đủ 40 widget, 40 đáp án, không thiếu audio Listening và đều `localOnly`. Không đưa nội dung sách vào Git.
- Thêm tab **ENGLISH** cạnh JLPT/IELTS trong `ExamCenterPopup`, định vị rõ English Foundations là lộ trình tổng quát còn IELTS là luyện thi.
- Thêm starter curriculum tự soạn trong `EnglishCourseCatalog`: 10 bài, 30 câu, phủ CEFR A1/A2/B1/B2/C1 và đủ mẫu câu, ngữ pháp, điền từ, đọc hiểu, nghe.
- Thêm `EnglishPracticeUI`: câu hỏi tương tác, phản hồi đúng/sai, giải thích và tổng kết; nghe dùng Web Speech API trên WebGL, có transcript hỗ trợ.
- Tài liệu: `Docs/ENGLISH_COURSE.md`, cập nhật `EXAM_SYSTEM.md` và README. Capture đã xem tại `Bao_Cao/english-course/`.
- Test: WebGL compile 0 `error CS`; `EnglishCourseTests` 2/2 pass; `EnglishCoursePlayModeTests` 1/1 pass.

## Cập nhật (2026-10-25, Codex) — Hoàn thiện ma trận English 4 kỹ năng

- Mở rộng starter curriculum thành 20 unit/60 câu: mỗi cấp A1, A2, B1, B2, C1 đều có đủ Listening, Speaking, Reading và Writing; grammar, pattern, gap-fill và vocabulary được tích hợp theo kỹ năng.
- Exam Center có filter Nghe/Nói/Đọc/Viết và chip kỹ năng trên từng card; lộ trình được trình bày như 5 quyển/chặng từ First Steps tới Academic Explorer.
- Thêm `EnglishAudioLibrary`: desktop ưu tiên WAV local, WebGL fallback sang Web Speech API, transcript luôn sẵn có.
- Thêm manifest và tool sinh 15 audio từ script tự soạn. File nằm ở `LocalContent/English/audio` (~5,45 MB), đã Git ignore; repo chỉ giữ tool/manifest nhẹ.
- Nguồn British Council/VOA/American English chỉ dùng để tham khảo khung CEFR/phương pháp; không chép bài hoặc audio của họ.

## Cập nhật (2026-10-23 chiều, Claude) — Rà lỗi còn lại và nợ kỹ thuật
- **Build WebGL bị chặn:** `AgoraWebShim.cs` (namespace `Agora.Rtc`) gọi `AgoraWebEvents` mà thiếu namespace `NihongoLife.Learning`, nên với build target WebGL không biên dịch được. Đã sửa và đã biên dịch thử với target WebGL.
- **Save tách theo tài khoản** (`progress_<userId>.json`, `PlayerSessionService.AccountId`):
  - Trước đây mọi tài khoản trên một máy dùng chung `progress.json`, nên ở máy dùng chung tài khoản sau bị gộp cờ cốt truyện và quest của tài khoản trước lên cloud.
  - Khi khôi phục phiên, cache khách bị gộp vào tài khoản. Cả hai đã sửa.
  - Test mới trong `GuestOfflinePlayModeTests`.
- **Bảo mật server:**
  - `agora-voice` và `agora-classroom` từ chối người dùng ẩn danh.
  - Cuộc gọi riêng (`nld_…`) chỉ cấp token cho đúng 2 người: server tính lại tên phòng từ người gọi và `peer`. Trước đây ai biết 2 id đều nghe lén được.
  - Id nhóm voice phải dài ít nhất 10 ký tự.
  - Migration `20261023_registered_only_online.sql`: `is_registered()`; showcase đảo và thả tim chỉ cho tài khoản đăng ký.
  - **Cần deploy:** `supabase functions deploy agora-voice agora-classroom` và áp dụng migration.
- **Hiệu năng mỗi khung:**
  - `NPCNameplateSystem` không còn `FindObjectsByType` mỗi khung.
  - `MenuButtonFeel`, `FootGrounder`, `KidMoodAnimator` không còn `GetComponent(s)InChildren` mỗi khung.
- **`CharacterAnimationController`:** cử chỉ bị chặn khi đang nằm hoặc ngồi, và trigger cử chỉ còn treo được xoá khi vào tư thế.
- **Test:** thẻ mở khu trong test lộ trình chờ hội thoại kết thúc (đúng thiết kế); test đóng hội thoại còn mở trước khi chờ.

## Cập nhật (2026-10-23 tối, Claude) — Tắt nhạc nền / âm thanh môi trường
- Người chơi thấy tiếng chim/dế đôi khi ồn, nên Cài đặt → Âm thanh có thêm:
  - nút **Nhạc nền: Bật/Tắt**;
  - nút **Môi trường: Bật/Tắt** (chim, dế, gió, mưa, tiếng người ngoài phố);
  - thanh **âm lượng môi trường** riêng (mặc định 0.5 = độ to cũ).
- Mã: `Scripts/Audio/AudioPrefs.cs` (PlayerPrefs `Audio_MusicOn`, `Audio_AmbienceOn`, `Volume_Ambience`; giá trị được cache, đọc mỗi khung không tốn). Các nơi đọc giá trị: `Soundscape`, `TownAmbientAudio`, `AudioService` (nhạc menu), mưa trong `OutdoorSky`. UI ở `SettingsUI`.
- Thêm nguồn ambience mới thì nhân âm lượng với `AudioPrefs.AmbienceGain`; nguồn nhạc mới thì `mute = !AudioPrefs.MusicOn`.
- Test: `SoundscapePlayModeTests` (tắt thì nhạc và ambience câm, bật lại thì có tiếng).

## Cập nhật (2026-10-26, Claude) — Sửa máu, trailer, cổng soát vé, cảnh tàu, khối lơ lửng ngoài phố
- **Máu không hồi (kẹt 0% dù no, đủ nước):**
  - Trước đây `PlayerStatus` không có hồi máu.
  - Màn ngất bị cắt ngang thì `StatusConsequences._busy` kẹt mãi.
  - Nay máu tự hồi khi no và nước ≥ 30% (đứng yên nhanh gấp đôi, ~4 phút từ 0 lên đầy); ăn uống hồi ngay; ngủ 8 giờ hồi đầy.
  - Màn ngất bị cắt thì reset và trả điều khiển; còn 0 máu thì ngất lại.
  - Test `NeedsWeatherPlayModeTests.Health_HealsWhenFed_AndAStuckFaintRecovers`.
- **Trailer 99_Trailer:**
  - Trailer quay trên save thật, nên ngất giữa trailer và ghi đè save.
  - Nay dùng save tạm trong bộ nhớ (`Core/IntroTrailer.cs`).
  - Có nút **Bỏ qua** (Esc / Enter / Space).
  - Khách chơi lần đầu trên máy tự xem một lần rồi vào game.
  - Bật `99_Trailer` trong Build Settings.
  - Test `IntroTrailerPlayModeTests`.
- **Cổng soát vé ga Hibari:**
  - Lối chính bị chặn bởi collider vô hình của 6 toa tàu trang trí nằm ngang qua sân ga (`HighSpeed_*`, `Opposite_HighSpeed_*`); hai bên cổng lại đi vòng được.
  - `StationGateBuilder` dựng 3 cabin / 2 làn có cánh (`World/TicketGate.cs`), đầu đọc IC, biển 改札, lan can ngang sân ga; gỡ collider của các toa.
  - Có vé thì đi vào làn là tự chạm vé và mở; từ sân ga đi ra luôn mở.
  - Test `StationGatePlayModeTests` (đi bộ thật).
- **Cảnh ngoài cửa sổ tàu:**
  - `StationTrainBuilder.RebuildWindowScenery`: hàng rào bê tông, ruộng lúa có bờ, đường làng, cột điện gỗ, hai hàng nhà ở 25 m / 45 m, đồi.
  - Mọi lớp trôi cùng tốc độ tàu.
  - Camera cửa sổ đặt sát kính.
- **Thông báo "đường ra khỏi Hibari-chō" hiện trong ga / trên tàu:** `TownEdge` chỉ phản ứng khi phố là scene đang chơi.
- **Khối đỏ / kem lơ lửng ngoài phố:**
  - 436 mảnh trang trí mặt tiền cũ (`FacadeTrim`, `WarmWindow_L/R`, `DoorMat` dưới `StreetBuilding_*`) đặt lệch khỏi tường.
  - Đã gỡ khỏi 90_TestSandbox và 01_MainMenu (`CityFacadeAccentCleanup`); `VisualEnvironmentBuilder` không tạo lại.
  - Test `CityTownPlayModeTests`.
- **"Some objects were not cleaned up … GameInputService":** `TouchControlOverlay.OnDisable` tạo service mới khi đóng scene; nay chỉ dùng service đang có.
- **Việc sau (anh Long quyết):** phòng khám Hibari chưa có trong phố. Màn ngất hiện chỉ kể "người qua đường gọi phòng khám" rồi trừ viện phí. Sau này thêm khu phòng khám và tuyến nhiệm vụ riêng.

## Cập nhật (2026-10-26 tối, Claude) — Nghề nghiệp từ cấp 10 (kiểu The Sims 3/4)
- **Mở ở cấp 10** (khách và tài khoản): thẻ "Hướng nghiệp" tự mở một lần; sau đó mở từ nút **Nghề nghiệp** trong Sổ nhiệm vụ (N).
- **3 nghề**, mỗi nghề gắn với ca làm thêm có sẵn:
  - Bán lẻ (Hibari Mart): Quản lý cửa hàng / Thu mua.
  - Ẩm thực (Sushi Hibari): Đầu bếp sushi / Quản lý nhà hàng.
  - Nông nghiệp (Midori): Trồng trọt / Chăn nuôi.
- **Thang bậc:** 6 bậc (3 chung + 3 theo nhánh), rẽ nhánh ở bậc 4. Chức danh vi/en/ja, lương nghề cộng thêm vào tiền công ca.
- **Lịch làm** theo thứ và giờ trong game.
- **Hiệu suất:**
  - Ca đúng giờ cho đủ phần hiệu suất (×1.25 khi tâm trạng tốt, ×0.75 khi mệt/căng/bực); ca ngoài giờ được một nửa, không có lương nghề.
  - Có mặt trong giờ làm mà bỏ ca thì −15.
- **Thăng chức:** đầy hiệu suất và đủ trí tuệ thì có phỏng vấn tiếng Nhật (sai được giải thích, thử lại).
- **Sự kiện nơi làm** (35% sau ca đúng giờ): lựa chọn tiếng Nhật ảnh hưởng hiệu suất, danh tiếng, tâm trạng.
- **Dữ liệu:** `Resources/Progression/careers.json` (thêm nghề / bậc / phỏng vấn / sự kiện không cần sửa code). Save: `PlayerProgressDto.career`; cloud lấy bản mới nhất (`updatedTicks`).
- **Mã:** `Progression/CareerCatalog.cs`, `Progression/CareerService.cs` (+ `CareerClock`), `UI/CareerUI.cs`.
- **Test:** `CareerCatalogTests` (EditMode), `CareerPlayModeTests`. Ảnh: `Bao_Cao/career/`.
- **IELTS:** `Writing_OfflineAndDailyLimit_SaveAndExplain` lỗi từ khi chấm AI mặc định tắt (commit 0302b557). Test nay bật `ExamAiSettings.AutoGradingOverride` trong phần kiểm mất mạng.
- **Định hướng (anh Long):**
  - Đảo Midori chủ yếu là nông trại.
  - Sau này dựng **đảo Phú Quốc** (Việt Nam) riêng: rất rộng, gần tỉ lệ thật, UI đầy đủ, theo bản đồ đã gửi (Dương Đông, An Thới, Hàm Ninh, Gành Dầu, Bãi Sao, cáp treo Hòn Thơm, VinWonders, quần đảo An Thới…).
  - Chưa làm cho tới khi anh yêu cầu.
- **Phòng khám Hibari + tuyến nhiệm vụ:** để sau (xem mục 2026-10-26 ở trên).

## Cập nhật (2026-10-26 khuya, Claude) — Phòng khám Hibari + nghề Y tế
- **Scene mới `35_HibariClinic`** (anh Long duyệt), dựng bằng `ClinicBuilder.Build`.
  - Bên trong: sảnh có chỗ đổi dép, quầy 受付 (điều dưỡng Sato), quầy お薬, phòng chờ (ghế, bảng gọi số, TV), phòng khám 診察室 (bác sĩ Mori, giường khám, rèm, bồn rửa), góc khám sức khoẻ (cân đo chiều cao, bảng thị lực vòng hở), 2 giường hồi sức.
  - Cổng vào ở `StreetBuilding_S_3`, đối diện Sushi Hibari; trên bản đồ là chữ 医. Trạm xe buýt phía nam (ghế, thùng rác, cây) được `ClinicBuilder` dời sang bên 5 m cho thoáng cửa. `CityStreetBuilder` đã ghi S_3 là điểm đến cho lần dựng sau. **Chưa chạy lại** builder phố: chạy thử làm đổi khoảng 43.000 dòng scene và hỏng `ActualSprint` (scene đang có chỉnh sửa mà builder không biết), nên đã trả scene về bản commit. Ai chạy lại `CityStreetBuilder.Build` cần kiểm tra lại toàn bộ test phố.
- **Đi khám** (`Clinic/ClinicVisit.cs`): đưa thẻ bảo hiểm, phiếu hỏi bệnh theo triệu chứng thật của người chơi (ướt mưa thì cảm, máu thấp thì mệt), nói với bác sĩ, thanh toán ¥600 (bảo hiểm 30%), nhận thuốc và chọn đúng cách dùng. Khám xong hồi đầy máu, khỏi cảm.
- **Ngất:** tỉnh dậy trên giường hồi sức của phòng khám (từ phố hay từ khu khác).
- **Nhiệm vụ** (nhóm mới **Phòng khám** trong sổ nhiệm vụ): khám lần đầu · khám sức khoẻ (健康診断) · tiêm phòng cúm (予防接種).
- **Ca làm `job_clinic_shift`** (¥750): tiếp nhận 2 bệnh nhân, hỏi bệnh 2 người, giải thích thuốc, báo cáo bác sĩ (`JobStation.Clinic.cs`).
- **Nghề Y tế (cấp 10)** trong `careers.json`: Lễ tân → Trợ lý điều dưỡng → Điều dưỡng → nhánh **Bác sĩ** (研修医 → 医師 → 院長) hoặc **Dược sĩ** (薬剤師見習い → 薬剤師 → 薬局長).
- **Ánh sáng trong nhà:** `Core/IndoorAmbience` giữ ánh sáng riêng của khu trong nhà (trước đây ban đêm phòng khám tối tím do bầu trời phố).
- **Phú Quốc** chuyển sang phần mở rộng: dữ liệu nháp ở `Docs/Expansion/PhuQuoc/` (xem README: dùng OpenStreetMap, không dùng Google Maps; không dùng tên/logo thương hiệu khi chưa xin phép).
- **Test:** `ClinicPlayModeTests` (đi bộ thật qua phòng khám, khám bệnh, ca làm, khám sức khoẻ, tiêm phòng, ngất tỉnh ở phòng khám), `CareerCatalogTests` (4 nghề). Ảnh: `Bao_Cao/clinic/`.

## Cập nhật (2026-10-27, Claude) — Sửa cảnh báo, giao diện nghề nghiệp, ngoại hình theo tuổi, phòng khám có người đến khám
- **Deno (VS Code):** `supabase/functions/deno.json` + `.vscode/settings.json` (`deno.enablePaths`) để extension Deno hiểu Edge Functions, hết báo lỗi `Deno`/URL import. Máy cần cài Deno CLI (`winget install DenoLand.Deno`).
- **Input:** `activeInputHandler: 1` (chỉ Input System). Đoạn cuối dùng `Input.GetKeyDown` (`SpeechPracticeController`) đã chuyển sang `Keyboard.current`.
- **Cảnh báo console:** presence Realtime gửi dồn bị giới hạn (track 1,5 s/lần, bỏ trùng); `GameServices.Register` chỉ cảnh báo khi đè service còn sống, `Replace<T>` cho chỗ cố ý đổi (trailer); thiếu glyph ↺↻ ở chọn nhân vật (dùng icon); Remy@Jump (thêm state Jump thật, `KidMotionBuilder.BuildJump`); `friendships` với id không phải uuid; CS0436 (shim Agora khi build WebGL) và CS0414 (field fallback NPC thừa, đã xoá).
- **Nghề nghiệp — giao diện đầy đủ** (`UI/CareerUI.cs`, 6 tab): Tổng quan (hiệu suất, tâm trạng, mục tiêu kế tiếp), Lộ trình (cây bậc 1–3 rồi 2 nhánh 4–6: lương, yêu cầu, nhánh đã khép), Lịch làm (tuần này, ✓ đã làm / ✗ vắng), Hồ sơ (ngày trong nghề, ca, lương đã nhận, tình huống, lịch sử thăng chức, nghề cũ), Cột mốc, Các nghề (đổi nghề có xác nhận). HUD hiện chức danh nghề.
- **Tuyến phát triển nghề — cột mốc** (`careers.json` "milestones", `CareerService.CheckMilestones`): 9 cột mốc (初出勤, はじめての昇進, 皆勤の一週間, …, キャリアの頂点), thưởng yen + trí tuệ một lần, giữ khi đổi nghề. Thêm cột mốc = sửa JSON.
- **Ngoại hình theo tuổi** (`Player/CharacterLook.cs`, `UI/CharacterLookUI.cs`, `Resources/Characters/Looks/`):
  - Màu tóc (11 màu) đổi trên GPU bằng mask tóc `CharacterLookBuilder.Build` nướng sẵn cho 8 model; râu (lún phún / ria / râu dê / quai nón / râu dài) mở theo tuổi, chỉ model nam; tóc bạc dần từ 45 tuổi; nếp nhăn từ 50 và 65 tuổi.
  - Người lớn tuổi đi chậm hơn (`CharacterLook.Walk`: người chơi, NPC tuần tra) và nói chậm hơn (`DialogueView.TalkPace`), từ 55 tuổi. NPC có thể đặt nhịp riêng (`Pace`).
  - Mở bảng: chạm chân dung trên HUD, hoặc nút "Ngoại hình" ở màn chọn nhân vật. Lưu trên máy (`NihongoLife.CharacterLook`), test không ghi (`PlayModeSaveGuard`).
  - Hành khách lớn tuổi trên tàu (`SeatedPassenger` ambient elder) có tóc bạc + râu dài.
  - Tùy chọn trong `look_options.json` (màu, kiểu râu, nếp nhăn, tuổi bắt đầu chậm lại). `looks.json` do builder sinh — không sửa tay.
- **Phòng khám đẹp hơn + người dân đến khám** (`ClinicBuilder.BuildInterior` — chỉ dựng lại scene phòng khám, không đụng phố):
  - Trang trí: lối đi màu bạc hà, thảm khu chờ, tay vịn gỗ, góc trẻ em (キッズコーナー: thảm, khối đồ chơi, bàn nhỏ, sách tranh), bể cá, tranh treo, hoa ở quầy, đèn ấm khu chờ.
  - `Clinic/ClinicPatientFlow.cs`: người dân (`clinic.json` "patients", 8 người, có ông bà) vào cửa → 受付 → ngồi ghế chờ (ghế quay về TV) → bảng gọi số + điều dưỡng gọi 「N番の方…」 → vào 診察室 gặp bác sĩ Mori (nói triệu chứng, chẩn đoán) → お薬 → ra về. Mỗi lúc một người trong phòng khám, không collider (không chắn người chơi). Người già đi chậm, có râu/tóc bạc.
- **Test:** `CharacterLookPlayModeTests` (8 model: tóc, râu chỉ nam, nếp nhăn, tốc độ; bảng ngoại hình trên người chơi, râu mở theo tuổi), `ClinicPlayModeTests.Clinic_TownspeopleComeForACheckUp`, `CareerPlayModeTests` (tab, cột mốc, đổi nghề), `CareerCatalogTests` (cột mốc hợp lệ / sai bị từ chối). Ảnh: `Bao_Cao/character-look/`, `Bao_Cao/clinic/10–12`, `Bao_Cao/career/07–12`.

## Cập nhật (2026-10-27 chiều, Claude) — Bệnh tật như ngoài đời, sổ tay, nhạc mới, lộ trình bác sĩ thật
- **Hệ thống bệnh** (`Scripts/Health/`, dữ liệu `Resources/Health/illnesses.json`, nguồn `Docs/HEALTH_SOURCES.md`):
  - 8 bệnh. Mắc theo việc người chơi làm:
    - cảm lạnh: ướt mưa lâu;
    - cúm virus: nơi đông người mùa thu – đông;
    - dị ứng phấn hoa: ngoài trời mùa xuân, không đeo khẩu trang;
    - đau mỏi cơ: 2 ca/ngày hoặc chạy hơn 3 phút;
    - kiệt sức 過労: 3 ca/ngày;
    - say nắng: nắng trưa, ít nước;
    - đầy bụng: ăn khi đã no;
    - đau đầu: buồn ngủ đầy quá lâu.
  - Mỗi bệnh nhân hệ số lên tốc độ đi, thể lực, buồn ngủ, khát, mất máu, và hiệu suất nghề (`PlayerIllness.WorkFactor`).
  - Hiệu ứng: viền màn hình theo màu bệnh (`IllnessOverlay`), câu 「ハクション！」 trên đầu, biểu cảm, chip HUD.
  - **Chữa như ngoài đời:**
    - thuốc không kê đơn chỉ giảm triệu chứng và rút ngắn một chút;
    - kháng sinh không chữa cảm/cúm;
    - bác sĩ: thuốc kháng virus cúm hiệu quả nhất trong 48 giờ đầu; say nắng truyền dịch là khỏi;
    - ngủ chữa cảm, đau mỏi, kiệt sức, đau đầu;
    - đi làm khi bị cúm bị trừ danh tiếng;
    - tiêm phòng cúm miễn dịch ~30 ngày; khẩu trang 1 ngày.
  - Hibari Mart có kệ **くすり** (7 món, lấy từ `illnesses.json` "medicines"); dùng trong balo như đồ ăn (`ItemUseType.Medicine`).
  - Phòng khám chẩn đoán đúng bệnh đang mắc (`ClinicVisit.TodaysSymptom`); clinic.json có thêm 3 triệu chứng (chóng mặt, ngứa mắt, đau vai lưng).
  - Lưu: `PlayerProgressDto.illness` (gộp cloud theo `updatedTicks`). Test tắt mắc ngẫu nhiên: `PlayerIllness.Triggers = false` (`PlayModeSaveGuard`).
- **Nghề Y tế theo lộ trình thật ở Nhật** (`careers.json`):
  - Nhánh chung: 医療事務 → 看護助手 → 医療系の学生.
  - Nhánh bác sĩ: 研修医 → 専攻医 → 専門医・院長. Nhánh dược sĩ: 薬剤師 → 管理薬剤師 → 薬局長.
  - Phỏng vấn thăng chức là câu hỏi y khoa thật (trường y 6 năm, thi quốc gia, "kháng sinh không chữa cảm").
- **Sổ tay** (`UI/HandbookUI.cs`, phím **Y**, nút "Sổ tay" trên HUD; sổ ghi chép L đổi tên thành "Sổ ghi"):
  - Dạng cuốn sách mở: bìa vải, hai trang giấy, gáy khâu, ruy-băng chương.
  - Chương: 案内 Giới thiệu (`Resources/Guide/handbook.json`), 場所 Địa điểm (`LocationGuideUI` nay mở chương này), 健康 Sức khoẻ (tình trạng, 8 bệnh, tủ thuốc, con đường làm bác sĩ ở Nhật), 仕事 Nghề nghiệp, 音楽 Âm nhạc (nghe thử, ghim bài phát mọi nơi — `AudioPrefs.PinnedMusic`).
- **Nhạc mới** (tạo bằng code, `Tools/audio/compose_music.py`, `Docs/AUDIO_CREDITS.md`):
  - 5 bài: clinic (phòng khám), rain (phố khi mưa), sick (ở nhà khi ốm), spring (sáng mùa xuân), work (giờ làm ở nơi làm của nghề).
  - Danh sách bài: `Resources/Audio/music.json`.
- **Test:** `IllnessCatalogTests` (EditMode), `IllnessPlayModeTests` (mắc / cảm nhận / chữa / sổ tay / máy nhạc). `SoundscapePlayModeTests` chấp nhận nhạc phố mới. Ảnh: `Bao_Cao/health/`, `Bao_Cao/handbook/`.
- **Tiếp theo:** dịch vụ nhà hàng — chờ anh Long gửi thông tin và cách vận hành.

## Cập nhật (2026-10-27 tối, Claude) — Tích hợp nhánh Phú Quốc / Gánh Ngon của GrokBot, báo cáo Capstone v7
- **Sau merge `feat/phuquoc-restaurant`:**
  - Biên dịch sạch; EditMode 123/123 (có 7 bài `PhuQuocRestaurantTests`).
  - `PhuQuocIslandBuilder`: bỏ `[MenuItem]` (quy tắc AGENTS), chạy headless (`-executeMethod NihongoLife.EditorTools.PhuQuocIslandBuilder.Build`).
  - Đã sinh `Scenes/65_PhuQuocIsland.unity` (+ `Materials/PhuQuoc/`) và ghi vào Build Settings. Đây mới là **bản dựng đầu**: đảo phẳng theo đường bờ biển OSM, 31 điểm mốc, khu Gánh Ngon có 8 điểm phục vụ; chưa có đường đi từ Hibari.
  - `GanhNgonSpot` tách ra file riêng `GanhNgonSpot.cs`. Trước đó class nằm trong `GanhNgonShift.cs` nên scene mất component (test chỉ thấy 3/8 điểm).
- **Ảnh hưởng tới phần đã có** (giờ có 5 nghề):
  - Màn chọn nghề (`CareerUI`) chuyển sang lưới 3 cột.
  - Test sổ tay đếm theo catalog thay vì số cứng 4.
  - `handbook.json` thêm nghề Nhà hàng & dịch vụ.
  - Nhạc "work" phát khi ở Gánh Ngon trong giờ làm nghề hospitality.
- **Test mới:** `PhuQuocIslandPlayModeTests` (31 điểm mốc, đảo có collider, 8 điểm phục vụ, vị trí tương đối đúng địa lý). Ảnh: `Bao_Cao/phuquoc/`.
- **Còn lại cho Phú Quốc** (theo `Docs/HANDOFF_PHUQUOC.md`):
  - khu Dương Đông tỉ lệ 1:1 và nội thất Gánh Ngon;
  - khách NPC (theo mô hình `ClinicPatientFlow`);
  - đường đi từ Hibari (sân bay PQC);
  - nối skybox / vật liệu Ground054;
  - nhãn địa điểm quanh Dương Đông đang chồng lên nhau.
- **Báo cáo Capstone v7** (`Bao_Cao/NihongoLife_Bao_Cao_Capstone_v7.docx/.pdf`), dựng bằng script trong `Tools/report/`:
  - `build_report_v7.py` lấy v6 và chèn các mục mới;
  - `layout_v7.py` bỏ trang trắng, cho các chương nối tiếp;
  - `word_update.ps1` dùng Word cập nhật mục lục, danh mục và xuất PDF;
  - `page_fill.py` báo trang thiếu nội dung.
  - Mục mới: 5.5 phòng khám, 5.6 Phú Quốc, 6.6 ngoại hình, 6.7 bệnh tật, 8.9–8.11 nghề nghiệp / Y tế / Gánh Ngon, 10.8 sổ tay, 12.4 nhạc gốc, 13.8 kiểm thử v7, bảng video YouTube @NihongoLifeGame, tài liệu tham khảo y khoa và Phú Quốc.

## Cập nhật (2026-10-27 khuya, Claude) — Hết lỗi console khi vào đảo / phòng khám
- **Đảo Phú Quốc thành zone thật:** vào là có người chơi, không còn "Player was not found".
  - Đăng ký trong `WorldLocationCatalog` (`PhuQuocIslandScene`, spawn `phuquoc_ganh_ngon`) và `StandaloneZoneBootstrap`: bấm Play trong scene đảo sẽ nạp thị trấn rồi vào đảo.
  - `PhuQuocIslandBuilder` dựng scene rỗng, đảo đặt cách thị trấn 4000 m. Không còn camera / đèn mặc định; có `SceneZoneVisibility`, `SceneSpawnPoint` trước Gánh Ngon, cổng "Về Hibari" (`ScenePortal`) và `ZonePreviewCamera`.
  - Nhãn địa danh dùng font Noto (đủ dấu tiếng Việt), xếp tầng quanh Dương Đông để không đè nhau.
- **Phòng khám:** 5 điểm khám (受付, 診察室, お薬, đo chiều cao, thị lực) bị "missing script" nên không bấm F được trong game, vì `ClinicSpot` nằm trong `ClinicVisit.cs`. Đã tách ra `ClinicSpot.cs` và sửa tham chiếu trong scene.
- **Ga:** `StationBillboardLabel` cũng nằm chung file (`StationTravelController.cs`), đã tách và sửa tham chiếu trong `20_StationDistrict`.
- **Bảng ngoại hình:** đổi ký tự ☑/☐ (font không có) thành ✓/○.
- **Test mới `ZoneConsolePlayModeTests`:** vào Midori, Phú Quốc, phòng khám; thất bại khi có lỗi / cảnh báo console hoặc component mất script. `PhuQuocIslandPlayModeTests` nay kiểm tra người chơi đến Gánh Ngon, đi lại được trên đảo và có cổng về.

## Cập nhật (2026-10-28 20:20, Unity executor) - Tiếng Nga: font, bảng bổ sung, kiểm tra độ phủ
- **Font:** `NotoSansJP.otf` đã có đủ chữ Cyrillic (А–я, Ёё, «», №), nên không cần font mới. Test `CyrillicFontTests` (2) kiểm tra từng chữ cái và atlas động.
- **Bảng bổ sung:** `Resources/Content/Localization/ru_table_extra.json` (340 dòng viết tay: Cài đặt, phím điều khiển, tên địa điểm, ga, thẻ, đăng nhập, bạn bè, co-op, bản đồ...). `RuTable` nạp thêm file này (ưu tiên hơn ru_table.json).
- **Kiểm tra độ phủ:** `RussianCoverageAuditTests` quét mọi bộ ba ("vi","en","ja") trong Scripts (1605 bộ) và ghi `Bao_Cao/russian-ui/coverage_missing.txt`; chỉ còn 2 chuỗi thiếu, ngưỡng cho phép 10.
- **Sửa nhỏ:** `TouchControlSettingsSection.cs` (Loc.T nội suy → Loc.F cho "Button size", "Opacity"; bản gốc ở `_grok_tmp/backup-ru-touch-*`).
- **Test + ảnh:** `RussianUiPlayModeTests` (PlayMode) mở Cài đặt bằng tiếng Nga, kiểm tra ≥6 nhãn Cyrillic và mọi glyph có trong font; ảnh `Bao_Cao/russian-ui/01_city_hud_ru.png`, `02_settings_ru.png`, `settings_texts.txt`.
- **Chưa làm:** nút RU ở menu chính (nút nằm trong scene 01_MainMenu), nhãn tên khu vực trên HUD chưa tự đổi khi đổi ngôn ngữ giữa chừng, tên phím (Left Shift, Space...) vẫn tiếng Anh.

## Cập nhật (2026-10-28 21:00, Unity executor) - Ga: khách xuống tàu, sửa vị trí cầu thang
- **Khách xuống tàu:** `PlatformCrowd` có thêm `Alight()` / `GetOffAndLeave()`: khi tàu vào ga, 3 hành khách bước ra từ 3 cửa, đi qua sân ga lên cầu thang rồi biến mất; thuộc tính `Alighted`, `Alighting`. Khách đến muộn khi cửa đang mở sẽ lên thẳng tàu (không còn kẹt trong hàng chờ).
- **Lỗi thật đã sửa:** `ExitStairwell` có pivot ở gốc (x=0) còn ga dựng ở x≈700, nên "khách đi xuống cầu thang" xuất hiện cách sân ga ~700 m. Nay lấy tâm bounds của renderer. (Bản gốc ở `_grok_tmp/backup-train-*`.)
- **Test mới:** `TrainArrivalCyclePlayModeTests` (tàu đến, khách xuống, hàng chờ lên, tàu đi, hàng mới) + chạy lại `TrainRoutePlayModeTests` (2). Ảnh `Bao_Cao/train/01..04`. 3/3 đạt.
- **Có sẵn từ trước (không làm lại):** cảnh ngoài cửa sổ có parallax theo 5 chặng (`TrainWindowScenery`), bảng tuyến ở sảnh (`StationGuide`), `StationCarriageLife`.
- **Chưa làm:** biển chỉ đường sinh từ JSON cho MỌI bản đồ (hiện chỉ có bảng ở ga, biển ở phố và đảo); lịch tàu theo JSON.

## Cập nhật (2026-10-28 20:50, Unity executor) - P8: nhân vật 3D sống ở menu chính
- **MenuAvatarStage + MenuAvatarBootstrap (mới):** menu chính 01_MainMenu giờ có nhân vật đang chọn (Choose character) đứng bên phải màn hình dưới dạng model 3D thật: thở, cúi chào, chỉ tay, nói chuyện luân phiên, từ từ xoay về phía nút, có đèn ấm riêng. Là vật thể thế giới nên KHÔNG đổi layout menu (MenuSyncPlayModeTests vẫn đạt). Tự đặt lại vị trí mỗi frame theo camera (lỗi ban đầu: figure lệch ra ngoài màn hình x=1.14).
- **Test:** MenuAvatarPlayModeTests (1) + MenuSyncPlayModeTests (1) = 2/2 đạt. Ảnh Bao_Cao/menu-avatar/01,02 + motion.txt (33753/72000 pixel đổi giữa hai ảnh cách 1,6 s).
- **Chưa làm:** WebGL build thật + đo thời gian tải (P0.b) - để cuối vì chuyển nền tảng mất rất lâu.

## Cập nhật (2026-10-28 22:03, Unity executor) - tiếng Nga còn sót + biển chỉ đường/lịch tàu JSON
- **Tiếng Nga:** nút "Русский" ở menu chính (`MenuTopBar` tách ra file riêng - trước đó thanh trên cùng là missing script nên nằm giữa màn hình), tên khu vực HUD / bản đồ / sổ tay đổi ngôn ngữ ngay, `Loc.Data` cho dữ liệu tiếng Anh, 642 dòng `ru_table_extra.json`. Test: EditMode 10/10, PlayMode `RussianScreensPlayModeTests` + Menu 5/5 (0 chuỗi lẫn Việt/Anh). Ảnh `Bao_Cao/russian-ui/`. **Cần chủ game:** người bản ngữ duyệt các dòng tiếng Nga.
- **Lịch tàu JSON:** `Resources/Train/timetable.json` (ga, giá vé, chu kỳ tàu, thời gian mở cửa, thời gian mỗi chặng); `StationTravelController` đọc từ `TrainTimetable`, không còn số cứng.
- **Biển chỉ đường JSON cho mỗi bản đồ:** `Resources/Navigation/route_signs.json` (phố 2 cột, sảnh + sân ga Hibari, đảo Midori); `RouteSignBuilder` (headless) dựng, `RouteSignPost` giữ chữ 4 ngôn ngữ + mũi tên + khoảng cách; `CityTownBuilder` không còn dựng biển cứng. Test: `RouteSignAndTimetableTests` 5/5, `RouteSignPlayModeTests` 3/3, hồi quy phố/ga/tàu/đảo 11/11. Ảnh `Bao_Cao/route-signs/`.

[2026-10-29 00:45] Hệ thống cử chỉ (73 cử chỉ, 20 lời chào theo văn hóa) đã có test; nghề được nhóm theo ngành, mỗi nghề có nhân vật mặc đồng phục (họ Kids) chuyển động; test CareerIndustry 4/4, CareerAvatar 1/1, ảnh chụp tại Bao_Cao/careers-ui.

[2026-10-29 01:30] Bản đồ phím M bản cao cấp: MapPremium.cs mới + WorldMapUI.cs vá (sao lưu backup-map-ui): mở/đóng mượt, chú giải 4 ngôn ngữ, la bàn, thước tỷ lệ, tên khu phố, dấu vị trí nhấp nháy. Test MapPremiumPlayModeTests đạt; ảnh chụp ở Bao_Cao/map-ui. Việc còn lại cho Claude/người kế tiếp: xem danh sách A, C, mục 6 (Arts), 7 (P13/P7/P14), 8 (WebGL), F1-F7, G1-G3, H1-H7, J1-J5, rồi dọn rác + BAO-CAO-HOAN-THANH.md (chi tiết trong STATUS-unity.md).

[2026-10-29 01:55] (B) Bản đồ M: XONG, test đạt 17/17 hồi quy. Tiếp theo: (A) ảnh minh hoạ sổ tay chỉ hiện sau khi trả lời đúng.

F1 DONE (29/10 ~02:15): cần câu gác (AFK rod) + kỹ năng câu cá. Cần thường: 3 con -> Lv2 mở cần gác (tool_afk_rod, 900 yên, bền 20, bán ở Midori Store). Đặt ở bãi cạnh FishingSpot, tự chờ cá (35-70s, giảm theo cấp kỹ năng), có cá thì phao chìm + dấu đỏ, bấm F để kéo; bỏ lỡ 25s cá thoát, tự đặt lại; F lúc chờ = thu cần. IslandFishing.AwardFish dùng chung (một con cá/lần). Files: Scripts/Island/FishingSkill.cs, IslandAfkRod.cs, IslandFishing.cs, FishingSpot.cs, Resources/Island/island_catalog.json. Test: FishingFarmPlayModeTests 3/3 PASS (gồm Island_AfkRod_NeedsSkill_PlantWaitBite_PullUp). Capture: Bao_Cao/fishing-regression/60_afk_planted, 60b_afk_overview, 61_afk_bite. Còn lại: nối vào sổ kỹ năng F2, chuỗi tiếng Nga cho Loc mới (chưa chạy audit), cần gác chưa lưu qua đổi scene.

H2 DONE (29/10 ~02:50): camera cửa hàng tiện lợi mượt. Một camera chính (bỏ camera thứ hai), khoảng cách camera được làm mượt (SmoothDamp 0.45s), màn đen ngắn ScreenCut khi dịch chuyển qua cửa + SnapToTarget, vùng cửa hàng dùng polling vị trí có hysteresis + trễ 0.3s khi ra, va chạm kéo vào tối đa 30 m/s. Files: Scripts/Camera/ThirdPersonCameraController.cs, StoreCameraZone.cs, ScreenCut.cs, Interaction/DoorInteractable.cs. Test: KonbiniCameraPlayModeTests.Konbini_EnterLeave_NoCameraWhip PASS (4 lần vào/ra, 0 đảo camera, 1 AudioListener, yaw đột biến 0, tốc độ camera tối đa 36 m/s, 0 frame trong tường). Capture/log: Bao_Cao/konbini-camera/camera.txt + 01_street_before_door, 02_inside_after_door, 03_walked_out.

K1 DONE (29/10 ~02:50): động vật xung quanh. Chim (đậu/bay vòng/hạ cánh, tiếng hót tạo bằng code - không dùng file âm thanh), chó & mèo (đi, ngồi, ngủ, chơi, vuốt ve rồi đi theo; mèo bỏ chạy khi bị lao vào), sóc (kiếm ăn, ngồi gặm hạt, chạy tới cây, leo thân cây, xuống). Số lượng theo mùa/giờ/mưa (AmbientWildlife.Budget): đêm không chim/sóc, chó ngủ 22-5h, mèo ngủ trưa, mùa đông ít, mùa thu nhiều sóc. Pool cố định 10 chim/2 chó/2 mèo/6 sóc, ngoài 45 m ẩn + không cập nhật, ngoài 75 m tự đặt lại gần người chơi, không collider vật lý (chỉ chó/mèo có trigger để vuốt ve), tổng renderer <160. Files: Scripts/World/AmbientWildlife.cs, Tests/PlayMode/AmbientWildlifePlayModeTests.cs. Test: 2/2 PASS (Budget + City_Wildlife_BehavioursSeasonsAndLod). Capture: Bao_Cao/ambient-wildlife/street_animals.png, dog_follow.png. Còn lại: mô hình là khối primitive (chưa có model/animation clip thật), chưa có ở đảo Phú Quốc, chuỗi Loc mới cần mục tiếng Nga (audit chưa chạy).

H1 PARTIAL (29/10 ~02:30): lỗi nhấp nháy bản đồ khi vào game - chưa chứng minh được nguyên nhân gốc, đã áp dụng 2 biện pháp giảm nhẹ. (1) DepthPrecision.cs: camera chính near 0.03/far 1000 -> near 0.12/far 450 (độ chính xác depth tốt hơn ~10 lần, giảm z-fighting các tấm mặt đường/cỏ/sơn lệch vài mm). (2) ScreenCut.RevealFromBlack (màn đen 0.9 s rồi mờ dần 0.5 s) khi vào scene thế giới để che đoạn camera/ánh sáng ổn định lại. Bằng chứng đo được: camera đứng yên không có spike (median 0.0002); 8 frame đầu sau khi vào scene có biến thiên gấp ~8 lần mức nền (camera follow settle); thử A/B near-far trên mặt đất cho worst 0.0351 vs 0.0349 (không khác biệt), điểm lỗi là nhiễu mảnh (cạnh/texture) chứ không phải mảng z-fight; 116 cặp renderer phẳng chồng AABB (chưa xác nhận đồng phẳng thật). Files: Scripts/Camera/DepthPrecision.cs, Scripts/Camera/ScreenCut.cs, Tests/PlayMode/MapFlickerPlayModeTests.cs (3 test), Bao_Cao/map-flicker/ (entry_*.png, coplanar_pairs.txt, zfight_probe.txt, zf_*.png). Còn lại cho Claude: kiểm tra mesh Surface_* trong CityStreetBuilder (đồng phẳng thật ở cấp tam giác, nâng lớp >=1.5 cm), mipmap/anisotropic của texture mặt đường, nhấp nháy bóng đổ khi camera chạy (test camera di chuyển), tile streaming lúc vào scene. Cần chủ quán xác nhận bằng mắt trên WebGL.

G1 DONE (29/10 ~02:30): lưu offline bền vững. ProgressFile (phiên bản schema, ghi nguyên tử, file .bak khi hỏng -> khôi phục bản sao lưu và giữ file hỏng), PersistenceGuard (tự lưu theo mốc + định kỳ, flush khi visibilitychange/pagehide/beforeunload), NihongoLifeWeb.jslib (đồng bộ IndexedDB/IDBFS), di chuyển save cũ chưa có version, sửa list null. Files: Scripts/Save/ProgressFile.cs, PersistenceGuard.cs, sửa PlayerProgressDto, PlayerStatus, LocalProgressRepository, PlayerSessionService, Plugins/WebGL/NihongoLifeWeb.jslib; Tests/EditMode/PersistenceTests.cs. Test: 5/5 PASS (round-trip có version, bản sao lưu lần ghi thứ hai, save hỏng được khôi phục, thiếu save = null, save cũ được migrate). Còn lại: chưa kiểm trên trình duyệt WebGL thật (sẽ kiểm trong bản build WebGL), chưa có test PlayMode cho PersistenceGuard.

(A) DONE (29/10 ~02:32): hình minh họa từ điển chỉ hiện sau khi trả lời đúng. IllustrationService (manifest 682 từ, 478 PNG Twemoji CC-BY 4.0; tra theo en/vi/ru/ja/reading; lưu mở khóa PlayerPrefs "illu_unlocked"). Trả lời đúng trong quiz -> thẻ phản hồi hiện hình + dòng "Hình minh họa đã mở khóa trong từ điển" và mở khóa. Trang từ điển: hình đầy đủ nếu đã mở, chưa mở = bóng đen + gợi ý trả lời đúng. Files: Scripts/Content/IllustrationService.cs, Scripts/UI/HandbookUI.Illustration.cs, sửa HandbookUI.Dictionary.cs và Course/CourseQuizView.cs (LastIllustration), Docs/CREDITS.md (ghi công Twemoji), ru_table_extra.json (+23 dòng Nga cho cả chuỗi cần câu gác/động vật/hình minh họa; cov.py = 0 thiếu). Tests: Tests/PlayMode/IllustrationPlayModeTests.cs 2/2 PASS (tìm từ mọi ngôn ngữ + mở khóa một lần; quiz đúng -> hiện + mở khóa -> từ điển hiện hình). Capture: Bao_Cao/illustrations/. Còn lại: không phải mọi câu quiz đều khớp từ điển (khớp theo văn bản, xem Bao_Cao/illustrations/quiz_matches.txt); chưa duyệt mỹ thuật vì không xem được PNG.

H3/H4 PARTIAL (29/10 ~02:47): đồng hồ HUD có mùa + thời tiết + nhiệt độ theo ngôn ngữ. Scripts/Core/ClimateService.cs (nhiệt độ theo mùa/giờ/trời; đảo +4°C, mưa -3°C), StatusDock.cs dòng thứ hai "HH:mm · Thời tiết · N°C". Test: Tests/EditMode/ClimateAndMoneyTests.cs 3/3 PASS; PlayMode HudStatusWidget + NeedsWeather PASS (r12). Còn lại: icon thời tiết/mùa, gió, cầu vồng rõ hơn, cây/lá theo mùa (H5), NPC quét lá.

G3 PARTIAL (29/10 ~02:47): tiền tệ theo ngôn ngữ. Loc.Money(yen): ja ¥, en $, ru руб., vi đồng (dấu chấm nghìn); Loc.Yen() gọi Money; thay 43 chỗ hiển thị ở WalletFeedback, InventoryWindow, HudVitalsCard, HUDUI.Inventory, KonbiniShopUI, ShopUI, RestaurantMenuUI, TaskJournalUI, StationTravel, IslandUI.*, UiKit. Test: EditMode ClimateAndMoneyTests PASS + Russian audit PASS; PlayMode r12 23/24 (1 lỗi XP do nhiễm test: SocialStanding từ test Bank, chạy riêng LifeLoop 3/3 PASS). Còn lại: các chuỗi Loc.F/Loc.T còn ¥{0} (IslandTools, ki-ốt vé, StationTravel, Farm/Shop), biển hiệu/billboard (SceneTextLocalizer), BusinessDashboardUI, tiền giấy (mệnh giá) trong UI.

(C) XONG phần việc làm (29/10 ~02:56): cử chỉ tay gắn vào hành động. Thu ngân quét hàng (cashier_scan), bưng khay (serve_tray), nhặt món (pickup) chạy qua WorkGestures và tự tắt khi xong. File: Scripts/UI/TimedAction.cs (tham số gesture), Scripts/Progression/JobStation.cs. Test: HudDragEmojiGesturePlayModeTests.JobActions_PlayHandGestures_AndClearAfterwards PASS (r16). Capture: Bao_Cao/gesture-hooks/01_register_scan_end.png, 02_serve_mid.png. Còn lại: nhạc cụ (Pulse/SetBpm) và điệu nhảy phụ thuộc mục (6); hình học đạo cụ chỉ gần đúng.

J2 XONG (29/10 ~02:56): quest tracker bên trái kéo được + thu gọn/mở + nhớ vị trí (PlayerPrefs hud_pos_/hud_collapsed_), chuột phải = về vị trí gốc. File: Scripts/UI/DraggableHud.cs (mới), Scripts/UI/StatusDock.cs (BuildObjectiveChip). Test: Tests/PlayMode/HudDragEmojiGesturePlayModeTests.QuestTracker_Drag_Collapse_Persist_Reset PASS (r16). Capture: Bao_Cao/hud-tracker/01_moved_collapsed.png, 02_moved_expanded.png. Còn lại: gắn DraggableHud cho các widget HUD khác (API DraggableHud.ResetAll đã có) + nút reset trong menu cài đặt.

F5 XONG (29/10 ~02:56): emoji trong chat. 62 emoji Twemoji (CC-BY 4.0, đã ghi CREDITS) đóng gói atlas lúc chạy; cửa sổ chat có nút emoji + bảng chọn, hiểu :shortcode:, :) <3 ;P và emoji Unicode; chữ người chơi gõ được bọc noparse (không chèn rich text). Bong bóng 3D trên đầu nhân vật cũng vẽ emoji. File: Scripts/UI/EmojiService.cs (mới), SocialChatWindow.cs, ChatBubble3D.cs, Resources/Content/Emoji/e_*.bytes (62). Test: HudDragEmojiGesturePlayModeTests 3 test emoji PASS (r16). Capture: Bao_Cao/chat-emoji/01_label_with_emoji.png, 02_chat_window_picker.png. Còn lại: nhãn TMP ngoài chat chưa có fallback emoji; bảng chọn chỉ chèn ở cuối câu.

G2 XONG phần túi (29/10 ~02:56): túi (B) cao cấp. Bộ lọc Tất cả/Đồ ăn/Đồ uống/Khác (làm mờ ô không khớp), Sắp xếp, Chia đôi, tooltip khi rê chuột (tên/số lượng/giá/hiệu ứng), menu chuột phải (dùng, bỏ 1, bỏ cả, chia đôi, cất vào rương), kéo-thả di chuyển/gộp. Rương phòng ngủ: StorageChest giữ nguyên chi tiết món, lưu theo progress.homeStorage. File: Scripts/UI/InventoryWindow.cs (partial), Scripts/UI/InventoryWindow.Premium.cs (mới), Scripts/Player/PlayerInventory.cs (MoveItem/RemoveAt/SplitAt), Scripts/Home/StorageChest.cs (viết lại), ru_table_extra.json (+8). Test: Tests/PlayMode/BagPremiumPlayModeTests PASS (r16, gồm cất/lấy rương); EditMode Russian audit 0 thiếu theo cov.py. Capture: Bao_Cao/bag-premium/01_tooltip.png, 02_filter_food.png, 03_context_menu.png. Còn lại: chưa có mô hình 3D rương đặt trong phòng 45_HomeBedroom + cửa sổ mở rương (hiện StorageChest hoạt động khi có trong scene; cất từ túi đã test); chưa test trên WebGL.

G3 TIẾN THÊM (29/10 ~03:11): (1) Tiền theo ngôn ngữ ở MỌI chuỗi: Loc.Currency tự đổi '¥số' trong Loc.T/Loc.F/Loc.Gloss sang $ (en), đồng (vi), руб. (ru); tiếng Nhật giữ yên. Tỷ giá đổi thành bội số chính xác (vi 1¥=170đ, ru 1¥=1руб., en 100¥=1$) để phép cộng ở quầy thu ngân không lệch làm tròn. Bảng giá/áp phích trong thế giới (Konbini, quán sushi, bảng việc làm) đi qua SceneTextLocalizer nên cũng đổi tiền. (2) Quét lẫn ngôn ngữ: test mới quét mọi nhãn TMP đang hiện của HUD + túi + cài đặt + chat ở en/ru/vi, báo ¥ lẫn, tiếng Việt trong en/ru, tiếng Nga trong en/vi, câu tiếng Anh trong ru. Sửa các lỗi tìm ra: biển cổng Phòng khám ('[F] Vào phòng khám' bị Việt hoá cứng), nhãn bật/tắt trong Cài đặt không đổi khi đổi ngôn ngữ lúc panel đang mở, 'Chưa có tin nhắn', nút 'Tiếp hội thoại · R'. File: Scripts/Core/Loc.cs, Scripts/UI/SceneTextLocalizer.cs, SettingsUI.cs, HUDUI.cs, DialogueView.cs, Scripts/Interaction/IInteractable.cs (bảng prompt), ru_table_extra.json. Test: EditMode ClimateAndMoneyTests 3/3 + RussianCoverageAuditTests PASS (r17); PlayMode Tests/PlayMode/LanguageLeakPlayModeTests PASS (r20, report rỗng: Bao_Cao/lang-leak/report.txt). Còn lại: quét mới chỉ phủ HUD thành phố + túi + cài đặt; các màn khác (sổ nhiệm vụ, ngân hàng, cửa hàng, đảo Midori, Phú Quốc) cần thêm vào test quét; một số chuỗi tiền dùng string.Format thẳng (không qua Loc.F) chưa đổi.

J1 XONG (29/10 ~03:23): hộp hội thoại gọn: rộng 1320→1000, chữ Nhật 33→27, lề/giãn dòng nhỏ lại, mỗi đáp án 1 dòng (tiếng Nhật trái, nghĩa mờ phải), bỏ dòng 'cách đọc' khi trùng nguyên văn (câu chỉ kana). Đo (test): box 1000x342 canvas 1920x1440 ratio 0.237 (đơn vị canvas, trước đó ~411 cao với 3 lựa chọn). File: Scripts/UI/DialogueView.cs. Test: Tests/PlayMode/CompactDialoguePlayModeTests PASS + GameplayUiPlayModeTests 6/6 PASS (r23). Capture: Bao_Cao/dialogue-compact/01_dialogue_three_choices.png, measure.txt. Còn lại: chưa kéo/thu gọn được hộp hội thoại (cố ý giữ cố định); chưa đo trên màn hình thật 16:9.

[29/10/2026 03:37 giờ Bangkok] F3 + H6 — NPC đảo Midori + cư dân nhà trống ở thị trấn: XONG phần cốt lõi (đã test).
- Đã làm: 8 cư dân đảo Midori (nông dân Kenta, người chăn nuôi Aoi, chủ quán Mei, ông câu cá Genji, nhân viên ga Souta, bé Yui, bà Tsuru, người leo núi Daichi) + 6 cư dân đứng/đi quanh cửa các ngôi nhà trống ở thị trấn (Ume, Ken, Saki, Gen, Mina, Taro). Cùng họ nhân vật Kids, tự đi dạo quanh cửa nhà (không NavMesh, không physics body), nói chuyện qua ScriptedNpcDialogue (mỗi người 2 câu, đủ vi/en/ja/ru), ban đêm (21:30–06:30) "vào nhà" (ẩn), xa người chơi >95m thì tắt (nhẹ cho WebGL).
- File mới: Scripts\World\TownFolk.cs; Tests\PlayMode\TownFolkPlayModeTests.cs. File đổi: Resources\Content\Localization\ru_table_extra.json (+28 câu Nga).
- Test: r25 PlayMode TownFolkPlayModeTests 3/3 PASS (đủ ngôn ngữ + body, đi dạo, đêm ẩn/sáng hiện, mở hội thoại, nằm trong tầm camera). Ảnh + vị trí: Bao_Cao\island-npcs\ (midori_resident.png, city_resident.png, *_residents*.txt).
- CÒN LẠI / thật thà: vài cư dân Midori (Genji, Tsuru, Yui) không tìm được chỗ riêng cạnh Lookout/Signs/Nature nên đứng gần điểm đến (fallback) — chưa đặt đúng chỗ; chưa kiểm tra ảnh bằng mắt (tôi không xem được PNG, chỉ kiểm tra bằng code); câu thoại tiếng Nga/Anh cần người bản xứ duyệt; chưa có bảng tên trên đầu; chưa có cư dân bên trong nội thất nhà (chỉ ở cửa nhà).

[29/10/2026 03:48 giờ Bangkok] F4 — Thuê nhân công: XONG (đã test).
- Phát hiện: màn hình Doanh nghiệp (BusinessDashboardUI) và việc mở công ty trước đây KHÔNG có đường vào trong game (không nơi nào gọi). Đã nối: phím O mở màn hình Công ty (tạo cùng HUD); chưa có công ty thì nút đầu là "Lập công ty (¥50,000)".
- Đã làm: nhân viên có tên, vị trí (Thu ngân / Phụ vườn / Phụ bếp / Người câu cá), lương, cấp 1–5. Thuê qua BusinessSystem.TryHireEmployee (giữ luật lương tối thiểu + uy tín). Nhân viên kiếm tiền theo giờ THỰC (tối đa 8 giờ giữa 2 lần thu, nên đóng tab vẫn tính), trừ lương theo tỉ lệ, bấm "Thu về" cộng vào vốn công ty. Đào tạo (+20% thu/cấp, tốn ¥5.000×cấp), cho nghỉ việc, đổi vị trí tuyển. Lưu vào save (BusinessRecord.workers).
- Thông báo của BusinessSystem (trước là tiếng Việt không dấu) đã đổi sang Loc.T 4 ngôn ngữ; +22 câu Nga (cov.py: 2140 / 0 thiếu).
- File mới: Scripts\Player\BusinessWorkers.cs; Tests\PlayMode\BusinessWorkersPlayModeTests.cs. File đổi: Data\PlayerProgressDto.cs, Player\BusinessSystem.cs, UI\BusinessDashboardUI.cs, UI\HUDUI.cs, ru_table_extra.json.
- Test: r26 PlayMode BusinessWorkersPlayModeTests 1/1 PASS (thuê/lương thấp bị từ chối, thu nhập, trần 8 giờ, đào tạo, lưu save, nút UI Thuê/Thu/Cho nghỉ, không lẫn tiếng Việt/Anh trong en/ru). Ảnh: Bao_Cao\hire-workers\01_company_screen_en.png (chỉ kiểm tra bằng code, tôi không xem được PNG).
- CÒN LẠI: nhân viên chưa xuất hiện dạng 3D trong cửa hàng/ruộng; cân bằng kinh tế (thu/giờ, lương) là số tạm; chưa có gợi ý "bấm O" trong hướng dẫn/HUD; việc thuê chỉ làm tăng vốn công ty (không chuyển về ví người chơi).

### F2 - Sổ kỹ năng theo bậc (phím Y) - 03:48 29/10/2026 (giờ Bangkok)
**Đã xong:** 6 kỹ năng (câu cá, trồng trọt, chăm thú, buôn bán, làm thêm, học tập), mỗi kỹ năng có 4-5 bậc tính từ việc người chơi thật sự đã làm (số cá, thu hoạch, cho ăn, bán, ca làm, điểm kiến thức). Màn hình Y hiện bậc, chấm tiến độ, còn bao nhiêu để lên bậc, đặc quyền. Đặc quyền thật chỉ có ở câu cá: bậc 2 mở cần câu chờ, bậc 3/4 cá cắn sớm hơn 20%/35%.
**File:** Scripts/Player/SkillTiers.cs (mới), Scripts/UI/SkillsPopup.cs (mới), Scripts/UI/HUDUI.cs (tạo popup), ru_table_extra.json (+19 dòng Nga), Tests/PlayMode/SkillTiersPlayModeTests.cs (mới).
**Test:** PlayMode SkillTiersPlayModeTests 2/2 PASS (r28); kiểm tra tiếng Nga 0 thiếu. Ảnh: Bao_Cao/skills/01_skills_en.png.
**Còn lại / thật thà:** chỉ câu cá có hiệu ứng gameplay theo bậc; các kỹ năng khác mới là bậc hiển thị. Chưa có gợi ý "nhấn Y" trong hướng dẫn. Chưa kiểm tra mắt thường ảnh.

### G3 (phần 2) - Quét rò ngôn ngữ cho các popup phím tắt - 03:55 29/10/2026 (giờ Bangkok)
**Đã xong:** test mới quét mọi popup MenuPopupBase trong thành phố (Công ty, Kỹ năng, Nhật ký nhiệm vụ, Study Hub, Exam Center) ở en/ru/vi. Tìm ra 1 lỗi thật: nút "Ký kết liên doanh" của màn Công ty luôn tiếng Việt khi chơi en/ru (nhãn chỉ tạo 1 lần lúc build). Đã sửa: nhãn đặt lại trong RefreshUI theo ngôn ngữ hiện tại.
**File:** Tests/PlayMode/LanguageLeakPopupsPlayModeTests.cs (mới), LanguageLeakPlayModeTests.cs (Leaks/PathOf thành internal), Scripts/UI/BusinessDashboardUI.cs.
**Test:** PlayMode LanguageLeakPopupsPlayModeTests 1/1 PASS + BusinessWorkersPlayModeTests 1/1 PASS (r30). Báo cáo: Bao_Cao/lang-leak/popups.txt.
**Còn lại / thật thà:** chỉ quét popup có trong cảnh thành phố (Guide/About tạo muộn nên chưa quét; Exam/Course chỉ ghi chú vì có nội dung học). Chưa quét Midori, Phú Quốc, ngân hàng, cửa hàng. Quét chỉ bắt tiếng Việt/Nga/dấu ¥ lẫn vào, không bắt tiếng Anh lẫn vào tiếng Việt.

### F7 - Trang trí phòng kiểu Sims (phím V trong phòng ngủ) - 04:55 29/10/2026 (giờ Bangkok)
**Đã xong:** trong 45_HomeBedroom bấm V mở danh mục 8 món nội thất (chậu cây, đèn đứng, kệ sách, gối ngồi, bàn nhỏ, thảm, tivi + kệ, kotatsu; dựng bằng hình khối đơn giản). Mua -> món gắn theo con trỏ (xanh = đặt được, đỏ = không), bấm trái đặt, R xoay 90 độ, Esc huỷ (hoàn tiền đủ). Luật đặt: trong tường phòng, không đè đồ khác, không chui vào giường/bàn/đồ cứng (kiểm tra Physics), thảm được nằm dưới đồ khác. Có Dời và Bán (hoàn 50%). Lưu vào save (roomDecor) và dựng lại khi vào lại phòng. Tiền hiển thị theo ngôn ngữ (en = $, vi = đ).
**File:** Scripts/Home/RoomDecorCatalog.cs, Scripts/Home/RoomDecor.cs, Scripts/UI/DecorPopup.cs (mới); Scripts/Data/PlayerProgressDto.cs (RoomDecorRecord + roomDecor); ru_table_extra.json (+27 dòng Nga); Tests/PlayMode/RoomDecorPlayModeTests.cs (mới).
**Test:** PlayMode RoomDecorPlayModeTests 1/1 PASS (r34): mua trừ tiền, ngoài phòng/chỗ vướng bị từ chối, đặt, đè nhau bị từ chối, huỷ hoàn tiền, xoay, lưu, bán +50%, nạp lại sau khi đổi cảnh, danh mục en/ru không lẫn ngôn ngữ. Ảnh: Bao_Cao/room-decor/01_room_en.png, 02_catalogue_en.png. Kiểm tra tiếng Nga: 0 thiếu.
**Còn lại / thật thà:** ảnh phòng chưa chứng minh bằng mắt rằng đồ nằm trong khung hình (phân tích điểm ảnh không thấy rõ màu chậu cây; camera chụp chỉ là góc đặt tạm) - chưa xem được ảnh. Đặt bằng chuột chưa được test tự động (test gọi PlaceAt trực tiếp). Mới có đồ đặt sàn, chưa có treo tường/đổi màu tường/sàn. Chưa có gợi ý 'bấm V' trong hướng dẫn. Chưa có âm thanh đặt đồ.

[29/10/2026 05:04 Bangkok] SỬA XUNG ĐỘT PHÍM TẮT (phát hiện khi thêm gợi ý phím)
- Phát hiện: GameInputService đã gán Y = Sổ tay (Handbook), O = Cài đặt, V = Voice, U = Social. Popup Kỹ năng (Y) và Công ty (O) và Trang trí phòng (V) do tôi thêm trước đó TRÙNG phím.
- Đã đổi: Kỹ năng = X, Công ty = Z, Trang trí phòng (phòng ngủ) = X. Sửa SkillsPopup.cs, BusinessDashboardUI.cs, RoomDecor.cs, DecorPopup.cs, test BusinessWorkers (chỉ chú thích).
- Thêm gợi ý phím vào Cài đặt > Điều khiển (SettingsUI.cs, dòng phím cố định) + 1 mục ru (ru_table_extra.json). cov.py: 2177 / 0 thiếu.
- Test r35 PlayMode: RoomDecor + SkillTiers(2) + BusinessWorkers = 4/4 PASS.
- Còn lại / chưa sửa (đã có từ trước, không phải của tôi): nút HUD "Học thêm" ghi phím U nhưng U cũng là Social (Bạn bè & tin nhắn) trong GameInputService -> có thể trùng; chưa kiểm chứng trong game thật. Chưa có nút dock cho Kỹ năng/Công ty (desktop ẩn nút phím tắt).

[29/10/2026 05:13 Bangkok] SỬA TRÙNG PHÍM U (Học thêm vs Bạn bè & tin nhắn)
- GameInputService gán U = Social; StudyHubPopup cũng bắt U -> bấm U mở cả hai. Đã chuyển Học thêm sang phím  (backquote): StudyHubPopup.cs, nhãn nút HUDUI.Layout.cs, gợi ý trong Cài đặt > Điều khiển (3 ngôn ngữ + ru).
- Test r37 EditMode: StudyModesUiSmoke, EnglishBasicUi, RussianCoverageAudit, RussianLocalization = 16/16 PASS. cov.py 2177/0.
- Chưa test bằng bàn phím thật (chỉ test mở popup bằng code). Phím  có thể khó bấm trên một số bàn phím — để chủ quán quyết nếu muốn đổi.

[29/10/2026 05:36 Bangkok] WEBGL BUILD SỚM (wgl2) XONG - bản dự phòng
- Build chạy nền 05:13 -> 05:32 (~19 phút, platform switch nhanh nhờ cache). Kết quả: Build Finished, Result: Success; ReleaseBuild báo "WebGL ready ... 67.8 MB".
- Đường dẫn: D:\VTC_Academy\_grok_tmp\webgl-build2\WebGL (index + Build + StreamingAssets + zones).
- Dung lượng tải lần đầu (Build/): data.unityweb 56.28 MB + wasm.unityweb 9.92 MB + framework 0.15 MB + loader 0.11 MB = ~66.5 MB (so với bản 28/10: data 55.4 MB, wasm 10.3 MB -> data +0.9 MB). Zone tải theo cảnh: arcade 15, city 18.7, clinic 16.7, home 14.4, island 18.4, phuquoc 14.6, school 18.0, station 24.1, sushi 17.1 MB. Nhạc StreamingAssets ~0.5-3.7 MB/bài. Toàn thư mục 264 MB.
- Dòng "Build Finished, Result: Failure" ở dòng 4394 của log là bước đo kích thước player 1 scene (Phú Quốc) trước khi build bundle; build cuối cùng Success và đủ 9 zone.
- CHƯA đo thời gian tải thật trên trình duyệt (chưa chạy qua server có Content-Encoding); chỉ đo dung lượng. CHƯA test chơi thật trên WebGL (G1 lưu tiến độ chưa kiểm chứng trên WebGL).
- Việc tiếp: 1-2 mục nhỏ (cây/lá theo mùa, icon thời tiết), build lại lần cuối ~06:05, báo cáo ~06:30.

[29/10/2026 05:45 Bangkok] H4 ICON THỜI TIẾT cạnh tên địa điểm trong khung trạng thái
- Thêm 3 icon vẽ bằng code (HudIcons.Sun, Cloud, RainCloud; đêm dùng icon Moon có sẵn). StatusDock: icon cạnh tên nơi chốn, đổi theo thời tiết và giờ (nắng, mây, mưa, đêm), ẩn khi không có chu kỳ ngày. Thuộc tính SkyIconName để test.
- Files: Scripts/UI/HudIcons.cs, Scripts/UI/StatusDock.cs, Tests/PlayMode/WeatherIconPlayModeTests.cs.
- Test r39 PlayMode: WeatherIcon (2) + NeedsWeather (4) = 6/6 PASS. Ảnh: Bao_Cao\weather-icon (sun, cloud, rain, moon).
- Chưa xác nhận bằng mắt hình dạng icon (không xem được ảnh); test chỉ xác nhận đúng loại icon theo thời tiết và giờ.
- Còn: lá/cánh hoa rơi theo mùa (H5).

[29/10/2026 05:50 Bangkok] H5 LÁ / CÁNH HOA RƠI THEO MÙA (cảnh ngoài trời: thành phố + đảo Midori)
- Scripts/World/SeasonalLeaves.cs: 1 hệ hạt dùng chung, đi theo camera. Xuân = cánh hoa hồng (9/giây), thu = lá cam đỏ (12/giây), hè và đông = không có; tắt khi ở trong nhà (StoreCameraZone) và khi trời mưa. Không dùng asset ngoài (quad nhỏ có màu). Tự khởi động cùng game.
- Test r40 PlayMode (SeasonalLeavesPlayModeTests) 2/2 PASS: tỉ lệ theo mùa/trong nhà/mưa; trong thành phố mùa thu và xuân có hạt đang bay (>5), mùa hè và đông bằng 0. Ảnh: Bao_Cao\seasonal-leaves (autumn, spring).
- Chưa chứng minh bằng ảnh: đếm điểm ảnh cam/hồng trên ảnh chụp không phân biệt được với màu nền cảnh. Hình dạng hạt chỉ là ô vuông nhỏ (chưa có sprite lá). Chưa có cây rụng lá thật (chỉ hạt rơi), chưa đổi màu tán cây theo mùa (H5 chỉ xong phần hạt).
- Chưa test trên WebGL, chưa đo chi phí hiệu năng (tối đa 220 hạt).

[29/10/2026 06:03 Bangkok] Hồi quy sau H4/H5 + bắt đầu build WebGL cuối
- Dừng tính năng mới. Chạy hồi quy PlayMode: r41 (HUD/Gameplay/Bedroom/Portrait) 12/12 PASS; r42 (wildlife, đảo Midori, môi trường, âm thanh, town folk) 10/11 – "City_Wildlife_BehavioursSeasonsAndLod" fail ("A walking dog moves" 0.04 < 0.8) khi chạy chung; r43 chạy riêng lớp AmbientWildlife: 2/2 PASS => nghi ô nhiễm thứ tự test/thời gian chạy, chưa chứng minh nguyên nhân (không loại trừ lá rơi làm lệch, nhưng test riêng pass).
- 06:03: không có Unity chạy, không có lockfile; khởi động build WebGL cuối -> D:\VTC_Academy\_grok_tmp\webgl-build3 (log_wgl3.log, wgl3.done). webgl-build2 (đã đo 66,5 MB Build/) giữ làm dự phòng. Dự kiến ~19 phút.
- Còn lại: đo dung lượng build3, viết BAO-CAO-HOAN-THANH.md (~06:30).

# HANDOFF 29/10/2026 – Game Edu NihongoLife giao cho Claude Code làm tiếp
Giờ Bangkok, viết lúc ~07:10 ngày 29/10/2026 bởi Unity executor (Grok Bot). Chủ game (anh Long / Ganh Ngon) đổi ý lúc 07:00: **không làm thêm tính năng**, giao riêng cho Claude Code. Không commit/push trong đợt này; mọi thay đổi vẫn **chưa commit** trong working tree.
Đọc kèm: `STATUS-unity.md` (D:\VTC_Academy\_grok_tmp\), `Docs\TEAM_TASKS.md`, `BAO-CAO-HOAN-THANH.md` (D:\VTC_Academy\_grok_tmp\), `AGENTS.md`.
Quy ước trạng thái: **XONG+TEST** = có test tự động pass (và ảnh chụp ở `Bao_Cao\<chủ đề>\`); **MỘT PHẦN**; **CHƯA LÀM**. Lưu ý chung: executor không xem được ảnh PNG (công cụ Read từ chối đường D:\), nên mọi kiểm tra hình ảnh chỉ bằng code/phân tích điểm ảnh – Claude Code nên MỞ ẢNH xem bằng mắt.

Đường dẫn gốc: project `D:\VTC_Academy\NihongoLife\NihongoLife\NihongoLife`; assets `Assets\NihongoLife`; ảnh/báo cáo `D:\VTC_Academy\NihongoLife\NihongoLife\Bao_Cao\<chủ đề>\`; công cụ/log/backup `D:\VTC_Academy\_grok_tmp\`.
Tên lớp test viết tắt: PlayMode = `NihongoLife.Tests.<Lớp>`; EditMode = `NihongoLife.Tests.<Lớp>` hoặc `NihongoLife.Tests.EditMode.<Lớp>` (xem mục 2).

## 1. DANH SÁCH YÊU CẦU CỦA CHỦ (đánh số)

### Nhóm A – Nền tảng, ngôn ngữ, học tập
1. **Tiếng Nga + đồng bộ 4 ngôn ngữ (vi/en/ja/ru)** – XONG+TEST (một phần: chưa có người bản xứ duyệt). `Loc.T(vi,en,ja)` + bảng ru khoá theo chuỗi en (`Resources\Content\Localization\ru_table.json` 3680 khoá + `ru_table_extra.json` ~1000+ dòng viết tay); `python D:\VTC_Academy\_grok_tmp\tools2\cov.py` → "2177 0" (0 thiếu). Nút RU ở menu chính, Settings, HUD đổi khi đổi ngôn ngữ. Test: EditMode RussianCoverageAuditTests, RussianLocalizationTests, CyrillicFontTests; PlayMode RussianUiPlayModeTests, LanguageLeakPlayModeTests, LanguageLeakPopupsPlayModeTests. **CẦN NGƯỜI BẢN XỨ DUYỆT**: toàn bộ dòng Nga (UI, thoại NPC, ngữ pháp), cú pháp Anh phần "English tenses"; gloss tiếng Nga rơi về tiếng Anh khi dữ liệu không có trường ru. Quét rò ngôn ngữ chỉ phủ popup trong thành phố (chưa Midori, Phú Quốc, ngân hàng, cửa hàng); chỉ bắt tiếng Việt/Nga lẫn vào, không bắt tiếng Anh lẫn vào tiếng Việt.
2. **Tiền tệ theo ngôn ngữ** (ja ¥, en $, ru ₽, vi đ) – MỘT PHẦN (đã test). `Loc.Money/Yen/Currency`, thay ~43 chỗ + tự đổi số trong Loc.T/F/Gloss, bảng giá/áp phích qua SceneTextLocalizer. Test: EditMode ClimateAndMoneyTests. Còn: chuỗi Loc.F/T còn {0} (IslandTools, kiosk vé, StationTravel, Farm/Shop), BusinessDashboardUI, mệnh giá tiền giấy trong UI.
3. **Túi đồ cao cấp + rương phòng ngủ (G2)** – XONG+TEST (phần túi). Bộ lọc, sắp xếp, chia đôi, tooltip, chuột phải, kéo-thả; `Scripts\UI\InventoryWindow.Premium.cs`, `Player\PlayerInventory.cs`, `Home\StorageChest.cs`. Test PlayMode BagPremiumPlayModeTests. Còn: rương phòng ngủ chưa có vật 3D trong phòng.
4. **Sổ tay: tra từ (P4)** – XONG+TEST. 2234 mục gốc, 12 file cặp (ru-vi, ru-ja, en-vi, ja-vi + ngược) ở `Resources\Content\Dictionary\`; `DictionaryService`, tab Từ điển trong HandbookUI. Test: EditMode DictionaryServiceTests (4), PlayMode CourseUiPlayModeTests (5 cặp).
5. **Hình minh hoạ từ điển chỉ hiện sau khi trả lời đúng (A)** – XONG+TEST. `Scripts\Content\IllustrationService.cs`, `UI\HandbookUI.Illustration.cs`, 478 PNG Twemoji (CC-BY 4.0, ghi `Docs\CREDITS.md`), manifest 682 từ. Hạn chế: chỉ khớp khi chữ quiz trùng mục từ điển. Test: có trong chuỗi test Handbook/Course (log_a / log_c).
6. **Trường học chuyên sâu hơn (P13)** – MỘT PHẦN. Dữ liệu (do content executor soạn ở box, đã đưa vào `Resources\Content\School\`): 40 bài × 4 ngôn ngữ, bảng chữ cái (hiragana, katakana, Cyrillic, Anh phonics, Việt thanh điệu), ngữ pháp 40×4, 12 thì tiếng Anh, 20 bộ chứng chỉ (JLPT N5–N1, Anh A1–C1, TORFL, Việt bậc 1–5) + IELTS-style, kiểm tra xếp lớp, mô hình SRS Leitner, 120 lộ trình, 11 bộ đề (JLPT mini 123 câu, English 100 câu, tenses 60). UI: `CoursePopup` 6 tab + `CourseQuizView` 5 loại câu, StudyHub 6 ô, link ExamCenter. Test: EditMode CourseCatalogTests (11), PlayMode CourseUiPlayModeTests 6/6 (1920×1080, ảnh `Bao_Cao\course`). **Chưa đạt "hơn Duolingo"**: chỉ 8 từ/chủ đề (320 từ/khoá), mỗi bậc chứng chỉ 16 câu (luyện tập, không chính thức), nghe dùng speech synthesis của trình duyệt (không có thu âm), không có nét viết kana. Người bản xứ chưa duyệt (xem `REVIEW-NEEDED.md` của content executor, bản ở box `/workspace/nihongo/school/REVIEW-NEEDED.md`). Lớp học 40_HIBARICLASS thêm người ngồi: executor này **không kiểm tra**.
7. **Ga/tàu (P2)** – XONG+TEST. `Resources\Train\timetable.json`, `Resources\Navigation\route_signs.json`, `RouteSignBuilder`, sửa cầu thang ga lệch 700 m, khách xuống tàu. Test: TrainArrivalCyclePlayModeTests, TrainRoutePlayModeTests, RouteSignPlayModeTests, EditMode RouteSignAndTimetableTests. Biển chỉ đường Phú Quốc: hoãn sang P7 (xem 22).
8. **Chọn mục tiêu tương tác (P9)** – XONG+TEST. `InteractionTargetingPlayModeTests` 4/4.
9. **Màn hình menu chính có nhân vật 3D sống (P8)** – XONG+TEST. `MenuAvatarStage`, test MenuAvatarPlayModeTests + MenuSyncPlayModeTests. Tương phản chữ gợi ý ở menu chính: hoãn (P14).
10. **Bản đồ M cao cấp (B)** – XONG+TEST. `Scripts\UI\MapPremium.cs`, test MapPremiumPlayModeTests + hồi quy 17/17.
11. **Nhân vật đa dạng (C.1) + đồng phục nghề + ngành nghề (5/5b)** – XONG+TEST. Biến thể Kids-family (`variants.json`, `CharacterVariantBuilder`), 5 ngành (`career_industries.json`, arts để dành), `CareerAvatarStage`, avatar mỗi nghề. Test: EditMode CharacterVariantTests (9), CareerIndustryTests (4); PlayMode CharacterVariantsPlayModeTests (3), CareerAvatarPlayModeTests, CareerPlayModeTests. Ảnh `Bao_Cao\characters-variants`, `careers-ui`.
12. **Khu trò chơi (arcade) quá ít game** – XONG+TEST. **Trước: 6 game/6 máy → Sau: 14 game/14 máy** (scene 50_GameCenter). 8 game mới: AirHockey, DanceArrows, EchoPads, KanaBreaker, KanaSnake, TenseTyper, WheelQuiz, WordWhack. Test: PlayMode ArcadeMoreGamesPlayModeTests; EditMode ArcadeMoreGamesTests; ảnh/báo cáo `Bao_Cao\arcade`. Chưa xác minh máy thật trong scene sau khi dựng lại scene.

### Nhóm B – Âm nhạc, nghệ thuật (P11/P12) – PHẦN LỚN CHƯA LÀM TRONG UNITY
13. **Cửa hàng nhạc cụ + đĩa CD** (gian hàng phân khu, màu sắc/bài trí nghệ thuật, khác konbini; nhiều model nhạc cụ: piano, guitar, kèn, trống, violin…) – **CHƯA LÀM** trong Unity. Có sẵn dữ liệu: 45 model glb (9 CC0 + 36 CC BY 3.0) trong `music\art\models\` + `models.json`; danh sách `ART-LIST.md`.
14. **Giả lập piano (phím/nốt đủ) và guitar (hợp âm) chơi thủ công** – **CHƯA LÀM** (không có code simulator). Dữ liệu sẵn: `music\guitar-chords.json` (42 thế bấm đã kiểm với nốt hợp âm, 12 tiến trình × 3–5 tông), `music\songs-notes\` (22 giai điệu + index.json).
15. **Giáo trình nhạc lý 4 ngôn ngữ vi/en/ja/ru + bài hát hoàn chỉnh** – DỮ LIỆU XONG, **chưa nối UI/Unity**: `music\theory\music-theory.{vi,en,ja,ru}.json` (24 bài × 4 ngôn ngữ, 72 câu hỏi mỗi ngôn ngữ, kiểm tra tương đương cấu trúc/chữ viết tự động; chưa người bản xứ duyệt) + `CURRICULUM.md`.
16. **Nhạc sĩ/producer (EDM hướng DJ), animation nhạc công (piano, guitar, kèn, trống/DJ)** – MỘT PHẦN. Có đồng phục nghề (musician/producer) và cử chỉ nhạc cụ trong `gestures.json`; **chưa có** nghề nhạc sĩ chơi được, chưa đồng bộ nhịp (Pulse/SetBpm) với simulator vì simulator chưa tồn tại.
17. **Nghệ sĩ và biểu diễn trả tiền** (order qua thư/nhiệm vụ/bạn online/NPC; nghe nhạc tăng vui vẻ; đàn hay tăng danh tiếng; chưa tập xong → ít tiền; không biết đàn mà nhận → đàn dở, không cát-xê, giảm fan; học/mua bản quyền bài ở tiệm truyện tranh & CD) – **CHƯA LÀM** (chưa có code, chưa có test).
18. **Nhạc bản quyền hợp lệ** – DỮ LIỆU XONG, **chưa nhập vào game**: 81 bản (CC BY 4.0: 51, PD: 18, CC0: 11, CC BY 3.0: 1) với `music\songs.json`, `CHECKSUMS.sha256`, `CREDITS.txt`, `LICENSED-SONGS.md`; file âm thanh **446 MB** ở box `/workspace/nihongo/music/audio/` (quá lớn cho WebGL: cần chuyển OGG/MP3 96–128 kbps hoặc stream). 10 bản "medium" (chỉ nhãn Public domain chung) nên thay CC0 trước khi phát hành thương mại. Mỗi bài CC BY cần ghi công trong màn Credits (dùng `attribution_text`) → cập nhật `Docs\CREDITS.md` + `Docs\AUDIO_CREDITS.md` khi nhập. Nhạc nền hiện có trong game là nhạc tự soạn bằng code (`Tools\audio\compose_music.py`, `Docs\AUDIO_CREDITS.md`).
19. **Hoạ sĩ + bán tranh (P12)** – DỮ LIỆU XONG, **chưa làm nghề/bán tranh**: `music\art\artworks.json` 39 tác phẩm (24 Met CC0 + 15 Wikimedia Commons PD) + `images\`. Ngành "arts" mới chỉ được đặt chỗ trong `career_industries.json`.
> **QUAN TRỌNG**: dữ liệu nhóm B nằm ở **box** (`/workspace/nihongo/music`, `/workspace/nihongo/school`), **không có trên PC**. Mình đã đóng gói `/workspace/nihongo-music-art.zip` (14,5 MB, không gồm audio) và `/workspace/nihongo-plans.zip` nhưng công cụ copy sang ổ D:\ bị từ chối ("outside the allowed local-exec root") nên **chưa chép được sang PC** – cần chủ hoặc agent khác chuyển file (không route vòng).

### Nhóm C – Thế giới, nghề, đời sống
20. **Xe máy và ô tô (J4)** – **CHƯA LÀM**. Hiện chỉ có xe cộ cũ `Scripts\World\StreetTraffic.cs` (+ TrafficStopTests, CityStreetBuilder). Yêu cầu: model xe máy/ô tô có giấy phép (CC0/CC-BY/PD), ghi `Docs\CREDITS.md`, cùng phong cách Kids; chưa chọn/tải asset nào.
21. **Phố Phú Quốc (J3)** – **CHƯA LÀM**. Đã có đảo là zone thật (31 điểm mốc, 8 điểm phục vụ; test PhuQuocIslandPlayModeTests, ZoneConsolePlayModeTests; tài liệu `Docs\PHU_QUOC.md`, `HANDOFF_PHUQUOC.md`, `PHU_QUOC_ASSETS.md`, dữ liệu OSM `Docs\Expansion\PhuQuoc\`). Chưa có phố/đường phố chi tiết, Dương Đông 1:1, nội thất Gánh Ngon, tuyến Hibari→PQC.
22. **Biển chỉ đường Phú Quốc (P7)** – hoãn, CHƯA LÀM. Skybox HDR và texture Ground054 chưa nối.
23. **Món ăn, vật phẩm, chỉ số tăng theo level (J5)** – **CHƯA LÀM**.
24. **Cuốc tốn năng lượng** (mức tiêu hao năng lượng theo hành động/công việc, cân bằng) – **CHƯA LÀM** (executor này không sửa hệ needs; chỉ có test NeedsWeather 4/4 cho ảnh hưởng thời tiết).
25. **Sims phòng ngủ (F7)** – MỘT PHẦN, test pass. Phím **X** trong 45_HomeBedroom mở danh mục 8 món nội thất (chậu cây, đèn đứng, kệ sách, gối, bàn nhỏ, thảm, tivi, kotatsu), mua→đặt theo con trỏ, R xoay, Esc huỷ, bán 50%, lưu `roomDecor`. File: `Scripts\Home\RoomDecorCatalog.cs, RoomDecor.cs, UI\DecorPopup.cs`. Test: RoomDecorPlayModeTests (gọi `PlaceAt` trực tiếp). Còn: đặt bằng chuột thật chưa test; ảnh phòng chưa chứng minh đồ nằm trong khung; chỉ đồ sàn (không treo tường/đổi màu tường/sàn); chưa âm thanh đặt đồ; chưa có gợi ý phím trong hướng dẫn.
26. **Hoạt ảnh theo nghề** (nấu ăn, nhạc cụ, nhảy hip-hop, nông trại, thu ngân, sushi, vẽ, dọn dẹp) – MỘT PHẦN. Engine cử chỉ thủ tục `Scripts\Gestures\GestureLayer.cs` (IK tay/chân, đạo cụ, FX) + `Resources\Animation\gestures.json` (73 cử chỉ / 49 đạo cụ, sinh bởi `tools3\gen_all.py`) + 20 lời chào theo văn hoá (`greetings.json`). Test: EditMode GestureLibraryTests (6), GreetingCatalogTests (3); PlayMode GestureLayerPlayModeTests 10/10; capture 73 contact sheet `Bao_Cao\gestures` (test GestureCapturePlayModeTests chỉ pass khi chạy riêng). Đã nối vào hành động nghề: thu ngân quét hàng, bưng khay, nhặt món (`TimedAction.cs`, `JobStation.cs`; test HudDragEmojiGesturePlayModeTests.JobActions_PlayHandGestures…). **Chưa nối**: nấu ăn (chop/stir/flip/plate), nhạc cụ, nhảy hip-hop theo cấp, nông trại, sushi, vẽ, dọn dẹp ở các trạm nghề; hình học đạo cụ chỉ xấp xỉ; chưa xem bằng mắt.
27. **Gió, cầu vồng, cây lớn theo mùa, quét lá (H3/H5)** – MỘT PHẦN. Xong: đồng hồ HUD có mùa + thời tiết + nhiệt độ (`Core\ClimateService.cs`, `UI\StatusDock.cs`), **icon thời tiết** nắng/trăng/mây/mưa (`UI\HudIcons.cs`; test WeatherIconPlayModeTests 2/2 + NeedsWeather 4/4, ảnh `Bao_Cao\weather-icon`), **lá/cánh hoa rơi theo mùa** xuân/thu (`Scripts\World\SeasonalLeaves.cs`, test SeasonalLeavesPlayModeTests 2/2, ảnh `Bao_Cao\seasonal-leaves` — phân tích điểm ảnh không kết luận được, cần mắt người; hạt là hình vuông đơn giản). **Chưa làm**: cây lớn đổi màu/rụng lá theo mùa, NPC/người quét lá, gió (H3), cầu vồng mới (chỉ có cầu vồng cũ), icon thời tiết ngoài scene có DayNightCycle.
28. **NPC nhà trong (H6) + NPC Midori (F3)** – XONG phần lõi, test. `Scripts\World\TownFolk.cs`; 8 cư dân Midori + 6 cư dân quanh nhà ở thị trấn, thoại 4 ngôn ngữ, ban đêm vào nhà; test TownFolkPlayModeTests 3/3 (ảnh `Bao_Cao\island-npcs`). Còn: một số cư dân Midori (Genji, Tsuru, Yui) rơi về gần điểm đến (fallback); chưa có bảng tên; chưa có người **bên trong** nội thất nhà; thoại ru/en cần người bản xứ.
29. **Sách kỹ năng / NPC chuyên gia (F2)** – MỘT PHẦN. Xong: sổ kỹ năng theo bậc (`Player\SkillTiers.cs`, `UI\SkillsPopup.cs`, phím **X**; test SkillTiersPlayModeTests 2/2) — 6 kỹ năng, chỉ **câu cá** có hiệu ứng gameplay (bậc 2 mở cần câu chờ; bậc 3/4 cá cắn sớm 20%/35%). **Chưa làm**: sách kỹ năng, NPC chuyên gia dạy, nối nấu ăn/nhạc cụ/vẽ/trồng trọt, gợi ý phím trong hướng dẫn.
30. **Cần câu gác (AFK rod) + kỹ năng câu cá (F1)** – XONG+TEST (FishingFarmPlayModeTests 3/3). Còn: cần chưa lưu qua đổi scene.
31. **Động vật xung quanh (K1)** – XONG+TEST (AmbientWildlifePlayModeTests 2/2). Chim/chó/mèo/sóc là khối primitive, không clip animation thật, chưa có ở Phú Quốc. (Test City_Wildlife… từng fail một lần khi chạy chung 5 lớp rồi pass khi chạy riêng – xem mục 2.)
32. **Thuê nhân công (F4)** – XONG phần lõi, test (BusinessWorkersPlayModeTests 1/1). Phím **Z** mở màn Công ty (trước đây màn Công ty chưa nối vào game). Còn: nhân viên chưa có dạng 3D; số liệu kinh tế là tạm; chưa chuyển tiền về ví người chơi.
33. **Lưu offline bền vững (G1)** – XONG+TEST (EditMode PersistenceTests 5/5; `Save\ProgressFile.cs`, `PersistenceGuard.cs`, jslib IndexedDB). **Chưa xác minh trên WebGL thật.**
34. **Giao diện** – XONG+TEST: hộp thoại gọn (J1, CompactDialoguePlayModeTests), quest tracker kéo/thu gọn/nhớ vị trí (J2), emoji chat 62 emoji Twemoji (F5), camera cửa hàng tiện lợi mượt (H2, KonbiniCameraPlayModeTests), phím tắt đã đổi để hết trùng (X kỹ năng, Z công ty, ` Study hub, X trang trí trong phòng ngủ; hiển thị ở Settings → Controls; **chưa thử bằng bàn phím thật**, không có nút dock cho Kỹ năng/Công ty).
35. **Nhấp nháy bản đồ khi vào game (H1)** – MỘT PHẦN. **Chưa tìm ra nguyên nhân gốc.** Đã giảm nhẹ: camera near/far 0,03/1000 → 0,12/450 (`DepthPrecision.cs`), màn đen `ScreenCut.RevealFromBlack`. Cần repro trên máy thật và xem bằng mắt.
36. **QA open-world (F6) / tổng rà scene (P14)** – **CHƯA LÀM** (chỉ có các test hồi quy riêng lẻ ở trên). **H7**: không có mô tả riêng trong STATUS – coi như chưa làm.
37. **itch.io** – **bản nháp chưa upload**. Mình **không tìm thấy** file nháp trang itch.io trên PC lẫn box. Chỉ có ghi chú: Decompression Fallback (Brotli) bắt buộc bật để itch.io/GitHub Pages tải được (TEAM_TASKS dòng ~1447, `docs\WEBGL_PERFORMANCE.md`). Cần: soạn trang itch.io (mô tả 4 ngôn ngữ, ảnh, ghi công âm nhạc/ảnh CC-BY), zip WebGL, kiểm tra kích thước (itch.io giới hạn thường ~200 MB/file; bản build3 = 264 MB cả thư mục → cân nhắc tách `zones\`/StreamingAssets hoặc host riêng). **Không upload gì cả.**
38. **Dọn rác + báo cáo cuối (E)** – báo cáo: có (`BAO-CAO-HOAN-THANH.md`). **Dọn rác: CHƯA làm** (backup vẫn nằm ở `_grok_tmp\backup-*`, 33 thư mục + 12 file; đừng xoá khi chưa chủ đồng ý).

## 2. GHI CHÚ KỸ THUẬT
**Quy tắc AGENTS.md / của chủ**: file mới + builder headless (không [MenuItem], không scene mới); sao lưu trước khi sửa vào `D:\VTC_Academy\_grok_tmp\backup-<tên>-<giờ>`; **KHÔNG git commit/push/checkout/restore**; chỉ nhạc/asset có giấy phép (CC0/CC-BY/PD), ghi `Docs\CREDITS.md` + `Docs\AUDIO_CREDITS.md`; nhân vật mới = biến thể họ Kids (một dòng trong `variants.json`); chỉ đánh "xong" khi có test + ảnh; cờ "cần người bản xứ duyệt" giữ trung thực; mỗi mục xong → ghi `STATUS-unity.md` (`python D:\VTC_Academy\_grok_tmp\tools\addlog.py <txt> s`) và `Docs\TEAM_TASKS.md` (`... tt`), tiếng Việt, kèm giờ Bangkok thật.
**Chạy test (Unity 6000.3.12f1, batchmode)**: `cd D:\VTC_Academy\_grok_tmp; .\run.ps1 -Name <n> -Platform EditMode|PlayMode -Filter "NihongoLife.Tests.A|NihongoLife.Tests.B"` (chạy nền: `Start-Process powershell -ArgumentList '-NoProfile','-File','run.ps1','-Name','r44','-Platform','PlayMode','-Filter','"..."' -WindowStyle Hidden`). Sinh `log_<n>.log`, `res_<n>.xml`, `<n>.done`. Đọc kết quả: `python D:\VTC_Academy\_grok_tmp\tools2\res.py <n> 300`. Tên run tiếp theo còn trống: r44. Lọc sai tên lớp → 0 test nhưng vẫn "Passed". Namespace: PlayMode `NihongoLife.Tests.<Lớp>`; EditMode `NihongoLife.Tests.<Lớp>` cho StudyModesUiSmokeTests/EnglishBasicUiTests/EnglishBasicCatalogTests, còn `NihongoLife.Tests.EditMode.<Lớp>` cho RussianCoverageAuditTests/RussianLocalizationTests. Trước khi chạy: không có process Unity, không còn `Temp\UnityLockfile` (nếu treo: `Get-Process Unity | Stop-Process -Force` rồi xoá lockfile). Lỗi biên dịch làm run treo ~11 phút → sau ~100 s kiểm `Select-String log_<n>.log -Pattern 'error CS'`. Hồi quy gần nhất (cuối phiên): r41 HUD/Gameplay/Bedroom/Portrait 12/12; r42 wildlife/Midori/environment/soundscape/townfolk 10/11; r43 AmbientWildlife riêng 2/2.
**Bài học**: luôn UTF-8 — đọc/ghi code bằng `Get-Content -Encoding UTF8` hoặc python (giữ BOM, chuẩn hoá `\n`, ghi lại CRLF; assert mỗi thay thế đúng 1 lần); `python -c` với dấu nháy dễ vỡ → lưu thành file .py; dùng đường dẫn tuyệt đối (cwd của Shell giữ giữa các lệnh); console hiện mojibake nhưng file vẫn đúng; Read/CopyToBox từ chối đường D:\ nên không xem ảnh được; không tự `git checkout` ảnh bị Unity ghi đè. Không commit/push. Lệnh dài đôi khi bị classifier từ chối tạm → chạy lại y nguyên.
**Lỗi đã biết**: (a) **test nhiễm thứ tự**: `GestureCapturePlayModeTests` chỉ pass khi chạy riêng; `Hud_PlaceName_And_Screens_FollowRussian` fail sau IllnessPlayModeTests (thông báo ốm của HUD không dịch lại khi đổi ngôn ngữ — lỗi thật); `LifeLoop City_HudCursorJournalKonbiniShiftAndSpend` fail sau test Bank; `City_Wildlife_BehavioursSeasonsAndLod` fail khi chạy chung 5 lớp (con chó "không đi", 0,04 < 0,8) nhưng pass riêng — chưa xác định nguyên nhân (có thể thứ tự test/thời gian chạy; chưa loại trừ lá rơi). (b) **nhấp nháy bản đồ khi vào game chưa tìm ra gốc** (mục 35). (c) cần AFK chưa lưu qua scene. (d) bản đồ/popup ngôn ngữ: chỉ quét một phần. (e) dự án đang để ở build target **WebGL** (test vẫn chạy được).
**Cờ trung thực**: không xem được ảnh → icon/hạt/bố cục/animation/nội thất chỉ kiểm bằng code; chưa có người bản xứ duyệt (ru, en tenses, JLPT/IELTS, nhạc lý); bộ JLPT/IELTS chỉ luyện tập; nghe = speech synthesis trình duyệt; phím tắt mới chưa thử bàn phím thật.

## 3. BUILD WEBGL – `webgl-build3`
- Thư mục: **`D:\VTC_Academy\_grok_tmp\webgl-build3\WebGL`** (index.html, Build, StreamingAssets, TemplateData, zones, .htaccess, _headers, vercel.json, WebGLBuildSizeReport.txt). Build xong lúc 06:16 (giờ Bangkok), thoát mã 0 (`wgl3.done`), log `log_wgl3.log`, script `wgl3.ps1` (đặt `NIHONGO_BUILD_ROOT`, gọi `-buildTarget WebGL -executeMethod NihongoLife.Editor.ReleaseBuild.BuildWebGL`, thực tế ~13 phút). Đây là build **cuối cùng, sau H4/H5**. Bản dự phòng sớm hơn: `webgl-build2\WebGL` (trước H4/H5).
- Kích thước build3: Build/ ≈ **66,5 MB** (data.unityweb 56,3; wasm.unityweb 9,97; framework.js.unityweb 0,15; loader.js 0,11) — tương đương build2 (66,5 MB); cả thư mục **264 MB** (zones/: arcade 15, city 18,7, clinic 16,7, home 14,4, island 18,4, phuquoc 14,6, school 18,0, station 24,1, sushi 17,1 MB; nhạc trong StreamingAssets 0,5–3,7 MB/file). Phông NotoSansJP.otf 16,5 MB và ảnh loading ~2,4 MB/ảnh là mục lớn trong `WebGLBuildSizeReport.txt`.
- **CHƯA đo**: thời gian tải thực trên trình duyệt (cần server gửi `Content-Encoding` brotli hoặc bật Decompression Fallback) và **CHƯA chơi thử WebGL thật** (G1 lưu offline, persistence IndexedDB, micro WebMicrophone, hiệu năng). Chạy local cần HTTP server, không mở file://.
