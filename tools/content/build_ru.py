# build_ru.py - merge ru parts, validate against master_todo.tsv, emit ru_table.json/.tsv + report
import re, json, glob, os, sys, collections
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # tools/
REPO=os.path.dirname(ROOT)
OUT=os.path.join(REPO,'NihongoLife','Assets','NihongoLife','Resources','Content','Localization')
os.makedirs(OUT,exist_ok=True)
en={}
for line in open(os.path.join(ROOT,'content','out','master_todo.tsv'),encoding='utf8'):
    line=line.rstrip('\n').rstrip('\r')
    if not line: continue
    i,t=line.split('\t',1); en[i]=t
ru={}; dup=[]
for p in sorted(glob.glob(os.path.join(ROOT,'content','ru','part_*.tsv'))):
    for line in open(p,encoding='utf8'):
        line=line.rstrip('\n').rstrip('\r')
        if not line: continue
        i,t=line.split('|',1)
        if i in ru: dup.append(i)
        ru[i]=t
skip={i for i,t in en.items() if t.startswith('{fileID')}
missing=[i for i in en if i not in ru and i not in skip]
extra=[i for i in ru if i not in en]
errs=[]; warns=[]
tok=re.compile(r'\{[^{}]*\}|<[^<>]+>|\\n|%')
lat=re.compile(r'[A-Za-z]')
for i,e in en.items():
    if i in skip or i not in ru: continue
    r=ru[i]
    # whitespace parity
    lead=e[:len(e)-len(e.lstrip())]; trail=e[len(e.rstrip()):]
    r=lead+r.strip()+trail; ru[i]=r
    a=sorted(tok.findall(e)); b=sorted(tok.findall(r))
    # decimal comma tolerance not needed for tokens; compare
    if a!=b: errs.append((i,'tokens',a,b))
    # mixed-script word
    for w in re.findall(r'[A-Za-zА-Яа-яЁё]+',re.sub(r'<[^<>]+>|\{[^{}]*\}|\\n','',r)):
        if re.search('[A-Za-z]',w) and re.search('[А-Яа-яЁё]',w): errs.append((i,'mixed-word',w,''))
    if r==e and re.search('[A-Za-z]{3,}',e): warns.append((i,'identical',e[:60]))
n=len(en)-len(skip)
tbl=[{'en':en[i],'ru':ru[i]} for i in sorted(en) if i not in skip and i in ru]
json.dump({'language':'ru','count':len(tbl),'entries':tbl},open(os.path.join(OUT,'ru_table.json'),'w',encoding='utf8'),ensure_ascii=False,indent=0)
with open(os.path.join(OUT,'ru_table.tsv'),'w',encoding='utf8',newline='\n') as f:
    for x in tbl: f.write(x['en'].replace('\t',' ')+'\t'+x['ru'].replace('\t',' ')+'\n')
lat_words=collections.Counter()
for i in ru:
    s=re.sub(r'<[^<>]+>|\{[^{}]*\}','',ru[i])
    for w in re.findall(r'[A-Za-z][A-Za-z\-]+',s): lat_words[w]+=1
rep=[f'master={len(en)} skipped_yaml_refs={len(skip)} translated={len(tbl)} missing={len(missing)} extra={len(extra)} dup={len(dup)} errors={len(errs)} identical_warn={len(warns)}']
rep+=['MISSING '+','.join(missing[:50]),'EXTRA '+','.join(extra[:50]),'DUP '+','.join(dup)]
for e in errs[:80]: rep.append('ERR %s %s %s %s'%e)
for w in warns[:80]: rep.append('WARN %s %s %s'%w)
rep.append('LATIN WORDS in ru (top 120): '+', '.join(f'{w}:{c}' for w,c in lat_words.most_common(120)))
open(os.path.join(ROOT,'content','out','ru_report.txt'),'w',encoding='utf8').write('\n'.join(rep))
print('\n'.join(rep[:1])); print(len(errs),len(warns))