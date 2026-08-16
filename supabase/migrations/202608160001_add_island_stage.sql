begin;

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
    v_generation_version text;
begin
    if auth.uid() is null then
        raise exception 'authentication required';
    end if;
    if v_date < current_date - 1 or v_date > current_date + 1 then
        raise exception 'challenge date outside allowed window';
    end if;

    if p_challenge_id ~ ('^' || p_date_key || '_city_neon_standard_seed_[0-9]{5}$')
       and p_world_template = 'synthetic-town-v1' then
        v_generation_version := 'town-generator-v1';
    elsif p_challenge_id ~ ('^' || p_date_key || '_island_coastal_standard_seed_[0-9]{5}$')
          and p_world_template = 'synthetic-island-v1' then
        v_generation_version := 'island-generator-v1';
    else
        raise exception 'invalid challenge identity or world template';
    end if;

    insert into public.signal_hunt_challenges (
        challenge_id, challenge_date, app_key, generation_seed, generation_version, world_template, config
    ) values (
        p_challenge_id, v_date, 'signal-hunt', p_generation_seed, v_generation_version, p_world_template, p_config
    ) on conflict (challenge_id) do nothing;
end;
$$;

revoke all on function public.signal_hunt_ensure_challenge(text, text, integer, text, jsonb) from public;
grant execute on function public.signal_hunt_ensure_challenge(text, text, integer, text, jsonb) to authenticated;

commit;
