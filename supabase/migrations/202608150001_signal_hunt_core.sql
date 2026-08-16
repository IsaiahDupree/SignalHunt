begin;

create table if not exists public.signal_hunt_challenges (
    id uuid primary key default gen_random_uuid(),
    challenge_id text not null unique,
    challenge_date date not null,
    app_key text not null default 'signal-hunt' check (app_key in ('signal-hunt', 'waypoint-rally', 'waypoint-wings')),
    generation_seed integer not null,
    generation_version text not null,
    world_template text not null,
    config jsonb not null,
    created_at timestamptz not null default now()
);

create unique index if not exists signal_hunt_challenges_daily_app_idx
    on public.signal_hunt_challenges (challenge_date, app_key);

create table if not exists public.signal_hunt_profiles (
    user_id uuid primary key references auth.users(id) on delete cascade,
    display_name text not null check (char_length(display_name) between 3 and 24),
    install_id uuid not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists public.signal_hunt_runs (
    id uuid primary key default gen_random_uuid(),
    challenge_id text not null references public.signal_hunt_challenges(challenge_id) on delete cascade,
    user_id uuid not null references auth.users(id) on delete cascade,
    time_ms integer not null check (time_ms between 0 and 300000),
    score integer not null check (score >= 0),
    collected_count smallint not null check (collected_count between 0 and 50),
    completed boolean not null,
    replay_data jsonb not null,
    created_at timestamptz not null default now()
);

create index if not exists signal_hunt_runs_challenge_rank_idx
    on public.signal_hunt_runs (challenge_id, completed desc, time_ms asc, score desc);
create index if not exists signal_hunt_runs_user_idx
    on public.signal_hunt_runs (user_id, created_at desc);

create table if not exists public.signal_hunt_cosmetics (
    cosmetic_id text primary key,
    app_key text not null check (app_key in ('shared', 'signal-hunt', 'waypoint-rally', 'waypoint-wings')),
    category text not null check (category in ('vehicle', 'trail', 'nameplate', 'replay-camera', 'world-theme', 'victory')),
    display_name text not null,
    metadata jsonb not null default '{}'::jsonb,
    active boolean not null default true,
    created_at timestamptz not null default now()
);

create table if not exists public.signal_hunt_inventory (
    user_id uuid not null references auth.users(id) on delete cascade,
    cosmetic_id text not null references public.signal_hunt_cosmetics(cosmetic_id),
    acquired_at timestamptz not null default now(),
    source text not null,
    primary key (user_id, cosmetic_id)
);

alter table public.signal_hunt_challenges enable row level security;
alter table public.signal_hunt_profiles enable row level security;
alter table public.signal_hunt_runs enable row level security;
alter table public.signal_hunt_cosmetics enable row level security;
alter table public.signal_hunt_inventory enable row level security;

create policy "active cosmetics are readable"
    on public.signal_hunt_cosmetics for select
    to authenticated
    using (active);

create policy "players read own inventory"
    on public.signal_hunt_inventory for select
    to authenticated
    using (user_id = auth.uid());

create or replace function public.signal_hunt_ensure_challenge(
    p_challenge_id text,
    p_date_key text,
    p_generation_seed integer,
    p_world_template text,
    p_config jsonb
) returns void
language plpgsql
security definer
set search_path = public
as $$
declare
    v_date date := p_date_key::date;
begin
    if auth.uid() is null then
        raise exception 'authentication required';
    end if;
    if v_date < current_date - 1 or v_date > current_date + 1 then
        raise exception 'challenge date outside allowed window';
    end if;
    if p_challenge_id !~ ('^' || p_date_key || '_city_neon_standard_seed_[0-9]{5}$') then
        raise exception 'invalid challenge identity';
    end if;

    insert into public.signal_hunt_challenges (
        challenge_id, challenge_date, app_key, generation_seed, generation_version, world_template, config
    ) values (
        p_challenge_id, v_date, 'signal-hunt', p_generation_seed, 'town-generator-v1', p_world_template, p_config
    ) on conflict (challenge_id) do nothing;
end;
$$;

create or replace function public.signal_hunt_submit_run(
    p_challenge_id text,
    p_player_name text,
    p_install_id uuid,
    p_time_ms integer,
    p_score integer,
    p_collected_count integer,
    p_completed boolean,
    p_replay_data jsonb
) returns uuid
language plpgsql
security definer
set search_path = public
as $$
declare
    v_run_id uuid;
begin
    if auth.uid() is null then
        raise exception 'authentication required';
    end if;
    if char_length(trim(p_player_name)) not between 3 and 24 then
        raise exception 'invalid player name';
    end if;
    if jsonb_array_length(coalesce(p_replay_data->'frames', '[]'::jsonb)) not between 2 and 3600 then
        raise exception 'invalid replay frame count';
    end if;

    insert into public.signal_hunt_profiles (user_id, display_name, install_id)
    values (auth.uid(), trim(p_player_name), p_install_id)
    on conflict (user_id) do update
        set display_name = excluded.display_name,
            install_id = excluded.install_id,
            updated_at = now();

    insert into public.signal_hunt_runs (
        challenge_id, user_id, time_ms, score, collected_count, completed, replay_data
    ) values (
        p_challenge_id, auth.uid(), p_time_ms, p_score, p_collected_count::smallint, p_completed, p_replay_data
    ) returning id into v_run_id;

    return v_run_id;
end;
$$;

create or replace function public.signal_hunt_leaderboard(
    p_challenge_id text,
    p_limit integer default 10
) returns table (
    rank bigint,
    player_name text,
    time_ms integer,
    score integer,
    collected_count smallint
)
language sql
security definer
set search_path = public
as $$
    with personal_best as (
        select distinct on (r.user_id)
            r.user_id, r.time_ms, r.score, r.collected_count, r.completed
        from public.signal_hunt_runs r
        where r.challenge_id = p_challenge_id
        order by r.user_id, r.completed desc, r.time_ms asc, r.score desc
    ), ranked as (
        select
            row_number() over (order by b.completed desc, b.time_ms asc, b.score desc) as rank,
            p.display_name as player_name,
            b.time_ms,
            b.score,
            b.collected_count
        from personal_best b
        join public.signal_hunt_profiles p on p.user_id = b.user_id
    )
    select * from ranked order by rank limit least(greatest(p_limit, 1), 100);
$$;

create or replace function public.signal_hunt_top_ghost(p_challenge_id text, p_limit integer default 1)
returns table (player_name text, replay_data jsonb)
language sql
security definer
set search_path = public
as $$
    select p.display_name, r.replay_data
    from public.signal_hunt_runs r
    join public.signal_hunt_profiles p on p.user_id = r.user_id
    where r.challenge_id = p_challenge_id and r.completed
    order by r.time_ms asc, r.score desc
    limit least(greatest(p_limit, 1), 10);
$$;

revoke all on function public.signal_hunt_ensure_challenge(text, text, integer, text, jsonb) from public;
revoke all on function public.signal_hunt_submit_run(text, text, uuid, integer, integer, integer, boolean, jsonb) from public;
revoke all on function public.signal_hunt_leaderboard(text, integer) from public;
revoke all on function public.signal_hunt_top_ghost(text, integer) from public;

grant execute on function public.signal_hunt_ensure_challenge(text, text, integer, text, jsonb) to authenticated;
grant execute on function public.signal_hunt_submit_run(text, text, uuid, integer, integer, integer, boolean, jsonb) to authenticated;
grant execute on function public.signal_hunt_leaderboard(text, integer) to authenticated;
grant execute on function public.signal_hunt_top_ghost(text, integer) to authenticated;

commit;
