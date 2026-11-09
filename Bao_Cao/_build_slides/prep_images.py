"""Pictures and videos of the deck: game captures from Bao_Cao, team photos / award / logos from assets/ (sources in
assets/SOURCES.txt), the three videos re-encoded to 720p from the finished files in Bao_Cao.
Usage: python prep_images.py <work_dir>   ->  <work_dir>/img (1600 px JPEGs) and <work_dir>/video, then
       node build_deck.js <work_dir>/img <work_dir>/video <out.pptx>
"""
import os
import subprocess
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
B = os.path.normpath(os.path.join(HERE, '..'))
A = os.path.join(HERE, 'assets')
WORK = sys.argv[1]
OUT = os.path.join(WORK, 'img')
VIDEO = os.path.join(WORK, 'video')
for folder in (OUT, VIDEO):
    os.makedirs(folder, exist_ok=True)

# 720p, compressed enough that the deck stays under GitHub's 100 MB a file.
for source, file, crf in (('NihongoLife_Trailer_v3.mp4', 'trailer.mp4', 27), ('NihongoLife_Demo_Day_Du.mp4', 'demo.mp4', 31), ('NihongoLife_Con_Duong_Su_Nghiep.mp4', 'careers.mp4', 29)):
    target = os.path.join(VIDEO, file)
    if not os.path.exists(target) or os.path.getmtime(target) < os.path.getmtime(os.path.join(B, source)):
        subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', os.path.join(B, source), '-vf', 'scale=1280:720', '-c:v', 'libx264', '-crf', str(crf),
                        '-preset', 'medium', '-c:a', 'aac', '-b:a', '96k', target], check=True)

PICS = {
    'cover_town': f'{A}/cover_town.jpg', 'cover_island': f'{A}/cover_island.jpg', 'menu': f'{B}/web-release/p_pc_1_menu.png',
    'problem': f'{A}/problem.jpg', 'idea': f'{B}/demo-tour/konbini.jpg', 'loop': f'{A}/loop.jpg',
    'town': f'{B}/web-release/01_town.png', 'station': f'{B}/train-route/00_platform_waiting.png', 'train': f'{B}/train-route/04_stop_gakuen.png',
    'school': f'{B}/town-life/02_classroom_from_board.png', 'sushi': f'{B}/sushi-flow/02_eating.png', 'sushi_menu': f'{B}/town-life/06_sushi_menu_pictures.png',
    'clinic': f'{B}/clinic/05_doctor.png', 'clinic_med': f'{B}/clinic/06_medicine.png', 'fashion': f'{B}/fashion/01_entrance.jpg', 'tryon': f'{B}/fashion/05_try_on.jpg',
    'arcade': f'{B}/gamecenter-regression/02_arcade_spawn.png', 'kana': f'{B}/gamecenter-regression/07_matching.png', 'hoop': f'{B}/demo-tour/hoop.jpg',
    'music': f'{B}/music-shop/08_character_plays_piano.png', 'gallery': f'{B}/music-shop/04_gallery_north.png', 'stage': f'{B}/music-shop/09_stage_show.png',
    'midori': f'{B}/swim/midori_3_ashore.png', 'fishing': f'{B}/demo-tour/fishing.jpg', 'swim': f'{B}/swim/phuquoc_2_stroke.png',
    'phuquoc': f'{B}/phuquoc/02_ganh_ngon.png', 'phuquoc_map': f'{B}/town-life/04_phuquoc_map.png',
    'konbini': f'{B}/demo-tour/konbini.jpg', 'heat': f'{B}/demo-tour/heat.jpg', 'flu': f'{B}/health/01_flu_fever.png',
    'career_choose': f'{B}/careers-ui/01_careers_top.png', 'career_interview': f'{B}/career/04_interview.png', 'career_ladder': f'{B}/career/07_ladder.png',
    'career_branch': f'{B}/career/05_branch.png', 'bank': f'{B}/bank/01_bank_with_money.png', 'bank_loan': f'{B}/bank/02_emergency_loan.png',
    'courses': f'{B}/video-courses/02_course_page.png', 'catalogue': f'{B}/video-courses/01_catalogue.png', 'course_path': f'{B}/course/02_path_route_ja_vi.png',
    'course_ru': f'{B}/course/10_path_ru_ui.png', 'exam_centre': f'{B}/school-regression/03_exam_centre.png', 'exam_result': f'{B}/school-regression/05_result.png',
    'chat': f'{B}/online-regression/chat_area.jpg', 'player_card': f'{B}/online-regression/player_card.jpg', 'friends': f'{B}/friends-ui/friends_vi_1920x1080.png',
    'web_pc': f'{B}/web-release/p_pc_6_town.png', 'web_phone': f'{B}/web-release/p_phone_6_town.png', 'web_phone_menu': f'{B}/web-release/p_phone_1_menu.png',
    'youtube': f'{A}/youtube.jpg', 'promo_phuquoc': f'{A}/promo_phuquoc.jpg', 'promo_trailer': f'{A}/promo_trailer.jpg', 'promo_demo': f'{A}/promo_demo.jpg',
    'team_long': f'{A}/team_long.png', 'team_huy': f'{A}/team_huy.png', 'team_dung': f'{A}/team_dung.png',
    'team_chau': f'{A}/team_chau.png', 'team_dang': f'{A}/team_dang.png', 'award': f'{A}/award.jpg', 'vtc': f'{A}/vtc.png',
    'people': f'{B}/characters-variants/01_all_people.png', 'kids_fashion': f'{B}/fashion/10_kid_mio_street_bomber.jpg',
    'classroom_ru': f'{B}/school-regression/02b_classroom_russian.png',
    'bedroom': f'{B}/bedroom-regression/02_room_overview.png', 'bedroom_study': f'{B}/bedroom-regression/06_study_card.png',
    'medicine_2d': f'{B}/health/medicine_3d/medicine_2d_contact.png', 'medicine_3d': f'{B}/health/medicine_3d/medicine_3d_contact.png',
    'medicine_shelf': f'{B}/medicine-shelf/02_medicine_section.png', 'konbini_medicine': f'{B}/health/04_konbini_medicine.png',
    'look_16': f'{B}/character-look/01_kids_16_natural.png', 'look_30': f'{B}/character-look/04_kids_30_blonde_goatee.png',
    'look_70': f'{B}/character-look/02_kids_70_black_full.png', 'look_80': f'{B}/character-look/03_kids_80_grey_long.png',
    'logo_sims': f'{A}/sims.png', 'logo_stardew': f'{A}/stardew.png', 'logo_sotf': f'{A}/sotf.jpg', 'logo_clannad': f'{A}/clannad.jpg',
}
# The eight playable kids, the ten careers' people (at rest) and the sixteen illnesses as they show on the body.
for i, kid in enumerate(['haruto', 'yui', 'sota', 'hina', 'riku', 'mio', 'yuto', 'sakura']):
    PICS['kid_' + kid] = f'{B}/all-characters/{i:02d}_kid_{kid}_guitar.png'
