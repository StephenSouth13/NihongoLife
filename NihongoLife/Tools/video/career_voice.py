#!/usr/bin/env python3
"""Narration of the career video (DemoRecordingTests.Careers_RecordFrames): every career from its first job to its top
title, read by a male neural voice (NamMinh). Titles come from Resources/Progression/careers.json, so the voice says
what the game shows. Usage (from the Unity project folder):  python Tools/video/career_voice.py <out_dir>
Writes <out_dir>/<beat>.mp3, lines.tsv and durations.tsv (beat, seconds).
"""
import asyncio
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import tour_voice  # noqa: E402
from tour_voice import length, speak  # noqa: E402

# A man's voice for the career video (the owner's choice): NamMinh, no pitch shift, a touch slower, warm mids.
tour_voice.VOICE = 'vi-VN-NamMinhNeural'
tour_voice.WARM = 'atempo=0.95,highpass=f=70,equalizer=f=180:t=q:w=1:g=2,equalizer=f=3200:t=q:w=1.2:g=1.5'

# The branch the video follows at level 4; DemoRecordingTests.Careers.cs picks the same ones.
BRANCH = {'retail': 'store', 'food': 'owner', 'farm': 'grower', 'hospitality': 'manager', 'medical': 'doctor',
          'chef': 'sushi', 'musician': 'pianist', 'producer': 'producer', 'painter': 'fine_art', 'fashion': 'designer'}

# How each career starts, in the words a friend would use.
START = {
    'retail': 'Đầu tiên là ngành bán lẻ ở Hibari Mart. Ngày đầu, bạn là {first}, anh Ito dạy từng câu chào khách.',
    'food': 'Tiếp theo là quán Sushi Hibari. Bạn vào làm từ chân {first}, rồi học cách mời khách thật lễ phép.',
    'farm': 'Lên đảo Midori với cô Hana nào. Người mới là {first}, tưới rau, nhặt trứng, quen dần với đất.',
    'hospitality': 'Ở Phú Quốc, quán Gánh Ngon đang cần người. Bạn bắt đầu là {first}, chị Mai kèm từng bàn.',
    'medical': 'Ngành y bắt đầu ở quầy lễ tân phòng khám Hibari. Là {first}, bạn nhận hồ sơ, gọi số, hỏi han bệnh nhân.',
    'chef': 'Vào bếp với bác Ota nha. {First} thì rửa rau, thái cá, nhìn đầu bếp làm và học từng động tác.',
    'musician': 'Tiệm nhạc Hibiki có một sân khấu nhỏ. Bạn lên đó với vai {first}, chơi vài bài cho khách nghe.',
    'producer': 'Ở bàn DJ, DJ Sōta nhận bạn làm {first}. Cắm dây, chỉnh loa, rồi được mở màn vài bản nhạc.',
    'painter': 'Góc giá vẽ của chú Hibiki dành cho người mê hội hoạ. Bạn khởi đầu là {first}, pha màu, căng toan.',
    'fashion': 'Cuối cùng là Hibari Mode. Người mới là {first}, gấp đồ, xếp kệ, rồi được anh Ren cho ra tư vấn khách.',
}
UP = [
    'Làm tốt thì {boss} gọi phỏng vấn thăng chức, toàn bằng tiếng Nhật. Tới bậc bốn, bạn chọn {a} hay {b}, ở đây mình chọn {chosen}.',
    'Mỗi lần lên chức là một buổi phỏng vấn tiếng Nhật với {boss}. Đến bậc bốn thì rẽ hướng, {a} hoặc {b}, mình đi theo {chosen}.',
    'Cứ đủ hiệu suất là được gọi phỏng vấn. Trả lời đúng bằng tiếng Nhật là lên bậc. Ở bậc bốn, mình chọn hướng {chosen}.',
]
TOP = [
    'Và đây là đỉnh cao của nghề: {top}. Từ {first} đến {top}, đi từng bậc một.',
    'Sáu bậc sau, bạn đã là {top}. Nhìn lại, chặng đường bắt đầu chỉ từ một {first}.',
    'Đỉnh cao rồi đó: {top}. Lương cao nhất ngành, và vốn tiếng Nhật cũng lên theo.',
]


def lower_first(text):
    """Spoken form of a title: notes in brackets dropped, '·' read as 'kiêm', an acronym (DJ) keeps its capitals."""
    text = re.sub(r'\s*\([^)]*\)', '', text).replace(' · ', ' kiêm ').strip()
    return text if text[:2].isupper() else text[0].lower() + text[1:]


def lines(careers):
    out = {'intro': 'Trong NihongoLife có mười ngành nghề, mỗi ngành sáu bậc. Mình cùng xem một người mới đi từ những ngày đầu tới đỉnh cao của từng ngành nha.'}
    for i, c in enumerate(careers):
        branches = {b['id']: b for b in c['branches']}
        chosen = branches[BRANCH[c['id']]]
        first = c['levels'][0]['titleVi']
        top = chosen['levels'][-1]['titleVi']
        names = [b['nameVi'] for b in c['branches']]
        spoken = lower_first(first)
        out[c['id']] = START[c['id']].format(first=spoken, First=spoken[0].upper() + spoken[1:])
        out[c['id'] + '_up'] = UP[i % len(UP)].format(boss=lower_first(c['bossNameVi']), a=lower_first(names[0]), b=lower_first(names[1]), chosen=lower_first(chosen['nameVi']))
        out[c['id'] + '_top'] = TOP[i % len(TOP)].format(top=lower_first(top), first=lower_first(first))
    out['outro'] = 'Mười ngành, mười con đường, và con đường nào cũng đi bằng tiếng Nhật. Bạn sẽ chọn nghề nào? Hẹn gặp bạn ở Hibari nha!'
    return out


async def main():
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    data = json.load(open(Path('Assets/NihongoLife/Resources/Progression/careers.json'), encoding='utf-8'))
    rows = []
    for beat, text in lines(data['careers']).items():
        path = out / f'{beat}.mp3'
        if not path.exists():
            await speak(text, path)
        rows.append((beat, text, length(path)))
    (out / 'lines.tsv').write_text(''.join(f'{b}\t{t}\n' for b, t, _ in rows), encoding='utf-8')
    (out / 'durations.tsv').write_text(''.join(f'{b}\t{s:.2f}\n' for b, _, s in rows), encoding='utf-8')
    print(f'{len(rows)} lines, {sum(s for _, _, s in rows) / 60:.1f} minutes of voice')


if __name__ == '__main__':
    assert set(BRANCH) == set(START), 'every career has its start line and its branch'
    asyncio.run(main())
