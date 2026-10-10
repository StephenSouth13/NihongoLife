"""Report update (08/10/2026): new chapters, appended sections, diagrams and refreshed screenshots.
Part kinds: ("h", text) · ("h3", text) · ("p", text) · ("fig", path, caption, width_in) · ("pair", [(path, cap), (path, cap)])
· ("table", headers, rows, widths) · ("callout", title, text) · ("bullets", [(lead, text), ...]).
Chapter/sub-heading numbers are rewritten by the builder after the new chapters are inserted."""

S = "shots7/"

# ─────────── Figures replacing outdated screenshots (keyed by original chapter index) ───────────
FIGS_OVERRIDE = {
    9: [(S + "ui_01_dialogue_box.png", "Hộp thoại dùng chung (DialogueView): tên người nói, câu Nhật, cách đọc, romaji, nghĩa tiếng Việt và nút × đóng", 6.0),
        ("diag/d04_dialogue_flow.png", "Workflow hội thoại học tập, gồm nhánh sai và đường phục hồi", 6.3)],
    10: [("shots/05_quests.png", "Nhật ký nhiệm vụ [J]: tab Đang làm / Có thể nhận / Đã xong, mục tiêu và từ vựng sẽ học", 6.0),
         (S + "ui_08_map_overview.png", "Bản đồ Hibari-chō [M]: tab Toàn cảnh, địa điểm ghi chữ Hán + kana, danh sách khoảng cách bên phải", 6.0)],
    14: [(S + "school_03_exam_centre.png", "Trung tâm luyện thi mở từ bàn thi trong lớp: tab JLPT / IELTS, đề “JLPT N5 – Đề luyện tập 1”", 6.0),
         ("diag/d07_exam_flow.png", "Workflow luyện thi từ chọn đề tới khi lưu kết quả", 6.3)],
    15: [(S + "school_04_exam_started.png", "Màn làm bài: bảng câu hỏi, đồng hồ đếm ngược, câu đọc chữ 「先生」 và nút Nộp phần này", 6.0)],
    4: [("diag/d11_world_hub.png", "Thành phố Hibari-chō là trung tâm; mỗi khu vực nạp thêm và có lối ra cố định về đúng cửa", 6.3)],
}

