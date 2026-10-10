"""Content additions/patches layered on top of the base report text (report-content.json)."""

# Figures per section index: (file, caption, width_inches)
FIGS = {
    2: [("shots/g01_gameplay.png", "Khu phố Hibari-chō trong gameplay — HUD, nhắc tương tác [F] và nhân vật người chơi", 6.3)],
    4: [("diag/d10_zones.png", "Sáu khu vực chơi được: thành phố, siêu thị, phòng trọ, ga tàu, nhà hàng sushi và lớp học", 6.3)],
    6: [("diag/d01_core_loop.png", "Vòng lặp cốt lõi: khám phá → mục tiêu → tương tác → chọn câu → sửa lỗi → tiến bộ", 6.0)],
    7: [("diag/d02_screen_map.png", "Bản đồ màn hình: từ Bootstrap tới Menu, chọn nhân vật, HUD và các popup", 6.3)],
    8: [("shots/01_menu.png", "Menu chính — Bắt đầu, Thoát, đổi ngôn ngữ VI/EN/JP, Xếp hạng, Cách chơi, Về tôi, Bạn bè, Hồ sơ, Online", 6.0),
        ("shots/02_guide.png", "Popup “Cách chơi” — 4 bước bắt đầu và bảng phím tắt", 6.0),
        ("shots/m02_about.png", "Popup “Về tôi” — thông tin tác giả, thư viện ảnh và nguồn tài nguyên", 6.0),
        ("diag/d03_menu_flow.png", "Workflow khởi động → Menu → Chọn nhân vật → Vào game", 6.3)],
    9: [("shots/g09_dialogue_choice.png", "Hội thoại với Tanaka: câu chào buổi sáng và ba lựa chọn trả lời N5", 6.0),
        ("diag/d04_dialogue_flow.png", "Workflow hội thoại học tập, gồm nhánh sai và đường phục hồi", 6.3)],
    10: [("shots/05_quests.png", "Nhật ký nhiệm vụ [J] — tab Đang làm / Có thể nhận / Đã xong, mục tiêu và từ vựng sẽ học", 6.0),
         ("shots/g04_map.png", "Bản đồ Nihongo City [M] — khu vực hiện tại, bản đồ tổng và vị trí người chơi", 6.0)],
    11: [("shots/m04_profile.png", "Hồ sơ người chơi — cấp độ, XP, số tình huống hoàn thành, tên hiển thị (chế độ offline)", 6.0)],
    12: [("diag/d05_shop_flow.png", "Workflow mua sắm tại konbini, gồm trường hợp không đủ Yen", 6.3)],
    13: [("shots/g08_restaurant_menu.png", "Thực đơn Sushi Hibari — 7 nhóm món, giá Yen, mô tả, nguyên liệu và lưu ý dị ứng", 6.0),
         ("diag/d06_restaurant_flow.png", "Workflow một bữa ăn tại nhà hàng, từ lúc vào quán tới lúc thanh toán", 6.3)],
    14: [("shots/g06_exam_hub.png", "Trung tâm luyện thi [K] — tab JLPT/IELTS và đề “JLPT N5 – Đề luyện tập 1”", 6.0),
         ("diag/d07_exam_flow.png", "Workflow luyện thi từ chọn đề tới khi lưu kết quả", 6.3)],
    15: [("shots/g07_exam_play.png", "Màn làm bài — bảng câu hỏi, đồng hồ đếm ngược, câu đọc chữ 「先生」 và nút Nộp phần", 6.0)],
    16: [("shots/m05_leaderboard.png", "Bảng xếp hạng — hiển thị trạng thái khi chưa kết nối online, có nút Làm mới", 6.0)],
    17: [("shots/07_settings.png", "Cài đặt — âm lượng nhạc nền/hiệu ứng, ngôn ngữ và đổi phím điều khiển", 6.0)],
    19: [("diag/d08_architecture.png", "Kiến trúc phân lớp: UI → Gameplay → Dịch vụ → Dữ liệu → Hạ tầng tùy chọn", 6.3),
         ("diag/d09_scenario_pipeline.png", "Luồng thực thi kịch bản từ ScenarioDefinition tới lưu tiến trình", 6.3)],
}

