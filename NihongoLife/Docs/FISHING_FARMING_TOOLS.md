# Farming, nông cụ và câu cá

Tài liệu này mô tả phần đã có trong `60_MidoriIsland`. Hệ thống dùng `PlayerController`, camera, collider, `PlayerInventory`, save local/cloud, quest và economy hiện hữu; không tạo một gameplay stack riêng.

## Trồng trọt

`FarmPlot` lưu từng ô ruộng trong `IslandState`: đất chưa xới → đã xới → gieo hạt → cần tưới → đang lớn → thu hoạch. Cây có ba giai đoạn; sau mỗi giai đoạn phải tưới lại. Thu hoạch đưa nông sản vào balo, tăng thống kê và phát sự kiện quest.

| Công việc | Yêu cầu | Thời gian chuẩn |
|---|---|---:|
| Xới đất | Ưu tiên cuốc; xẻng là phương án chậm hơn | 1,6 giây / 2,6 giây |
| Dọn ô ruộng | Ưu tiên xẻng; cuốc là phương án chậm hơn | 1,8 giây / 2,5 giây |
| Gieo hạt | Có hạt giống trong balo | 1,2 giây |
| Tưới | Bình tưới còn độ bền | 1,5 giây |
| Thu hoạch | Cây đạt giai đoạn 3 | 1,4 giây |

`IslandTools` đọc độ bền từ `island_catalog.json`. Chỉ thao tác hoàn tất mới hao một lần dùng; khi về 0, công cụ đang dùng bị xoá khỏi balo và món dự phòng bắt đầu với độ bền đầy. Không có fallback làm bằng tay cho công việc yêu cầu công cụ.

## Câu cá

Tương tác với `FishingSpot` tại cầu tàu bằng <kbd>F</kbd>. `IslandFishing` chạy các trạng thái `Casting → Waiting → Bite → Reeling → Showing`:

1. Kiểm tra cần câu và dùng luồng inventory chung.
2. Khoá di chuyển tạm thời, xoay người ra mặt nước, gắn cần vào tay phải và quăng phao.
3. Chờ 2,5–6 giây; khi phao chìm, người chơi có 1,8 giây để nhấn <kbd>F</kbd> hoặc nút Kéo.
4. Cá được chọn theo trọng số trong catalog, hiện bằng prefab thật rồi mới thêm đúng một lần vào balo.
5. Cá được lưu, tính quest/thành tựu và xuất hiện trong tab bán của cửa hàng Midori.

Kéo sớm, bỏ lỡ, nhấn <kbd>Esc</kbd>, thiếu cần hoặc đầy balo không trao phần thưởng. Các prefab hiện có gồm cá tráp (`tai`), cá ngừ (`maguro`), cá bơn (`hirame`) và cá nóc (`fugu`), cùng cần, phao, cuốc và xẻng cầm tay.

## Kiểm thử

`FishingFarmPlayModeTests` kiểm tra trực tiếp trong scene hiện hữu: thiếu công cụ, ưu tiên công cụ, độ bền/hỏng, huỷ không mất độ bền, trồng–tưới–lớn–thu hoạch, animation/prop ở tay phải, toàn bộ nhánh câu cá, bán cá, quest và save/reload. Test cũng chụp ảnh regression vào `Bao_Cao/fishing-farm-regression/`.

Không coi scene hoàn tất chỉ nhờ unit test: mỗi thay đổi hình ảnh, collider, cầu tàu hoặc điểm quăng phao vẫn phải được xem và va chạm thử trong Play Mode.
