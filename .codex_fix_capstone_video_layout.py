from pathlib import Path
from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH

p = Path(r"Bao_Cao/NihongoLife_Bao_Cao_Capstone_v4.docx")
d = Document(p)
video = next(x for x in d.paragraphs if x.text.strip().startswith("Trailer v2:"))
video.alignment = WD_ALIGN_PARAGRAPH.LEFT
video.paragraph_format.first_line_indent = None
video.paragraph_format.keep_together = True
d.save(p)
print(p)
