import json, sys
sys.path.insert(0,'/workspace'); from abc_to_notes import notes
import music21
NCC="https://filedn.eu/lXAoeBpTFiwFLer1TexITzk/ngocanchieu.org/20-nh%E1%BA%A1c-l%C3%BD/05-h%E1%BB%87-th%E1%BB%91ng-k%C3%BD-%C3%A2m-abc/nh%E1%BA%A1c-k%C3%BD-%C3%A2m-abc/index.html"
JC="https://trillian.mit.edu/~jc/music/abc/"
TRAD="Traditional anonymous folk melody (public domain); note data derived by NihongoLife from the reference transcription, released CC0"
new=[
 dict(id="ly-cay-da",file="LyCayDa",title="Lý cây đa (The Banyan Tree)",title_vi="Lý cây đa",title_ja="リー・カイ・ダー（ガジュマルの木）",title_ru="Ли кай да (Баньян)",composer="Dân ca quan họ Bắc Ninh (traditional)",key="C pentatonic",ts="2/4",bpm=88,diff=2,region="vietnam",instruments=["dan_tranh","sao_truc"],src=NCC),
 dict(id="ly-ngua-o",file="LyNguaO",title="Lý ngựa ô (The Black Horse)",title_vi="Lý ngựa ô",title_ja="リー・グア・オー（黒い馬）",title_ru="Ли нгыа о (Вороной конь)",composer="Dân ca Nam Bộ (traditional)",key="E♭ pentatonic",ts="2/4",bpm=110,diff=3,region="vietnam",instruments=["dan_tranh","dan_bau"],src=NCC),
 dict(id="ho-hui",file="HoHui",title="Hò hụi (Boatmen's Chant)",title_vi="Hò hụi",title_ja="ホー・フイ（舟歌）",title_ru="Хо хуй (песня лодочников)",composer="Dân ca vùng biển (traditional; collected by Dương Bích Hà)",key="D pentatonic",ts="2/4",bpm=84,diff=2,region="vietnam",instruments=["dan_bau","sao_truc"],src=NCC),
 dict(id="sakura-sakura-full",file="Sakura_Am",title="Sakura Sakura (full melody)",title_vi="Sakura Sakura (cả bài)",title_ja="さくら さくら（全曲）",title_ru="Сакура, сакура (полностью)",composer="Japanese traditional (Edo period)",key="A miyako-bushi",ts="4/4",bpm=66,diff=2,region="japan",instruments=["koto","shakuhachi"],src=JC+"Japan/Sakura_Am.abc"),
 dict(id="kojo-no-tsuki",file="KojoNoTsuki_Dm",title="Kōjō no Tsuki (Moon over the Ruined Castle)",title_vi="Trăng trên thành hoang (Kōjō no Tsuki)",title_ja="荒城の月",title_ru="Луна над разрушенным замком",composer="Rentarō Taki (1901), arr. Kōsaku Yamada (1917)",key="D minor",ts="4/4",bpm=60,diff=2,region="japan",instruments=["shakuhachi","koto"],src=JC+"Japan/KojoNoTsuki_Dm.abc",lic="Public domain (Taki d. 1903; Yamada's 1917 version, Yamada d. 1965, PD in Japan under life+50 before the 2018 extension and published before 1929 in the US); note data derived by NihongoLife, released CC0"),
 dict(id="soran-bushi",file="Sooranbushi_F",title="Sōran Bushi (Hokkaido fishermen's song)",title_vi="Sōran Bushi (hò kéo lưới Hokkaido)",title_ja="ソーラン節",title_ru="Соран-бущи",composer="Japanese folk song (Hokkaido)",key="F (min'yō scale)",ts="2/4",bpm=96,diff=2,region="japan",instruments=["shamisen","shakuhachi"],src=JC+"Japan/Sooranbushi_F.abc"),
 dict(id="takeda-no-komoriuta",file="TakedaNoKomoriuta",title="Takeda no Komoriuta (Lullaby of Takeda)",title_vi="Bài ru Takeda",title_ja="竹田の子守唄",title_ru="Колыбельная Такэды",composer="Japanese folk lullaby (Kyoto)",key="C♯ minor",ts="4/4",bpm=60,diff=1,region="japan",instruments=["shakuhachi","koto"],src=JC+"Japan/TakedaNoKomoriuta.abc"),
 dict(id="kalinka",file="Kalinka",title="Kalinka",title_vi="Kalinka",title_ja="カリンカ",title_ru="Калинка",composer="Ivan Larionov (1860)",key="D minor / F major",ts="2/4",bpm=120,diff=2,region="russia",instruments=["balalaika"],src=JC+"Russia/Kalinka.abc",lic="Public domain (Ivan Larionov, d. 1889); note data derived by NihongoLife, released CC0"),
 dict(id="korobeiniki",file="Korobeiniki",title="Korobeiniki (The Peddlers)",title_vi="Korobeiniki (Những người bán rong)",title_ja="コロブチカ",title_ru="Коробейники",composer="Russian folk song (words N. Nekrasov, 1861)",key="A minor",ts="4/4",bpm=132,diff=2,region="russia",instruments=["balalaika"],src="NihongoLife's own transcription of the traditional tune",lic="Traditional melody (public domain); note data transcribed by NihongoLife, released CC0"),
 dict(id="ochi-chyornye",file="OchiChorniya",title="Ochi Chyornye (Dark Eyes)",title_vi="Đôi mắt đen (Ochi Chyornye)",title_ja="黒い瞳",title_ru="Очи чёрные",composer="Florian Hermann, Valse Hommage (1843)",key="D minor",ts="3/4",bpm=100,diff=2,region="russia",instruments=["violin","balalaika"],src=JC+"Russia/OchiChorniya.abc",lic="Public domain (Florian Hermann, 19th century); note data derived by NihongoLife, released CC0"),
 dict(id="metelitsa",file="Metelitsa",title="Metelitsa (Snowstorm)",title_vi="Metelitsa (Bão tuyết)",title_ja="メチェリーツァ（吹雪）",title_ru="Метелица",composer="Russian folk dance",key="D minor",ts="4/4",bpm=112,diff=2,region="russia",instruments=["balalaika"],src=JC+"Russia/Metelitsa.abc"),
 dict(id="hopak",file="Hopak",title="Hopak (Kozachok)",title_vi="Hopak (vũ khúc Cô-dắc)",title_ja="ホパーク",title_ru="Гопак",composer="Ukrainian folk dance",key="G / C major",ts="2/4",bpm=116,diff=3,region="world",instruments=["violin","balalaika"],src=JC+"Russia/Hopak.abc"),
 dict(id="arirang",file="Arirang_G",title="Arirang",title_vi="Arirang",title_ja="アリラン",title_ru="Ариран",composer="Korean folk song",key="G major",ts="3/4",bpm=84,diff=2,region="world",instruments=["gayageum"],src=JC+"Korea/Arirang_G.abc"),
 dict(id="mo-li-hua",file="MoLiHua_F",title="Mò Lì Huā (Jasmine Flower)",title_vi="Hoa nhài (Mò Lì Huā)",title_ja="茉莉花（ジャスミンの花）",title_ru="Цветок жасмина",composer="Chinese folk song (Jiangsu)",key="F pentatonic",ts="4/4",bpm=72,diff=2,region="world",instruments=["erhu","sao_truc"],src=JC+"China/MoLiHua_F.abc"),
]
regions={"ode-to-joy":"classical","beethoven-5th-motif":"classical","minuet-in-g":"classical","fur-elise":"classical","prelude-in-c-bwv846":"classical","canon-in-d":"classical","greensleeves":"world","amazing-grace":"world","silent-night":"world","jingle-bells":"children","joy-to-the-world":"world","twinkle-twinkle":"children","mary-had-a-little-lamb":"children","hot-cross-buns":"children","kaeru-no-uta":"japan","frere-jacques":"children","london-bridge":"children","au-clair-de-la-lune":"children","old-macdonald":"children","good-morning-to-all":"children","row-row-row-your-boat":"children","sakura-sakura":"japan"}
extra={"sakura-sakura":["koto","shakuhachi"],"kaeru-no-uta":["piano","koto"],"canon-in-d":["piano","violin"],"greensleeves":["piano","guitar"],"ode-to-joy":["piano","violin"],"minuet-in-g":["piano","violin"]}
primary={"sakura-sakura":"koto"}
d=json.load(open('src/songs.json',encoding='utf-8'))
for s in d['songs']:
    s['region']=regions[s['id']]
    if s['id'] in primary: s['instrument']=primary[s['id']]
    s['instruments']=extra.get(s['id'],[s['instrument']])
    if s['instruments'][0]!=s['instrument']: s['instruments']=[s['instrument']]+[i for i in s['instruments'] if i!=s['instrument']]
have={s['id'] for s in d['songs']}
for n in new:
    assert n['id'] not in have
    ns=notes(f"/workspace/abc/{n['file']}.abc")
    for x in ns: x['note']=music21.pitch.Pitch(midi=x['midi']).nameWithOctave.replace('-','b')
    ns=[{'midi':x['midi'],'note':x['note'],'start':x['start'],'duration':x['duration']} for x in ns]
    d['songs'].append(dict(id=n['id'],title=n['title'],title_vi=n['title_vi'],title_ja=n['title_ja'],title_ru=n['title_ru'],composer=n['composer'],key=n['key'],time_signature=n['ts'],tempo_bpm=n['bpm'],difficulty=n['diff'],level="beginner" if n['diff']<=1 else "intermediate",instrument=n['instruments'][0],instruments=n['instruments'],region=n['region'],source=n['src'],notes_licence=n.get('lic',TRAD),notes=ns))
    print(n['id'],len(ns),min(x['midi'] for x in ns),max(x['midi'] for x in ns))
# note names consistent with existing? check one existing
print(d['songs'][0]['notes'][0])
json.dump(d,open('songs.out.json','w',encoding='utf-8',newline='\r\n'),ensure_ascii=False,indent=1)
print(len(d['songs']))
