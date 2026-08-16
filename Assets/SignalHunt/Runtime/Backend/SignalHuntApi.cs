using System;
using System.Collections;
using System.Text;
using SignalHunt.Core;
using SignalHunt.Replay;
using UnityEngine;
using UnityEngine.Networking;

namespace SignalHunt.Backend
{
    [Serializable]
    public sealed class LeaderboardEntry
    {
        public int rank;
        public string playerName;
        public int timeMs;
        public int score;
        public int collectedCount;
    }

    [Serializable]
    public sealed class LeaderboardResult
    {
        public LeaderboardEntry[] entries;
        public string error;
    }

    [Serializable]
    internal sealed class EnsureChallengeRequest
    {
        public string p_challenge_id;
        public string p_date_key;
        public int p_generation_seed;
        public string p_world_template;
        public DailyChallenge p_config;
    }

    [Serializable]
    internal sealed class SubmitRunRequest
    {
        public string p_challenge_id;
        public string p_player_name;
        public string p_install_id;
        public int p_time_ms;
        public int p_score;
        public int p_collected_count;
        public bool p_completed;
        public ReplayRun p_replay_data;
    }

    [Serializable]
    internal sealed class LeaderboardRequest
    {
        public string p_challenge_id;
        public int p_limit;
    }

    [Serializable]
    internal sealed class LeaderboardWireEntry
    {
        public int rank;
        public string player_name;
        public int time_ms;
        public int score;
        public int collected_count;
    }

    [Serializable]
    internal sealed class LeaderboardWireWrapper
    {
        public LeaderboardWireEntry[] items;
    }

    [Serializable]
    internal sealed class TopGhostWireEntry
    {
        public string player_name;
        public ReplayRun replay_data;
    }

    [Serializable]
    internal sealed class TopGhostWireWrapper
    {
        public TopGhostWireEntry[] items;
    }

    public sealed class SignalHuntApi : MonoBehaviour
    {
        private SignalHuntRuntimeConfig _config;
        private SupabaseSession _session;

        public bool IsConfigured => _config != null && _config.IsConfigured;

        public void Initialize()
        {
            _config = SignalHuntRuntimeConfig.Load();
            _session = new SupabaseSession(_config);
        }

        public IEnumerator SubmitAndFetchLeaderboard(
            DailyChallenge challenge,
            ReplayRun run,
            Action<LeaderboardResult> completed)
        {
            if (!IsConfigured)
            {
                completed?.Invoke(new LeaderboardResult { error = "Run saved locally · connect backend config for the daily leaderboard" });
                yield break;
            }

            yield return _session.EnsureAuthenticated();
            if (!string.IsNullOrEmpty(_session.Error))
            {
                completed?.Invoke(new LeaderboardResult { error = _session.Error });
                yield break;
            }

            var ensurePayload = new EnsureChallengeRequest
            {
                p_challenge_id = challenge.challengeId,
                p_date_key = challenge.dateKey,
                p_generation_seed = unchecked((int)challenge.generationSeed),
                p_world_template = challenge.worldTemplate,
                p_config = challenge
            };
            yield return PostRpc("signal_hunt_ensure_challenge", JsonUtility.ToJson(ensurePayload), null);

            var submitPayload = new SubmitRunRequest
            {
                p_challenge_id = challenge.challengeId,
                p_player_name = PlayerIdentity.DisplayName,
                p_install_id = PlayerIdentity.InstallId,
                p_time_ms = run.result.timeMs,
                p_score = run.result.score,
                p_collected_count = run.result.collectedCount,
                p_completed = run.result.completed,
                p_replay_data = run
            };
            string submitError = null;
            yield return PostRpc("signal_hunt_submit_run", JsonUtility.ToJson(submitPayload), error => submitError = error);
            if (!string.IsNullOrEmpty(submitError))
            {
                completed?.Invoke(new LeaderboardResult { error = submitError });
                yield break;
            }

            string leaderboardJson = null;
            string leaderboardError = null;
            yield return PostRpc("signal_hunt_leaderboard",
                JsonUtility.ToJson(new LeaderboardRequest { p_challenge_id = challenge.challengeId, p_limit = 10 }),
                error => leaderboardError = error,
                body => leaderboardJson = body);
            if (!string.IsNullOrEmpty(leaderboardError))
            {
                completed?.Invoke(new LeaderboardResult { error = leaderboardError });
                yield break;
            }

            completed?.Invoke(ParseLeaderboard(leaderboardJson));
        }

