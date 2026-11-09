// NihongoLife capstone deck: one deck, every part of the game, pictures captured in the game.
// Owner's rules: Roboto, no text under 30 pt (Japanese lines use Yu Gothic), the school's logo on every slide, a table of contents,
// one slide per scene, per career (icon + avatar) and the illnesses with their research. Builder of the game: Quách Thành Long.
// Usage: python prep_images.py <work_dir>; node build_deck.js <work_dir>/img <work_dir>/video <out.pptx>  (pptxgenjs 3.12)
const fs = require('fs');
const path = require('path');
const pptxgen = require('pptxgenjs');

const [IMG_DIR, VIDEO_DIR, OUT] = process.argv.slice(2);
const RES = path.join(__dirname, '..', '..', 'NihongoLife', 'Assets', 'NihongoLife', 'Resources');
const json = p => JSON.parse(fs.readFileSync(path.join(RES, p), 'utf8').replace(/^﻿/, ''));
const CAREERS = json('Progression/careers.json').careers;
const INDUSTRY = json('Progression/career_industries.json');
const ILLNESSES = json('Health/illnesses.json');

const pres = new pptxgen();
pres.layout = 'LAYOUT_WIDE'; // 13.333 x 7.5 in
pres.author = 'Quách Thành Long';
pres.company = 'VTC Academy';
pres.title = 'NihongoLife – Báo cáo Capstone';
pres.theme = { headFontFace: 'Roboto', bodyFontFace: 'Roboto' };

const NAVY = '0F1B33', NAVY2 = '1B2A4A', CREAM = 'F7F3EA', GOLD = 'F2B33D', RED = 'D9483B', TEAL = '2E8B7A', WHITE = 'FFFFFF', MUTED = 'C8D2E2', INK = '1B2438';
const FONT = 'Roboto', JP = 'Yu Gothic';
const W = 13.333;

const img = name => {
  for (const ext of ['.png', '.jpg']) { const p = path.join(IMG_DIR, name + ext); if (fs.existsSync(p)) return p; }
  throw new Error('missing picture ' + name);
};

for (const [name, dark] of [['DARK', true], ['LIGHT', false]]) {
  pres.defineSlideMaster({
    title: name,
    background: { color: dark ? NAVY : CREAM },
    objects: [
      { image: { x: 0.45, y: 0.28, w: 1.62, h: 0.45, path: img(dark ? 'logo_dark' : 'logo_light') } },
      { rect: { x: 11.3, y: 0.26, w: 1.6, h: 0.46, fill: { color: WHITE }, line: { color: dark ? NAVY2 : 'E4DCCB', width: 0.75 } } },
      { image: { x: 11.38, y: 0.355, w: 1.44, h: 0.244, path: img('vtc') } },
    ],
  });
}

// ─────────── helpers ───────────

let section = '';
function slide(title, { dark = true, notes = '', number = true } = {}) {
  const s = pres.addSlide({ masterName: dark ? 'DARK' : 'LIGHT', sectionTitle: section });
  if (title) s.addText(title, { x: 0.45, y: 0.85, w: 12.4, h: 0.85, fontFace: FONT, fontSize: 40, bold: true, color: dark ? WHITE : NAVY, margin: 0, valign: 'middle', isTextBox: true, objectName: 'Title' });
  if (number) s.slideNumber = { x: 12.05, y: 6.98, w: 0.85, h: 0.45, fontFace: FONT, fontSize: 30, color: dark ? '8FA3BF' : '8A8070', align: 'right' };
  if (notes) s.addNotes(notes);
  s.dark = dark;
  return s;
}

// Text: never under 30 pt (the owner's rule); Roboto, Japanese in Yu Gothic.
function text(s, value, o = {}) {
  const size = o.fontSize ?? 30;
  if (size < 30) throw new Error('text under 30 pt: ' + JSON.stringify(value).slice(0, 60));
  s.addText(value, { fontFace: FONT, color: s.dark ? WHITE : INK, margin: 0, isTextBox: true, valign: 'top', ...o, fontSize: size });
}

function picture(s, name, x, y, w, h, caption) {
  s.addImage({ path: img(name), x, y, w, h, sizing: { type: 'cover', w, h }, objectName: 'Picture ' + name, altText: caption || name });
  if (caption) {
    s.addShape(pres.shapes.RECTANGLE, { x, y: y + h - 0.66, w, h: 0.66, fill: { color: NAVY, transparency: 18 }, line: { color: NAVY, transparency: 100 } });
    s.addShape(pres.shapes.OVAL, { x: x + 0.2, y: y + h - 0.44, w: 0.22, h: 0.22, fill: { color: GOLD }, line: { color: GOLD } });
    text(s, caption, { x: x + 0.55, y: y + h - 0.66, w: w - 0.7, h: 0.66, valign: 'middle', color: WHITE });
  }
}

function bullets(s, items, x, y, w, h, color) {
  s.addText(items.map((t, i) => ({ text: t, options: { bullet: true, breakLine: i < items.length - 1, paraSpaceAfter: 12 } })),
    { x, y, w, h, fontFace: FONT, fontSize: 30, color: color ?? (s.dark ? WHITE : INK), margin: 0, valign: 'top', isTextBox: true });
}

function card(s, x, y, w, h, head, body, accent = GOLD) {
  s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w, h, rectRadius: 0.12, fill: { color: s.dark ? NAVY2 : WHITE }, line: { color: accent, width: 1.5 } });
  text(s, head, { x: x + 0.25, y: y + 0.18, w: w - 0.5, h: 0.6, bold: true, color: accent, fontSize: 32 });
  if (body) text(s, body, { x: x + 0.25, y: y + 0.82, w: w - 0.5, h: h - 0.95, color: s.dark ? MUTED : INK });
}

function stat(s, x, y, w, value, label, accent = GOLD) {
  s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w, h: 2.3, rectRadius: 0.12, fill: { color: s.dark ? NAVY2 : WHITE }, line: { color: s.dark ? NAVY2 : 'E4DCCB' } });
  text(s, value, { x: x + 0.25, y: y + 0.15, w: w - 0.5, h: 1.0, fontSize: 60, bold: true, color: accent });
  text(s, label, { x: x + 0.25, y: y + 1.15, w: w - 0.5, h: 1.05, color: s.dark ? MUTED : INK });
}

function video(s, file, cover, x, y, w, h, caption, link) {
  const poster = 'data:image/jpeg;base64,' + fs.readFileSync(img(cover)).toString('base64');
  s.addMedia({ type: 'video', path: path.join(VIDEO_DIR, file), cover: poster, x, y, w, h });
  text(s, caption, { x, y: y + h + 0.1, w, h: 0.55, color: GOLD, align: 'center', ...(link ? { hyperlink: { url: 'https://' + link } } : {}) });
}

// A full-bleed picture covers the master's school logo: it is drawn again on top.
function schoolLogo(s) {
  s.addShape(pres.shapes.RECTANGLE, { x: 11.3, y: 0.26, w: 1.6, h: 0.46, fill: { color: WHITE }, line: { color: NAVY2, width: 0.75 } });
  s.addImage({ path: img('vtc'), x: 11.38, y: 0.355, w: 1.44, h: 0.244 });
}

// A round badge with a white icon (careers) or a kanji (illnesses) on a colour.
function badge(s, x, y, d, color, iconName, kanji) {
  s.addShape(pres.shapes.OVAL, { x, y, w: d, h: d, fill: { color }, line: { color: WHITE, width: 1.5 } });
  if (iconName) s.addImage({ path: img(iconName), x: x + d * 0.2, y: y + d * 0.2, w: d * 0.6, h: d * 0.6 });
  if (kanji) text(s, kanji, { x, y, w: d, h: d, fontFace: JP, bold: true, color: WHITE, align: 'center', valign: 'middle', fontSize: Math.max(30, Math.round(d * 40)) });
}

const industryOf = id => {
  const look = INDUSTRY.looks.find(l => l.career === id);
  return INDUSTRY.industries.find(i => i.id === look.industry);
};
const colorOf = id => industryOf(id).color.replace('#', '');
// Spoken-length titles: notes in brackets dropped, the first part of "A · B".
const shortTitle = t => t.replace(/\s*\([^)]*\)/g, '').split(' · ')[0].trim();
const SHORT_NAME = { retail: 'Bán lẻ', food: 'Ẩm thực', farm: 'Nông nghiệp', hospitality: 'Nhà hàng PQ', medical: 'Y tế', chef: 'Đầu bếp', musician: 'Nhạc công', producer: 'DJ & sản xuất', painter: 'Hoạ sĩ', fashion: 'Thời trang' };
// The branch the career video follows (Tools/video/career_voice.py).
const BRANCH = { retail: 'store', food: 'owner', farm: 'grower', hospitality: 'manager', medical: 'doctor', chef: 'sushi', musician: 'pianist', producer: 'producer', painter: 'fine_art', fashion: 'designer' };

// A feature slide: 3–4 game captures side by side, each with a short head (gold) and what it does (30 pt).
// A head is a string or runs ([{ text, ja }]) so Japanese words get the Japanese font.
function features(f) {
  const s = slide(f.title, { notes: f.notes });
  const n = f.items.length, gap = 0.2, w = (12.45 - gap * (n - 1)) / n;
  f.items.forEach((it, i) => {
    const x = 0.45 + i * (w + gap);
    s.addImage({ path: img(it.pic), x, y: 1.9, w, h: 2.8, sizing: { type: 'cover', w, h: 2.8 }, altText: typeof it.head === 'string' ? it.head : it.head.map(r => r.text).join('') });
    const head = typeof it.head === 'string' ? it.head : it.head.map(r => ({ text: r.text, options: r.ja ? { fontFace: JP } : {} }));
    text(s, head, { x, y: 4.82, w, h: 0.6, bold: true, color: GOLD });
    text(s, it.body, { x, y: 5.45, w, h: 1.45, color: MUTED });
  });
}

// Six captures in a 3 × 2 grid with a one-line caption each (the Game Center's machines).
function grid6(f) {
  const s = slide(f.title, { notes: f.notes });
  f.items.forEach((it, i) => {
    const w = 4.03, h = 1.95, x = 0.45 + (i % 3) * (w + 0.18), y = 1.9 + Math.floor(i / 3) * 2.6;
    s.addImage({ path: img(it.pic), x, y, w, h, sizing: { type: 'cover', w, h }, altText: it.head });
    text(s, it.head, { x, y: y + h + 0.03, w, h: 0.55, bold: true, color: GOLD, align: 'center' });
  });
}

// ─────────── content ───────────

