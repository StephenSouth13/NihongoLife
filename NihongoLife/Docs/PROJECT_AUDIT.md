# Kiểm tra tổng thể: scene, cốt truyện, gameplay (2026-09-21)

> **Cập nhật 2026-09-22**: **14/14 quest trong campaign đã viết sâu** (439 node), có bộ nhớ truyện (story flags), không còn quest nào dựng bằng code. Bảng "Cốt truyện" bên dưới là số liệu chụp nhanh buổi sáng 2026-09-21 và đã lỗi thời; xem `QUEST_AND_ONLINE_PLAN.md` mục 2 và `STORY_BIBLE.md` mục 12 cho trạng thái hiện tại (đúng nhất). Lưu ý thêm: đã tìm thấy một lỗi biên dịch (`WorldMapUI.cs`, không liên quan tới nội dung) trong commit gần nhất — xem báo cáo cuối cùng.

Cách kiểm tra: đọc trực tiếp YAML của 7 scene, chạy script kiểm tra đồ thị cho 11 scenario asset, đối chiếu code (`ScenarioManager`, `GameControlService`, `LocalScenarioRepository`, `SushiRestaurantRuntime`...), tài liệu (`STORY_BIBLE`, `SOCIAL_STORY_ROADMAP`, `PROJECT_STATUS_AND_SOURCES`, `TODO_CHECKLIST`) và `Editor.log`.
**Chưa chạy Play Mode**, nên mọi thứ dưới đây là kết quả kiểm tra tĩnh, không phải kết luận "chơi được".

**Kết luận ngắn: chưa hoàn chỉnh.** Hạ tầng (scene, cổng, menu, HUD, hệ thống hội thoại, nhà hàng) khá ổn; cốt truyện mới chạy trọn được ở phần đầu; nhiều chương chưa nối được vào thế giới thật.

## 1. Scene

| Scene | Build | Vai trò | Ghi chú |
|---|---|---|---|
| `00_Bootstrap` | bật | khởi tạo `AppRoot` | ổn |
| `01_MainMenu` | bật | menu + thành phố + camera quay | sương mù bật (0.0045), ambient Flat |
| `90_TestSandbox` | bật | thành phố chơi: người chơi, HUD, 5 NPC, cửa hàng | sương mù tắt, ambient Flat; 2 cổng vào zone |
| `20_StationDistrict` | bật (additive) | nhà ga | 0 NPC, 0 khu vực cốt truyện, không có đèn hướng |
| `30_SushiRestaurant` | bật (additive) | nhà hàng sushi | 0 NPC, 2 bàn phục vụ, bảng thực đơn |
| `99_ControlRoom`, `99_Trailer` | tắt | công cụ / trailer | không nằm trong build, đúng chủ đích |

**Đã đồng nhất (đã kiểm chứng bằng YAML):** 4/4 cặp cổng – điểm spawn khớp nhau (`sushi_entrance` ↔ `city_sushi_return`, `station_entrance` ↔ `city_station_return`), cùng một thành phố ở menu/sandbox/trailer, không còn `[MenuItem]` nào trong code, HUD chỉ ở sandbox và zone dùng lại HUD đó.

**Chưa đồng nhất / rủi ro:**
1. **Ánh sáng**: menu có sương mù, sandbox không; zone không có đèn hướng nên phụ thuộc scene đang active. Cần một quy chuẩn chung (giờ trong ngày, sương, ambient) và kiểm tra bằng mắt.
2. **Zone rỗng về cốt truyện**: nhà hàng và nhà ga không có NPC, không có `areaId`, không có `ScenarioSceneInitializer`. Scenario ở đó chỉ là hội thoại chạy ở bất kỳ đâu.
3. **Môi trường dựng lúc chạy** (trái `AGENTS.md`): `SushiRestaurantRuntime` sinh tường an toàn, bàn đầu bếp, rèm noren, đèn treo và một đầu bếp NPC lúc chạy; `RuntimeCollisionRepair`, `StoreLayoutStabilizer`, `StationMetroEnvironment` cùng kiểu. Phải ghi vào scene rồi bỏ đoạn sinh lúc chạy, nếu không sẽ chồng lên môi trường đã dựng.
4. **Đầu bếp trùng danh tính**: code dùng tên "Haruto" với 3 câu cứng, trong khi canon là Ota (`npc_sushi_chef`). Đã đổi tên hiển thị thành Ota; chưa nối với scenario.

