using System;
using SignalHunt.Core;

namespace WaypointRally.Core
{
    public enum RallyStage
    {
        HarborTown,
        Dustlands
    }

    public static class RallyDailyChallenge
    {
        public static DailyChallenge ForUtcDate(DateTime utcDate, RallyStage? forcedStage = null)
        {
            var date = utcDate.ToUniversalTime().Date;
            var stage = forcedStage ?? (date.DayOfYear % 2 == 0 ? RallyStage.HarborTown : RallyStage.Dustlands);
            var town = stage == RallyStage.HarborTown;
            var stageKey = town ? "harbor-town" : "dustlands";
            var displayName = town ? "Turbo Harbor" : "Dustlands Run";
            var theme = town ? "coastal" : "sunset";
            var template = town ? "rally-harbor-town-v1" : "rally-dustlands-v1";
            var dateKey = date.ToString("yyyy-MM-dd");
            var identity = $"{dateKey}|{DailyGameCatalog.WaypointRallyAppKey}|{stageKey}|{theme}|standard|{template}";
            var generationSeed = DailyChallenge.StableHash(identity);
            var displaySeed = (int)(generationSeed % 100000u);

            return new DailyChallenge
            {
                appKey = DailyGameCatalog.WaypointRallyAppKey,
                modeKey = DailyGameCatalog.Get(DailyGameCatalog.WaypointRallyAppKey).modeKey,
                challengeId = $"{dateKey}_{stageKey}_{theme}_standard_seed_{displaySeed:D5}",
                dateKey = dateKey,
                season = SeasonFor(date.Month),
                theme = theme,
                difficulty = "standard",
                worldTemplate = template,
                generationVersion = "rally-course-generator-v1",
                stageKey = stageKey,
                stageDisplayName = displayName,
                displaySeed = displaySeed,
                generationSeed = generationSeed,
                collectibleCount = 10
            };
        }

        public static DailyChallenge Today()
        {
            var stage = StageOverrideFromCommandLine();
            var overrideDate = UnityEngine.PlayerPrefs.GetString("waypointrally.challenge_date", string.Empty);
            if (DateTime.TryParse(overrideDate, out var parsed))
            {
                return ForUtcDate(DateTime.SpecifyKind(parsed, DateTimeKind.Utc), stage);
            }
            return ForUtcDate(DateTime.UtcNow, stage);
        }

        private static RallyStage? StageOverrideFromCommandLine()
        {
            var arguments = Environment.GetCommandLineArgs();
            var marker = Array.IndexOf(arguments, "--rally-stage");
            if (marker < 0 || marker + 1 >= arguments.Length)
            {
                return null;
            }
            return arguments[marker + 1].ToLowerInvariant() switch
            {
                "town" or "harbor-town" => RallyStage.HarborTown,
                "dust" or "dustlands" => RallyStage.Dustlands,
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
