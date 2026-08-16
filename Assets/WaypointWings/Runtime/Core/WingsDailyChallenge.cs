using System;
using SignalHunt.Core;

namespace WaypointWings.Core
{
    public enum WingsStage
    {
        Archipelago,
        Skyway
    }

    public static class WingsDailyChallenge
    {
        public static DailyChallenge ForUtcDate(DateTime utcDate, WingsStage? forcedStage = null)
        {
            var date = utcDate.ToUniversalTime().Date;
            var stage = forcedStage ?? (date.DayOfYear % 2 == 0 ? WingsStage.Archipelago : WingsStage.Skyway);
            var stageKey = stage == WingsStage.Archipelago ? "archipelago" : "skyway";
            var displayName = stage == WingsStage.Archipelago ? "Sunrise Archipelago" : "Neon Skyway";
            var theme = stage == WingsStage.Archipelago ? "sunrise" : "neon";
            var template = stage == WingsStage.Archipelago ? "wings-archipelago-v1" : "wings-skyway-v1";
            var dateKey = date.ToString("yyyy-MM-dd");
            var identity = $"{dateKey}|{DailyGameCatalog.WaypointWingsAppKey}|{stageKey}|{theme}|standard|{template}";
            var generationSeed = DailyChallenge.StableHash(identity);
            var displaySeed = (int)(generationSeed % 100000u);

            return new DailyChallenge
            {
                appKey = DailyGameCatalog.WaypointWingsAppKey,
                modeKey = DailyGameCatalog.Get(DailyGameCatalog.WaypointWingsAppKey).modeKey,
                challengeId = $"{dateKey}_{stageKey}_{theme}_standard_seed_{displaySeed:D5}",
                dateKey = dateKey,
                season = SeasonFor(date.Month),
                theme = theme,
                difficulty = "standard",
                worldTemplate = template,
                generationVersion = "wings-course-generator-v1",
                stageKey = stageKey,
                stageDisplayName = displayName,
                displaySeed = displaySeed,
                generationSeed = generationSeed,
                collectibleCount = 14
            };
        }

        public static DailyChallenge Today()
        {
            var stage = StageOverrideFromCommandLine();
            var overrideDate = UnityEngine.PlayerPrefs.GetString("waypointwings.challenge_date", string.Empty);
            if (DateTime.TryParse(overrideDate, out var parsed))
            {
                return ForUtcDate(DateTime.SpecifyKind(parsed, DateTimeKind.Utc), stage);
            }
            return ForUtcDate(DateTime.UtcNow, stage);
        }

        private static WingsStage? StageOverrideFromCommandLine()
        {
            var arguments = Environment.GetCommandLineArgs();
            var marker = Array.IndexOf(arguments, "--wings-stage");
            if (marker < 0 || marker + 1 >= arguments.Length)
            {
                return null;
            }
            return arguments[marker + 1].ToLowerInvariant() switch
            {
                "archipelago" => WingsStage.Archipelago,
                "skyway" => WingsStage.Skyway,
                _ => null
            };
        }

        private static string SeasonFor(int month)
        {
            return month switch
            {
                12 or 1 or 2 => "winter",
                3 or 4 or 5 => "spring",
                6 or 7 or 8 => "summer",
                _ => "autumn"
            };
        }
    }
}
