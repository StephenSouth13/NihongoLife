# -*- coding: utf-8 -*-
"""P4 dictionary builder. Source: tools/content/p4/src/*.txt  (en|vi|ru|ja|reading|pos), all ORIGINAL, self-authored.
Outputs Resources/Content/Dictionary/{entries,ru-vi,vi-ru,ru-ja,ja-ru,en-vi,vi-en,ja-vi,vi-ja,en-ru,ru-en,ja-en,en-ja}.json + index.json
and tools/content/out/dict_report.txt. Run: python -X utf8 tools/content/p4/build_dictionary.py"""
import json, os, re, sys, collections
HERE=os.path.dirname(os.path.abspath(__file__)); REPO=os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
SRC=os.path.join(HERE,'src')
OUT=os.path.join(REPO,'NihongoLife','Assets','NihongoLife','Resources','Content','Dictionary')
REP=os.path.join(REPO,'tools','content','out','dict_report.txt')
os.makedirs(OUT,exist_ok=True); os.makedirs(os.path.dirname(REP),exist_ok=True)
FILES=[('daily.txt','daily'),('food.txt','restaurant'),('medical.txt','medical'),('bank.txt','bank'),('travel.txt','travel'),
       ('shop_school_work.txt','shopping'),('general.txt','daily'),('life_tech.txt','daily'),('extra.txt','daily')]
SWITCH={'school':'school','company':'work'}   # inside shop_school_work.txt the domain changes at these first-keys
KANA=re.compile(r'^[\u3041-\u3096\u30A1-\u30FA\u30FCー、。 ・~！？!?]+$')
KANJI=re.compile(r'[\u4E00-\u9FFF々]')
CYR=re.compile(r'[\u0400-\u04FF]')
POS={'n','v','adj','adv','phr','num','conj','pron','interj'}
errs=[];warn=[];ents=[];seen={}
for fn,dom in FILES:
    cur=dom
    for ln,line in enumerate(open(os.path.join(SRC,fn),encoding='utf-8-sig').read().split('\n'),1):
        line=line.rstrip('\r')
        if not line.strip(): continue
        if line.startswith('#domain '): cur=line.split()[1]; continue
        f=line.split('|')
        if len(f)!=6: errs.append(f'{fn}:{ln}: {len(f)} fields: {line[:60]}'); continue
        en,vi,ru,ja,rd,pos=[x.strip() for x in f]
        if fn=='shop_school_work.txt' and en in SWITCH: cur=SWITCH[en]
        loc=f'{fn}:{ln}'
        if not (en and vi and ru and ja): errs.append(f'{loc}: empty field ({en})'); continue
        if pos not in POS: errs.append(f'{loc}: bad pos {pos}')
        if not CYR.search(ru): errs.append(f'{loc}: ru has no Cyrillic: {ru}')
        if CYR.search(vi) or CYR.search(ja): errs.append(f'{loc}: Cyrillic in vi/ja')
        if KANJI.search(ja) and not rd and pos!='phr': errs.append(f'{loc}: kanji without reading: {ja}')
        if KANJI.search(ja) and not rd and pos=='phr': errs.append(f'{loc}: phrase kanji without reading: {ja}')
        if rd and not KANA.match(rd): errs.append(f'{loc}: reading not kana: {rd}')
        if not rd and not KANJI.search(ja) and not re.search(r'[A-Za-z0-9]',ja) and not KANA.match(ja): errs.append(f'{loc}: odd ja {ja}')
        key=en.lower()
        if key in seen:
            p=seen[key]
            if p['ja']==ja:
                if cur not in p['domains']: p['domains'].append(cur)
                warn.append(f'{loc}: duplicate of {p["id"]} merged ({en})')
            else: errs.append(f'{loc}: duplicate en "{en}" with different ja ({p["ja"]} vs {ja})')
            continue
        e={'id':'','en':en,'vi':vi,'ru':ru,'ja':ja,'reading':rd,'pos':pos,'domains':[cur]}
        seen[key]=e; ents.append(e)
