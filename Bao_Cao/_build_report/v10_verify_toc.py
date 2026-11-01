import re, sys, docx, fitz
pdf=fitz.open(sys.argv[1]); d=docx.Document(sys.argv[2])
pages=[p.get_text() for p in pdf]
norm=lambda s: re.sub(r'\s+',' ',s).strip()
txt=[norm(t) for t in pages]
first=next(i for i,t in enumerate(txt) if 'CHƯƠNG 1. GIỚI THIỆU ĐỀ TÀI' in t and 'Bối cảnh và vấn đề' in t and '....' not in t)
print('Chapter 1 starts at pdf page',first+1)
def roman(n):
    r='';v=[(10,'x'),(9,'ix'),(5,'v'),(4,'iv'),(1,'i')]
    for a,b in v:
        while n>=a: r+=b;n-=a
    return r
entries=[]
for p in d.paragraphs:
    if p.style.name.lower().startswith('toc ') and p.style.name.lower()!='toc heading':
        m=re.match(r'^(.*)\t([ivx]+|\d+)$',p.text)
        if m: entries.append((p.style.name,m.group(1).strip(),m.group(2)))
print('entries',len(entries),{k:sum(1 for e in entries if e[0]==k) for k in sorted({e[0] for e in entries})})
bad=[]
for st,t,pg in entries:
    i=first+int(pg)-1 if pg.isdigit() else None
    if i is None:
        n=['i','ii','iii','iv','v','vi','vii','viii','ix','x','xi','xii','xiii','xiv','xv','xvi','xvii','xviii','xix','xx'].index(pg)+1
        i=n
    key=norm(t)[:40]
    if i>=len(txt) or key not in txt[i]:
        bad.append((st,t[:60],pg,[j-first+1 for j,x in enumerate(txt) if key in x and j>first-1][:4]))
print('mismatches',len(bad),bad[:8])
heads=[p.text for p in d.paragraphs if p.style.name in('Heading 1','Heading 2','Heading 3')]
tt={norm(e[1]) for e in entries}
print('headings',len(heads),'missing in toc',[h[:50] for h in heads if norm(h) not in tt][:6])
caps=[p.text for p in d.paragraphs if p.style.name in('ChuThichHinh','ChuThichBang')]
print('captions',len(caps),'missing',[c[:50] for c in caps if norm(c) not in tt][:6])
