using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SignalHunt.Backend
{
    [Serializable]
    internal sealed class AuthUser
    {
        public string id;
    }

    [Serializable]
    internal sealed class AuthResponse
    {
        public string access_token;
        public string refresh_token;
        public int expires_in;
        public AuthUser user;
    }

    [Serializable]
    internal sealed class RefreshRequest
    {
        public string refresh_token;
    }

    internal sealed class SupabaseSession
    {
        private const string AccessKey = "signalhunt.auth.access";
        private const string RefreshKey = "signalhunt.auth.refresh";
        private const string UserKey = "signalhunt.auth.user";
        private const string ExpiryKey = "signalhunt.auth.expiry";

        private readonly SignalHuntRuntimeConfig _config;

        public string AccessToken { get; private set; }
        public string UserId { get; private set; }
        public string Error { get; private set; }

        public SupabaseSession(SignalHuntRuntimeConfig config)
        {
            _config = config;
            AccessToken = PlayerPrefs.GetString(AccessKey, string.Empty);
            UserId = PlayerPrefs.GetString(UserKey, string.Empty);
        }

        public IEnumerator EnsureAuthenticated()
        {
            Error = string.Empty;
            var expiryText = PlayerPrefs.GetString(ExpiryKey, "0");
            long.TryParse(expiryText, out var expiry);
            if (!string.IsNullOrEmpty(AccessToken) && expiry > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 120)
            {
                yield break;
            }

            var refreshToken = PlayerPrefs.GetString(RefreshKey, string.Empty);
            if (!string.IsNullOrEmpty(refreshToken))
            {
                yield return RequestSession(
                    $"{_config.supabaseUrl.TrimEnd('/')}/auth/v1/token?grant_type=refresh_token",
                    JsonUtility.ToJson(new RefreshRequest { refresh_token = refreshToken }));
                if (string.IsNullOrEmpty(Error))
                {
                    yield break;
                }
            }

            Error = string.Empty;
            yield return RequestSession($"{_config.supabaseUrl.TrimEnd('/')}/auth/v1/signup", "{}");
        }

        private IEnumerator RequestSession(string url, string json)
        {
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", _config.supabaseAnonKey);
            request.timeout = 15;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Error = $"Authentication unavailable ({request.responseCode}).";
                yield break;
            }

            var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
            if (response == null || string.IsNullOrEmpty(response.access_token))
            {
                Error = "Authentication returned an invalid session.";
                yield break;
            }

            AccessToken = response.access_token;
            UserId = response.user?.id ?? UserId;
            var expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Mathf.Max(60, response.expires_in);
            PlayerPrefs.SetString(AccessKey, AccessToken);
            PlayerPrefs.SetString(RefreshKey, response.refresh_token ?? string.Empty);
            PlayerPrefs.SetString(UserKey, UserId ?? string.Empty);
            PlayerPrefs.SetString(ExpiryKey, expiresAt.ToString());
            PlayerPrefs.Save();
        }
    }
}
