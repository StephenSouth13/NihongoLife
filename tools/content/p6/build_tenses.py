# -*- coding: utf-8 -*-
# Builds English tense course JSON: lessons, conjugation key, exercises. Run: python -X utf8 tools/content/p6/build_tenses.py
import json, os, sys, random, re
HERE=os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0,HERE)
from tenses_data import T
REPO=os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
OUT=os.path.join(REPO,'NihongoLife','Assets','NihongoLife','Resources','Content','English'); os.makedirs(OUT,exist_ok=True)

# base, 3sg, past, pp, ing, vi, ru, ja
V=[l.split('|') for l in """work|works|worked|worked|working|làm việc|работать|働く
play|plays|played|played|playing|chơi|играть|遊ぶ・する
study|studies|studied|studied|studying|học|учиться, изучать|勉強する
live|lives|lived|lived|living|sống|жить|住む
stop|stops|stopped|stopped|stopping|dừng lại|останавливаться|止まる
go|goes|went|gone|going|đi|идти, ехать|行く
eat|eats|ate|eaten|eating|ăn|есть|食べる
write|writes|wrote|written|writing|viết|писать|書く
take|takes|took|taken|taking|lấy, đi (xe)|брать, ехать на|取る・乗る
see|sees|saw|seen|seeing|nhìn thấy|видеть|見る
come|comes|came|come|coming|đến|приходить|来る
buy|buys|bought|bought|buying|mua|покупать|買う
make|makes|made|made|making|làm, chế biến|делать, готовить|作る
give|gives|gave|given|giving|đưa, tặng|давать|あげる
run|runs|ran|run|running|chạy|бегать|走る
read|reads|read|read|reading|đọc|читать|読む
learn|learns|learned|learned|learning|học (được)|учить, узнавать|学ぶ
drink|drinks|drank|drunk|drinking|uống|пить|飲む
meet|meets|met|met|meeting|gặp|встречать|会う
begin|begins|began|begun|beginning|bắt đầu|начинать(ся)|始まる
cook|cooks|cooked|cooked|cooking|nấu ăn|готовить|料理する
finish|finishes|finished|finished|finishing|hoàn thành|заканчивать|終える
sleep|sleeps|slept|slept|sleeping|ngủ|спать|眠る
travel|travels|traveled|traveled|traveling|đi du lịch|путешествовать|旅行する
swim|swims|swam|swum|swimming|bơi|плавать|泳ぐ
arrive|arrives|arrived|arrived|arriving|đến nơi|прибывать|到着する
save|saves|saved|saved|saving|tiết kiệm|копить, экономить|貯める
leave|leaves|left|left|leaving|rời đi|уходить, покидать|出発する
teach|teaches|taught|taught|teaching|dạy|преподавать|教える
wait|waits|waited|waited|waiting|chờ, đợi|ждать|待つ
call|calls|called|called|calling|gọi điện|звонить|電話する
help|helps|helped|helped|helping|giúp đỡ|помогать|手伝う""".split('\n')]
VD={v[0]:dict(base=v[0],third=v[1],past=v[2],pp=v[3],ing=v[4],vi=v[5],ru=v[6],ja=v[7]) for v in V}
assert len(VD)==len(V)==32
KEYVERBS=[v[0] for v in V]  # all 29 in the key

def cls(subj):
    s=subj.lower()
    if s=='i': return '1sg'
    if s in('you','we','they') or s.endswith('s') and s not in ('lan',) and ' ' in s and s.split()[-1].endswith('s'): return 'pl'
    return '3sg'
def plural_names(subj): return subj.lower() in ('my parents','the guests','the kids','the children','the staff')
def cl(subj):
    s=subj.lower()
    if s=='i': return '1sg'
    if s in('you','we','they') or plural_names(subj): return 'pl'
    return '3sg'

