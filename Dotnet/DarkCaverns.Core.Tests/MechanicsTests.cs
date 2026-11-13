// SPDX-License-Identifier: GPL-3.0-or-later

using DarkCaverns.Core.Mechanics;
using NUnit.Framework;

namespace DarkCaverns.Core.Tests
{
    public class MechanicsTests
    {
        private const int W = 21, H = 21;

        [Test]
        public void OpenFieldOfViewIsRoundAndLimitedByDistance()
        {
            var fov = new bool[W * H];
            ShadowCaster.CastShadow(10, 10, W, fov, new bool[W * H], 5);

            Assert.That(fov[10 + 10 * W], Is.True);
            Assert.That(fov[15 + 10 * W], Is.True, "straight line at full distance");
            Assert.That(fov[16 + 10 * W], Is.False, "beyond distance");
            Assert.That(fov[15 + 15 * W], Is.False, "corner of the square is cut off");
            Assert.That(fov[13 + 13 * W], Is.True, "diagonal within the circle");
        }

        [Test]
        public void WallsCastShadows()
        {
            var blocking = new bool[W * H];
            for (var y = 0; y < H; y++)
            {
                blocking[12 + y * W] = true; // a wall running north to south
            }

            var fov = new bool[W * H];
            ShadowCaster.CastShadow(10, 10, W, fov, blocking, 8);

            Assert.That(fov[12 + 10 * W], Is.True, "the wall itself is seen");
            Assert.That(fov[13 + 10 * W], Is.False, "behind the wall is hidden");
            Assert.That(fov[5 + 10 * W], Is.True, "the open side is seen");
        }

        [Test]
        public void SeesToTheMapEdgeWithoutAWallBorder()
        {
            var fov = new bool[W * H];
            Assert.DoesNotThrow(() => ShadowCaster.CastShadow(0, 0, W, fov, new bool[W * H], 10));
            Assert.That(fov[5 + 0 * W], Is.True);
        }

        [Test]
        public void PathGoesAroundWalls()
        {
            var passable = new bool[W * H];
            for (var i = 0; i < passable.Length; i++)
            {
                passable[i] = true;
            }

            for (var y = 0; y < H - 1; y++)
            {
                passable[10 + y * W] = false; // wall with a gap at the bottom
            }

            var path = PathFinder.FindPath(W, H, passable, 5 + 5 * W, 15 + 5 * W);
            Assert.That(path, Is.Not.Null);
            Assert.That(path[path.Count - 1], Is.EqualTo(15 + 5 * W));
            Assert.That(path, Does.Contain(10 + (H - 1) * W), "must pass through the gap");

            var distance = PathFinder.Distances(W, H, passable, 5 + 5 * W);
            Assert.That(path.Count, Is.EqualTo(distance[15 + 5 * W]));
        }

        [Test]
        public void NoPathWhenWalledOff()
        {
            var passable = new bool[W * H];
            for (var i = 0; i < passable.Length; i++)
            {
                passable[i] = i % W != 10;
            }

            Assert.That(PathFinder.FindPath(W, H, passable, 5, 15), Is.Null);
        }
    }
}
