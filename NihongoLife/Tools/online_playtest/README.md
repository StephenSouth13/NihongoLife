# Online playtest (live Supabase)

Scripts that test the real backend with the playtest accounts. They read `LocalSecrets/secrets.json` (git-ignored) and never print secrets.

- `make_accounts.py` creates or refreshes 5 confirmed students (playtest01..05@nihongolife.test) and 1 teacher (teacher01@nihongolife.test, `app_metadata.role = "teacher"`) directly in `auth.users`/`auth.identities`, using `NIHONGOLIFE_SUPABASE_DB_URL`. The passwords go to `NIHONGOLIFE_PLAYTEST_ACCOUNTS` in the same file.
- `online_test.py` signs in all 6 accounts, checks `is_registered()` (registered-only gate), runs 5 concurrent realtime clients (presence, chat, positions, leave), and runs the teacher-call flow through `agora-classroom` (no teacher, ring, accept, connect tokens, end, request/approve/reject). It writes a report to `D:\VTC_Academy\_grok_tmp\online\report.json`.
- `reject_live.py` applies `supabase/migrations/20261031_speaking_request_reject.sql` and checks the teacher "decline" RPC.

Needs Python 3.11 with `requests`, `websockets` and `psycopg2-binary` installed.

- `online_test.py` also checks the server-side incoming-call notifications (Realtime broadcast `request`/`open`/`rejected` on `nl:speaking` and `call_request` on `nl:teacher:<teacherId>`) and the explicit `decline` action (alias `reject`) of `agora-classroom`.
- Deploy: `npx supabase functions deploy agora-classroom --project-ref pkxrtvlerjjiignmlebx --use-api` with `SUPABASE_ACCESS_TOKEN` loaded from `LocalSecrets/secrets.json` (never commit it).
