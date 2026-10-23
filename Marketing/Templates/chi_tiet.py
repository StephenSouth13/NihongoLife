# -*- coding: utf-8 -*-
"""Sinh chi-tiet.md cho từng bài. Giờ theo ICT."""

from datetime import timedelta

from tukhoa import HASHTAG, SLUG_THEME, theme_by_id

START_NOTE = "26/10/2026"

PERSONA = {
    "minh": (
        "Minh, 17 tuổi — chân dung giả định, không phải học viên thật",
        "Đang học N5 để nộp hồ sơ. Ngại nói sai trước lớp. Xem TikTok và nhóm trường. Bài này phải cho Minh làm một câu, không phải một giáo trình.",
    ),
    "lan": (
        "Lan, 21 tuổi — chân dung giả định, không phải học viên thật",
        "Luyện IELTS và học thêm tiếng Nhật. Cần chỗ luyện có đồng hồ. Sợ nhầm điểm trong game với band chính thức. Bài này phải nói rõ chỗ đó.",
    ),
    "khoa": (
        "Khoa, 24 tuổi — chân dung giả định, không phải học viên thật",
        "Sắp sang Nhật làm. Cần mua đồ, hỏi vé, gọi món. Ít kiên nhẫn với bài dài. Bài này là một việc Khoa sẽ gặp.",
    ),
    "ca": (
        "Cả ba chân dung — giả định, không phải người thật",
        "Minh (17, N5), Lan (21, IELTS), Khoa (24, sắp sang Nhật). Bài này nói một luật chung của game, cả ba đọc được.",
    ),
}

SLUG_PERSONA = {
    "01-song-o-nhat": "ca",
    "02-chon-sai": "minh",
    "03-cau-nhat-nghia-viet": "minh",
    "01-bay-gio-sang": "khoa",
    "02-com-nam": "khoa",
    "03-ve-tau": "khoa",
    "01-moi-choi-thu": "minh",
    "02-khach-mo-pho": "khoa",
    "03-lo-trinh": "lan",
    "01-ket-cho-nao": "minh",
    "02-cach-doc": "minh",
    "03-mot-viec": "ca",
    "01-ra-mat-web": "ca",
    "02-hoang-hon": "khoa",
    "03-lop-hibari": "lan",
    "01-gio-choi-chung": "minh",
    "02-chat-khu-vuc": "lan",
    "03-giu-t": "khoa",
    "01-dang-n5": "minh",
    "02-tri-tue": "lan",
    "03-mo-sushi": "khoa",
    "01-dang-ielts": "lan",
    "02-ket-ban": "minh",
    "03-luyen-noi": "lan",
    "01-game-center": "minh",
    "02-sau-may": "minh",
    "03-dao-midori": "khoa",
    "01-an-chay-ngu": "khoa",
    "02-chao-bang-e": "minh",
    "03-dem-pho": "khoa",
    "01-buoi-clb": "ca",
    "02-khoe-cau": "minh",
    "03-trung-tam": "lan",
    "01-mot-khu-pho": "ca",
    "02-giai-trien-vong": "ca",
    "03-van-mien-phi": "ca",
}

