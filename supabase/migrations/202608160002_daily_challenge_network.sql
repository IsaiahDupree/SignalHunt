begin;

alter table public.signal_hunt_runs
    add column if not exists client_run_id uuid;

create unique index if not exists signal_hunt_runs_client_attempt_idx
    on public.signal_hunt_runs (user_id, client_run_id)
    where client_run_id is not null;

create or replace function public.daily_challenge_ensure(
    p_app_key text,
    p_challenge_id text,
    p_date_key text,
    p_generation_seed integer,
    p_generation_version text,
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
    if p_app_key not in ('signal-hunt', 'waypoint-rally', 'waypoint-wings') then
        raise exception 'invalid app key';
    end if;
    if v_date < current_date - 1 or v_date > current_date + 1 then
        raise exception 'challenge date outside allowed window';
    end if;
    if p_challenge_id !~ ('^' || p_date_key || '_[a-z0-9-]+_[a-z0-9-]+_[a-z0-9-]+_seed_[0-9]{5}$') then
        raise exception 'invalid challenge identity';
    end if;
    if p_world_template !~ '^[a-z0-9-]+-v[0-9]+$'
       or p_generation_version !~ '^[a-z0-9-]+-v[0-9]+$' then
        raise exception 'invalid generation contract';
    end if;

    insert into public.signal_hunt_challenges (
        challenge_id, challenge_date, app_key, generation_seed, generation_version, world_template, config
    ) values (
        p_challenge_id, v_date, p_app_key, p_generation_seed, p_generation_version, p_world_template, p_config
    ) on conflict (challenge_id) do nothing;

    if not exists (
        select 1 from public.signal_hunt_challenges c
        where c.challenge_id = p_challenge_id
          and c.challenge_date = v_date
          and c.app_key = p_app_key
          and c.generation_seed = p_generation_seed
          and c.generation_version = p_generation_version
          and c.world_template = p_world_template
    ) then
        raise exception 'challenge contract does not match stored seed';
    end if;
end;
$$;

create or replace function public.daily_challenge_submit_run(
    p_app_key text,
    p_challenge_id text,
    p_client_run_id uuid,
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
    if not exists (
        select 1 from public.signal_hunt_challenges c
        where c.challenge_id = p_challenge_id and c.app_key = p_app_key
    ) then
        raise exception 'unknown app challenge';
    end if;
    if char_length(trim(p_player_name)) not between 3 and 24 then
        raise exception 'invalid player name';
    end if;
    if p_time_ms not between 0 and 300000
       or p_score < 0
       or p_collected_count not between 0 and 50 then
        raise exception 'invalid run result';
    end if;
    if jsonb_array_length(coalesce(p_replay_data->'frames', '[]'::jsonb)) not between 2 and 3600 then
        raise exception 'invalid replay frame count';
    end if;
    if p_replay_data->>'challengeId' is distinct from p_challenge_id
       or p_replay_data->>'appKey' is distinct from p_app_key then
        raise exception 'replay envelope does not match challenge';
    end if;

    insert into public.signal_hunt_profiles (user_id, display_name, install_id)
    values (auth.uid(), trim(p_player_name), p_install_id)
    on conflict (user_id) do update
        set display_name = excluded.display_name,
            install_id = excluded.install_id,
            updated_at = now();

    insert into public.signal_hunt_runs (
        challenge_id, user_id, client_run_id, time_ms, score, collected_count, completed, replay_data
    ) values (
        p_challenge_id, auth.uid(), p_client_run_id, p_time_ms, p_score,
        p_collected_count::smallint, p_completed, p_replay_data
    ) on conflict (user_id, client_run_id) where client_run_id is not null do update
        set replay_data = excluded.replay_data
    returning id into v_run_id;

    return v_run_id;
end;
$$;

create or replace function public.daily_challenge_leaderboard(
    p_app_key text,
    p_challenge_id text,
    p_limit integer default 10
) returns table (
    rank bigint,
    player_name text,
    time_ms integer,
    score integer,
    collected_count smallint,
    attempt_count bigint
)
language sql
security definer
set search_path = public
as $$
    with eligible as (
        select r.*,
               count(*) over (partition by r.user_id) as attempt_count
        from public.signal_hunt_runs r
        join public.signal_hunt_challenges c on c.challenge_id = r.challenge_id
        where r.challenge_id = p_challenge_id and c.app_key = p_app_key
    ), personal_best as (
        select distinct on (r.user_id)
            r.user_id, r.time_ms, r.score, r.collected_count, r.completed, r.attempt_count
        from eligible r
        order by r.user_id, r.completed desc, r.score desc, r.time_ms asc
    ), ranked as (
        select
            row_number() over (order by b.completed desc, b.score desc, b.time_ms asc) as rank,
            p.display_name as player_name,
            b.time_ms,
            b.score,
            b.collected_count,
            b.attempt_count
        from personal_best b
        join public.signal_hunt_profiles p on p.user_id = b.user_id
    )
    select * from ranked order by rank limit least(greatest(p_limit, 1), 100);
$$;

create or replace function public.daily_challenge_montage(
    p_app_key text,
    p_challenge_id text,
    p_limit integer default 16
) returns table (
    rank bigint,
    run_id uuid,
    player_name text,
    replay_data jsonb,
    time_ms integer,
    score integer,
    attempt_count bigint
)
language sql
security definer
set search_path = public
as $$
    with eligible as (
        select r.*,
               count(*) over (partition by r.user_id) as attempt_count
        from public.signal_hunt_runs r
        join public.signal_hunt_challenges c on c.challenge_id = r.challenge_id
        where r.challenge_id = p_challenge_id and c.app_key = p_app_key
    ), personal_best as (
        select distinct on (r.user_id)
            r.id, r.user_id, r.replay_data, r.time_ms, r.score, r.completed, r.attempt_count
        from eligible r
        order by r.user_id, r.completed desc, r.score desc, r.time_ms asc
    ), ranked as (
        select
            row_number() over (order by b.completed desc, b.score desc, b.time_ms asc) as rank,
            b.id as run_id,
            p.display_name as player_name,
            b.replay_data,
            b.time_ms,
            b.score,
            b.attempt_count
        from personal_best b
        join public.signal_hunt_profiles p on p.user_id = b.user_id
    )
    select * from ranked order by rank limit least(greatest(p_limit, 1), 32);
$$;

revoke all on function public.daily_challenge_ensure(text, text, text, integer, text, text, jsonb) from public;
revoke all on function public.daily_challenge_submit_run(text, text, uuid, text, uuid, integer, integer, integer, boolean, jsonb) from public;
revoke all on function public.daily_challenge_leaderboard(text, text, integer) from public;
revoke all on function public.daily_challenge_montage(text, text, integer) from public;

grant execute on function public.daily_challenge_ensure(text, text, text, integer, text, text, jsonb) to authenticated;
grant execute on function public.daily_challenge_submit_run(text, text, uuid, text, uuid, integer, integer, integer, boolean, jsonb) to authenticated;
grant execute on function public.daily_challenge_leaderboard(text, text, integer) to authenticated;
grant execute on function public.daily_challenge_montage(text, text, integer) to authenticated;

commit;
