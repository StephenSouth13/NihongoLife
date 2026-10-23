#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Ảnh nguồn cho bộ marketing. Chỉ bản còn đúng nhân vật hiện tại được chép vào HinhAnh/.

Nhân vật người chơi trên main: tám bé trong PlayableCharacterCatalog
(Haruto mặc định). Tanaka/Remy (id cũ «student») đã bị bỏ.
"""

from __future__ import annotations

import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HINH = ROOT / "Marketing" / "HinhAnh"

# Ảnh Play Mode sau khi người chơi là tám bé. Nguồn chủ yếu là Bao_Cao/*-regression.
COPIES = [
    ("NhanVat/haruto-pose.jpg", "Bao_Cao/character-regression/poses_kid_haruto.jpg", "Haruto, tám tư thế. Nền kiểm tra animation, không phải cảnh phố."),
    ("NhanVat/yui-pose.jpg", "Bao_Cao/character-regression/poses_kid_yui.jpg", "Yui, tám tư thế. Cùng rig với Haruto."),
    ("NhanVat/menu-chon-haruto.jpg", "Bao_Cao/character-regression/menu_character_select_haruto.jpg", "Màn chọn nhân vật, Haruto đang được chọn. Tám bé có trong danh sách."),
    ("NhanVat/chan-dung-haruto.png", "Bao_Cao/hud-regression/portrait_kid_haruto.png", "Chân dung HUD của Haruto."),
    ("NhanVat/tam-trang-met.jpg", "Bao_Cao/character-regression/mood_tired.jpg", "Haruto tư thế mệt. Tâm trạng trong SocialStanding."),
    ("BoiCanh/pho-spawn.png", "Bao_Cao/city-regression/01_spawn.png", "Haruto đứng giữa phố Hibari-chō, có xe và biển Nhật."),
    ("BoiCanh/pho-vach.png", "Bao_Cao/street-regression/01_crossing_walk_phase.png", "Haruto qua vạch kẻ đường. Ảnh gốc còn danh sách dev ở mép, nên bài tuần 1 dùng pho-spawn.png."),
    ("BoiCanh/pho-ngay.jpg", "docs/readme/horizon_day.jpg", "Phố ban ngày, nhìn dọc đường. Không có mặt nhân vật."),
    ("BoiCanh/pho-hoang-hon.jpg", "docs/readme/horizon_dusk.jpg", "Phố lúc hoàng hôn. Không có mặt nhân vật."),
    ("BoiCanh/pho-dem.jpg", "docs/readme/horizon_night.jpg", "Phố ban đêm. Không có mặt nhân vật."),
    ("BoiCanh/phong-sang.png", "Bao_Cao/bedroom-regression/11_morning.png", "Buổi sáng trong phòng, đồng hồ 7:00, bé ngồi giường."),
    ("BoiCanh/phong-hoc.png", "Bao_Cao/bedroom-regression/06_study_card.png", "Bé ngồi bàn trong phòng, thẻ từ trên màn hình."),
    ("BoiCanh/phong-tro.png", "Bao_Cao/bedroom-regression/02_room_overview.png", "Nhìn phòng trọ, bé trong phòng."),
    ("BoiCanh/konbini.png", "Bao_Cao/ui-regression/02_shop_onigiri.png", "Bé đứng kệ cơm nắm trong Hibari Mart."),
    ("BoiCanh/ga-320.png", "Bao_Cao/ui-regression/12_station_fare.png", "Quầy vé, giá tới Minato 320 yên, bé ở ga."),
    ("BoiCanh/dao-nong-trai.png", "Bao_Cao/island-regression/07_farm_untilled.png", "Đảo Midori, bé cầm cuốc trước luống."),
    ("BoiCanh/dao-toan-canh.png", "Bao_Cao/island-regression/18_island_overview.png", "Toàn cảnh đảo Midori, cổng torii."),
    ("BoiCanh/sushi.png", "Bao_Cao/lifeloop-regression/20_sushi_order.png", "Bé ở quầy sushi, menu gọi món."),
    ("BoiCanh/lop.png", "Bao_Cao/school-regression/07_classroom.png", "Lớp Hibari, bé và cô giáo."),
    ("BoiCanh/game-center.png", "Bao_Cao/gamecenter-regression/05_board.png", "Bé chơi bàn kana trong Game Center."),
    ("BoiCanh/quay-qua.png", "Bao_Cao/gamecenter-regression/09b_prize_counter.png", "Quầy đổi quà Game Center, bé đứng trước quầy."),
    ("UI-Menu/hoi-thoai.png", "Bao_Cao/ui-regression/01_dialogue_box.png", "Hội thoại: câu Nhật, cách đọc, nghĩa Việt. Bé trong cảnh."),
    ("UI-Menu/ban-do.png", "Bao_Cao/map-progression/04_map_all_open.png", "Bản đồ khách, các khu đã mở."),
    ("UI-Menu/lo-trinh.png", "Bao_Cao/map-progression/02_journal_roadmap.png", "Sổ lộ trình tài khoản, mốc trí tuệ."),
    ("UI-Menu/balo.png", "Bao_Cao/ui-regression/07_bag.png", "Balo đang mở, bé đứng trong phố."),
    ("UI-Menu/bieu-cam.png", "Bao_Cao/ui-regression/24_emote_wheel.png", "Vòng biểu cảm, bé giơ tay."),
    ("UI-Menu/thanh-toan.png", "Bao_Cao/ui-regression/05_checkout.png", "Quầy tính tiền, bé trả tiền."),
    ("UI-Menu/doi.png", "Bao_Cao/hud-regression/needs_hunger_8.jpg", "HUD đói và khát, chân dung bé."),
    ("Online-GiaoTiep/choi-chung.png", "Bao_Cao/guest-online/02_registered_online.png", "Vài người chơi trong phố, tài khoản đã đăng ký."),
    ("Online-GiaoTiep/chat.jpg", "Bao_Cao/online-regression/chat_area.jpg", "Chat khu vực, avatar là bé."),
    ("Online-GiaoTiep/the-nguoi-choi.jpg", "Bao_Cao/online-regression/player_card.jpg", "Thẻ người chơi, model bé, có danh tiếng."),
    ("Online-GiaoTiep/ban-be.png", "Bao_Cao/friends-ui/friends_vi_1920x1080.png", "Màn bạn bè tiếng Việt."),
    ("Thi-JLPT-IELTS/jlpt-tu-vung.jpg", "Bao_Cao/jlpt-regression/01_vocab_question.jpg", "Câu từ vựng dạng JLPT, bé ngồi bàn."),
    ("Thi-JLPT-IELTS/ket-qua-lop.png", "Bao_Cao/school-regression/05_result.png", "Kết quả bài trong lớp, bé và cô giáo."),
    ("Thi-JLPT-IELTS/hanh-lang.png", "Bao_Cao/school-regression/03_exam_centre.png", "Hành lang khu thi, bé đi vào."),
    ("Thi-JLPT-IELTS/ielts-viet.jpg", "Bao_Cao/ielts-productive/02_writing_examiner.jpg", "Màn luyện viết theo dạng IELTS. Không có model 3D."),
    ("Thi-JLPT-IELTS/ielts-noi.jpg", "Bao_Cao/ielts-productive/06_speaking_part1.jpg", "Phòng luyện nói, bé ngồi đối diện."),
    ("Logo-Brand/giai-trien-vong-2025.jpg", "NihongoLife/Assets/NihongoLife/Resources/MenuGallery/01_team_award_2025.jpg", "Ảnh thật ba nhà sáng lập với giấy khen Giải Triển vọng 2025."),
    ("Logo-Brand/logo-dark.png", "Bao_Cao/Logo/logo_dark.png", "Logo chữ trên nền tối."),
    ("Logo-Brand/logo-light.png", "Bao_Cao/Logo/logo_light.png", "Logo chữ trên nền sáng."),
    ("Logo-Brand/logo-mark.png", "Bao_Cao/Logo/logo_mark.png", "Dấu NL."),
]

# Bài nào dùng ảnh nào. Khoá là slug.
SLUG_SHOT = {
    "01-song-o-nhat": "Bao_Cao/city-regression/01_spawn.png",
    "02-chon-sai": "Bao_Cao/ui-regression/01_dialogue_box.png",
    "03-cau-nhat-nghia-viet": "Bao_Cao/ui-regression/01_dialogue_box.png",
    "01-bay-gio-sang": "Bao_Cao/bedroom-regression/11_morning.png",
    "02-com-nam": "Bao_Cao/ui-regression/02_shop_onigiri.png",
    "03-ve-tau": "Bao_Cao/ui-regression/12_station_fare.png",
    "01-moi-choi-thu": "Bao_Cao/city-regression/01_spawn.png",
    "02-khach-mo-pho": "Bao_Cao/map-progression/04_map_all_open.png",
    "03-lo-trinh": "Bao_Cao/map-progression/02_journal_roadmap.png",
    "01-ket-cho-nao": "Bao_Cao/bedroom-regression/06_study_card.png",
    "02-cach-doc": "Bao_Cao/ui-regression/01_dialogue_box.png",
    "03-mot-viec": "Bao_Cao/bedroom-regression/02_room_overview.png",
    "01-ra-mat-web": "Bao_Cao/character-regression/menu_character_select_haruto.jpg",
    "02-hoang-hon": "docs/readme/horizon_dusk.jpg",
    "03-lop-hibari": "Bao_Cao/school-regression/07_classroom.png",
    "01-gio-choi-chung": "Bao_Cao/guest-online/02_registered_online.png",
    "02-chat-khu-vuc": "Bao_Cao/online-regression/chat_area.jpg",
    "03-giu-t": "Bao_Cao/online-regression/player_card.jpg",
    "01-dang-n5": "Bao_Cao/jlpt-regression/01_vocab_question.jpg",
    "02-tri-tue": "Bao_Cao/school-regression/05_result.png",
    "03-mo-sushi": "Bao_Cao/lifeloop-regression/20_sushi_order.png",
    "01-dang-ielts": "Bao_Cao/ielts-productive/02_writing_examiner.jpg",
    "02-ket-ban": "Bao_Cao/friends-ui/friends_vi_1920x1080.png",
    "03-luyen-noi": "Bao_Cao/ielts-productive/06_speaking_part1.jpg",
    "01-game-center": "Bao_Cao/gamecenter-regression/05_board.png",
    "02-sau-may": "Bao_Cao/gamecenter-regression/09b_prize_counter.png",
    "03-dao-midori": "Bao_Cao/island-regression/07_farm_untilled.png",
    "01-an-chay-ngu": "Bao_Cao/hud-regression/needs_hunger_8.jpg",
    "02-chao-bang-e": "Bao_Cao/ui-regression/24_emote_wheel.png",
    "03-dem-pho": "docs/readme/horizon_night.jpg",
    "01-buoi-clb": "docs/readme/horizon_day.jpg",
    "02-khoe-cau": "Bao_Cao/ui-regression/05_checkout.png",
    "03-trung-tam": "Bao_Cao/school-regression/03_exam_centre.png",
    "01-mot-khu-pho": "docs/readme/horizon_dusk.jpg",
    "02-giai-trien-vong": "NihongoLife/Assets/NihongoLife/Resources/MenuGallery/01_team_award_2025.jpg",
    "03-van-mien-phi": "Bao_Cao/city-regression/01_spawn.png",
}

# Cắt thanh dev dưới mép. UI full màn và ảnh giấy khen không cắt.
TRIM = {
    "01-song-o-nhat": (0.0, 0.0, 0.08, 0.0),
    "02-chon-sai": (0.0, 0.0, 0.12, 0.0),
    "03-cau-nhat-nghia-viet": (0.0, 0.0, 0.12, 0.0),
    "01-bay-gio-sang": (0.0, 0.0, 0.10, 0.0),
    "02-com-nam": (0.0, 0.0, 0.10, 0.0),
    "03-ve-tau": (0.0, 0.0, 0.10, 0.0),
    "01-moi-choi-thu": (0.0, 0.0, 0.10, 0.0),
    "01-ket-cho-nao": (0.0, 0.0, 0.10, 0.0),
    "02-cach-doc": (0.0, 0.0, 0.12, 0.0),
    "03-mot-viec": (0.0, 0.0, 0.10, 0.0),
    "03-lop-hibari": (0.0, 0.0, 0.10, 0.0),
    "01-gio-choi-chung": (0.0, 0.0, 0.10, 0.0),
    "02-chat-khu-vuc": (0.0, 0.0, 0.10, 0.0),
    "03-giu-t": (0.0, 0.0, 0.08, 0.0),
    "01-dang-n5": (0.0, 0.0, 0.08, 0.0),
    "02-tri-tue": (0.0, 0.0, 0.10, 0.0),
    "03-mo-sushi": (0.0, 0.0, 0.10, 0.0),
    "03-luyen-noi": (0.0, 0.0, 0.10, 0.0),
    "01-game-center": (0.0, 0.0, 0.10, 0.0),
    "02-sau-may": (0.0, 0.0, 0.10, 0.0),
    "03-dao-midori": (0.0, 0.0, 0.10, 0.0),
    "02-chao-bang-e": (0.0, 0.0, 0.10, 0.0),
    "02-khoe-cau": (0.0, 0.0, 0.10, 0.0),
    "03-trung-tam": (0.0, 0.0, 0.10, 0.0),
    "03-van-mien-phi": (0.0, 0.0, 0.10, 0.0),
}

SHOT_MAP = {src: f"Marketing/HinhAnh/{dest}" for dest, src, _why in COPIES}

# Ảnh đã dùng trong bài và bị loại vì thấy nhân vật người lớn cũ hoặc UI chọn nhân vật cũ.
WHY = {
    "docs/readme/dialogue.jpg": "Hội thoại: người chơi người lớn (kiểu Tanaka/Remy) và NPC người lớn.",
    "docs/readme/shop.jpg": "Trong konbini, người chơi người lớn đứng ở quầy.",
    "docs/readme/morning.jpg": "Phòng ngủ, người chơi người lớn trên giường.",
    "docs/readme/train.jpg": "Trong tàu, người chơi người lớn ngồi ghế.",
    "docs/readme/map.jpg": "Bản đồ, có người chơi người lớn.",
    "docs/readme/study.jpg": "Bàn học, người chơi người lớn.",
    "docs/readme/bedroom.jpg": "Phòng trọ, người chơi người lớn.",
    "docs/readme/exam_centre.jpg": "Hành lang lớp, người chơi người lớn.",
    "docs/readme/exam_desk.jpg": "Bàn thi, người chơi người lớn ngồi làm bài.",
    "docs/readme/teacher.jpg": "Trong lớp, người chơi người lớn.",
    "docs/readme/kana_result.jpg": "Game Center, người chơi người lớn và thanh dev.",
    "docs/readme/prize.jpg": "Quầy quà, người chơi người lớn.",
    "docs/readme/bag.jpg": "Balo, người chơi người lớn.",
    "docs/readme/emote.jpg": "Vòng biểu cảm, người chơi người lớn.",
    "docs/readme/checkout.jpg": "Quầy tính tiền, người chơi người lớn.",
    "Bao_Cao/HinhAnh_Game/g09_dialogue_choice.png": "Khung chọn câu, người chơi người lớn.",
    "Bao_Cao/HinhAnh_Game/g04_map.png": "Bản đồ lớn, người chơi người lớn.",
    "Bao_Cao/HinhAnh_Game/g08_restaurant_menu.png": "Quán sushi, người chơi người lớn.",
    "Bao_Cao/HinhAnh_Game/01_menu.png": "Menu chọn nhân vật còn model thấp khối cũ, không phải tám bé.",
    "Bao_Cao/online-regression/crowd_target_first.jpg": "Phố đông, nhân vật người lớn.",
    "Bao_Cao/online-regression/chat_area.jpg": "Chat khu vực, nhân vật người lớn.",
    "Bao_Cao/online-regression/player_card.jpg": "Thẻ người chơi, model người lớn.",
    "Bao_Cao/online-regression/chat_friends.jpg": "Danh sách bạn, kèm model người lớn.",
    "Bao_Cao/ielts-productive/06_speaking_part1.jpg": "Phòng luyện nói, nhân vật người lớn.",
}

NEED_SHOT = {
    "02-chon-sai": "Hội thoại chọn câu, bé Haruto hoặc bé đang chọn, không thanh dev.",
    "03-cau-nhat-nghia-viet": "Khung câu Nhật và nghĩa Việt, bé đứng trong cảnh.",
    "01-bay-gio-sang": "Bé ngủ dậy trong phòng trọ, đồng hồ 7:00.",
    "02-com-nam": "Bé đứng ở quầy Hibari Mart, thấy cơm nắm.",
    "03-ve-tau": "Bé trên tàu hoặc máy bán vé, giá trong tình huống là 320 yên.",
    "02-khach-mo-pho": "Bản đồ khách, không có nút dev, bé nếu có thì là một trong tám bé.",
    "03-lo-trinh": "Bản đồ lộ trình trí tuệ 100/160/240/320, bé đang chọn.",
    "01-ket-cho-nao": "Bé ở bàn học hoặc lớp, không biến thành review.",
    "02-cach-doc": "Ba chế độ hội thoại, bé và câu Nhật.",
    "03-mot-viec": "Một việc nhỏ trong phòng hoặc phố, bé Haruto.",
    "01-ra-mat-web": "Menu chính hiện tại và màn chọn tám bé.",
    "03-lop-hibari": "Hành lang hoặc cửa lớp Hibari, bé đi vào.",
    "01-gio-choi-chung": "Vài bé online trong phố. Không dùng đám người lớn cũ.",
    "02-chat-khu-vuc": "Chat khu vực, avatar là bé.",
    "03-giu-t": "Giữ T, bé nói, không lộ App ID.",
    "01-dang-n5": "Bé ngồi bàn luyện dạng N5, không logo kỳ thi.",
    "02-tri-tue": "Sau bài, số trí tuệ trên HUD, bé trong lớp.",
    "03-mo-sushi": "Bé ở quán sushi, menu gọi món.",
    "02-ket-ban": "Màn bạn bè, avatar bé.",
    "03-luyen-noi": "Phòng luyện nói trên máy tính, bé hoặc chỉ UI, không model người lớn.",
    "01-game-center": "Bé trong Game Center, một máy kana.",
    "02-sau-may": "Sáu máy đúng tên, bé đứng trước một máy.",
    "03-dao-midori": "Đảo Midori: bé cuốc, gieo, câu. Không ghép phố.",
    "01-an-chay-ngu": "HUD đói/khát hoặc balo, bé trong cảnh.",
    "02-chao-bang-e": "Vòng biểu cảm, bé giơ tay chào.",
    "02-khoe-cau": "Quầy tính tiền, bé trả tiền, không thanh dev.",
    "03-trung-tam": "Lớp hoặc giáo viên NPC hiện tại, người chơi là bé.",
}


def apply_shots(posts: list[dict]) -> None:
    for post in posts:
        shot = SLUG_SHOT.get(post["slug"], post.get("shot") or "")
        if shot in SHOT_MAP:
            post["art_status"] = "current"
            post["shot_source"] = shot
            post["shot"] = SHOT_MAP[shot]
            if post.get("layout") in ("need", "type"):
                post.pop("layout", None)
            if post["slug"] in TRIM:
                post["trim"] = TRIM[post["slug"]]
        else:
            post["art_status"] = "need"
            post["shot_source"] = shot or "(chưa có file pixel)"
            post["layout"] = "need"
            post["shot"] = ""


def _write_readme(posts: list[dict]) -> None:
    by_shot: dict[str, list[str]] = {}
    for post in posts:
        if post.get("art_status") == "current":
            by_shot.setdefault(post["shot"], []).append(f"Tuan-{post['week']:02d}/{post['slug']}")
    lines = [
        "# Ảnh nguồn",
        "",
        "Người chơi hiện tại, đọc từ `PlayableCharacterCatalog` trên main: tám bé **Haruto** (mặc định), Yui, Sota, Hina, Riku, Mio, Yuto, Sakura. Prefab `Resources/Characters/Kids/NL_kid_*`.",
        "",
        "Người lớn Mixamo id `student` (Tanaka/Remy) đã bỏ khỏi danh sách chọn. Ảnh nào thấy model đó thì không đăng.",
        "",
        "Ảnh Play Mode của tám bé lấy từ `Bao_Cao/` (character-regression, ui-regression, city, school, island, online, JLPT, IELTS). Bản đã chép vào đây là pixel, không phải con trỏ LFS.",
        "",
        "## Đang dùng",
        "",
        "| File | Thấy gì | Nguồn | Còn đúng | Bài dùng |",
        "|---|---|---|---|---|",
    ]
    for dest, src, what in COPIES:
        rel = f"Marketing/HinhAnh/{dest}"
        used = ", ".join(by_shot.get(rel, [])) or "README gốc, hoặc ảnh dự phòng trong thư mục này"
        if dest.startswith("Logo-Brand/logo"):
            used = "Press kit và `Logo-Brand`. Không gắn lên bài Facebook tuần."
        if dest.endswith("giai-trien-vong-2025.jpg"):
            used = ", ".join(by_shot.get(rel, [])) or "Tuan-12/02-giai-trien-vong"
        lines.append(f"| `{dest}` | {what} | `{src}` | Có | {used} |")
    lines += [
        "",
        "Bìa Word 12 tuần dùng `BoiCanh/pho-hoang-hon.jpg`. Bìa kế hoạch năm và bìa từ khoá dùng `BoiCanh/pho-ngay.jpg`.",
        "",
        "## Thư mục chưa có ảnh",
        "",
        "- `Mockup-Template/` — thẻ Facebook sinh từ `Templates/card.html`. Không lưu ảnh nhân vật cũ làm mẫu.",
        "",
        "## Đã loại",
        "",
        "Ảnh `docs/readme/` và `Bao_Cao/HinhAnh_Game/` đời trước có người chơi người lớn hoặc menu thấp khối. Bài không dùng các file đó. Ảnh thay thế là bản Play Mode của tám bé, liệt ở bảng trên.",
        "",
        "| Nguồn cũ | Vì sao bỏ | Bài | Cần chụp |",
        "|---|---|---|---|",
    ]
    seen = set()
    for post in posts:
        if post.get("art_status") != "need":
            continue
        src = post.get("shot_source") or ""
        why = WHY.get(src, "Chưa có ảnh đúng nhân vật bé hoặc cảnh này.")
        need = NEED_SHOT.get(post["slug"], "Chụp lại bằng một trong tám bé.")
        key = (src, post["slug"])
        if key in seen:
            continue
        seen.add(key)
        lines.append(f"| `{src}` | {why} | `Tuan-{post['week']:02d}/{post['slug']}` | {need} |")
    lines.append("")
    (HINH / "README.md").write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    import sys
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    from posts import POSTS

    for dest, src, _why in COPIES:
        target = HINH / dest
        source = ROOT / src
        target.parent.mkdir(parents=True, exist_ok=True)
        if not source.is_file():
            raise SystemExit(f"missing source {source}")
        head = source.read_bytes()[:30]
        if head.startswith(b"version https://git-lfs"):
            if not (target.exists() and target.stat().st_size > 800 and not target.read_bytes()[:30].startswith(b"version https://git-lfs")):
                raise SystemExit(f"lfs pointer, not pixels: {source}")
        else:
            shutil.copyfile(source, target)
        if dest == "BoiCanh/pho-vach.png":
            _crop_vach(target)
    for folder in ("NhanVat", "UI-Menu", "Online-GiaoTiep", "BoiCanh", "Thi-JLPT-IELTS", "Logo-Brand"):
        stale = HINH / folder / "CHUA_CO.md"
        if stale.exists():
            stale.unlink()
    mock = HINH / "Mockup-Template"
    mock.mkdir(parents=True, exist_ok=True)
    (mock / "CHUA_CO.md").write_text(
        "# Mockup-Template\n\n"
        "Thẻ Facebook sinh từ `Marketing/Templates/card.html`. Không để ảnh nhân vật cũ ở đây.\n",
        encoding="utf-8",
    )
    _write_readme(POSTS)
    need = sum(1 for p in POSTS if p.get("art_status") == "need")
    print("copied", len(COPIES), "need posts", need)


def _crop_vach(target: Path) -> None:
    """Danh sách dev nằm mép trái của ảnh vạch kẻ đường. Cắt trước khi đưa lên README."""
    from PIL import Image
    im = Image.open(target).convert("RGB")
    w, h = im.size
    if w < 1400:
        return
    im.crop((int(w * 0.34), int(h * 0.02), int(w * 0.99), int(h * 0.90))).save(target, quality=90)


if __name__ == "__main__":
    main()
