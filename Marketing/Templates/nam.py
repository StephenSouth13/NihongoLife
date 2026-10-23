# -*- coding: utf-8 -*-
"""Kế hoạch năm sau 12 tuần. Ngày lễ đã đối chiếu; ngày thi JLPT là cửa sổ, không phải lịch chính thức."""

from datetime import date


def task(when, day, owner, text, channel="Nội bộ"):
    return {"when": when, "date": day, "owner": owner, "text": text, "channel": channel}


SEASONS = [
    {
        "slug": "Mua-Xuan",
        "title": "Mùa xuân",
        "start": date(2027, 1, 18),
        "end": date(2027, 4, 18),
        "quarter": "Quý 1",
        "idea": "Giữ người đã vào trong 12 tuần. Tết Đinh Mùi và mùa hoa anh đào là hai cửa kể chuyện, không phải hai đợt khoe số.",
        "angles": [
            "Lời chào đầu năm và câu về nhà, gắn với phòng trọ trong game.",
            "Một việc đi đường: vé tàu, không phải mẹo du lịch bịa.",
            "Phố đổi giờ trong ngày, dùng ảnh hoàng hôn và đêm đã có.",
            "Học kỳ 2 của trường Việt Nam: một bài mỗi tuần, không nhồi.",
        ],
        "keywords": ["Tết", "học tiếng Nhật đầu năm", "hoa anh đào", "đi tàu tiếng Nhật"],
        "tasks": [
            task("18/01", date(2027, 1, 18), "Long", "Đọc bảng 12 tuần. Ô nào chưa đo thì giữ chữ chưa đo, không chuyển thành kết quả năm."),
            task("18/01", date(2027, 1, 18), "Huy", "Quyết định giờ chơi chung còn giữ thứ Bảy 20:00 hay đổi. Sửa caption trước khi đăng."),
            task("25/01", date(2027, 1, 25), "Dũng", "Liệt kê ảnh Tết và ảnh phố đã có. Thiếu thì viết CAN_QUAY, không ghép ảnh lạ."),
            task("12/04", date(2027, 4, 12), "Huy", "Tổng kết mùa trong một trang: bài đã đăng, bài hoãn, footage còn thiếu."),
        ],
    },
    {
        "slug": "Mua-He",
        "title": "Mùa hè",
        "start": date(2027, 4, 19),
        "end": date(2027, 7, 16),
        "quarter": "Quý 2",
        "idea": "Đưa lớp và dạng đề lên trước. Tanabata và cửa sổ JLPT tháng 7 là dịp, không phải ngày thi do đội công bố.",
        "angles": [
            "Một đề luyện dạng N5, nói rõ không phải chứng chỉ.",
            "Tanabata: viết một điều ước bằng một câu tiếng Nhật có nghĩa Việt.",
            "Game Center sau giờ học, đúng sáu máy.",
            "Một buổi CLB nếu có nhóm nhận, 0 đồng vẫn làm được.",
        ],
        "keywords": ["luyện N5", "Tanabata", "game học tiếng Nhật", "CLB tiếng Nhật"],
        "tasks": [
            task("19/04", date(2027, 4, 19), "Long", "Chốt có hay không một bản cập nhật nội dung trong quý. Chưa có người nhận thì không hẹn ngày trên fanpage."),
            task("03/05", date(2027, 5, 3), "Huy", "Hỏi một CLB về buổi chơi. Chưa đồng ý thì không đăng tên trường."),
            task("14/06", date(2027, 6, 14), "Dũng", "Quay 20 giây bàn thi nếu CAN_QUAY tuần 7 vẫn trống. File để ổ D."),
            task("12/07", date(2027, 7, 12), "Huy", "Sau cửa sổ tháng 7: cảm ơn người đã luyện, không đăng bảng đỗ."),
        ],
    },
    {
        "slug": "Mua-Thu",
        "title": "Mùa thu",
        "start": date(2027, 7, 17),
        "end": date(2027, 10, 17),
        "quarter": "Quý 3",
        "idea": "Obon, khai giảng, ngày Việt Nam–Nhật Bản. Nói về lớp và về phố, không nhận hợp đồng trên bài đăng.",
        "angles": [
            "Về nhà, đi thăm, câu ただいま — Obon là dịp kể, lịch vùng miền thì nói thẳng là không chỉ một ngày.",
            "Khai giảng: một tuần làm quen phố cho lớp N5.",
            "21/9: quan hệ Việt Nam–Nhật từ 1973, rồi một câu tiếng Nhật mình muốn nói được.",
            "Trung tâm hỏi thì gửi PressKit, chưa gửi bảng giá.",
        ],
        "keywords": ["khai giảng", "ngày Việt Nam Nhật Bản", "Obon", "lớp N5"],
        "tasks": [
            task("02/08", date(2027, 8, 2), "Dũng", "Chọn 5 ảnh phố và lớp đã sạch chữ dev để bộ khai giảng."),
            task("23/08", date(2027, 8, 23), "Long", "Soạn một trang sự thật gửi thầy cô: web/PC, khách và tài khoản, thi là luyện tập, MIT."),
            task("06/09", date(2027, 9, 6), "Huy", "Đăng bài khai giảng. Ngày trường dời thì sửa giờ, không giữ caption cũ."),
            task("11/10", date(2027, 10, 11), "Huy", "Ghi lại trung tâm nào đã hỏi, cuộc nào chỉ là chào. Không biến thành đối tác."),
        ],
    },
    {
        "slug": "Mua-Dong",
        "title": "Mùa đông",
        "start": date(2027, 10, 18),
        "end": date(2028, 1, 16),
        "quarter": "Quý 4",
        "idea": "Cửa sổ JLPT tháng 12, Giáng sinh, rồi chuẩn bị Tết Mậu Thân. Kết năm bằng việc còn làm được, không bằng số đẹp.",
        "angles": [
            "Một tuần luyện dạng N5 trước cửa sổ tháng 12.",
            "Đêm phố và đèn, ảnh đêm đã có. Không thêm tuyết nếu game không có cảnh đó.",
            "Giáng sinh: một câu chúc có nghĩa Việt, không bán đồ.",
            "Đầu tháng 1/2028: việc chuẩn bị Tết năm sau, lịch năm này kết thúc 16/01.",
        ],
        "keywords": ["ôn N5 cuối năm", "Giáng sinh", "đêm ở Nhật", "Tết"],
        "tasks": [
            task("18/10", date(2027, 10, 18), "Long", "Nhắc ngân sách năm sau vẫn là ba mức 0 / 2 triệu / 10 triệu. Chưa xin thì không tiêu."),
            task("15/11", date(2027, 11, 15), "Huy", "Lịch bốn bài cửa sổ tháng 12. Mỗi bài một việc, không phải khóa ôn cấp tốc."),
            task("27/12", date(2027, 12, 27), "Dũng", "Gói 10 ảnh sạch cho năm sau: phố, hội thoại, thi, tàu, Game Center."),
            task("10/01", date(2028, 1, 10), "Long", "Buổi cả ba người: giữ giờ chơi chung, hay dừng. Ghi quyết định, không ghi số bịa."),
        ],
    },
]