def parts(tense, subj, vb):
    """returns (aux_list_for_aff, rest_aff) forms: dict with aff,neg,q (without subject / tail).
    aff: verb phrase string; neg: verb phrase string; q: (aux, rest) so question = aux+' '+subj+' '+rest"""
    v=VD[vb]; c=cl(subj)
    be={'1sg':'am','3sg':'is','pl':'are'}[c]; was={'1sg':'was','3sg':'was','pl':'were'}[c]
    have='has' if c=='3sg' else 'have'; do='does' if c=='3sg' else 'do'
    if tense=='present_simple':   return (v['third'] if c=='3sg' else v['base'], f"{do} not {v['base']}", (do.capitalize(), v['base']))
    if tense=='present_continuous': return (f"{be} {v['ing']}", f"{be} not {v['ing']}", (be.capitalize(), v['ing']))
    if tense=='present_perfect':  return (f"{have} {v['pp']}", f"{have} not {v['pp']}", (have.capitalize(), v['pp']))
    if tense=='present_perfect_continuous': return (f"{have} been {v['ing']}", f"{have} not been {v['ing']}", (have.capitalize(), f"been {v['ing']}"))
    if tense=='past_simple':      return (v['past'], f"did not {v['base']}", ('Did', v['base']))
    if tense=='past_continuous':  return (f"{was} {v['ing']}", f"{was} not {v['ing']}", (was.capitalize(), v['ing']))
    if tense=='past_perfect':     return (f"had {v['pp']}", f"had not {v['pp']}", ('Had', v['pp']))
    if tense=='past_perfect_continuous': return (f"had been {v['ing']}", f"had not been {v['ing']}", ('Had', f"been {v['ing']}"))
    if tense=='future_simple':    return (f"will {v['base']}", f"will not {v['base']}", ('Will', v['base']))
    if tense=='future_continuous':return (f"will be {v['ing']}", f"will not be {v['ing']}", ('Will', f"be {v['ing']}"))
    if tense=='future_perfect':   return (f"will have {v['pp']}", f"will not have {v['pp']}", ('Will', f"have {v['pp']}"))
    if tense=='future_perfect_continuous': return (f"will have been {v['ing']}", f"will not have been {v['ing']}", ('Will', f"have been {v['ing']}"))
    raise KeyError(tense)

def contract(s):
    for a,b in (("does not ","doesn't "),("do not ","don't "),("did not ","didn't "),("will not ","won't "),("had not ","hadn't "),("has not ","hasn't "),("have not ","haven't "),("is not ","isn't "),("are not ","aren't "),("was not ","wasn't "),("were not ","weren't ")):
        if s.startswith(a): return b+s[len(a):]
    return s

TIDS=[t['id'] for t in T]
# ---------- conjugation key ----------
SUBJ=[('I','1sg'),('he','3sg'),('they','pl')]
key=[]
for vb in KEYVERBS:
    v=VD[vb]; rec=dict(verb=vb,forms=dict(base=v['base'],thirdPerson=v['third'],past=v['past'],pastParticiple=v['pp'],ing=v['ing']),
                      meaning=dict(vi=v['vi'],ru=v['ru'],ja=v['ja']),irregular=(v['past']!=v['base']+'ed' and not v['past'].endswith('ed')) or v['pp'] in ('run','come','read'),tenses={})
    for tid in TIDS:
        d={}
        for subj,_ in SUBJ:
            a,n,q=parts(tid,subj,vb)
            d[subj]=dict(affirmative=f"{subj} {a}",negative=f"{subj} {n}",negativeShort=f"{subj} {contract(n)}",question=f"{q[0]} {subj} {q[1]}?")
        rec['tenses'][tid]=d
    key.append(rec)
json.dump(dict(version=1,tenses=TIDS,subjects=[s for s,_ in SUBJ],verbCount=len(key),verbs=key),open(os.path.join(OUT,'verb_conjugation_key.json'),'w',encoding='utf8'),ensure_ascii=False,indent=1)
print('key verbs',len(key),'forms',len(key)*len(TIDS)*len(SUBJ)*4)
# ---------- exercises ----------
from exercises_spec import SPEC
CONFUSE={'present_simple':['present_continuous','past_simple','present_perfect'],'present_continuous':['present_simple','present_perfect_continuous','future_simple'],
 'present_perfect':['past_simple','present_perfect_continuous','present_simple'],'present_perfect_continuous':['present_perfect','present_continuous','past_perfect_continuous'],
 'past_simple':['present_perfect','past_continuous','past_perfect'],'past_continuous':['past_simple','past_perfect_continuous','present_continuous'],
 'past_perfect':['past_simple','present_perfect','past_perfect_continuous'],'past_perfect_continuous':['past_perfect','past_continuous','present_perfect_continuous'],
 'future_simple':['present_simple','future_continuous','future_perfect'],'future_continuous':['future_simple','present_continuous','future_perfect_continuous'],
 'future_perfect':['future_simple','future_perfect_continuous','present_perfect'],'future_perfect_continuous':['future_perfect','future_continuous','present_perfect_continuous']}