# Ba câu mở. Câu 1 là câu đang dùng trên caption.
HOOKS = {
    "01-song-o-nhat": (
        "Học tiếng Nhật bằng cách sống ở Nhật.",
        "Nếu sách làm bạn nản, hãy thử một ngày trong phố.",
        "Hibari-chō: chào, mua đồ, đi tàu. Mỗi việc là một câu.",
    ),
    "02-chon-sai": (
        "Chọn sai cũng được giải thích.",
        "Nói sai trong game không bị đuổi khỏi câu chuyện.",
        "Sai một câu. Nhân vật chỉ chỗ lệch. Bạn chọn lại.",
    ),
    "03-cau-nhat-nghia-viet": (
        "Câu tiếng Nhật hiện ra, nghĩa tiếng Việt nằm ngay dưới.",
        "Đừng đoán nghĩa. Nhìn dòng tiếng Việt ngay bên dưới.",
        "Nhật trên câu, Việt dưới câu. Cách đọc bật khi bạn cần.",
    ),
    "01-bay-gio-sang": (
        "Ngủ dậy lúc 7 giờ, trong phòng trọ của bạn.",
        "Về nhà nói ただいま, rồi ngủ để sang sáng.",
        "Giường trong phòng đưa đồng hồ tới 7 giờ hôm sau.",
    ),
    "02-com-nam": (
        "Một phút mua cơm nắm: thấy món, bỏ giỏ, ra quầy.",
        "Đứng trước kệ cơm nắm, bạn nói được câu gì?",
        "Vào Hibari Mart. Ra quầy レジ. Một việc thôi.",
    ),
    "03-ve-tau": (
        "Đến ga Minato giá 320 yên. Nói lại được không?",
        "三百二十円. Ba trăm hai mươi, không phải hai trăm ba mươi.",
        "Hỏi giá vé, rồi lên tàu. Ảnh có rồi, video vẫn chờ quay.",
    ),
    "01-moi-choi-thu": (
        "Muốn chơi thử cùng bọn mình không?",
        "Một nhóm nhỏ, trước khi mở rộng. Không hứa quà.",
        "Nhắn «chơi thử». Nói bạn học để đi học, đi làm, hay vì thích.",
    ),
    "02-khach-mo-pho": (
        "Không muốn đăng ký ngay? Vào khách, cả phố vẫn mở.",
        "Khách chơi ngoại tuyến. Save nằm trên máy.",
        "Chưa có tài khoản vẫn vào ga, quán, Game Center, đảo.",
    ),
    "03-lo-trinh": (
        "Tài khoản không mở hết phố ngay. Trí tuệ mở từng nơi.",
        "Sushi 100, ga 160, Game Center 240, đảo 320.",
        "Sổ nhiệm vụ phím N ghi bạn còn thiếu bao nhiêu trí tuệ.",
    ),
    "01-ket-cho-nao": (
        "Bạn đang kẹt chỗ nào khi học N5?",
        "Kana, hội thoại, hay nghe? Viết một chỗ.",
        "Bọn mình đọc góp ý. Không biến câu của bạn thành review.",
    ),
    "02-cach-doc": (
        "Mới học thì bật cách đọc. Muốn tự nhớ thì tắt đi.",
        "Hướng dẫn, luyện tập, kiểm tra: cùng một câu, ba mức gợi ý.",
        "Romaji là cái bật khi cần, không phải cái để mãi.",
    ),
    "03-mot-viec": (
        "Đừng học một mạch cho xong. Một việc trong phố là đủ một ngày.",
        "Hôm nay mua cơm nắm. Mai hỏi giá vé. Vậy là đủ.",
        "Lịch học đã đầy. Bài này chỉ xin một việc.",
    ),
    "01-ra-mat-web": (
        "Hôm nay chơi được trên web và trên máy tính.",
        "Có link thật mới dán. Chưa có thì hoãn bài này.",
        "WebGL và Windows. Phần học cốt lõi không bán từng bài.",
    ),
    "02-hoang-hon": (
        "Ở lại phố tới lúc đèn đường bật.",
        "Cùng một phố, khác một giờ. Hoàng hôn là ảnh thật.",
        "Video bay qua phố vẫn chờ quay. Ảnh tĩnh đăng được.",
    ),
    "03-lop-hibari": (
        "Ngồi vào lớp Hibari và luyện một đề, ngay trong game.",
        "Chọn dạng N5 hoặc dạng IELTS. Điểm game không phải điểm thi.",
        "Trường mở từ đầu. Không cần chờ trí tuệ.",
    ),
    "01-gio-choi-chung": (
        "Tối nay vào phố cùng nhau. Một giờ, cho đỡ vắng.",
        "20:00 thứ Bảy, bản máy tính để gặp nhau.",
        "Web chơi được phố. Hẹn mặt nhau thì mở PC.",
    ),
    "02-chat-khu-vuc": (
        "Một kênh chat cho người đang đứng trong phố.",
        "Tin hiện lúc bạn chơi, không thành nhật ký.",
        "Bấm tên người gửi để nhắn riêng hoặc chặn.",
    ),
    "03-giu-t": (
        "Giữ T để nói. Người đứng gần nghe thấy.",
        "Voice khu vực: phím mặc định là T, trên bản máy tính.",
        "Đừng hẹn voice trên web cho tới khi đội kiểm tra xong.",
    ),
    "01-dang-n5": (
        "Tuần này làm một đề luyện theo dạng N5, trong game.",
        "Ngồi bàn, hoặc nhấn K. Làm xong là được, không cần đỗ.",
        "Điểm đó không thành chứng chỉ. Đừng đăng bảng ai đỗ.",
    ),
    "02-tri-tue": (
        "Trong game, thứ bạn kiếm được gọi là trí tuệ.",
        "Lớp, bàn học, bài luyện, nhiệm vụ, Game Center: đều có thể cộng trí tuệ.",
        "Đủ mức và xong bước truyện thì một nơi mới mở.",
    ),
    "03-mo-sushi": (
        "Bước kế tiếp sau konbini: gọi món ở quán sushi.",
        "Tài khoản cần 100 trí tuệ và đã tự mua một bữa.",
        "Khách ngoại tuyến vào quán được luôn. Bài này nói về lộ trình.",
    ),
    "01-dang-ielts": (
        "Lớp Hibari có bài luyện viết theo dạng IELTS.",
        "Ảnh này là bài viết trong game, không phải bài thi thật.",
        "Reading và Listening có khi máy đã có gói đề. Không hứa mọi máy.",
    ),
    "02-ket-ban": (
        "Kết bạn trong phố, rồi rủ nhau một câu tiếng Nhật.",
        "Có nút chặn. Đội không trực chat 24 giờ.",
        "Bản máy tính, tài khoản đã đăng ký.",
    ),
    "03-luyen-noi": (
        "Có bài luyện nói trong game. Phòng với giáo viên thì quay riêng, trên PC.",
        "Đừng đăng một khung hình giả cho phòng video.",
        "Micro kiểu ứng dụng máy tính không có trên web.",
    ),
    "01-game-center": (
        "Tan học, Kim rủ ra Game Center.",
        "Ghép kana, lấy sao, đổi vé ở quầy Aoi.",
        "Tài khoản mở nơi này khi đủ 240 trí tuệ và đã đi tàu.",
    ),
    "02-sau-may": (
        "Sáu máy trong Game Center. Chọn một máy, chơi một ván.",
        "Kana Match, Word Shooter, Order Rush, Kana Rain, UFO Catcher, Basket Count.",
        "Đừng thêm máy thứ bảy trên caption.",
    ),
    "03-dao-midori": (
        "Đảo Midori có nông trại và chỗ câu. Ảnh thật sẽ đăng khi quay xong.",
        "Cuốc, gieo, tưới. Câu cá cần cần câu. Không làm tay không.",
        "Bài này cố ý không ghép cảnh phố vào làm đảo.",
    ),
    "01-an-chay-ngu": (
        "Chạy nhiều thì đói. Về phòng để ngủ.",
        "Hết sức thì không chạy tiếp. Ăn, uống, ngủ để hồi.",
        "Mở balo phím B. Đây là nhịp một ngày, không phải hình phạt.",
    ),
    "02-chao-bang-e": (
        "Chưa kịp gõ chat? Giữ E và chào bằng một câu có sẵn.",
        "Vòng biểu cảm có câu tiếng Nhật đi kèm.",
        "Làm được cả khi chơi một mình.",
    ),
    "03-dem-pho": (
        "Đêm ở Hibari, cửa sổ vẫn sáng.",
        "Phố không tắt hết khi hết giờ học.",
        "Đi một vòng rồi về ngủ. Ảnh đêm là ảnh trong game.",
    ),
    "01-buoi-clb": (
        "CLB tiếng Nhật nào muốn chơi cùng một buổi?",
        "Một buổi, không thu phí, không in số người chơi.",
        "Xin phép người phụ trách trước khi vào nhóm.",
    ),
    "02-khoe-cau": (
        "Đăng một câu bạn vừa nói được trong game.",
        "Câu Nhật kèm nghĩa Việt. Ảnh thì xoá tên nếu bạn muốn.",
        "Bọn mình chỉ đăng lại khi bạn trả lời «đăng được».",
    ),
    "03-trung-tam": (
        "Trung tâm và lớp N5 có thể cho học viên vào phố, không khoá bài học.",
        "Đây là lời mời nói chuyện, chưa phải bảng giá.",
        "Lấy một trang sự thật trong PressKit trước khi gửi sếp trung tâm.",
    ),
    "01-mot-khu-pho": (
        "Mười hai tuần, một khu phố. Cảm ơn bạn đã vào.",
        "Không tổng kết bằng hàng nghìn người.",
        "Số nào chưa đo thì để trống.",
    ),
    "02-giai-trien-vong": (
        "Giấy khen Giải Triển vọng là của cả nhóm.",
        "Unique Project 2025, VTC Academy. Đúng ba nhà sáng lập.",
        "Không thêm giải, không thêm đơn vị trao giải.",
    ),
    "03-van-mien-phi": (
        "Dù có nhận tài trợ sau này, bài học cốt lõi vẫn miễn phí.",
        "Áo hay đồ phòng, nếu có sau này, không được chặn đường học.",
        "Bọn mình chưa mở cửa hàng. Bài này là cam kết, không phải bảng giá.",
    ),
}

