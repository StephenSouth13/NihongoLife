# Kế hoạch: nhiệm vụ hoàn chỉnh và chế độ Online

Mục tiêu: mọi phần của game là một **nhiệm vụ (quest)** có bối cảnh, mục tiêu, phần thưởng, điều kiện mở khóa, địa điểm và bài học rõ ràng; và mỗi quest phù hợp đều chơi được **cùng bạn bè (co-op)**.
Tài liệu bổ sung cho `STORY_BIBLE.md` (nội dung) và `SOCIAL_STORY_ROADMAP.md` (nhánh, nghề). Hiện trạng kiểm tra: `PROJECT_AUDIT.md`.

## 1. Khung nhiệm vụ (đã có trong code, chưa Play)

Mỗi `ScenarioDefinition` giờ là một quest, các trường mới:

| Trường | Ý nghĩa |
|---|---|
| `questType` | `main`, `side`, `community`, `career`, `coop` (nhãn trong nhật ký) |
| `giverNpcId` | NPC giao nhiệm vụ |
| `briefingVi/En/Ja` | 1-2 câu bối cảnh, vì sao bạn làm việc này |
| `locationHintVi/En/Ja` | đi đâu để bắt đầu |
| `rewardYen` | tiền thưởng lần hoàn thành đầu (thêm vào `baseKnowledgeReward` và XP có sẵn) |
| `branchId`, `requiredKnowledge`, `requiredScenarioIds`, `unlockScenarioIds`, `repeatable` | mở khóa, đã có từ trước, nay đã được điền cho cả 14 quest |
| `supportsCoOp`, `minPlayers`, `maxPlayers` | quest có chơi cùng bạn được không |

- **Nhật ký nhiệm vụ** (`QuestLogPopup`, phím **J**): 3 tab Đang làm / Có thể nhận / Đã xong; xem bối cảnh, mục tiêu (đánh dấu ○ ◐ ● ✕), người giao, địa điểm, phần thưởng, điều kiện, bài sẽ học; bấm **Bắt đầu nhiệm vụ** hoặc **Chỉ đường**. Quest mới thêm asset là tự xuất hiện.
- **Phần thưởng** áp dụng lúc hoàn thành lần đầu (tiền + kiến thức + XP) và hiện ở màn kết quả.
- **Điều kiện mở khóa** phản chiếu đúng thứ tự campaign để nhật ký không mâu thuẫn với cốt truyện.

## 2. Danh mục nhiệm vụ

**Đã có (14)**: intro.arrival, street.first_talk (đã viết lại đủ chuẩn Edu: 28 node), house1.greeting, school.self_intro, konbini.buy_onigiri, house2.lostcat, house3.garbage, restaurant.sushi_dining, restaurant.order_ramen, neighborhood.cat_followup, neighborhood.recycling_morning, konbini.evening_shift, station.buy_ticket, town.summer_festival.

**Còn phải viết lại theo chuẩn Edu** (đang 3-5 node): intro.arrival, house1.greeting, house2.lostcat, house3.garbage; và thêm tag học cho konbini, ramen, station, festival.

**Nhiệm vụ mới cần viết** (mỗi cái: nhiều nhánh, sai → sửa → thử lại, ≤2 mẫu ngữ pháp mới):

| Nhóm | Quest | Bài học chính | Cần dựng trong scene |
|---|---|---|---|
| Trường (main) | school.numbers_prices, school.time_schedule, school.directions, school.test_day | số, giờ, chỉ đường, kiểm tra cuối | lớp học (cần bạn duyệt scene) |
| Sinh tồn | survival.hungry, survival.thirsty, survival.tired_home, survival.sick_clinic | nói khi đói/khát/mệt/ốm, khám bệnh | kích hoạt theo chỉ số; phòng khám |
| Cộng đồng | community.lost_wallet (交番), community.tourist_help, community.neighbor_gift | khai báo mất đồ, chỉ đường, tặng quà/おすそわけ | đồn công an nhỏ, NPC du khách |
| Nghề | career.restaurant_waiter, career.station_assistant, career.school_ta | phục vụ, thông báo ga, hỗ trợ lớp | Aoki/Ota, Kimura, Morita |
| Cuối chương | town.festival_prep (chuẩn bị lễ hội) trước summer_festival | mượn, nhờ vả, phân công | quầy lễ hội |
| Co-op | coop.restaurant_pair, coop.shopping_list, coop.festival_booth | đổi vai khách ↔ nhân viên, cùng mua sắm, cùng bán hàng | xem mục 3 |

