# Phú Quốc — dữ liệu thật cho game / real-world reference

Tài liệu này là nguồn cho `Resources/PhuQuoc/phu_quoc_map.json`, `Resources/PhuQuoc/ganh_ngon.json`,
tuyến nghề **Nhà hàng & dịch vụ** (`careers.json` → `hospitality`) và cảnh `65_PhuQuocIsland`.

## 1. Đảo Phú Quốc — số liệu chính
- Đặc khu Phú Quốc, tỉnh An Giang (từ 16/6/2025; trước đó là thành phố thuộc Kiên Giang). Đảo lớn nhất Việt Nam, **575 km²**, ~50 km Bắc–Nam, ~25 km Đông–Tây nơi rộng nhất, hình gần tam giác. [Wikipedia]
- Trung tâm hành chính: **Dương Đông** (bờ Tây). Phường thứ hai: **An Thới** (mũi Nam). Các xã: Bãi Thơm, Cửa Cạn, Cửa Dương, Dương Tơ, Gành Dầu, Hàm Ninh. [Wikipedia]
- Dãy núi "99 ngọn" chạy dọc đảo, cao nhất **Núi Chúa 603 m** (phía Bắc). [Wikipedia]
- **Vườn quốc gia Phú Quốc** (lập 2001) phủ 336,57 km² nửa phía Bắc. Khu bảo tồn biển (2007) 187 km². [Wikipedia]
- Đặc sản: **nước mắm** (chỉ dẫn địa lý) và **tiêu** (trồng khắp đảo, nhiều ở Gành Dầu, Cửa Dương); nuôi ngọc trai. [Wikipedia]
- Khí hậu: mùa mưa tháng 4–11, mùa khô 12–3; nóng nhất tháng 4–5 (~35 °C). [Wikipedia]
- Sân bay quốc tế Phú Quốc (PQC) mở 2/12/2012; phà/tàu nối Rạch Giá, Hà Tiên. [Wikipedia]
- Di tích **Nhà tù Phú Quốc** (1953–1975). [Wikipedia]

## 2. Địa điểm (toạ độ thật, đã tra OpenStreetMap ngày 27/10/2026)
Toạ độ game = (x Đông, z Bắc) tính từ Gánh Ngon, **1 đơn vị = 100 m**. Độ chính xác ~100 m–1 km.

