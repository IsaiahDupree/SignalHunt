using System;
using System.Collections.Generic;

namespace SignalHunt.Core
{
    [Serializable]
    public sealed class DailyGameProfile
    {
        public string appKey;
        public string modeKey;
        public string displayName;
        public string playerVerb;
        public string replayHook;
        public bool usesVerticalMovement;
    }

    public static class DailyGameCatalog
    {
        public const string SignalHuntAppKey = "signal-hunt";
        public const string TreasureHuntAppKey = "treasure-hunt";
        public const string WaypointRallyAppKey = "waypoint-rally";
        public const string WaypointWingsAppKey = "waypoint-wings";

        private static readonly DailyGameProfile[] Profiles =
        {
            new()
            {
                appKey = SignalHuntAppKey,
                modeKey = "daily-hunt",
                displayName = "Signal Hunt",
                playerVerb = "Find every hidden signal",
                replayHook = "Search routes and missed-item reveals",
                usesVerticalMovement = false
            },
            new()
            {
                appKey = TreasureHuntAppKey,
                modeKey = "daily-treasure-hunt",
                displayName = "Treasure Hunter",
                playerVerb = "Find every hidden artifact",
                replayHook = "Search trails and missed-treasure reveals",
                usesVerticalMovement = false
            },
            new()
            {
                appKey = WaypointRallyAppKey,
                modeKey = "daily-race",
                displayName = "Waypoint Rally",
                playerVerb = "Drive the fastest checkpoint route",
                replayHook = "Ghost races and shortcut comparisons",
                usesVerticalMovement = false
            },
            new()
            {
                appKey = WaypointWingsAppKey,
                modeKey = "daily-flight",
                displayName = "Waypoint Wings",
                playerVerb = "Fly the fastest airborne path",
                replayHook = "Flight paths and closest-call cuts",
                usesVerticalMovement = true
            }
        };

        public static IReadOnlyList<DailyGameProfile> All => Profiles;

        public static DailyGameProfile Get(string appKey)
        {
            foreach (var profile in Profiles)
            {
                if (string.Equals(profile.appKey, appKey, StringComparison.Ordinal))
                {
                    return profile;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(appKey), appKey, "Unknown daily game app key.");
        }
    }
}