const SCENES = [
  { title: 'Menu chính', pic: 'menu', meaning: 'Cửa vào, chọn cách học', acts: ['Chọn 1 trong 8 nhân vật', '4 ngôn ngữ giao diện', 'Chơi khách hoặc tài khoản'], ja: 'ようこそ！', vi: 'Chào mừng!', notes: 'Scene 01_MainMenu: thị trấn 3D phía sau, nhân vật xem trước, đổi ngôn ngữ (Việt, Anh, Nhật, Nga) đổi cả chữ lẫn đơn vị tiền, chọn chế độ máy tính hay điện thoại. Khách chơi mở toàn bộ thị trấn; tài khoản chơi theo lộ trình và vào được online.' },
  { title: 'Thị trấn Hibari', pic: 'town', meaning: 'Giao tiếp mỗi ngày', acts: ['Chào hàng xóm, hỏi đường', 'Bảng tin, nhiệm vụ', 'Xe cộ, đèn giao thông'], ja: 'おはようございます。', vi: 'Chào buổi sáng.', notes: 'Scene 90_TestSandbox: phố chính, xe chạy đúng làn, dừng trước vạch, người đi bộ; bảng tin khu phố nhận nhiệm vụ giúp hàng xóm; bản đồ M chỉ đường tới mọi nơi.' },
  { title: 'Hibari Mart (konbini)', pic: 'konbini', meaning: 'Tự lo bữa ăn', acts: ['Lấy hàng, ra quầy tính tiền', 'Trả lời chuyện túi, hâm nóng', 'Làm thêm kiếm tiền'], ja: '袋はいりますか？', vi: 'Có cần túi không?', notes: 'Konbini mở 24/7 ở Nhật. Lấy hàng (F), ra quầy, trả lời thu ngân bằng tiếng Nhật; bảng アルバイト nhận ca làm thêm; đồ ăn uống hồi chỉ số đói, khát. Nguồn: Resources/Guide/locations.json.' },
  { title: 'Phòng trọ Hibari Heights', pic: 'bedroom', meaning: 'Nghỉ ngơi, tự chăm sóc', acts: ['Ngủ qua đêm, hồi sức', 'Tủ lạnh, bồn rửa', 'Bàn học ôn bài'], ja: 'ただいま。', vi: 'Tôi về rồi.', notes: 'Scene 45_HomeBedroom, mở từ đầu (0 trí tuệ): giường để ngủ và qua ngày, tủ lạnh, bồn rửa tay, bàn học cho câu hỏi ôn tập cộng trí tuệ.' },
  { title: 'Ga Hibari và tuyến tàu', pic: 'station', meaning: 'Đi lại công cộng ở Nhật', acts: ['Mua vé ở máy', 'Qua cổng soát vé', 'Tàu tới đảo, sân bay'], ja: '切符を一枚ください。', vi: 'Cho tôi một vé.', notes: 'Scene 20_StationDistrict, mở ở 160 trí tuệ: máy bán vé, nhân viên Kimura, cổng soát vé là lối duy nhất ra sân ga; tàu 5 ga, mỗi ga có cảnh riêng ngoài cửa sổ.' },
  { title: 'Nhà hàng Sushi Hibari', pic: 'sushi', meaning: 'Văn hoá ẩm thực', acts: ['Xem thực đơn có hình', 'Gọi món bằng tiếng Nhật', 'Trả tiền, làm phục vụ'], ja: 'お会計お願いします。', vi: 'Cho tôi tính tiền.', notes: 'Scene 30_SushiRestaurant, mở ở 100 trí tuệ: ngồi bàn, thực đơn có hình, cách đọc và giá; ăn bằng đũa; trả tiền ở quầy; ca làm phục vụ và đầu bếp.' },
  { title: 'Phòng khám Hibari', pic: 'clinic', meaning: 'Kể bệnh bằng tiếng Nhật', acts: ['Đăng ký, phiếu hỏi bệnh', 'Khám với bác sĩ', 'Trả tiền, nhận thuốc'], ja: '熱があります。', vi: 'Tôi bị sốt.', notes: 'Scene 35_HibariClinic: 受付 → 問診票 → 診察 → 会計 → お薬, khỏi hẳn sau khi khám; ngất thì tỉnh lại trên giường phòng khám; ca làm của ngành y.' },
  { title: 'Trường Nhật ngữ Hibari', pic: 'school', meaning: 'Học có hệ thống', acts: ['Bàn đầu lớp mở Lớp học', 'Thi thử JLPT, IELTS', 'Phòng luyện nói'], ja: '質問があります。', vi: 'Em có câu hỏi.', notes: 'Scene 40_HIBARICLASS: lớp của cô Morita; bàn đầu lớp mở cửa sổ Lớp học (chọn ngôn ngữ, bài học, video, thi thử); phòng Speaking luyện nói với giáo viên qua video.' },
  { title: 'Game Center Hibari', pic: 'arcade', meaning: 'Ôn chữ bằng trò chơi', acts: ['Kana Match, Word Shooter', 'Ném bóng rổ, gắp thú', 'Đổi vé lấy quà'], ja: 'もう一回！', vi: 'Thêm lần nữa!', notes: 'Scene 50_GameCenter, mở ở 240 trí tuệ (Kim rủ đi sau giờ học): mỗi máy một trò, trò nào cũng luyện chữ hoặc số đếm; vé thắng được đổi quà ở quầy.' },
  { title: 'Hibiki Music & Art', pic: 'music', meaning: 'Âm nhạc và hội hoạ', acts: ['Nhân vật tự chơi đàn', 'DJ và sân khấu', 'Phòng tranh 39 bức'], ja: 'きれいな絵ですね。', vi: 'Bức tranh đẹp quá.', notes: 'Scene 55_MusicShop: nhân vật tự chơi bài nhạc (chỉnh nhịp, âm lượng), chơi tay bằng bàn phím vẫn có; bàn DJ, sân khấu; phòng tranh 3D treo 39 tác phẩm; nghề nhạc công, DJ, hoạ sĩ.' },
  { title: 'Hibari Mode (thời trang)', pic: 'tryon', meaning: 'Mua sắm có văn hoá', acts: ['18 bộ đồ, 7 nhóm', 'Thử đồ miễn phí', 'Ca làm thời trang'], ja: '試着できますか？', vi: 'Thử đồ được không?', notes: 'Scene 56_FashionStore: thử đồ chỉ đổi hình hiển thị, mua mới vào tủ đồ, bạn bè online cũng thấy; ca làm gấp đồ, tư vấn cỡ, dẫn khách vào phòng thử.' },
  { title: 'Đảo Xanh Midori', pic: 'fishing', meaning: 'Lao động và thiên nhiên', acts: ['Gieo hạt, tưới, thu hoạch', 'Câu 16 loài cá', 'Bơi biển'], ja: '魚が釣れた！', vi: 'Câu được cá rồi!', notes: 'Scene 60_MidoriIsland, mở ở 320 trí tuệ, đi tàu từ ga Hibari: ruộng (cuốc, gieo, tưới, thu hoạch), câu cá ở bến từ cá nhỏ tới cá mập, bơi biển, bán nông sản.' },
  { title: 'Đảo Phú Quốc', pic: 'phuquoc', meaning: 'Cầu nối Việt – Nhật', acts: ['Bản đồ thật OpenStreetMap', 'Quán Gánh Ngon, chợ', 'Phục vụ khách Nhật'], ja: 'いらっしゃいませ。', vi: 'Xin mời vào.', notes: 'Scene 65_PhuQuocIsland, dựng từ toạ độ OpenStreetMap thật (1 đơn vị = 100 m): Dương Đông, chợ, khu nghỉ dưỡng; quán Gánh Ngon là nơi làm của nghề nhà hàng Phú Quốc.' },
];

function sceneSlide(sc, i) {
  const s = slide(`${sc.title}`, { notes: sc.notes });
  picture(s, sc.pic, 0.45, 1.95, 6.8, 4.95, `Khu vực ${i + 1}/${SCENES.length}`);
  text(s, sc.meaning, { x: 7.55, y: 1.95, w: 5.35, h: 0.65, bold: true, color: GOLD });
  bullets(s, sc.acts, 7.55, 2.75, 5.35, 2.75);
  s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: 7.55, y: 5.6, w: 5.35, h: 1.3, rectRadius: 0.1, fill: { color: NAVY2 }, line: { color: GOLD, width: 1 } });
  text(s, sc.ja, { x: 7.75, y: 5.66, w: 5.0, h: 0.6, fontFace: JP, bold: true, color: GOLD });
  text(s, sc.vi, { x: 7.75, y: 6.24, w: 5.0, h: 0.6, color: MUTED });
}

function careerSlide(c) {
  const color = colorOf(c.id);
  const branch = c.branches.find(b => b.id === BRANCH[c.id]);
  const other = c.branches.find(b => b.id !== BRANCH[c.id]);
  const levels = [...c.levels, ...branch.levels];
  const s = slide(c.nameVi, { notes: `${c.descriptionVi} Nơi làm: ${c.workplaceVi}; sếp: ${c.bossNameVi}; giờ làm ${c.workStart}:00–${c.workEnd}:00. Ba bậc chung: ${c.levels.map(l => l.titleVi).join(', ')}. Bậc 4 chọn nhánh: ${branch.nameVi} (${branch.levels.map(l => l.titleVi).join(', ')}) hoặc ${other.nameVi} (${other.levels.map(l => l.titleVi).join(', ')}). Mỗi lần lên bậc là một buổi phỏng vấn tiếng Nhật với ${c.bossNameVi}.` });
  s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: 0.45, y: 1.95, w: 4.4, h: 4.95, rectRadius: 0.15, fill: { color: NAVY2 }, line: { color, width: 2 } });
  s.addImage({ path: img('avatar_' + c.id), x: 0.6, y: 2.2, w: 4.1, h: 2.95, sizing: { type: 'contain', w: 4.1, h: 2.95 } });
  badge(s, 0.65, 2.1, 0.95, color, 'icon_' + c.id);
  text(s, c.workplaceVi, { x: 0.7, y: 5.15, w: 3.9, h: 1.0, color: MUTED });
  text(s, c.bossNameVi, { x: 0.7, y: 6.3, w: 3.9, h: 0.5, bold: true, color: GOLD });
  text(s, `Bậc 4: ${branch.nameVi} hoặc ${other.nameVi}`, { x: 5.15, y: 1.95, w: 7.75, h: 0.95, color: GOLD });
  levels.forEach((l, i) => {
    const y = 2.95 + i * 0.66;
    const top = i === levels.length - 1;
    badge(s, 5.15, y + 0.04, 0.52, top ? GOLD.replace('#', '') : i >= 3 ? color : '3A4A6B');
    text(s, String(i + 1), { x: 5.15, y: y + 0.04, w: 0.52, h: 0.52, align: 'center', valign: 'middle', bold: true, color: top ? NAVY : WHITE });
    text(s, shortTitle(l.titleVi), { x: 5.85, y, w: 7.05, h: 0.6, valign: 'middle', bold: top, color: top ? GOLD : WHITE });
  });
}

function illnessGrid(list, n) {
  const s = slide(`16 bệnh thường gặp (${n}/2)`, { notes: list.map(x => `${x.vi} (${x.kanji}, ${x.ja}): ${x.cureVi}`).join(' ') });
  list.forEach((x, i) => {
    const cx = 0.45 + (i % 4) * 3.15, cy = 1.95 + Math.floor(i / 4) * 2.5;
    s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: cx, y: cy, w: 3.0, h: 2.35, rectRadius: 0.1, fill: { color: NAVY2 }, line: { color: x.color.replace('#', ''), width: 1.5 } });
    s.addImage({ path: img('ill_' + x.id), x: cx + 0.05, y: cy + 0.05, w: 2.9, h: 1.25, sizing: { type: 'cover', w: 2.9, h: 1.25 } });
    badge(s, cx + 0.12, cy + 0.12, 0.75, x.color.replace('#', ''), null, x.glyph);
    text(s, ILL_SHORT[x.id] || x.vi, { x: cx + 0.1, y: cy + 1.32, w: 2.8, h: 1.0, valign: 'middle', align: 'center' });
  });
}
const ILL_SHORT = { cold: 'Cảm lạnh', flu: 'Cúm', muscle_ache: 'Đau mỏi cơ', overwork: 'Kiệt sức', heatstroke: 'Say nắng', stomach: 'Khó tiêu', hay_fever: 'Dị ứng hoa', headache: 'Đau đầu', norovirus: 'Norovirus', bruise: 'Bầm tím', hand_foot_mouth: 'Tay chân miệng', conjunctivitis: 'Đau mắt đỏ', strep_throat: 'Viêm họng', otitis_media: 'Viêm tai giữa', heat_rash: 'Rôm sảy', toothache: 'Sâu răng' };