# ─────────── Sections appended to existing chapters ───────────
APPEND = {
    # 1 · Tóm tắt — extra rows in the summary table
    1: [("table", ["Hạng mục", "Hiện trạng (08/10/2026)"], [
        ["Khu vực chơi được", "7: phố Hibari-chō, konbini ひばりマート, phòng trọ, ga tàu, nhà hàng sushi, lớp học Hibari, Game Center"],
        ["Nội dung học", "14 kịch bản N5, 2 đề luyện thi (JLPT N5, IELTS), mini-game Kana Match với 5 bộ thẻ"],
        ["Quy mô mã nguồn", "198 script runtime (~40.000 dòng C#), 8 script mini-game, 13 file kiểm thử"],
        ["Kiểm thử tự động", "Play Mode và EditMode chạy bằng thao tác thật, kèm hơn 150 ảnh tự chụp (xem chương Kiểm thử)"],
        ["Dịch vụ trực tuyến", "Supabase (đăng nhập, lưu cloud, bạn bè, chat, xếp hạng, co-op), Gemini (phát âm, NPC AI, chấm IELTS), Agora (lớp học video)"],
    ], [2600, 6420])],

    # 5 · Gameplay — life systems detail
    6: [("h", "5.6. Chỉ số sống và thể lực"),
        ("p", "Bốn chỉ số hiển thị trên thẻ HUD là Sức khỏe (体), Thể lực (元), No (食) và Khát (水); cửa sổ nhân vật [Tab] có thêm Tỉnh táo (眠). Thể lực là chỉ số thay đổi nhanh nhất vì gắn trực tiếp với cách di chuyển. Bản trước để thể lực hồi 7 điểm/giây kể cả khi đang chạy trong khi chạy chỉ tốn 4,5 điểm/giây, nên nhân vật tăng tốc mà thanh không giảm. Bản hiện tại tách rõ các trạng thái như sơ đồ dưới."),
        ("table", ["Trạng thái", "Thể lực", "No / Khát"], [
            ["Đứng yên", "Hồi 9/giây (sau 1,2 giây kể từ lần chạy cuối)", "Giảm chậm: 1,6 / 2,4 mỗi phút"],
            ["Đi bộ", "Hồi 4/giây", "×1,5"],
            ["Chạy (giữ Shift)", "−14/giây, không hồi", "No ×4, Khát ×5; buồn ngủ ×2"],
            ["Kiệt sức", "Khoá chạy tới khi hồi đủ 25; HUD nháy đỏ “Hết sức”", "Như đi bộ"],
            ["Ngủ / ăn uống", "Ngủ: đầy thể lực; món ăn cộng theo bảng vật phẩm", "Món ăn cộng No/Khát"],
        ], [2000, 4200, 2820]),
        ("fig", "diag/d19_state_stamina.png", "Máy trạng thái thể lực của PlayerStatus", 6.3),
        ("callout", "Lưu ý về bản lưu.", "Chỉ số bằng 0 trong bản lưu là trạng thái thật (đói, khát, kiệt sức), không còn bị tự đặt lại thành 100 khi nạp game. Bài kiểm thử CompletionAudit chạy nhân vật qua bộ điều khiển thật và kiểm tra cả trường hợp này."),
        ],

    # 8 · HUD & dialogue
    9: [("h", "8.5. HUD dùng chung ở mọi khu vực"),
        ("p", "HUD được gom về một thành phần StatusDock: thẻ chỉ số góc trái dưới và thanh nút góc phải dưới (Balo B · Nhân vật Tab · Bản đồ M · Nhiệm vụ J · Luyện thi K · Cài đặt Esc). Trước đây mở thẳng phòng trọ hay nhà ga thì không có HUD; nay StandaloneZoneBootstrap luôn nạp thành phố làm nền nên khu nào cũng có HUD, balo và lối ra."),
        ("pair", [(S + "bedroom_13_standalone_hud.png", "HUD khi Play thẳng từ phòng trọ"), (S + "ui_23_character_window.png", "Cửa sổ nhân vật [Tab] với 5 chỉ số")]),
        ("h", "8.6. Đóng cửa sổ bằng nút ×"),
        ("p", "Mọi cửa sổ phủ màn hình có một nút × đỏ ở góc trên phải (NLUi.CloseButton): hội thoại, mini-game, cửa hàng, balo, cửa sổ nhân vật, máy bán vé. Esc và Tab vẫn hoạt động như cũ. Với lời dẫn truyện, đóng hộp thoại không làm mất bước truyện: node hiện tại được giữ lại (IsStoryPaused) và nút “Tiếp hội thoại · R” xuất hiện để quay lại đúng câu đang đọc."),
        ("pair", [(S + "completion_02_narration_close.png", "Lời dẫn truyện có nút × đóng"), (S + "school_01_welcome.png", "Sau khi đóng: nút “Tiếp hội thoại · R” ở giữa đáy màn hình")]),
        ("h", "8.7. Vòng biểu cảm"),
        ("p", "Giữ E mở vòng 8 biểu cảm; rê chuột, phím 1–8 hoặc chạm để chọn, thả E để dùng; nhấn nhanh E lặp lại biểu cảm vừa dùng. Mỗi ô gắn một câu tiếng Nhật thông dụng (こんにちは, ありがとう, すみません…), nhân vật cúi chào hoặc vẫy tay và bong bóng chữ hiện trên đầu."),
        ("pair", [(S + "ui_24_emote_wheel.png", "Vòng biểu cảm khi giữ E"), (S + "ui_25_emote_bubble.png", "Bong bóng câu chào trên đầu nhân vật")]),
        ("fig", "diag/d14_seq_dialogue.png", "Sơ đồ tuần tự hội thoại học tập, gồm nhánh đúng/sai và tạm rời", 6.3),
        ],

    # 9 · Quest & map
    10: [("h", "9.5. Chỉ đường và cổng khu vực"),
         ("p", "Chọn một địa điểm trên bản đồ sẽ bật WaypointGuide: mũi tên vàng trên mặt đất và khoảng cách còn lại. Mỗi lối vào khu vực có PortalBeacon (cột sáng + biển tên song ngữ) nên người chơi luôn thấy đi đâu."),
         ("pair", [(S + "gamecenter_00_waypoint_guide.png", "Chỉ đường tới Game Center"), (S + "ui_22_portal_sign_and_dock.png", "Biển cổng khu vực và thanh nút HUD")]),
         ("fig", "diag/d23_seq_zone.png", "Sơ đồ tuần tự chuyển khu vực (nạp additive, ẩn thành phố, spawn đúng cửa)", 6.3),
         ("h", "9.6. Đường chân trời không lộ giới hạn bản đồ"),
         ("p", "Phố Hibari-chō lặp vô tận theo trục đường, nhưng hai bên trước đây là khoảng đen và mép bản đồ. Root Horizon_Backdrop trong 90_TestSandbox thêm dải đất, khu nhà ngoại ô có cửa sổ (sáng đèn khi tối), vòng đồi và dải sương lấy màu fog theo giờ trong ngày. Phần này bám theo camera, không có collider; chống rơi vẫn do WorldBoundsGuard đảm nhiệm."),
         ("fig", S + "horizon_triple.png", "Đường chân trời lúc ban ngày, hoàng hôn và ban đêm — không còn thấy mép bản đồ", 6.3),
         ],

    # 10 · Bag & profile
    11: [("h", "10.5. Balo có ảnh thật của từng món"),
         ("p", "Ô balo và thẻ chi tiết hiện ảnh của vật phẩm (Resources/Items/<itemId>.png, nạp qua ItemIcons); chữ Hán thu nhỏ thành nhãn ở góc. Ba loại cơm nắm dùng ảnh chụp thật; các món khác là ảnh minh hoạ hoặc render từ mô hình 3D. Món mới chưa có ảnh vẫn hiện chữ Hán như trước."),
         ("pair", [(S + "ui_07_bag.png", "Balo [B]: lưới 16 ô, thẻ chi tiết, Ăn / Bỏ, thể trạng"), (S + "completion_01_actual_sprint.png", "Chạy thật: thanh Thể lực giảm trên HUD")]),
         ("fig", S + "items_grid.png", "Bộ ảnh vật phẩm dùng chung cho cửa hàng, giỏ hàng, balo và quầy quà", 6.0),
         ],

    # 11 · Shop
    12: [("h", "11.5. Kệ hàng, giỏ và quầy thu ngân"),
         ("p", "Nhấn F ở từng kệ mở KonbiniShopUI theo khu: cơm nắm/bánh mì, bánh kẹo, đồ uống, đồ nóng. Món được bỏ vào giỏ (KonbiniBasket), chip giỏ hàng nhắc mang ra quầy; chị Ito tính tiền, hỏi có cần túi và trả lời bằng tiếng Nhật."),
         ("pair", [(S + "ui_02_shop_onigiri.png", "Kệ cơm nắm với ảnh từng món"), (S + "ui_05_checkout.png", "Quầy レジ: giỏ hàng có ảnh và tổng tiền")]),
         ("fig", "diag/d15_seq_checkout.png", "Sơ đồ tuần tự mua hàng: kệ → giỏ → thanh toán → hội thoại cảm ơn", 6.3),
         ],

    # 13/14 · Exam
    14: [("h", "13.5. Bàn thi trong lớp học Hibari"),
         ("p", "Ngoài phím K, người chơi vào lớp 40_HIBARICLASS, ngồi ở bàn thi (ClassroomExamDesk) và nhấn F. Trung tâm luyện thi mở ở chế độ khoá gameplay: chuột được mở, nhân vật và camera đứng yên, nên bấm được tab JLPT/IELTS và nút Bắt đầu."),
         ("pair", [(S + "school_02_exam_desk.png", "Bàn thi trong lớp, nhắc “Làm bài thi · F”"), (S + "school_03a_ielts_selection.png", "Tab IELTS: đề Academic luyện tập 1")]),
         ],
    15: [("h", "14.5. Kết quả trong lớp học"),
         ("p", "Khi nộp bài, ClassroomRuntime chạy nghi thức kết thúc: đèn dịu, pháo giấy, cô Morita nhận xét theo điểm và bảng đen ghi điểm cao nhất của người chơi."),
         ("pair", [(S + "school_05_result.png", "Kết quả: ĐẠT 170/180, điểm từng phần"), (S + "school_06_teacher_comment.png", "Cô Morita nhận xét, người chơi đáp lại")]),
         ],

    # 15 · Online
    16: [("h", "15.5. Hiện trạng tích hợp dịch vụ (08/10/2026)"),
         ("table", ["Dịch vụ", "Dùng cho", "Hiện trạng đã kiểm chứng"], [
             ["Supabase Auth + PostgREST", "Đăng nhập, profiles, player_progress, leaderboard, friendships, co-op", "Mã nguồn hoàn chỉnh; thiếu cấu hình dự án thì game tự chạy offline và báo “chưa kết nối”"],
             ["Supabase Realtime", "Presence (vị trí người chơi khác), chat theo kênh", "Có mã kết nối và lưu chat_messages; chưa thử tải nhiều người"],
             ["Google Gemini", "Chấm phát âm [V], NPC trả lời bằng AI, chấm IELTS Writing", "Model cũ gemini-2.5-flash trả 404 với tài khoản hiện tại; đã đổi sang gemini-flash-latest và gọi thử trả 200"],
             ["Agora RTC", "Lớp học video [F8] với giáo viên", "Đã nối SDK hiện có, kênh nihongolife-classroom; cuộc gọi thật giữa hai máy chưa được kiểm chứng"],
         ], [2300, 3300, 3420]),
         ("fig", "diag/d22_deployment.png", "Sơ đồ triển khai: máy người chơi và ba dịch vụ trực tuyến tuỳ chọn", 6.3),
         ("fig", "diag/d17_seq_ai.png", "Sơ đồ tuần tự luyện phát âm và NPC dùng AI, có đường lui offline", 6.3),
         ("callout", "Bảo mật khoá.", "Khoá Gemini đọc từ biến môi trường NIHONGOLIFE_GEMINI_API_KEY, không nằm trong asset. App ID của Agora là định danh công khai; token phòng do giáo viên cấp, không có certificate trong client."),
         ],

    # 17 · Use case
    18: [("fig", "diag/d12_use_case.png", "Sơ đồ use case tổng quát: người chơi, 18 use case và ba hệ thống ngoài", 6.3),
         ("h", "17.3. Đặc tả một số use case chính"),
         ("table", ["Use case", "Luồng chính", "Ngoại lệ / luồng phụ"], [
             ["Hội thoại N5 với NPC", "F gần NPC → đọc câu → chọn 1–4 → nhận phản hồi → node tiếp theo → lưu", "Chọn sai: giải thích + chọn lại; × / Esc: tạm rời, R để tiếp"],
             ["Mua sắm ở konbini", "F ở kệ → chọn món, số lượng → giỏ → F ở quầy → thanh toán → Ito cảm ơn", "Thiếu Yen: báo lỗi, giữ giỏ; bớt món ở quầy"],
             ["Mua vé & đi tàu", "Hỏi Kimura → mua vé → qua cổng → chờ sân 2 → lên tàu → xuống đúng ga", "Không vé: cổng chặn; thiếu Yen: không bán"],
             ["Luyện thi JLPT / IELTS", "F ở bàn thi / K → chọn đề → làm từng phần → nộp → kết quả, nhận xét", "Hết giờ tự nộp; đóng giữa chừng không lưu lượt"],
             ["Chơi Kana Match", "F ở máy → chọn bộ → đếm 3-2-1 → lật cặp → kết quả, sao, vé", "× / Esc: dừng giữa chừng, vẫn hiện bảng kết quả"],
             ["Luyện phát âm [V]", "V ghi → V dừng → gửi Gemini → điểm và góp ý", "Không mạng / không khoá: báo lỗi, giữ chế độ offline"],
         ], [2200, 4100, 2720]),
         ],

    # 18 · Architecture
    19: [("h", "18.5. Sơ đồ lớp"),
         ("p", "Sơ đồ dưới rút gọn các lớp trung tâm. AppRoot đăng ký dịch vụ vào GameServices; ScenarioManager điều khiển DialogueManager, DialogueView hiển thị và ScoringManager ghi điểm. PlayerController báo vận động cho PlayerStatus và tự thêm PostureStabilizer. Mini-game đi qua MiniGameController và giao diện IMiniGame nên trò mới chỉ cần hiện thực interface này."),
         ("fig", "diag/d13_class.png", "Sơ đồ lớp các thành phần cốt lõi", 6.3),
         ("h", "18.6. Thẳng lưng khi đi (PostureStabilizer)"),
         ("p", "Clip đi bộ Mixamo làm cột sống lắc ngang và hai chân bước chéo lên một đường, nhìn ẻo lả. PostureStabilizer chạy sau Animator (LateUpdate, thứ tự 500), kéo cột sống/ngực/cổ về trục đứng nhưng giữ độ nghiêng trước tự nhiên, và xoay đùi ra ngoài 4°. Đo trên chu kỳ đi thật: vai lệch 6,3° → 2,1°, lưng nghiêng 3,7° → 1,7°, khoảng cách hai bàn chân hẹp nhất 6 cm → 15 cm. Bỏ qua khi ngồi/nằm."),
         ("fig", S + "walk_compare.png", "Dáng đi trước và sau PostureStabilizer (6 khung hình nhìn chính diện)", 6.3),
         ],

    # 19 · Data
    20: [("h", "19.4. Mô hình dữ liệu"),
         ("p", "Bản lưu cục bộ là một PlayerProgressDto dạng JSON. Khi đăng nhập Supabase, SupabaseProgressRepository upsert toàn bộ bản lưu vào player_progress.progress_json kèm danh sách kịch bản đã xong; các bảng còn lại phục vụ hồ sơ, xếp hạng, bạn bè, chat và co-op."),
         ("fig", "diag/d18_erd.png", "Mô hình dữ liệu Supabase và bản lưu cục bộ", 6.3),
         ],
}

