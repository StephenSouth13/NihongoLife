# Danh mục sơ đồ BA

| Mã | Sơ đồ | Nhóm | Mục đích BA | File |
|---|---|---|---|---|
| D01 | Core gameplay loop | Business flow | Giải thích vòng lặp học — làm nhiệm vụ — nhận phản hồi — tiến bộ | `02_Business_and_User_Flows/d01_core_loop.png` |
| D11 | World hub | User/navigation flow | Xác định các khu vực, điểm đến và quan hệ điều hướng | `02_Business_and_User_Flows/d11_world_hub.png` |
| D12 | Use case | Business requirements | Xác định actor và nhóm chức năng chính | `02_Business_and_User_Flows/d12_use_case.png` |
| D08 | Layered architecture | System context | Liên kết UI/gameplay/data/online service | `03_System_Architecture/d08_architecture.png` |
| D13 | Class diagram | Logical design | Tham chiếu quan hệ lớp lõi phục vụ impact analysis | `03_System_Architecture/d13_class.png` |
| D18 | Supabase ERD | Data model | Tham chiếu entity, quan hệ và dữ liệu online | `04_Data_Model/d18_erd.png` |
| D14 | Dialogue sequence | Interaction flow | Mô tả chuỗi xử lý bài học hội thoại | `05_Sequence_and_State/d14_seq_dialogue.png` |
| D16 | Mini-game sequence | Interaction flow | Mô tả chuỗi xử lý Kana Match/mini-game | `05_Sequence_and_State/d16_seq_minigame.png` |
| D19 | Stamina state | State model | Mô tả trạng thái chạy, cạn thể lực và hồi phục | `05_Sequence_and_State/d19_state_stamina.png` |
| D22 | Deployment | Operations | Mô tả Unity client và các dịch vụ ngoài | `06_Delivery_and_Operations/d22_deployment.png` |
| D24 | Development workflow | Delivery process | Mô tả luồng phát triển, kiểm thử và bàn giao | `06_Delivery_and_Operations/d24_dev_workflow.png` |

## Quy ước sử dụng

- Sơ đồ là bản chụp trạng thái tài liệu tại ngày 2026-10-10.
- Khi thay đổi use case, entity hoặc luồng tích hợp, cập nhật cả sơ đồ nguồn và bản sao trong BA pack.
- ERD mô tả phần online; dữ liệu local/offline vẫn cần đối chiếu với save model trong Unity trước khi chốt yêu cầu migration.
