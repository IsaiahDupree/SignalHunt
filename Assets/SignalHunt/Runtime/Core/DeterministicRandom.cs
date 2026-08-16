using System;

namespace SignalHunt.Core
{
    /// <summary>
    /// A tiny platform-stable PRNG. System.Random is intentionally avoided so a
    /// challenge generated on the server matches iOS, Android, and the editor.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private uint _state;

        public DeterministicRandom(uint seed)
        {
            _state = seed == 0 ? 0x6D2B79F5u : seed;
        }

        public uint NextUInt()
        {
            var value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            _state = value;
            return value;
        }

        public float NextFloat()
        {
            return (NextUInt() & 0x00FFFFFFu) / 16777216f;
        }

        public int Range(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumExclusive));
            }

            var span = (uint)(maximumExclusive - minimumInclusive);
            return minimumInclusive + (int)(NextUInt() % span);
        }

        public float Range(float minimumInclusive, float maximumInclusive)
        {
            return minimumInclusive + NextFloat() * (maximumInclusive - minimumInclusive);
        }

        public bool Chance(float probability)
        {
            return NextFloat() < Math.Clamp(probability, 0f, 1f);
        }
    }
}
