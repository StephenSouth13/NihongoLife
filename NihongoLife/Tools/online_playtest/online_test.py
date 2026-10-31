"""Live test against the real NihongoLife Supabase backend: 5 concurrent clients (presence, chat, pose, leave)
and the teacher-call (agora-classroom) flow. Secrets are read from LocalSecrets/secrets.json and never printed."""
import asyncio, json, os, sys, time, uuid, secrets as pysecrets
import requests, websockets

ROOT = r'D:\VTC_Academy\NihongoLife\NihongoLife\NihongoLife'
OUT = r'D:\VTC_Academy\_grok_tmp\online'
os.makedirs(OUT, exist_ok=True)
cfg = json.load(open(os.path.join(ROOT, 'LocalSecrets', 'secrets.json'), encoding='utf-8-sig'))
URL = cfg['NIHONGOLIFE_SUPABASE_URL'].rstrip('/'); KEY = cfg['NIHONGOLIFE_SUPABASE_ANON_KEY']
H = {'apikey': KEY, 'Content-Type': 'application/json'}
report = {'auth': [], 'realtime': {}, 'teacher': []}
def log(*a): print(*a, flush=True)

def auth_all():
    sessions = []
    for a in cfg['NIHONGOLIFE_PLAYTEST_ACCOUNTS']:
        r = requests.post(URL + '/auth/v1/token?grant_type=password', headers=H, json={'email': a['email'], 'password': a['password']}, timeout=20)
        j = r.json() if r.content else {}
        tok = j.get('access_token'); u = j.get('user') or {}
        report['auth'].append({'account': a['email'], 'http': r.status_code, 'session': bool(tok), 'anonymous': u.get('is_anonymous'),
                               'role': (u.get('app_metadata') or {}).get('role', 'student'), 'error': None if tok else str(j)[:160]})
        log('AUTH', a['email'], r.status_code, 'session' if tok else j)
        sessions.append({'name': a['display_name'], 'email': a['email'], 'token': tok, 'uid': u.get('id'), 'role': a['role']})
    return sessions

def gate(sessions):
    rpc = URL + '/rest/v1/rpc/is_registered'
    g = {}
    for s in sessions:
        r = requests.post(rpc, headers={**H, 'Authorization': 'Bearer ' + s['token']}, json={}, timeout=20); g[s['email']] = r.json()
    r = requests.post(rpc, headers={**H, 'Authorization': 'Bearer ' + KEY}, json={}, timeout=20); g['anon-key guest'] = r.json()
    report['gate'] = g; log('GATE', g)

class Client:
    def __init__(self, s, topic):
        self.s = s; self.topic = 'realtime:' + topic; self.ref = 0; self.joined = False
        self.presence = set(); self.chat = []; self.poses = []; self.errors = []
    def nref(self): self.ref += 1; return str(self.ref)
    async def run(self, start_evt, stop_evt, leave_evt=None):
        ws_url = URL.replace('https://', 'wss://') + '/realtime/v1/websocket?apikey=' + KEY + '&vsn=1.0.0'
        async with websockets.connect(ws_url, max_size=2**22) as ws:
            self.ws = ws
            jr = self.nref()
            await ws.send(json.dumps({'topic': self.topic, 'event': 'phx_join', 'ref': jr, 'join_ref': jr, 'payload': {
                'config': {'broadcast': {'self': False, 'ack': False}, 'presence': {'key': self.s['uid'], 'enabled': True}, 'postgres_changes': [], 'private': False},
                'access_token': self.s['token']}}))
            self.join_ref = jr
            reader = asyncio.create_task(self.read())
            await asyncio.sleep(0.1)
            await start_evt.wait()
            await self.send('presence', {'type': 'presence', 'event': 'track', 'payload': {'name': self.s['name'], 'scene': 'playtest', 'character': 'default'}})
            await asyncio.sleep(3)
            self.t_chat = time.time()
            await self.send('broadcast', {'type': 'broadcast', 'event': 'c', 'payload': {'from': self.s['uid'], 'name': self.s['name'], 'text': 'xin chao tu ' + self.s['name'], 't': time.time()}})
            for k in range(3):
                await self.send('broadcast', {'type': 'broadcast', 'event': 'p', 'payload': {'id': self.s['uid'], 'x': 100 * k, 'y': 0, 'z': 50, 'r': 90, 't': time.time()}})
                await asyncio.sleep(0.3)
            if leave_evt is not None:
                await asyncio.sleep(2); await ws.close(); leave_evt.set(); reader.cancel(); return
            await stop_evt.wait()
            reader.cancel()
    async def send(self, event, payload):
        await self.ws.send(json.dumps({'topic': self.topic, 'event': event, 'ref': self.nref(), 'join_ref': self.join_ref, 'payload': payload}))
    async def read(self):
        try:
            async for raw in self.ws:
                m = json.loads(raw); ev = m.get('event'); p = m.get('payload') or {}
                if ev == 'phx_reply' and m.get('ref') == self.join_ref:
                    self.joined = p.get('status') == 'ok'
                    if not self.joined: self.errors.append(str(p)[:200])
                elif ev == 'presence_state':
                    self.presence |= set(p.keys())
                elif ev == 'presence_diff':
                    self.presence |= set((p.get('joins') or {}).keys()); self.presence -= set((p.get('leaves') or {}).keys())
                elif ev == 'broadcast':
                    e = p.get('event'); pl = p.get('payload') or {}
                    if e == 'c': self.chat.append((pl.get('name'), time.time() - pl.get('t', time.time())))
                    if e == 'p': self.poses.append(pl.get('id'))
                elif ev in ('phx_error', 'system') and p.get('status') == 'error':
                    self.errors.append(str(p)[:200])
        except (asyncio.CancelledError, websockets.ConnectionClosed):
            pass

