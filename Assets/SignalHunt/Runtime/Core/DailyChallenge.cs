using System;
using UnityEngine;

namespace SignalHunt.Core
{
    public enum WorldStage
    {
        City,
        Island
    }

    [Serializable]
    public sealed class DailyChallenge
    {
        public string challengeId;
        public string dateKey;
        public string season;
        public string theme;
        public string difficulty;
        public string worldTemplate;
        public string stageKey;
        public string stageDisplayName;
        public int displaySeed;
        public uint generationSeed;
        public int collectibleCount;

        public WorldStage Stage => stageKey == "island" ? WorldStage.Island : WorldStage.City;

        public static DailyChallenge ForUtcDate(DateTime utcDate, WorldStage? forcedStage = null)
        {
            var date = utcDate.ToUniversalTime().Date;
            const string difficulty = "standard";
            var stage = forcedStage ?? (date.DayOfYear % 2 == 0 ? WorldStage.Island : WorldStage.City);
            var stageKey = stage == WorldStage.Island ? "island" : "city";
            var stageDisplayName = stage == WorldStage.Island ? "Emerald Isle" : "Neon District";
            var theme = stage == WorldStage.Island ? "coastal" : "neon";
            var template = stage == WorldStage.Island ? "synthetic-island-v1" : "synthetic-town-v1";
            var season = SeasonFor(date.Month);
            var dateKey = date.ToString("yyyy-MM-dd");
            var identity = $"{dateKey}|{season}|{theme}|{difficulty}|{template}";
            var generationSeed = StableHash(identity);
            var displaySeed = (int)(generationSeed % 100000u);

            return new DailyChallenge
            {
                challengeId = $"{dateKey}_{stageKey}_{theme}_{difficulty}_seed_{displaySeed:D5}",
                dateKey = dateKey,
                season = season,
                theme = theme,
                difficulty = difficulty,
                worldTemplate = template,
                stageKey = stageKey,
                stageDisplayName = stageDisplayName,
                displaySeed = displaySeed,
                generationSeed = generationSeed,
                collectibleCount = 10
            };
        }

        public static DailyChallenge Today()
        {
            var forcedStage = StageOverrideFromCommandLine();
            var overrideDate = PlayerPrefs.GetString("signalhunt.challenge_date", string.Empty);
            if (DateTime.TryParse(overrideDate, out var parsed))
            {
                return ForUtcDate(DateTime.SpecifyKind(parsed, DateTimeKind.Utc), forcedStage);
            }

            return ForUtcDate(DateTime.UtcNow, forcedStage);
        }

        private static WorldStage? StageOverrideFromCommandLine()
        {
            var arguments = Environment.GetCommandLineArgs();
            var marker = Array.IndexOf(arguments, "--signalhunt-stage");
            if (marker < 0 || marker + 1 >= arguments.Length)
            {
                return null;
            }

            return arguments[marker + 1].ToLowerInvariant() switch
            {
                "city" => WorldStage.City,
                "island" => WorldStage.Island,
                _ => null
            };
        }

        public static uint StableHash(string value)
        {
            unchecked
            {
                const uint offset = 2166136261u;
                const uint prime = 16777619u;
                var hash = offset;
                foreach (var character in value)
                {
                    hash ^= character;
                    hash *= prime;
                }

                return hash;
            }
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