# ─────────── New chapters (inserted after the given original chapter index) ───────────
NEW_CHAPTERS = [
    (13, {"title": "13. UI workflow — Ga tàu và di chuyển", "lead": "Một chuyến tàu thật: hỏi giá, mua vé, qua cổng, chờ, lên tàu và xuống đúng ga.", "parts": [
        ("h", "13.1. Ga Hibari"),
        ("p", "Ga 20_StationDistrict có quầy vé với anh Kimura (StationTicketClerk), máy bán vé けんばいき, cổng soát vé, sân ga số 2 có cửa chắn và bảng LED. Tuyến có ba ga: Hibari, Gakuen-mae (¥180, đi tới trường) và Minato (¥320, đi tới nhà hàng sushi)."),
        ("pair", [(S + "ui_11_station_staff.png", "Hỏi anh Kimura ở quầy vé"), (S + "ui_20_ticket_machine.png", "Máy bán vé: chọn ga đến, có nút ×")]),
        ("h", "13.2. Trên tàu"),
        ("p", "Khi lên tàu, HUD trên tàu hiện bảng LED ga kế tiếp, đồng hồ, sơ đồ tuyến, chip vé và trạng thái cửa. Q chuyển sang camera cửa sổ để ngắm cảnh (TrainWindowScenery); F nói chuyện với hành khách ngồi cạnh. Tàu dừng ở Gakuen-mae rồi tới Minato, người chơi xuống tàu và ra ga tương ứng."),
        ("pair", [(S + "ui_15_on_train.png", "HUD trên tàu"), (S + "ui_16_window_view.png", "Q · ngắm cảnh qua cửa sổ")]),
        ("fig", "diag/d21_train_flow.png", "Workflow chuyến tàu Ga Hibari → Minato", 6.3),
        ("table", ["#", "Người chơi", "Game / UI phản hồi"], [
            ["1", "Vào ga, F với Kimura", "Hỏi đáp tiếng Nhật: giá ¥320, sân số 2"],
            ["2", "Mua vé ở quầy hoặc máy", "Trừ Yen, thêm vé train_ticket_minato vào balo (có ảnh vé)"],
            ["3", "Đi qua cổng", "Cổng mở khi có vé; không có vé thì chặn và nhắc"],
            ["4", "Chờ ở sân 2", "Bảng LED đếm giờ tàu tới, cửa chắn mở khi tàu dừng"],
            ["5", "Lên tàu, ngồi", "HUD trên tàu; Q ngắm cảnh, F trò chuyện"],
            ["6", "Tới Minato", "Ra ga Minato, đi tiếp tới Sushi Hibari"],
        ], [500, 3000, 5520]),
        ("callout", "Kiểm thử.", "Station_FullTripToMinato và Station_GakuenMaeTicketGoesToSchool đi hết chuyến bằng thao tác thật và chụp 12 ảnh mỗi lượt."),
    ]}),
    (15, {"title": "15. UI workflow — Game Center và mini-game", "lead": "Một khung mini-game dùng chung, vé thưởng và quầy đổi quà.", "parts": [
        ("h", "15.1. Game Center"),
        ("p", "Scene 50_GameCenter có biển neon theo khu, poster, đèn rọi màu, các máy chơi (MiniGameLauncher) và quầy quà của Aoi (PrizeCounter). Từ bản đồ chọn Game Center, chỉ đường đưa người chơi tới cửa."),
        ("pair", [(S + "gamecenter_02_arcade_spawn.png", "Sảnh Game Center"), (S + "gamecenter_09b_prize_counter.png", "Quầy đổi quà của Aoi")]),
        ("h", "15.2. Kana Match"),
        ("p", "Người chơi chọn một trong 5 bộ thẻ (hiragana ↔ romaji, katakana ↔ romaji, kanji N5 ↔ cách đọc, đồ ăn, từ vựng), đếm ngược 3-2-1 rồi lật từng cặp. Có combo, điểm trực tiếp, đồng hồ và số cặp còn lại. Kết thúc có sao (★), độ chính xác, kỷ lục theo bộ và vé thưởng game_ticket. Nút × hoặc Esc dừng giữa chừng và hiện bảng kết quả “dừng giữa chừng”."),
        ("pair", [(S + "gamecenter_04_set_picker.png", "Chọn bộ thẻ"), (S + "gamecenter_07_matching.png", "Đang lật cặp, combo và điểm")]),
        ("fig", S + "gamecenter_08_result.png", "Bảng kết quả: sao, độ chính xác, vé thưởng, nút ×", 5.6),
        ("h", "15.3. Kiến trúc mini-game dùng chung"),
        ("p", "MiniGameController khoá di chuyển, đổi camera, tạo trò chơi qua interface IMiniGame và khôi phục mọi thứ khi đóng. Kết quả (MiniGameResult) đi vào ScoringManager (nhóm Vocabulary, ResponseAccuracy) và LearningMasteryManager, nên mini-game đóng góp vào cùng hồ sơ học tập với kịch bản. Word Shooter và Order Rush đã có định nghĩa dữ liệu, chờ làm gameplay."),
        ("fig", "diag/d16_seq_minigame.png", "Sơ đồ tuần tự Kana Match: từ lúc nhấn F tới đổi quà", 6.3),
        ("fig", "diag/d20_state_minigame.png", "Máy trạng thái của Kana Match", 6.3),
    ]}),
    (22, {"title": "22. Quy trình phát triển và phối hợp nhóm", "lead": "Mọi thay đổi đi qua builder, kiểm thử Play Mode và ảnh chụp trước khi được coi là xong.", "parts": [
        ("h", "22.1. Vai trò"),
        ("table", ["Thành viên", "Vai trò"], [
            ["Quách Thành Long (chủ dự án)", "Định hướng, giao yêu cầu bằng ảnh chụp lỗi, duyệt kết quả và commit"],
            ["Claude", "Quản lý nội dung, phòng trọ, thành phố, UI, mini-game; viết thẻ công việc trong Docs/TEAM_TASKS.md"],
            ["Codex", "Kiểm tra độc lập sau mỗi vòng, sửa lỗi phát hiện thêm (lớp học, voice, Agora, chỉ số)"],
        ], [3000, 6020]),
        ("h", "22.2. Quy trình một vòng sửa lỗi"),
        ("p", "Mỗi yêu cầu được ghi thành thẻ có file:dòng và phạm vi rõ. Thay đổi scene không làm tay mà qua builder chạy bằng Unity batchmode (-executeMethod), lưu thẳng vào scene/prefab để mở project bấm Play là đúng. Sau đó chạy toàn bộ Play Mode và EditMode test; ảnh chụp tự động được xem lại từng tấm trước khi cập nhật tài liệu, slide và báo cáo."),
        ("fig", "diag/d24_dev_workflow.png", "Quy trình làm việc và kiểm chứng của nhóm", 6.3),
        ("h", "22.3. Builder chính"),
        ("table", ["Builder (Scripts/Editor)", "Tạo / cập nhật"], [
            ["CityTownBuilder, ThirdPartyAssetIntegrator", "Phố Hibari-chō, konbini, cổng khu vực"],
            ["HomeBedroomBuilder", "Phòng trọ 45_HomeBedroom"],
            ["StationTrainBuilder, StationClerkBuilder", "Ga, tàu, quầy vé và anh Kimura"],
            ["ClassroomBuilder", "Lớp học 40_HIBARICLASS, bàn thi"],
            ["GameCenterBuilder", "Scene 50_GameCenter, máy chơi, quầy quà"],
            ["HorizonBuilder", "Đường chân trời Horizon_Backdrop"],
            ["ItemIconRenderer", "Ảnh render cho các món có mô hình 3D phù hợp"],
            ["TrailerReelSceneBuilder", "Scene 99_Trailer để quay trailer"],
        ], [3800, 5220]),
    ]}),
]