EVENTS = [
    {
        "slug": "Tet-Nguyen-Dan",
        "title": "Tết Nguyên Đán",
        "season": "Mùa xuân",
        "when": "Mùng 1 Tết Đinh Mùi: thứ Bảy 6/2/2027. Giao thừa: đêm 5/2/2027. Ông Công ông Táo: 30/1/2027.",
        "window": "25/1–14/2/2027",
        "idea": "Chúc bằng một câu tiếng Nhật có nghĩa Việt. Nhắc về nhà, ngủ dậy, đi tàu ngày sau Tết. Không bịa lì xì, không đăng số người chơi năm cũ.",
        "timeline": "Thứ Hai 18/01 soạn caption (T-21 rơi vào 16/01, vẫn trong tuần 12 của đợt mở, nên dời sang ngày năm bắt đầu). T-7 đăng bài ông Táo nếu còn sức. Mùng 1 một bài ngắn. Mùng 3 nghỉ đăng nếu cả đội về quê, báo trước.",
        "angles": [
            "Câu chúc đầu năm kèm nghĩa.",
            "Về nhà nói ただいま, gắn phòng trọ.",
            "Sau Tết, một việc nhỏ trở lại phố, không phải thử thách 7 ngày.",
        ],
        "keywords": ["Tết Đinh Mùi", "chúc Tết tiếng Nhật", "học tiếng Nhật đầu năm", "あけまして"],
        "tasks": [
            task("18/01", date(2027, 1, 18), "Huy", "Viết 3 caption Tết (đáng lẽ T-21, dời sang ngày mở năm). Mỗi caption một câu Nhật kèm nghĩa. Không có số."),
            task("T-14", date(2027, 1, 23), "Dũng", "Chọn ảnh phòng và phố đã có. Thiếu cảnh đào thì không vẽ."),
            task("T-10", date(2027, 1, 27), "Long", "UTM campaign=tet-2027. Chưa có link chơi thì CTA là theo dõi, không dán URL giả."),
            task("T-1", date(2027, 2, 5), "Huy", "Lên lịch bài giao thừa 18:30. Mùng 1 đăng 09:30, ngắn."),
            task("T+2", date(2027, 2, 8), "Huy", "Trả lời lời chúc. Không cắt thành review."),
            task("T+7", date(2027, 2, 13), "Long", "Ghi lượt mở link hoặc chưa đo."),
        ],
    },
    {
        "slug": "JLPT-Thang-7",
        "title": "Cửa sổ JLPT tháng 7",
        "season": "Mùa hè",
        "when": "Kỳ thi thường rơi vào Chủ nhật đầu tháng 7. Năm 2027 ngày đó là 4/7. Đây là cửa sổ chuẩn bị của đội, không phải lịch chính thức, không có logo kỳ thi.",
        "window": "14/6–12/7/2027",
        "idea": "Mời làm một đề luyện dạng N5 trong lớp Hibari. Nhắc đi thi thật thì xem trang của ban tổ chức, không xem fanpage.",
        "timeline": "Bốn tuần: vì sao luyện, một đề, trí tuệ là gì, sau cửa sổ thì cảm ơn không đăng bảng đỗ.",
        "angles": [
            "Ngồi bàn hoặc nhấn K.",
            "Nghe một phần, không tua điểm.",
            "Điểm game khác điểm thi.",
        ],
        "keywords": ["luyện dạng JLPT N5", "ôn N5 tháng 7", "không phải điểm thi", "JLPT N5 practice"],
        "tasks": [
            task("T-21", date(2027, 6, 13), "Long", "Bốn dòng «không phải điểm thi thật» để dán dưới mọi bài cửa sổ này."),
            task("T-14", date(2027, 6, 20), "Dũng", "Kiểm tra ảnh bàn thi còn chữ dev không. Còn thì crop hoặc quay lại."),
            task("T-7", date(2027, 6, 27), "Huy", "Đăng bài mở cửa sổ. CTA là vào lớp, không phải đăng ký thi hộ."),
            task("T-0", date(2027, 7, 4), "Huy", "Không đăng «hôm nay là ngày thi chính thức» trừ khi ban tổ chức đã công bố và anh dẫn nguồn. Mặc định chỉ nói cửa sổ luyện."),
            task("T+7", date(2027, 7, 11), "Long", "Đếm bài luyện chỉ khi có log hoặc lời kể. Không ước."),
        ],
    },
    {
        "slug": "JLPT-Thang-12",
        "title": "Cửa sổ JLPT tháng 12",
        "season": "Mùa đông",
        "when": "Kỳ thi thường rơi vào Chủ nhật đầu tháng 12. Năm 2027 ngày đó là 5/12. Tháng 12/2026 nằm trong lịch 12 tuần (tuần 6), không làm lại chiến dịch đó. Cửa sổ này là tháng 12/2027.",
        "window": "15/11–13/12/2027",
        "idea": "Ôn lại một đề dạng N5 cho người đã chơi từ đầu năm. Vẫn không có logo, không có bảng đỗ.",
        "timeline": "Giống cửa sổ tháng 7 nhưng ngắn hơn: ba bài, vì cuối năm đội ít sức.",
        "angles": [
            "Một đề, không phải khóa cấp tốc.",
            "Trí tuệ mở ga và quán, kể lại cho người mới.",
            "Sau cửa sổ, một bài cảm ơn không có số.",
        ],
        "keywords": ["ôn N5 tháng 12", "luyện đề dạng N5", "trí tuệ", "N5 練習"],
        "tasks": [
            task("T-21", date(2027, 11, 14), "Huy", "Chọn 3 caption từ ngân hàng JLPT. Bỏ câu nào hứa đỗ."),
            task("T-14", date(2027, 11, 21), "Dũng", "Nếu video bàn thi trên ổ D đã xem hết thì cắt 20 giây. Chưa thì dùng ảnh tĩnh."),
            task("T-7", date(2027, 11, 28), "Long", "UTM campaign=jlpt-cua-so-12-2027."),
            task("T-0", date(2027, 12, 5), "Huy", "Đăng nhắc luyện. Không khẳng định lịch thi chính thức nếu chưa có nguồn."),
            task("T+7", date(2027, 12, 12), "Long", "Ghi số bài luyện hoặc chưa đo."),
        ],
    },
    {
        "slug": "Mua-Hoa-Anh-Dao",
        "title": "Mùa hoa anh đào",
        "season": "Mùa xuân",
        "when": "Cuối tháng 3 đến đầu tháng 4 là mùa kể ở Nhật. Hibari-chō là phố trong game, không phải dự báo hoa của một thành phố thật. Đỉnh nội dung: tuần 29/3–4/4/2027.",
        "window": "22/3–11/4/2027",
        "idea": "Nếu game chưa có cây đào nở thì nói về phố buổi xuân và câu đi dạo, dùng ảnh ngày đã có. Không ghép ảnh hoa stock.",
        "timeline": "Một bài giới thiệu, một bài câu đi dạo, một bài ảnh thật hoặc CAN_QUAY.",
        "angles": [
            "Đi bộ trong phố, chào một câu.",
            "Ảnh hoàng hôn đã có, không cần hoa giả.",
            "Hẹn giờ chơi chung một buổi cuối tuần mùa này.",
        ],
        "keywords": ["mùa hoa anh đào", "桜", "đi dạo tiếng Nhật", "mùa xuân Nhật Bản"],
        "tasks": [
            task("T-21", date(2027, 3, 8), "Dũng", "Tìm trong repo một khung có hoa hoặc sắc xuân. Không có thì ghi rõ bài dùng ảnh phố ngày."),
            task("T-7", date(2027, 3, 22), "Huy", "Viết caption không nói «đào nở tại Hibari» nếu ảnh không có đào."),
            task("T-0", date(2027, 3, 29), "Huy", "Đăng bài mở mùa, 19:30."),
            task("T+7", date(2027, 4, 5), "Long", "Ghi bình luận hỏi về du học. Trả bằng việc trong game, không tư vấn visa."),
        ],
    },
    {
        "slug": "Tanabata",
        "title": "Tanabata",
        "season": "Mùa hè",
        "when": "7/7/2027, thứ Tư.",
        "window": "28/6–11/7/2027",
        "idea": "Một điều ước viết bằng một câu tiếng Nhật, kèm nghĩa Việt. Không cần đạo cụ nếu game chưa có trúc treo điều ước.",
        "timeline": "T-7 gọi ý viết câu. Đúng 7/7 đăng bài ngắn 12:15. T+2 đăng lại hai câu của người chơi chỉ khi họ cho phép.",
        "angles": [
            "Viết 〜がほしい hoặc một câu ước ngắn, đúng với hội thoại Game Center nếu đang kể Kim.",
            "Không biến thành cuộc thi có giải nếu quỹ quà bằng 0.",
            "Cửa sổ JLPT tháng 7 ở cạnh đó: đừng trộn hai CTA.",
        ],
        "keywords": ["Tanabata", "七夕", "câu ước tiếng Nhật", "viết một câu"],
        "tasks": [
            task("T-10", date(2027, 6, 27), "Huy", "Soạn bài riêng với cửa sổ JLPT. Hai caption, hai CTA."),
            task("T-3", date(2027, 7, 4), "Dũng", "Không làm thẻ tre giả. Dùng thẻ chữ hoặc ảnh phố."),
            task("T-0", date(2027, 7, 7), "Huy", "Đăng 12:15. Mời một câu kèm nghĩa."),
            task("T+2", date(2027, 7, 9), "Huy", "Hỏi «đăng được» trước khi đưa câu của ai lên story."),
        ],
    },
    {
        "slug": "Obon",
        "title": "Obon",
        "season": "Mùa thu",
        "when": "Phần lớn Nhật Bản nghỉ khoảng 13–16/8. Năm 2027: 13/8 là thứ Sáu, 16/8 là thứ Hai. Một số vùng nghỉ giữa tháng 7. Bài nói rõ không phải một ngày duy nhất.",
        "window": "2/8–16/8/2027",
        "idea": "Kể chuyện về nhà và câu chào, gắn phòng trọ. Không dựng bàn thờ, không dùng ảnh tang nếu không có trong game.",
        "timeline": "Một bài giải thích ngắn, một bài câu về nhà, rồi im đến giữa tháng nếu đội nghỉ.",
        "angles": [
            "ただいま khi về.",
            "Lịch Obon khác nhau theo vùng.",
            "Người học ở Việt Nam không cần «ăn Tết Nhật». Chỉ cần một câu.",
        ],
        "keywords": ["Obon", "お盆", "về nhà tiếng Nhật", "ただいま"],
        "tasks": [
            task("T-14", date(2027, 7, 30), "Huy", "Đọc lại caption, xóa câu nào biến Obon thành lễ hội mua sắm."),
            task("T-7", date(2027, 8, 6), "Dũng", "Ảnh phòng trọ sáng. Không ghép ảnh lễ ngoài repo."),
            task("T-0", date(2027, 8, 13), "Huy", "Đăng 19:30. Nói lịch vùng miền trong chính caption."),
            task("T+3", date(2027, 8, 16), "Huy", "Nghỉ trả lời nếu cả đội vắng. Ghim một dòng «trả lời sau 17/8»."),
        ],
    },
    {
        "slug": "Halloween",
        "title": "Halloween",
        "season": "Mùa đông",
        "when": "31/10/2027, Chủ nhật.",
        "window": "25/10–1/11/2027",
        "idea": "Chỉ làm nếu phố trong game có gì để kể buổi tối. Ảnh đêm đã có. Không thêm ma, bí ngô, hay áo choàng nếu game không có.",
        "timeline": "Một bài đêm phố. Không chuỗi 7 ngày. Không bán đồ hóa trang.",
        "angles": [
            "Đêm Hibari, cửa sổ còn sáng.",
            "Một câu chào buổi tối.",
            "Bỏ sự kiện nếu không kịp ảnh thật.",
        ],
        "keywords": ["đêm ở Nhật", "Halloween tiếng Nhật", "chào buổi tối", "夜"],
        "tasks": [
            task("T-10", date(2027, 10, 21), "Dũng", "Quyết định dùng horizon_night hay hoãn. Không vẽ bí ngô."),
            task("T-3", date(2027, 10, 28), "Huy", "Caption không hứa sự kiện hóa trang trong game."),
            task("T-0", date(2027, 10, 31), "Huy", "Đăng 18:30 nếu còn ảnh. Không thì hoãn và ghi trên lịch."),
            task("T+1", date(2027, 11, 1), "Long", "Không đo riêng nếu bài hoãn. Ghi hoãn."),
        ],
    },
    {
        "slug": "Giang-Sinh",
        "title": "Giáng sinh",
        "season": "Mùa đông",
        "when": "25/12/2027, thứ Bảy. Giáng sinh 2026 nằm trong tuần 9 của lịch 12 tuần, không viết lại chiến dịch đó.",
        "window": "18/12–26/12/2027",
        "idea": "Một câu chúc kèm nghĩa. Ảnh đêm hoặc phòng. Không cửa hàng quà, vì game chưa bán đồ.",
        "timeline": "T-7 một bài, đúng 25/12 một bài ngắn buổi sáng, 26/12 nghỉ.",
        "angles": [
            "Câu chúc, nghĩa Việt.",
            "Đêm phố đã có ảnh.",
            "Nhắc phần học vẫn miễn phí, không flash sale.",
        ],
        "keywords": ["Giáng sinh tiếng Nhật", "メリークリスマス", "câu chúc", "đêm Hibari"],
        "tasks": [
            task("T-10", date(2027, 12, 15), "Huy", "Viết câu chúc và nghĩa. Nhờ một người trong đội đọc lại tiếng Nhật."),
            task("T-4", date(2027, 12, 21), "Dũng", "Ảnh đêm, không sticker tuyết đè lên hình game."),
            task("T-0", date(2027, 12, 25), "Huy", "Đăng 09:30. CTA là vào phố hoặc nghỉ, không phải mua."),
            task("T+2", date(2027, 12, 27), "Huy", "Trả lời lời chúc đã nhận trong hai ngày."),
        ],
    },
    {
        "slug": "Khai-Giang",
        "title": "Khai giảng",
        "season": "Mùa thu",
        "when": "Ngày khai giảng phổ thông thường là 5/9. Năm 2027 ngày đó là Chủ nhật, nhiều trường dời sang thứ Hai 6/9. Sửa khi có lịch trường.",
        "window": "23/8–12/9/2027",
        "idea": "Gói một tuần cho lớp N5: khách vào phố, một hội thoại, một bài luyện. Gửi thầy cô trang sự thật, không gửi hợp đồng.",
        "timeline": "T-14 gửi thư. T-7 bài mời CLB. Đúng ngày khai giảng một bài. Tuần sau một bài «một việc mỗi ngày».",
        "angles": [
            "Lớp mới, một câu chào.",
            "Khách chơi không cần tài khoản.",
            "Trung tâm muốn dùng thì đọc PressKit.",
        ],
        "keywords": ["khai giảng", "lớp N5", "CLB tiếng Nhật", "học tiếng Nhật năm mới"],
        "tasks": [
            task("T-21", date(2027, 8, 16), "Long", "Cập nhật trang sự thật nếu luật khách hoặc web đổi. Không đổi thì ghi «còn đúng»."),
            task("T-14", date(2027, 8, 23), "Huy", "Gửi thầy cô và CLB đã quen. Không gửi hàng loạt nhóm lạ."),
            task("T-7", date(2027, 8, 30), "Dũng", "Năm ảnh sạch cho tờ một trang, in chỉ khi có ngân sách."),
            task("T-0", date(2027, 9, 6), "Huy", "Đăng 19:30. Nếu trường khai giảng đúng 5/9 thì chuyển bài sang tối 5/9."),
            task("T+7", date(2027, 9, 13), "Long", "Ghi số lớp hỏi thật. Không thêm lớp cho đủ mốc."),
        ],
    },
    {
        "slug": "Ngay-Viet-Nam-Nhat-Ban",
        "title": "Ngày Việt Nam – Nhật Bản",
        "season": "Mùa thu",
        "when": "21/9, ngày hai nước thiết lập quan hệ ngoại giao năm 1973. Năm 2027 là thứ Ba 21/9.",
        "window": "14/9–24/9/2027",
        "idea": "Một bài về vì sao nhóm làm phố này, và một câu tiếng Nhật người Việt hay cần. Không nhân danh cơ quan nhà nước, không gắn logo sứ quán.",
        "timeline": "Một bài ngày 21/9 lúc 19:30. Không chuỗi sự kiện.",
        "angles": [
            "Ba nhà sáng lập, VTC Academy, dự án cộng đồng.",
            "Một tình huống đi Nhật: mua đồ hoặc hỏi đường.",
            "Trang đội ngũ đã có URL thật.",
        ],
        "keywords": ["Việt Nam Nhật Bản", "quan hệ Việt Nhật", "học tiếng Nhật", "21 tháng 9"],
        "tasks": [
            task("T-10", date(2027, 9, 11), "Huy", "Viết bài không dùng huy hiệu cơ quan. Dẫn 21/9/1973 như mốc lịch sử."),
            task("T-5", date(2027, 9, 16), "Long", "Kiểm tra URL trang đội ngũ còn sống."),
            task("T-0", date(2027, 9, 21), "Huy", "Đăng 19:30."),
            task("T+2", date(2027, 9, 23), "Huy", "Trả lời câu hỏi du học bằng giới hạn của game: bọn mình không tư vấn visa."),
        ],
    },
    {
        "slug": "Moc-Cap-Nhat-Game",
        "title": "Mốc cập nhật và ra mắt",
        "season": "Cả năm",
        "when": "Bốn mốc nội bộ, không phải tính năng đã hứa. Chỉ công bố khi việc đã có người nhận và bản chạy được.",
        "window": "18/1/2027, 19/4/2027, 6/9/2027, 1/12/2027",
        "idea": "Marketing không công bố bản cập nhật trước lập trình. Mỗi mốc là một buổi quyết định, rồi mới có bài.",
        "timeline": "Mốc A 18/1: đọc số 12 tuần. Mốc B 19/4: có bản nội dung mới hay không. Mốc C 6/9: bản cho lớp, nếu web ổn. Mốc D 1/12: gói ôn, chỉ dùng đề đang có.",
        "angles": [
            "Nói cái đã chạy: web hoặc PC, khách, tài khoản, lớp.",
            "CAN_QUAY còn thiếu thì ghi thiếu.",
            "Không teaser tính năng chưa vào game.",
        ],
        "keywords": ["cập nhật NihongoLife", "chơi trên web", "bản cho lớp", "không hứa tính năng"],
        "tasks": [
            task("Mốc A", date(2027, 1, 18), "Long", "Buổi 20 phút. Danh sách việc năm sau chỉ gồm việc có tên người."),
            task("Mốc B", date(2027, 4, 19), "Dũng", "Nếu không có bản mới, báo Huy để đổi CTA. Không đăng «sắp ra mắt»."),
            task("Mốc C", date(2027, 9, 6), "Long", "Tự mở một lượt web trước bài khai giảng. Hỏng thì hoãn bài."),
            task("Mốc D", date(2027, 12, 1), "Huy", "Gói ôn chỉ dùng bài luyện đang có trong lớp. Không xin đề bản quyền đưa lên git."),
        ],
    },
]


