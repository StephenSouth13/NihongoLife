# Contributing to NihongoLife / Đóng góp cho NihongoLife

NihongoLife is a Unity 6000.3.12f1 (URP) life-sim for Vietnamese learners of Japanese N5. Game code lives in `NihongoLife/Assets/NihongoLife/Scripts`. The Unity project to open is the inner `NihongoLife/` folder.

NihongoLife là game mô phỏng cuộc sống trên Unity 6000.3.12f1 (URP) cho người Việt học tiếng Nhật N5. Mã game nằm ở `NihongoLife/Assets/NihongoLife/Scripts`. Hãy mở thư mục `NihongoLife/` bên trong repo bằng Unity Hub.

## English

### Setup

1. Install [Unity 6000.3.12f1](https://unity.com/releases/editor/whats-new/6000.3.12f1) and [Git LFS](https://git-lfs.com/) (`git lfs install`) before you add or change large binary assets.
2. Clone the repo and open `NihongoLife/` (the Unity project). Press Play on `Assets/NihongoLife/Scenes/00_Bootstrap.unity`. There is no setup menu.
3. Optional online / AI: copy `NihongoLife/LocalSecrets/secrets.example.json` to `NihongoLife/LocalSecrets/secrets.json`. The example shows the four fields: `NIHONGOLIFE_SUPABASE_URL` (`https://<SUPABASE_PROJECT_REF>.supabase.co`), `NIHONGOLIFE_SUPABASE_ANON_KEY` (anon key, not service_role), `AGORA_APP_ID` (App ID, not the certificate), `NIHONGOLIFE_GEMINI_API_KEY`. Replace the `<...>` placeholders. Environment variables with the same names override the file. Unreplaced placeholders are ignored and the game stays offline. Never commit `secrets.json` or paste keys into scenes, ScriptableObjects, or pull requests.

### Rules that keep the game shippable

- Do not add `[MenuItem]` commands. Build and repair steps must run from code that already executes, not from a manual menu.
- Do not create a new Unity scene unless a maintainer has approved that exact scene. Extend the existing gameplay scene and prefabs.
- Finished content belongs in scenes, prefabs, and assets. Opening the project and pressing Play must already be the finished state.
- Do not stack a replacement environment on top of an old one. Remove or disable the old hierarchy, then check renderers, colliders, doors, spawn points, and navigation together.
- Do not rename or move a `MonoBehaviour` script that scenes or prefabs reference. The `.meta` GUID is the serialized identity. New partial-class files are fine; the original file stays the component.
- Do not rename a `[SerializeField]` without `[FormerlySerializedAs]`.
- Do not commit API keys, `.env` files, test logs, `task-o-*` results, or `.codex_*.py` scratch scripts.
- New `.cs` files under `Assets/` need a `.meta` file with a fresh GUID.
- A scene is not "done" until someone has played it and checked collision, not only screenshots.

### Tests

EditMode and PlayMode tests live under `NihongoLife/Assets/NihongoLife/Tests`. Run them from the Unity Test Runner or batch mode (see the README). `MidoriIsland_FullAcceptanceFlow` and `MidoriIsland_PerformanceAndLayout` skip themselves when `SystemInfo.graphicsDeviceType` is `Null` (headless `-nographics`). Run those two with a GPU before claiming island visuals are fine. A skip is not a pass.

### Pull requests

Use the pull request template. Describe player-visible behavior, list scenes you played, and say what you could not run. Keep refactors behavior-preserving unless the change is the point of the PR.

## Tiếng Việt

### Chuẩn bị

1. Cài Unity **6000.3.12f1** và Git LFS (`git lfs install`) trước khi thêm hoặc sửa asset nhị phân lớn.
2. Clone repo và mở thư mục `NihongoLife/`. Bấm Play ở `Assets/NihongoLife/Scenes/00_Bootstrap.unity`. Không có bước setup trong menu.
3. Online / AI (tuỳ chọn): copy `NihongoLife/LocalSecrets/secrets.example.json` thành `secrets.json` cùng thư mục. File mẫu có bốn trường: `NIHONGOLIFE_SUPABASE_URL` (`https://<SUPABASE_PROJECT_REF>.supabase.co`), `NIHONGOLIFE_SUPABASE_ANON_KEY` (khoá anon, không phải service_role), `AGORA_APP_ID` (App ID, không phải certificate), `NIHONGOLIFE_GEMINI_API_KEY`. Thay các placeholder `<...>`. Biến môi trường cùng tên được ưu tiên hơn file. Placeholder chưa thay bị bỏ qua và game chạy offline. Không commit `secrets.json`, không dán khoá vào scene, ScriptableObject hay pull request.

### Quy tắc để game vẫn chạy được

- Không thêm lệnh `[MenuItem]`. Build và sửa scene phải chạy từ code, không cần bấm menu tay.
- Không tạo scene Unity mới nếu người bảo trì chưa duyệt đúng scene đó. Hãy mở rộng scene và prefab đang có.
- Nội dung xong phải nằm trong scene, prefab và asset. Mở project và bấm Play là chơi được bản hoàn chỉnh.
- Không chồng môi trường mới lên môi trường cũ. Gỡ hoặc tắt hierarchy cũ, rồi kiểm tra renderer, collider, cửa, điểm spawn và navigation cùng lúc.
- Không đổi tên hoặc di chuyển file `MonoBehaviour` mà scene/prefab đang trỏ tới. GUID trong `.meta` là danh tính đã serialize. Tách `partial class` thì được; file gốc vẫn là component.
- Không đổi tên `[SerializeField]` nếu chưa có `[FormerlySerializedAs]`.
- Không commit API key, file `.env`, log test, kết quả `task-o-*`, hay script tạm `.codex_*.py`.
- File `.cs` mới trong `Assets/` cần file `.meta` với GUID mới.
- Chưa chơi thử và chưa kiểm tra va chạm thì chưa gọi scene là xong.

### Kiểm thử

Test EditMode và PlayMode nằm ở `NihongoLife/Assets/NihongoLife/Tests`. Chạy bằng Unity Test Runner hoặc batchmode (xem README). `MidoriIsland_FullAcceptanceFlow` và `MidoriIsland_PerformanceAndLayout` tự bỏ qua khi `SystemInfo.graphicsDeviceType` là `Null` (headless `-nographics`). Hãy chạy hai test đó trên máy có GPU trước khi kết luận hình ảnh đảo đã ổn. Bỏ qua không có nghĩa là đạt.

### Pull request

Dùng mẫu pull request. Nêu hành vi người chơi thấy được, scene đã chơi, và phần chưa chạy được. Refactor giữ nguyên hành vi, trừ khi thay đổi hành vi chính là mục đích của PR.

## Further reading / Đọc thêm

- [Architecture for new contributors](docs/ARCHITECTURE.md)
- [Unity architecture notes](NihongoLife/Docs/ARCHITECTURE.md)
- [Code of conduct](CODE_OF_CONDUCT.md)