TN={t['id']:t for t in T}
INSTR={'fill':dict(vi="Điền dạng đúng của động từ trong ngoặc.",ru="Впишите правильную форму глагола в скобках.",ja="かっこの動詞を正しい形にしなさい。",en="Write the correct form of the verb in brackets."),
 'fill_neg':dict(vi="Điền dạng PHỦ ĐỊNH của động từ trong ngoặc.",ru="Впишите ОТРИЦАТЕЛЬНУЮ форму глагола в скобках.",ja="かっこの動詞を否定形にしなさい。",en="Write the NEGATIVE form of the verb in brackets."),
 'choice':dict(vi="Chọn đáp án đúng.",ru="Выберите правильный вариант.",ja="正しいものを選びなさい。",en="Choose the correct answer."),
 'reorder':dict(vi="Sắp xếp các từ thành câu hỏi đúng.",ru="Расставьте слова так, чтобы получился вопрос.",ja="語を並べ替えて疑問文を作りなさい。",en="Put the words in order to make a question.")}
def cap(s): return s[0].upper()+s[1:] if s else s
def build_sentence(prefix,subj,tail_phrase,tail):
    new_sentence = (prefix=='' or prefix.rstrip().endswith(('.','!','?')))
    s=cap(subj) if new_sentence else subj
    if subj=='I': s='I'
    return (prefix+s+' '+tail_phrase+(' '+tail if tail else '')).strip()+'.'
exercises=[]
for ti,tid in enumerate(TIDS):
    lines=[l for l in SPEC[tid].strip().split('\n') if l.strip()]
    assert len(lines)==10,(tid,len(lines))
    for k,l in enumerate(lines,1):
        prefix,subj,vb,tail,form=l.split('|'); assert vb in VD,vb
        a,n,q=parts(tid,subj,vb)
        meta=dict(verb=vb,tense=tid)
        ex=dict(id=f"tx_{tid}_{k:02d}",tense=tid,order=k,verb=vb)
        base_aff=build_sentence(prefix,subj,a,tail)
        if k<=6:
            neg=(k==6); ans=n if neg else a
            full=build_sentence(prefix,subj,ans,tail)
            blank=build_sentence(prefix,subj,'_____ ('+vb+')',tail)
            alts=[ans]+([contract(n)] if neg else [])
            ex.update(type='FillBlank',polarity='negative' if neg else 'affirmative',promptEn=blank,sentence=full,
                      acceptedAnswers=sorted(set(alts)),instruction=INSTR['fill_neg' if neg else 'fill'])
        elif k<=9:
            correct=a; opts=[correct]
            for ct in CONFUSE[tid]:
                x=parts(ct,subj,vb)[0]
                if x not in opts: opts.append(x)
            # make sure 4 distinct
            for ct in TIDS:
                if len(opts)>=4: break
                x=parts(ct,subj,vb)[0]
                if x not in opts: opts.append(x)
            opts=opts[:4]
            rnd=random.Random(f"{tid}{k}"); rnd.shuffle(opts)
            ex.update(type='MultipleChoice',polarity='affirmative',promptEn=build_sentence(prefix,subj,'_____',tail),sentence=base_aff,
                      choices=opts,correctChoiceIndex=opts.index(correct),instruction=INSTR['choice'])
        else:
            qs=(prefix+q[0]+' '+subj+' '+q[1]+(' '+tail if tail else '')+'?').strip()
            if prefix and not prefix.rstrip().endswith(('.','!','?')): qs=prefix+qs
            words=re.sub(r'\?$','',qs).split(' ')
            rnd=random.Random(f"{tid}ord"); sh=words[:]
            for _ in range(10):
                rnd.shuffle(sh)
                if sh!=words: break
            ex.update(type='Reorder',polarity='question',promptEn=' / '.join(sh),sentence=qs,tokens=sh,answerTokens=words,
                      acceptedAnswers=[qs],instruction=INSTR['reorder'])
        t=TN[tid]
        sig=tail if tail else ''
        ex['explanation']=dict(
          en=f"Tense: {t['name']['en']}. Form: {t['formula']['affirmative'] if ex['polarity']=='affirmative' else (t['formula']['negative'] if ex['polarity']=='negative' else t['formula']['question'])}. Answer: {ex['sentence']}",
          vi=f"Thì: {t['name']['vi']}. Cấu trúc: {t['formula']['affirmative'] if ex['polarity']=='affirmative' else (t['formula']['negative'] if ex['polarity']=='negative' else t['formula']['question'])}. Đáp án: {ex['sentence']} — nghĩa động từ: {VD[vb]['vi']}. Dấu hiệu: {t['signals'][0]}…",
          ru=f"Время: {t['name']['ru']}. Формула: {t['formula']['affirmative'] if ex['polarity']=='affirmative' else (t['formula']['negative'] if ex['polarity']=='negative' else t['formula']['question'])}. Ответ: {ex['sentence']} — глагол: {VD[vb]['ru']}. Маркеры: {t['signals'][0]}…",
          ja=f"時制：{t['name']['ja']}。形：{t['formula']['affirmative'] if ex['polarity']=='affirmative' else (t['formula']['negative'] if ex['polarity']=='negative' else t['formula']['question'])}。答え：{ex['sentence']}（動詞の意味：{VD[vb]['ja']}）。目印：{t['signals'][0]}…")
        exercises.append(ex)