def _fmt(day: date) -> str:
    return day.strftime("%d/%m/%Y")


def write_tree(root) -> None:
    root.mkdir(parents=True, exist_ok=True)
    index = [
        "# Sự kiện trong năm",
        "",
        "Mỗi thư mục có `README.md` và `checklist.md`. Sửa nội dung ở `Marketing/Templates/nam.py` rồi chạy `python3 Marketing/Templates/regenerate.py --skip-images` nếu muốn sinh lại checklist. Kế hoạch chữ ở `KeHoach_Nam_2026-2027.md` sẽ được cập nhật thường xuyên, không cần chờ script.",
        "",
        "Ngày JLPT trong các thư mục là cửa sổ đội tự chuẩn bị. Không phải lịch thi chính thức. Không dùng logo kỳ thi.",
        "",
    ]
    for event in EVENTS:
        folder = root / "SuKien" / event["slug"]
        folder.mkdir(parents=True, exist_ok=True)
        readme = [
            f"# {event['title']}",
            "",
            f"Mùa: {event['season']}.",
            "",
            "## Khi nào",
            "",
            event["when"],
            "",
            f"Cửa sổ làm việc: {event['window']}.",
            "",
            "## Ý",
            "",
            event["idea"],
            "",
            "## Tiến độ",
            "",
            event["timeline"],
            "",
            "## Góc bài",
            "",
        ]
        readme += [f"- {angle}" for angle in event["angles"]]
        readme += ["", "## Từ khoá", "", ", ".join(event["keywords"]), ""]
        (folder / "README.md").write_text("\n".join(readme) + "\n", encoding="utf-8")
        _write_checklist(folder / "checklist.md", event["title"], event["tasks"])
        index.append(f"- [{event['title']}](SuKien/{event['slug']}/README.md) — {event['window']}")
    (root / "SuKien" / "README.md").write_text("\n".join(index) + "\n", encoding="utf-8")

    for season in SEASONS:
        folder = root / season["slug"]
        folder.mkdir(parents=True, exist_ok=True)
        readme = [
            f"# {season['title']}",
            "",
            f"{season['quarter']}. Từ {_fmt(season['start'])} đến {_fmt(season['end'])}.",
            "",
            "## Ý",
            "",
            season["idea"],
            "",
            "## Góc bài",
            "",
        ]
        readme += [f"- {angle}" for angle in season["angles"]]
        readme += ["", "## Từ khoá", "", ", ".join(season["keywords"]), "", "Sự kiện nằm trong mùa này xem `SuKien/`.", ""]
        (folder / "README.md").write_text("\n".join(readme) + "\n", encoding="utf-8")
        _write_checklist(folder / "checklist.md", season["title"], season["tasks"])

    lines = [
        "# Checklist công việc cả năm",
        "",
        "Dành cho anh Huy, anh Long và anh Dũng. Hạn là mốc nội bộ. File này sinh cùng checklist trong từng mùa và từng sự kiện. Sẽ sửa thường xuyên: thêm việc thì sửa `Templates/nam.py`, hoặc ghi tay rồi đừng chạy đè script cho tới khi nhập lại.",
        "",
        "Luật: không bịa số, không logo kỳ thi, không hứa tính năng chưa chạy, voice và chat realtime ghi bản máy tính cho tới khi web được kiểm tra.",
        "",
        "## Theo mùa",
        "",
    ]
    for season in SEASONS:
        lines.append(f"### {season['title']}")
        lines.append("")
        lines.append(_table(season["tasks"], season["slug"]))
        lines.append("")
    lines.append("## Theo sự kiện")
    lines.append("")
    for event in EVENTS:
        lines.append(f"### {event['title']}")
        lines.append("")
        lines.append(_table(event["tasks"], f"SuKien/{event['slug']}"))
        lines.append("")
    (root / "Checklist_Cong_Viec.md").write_text("\n".join(lines), encoding="utf-8")