const MEANING = [
  { kanji: '学', head: 'Học qua tình huống thật', short: 'Nói được ngoài đời', title: 'Học ngôn ngữ như đang sống ở Nhật', color: TEAL, pic: 'loop', caption: 'Hội thoại có cách đọc và nghĩa',
    points: ['Câu nói gắn với việc thật', 'Sai được giải thích, thử lại', 'Trí tuệ mở thế giới mới'],
    notes: 'Học theo tình huống: mỗi câu tiếng Nhật xuất hiện đúng lúc cần dùng (mua đồ, hỏi đường, gọi món, khám bệnh). Câu có cách đọc và nghĩa; trả lời sai được giải thích và chọn lại, không mất tiến trình. Điểm trí tuệ mở khu vực và chương truyện mới, nên tiến bộ hiện ra ngay trong thế giới.' },
  { kanji: '職', head: 'Định hướng nghề nghiệp', short: '10 ngành, 6 bậc', title: 'Định hướng nghề nghiệp', color: GOLD, pic: 'career_interview', caption: 'Phỏng vấn thăng chức',
    points: ['Thử 10 ngành trước khi chọn', 'Thăng chức bằng phỏng vấn', 'Hiểu văn hoá nơi làm việc'],
    notes: 'Người chơi thử 10 ngành (bán lẻ, ẩm thực, đầu bếp, nhà hàng Phú Quốc, y tế, nông nghiệp, nhạc công, DJ, hoạ sĩ, thời trang), mỗi ngành 6 bậc, bậc 4 chọn nhánh. Mỗi lần lên bậc là một buổi phỏng vấn tiếng Nhật với sếp; ca làm dạy kính ngữ và cách cư xử nơi làm việc ở Nhật.' },
  { kanji: '橋', head: 'Cầu nối Việt – Nhật', short: 'Hibari và Phú Quốc', title: 'Cầu nối Việt Nam – Nhật Bản', color: RED, pic: 'phuquoc', caption: 'Gánh Ngon, Phú Quốc',
    points: ['Chuẩn bị sang Nhật học, làm', 'Đón khách Nhật ở Phú Quốc', 'Ba kết thúc: ở lại, về nước'],
    notes: 'Người Việt chuẩn bị du học, đi làm ở Nhật làm quen trước với đời sống ở đó. Ở Phú Quốc (bản đồ OpenStreetMap thật), quán Gánh Ngon đón đoàn khách Nhật: dùng tiếng Nhật ngay trên quê hương. Cốt truyện kết thúc bằng lựa chọn nơi sống: ở lại Nhật, về Việt Nam làm cầu nối, hay sống giữa hai nước.' },
  { kanji: '生', head: 'Kỹ năng sống', short: 'Văn hoá, sức khoẻ, tiền', title: 'Kỹ năng sống và văn hoá', color: '4C6FD6', pic: 'clinic', caption: 'Phòng khám Hibari',
    points: ['Lịch sự nơi công cộng', 'Chữa bệnh đúng y khoa', 'Quản lý tiền, ngân hàng'],
    notes: 'Phép lịch sự: chào hỏi, đi tàu, đổ rác đúng ngày, いただきます trước bữa ăn. Sức khoẻ: 16 bệnh, thuốc không kê đơn chỉ làm dịu, kháng sinh không trị cảm cúm (nguồn y khoa Nhật). Tiền: lương, chi tiêu, ngân hàng có quỹ khẩn cấp không lãi, công ty chốt sổ mỗi tuần.' },
];
const LEVELS = [['N5', '0 trí tuệ', 'Chương 1–4', 'Sống tự lập'], ['N4', '150 trí tuệ', 'Chương 5–6', 'Chọn nghề'], ['N3', '400 trí tuệ', 'Chương 7', 'Thi N3'], ['N2', '800 trí tuệ', 'Chương 8', 'Làm đàn anh'], ['N1', '1400 trí tuệ', 'Chương 9', 'Cầu nối']];
const MAPS = [
  { file: '90_testsandbox', scene: '90_TestSandbox', title: 'thị trấn Hibari-chō', spots: ['Phố Sakura, ga, trường', 'Cửa hàng, phòng khám'], notes: 'Phố chính Sakura-dōri có vỉa hè, phố bên Bắc–Nam, hẻm sau và công viên. 11 địa điểm trên bản đồ M: Hibari Mart, bảng tin, Sushi Hibari, nhà trọ, ga, trường, công viên, Game Center, phòng khám, tiệm nhạc Hibiki, Hibari Mode.' },
  { file: '20_stationdistrict', scene: '20_StationDistrict', title: 'ga Hibari', spots: ['Máy bán vé, cổng soát vé', 'Sân ga: đi Midori'], notes: 'Máy bán vé, cổng soát vé (lối duy nhất giữa sảnh và sân ga), sân ga số 2 đi sân bay qua đảo Midori, lối ra thành phố.' },
  { file: '30_sushirestaurant', scene: '30_SushiRestaurant', title: 'Sushi Hibari', spots: ['Lễ tân, quầy bếp Ota', 'Khu bàn ăn'], notes: 'Lễ tân đón khách, quầy đầu bếp Ota, khu bàn ăn có thực đơn hình, lối ra phố.' },
  { file: '35_hibariclinic', scene: '35_HibariClinic', title: 'phòng khám Hibari', spots: ['Tiếp nhận, phòng khám', 'Giường nghỉ, nhận thuốc'], notes: 'Quầy tiếp nhận (受付), phòng chờ, phòng khám bác sĩ (診察室), khu khám sức khoẻ và giường nghỉ, quầy nhận thuốc (お薬).' },
  { file: '40_hibariclass', scene: '40_ HIBARICLASS', title: 'trường Hibari', spots: ['Bảng đen, cô Morita', 'Bàn học, bàn đầu lớp'], notes: 'Bảng đen và cô Morita, bàn học sinh, bàn đầu lớp mở cửa sổ Lớp học, lối ra phố.' },
  { file: '45_homebedroom', scene: '45_HomeBedroom', title: 'phòng trọ', spots: ['Giường, bàn học', 'Bếp, tủ lạnh'], notes: 'Giường để ngủ qua đêm, bàn học ôn từ, bếp và tủ lạnh, cửa ra phố.' },
  { file: '50_gamecenter', scene: '50_GameCenter', title: 'Game Center', spots: ['Các máy luyện chữ', 'Bóng rổ, gắp thú'], notes: 'Dãy máy Kana Match và các trò luyện chữ, ném bóng rổ, máy gắp thú, quầy đổi quà, lối ra phố.' },
  { file: '55_musicshop', scene: '55_MusicShop', title: 'Hibiki Music & Art', spots: ['Phòng tranh 39 bức', 'Piano, DJ, giá vẽ'], notes: 'Phòng tranh 3D (39 tác phẩm), sân khấu và bàn DJ, đàn piano, tường guitar, giá vẽ, sách nhạc lý, quầy chú Hibiki.' },
  { file: '56_fashionstore', scene: '56_FashionStore', title: 'Hibari Mode', spots: ['Các nhóm trang phục', 'Phòng thử, quầy'], notes: 'Khu thường ngày, công sở, thể thao, theo mùa và lễ hội; phòng thử đồ và gương; quầy anh Ren.' },
  { file: '60_midoriisland', scene: '60_MidoriIsland', title: 'đảo Midori', spots: ['Ruộng, chuồng thú', 'Bến câu cá, ga tàu'], notes: 'Đảo tròn có ga tàu ở phía Nam, đường chữ thập, ruộng trồng trọt, chuồng thú, bến câu cá và bãi biển để bơi.' },
  { file: '65_phuquocisland', scene: '65_PhuQuocIsland', title: 'Phú Quốc (Dương Đông)', spots: ['Phố, chợ, bờ biển', 'Quán Gánh Ngon'], notes: 'Khu trung tâm Dương Đông quanh quán Gánh Ngon: phố, chợ, khu nghỉ dưỡng, bờ biển. Cả đảo dựng từ toạ độ OpenStreetMap, 1 đơn vị = 100 m.' },
];