json.dump(dict(version=1,count=len(exercises),perTense=10,exercises=exercises),open(os.path.join(OUT,'tense_exercises.json'),'w',encoding='utf8'),ensure_ascii=False,indent=1)
print('exercises',len(exercises))

# ---------- lessons ----------
CEFR={'present_simple':'A1','present_continuous':'A1','present_perfect':'A2','present_perfect_continuous':'B1','past_simple':'A1','past_continuous':'A2','past_perfect':'B1','past_perfect_continuous':'B2','future_simple':'A2','future_continuous':'B1','future_perfect':'B2','future_perfect_continuous':'C1'}
lessons=[]
for i,t in enumerate(T,1):
    l=dict(id=f"en_tense_{i:02d}_{t['id']}",order=i,tenseId=t['id'],cefr=CEFR[t['id']],
      title=dict(en=t['name']['en'],vi=t['name']['vi'],ru=t['name']['ru'],ja=t['name']['ja']),
      formula=t['formula'],usage=t['usage'],signalWords=t['signals'],examples=t['examples'],commonMistake=t['commonMistake'],
      conjugationFile='verb_conjugation_key.json',exerciseIds=[f"tx_{t['id']}_{k:02d}" for k in range(1,11)])
    lessons.append(l)
overview=dict(
 vi="12 thì cơ bản = 3 mốc thời gian (hiện tại, quá khứ, tương lai) × 4 dạng (đơn, tiếp diễn, hoàn thành, hoàn thành tiếp diễn). Đơn: thói quen/sự thật; tiếp diễn: đang diễn ra; hoàn thành: đã xong, có liên quan mốc; hoàn thành tiếp diễn: kéo dài liên tục đến mốc.",
 ru="12 основных времён = 3 момента (настоящее, прошедшее, будущее) × 4 вида (простое, длительное, совершённое, совершённо-длительное). Простое — привычки/факты; длительное — идёт в процессе; совершённое — завершено к моменту; совершённо-длительное — длилось до момента.",
 ja="基本の12時制＝3つの時間（現在・過去・未来）×4つの形（単純・進行・完了・完了進行）。単純＝習慣・事実、進行＝進行中、完了＝その時点で完了、完了進行＝その時点まで継続。",
 en="12 basic tenses = 3 times (present, past, future) × 4 aspects (simple, continuous, perfect, perfect continuous).")
json.dump(dict(version=1,courseId='en_tenses_12',title=dict(en='12 English Tenses',vi='12 thì trong tiếng Anh',ru='12 времён английского языка',ja='英語の12時制'),
   overview=overview,lessonCount=len(lessons),lessons=lessons,
   schemaNotes="lessons[].examples/usage/commonMistake/title are per-language objects (vi/ru/ja/en); exerciseIds reference tense_exercises.json; conjugation in verb_conjugation_key.json"),
   open(os.path.join(OUT,'tenses_course.json'),'w',encoding='utf8'),ensure_ascii=False,indent=1)
print('lessons',len(lessons))
