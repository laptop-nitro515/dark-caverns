// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System;

namespace DarkCaverns.Core.Random
{
    /// <summary>
    /// Seeded random number generator (xoshiro128**). Each level, the item
    /// flavours and so on get their own instance, so a seed always rebuilds
    /// the same dungeon no matter what the player did elsewhere.
    /// </summary>
    public sealed class Rng
    {
        private uint s0, s1, s2, s3;

        public Rng(ulong seed)
        {
            // SplitMix64 spreads a single seed over the four state words.
            var x = seed;
            s0 = (uint) SplitMix(ref x);
            s1 = (uint) SplitMix(ref x);
            s2 = (uint) SplitMix(ref x);
            s3 = (uint) SplitMix(ref x);
            if ((s0 | s1 | s2 | s3) == 0)
            {
                s0 = 1;
            }
        }

        private static ulong SplitMix(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            var z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        private static uint Rotl(uint x, int k) => (x << k) | (x >> (32 - k));

        /// <summary>Next raw 32-bit value.</summary>
        public uint NextUInt()
        {
            var result = Rotl(s1 * 5, 7) * 9;
            var t = s1 << 9;
            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;
            s2 ^= t;
            s3 = Rotl(s3, 11);
            return result;
        }

        /// <summary>Integer in [0, max). Unbiased (Lemire's method).</summary>
        public int Int(int max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max));
            }

            var range = (uint) max;
            var m = (ulong) NextUInt() * range;
            var low = (uint) m;
            if (low < range)
            {
                var threshold = (uint) (-(int) range) % range;
                while (low < threshold)
                {
                    m = (ulong) NextUInt() * range;
                    low = (uint) m;
                }
            }

            return (int) (m >> 32);
        }

        /// <summary>Integer in [min, max], both inclusive.</summary>
        public int IntRange(int min, int max) => min + Int(max - min + 1);

        /// <summary>Float in [0, 1).</summary>
        public float Float() => (NextUInt() >> 8) * (1f / (1 << 24));

        /// <summary>True with the given chance, 0 to 1.</summary>
        public bool Chance(float probability) => Float() < probability;

        /// <summary>A random element of the list.</summary>
        public T Pick<T>(System.Collections.Generic.IReadOnlyList<T> items) => items[Int(items.Count)];

        /// <summary>Fisher-Yates shuffle in place.</summary>
        public void Shuffle<T>(System.Collections.Generic.IList<T> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = Int(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        /// <summary>A new generator seeded from this one, for independent streams.</summary>
        public Rng Fork() => new Rng(((ulong) NextUInt() << 32) | NextUInt());
    }
}
