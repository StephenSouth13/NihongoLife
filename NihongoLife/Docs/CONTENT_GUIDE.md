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
