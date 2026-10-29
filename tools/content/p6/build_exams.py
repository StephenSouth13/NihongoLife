# -*- coding: utf-8 -*-
# Builds exam JSON (mirrors ExamDefinition fields) -> Resources/Content/Exams/*.json ; optional --assets writes YAML via Tools/exam/exam_lib
import json, os, sys, random
HERE=os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0,HERE)
from jlpt_data import EXAMS as J, MONDAI
from english_exam_data import E
REPO=os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
OUT=os.path.join(REPO,'NihongoLife','Assets','NihongoLife','Resources','Content','Exams'); os.makedirs(OUT,exist_ok=True)
ENG=os.path.join(REPO,'NihongoLife','Assets','NihongoLife','Resources','Content','English')
SCHEMA="nihongolife.exam/1"
def shuffled(choices,ans,seed):
    idx=list(range(len(choices))); random.Random(seed).shuffle(idx)
    return [choices[i] for i in idx], idx.index(ans)
def mc_q(id,prompt_vi,prompt_en,prompt_ja,choices,ans,vi,en,tags,passage="",mondai="",text='ja'):
    ch,a=shuffled(choices,ans,id)
    ex=[dict(textVi=c,textEn=c,textJa=c if text=='ja' else "") for c in ch]
    return dict(id=id,type="MultipleChoice",passageId=passage,promptVi=prompt_vi,promptEn=prompt_en,promptJa=prompt_ja,promptReading="",
                choices=ex,correctChoiceIndex=a,acceptedAnswers=[],explanationVi=vi,explanationEn=en,tags=tags,points=1,mondai=mondai)
JL={"N5":dict(vi="Đề luyện JLPT N5 (mini, không có phần nghe)",en="JLPT N5 practice mini-test (no listening)",ja="日本語能力試験 N5 ミニ模試（聴解なし）",ru="Мини-тест JLPT N5 (без аудирования)",total=53,sec=13,groups=True,band=(0,0)),
 "N4":dict(vi="Đề luyện JLPT N4 (mini, không có phần nghe)",en="JLPT N4 practice mini-test (no listening)",ja="日本語能力試験 N4 ミニ模試（聴解なし）",ru="Мини-тест JLPT N4 (без аудирования)",total=60,sec=13,groups=True,band=(0,0)),
 "N3":dict(vi="Đề luyện JLPT N3 (mini, không có phần nghe)",en="JLPT N3 practice mini-test (no listening)",ja="日本語能力試験 N3 ミニ模試（聴解なし）",ru="Мини-тест JLPT N3 (без аудирования)",total=63,sec=13,groups=False,band=(0,0)),
 "N2":dict(vi="Đề luyện JLPT N2 (mini, không có phần nghe)",en="JLPT N2 practice mini-test (no listening)",ja="日本語能力試験 N2 ミニ模試（聴解なし）",ru="Мини-тест JLPT N2 (без аудирования)",total=60,sec=13,groups=False,band=(0,0)),
 "N1":dict(vi="Đề luyện JLPT N1 (mini, không có phần nghe)",en="JLPT N1 practice mini-test (no listening)",ja="日本語能力試験 N1 ミニ模試（聴解なし）",ru="Мини-тест JLPT N1 (без аудирования)",total=67,sec=13,groups=False,band=(0,0))}