## 2. Cốt truyện

**Nguồn canon**: `STORY_BIBLE.md` là cốt truyện (nhân vật chính là du học sinh Việt tại ひばり日本語学院, 4 chương). `SOCIAL_STORY_ROADMAP.md` là hệ thống tiến trình (4 Act, nghề, nhánh theo kiến thức). Hai tài liệu không mâu thuẫn nếu hiểu: **Bible = nội dung chính; Roadmap = cách mở khóa nhánh phụ**. Đã thêm bảng ánh xạ ở đầu roadmap.

**Đã sửa hôm nay**: danh sách campaign thật nằm trong `NihongoLifeControlDatabase.asset` (ghi đè giá trị mặc định trong code), nên `school.self_intro` và `restaurant.sushi_dining` từng **không** nằm trong campaign và `house1` bị xếp sau `konbini`. Thứ tự hiện tại: intro → street → house1 → school → konbini → lostcat → garbage → sushi_dining → cat_followup → recycling → evening_shift.

| Scenario | Ch | Node | Lựa chọn | Đánh giá |
|---|---|---|---|---|
| intro.arrival | 0 | 5 | 1 | mở màn, mỏng |
| street.first_talk | 1 | 4 | 2 | mỏng, 2 lựa chọn không có tag học |
| house1.greeting | 1 | 4 | 2 | mỏng, không tag |
| school.self_intro | 1 | 22 | 16 | đạt chuẩn Edu (5 lựa chọn sai cố ý để trống tag) |
| konbini.buy_onigiri | 2 | 14 | 8 | có gameplay thật (đi tới quầy, nhặt đồ); 8 lựa chọn không tag |
| house2.lostcat | 3 | 3 | 1 | rất mỏng |
| house3.garbage | 3 | 3 | 1 | rất mỏng |
| restaurant.sushi_dining | 3 | 74 | 61 | sâu nhất, đạt chuẩn Edu; chưa Play |
| restaurant.order_ramen | 3 | 13 | 11 | **không nằm trong campaign**; Yamada chưa có NPC |
| station.buy_ticket | 4 | 12 | 7 | **không nằm trong campaign**; cần `station_entrance` + NPC |
| town.summer_festival | 4 | 7 | 6 | **không nằm trong campaign**; cần ramen + station để đúng mạch |
| 3 scenario dựng bằng code (`BuiltInStoryScenarioCatalog`) | 4-6 | ~8 mỗi cái | | chạy được nhưng nằm trong code, chapter lệch bible |

**Vấn đề chính:**
- **Chiều sâu không đều**: 5 scenario chỉ 3-5 node và 1-2 lựa chọn, không đạt Edu Standard (thiếu nhánh sai → sửa → thử lại, thiếu tag mastery, thiếu tóm tắt). Trái ưu tiên "ít nhưng sâu".
- **Ba scenario đã viết chưa chơi được trong campaign** (ramen, station, festival), và lễ hội nhắc Yamada/Sato/Suzuki: cần ramen vào campaign trước để không lệch mạch.
- **NPC thiếu trong scene**: có `npc_neighbor_1/2/3`, `npc_cashier`, `npc_guide` (không scenario nào dùng); thiếu `npc_teacher_morita`, `npc_classmate_kim`, `npc_sushi_staff` (Aoki), `npc_sushi_chef` (Ota), `npc_ramen_owner`, `npc_station_staff`.
- **Khu vực thiếu**: scenario dùng `ramen_shop_entrance`, `station_entrance` nhưng scene chỉ có `store_entrance` và `cashier`; đưa station/ramen vào campaign lúc này sẽ **kẹt ở node GoToArea**.
- **Nhánh chưa cấu hình**: mọi scenario để `branchId = main`, chưa có `requiredKnowledge`/tiên quyết như roadmap mô tả.
- **Chưa có lớp trường học** (không có zone; cần bạn duyệt đúng scene mới theo `AGENTS.md`).
- Tiếng Nhật, furigana, dịch chưa được người bản ngữ duyệt; chưa có giọng đọc.

