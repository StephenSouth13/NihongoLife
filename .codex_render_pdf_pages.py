from pathlib import Path
import fitz
from PIL import Image, ImageDraw

root = Path(r".docx_qa_capstone_v4")
pdf = fitz.open(root / "capstone.pdf")
thumbs=[]
for i,page in enumerate(pdf):
    pix=page.get_pixmap(matrix=fitz.Matrix(1.5,1.5),alpha=False)
    out=root/f"page-{i+1:03d}.png"
    pix.save(out)
    im=Image.open(out)
    t=im.copy();t.thumbnail((230,325))
    thumbs.append((i+1,t.copy()))
for batch in range(0,len(thumbs),12):
    group=thumbs[batch:batch+12]
    sheet=Image.new("RGB",(4*250,3*355),"#d8d8d8")
    draw=ImageDraw.Draw(sheet)
    for j,(num,im) in enumerate(group):
        x=(j%4)*250+10;y=(j//4)*355+20
        sheet.paste(im,(x,y));draw.text((x,y-16),f"Page {num}",fill="black")
    sheet.save(root/f"contact-{batch//12+1:02d}.png")
print(len(pdf))