exams=[]
for key,e in J.items():
    L=e['level']; meta=JL[L]; eid=f"jlpt_{key}_mini_1"
    seen=set(); mondai=[]
    def mnd(m):
        if m not in seen: seen.add(m); mondai.append(dict(id=m,titleJa=MONDAI[m][0],titleVi=MONDAI[m][1]))
    qv=[];n=0
    for q in e['vocab']:
        n+=1; mnd(q['m']); qv.append(mc_q(f"{eid}_v{n:02d}",MONDAI[q['m']][1],MONDAI[q['m']][2],q['p'],q['c'],q['a'],q['vi'],q['en'],[L,q['m']],mondai=q['m']))
    qg=[];passages=[];n=0
    for q in e['gram']:
        n+=1; mnd(q['m']); qg.append(mc_q(f"{eid}_g{n:02d}",MONDAI[q['m']][1],MONDAI[q['m']][2],q['p'],q['c'],q['a'],q['vi'],q['en'],[L,q['m'],'grammar'],mondai=q['m']))
    for p in e['reading']:
        pid=f"{eid}_{p['id']}"; passages.append(dict(id=pid,titleJa=p['title'],titleVi="Bài đọc",titleEn="Reading",bodyJa=p['body'],bodyVi="",bodyEn="",bodyReading="",maxPlays=0))
        for q in p['qs']:
            n+=1; mnd(q['m']); qg.append(mc_q(f"{eid}_g{n:02d}",MONDAI[q['m']][1],MONDAI[q['m']][2],q['p'],q['c'],q['a'],q['vi'],q['en'],[L,'reading'],passage=pid,mondai=q['m']))
    grp="language" if meta['groups'] else ""
    secs=[dict(id="vocab",type="JlptVocabulary",titleVi="Từ vựng / Chữ Hán",titleEn="Vocabulary / Kanji",titleJa="文字・語彙",timeLimitSeconds=900,scoreScaleMax=60,scoreGroup=grp,mondai=[m for m in mondai if m['id'] in ('m_read','m_orth','m_ctx','m_syn')],passages=[],questions=qv),
          dict(id="grammar_reading",type="JlptGrammarReading",titleVi="Ngữ pháp / Đọc hiểu",titleEn="Grammar / Reading",titleJa="文法・読解",timeLimitSeconds=1800,scoreScaleMax=60,scoreGroup=grp,mondai=[m for m in mondai if m['id'] in ('m_gram','m_read_c')],passages=passages,questions=qg)]
    exams.append(dict(schema=SCHEMA,id=eid,examType="Jlpt",family="JLPT",level=L,learnerLevel={"N5":"Basic","N4":"Elementary","N3":"Intermediate","N2":"Upper-intermediate","N1":"Advanced"}[L],
      recommendedBandMin=0,recommendedBandMax=0,titleVi=meta['vi'],titleEn=meta['en'],titleJa=meta['ja'],titleRu=meta['ru'],
      descriptionVi="Đề tự soạn theo cấu trúc JLPT (từ vựng + ngữ pháp + đọc hiểu). Không có phần nghe; điểm đạt đã quy đổi theo thang 120 (2/3 của 180).",
      descriptionEn="Original practice test in JLPT style (vocabulary + grammar + reading). No listening section; pass marks scaled to a 120-point scale (2/3 of 180).",
      descriptionJa="JLPT形式のオリジナル練習問題（文字・語彙・文法・読解）。聴解なし。合格点は120点満点に換算。",descriptionRu="Авторский тренировочный тест в формате JLPT (лексика, грамматика, чтение). Без аудирования; проходной балл пересчитан на 120.",
      jlptTotalPassScore=meta['total'],jlptSectionPassScore=meta['sec'],
      scoreGroups=([dict(id="language",titleJa="言語知識・読解",titleVi="Kiến thức ngôn ngữ + Đọc hiểu",max=120,passMin=38)] if meta['groups'] else []),sections=secs))
# ---- English levels
NAMES={"A1":("Beginner","Cơ bản","Basic","Начальный"),"A2":("Elementary","Sơ cấp","Elementary","Элементарный"),"B1":("Intermediate","Trung cấp","Intermediate","Средний"),"B2":("Upper-intermediate","Trung cao cấp","Upper-intermediate","Выше среднего"),"C1":("Advanced","Cao cấp","Advanced","Продвинутый")}
BAND={"A1":(1.0,2.5),"A2":(2.5,3.5),"B1":(4.0,5.0),"B2":(5.5,6.5),"C1":(7.0,8.0)}
def eng_secs(eid,L,e):
    S=[]
    def sec(sid,tv,te,items,passages=None,time=900,tag=''):
        qs=[]
        for n,q in enumerate(items,1):
            qs.append(mc_q(f"{eid}_{sid[0]}{n:02d}","Chọn đáp án đúng. "+q['p'],q['p'],"",q['c'],q['a'],q['vi'],q['en'],[L,tag or sid],passage=(passages[0]['id'] if passages else ""),text='en'))
        return dict(id=sid,type="IeltsReading",titleVi=tv,titleEn=te,titleJa="",timeLimitSeconds=time,scoreScaleMax=9,scoreGroup="",mondai=[],passages=passages or [],questions=qs)
    S.append(sec("grammar","Ngữ pháp","Grammar",e['grammar'],time=600))
    S.append(sec("vocabulary","Từ vựng","Vocabulary",e['vocab'],time=300))
    p=e['passage']; pid=f"{eid}_passage"
    S.append(sec("reading","Đọc hiểu","Reading",p['qs'],passages=[dict(id=pid,titleJa="",titleVi="Bài đọc",titleEn=p['title'],bodyJa="",bodyVi="",bodyEn=p['body'],bodyReading="",maxPlays=0)],time=600))
    return S
