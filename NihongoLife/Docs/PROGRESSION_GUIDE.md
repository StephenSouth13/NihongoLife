# Tiến trình, nhiệm vụ và việc làm thêm — hướng dẫn chỉnh sửa

**Một file duy nhất:** `Assets/NihongoLife/Resources/Progression/progression.json`.
Mọi nhiệm vụ, việc làm thêm, phần thưởng (¥ / XP / kiến thức) và ngưỡng lên cấp đều nằm trong file này. Code không viết cứng con số nào.

Sửa xong thì chạy test EditMode `ProgressionCatalogTests`. Test báo ngay nếu dữ liệu sai: trùng id, sự kiện không hỗ trợ, thiếu chữ Việt/Anh, thưởng âm, phụ thuộc vòng, vật phẩm không tồn tại, ngưỡng cấp ≤ 0.

## Đổi phần thưởng hoặc giá trị

```json
"rewards": { "yen": 600, "xp": 40, "knowledge": 6 }
```

- `yen` cộng vào ví chung (`PlayerInventory`). Không có ví thứ hai.
- `xp` cộng vào cấp độ.
- `knowledge` cộng vào kiến thức, là chỉ số riêng, không gộp với XP.

## Đổi ngưỡng lên cấp

```json
"levels": { "xpToNext": [100, 160, 240, ...] }
```

- Phần tử thứ *i* là số XP cần để đi từ cấp *i+1* lên cấp *i+2*.
- Bản lưu giữ **tổng XP** (`progress.xp`); cấp độ được tính lại từ con số đó.
- Hết mảng thì bước cuối cùng được lặp lại.

## Thêm một nhiệm vụ mới

Thêm một phần tử vào `quests`:

```json
{
  "id": "learn_konbini_words", "category": "learning",
  "titleVi": "...", "titleEn": "...", "titleJa": "...", "descriptionVi": "...", "descriptionEn": "...",
  "giverNpc": "", "giverNameVi": "Tự học", "locationVi": "ひばりマート", "scene": "90_TestSandbox",
  "minLevel": 1, "minKnowledge": 0, "requiresQuests": [],
  "repeatable": false, "cooldownMinutes": 0, "grantOnAccept": [],
  "objectives": [
    { "id": "buy", "event": "buy", "target": "konbini", "count": 3, "afterAll": false, "textVi": "Mua 3 món ở konbini", "textEn": "Buy 3 items" }
  ],
  "rewards": { "yen": 0, "xp": 20, "knowledge": 5 }
}
```

Nhiệm vụ tự xuất hiện trong Sổ nhiệm vụ (phím **N**) và trên ô theo dõi ở HUD. Không cần sửa script nào.

**Các trường:**

| Trường | Ý nghĩa |
|---|---|
| `category` | `job` · `farm` · `learning` · `daily` · `story` |
| `requiresQuests` | id các nhiệm vụ phải xong trước (không được tạo vòng) |
| `repeatable` + `cooldownMinutes` | làm lại sau bao nhiêu phút (ca làm, việc hằng ngày) |
| `grantOnAccept` | đồ được phát khi nhận việc, ví dụ hạt giống cho ca nông trại |
| `afterAll: true` | chỉ tính khi các mục khác đã xong, thường dùng cho "báo cáo với chủ" (`event: talk`, `target: <npcId>`) |
| `target` | id cụ thể hoặc `"*"` cho bất kỳ |

## Sự kiện có sẵn (`event`) và nơi phát ra

| event | target | Phát từ |
|---|---|---|
| `till` `plant` `water` `harvest` | id ô ruộng / cây | `Island/FarmPlot.cs` |
| `feed` `pet` | id con vật | `Island/IslandAnimal.cs` |
| `buy` | id món, `konbini`, `midori_store` | `UI/KonbiniShopUI.cs`, `Island/IslandGameplay.cs` |
| `sell` | id nông sản | `Island/IslandGameplay.cs` |
| `talk` | npcId | `NPC/NPCController.cs`, `Progression/JobGiver.cs` |
| `learn` | khoá từ vựng | `Island/IslandData.cs` (`IslandState.Discover`) |
| `restock` `assist` `checkout` | `konbini_shelf` / `konbini_customer` / `konbini_register` | `Progression/JobStation.cs` |
| `order` `serve` | `sushi_table` | `Progression/JobStation.cs` |

Nếu cần một hành động mới, gọi `QuestService.Raise("<event>", "<target>")` ở đúng chỗ hành động hoàn thành, rồi thêm tên event vào `ProgressionCatalog.SupportedEvents`.

## Việc làm thêm (job)

**Một ca job gồm:**
- một bảng tuyển (`JobStation` loại `Board`) để mở đúng việc trong sổ nhiệm vụ;
- các trạm làm việc trong scene;
- mục cuối cùng là báo cáo với chủ để nhận lương.

**Đặt trạm vào scene:**
- Konbini và nhà hàng sushi: chạy `Unity -batchmode -executeMethod NihongoLife.EditorTools.JobSiteBuilder.Build`.
- Đảo Xanh: Hana và bảng tuyển được đặt khi chạy `IslandBuilder.Build`.

**Nguyên tắc:**
- Trạm chỉ tương tác được khi ca đang chạy và bước đó còn dang dở.
- Trả lời sai không được tính.
- Lương trả đúng một lần: trạng thái được đánh dấu và lưu trước, rồi mới trả tiền (`QuestStateRecord.rewardClaimed`).
- Mỗi lúc chỉ làm một ca. Huỷ ca thì không có lương; đồ đã phát vẫn giữ.

## Lưu ý

- **Giá hàng nằm chỗ khác:**
  - giá konbini nằm trong `Shop/KonbiniCatalog.cs`;
  - giá nông sản và hạt giống trên Đảo Xanh nằm trong `Resources/Island/island_catalog.json`.
- **Bản lưu:** trạng thái nhiệm vụ của người chơi nằm trong `PlayerProgressDto.quests`, tách khỏi định nghĩa. Cloud sync dùng chung `progress_json`. Chưa kiểm thử gộp bản lưu online/offline cho trường `quests`.
