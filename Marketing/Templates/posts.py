# -*- coding: utf-8 -*-
"""Nội dung 12 tuần. Sự thật lấy từ code và tài liệu trong repo. Không có số liệu người chơi."""

WEEKS = [
    {
        "n": 1,
        "phase": "Chuẩn bị",
        "theme": "Sống ở Nhật để học",
        "goal": "Chốt giọng, link và chỗ đăng. Ba bài đầu nói đúng một câu: học bằng cách sống trong phố.",
        "kpi": "Fanpage có ảnh bìa và 3 bài. Bảng link điền ít nhất link đội ngũ. Chưa cần lượt chơi.",
        "long": "Tạo bảng UTM mẫu (nguồn, chiến dịch, tuần). Kiểm tra trang đội ngũ đang sống. Ghi lại chỗ sẽ gắn bản WebGL, chưa bịa URL.",
        "huy": "Mở Tuan-01, đọc caption, thay {{LINK_*}} nếu đã có. Đăng thứ Ba, năm, bảy. Trả lời mọi bình luận trong ngày.",
        "dung": "Xem CAN_QUAY của trailer móc câu. Nếu máy Unity rảnh, quay shot hoàng hôn theo danh sách. Chưa có thì dùng ảnh sẵn, không vẽ phố giả.",
    },
    {
        "n": 2,
        "phase": "Chuẩn bị",
        "theme": "Một ngày ở Hibari-chō",
        "goal": "Cho người xem thấy ba việc cụ thể: sáng dậy, mua cơm nắm, đi tàu.",
        "kpi": "3 bài ra đúng ngày. Mỗi bài có ít nhất một câu tiếng Nhật kèm nghĩa trong caption.",
        "long": "Nháp một trang «cách mở game» 5 bước, để tuần ra mắt chỉ việc gắn link. Không đụng code gameplay.",
        "huy": "Đăng 3 bài Tuan-02. Ghim bài cơm nắm. Soạn lời mời Discord, chưa spam nhóm trường.",
        "dung": "Quay chuỗi quầy konbini và ga với một trong tám bé. Ảnh tĩnh cũ là người lớn, đã bỏ. Bài là thẻ CẦN ẢNH MỚI.",
    },
    {
        "n": 3,
        "phase": "Teaser và beta kín",
        "theme": "Mời chơi thử",
        "goal": "Mở danh sách beta kín. Nói rõ khách mở cả phố, tài khoản đi lộ trình.",
        "kpi": "Mốc: 30 người ghi danh chơi thử. Đây là mục tiêu, không phải số đã có.",
        "long": "Chuẩn bị bản build để gửi người thử (web hoặc PC). Ghi máy yếu cần biết trước: WebGL nặng.",
        "huy": "Đăng bài mời. Thu tên + máy (web/PC) bằng form hoặc tin nhắn. Không hứa quà nếu chưa có quà.",
        "dung": "Chụp menu và màn chọn tám bé (Haruto mặc định). Không dùng `01_menu.png` — model cũ.",
    },
    {
        "n": 4,
        "phase": "Teaser và beta kín",
        "theme": "Nghe người học",
        "goal": "Beta nói họ kẹt chỗ nào. Đội ghi lại, không sửa gameplay trong đợt marketing này.",
        "kpi": "Mốc: 15 người thử gửi một góp ý cụ thể (một câu). Góp ý vào một file, không biến thành review.",
        "long": "Đọc góp ý kỹ thuật (vào được game không, web có tải không). Ghi lỗi sang TEAM_TASKS, không vá vội trong nhánh marketing.",
        "huy": "Đăng câu hỏi mở. Nhắn từng người thử. Cảm ơn bằng lời, không bịa lời chứng thực để đăng lại.",
        "dung": "Ngồi một buổi với 2 người thử, ghi cảnh họ kẹt (hội thoại, bản đồ, thi). Chỉ quay màn hình thật.",
    },
    {
        "n": 5,
        "phase": "Ra mắt web",
        "theme": "Chơi trên web và PC",
        "goal": "Ngày có link chơi thật thì đăng. Chưa có link thì đổi câu kêu gọi, không dán URL giả.",
        "kpi": "Mốc tuần ra mắt: 80 lượt mở link chơi, 20 tài khoản mới. Đo bằng UTM, chưa có thì ghi «chưa đo».",
        "long": "Gắn link WebGL hoặc bản tải PC vào {{LINK_CHOI}}. Bật thống kê trang nếu có quyền. Kiểm tra một lượt trên trình duyệt.",
        "huy": "Đăng 3 bài ra mắt. Gửi cho CLB và thầy cô đã xin phép. Trả lời «chơi ở đâu» trong vòng một ngày.",
        "dung": "Trailer móc câu 15 giây nếu khung hình trên ổ D đã ghép xong. Chưa xong thì không đăng video đen.",
    },
    {
        "n": 6,
        "phase": "Ra mắt web",
        "theme": "Giờ chơi chung",
        "goal": "Một khung giờ cố định để phố đỡ vắng. Voice và chat nói đúng nền tảng.",
        "kpi": "Mốc: một buổi có ít nhất 5 tài khoản online cùng lúc. Không đủ thì vẫn đăng giờ tuần sau, ghi số thật.",
        "long": "Chọn giờ (gợi ý 20:00 thứ Bảy). Nhắc giới hạn web: realtime và voice đang chắc trên bản máy tính.",
        "huy": "Đăng giờ chơi, nhắc lại trước 3 giờ và trước 15 phút. Sau buổi, kể lại bằng lời, không gắn số bịa.",
        "dung": "Có mặt trong buổi để phố có người. Nếu quay, chỉ quay màn hình game, không quay mặt người khác khi chưa hỏi.",
    },
    {
        "n": 7,
        "phase": "Tăng trưởng",
        "theme": "Luyện theo dạng JLPT N5",
        "goal": "Một thử thách học: vào lớp Hibari, làm một đề luyện, xem trí tuệ tăng trong game.",
        "kpi": "Mốc: 25 tài khoản nộp một bài luyện trong tuần. Chỉ đếm nếu game hoặc người thử báo về. Không ước.",
        "long": "Viết 4 dòng «điểm trong game không phải điểm thi thật» để Huy dán dưới mọi bài tuần này.",
        "huy": "Đăng thử thách. Nhắc không dùng logo kỳ thi. Thu ảnh màn hình người chơi tự gửi, xin phép trước khi đăng lại.",
        "dung": "Quay 20 giây ngồi bàn thi: mở đề, đồng hồ, nộp. Không chèn điểm giả lên hình.",
    },
    {
        "n": 8,
        "phase": "Tăng trưởng",
        "theme": "Luyện IELTS và nói với người thật",
        "goal": "Tách hai việc: đề luyện trong lớp, và nói chuyện online trên bản máy tính.",
        "kpi": "Mốc: 10 người làm một phần Reading hoặc Listening; 1 buổi voice hoặc chat có người ngoài đội.",
        "long": "Kiểm tra voice Agora trên PC trước giờ hẹn. Web chưa cấu hình thì nói trước, không để người học bấm im.",
        "huy": "Caption nào nói IELTS cũng có câu «không phải kết quả chính thức». Mời một nhóm nhỏ, không mua review.",
        "dung": "Quay phòng nói trên PC nếu vào được. Ảnh luyện nói cũ là người lớn, đã bỏ. Chưa có ảnh bé thì giữ thẻ CẦN ẢNH MỚI.",
    },
    {
        "n": 9,
        "phase": "Tăng trưởng",
        "theme": "Game Center và đảo Midori",
        "goal": "Cho thấy học bằng trò chơi, và nói thật là ảnh đảo vẫn chờ quay.",
        "kpi": "Mốc: 20 người kể một máy họ đã chơi (Kana Match, UFO, Kana Rain, Order Rush, Word Shooter, Basket Count).",
        "long": "Liệt kê 6 máy đúng tên trong game lên một ghi chú ngắn để caption không bịa thêm trò.",
        "huy": "Bài Game Center và bài đảo là thẻ CẦN ẢNH MỚI. Không ghép phố hay ảnh người lớn vào đảo.",
        "dung": "Quay đảo: cuốc, gieo, tưới, câu cá. Khung hình để ổ D, không commit video.",
    },
    {
        "n": 10,
        "phase": "Giữ chân",
        "theme": "Một ngày có ăn, chạy, ngủ",
        "goal": "Người đã vào game biết vì sao phải ăn, nghỉ, chào. Giữ nhịp đăng khi hết tuần lễ.",
        "kpi": "Mốc: 40% số người đã đăng ký trong 4 tuần trước còn mở game một lần. Chưa đo được thì ghi «chưa đo», không điền tạm.",
        "long": "Xem lỗi web tuần ra mắt còn ai báo. Cập nhật một dòng «đã biết / chưa sửa» cho Huy trả lời.",
        "huy": "Đăng 3 bài, kể cả 31/12 và 2/1. Nghỉ đăng thì báo trước, không biến mất.",
        "dung": "Quay vòng biểu cảm giữ E, câu tiếng Nhật hiện trên vòng, người chơi là một bé. Không dùng emote.jpg cũ.",
    },
    {
        "n": 11,
        "phase": "Giữ chân và mở rộng",
        "theme": "CLB, lớp và trung tâm",
        "goal": "Một buổi chơi chung với CLB hoặc lớp. Mời trung tâm bằng thư, chưa ký gì.",
        "kpi": "Mốc: 1 buổi offline hoặc online với nhóm có sẵn (lớp, CLB, nhóm bạn học). 0 đồng vẫn làm được.",
        "long": "Soạn một trang sự thật để Huy gửi trung tâm: nền tảng, khách/tài khoản, thi là luyện tập, mã nguồn MIT.",
        "huy": "Xin phép admin nhóm trước khi đăng. Gặp CLB thì mang ảnh in từ tuần này, không in số liệu giả.",
        "dung": "Đứng máy ở buổi CLB nếu cần. Quay màn hình, không quay mặt cả lớp nếu chưa xin.",
    },
    {
        "n": 12,
        "phase": "Giữ chân và mở rộng",
        "theme": "Mở, miễn phí, còn làm tiếp",
        "goal": "Khóa 12 tuần: cảm ơn, nói phần học vẫn miễn phí, liệt kê việc tháng sau không hứa quá những gì đội làm nổi.",
        "kpi": "Buổi tổng kết thứ Hai có đủ 3 người. Bảng đo điền số thật hoặc «chưa đo». Chốt có làm tiếp giờ chơi chung hay không.",
        "long": "Xuất số UTM 12 tuần. Viết 5 dòng «tháng sau làm gì» chỉ gồm việc đã có người nhận.",
        "huy": "Đăng bài tổng kết và bài giải thưởng đúng chữ trên giấy khen. Không thêm giải khác.",
        "dung": "Gói 10 khung với tám bé (phố, hội thoại, thi, tàu, Game Center, menu chọn nhân vật). Không gửi lại ảnh Tanaka/Remy.",
    },
]