def _table(tasks, folder) -> str:
    rows = ["| Hạn | Ngày | Người | Việc | Chỗ |", "|---|---|---|---|---|"]
    for item in tasks:
        rows.append(f"| {item['when']} | {_fmt(item['date'])} | {item['owner']} | {item['text']} | `{folder}` |")
    return "\n".join(rows)


def _write_checklist(path, title, tasks) -> None:
    lines = [
        f"# Checklist — {title}",
        "",
        "Người làm: Long, Huy, Dũng. Đánh dấu tay khi xong. Hạn trễ thì ghi ngày thật, không xóa dòng.",
        "",
        "| Hạn | Ngày | Người | Việc | Xong |",
        "|---|---|---|---|---|",
    ]
    for item in tasks:
        lines.append(f"| {item['when']} | {_fmt(item['date'])} | {item['owner']} | {item['text']} | [ ] |")
    lines.append("")
    path.write_text("\n".join(lines), encoding="utf-8")


def annual_rows() -> list[dict]:
    rows = []
    for season in SEASONS:
        for item in season["tasks"]:
            rows.append({
                "date": item["date"],
                "quarter": season["quarter"],
                "season": season["title"],
                "event": season["title"],
                "when": item["when"],
                "text": item["text"],
                "owner": item["owner"],
                "channel": item["channel"],
                "folder": f"KeHoachNam/{season['slug']}/checklist.md",
            })
    for event in EVENTS:
        quarter = "Cả năm"
        for season in SEASONS:
            if season["start"] <= event["tasks"][0]["date"] <= season["end"]:
                quarter = season["quarter"]
                break
        for item in event["tasks"]:
            rows.append({
                "date": item["date"],
                "quarter": quarter,
                "season": event["season"],
                "event": event["title"],
                "when": item["when"],
                "text": item["text"],
                "owner": item["owner"],
                "channel": item["channel"],
                "folder": f"KeHoachNam/SuKien/{event['slug']}/checklist.md",
            })
    rows.sort(key=lambda row: (row["date"], row["event"]))
    return rows