## 3. Gameplay

| Hệ thống | Tình trạng |
|---|---|
| Di chuyển, camera, tương tác, hội thoại, chấm điểm | có; cần Play QA (chuột/camera, giật) |
| HUD: chỉ số, thông báo, responsive | ghi cứng vào scene; chưa Play |
| Menu, cài đặt, Về tôi, Cách chơi, âm thanh menu | mới cập nhật; chưa Play |
| Cửa hàng tiện lợi + mua đồ | có; vật phẩm/quầy đã ghép, cần QA |
| Nhà hàng: gọi món, phục vụ, ăn, trả tiền, chặn ra cửa khi chưa trả | có; chưa Play; chưa có nhân vật phục vụ đi lại |
| Sinh tồn: máu, năng lượng, no, khát | có; **chưa có WC** và chưa kích hoạt hội thoại theo chỉ số |
| Việc làm / nhà ga (`JobInteractable`, `StationTravelController`) | có nền tảng; chưa nối cốt truyện |
| Online (Supabase, chat, bạn bè, co-op) | có nền tảng, chưa QA đủ để phát hành |
| Nhạc trong game, giọng đọc, animation NPC/đầu bếp, chiến đấu, trồng trọt | thiếu hoặc rất sơ khai |
| Test tự động | chỉ có test engine scenario (EditMode) và smoke test PlayMode cho luồng cửa hàng; chưa chạy lại |

Ước lượng của `PROJECT_STATUS_AND_SOURCES.md` (nền tảng 60-65%, cốt truyện 3 giờ 30-35%, sẵn sàng phát hành web/mobile 25-30%) vẫn hợp lý sau kiểm tra này.

## 4. Việc cần làm để đạt "cốt truyện + gameplay hoàn chỉnh" (theo ưu tiên)

**P0 – chặn việc gọi là chơi được trọn vẹn**
1. Play thử toàn bộ campaign theo thứ tự trên trong Unity, ghi lỗi từng scenario. *(Codex/bạn)*
2. Đặt NPC vào scene và khai báo khu vực: Aoki, Ota, Yamada (quán ramen), nhân viên ga, cô Morita/Kim (khi có lớp học), rồi mới đưa ramen/station/festival vào campaign. *(Codex dựng scene, Claude kiểm graph)*
3. Ghi môi trường sinh lúc chạy vào scene và bỏ các script sinh (`SushiRestaurantRuntime`, `StationMetroEnvironment`, patcher va chạm). *(Codex)*

**P1 – chiều sâu và nhất quán**
4. Viết lại 5 scenario mỏng theo Edu Standard (nhánh sai → sửa → thử lại, tag, tóm tắt) và thêm tag cho konbini/ramen/station/festival. *(Claude)*
5. Chuyển 3 scenario dựng bằng code thành asset, sửa chapter theo bible, thêm `branchId`/`requiredKnowledge`. *(Claude)*
6. Quy chuẩn ánh sáng chung (menu ↔ sandbox ↔ zone). *(Codex)*
7. Chỉ số sinh tồn kích hoạt hội thoại (đói, khát, WC). *(Claude thiết kế, Codex code)*

**P2**
8. Giọng đọc, nhạc trong game, animation phục vụ/đầu bếp, lớp học (cần bạn duyệt scene). 9. Build WebGL và test thiết bị. 10. Người bản ngữ duyệt tiếng Nhật.
