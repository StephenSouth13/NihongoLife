"""Selects, crops and compresses screenshots for the v4 report into img/ (JPEG, max 1600 px wide)."""
from pathlib import Path
from PIL import Image

B = Path(__file__).resolve().parents[1]          # Bao_Cao
OUT = Path(__file__).parent / "img"
OUT.mkdir(exist_ok=True)

# name: (source relative to Bao_Cao, crop box as fractions (l, t, r, b) or None)
SEL = {
    "hud_city": ("ui-regression/22_portal_sign_and_dock.png", None),
    "hud_1280": ("lifeloop-regression/01_hud_1280x720.png", None),
    "dialogue": ("ui-regression/01_dialogue_box.png", None),
    "shop_onigiri": ("ui-regression/02_shop_onigiri.png", None),
    "checkout": ("ui-regression/05_checkout.png", None),
    "bag": ("ui-regression/07_bag.png", None),
    "map": ("ui-regression/08_map_overview.png", None),
    "emote": ("ui-regression/24_emote_wheel.png", None),
    "station_staff": ("ui-regression/11_station_staff.png", None),
    "ticket_machine": ("ui-regression/20_ticket_machine.png", None),
    "on_train": ("ui-regression/15_on_train.png", None),
    "arrived_school": ("ui-regression/21_arrived_school.png", None),
    "bedroom": ("zone-regression/bedroom.png", None),
    "classroom_desk": ("school-regression/02_exam_desk.png", None),
    "exam_centre": ("school-regression/03a_ielts_selection.png", None),
    "exam_started": ("school-regression/04_exam_started.png", None),
    "exam_result": ("school-regression/06_teacher_comment.png", None),
    "gc_board": ("gamecenter-regression/05_board.png", None),
    "gc_match": ("gamecenter-regression/07_matching.png", None),
    "gc_result": ("gamecenter-regression/08_result.png", None),
    "gc_entrance": ("gamecenter-regression/01_city_entrance.png", None),
    "isl_overview": ("island-regression/18_island_overview.png", None),
    "isl_arrival": ("island-regression/02_arrival_welcome.png", None),
    "isl_seed": ("island-regression/08_farm_seed_picker.png", None),
    "isl_crop": ("island-regression/10_crop_stage3.png", None),
    "isl_ready": ("island-regression/11_farm_ready.png", None),
    "isl_animal": ("island-regression/16_animal_fed.png", None),
    "isl_shop": ("island-regression/03_shop_1920x1080.png", None),
    "isl_fashion": ("island-regression/04_shop_fashion_honest_empty.png", None),
    "isl_sell": ("island-regression/13_shop_sell.png", None),
    "isl_english": ("island-regression/14_shop_english.png", None),
    "isl_notebook": ("island-regression/17_notebook.png", None),
    "isl_travel": ("island-regression/20_travel_screen.png", None),
    "isl_back": ("island-regression/21_back_at_hibari.png", None),
    "isl_onboard": ("island-regression/01_onboard_to_midori.png", None),
    "dig_hand": ("lifeloop-regression/30_dig_by_hand.png", None),
    "farm_paid": ("lifeloop-regression/31_farm_paid.png", None),
    "journal": ("lifeloop-regression/04_journal_job.png", (0.19, 0.16, 0.81, 0.83)),
    "journal_active": ("lifeloop-regression/05_journal_active.png", (0.19, 0.16, 0.81, 0.83)),
    "profile": ("lifeloop-regression/03_profile_tab.png", (0.25, 0.26, 0.75, 0.74)),
    "settings": ("lifeloop-regression/02_settings_controls.png", (0.12, 0.06, 0.88, 0.94)),
    "customer_q": ("lifeloop-regression/08_customer_question.png", (0.30, 0.25, 0.70, 0.79)),
    "register_q": ("lifeloop-regression/09_register_total.png", (0.30, 0.26, 0.70, 0.78)),
    "restock": ("lifeloop-regression/07_restock_progress.png", (0.0, 0.0, 1.0, 1.0)),
    "sushi_order": ("lifeloop-regression/20_sushi_order.png", (0.30, 0.30, 0.70, 0.76)),
    "sushi_menu": ("HinhAnh_Game/g08_restaurant_menu.png", None),
    "menu": ("HinhAnh_Game/01_menu.png", None),
    "guide": ("HinhAnh_Game/02_guide.png", None),
}

for name, (src, crop) in SEL.items():
    im = Image.open(B / src).convert("RGB")
    if crop:
        w, h = im.size
        im = im.crop((int(crop[0] * w), int(crop[1] * h), int(crop[2] * w), int(crop[3] * h)))
    if im.width > 1600:
        im = im.resize((1600, int(im.height * 1600 / im.width)), Image.LANCZOS)
    im.save(OUT / f"{name}.jpg", quality=86, optimize=True)
print(len(SEL), "images ->", OUT)