POSTS = [
    dict(week=1, slug="01-song-o-nhat", dow=1, kicker="Mở đầu",
         lines=["Học tiếng Nhật", "bằng cách sống ở Nhật"],
         sub="Khu phố Hibari-chō. Mỗi việc trong ngày là một câu nói.",
         cta="Theo dõi để nhận link chơi", meta="Cộng đồng mở",
         shot="docs/readme/horizon_dusk.jpg", pos="center 42%", zoom="1.08", story=True,
         hook="Học tiếng Nhật bằng cách sống ở Nhật.",
         caption="""Học tiếng Nhật bằng cách sống ở Nhật.

NihongoLife đưa bạn vào khu phố Hibari-chō. Chào hàng xóm, mua đồ, hỏi đường, đi tàu, ngồi lớp. Mỗi việc là một tình huống nói. Chọn sai thì được giải thích và chọn lại.

Chơi trên web và trên máy tính. Phần học cốt lõi mở cho cộng đồng. Bọn mình là The Next Generation, ba nhà sáng lập, làm tại VTC Academy.

Link chơi sẽ gắn ở đây khi bản web sẵn sàng:
{{LINK_CHOI}}

#NihongoLife #HocTiengNhat #HibariCho #TiengNhatN5 #SongONhat"""),
    dict(week=1, slug="02-chon-sai", dow=3, kicker="Hội thoại",
         lines=["Chọn sai", "cũng được giải thích"],
         sub="NPC nói lại chỗ lệch. Bạn chọn lần nữa.",
         cta="Xem một câu hội thoại", meta="N5 trong phố",
         shot="Bao_Cao/HinhAnh_Game/g09_dialogue_choice.png", pos="center 42%", zoom="1.06",
         trim=(0.0, 0.0, 0.16, 0.0),
         hook="Chọn sai cũng được giải thích.",
         caption="""Chọn sai cũng được giải thích.

Trong hội thoại, bạn chọn câu trả lời. Sai thì nhân vật chỉ ra chỗ lệch và cho chọn lại. Đóng khung bằng nút × không làm mất bước truyện.

Đây là cách bọn mình muốn người mới đỡ sợ nói. Không có bảng xếp hạng «ai giỏi hơn» trong bài này. Chỉ có một việc: nói lại cho đúng.

{{LINK_FANPAGE}}

#NihongoLife #HocTiengNhat #HoiThoai #TiengNhatN5 #HibariCho"""),
    dict(week=1, slug="03-cau-nhat-nghia-viet", dow=5, kicker="Ba lớp chữ",
         lines=["Câu Nhật,", "nghĩa Việt ngay dưới"],
         sub="Tuỳ chế độ, có thêm cách đọc và romaji.",
         cta="Bật Hướng dẫn khi mới học", meta="Việt · Anh · Nhật",
         shot="docs/readme/dialogue.jpg", pos="center 48%", zoom="1.05",
         hook="Câu tiếng Nhật hiện ra, nghĩa tiếng Việt nằm ngay dưới.",
         caption="""Câu tiếng Nhật hiện ra. Nghĩa tiếng Việt nằm ngay dưới.

Khung hội thoại dùng chung mọi nơi. Tuỳ chế độ Hướng dẫn, Luyện tập hay Kiểm tra, bạn thấy thêm cách đọc và romaji. Giao diện đổi được Việt, Anh hoặc Nhật trong phần cài đặt.

Bài này không phải mẹo «học 100 từ một đêm». Một câu, hiểu câu đó, rồi đi tiếp.

#NihongoLife #HocTiengNhat #Hiragana #TiengNhatN5 #HibariCho"""),

    dict(week=2, slug="01-bay-gio-sang", dow=1, kicker="Buổi sáng",
         lines=["Ngủ dậy lúc 7 giờ", "trong phòng trọ"],
         sub="Giường đưa đồng hồ sang sáng hôm sau.",
         cta="Vào nhà nói ただいま", meta="Phòng 1K",
         shot="docs/readme/morning.jpg", pos="center 40%", zoom="1.06", story=True,
         hook="Ngủ dậy lúc 7 giờ, trong phòng trọ của bạn.",
         caption="""Ngủ dậy lúc 7 giờ, trong phòng của bạn.

Giường trong phòng trọ cho nhân vật nằm xuống. Màn hình tối, đồng hồ trong game nhảy sang sáng hôm sau, mặc định 7 giờ. Đói, mệt được nghỉ. Về nhà có câu ただいま.

Phòng mở khi bạn tới Hibari. Đây là nhà, không phải màn hình menu.

#NihongoLife #HocTiengNhat #PhongTro #HibariCho #SongONhat"""),
    dict(week=2, slug="02-com-nam", dow=3, kicker="Konbini",
         lines=["Mua cơm nắm", "bằng tiếng Nhật"],
         sub="Kệ có ảnh món, tên Nhật và giá yên.",
         cta="Ra quầy レジ để trả tiền", meta="Hibari Mart",
         shot="docs/readme/shop.jpg", pos="center 45%", zoom="1.08",
         footage=[
             ("5–12 giây", "Vào Hibari Mart", "Đi theo sau nhân vật tới kệ cơm nắm", "Mua cơm nắm bằng tiếng Nhật.", "Cảnh phố rồi cửa hàng. Không thanh debug."),
             ("8 giây", "Quầy tính tiền", "Gần chị Ito", "こんにちは. Trả lời câu hỏi túi.", "Dùng hội thoại thật trong game."),
         ],
         hook="Một phút mua cơm nắm: thấy món, bỏ giỏ, ra quầy.",
         caption="""Một phút mua cơm nắm.

Vào Hibari Mart, đứng trước kệ, xem ảnh món, tên tiếng Nhật và giá. Bỏ vào giỏ, ra quầy レジ. Chị Ito tính tiền và hỏi túi. Bạn trả lời bằng tiếng Nhật.

Mua được bữa đầu tiên là bước mở quán sushi ở lộ trình tài khoản: cần 100 trí tuệ và xong việc mua ở konbini. Khách chơi ngoại tuyến thì phố mở sẵn.

#NihongoLife #HocTiengNhat #Konbini #ComNam #HibariCho"""),
    dict(week=2, slug="03-ve-tau", dow=5, kicker="Ga Hibari",
         lines=["Hỏi giá vé", "đi ga Minato"],
         sub="Trong truyện, vé tới Minato là 320 yên.",
         cta="Sân số 2, rồi lên tàu", meta="さんびゃくにじゅうえん",
         shot="docs/readme/train.jpg", pos="center 50%", zoom="1.04",
         footage=[
             ("8 giây", "Quầy vé", "Đối mặt anh Kimura", "Đến Minato bao nhiêu yên?", "Câu 三百二十円です。"),
             ("8 giây", "Cổng và sân", "Đi theo người chơi", "Sân số 2.", "Không cắt cảnh ngoài game."),
             ("6 giây", "Trong toa", "Nhìn bảng LED rồi cửa sổ", "Xuống ở Minato để ăn sushi.", "Có thể nhấn Q ngắm cửa sổ khi quay tay."),
         ],
         hook="Đến ga Minato giá 320 yên. Nói lại được không?",
         caption="""Đến ga Minato giá 320 yên. Nói lại được không?

Trong tình huống ga, anh Kimura báo giá 三百二十円 (320 yên), khoảng 20 phút. Nhầm thành 230 yên là một đáp án sai có giải thích, vì thứ tự chữ số khác nhau.

Với tài khoản đang đi lộ trình, ga mở khi đủ 160 trí tuệ và đã ăn ở quán sushi. Khách ngoại tuyến đi được cả phố.

Ảnh tàu cũ có người chơi người lớn, đã bỏ. Bài này là thẻ chữ ghi CẦN ẢNH MỚI cho tới khi quay lại với một trong tám bé. Video cả đoạn mua vé vẫn chờ quay.

#NihongoLife #HocTiengNhat #DiTau #GaHibari #TiengNhatN5"""),

    dict(week=3, slug="01-moi-choi-thu", dow=1, kicker="Beta kín",
         lines=["Muốn chơi thử", "cùng bọn mình?"],
         sub="Một nhóm nhỏ, trước khi mở rộng.",
         cta="Nhắn «chơi thử» cho fanpage", meta="Không mất phí",
         shot="docs/readme/horizon_day.jpg", pos="center 40%", zoom="1.06", story=True,
         hook="Muốn chơi thử cùng bọn mình không?",
         caption="""Muốn chơi thử cùng bọn mình không?

Bọn mình mở một đợt nhỏ để nghe người đang học N5. Bạn cần nói mình chơi trên web hay máy tính, và đang học để đi học, đi làm hay vì thích.

Chưa có quà. Chưa có bảng xếp hạng công khai. Có một việc: chơi thật và gửi một câu góp ý. Bọn mình không biến câu đó thành lời khen trên bài viết.

Nhắn «chơi thử» tại {{LINK_FANPAGE}}

#NihongoLife #HocTiengNhat #ChoiThu #TiengNhatN5 #HibariCho"""),
    dict(week=3, slug="02-khach-mo-pho", dow=3, kicker="Chơi khách",
         lines=["Không có tài khoản", "vẫn đi được cả phố"],
         sub="Khách chơi ngoại tuyến, save nằm trên máy.",
         cta="Nút «Chơi Khách» ở cửa đăng nhập", meta="Full phố",
         shot="docs/readme/map.jpg", pos="center 28%", zoom="1.18",
         hook="Không muốn đăng ký ngay? Vào khách, cả phố vẫn mở.",
         caption="""Không muốn đăng ký ngay? Vào bằng khách, cả phố vẫn mở.

Khách chơi ngoại tuyến: Hibari Mart, trường, ga, quán, Game Center, đảo đều vào được, và không bị giữ ở đoạn truyện đầu. Save khách nằm trên máy.

Muốn chat, kết bạn, voice và đi đúng lộ trình trí tuệ thì đăng ký tài khoản. Bấm chơi online khi chưa đăng nhập sẽ được mời đăng ký.

#NihongoLife #HocTiengNhat #ChoiKhach #HibariCho #TiengNhatN5"""),
    dict(week=3, slug="03-lo-trinh", dow=5, kicker="Tài khoản",
         lines=["Đăng ký để đi", "đúng lộ trình"],
         sub="Trí tuệ và nhiệm vụ mở từng nơi.",
         cta="Sổ nhiệm vụ phím N", meta="Sushi 100 · Ga 160",
         shot="Bao_Cao/HinhAnh_Game/g04_map.png", pos="center 40%", zoom="1.06",
         trim=(0.0, 0.0, 0.18, 0.0),
         hook="Tài khoản không mở hết phố ngay. Trí tuệ mở từng nơi.",
         caption="""Tài khoản không mở hết phố ngay. Trí tuệ mở từng nơi.

Hibari Mart và trường mở từ đầu. Phòng trọ có khi bạn tới. Quán sushi cần 100 trí tuệ và đã mua được bữa ở konbini. Ga cần 160, sau bữa sushi. Game Center cần 240, sau chuyến tàu. Đảo Midori cần 320, sau buổi Game Center với Kim.

Trí tuệ tăng khi học, làm bài, làm nhiệm vụ. Sổ nhiệm vụ (phím N) và bản đồ (phím M) ghi chỗ còn thiếu.

#NihongoLife #HocTiengNhat #TriTue #LoTrinh #HibariCho"""),

    dict(week=4, slug="01-ket-cho-nao", dow=1, kicker="Hỏi thật",
         lines=["Bạn đang kẹt", "chỗ nào trong N5?"],
         sub="Viết một chỗ. Bọn mình đọc.",
         cta="Bình luận một chữ: kana, hội thoại, nghe", meta="Beta tuần 4",
         shot="docs/readme/study.jpg", pos="center 42%", zoom="1.05", story=True,
         hook="Bạn đang kẹt chỗ nào khi học N5?",
         caption="""Bạn đang kẹt chỗ nào khi học N5?

Kana, hội thoại, nghe, hay việc nhớ từ sau một ngày? Viết một chỗ trong bình luận. Bọn mình đang cho một nhóm nhỏ chơi thử và muốn nghe chỗ game chưa đỡ được.

Bọn mình sẽ không cắt bình luận của bạn thành «review 5 sao». Nếu bạn muốn câu chữ của mình lên một bài sau, bọn mình hỏi lại.

#NihongoLife #HocTiengNhat #TiengNhatN5 #N5 #HibariCho"""),
    dict(week=4, slug="02-cach-doc", dow=3, kicker="Hướng dẫn",
         lines=["Có cách đọc", "khi bạn cần"],
         sub="Hướng dẫn hiện romaji. Kiểm tra thì giấu bớt.",
         cta="Đổi chế độ trong hội thoại", meta="Không phải đề thi thật",
         shot="docs/readme/dialogue.jpg", pos="center 62%", zoom="1.12",
         hook="Mới học thì bật cách đọc. Muốn tự nhớ thì tắt đi.",
         caption="""Mới học thì bật cách đọc. Muốn tự nhớ thì tắt đi.

Chế độ Hướng dẫn hiện câu Nhật, cách đọc, romaji và nghĩa. Luyện tập bớt gợi ý. Kiểm tra để bạn tự chọn. Cùng một khung hội thoại, ba mức.

Đây là lựa chọn trong game, không phải chứng chỉ. Học tới đâu, bật tới đó.

#NihongoLife #HocTiengNhat #Romaji #Hiragana #TiengNhatN5"""),
    dict(week=4, slug="03-mot-viec", dow=5, kicker="Nhịp học",
         lines=["Một việc nhỏ", "mỗi ngày"],
         sub="Mua đồ, chào, hoặc mười phút ở bàn học.",
         cta="Mai làm một việc là đủ", meta="Đừng nhồi",
         shot="docs/readme/bedroom.jpg", pos="center 45%", zoom="1.06",
         hook="Đừng học một mạch cho xong. Một việc trong phố là đủ một ngày.",
         caption="""Đừng học một mạch cho xong. Một việc trong phố là đủ một ngày.

Hôm nay mua cơm nắm. Mai hỏi giá vé. Ngày kia ngồi bàn học trong phòng. Game có cả thi và nhiệm vụ, nhưng người mới không cần làm hết trong một tối.

Bọn mình làm game này vì lịch học đã đầy. Bài đăng cũng vậy: một việc, một câu.

#NihongoLife #HocTiengNhat #MoiNgay #HibariCho #TiengNhatN5"""),

    dict(week=5, slug="01-ra-mat-web", dow=1, kicker="Ra mắt",
         lines=["Chơi trên web", "và trên máy tính"],
         sub="Link thật mới được dán. Chưa có thì hẹn lại.",
         cta="Theo dõi để nhận link chơi", meta="WebGL · PC",
         shot="Bao_Cao/HinhAnh_Game/01_menu.png", pos="center 35%", zoom="1.08", story=True,
         trim=(0.02, 0.0, 0.14, 0.0),
         hook="Hôm nay chơi được trên web và trên máy tính.",
         caption="""Hôm nay chơi được trên web và trên máy tính.

NihongoLife chạy bằng Unity, bản web WebGL và bản Windows. Vào phố Hibari-chō, học bằng việc hàng ngày. Phần học cốt lõi không bán lẻ từng bài.

Nếu dòng dưới còn là mã {{LINK_CHOI}} thì Long chưa gắn link. Đừng đăng bài này cho tới khi có URL thật. Ảnh menu cũ không còn đúng tám bé. Thẻ Facebook ghi CẦN ẢNH MỚI cho tới khi có màn chọn nhân vật hiện tại.

{{LINK_CHOI}}

#NihongoLife #HocTiengNhat #ChoiNgay #WebGL #HibariCho"""),
    dict(week=5, slug="02-hoang-hon", dow=3, kicker="Hibari-chō",
         lines=["Hoàng hôn xuống", "đèn phố bật"],
         sub="Cùng một phố, khác một giờ.",
         cta="Ở lại tới tối trong game", meta="Giờ trong phố",
         shot="docs/readme/horizon_dusk.jpg", pos="center 55%", zoom="1.1",
         footage=[
             ("0–4 giây", "Phố lúc hoàng hôn", "Crane chậm, không giật", "Học tiếng Nhật bằng cách sống ở Nhật.", "Letterbox. Shot mở trailer."),
             ("3 giây", "Đèn đường", "Cắt gần một cột đèn", "Ở lại tới tối.", "Không màn đen dài."),
         ],
         hook="Ở lại phố tới lúc đèn đường bật.",
         caption="""Ở lại phố tới lúc đèn đường bật.

Trong game, trời đổi theo giờ. Hoàng hôn, cửa sổ sáng, rồi đêm. Ảnh này chụp từ phố thật trong dự án, không phải tranh vẽ.

Video bay qua phố vẫn nằm trong danh sách cần quay. Ảnh tĩnh đăng được ngay.

#NihongoLife #HocTiengNhat #HoangHon #HibariCho #SongONhat"""),
    dict(week=5, slug="03-lop-hibari", dow=5, kicker="Trường Hibari",
         lines=["Vào lớp", "luyện theo dạng đề"],
         sub="Chọn JLPT N5 hoặc IELTS. Điểm game không phải điểm thi.",
         cta="Ngồi bàn thi hoặc nhấn K", meta="Không logo kỳ thi",
         shot="docs/readme/exam_centre.jpg", pos="center 42%", zoom="1.06",
         hook="Ngồi vào lớp Hibari và luyện một đề, ngay trong game.",
         caption="""Ngồi vào lớp Hibari và luyện một đề, ngay trong game.

Bạn chọn luyện theo dạng JLPT N5 hoặc theo dạng IELTS. Có đồng hồ. Nộp bài xong, cô Morita nhận xét. Điểm này cộng trí tuệ trong game. Nó không phải kết quả JLPT hay IELTS chính thức, và bọn mình không gắn logo của kỳ thi.

Trường mở từ đầu, không cần chờ trí tuệ.

#NihongoLife #HocTiengNhat #LuyenDe #TiengNhatN5 #TruongHibari"""),

    dict(week=6, slug="01-gio-choi-chung", dow=1, kicker="Hẹn giờ",
         lines=["Tối nay", "vào phố cùng nhau"],
         sub="Một giờ cố định. Phố đỡ vắng.",
         cta="20:00 thứ Bảy · bản máy tính để gặp nhau", meta="Giờ chơi chung",
         shot="Bao_Cao/online-regression/crowd_target_first.jpg", pos="center 48%", zoom="1.08", story=True,
         trim=(0.08, 0.0, 0.16, 0.0),
         footage=[
             ("6 giây", "Hai người trên phố", "Camera sau vai", "Tối nay vào phố cùng nhau.", "Chỉ quay khi có tài khoản online thật. Không spawn giả."),
             ("4 giây", "Danh thiếp người chơi", "UI thật", "Kết bạn.", "Cắt bỏ mọi dòng debug."),
         ],
         hook="Tối nay vào phố cùng nhau. Một giờ, cho đỡ vắng.",
         caption="""Tối nay vào phố cùng nhau. Một giờ, cho đỡ vắng.

Online cần tài khoản đã đăng ký. Trên bản máy tính bạn thấy người khác, chat, kết bạn. Voice khu vực: giữ phím T (phím mặc định) khi đang ở chỗ công cộng.

Bản web chơi được phần phố. Phần gặp nhau realtime bọn mình đang chắc trên máy tính. Đến giờ mà web chưa vào phòng được, hãy mở bản PC và nói trước trong bài.

Giờ mặc định đội đề xuất: 20:00 thứ Bảy. Đổi giờ thì sửa caption trước khi đăng.

#NihongoLife #HocTiengNhat #ChoiChung #HibariCho #Online"""),
    dict(week=6, slug="02-chat-khu-vuc", dow=3, kicker="Chat",
         lines=["Chat khu vực", "khi đang online"],
         sub="Tin hiện lúc bạn chơi, không lưu thành nhật ký.",
         cta="Bấm tên người gửi để nhắn riêng", meta="Bản máy tính",
         shot="Bao_Cao/online-regression/chat_area.jpg", pos="center 78%", zoom="1.25",
         footage=[
             ("8 giây", "Mở chat khu vực", "UI thật, đã cắt banner dev", "Một kênh cho người đang online.", "Không gõ mật khẩu, không gõ tên người thật ngoài đội."),
         ],
         hook="Một kênh chat cho người đang đứng trong phố.",
         caption="""Một kênh chat cho người đang đứng trong phố.

Kênh chung hiện với người đang online. Tin không lưu thành nhật ký sau khi bạn thoát. Lịch sự, không gửi thông tin cá nhân. Bấm tên người gửi để nhắn riêng, kết bạn hoặc chặn.

Ảnh chat cũ có nhân vật người lớn, đã bỏ. Bài này là thẻ CẦN ẢNH MỚI. Video sạch vẫn cần quay lại.

#NihongoLife #HocTiengNhat #Chat #HibariCho #Online"""),
    dict(week=6, slug="03-giu-t", dow=5, kicker="Voice · PC",
         lines=["Giữ T để nói", "với người đứng gần"],
         sub="Voice khu vực trên bản máy tính.",
         cta="Đứng chỗ công cộng, giữ T", meta="Không phải bản web",
         shot="Bao_Cao/online-regression/player_card.jpg", pos="center 55%", zoom="1.08",
         footage=[
             ("5 giây", "Thanh voice", "Khi giữ T", "Giữ T để nói.", "Quay trên PC đã cấu hình Agora. Không lộ App ID."),
             ("5 giây", "Hai người cách vài mét", "Nghe tiếng theo khoảng cách", "Nghe người đứng gần.", "Xin phép người cùng quay."),
         ],
         hook="Giữ T để nói. Người đứng gần nghe thấy.",
         caption="""Giữ T để nói. Người đứng gần nghe thấy.

Voice khu vực chạy trên bản máy tính, trong chỗ công cộng, tài khoản đã đăng ký. Phím mặc định là T. Phòng riêng thì voice khu vực tắt. Đang mở phòng Speaking thì đóng phòng đó trước.

Bọn mình chưa gọi đây là tính năng web. Khi web nói được, sẽ có bài khác, không sửa lại lịch sử bài này.

#NihongoLife #HocTiengNhat #LuyenNoi #Voice #HibariCho"""),

    dict(week=7, slug="01-dang-n5", dow=1, kicker="Thử thách",
         lines=["Luyện một đề", "theo dạng N5"],
         sub="Trong lớp. Điểm chỉ tính trong game.",
         cta="Ngồi vào bàn, hoặc nhấn K", meta="Không phải điểm thi thật",
         shot="docs/readme/exam_desk.jpg", pos="center 48%", zoom="1.06", story=True,
         footage=[
             ("4 giây", "Ngồi bàn", "Tay bấm F", "Ngồi vào bàn thi.", "Lớp Hibari."),
             ("10 giây", "Làm vài câu", "Thấy đồng hồ", "Luyện theo dạng N5.", "Không tua tới điểm giả."),
             ("4 giây", "Màn hình xong", "Dòng trí tuệ thật", "Trí tuệ tăng trong game.", "Che số nếu không muốn công bố. Không thêm sao."),
         ],
         hook="Tuần này làm một đề luyện theo dạng N5, trong game.",
         caption="""Tuần này làm một đề luyện theo dạng N5, trong game.

Vào trường Hibari, ngồi bàn hoặc nhấn K, chọn phần JLPT N5. Làm tới hết giờ hoặc tới khi bạn nộp. Cô Morita nhận xét. Game cộng trí tuệ.

Điểm đó không quy đổi thành chứng chỉ. Bọn mình không đăng bảng «ai đỗ». Bạn tự giữ kết quả của mình.

#NihongoLife #HocTiengNhat #LuyenDe #TiengNhatN5 #TruongHibari"""),
    dict(week=7, slug="02-tri-tue", dow=3, kicker="Trí tuệ",
         lines=["Học xong", "trí tuệ tăng"],
         sub="Lớp, bàn học, bài thi, nhiệm vụ, Game Center.",
         cta="Mở sổ nhiệm vụ phím N", meta="Không gọi là kiến thức",
         shot="docs/readme/teacher.jpg", pos="center 40%", zoom="1.05",
         hook="Trong game, thứ bạn kiếm được gọi là trí tuệ.",
         caption="""Trong game, thứ bạn kiếm được gọi là trí tuệ.

Lớp Hibari, bàn học ở nhà, bài luyện, nhiệm vụ và Game Center đều có thể cộng trí tuệ. Bản đồ và cửa ghi rõ còn thiếu bao nhiêu. Đủ mức và xong bước truyện thì một nơi mới mở.

Bọn mình dùng đúng chữ trong giao diện tiếng Việt: trí tuệ. Không đổi thành một từ khác trên bài đăng.

#NihongoLife #HocTiengNhat #TriTue #LoTrinh #HibariCho"""),
    dict(week=7, slug="03-mo-sushi", dow=5, kicker="Sushi Hibari",
         lines=["Gọi được một đĩa", "ở quán sushi"],
         sub="Tài khoản cần 100 trí tuệ và đã mua cơm nắm.",
         cta="Mở menu quán và chọn món", meta="Sau konbini",
         shot="Bao_Cao/HinhAnh_Game/g08_restaurant_menu.png", pos="center 42%", zoom="1.05",
         trim=(0.0, 0.0, 0.16, 0.0),
         hook="Bước kế tiếp sau konbini: gọi món ở quán sushi.",
         caption="""Bước kế tiếp sau konbini: gọi món ở quán sushi.

Với tài khoản, quán mở khi bạn có 100 trí tuệ và đã tự mua được một bữa ở Hibari Mart. Trong quán, menu là hội thoại: gọi món, nghe lại, sửa nếu chọn sai.

Khách ngoại tuyến vào quán được luôn. Bài này nói về lộ trình của người đã đăng ký.

#NihongoLife #HocTiengNhat #Sushi #GoiMon #HibariCho"""),

    dict(week=8, slug="01-dang-ielts", dow=1, kicker="Dạng IELTS",
         lines=["Luyện viết", "theo dạng IELTS"],
         sub="Bài trong game. Không phải bài thi thật.",
         cta="Mở phần IELTS trong lớp", meta="Không logo kỳ thi",
         shot="Bao_Cao/ielts-productive/02_writing_examiner.jpg", pos="center 42%", zoom="1.05", story=True,
         hook="Lớp Hibari có bài luyện viết theo dạng IELTS.",
         caption="""Lớp Hibari có bài luyện viết theo dạng IELTS.

Ảnh này là màn luyện viết trong game: đề, bài làm, nhận xét. Reading và Listening cũng có trong lớp, khi máy đã có gói đề. Điểm trong game không phải band chính thức. Bọn mình không gắn logo của kỳ thi hay của tổ chức khảo thí.

Đề bản quyền không nằm trên Git. Máy nào thiếu gói đề thì phần đó không mở. Bài đăng không hứa mọi máy đều có đủ đề.

#NihongoLife #HocTiengNhat #LuyenIELTS #TruongHibari #LuyenViet"""),
    dict(week=8, slug="02-ket-ban", dow=3, kicker="Người thật",
         lines=["Kết bạn", "rồi rủ nhau học"],
         sub="Danh thiếp, nhắn riêng, chặn nếu cần.",
         cta="Bản máy tính · tài khoản đã đăng ký", meta="Có nút chặn",
         shot="Bao_Cao/online-regression/chat_friends.jpg", pos="center 70%", zoom="1.2",
         footage=[
             ("6 giây", "Danh sách bạn", "UI bạn bè thật", "Kết bạn, rồi rủ nhau học.", "Crop mọi banner dev."),
             ("4 giây", "Lời mời", "Bấm kết bạn", "Rủ một câu tiếng Nhật.", "Không lộ email."),
         ],
         hook="Kết bạn trong phố, rồi rủ nhau một câu tiếng Nhật.",
         caption="""Kết bạn trong phố, rồi rủ nhau một câu tiếng Nhật.

Trên bản máy tính, tài khoản đã đăng ký mở được bạn bè và chat. Bấm tên một người để nhắn riêng, gửi lời mời kết bạn, hoặc chặn. Chặn thì ẩn tin và lời mời của người đó.

Bọn mình không có người kiểm duyệt trực 24 giờ. Giờ chơi chung có mặt một người trong đội. Ngoài giờ, hãy chặn và báo trong Discord của nhóm.

{{LINK_DISCORD}}

#NihongoLife #HocTiengNhat #KetBan #LuyenNoi #HibariCho"""),
    dict(week=8, slug="03-luyen-noi", dow=5, kicker="Phần nói",
         lines=["Luyện phần nói", "ngay trong game"],
         sub="Thẻ chữ. Ảnh luyện nói cũ đã bỏ.",
         cta="Micro trên bản máy tính", meta="PC",
         shot="Bao_Cao/ielts-productive/06_speaking_part1.jpg", pos="center 45%", zoom="1.05",
         footage=[
             ("10 giây", "Phòng Speaking với giáo viên", "Chỉ khi Agora đã cấu hình", "Nói với người thật, trên PC.", "Không quay nếu phòng không vào được. Không lộ App ID."),
         ],
         hook="Có bài luyện nói trong game. Phòng với giáo viên thì quay riêng, trên PC.",
         caption="""Có bài luyện nói trong game. Phòng video với giáo viên là một cửa khác, trên bản máy tính.

Ảnh luyện nói cũ có nhân vật người lớn, đã bỏ. Bài này là thẻ CẦN ẢNH MỚI. Đó không phải cuộc gọi với giáo viên. Phòng Speaking dùng video trên PC khi máy đã cấu hình. Bản web không có micro theo kiểu ứng dụng máy tính. Bọn mình không đăng một khung hình giả để cho có.

Khi nào quay được phòng thật, video để ở ổ D và gắn link, không nhét file nặng vào git.

#NihongoLife #HocTiengNhat #LuyenNoi #LuyenIELTS #HibariCho"""),

    dict(week=9, slug="01-game-center", dow=1, kicker="Sau giờ học",
         lines=["Tan học", "Kim rủ Game Center"],
         sub="Ghép kana, lấy sao, đổi vé.",
         cta="Mở khi đủ 240 trí tuệ trên tài khoản", meta="Có vé thưởng",
         shot="docs/readme/kana_result.jpg", pos="center 42%", zoom="1.05", story=True,
         footage=[
             ("4 giây", "Kim rủ", "Hội thoại thật", "Đi Game Center không?", "scenario arcade."),
             ("8 giây", "Một ván Kana Match", "Thấy thẻ kana", "Ghép hiragana.", "Không tăng tốc giả."),
         ],
         hook="Tan học, Kim rủ ra Game Center.",
         caption="""Tan học, Kim rủ ra Game Center.

Tài khoản mở được nơi này khi đủ 240 trí tuệ và đã đi tàu. Trong truyện, Kim rủ bạn đếm lần chơi, đếm vé, và nói thứ mình muốn. Kana Match lật thẻ hiragana, katakana, kanji N5 và từ vựng. Hết ván có sao, độ chính xác và vé.

Vé đổi quà ở quầy của Aoi. Ảnh kết quả cũ có người chơi người lớn, đã bỏ. Bài này là thẻ CẦN ẢNH MỚI.

#NihongoLife #HocTiengNhat #GameCenter #Kana #TiengNhatN5"""),
    dict(week=9, slug="02-sau-may", dow=3, kicker="Sáu máy",
         lines=["Sáu máy chơi", "để nhớ chữ"],
         sub="Kana Match, Word Shooter, Order Rush, Kana Rain, UFO Catcher, Basket Count.",
         cta="Chọn một máy, chơi một ván", meta="Đúng tên trong game",
         shot="docs/readme/prize.jpg", pos="center 40%", zoom="1.06",
         hook="Sáu máy trong Game Center. Chọn một máy, chơi một ván.",
         caption="""Sáu máy trong Game Center. Chọn một máy, chơi một ván.

Đúng tên trong game: Kana Match, Word Shooter, Order Rush, Kana Rain, UFO Catcher, Basket Count. Vé thắng mang ra quầy đổi quà. Kết quả ghi vào hồ sơ học, cùng chỗ với các bài hội thoại.

Bọn mình không thêm máy thứ bảy trên bài đăng. Ảnh quầy quà cũ có người chơi người lớn, đã bỏ. Bài này là thẻ CẦN ẢNH MỚI. Cận cảnh gắp thú vẫn chờ quay.

#NihongoLife #HocTiengNhat #GameCenter #KanaMatch #HibariCho"""),
    dict(week=9, slug="03-dao-midori", dow=5, kicker="Đảo Midori", layout="type",
         lines=["Đảo Midori", "cuốc, gieo, câu cá"],
         sub="Ảnh đảo chưa có trong bản đăng. Dưới đây là việc thật trong game.",
         cta="Ảnh sẽ đăng khi quay xong", meta="Chưa có hình giả",
         shot="", pos="center", zoom="1",
         bullets=["Mua đúng cuốc, xẻng, bình tưới", "Gieo, tưới, rồi mới thu", "Câu cá cần có cần câu"],
         footage=[
             ("6 giây", "Nhìn toàn đảo", "Từ cao, trời trong", "Đảo Midori.", "Không dùng ảnh phố để thế."),
             ("8 giây", "Xới và gieo", "Thấy nông cụ trên tay", "Không làm tay không.", "Công cụ hao độ bền khi làm xong."),
             ("8 giây", "Câu cá", "Quăng phao, kéo đúng lúc", "Kéo bằng F.", "Kéo sớm thì không có cá."),
         ],
         hook="Đảo Midori có nông trại và chỗ câu. Ảnh thật sẽ đăng khi quay xong.",
         caption="""Đảo Midori có nông trại và chỗ câu. Ảnh thật sẽ đăng khi quay xong.

Bài này cố ý không ghép một cảnh khác. Việc trong game, với tài khoản: đảo mở ở 320 trí tuệ, sau buổi Game Center với Kim. Trồng trọt cần mua đúng cuốc, xẻng hoặc bình tưới. Làm dở thì ruộng không đổi và không mất độ bền. Câu cá cần cần câu. Kéo đúng lúc mới có cá. Cá vào balo, bán được ở cửa hàng trên đảo.

Khách ngoại tuyến vào đảo được. File ảnh đảo trong repo hiện chưa tải đủ nên bọn mình không dùng.

#NihongoLife #HocTiengNhat #DaoMidori #NongTrai #HibariCho"""),

    dict(week=10, slug="01-an-chay-ngu", dow=1, kicker="Chỉ số sống",
         lines=["Chạy, đói, khát", "rồi về nhà nghỉ"],
         sub="Balo cho biết bạn đang ổn hay không.",
         cta="Mở balo phím B", meta="Ăn và ngủ để hồi",
         shot="docs/readme/bag.jpg", pos="center 45%", zoom="1.06", story=True,
         hook="Chạy nhiều thì đói. Về phòng để ngủ.",
         caption="""Chạy nhiều thì đói. Về phòng để ngủ.

HUD có sức khỏe, thể lực, no, khát, tỉnh táo. Chạy tốn thể lực. Hết sức thì không chạy tiếp cho tới khi hồi một phần. Ăn và uống trong balo. Ngủ ở giường đưa sang sáng.

Đây là nhịp của phố, không phải hình phạt. Một ngày trong game giống một ngày cần ăn và nghỉ.

#NihongoLife #HocTiengNhat #ChiSo #PhongTro #HibariCho"""),
    dict(week=10, slug="02-chao-bang-e", dow=3, kicker="Biểu cảm",
         lines=["Giữ E", "để chào bằng tiếng Nhật"],
         sub="Vòng biểu cảm có câu đi kèm.",
         cta="Giữ E, chọn một câu", meta="Không cần chat",
         shot="docs/readme/emote.jpg", pos="center 45%", zoom="1.05",
         footage=[
             ("6 giây", "Vòng biểu cảm", "Giữ E", "Chào bằng một câu có sẵn.", "Quay cả câu tiếng Nhật trên vòng."),
         ],
         hook="Chưa kịp gõ chat? Giữ E và chào bằng một câu có sẵn.",
         caption="""Chưa kịp gõ chat? Giữ E và chào bằng một câu có sẵn.

Vòng biểu cảm gắn với câu tiếng Nhật. Dùng khi đứng cạnh người khác hoặc chỉ để tập miệng. Ảnh vòng cũ có người chơi người lớn, đã bỏ. Bài này là thẻ CẦN ẢNH MỚI cho tới khi chụp lại với một bé.

Chat chữ và voice là cửa khác, cần tài khoản và bản máy tính. Biểu cảm thì làm được cả khi chơi một mình.

#NihongoLife #HocTiengNhat #BieuCam #ChaoHoi #HibariCho"""),
    dict(week=10, slug="03-dem-pho", dow=5, kicker="Ban đêm",
         lines=["Đêm ở Hibari", "cửa sổ còn sáng"],
         sub="Phố không tắt hết khi hết giờ học.",
         cta="Đi một vòng rồi về ngủ", meta="Đúng giờ trong game",
         shot="docs/readme/horizon_night.jpg", pos="center 40%", zoom="1.08",
         hook="Đêm ở Hibari, cửa sổ vẫn sáng.",
         caption="""Đêm ở Hibari, cửa sổ vẫn sáng.

Trời tối, đèn trong nhà bật, phố vẫn đi được. Bạn có thể về phòng ngủ để sang ngày mới, hoặc ở ngoài thêm một lát.

Ảnh đêm này chụp từ game. Không thêm hiệu ứng ngoài.

#NihongoLife #HocTiengNhat #BanDem #HibariCho #SongONhat"""),

    dict(week=11, slug="01-buoi-clb", dow=1, kicker="CLB",
         lines=["Một buổi CLB", "cùng vào một phố"],
         sub="Xin phép người phụ trách trước.",
         cta="Nhắn fanpage để hẹn giờ", meta="Miễn phí",
         shot="docs/readme/horizon_day.jpg", pos="center 62%", zoom="1.12", story=True,
         hook="CLB tiếng Nhật nào muốn chơi cùng một buổi?",
         caption="""CLB tiếng Nhật nào muốn chơi cùng một buổi?

Bọn mình đưa game tới một buổi sinh hoạt: mỗi người làm một việc trong phố, rồi kể câu mình vừa nói được. Không thu phí buổi đó. Không phát tờ rơi ghi số người chơi, vì bọn mình không bịa số.

Người phụ trách CLB nhắn {{LINK_FANPAGE}}. Nếu trường cần giấy, Long gửi trang sự thật trong press kit.

#NihongoLife #HocTiengNhat #CLB #TiengNhatN5 #HibariCho"""),
    dict(week=11, slug="02-khoe-cau", dow=3, kicker="Bài của bạn",
         lines=["Khoe một câu", "bạn vừa nói được"],
         sub="Ảnh màn hình của bạn. Xin phép trước khi bọn mình đăng lại.",
         cta="Bình luận câu tiếng Nhật + nghĩa", meta="Không sửa lời bạn",
         shot="docs/readme/checkout.jpg", pos="center 48%", zoom="1.06",
         hook="Đăng một câu bạn vừa nói được trong game.",
         caption="""Đăng một câu bạn vừa nói được trong game.

Ví dụ ở quầy: hỏi túi, trả tiền, cảm ơn. Viết câu tiếng Nhật và nghĩa tiếng Việt. Nếu gửi ảnh màn hình, xóa tên tài khoản nếu bạn không muốn hiện.

Bọn mình chỉ đăng lại khi bạn trả lời «đăng được». Không thêm sao, không thêm điểm, không viết hộ lời khen.

#NihongoLife #HocTiengNhat #CauNhat #Konbini #HibariCho"""),
    dict(week=11, slug="03-trung-tam", dow=5, kicker="Lớp N5",
         lines=["Dạy N5?", "Cho lớp vào phố"],
         sub="Phần học cốt lõi vẫn miễn phí với học viên.",
         cta="Xin bản sự thật ở press kit", meta="Chưa phải hợp đồng",
         shot="docs/readme/teacher.jpg", pos="center 42%", zoom="1.05",
         hook="Trung tâm và lớp N5 có thể cho học viên vào phố, không khoá bài học.",
         caption="""Trung tâm và lớp N5 có thể cho học viên vào phố. Bài học cốt lõi không bị khoá sau một lần thu phí.

NihongoLife là dự án cộng đồng, mã nguồn MIT, ba nhà sáng lập. Thi trong game là luyện tập, không thay bài thi thật. Buổi này là lời mời nói chuyện, chưa phải bảng giá, chưa phải hợp đồng.

Muốn bản một trang để gửi sếp trung tâm, lấy trong thư mục PressKit của bộ marketing.

#NihongoLife #HocTiengNhat #LopN5 #GiaoVien #HibariCho"""),

    dict(week=12, slug="01-mot-khu-pho", dow=1, kicker="Tuần 12",
         lines=["Mười hai tuần", "một khu phố"],
         sub="Cảm ơn người đã chơi thử và người đã chỉ chỗ lệch.",
         cta="Giờ chơi chung vẫn giữ nếu còn người", meta="Không tổng kết bằng số giả",
         shot="docs/readme/horizon_dusk.jpg", pos="center 30%", zoom="1.1", story=True,
         hook="Mười hai tuần, một khu phố. Cảm ơn bạn đã vào.",
         caption="""Mười hai tuần, một khu phố. Cảm ơn bạn đã vào.

Bọn mình không đăng tổng kết kiểu «hàng nghìn người». Số nào đo được nằm trong bảng nội bộ. Ô nào chưa gắn thống kê thì để trống, không điền cho đẹp.

Nếu bạn mới thấy trang: Hibari-chō vẫn đó. Một việc mỗi ngày. Link chơi:
{{LINK_CHOI}}

#NihongoLife #HocTiengNhat #HibariCho #CamOn #TiengNhatN5"""),
    dict(week=12, slug="02-giai-trien-vong", dow=3, kicker="Trong đội",
         lines=["Giải Triển vọng", "của cả nhóm"],
         sub="Unique Project 2025 · VTC Academy.",
         cta="Ba nhà sáng lập, không thêm tên", meta="The Next Generation",
         shot="NihongoLife/Assets/NihongoLife/Resources/MenuGallery/01_team_award_2025.jpg", pos="center 40%", zoom="1.04",
         hook="Giấy khen Giải Triển vọng là của cả nhóm.",
         caption="""Giấy khen Giải Triển vọng là của cả nhóm.

Unique Project 2025, VTC Academy. Ảnh trong bài là ảnh giấy khen đang dùng trong menu game. Ba nhà sáng lập: Quách Thành Long, Nguyễn Lê Gia Huy, Lương Nguyễn Tấn Dũng. Nhóm The Next Generation.

Bọn mình không thêm giải, không thêm đơn vị trao giải, không gắn logo kỳ thi tiếng.

Trang đội ngũ: https://website-the-next-generation-o323.vercel.app/team

#NihongoLife #TheNextGeneration #VTCAcademy #HibariCho #HocTiengNhat"""),
    dict(week=12, slug="03-van-mien-phi", dow=5, kicker="Cộng đồng",
         lines=["Phần học cốt lõi", "vẫn miễn phí"],
         sub="Mã nguồn mở. Không khoá bài N5 sau quảng cáo.",
         cta="Xem hướng đi trong kế hoạch tuần 12", meta="MIT",
         shot="docs/readme/horizon_day.jpg", pos="center 48%", zoom="1.05",
         hook="Dù có nhận tài trợ sau này, bài học cốt lõi vẫn miễn phí.",
         caption="""Dù có nhận tài trợ sau này, bài học cốt lõi vẫn miễn phí.

NihongoLife là dự án cộng đồng, giấy phép MIT. Hội thoại, phố, lớp luyện và lộ trình trí tuệ là phần để người học vào được. Nếu sau này có áo trong game, đồ phòng, hoặc học bổng từ một bên tài trợ, những thứ đó không được chặn đường học.

Bọn mình chưa mở cửa hàng. Bài này là cam kết, không phải bảng giá.

#NihongoLife #HocTiengNhat #MaNguonMo #MienPhi #HibariCho"""),
]

from hinh_anh import apply_shots

apply_shots(POSTS)