async def realtime(sessions):
    topic = 'nl:lobby:playtest-' + uuid.uuid4().hex[:4]  # same protocol as nl:lobby / w:{scene}:{n}, isolated topic
    clients = [Client(s, topic) for s in sessions]
    start, stop, left = asyncio.Event(), asyncio.Event(), asyncio.Event()
    tasks = [asyncio.create_task(c.run(start, stop, left if i == 4 else None)) for i, c in enumerate(clients)]
    await asyncio.sleep(2.5); start.set()
    await asyncio.sleep(6)
    peak = {c.s['name']: len(c.presence) for c in clients}
    await left.wait(); await asyncio.sleep(2.5)
    after = {c.s['name']: len(c.presence) for c in clients[:4]}
    stop.set(); await asyncio.gather(*tasks, return_exceptions=True)
    r = report['realtime']
    r['topic'] = topic
    r['joined'] = {c.s['name']: c.joined for c in clients}
    r['presence_peak'] = peak
    r['presence_after_one_left'] = after
    r['chat_received'] = {c.s['name']: sorted(n for n, _ in c.chat) for c in clients}
    lat = [d for c in clients for _, d in c.chat]
    r['chat_latency_ms'] = {'max': round(max(lat) * 1000) if lat else None, 'avg': round(sum(lat) / len(lat) * 1000) if lat else None}
    r['poses_received'] = {c.s['name']: len(c.poses) for c in clients}
    r['errors'] = {c.s['name']: c.errors for c in clients if c.errors}
    log('REALTIME', json.dumps(r, ensure_ascii=False, indent=1))