WHEN = {
    1: ("19:30", "Sau giờ học hoặc giờ làm. Minh và Lan còn online, Khoa tan ca."),
    3: ("12:15", "Nghỉ trưa. Bài đọc xong trong vài phút."),
    5: ("18:30", "Trước 20:00. Nếu tuần có giờ chơi chung, người đọc còn kịp vào."),
}

REPLIES = {
    "hoc": [
        "Đúng rồi, chọn sai thì được giải thích và chọn lại. Bạn đang kẹt câu nào?",
        "Nghĩa tiếng Việt nằm dưới câu Nhật. Cần thì bật cách đọc trong chế độ Hướng dẫn.",
        "Bọn mình chưa có link chơi thì sẽ gắn vào {{LINK_CHOI}}, không gửi link lạ.",
    ],
    "jlpt": [
        "Đây là luyện theo dạng đề trong game. Điểm không phải kết quả thi chính thức.",
        "Bọn mình không gắn logo kỳ thi. Bạn làm một bài trong lớp Hibari là đủ cho thử thách này.",
        "Muốn khoe ảnh màn hình thì trả lời «đăng được». Bọn mình không thêm điểm.",
    ],
    "ielts": [
        "Điểm trên màn hình là điểm luyện trong game, không phải band chính thức.",
        "Ảnh bài viết là phần đang có. Reading và Listening mở khi máy có gói đề, không hứa mọi máy.",
        "Phòng video với giáo viên là bản máy tính, khi máy đã cấu hình. Web chưa hẹn voice.",
    ],
    "game": [
        "Sáu máy đúng tên trong game. Bạn chơi máy nào thì kể tên máy đó.",
        "Khách vào được cả phố, ngoại tuyến. Tài khoản thì đi theo trí tuệ.",
        "Ảnh đảo chưa có thì bọn mình không ghép cảnh khác. Có footage sẽ đăng sau.",
    ],
    "duhoc": [
        "320 yên trong bài là giá trong tình huống ga của game, không phải giá tàu ngoài đời.",
        "Một việc thôi: mua cơm nắm, hoặc hỏi vé, hoặc gọi một đĩa. Mai làm việc khác.",
        "Tài khoản cần đủ trí tuệ và xong bước truyện mới mở quán hoặc ga. Khách thì phố mở sẵn.",
    ],
    "online": [
        "Chat và voice đang chắc trên bản máy tính, tài khoản đã đăng ký.",
        "Giữ T là phím mặc định để nói. Đổi phím trong cài đặt thì làm theo phím bạn đã gắn.",
        "Có người nói năng khó chịu thì bấm chặn. Ngoài game, báo trong Discord. Đội không trực 24 giờ.",
    ],
    "congdong": [
        "Buổi CLB không thu phí. Bọn mình không in số người chơi.",
        "Gửi trung tâm thì dùng trang trong PressKit. Chưa phải hợp đồng.",
        "Câu của bạn chỉ được đăng lại khi bạn nói «đăng được».",
    ],
}


