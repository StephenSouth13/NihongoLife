import collections, io, sys, zipfile
import fitz
from PIL import Image, ImageChops, ImageStat
a=fitz.open(r'D:/VTC_Academy/NihongoLife/NihongoLife/Bao_Cao/NihongoLife_Thuyet_Trinh_Capstone_v9.pdf')
b=fitz.open('slides_v10.pdf')
print('pages',a.page_count,b.page_count,b[0].rect)
sizes=collections.Counter(); fonts=collections.Counter(); low=[]
for i,p in enumerate(b):
    for bl in p.get_text('dict')['blocks']:
        for l in bl.get('lines',[]):
            for s in l['spans']:
                t=s['text'].strip()
                if not t: continue
                sizes[round(s['size'])]+=len(t); fonts[s['font']]+=len(t)
                if s['size']<29.5: low.append((i+1,round(s['size'],1),t[:30]))
print(sorted(sizes.items())); print(fonts.most_common()); print('below 30:',low[:10])
z=zipfile.ZipFile('slides_v10.pptx'); print('embedded',[n for n in z.namelist() if 'font' in n.lower() and not n.endswith('.rels')][:8])
sc=b[0].rect.width/a[0].rect.width
diffs=[]
ims=[]
for n in range(a.page_count):
    x=Image.open(io.BytesIO(a[n].get_pixmap(dpi=40).tobytes('png'))).convert('RGB')
    y=Image.open(io.BytesIO(b[n].get_pixmap(dpi=40).tobytes('png'))).convert('RGB').resize(x.size)
    d=ImageChops.difference(x,y).convert('L'); diffs.append((round(ImageStat.Stat(d).mean[0],1),n+1))
    ims.append(y)
print('largest differences',sorted(diffs,reverse=True)[:8])
W,H=ims[0].size;c=6;r=(len(ims)+c-1)//c
s=Image.new('RGB',(c*W,r*H),'white')
for k,i in enumerate(ims): s.paste(i,((k%c)*W,(k//c)*H))
s.save('slides_v10_sheet.png')