**Quy tắc thưởng** (để kinh tế không lệch): main 200-400¥, cộng đồng 200-400¥, nghề 800-1500¥/ca, cuối chương 1000¥; quest tiêu tiền (nhà hàng) thưởng 0¥ nhưng cho nhiều kiến thức; làm lại chỉ nhận 25% kiến thức, không tiền.

## 3. Chế độ Online

### 3.1 Hiện trạng (đã kiểm tra thực tế trên dự án Supabase `nihongolife`)
- Có đủ 8 bảng (`profiles`, `player_progress`, `scenario_scores`, `chat_messages`, `coop_sessions`, `coop_participants`, `friendships`, `leaderboard`), RLS bật, cảnh báo bảo mật duy nhất: chưa bật kiểm tra mật khẩu bị lộ (Dashboard).
- **Tất cả bảng đều 0 dòng**: chưa có ai từng đăng nhập/tạo phiên co-op, nên online **chưa từng được chạy thật**. Việc chặn: bật Anonymous Sign-Ins và tắt Confirm email trong Supabase Dashboard.
- Code có `CoopSessionService`, `CoopLobbyUI`, `FriendService`, `LeaderboardService`, chat Realtime.
- **Lỗi đã sửa hôm nay**: `CoopScenarioController` chưa từng được tạo và luật "ai được chọn" dựa trên `speakerId` (luôn là NPC) nên không ai chọn được; khi đối tác chọn thì node bị chuyển 2-3 lần.

### 3.2 Co-op theo lượt (đã nối, chưa test 2 người)
- Cả hai người cùng thấy một quest. Người tạo phòng (host) chọn câu trả lời ở lượt đầu, sau đó luân phiên. Mỗi lựa chọn và hậu quả (đúng/kém/sai → NPC sửa) hiện cho cả hai, nên cả hai cùng luyện một mẫu câu.
- Chip thông báo trong HUD báo "Đến lượt bạn" / "Đang chờ bạn cùng chơi" (`HudNotificationTray`).
- Lobby tự chọn quest co-op phù hợp mà người chơi đã mở khóa. Hiện bật co-op cho: school.self_intro, restaurant.sushi_dining, town.summer_festival (chỉ hội thoại, không có bước đi tới khu vực nên không lệch giữa hai máy).
- Điểm số/thành thạo: người chọn được ghi; người xem chưa được ghi (việc tiếp theo).

### 3.3 Lộ trình
| Giai đoạn | Nội dung | Ai |
|---|---|---|
| O1 Nền tảng chạy thật | bật đăng nhập ẩn danh, tạo hồ sơ tự động, lưu tiến độ và điểm lên cloud, bảng xếp hạng cập nhật khi hoàn thành quest, chat thị trấn | bạn (Dashboard) + Codex kiểm |
| O2 Co-op theo lượt | test 2 máy: tạo phòng, vào phòng, đồng bộ node, lượt, kết thúc; ghi điểm cho cả hai | Codex test, Claude sửa nếu lỗi logic |
| O3 Co-op đổi vai | quest có phía "nhân viên" (khách ↔ nhân viên nhà hàng, người mua ↔ thu ngân): thêm `coopRole` và lựa chọn phía nhân viên vào dữ liệu | Claude thiết kế/viết, Codex code |
| O4 Thế giới chung | thấy người chơi khác trong thị trấn, kết bạn, mời vào phòng, chat theo phòng; báo cáo/tắt tiếng; không PvP | Codex |
| O5 Sự kiện | quest theo ngày/tuần từ server (bảng `quest_events`), xếp hạng tuần, chống gian lận điểm bằng Edge Function | Claude thiết kế bảng, Codex làm |

Nguyên tắc an toàn: không PvP, chat có bộ lọc và nút báo cáo, chỉ hiển thị tên hiển thị, không thu thập thông tin cá nhân, điểm số cuối cùng do server xác nhận (O5).

## 4. Thứ tự làm tiếp

1. **Play thử khung quest + nhật ký J + quest street mới** (Codex/bạn) rồi sửa lỗi.
2. Viết lại 4 quest mỏng theo chuẩn Edu (Claude): intro → house1 → lostcat → garbage.
3. O1 + O2 chạy thật với 2 máy (bạn bật Dashboard, Codex test).
4. Dựng NPC/khu vực còn thiếu (TASK-I) rồi đưa ramen, ga, lễ hội vào campaign.
5. Quest sinh tồn + cộng đồng mới (Claude), rồi co-op đổi vai (O3).