# ─────────── Replacement tables ───────────
TEST_TABLE = (["Bộ kiểm thử (Play Mode)", "Nội dung kiểm tra"], [
    ["GameplayUiPlayModeTests (6 test)", "Hội thoại + nút ×, konbini → giỏ → thu ngân → balo, bản đồ; HUD và cổng; biểu cảm; kịch bản mở đầu; hai chuyến tàu"],
    ["CompletionAuditPlayModeTests", "Chạy thật qua bộ điều khiển, cạn và hồi thể lực, bản lưu chỉ số 0, đóng và tiếp lời dẫn truyện"],
    ["WorldFeelPlayModeTests", "Đường chân trời ngày/chiều/tối, thể lực khi chạy, dáng đi thẳng lưng (đo góc và khoảng cách chân)"],
    ["GameCenterPlayModeTests", "Kana Match trọn vòng 14 điểm kiểm chứng, nút ×, đổi quà"],
    ["SchoolPlayModeTests", "Bàn thi trong lớp, chọn JLPT/IELTS, làm bài, kết quả và nhận xét"],
    ["BedroomPlayModeTests (2 test)", "Mọi tương tác phòng trọ; Play thẳng từ phòng vẫn có HUD và ra phố được"],
    ["CityTown · ZoneTransition · Scenario", "Lối vào konbini và 4 khu; vòng phố → ga → phòng → phố; chạy thử kịch bản"],
    ["FontAtlasDiagnosticTests", "Biển chữ Nhật trong Game Center không mất ký tự"],
    ["TrailerRecordingTests [Explicit]", "Quay khung hình trailer; chỉ chạy khi cần ghi video"],
], [3300, 5720])

