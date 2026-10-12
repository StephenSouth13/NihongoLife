# Builds the v5 file from the v4 one (removed from Bao_Cao; restore with: git show HEAD:Bao_Cao/<v4 file> > ../<v4 file>).
# Usage: python -I <this file> <playmode passed> <playmode total>
import copy, os, sys
import docx
from docx.shared import Cm

BAO = r'D:\VTC_Academy\NihongoLife\NihongoLife\Bao_Cao'
src = os.path.join(BAO, 'NihongoLife_Bao_Cao_Capstone_v4.docx')
dst = os.path.join(BAO, 'NihongoLife_Bao_Cao_Capstone_v5.docx')
passed, total = sys.argv[1], sys.argv[2]
d = docx.Document(src)

def heading(text):
    for p in d.paragraphs:
        if p.style.name.startswith('Heading') and p.text.strip().startswith(text): return p
    raise KeyError(text)

body_style = None
for p in d.paragraphs:
    if p.style.name in ('Normal', 'Body Text') and len(p.text) > 120: body_style = p.style; break
caption_style = next((s for s in d.styles if s.name == 'Caption'), None)

class Inserter:
    def __init__(self, before): self.before = before
    def h2(self, text): self.before.insert_paragraph_before(text, style='Heading 2')
    def para(self, text):
        p = self.before.insert_paragraph_before(text)
        if body_style is not None: p.style = body_style
    def bullets(self, items):
        for it in items:
            try: self.before.insert_paragraph_before(it, style='List Bullet')
            except KeyError: self.para('• ' + it)
    def image(self, path, caption, width=15.5):
        p = self.before.insert_paragraph_before()
        p.alignment = 1
        p.add_run().add_picture(path, width=Cm(width))
        c = self.before.insert_paragraph_before(caption)
        c.alignment = 1
        if caption_style is not None: c.style = caption_style
    def table(self, rows):
        t = d.add_table(rows=len(rows), cols=len(rows[0]))
        try: t.style = 'Table Grid'
        except Exception: pass
        for i, r in enumerate(rows):
            for j, v in enumerate(r): t.cell(i, j).text = v
        self.before._p.addprevious(t._tbl)

img = lambda *p: os.path.join(BAO, *p)

# ---- Chương 5: nhân vật --------------------------------------------------
w = Inserter(heading('CHƯƠNG 6'))
w.h2('5.4. Toàn bộ nhân vật là Kids (bản v5)')
w.para('Từ bản v5, mọi nhân vật trong game dùng bộ Kids Characters for Survival: tám bé người chơi chọn được, và các NPC (thu ngân, nhân viên ga, đầu bếp, giáo viên, hàng xóm). Prefab NPC giữ nguyên object gốc nên vị trí, hội thoại và kịch bản trong scene không đổi; chỉ model con được thay bằng KidsCharactersSetup.BuildNpcs. Nhân viên đứng sau quầy là “anh chị lớn”, cao khoảng 1,7 m (×1,25) để mặt cao hơn quầy; người qua đường cao như người chơi (khoảng 1,35 m). Bốn model Mixamo người lớn (Remy, Lilly, Elizabeth, Eminem — 1,1 GB) đã bị xóa; các clip Remy@ (ngồi, câu cá, tưới) vẫn được giữ làm animation humanoid cho Kids.')
w.image(img('character-regression', 'npc_station_1.jpg'), 'Hình 5.x. Nhân viên ga Kimura (Kids) cao hơn quầy bán vé', 12)

# ---- Chương 8: mở khóa và ví ----------------------------------------------
w = Inserter(heading('CHƯƠNG 9'))
w.h2('8.7. Mở khóa địa điểm theo điểm kiến thức')
w.para('Người học mới chỉ vào được Hibari Mart, Trường Hibari và nhà trọ — nơi đáp ứng nhu cầu cơ bản và kiếm điểm kiến thức. Các địa điểm khác mở dần theo ngưỡng đặt trong GameControlDatabase. Việc chặn chỉ nằm ở một chỗ (SceneFlowController). Cửa đang khóa ghi rõ số điểm còn thiếu; nơi đã từng vào thì giữ mở; nơi mà bước kịch bản hiện tại cần đến thì không bị chặn.')
w.table([['Địa điểm', 'Điểm kiến thức cần'], ['Hibari Mart · Trường · Nhà trọ', 'Luôn mở'], ['Ga Hibari', '40'], ['Nhà hàng Sushi Hibari', '90'], ['Game Center Hibari', '160'], ['Đảo Xanh (Midori)', '250']])
w.image(img('unlock-regression', 'journal_unlocks.jpg'), 'Hình 8.x. Sổ nhiệm vụ: tiến độ tới địa điểm kế tiếp', 14)
w.h2('8.8. Ví và lịch sử giao dịch')
w.para('Mỗi lần ví thay đổi đều được ghi kèm lý do (mua ở Hibari Mart, phí phòng khám, thưởng nhiệm vụ, vé tàu…). Trên HUD, số tiền “+¥/−¥” bay lên từ chip tiền, và chip đổi màu. Bấm vào chip để xem 30 giao dịch gần nhất (WalletLedger, WalletFeedback).')
w.image(img('unlock-regression', 'wallet_history.jpg'), 'Hình 8.y. Lịch sử ví và số tiền bay lên từ chip', 14)

