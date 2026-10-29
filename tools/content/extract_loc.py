#!/usr/bin/env python3
"""Extract every (vi,en,ja) literal triple and every hard-coded UI string from the C# sources.
Usage: python tools/content/extract_loc.py   (run from repo root). Writes tools/content/out/*.json
Original tool by the Grok content workstream (no third-party code)."""
import re, os, sys, json, glob, collections
ROOT = os.path.join('NihongoLife', 'Assets', 'NihongoLife')
OUT = os.path.join('tools', 'content', 'out'); os.makedirs(OUT, exist_ok=True)

ESC = {'n':'\n','t':'\t','r':'\r','\\':'\\','"':'"',"'":"'",'0':'\0','{':'{','}':'}'}
def unesc(s):
    out=[];i=0
    while i<len(s):
        c=s[i]
        if c=='\\' and i+1<len(s):
            n=s[i+1]
            if n=='u' and i+5<len(s):
                try: out.append(chr(int(s[i+2:i+6],16))); i+=6; continue
                except: pass
            out.append(ESC.get(n,n)); i+=2
        else: out.append(c); i+=1
    return ''.join(out)

def read_string(t, i):
    """t[i] starts a string literal (", $", @", $@", @$"). Returns (end_index_exclusive, raw_value, is_interp, ok)."""
    j=i; interp=False; verb=False
    while t[j] in '$@':
        if t[j]=='$': interp=True
        else: verb=True
        j+=1
    if t[j]!='"': return None
    j+=1; buf=[]; depth=0
    while j<len(t):
        c=t[j]
        if verb:
            if c=='"':
                if j+1<len(t) and t[j+1]=='"': buf.append('"'); j+=2; continue
                return j+1, ''.join(buf), interp, True
            buf.append(c); j+=1
        else:
            if c=='\\' and j+1<len(t): buf.append(t[j:j+2]); j+=2; continue
            if interp and c=='{':
                if j+1<len(t) and t[j+1]=='{': buf.append('{{'); j+=2; continue
                # embedded expression: copy until matching }, tolerate nested strings
                d=1; k=j+1
                while k<len(t) and d>0:
                    if t[k]=='"':
                        r=read_string(t,k)
                        if r: k=r[0]; continue
                    if t[k]=='{': d+=1
                    elif t[k]=='}': d-=1
                    k+=1
                buf.append(t[j:k]); j=k; continue
            if c=='"': return j+1, unesc(''.join(buf)) if not verb else ''.join(buf), interp, True
            if c=='\n': return j, ''.join(buf), interp, False
            buf.append(c); j+=1
    return None

def split_args(t, i):
    """t[i] is just after '('. Returns (list of (start,end) arg spans, index after ')')."""
    args=[]; depth=0; start=i; j=i
    while j<len(t):
        c=t[j]
        if c in '"$@':
            if c in '$@' and not (j+1<len(t) and t[j+1] in '"$@'): j+=1; continue
            r=read_string(t,j)
            if r: j=r[0]; continue
        if c=="'" :
            k=t.find("'",j+1+(1 if t[j+1]=='\\' else 0)+1)
            if k!=-1 and k-j<=8: j=k+1; continue
        if c=='/' and j+1<len(t) and t[j+1]=='/':
            j=t.find('\n',j); j=len(t) if j<0 else j; continue
        if c in '([{': depth+=1
        elif c in ')]}':
            if depth==0:
                args.append((start,j)); return args, j+1
            depth-=1
        elif c==',' and depth==0:
            args.append((start,j)); start=j+1
        j+=1
    return args, j

def literal_of(expr):
    """expr is pure string literal(s) joined by + ; returns (value, interp) or None."""
    e=expr.strip(); parts=[]; i=0; interp=False
    while i<len(e):
        while i<len(e) and e[i] in ' \t\r\n': i+=1
        if i>=len(e): break
        r=read_string(e,i) if e[i] in '"$@' else None
        if not r or not r[3]: return None
        interp = interp or r[2]
        parts.append(r[1]); i=r[0]
        while i<len(e) and e[i] in ' \t\r\n': i+=1
        if i<len(e):
            if e[i]=='+': i+=1
            else: return None
    return (''.join(parts), interp) if parts else None

def line_of(t, pos): return t.count('\n',0,pos)+1

HEADS = r'(?:NihongoLife\.Core\.)?Loc\.(?:T|F|Pick|Triple)|\bTri|\bL|\bLocalize|\bPick|\bTr'
CALL = re.compile(r'(?<![\w\.])((?:NihongoLife\.Core\.)?Loc\.(?:T|F|Pick|Triple)|Tri|L|Localize|Pick|Tr|T|F)\s*\(')

def scan_cs():
    triples=[]; dyn=[]
    for f in sorted(glob.glob(os.path.join(ROOT,'Scripts','**','*.cs'),recursive=True)):
        rel=f.replace('\\','/')
        t=open(f,encoding='utf-8-sig',errors='replace').read()
        for m in CALL.finditer(t):
            head=m.group(1)
            args,end=split_args(t,m.end())
            if len(args)<3: continue
            exprs=[t[a:b] for a,b in args[:3]]
            lits=[literal_of(x) for x in exprs]
            # decide if it is a language triple: at least two args literal strings
            nlit=sum(1 for x in lits if x)
            if nlit==0: continue
            ln=line_of(t,m.start())
            if all(lits):
                triples.append({'file':rel,'line':ln,'head':head,'vi':lits[0][0],'en':lits[1][0],'ja':lits[2][0],'interp':any(x[1] for x in lits)})
            elif nlit>=1 and head.startswith(('Loc.','NihongoLife.Core.Loc')):
                dyn.append({'file':rel,'line':ln,'head':head,'args':[x.strip()[:120] for x in exprs]})
    return triples, dyn

if __name__=='__main__':
    tr,dy=scan_cs()
    json.dump(tr,open(os.path.join(OUT,'cs_triples.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=0)
    json.dump(dy,open(os.path.join(OUT,'cs_dynamic.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=0)
    c=collections.Counter(x['head'] for x in tr)
    print(len(tr),'triples', len(dy),'partial/dynamic'); print(c.most_common())
    print('unique en:',len({x['en'] for x in tr}))