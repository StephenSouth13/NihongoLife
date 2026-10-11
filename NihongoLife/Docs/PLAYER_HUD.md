# Player HUD

Tài liệu này mô tả Player HUD hiện tại của NihongoLife. HUD được dùng xuyên suốt các khu vực và không thay thế `PlayerController`, camera, collider, inventory, save hay hệ thống online.

## Mục tiêu thiết kế

- Giữ vùng giữa màn hình sạch để người chơi quan sát thế giới và đọc hội thoại.
- Chỉ hiển thị thông tin cần thiết ngay lúc đó; chi tiết nằm trong cửa sổ chuyên dụng.
- Không để thông báo, nhiệm vụ, prompt và cửa sổ gameplay chồng lên nhau.
- Cùng một dữ liệu và thứ tự ưu tiên trên desktop, WebGL và thiết bị cảm ứng.

## Bố cục và mức ưu tiên

| Vị trí | Thành phần | Nội dung |
|---|---|---|
| Trái dưới | `StatusDock/StatusWidget` | Portrait model thật, tên, địa điểm, level, EXP, ví và năm nhu cầu |
| Trái trên | `TrackedObjective` | Một mục tiêu đang theo dõi, tiến độ và phím mở Sổ nhiệm vụ |
| Giữa trên | Zone bar + `HudFeed` | Trạng thái ga/đảo và tối đa ba thông báo ngắn |
| Giữa dưới | Context prompt | Một hành động tương tác gần nhất |
| Giữa/phải | Contextual windows | Inventory, nhân vật, bản đồ, nhiệm vụ, shop và các popup |

Desktop không hiện dãy nút tắt vì các chức năng đã có phím. Touch layout giữ action bar, joystick và nút tương tác.

## Thẻ trạng thái nhân vật

`StatusDock` đọc dữ liệu từ `PlayerStatus` và `PlayerInventory`:

- Portrait được `HudPortrait` chụp từ model 3D thật của người chơi, không dùng avatar giả cố định.
- Level nằm trên huy hiệu ở viền portrait; EXP dùng một thanh mảnh dưới thông tin nhận dạng.
- Ví hiển thị riêng trong pill màu vàng.
- Năm vòng radial sử dụng icon vector do `HudIcons` cung cấp: Sức khỏe, Thể lực, No, Khát và Tỉnh táo.
- Giá trị chuyển động mượt về mục tiêu. Khi nhu cầu dưới 20%, một warning chip ngắn xuất hiện phía trên; không tạo thêm popup che màn hình.

Nhấn <kbd>Tab</kbd> mở cửa sổ nhân vật với preview 3D và các số liệu tiền, kiến thức, ca làm, vốn từ. Đây là cửa sổ chi tiết; status widget vẫn là bản tóm tắt.

## Mục tiêu, prompt và thông báo

- `HudObjective.Provider` cung cấp mục tiêu theo hệ thống hiện tại; nếu không có provider, HUD dùng objective đang active của `ScenarioManager`.
- Objective text tối đa hai dòng và dùng ellipsis để không tràn.
- Context prompt chỉ hiện một tương tác. `HudPrompt.ContextualVisible` giúp hint/notification khác tránh cùng một lane.
- Hệ thống gameplay đăng thông báo bằng `HudFeed.Post(text, kind, seconds, key)`. Các loại hiện có: `Info`, `Reward`, `Warning`.
- Card cùng `key` được cập nhật thời gian/nội dung thay vì nhân bản. Feed giữ tối đa ba card và tự nhường chỗ khi modal mở.

## Responsive và trạng thái ẩn

HUD dùng `HudCanvasFitter`, safe area và anchor theo cạnh. Khi hội thoại hoặc hoạt động toàn màn hình mở, dock và feed mờ/ẩn để ưu tiên nội dung chính. Không đặt thành phần quan trọng theo tọa độ tuyệt đối tính từ tâm màn hình.

Ma trận kiểm tra tối thiểu:

| Kích thước | Mục tiêu |
|---|---|
| 1920×1080 | Desktop chuẩn |
| 1366×768 | Laptop phổ biến |
| 2560×1080 | Ultrawide |
| 1024×768 | Tỉ lệ 4:3 |
| 844×390 | Điện thoại ngang |
| 390×844 | Điện thoại dọc |

Ở mỗi kích thước cần mở lần lượt hội thoại, inventory, cửa sổ nhân vật, bản đồ, Sổ nhiệm vụ, shop và ba notification; xác nhận text không tràn, prompt không đè cửa sổ và các nút cảm ứng vẫn thao tác được.

## Tệp liên quan

- `Assets/NihongoLife/Scripts/UI/HUDUI.cs`: kết nối gameplay và các cửa sổ.
- `Assets/NihongoLife/Scripts/UI/StatusDock.cs`: status widget, objective và cửa sổ nhân vật.
- `Assets/NihongoLife/Scripts/UI/HudPortrait.cs`: render portrait từ model người chơi.
- `Assets/NihongoLife/Scripts/UI/HudFeed.cs`: hàng đợi thông báo ngắn.
- `Assets/NihongoLife/Scripts/UI/HudIcons.cs` và `HudGraphics.cs`: icon/sprite dùng chung.
- `Assets/NihongoLife/Scripts/UI/HudCanvasFitter.cs`, `HudSafeArea.cs`, `HudFitRect.cs`: responsive layout.

## Video bàn giao

- [`NihongoLife_Trailer_v2.mp4`](../../Bao_Cao/NihongoLife_Trailer_v2.mp4): H.264/AAC, 1920×1080, 30 fps, khoảng 1 phút 25 giây.
- [`NihongoLife_Gameplay_Demo.mp4`](../../Bao_Cao/NihongoLife_Gameplay_Demo.mp4): H.264/AAC, 1920×1080, 30 fps, khoảng 4 phút 18 giây.

Hai video là tệp xem/bàn giao; không đưa vào Unity `Assets` và không dùng làm nguồn runtime của HUD.
