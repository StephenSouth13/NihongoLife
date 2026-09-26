# Chuẩn Cấu Trúc JSON Kịch Bản (Scenario) NihongoLife

Để Import thành công Nhiệm vụ (Quest) / Kịch bản hội thoại vào Game thông qua tool `NihongoLife -> Scenario -> Import Scenario from JSON`, bạn cần yêu cầu AI (ChatGPT/Claude) xuất kịch bản theo ĐÚNG định dạng JSON sau.

### Quy ước ScenarioNodeType (Số nguyên)
- `0`: Dialogue (Hội thoại)
- `1`: CollectItem (Nhặt đồ)
- `2`: InspectItem (Tương tác đồ vật)
- `3`: GoToArea (Đi đến khu vực)
- `4`: Complete (Hoàn thành nhiệm vụ)
- `5`: Fail (Thất bại)
- `6`: TalkToNPC (Bắt chuyện NPC)
- `7`: Branch (Rẽ nhánh điều kiện)

### Mẫu JSON Prompt cho AI
Gửi Prompt sau cho AI:
*"Đóng vai đạo diễn game, hãy viết cho tôi 1 nhiệm vụ đi mua bánh mỳ tại cửa hàng tiện lợi. Yêu cầu vận dụng ngữ pháp N5 (Te-form) khi nhờ vả. Xuất ra định dạng JSON đúng chuẩn sau đây. Giữ nguyên cấu trúc các key."*

```json
{
  "id": "scenario_buy_bread",
  "titleJa": "パンを買う",
  "titleEn": "Buy Bread",
  "descriptionJa": "コンビニでパンを買ってみましょう。",
  "descriptionEn": "Go to the convenience store and buy bread.",
  "chapterIndex": 1,
  "requiredKnowledge": 100,
  "questType": "main",
  "giverNpcId": "npc_mom",
  "rewardYen": 500,
  "rewardItems": [
    "item_bread",
    "item_coffee"
  ],
  "learningDifficulty": 2,
  "baseKnowledgeReward": 60,
  "learningTargets": [
    "grammar.n5.kudasai"
  ],
  "objectives": [
    {
      "id": "obj_talk_clerk",
      "titleJa": "店員と話す",
      "titleEn": "Talk to the clerk",
      "isOptional": false
    }
  ],
  "startNodeId": "node_intro",
  "nodes": [
    {
      "id": "node_intro",
      "nodeType": 0,
      "speakerName": "Clerk",
      "textJa": "いらっしゃいませ！",
      "textEn": "Welcome!",
      "nextNodeId": "",
      "choices": [
        {
          "textJa": "パンをください",
          "textEn": "Bread, please.",
          "nextNodeId": "node_complete",
          "scoreModifiers": [
            {
              "category": "Grammar",
              "value": 15,
              "reason": "Correct use of 'kudasai'"
            }
          ]
        },
        {
          "textJa": "パン。",
          "textEn": "Bread.",
          "nextNodeId": "node_fail",
          "scoreModifiers": [
            {
              "category": "Politeness",
              "value": -10,
              "reason": "Too casual/rude"
            }
          ]
        }
      ]
    },
    {
      "id": "node_complete",
      "nodeType": 4,
      "objectiveIdToComplete": "obj_talk_clerk"
    },
    {
      "id": "node_fail",
      "nodeType": 5
    }
  ]
}
```