// What can be done in each area, every picture captured in the game (Bao_Cao/_build_slides/feature_pics.py).
const FEATURES = [
  { title: 'Thị trấn Hibari: đi đâu, làm gì', notes: 'Bản đồ M chỉ đường tới 11 địa điểm. Cốt truyện 9 chương: thẻ nhiệm vụ dẫn từng bước, sổ nhiệm vụ (J) ghi lộ trình. Tài khoản mở khu mới bằng trí tuệ (khách chơi mở hết). Thời tiết và 4 mùa: mưa ướt người dễ bị cảm lạnh, trời nóng dễ say nắng. Sổ tay Y: hướng dẫn, địa điểm, sức khoẻ, nghề nghiệp.', items: [
    { pic: 'f_to_map', head: 'Bản đồ M', body: 'Chỉ đường tới 11 nơi trong thị trấn' },
    { pic: 'f_to_story', head: 'Cốt truyện', body: '9 chương, nhiệm vụ dẫn từng bước' },
    { pic: 'f_to_unlock', head: 'Mở khu mới', body: 'Đủ trí tuệ thì khu mới mở ra' },
    { pic: 'f_to_rain', head: 'Thời tiết', body: 'Mưa ướt người dễ bị cảm lạnh' },
  ] },
  { title: 'Hibari Mart (konbini)', notes: 'Mua đồ ăn uống để hồi chỉ số đói và khát; thu ngân hỏi bằng tiếng Nhật (có cần túi, có hâm nóng không). Bảng アルバイト nhận ca làm thêm: bày hàng lên kệ, tính tiền, trả lời câu hỏi của khách. Quầy thuốc không kê đơn: thuốc chỉ làm đỡ triệu chứng, muốn khỏi phải đi khám (đúng thực tế y khoa).', items: [
    { pic: 'konbini', head: 'Làm thêm', body: 'Bày hàng, tính tiền, trả lời khách bằng tiếng Nhật' },
    { pic: 'f_kb_inside', head: 'Mua đồ ăn uống', body: 'Ăn uống để hồi chỉ số đói và khát' },
    { pic: 'medicine_shelf', head: 'Quầy thuốc', body: 'Thuốc không kê đơn chỉ làm đỡ triệu chứng' },
  ] },
  { title: 'Phòng trọ Hibari Heights', notes: 'Phòng của nhân vật, mở từ đầu. Tủ lạnh: lấy cơm nắm (hồi đói +30). Bồn rửa: uống nước (hồi khát +35). Bàn học: câu ôn từ vựng N5, trả lời đúng được cộng trí tuệ. Giường: ngủ qua đêm, ngủ đủ 8 tiếng hồi đầy sức khoẻ, sáng ra chào おはようございます.', items: [
    { pic: 'f_be_fridge', head: 'Tủ lạnh', body: 'Lấy cơm nắm, hồi đói' },
    { pic: 'f_be_sink', head: 'Bồn rửa', body: 'Uống nước, hồi khát' },
    { pic: 'f_be_correct', head: 'Bàn học', body: 'Ôn từ vựng, cộng trí tuệ' },
    { pic: 'f_be_morning', head: 'Ngủ qua đêm', body: 'Ngủ 8 tiếng hồi đầy sức khoẻ' },
  ] },
  { title: 'Ga Hibari và chuyến tàu', notes: 'Mua vé ở quầy của anh Kimura (hỏi bằng tiếng Nhật) hoặc máy bán vé. Cổng soát vé là lối duy nhất giữa sảnh và sân ga, có vé mới mở. Tuyến 5 ga: Hibari, Gakuen-mae, Minato, Đảo Midori, Sân bay; từ sân bay bay tới Phú Quốc. Ngoài cửa sổ mỗi ga có cảnh riêng, đổi cảnh trong đường hầm, sân ga có biển tên trượt vào dừng đúng cửa sổ.', items: [
    { pic: 'f_st_kimura', head: 'Quầy vé', body: 'Mua vé với anh Kimura' },
    { pic: 'f_st_gate', head: 'Cổng soát vé', body: 'Có vé mới vào sân ga' },
    { pic: 'f_st_board', head: 'Tuyến 5 ga', body: 'Từ Hibari tới sân bay' },
    { pic: 'f_st_airport', head: 'Ngoài cửa sổ', body: 'Mỗi ga một cảnh riêng' },
  ] },
  { title: 'Nhà hàng Sushi Hibari', notes: 'Thực đơn có hình, tên món, cách đọc và giá. Ngồi bàn ăn bằng đũa, ăn xong trả tiền ở quầy (お会計お願いします). Ca làm phục vụ: bưng khay món ra bàn; nghề đầu bếp làm ở quầy bếp của bác Ota.', items: [
    { pic: 'sushi_menu', head: 'Thực đơn có hình', body: 'Tên món, cách đọc và giá' },
    { pic: 'sushi', head: 'Ăn bằng đũa', body: 'Ngồi bàn ăn, xong trả tiền ở quầy' },
    { pic: 'f_su_tray', head: 'Ca phục vụ', body: 'Bưng khay món ra bàn cho khách' },
  ] },
  { title: 'Phòng khám Hibari: khám như ở Nhật', notes: 'Năm bước như phòng khám thật ở Nhật: 受付 (tiếp nhận, đưa thẻ bảo hiểm), 問診票 (phiếu hỏi bệnh, chọn triệu chứng bằng tiếng Nhật), 診察 (bác sĩ hỏi và khám), 会計 (trả tiền), お薬 (nhận thuốc). Khám xong khỏi hẳn. Có khám mắt, gọi số chờ; ngất thì tỉnh lại trên giường phòng khám; ca làm của ngành y.', items: [
    { pic: 'f_cl_reception', head: [{ text: '受付', ja: true }, { text: ' tiếp nhận' }], body: 'Chào lễ tân, đưa thẻ bảo hiểm' },
    { pic: 'f_cl_form', head: [{ text: '問診票', ja: true }], body: 'Chọn triệu chứng bằng tiếng Nhật' },
    { pic: 'clinic', head: [{ text: '診察', ja: true }, { text: ' khám' }], body: 'Bác sĩ hỏi bệnh và khám' },
    { pic: 'clinic_med', head: [{ text: 'お薬', ja: true }, { text: ' thuốc' }], body: 'Nhận thuốc, khám xong là khỏi' },
  ] },
  { title: 'Hibiki Music & Art', notes: 'Ngồi vào đàn grand piano: nhân vật tự đàn 36 bài, chỉnh nhịp, âm lượng, đổi piano/guitar; tay đặt đúng trên phím đàn. Sân khấu: diễn với nhịp 80–125% thì khán giả thích, có thêm fan. Bàn DJ: chọn bài hợp tông, nhịp để nối. Giá vẽ: tô tranh theo số; phòng tranh 3D treo 39 tác phẩm. Mua guitar thì đeo sau lưng, ra phố đàn kiếm tiền. Nghề nhạc công, DJ, hoạ sĩ.', items: [
    { pic: 'music', head: 'Tự đàn piano', body: '36 bài, chỉnh nhịp và âm lượng' },
    { pic: 'f_mu_gig', head: 'Diễn sân khấu', body: 'Nhịp 80–125% khán giả mới thích' },
    { pic: 'f_mu_dj', head: 'Bàn DJ', body: 'Chọn bài hợp nhịp để nối' },
    { pic: 'f_mu_paint', head: 'Vẽ tranh', body: 'Tô theo số; phòng tranh 39 bức' },
  ] },
  { title: 'Hibari Mode (thời trang)', notes: '18 bộ đồ chia nhóm thường ngày, công sở, thể thao, theo mùa, lễ hội. Thử đồ miễn phí, chỉ đổi hình hiển thị; rời cửa sổ là trở lại đồ đang mặc. Mua rồi thì vào tủ đồ, bạn bè online cũng thấy. Ca làm thời trang: gấp đồ, tư vấn cỡ, trả lời khách bằng tiếng Nhật, dẫn khách vào phòng thử.', items: [
    { pic: 'tryon', head: 'Thử đồ', body: 'Thử miễn phí, rời quầy là trả lại' },
    { pic: 'f_fa_quiz', head: 'Ca làm thời trang', body: 'Tư vấn khách bằng tiếng Nhật' },
    { pic: 'kids_fashion', head: '18 bộ đồ', body: 'Mua rồi bạn bè online cũng thấy' },
  ] },
  { title: 'Đảo Midori: nông trại', notes: 'Ruộng 4 bước, mỗi việc kèm một động từ tiếng Nhật (つちを たがやします, みずを あげます…). Chưa có cuốc thì đào bằng tay, chậm hơn. 4 loại cây: cà rốt, cà chua, ngô, bí ngô; mỗi giai đoạn lớn cần tưới lại; cây vẫn lớn trong lúc người chơi rời đảo. Bộ quà chào mừng: cuốc, bình tưới, 3 túi hạt cà rốt.', items: [
    { pic: 'f_mi_till', head: 'Cuốc đất', body: 'Chưa có cuốc thì đào tay, chậm hơn' },
    { pic: 'f_mi_seed', head: 'Gieo hạt', body: 'Cà rốt, cà chua, ngô, bí ngô' },
    { pic: 'f_mi_water', head: 'Tưới nước', body: 'Mỗi giai đoạn lớn cần tưới lại' },
    { pic: 'f_mi_ready', head: 'Thu hoạch', body: 'Cây vẫn lớn khi bạn đi vắng' },
  ] },
  { title: 'Đảo Midori: câu cá', notes: 'Đứng ở bến, quăng cần; cá cắn câu thì kéo ngay. 16 loài cá có tên tiếng Nhật, từ cá bơn (ひらめ), cá tráp, cá nóc tới cá kiếm và cá mập. Cần câu tự động thả câu trong lúc người chơi làm việc khác. Cá bán ở cửa hàng Midori.', items: [
    { pic: 'f_mi_cast', head: 'Quăng cần', body: 'Đứng ở bến, quăng cần ra biển' },
    { pic: 'f_mi_bite', head: 'Cá cắn câu', body: 'Thấy báo là kéo ngay' },
    { pic: 'f_mi_caught', head: 'Câu được', body: '16 loài, từ cá bơn tới cá mập' },
    { pic: 'f_mi_sellfish', head: 'Bán cá', body: 'Bán ở cửa hàng Midori' },
  ] },
  { title: 'Đảo Midori: thú nuôi, cửa hàng, biển', notes: '5 con vật: bò, alpaca, lừa, ngựa, chó; cho ăn, làm quen, học tên tiếng Nhật. Cửa hàng Midori (phím P): nông nghiệp, thời trang, công nghệ, bán nông sản và cá. Sổ tay đảo: từ vựng đã học, thành tích. Biển không có tường: bơi ra xa rồi lên bờ.', items: [
    { pic: 'f_mi_animal', head: 'Chăm thú', body: 'Bò, alpaca, lừa, ngựa, chó' },
    { pic: 'f_mi_shop', head: 'Cửa hàng', body: 'Hạt giống, dụng cụ, đồ thời trang' },
    { pic: 'f_mi_notebook', head: 'Sổ tay đảo', body: 'Từ đã học và thành tích' },
    { pic: 'midori', head: 'Bơi biển', body: 'Bơi ra xa rồi lên bờ' },
  ] },
  { title: 'Đảo Midori: trang trí, thăm đảo bạn', notes: '32 món trang trí, trong đó có góc Nhật Bản: cổng torii, đèn đá, hoa anh đào, tre, hồ cá koi, đèn lồng chōchin. Tự đặt, xoay, dời từng món; không đặt chồng lên lối đi. Tài khoản online thăm đảo của bạn bè và thả tim.', items: [
    { pic: 'f_mi_decor_shop', head: '32 món', body: 'Cây, hoa, đá, ghế, giếng…' },
    { pic: 'f_mi_edit', head: 'Tự sắp xếp', body: 'Đặt, xoay, dời từng món' },
    { pic: 'f_mi_jp', head: 'Góc Nhật Bản', body: 'Cổng torii, đèn đá, hoa anh đào' },
    { pic: 'f_on_visit_like', head: 'Thăm đảo bạn', body: 'Đi thăm và thả tim (online)' },
  ] },
  { title: 'Đảo Phú Quốc', notes: 'Dựng từ toạ độ OpenStreetMap thật, 1 đơn vị = 100 m, 31 địa danh: Dương Đông, chợ đêm Dinh Cậu, chùa Sùng Hưng, nhà thùng nước mắm, vườn tiêu, Suối Tranh, làng chài Hàm Ninh, Bãi Sao, Bãi Khem, di tích nhà tù Phú Quốc, An Thới và ga cáp treo Hòn Thơm. Quán Gánh Ngon là nơi làm của nghề nhà hàng Phú Quốc: phục vụ khách Nhật bằng tiếng Nhật. Đi tàu ra sân bay rồi bay tới đảo.', items: [
    { pic: 'f_pq_overview', head: 'Bản đồ thật', body: '31 địa danh theo OpenStreetMap' },
    { pic: 'phuquoc', head: 'Gánh Ngon', body: 'Quán ăn: phục vụ khách Nhật' },
    { pic: 'f_pq_anthoi', head: 'An Thới', body: 'Quần đảo phía Nam, cáp treo Hòn Thơm' },
    { pic: 'f_pq_swim', head: 'Bơi biển', body: 'Bãi biển của đảo' },
  ] },
];

// The study part in detail: mock tests of four systems, AI marking, results, video lessons, the learner's own path.
const STUDY = [
  { title: 'Thi thử: 4 hệ thống, nhiều cấp độ', notes: 'JLPT N5–N1: từ vựng, chữ Hán, ngữ pháp, đọc hiểu (đề N5 có thêm phần nghe). IELTS Academic: Nghe, Đọc, Viết, Nói. Tiếng Anh A1–C1 (20 câu mỗi đề) và đề 12 thì 60 câu. TORFL tiếng Nga A1–B2: từ vựng ngữ pháp, đọc, nghe. Đề tự soạn theo định dạng kỳ thi, không phải đề chính thức; kết quả lưu vào lịch sử.', items: [
    { pic: 'f_jlpt_vocab', head: 'JLPT N5 → N1', body: 'Từ vựng, ngữ pháp, đọc; N5 có nghe' },
    { pic: 'f_ielts_select', head: 'IELTS', body: 'Academic: Nghe, Đọc, Viết, Nói' },
    { pic: 'f_exam_started', head: 'Anh A1 → C1', body: 'Thêm đề 12 thì, 60 câu' },
    { pic: 'f_torfl', head: 'TORFL', body: 'Tiếng Nga, đề thử A1 → B2' },
  ] },
  { title: 'IELTS Viết và Nói: chấm bằng AI', notes: 'Bài Viết Task 1 và Task 2 được giám khảo AI ước tính band theo 4 tiêu chí (Task Achievement, Coherence and Cohesion, Lexical Resource, Grammatical Range and Accuracy) kèm nhận xét. Phần Nói Part 1–3 ghi âm câu trả lời, có thời gian chuẩn bị, chấm theo lưu loát, từ vựng, ngữ pháp, phát âm. Điểm là ước tính để luyện tập, không phải điểm chính thức.', items: [
    { pic: 'f_ielts_write', head: 'Viết Task 1, 2', body: 'Band theo 4 tiêu chí, kèm nhận xét' },
    { pic: 'f_ielts_speak', head: 'Nói Part 1 → 3', body: 'Ghi âm câu trả lời, có thời gian chuẩn bị' },
    { pic: 'f_ielts_speak_res', head: 'Điểm phần Nói', body: 'Lưu loát, từ vựng, ngữ pháp, phát âm' },
  ] },
  { title: 'Kết quả và nhận xét sau bài thi', notes: 'Sau mỗi đề: điểm, đạt hay chưa đạt, phân tích đúng sai theo từng dạng bài để biết phần yếu; cô Morita nhận xét và khuyên ôn lại. Phần nghe của đề N5 phát lời thoại rồi chọn đáp án.', items: [
    { pic: 'f_jlpt_result', head: 'Phân tích kết quả', body: 'Đúng sai theo từng dạng bài' },
    { pic: 'f_teacher', head: 'Cô Morita nhận xét', body: 'Khuyên ôn lại phần còn yếu' },
    { pic: 'f_jlpt_listen', head: 'Phần nghe', body: 'Nghe lời thoại rồi chọn đáp án' },
  ] },
  { title: 'Học qua video: 181 bài', notes: '4 khoá video: tiếng Nga từ con số 0 (93 bài, 10 phần), tiếng Anh (71 bài, 7 phần), tiếng Nhật kana, mẫu câu, JLPT (10 bài), khám phá NihongoLife (7 bài). Làm như một trang học trực tuyến: giáo trình theo phần, lưu tiến độ đã xem, phát ngay trong game hoặc mở YouTube; video gắn đúng bài học trong Lớp học.', items: [
    { pic: 'catalogue', head: '4 khoá học', body: 'Nga, Anh, Nhật và hướng dẫn game' },
    { pic: 'courses', head: 'Giáo trình theo phần', body: 'Lưu tiến độ đã xem' },
    { pic: 'course_path', head: 'Video theo bài', body: 'Mỗi bài học có video đi kèm' },
  ] },
  { title: 'Lộ trình học của riêng mình', notes: 'Bài kiểm tra đầu vào 20 câu xếp người học vào đúng trình độ (ví dụ 12/20 → N3) rồi mở lộ trình từ bài phù hợp. Bảng chữ cái hiragana, katakana có âm đọc; ngữ pháp từng mẫu câu kèm ví dụ; 12 thì tiếng Anh; từ điển tra qua lại Nhật, Anh, Việt, Nga.', items: [
    { pic: 'f_placement', head: 'Đầu vào', body: '20 câu xếp bạn vào đúng trình độ' },
    { pic: 'f_alphabet', head: 'Bảng chữ cái', body: 'Hiragana, katakana có âm đọc' },
    { pic: 'f_grammar', head: 'Ngữ pháp', body: 'Từng mẫu câu kèm ví dụ' },
    { pic: 'f_dict', head: 'Từ điển', body: 'Tra Nhật, Anh, Việt, Nga' },
  ] },
];