# Step-by-step UI tables appended to UI workflow sections: (title, rows)
STEP_HEAD = ["#", "Người chơi", "Game / UI phản hồi"]
STEPS = {
    8: ("Bảng thao tác — Khởi động và chọn nhân vật", [
        ["1", "Mở game", "00_Bootstrap tạo AppRoot, đăng ký GameServices rồi chuyển sang 01_MainMenu"],
        ["2", "Chọn VI / EN / JP", "GameSettingsService đổi ngôn ngữ; mọi nhãn, kể cả popup đang mở, cập nhật ngay"],
        ["3", "Bấm “Cách chơi”", "GuidePopup hiện 4 bước và bảng phím: WASD, Left Shift, F, B, Tab, M, Enter, V"],
        ["4", "Bấm “Bắt đầu”", "Mở panel Chọn nhân vật, có preview 3D xoay được bằng cách kéo chuột"],
        ["5", "Bấm < > và nhập tên", "Đổi nhân vật trong PlayableCharacterCatalog (ví dụ Tanaka – Học viên)"],
        ["6", "Bấm “Chọn nhân vật này”", "Lưu hồ sơ; SceneFlowController tải khu phố và đặt người chơi vào điểm spawn"],
        ["7", "Bấm “Quay lại”", "Đóng panel, trở về Menu chính mà không mất lựa chọn ngôn ngữ"],
    ]),
    9: ("Bảng thao tác — Hội thoại (ví dụ: chào buổi sáng với Tanaka)", [
        ["1", "Đi tới gần NPC", "HUD hiện nhắc “[F] Tương tác” ở cạnh dưới màn hình"],
        ["2", "Nhấn F", "DialogueManager mở node; hiện người nói “Tanaka” và câu 「おはようございます！いい朝ですね。」"],
        ["3", "Đọc gợi ý", "Theo LearningMode: Guided hiện dịch + romaji + furigana; Practice ẩn romaji; Assessment chỉ tiếng Nhật"],
        ["4", "Chọn 「おはようございます。」", "Đáp án lịch sự, đúng ngữ cảnh: cộng điểm ResponseAccuracy, đi tiếp node sau"],
        ["5", "Chọn 「おはよう。」", "Câu thân mật, chưa phù hợp với người mới gặp: NPC nhắc về mức độ lịch sự"],
        ["6", "Chọn 「こんばんは。」", "Sai thời điểm trong ngày: giải thích rồi cho chọn lại (recovery path)"],
        ["7", "Hoàn tất hội thoại", "setFlags ghi nhớ lựa chọn, mục tiêu được đánh dấu xong, HUD cập nhật"],
    ]),
    10: ("Bảng thao tác — Nhiệm vụ và bản đồ", [
        ["1", "Nhấn J", "Mở Nhật ký nhiệm vụ với 3 tab: Đang làm / Có thể nhận / Đã xong"],
        ["2", "Chọn “Chào mừng đến Hibari-chō”", "Hiện tiêu đề tiếng Nhật, địa điểm Sakura-dōri, 3 mục tiêu, phần thưởng ~100 kiến thức và từ vựng sẽ học"],
        ["3", "Bấm “Bắt đầu nhiệm vụ”", "ScenarioManager kích hoạt kịch bản; HUD hiện mục tiêu và mũi tên chỉ hướng"],
        ["4", "Nhấn M", "Mở Bản đồ Nihongo City: Sushi Hibari, Trường Nhật ngữ Hibari, Ga Sakura Metro, Cửa hàng tiện lợi, Khu dân cư, Công viên"],
        ["5", "Chuyển “Khu vực hiện tại / Bản đồ tổng”", "Đổi giữa bản đồ vùng và góc nhìn từ trên xuống; marker vị trí cập nhật theo thời gian thực"],
        ["6", "Nhấn M / Esc", "Đóng bản đồ, trả quyền điều khiển cho nhân vật"],
    ]),
    11: ("Bảng thao tác — Balo, thể trạng và hồ sơ", [
        ["1", "Nhấn B", "Mở Balo 16 ô; chọn một ô để xem chi tiết"],
        ["2", "Chọn vật phẩm", "Hiện tên, số lượng, công dụng; các nút Chi tiết / Bỏ ra / Chia nhỏ / Sắp xếp"],
        ["3", "Dùng vật phẩm (C)", "Trừ số lượng và cộng chỉ số tương ứng (no, khát, năng lượng) trong cùng một thao tác"],
        ["4", "Nhấn Tab", "Mở bảng nhân vật: máu, năng lượng, no, khát theo PlayerStatus"],
        ["5", "Mở “Hồ sơ” từ Menu", "Hiện cấp độ, XP, số tình huống hoàn thành; sửa Display Name và Bio rồi bấm Lưu"],
    ]),
    12: ("Bảng thao tác — Mua cơm nắm tại konbini", [
        ["1", "Nhận nhiệm vụ mua onigiri", "Mục tiêu xuất hiện trên HUD, có điểm đến trên bản đồ"],
        ["2", "Đi vào Cửa hàng tiện lợi", "Node GoToArea hoàn thành khi vào đúng khu vực"],
        ["3", "Xem kệ hàng", "Node InspectItem: hiện tên món bằng tiếng Nhật và giá"],
        ["4", "Mở ShopUI, chọn món", "Danh mục, giá Yen, số lượng; số dư cập nhật ngay"],
        ["5", "Thanh toán tại quầy", "Hội thoại hỏi giá và trả tiền (「これ、お願いします。」 khi đưa hàng lên quầy)"],
        ["6", "Không đủ Yen", "Giao dịch bị từ chối, giữ nguyên tiền và balo, gợi ý chọn món khác"],
        ["7", "Nhận vật phẩm", "Node CollectItem: vật phẩm vào balo, mục tiêu được đánh dấu xong"],
    ]),
    13: ("Bảng thao tác — Bữa ăn tại Sushi Hibari", [
        ["1", "Vào quán", "Nhân viên chào 「いらっしゃいませ」 và hỏi số người"],
        ["2", "Ngồi vào bàn", "RestaurantTable giữ chỗ; mở thực đơn bằng E"],
        ["3", "Duyệt 7 nhóm món", "握り · 巻き物 · ちらし・丼 · 一品料理 · 飲み物 · デザート · 名物"],
        ["4", "Chọn món (ví dụ まぐろ ¥280)", "Hiện cách đọc, nghĩa, khẩu phần, nguyên liệu, lưu ý dị ứng và cách ăn"],
        ["5", "Xác nhận gọi món", "Giỏ hàng tổng hợp; món được mang ra bàn"],
        ["6", "Dùng bữa", "Học 「いただきます」 trước và 「ごちそうさまでした」 sau khi ăn"],
        ["7", "Tính tiền", "「お会計お願いします」 → trừ Yen đúng tổng giỏ hàng; chưa thanh toán thì chưa rời quán"],
    ]),
    14: ("Bảng thao tác — Chọn đề luyện thi", [
        ["1", "Nhấn K (hoặc tương tác ở trường)", "Mở Trung tâm luyện thi"],
        ["2", "Chọn tab JLPT hoặc IELTS", "Danh sách đề lấy từ ExamRepository (Resources/Exams)"],
        ["3", "Xem thẻ đề", "Tên đề, mô tả các phần, cấp độ N5 và kết quả tốt nhất (nếu đã làm)"],
        ["4", "Bấm “Bắt đầu”", "ExamManager.StartAttempt tạo lượt mới; nếu đang làm dở thì tiếp tục lượt cũ"],
    ]),
    15: ("Bảng thao tác — Làm bài và nộp", [
        ["1", "Chọn câu trên bảng số 1–6", "Chuyển câu; ô đang chọn được tô vàng"],
        ["2", "Đọc câu hỏi", "Ví dụ 「『先生』の読み方はどれですか。」 với 4 đáp án せんせい / せんせえ / せいせん / せんせ"],
        ["3", "Chọn đáp án", "Lưu câu trả lời; bộ đếm “Đã trả lời x/6 câu” cập nhật"],
        ["4", "Bấm Trước / Sau", "Di chuyển trong phần thi hiện tại"],
        ["5", "Bấm “Nộp phần này”", "Khóa đáp án phần đó và chuyển sang phần kế tiếp"],
        ["6", "Hết giờ", "Phần thi tự khóa và tự chuyển; câu chưa làm tính là bỏ trống"],
        ["7", "Xem kết quả", "Điểm từng phần, đậu/chưa đậu theo ngưỡng JLPT (80 tổng, 19 mỗi phần); lưu ExamAttemptRecord"],
    ]),
    16: ("Bảng thao tác — Online", [
        ["1", "Bấm “Đăng nhập”", "AuthUI: nhập email/mật khẩu, gửi xác thực"],
        ["2", "Đăng nhập thành công", "Tải hồ sơ và tiến trình cloud, bật presence, chat và bạn bè"],
        ["3", "Mở Xếp hạng / Bạn bè", "Hiện danh sách; nếu chưa kết nối thì báo “Chưa kết nối online” kèm nút Làm mới"],
        ["4", "Mở Co-op", "CoopLobbyUI: tạo hoặc tham gia phiên học cùng bạn"],
    ]),
    17: ("Bảng thao tác — Cài đặt", [
        ["1", "Mở Cài đặt (Menu chính hoặc Esc trong game)", "Hiện Âm thanh, Ngôn ngữ và Điều khiển"],
        ["2", "Kéo thanh Nhạc nền / Hiệu ứng", "Âm lượng áp dụng ngay, hiển thị phần trăm"],
        ["3", "Bấm VI / EN / JP", "Đổi ngôn ngữ toàn bộ giao diện"],
        ["4", "Bấm một phím trong bảng Điều khiển", "Chờ phím mới rồi gán lại (Đi tới W, Chạy Left Shift, Tương tác F, Balo B, Bản đồ M…)"],
        ["5", "Bấm “Mặc định” / “Đóng”", "Khôi phục phím gốc hoặc lưu rồi quay lại đúng màn hình trước đó"],
    ]),
}

