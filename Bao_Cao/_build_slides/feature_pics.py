"""Game captures for the deck's feature section ("Chức năng chi tiết"): key -> path under Bao_Cao.
Imported by prep_images.py; contact sheets for picking them: python feature_pics.py <out_dir>."""
F = {
    # exams and study
    'f_jlpt_vocab': 'jlpt-regression/01_vocab_question.jpg', 'f_jlpt_listen': 'jlpt-regression/02_listening_playing.jpg',
    'f_jlpt_result': 'jlpt-regression/03_results_analysis.jpg', 'f_ielts_select': 'school-regression/03a_ielts_selection.png',
    'f_ielts_write': 'ielts-productive/02_writing_examiner.jpg', 'f_ielts_coach': 'ielts-productive/03_writing_coach.jpg',
    'f_ielts_speak': 'ielts-productive/07_speaking_part2.jpg', 'f_ielts_speak_res': 'ielts-productive/08_speaking_result.jpg',
    'f_torfl': 'russian-ui/06_ExamCenterPopup_ru.png', 'f_exam_desk': 'school-regression/02_exam_desk.png',
    'f_teacher': 'school-regression/06_teacher_comment.png', 'f_exam_started': 'school-regression/04_exam_started.png',
    'f_video_controls': 'school-regression/01a_video_controls.png', 'f_studyhub': 'course/30_studyhub_six_tiles_vi.png',
    'f_placement': 'course/20_placement_question.png', 'f_placement_res': 'course/21_placement_result.png',
    'f_dict': 'course/40_dict_ja_vi.png', 'f_tenses': 'course/08_tenses_vi.png', 'f_alphabet': 'course/04_alphabet_ja_vi.png',
    'f_cert': 'course/06_certificates_ja_vi.png', 'f_grammar': 'course/05_grammar_ja_vi.png',
    'f_en_course': 'english-course/02_a1_sentence_pattern_feedback.png',
    # Midori island
    'f_mi_onboard': 'island-regression/01_onboard_to_midori.png', 'f_mi_seed': 'island-regression/08_farm_seed_picker.png',
    'f_mi_grow': 'island-regression/09_farm_growing.png', 'f_mi_ready': 'island-regression/11_farm_ready.png',
    'f_mi_till': 'fishing-regression/42_till_with_hoe.jpg', 'f_mi_water': 'fishing-regression/44_water.jpg',
    'f_mi_cast': 'fishing-regression/50_cast.jpg', 'f_mi_bite': 'fishing-regression/52_bite.jpg', 'f_mi_reel': 'fishing-regression/53_reel.jpg',
    'f_mi_caught': 'fishing-regression/54_caught.jpg', 'f_mi_sellfish': 'fishing-regression/55_sell_fish.jpg',
    'f_mi_afk': 'fishing-regression/60b_afk_overview.jpg', 'f_mi_animal': 'island-regression/16_animal_fed.png',
    'f_mi_shop': 'island-regression/03_shop_agriculture.png', 'f_mi_notebook': 'island-regression/17_notebook.png',
    'f_mi_swim': 'swim/midori_2_stroke.png', 'f_mi_decor_shop': 'island-decor/01_shop_decor.png', 'f_mi_edit': 'island-decor/06_moving_piece.png',
    'f_mi_decorated': 'island-decor/10_overview_decorated.png', 'f_mi_visit': 'island-decor/11_visit_friend.png',
    'f_mi_jp': 'island-decor/jp_torii.png', 'f_mi_overview': 'island-regression/18_island_overview.png', 'f_mi_birds': 'island-people/01_bird_on_ground.png',
    # Game Center
    'f_gc_kana_breaker': 'arcade/kana_breaker_vi.jpg', 'f_gc_kana_rain': 'arcade/kana_rain_vi.jpg', 'f_gc_air_hockey': 'arcade/air_hockey_vi.jpg',
    'f_gc_dance': 'arcade/dance_arrows_vi.jpg', 'f_gc_echo': 'arcade/echo_pads_vi.jpg', 'f_gc_hoop': 'arcade/hoop_shot_vi.jpg',
    'f_gc_basket': 'arcade/basket_count_vi.jpg', 'f_gc_snake': 'arcade/kana_snake_vi.jpg', 'f_gc_order': 'arcade/order_rush_vi.jpg',
    'f_gc_tense': 'arcade/tense_typer_vi.jpg', 'f_gc_ufo': 'arcade/ufo_catcher_vi.jpg', 'f_gc_wheel': 'arcade/wheel_quiz_vi.jpg',
    'f_gc_shooter': 'arcade/word_shooter_vi.jpg', 'f_gc_whack': 'arcade/word_whack_vi.jpg', 'f_gc_match': 'gamecenter-regression/07_matching.png',
    'f_gc_prize': 'gamecenter-regression/09b_prize_counter.png',
    # konbini, sushi, clinic, station
    'f_kb_box': 'lifeloop-regression/06_carrying_box.png', 'f_kb_restock': 'lifeloop-regression/07_restock_progress.png',
    'f_kb_question': 'lifeloop-regression/08_customer_question.png', 'f_kb_total': 'lifeloop-regression/09_register_total.png',
    'f_kb_paid': 'lifeloop-regression/10_paid.png', 'f_kb_inside': 'konbini-camera/02_inside_after_door.png',
    'f_su_order': 'lifeloop-regression/20_sushi_order.png', 'f_su_served': 'sushi-flow/01_served.png', 'f_su_paid': 'lifeloop-regression/21_sushi_paid.png',
    'f_su_customers': 'town-life/05_sushi_customers.png', 'f_su_tray': 'sushi-flow/04_tray_front.png',
    'f_cl_reception': 'clinic/03_reception.png', 'f_cl_form': 'clinic/04_questionnaire.png', 'f_cl_eye': 'clinic/08_eye_test.png',
    'f_cl_number': 'clinic/11_number_called.png', 'f_cl_shift': 'clinic/07_shift.png', 'f_cl_bed': 'clinic/09_woke_in_clinic.png',
    'f_cl_consult': 'clinic/12_consultation.png',
    'f_st_kimura': 'station-regression/02_at_kimura.png', 'f_st_gate': 'station-regression/gate_open.png', 'f_st_board': 'train-route/20_hall_route_board.png',
    'f_st_leave': 'train-route/02_leaving_hibari.png', 'f_st_midori': 'train-route/08_stop_midori.png', 'f_st_airport': 'train-route/10_stop_airport.png',
    'f_st_pq': 'train-route/11_arrived_phuquoc.png', 'f_st_queue': 'train-route/21_platform_queue.png',
    # music, fashion, home, town
    'f_mu_dj': 'music-shop/11_dj_mix.png', 'f_mu_paint': 'music-shop/13_painting_done.png', 'f_mu_card': 'music-shop/07_painting_card.png',
    'f_mu_wall': 'music-shop/06_gallery_wall_close.png', 'f_mu_busk': 'busking/03_busking.png', 'f_mu_gig': 'music-shop/10_gig_done.png',
    'f_mu_piano': 'gestures/piano_hands_musician_yuto.png',
    'f_fa_floor': 'fashion/02_floor_west.jpg', 'f_fa_quiz': 'fashion/20_quiz_FashionAdvise.jpg', 'f_fa_till': 'fashion/04_fitting_till.jpg',
    'f_fa_kid': 'fashion/10_kid_hina_summer_beach.jpg',
    'f_be_fridge': 'bedroom-regression/05_fridge.png', 'f_be_sink': 'bedroom-regression/04_sink.png', 'f_be_sleep': 'bedroom-regression/10_sleep_night.png',
    'f_be_morning': 'bedroom-regression/11_morning.png', 'f_be_correct': 'bedroom-regression/07_study_correct.png',
    'f_to_map': 'map-progression/04_map_all_open.png', 'f_to_handbook': 'handbook/03_places.png', 'f_to_story': 'story-flow/01_new_game_story_starts.png',
    'f_to_journal': 'map-progression/02_journal_roadmap.png', 'f_to_unlock': 'map-progression/01_unlock_card_sushi.png',
    'f_to_hud': 'hud-regression/hud_1920x1080.jpg', 'f_to_rain': 'hud-regression/needs_rain_cold.jpg',
    # Phú Quốc, online
    'f_pq_overview': 'phuquoc/01_island_overview_crop.png', 'f_pq_anthoi': 'phuquoc/03_an_thoi_hon_thom.png', 'f_pq_swim': 'swim/phuquoc_2_stroke.png',
    'f_on_friends_chat': 'online-regression/chat_friends.jpg', 'f_on_groups': 'online-regression/chat_groups.jpg',
    'f_on_crowd': 'online-regression/crowd_target_first.jpg', 'f_on_visit_like': 'island-decor/12_visit_liked.png',
    # career milestone stories (CareerStoryPlayModeTests)
    'f_cs_question': 'career-story/02_story_question.png', 'f_cs_reply': 'career-story/03_story_reply.png',
    'f_cs_interview': 'career-story/04_interview_after_story.png',
    # career tasks (CareerTasksPlayModeTests), the game's own lesson videos (VideoCoursesPlayModeTests), phone controls
    'f_ct_card': 'career-tasks/01_task_card.png', 'f_ct_hint': 'career-tasks/03_wrong_hint.png', 'f_ct_overview': 'career-tasks/04_career_tasks.png',
    'f_vs_course': 'video-courses/03_studio_course.png', 'f_vs_play': 'video-courses/04_studio_playing.png',
    'f_ph_hud': 'phone-controls/phone_web_915x412_hud.png', 'f_ph_menu': 'phone-controls/phone_web_915x412_dpr1_vi_main.png',
    'f_ph_wide': 'phone-controls/phone_2400x1080_hud.png',
}

if __name__ == '__main__':
    import os
    import sys
    from PIL import Image, ImageDraw, ImageFont
    here = os.path.dirname(os.path.abspath(__file__))
    base = os.path.normpath(os.path.join(here, '..'))
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    keys = [k for k in F if os.path.exists(os.path.join(base, F[k]))]
    print('missing:', [k for k in F if k not in keys])
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 22)
    per = 12
    for si in range(0, len(keys), per):
        sheet = Image.new('RGB', (4 * 480, 3 * 330), (20, 20, 20))
        d = ImageDraw.Draw(sheet)
        for j, k in enumerate(keys[si:si + per]):
            im = Image.open(os.path.join(base, F[k])).convert('RGB')
            im.thumbnail((470, 290))
            x, y = (j % 4) * 480, (j // 4) * 330
            sheet.paste(im, (x + 5, y + 5))
            d.text((x + 8, y + 300), k, fill=(255, 220, 80), font=font)
        sheet.save(os.path.join(out, f's{si // per:02d}.jpg'), quality=80)
    print(len(keys), 'pictures')
