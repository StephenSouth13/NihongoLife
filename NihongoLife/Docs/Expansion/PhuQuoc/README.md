# Phú Quốc (phần mở rộng — chưa đưa vào game)

Anh Long quyết định (2026-10-26): Phú Quốc làm sau, như một bản mở rộng lớn. Hiện chỉ có dữ liệu nháp; game không tham chiếu tới thư mục này.

## Định hướng của anh Long
- Đảo rất rộng, dựng 3D sát thực tế, chia **3 khu**:
  - **Bắc**: hệ sinh thái nghỉ dưỡng, vui chơi (khu do Vingroup đầu tư).
  - **Trung tâm**: Dương Đông.
  - **Nam**: khu do Sun Group phát triển (An Thới, cáp treo Hòn Thơm…).
- Có nhiều cửa hàng quần áo, đồ ăn, nhà hàng, trường học, dạy học… và **rất nhiều ngành nghề**.
- Đảo Midori giữ vai trò nông trại; Phú Quốc là khu du lịch, đô thị lớn.

## Có gì ở đây
- `phuquoc.json`: đường viền đảo, các đảo An Thới, đường chính và 19 địa điểm (mô tả vi/en/ja, điều thú vị, câu hỏi tiếng Nhật của khách du lịch cho tuyến "dẫn đoàn khách Nhật").
  - Toạ độ tính theo pixel của bản đồ tham khảo; khoảng 0,9 m/pixel (≈1:45, từ mũi Trâu Nằm đến An Thới ≈ 1,1 km).
- `PhuQuocCatalog.cs.txt`: đọc dữ liệu, đổi toạ độ bản đồ ↔ thế giới, chia tam giác đa giác (cho lưới địa hình và bản đồ UI). Đổi lại thành `.cs` khi bắt đầu làm.
- Tên scene đề xuất: `70_PhuQuocIsland` (cần anh duyệt đúng tên trước khi tạo).

## Lưu ý khi làm tiếp
- **Nguồn bản đồ:**
  - Không lấy dữ liệu hay ảnh 3D từ Google Maps: điều khoản của Google cấm trích xuất để dựng game.
  - Dùng **OpenStreetMap** (giấy phép ODbL, phải ghi "© OpenStreetMap contributors" trong phần credits). Đã thử Overpass API trong vùng bbox `9.80,103.83,10.47,104.10`: có đường bờ biển (41 đoạn), 612 tuyến đường chính, 917 điểm du lịch; cửa hàng, nhà hàng, nhà dân thì quá tải máy chủ công cộng, cần tải theo từng lần nhỏ hoặc dùng bản sao Overpass riêng.
- **Thương hiệu:**
  - Tên và logo thương mại (Vinpearl, VinWonders, Grand World, Sun World, Sunset Sanato…) không dùng khi chưa có phép.
  - Trong game nên đặt tên mô tả hoặc tên riêng của game (vd. "Khu vui chơi Bắc Đảo", "Thị trấn kênh đào"), còn tên địa danh công cộng (Dương Đông, Bãi Sao, An Thới, Hàm Ninh…) dùng được.
- **Ngành nghề:** hệ nghề nghiệp đã sẵn (`Resources/Progression/careers.json` + ca làm trong `progression.json`). Mỗi nghề mới ở Phú Quốc (hướng dẫn viên tiếng Nhật, lễ tân resort, bán hàng, giáo viên…) chỉ cần thêm dữ liệu và trạm làm việc.
