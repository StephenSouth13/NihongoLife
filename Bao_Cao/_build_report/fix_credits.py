"""Credits in the report, as the owner set them on 2026-11-08: Quách Thành Long built the whole game; the other members of
The Next Generation are co-founders / communication and did not build it directly. Edits the report in place.
Usage (from Bao_Cao/_build_report): python fix_credits.py ../NihongoLife_Bao_Cao_Capstone.docx"""
import sys

from docx import Document

FIXES = [
    ('Em cảm ơn các bạn trong nhóm The Next Generation: Nguyễn Lê Gia Huy và Lương Nguyễn Tấn Dũng đã cùng phát triển nhân vật, animation và gameplay; Nguyễn Ngọc Minh Châu và Đăng phụ trách truyền thông nội dung, đăng bài website và khảo sát người dùng.',
     'Em cảm ơn các bạn trong nhóm The Next Generation đã đồng hành cùng dự án: Nguyễn Lê Gia Huy và Lương Nguyễn Tấn Dũng (đồng sáng lập), Nguyễn Ngọc Minh Châu và Đăng phụ trách truyền thông nội dung, đăng bài website và khảo sát người dùng.'),
    ('Trách nhiệm: Quách Thành Long — người chịu trách nhiệm chính và đóng góp nhiều nhất.',
     'Trách nhiệm: Quách Thành Long — người trực tiếp xây dựng toàn bộ game (thiết kế, lập trình, nội dung, kiểm thử).'),
    ('Trưởng nhóm · Lập trình game & web · Người chịu trách nhiệm chính', 'Trưởng nhóm · Xây dựng toàn bộ game'),
    ('Nhân vật · Animation · Gameplay', 'Đồng sáng lập · Không trực tiếp xây dựng game'),
]


def replace(paragraph, old, new):
    if old not in paragraph.text:
        return 0
    for run in paragraph.runs:
        if old in run.text:
            run.text = run.text.replace(old, new)
            return 1
    # Spread over several runs: the paragraph keeps the first run's formatting.
    text = paragraph.text.replace(old, new)
    paragraph.runs[0].text = text
    for run in paragraph.runs[1:]:
        run.text = ''
    return 1


doc = Document(sys.argv[1])
paragraphs = list(doc.paragraphs) + [p for t in doc.tables for row in t.rows for cell in row.cells for p in cell.paragraphs]
done = {old: sum(replace(p, old, new) for p in paragraphs) for old, new in FIXES}
for old, n in done.items():
    print(n, old[:60])
assert all(n > 0 for n in done.values()) or all(new in '\n'.join(p.text for p in paragraphs) for _, new in FIXES), 'a credit line was not found'
doc.save(sys.argv[1])
