#!/usr/bin/env python3
"""Build the master list of translatable English strings (code + JSON + ScriptableObject assets).
Run from repo root:  python tools/content/build_master.py
Outputs tools/content/out/master_en.json  ({en: {"id","kinds","n","src"}}) and master_todo.tsv (id<TAB>en)."""
import os, re, json, glob, collections, sys
sys.path.insert(0, os.path.join('tools','content'))
import extract_loc as X
ROOT = X.ROOT; OUT = X.OUT
CJK = re.compile(r'[\u3040-\u30ff\u3400-\u4dbf\u4e00-\u9fff\uff66-\uff9f]')
def translatable(s):
    if not s or not s.strip(): return False
    if CJK.search(s): return False              # Japanese being learned is content, not interface
    if not re.search(r'[A-Za-z]{2}', s): return False
    return True

def yaml_val(v):
    v=v.strip()
    if len(v)>=2 and v[0]=='"' and v[-1]=='"': return X.unesc(v[1:-1])
    if len(v)>=2 and v[0]=="'" and v[-1]=="'": return v[1:-1].replace("''","'")
    return v

def scan_json():
    res=[]
    for f in sorted(glob.glob(os.path.join(ROOT,'Resources','**','*.json'),recursive=True)):
        rel=f.replace('\\','/')
        try: d=json.load(open(f,encoding='utf-8-sig'))
        except Exception as e: print('bad json',rel,e); continue
        def walk(o,p):
            if isinstance(o,dict):
                for k,v in o.items():
                    q=p+'/'+k
                    if isinstance(v,str) and (k=='en' or (k.endswith('En') and len(k)>2)): res.append((v,rel,q))
                    walk(v,q)
            elif isinstance(o,list):
                for i,v in enumerate(o): walk(v,p+'/'+str(i))
        walk(d,'')
    return res

def yaml_multiline(lines,i,first):
    """first = text after 'key:'; returns (value, next_index). Handles Unity's folded double/single quoted scalars."""
    v=first.strip()
    if v[:1] not in ('"',"'"): return v,i+1
    q=v[0]; buf=v; j=i+1
    def closed(t):
        k=1
        while k<len(t):
            if q=='"' and t[k]=='\\': k+=2; continue
            if t[k]==q:
                if q=="'" and k+1<len(t) and t[k+1]=="'": k+=2; continue
                return k==len(t)-1 or t[k+1:].strip()==''
            k+=1
        return False
    while not closed(buf) and j<len(lines):
        nxt=lines[j].strip()
        if nxt=='' : buf+='\n' if False else '\\n'
        elif buf.endswith('\\') and q=='"' and not buf.endswith('\\\\'): buf=buf[:-1]+nxt
        else: buf+=' '+nxt
        j+=1
    return yaml_val(buf),j

def scan_assets():
    res=[]; pat=re.compile(r'^\s*-?\s*(\w*?)(En|en):\s*(.*)$')
    for ext in ('asset','prefab','unity'):
        for f in glob.glob(os.path.join(ROOT,'**','*.'+ext),recursive=True):
            rel=f.replace('\\','/')
            lines=open(f,encoding='utf-8',errors='replace').read().split('\n')
            i=0
            while i<len(lines):
                m=pat.match(lines[i])
                if m and m.group(3).strip() not in ('','""',"''"):
                    val,j=yaml_multiline(lines,i,m.group(3))
                    res.append((val,rel,str(i+1))); i=j
                else: i+=1
    return res

if __name__=='__main__':
    tr,dy=X.scan_cs()
    master=collections.OrderedDict()
    def add(en,kind,src):
        if not translatable(en): return
        e=master.setdefault(en,{'kinds':set(),'n':0,'src':[]})
        e['kinds'].add(kind); e['n']+=1
        if len(e['src'])<3: e['src'].append(src)
    for t in tr:
        if t['head'] in ('L','Tr','T','F'): continue   # L(ja, kana, romaji) etc. are not interface triples
        add(t['en'],'code',f"{t['file']}:{t['line']}")
    for en,f,p in scan_json(): add(en,'json',f+'#'+p)
    for en,f,p in scan_assets(): add(en,'asset',f+':'+p)
    ids={}
    with open(os.path.join(OUT,'master_todo.tsv'),'w',encoding='utf-8',newline='\n') as fh:
        for n,en in enumerate(sorted(master,key=lambda s:(sorted(master[s]['kinds'])[0],s)),1):
            rid='R%04d'%n; ids[en]=rid
            fh.write(rid+'\t'+en.replace('\\','\\\\').replace('\n','\\n').replace('\t','\\t')+'\n')
    out={en:{'id':ids[en],'kinds':sorted(v['kinds']),'n':v['n'],'src':v['src']} for en,v in master.items()}
    json.dump(out,open(os.path.join(OUT,'master_en.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=0)
    byk=collections.Counter(k for v in master.values() for k in v['kinds'])
    print('unique en',len(master),byk,'chars',sum(len(s) for s in master))