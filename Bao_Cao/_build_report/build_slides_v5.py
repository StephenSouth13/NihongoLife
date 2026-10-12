# Builds the v5 file from the v4 one (removed from Bao_Cao; restore with: git show HEAD:Bao_Cao/<v4 file> > ../<v4 file>).
# Usage: python -I <this file> <playmode passed> <playmode total>
import copy, os, sys, xml.etree.ElementTree as ET
from PIL import Image
from pptx import Presentation

BAO = r'D:\VTC_Academy\NihongoLife\NihongoLife\Bao_Cao'
SCR = os.path.join(os.path.dirname(os.path.abspath(__file__)), '_v5_images'); os.makedirs(SCR, exist_ok=True)
src = os.path.join(BAO, 'NihongoLife_Thuyet_Trinh_Capstone_v4.pptx')
dst = os.path.join(BAO, 'NihongoLife_Thuyet_Trinh_Capstone_v5.pptx')
passed, total = sys.argv[1], sys.argv[2]

p = Presentation(src)

def shape(slide, name):
    for sh in slide.shapes:
        if sh.name == name: return sh
    raise KeyError(name)

def set_text(slide, name, text):
    tf = shape(slide, name).text_frame
    runs = [r for para in tf.paragraphs for r in para.runs]
    if not runs: tf.text = text; return
    runs[0].text = text
    for r in runs[1:]: r.text = ''
    for para in tf.paragraphs[1:]:
        for r in para.runs: r.text = ''

