// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using DarkCaverns.Core.Random;
using NUnit.Framework;

namespace DarkCaverns.Core.Tests
{
    public class RandomTests
    {
        [Test]
        public void SameSeedGivesSameSequence()
        {
            var a = new Rng(1234);
            var b = new Rng(1234);
            for (var i = 0; i < 1000; i++)
            {
                Assert.That(a.NextUInt(), Is.EqualTo(b.NextUInt()));
            }
        }

        [Test]
        public void DifferentSeedsDiffer()
        {
            Assert.That(new Rng(1).NextUInt(), Is.Not.EqualTo(new Rng(2).NextUInt()));
        }

        [Test]
        public void IntStaysInRangeAndHitsEveryValue()
        {
            var rng = new Rng(42);
            var seen = new bool[7];
            for (var i = 0; i < 10000; i++)
            {
                var v = rng.Int(7);
                Assert.That(v, Is.InRange(0, 6));
                seen[v] = true;
            }

            Assert.That(seen, Is.All.True);
        }

        [Test]
        public void IntRejectsNonPositiveMax()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rng(1).Int(0));
        }

        [Test]
        public void FloatIsInUnitInterval()
        {
            var rng = new Rng(7);
            for (var i = 0; i < 10000; i++)
            {
                Assert.That(rng.Float(), Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [TestCase("2d6", 2, 6, 0)]
        [TestCase("1d8+2", 1, 8, 2)]
        [TestCase("3d4-1", 3, 4, -1)]
        [TestCase(" 10D10 ", 10, 10, 0)]
        public void DiceParse(string text, int count, int sides, int bonus)
        {
            Assert.That(Dice.Parse(text), Is.EqualTo(new Dice(count, sides, bonus)));
        }

        [TestCase("")]
        [TestCase("d6")]
        [TestCase("2x6")]
        [TestCase("2d")]
        [TestCase("2d6+")]
        public void DiceParseRejectsJunk(string text)
        {
            Assert.That(Dice.TryParse(text, out _), Is.False);
        }

        [Test]
        public void DiceRollsStayWithinBounds()
        {
            var dice = new Dice(3, 6, 2);
            var rng = new Rng(99);
            for (var i = 0; i < 5000; i++)
            {
                Assert.That(dice.Roll(rng), Is.InRange(dice.Min, dice.Max));
            }

            Assert.That(dice.Min, Is.EqualTo(5));
            Assert.That(dice.Max, Is.EqualTo(20));
            Assert.That(dice.ToString(), Is.EqualTo("3d6+2"));
        }
    }
}