const ARCADE = { title: 'Game Center: 15 trò luyện chữ', notes: '15 máy, trò nào cũng luyện chữ, từ hoặc số: Kana Match, Word Shooter, Order Rush (gọi món), Kana Rain, gắp thú UFO, Basket Count (đếm số), Word Whack, Echo Pads, Kana Snake, Kana Breaker, Dance Arrows, Tense Typer (12 thì tiếng Anh), Wheel Quiz, Air Hockey, Hoop Shot. Vé thắng đổi quà ở quầy bằng tiếng Nhật.', items: [
  { pic: 'f_gc_match', head: 'Kana Match' }, { pic: 'f_gc_kana_breaker', head: 'Kana Breaker' }, { pic: 'f_gc_order', head: 'Order Rush' },
  { pic: 'f_gc_tense', head: 'Tense Typer' }, { pic: 'f_gc_ufo', head: 'Gắp thú UFO' }, { pic: 'f_gc_basket', head: 'Basket Count' },
] };

// ─────────── slides by section (built twice: once to count for the table of contents) ───────────

const SECTIONS = [
  ['Giới thiệu và ý nghĩa', [
    () => {
      const s = slide('', { number: false, notes: 'Báo cáo đồ án tốt nghiệp Project 3 – Capstone, VTC Academy, TP. Hồ Chí Minh, 11/2026. Người thực hiện: Quách Thành Long (K24GD03), xây dựng toàn bộ game. Giảng viên hướng dẫn: Nguyễn Ngọc Chấn.' });
      s.addImage({ path: img('cover_island'), x: 6.2, y: 0, w: W - 6.2, h: 7.5, sizing: { type: 'cover', w: W - 6.2, h: 7.5 } });
      s.addImage({ path: img('logo_dark'), x: 0.6, y: 1.25, w: 4.6, h: 1.28 });
      text(s, [{ text: 'Game giáo dục 3D ', options: { color: WHITE } }, { text: 'học ngôn ngữ', options: { color: GOLD, bold: true } }], { x: 0.6, y: 2.8, w: 5.4, h: 1.3, fontSize: 36, bold: true });
      text(s, 'Nhật · Anh · Nga · Việt', { x: 0.6, y: 4.15, w: 5.4, h: 0.6, color: MUTED });
      text(s, 'Thực hiện:', { x: 0.6, y: 4.95, w: 5.4, h: 0.55, color: MUTED });
      text(s, 'Quách Thành Long', { x: 0.6, y: 5.45, w: 5.4, h: 0.65, bold: true, fontSize: 36 });
      text(s, 'GVHD: Nguyễn Ngọc Chấn', { x: 0.6, y: 6.3, w: 5.4, h: 0.55, color: GOLD });
      schoolLogo(s);
    },
    'TOC',
    () => {
      const s = slide('Vấn đề của người học', { dark: false, notes: 'Người mới học ngoại ngữ thường học thuộc từ giấy, từng câu tách khỏi tình huống: không biết dùng khi hỏi đường, gọi món, phỏng vấn thật. Thiếu động lực vì không thấy tiến bộ. Nguồn: Bao_Cao chương 1–2.' });
      bullets(s, ['Học thuộc từ trên giấy', 'Câu tách khỏi tình huống thật', 'Ra ngoài đời không dám nói', 'Thiếu mục tiêu, nhanh chán'], 0.6, 2.0, 5.6, 4.8);
      picture(s, 'problem', 6.5, 1.95, 6.4, 4.9, 'Học xa tình huống thật');
    },
    () => {
      const s = slide('Ý tưởng: sống ở Nhật để học tiếng Nhật', { notes: 'Mọi việc thường ngày là một tình huống giao tiếp: mua đồ, đi tàu, đi khám, đi làm. Có mục tiêu, phản hồi ngay, gắn với cốt truyện và con đường nghề nghiệp. Nguồn: Bao_Cao mục 1.2, 5.1.' });
      picture(s, 'idea', 0.45, 1.95, 7.6, 4.95, 'Làm thêm ở Hibari Mart');
      bullets(s, ['Mỗi việc thường ngày là một tình huống giao tiếp', 'Có mục tiêu, phản hồi ngay', 'Gắn với cốt truyện và nghề nghiệp'], 8.45, 2.0, 4.45, 4.9);
    },
    () => {
      const s = slide('Ý nghĩa của NihongoLife', { notes: 'NihongoLife là game giáo dục định hướng: người chơi học ngôn ngữ bằng chính cuộc sống trong game. Bốn ý nghĩa: học qua tình huống thật (学), định hướng nghề nghiệp (職), cầu nối Việt – Nhật (橋), kỹ năng sống và văn hoá (生). Mỗi ý nghĩa có một slide riêng ngay sau.' });
      MEANING.forEach((m, i) => {
        const x = 0.45 + i * 3.15;
        s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y: 1.95, w: 3.0, h: 4.95, rectRadius: 0.12, fill: { color: NAVY2 }, line: { color: m.color, width: 2 } });
        s.addImage({ path: img(m.pic), x: x + 0.06, y: 2.01, w: 2.88, h: 2.0, sizing: { type: 'cover', w: 2.88, h: 2.0 } });
        badge(s, x + 0.95, 3.45, 1.1, m.color, null, m.kanji);
        text(s, m.head, { x: x + 0.12, y: 4.65, w: 2.76, h: 1.0, bold: true, color: m.color, align: 'center', valign: 'middle' });
        text(s, m.short, { x: x + 0.12, y: 5.65, w: 2.76, h: 1.15, color: MUTED, align: 'center' });
      });
    },
    ...MEANING.map(m => () => {
      const s = slide(m.title, { dark: false, notes: m.notes });
      picture(s, m.pic, 0.45, 1.95, 6.6, 4.95, m.caption);
      badge(s, 7.4, 1.95, 1.35, m.color, null, m.kanji);
      text(s, m.head, { x: 8.95, y: 2.0, w: 3.95, h: 1.25, bold: true, color: NAVY, valign: 'middle' });
      bullets(s, m.points, 7.4, 3.6, 5.5, 3.3, INK);
    }),
    () => {
      const s = slide('NihongoLife dành cho ai?', { notes: 'Người học tiếng Nhật từ con số 0 (N5) tới N1; người chuẩn bị du học, đi làm ở Nhật (văn hoá, phỏng vấn, nơi làm việc); người luyện IELTS và tiếng Nga (TORFL); giáo viên dạy qua phòng luyện nói video. Khách chơi offline, tài khoản để học cùng người thật.' });
      [['学', 'Người học tiếng Nhật', 'Từ N5 tới N1', TEAL], ['渡', 'Sắp sang Nhật', 'Du học, đi làm', GOLD], ['試', 'Luyện thi', 'IELTS, TORFL', RED], ['師', 'Giáo viên', 'Lớp nói qua video', '4C6FD6']].forEach(([k, h, b, c], i) => {
        const x = 0.45 + i * 3.15;
        s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y: 1.95, w: 3.0, h: 4.95, rectRadius: 0.12, fill: { color: NAVY2 }, line: { color: c, width: 2 } });
        badge(s, x + 0.7, 2.25, 1.6, c, null, k);
        text(s, h, { x: x + 0.12, y: 4.05, w: 2.76, h: 1.35, bold: true, color: c, align: 'center', valign: 'middle' });
        text(s, b, { x: x + 0.12, y: 5.45, w: 2.76, h: 1.2, color: MUTED, align: 'center' });
      });
    },
    () => {
      const s = slide('Nguồn cảm hứng', { notes: 'The Sims: nhu cầu, nghề có bậc, sự kiện nơi làm. Stardew Valley: nông trại ở đảo Midori, nhịp ngày đêm. Sons of the Forest: HUD tối giản. Visual novel (ví dụ Clannad): lựa chọn trong hội thoại, nhiều kết thúc. Logo và ảnh bìa thuộc bản quyền của từng hãng, dùng để trích dẫn.' });
      [['logo_sims', 'The Sims', 'Nhu cầu, nghề có bậc', GOLD], ['logo_stardew', 'Stardew Valley', 'Nông trại, ngày đêm', TEAL], ['logo_sotf', 'Sons of the Forest', 'HUD tối giản', RED], ['logo_clannad', 'Visual novel', 'Lựa chọn, nhiều kết thúc', '8FA3BF']].forEach(([logo, h, b, c], i) => {
        const x = 0.45 + i * 3.15;
        s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y: 1.95, w: 3.0, h: 4.95, rectRadius: 0.12, fill: { color: NAVY2 }, line: { color: c, width: 1.5 } });
        s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: x + 0.15, y: 2.1, w: 2.7, h: 2.4, rectRadius: 0.08, fill: { color: WHITE }, line: { color: WHITE } });
        s.addImage({ path: img(logo), x: x + 0.25, y: 2.2, w: 2.5, h: 2.2, sizing: { type: 'contain', w: 2.5, h: 2.2 } });
        text(s, h, { x: x + 0.15, y: 4.65, w: 2.7, h: 1.0, bold: true, color: c, align: 'center' });
        text(s, b, { x: x + 0.15, y: 5.6, w: 2.7, h: 1.2, color: MUTED, align: 'center' });
      });
    },
    () => {
      const s = slide('Trailer chính thức · 1:25', { notes: 'Video nhúng bản 720p, tự phát khi vào slide (bấm vào khung để dừng hoặc phát lại). Bản gốc trên YouTube: https://youtu.be/VZOgpwRN_TI' });
      video(s, 'trailer.mp4', 'promo_trailer', 2.2, 1.85, 8.9, 5.0, 'youtu.be/VZOgpwRN_TI', 'youtu.be/VZOgpwRN_TI');
    },
    () => {
      const s = slide('Demo đầy đủ chức năng', { notes: 'Một lượt đi qua toàn bộ trò chơi, ghi trực tiếp trong game (DemoRecordingTests.Tour_RecordFrames), có lời dẫn, nhạc gốc của từng khu và phụ đề. Bản đầy đủ: Bao_Cao/NihongoLife_Demo_Day_Du.mp4. Bản demo ngắn trên YouTube: https://youtu.be/618u7bIlArQ' });
      video(s, 'demo.mp4', 'menu', 2.2, 1.85, 8.9, 5.0, 'Lời dẫn, nhạc, phụ đề · ghi trong game');
    },
    () => {
      const s = slide('Vòng lặp chính', { notes: 'Nhận nhiệm vụ, khám phá thế giới, giao tiếp bằng tiếng, nếu sai thì được giải thích và thử lại, nhận thưởng (yên, XP, trí tuệ), mở khu vực mới.' });
      ['Nhận nhiệm vụ', 'Khám phá thế giới', 'Giao tiếp bằng tiếng', 'Sai: giải thích, thử lại', 'Nhận thưởng', 'Mở khu vực mới'].forEach((t, i) => {
        const y = 1.95 + i * 0.83;
        s.addShape(pres.shapes.OVAL, { x: 0.5, y, w: 0.62, h: 0.62, fill: { color: i === 2 ? GOLD : NAVY2 }, line: { color: GOLD, width: 1.5 } });
        text(s, String(i + 1), { x: 0.5, y: y + 0.03, w: 0.62, h: 0.56, align: 'center', valign: 'middle', bold: true, color: i === 2 ? NAVY : GOLD });
        text(s, t, { x: 1.3, y, w: 5.2, h: 0.62, valign: 'middle', bold: i === 2, color: i === 2 ? GOLD : WHITE });
      });
      picture(s, 'loop', 6.75, 1.95, 6.15, 4.95, 'Hội thoại có cách đọc');
    },
  ]],
  ['Thế giới game: 13 khu vực', [
    ...SCENES.map((sc, i) => () => sceneSlide(sc, i)),
    () => {
      const s = slide('Văn hoá Nhật trong đời sống', { dark: false, notes: 'Konbini: mua onigiri, trả tiền, xin túi. Tàu điện: mua vé, qua cổng soát vé. Quán sushi: thực đơn có hình, cách đọc, giá; câu いただきます trước bữa. Phép lịch sự: chào hỏi, đổ rác đúng ngày, cúi chào.' });
      picture(s, 'konbini', 0.45, 1.95, 4.05, 4.95, 'Konbini');
      picture(s, 'train', 4.65, 1.95, 4.05, 4.95, 'Tàu điện');
      picture(s, 'sushi_menu', 8.85, 1.95, 4.05, 4.95, 'Thực đơn sushi');
    },
  ]],
  ['Bản đồ từ trên cao', [
    () => {
      const s = slide('11 khu vực nhìn từ trên cao', { notes: 'Ảnh chụp trực tiếp trong game (DemoRecordingTests.Maps_TopDown): mỗi khu một bản đồ nhìn thẳng xuống (mái và trần trong nhà được ẩn) và một góc nhìn chim bay 50°. Các slide sau là từng khu.' });
      MAPS.forEach((m, i) => {
        const x = 0.45 + (i % 4) * 3.15, y = 1.95 + Math.floor(i / 4) * 1.68;
        s.addImage({ path: img('aerial_' + m.file), x, y, w: 3.0, h: 1.55, sizing: { type: 'cover', w: 3.0, h: 1.55 }, altText: m.title });
      });
      text(s, 'Chụp trong game', { x: 9.9, y: 5.55, w: 3.0, h: 1.4, color: GOLD, valign: 'middle', align: 'center' });
    },
    ...MAPS.map(m => () => {
      const s = slide('Bản đồ: ' + m.title, { notes: `${m.notes} Trái: nhìn thẳng từ trên xuống; phải: góc nhìn chim bay. Scene ${m.scene}.` });
      s.addShape(pres.shapes.RECTANGLE, { x: 0.45, y: 1.95, w: 7.75, h: 4.95, fill: { color: '141C2B' }, line: { color: NAVY2 } });
      s.addImage({ path: img('map_' + m.file), x: 0.45, y: 1.95, w: 7.75, h: 4.95, sizing: { type: 'contain', w: 7.75, h: 4.95 }, altText: 'Bản đồ ' + m.title });
      s.addImage({ path: img('aerial_' + m.file), x: 8.45, y: 1.95, w: 4.45, h: 2.78, sizing: { type: 'cover', w: 4.45, h: 2.78 }, altText: 'Góc nhìn chim bay ' + m.title });
      bullets(s, m.spots, 8.45, 4.95, 4.45, 1.95);
    }),
  ]],
  ['Chức năng từng khu', [
    ...FEATURES.slice(0, 6).map(f => () => features(f)),
    () => grid6(ARCADE),
    ...FEATURES.slice(6).map(f => () => features(f)),
  ]],
  ['Nhân vật và lộ trình', [
    () => {
      const s = slide('8 nhân vật chơi được', { notes: 'Haruto, Yui, Sota, Hina, Riku, Mio, Yuto, Sakura: mỗi em một ngoại hình, màu da, kiểu tóc riêng; ai cũng đi, chạy, chơi đàn, cử chỉ, mặc được mọi bộ đồ. Ảnh: AllCharactersPlayPlayModeTests (mỗi nhân vật vào game và chơi đàn).' });
      ['Haruto', 'Yui', 'Sota', 'Hina', 'Riku', 'Mio', 'Yuto', 'Sakura'].forEach((n, i) => {
        const x = 0.45 + (i % 4) * 3.15, y = 1.95 + Math.floor(i / 4) * 2.5;
        s.addImage({ path: img('kid_' + n.toLowerCase()), x, y, w: 3.0, h: 1.75, sizing: { type: 'cover', w: 3.0, h: 1.75 } });
        text(s, n, { x, y: y + 1.78, w: 3.0, h: 0.55, align: 'center', bold: true, color: GOLD });
      });
    },
    () => {
      const s = slide('Ngoại hình theo tuổi', { notes: 'Ngoại hình đổi theo tuổi: màu tóc qua mặt nạ tóc (bạc dần khi già), râu, nếp nhăn gắn trên đầu. Tuổi và nhịp già lấy từ look_options.json. Ảnh: CharacterLookPlayModeTests, 8 nhân vật ở 16, 30, 70, 80 tuổi.' });
      [['look_16', '16 tuổi'], ['look_30', '30 tuổi, có râu'], ['look_70', '70 tuổi'], ['look_80', '80 tuổi, tóc bạc']].forEach(([p, c], i) =>
        picture(s, p, 0.45 + (i % 2) * 6.3, 1.95 + Math.floor(i / 2) * 2.5, 6.1, 2.35, c));
    },
    () => {
      const s = slide('Cư dân thị trấn', { notes: '45 cư dân có tên, tuổi, màu da, dáng người riêng; 16 bộ đồng phục theo nghề (bác sĩ, y tá, đầu bếp, bồi bàn, ngân hàng, nhà ga, cà phê, du khách, konbini, nông dân, sushi, nhạc công, DJ, hoạ sĩ, thời trang, cư dân). Đồng phục tô lên quần áo; mũ, khăn, ống nghe là vật thể gắn theo xương.' });
      picture(s, 'people', 0.45, 1.95, 8.2, 4.95, '45 cư dân, 16 đồng phục');
      bullets(s, ['Tên, tuổi, màu da riêng', '16 đồng phục theo nghề', 'Đi làm theo giờ'], 8.95, 2.0, 3.95, 4.9);
    },
    () => {
      const s = slide('Hai chỉ số phát triển', { dark: false, notes: 'Cấp độ (XP) đến từ nhiệm vụ, ca làm, sự kiện; ở cấp 10 mở hệ thống nghề nghiệp. Trí tuệ đến từ bài học, bàn học, bài thi, nhiệm vụ và cốt truyện; trí tuệ mở khu vực mới và là điều kiện phỏng vấn thăng chức.' });
      card(s, 0.45, 2.0, 6.1, 4.85, 'Cấp độ · XP', 'Từ nhiệm vụ, ca làm, sự kiện.\nCấp 10 mở nghề nghiệp.', TEAL);
      card(s, 6.8, 2.0, 6.1, 4.85, 'Trí tuệ', 'Từ bài học, bài thi, cốt truyện.\nMở khu vực, điều kiện thăng chức.', GOLD);
    },
    () => {
      const s = slide('Lộ trình nhân vật: mở thế giới', { notes: 'Phòng trọ (0 trí tuệ, vừa đến Hibari) → Quán sushi (100, sau khi tự mua cơm nắm ở konbini) → Ga tàu (160, sau bữa sushi) → Game Center (240, Kim rủ sau giờ học) → Đảo Xanh (320, chuyến đi tới Midori) → Nghề nghiệp ở cấp 10. Khách chơi mở toàn bộ thị trấn ngay. Nguồn: Core/LocationUnlocks.cs.' });
      const stops = [['0', 'Phòng trọ'], ['100', 'Quán sushi'], ['160', 'Ga tàu'], ['240', 'Game Center'], ['320', 'Đảo Xanh'], ['Cấp 10', 'Nghề nghiệp']];
      s.addShape(pres.shapes.LINE, { x: 1.4, y: 3.65, w: 10.6, h: 0, line: { color: GOLD, width: 4 } });
      stops.forEach(([n, t], i) => {
        const x = 0.45 + i * 2.12;
        text(s, n, { x, y: 2.15, w: 2.0, h: 0.8, align: 'center', bold: true, fontSize: 36, color: i === 5 ? TEAL : GOLD });
        s.addShape(pres.shapes.OVAL, { x: x + 0.72, y: 3.37, w: 0.56, h: 0.56, fill: { color: i === 5 ? TEAL : GOLD }, line: { color: WHITE, width: 1.5 } });
        text(s, t, { x, y: 4.15, w: 2.0, h: 1.1, align: 'center' });
      });
      text(s, 'Số trên: trí tuệ cần có. Khách chơi mở hết ngay.', { x: 0.45, y: 5.75, w: 12.4, h: 0.9, color: MUTED });
    },
    () => {
      const s = slide('Tiếng Nhật N5 → N1: trí tuệ mở trình độ', { dark: false, notes: 'Trí tuệ là điểm học tập: có được khi học bài, làm bài thi, ôn ở bàn học, làm nhiệm vụ và cốt truyện. Đủ mốc thì lên trình độ JLPT tiếp theo và mở chương truyện: N5 từ 0 trí tuệ (chương 1–4: sống tự lập, đi học, hàng xóm, lễ hội), N4 từ 150 (chương 5–6: chọn nghề, Phú Quốc), N3 từ 400 (chương 7: kỳ thi N3), N2 từ 800 (chương 8: thành đàn anh, dẫn dắt bạn mới), N1 từ 1400 (chương 9: hai quê hương). Mốc này cũng là điều kiện phỏng vấn thăng chức. Nguồn: Docs/STORY.md.' });
      text(s, 'Học, thi, làm nhiệm vụ → có trí tuệ. Đủ mốc thì lên trình độ, mở chương mới.', { x: 0.45, y: 1.9, w: 12.4, h: 1.05, color: INK });
      LEVELS.forEach(([lv, k, ch, can], i) => {
        const x = 0.45 + i * 2.5, top = 4.45 - i * 0.35, top1 = i === 4;
        s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y: top, w: 2.38, h: 6.95 - top, rectRadius: 0.1, fill: { color: top1 ? GOLD : [NAVY2, NAVY2, TEAL, TEAL][i] }, line: { color: WHITE, width: 1 } });
        const ink = top1 ? NAVY : WHITE;
        text(s, lv, { x, y: top + 0.08, w: 2.38, h: 0.62, align: 'center', bold: true, fontSize: 36, color: ink });
        text(s, k, { x, y: top + 0.7, w: 2.38, h: 0.5, align: 'center', color: top1 ? NAVY : GOLD });
        text(s, ch, { x, y: top + 1.2, w: 2.38, h: 0.5, align: 'center', color: ink });
        text(s, can, { x: x + 0.08, y: top + 1.72, w: 2.22, h: 0.5, align: 'center', bold: true, color: ink });
      });
    },
    () => {
      const s = slide('Ba kết thúc của câu chuyện', { notes: 'Chương 9 (N1): ở lễ hội mùa hè, Tanaka phát biểu về người chơi: "Lúc đầu còn không biết đường, giờ là người nối Nhật Bản và Việt Nam." Người chơi chọn nơi sống: ở lại Hibari-chō ("Vui quá, ta vẫn là hàng xóm"), về Việt Nam làm cầu nối ("Sẽ nhớ lắm nhưng tự hào lắm"), hay sống giữa hai nước ("Hai quê hương, đúng là bạn"). Lời kết thêm từ Kim, chị Mai, anh Aoki nếu đã thân (độ thân thiết từ 20). Nguồn: Docs/STORY.md chương 9.' });
      [['town', 'Ở lại Hibari-chō', 'Vẫn là hàng xóm', TEAL], ['phuquoc', 'Về Việt Nam', 'Làm cầu nối hai nước', GOLD], ['train', 'Sống giữa hai nước', 'Hai quê hương', RED]].forEach(([p, h, b, c], i) => {
        const x = 0.45 + i * 4.18;
        s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y: 1.95, w: 4.0, h: 4.95, rectRadius: 0.12, fill: { color: NAVY2 }, line: { color: c, width: 2 } });
        s.addImage({ path: img(p), x: x + 0.06, y: 2.01, w: 3.88, h: 2.6, sizing: { type: 'cover', w: 3.88, h: 2.6 } });
        badge(s, x + 0.15, 2.15, 0.8, c);
        text(s, String(i + 1), { x: x + 0.15, y: 2.15, w: 0.8, h: 0.8, align: 'center', valign: 'middle', bold: true, color: WHITE });
        text(s, h, { x: x + 0.15, y: 4.8, w: 3.7, h: 0.7, bold: true, color: c, align: 'center' });
        text(s, b, { x: x + 0.15, y: 5.6, w: 3.7, h: 1.1, color: MUTED, align: 'center' });
      });
    },
    () => {
      const s = slide('Cốt truyện 9 chương', { notes: 'Mỗi chương mở bằng cảnh truyện, có lựa chọn ảnh hưởng cốt truyện; độ thân thiết ≥ 20 tạo gắn bó. Nguồn: Docs/STORY.md.' });
      [['N5', 'Cuộc sống mới'], ['N5', 'Ngày đầu đi học'], ['N5', 'Hàng xóm, sushi'], ['N4', 'Lễ hội mùa hè'], ['N4', 'Chọn nghề'], ['N4', 'Cầu nối Phú Quốc'], ['N3', 'Mùa thi N3'], ['N2', 'Thành đàn anh'], ['N1', 'Hai quê hương']].forEach(([lv, t], i) => {
        const x = 0.45 + (i % 3) * 4.18, y = 1.95 + Math.floor(i / 3) * 1.68;
        s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w: 4.0, h: 1.5, rectRadius: 0.12, fill: { color: i === 8 ? GOLD : NAVY2 }, line: { color: i === 8 ? GOLD : NAVY2 } });
        text(s, `${i + 1} · ${lv}`, { x: x + 0.22, y: y + 0.12, w: 3.6, h: 0.6, bold: true, fontSize: 32, color: i === 8 ? NAVY : GOLD });
        text(s, t, { x: x + 0.22, y: y + 0.74, w: 3.6, h: 0.65, color: i === 8 ? NAVY : WHITE });
      });
    },
  ]],
  ['Nghề nghiệp và kinh tế', [
    () => {
      const s = slide('10 ngành nghề · 6 bậc', { notes: 'Mở ở cấp 10 cho khách lẫn tài khoản. Mỗi lần lên bậc là một buổi phỏng vấn tiếng Nhật; bậc 4 chọn nhánh; 35% có sự kiện nơi làm sau ca. Làm trái ngành chỉ được 50% tiền và kinh nghiệm. Icon theo bộ biểu tượng của game, màu theo nhóm ngành.' });
      CAREERS.forEach((c, i) => {
        const x = 0.45 + (i % 5) * 2.5, y = 1.95 + Math.floor(i / 5) * 2.5;
        s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w: 2.35, h: 2.35, rectRadius: 0.12, fill: { color: NAVY2 }, line: { color: colorOf(c.id), width: 1.5 } });
        badge(s, x + 0.7, y + 0.15, 0.95, colorOf(c.id), 'icon_' + c.id);
        text(s, SHORT_NAME[c.id], { x: x + 0.1, y: y + 1.2, w: 2.15, h: 1.05, align: 'center', valign: 'middle' });
      });
    },
    ...CAREERS.map(c => () => careerSlide(c)),
    () => features({ title: 'Truyện mốc trước mỗi lần thăng chức', notes: '70 cảnh truyện ngắn (10 ngành × bậc 2, 3, 4 và bậc 5, 6 của mỗi nhánh), dữ liệu career_stories.json sinh từ Tools/story/career_stories.py. Khi thanh hiệu suất đầy, một cảnh ở nơi làm việc diễn ra trước buổi phỏng vấn: sếp, đồng nghiệp hoặc khách nói bằng tiếng Nhật, người chơi chọn 1 trong 3 câu trả lời. Câu tốt nhất (lịch sự, chu đáo) được +5 trí tuệ và danh tiếng rồi vào phỏng vấn; câu tạm được thêm chút danh tiếng; câu kém mất 20% hiệu suất nên phỏng vấn lùi lại ca sau. Mỗi bậc giờ chỉ cần 1–3 ca. Câu trả lời của ngành y theo đúng thực tế: không kê kháng sinh cho cảm, đo huyết áp sau khi nghỉ, sổ thuốc khi uống chung thuốc.', items: [
      { pic: 'f_cs_question', head: 'Tình huống ở nơi làm', body: 'Khách, sếp, đồng nghiệp nói tiếng Nhật' },
      { pic: 'f_cs_reply', head: '3 câu trả lời', body: 'Câu tốt nhất được +5 trí tuệ' },
      { pic: 'f_cs_interview', head: 'Rồi mới phỏng vấn', body: 'Trả lời kém: phỏng vấn lùi lại ca sau' },
    ] }),
    () => {
      const s = slide('Video: con đường sự nghiệp', { notes: 'Mười ngành, mỗi ngành từ bậc 1 đến bậc 6, ghi trực tiếp trong game (DemoRecordingTests.Careers_RecordFrames): ca làm thật, phỏng vấn thăng chức tiếng Nhật, chọn nhánh ở bậc 4. Lời dẫn giọng nam. Bản đầy đủ: Bao_Cao/NihongoLife_Con_Duong_Su_Nghiep.mp4.' });
      video(s, 'careers.mp4', 'career_interview', 2.2, 1.85, 8.9, 5.0, '10 ngành · bậc 1 đến bậc 6');
    },
    () => {
      const s = slide('Tiền, ngân hàng và công ty', { notes: 'Ví chung; ngân hàng có quỹ khẩn cấp không lãi, khoản vay, gia hạn; trễ hạn không tịch thu đồ, chỉ giữ tối đa một phần thu nhập. Công ty: chi phí và lương chốt mỗi 7 ngày game, doanh thu của nhân viên nằm trong công ty đến khi chủ rút. Nguồn: bank.json, business.json.' });
      picture(s, 'bank', 0.45, 1.95, 6.1, 4.95, 'Ngân hàng Hibari');
      bullets(s, ['Nhiệm vụ trả tiền và XP', 'Quỹ khẩn cấp không lãi', 'Công ty chốt chi phí mỗi tuần', 'Nhân viên làm ra tiền cho chủ'], 6.9, 2.0, 6.0, 4.9);
    },
  ]],
  ['Sức khoẻ và y khoa', [
    () => illnessGrid(ILLNESSES.illnesses.slice(0, 8), 1),
    () => illnessGrid(ILLNESSES.illnesses.slice(8, 16), 2),
    () => {
      const s = slide('7 loại thuốc trong game', { dark: false, notes: ILLNESSES.medicines.map(m => `${m.vi} (${m.ja})`).join(', ') + '. Mua ở kệ thuốc Hibari Mart hoặc nhận ở phòng khám. Mô hình 3D và icon dựng bằng MedicineModelBuilder.' });
      picture(s, 'medicine_3d', 0.45, 1.95, 6.1, 4.95, 'Mô hình 3D');
      bullets(s, ILLNESSES.medicines.map(m => m.vi), 6.85, 1.95, 6.05, 4.95);
    },
    () => {
      const s = slide('Điều trị đúng y khoa', { notes: 'Nguồn (Docs/HEALTH_SOURCES.md): AMR Clinical Reference Center (NCGM) về kháng sinh và cảm; tờ hướng dẫn cúm của thành phố Takamatsu (thuốc kháng virus trong 48 giờ); Bộ Y tế, Lao động và Phúc lợi Nhật (MHLW) về say nắng; Hiệp hội Da liễu Nhật (rôm sảy); Hiệp hội Nha khoa Nhật (sâu răng); quy định nghỉ học khi cúm. Game không thay lời khuyên y tế.' });
      bullets(s, ['Thuốc không kê đơn chỉ làm dịu', 'Kháng sinh không trị cảm, cúm', 'Thuốc cúm tốt nhất trong 48 giờ', 'Viêm họng liên cầu: đủ 10 ngày kháng sinh', 'Sâu răng: phải đi nha sĩ'], 0.6, 2.0, 6.6, 4.9);
      picture(s, 'clinic_med', 7.45, 1.95, 5.45, 4.95, 'Nhận thuốc ở phòng khám');
    },
    () => {
      const s = slide('Nguồn nghiên cứu', { notes: 'Toàn bộ bảng nguồn: Docs/HEALTH_SOURCES.md (kiểm tra ngày 27/10/2026). Thời gian trong game được rút ngắn (1 giờ game = 1 phút), quy tắc điều trị giữ nguyên.' });
      [['NCGM · AMR', 'Kháng sinh, cảm lạnh'], ['MHLW', 'Say nắng, sơ cứu'], ['Takamatsu', 'Cúm, 48 giờ'], ['Da liễu Nhật', 'Rôm sảy'], ['Nha khoa Nhật', 'Sâu răng'], ['Vanderbilt', 'Đau mỏi cơ']]
        .forEach(([h, b], i) => card(s, 0.45 + (i % 3) * 4.18, 2.0 + Math.floor(i / 3) * 2.5, 4.0, 2.3, h, b, [TEAL, GOLD, RED, '4C6FD6', '8FA3BF', TEAL][i]));
    },
  ]],
  ['Học tập', [
    () => {
      const s = slide('Học tập: con số chính', { dark: false, notes: '40 bài cho mỗi ngôn ngữ (Việt, Anh, Nhật, Nga) với bảng chữ cái, ngữ pháp, chứng chỉ N5–N1 / A1–C1 và bài kiểm tra trình độ; 181 bài video theo khoá; lộ trình tiếng Nga 53 bước; 86 nhiệm vụ.' });
      stat(s, 0.45, 2.0, 2.95, '4', 'ngôn ngữ', RED);
      stat(s, 3.6, 2.0, 2.95, '160', 'bài học', TEAL);
      stat(s, 6.75, 2.0, 2.95, '181', 'video bài giảng', GOLD);
      stat(s, 9.9, 2.0, 2.95, '53', 'bước tiếng Nga', '4C6FD6');
      text(s, 'Mỗi ngôn ngữ: bài học, bảng chữ, ngữ pháp, chứng chỉ, kiểm tra trình độ.', { x: 0.45, y: 4.6, w: 12.4, h: 1.4, color: NAVY });
    },
    () => {
      const s = slide('Lớp học: chọn ngôn ngữ, mọi thứ một chỗ', { notes: 'Một nút "Lớp học" (phím `) và bàn đầu lớp ở trường Hibari mở cùng một cửa sổ: chọn Nhật / Anh / Nga / Việt rồi chỉ thấy bài học, video và thi thử của ngôn ngữ đó. K vẫn mở thẳng phòng thi.' });
      picture(s, 'classroom_ru', 0.45, 1.95, 7.6, 4.95, 'Lớp học, chọn tiếng Nga');
      bullets(s, ['Một nút, bốn ngôn ngữ', 'Bài học, video, thi thử', 'Video gắn đúng từng bài'], 8.35, 2.0, 4.55, 4.9);
    },
    ...STUDY.map(f => () => features(f)),
  ]],
  ['Online và kỹ thuật', [
    () => {
      const s = slide('Chơi online cùng người thật', { notes: 'Chỉ tài khoản đã đăng ký (server kiểm tra is_registered). Gặp nhau trong thị trấn, biểu cảm, nhắn tin, bạn bè, nhóm. Voice khu vực tự bật khi vào thế giới online, giữ T để nói, biểu tượng sóng âm trên đầu người đang nói. Gọi riêng và phòng luyện nói với giáo viên qua Agora.' });
      picture(s, 'chat', 0.45, 1.95, 6.1, 4.95, 'Chat, bạn bè, nhóm');
      picture(s, 'player_card', 6.8, 1.95, 6.1, 4.95, 'Gặp người chơi khác');
    },
    () => {
      const s = slide('Kiểm thử online trên Supabase thật', { dark: false, notes: 'Ngày 08/11/2026, Tools/online_playtest/online_test.py: 6 tài khoản, 5 người kết nối cùng lúc, chat trễ trung bình 78 ms, tối đa 93 ms; toàn bộ luồng gọi giáo viên đạt. Kiểm thử tải trước đó: 20 người 97,9% tin đến, 40 người 96,8%.' });
      stat(s, 0.45, 2.0, 2.95, '5', 'người cùng lúc', TEAL);
      stat(s, 3.6, 2.0, 2.95, '78 ms', 'trễ chat TB', GOLD);
      stat(s, 6.75, 2.0, 2.95, '100%', 'luồng gọi đạt', RED);
      stat(s, 9.9, 2.0, 2.95, '40', 'người tải thử', '4C6FD6');
      text(s, 'Chỉ tài khoản đã đăng ký được chơi online.', { x: 0.45, y: 4.6, w: 12.4, h: 1.0, color: NAVY });
    },
    () => {
      const s = slide('Nền tảng và công nghệ', { notes: 'Unity 6 URP; WebGL (mỗi khu là một gói tải theo nhu cầu); Android; Supabase (tài khoản, realtime, Edge Functions); Agora RTC (voice, video); Gemini (tuỳ chọn chấm IELTS); OpenStreetMap (bản đồ Phú Quốc); GitHub; YouTube.' });
      [['Unity 6', GOLD], ['WebGL', TEAL], ['Android', '6CBF5A'], ['Supabase', '3ECF8E'], ['Agora', '4C9BFF'], ['Gemini', 'B48CFF'], ['OpenStreetMap', RED], ['GitHub', MUTED], ['YouTube', 'FF4E45']]
        .forEach(([t, c], i) => card(s, 0.45 + (i % 3) * 4.18, 2.0 + Math.floor(i / 3) * 1.65, 4.0, 1.45, t, '', c));
    },
    () => {
      const s = slide('Kết quả kỹ thuật', { dark: false, notes: 'Lượt chạy toàn phần 08/11/2026: EditMode 339/339 đạt; Play Mode 240 ca, 225 đạt, 0 lỗi, 12 ca quay video/chụp ảnh chỉ chạy khi gọi riêng, 3 ca không kết luận được vì phụ thuộc bên ngoài. Bản web 101 MB, 58–60 khung hình mỗi giây trên trình duyệt.' });
      stat(s, 0.45, 2.0, 4.0, '339', 'test EditMode', TEAL);
      stat(s, 4.65, 2.0, 4.0, '225', 'test Play Mode đạt', GOLD);
      stat(s, 8.85, 2.0, 4.0, '0', 'lỗi', RED);
      picture(s, 'web_pc', 0.45, 4.5, 12.45, 2.4, 'Bản web 101 MB, 60 fps');
    },
    () => {
      const s = slide('Máy tính, web và điện thoại', { notes: 'Một bản build: máy tính, trình duyệt, điện thoại (xoay ngang). Chế độ điều khiển Tự động / Bật / Tắt chọn từ menu hoặc Cài đặt.' });
      picture(s, 'web_pc', 0.45, 1.95, 7.4, 4.95, 'Máy tính và web');
      picture(s, 'web_phone', 8.1, 1.95, 4.8, 2.35, 'Điện thoại');
      picture(s, 'web_phone_menu', 8.1, 4.55, 4.8, 2.35, 'Menu điện thoại');
    },
  ]],
  ['Người thực hiện và phát triển', [
    () => {
      const s = slide('Người thực hiện', { notes: 'Quách Thành Long (K24GD03) xây dựng toàn bộ game: thiết kế, lập trình Unity, nội dung 4 ngôn ngữ, web, online, kiểm thử và báo cáo.' });
      s.addImage({ path: img('team_long'), x: 0.9, y: 2.0, w: 3.6, h: 3.6 });
      text(s, 'Quách Thành Long', { x: 0.45, y: 5.8, w: 4.5, h: 0.7, align: 'center', bold: true, fontSize: 34, color: GOLD });
      text(s, 'Xây dựng toàn bộ game', { x: 5.3, y: 2.0, w: 7.6, h: 0.7, bold: true });
      bullets(s, ['Thiết kế và lập trình Unity', 'Nội dung 4 ngôn ngữ', 'Web, online, kiểm thử', 'Báo cáo và truyền thông'], 5.3, 2.9, 7.6, 3.9);
    },
    () => {
      const s = slide('Đội ngũ The Next Generation', { dark: false, notes: 'Thành viên nhóm đồng hành cùng dự án, không trực tiếp xây dựng game: Nguyễn Lê Gia Huy, Lương Nguyễn Tấn Dũng (đồng sáng lập), Nguyễn Ngọc Minh Châu (marketing), Đăng (content).' });
      [['team_huy', 'Gia Huy'], ['team_dung', 'Tấn Dũng'], ['team_chau', 'Minh Châu'], ['team_dang', 'Đăng']].forEach(([p, n], i) => {
        const x = 0.45 + i * 3.15;
        s.addImage({ path: img(p), x: x + 0.4, y: 2.0, w: 2.2, h: 2.2 });
        text(s, n, { x, y: 4.35, w: 3.0, h: 0.6, align: 'center', bold: true, color: NAVY });
      });
      text(s, 'Đồng hành cùng dự án, không trực tiếp xây dựng game.', { x: 0.45, y: 5.5, w: 12.4, h: 1.0, color: INK, align: 'center' });
    },
    () => {
      const s = slide('Giải thưởng và kênh YouTube', { notes: 'Giải Triển vọng – Unique Project 2025, VTC Academy. Kênh youtube.com/@NihongoLifeGame: tiếng Nga 64 video, tiếng Anh 34 video, 3 video quảng bá.' });
      picture(s, 'award', 0.45, 1.95, 4.95, 4.95, 'Giải Triển vọng 2025');
      picture(s, 'youtube', 5.6, 1.95, 7.3, 3.4, '@NihongoLifeGame');
      text(s, '64 video tiếng Nga · 34 video tiếng Anh', { x: 5.6, y: 5.55, w: 7.3, h: 1.2, color: GOLD });
    },
    () => {
      const s = slide('Hướng mở rộng', { notes: 'Kịch bản N4–N1 có giọng đọc thật; phố Dương Đông chi tiết hơn; kiểm thử lớp học với giáo viên thật; phát hành web, itch.io, Google Play. Ưu tiên: người bản ngữ duyệt nội dung tiếng Nga và đề tự soạn.' });
      bullets(s, ['Kịch bản N4–N1, giọng đọc thật', 'Phố Dương Đông chi tiết hơn', 'Lớp học với giáo viên thật', 'Web, itch.io, Google Play'], 0.6, 2.0, 6.0, 4.9);
      picture(s, 'promo_phuquoc', 6.8, 1.95, 6.1, 4.95, 'Phú Quốc, khu vực mới');
    },
    () => {
      const s = slide('Lộ trình truyền thông', { dark: false, notes: '26/10/2026 mở đợt 12 tuần; 17/01/2027 kết thúc đợt; 01/2027–01/2028 kế hoạch năm.' });
      s.addShape(pres.shapes.LINE, { x: 2.3, y: 3.4, w: 8.7, h: 0, line: { color: GOLD, width: 4 } });
      [['26/10/2026', 'Mở đợt 12 tuần'], ['17/01/2027', 'Kết thúc đợt'], ['2027–2028', 'Kế hoạch năm']].forEach(([d, t], i) => {
        const x = 2.3 + i * 4.35;
        s.addShape(pres.shapes.OVAL, { x: x - 0.25, y: 3.15, w: 0.5, h: 0.5, fill: { color: GOLD }, line: { color: GOLD } });
        text(s, d, { x: x - 1.6, y: 3.95, w: 3.6, h: 0.65, align: 'center', bold: true, color: NAVY, fontSize: 34 });
        text(s, t, { x: x - 1.6, y: 4.7, w: 3.6, h: 0.65, align: 'center', color: INK });
      });
    },
    () => {
      const s = slide('', { number: false, notes: 'Liên hệ: trang web https://webservice.gzv.one/store/demo/nihongolife-edu-game-theme · YouTube https://www.youtube.com/channel/UCAKp-_EsasxPY2oa-4CzWtQ · github.com/StephenSouth13/NihongoLife' });
      s.addImage({ path: img('midori'), x: 6.6, y: 0, w: W - 6.6, h: 7.5, sizing: { type: 'cover', w: W - 6.6, h: 7.5 } });
      text(s, 'Cảm ơn thầy cô\nvà các bạn đã lắng nghe!', { x: 0.6, y: 1.6, w: 5.8, h: 2.2, fontSize: 44, bold: true, color: GOLD });
      text(s, 'Rất mong nhận được góp ý', { x: 0.6, y: 4.0, w: 5.8, h: 0.6, color: MUTED });
      text(s, 'YouTube @NihongoLifeGame', { x: 0.6, y: 5.3, w: 5.8, h: 0.6, hyperlink: { url: 'https://www.youtube.com/channel/UCAKp-_EsasxPY2oa-4CzWtQ' } });
      text(s, 'Trang web NihongoLife', { x: 0.6, y: 5.95, w: 5.8, h: 0.6, hyperlink: { url: 'https://webservice.gzv.one/store/demo/nihongolife-edu-game-theme' } });
      text(s, 'GitHub: NihongoLife', { x: 0.6, y: 6.6, w: 5.8, h: 0.6, hyperlink: { url: 'https://github.com/StephenSouth13/NihongoLife' } });
      schoolLogo(s);
    },
  ]],
];