SCORING = ("5.5. Cách tính điểm và điều kiện hoàn thành", [
    "ScoringManager ghi lại các ScoreEvent trong suốt một tình huống. Khi kết thúc, điểm được tổng hợp theo 5 nhóm. Bốn nhóm kỹ năng bắt đầu từ 100 và bị trừ hoặc cộng theo từng lựa chọn; nhóm Hoàn thành nhiệm vụ đạt 100 khi kịch bản kết thúc thành công. Mỗi nhóm được giới hạn trong khoảng 0–100, và điểm tổng là trung bình cộng của 5 nhóm.",
], ["Nhóm điểm", "Ý nghĩa", "Cách ghi nhận"], [
    ["Vocabulary", "Hiểu và dùng đúng từ vựng", "Sự kiện từ lựa chọn hoặc vật phẩm có tag từ vựng"],
    ["Grammar", "Dùng đúng mẫu câu", "Lựa chọn sai ngữ pháp bị trừ điểm"],
    ["Listening", "Nghe hiểu câu NPC", "Ghi nhận ở các node có audio hoặc yêu cầu nghe"],
    ["ResponseAccuracy", "Trả lời đúng ngữ cảnh, đúng mức lịch sự", "Mỗi lựa chọn đúng hoặc sai trong hội thoại"],
    ["TaskCompletion", "Hoàn thành mục tiêu", "100 khi tình huống kết thúc thành công"],
], "Không có trạng thái “thua”: chọn sai chỉ làm giảm điểm nhóm tương ứng và mở nhánh giải thích. Người chơi luôn có thể thử lại để hoàn thành tình huống, đúng với tinh thần “nói sai cũng không sao”.")

