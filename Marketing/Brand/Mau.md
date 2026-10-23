# Màu

Lấy từ `UiTheme` trong `NihongoLife/Assets/NihongoLife/Scripts/UI/UiKit.cs`. Đổi float 0–1 sang hex. Đây là màu panel đêm và vàng của nút chính, không phải bảng màu vẽ thêm.

| Tên trong code | Hex | Dùng trên bài |
|---|---|---|
| Sunken | `#0B0F17` | Nền chữ, chân ảnh |
| Ink | `#111721` | Thanh tiêu đề, bìa PDF |
| Surface | `#1A212E` | Thẻ nổi |
| Raised | `#222B3B` | Lớp hover, ít dùng trên bài |
| Gold | `#F7BD3D` | Nhãn, gạch chân, nút |
| GoldDeep | `#DB941F` | Vàng đậm khi cần tách khỏi Gold |
| OnGold | `#1F1405` | Chữ trên nền vàng |
| Text | `#F2F5FA` | Chữ chính trên nền đêm |
| TextSoft | `#C7D1E0` | Dòng phụ |
| Muted | `#94A3B8` | Chữ nhỏ |
| Teal | `#29B899` | Chỉ khi cần một điểm phụ, không thay vàng |
| Live | `#ED426B` | Màu «đang sống» trong UI. Không dùng làm huy hiệu giả |

`UiTheme` còn `Jlpt` (`#DB4540`) và `Ielts` (`#3378E6`) để tô nhóm đề trong game. Trên bài đăng không dùng hai màu đó như logo kỳ thi.

Brief từng ghi vàng `#F2B233` và xanh đêm `#0B0F16`. Hai mã đó gần bảng trên. Bài và PDF dùng hex của `UiTheme` cho khớp game.

Nền giấy trong PDF: `#F7F4EE`. Viền bảng: `#E4D7B8`.

Chữ trên ảnh luôn nằm trên lớp đêm ở cuối khung, để vàng và trắng đọc được trên cả trời sáng lẫn nội thất tối.
