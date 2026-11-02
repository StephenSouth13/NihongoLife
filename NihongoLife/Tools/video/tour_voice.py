#!/usr/bin/env python3
"""Narration of the full product tour (DemoRecordingTests.Tour_RecordFrames): Vietnamese lines, one per beat, read by
Microsoft's neural voice through edge-tts, and the durations the recording holds each beat for.
Usage (from any folder):  python tour_voice.py <out_dir>
Writes <out_dir>/<beat>.mp3, lines.tsv (beat, text) and durations.tsv (beat, seconds).
"""
import asyncio
import re
import subprocess
import sys
from pathlib import Path

import edge_tts

VOICE = 'vi-VN-HoaiMyNeural'
# The service refuses rate and pitch for this voice: a touch slower and a little higher is done on the audio.
WARM = 'asetrate=24000*1.03,aresample=24000,atempo=0.92,highpass=f=90,equalizer=f=3000:t=q:w=1.2:g=2'

# Short sentences and commas: the pauses are what keeps a synthetic voice from sounding like a list being read out.
LINES = {
    'intro': 'Chào bạn nha! Hôm nay mình dắt bạn đi một vòng NihongoLife, một thị trấn nhỏ ở Nhật, nơi bạn học tiếng bằng chính cuộc sống mỗi ngày.',
    'menu': 'Đây là màn hình đầu tiên. Game có bốn ngôn ngữ, tiếng Việt, tiếng Anh, tiếng Nhật và tiếng Nga. Bạn đổi một cái là mọi chữ, cả đơn vị tiền, đổi theo liền.',
    'mode': 'Chơi bằng máy tính hay điện thoại, bạn tự chọn ở nút này nhé. Điện thoại thì có cần điều khiển và các nút to, dễ bấm.',
    'town': 'Và đây là thị trấn Hibari! Có đường phố, xe cộ, cửa hàng, và cả những người hàng xóm dễ thương. Mọi thứ đều nói tiếng Nhật với bạn.',
    'dock': 'Các phím tắt được gom gọn vào một nút thôi. Bấm một cái là thấy balo, bản đồ, nhiệm vụ, sổ tay. Bấm lại là gọn gàng ngay.',
    'toggles': 'Màn hình chật quá hả? Bạn cất thẻ nhân vật hay thanh nhiệm vụ đi, lúc nào cần thì mở lại. Nhẹ nhàng lắm.',
    'map': 'Lạc đường thì mở bản đồ lên nha. Chỗ nào cũng có ghi chú rõ ràng, chọn một nơi là có đường chỉ tới.',
    'konbini': 'Nào, mình đi làm thêm thôi! Đây là anh Ito ở cửa hàng tiện lợi. Xếp hàng lên kệ, chỉ đường cho khách, tính tiền, tất cả đều bằng tiếng Nhật.',
    'wage': 'Xong ca là anh Ito trả lương liền. Có tiền, có kinh nghiệm, và thêm một chút trí tuệ nữa.',
    'fashion': 'Có tiền rồi thì đi mua sắm nào! Đây là Hibari Mode, cửa hàng quần áo mới toanh của thị trấn, với mười tám bộ đồ hoàn chỉnh.',
    'tryon': 'Thích bộ nào thì bấm mặc thử, nhân vật mặc liền cho bạn xem. Mũ, kính, khăn, đủ cả. Không ưng thì thôi, không mất đồng nào.',
    'buy': 'Ưng rồi thì mua thôi. Đồ đã mua nằm trong tủ đồ của bạn, đi đâu cũng thay được, và bạn bè online cũng nhìn thấy nữa.',
    'fashionjob': 'Ở đây bạn còn xin làm được luôn. Gấp đồ, tư vấn cỡ áo, dẫn khách vào phòng thử. Đây là một ngành nghề hoàn toàn mới trong game.',
    'heat': 'Trời nắng gắt quá, bạn ấy bị say nắng rồi! Thấy không, bạn ấy quạt tay, người chao đảo, màn hình cũng nóng lên. Mỗi bệnh trong game đều hiện ra trên cơ thể như vậy.',
    'handbook': 'Trong sổ tay có đủ mười sáu bệnh thường gặp, nguyên nhân, cách phòng, cách chữa, và đều viết theo y khoa thật.',
    'clinic': 'Mệt thì đến phòng khám thôi. Bạn đăng ký ở quầy, điền phiếu hỏi bệnh, rồi kể cho bác sĩ nghe bằng tiếng Nhật.',
    'medicine': 'Khám xong thì trả tiền, nhận thuốc, và nghe dặn cách uống. Từ vựng này ra Nhật là dùng được ngay đó.',
    'sushi': 'Đói bụng rồi, mình ghé quán sushi nha. Gọi món xong là phục vụ bưng tận bàn.',
    'eat': 'Bạn ấy kéo ghế vào bàn, cầm đũa ăn thật ngon, xong thì ra quầy thanh toán. Lối vào và lối ra có hàng rào riêng, nên không ai bị lạc cả.',
    'school': 'Đây là trường Nhật ngữ Hibari. Có lớp học, có thầy cô, và cả phòng luyện nói với giáo viên thật.',
    'courses': 'Thư viện video được làm lại như một trang học trực tuyến. Hơn một trăm năm mươi bài theo khoá, tiếng Nga, tiếng Anh, tiếng Nhật, và game tự nhớ bạn đã học tới đâu.',
    'exam': 'Muốn thử sức thì có phòng thi JLPT và IELTS, chấm điểm rõ ràng, có cả phần nói và viết.',
    'arcade': 'Học xong rồi, mình đi chơi nha. Game Center có cả chục máy, mỗi máy một trò, và trò nào cũng giúp bạn nhớ chữ.',
    'hoop': 'Trò ném bóng rổ này nè, bạn vuốt để ném, canh lực cho khéo. Nghe vui tai lắm luôn.',
    'ufo': 'Còn đây là máy gắp thú. Bạn tự lái càng, thả xuống, và cầu mong nó đừng tuột. Hồi hộp y như ngoài đời vậy đó.',
    'music': 'Thích âm nhạc thì ghé tiệm Hibiki. Nhân vật tự chơi đàn cho bạn nghe, bạn chỉnh nhịp và âm lượng thôi.',
    'gallery': 'Đi vào trong là phòng tranh, treo những kiệt tác thật. Đứng trước bức nào, bấm một cái là đọc được câu chuyện của bức đó.',
    'island': 'Đi tàu một đoạn là tới Đảo Xanh. Ở đây có nông trại, có biển, không khí thoải mái hẳn.',
    'fishing': 'Mình thử câu cá nha. Đảo có mười sáu loài, từ cá nhỏ tới cá mập, câu càng giỏi càng gặp cá hiếm. Trúng con to rồi nè!',
    'swim': 'Và giờ thì nhảy xuống biển được rồi nhé. Nhân vật bơi thật, nổi trên mặt nước, bơi vào bờ là tự leo lên.',
    'phuquoc': 'Bay xa hơn nữa, chúng mình có đảo Phú Quốc, dựng theo bản đồ thật. Có chợ hải sản, có khu nghỉ dưỡng, và quán Gánh Ngon để bạn làm nghề khách sạn.',
    'career': 'Từ cấp mười bạn chọn ngành của mình. Làm đúng ngành thì lên chức, làm trái ngành thì chỉ được một nửa lương và kinh nghiệm thôi, nên chọn cho kỹ nha.',
    'bank': 'Ngoài ra còn có ngân hàng, có khoản vay, mở công ty thì mỗi tuần phải trả chi phí, còn nhân viên thì làm ra tiền cho bạn.',
    'online': 'Đăng nhập là gặp nhau ngay trong thị trấn, thả biểu cảm, nhắn tin, gọi thoại, và luyện nói cùng nhau.',
    'outro': 'Vậy là mình đã đi hết một vòng rồi đó. Cảm ơn bạn đã xem cùng mình. Hẹn gặp bạn ở thị trấn Hibari nha!',
}


