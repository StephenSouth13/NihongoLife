import io, json, requests, psycopg2
R = r'D:\VTC_Academy\NihongoLife\NihongoLife\NihongoLife'
c = json.load(io.open(R + r'\LocalSecrets\secrets.json', encoding='utf-8-sig'))
URL = c['NIHONGOLIFE_SUPABASE_URL'].rstrip('/'); KEY = c['NIHONGOLIFE_SUPABASE_ANON_KEY']; H = {'apikey': KEY, 'Content-Type': 'application/json'}
db = psycopg2.connect(c['NIHONGOLIFE_SUPABASE_DB_URL']); cur = db.cursor()
cur.execute(io.open(R + r'\supabase\migrations\20261031_speaking_request_reject.sql', encoding='utf-8').read()); db.commit()
cur.execute("notify pgrst, 'reload schema'"); db.commit()
print('migration applied')
tok = {}
for a in c['NIHONGOLIFE_PLAYTEST_ACCOUNTS']:
    j = requests.post(URL + '/auth/v1/token?grant_type=password', headers=H, json={'email': a['email'], 'password': a['password']}, timeout=20).json()
    tok[a['email']] = (j['access_token'], j['user']['id'])
T = tok['teacher01@nihongolife.test']; A = tok['playtest01@nihongolife.test']; B = tok['playtest02@nihongolife.test']
fn = URL + '/functions/v1/agora-classroom'
def call(who, body): return requests.post(fn, headers={**H, 'Authorization': 'Bearer ' + who[0]}, json=body, timeout=25).json()
def rpc(who, rid): r = requests.post(URL + '/rest/v1/rpc/reject_speaking_request', headers={**H, 'Authorization': 'Bearer ' + who[0]}, json={'request_id': rid}, timeout=20); return r.status_code, r.text.strip()
import time; time.sleep(2)
room = call(T, {'action': 'create'})['room']['id']
call(A, {'action': 'request_join', 'roomId': room}); call(B, {'action': 'request_join', 'roomId': room})
reqs = {q['student_id']: q['id'] for q in call(T, {'action': 'requests'})['requests']}
res = {}
res['student_cannot_reject'] = rpc(A, reqs[B[1]])
res['anon_cannot_reject'] = requests.post(URL + '/rest/v1/rpc/reject_speaking_request', headers={**H, 'Authorization': 'Bearer ' + KEY}, json={'request_id': reqs[B[1]]}, timeout=20).status_code
res['teacher_rejects_B'] = rpc(T, reqs[B[1]])
res['reject_twice'] = rpc(T, reqs[B[1]])
res['pending_after'] = len(call(T, {'action': 'requests'})['requests'])
ap = call(T, {'action': 'approve', 'requestId': reqs[A[1]]}); res['approve_A_still_works'] = ap.get('ok')
res['B_token'] = call(B, {'action': 'token', 'roomId': room}).get('ok')
call(T, {'action': 'close', 'roomId': room})
cur.execute("select student_id::text,status from speaking_room_requests where room_id=%s", (room,)); st = dict(cur.fetchall())
res['db_status'] = {'A': st.get(A[1]), 'B': st.get(B[1])}
print(json.dumps(res))
