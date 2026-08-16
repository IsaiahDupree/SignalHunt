using System;
using UnityEngine;

namespace SignalHunt.Backend
{
    public static class PlayerIdentity
    {
        private const string InstallIdKey = "signalhunt.install_id";
        private const string DisplayNameKey = "signalhunt.display_name";

        public static string InstallId
        {
            get
            {
                var value = PlayerPrefs.GetString(InstallIdKey, string.Empty);
                if (!Guid.TryParse(value, out _))
                {
                    value = Guid.NewGuid().ToString("D");
                    PlayerPrefs.SetString(InstallIdKey, value);
                    PlayerPrefs.Save();
                }

                return value;
            }
        }

        public static string DisplayName
        {
            get
            {
                var value = PlayerPrefs.GetString(DisplayNameKey, string.Empty).Trim();
                if (string.IsNullOrEmpty(value))
                {
                    value = $"HUNTER-{InstallId[..4].ToUpperInvariant()}";
                    PlayerPrefs.SetString(DisplayNameKey, value);
                    PlayerPrefs.Save();
                }

                return value;
            }
            set
            {
                var normalized = (value ?? string.Empty).Trim();
                if (normalized.Length is < 3 or > 24)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Player names must be 3–24 characters.");
                }

                PlayerPrefs.SetString(DisplayNameKey, normalized);
                PlayerPrefs.Save();
            }
        }
    }
}