def _dow_date(start, post):
    return start + timedelta(days=(post["week"] - 1) * 7 + post["dow"])


def render(post: dict, start) -> str:
    day = _dow_date(start, post)
    dow_name = ["Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy", "Chủ Nhật"][day.weekday()]
    hour, why = WHEN[post["dow"]]
    why = why.rstrip(" .")
    persona_name, persona_body = PERSONA[SLUG_PERSONA[post["slug"]]]
    theme = theme_by_id(SLUG_THEME[post["slug"]])
    hooks = HOOKS[post["slug"]]
    kind = "Ảnh vuông 1080 và ảnh dọc 1080×1350"
    if post.get("story"):
        kind += ", kèm story 1080×1920"
    if post.get("layout") == "type":
        kind += ". Nền là thẻ chữ, không có ảnh giả của đảo"
    files = ["caption.txt", "fb-1080.png", "fb-1350.png", "chi-tiet.md"]
    if post.get("story"):
        files.append("story-1920.png")
    if post.get("footage"):
        files.append("CAN_QUAY.md")
    shot = post.get("shot") or "(không dùng ảnh — thẻ chữ)"
    lines = " / ".join(post["lines"])
    visual = [
        f"Ảnh đăng dùng nền `{shot}`.",
        f"Khung chữ: nhãn «{post['kicker']}», headline «{lines}», dòng phụ «{post['sub']}».",
        "Font Montserrat. Nền chữ là đêm `#0B0F17`, nhãn vàng `#F7BD3D`, lấy từ UiTheme.",
        "Không thêm sao, không thêm số người chơi, không logo kỳ thi.",
    ]
    if post.get("layout") == "type":
        visual.append("Ba dòng việc trên thẻ là việc có trong game: cuốc/xẻng/bình tưới, gieo rồi tưới, câu cá cần cần câu.")
    if post.get("footage"):
        visual.append("Video chưa có trong git. Làm theo CAN_QUAY.md. Khung hình để ổ D, không commit mp4.")
        visual.append("Cảnh cần quay:")
        for row in post["footage"]:
            visual.append(f"- {row[0]} · {row[1]} · {row[2]} · phụ đề: {row[3]}")
    else:
        visual.append("Bài này đăng bằng ảnh tĩnh đã có. Không cần footage mới để bấm đăng.")
    measure = [
        "Trong 48 giờ sau giờ đăng, ghi vào bảng thứ Hai:",
        "",
        "- Bài đã lên chưa, đúng giờ chưa. Trễ thì ghi giờ thật.",
        "- Số bình luận đã trả lời. Không ước.",
        "- Nếu caption có link và Long đã gắn UTM `utm_campaign=tuan-%02d`: số lần mở link. Chưa gắn thì ghi «chưa đo», không điền số cho đẹp." % post["week"],
        f"- Mốc tuần là mục tiêu, không phải kết quả. Xem README tuần. Nhãn bài: {post.get('kicker')}.",
        "- Không chụp một bình luận đẹp rồi gọi là đánh giá chung.",
    ]
    if SLUG_THEME[post["slug"]] in ("jlpt", "ielts"):
        measure.append("- Đếm người nói họ đã làm một bài chỉ khi họ tự viết, hoặc khi log đội đọc được. Không nhân số.")
    if SLUG_THEME[post["slug"]] == "online":
        measure.append("- Buổi chơi chung: đếm tài khoản có mặt nếu nhìn thấy trong game. Không thấy thì ghi «chưa đếm».")
    replies = REPLIES[SLUG_THEME[post["slug"]]]
    tags = HASHTAG[SLUG_THEME[post["slug"]]]
    body = f"""# Chi tiết bài

Dành cho anh Huy khi đăng. Giờ Việt Nam (ICT). Chân dung là người giả định để viết, không phải học viên thật.

## Mục tiêu

{post["hook"]}

Bài nằm trong tuần {post["week"]:02d}. Người đọc xong phải làm được một việc: hiểu câu trong caption, hoặc bấm đúng chỗ (link, bình luận, giờ chơi). Không xin họ «ủng hộ fanpage» suông.

## Chân dung

**{persona_name}.** {persona_body}

## Định dạng

- Kênh chính: Facebook fanpage.
- {kind}.
- File trong thư mục: {", ".join(files)}.
- TikTok và nhóm học chỉ dùng lại caption này khi lịch Excel bảo đăng, và khi đã có hình thật nếu dòng đó ghi «Cần quay».

## Ngày và giờ

- **{dow_name} {day.strftime("%d/%m/%Y")}, {hour}.**
- Vì sao giờ này: {why}.
- Đổi giờ thì sửa dòng này trước khi lên lịch. Đừng để caption nói 20:00 trong khi bài đăng lúc khác.

## Hook — ba cách mở

Dùng cách 1 trên Facebook. Cách 2 khi đăng lại story. Cách 3 khi đọc thành lời trên TikTok, sau khi đã có footage.

1. {hooks[0]}
2. {hooks[1]}
3. {hooks[2]}

## Caption đầy đủ

Dán nguyên khối dưới đây sau khi thay mã link. Bản giống `caption.txt`.

```
{post["caption"].strip()}
```

## CTA

**{post["cta"]}**

Việc người đọc làm tiếp: một hành động. Nếu CTA còn là mã `{{{{LINK_*}}}}` thì chưa được đăng. Link đội ngũ đã có thì mới dán URL đó. Không bịa link chơi.

## Hình và cảnh

{chr(10).join(line if line.startswith("- ") else "- " + line for line in visual)}

## Từ khoá và hashtag

Chủ đề trong ngân hàng: **{theme["ten"]}** (`Marketing/TuKhoa/Ngan_Hang_Tu_Khoa.md`).

- Cốt lõi: {", ".join(theme["core"][:4])}
- Đuôi dài nên nhét một câu: {theme["longtail"][0]}
- English: {", ".join(theme["en"])}
- 日本語: {", ".join(theme["ja"])}
- Google, nếu có người hỏi «tìm kiểu gì»: {theme["google"][0]}
- Câu TikTok: {theme["tt"]}
- Tiêu đề YouTube của chủ đề, đổi cho khớp cảnh bài này khi đã có video: {theme["yt_title"]}

Hashtag trộn Việt / Anh / Nhật, để cuối caption:

{tags}

Thêm tag riêng của bài chỉ khi nó đã nằm trong caption. Không thêm tag tên tổ chức thi.

## Gợi ý trả lời bình luận

Trả trong ngày. Giữ nguyên câu hỏi của họ. Không thêm điểm, không viết hộ lời khen.

1. {replies[0]}
2. {replies[1]}
3. {replies[2]}
4. Nếu họ gửi ảnh màn hình: «Bọn mình chỉ đăng lại khi bạn trả lời đăng được. Tên tài khoản bạn muốn xoá thì nói một tiếng.»
5. Nếu họ hỏi chứng chỉ hoặc band: «Trong game là luyện tập. Không quy đổi thành kết quả thi.»

## Cách đo

{chr(10).join(measure)}

Buổi thứ Hai: Long đọc số, Huy đọc bình luận chưa trả, Dũng nói footage của bài này đã có trên ổ D hay vẫn là CAN_QUAY. Ô trống để «chưa đo».
"""
    return body
