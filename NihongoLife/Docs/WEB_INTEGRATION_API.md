# TÀI LIỆU YÊU CẦU TÍCH HỢP API: QUANG DUNG NIHONGO (WEB) & NIHONGOLIFE (GAME)

Tài liệu này dùng để gửi cho bộ phận kỹ thuật / lập trình viên quản lý website `quangdungnihongo.com`. Nó liệt kê danh sách các API (Cổng giao tiếp) cần thiết để biến Game thành "Metaverse" của Trung tâm.

Hệ thống Game đang sử dụng **Unity** làm Client và **Supabase** làm Cloud Backend.

---

## PHẦN 1: TÍCH HỢP ĐĂNG NHẬP (SSO - Single Sign-On)
**Mục tiêu:** Học viên tải game về, dùng tài khoản học viên trên Website để đăng nhập luôn vào Game. Không cần tạo acc mới.

**Cách 1: Nếu Website có sẵn API Đăng nhập (Khuyên dùng)**
- **Game sẽ gửi yêu cầu (POST):** `https://quangdungnihongo.com/api/v1/auth/login`
- **Dữ liệu gửi lên (JSON):** `{"username": "...", "password": "..."}`
- **Web cần trả về (JSON):** 
  ```json
  {
    "status": "success",
    "token": "JWT_TOKEN_CUA_WEB",
    "user": {
      "id": "USR123",
      "name": "Nguyễn Văn A",
      "level": "N4"
    }
  }
  ```
*(Game sẽ lấy ID này để tạo file Save nhân vật trong hệ thống Supabase).*

**Cách 2: Website đồng bộ thẳng qua Supabase (Tự động hóa hoàn toàn)**
- Backend của Website cài đặt thư viện Supabase (Ví dụ: `supabase-js` hoặc `supabase-php`).
- Bất cứ khi nào có học viên đăng ký acc mới trên Web, Backend của Web tự động bắn 1 lệnh tạo User sang CSDL Supabase của Game. Khi đó Game tự động có tài khoản.

---

## PHẦN 2: TÍCH HỢP HỌC TRÊN WEB - NHẬN THƯỞNG TRONG GAME
**Mục tiêu:** Kích thích học viên lên Web học lý thuyết. Cứ xem xong 1 video / làm xong 1 bài test trên Web là trong Game tự động nhận được Tiền (Yen) để mua nhà, mua xe.

**Bộ phận Web cần làm:**
Xây dựng một **Webhook** hoặc **Trigger**. Khi hệ thống Web ghi nhận Học viên A hoàn thành bài giảng:
- **Gửi HTTP POST (từ Web)** tới Supabase REST API của Game.
- **Dữ liệu gửi (JSON):** Cập nhật cột `Yen` của Học viên A thêm 500 Yen, hoặc chèn một dòng vào bảng `notifications` (Game sẽ tự động đọc bảng này và nổ hiệu ứng nhận tiền trên màn hình người chơi).

---

## PHẦN 3: TÍCH HỢP THÀNH TÍCH GAME - NHẬN QUÀ NGOÀI ĐỜI
**Mục tiêu:** Game thủ cày cuốc, thi đỗ kỳ thi JLPT N4 ảo trong Game với điểm tuyệt đối. Game sẽ báo cho Web để cấp Voucher giảm giá khóa học thật.

**Bộ phận Web cần cung cấp 1 API (Webhook):**
- **Endpoint:** `https://quangdungnihongo.com/api/v1/game/voucher-request`
- **Game sẽ gọi API này và gửi (JSON):**
  ```json
  {
    "userId": "USR123",
    "achievement": "PASSED_JLPT_N4_SIMULATION",
    "score": 180,
    "secretKey": "KEY_BAO_MAT_CHI_2_BEN_BIET"
  }
  ```
- **Web cần xử lý:** Nhận thông tin, tự động tạo ra 1 mã Voucher giảm 20% học phí và gửi vào Email của học viên, hoặc lưu vào tài khoản trên Web của học viên. Trả kết quả thành công về cho Game để Game hiện thông báo chúc mừng.

---

## TỔNG KẾT NHIỆM VỤ CHO TEAM WEB
1.  **Cung cấp API Login:** Xác thực tài khoản / mật khẩu.
2.  **Cung cấp API Nhận Thành tích:** Cấp voucher / điểm thưởng trên web khi học viên chơi game giỏi.
3.  **Viết Script bắn Data sang Supabase:** Khi học viên nạp tiền/học xong bài trên web, bắn API cập nhật Tiền ảo/Item vào Supabase của Game. (Mình sẽ cung cấp thông tin kết nối Supabase sau khi thống nhất).