def cropped(path, w, h, tag):
    im = Image.open(path).convert('RGB')
    target = w / h
    W, H = im.size
    if W / H > target:
        nw = int(H * target); im = im.crop(((W - nw) // 2, 0, (W - nw) // 2 + nw, H))
    else:
        nh = int(W / target); im = im.crop((0, (H - nh) // 2, W, (H - nh) // 2 + nh))
    out = os.path.join(SCR, f'v5_{tag}.jpg'); im.save(out, quality=90); return out

def replace_picture(slide, pic, path, tag):
    l, t, w, h = pic.left, pic.top, pic.width, pic.height
    img = cropped(path, w, h, tag)
    pic._element.getparent().remove(pic._element)
    slide.shapes.add_picture(img, l, t, w, h)

def pictures(slide):
    return [sh for sh in slide.shapes if sh.shape_type == 13]

# ---- 07 ONLINE -------------------------------------------------------------
s = p.slides[7]
set_text(s, 'title', 'Online thật: shard, chat, bạn bè, voice')
set_text(s, 'use0', 'Shard 40 người · chat · bạn bè · tin riêng E2E')
set_text(s, 'state0', '4/4 test online thật · 40 người: 96,8% tin đến')
set_text(s, 'use1', 'Phát âm · NPC AI · IELTS Writing/Speaking')
set_text(s, 'state1', 'Chạy thật, có giới hạn chi phí')
set_text(s, 'use2', 'Voice khu vực · nhóm · gọi riêng · giữ T để nói')
set_text(s, 'state2', 'Code xong · chờ deploy agora-voice')
replace_picture(s, pictures(s)[0], os.path.join(BAO, 'online-regression', 'chat_area.jpg'), 'online')

# ---- 08 NHÂN VẬT -----------------------------------------------------------
s = p.slides[8]
set_text(s, 'kicker', '08 · NHÂN VẬT')
set_text(s, 'title', '100% nhân vật Kids — người chơi lẫn NPC')
imgs = [('character-regression', 'poses_kid_haruto.jpg', 'HARUTO'), ('character-regression', 'poses_kid_yui.jpg', 'YUI'),
        ('character-regression', 'npc_station_1.jpg', 'NHÂN VIÊN GA'), ('character-regression', 'npc_city_1.jpg', 'HÀNG XÓM'),
        ('online-regression', 'crowd_target_first.jpg', 'NGƯỜI CHƠI KHÁC')]
for i, (pic, (folder, f, label)) in enumerate(zip(sorted(pictures(s), key=lambda x: x.left), imgs)):
    replace_picture(s, pic, os.path.join(BAO, folder, f), f'kid{i}')
    set_text(s, f'pose{i}', label)
set_text(s, 'audit', '8 bé chơi được · NPC dùng model Kids (nhân viên ×1,25 ≈ 1,7 m cho vừa quầy) · đã xóa 4 model Mixamo người lớn (1,1 GB)')
set_text(s, 'gap', 'Clip Remy@ (ngồi, câu cá, tưới) được giữ làm animation humanoid cho Kids — không còn model người lớn nào trong scene.')

# ---- 09 KIỂM THỬ -----------------------------------------------------------
s = p.slides[9]
set_text(s, 'metric', f'{passed} / {total}')
set_text(s, 'metric-label', 'Play Mode (mốc v5 · 12/10/2026)')
set_text(s, 'evidence', 'Supabase thật · load test 20/40 người · ảnh tự chụp · va chạm và input thật')
replace_picture(s, pictures(s)[0], os.path.join(BAO, 'unlock-regression', 'map_locked.jpg'), 'tests')

# ---- 10 LỘ TRÌNH -----------------------------------------------------------
s = p.slides[10]
set_text(s, 'title', 'Bước tiếp theo sau v5')
set_text(s, 'kicker', '12 · LỘ TRÌNH'); set_text(s, 'page', '13')
for i, (a, b) in enumerate([('Deploy voice', 'agora-voice + thử 2 máy'), ('Mở rộng online', 'gói Supabase cho 1000+ người'),
                            ('Nội dung N4', 'tình huống ở ga, đảo, quán'), ('Kiểm chứng', 'Play Mode + ảnh + load test')]):
    set_text(s, f'st{i}', a); set_text(s, f'sd{i}', b)
set_text(s, 'guardrail', 'Không tạo scene mới · lưu kết quả vào scene/prefab · chỉ báo xong khi có Play Mode và ảnh chứng minh')

# ---- new slides (copied from 04 GAMEPLAY layout) ---------------------------
template = p.slides[4]
def new_slide(kicker, title, page, lead, bullets, image, tag):
    slide = p.slides.add_slide(template.slide_layout)
    for ph in list(slide.placeholders): ph._element.getparent().remove(ph._element)
    for sh in template.shapes:
        if sh.shape_type == 13: continue
        slide.shapes._spTree.append(copy.deepcopy(sh._element))
    set_text(slide, 'kicker', kicker); set_text(slide, 'title', title); set_text(slide, 'page', page); set_text(slide, 'lead', lead)
    for i, b in enumerate(bullets): set_text(slide, f'bullet-text-{i}', b)
    pic = pictures(template)[0]
    img = cropped(image, pic.width, pic.height, tag)
    slide.shapes.add_picture(img, pic.left, pic.top, pic.width, pic.height)
    return slide

a = new_slide('10 · MỞ KHÓA & CẨM NANG', 'Kiến thức mở dần thế giới', '11',
              'Bắt đầu từ Hibari Mart và trường học; điểm kiến thức mở ga, quán sushi, game center rồi Đảo Xanh.',
              ['Ga 40 · Sushi 90 · Game Center 160 · Đảo 250', 'Cửa khóa nói rõ còn thiếu bao nhiêu điểm', 'Bản đồ + Sổ nhiệm vụ hiện tiến độ', 'Cẩm nang: ảnh thật, ý nghĩa, câu mẫu'],
              os.path.join(BAO, 'unlock-regression', 'guide_station_locked.jpg'), 'guide')
b = new_slide('11 · XÃ HỘI ONLINE', 'Chơi cùng nhau — không ồn, không lag', '12',
              'Mỗi khu vực chia phòng 40 người; chat tạm thời, tin riêng mã hóa đầu-cuối.',
              ['Vị trí 2–6 Hz, tự giảm khi phòng đông', 'Chat khu vực · cộng đồng · nhóm · bạn bè', 'Tin riêng RSA + AES, có mã an toàn', 'F chọn đúng người: vòng vàng, Q đổi'],
              os.path.join(BAO, 'online-regression', 'crowd_target_cycled.jpg'), 'social')

# move the two new slides before the closing slide
ids = p.slides._sldIdLst
items = list(ids)
closing = items[10]  # insert before the roadmap
for el in (items[-2], items[-1]):
    ids.remove(el)
    ids.insert(list(ids).index(closing), el)

# closing slide: v4 -> v5
for sh in p.slides[len(p.slides._sldIdLst) - 1].shapes:
    if sh.has_text_frame:
        for para in sh.text_frame.paragraphs:
            for r in para.runs: r.text = r.text.replace('V4', 'V5')
p.save(dst)
print('saved', dst, len(p.slides._sldIdLst))
