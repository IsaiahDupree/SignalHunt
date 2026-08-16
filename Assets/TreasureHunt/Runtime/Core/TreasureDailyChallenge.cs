using System;
using SignalHunt.Core;

namespace TreasureHunt.Core
{
    public enum TreasureStage
    {
        SunkenRuins,
        CrystalHollow
    }

    public static class TreasureDailyChallenge
    {
        public static DailyChallenge ForUtcDate(DateTime utcDate, TreasureStage? forcedStage = null)
        {
            var date = utcDate.ToUniversalTime().Date;
            var stage = forcedStage ?? (date.DayOfYear % 2 == 0 ? TreasureStage.SunkenRuins : TreasureStage.CrystalHollow);
            var ruins = stage == TreasureStage.SunkenRuins;
            var stageKey = ruins ? "sunken-ruins" : "crystal-hollow";
            var displayName = ruins ? "Sunken Ruins" : "Crystal Hollow";
            var theme = ruins ? "jungle" : "luminous";
            var template = ruins ? "treasure-sunken-ruins-v1" : "treasure-crystal-hollow-v1";
            var dateKey = date.ToString("yyyy-MM-dd");
            var identity = $"{dateKey}|{DailyGameCatalog.TreasureHuntAppKey}|{stageKey}|{theme}|standard|{template}";
            var generationSeed = DailyChallenge.StableHash(identity);
            var displaySeed = (int)(generationSeed % 100000u);

            return new DailyChallenge
            {
                appKey = DailyGameCatalog.TreasureHuntAppKey,
                modeKey = DailyGameCatalog.Get(DailyGameCatalog.TreasureHuntAppKey).modeKey,
                challengeId = $"{dateKey}_{stageKey}_{theme}_standard_seed_{displaySeed:D5}",
                dateKey = dateKey,
                season = SeasonFor(date.Month),
                theme = theme,
                difficulty = "standard",
                worldTemplate = template,
                generationVersion = "treasure-world-generator-v1",
                stageKey = stageKey,
                stageDisplayName = displayName,
                displaySeed = displaySeed,
                generationSeed = generationSeed,
                collectibleCount = 12
            };
        }

        public static DailyChallenge Today()
        {
            var stage = StageOverrideFromCommandLine();
            var commandLineDate = DateOverrideFromCommandLine();
            if (commandLineDate.HasValue)
            {
                return ForUtcDate(commandLineDate.Value, stage);
            }
            var overrideDate = UnityEngine.PlayerPrefs.GetString("treasurehunt.challenge_date", string.Empty);
            if (DateTime.TryParse(overrideDate, out var parsed))
            {
                return ForUtcDate(DateTime.SpecifyKind(parsed, DateTimeKind.Utc), stage);
            }
            return ForUtcDate(DateTime.UtcNow, stage);
        }

        private static DateTime? DateOverrideFromCommandLine()
        {
            var arguments = Environment.GetCommandLineArgs();
            var marker = Array.IndexOf(arguments, "--treasure-date");
            if (marker < 0 || marker + 1 >= arguments.Length ||
                !DateTime.TryParse(arguments[marker + 1], out var parsed))
            {
                return null;
            }
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }

        private static TreasureStage? StageOverrideFromCommandLine()
        {
            var arguments = Environment.GetCommandLineArgs();
            var marker = Array.IndexOf(arguments, "--treasure-stage");
            if (marker < 0 || marker + 1 >= arguments.Length)
            {
                return null;
            }
            return arguments[marker + 1].ToLowerInvariant() switch
            {
                "ruins" or "sunken-ruins" => TreasureStage.SunkenRuins,
                "crystals" or "crystal-hollow" => TreasureStage.CrystalHollow,
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
