// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Globalization;

namespace DarkCaverns.Core.Random
{
    /// <summary>Moria-style dice such as 2d6 or 1d8+2.</summary>
    public readonly struct Dice : IEquatable<Dice>
    {
        public readonly int Count;
        public readonly int Sides;
        public readonly int Bonus;

        public Dice(int count, int sides, int bonus = 0)
        {
            if (count < 0 || sides < 0)
            {
                throw new ArgumentOutOfRangeException(count < 0 ? nameof(count) : nameof(sides));
            }

            Count = count;
            Sides = sides;
            Bonus = bonus;
        }

        public int Min => Sides == 0 ? Bonus : Count + Bonus;
        public int Max => Count * Sides + Bonus;

        public int Roll(Rng rng)
        {
            var sum = Bonus;
            for (var i = 0; i < Count && Sides > 0; i++)
            {
                sum += rng.IntRange(1, Sides);
            }

            return sum;
        }

        /// <summary>Parses "NdS", "NdS+B" or "NdS-B".</summary>
        public static Dice Parse(string text)
        {
            if (!TryParse(text, out var dice))
            {
                throw new FormatException($"Not a dice expression: '{text}'");
            }

            return dice;
        }

        public static bool TryParse(string text, out Dice dice)
        {
            dice = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var s = text.Trim().ToLowerInvariant();
            var d = s.IndexOf('d');
            if (d <= 0)
            {
                return false;
            }

            var signAt = s.IndexOfAny(new[] { '+', '-' }, d);
            var sidesText = signAt < 0 ? s.Substring(d + 1) : s.Substring(d + 1, signAt - d - 1);
            var bonus = 0;
            if (!int.TryParse(s.Substring(0, d), NumberStyles.None, CultureInfo.InvariantCulture, out var count)
                || !int.TryParse(sidesText, NumberStyles.None, CultureInfo.InvariantCulture, out var sides)
                || (signAt >= 0 && !int.TryParse(s.Substring(signAt), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out bonus)))
            {
                return false;
            }

            dice = new Dice(count, sides, bonus);
            return true;
        }

        public override string ToString() =>
            Bonus == 0 ? $"{Count}d{Sides}" : $"{Count}d{Sides}{(Bonus > 0 ? "+" : "")}{Bonus}";

        public bool Equals(Dice other) => Count == other.Count && Sides == other.Sides && Bonus == other.Bonus;
        public override bool Equals(object obj) => obj is Dice other && Equals(other);
        public override int GetHashCode() => (Count * 397 ^ Sides) * 397 ^ Bonus;
    }
}
