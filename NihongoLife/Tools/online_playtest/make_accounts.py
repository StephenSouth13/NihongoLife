import json, io, secrets, string, uuid, psycopg2
P = r'D:\VTC_Academy\NihongoLife\NihongoLife\NihongoLife\LocalSecrets\secrets.json'
c = json.load(io.open(P, encoding='utf-8-sig'))
accts = c.get('NIHONGOLIFE_PLAYTEST_ACCOUNTS') or []
def pw():
    a = string.ascii_letters + string.digits
    return ''.join(secrets.choice(a) for _ in range(20)) + 'a9Z'
want = [(f'playtest0{i}@nihongolife.test', f'Playtest 0{i}', 'student') for i in range(1, 6)]
want.append(('teacher01@nihongolife.test', 'Teacher Playtest', 'teacher'))
known = {a['email']: a for a in accts}
conn = psycopg2.connect(c['NIHONGOLIFE_SUPABASE_DB_URL']); conn.autocommit = False
cur = conn.cursor()
cur.execute("select n.nspname from pg_proc p join pg_namespace n on n.oid=p.pronamespace where proname='gen_salt' limit 1")
ns = cur.fetchone()[0]
out = []
for email, name, role in want:
    a = known.get(email) or {'email': email, 'password': pw(), 'display_name': name, 'role': role}
    app = json.dumps({'provider': 'email', 'providers': ['email'], **({'role': 'teacher'} if role == 'teacher' else {})})
    meta = json.dumps({'display_name': name})
    cur.execute('select id from auth.users where email=%s', (email,))
    row = cur.fetchone()
    if row:
        uid = row[0]
        cur.execute(f"update auth.users set encrypted_password={ns}.crypt(%s,{ns}.gen_salt('bf')), email_confirmed_at=coalesce(email_confirmed_at,now()), raw_app_meta_data=%s::jsonb, raw_user_meta_data=%s::jsonb, updated_at=now() where id=%s", (a['password'], app, meta, uid))
    else:
        uid = str(uuid.uuid4())
        cur.execute(f"""insert into auth.users(instance_id,id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at,confirmation_token,recovery_token,email_change_token_new,email_change,is_anonymous)
          values('00000000-0000-0000-0000-000000000000',%s,'authenticated','authenticated',%s,{ns}.crypt(%s,{ns}.gen_salt('bf')),now(),%s::jsonb,%s::jsonb,now(),now(),'','','','',false)""", (uid, email, a['password'], app, meta))
        cur.execute("""insert into auth.identities(provider_id,user_id,identity_data,provider,last_sign_in_at,created_at,updated_at)
          values(%s,%s,%s::jsonb,'email',now(),now(),now())""", (uid, uid, json.dumps({'sub': uid, 'email': email, 'email_verified': True})))
    a['user_id'] = str(uid); out.append(a)
conn.commit()
c['NIHONGOLIFE_PLAYTEST_ACCOUNTS'] = out
io.open(P, 'w', encoding='utf-8').write(json.dumps(c, indent=2, ensure_ascii=False))
print('accounts ready:', [(a['email'], a['role']) for a in out])
