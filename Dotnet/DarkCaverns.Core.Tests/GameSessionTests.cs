// SPDX-License-Identifier: GPL-3.0-or-later

using DarkCaverns.Core.Game;
using DarkCaverns.Core.World;
using NUnit.Framework;

namespace DarkCaverns.Core.Tests
{
    public class GameSessionTests
    {
        [Test]
        public void StartsOnFloorOneNextToTheEntrance()
        {
            var game = new GameSession(2026, startDepth: 1);
            Assert.That(game.Depth, Is.EqualTo(1));
            Assert.That(game.Level.Map[game.HeroCell], Is.EqualTo(Terrain.Floor));
            Assert.That(game.Level.Visible[game.HeroCell], Is.True);
            Assert.That(game.Level.Visible[game.Level.Entrance], Is.True);
            Assert.That(game.Messages[0], Does.Contain("Upper Mines"));
        }

        [Test]
        public void CannotWalkIntoWalls()
        {
            var game = new GameSession(5, startDepth: 1);
            var level = game.Level;
            var moved = 0;
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var before = game.HeroCell;
                    var target = level.Cell(game.HeroX + dx, game.HeroY + dy);
                    var expected = (dx != 0 || dy != 0) && level.Map[target].IsPassable();
                    var ok = game.TryMove(dx, dy);
                    Assert.That(ok, Is.EqualTo(expected), $"move {dx},{dy}");
                    if (ok)
                    {
                        moved++;
                        if (game.Level != level)
                        {
                            return; // stepped onto stairs
                        }

                        game.TryMove(-dx, -dy);
                    }

                    Assert.That(game.Level == level);
                    if (!ok)
                    {
                        Assert.That(game.HeroCell, Is.EqualTo(before));
                    }
                }
            }

            Assert.That(moved, Is.GreaterThan(0));
        }

        [Test]
        public void WalkingToTheExitDescendsAndStairsLeadBack()
        {
            var game = new GameSession(77, startDepth: 1);
            var first = game.Level;

            // Reveal the floor so the path planner can use it.
            for (var i = 0; i < first.Length; i++)
            {
                first.Visited[i] = true;
            }

            var path = game.PathTo(first.Exit);
            Assert.That(path, Is.Not.Null);
            foreach (var cell in path)
            {
                var dx = first.X(cell) - game.HeroX;
                var dy = first.Y(cell) - game.HeroY;
                Assert.That(game.TryMove(dx, dy), Is.True);
            }

            Assert.That(game.Depth, Is.EqualTo(2));

            // Floor 2's entrance leads back up to the same floor 1, beside its exit.
            var second = game.Level;
            for (var i = 0; i < second.Length; i++)
            {
                second.Visited[i] = true;
            }

            path = game.PathTo(second.Entrance);
            foreach (var cell in path)
            {
                game.TryMove(second.X(cell) - game.HeroX, second.Y(cell) - game.HeroY);
            }

            Assert.That(game.Level, Is.SameAs(first));
            int ex = first.X(first.Exit), ey = first.Y(first.Exit);
            Assert.That(System.Math.Max(System.Math.Abs(game.HeroX - ex), System.Math.Abs(game.HeroY - ey)), Is.EqualTo(1));
        }

        [Test]
        public void DoorsOpenOnlyWhileOccupied()
        {
            for (ulong seed = 1; seed < 50; seed++)
            {
                var game = new GameSession(seed, startDepth: 1);
                var level = game.Level;
                var door = System.Array.IndexOf(level.Map, Terrain.Door);
                if (door < 0)
                {
                    continue;
                }

                for (var i = 0; i < level.Length; i++)
                {
                    level.Visited[i] = true;
                }

                var path = game.PathTo(door);
                if (path == null)
                {
                    continue;
                }

                foreach (var cell in path)
                {
                    game.TryMove(level.X(cell) - game.HeroX, level.Y(cell) - game.HeroY);
                    if (game.Level != level)
                    {
                        break;
                    }
                }

                if (game.Level != level || game.HeroCell != door)
                {
                    continue;
                }

                Assert.That(level.Map[door], Is.EqualTo(Terrain.OpenDoor));
                Assert.That(level.Map[door].BlocksSight(), Is.False);
                return;
            }

            Assert.Fail("no seed produced a reachable door");
        }
    }
}