BUGS_EXTRA = [
    ["Không thoát được mini-game, lời dẫn truyện", "Chỉ có phím Esc; lời dẫn truyện chặn rời", "Nút × dùng chung cho mọi cửa sổ; lời dẫn giữ node và có nút “Tiếp hội thoại · R”"],
    ["Thấy mép bản đồ và khoảng đen ở chân trời", "Phố lặp theo trục đường nhưng hai bên không có gì", "Horizon_Backdrop: đất, khu nhà ngoại ô, đồi, dải sương theo màu fog"],
    ["Vật phẩm chỉ hiện chữ Hán", "Chưa có ảnh cho item", "Bộ ảnh Resources/Items + ItemIcons cho cửa hàng, giỏ, balo"],
    ["Chạy mà thể lực không giảm", "Hồi 7/giây cả khi chạy, cao hơn mức tốn 4,5/giây; scene còn lưu giá trị cũ", "Không hồi khi chạy, tốn 14/giây, trạng thái kiệt sức; sửa giá trị trong scene"],
    ["Nhân vật đi ẻo, bước chéo", "Clip Mixamo lắc cột sống, chân bước trên một đường", "PostureStabilizer: thẳng lưng, xoay đùi ra 4°"],
    ["HUD gọi sức khỏe là “Thể lực”", "Nhãn đặt nhầm", "Đổi thành Sức khỏe / Thể lực đúng thanh"],
    ["Không bấm được JLPT/IELTS ở bàn thi", "Cửa sổ thi không mở khoá chuột", "MenuPopupBase.LocksGameplay: mở chuột, khoá di chuyển và camera"],
    ["Voice / NPC AI báo lỗi 404", "Model gemini-2.5-flash không còn mở cho tài khoản mới", "Đổi sang gemini-flash-latest (đã gọi thử trả 200)"],
]

