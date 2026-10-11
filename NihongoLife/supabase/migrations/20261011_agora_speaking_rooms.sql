create table if not exists public.speaking_rooms (
  id uuid primary key default gen_random_uuid(),
  student_id uuid references auth.users(id) on delete set null,
  student_name text not null default 'Học viên',
  student_avatar_url text,
  student_bio text,
  student_level integer not null default 1,
  student_xp integer not null default 0,
  teacher_id uuid references auth.users(id) on delete set null,
  teacher_name text,
  channel_name text not null unique,
  status text not null default 'waiting' check (status in ('waiting', 'matched', 'closed')),
  created_at timestamptz not null default now(),
  matched_at timestamptz,
  closed_at timestamptz
);

alter table public.speaking_rooms add column if not exists student_avatar_url text;
alter table public.speaking_rooms add column if not exists student_bio text;
alter table public.speaking_rooms add column if not exists student_level integer not null default 1;
alter table public.speaking_rooms add column if not exists student_xp integer not null default 0;
alter table public.speaking_rooms add column if not exists created_by uuid references auth.users(id) on delete set null;
alter table public.speaking_rooms add column if not exists creator_role text not null default 'student' check (creator_role in ('student', 'teacher'));
alter table public.speaking_rooms alter column student_id drop not null;

create unique index if not exists speaking_rooms_one_open_per_student
  on public.speaking_rooms(student_id) where status in ('waiting', 'matched');
create unique index if not exists speaking_rooms_one_active_per_teacher
  on public.speaking_rooms(teacher_id) where status = 'matched';
create index if not exists speaking_rooms_waiting_created
  on public.speaking_rooms(created_at) where status = 'waiting';

alter table public.speaking_rooms enable row level security;

-- Room mutations go through the Edge Function using the service role. This prevents a
-- modified game client from pretending to be a teacher or claiming somebody else's room.
revoke all on public.speaking_rooms from anon, authenticated;

create table if not exists public.speaking_room_requests (
  id uuid primary key default gen_random_uuid(),
  room_id uuid not null references public.speaking_rooms(id) on delete cascade,
  student_id uuid not null references auth.users(id) on delete cascade,
  student_name text not null,
  student_avatar_url text,
  student_bio text,
  student_level integer not null default 1,
  student_xp integer not null default 0,
  status text not null default 'pending' check (status in ('pending', 'accepted', 'rejected')),
  created_at timestamptz not null default now(),
  unique(room_id, student_id)
);
alter table public.speaking_room_requests enable row level security;
revoke all on public.speaking_room_requests from anon, authenticated;

