# NihongoLife architecture / Kiến trúc cho người mới

Short map for contributors. The deeper Unity notes stay in [`NihongoLife/Docs/ARCHITECTURE.md`](../NihongoLife/Docs/ARCHITECTURE.md).

Bản đồ ngắn cho người mới. Ghi chú Unity chi tiết hơn nằm ở [`NihongoLife/Docs/ARCHITECTURE.md`](../NihongoLife/Docs/ARCHITECTURE.md).

## What you open / Mở gì

The git root contains the README, reports (`Bao_Cao/`), and the Unity project in `NihongoLife/`. Open **`NihongoLife/`** in Unity 6000.3.12f1. Press Play on `Assets/NihongoLife/Scenes/00_Bootstrap.unity`. Other zones can be played directly; they boot through the city so the HUD and exits exist.

Repo gốc chứa README, báo cáo (`Bao_Cao/`) và project Unity ở `NihongoLife/`. Mở **`NihongoLife/`** bằng Unity 6000.3.12f1. Play scene `Assets/NihongoLife/Scenes/00_Bootstrap.unity`. Các khu khác chơi trực tiếp được; game tự nạp thành phố nên vẫn có HUD và lối ra.

## Assemblies / Assembly

| Assembly | Where | Role |
|---|---|---|
| `NihongoLife` | `Assets/NihongoLife/Scripts` (not `Editor/`) | Runtime game |
| `NihongoLife.Editor` | `Assets/NihongoLife/Scripts/Editor` | Scene and asset builders. No `[MenuItem]`. |
| `NihongoLife.MiniGames` | `Assets/NihongoLife/MiniGames` | Kana Match and the mini-game host |
| `NihongoLife.Tests.EditMode` / `PlayMode` | `Assets/NihongoLife/Tests` | NUnit + Unity Test Framework |

A `partial class` must stay inside one assembly. UI and world partials live next to their original file. `IslandUI`, `HUDUI`, `IeltsTestUI`, and `StationTravelController` are still the MonoBehaviours that scenes reference. `CityStreetBuilder` and `SceneBuilder` are static editor entry points split the same way.

`partial class` phải nằm trong cùng một assembly. Các partial UI và world nằm cạnh file gốc. `IslandUI`, `HUDUI`, `IeltsTestUI` và `StationTravelController` vẫn là MonoBehaviour mà scene đang trỏ. `CityStreetBuilder` và `SceneBuilder` là entry point editor (class tĩnh) được tách cùng kiểu.

## Runtime shape / Hình dạng lúc chạy

`AppRoot` in the bootstrap scene registers services in `GameServices` (scene flow, audio, settings, input, saves, control database). Gameplay reads those services instead of constructing them.

`GameControlDatabase` (`Resources/Control/NihongoLifeControlDatabase.asset`) holds scenario order, language, and feature flags. Supabase, Agora, and Gemini secrets are **not** stored in that asset. `LocalSecrets` reads environment variables, then `NihongoLife/LocalSecrets/secrets.json`. Empty config leaves saves local, NPC lines authored, and Agora idle.

`ScenarioManager` runs a `ScenarioDefinition` (objectives and dialogue), not a separate manager per shop or house. `DialogueManager` shows Japanese, reading, romaji, and translation according to the learning mode. `LearningMasteryManager` records N5 vocabulary and grammar.

Zones (street, konbini, room, station, sushi, school, game center, Midori island) are scenes plus runtime helpers. `SceneFlowController` loads and returns. `SceneRefs` caches `FindFirstObjectByType` and `GameObject.Find` for active objects and drops the cache when a scene unloads. Editor builders still call `GameObject.Find` directly.

`AppRoot` ở scene bootstrap đăng ký dịch vụ trong `GameServices` (chuyển scene, âm thanh, cài đặt, input, lưu game, control database). Gameplay lấy dịch vụ từ đó.

`GameControlDatabase` giữ thứ tự kịch bản, ngôn ngữ và cờ tính năng. Khoá Supabase, Agora và Gemini **không** nằm trong asset đó. `LocalSecrets` đọc biến môi trường, rồi `NihongoLife/LocalSecrets/secrets.json`. Thiếu cấu hình thì lưu cục bộ, NPC dùng câu soạn sẵn, Agora không gọi.

`ScenarioManager` chạy một `ScenarioDefinition` (mục tiêu và hội thoại), không có manager riêng cho từng cửa hàng. `DialogueManager` hiện tiếng Nhật, cách đọc, romaji và bản dịch theo chế độ học. `LearningMasteryManager` ghi vốn từ và ngữ pháp N5.

Các khu (phố, konbini, phòng, ga, sushi, trường, game center, đảo Midori) là scene cùng script runtime. `SceneFlowController` lo nạp và quay lại. `SceneRefs` nhớ kết quả `FindFirstObjectByType` và `GameObject.Find` khi object còn active, và xoá cache khi scene unload. Builder trong Editor vẫn gọi `GameObject.Find` trực tiếp.

## What not to break / Đừng làm gãy

- Serialized script GUIDs (`.meta` next to a MonoBehaviour).
- `[SerializeField]` names, unless you add `[FormerlySerializedAs]`.
- Prefab and scene YAML references. Do not retarget them to a new file just because a method moved into a partial.
- `executeMethod` names such as `NihongoLife.EditorTools.CityStreetBuilder.Build` and `NihongoLife.Editor.SceneBuilder.BuildAllScenes`. They still resolve on the partial class.

GUID script đã serialize, tên `[SerializeField]`, và tham chiếu YAML của prefab/scene. Đừng trỏ chúng sang file mới chỉ vì một hàm chuyển sang partial. Tên `executeMethod` của `CityStreetBuilder.Build` và `SceneBuilder.BuildAllScenes` vẫn nằm trên class partial đó.
