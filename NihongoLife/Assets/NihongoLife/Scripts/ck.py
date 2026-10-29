import json,sys
sys.stdout.reconfigure(encoding='utf-8')
R=r'D:\VTC_Academy\NihongoLife\NihongoLife\NihongoLife\Assets\NihongoLife\Resources'
t=json.load(open(R+r'\Content\Localization\ru_table.json',encoding='utf-8-sig'))
x=json.load(open(R+r'\Content\Localization\ru_table_extra.json',encoding='utf-8-sig'))
print(type(t).__name__, len(t), type(x).__name__, len(x), list(t)[:2] if isinstance(t,dict) else t[:1])