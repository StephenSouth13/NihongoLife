# Agora voice Edge Function

Issues Agora RTC tokens for player voice rooms (area voice per world shard, chat groups, private calls).
Uses the same secrets as `agora-classroom`:

```bash
supabase secrets set AGORA_APP_ID=... AGORA_APP_CERTIFICATE=...   # once, shared with agora-classroom
supabase functions deploy agora-voice
```

Callers must send a Supabase user JWT (a normal or anonymous sign-in). Room names are restricted to
`nlv_…`, `nlg_…`, `nld_…`, so the function cannot be used to mint tokens for classroom channels.
