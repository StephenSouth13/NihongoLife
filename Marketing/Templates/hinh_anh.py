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

# Ảnh không có model người chơi cũ. Phố là môi trường hiện tại, nhìn xa.
COPIES = [
    ("BoiCanh/pho-ngay.jpg", "docs/readme/horizon_day.jpg", "Phố Hibari-chō ban ngày, nhìn dọc đường. Không có mặt nhân vật."),
    ("BoiCanh/pho-hoang-hon.jpg", "docs/readme/horizon_dusk.jpg", "Phố Hibari-chō lúc hoàng hôn. Không có mặt nhân vật."),
    ("BoiCanh/pho-dem.jpg", "docs/readme/horizon_night.jpg", "Phố Hibari-chō ban đêm, đèn cửa sổ. Không có mặt nhân vật."),
    ("Thi-JLPT-IELTS/ielts-viet.jpg", "Bao_Cao/ielts-productive/02_writing_examiner.jpg", "Màn luyện viết theo dạng IELTS. Chỉ chữ và nhận xét, không có model 3D."),
    ("Logo-Brand/giai-trien-vong-2025.jpg", "NihongoLife/Assets/NihongoLife/Resources/MenuGallery/01_team_award_2025.jpg", "Ảnh thật ba nhà sáng lập với giấy khen Giải Triển vọng 2025. Không phải nhân vật trong game."),
    ("Logo-Brand/logo-dark.png", "Bao_Cao/Logo/logo_dark.png", "Logo chữ trên nền tối."),
    ("Logo-Brand/logo-light.png", "Bao_Cao/Logo/logo_light.png", "Logo chữ trên nền sáng."),
    ("Logo-Brand/logo-mark.png", "Bao_Cao/Logo/logo_mark.png", "Dấu NL."),
]

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
        shot = post.get("shot") or ""
        if shot in SHOT_MAP:
            post["art_status"] = "current"
            post["shot_source"] = shot
            post["shot"] = SHOT_MAP[shot]
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
        "File chụp tám bé trong `Bao_Cao/character-regression/` đang là con trỏ Git LFS, máy này không có pixel, nên không chép vào đây và không gắn lên bài.",
        "",
        "## Đang dùng",
        "",
        "| File | Thấy gì | Nguồn | Còn đúng | Bài dùng |",
        "|---|---|---|---|---|",
    ]
    for dest, src, what in COPIES:
        rel = f"Marketing/HinhAnh/{dest}"
        used = ", ".join(by_shot.get(rel, [])) or "— (logo, hoặc bìa Word)"
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
        "- `NhanVat/` — cần chân dung và cảnh chơi của tám bé. Chưa có pixel trong bản checkout này.",
        "- `UI-Menu/` — menu cũ `01_menu.png` là model thấp khối, đã loại. Cần chụp menu và màn chọn tám bé.",
        "- `Online-GiaoTiep/` — ảnh online cũ là nhân vật người lớn. Cần chụp lại chat, bạn bè, giữ phím T.",
        "- `Mockup-Template/` — thẻ Facebook sinh từ `Templates/card.html`. Không lưu ảnh nhân vật cũ làm mẫu.",
        "",
        "## Đã loại",
        "",
        "Không chép vào thư mục này. Bài tương ứng là thẻ **CẦN ẢNH MỚI**.",
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
        if not source.is_file() or source.stat().st_size < 800:
            raise SystemExit(f"missing source {source}")
        head = source.read_bytes()[:20]
        if head.startswith(b"version https://git-lfs"):
            raise SystemExit(f"lfs pointer, not pixels: {source}")
        shutil.copyfile(source, target)
    for folder, note in (
        ("NhanVat", "Chưa có ảnh tám bé. Xem README thư mục cha.\n"),
        ("UI-Menu", "Chưa có menu hiện tại. `Bao_Cao/HinhAnh_Game/01_menu.png` là màn chọn nhân vật kiểu người lớn / low-poly cũ, đã loại. Cần chụp menu và màn chọn Haruto cùng bảy bé kia.\n"),
        ("Online-GiaoTiep", "Chưa có ảnh online với bé. Ảnh cũ đã loại.\n"),
        ("Mockup-Template", "Thẻ sinh từ Templates/card.html. Không để ảnh nhân vật cũ ở đây.\n"),
    ):
        path = HINH / folder
        path.mkdir(parents=True, exist_ok=True)
        (path / "CHUA_CO.md").write_text("# " + folder + "\n\n" + note, encoding="utf-8")
    _write_readme(POSTS)
    need = sum(1 for p in POSTS if p.get("art_status") == "need")
    print("copied", len(COPIES), "need posts", need)


if __name__ == "__main__":
    main()
