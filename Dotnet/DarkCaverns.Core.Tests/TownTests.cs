// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;
using DarkCaverns.Core.Game;
using DarkCaverns.Core.Mechanics;
using DarkCaverns.Core.World;
using NUnit.Framework;

namespace DarkCaverns.Core.Tests
{
    public class TownTests
    {
        [Test]
        public void TownHasEachStoreOnceAndEverythingIsReachable([Values(1UL, 2UL, 3UL, 42UL, 2026UL, 0xFEEDUL)] ulong seed)
        {
            var town = TownGenerator.Generate(seed);

            Assert.That(town.IsTown);
            Assert.That(town.Shops.Values.OrderBy(s => s), Is.EqualTo(Enum.GetValues(typeof(Shop)).Cast<Shop>()));
            Assert.That(town.Map[town.Exit], Is.EqualTo(Terrain.Exit));

            var distance = PathFinder.Distances(town.Width, town.Height, town.PassableMask(), town.Exit);
            foreach (var door in town.Shops.Keys)
            {
                Assert.That(town.Map[door], Is.EqualTo(Terrain.ShopEntrance));
                Assert.That(distance[door], Is.GreaterThan(0), $"store door {door} unreachable on seed {seed}");
            }

            for (var i = 0; i < town.Length; i++)
            {
                if (town.Map[i].IsPassable())
                {
                    Assert.That(distance[i], Is.GreaterThanOrEqualTo(0), $"cell {i} cut off on seed {seed}");
                }
            }
        }

        [Test]
        public void TownIsTheSameForTheSameSeed()
        {
            Assert.That(TownGenerator.Generate(9).Map, Is.EqualTo(TownGenerator.Generate(9).Map));
            Assert.That(TownGenerator.Generate(9).Map, Is.Not.EqualTo(TownGenerator.Generate(10).Map));
        }

        [Test]
        public void NewGameStartsInTownWithTheMapKnown()
        {
            var game = new GameSession(11);
            Assert.That(game.InTown);
            Assert.That(game.Region, Is.Null);
            Assert.That(game.LocationName, Is.EqualTo("Town"));
            Assert.That(game.Level.Visited, Is.All.True);
            Assert.That(game.MaxDepth, Is.EqualTo(0));
        }

        [Test]
        public void StairsLinkTownAndFloorOne()
        {
            var game = new GameSession(12);
            var town = game.Level;
            WalkTo(game, town.Exit);
            Assert.That(game.Depth, Is.EqualTo(1));

            var floor1 = game.Level;
            Reveal(floor1);
            WalkTo(game, floor1.Entrance);
            Assert.That(game.Level, Is.SameAs(town));
            Assert.That(Chebyshev(town, game.HeroCell, town.Exit), Is.EqualTo(1), "arrive beside the town stairs");
        }

        [Test]
        public void WalkingIntoAStoreDoorRaisesShopEntered()
        {
            var game = new GameSession(13);
            Shop? entered = null;
            game.ShopEntered += s => entered = s;

            var door = game.Level.Shops.First();
            WalkTo(game, door.Key);
            Assert.That(entered, Is.EqualTo(door.Value));
            Assert.That(game.Messages.Last(), Does.Contain(door.Value.Name()));
        }

        [Test]
        public void RecallTakesYouHomeAndBackToYourDeepestFloor()
        {
            var game = new GameSession(14, startDepth: 3);
            Assert.That(game.MaxDepth, Is.EqualTo(3));

            game.ReadWordOfRecall();
            Assert.That(game.RecallActive);
            var turns = RestUntilLevelChanges(game);
            Assert.That(turns, Is.InRange(16, 35), "15 + 1d20 turns");
            Assert.That(game.InTown);
            Assert.That(game.Messages, Has.Some.Contains("yanked upwards"));

            game.ReadWordOfRecall();
            RestUntilLevelChanges(game);
            Assert.That(game.Depth, Is.EqualTo(3));
            Assert.That(game.Messages, Has.Some.Contains("yanked downwards"));
        }

        [Test]
        public void ReadingRecallAgainCancelsIt()
        {
            var game = new GameSession(15, startDepth: 2);
            game.ReadWordOfRecall();
            game.ReadWordOfRecall();
            Assert.That(game.RecallActive, Is.False);
            for (var i = 0; i < 60; i++)
            {
                game.Rest();
            }

            Assert.That(game.Depth, Is.EqualTo(2));
        }

        [Test]
        public void RecallFromTownBeforeDescendingDoesNothing()
        {
            var game = new GameSession(16);
            game.ReadWordOfRecall();
            for (var i = 0; i < 60; i++)
            {
                game.Rest();
            }

            Assert.That(game.InTown);
            Assert.That(game.Messages.Last(), Does.Contain("not been anywhere"));
        }

        private static int RestUntilLevelChanges(GameSession game)
        {
            var level = game.Level;
            for (var turns = 1; turns <= 100; turns++)
            {
                game.Rest();
                if (game.Level != level)
                {
                    return turns;
                }
            }

            Assert.Fail("recall never fired");
            return -1;
        }

        private static void Reveal(Level level)
        {
            for (var i = 0; i < level.Length; i++)
            {
                level.Visited[i] = true;
            }
        }

        private static void WalkTo(GameSession game, int target)
        {
            var level = game.Level;
            var path = game.PathTo(target);
            Assert.That(path, Is.Not.Null, "no path");
            foreach (var cell in path)
            {
                Assert.That(game.TryMove(level.X(cell) - game.HeroX, level.Y(cell) - game.HeroY), Is.True);
                if (game.Level != level)
                {
                    return;
                }
            }
        }

        private static int Chebyshev(Level level, int a, int b) =>
            Math.Max(Math.Abs(level.X(a) - level.X(b)), Math.Abs(level.Y(a) - level.Y(b)));
    }
}