# ─────────── Paragraph patches (original chapter index → {old prefix: new text}) ───────────
PATCH_EXTRA = {
    22: {"Snapshot EditorBuildSettings": "Build settings bật 00_Bootstrap, 01_MainMenu, 90_TestSandbox (phố Hibari-chō), 20_StationDistrict, 30_SushiRestaurant, 40_ HIBARICLASS, 45_HomeBedroom và 50_GameCenter. 99_ControlRoom (bảng điều khiển nội bộ) và 99_Trailer (quay trailer) không đưa vào bản chơi. Mọi khu vực được nạp thêm (additive) trên nền thành phố."},
    23: {"Menu/chọn nhân vật": "Các bộ kiểm thử Play Mode hiện có được liệt kê ở cuối chương. Mỗi bộ điều khiển nhân vật bằng thao tác thật (di chuyển, nhấn F, chọn đáp án, bấm nút) thay vì gọi thẳng vào dữ liệu, và chụp ảnh ở từng bước để người duyệt xem lại.",
         "Ảnh Play Mode chứng minh": "Một bài đạt nghĩa là luồng đó chạy đúng trong phiên kiểm thử, kèm ảnh làm bằng chứng. Các phần phụ thuộc dịch vụ ngoài (cuộc gọi Agora giữa hai máy, nhiều người chơi cùng lúc trên Supabase) chưa có bài kiểm thử tự động và được ghi rõ ở mục Hiện trạng tích hợp dịch vụ."},
    25: {"Mức hoàn thiện": "Còn lại: nội dung tiếng Nhật nên được giáo viên bản ngữ duyệt; NPC chưa có lồng tiếng thật; Word Shooter và Order Rush mới có dữ liệu; cuộc gọi Agora giữa hai máy và tải nhiều người trên Supabase chưa được kiểm chứng; mới ba món cơm nắm dùng ảnh chụp thật, các món khác là ảnh minh hoạ.",
         "Ưu tiên 1": "Ưu tiên 1: hoàn thiện Word Shooter và Order Rush trên khung mini-game sẵn có. Ưu tiên 2: thay ảnh minh hoạ vật phẩm bằng ảnh chụp/render thống nhất và thêm lồng tiếng NPC. Ưu tiên 3: kiểm thử Agora và Supabase với nhiều máy thật. Ưu tiên 4: tối ưu và đóng gói WebGL/mobile theo số đo hiệu năng."},
}