def teacher(students, t):
    fn = URL + '/functions/v1/agora-classroom'
    def call(s, body, expect):
        try:
            r = requests.post(fn, headers={**H, 'Authorization': 'Bearer ' + s['token']}, json=body, timeout=25)
            j = r.json() if r.content else {}
        except Exception as e:
            r, j = None, {'exception': type(e).__name__}
        code = r.status_code if r is not None else None
        ok = code == expect
        slim = {k: v for k, v in j.items() if k not in ('token', 'room', 'rooms', 'requests')} if isinstance(j, dict) else {}
        if isinstance(j, dict):
            if j.get('token'): slim['token'] = '<%d chars>' % len(j['token'])
            if j.get('room'): slim['room'] = {k: j['room'].get(k) for k in ('id', 'status', 'creator_role', 'student_id', 'teacher_id')}
            if 'rooms' in j: slim['rooms'] = len(j['rooms'])
            if 'requests' in j: slim['requests'] = len(j['requests'])
        report['teacher'].append({'step': body.get('_step'), 'who': s['name'], 'action': body.get('action'), 'http': code, 'expected': expect, 'pass': ok, 'resp': slim})
        log('PASS' if ok else 'FAIL', body.get('_step'), s['name'], body.get('action'), code, json.dumps(slim, ensure_ascii=False)[:220])
        return j if isinstance(j, dict) else {}
    a, b = students[0], students[1]
    # 1) no teacher online: the call rings nobody, no media token, student hangs up
    call(t, {'action': 'close', 'roomId': (call(t, {'action': 'mine', '_step': 'pre'}, 200).get('room') or {}).get('id') or '00000000-0000-0000-0000-000000000000', '_step': 'pre'}, 200)
    call(a, {'action': 'role', '_step': 'student role'}, 200)
    call(t, {'action': 'role', '_step': 'teacher role'}, 200)
    n = call(a, {'action': 'list', '_step': 'no-teacher: student sees teacher rooms'}, 200)
    room = call(a, {'action': 'create', '_step': 'student calls a teacher'}, 200).get('room', {}).get('id')
    call(a, {'action': 'token', 'roomId': room, '_step': 'no-teacher: no media token yet'}, 404)
    call(b, {'action': 'claim', 'roomId': room, '_step': 'another student cannot answer'}, 403)
    # 2) teacher sees the ringing call and answers it
    r = requests.post(fn, headers={**H, 'Authorization': 'Bearer ' + t['token']}, json={'action': 'list'}, timeout=25).json()
    report['ring_seen'] = room in [x.get('id') for x in (r.get('rooms') or [])]; log('RING seen by teacher:', report['ring_seen'])
    ans = call(t, {'action': 'claim', 'roomId': room, '_step': 'teacher accepts (claim)'}, 200)
    st = call(a, {'action': 'token', 'roomId': room, '_step': 'student gets connect token'}, 200)
    call(b, {'action': 'token', 'roomId': room, '_step': 'outsider denied token'}, 403)
    report['connect'] = {'same_channel': bool(ans.get('channelName')) and ans.get('channelName') == st.get('channelName'),
                         'distinct_uids': ans.get('uid') != st.get('uid'), 'expires_in': st.get('expiresIn')}
    call(students[2], {'action': 'claim', 'roomId': room, '_step': 'second teacher answer race (as student)'}, 403)
    call(a, {'action': 'close', 'roomId': room, '_step': 'student ends call'}, 200)
    call(a, {'action': 'token', 'roomId': room, '_step': 'token after end refused'}, 404)
    m = call(a, {'action': 'mine', '_step': 'no active call after end'}, 200)
    report['ended_clean'] = m.get('room') is None
    # 3) teacher-hosted room: two students request, teacher approves one, the other is rejected
    tr = call(t, {'action': 'create', '_step': 'teacher opens room'}, 200).get('room', {}).get('id')
    call(a, {'action': 'request_join', 'roomId': tr, '_step': 'student A requests'}, 200)
    call(b, {'action': 'request_join', 'roomId': tr, '_step': 'student B requests'}, 200)
    reqs = requests.post(fn, headers={**H, 'Authorization': 'Bearer ' + t['token']}, json={'action': 'requests'}, timeout=25).json().get('requests') or []
    report['teacher'].append({'step': 'teacher sees 2 requests', 'pass': len(reqs) == 2, 'resp': {'requests': len(reqs)}}); log('REQS', len(reqs))
    ra = next((r['id'] for r in reqs if r['student_id'] == a['uid']), None)
    call(a, {'action': 'approve', 'requestId': ra or 'x', '_step': 'student cannot approve'}, 403)
    call(t, {'action': 'approve', 'requestId': ra or 'x', '_step': 'teacher approves A'}, 200)
    call(a, {'action': 'token', 'roomId': tr, '_step': 'A connects'}, 200)
    call(b, {'action': 'token', 'roomId': tr, '_step': 'B (rejected) denied'}, 403)
    call(t, {'action': 'close', 'roomId': tr, '_step': 'teacher ends'}, 200)
    report['teacher_room'] = tr

class Listener:
    """Raw Realtime socket on several public topics; records server broadcasts (incoming-call notifications)."""
    def __init__(self, s, topics): self.s = s; self.topics = topics; self.got = []; self.ref = 0
    async def run(self, ready, stop):
        ws_url = URL.replace('https://', 'wss://') + '/realtime/v1/websocket?apikey=' + KEY + '&vsn=1.0.0'
        async with websockets.connect(ws_url, max_size=2**22) as ws:
            for t in self.topics:
                self.ref += 1
                await ws.send(json.dumps({'topic': 'realtime:' + t, 'event': 'phx_join', 'ref': str(self.ref), 'join_ref': str(self.ref), 'payload': {
                    'config': {'broadcast': {'self': False, 'ack': False}, 'presence': {'key': '', 'enabled': False}, 'postgres_changes': [], 'private': False},
                    'access_token': self.s['token']}}))
            async def read():
                async for raw in ws:
                    m = json.loads(raw)
                    if m.get('event') == 'broadcast':
                        p = m.get('payload') or {}; self.got.append((m.get('topic'), p.get('event'), p.get('payload') or {}))
            task = asyncio.create_task(read()); await asyncio.sleep(1.5); ready.set(); await stop.wait(); task.cancel()

