# -*- coding: utf-8 -*-
"""Convert Resources/Content/Exams/*.json -> ExamDefinition YAML .asset using Tools/exam/exam_lib.py (no Unity needed).
   python tools/content/p6/exam_json_to_asset.py [--out DIR]   default DIR = tools/content/out/exam_assets (review first)
   To install: --out NihongoLife/Assets/NihongoLife/Resources/Exams   (Unity generates .meta on next import)"""
import json, os, sys, glob
HERE=os.path.dirname(os.path.abspath(__file__))
REPO=os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
sys.path.insert(0,os.path.join(REPO,'NihongoLife','Tools','exam'))
from exam_lib import Exam, Section, Passage, Question, Choice
ST={"JlptVocabulary":"jlpt_vocab","JlptGrammarReading":"jlpt_grammar_reading","JlptListening":"jlpt_listening","IeltsListening":"ielts_listening","IeltsReading":"ielts_reading","IeltsWriting":"ielts_writing","IeltsSpeaking":"ielts_speaking"}
QT={"MultipleChoice":"mc","TrueFalseNotGiven":"tfng","FillBlank":"fill","Essay":"essay","SpeakingPrompt":"speaking"}
out=os.path.join(REPO,'tools','content','out','exam_assets')
if '--out' in sys.argv: out=os.path.abspath(sys.argv[sys.argv.index('--out')+1])
os.makedirs(out,exist_ok=True)
src=os.path.join(REPO,'NihongoLife','Assets','NihongoLife','Resources','Content','Exams')
n=0
for f in sorted(glob.glob(os.path.join(src,'*.json'))):
    d=json.load(open(f,encoding='utf8'))
    if d.get('schema')!='nihongolife.exam/1' or 'sections' not in d: continue
    secs=[]
    for s in d['sections']:
        ps=[Passage(p['id'],p.get('titleVi',''),p.get('titleEn',''),p.get('titleJa',''),p.get('bodyVi',''),p.get('bodyEn',''),p.get('bodyJa',''),p.get('bodyReading',''),p.get('maxPlays',0)) for p in s['passages']]
        qs=[Question(q['id'],QT[q['type']],q['promptVi'],q['promptEn'],q['promptJa'],q.get('promptReading',''),q.get('passageId',''),
              [Choice(c['textVi'],c['textEn'],c['textJa']) for c in q['choices']],q['correctChoiceIndex'],q['acceptedAnswers'],
              explanation_vi=q['explanationVi'],explanation_en=q['explanationEn'],tags=q['tags'],points=q['points'],mondai=q.get('mondai','')) for q in s['questions']]
        secs.append(Section(s['id'],ST[s['type']],s['titleVi'],s['titleEn'],s.get('titleJa',''),s['timeLimitSeconds'],s['scoreScaleMax'],ps,qs,s.get('scoreGroup',''),[(m['id'],m['titleJa'],m['titleVi']) for m in s['mondai']]))
    ex=Exam(d['id'],'jlpt' if d['examType']=='Jlpt' else 'ielts',d['level'],d['titleVi'],d['titleEn'],d['titleJa'],d['descriptionVi'],d['descriptionEn'],secs,
            d['jlptTotalPassScore'] or 80,d['jlptSectionPassScore'] or 19,[(g['id'],g['titleJa'],g['titleVi'],g['max'],g['passMin']) for g in d['scoreGroups']])
    ex.write(os.path.join(out,d['id']+'.asset'),name=d['id']); n+=1
print('wrote',n,'assets to',out)