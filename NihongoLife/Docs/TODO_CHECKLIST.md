# Development Checklist

Checklist này là danh sách công việc còn lại sau khi đối chiếu với `ARCHITECTURE.md`, `CONTENT_GUIDE.md`, `DEVELOPMENT.md`, `ROADMAP.md` và `SCENARIO_SYSTEM.md`.

## Trạng thái hiện tại

- [x] Core service locator và bootstrap flow.
- [x] Local scenario repository tải `ScenarioDefinition` từ `Resources/Scenarios`.
- [x] Scenario engine với dialogue, choice, scoring và objective.
- [x] Scenario `scenario.konbini.buy_onigiri` dạng vertical slice.
- [x] Kết nối visual wrapper với asset Kenney thật cho đường, tòa nhà, kệ, quầy và vật phẩm.
- [x] Kết nối tương tác vật phẩm `onigiri`/`water`.
- [x] Kết nối trigger điều hướng thật cho cửa hàng và quầy thu ngân.
- [x] Sửa objective để chỉ hoàn tất sau hành động thật của người chơi.
- [x] Smoke test PlayMode cho flow vào cửa hàng → lấy onigiri → đến quầy.

## P0 — Bắt buộc trước khi gọi là vertical slice hoàn chỉnh

- [ ] Chạy toàn bộ EditMode và PlayMode tests trong Unity Test Runner; lưu kết quả test mới.
- [ ] Chạy thử thủ công từ `00_Bootstrap` đến `90_TestSandbox`: di chuyển, tương tác, dialogue, choice và Result UI.
- [ ] Xác nhận `InputSystem_Actions` có binding ổn định cho di chuyển, camera và tương tác trên Windows/WebGL.
- [ ] Thêm validation khi mở scenario: kiểm tra node ID, `nextNodeId`, `targetAreaId`, `targetItemId` và choice branch bị thiếu.
- [ ] Bổ sung xử lý lỗi khi asset visual bắt buộc không tồn tại; không âm thầm thay bằng primitive trong bản production.
- [ ] Kiểm tra collider, layer, trigger và điểm spawn để người chơi luôn tiếp cận được các mục tiêu.
- [ ] Bổ sung NavMesh/AI Navigation bake cho khu vực có nhân vật hoặc loại bỏ dependency nếu NPC chỉ đứng tại chỗ.

## P1 — Hoàn thiện trải nghiệm gameplay

- [ ] Thay NPC capsule màu magenta bằng character prefab thật.
- [ ] Gắn Mecanim animations: idle, walk, talk và checkout gesture.
- [ ] Tách gameplay collider khỏi visual prefab bằng layer/prefab rõ ràng.
- [ ] Hiển thị objective active/completed nhất quán khi chuyển scene và khi scenario kết thúc.
- [ ] Hoàn thiện pause, restart scenario và quay lại menu; bảo đảm hủy event subscription đúng cách.
- [ ] Thêm feedback cho sai vật phẩm, sai lựa chọn và vùng đích chưa đúng.
- [ ] Thêm audio playback cho `voiceClip`, music/ambient và volume settings.
- [ ] Kiểm tra camera collision, cursor lock và trải nghiệm bàn phím/chuột.

## P1 — Nội dung học tập

- [ ] Rà soát toàn bộ Japanese, reading, romaji và bản dịch tiếng Việt với người duyệt ngôn ngữ.
- [ ] Đồng bộ learning tags trong scenario với kho grammar/vocabulary thực tế.
- [ ] Kiểm tra các lựa chọn đúng/sai có score modifier và lý do phù hợp.
- [ ] Hoàn thiện Learning Mode: GuidedPractice, Practice và Assessment; kiểm tra ẩn/hiện hint.
- [ ] Bổ sung voice clip native cho các dialogue quan trọng.
- [ ] Viết scenario Chapter 3: gọi món ramen.
- [ ] Viết scenario Chapter 4: mua vé và hỏi đường ở nhà ga.
- [ ] Tạo checklist QA nội dung cho mỗi scenario mới.

## P2 — Dữ liệu, lưu tiến độ và tài khoản

- [ ] Kiểm tra local save trên Windows và WebGL; xác nhận migration khi schema thay đổi.
- [ ] Thêm versioning/migration cho profile và scenario content.
- [ ] Bổ sung export/import profile phục vụ QA và lớp học.
- [ ] Thiết kế API contract cho `ScoreBreakdownDto`, progress và session token.
- [ ] Thay `LocalProgressRepository` bằng remote repository qua HTTPS.
- [ ] Kết nối Next.js portal với WebGL launch token.
- [ ] Xác thực token, quyền truy cập scenario và chống gửi điểm giả ở server.
- [ ] Đồng bộ kết quả scenario vào Supabase và xử lý offline/retry.

## P2 — Build, hiệu năng và phát hành

- [ ] Thiết lập build profile Windows và WebGL reproducible.
- [ ] Kiểm tra kích thước texture/model/audio và thời gian tải scene.
- [ ] Bổ sung loading screen, progress indicator và xử lý lỗi mạng.
- [ ] Kiểm tra WebGL với IndexedDB/persistent data path.
- [ ] Kiểm tra memory leak từ event, instantiated choice buttons và scene reload.
- [ ] Thiết lập CI chạy compile, tests và validation scenario.
- [ ] Chuẩn hóa license/attribution cho asset bên thứ ba trong bản phát hành.

## P3 — Công cụ authoring và quản trị nội dung

- [ ] Tạo editor validator hiển thị lỗi trực tiếp trên `ScenarioDefinition`.
- [ ] Tạo công cụ preview dialogue graph và kiểm tra node unreachable.
- [ ] Tạo template scenario cho Chapter 1–4.
- [ ] Cho phép designer thay visual prefab, trigger position và prompt bằng Inspector.
- [ ] Tạo data export/import để quản lý nội dung ngoài Unity khi cần.
- [ ] Viết hướng dẫn đóng góp content và quy trình review trong `CONTENT_GUIDE.md`.

## Definition of Done cho mỗi scenario

- [ ] Có `ScenarioDefinition` trong `Resources/Scenarios` với ID duy nhất.
- [ ] Có start node hợp lệ và mọi node đều reachable hoặc được đánh dấu có chủ đích.
- [ ] Mọi objective đều có đường hoàn thành và trạng thái hiển thị đúng.
- [ ] Mọi `CollectItem`, `InspectItem` và `GoToArea` trỏ tới component thật trong scene.
- [ ] Có dialogue/choice, bản dịch, reading/romaji và learning tags đã review.
- [ ] Có score modifier và kết quả success/fail có thể kiểm tra.
- [ ] Chạy được smoke test và manual playthrough từ đầu đến Result UI.
- [ ] Không có missing reference, compile error hoặc asset placeholder ngoài ý muốn.

## Quy ước cập nhật checklist

- Đánh dấu `[x]` chỉ sau khi đã kiểm chứng trong Unity hoặc test tương ứng.
- Mỗi mục lớn nên được tách thành issue/commit riêng.
- Khi một mục thay đổi phạm vi, cập nhật cả checklist và tài liệu liên quan.
- Không đánh dấu hoàn thành chỉ vì code đã viết; phải kiểm tra flow runtime.
