-- Teachers can turn a join request down explicitly. Before this the agora-classroom Edge Function only rejected the
-- other pending requests when one student was approved. Database-only (an RPC), so no Edge Function redeploy is needed.
create or replace function public.reject_speaking_request(request_id uuid)
returns boolean
language plpgsql
security definer
set search_path = public
as $$
begin
    if auth.uid() is null or request_id is null or not public.is_registered() then
        return false;
    end if;
    update public.speaking_room_requests r
        set status = 'rejected'
        from public.speaking_rooms s
        where r.id = request_id and r.status = 'pending' and s.id = r.room_id and s.teacher_id = auth.uid();
    return found;
end;
$$;

revoke all on function public.reject_speaking_request(uuid) from public, anon;
grant execute on function public.reject_speaking_request(uuid) to authenticated;