async def notify_decline(students, t):
    fn = URL + '/functions/v1/agora-classroom'
    def post(s, body): r = requests.post(fn, headers={**H, 'Authorization': 'Bearer ' + s['token']}, json=body, timeout=25); return r.status_code, (r.json() if r.content else {})
    c, d = students[2], students[3]
    tl = Listener(t, ['nl:speaking', 'nl:teacher:' + t['uid']]); sl = Listener(c, ['nl:speaking'])
    ready1, ready2, stop = asyncio.Event(), asyncio.Event(), asyncio.Event()
    tasks = [asyncio.create_task(tl.run(ready1, stop)), asyncio.create_task(sl.run(ready2, stop))]
    await ready1.wait(); await ready2.wait()
    loop = asyncio.get_running_loop(); run = lambda s, b: loop.run_in_executor(None, post, s, b)
    checks = []
    def chk(name, ok, info=None): checks.append({'step': name, 'pass': bool(ok), 'info': info}); log('PASS' if ok else 'FAIL', name, info if info is not None else '')
    _, mine = await run(t, {'action': 'mine'})
    if mine.get('room'): await run(t, {'action': 'close', 'roomId': mine['room']['id']})
    _, tr = await run(t, {'action': 'create'}); room = (tr.get('room') or {}).get('id')
    code, j = await run(c, {'action': 'request_join', 'roomId': room}); rid = j.get('requestId')
    chk('request_join returns requestId + notified', code == 200 and rid and j.get('notified') is True, {'http': code, 'notified': j.get('notified')})
    await run(d, {'action': 'request_join', 'roomId': room})
    await asyncio.sleep(2.5)
    lob = [p for tp, e, p in tl.got if tp == 'realtime:nl:speaking' and e == 'request' and p.get('src') == 'server' and p.get('s') == c['uid']]
    per = [p for tp, e, p in tl.got if tp == 'realtime:nl:teacher:' + t['uid'] and e == 'call_request' and p.get('rid') == rid]
    chk('teacher gets lobby "request" (old clients) from server', lob and lob[0].get('ti') == t['uid'] and lob[0].get('rid') == rid, len(lob))
    chk('teacher gets per-teacher "call_request" ring', len(per) >= 1, len(per))
    code, j = await run(c, {'action': 'decline', 'requestId': rid}); chk('student cannot decline', code == 403, code)
    code, j = await run(t, {'action': 'decline', 'requestId': rid}); chk('teacher declines request', code == 200 and j.get('declined') and j.get('notified'), {'http': code, 'notified': j.get('notified')})
    code, j = await run(t, {'action': 'decline', 'requestId': rid}); chk('decline twice -> 409', code == 409, code)
    await asyncio.sleep(2.5)
    rej = [p for tp, e, p in sl.got if e == 'rejected' and p.get('s') == c['uid'] and p.get('r') == room]
    chk('student hears "rejected" broadcast', len(rej) >= 1, len(rej))
    _, rq = await run(t, {'action': 'requests'}); left = [r['student_id'] for r in rq.get('requests') or []]
    chk('declined request gone, other still pending', c['uid'] not in left and d['uid'] in left, len(left))
    reqs_d = [r['id'] for r in rq.get('requests') or [] if r['student_id'] == d['uid']]
    code, j = await run(t, {'action': 'reject', 'requestId': reqs_d[0] if reqs_d else 'x'}); chk('legacy alias "reject" works', code == 200, code)
    code, j = await run(t, {'action': 'decline'}); chk('decline without requestId -> 400', code == 400, code)
    await run(t, {'action': 'close', 'roomId': room})
    code, j = await run(c, {'action': 'create'}); sr = (j.get('room') or {}).get('id')
    await asyncio.sleep(2.5)
    op = [p for tp, e, p in tl.got if e == 'open' and p.get('src') == 'server' and p.get('r') == sr]
    chk('student call -> teacher gets "open" ring', code == 200 and j.get('notified') and len(op) >= 1, len(op))
    await run(c, {'action': 'close', 'roomId': sr})
    stop.set(); await asyncio.gather(*tasks, return_exceptions=True)
    report['notify_decline'] = checks

sessions = auth_all()
students = [s for s in sessions if s['role'] != 'teacher']; t = next(s for s in sessions if s['role'] == 'teacher')
if all(s['token'] for s in sessions):
    gate(sessions)
    asyncio.run(realtime(students))
    teacher(students, t)
    asyncio.run(notify_decline(students, t))
json.dump(report, open(os.path.join(OUT, 'report.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
tp = [x for x in report['teacher'] if 'pass' in x]; nd = report.get('notify_decline', [])
log('SUMMARY teacher-call %d/%d, notify+decline %d/%d' % (sum(x['pass'] for x in tp), len(tp), sum(x['pass'] for x in nd), len(nd)))
log('DONE')
