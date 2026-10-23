-- Online play is for registered accounts (the game's OnlineAccess): an anonymous Supabase session is still a guest.
-- Anonymous sessions have the `authenticated` role too, so the island showcase policies and like_island() check the
-- JWT's is_anonymous claim. Reading showcases stays open to any signed-in session.

create or replace function public.is_registered()
returns boolean
language sql
stable
as $$
    select auth.uid() is not null and coalesce((auth.jwt() ->> 'is_anonymous')::boolean, false) = false;
$$;

drop policy if exists "island_showcases_insert_own" on public.island_showcases;
create policy "island_showcases_insert_own" on public.island_showcases
    for insert to authenticated with check (auth.uid() = user_id and public.is_registered());

drop policy if exists "island_showcases_update_own" on public.island_showcases;
create policy "island_showcases_update_own" on public.island_showcases
    for update to authenticated using (auth.uid() = user_id) with check (auth.uid() = user_id and public.is_registered());

-- One heart per visitor per island, registered visitors only; returns the island's total.
create or replace function public.like_island(target uuid)
returns integer
language plpgsql
security definer
set search_path = public
as $$
declare
    total integer;
begin
    if auth.uid() is null or target is null or auth.uid() = target or not public.is_registered() then
        select coalesce(likes, 0) into total from public.island_showcases where user_id = target;
        return coalesce(total, 0);
    end if;
    insert into public.island_likes (visitor_id, owner_id) values (auth.uid(), target)
        on conflict do nothing;
    update public.island_showcases
        set likes = (select count(*) from public.island_likes where owner_id = target)
        where user_id = target
        returning likes into total;
    return coalesce(total, 0);
end;
$$;

revoke all on function public.like_island(uuid) from public, anon;
grant execute on function public.like_island(uuid) to authenticated;