| Tên (VI) | Name (EN) | Loại | Lat, Lon | Game (x, z) | Nguồn | Ghi chú |
|---|---|---|---|---|---|---|
| Nhà hàng Gánh Ngon | Gánh Ngon Restaurant | restaurant | 10.2154, 103.9600 | (0, 0) | OpenStreetMap (Nominatim) | 113 Trần Hưng Đạo, Dương Đông (OSM point is the Trần Hưng Đạo street segment) |
| Dương Đông | Dương Đông town | town | 10.2170, 103.9590 | (-1, 2) | Wikipedia | Administrative centre, west coast |
| Chợ đêm Dinh Cậu | Dinh Cậu Night Market | market | 10.2163, 103.9606 | (1, 1) | OpenStreetMap (Nominatim) |  |
| Chợ Dương Đông | Dương Đông Market | market | 10.2205, 103.9598 | (-0, 6) | OpenStreetMap (Nominatim) |  |
| Dinh Cậu | Dinh Cậu Shrine | landmark | 10.2172, 103.9564 | (-4, 2) | OpenStreetMap (Nominatim) | Rock shrine at the Dương Đông river mouth |
| Chùa Sùng Hưng | Sùng Hưng Pagoda | landmark | 10.2152, 103.9598 | (-0, -0) | OpenStreetMap (Nominatim) |  |
| Nhà thùng nước mắm Khải Hoàn | Khải Hoàn fish sauce factory | factory | 10.2199, 103.9715 | (13, 5) | OpenStreetMap (Nominatim) | Phú Quốc fish sauce (protected origin) |
| Vườn tiêu (Đức Thạnh) | Pepper farm (Đức Thạnh) | farm | 10.2062, 103.9903 | (33, -10) | OpenStreetMap (Nominatim) | Pepper is grown island-wide, esp. Gành Dầu & Cửa Dương |
| Sân bay quốc tế Phú Quốc | Phú Quốc International Airport | transport | 10.1727, 103.9921 | (35, -47) | OpenStreetMap (Nominatim) | IATA PQC, opened 2 Dec 2012 |
| Bãi Trường | Long Beach (Bãi Trường) | beach | 10.1361, 103.9764 | (18, -88) | OpenStreetMap (Nominatim) |  |
| Suối Tranh | Tranh Stream | nature | 10.1838, 104.0153 | (61, -35) | OpenStreetMap (Nominatim) |  |
| Làng chài Hàm Ninh | Hàm Ninh fishing village | village | 10.1763, 104.0326 | (80, -43) | OpenStreetMap (Nominatim) | East coast |
| Chùa Hộ Quốc | Hộ Quốc Pagoda | landmark | 10.0996, 104.0472 | (96, -128) | ước lượng / approximate | Approximate (not geocoded) — on the south-east coast, Bãi Đầm |
| Bãi Sao | Sao Beach | beach | 10.0525, 104.0352 | (82, -180) | OpenStreetMap (Nominatim) |  |
| Bãi Khem | Khem Beach | beach | 10.0358, 104.0306 | (77, -199) | OpenStreetMap (Nominatim) |  |
| Di tích Nhà tù Phú Quốc | Phú Quốc Prison relic | history | 10.0434, 104.0188 | (64, -190) | OpenStreetMap (Nominatim) | 1953–1975 prison camp, now a historic site |
| Thị trấn Hoàng Hôn (Cầu Hôn) | Sunset Town (Kiss Bridge) | entertainment | 10.0283, 104.0042 | (48, -207) | OpenStreetMap (Nominatim) | Kiss Bridge (Cầu Hôn) point |
| An Thới | An Thới town | town | 10.0121, 104.0148 | (60, -225) | OpenStreetMap (Nominatim) | Southern ward |
| Cảng An Thới | An Thới Port | transport | 10.0126, 104.0148 | (60, -224) | OpenStreetMap (Nominatim) |  |
| Ga cáp treo Hòn Thơm (An Thới) | Hòn Thơm cable car station | transport | 9.9920, 104.0124 | (58, -247) | OpenStreetMap (Nominatim) |  |
| Hòn Thơm (Sun World) | Hòn Thơm island (Sun World) | island | 9.9543, 104.0176 | (63, -289) | OpenStreetMap (Nominatim) | Off the south tip; reached by cable car |
| Bãi Ông Lang | Ông Lang Beach | beach | 10.2574, 103.9367 | (-26, 46) | OpenStreetMap (Nominatim) |  |
| Cửa Cạn | Cửa Cạn | village | 10.2776, 103.9236 | (-40, 69) | OpenStreetMap (Nominatim) |  |
| Grand World (chợ đêm phố đi bộ) | Grand World (walking-street night market) | entertainment | 10.3249, 103.8581 | (-112, 121) | OpenStreetMap (Nominatim) |  |
| VinWonders Phú Quốc | VinWonders Phú Quốc | entertainment | 10.3384, 103.8547 | (-115, 136) | OpenStreetMap (Nominatim) |  |
| Bãi Dài | Dài Beach | beach | 10.3359, 103.8491 | (-121, 133) | OpenStreetMap (Nominatim) |  |
| Gành Dầu | Gành Dầu | village | 10.3708, 103.8447 | (-126, 172) | OpenStreetMap (Nominatim) | North-west cape, facing Cambodia |
| Rạch Vẹm | Rạch Vẹm | village | 10.3612, 103.9331 | (-29, 161) | OpenStreetMap (Nominatim) | North coast |
| Vườn quốc gia Phú Quốc | Phú Quốc National Park | nature | 10.3283, 104.0246 | (71, 125) | OpenStreetMap (Nominatim) | 336.57 km² in the north (est. 2001) |
| Núi Chúa (603 m) | Chúa Mountain (603 m) | nature | 10.3777, 104.0388 | (86, 180) | OpenStreetMap (Nominatim) | Highest peak of the 99-peak ridge |
| Bãi Thơm | Bãi Thơm | village | 10.4120, 104.0315 | (78, 217) | OpenStreetMap (Nominatim) | North-east |