async def clause(text, path):
    """One request; a text the service refuses comes back split at its commas, each part with its own request."""
    body = text.rstrip('.!? ')
    for wording in (text, body + ' nha.', 'Và ' + body[0].lower() + body[1:] + '.', 'Rồi ' + body[0].lower() + body[1:] + '.'):
        try:
            await edge_tts.Communicate(wording, VOICE).save(str(path))
            return [path]
        except edge_tts.exceptions.NoAudioReceived:
            await asyncio.sleep(1)
    parts = [p.strip() for p in text.replace('!', ',').replace('.', ',').split(',') if p.strip()]
    if len(parts) < 2:
        raise RuntimeError('no audio for: ' + text)
    files = []
    for i, part in enumerate(parts):
        files += await clause(part + '.', path.with_name(f'{path.stem}_{i}.mp3'))
    return files


async def speak(text, path):
    sentences = [t.strip() + m for t, m in re.findall(r'([^.!?]+)([.!?]+)', text)]
    files = []
    for i, sentence in enumerate(sentences):
        files += await clause(sentence, path.with_name(f'{path.stem}.part{i}.mp3'))
    names = path.with_suffix('.txt')
    names.write_text(''.join(f"file '{f.name}'\n" for f in files), encoding='utf-8')
    subprocess.run(['ffmpeg', '-y', '-v', 'error', '-f', 'concat', '-safe', '0', '-i', names.name, '-af', WARM, '-b:a', '128k', path.name], check=True, cwd=path.parent)
    names.unlink()
    for f in files:
        f.unlink()


def length(path):
    return float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', str(path)]).decode().strip())


async def main():
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    rows = []
    for beat, text in LINES.items():
        path = out / f'{beat}.mp3'
        if not path.exists():
            try:
                await speak(text, path)
            except RuntimeError as refused:
                print(beat, refused)
                continue
        rows.append((beat, text, length(path)))
    (out / 'lines.tsv').write_text(''.join(f'{b}\t{t}\n' for b, t, _ in rows), encoding='utf-8')
    (out / 'durations.tsv').write_text(''.join(f'{b}\t{s:.2f}\n' for b, _, s in rows), encoding='utf-8')
    total = sum(s for _, _, s in rows)
    print(f'{len(rows)} lines, {total / 60:.1f} minutes of voice')


if __name__ == '__main__':
    asyncio.run(main())