for i,e in enumerate(ents,1): e['id']=f'w{i:05d}'
# second pass: ja duplicates with different en (ambiguous headword) -> warning only
jaseen=collections.defaultdict(list)
for e in ents: jaseen[e['ja']].append(e['en'])
for j,v in jaseen.items():
    if len(v)>1: warn.append(f'ja homograph "{j}": {v}')
def split_syn(s):
    """headword variants: split on ', ' / '; ' outside parentheses"""
    out=[];depth=0;cur=''
    for ch in s:
        if ch in '(（': depth+=1
        if ch in ')）': depth-=1
        if ch in ',;、' and depth==0: out.append(cur.strip()); cur=''
        else: cur+=ch
    out.append(cur.strip()); return [x for x in out if x]
def strip_par(s): return re.sub(r'\s*[\(（][^)）]*[\)）]','',s).strip()
def keys(lang,e):
    v=e[lang]
    ks=split_syn(v)
    ks=[strip_par(k) or k for k in ks]
    if lang=='ja' and e['reading']: ks.append(e['reading'])
    out=[]
    for k in ks:
        if k not in out: out.append(k)
    return out
def rec(lang_to,e):
    r={'id':e['id'],'t':e[lang_to],'p':e['pos'],'d':e['domains']}
    if e['reading']: r['r']=e['reading']
    return r
pairs=[('ru','vi'),('vi','ru'),('ru','ja'),('ja','ru'),('en','vi'),('vi','en'),('ja','vi'),('vi','ja'),('en','ru'),('ru','en'),('ja','en'),('en','ja')]
sizes={}
for a,b in pairs:
    idx=collections.OrderedDict()
    for e in sorted(ents,key=lambda x:x[a].lower()):
        for k in keys(a,e):
            idx.setdefault(k,[]).append(rec(b,e)) if True else None
    # when the source side is ja include reading as separate key (kana lookup) — already in keys
    doc={'schema':'nihongolife.dict/1','from':a,'to':b,'count':len(ents),'headwords':len(idx),
         'sourceNote':'Original self-authored data (Gánh Ngon / NihongoLife). No third-party dictionary content.',
         'entries':[{'k':k,'v':v} for k,v in sorted(idx.items(),key=lambda kv:kv[0].lower())]}
    p=os.path.join(OUT,f'{a}-{b}.json'); json.dump(doc,open(p,'w',encoding='utf-8'),ensure_ascii=False,separators=(',',':')); sizes[f'{a}-{b}']=os.path.getsize(p)
doc={'schema':'nihongolife.dict/1','kind':'concepts','count':len(ents),'entries':ents}
p=os.path.join(OUT,'entries.json'); json.dump(doc,open(p,'w',encoding='utf-8'),ensure_ascii=False,separators=(',',':')); sizes['entries']=os.path.getsize(p)
dom=collections.Counter(d for e in ents for d in e['domains']); posc=collections.Counter(e['pos'] for e in ents)
index={'schema':'nihongolife.dict/1','count':len(ents),'domains':dict(dom),'pos':dict(posc),'pairs':[f'{a}-{b}' for a,b in pairs],'files':sizes,
       'licence':'Original content authored for NihongoLife; no JMdict/Wiktionary/Tatoeba/OpenCorpora data used.'}
json.dump(index,open(os.path.join(OUT,'index.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=1)
with open(REP,'w',encoding='utf-8') as f:
    f.write(f'entries {len(ents)}\ndomains {dict(dom)}\npos {dict(posc)}\nsizes {sizes}\nerrors {len(errs)}\nwarnings {len(warn)}\n')
    f.write('\n'.join('ERR '+x for x in errs)+'\n'+'\n'.join('WARN '+x for x in warn)+'\n')
print('entries',len(ents),dict(dom),'errors',len(errs),'warnings',len(warn))
for x in errs[:40]: print('ERR',x)