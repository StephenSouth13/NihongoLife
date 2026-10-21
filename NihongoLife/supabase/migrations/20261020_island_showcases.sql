-- Midori Island showcases: what friends see when they visit someone's island (IslandShowcaseService / IslandVisit).
-- layout_json holds the decorations and fields (IslandLayout as JSON); likes is maintained by like_island().

create table if not exists public.island_showcases (
    user_id      uuid primary key references auth.users (id) on delete cascade,
    display_name text not null default '',
    layout_json  text not null default '{}',
    likes        integer not null default 0,
    updated_at   timestamptz not null default now()
);

alter table public.island_showcases enable row level security;

drop policy if exists "island_showcases_read" on public.island_showcases;
create policy "island_showcases_read" on public.island_showcases
    for select to authenticated using (true);

drop policy if exists "island_showcases_insert_own" on public.island_showcases;
create policy "island_showcases_insert_own" on public.island_showcases
    for insert to authenticated with check (auth.uid() = user_id);

drop policy if exists "island_showcases_update_own" on public.island_showcases;
create policy "island_showcases_update_own" on public.island_showcases
    for update to authenticated using (auth.uid() = user_id) with check (auth.uid() = user_id);

-- Owners cannot set their own heart count: likes only changes inside like_island() (security definer, runs as owner).
create or replace function public.island_showcases_keep_likes()
returns trigger
language plpgsql
as $$
begin
    if current_user in ('authenticated', 'anon') then
        if tg_op = 'INSERT' then new.likes := 0; else new.likes := old.likes; end if;
    end if;
    new.updated_at := now();
    return new;
end;
$$;

drop trigger if exists island_showcases_keep_likes on public.island_showcases;
create trigger island_showcases_keep_likes
    before insert or update on public.island_showcases
    for each row execute function public.island_showcases_keep_likes();

create table if not exists public.island_likes (
    visitor_id uuid not null references auth.users (id) on delete cascade,
    owner_id   uuid not null references auth.users (id) on delete cascade,
    created_at timestamptz not null default now(),
    primary key (visitor_id, owner_id)
);

alter table public.island_likes enable row level security;

drop policy if exists "island_likes_read_own" on public.island_likes;
create policy "island_likes_read_own" on public.island_likes
    for select to authenticated using (auth.uid() = visitor_id or auth.uid() = owner_id);

-- One heart per visitor per island; returns the island's total.
create or replace function public.like_island(target uuid)
returns integer
language plpgsql
security definer
set search_path = public
as $$
declare
    total integer;
begin
    if auth.uid() is null or target is null or auth.uid() = target then
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
