using System;
using UnityEngine;

namespace SignalHunt.Backend
{
    public sealed class SignalHuntRuntimeConfig : ScriptableObject
    {
        public string supabaseUrl;
        public string supabaseAnonKey;

        public bool IsConfigured =>
            Uri.TryCreate(supabaseUrl, UriKind.Absolute, out _) &&
            !string.IsNullOrWhiteSpace(supabaseAnonKey);

        public static SignalHuntRuntimeConfig Load()
        {
            var asset = Resources.Load<SignalHuntRuntimeConfig>("SignalHuntRuntimeConfig");
            if (asset != null)
            {
                return asset;
            }

            var runtime = CreateInstance<SignalHuntRuntimeConfig>();
#if UNITY_EDITOR
            runtime.supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
            runtime.supabaseAnonKey = Environment.GetEnvironmentVariable("SIGNAL_HUNT_SUPABASE_ANON_KEY") ??
                                      Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
#endif
            return runtime;
        }
    }
}
