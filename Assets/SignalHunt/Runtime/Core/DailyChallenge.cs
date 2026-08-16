using System;
using UnityEngine;

namespace SignalHunt.Core
{
    [Serializable]
    public sealed class DailyChallenge
    {
        public string challengeId;
        public string dateKey;
        public string season;
        public string theme;
        public string difficulty;
        public string worldTemplate;
        public int displaySeed;
        public uint generationSeed;
        public int collectibleCount;

        public static DailyChallenge ForUtcDate(DateTime utcDate)
        {
            var date = utcDate.ToUniversalTime().Date;
            const string theme = "neon";
            const string difficulty = "standard";
            const string template = "synthetic-town-v1";
            var season = SeasonFor(date.Month);
            var dateKey = date.ToString("yyyy-MM-dd");
            var identity = $"{dateKey}|{season}|{theme}|{difficulty}|{template}";
            var generationSeed = StableHash(identity);
            var displaySeed = (int)(generationSeed % 100000u);

            return new DailyChallenge
            {
                challengeId = $"{dateKey}_city_{theme}_{difficulty}_seed_{displaySeed:D5}",
                dateKey = dateKey,
                season = season,
                theme = theme,
                difficulty = difficulty,
                worldTemplate = template,
                displaySeed = displaySeed,
                generationSeed = generationSeed,
                collectibleCount = 10
            };
        }

        public static DailyChallenge Today()
        {
            var overrideDate = PlayerPrefs.GetString("signalhunt.challenge_date", string.Empty);
            if (DateTime.TryParse(overrideDate, out var parsed))
            {
                return ForUtcDate(DateTime.SpecifyKind(parsed, DateTimeKind.Utc));
            }

            return ForUtcDate(DateTime.UtcNow);
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