# ─────────── Latest automated results (read from the Unity result files) ───────────
def _results():
    import re
    from pathlib import Path
    base = Path(r"D:/VTC_Academy/NihongoLife/NihongoLife/Bao_Cao")
    out = {}
    for key, name in (("play", "final_play.xml"), ("edit", "final_edit.xml")):
        f = base / name
        if f.exists():
            head = re.search(r"<test-run [^>]*", f.read_text(encoding="utf-8")).group(0)
            out[key] = {k: int(v) for k, v in re.findall(r'(total|passed|failed|skipped)="(\d+)"', head)}
    return out


_R = _results()
_p, _e = _R.get("play"), _R.get("edit")
RESULTS_TABLE = (["Mức kiểm tra", "Kết quả (08/10/2026)"], [
    ["Play Mode", f"{_p['passed']}/{_p['total'] - _p['skipped']} đạt" + (f", {_p['failed']} lỗi" if _p and _p['failed'] else "") + f"; {_p['skipped']} bài [Explicit] (quay trailer) chỉ chạy khi cần" if _p else "chưa có kết quả"],
    ["EditMode", f"{_e['passed']}/{_e['total']} đạt (kiểm tra 14 kịch bản và dữ liệu)" if _e else "chưa có kết quả"],
    ["Ảnh tự chụp", "Hơn 150 ảnh trong Bao_Cao/*-regression: thành phố, phòng trọ, ga tàu, lớp học, Game Center, đường chân trời, dáng đi"],
    ["Nội dung", "14 kịch bản, 2 đề thi, 1 thực đơn, 5 bộ thẻ Kana Match, 21 ảnh vật phẩm được nạp đúng"],
    ["Chưa có kiểm thử tự động", "Cuộc gọi Agora giữa hai máy; nhiều người chơi cùng lúc trên Supabase; micro thật"],
], [2600, 6420])