for key,e in E.items():
    L=e['level']; eid=f"en_{key}_practice_1"; nm=NAMES[L]
    exams.append(dict(schema=SCHEMA,id=eid,examType="Ielts",family="English",level=L,learnerLevel=nm[0],recommendedBandMin=BAND[L][0],recommendedBandMax=BAND[L][1],
      titleVi=f"Đề luyện tiếng Anh {L} ({nm[1]})",titleEn=f"English practice test {L} ({nm[2]})",titleJa=f"英語練習テスト {L}",titleRu=f"Тренировочный тест по английскому {L} ({nm[3]})",
      descriptionVi="Đề tự soạn: 10 câu ngữ pháp, 5 câu từ vựng, 1 bài đọc (5 câu). Có giải thích tiếng Việt.",descriptionEn="Original test: 10 grammar, 5 vocabulary, 1 reading passage (5 questions) with explanations.",
      descriptionJa="オリジナル練習：文法10問・語彙5問・読解1題（5問）。",descriptionRu="Авторский тест: 10 вопросов по грамматике, 5 по лексике, 1 текст (5 вопросов).",
      jlptTotalPassScore=0,jlptSectionPassScore=0,scoreGroups=[],sections=eng_secs(eid,L,e)))
# ---- 12 tenses mixed test from the exercise bank
ex=json.load(open(os.path.join(ENG,'tense_exercises.json'),encoding='utf8'))['exercises']
tq=[];n=0
for x in ex:
    if x['type']=='MultipleChoice':
        n+=1; ch=x['choices']; tq.append(dict(id=f"en_tenses_12_mixed_1_c{n:02d}",type="MultipleChoice",passageId="",promptVi="Chọn dạng đúng của động từ. "+x['promptEn'],promptEn=x['promptEn'],promptJa="",promptReading="",
           choices=[dict(textVi=c,textEn=c,textJa="") for c in ch],correctChoiceIndex=x['correctChoiceIndex'],acceptedAnswers=[],explanationVi=x['explanation']['vi'],explanationEn=x['explanation']['en'],tags=["tenses",x['tense']],points=1,mondai=""))
for x in ex:
    if x['type']=='FillBlank' and x['order'] in (1,6):
        n+=1; tq.append(dict(id=f"en_tenses_12_mixed_1_f{n:02d}",type="FillBlank",passageId="",promptVi="Điền dạng đúng của động từ. "+x['promptEn'],promptEn=x['promptEn'],promptJa="",promptReading="",
           choices=[],correctChoiceIndex=-1,acceptedAnswers=x['acceptedAnswers'],explanationVi=x['explanation']['vi'],explanationEn=x['explanation']['en'],tags=["tenses",x['tense']],points=1,mondai=""))
exams.append(dict(schema=SCHEMA,id="en_tenses_12_mixed_1",examType="Ielts",family="English",level="A1-C1",learnerLevel="Basic to Advanced",recommendedBandMin=1,recommendedBandMax=7,
  titleVi="Kiểm tra tổng hợp 12 thì tiếng Anh",titleEn="12 English Tenses mixed test",titleJa="英語12時制 総合テスト",titleRu="Итоговый тест по 12 временам английского",
  descriptionVi="36 câu trắc nghiệm + 24 câu điền từ trích từ ngân hàng bài tập 12 thì.",descriptionEn="36 multiple-choice + 24 fill-in questions from the 12-tense exercise bank.",descriptionJa="12時制の練習問題から36問選択＋24問穴埋め。",descriptionRu="36 вопросов с выбором + 24 на заполнение пропусков из банка упражнений.",
  jlptTotalPassScore=0,jlptSectionPassScore=0,scoreGroups=[],sections=[dict(id="tenses",type="IeltsReading",titleVi="12 thì",titleEn="12 Tenses",titleJa="12時制",timeLimitSeconds=1800,scoreScaleMax=9,scoreGroup="",mondai=[],passages=[],questions=tq)]))
index=[]
for ex_ in exams:
    json.dump(ex_,open(os.path.join(OUT,ex_['id']+'.json'),'w',encoding='utf8'),ensure_ascii=False,indent=1)
    nq=sum(len(s['questions']) for s in ex_['sections'])
    index.append(dict(id=ex_['id'],family=ex_['family'],level=ex_['level'],questions=nq,file=ex_['id']))
json.dump(dict(schema=SCHEMA,exams=index),open(os.path.join(OUT,'index.json'),'w',encoding='utf8'),ensure_ascii=False,indent=1)
print(len(exams),'exams',sum(i['questions'] for i in index),'questions'); [print(i['id'],i['questions']) for i in index]