for career in ['retail', 'food', 'chef', 'hospitality', 'medical', 'farm', 'musician', 'producer', 'painter', 'fashion']:
    PICS['avatar_' + career] = f'{B}/careers-ui/avatar_{career}.png'
for f in sorted(os.listdir(f'{B}/health/body')):
    PICS['ill_' + os.path.splitext(f)[0]] = f'{B}/health/body/{f}'
# Every area from above (DemoRecordingTests.Maps_TopDown): a straight-down map and a 50° aerial view.
for f in sorted(os.listdir(f'{B}/maps')):
    PICS[os.path.splitext(f)[0].lower()] = f'{B}/maps/{f}'
# Career icons from the game's own icon set (white, drawn on the industry colour in the deck).
ICONS = os.path.normpath(os.path.join(B, '..', 'NihongoLife', 'Assets', 'NihongoLife', 'Resources', 'UI', 'Icons'))
for career, icon in {'retail': 'local_convenience_store', 'food': 'ramen_dining', 'chef': 'set_meal', 'hospitality': 'hotel', 'medical': 'medical_services',
                     'farm': 'agriculture', 'musician': 'music_note', 'producer': 'headphones', 'painter': 'draw', 'fashion': 'checkroom'}.items():
    PICS['icon_' + career] = os.path.join(ICONS, f'ic_{icon}.png')
# The feature section's captures (feature_pics.py).
sys.path.insert(0, HERE)
from feature_pics import F  # noqa: E402
PICS.update({key: f'{B}/{rel}' for key, rel in F.items()})
missing = []
for name, src in PICS.items():
    if not os.path.exists(src):
        missing.append(name)
        continue
    im = Image.open(src)
    if name == 'vtc' or name.split('_')[0] in ('team', 'avatar', 'icon', 'logo'):  # transparent: kept as PNG
        im.thumbnail((900, 900))
        im.save(os.path.join(OUT, name + '.png'))
    else:
        im = im.convert('RGB')
        im.thumbnail((1600, 1600))
        im.save(os.path.join(OUT, name + '.jpg'), quality=86)
for logo in ('logo_dark', 'logo_light'):
    Image.open(os.path.join(B, 'Logo', logo + '.png')).save(os.path.join(OUT, logo + '.png'))
print(len(PICS) - len(missing), 'pictures;', 'missing:', missing)
