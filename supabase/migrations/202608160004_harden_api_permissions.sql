begin;

-- Do not rely on Supabase's legacy auto-exposure defaults. The game writes and
-- reads challenge data only through the validated SECURITY DEFINER RPCs below.
revoke all privileges on table public.signal_hunt_challenges from public, anon, authenticated;
revoke all privileges on table public.signal_hunt_profiles from public, anon, authenticated;
revoke all privileges on table public.signal_hunt_runs from public, anon, authenticated;
revoke all privileges on table public.signal_hunt_cosmetics from public, anon, authenticated;
revoke all privileges on table public.signal_hunt_inventory from public, anon, authenticated;

-- These are the only tables intentionally available through PostgREST. RLS
-- still limits cosmetics to active rows and inventory to auth.uid().
grant select on table public.signal_hunt_cosmetics to authenticated;
grant select on table public.signal_hunt_inventory to authenticated;

-- A catalog-only search path prevents caller-controlled objects in public from
-- shadowing built-ins used by SECURITY DEFINER functions. All application
-- relations and auth helpers in the function bodies are schema-qualified.
alter function public.signal_hunt_ensure_challenge(text, text, integer, text, jsonb)
    set search_path = pg_catalog;
alter function public.signal_hunt_submit_run(text, text, uuid, integer, integer, integer, boolean, jsonb)
    set search_path = pg_catalog;
alter function public.signal_hunt_leaderboard(text, integer)
    set search_path = pg_catalog;
alter function public.signal_hunt_top_ghost(text, integer)
    set search_path = pg_catalog;
alter function public.daily_challenge_ensure(text, text, text, integer, text, text, jsonb)
    set search_path = pg_catalog;
alter function public.daily_challenge_submit_run(text, text, uuid, text, uuid, integer, integer, integer, boolean, jsonb)
    set search_path = pg_catalog;
alter function public.daily_challenge_leaderboard(text, text, integer)
    set search_path = pg_catalog;
alter function public.daily_challenge_montage(text, text, integer)
    set search_path = pg_catalog;

-- Reassert an explicit allowlist even on projects that previously enabled
-- auto_expose_new_tables. The service-role grants, if any, are not changed.
revoke all privileges on function public.signal_hunt_ensure_challenge(text, text, integer, text, jsonb)
    from public, anon, authenticated;
revoke all privileges on function public.signal_hunt_submit_run(text, text, uuid, integer, integer, integer, boolean, jsonb)
    from public, anon, authenticated;
revoke all privileges on function public.signal_hunt_leaderboard(text, integer)
    from public, anon, authenticated;
revoke all privileges on function public.signal_hunt_top_ghost(text, integer)
    from public, anon, authenticated;
revoke all privileges on function public.daily_challenge_ensure(text, text, text, integer, text, text, jsonb)
    from public, anon, authenticated;
revoke all privileges on function public.daily_challenge_submit_run(text, text, uuid, text, uuid, integer, integer, integer, boolean, jsonb)
    from public, anon, authenticated;
revoke all privileges on function public.daily_challenge_leaderboard(text, text, integer)
    from public, anon, authenticated;
revoke all privileges on function public.daily_challenge_montage(text, text, integer)
    from public, anon, authenticated;

grant execute on function public.signal_hunt_ensure_challenge(text, text, integer, text, jsonb)
    to authenticated;
grant execute on function public.signal_hunt_submit_run(text, text, uuid, integer, integer, integer, boolean, jsonb)
    to authenticated;
grant execute on function public.signal_hunt_leaderboard(text, integer)
    to authenticated;
grant execute on function public.signal_hunt_top_ghost(text, integer)
    to authenticated;
grant execute on function public.daily_challenge_ensure(text, text, text, integer, text, text, jsonb)
    to authenticated;
grant execute on function public.daily_challenge_submit_run(text, text, uuid, text, uuid, integer, integer, integer, boolean, jsonb)
    to authenticated;
grant execute on function public.daily_challenge_leaderboard(text, text, integer)
    to authenticated;
grant execute on function public.daily_challenge_montage(text, text, integer)
    to authenticated;

commit;