// Table of contents: each section with the slide it starts on (every builder makes exactly one slide).
const counts = SECTIONS.map(([, list]) => list.length);
const starts = counts.map((_, i) => 1 + counts.slice(0, i).reduce((a, b) => a + b, 0));
function toc() {
  const s = slide('Mục lục', { dark: false, notes: 'Các phần của bài trình bày, kèm số trang bắt đầu.' });
  SECTIONS.forEach(([title], i) => {
    const rows = Math.ceil(SECTIONS.length / 2);
    const x = 0.45 + Math.floor(i / rows) * 6.3, y = 1.95 + (i % rows) * (4.95 / rows);
    s.addShape(pres.shapes.OVAL, { x, y: y + 0.1, w: 0.75, h: 0.75, fill: { color: i % 2 ? TEAL : GOLD }, line: { color: WHITE } });
    text(s, String(i + 1), { x, y: y + 0.1, w: 0.75, h: 0.75, align: 'center', valign: 'middle', bold: true, color: WHITE });
    text(s, title, { x: x + 0.95, y, w: 4.3, h: 0.95, valign: 'middle', color: NAVY, bold: true });
    text(s, String(starts[i]), { x: x + 5.1, y, w: 0.9, h: 0.95, valign: 'middle', align: 'right', color: '8A8070' });
  });
}

for (const [title, list] of SECTIONS) {
  section = title;
  pres.addSection({ title });
  for (const build of list) build === 'TOC' ? toc() : build();
}

// Folder entries in the zip break opening through cloud services: the deck is rewritten with its files only, in order.
pres.writeFile({ fileName: OUT }).then(async f => {
  const JSZip = require('jszip');
  const source = await JSZip.loadAsync(fs.readFileSync(OUT));
  const clean = new JSZip();
  for (const name of Object.keys(source.files)) if (!source.files[name].dir) clean.file(name, await source.files[name].async('nodebuffer'), { createFolders: false });
  fs.writeFileSync(OUT, await clean.generateAsync({ type: 'nodebuffer', compression: 'DEFLATE' }));
  console.log('wrote', f, pres.slides.length, 'slides; sections start at', starts.join(', '));
});