# ---- Chương 10: UI ------------------------------------------------------
w = Inserter(heading('CHƯƠNG 11'))
w.h2('10.6. Cẩm nang địa điểm trong bản đồ')
w.para('Từ bản đồ (M), nút “Cẩm nang địa điểm” hoặc dấu “?” cạnh từng nơi mở trang cẩm nang. Mỗi trang có ảnh chụp trong game (chỉ cảnh 3D, chụp bằng GuidePhotosPlayModeTests), ý nghĩa của địa điểm trong đời sống ở Nhật, cách tương tác, câu tiếng Nhật hữu ích và trạng thái mở khóa. Nội dung nằm trong Resources/Guide/locations.json, không viết cứng trong UI.')
w.image(img('unlock-regression', 'guide_konbini.jpg'), 'Hình 10.x. Cẩm nang: Hibari Mart', 14)
w.h2('10.7. Chọn đúng người khi nhấn F giữa đám đông')
w.para('InteractionDetector chấm điểm từng thứ trong tầm với theo khoảng cách, hướng nhân vật và tâm màn hình. Mục tiêu hiện tại được cộng điểm để không nhảy qua lại. Phím Q chuyển mục tiêu, bấm chuột để chọn. Người được chọn có vòng vàng dưới chân và nhãn “▼ [F] Tên · Q đổi i/n”.')
w.image(img('online-regression', 'crowd_target_cycled.jpg'), 'Hình 10.y. Hai người chơi khác đứng gần: mục tiêu F rõ ràng, Q để đổi', 14)

# ---- Chương 11: online ----------------------------------------------------
w = Inserter(heading('CHƯƠNG 12'))
w.h2('11.5. Thế giới online nhiều người chơi')
w.para('Client Supabase Realtime được viết lại: nhiều kênh trên một socket; thread riêng cho đọc và ghi (ClientWebSocket của Mono chặn thread pool); hàng đợi gửi tuần tự; bộ đọc JSON chuẩn; tự kết nối lại. Mỗi địa điểm được chia thành các phòng (shard) tối đa 40 người. Vị trí gửi 2–6 Hz tùy độ đông và tự chậm lại khi server báo vượt giới hạn; avatar nội suy mượt. Chat khu vực, cộng đồng và nhóm là tạm thời, không lưu. Tin nhắn riêng được mã hóa đầu-cuối (RSA-2048 + AES-256 + HMAC, có chữ ký và mã an toàn). Bạn bè hiện online/offline theo kênh lobby. Voice (Agora) có voice khu vực theo khoảng cách, voice nhóm, gọi riêng, giữ T để nói và khử ồn.')
w.image(img('online-regression', 'chat_area.jpg'), 'Hình 11.x. Cửa sổ trò chuyện: khu vực, cộng đồng, nhóm, riêng tư, bạn bè', 14)
w.para('Kết quả load test (Tools/online_load_test.py, cùng giao thức với game, trên gói Supabase hiện tại):')
w.table([['Người trong 1 phòng', 'Tin đến nơi', 'Độ trễ p50 / p95', 'Ghi chú'],
         ['20', '97,9%', '96 / 407 ms', 'không lỗi giới hạn'],
         ['40', '96,8%', '131 / 457 ms', 'không lỗi giới hạn'],
         ['60 kết nối từ 1 máy', '—', '—', '39/60 bị từ chối kết nối']])
w.para('Muốn hơn 1000 người cùng lúc cần nâng gói Supabase hoặc chuyển phần đồng bộ vị trí sang game server riêng. Kiến trúc shard và chat hiện tại vẫn giữ được.')

# ---- Chương 13: kiểm thử ---------------------------------------------------
w = Inserter(heading('CHƯƠNG 14'))
w.h2('13.6. Kiểm thử đợt v5')
w.para(f'Toàn bộ bộ Play Mode: {passed}/{total} test qua (không tính các test quay video và chụp ảnh chạy riêng). Test mới: OnlineSocialPlayModeTests (Supabase thật: realtime, shard, chat nhóm, tin riêng mã hóa — kênh nghe trộm chỉ thấy bản mã, bạn bè online/offline, F trong đám đông), LocationUnlocksPlayModeTests (cửa khóa, bản đồ, Sổ nhiệm vụ, cẩm nang, ví), KidsNpcScalePlayModeTests (mọi NPC là Kids, đúng tỉ lệ), NeedsWeatherPlayModeTests (thời tiết ảnh hưởng chỉ số, cảnh báo trước khi phạt, cầu vồng).')

# cover/version text
for p in d.paragraphs:
    for r in p.runs:
        if 'v4' in r.text or 'V4' in r.text: r.text = r.text.replace('v4', 'v5').replace('V4', 'V5')
d.save(dst)
print('saved', dst)