Bố cục tương đối (Bắc ở trên):
```
            Gành Dầu ·  Rạch Vẹm     Bãi Thơm
      VinWonders/Grand World      Núi Chúa ▲ (VQG)
         Bãi Dài
               Cửa Cạn · Ông Lang
      Dương Đông ● Dinh Cậu, chợ đêm, Gánh Ngon     Suối Tranh   Hàm Ninh (bờ Đông)
            Bãi Trường      ✈ Sân bay
                                     Chùa Hộ Quốc
              Sunset Town · An Thới · Nhà tù · Bãi Khem · Bãi Sao
                     ⛴ Cáp treo → Hòn Thơm (ngoài khơi phía Nam)
```

## 3. Bố cục bản đồ trong game & tỉ lệ
- **Không làm 1:1**: đảo thật 575 km² ≈ 50 km × 25 km — đi bộ trong game mất hàng giờ, Unity mất độ chính xác số thực ở xa gốc toạ độ, WebGL không tải nổi. Tỉ lệ chọn: **1:100 theo phương ngang** (1 đơn vị Unity = 100 m → đảo ~500 × 250 đơn vị), **1:50 theo phương đứng** (Núi Chúa ≈ 12 đơn vị) để địa hình còn đọc được.
- Mọi điểm đặt bằng phép chiếu equirectangular từ toạ độ thật quanh Gánh Ngon (`PhuQuocMap.ToWorld`), nên **vị trí tương đối giữ đúng địa lý thật** (EditMode test kiểm tra: Hòn Thơm ở phía Nam An Thới, Bãi Dài Tây-Bắc Dương Đông, Hàm Ninh bờ Đông…).
- Đường bờ biển vẽ tay theo bản đồ OSM (~1 km sai số), 27 điểm — cần chi tiết hơn thì thay bằng dữ liệu coastline OSM.
- Khu chơi chi tiết (đường phố, nội thất) nên làm ở **tỉ lệ 1:1 cục bộ** quanh Dương Đông (Trần Hưng Đạo – Bạch Đằng – chợ đêm – Dinh Cậu) và các điểm dịch chuyển (portal) tới các khu khác, như cách game đang làm với các scene.

## 4. Nhà hàng Gánh Ngon (thông tin từ anh Huy)
- 113 Trần Hưng Đạo, Dương Đông, Phú Quốc · mở cửa 10:00–23:00 · khách đa ngôn ngữ.
- Món: hải sản nướng BBQ, bò nướng, **lẩu gà sầu riêng**, **sầu riêng chiên**, **cơm niêu buổi trưa 10:00–13:00**, quầy pub/cocktail, **9 phòng VIP**.
- Không có giá (không bịa giá); không có món động vật hoang dã (validator chặn). Tên nhân viên trong game (chị Mai, Vy, anh Tùng, anh Khoa) là nhân vật hư cấu.

## Nguồn
- Wikipedia — Phú Quốc: https://en.wikipedia.org/wiki/Ph%C3%BA_Qu%E1%BB%91c
- OpenStreetMap Nominatim (toạ độ): https://nominatim.openstreetmap.org/ — dữ liệu © OpenStreetMap contributors, ODbL.
- Cổng thông tin đặc khu Phú Quốc: https://phuquoc.angiang.gov.vn
- Gánh Ngon: thông tin do chủ nhà hàng (Huy) cung cấp.
