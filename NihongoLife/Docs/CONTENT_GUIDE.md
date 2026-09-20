# Content Creation Guide

This document details how to author new Japanese learning scenarios for Nihongo Life.

## Adding a New Scenario Asset

1. In the Unity Project window, navigate to a resource folder (recommended: `Assets/NihongoLife/Resources/Scenarios/`).
2. Right-click and choose **Create -> NihongoLife -> Scenario Definition**.
3. Set the `id` field using a dot-notation namespace (e.g. `scenario.restaurant.order_ramen`).
4. Fill in the title, description, and chapter metadata.

## Configured Curriculum Guidelines

Keep scenarios focused on practical everyday-life interactions:
- **Chapter 1 — Greetings & Introductions**: `Chapter 1 — はじめまして`
- **Chapter 2 — Store Transactions**: `Chapter 2 — コンビニ` (Buy items, handle prompts about receipt, bags, points card).
- **Chapter 3 — Diners & Restaurants**: `Chapter 3 — レストラン` (Order food, choose table types, requests).
- **Chapter 4 — Transit & Station Navigation**: `Chapter 4 — 駅` (Ask directions, buy tickets).

## Writing Dialogue Branches

When creating Dialogue choices:
- Each option should represent a plausible learner response.
- Assign **Score Event Modifiers** to reward grammatically correct or high-politeness responses (e.g. `Grammar +10` for using polite forms, `ResponseAccuracy -5` for choosing inappropriate/casual responses in a formal context).
- Attach **Grammar/Vocabulary Tags** (e.g. `grammar.n5.wo_kudasai`) to update specific learning mastery levels on successful completion.

## Edu Dialogue Standard (bắt buộc cho mọi scenario học)

Chuẩn để mọi bài hội thoại đều "dạy được", không chỉ chạy được. Scenario nào chưa đạt thì ghi vào backlog chuẩn hoá, không tick hoàn thành.

1. **Mỗi node có đủ 4 lớp chữ**: `textJa` (kanji thật ở mức N5, không kanji ngoài N5 mà không kèm đọc), `textReading` (hiragana đầy đủ), `textRomaji`, `textEn` (bản dịch Việt — field tên cũ là `textEn`). Không để trống reading/romaji ở node có lời thoại.
2. **Mỗi câu tiếng Nhật của người học** (`DialogueChoice.textJa`) đều có `grammarTags` và/hoặc `vocabularyTags` khớp kho id (`grammar.n5.*`, `vocab.n5.*`). Lựa chọn đúng và lựa chọn "kém tự nhiên" bắt buộc có tag; lựa chọn **sai có chủ đích thì để trống tag** (không cộng độ thành thạo cho câu sai).
3. **Mỗi lượt có 3 loại lựa chọn**: (a) đúng và đủ lịch sự, (b) đúng ngữ pháp nhưng kém tự nhiên/kém lịch sự hơn (điểm thấp hơn, NPC vẫn chấp nhận và gợi ý bản tốt hơn), (c) sai có chủ đích (lỗi điển hình của người Việt: nhầm trợ từ `に/から`, thiếu `です`, dùng thể thường với thầy cô, chào sai buổi).
4. **Lựa chọn sai không kết thúc bài**: dẫn tới node phản hồi của NPC **giải thích vì sao sai bằng lời tự nhiên** (NPC sửa nhẹ, khen trước rồi sửa), sau đó quay lại đúng node hỏi để thử lại (recovery path). Node phản hồi có `animationCue` phù hợp.
5. **`scoreModifiers` luôn có `reason` cụ thể** (nêu mẫu câu, không ghi chung chung "đúng/sai") vì `reason` hiển thị trong màn kết quả.
6. **Node cuối phải tổng kết bài học**: 1 câu NPC nhắc lại mẫu câu vừa học (đúng vai, không phá immersion) để tăng ghi nhớ.
7. **Nhất quán thế giới**: `speakerId`/tên NPC theo `STORY_BIBLE.md` mục 3; địa danh theo 1.1; không đặt lời thoại cho nhân vật chính; ký hiệu ［なまえ］ thay cho tên riêng của người chơi.
8. **Mức độ khó tăng dần theo chapter**: chapter sau được dùng lại ngữ pháp chapter trước như "kiến thức nền" (callback), không giới thiệu quá 2 mẫu ngữ pháp mới trong 1 scenario.
9. **Định nghĩa xong (DoD)**: chạy hết mọi nhánh (đúng, kém, sai → recovery) trong Play Mode, đọc lại toàn bộ chữ Nhật với người biết tiếng Nhật, không có `nextNodeId` treo, mọi objective có đường hoàn thành.

## Restaurant content pipeline (thực đơn, đặc sản, hội thoại quán ăn)

Toàn bộ thông tin món ăn nằm trong dữ liệu, không hard-code trong UI. Sửa/mở rộng **không cần đụng code, không cần menu Editor**.

