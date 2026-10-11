# Development Guide

This guide describes how to configure, test, and build the Nihongo Life simulation.

## Scene Structure

The application flow requires that the bootstrap scene runs first to initialize persistent game objects:

1. **`00_Bootstrap`**: Loads the persistent `AppRoot` prefab (which registers scene controllers, audio channels, save profiles, and scenario registries) and then auto-loads the Main Menu.
2. **`01_MainMenu`**: Offers game starts, chapters configuration, and settings.
3. **`90_TestSandbox`**: Interactive testing scene containing cashier booths, shelves, trigger areas, and player spawn pivots.

## Running Unit & Integration Tests

Open the Unity Test Runner window (**Window -> General -> Test Runner**):
- Under the **EditMode** tab, run the `ScenarioEngineTests` group.
- These verify scoring aggregations, node graph resolution, and serialization formats.

## WebGL Build Considerations

- Local saving utilizes `File.WriteAllText` targeting `Application.persistentDataPath`. On WebGL, Unity uses Emscripten FS which persists to IndexedDB.
- Avoid synchronous thread blocking or direct file streaming outside `Application.persistentDataPath`.
- Optimize texture size configurations and keep low poly geometry to reduce load latency.

## HUD & Responsive UI Standard

Mục tiêu: HUD gọn, đẹp và đọc được trên desktop, tablet, điện thoại ngang/dọc và WebGL. Kiến trúc hiện tại được mô tả đầy đủ trong [`PLAYER_HUD.md`](PLAYER_HUD.md).

1. `HUDUI` là đầu mối kết nối gameplay. `StatusDock` dựng HUD thường trực dùng chung cho mọi zone; không tạo thêm một HUD cạnh tranh trong scene con.
2. Bố cục cố định theo mức ưu tiên: trạng thái ở trái dưới, mục tiêu ở trái trên, zone bar/feed ở giữa trên, prompt tương tác ở giữa dưới, cửa sổ ngữ cảnh ở giữa/phải.
3. `HudCanvasFitter`, `HudSafeArea` và `HudFitRect` chịu trách nhiệm scale, safe area và chống tràn. Không sửa layout bằng độ phân giải cụ thể trong `Update`.
4. Chỉ số nhân vật nằm trong `StatusDock`: portrait model thật, Lv/EXP, ví và năm vòng Sức khỏe/Thể lực/No/Khát/Tỉnh táo. Cảnh báo chỉ xuất hiện khi một chỉ số dưới 20%.
5. Thông báo ngắn dùng `HudFeed.Post(text, kind, seconds, key)`. Feed tối đa ba card, gộp cùng `key`, tạm dừng timer khi cửa sổ mở và ẩn trong hoạt động toàn màn hình.
6. Desktop dùng phím tắt và không hiện action bar. Touch layout giữ action bar, joystick và nút tương tác. Hội thoại/full-screen activity làm dock mờ hoặc ẩn để tránh che nội dung.
7. Kiểm tra bắt buộc khi đổi HUD: 1920×1080, 1366×768, 2560×1080, 1024×768, 844×390 và 390×844; mở hội thoại, inventory, cửa sổ nhân vật, bản đồ, nhiệm vụ, shop và thông báo liên tiếp để xác nhận không tràn hoặc đè nhau.

## Audio

- **Dịch vụ chung**: `AudioService` (BGM / SFX / giọng nói; thanh trượt Cài đặt lưu ở `Volume_BGM`, `Volume_SFX`). Âm UI lấy từ `Resources/Audio/GameAudioCatalog.asset` (Kenney Interface Sounds): `UiClick`, `UiConfirm`, `UiBack`, `UiError`, `UiHover`, `UiOpen`, `UiClose`, `UiTick` + cửa, bước chân, nhặt đồ...
- **Tự động cho UI**: mọi nút dựng bằng `UIStyleKit.StyleButton` đã có tiếng bấm và tiếng hover nhẹ (`ButtonColorSwap`). Popup/panel gọi `UiOpen`/`UiClose` khi mở/đóng; kéo thanh SFX có tiếng xem trước (`UiTick`).
- **Nhạc menu**: `MenuMusicController` (gắn bởi `MainMenuUI`). Có file `Assets/NihongoLife/Resources/Audio/Music/menu_bgm.*` (ogg/mp3/wav) thì dùng file đó; không có thì tự sinh nhạc nền phong cách Nhật lúc chạy (`ProceduralMenuMusic`: đàn koto Karplus-Strong, thang âm D yo, pad, loop liền mạch, không cần bản quyền). Vào game thì nhạc menu tắt dần (`FadeOutBGM`).
- **Nhạc trong game**: `TownAmbientAudio` phát `GameControlDatabase.townBgmClip` (đang trống nên chưa có nhạc) và đã theo thanh BGM.
- Trình duyệt chặn tự phát âm thanh trước lần bấm/chạm đầu tiên của người dùng; nhạc menu sẽ bắt đầu sau lần tương tác đầu.
