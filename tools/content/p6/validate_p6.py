# -*- coding: utf-8 -*-
"""P6 validator: python -X utf8 tools/content/p6/validate_p6.py  (exit 1 on error)"""
import json,glob,os,sys
H=os.path.dirname(os.path.abspath(__file__)); R=os.path.dirname(os.path.dirname(os.path.dirname(H)))
C=os.path.join(R,'NihongoLife','Assets','NihongoLife','Resources','Content')
err=[];nq=0;ne=0
for f in sorted(glob.glob(os.path.join(C,'Exams','*.json'))):
    d=json.load(open(f,encoding='utf-8'))
    if 'sections' not in d: continue
    ne+=1; ids=set()
    for s in d['sections']:
        pids={p['id'] for p in s['passages']}
        for q in s['questions']:
            nq+=1
            if q['id'] in ids: err.append(f"{d['id']}: dup id {q['id']}")
            ids.add(q['id'])
            if q['type']=='MultipleChoice':
                n=len(q['choices'])
                if n<2 or not (0<=q['correctChoiceIndex']<n): err.append(f"{q['id']}: bad correctChoiceIndex")
                if len({c['textEn'] or c['textJa'] for c in q['choices']})<n: err.append(f"{q['id']}: duplicate choices")
            elif q['type']=='FillBlank' and not q['acceptedAnswers']: err.append(f"{q['id']}: no acceptedAnswers")
            if not (q['explanationVi'] or q['explanationEn']): err.append(f"{q['id']}: no explanation")
            if q.get('passageId') and q['passageId'] not in pids: err.append(f"{q['id']}: unknown passage")
ck=json.load(open(os.path.join(C,'English','verb_conjugation_key.json'),encoding='utf-8'))
ex=json.load(open(os.path.join(C,'English','tense_exercises.json'),encoding='utf-8'))
co=json.load(open(os.path.join(C,'English','tenses_course.json'),encoding='utf-8'))
print('exams',ne,'questions',nq,'| verbs',len(ck.get('verbs',ck)),'| exercises',len(ex.get('exercises',ex)),'| lessons',len(co.get('lessons',co)))
for e in err: print('ERR',e)
print('errors',len(err)); sys.exit(1 if err else 0)