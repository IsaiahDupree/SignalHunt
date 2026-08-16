begin;

alter table public.signal_hunt_challenges
    drop constraint if exists signal_hunt_challenges_app_key_check;
alter table public.signal_hunt_challenges
    add constraint signal_hunt_challenges_app_key_check
    check (app_key in ('signal-hunt', 'treasure-hunt', 'waypoint-rally', 'waypoint-wings'));

alter table public.signal_hunt_cosmetics
    drop constraint if exists signal_hunt_cosmetics_app_key_check;
alter table public.signal_hunt_cosmetics
    add constraint signal_hunt_cosmetics_app_key_check
    check (app_key in ('shared', 'signal-hunt', 'treasure-hunt', 'waypoint-rally', 'waypoint-wings'));

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
    if p_app_key not in ('signal-hunt', 'treasure-hunt', 'waypoint-rally', 'waypoint-wings') then
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

revoke all on function public.daily_challenge_ensure(text, text, text, integer, text, text, jsonb) from public;
grant execute on function public.daily_challenge_ensure(text, text, text, integer, text, text, jsonb) to authenticated;

commit;