BUGS = ("22.1. Lỗi phát hiện và đã sửa trong đợt kiểm thử", ["Lỗi", "Nguyên nhân", "Cách sửa"], [
    ["Trung tâm luyện thi không có đề nào", "Lớp ExamDefinition (ScriptableObject) nằm trong file ExamModels.cs nên Unity không gắn đúng script cho asset đề thi", "Tách ra file ExamDefinition.cs và giữ nguyên GUID; 2 đề JLPT N5 và IELTS đã tải được"],
    ["Bấm “Bắt đầu” bài thi bị lỗi trong Editor", "Dùng toán tử ?? với GetComponent<AudioSource>(); trong Unity, giá trị “fake null” làm ?? không hoạt động", "Kiểm tra == null một cách rõ ràng; áp dụng cho ExamPlayUI, HUDUI, MainMenuUI, JobInteractable, HibariClassroomRuntime"],
    ["Lỗi biên dịch CS0234 ở HomeBedroomRuntime", "Tham chiếu sai namespace NihongoLife.Core.PlayerController", "Sửa thành NihongoLife.Player.PlayerController"],
    ["Nhấn F ở giường không ngủ", "Giường không có script tương tác (chỉ có collider)", "Gắn BedroomRestInteractable với điểm nằm/đứng; chu trình đêm → 07:00 sáng, hồi năng lượng, lưu game"],
    ["Nhân vật T-pose khi ngủ/ngồi", "Clip Sitting/Laying import kiểu Generic trong khi rig là Humanoid", "Chuyển sang Humanoid, thêm state Sit/Lay vào NL_Humanoid"],
    ["Phòng ngủ chỉ là khối hộp, UI vỡ chữ", "Bảng HUD riêng trùng với HUD chính, font thiếu dấu", "Dựng lại căn hộ 1K bằng Kenney furniture; UI phòng mới, chỉ hiện khi cần"],
    ["Siêu thị lộ thiên, kẹt lối đi", "61 object gốc bị nhân bản, 3 tường va chạm vô hình giữa lối đi, nhà bên cạnh trùm lên lô đất", "Dựng tòa nhà ひばりマート, dọn rác, test đi bộ qua lối đi"],
    ["Nhiệm vụ konbini không hoàn thành được", "Thiếu NPC thu ngân npc_cashier (người giao nhiệm vụ)", "Thêm chị Ito sau quầy, có tương tác Thanh toán"],
    ["Thu ngân / nhân viên đứng T-pose, NPC trắng toát", "Mô hình NL_Cashier không có xương; vật liệu Lilly thiếu texture", "Dựng lại NL_Cashier từ mô hình có xương + tạp dề; nối lại texture"],
])