**Các phần**
| Phần | File | Vai trò |
|---|---|---|
| Thực đơn | `Assets/NihongoLife/Resources/Restaurants/<id>.asset` (`RestaurantMenuDefinition`) | Danh mục, món (giá, nguyên liệu, dị ứng, cách ăn, đặc sản, chỉ số no/khát, model bày bàn), cụm từ, nghi thức, lời thoại nhân viên |
| Bảng thực đơn trên tường | GameObject `RestaurantMenuBoard_Sushi` (component `RestaurantMenuBoard`) | Chỉ **xem** (tra cứu món, đặc sản, cụm từ, nghi thức); không gọi món ở đây |
| Bàn ăn | `DiningTableService_A/B` trong `30_SushiRestaurant.unity` (component `RestaurantTable`, trigger + con `ServeAnchor`) | Gọi món, phục vụ tại bàn, ăn, thanh toán |
| Giao diện | `RestaurantMenuUI` (dựng lúc chạy, dùng chung mọi quán) | Tab: theo danh mục / 名物 / 会話 / マナー, và tab 注文 khi mở từ bàn; thanh phụ đề lời nhân viên |
| Hội thoại | `Resources/Scenarios/scenario_restaurant_sushi_dining.asset` | Nhánh hội thoại kiểu bài học (đúng / kém tự nhiên / sai → sửa → thử lại) |

**Vòng gọi món tại bàn** (`RestaurantTable`): E ở bàn → mở thực đơn chế độ gọi món (nút - / + chọn số lượng; tối đa 6 món khác nhau, 9 phần/món, 20 phần/bàn) → tab 注文 hiện câu người chơi "nói" (「すみません、まぐろを二つ、お願いします。」, số đếm ひとつ/ふたつ/みっつ...) và tổng tiền đọc bằng chữ Nhật → bấm 注文する → nhân viên đọc lại đơn (phụ đề Nhật + furigana + dịch) → chờ vài giây → món hiện trên bàn (model từ asset) → E "いただきます" (hồi no/khát theo `hungerRestore`/`thirstRestore`, đĩa biến mất dần) → E "お会計" (trừ tiền, nhân viên đọc tổng tiền bằng số Nhật, ví dụ 千百八十円). Không đủ tiền thì chặn ngay lúc gọi món. Thanh toán sau khi ăn đúng phong tục Nhật.

**Thêm/sửa món**: mở `menu_sushi_hibari.asset` trong Inspector → thêm phần tử vào `dishes` (id duy nhất, `categoryId` khớp `categories`). Bắt buộc điền `nameJa`, `reading`, `romaji`, `nameVi`, `priceYen`; đặc sản đặt `isSpecialty` + `specialtyNoteVi`. Để món dùng được ở bàn ăn, điền thêm `hungerRestore`/`thirstRestore` (thang 0-100, mỗi phần), `servingVi` (một phần gồm gì), và tuỳ chọn `servedModel` + `servedModelSize` (mét, cạnh dài nhất; code tự co giãn theo kích thước này nên không phụ thuộc scale import FBX) + `servedModelEuler` (model của Sushi Restaurant Kit nằm ngang khi import nên dùng -90, 0, 0), `servedOnPlate`. `vocabularyTag` phải là id có thật (`vocab.n5.*`) hoặc để trống. Thêm cụm từ vào `phrases`, nghi thức vào `etiquette`. Lời nhân viên chỉnh ở `serviceLines` (khoá `order_confirm` dùng `{order}`, `served`, `bill` dùng `{total}`); tên nhân viên ở `staffNameJa`. Đĩa dùng chung: `plateModel`, `plateModelSize`.

**Thêm quán mới**: nhân đôi asset thực đơn (Ctrl+D) trong `Resources/Restaurants/`, đổi `id`/tên/món; trong scene của quán đó đặt 1 GameObject có `RestaurantMenuBoard` + Collider trigger (bảng xem) và mỗi bàn 1 GameObject có `RestaurantTable` + Collider trigger + 1 con `ServeAnchor` đặt ngang mặt bàn; đổi `menuResourcePath` sang asset mới. Không tạo thêm `[MenuItem]` hay script cài đặt.

**Giữ đồng bộ với scenario**: giá và tên món nói trong hội thoại (`scenario_restaurant_sushi_dining`) phải khớp asset thực đơn (ví dụ ひばりセット ¥1,180, うなぎ ¥380). Đổi giá trong asset thì rà lại các node có giá tiền.

**Mẫu câu quán ăn bắt buộc có trong mỗi bài nhà hàng**: いらっしゃいませ / 何名様ですか / 〜をください / 〜をお願いします / おすすめは何ですか / お会計をお願いします / いただきます / ごちそうさまでした. Người chơi **không** phải tự nói kính ngữ của nhân viên (かしこまりました…), chỉ cần nghe hiểu.

**Kiểm tra trước khi coi là xong**: chạy Play Mode: bấm E ở bảng thực đơn (mở/đóng bằng ESC, không kẹt khoá di chuyển); ở bàn ăn chạy hết vòng gọi món → chờ → món hiện đúng chỗ, đúng kích thước → ăn (thanh no/khát tăng) → thanh toán (ví trừ đúng); thử không đủ tiền; chơi hết các nhánh scenario; đọc lại tiếng Nhật với người bản ngữ (chưa được review).
