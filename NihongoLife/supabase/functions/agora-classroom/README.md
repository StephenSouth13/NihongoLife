# Agora classroom Edge Function

Deploy after applying `supabase/migrations/20261011_agora_speaking_rooms.sql`.

Set these server-side secrets (never put the certificate in Unity):

```bash
supabase secrets set AGORA_APP_ID=... AGORA_APP_CERTIFICATE=...
supabase functions deploy agora-classroom
```

Set a teacher through a trusted admin path, for example Supabase Dashboard user app metadata:

```json
{ "role": "teacher" }
```

Students keep the default role (`student`). The function validates the signed-in user and reads
`app_metadata.role`; editable `user_metadata` is deliberately not trusted for authorization.