        public IEnumerator FetchTopGhost(DailyChallenge challenge, Action<ReplayRun, string> completed)
        {
            if (!IsConfigured)
            {
                completed?.Invoke(null, null);
                yield break;
            }

            yield return _session.EnsureAuthenticated();
            if (!string.IsNullOrEmpty(_session.Error))
            {
                completed?.Invoke(null, _session.Error);
                yield break;
            }

            var ensurePayload = new EnsureChallengeRequest
            {
                p_challenge_id = challenge.challengeId,
                p_date_key = challenge.dateKey,
                p_generation_seed = unchecked((int)challenge.generationSeed),
                p_world_template = challenge.worldTemplate,
                p_config = challenge
            };
            string ensureError = null;
            yield return PostRpc("signal_hunt_ensure_challenge", JsonUtility.ToJson(ensurePayload), error => ensureError = error);
            if (!string.IsNullOrEmpty(ensureError))
            {
                completed?.Invoke(null, ensureError);
                yield break;
            }

            string body = null;
            string ghostError = null;
            yield return PostRpc("signal_hunt_top_ghost",
                JsonUtility.ToJson(new LeaderboardRequest { p_challenge_id = challenge.challengeId, p_limit = 1 }),
                error => ghostError = error,
                response => body = response);
            if (!string.IsNullOrEmpty(ghostError))
            {
                completed?.Invoke(null, ghostError);
                yield break;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<TopGhostWireWrapper>($"{{\"items\":{body}}}");
                var entry = wrapper?.items != null && wrapper.items.Length > 0 ? wrapper.items[0] : null;
                if (entry?.replay_data != null)
                {
                    entry.replay_data.playerName = entry.player_name;
                }
                completed?.Invoke(entry?.replay_data, null);
            }
            catch (Exception exception)
            {
                completed?.Invoke(null, $"Top ghost could not be read: {exception.Message}");
            }
        }

        private IEnumerator PostRpc(string rpc, string json, Action<string> error, Action<string> success = null)
        {
            var url = $"{_config.supabaseUrl.TrimEnd('/')}/rest/v1/rpc/{rpc}";
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", _config.supabaseAnonKey);
            request.SetRequestHeader("Authorization", $"Bearer {_session.AccessToken}");
            request.timeout = 25;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                error?.Invoke($"Daily network unavailable ({request.responseCode}); run remains saved locally.");
                yield break;
            }

            success?.Invoke(request.downloadHandler.text);
        }

        private static LeaderboardResult ParseLeaderboard(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<LeaderboardWireWrapper>($"{{\"items\":{json}}}");
                var wire = wrapper?.items ?? Array.Empty<LeaderboardWireEntry>();
                var entries = new LeaderboardEntry[wire.Length];
                for (var index = 0; index < wire.Length; index++)
                {
                    entries[index] = new LeaderboardEntry
                    {
                        rank = wire[index].rank,
                        playerName = wire[index].player_name,
                        timeMs = wire[index].time_ms,
                        score = wire[index].score,
                        collectedCount = wire[index].collected_count
                    };
                }

                return new LeaderboardResult { entries = entries };
            }
            catch (Exception exception)
            {
                return new LeaderboardResult { error = $"Leaderboard response could not be read: {exception.Message}" };
            }
        }
    }
}