# Section-level text replacements: {section_index: {old_prefix: new_text or None to delete}}
PATCH = {
    25: {"Mức hoàn thiện": "Một số hạng mục cần hoàn thiện thêm: nội dung tiếng Nhật nên được giáo viên bản ngữ duyệt lại, NPC cần lồng tiếng thật, tính năng nhiều người chơi cần kiểm thử với backend thật, và cần tối ưu cho WebGL/mobile dựa trên số đo hiệu năng."},
    18: {"Đọc được nhiều ngôn ngữ": "Hiển thị tốt ba ngôn ngữ; phản hồi thao tác rõ ràng; mở rộng nội dung qua dữ liệu; quản lý vòng đời scene và service; không để lộ khóa bí mật của dịch vụ cloud; phục hồi được khi lỗi mạng hoặc lỗi tải media."},
    0: {"Báo cáo trình bày thiết kế": "Báo cáo này trình bày ý tưởng, thiết kế gameplay, luồng giao diện, kiến trúc kỹ thuật và kết quả kiểm thử của NihongoLife, kèm hình ảnh chụp trực tiếp từ game."},
    1: {"Ngoài campaign, mã nguồn": "Ngoài campaign, game có trung tâm luyện thi JLPT/IELTS, balo và chỉ số nhân vật, bản đồ, nhà hàng có thực đơn chi tiết và các tính năng cộng đồng online (đăng nhập, bạn bè, xếp hạng, co-op). Kết quả luyện thi chỉ dùng để tự đánh giá, không thay thế điểm thi chính thức."},
    5: {"Danh sách trên xác nhận": "14 tình huống trên được nạp từ Resources/Scenarios. Thứ tự mở khóa do campaign và điều kiện requiredKnowledge quyết định, nên người chơi đi theo một tuyến truyện liền mạch từ ngày đầu đến lễ hội mùa hè."},
    26: {"Mẫu đầu vào:": None, "Các nguồn dưới đây": "Tài liệu thiết kế nội bộ của dự án và các nguồn tham khảo bên ngoài được dùng trong quá trình phát triển."},
    24: {"Thông tin credit": "Game sử dụng các bộ tài nguyên miễn phí/có giấy phép và được ghi công trong popup “Về tôi”. Bảng dưới liệt kê các nguồn chính."},
}

