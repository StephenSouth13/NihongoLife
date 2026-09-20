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

Mục tiêu: HUD gọn, đẹp, đọc được trên mọi thiết bị (máy tính, tablet, điện thoại ngang/dọc) khi đưa lên website. Áp dụng cho mọi UI mới.

1. **UI của HUD phải nằm trong scene** (đúng như `AGENTS.md`), không dựng lúc chạy. Nhìn Scene view phải giống Play Mode trên máy tính. Cấu trúc gameplay: `Canvas` (có `HudCanvasFitter`) > `SafeArea` (có `HudSafeArea`) > `HUDPanel` > các panel.
2. **Không đặt vị trí/kích thước bằng code lúc chạy** cho panel HUD (đã bỏ `RepairRuntimeLayout`). Chỉnh trực tiếp `RectTransform` trong scene.
3. **Co giãn theo thiết bị** làm bằng 3 component dùng chung: `HudCanvasFitter` (chọn độ phân giải tham chiếu: máy tính 1080p, thiết bị cảm ứng ~540-720 theo chiều cao CSS, chế độ Expand nên luôn thấy đủ vùng thiết kế), `HudSafeArea` (tránh tai thỏ/thanh trình duyệt), `HudFitRect` (panel cố định kích thước tự thu nhỏ để vừa màn hình; trên màn lớn giữ nguyên kích thước thiết kế). Panel dựng lúc chạy (menu, shop) gắn `HudFitRect` ngay khi tạo.
4. **Neo theo cạnh**: mỗi phần tử neo vào góc/cạnh gần nhất (mission góc trái trên, chỉ số + thông báo cột phải trên, hội thoại giữa dưới) — không dùng vị trí tuyệt đối theo tâm màn hình.
5. **Chỉ số nhân vật** nằm trong `HudRightColumn/VitalsCard` (`HudVitalsCard`: Lv/EXP, ví, Máu/Năng lượng/No/Khát; thanh đỏ nhấp nháy khi dưới 25%). Thêm chỉ số mới: nhân đôi 1 hàng trong scene, thêm vào danh sách `rows`, mở rộng `ReadValue`.
6. **Thông báo** nằm trong `HudRightColumn/NotificationTray` (3 khe cố định, `HudNotificationTray`). Hệ thống nào cần báo chỉ gọi `HudNotificationTray.Instance.Post(id, tiêu đề, phụ đề, onClick, tone)` và `Clear(id)`; không tự dựng chip riêng.
7. **Kiểm tra bắt buộc** khi đổi HUD: Game view ở 1920x1080, 1366x768, 2560x1080 (ultrawide), 1024x768 (4:3), 844x390 và 390x844 (điện thoại ngang/dọc; bật `forceTouchLayout` trên `HudCanvasFitter` để giả lập cảm ứng), rồi mở hội thoại, túi đồ, hồ sơ, thực đơn nhà hàng và thông báo hóa đơn — không panel nào tràn màn hình hay đè nhau.
