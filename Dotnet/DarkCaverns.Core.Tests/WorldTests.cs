// SPDX-License-Identifier: GPL-3.0-or-later

using System.Linq;
using DarkCaverns.Core.Mechanics;
using DarkCaverns.Core.World;
using NUnit.Framework;

namespace DarkCaverns.Core.Tests
{
    public class WorldTests
    {
        [Test]
        public void FortyFloorsInFiveRegions()
        {
            Assert.That(Region.DeepestFloor, Is.EqualTo(40));
            Assert.That(Region.ForDepth(1).Name, Is.EqualTo("Upper Mines"));
            Assert.That(Region.ForDepth(8).Index, Is.EqualTo(0));
            Assert.That(Region.ForDepth(9).Index, Is.EqualTo(1));
            Assert.That(Region.ForDepth(40).Name, Is.EqualTo("The Balrog's Pit"));
            Assert.That(Enumerable.Range(1, 40).Count(Region.IsBossFloor), Is.EqualTo(5));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Region.ForDepth(41));
        }

        [Test]
        public void GenerationIsDeterministic()
        {
            var a = LevelGenerator.Generate(555, 3);
            var b = LevelGenerator.Generate(555, 3);
            Assert.That(a.Map, Is.EqualTo(b.Map));
            Assert.That(a.Entrance, Is.EqualTo(b.Entrance));

            var other = LevelGenerator.Generate(555, 4);
            Assert.That(other.Map, Is.Not.EqualTo(a.Map));
        }

        [Test]
        public void EveryFloorIsFullyConnectedAndSealed([Range(1, 40)] int depth)
        {
            foreach (var seed in new ulong[] { 1, 2, 3, 0xDEADBEEF, 987654321 })
            {
                var level = LevelGenerator.Generate(seed, depth);
                var passable = level.PassableMask();

                Assert.That(level.Rooms.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(level.Map[level.Entrance], Is.EqualTo(Terrain.Entrance));
                Assert.That(level.Map[level.Exit], Is.EqualTo(Terrain.Exit));

                var distance = PathFinder.Distances(level.Width, level.Height, passable, level.Entrance);
                for (var i = 0; i < level.Length; i++)
                {
                    if (passable[i])
                    {
                        Assert.That(distance[i], Is.GreaterThanOrEqualTo(0), $"seed {seed} depth {depth}: cell {i} unreachable");
                    }
                }

                for (var x = 0; x < level.Width; x++)
                {
                    Assert.That(passable[level.Cell(x, 0)] || passable[level.Cell(x, level.Height - 1)], Is.False);
                }

                for (var y = 0; y < level.Height; y++)
                {
                    Assert.That(passable[level.Cell(0, y)] || passable[level.Cell(level.Width - 1, y)], Is.False);
                }
            }
        }

        [Test]
        public void DoorsSitBetweenTwoWalls()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var level = LevelGenerator.Generate(seed, 1);
                for (var i = 0; i < level.Length; i++)
                {
                    if (level.Map[i] != Terrain.Door)
                    {
                        continue;
                    }

                    int x = level.X(i), y = level.Y(i);
                    var horizontal = !level[x, y - 1].IsPassable() && !level[x, y + 1].IsPassable();
                    var vertical = !level[x - 1, y].IsPassable() && !level[x + 1, y].IsPassable();
                    Assert.That(horizontal || vertical, $"door at {x},{y} on seed {seed}");
                }
            }
        }
    }
}