TABLE_REPLACE = {
    23: {0: (["Mức kiểm tra", "Kết quả"], [
        ["Kiểm thử tự động", "EditMode 10/10 · Play Mode 5/5 (07/10/2026)"],
        ["Play Mode test", "Phòng ngủ (ngủ, học, bếp, tủ lạnh, đèn, va chạm tường), thành phố (siêu thị, 4 lối vào, bản đồ), chuyển khu vực, kịch bản konbini; hơn 40 ảnh tự chụp"],
        ["Chuyển scene", "Tải được 20_StationDistrict, 30_SushiRestaurant, 40_HIBARICLASS, 45_HomeBedroom"],
        ["Nội dung", "14 kịch bản, 2 đề thi, 1 thực đơn nhà hàng được nạp đúng"],
        ["Còn cần kiểm tra thêm", "Online với backend thật, quyền micro, mô hình riêng cho nhân viên sushi/ga"],
    ], [2600, 6440])},
    24: {0: (["Tài nguyên", "Vai trò trong game"], [
        ["Unity 6 (URP) · TextMeshPro", "Engine, render pipeline và hiển thị chữ đa ngôn ngữ"],
        ["Noto Sans JP", "Font hiển thị tiếng Việt, kana và kanji"],
        ["Quaternius — Sushi Restaurant Kit", "Nội thất và đồ ăn trong nhà hàng sushi"],
        ["KayKit City Builder · Kenney", "Nhà, đường phố, đạo cụ khu phố Hibari-chō"],
        ["Mixamo", "Nhân vật và animation di chuyển"],
        ["Devion Games", "Thành phần UI/inventory tham khảo"],
        ["Supabase · Gemini · Agora", "Đăng nhập & lưu cloud, chấm Writing/Speaking, voice chat"],
    ], [3400, 5640])},
}
DROP_SUBSECTIONS = {24: ["23.1.", "23.2."]}
EXTRA_REFS = [
    ["Unity Manual & Scripting API", "docs.unity3d.com"],
    ["The Japan Foundation — JLPT N5", "jlpt.jp — cấp độ và cấu trúc đề"],
    ["Marugoto / Irodori (Japan Foundation)", "Tình huống giao tiếp đời sống cơ bản"],
    ["Quaternius · Kenney · KayKit", "quaternius.com · kenney.nl · kaylousberg.itch.io"],
]
