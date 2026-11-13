// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using DarkCaverns.Core.Mechanics;
using DarkCaverns.Core.Random;

namespace DarkCaverns.Core.World
{
    /// <summary>
    /// First-pass floor generator: rectangular rooms joined by corridors, with
    /// doors where corridors meet room walls. SPD's room builders and painters
    /// replace this in a later milestone; the Level it produces stays the same.
    /// </summary>
    public static class LevelGenerator
    {
        public const int DefaultWidth = 48;
        public const int DefaultHeight = 36;

        public static Level Generate(ulong gameSeed, int depth, int width = DefaultWidth, int height = DefaultHeight)
        {
            // Each floor has its own stream so floor N is the same however you got there.
            var rng = new Rng(gameSeed ^ ((ulong) depth * 0x9E3779B97F4A7C15UL));
            var level = new Level(width, height, depth);

            PlaceRooms(level, rng);
            ConnectRooms(level, rng);
            WallInCorridors(level);
            FixDoors(level);
            PlaceStairs(level, rng);

            return level;
        }

        private static void PlaceRooms(Level level, Rng rng)
        {
            var target = rng.IntRange(8, 12);
            for (var attempt = 0; attempt < 400 && level.Rooms.Count < target; attempt++)
            {
                var w = rng.IntRange(5, 11); // includes walls, so 3 to 9 floor cells across
                var h = rng.IntRange(5, 9);
                var left = rng.IntRange(1, level.Width - w - 1);
                var top = rng.IntRange(1, level.Height - h - 1);
                var room = new Room(left, top, left + w - 1, top + h - 1);

                var overlaps = false;
                foreach (var other in level.Rooms)
                {
                    if (room.Intersects(other, 2))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (overlaps)
                {
                    continue;
                }

                level.Rooms.Add(room);
                for (var y = room.Top; y <= room.Bottom; y++)
                {
                    for (var x = room.Left; x <= room.Right; x++)
                    {
                        level[x, y] = room.ContainsInterior(x, y) ? Terrain.Floor : Terrain.Wall;
                    }
                }
            }

            if (level.Rooms.Count < 2)
            {
                throw new InvalidOperationException("Level too small to place two rooms");
            }
        }

        private static void ConnectRooms(Level level, Rng rng)
        {
            var rooms = new List<Room>(level.Rooms);
            rooms.Sort((a, b) => a.CenterX.CompareTo(b.CenterX));

            // A chain through every room keeps the floor connected.
            for (var i = 0; i + 1 < rooms.Count; i++)
            {
                Corridor(level, rng, rooms[i], rooms[i + 1]);
            }

            // A few extra links make loops, so the player isn't always backtracking.
            var extra = rng.IntRange(1, 3);
            for (var i = 0; i < extra; i++)
            {
                var a = rng.Pick(rooms);
                var b = rng.Pick(rooms);
                if (!a.Equals(b))
                {
                    Corridor(level, rng, a, b);
                }
            }
        }

        private static void Corridor(Level level, Rng rng, Room a, Room b)
        {
            int x0 = a.CenterX, y0 = a.CenterY, x1 = b.CenterX, y1 = b.CenterY;
            if (rng.Chance(0.5f))
            {
                CarveLine(level, x0, y0, x1, y0);
                CarveLine(level, x1, y0, x1, y1);
            }
            else
            {
                CarveLine(level, x0, y0, x0, y1);
                CarveLine(level, x0, y1, x1, y1);
            }
        }

        private static void CarveLine(Level level, int x0, int y0, int x1, int y1)
        {
            int dx = Math.Sign(x1 - x0), dy = Math.Sign(y1 - y0);
            int x = x0, y = y0;
            while (true)
            {
                var t = level[x, y];
                if (t == Terrain.Empty)
                {
                    level[x, y] = Terrain.Floor;
                }
                else if (t == Terrain.Wall)
                {
                    level[x, y] = Terrain.Door;
                }

                if (x == x1 && y == y1)
                {
                    break;
                }

                x += dx;
                y += dy;
            }
        }

        private static void WallInCorridors(Level level)
        {
            for (var y = 0; y < level.Height; y++)
            {
                for (var x = 0; x < level.Width; x++)
                {
                    if (!level[x, y].IsPassable())
                    {
                        continue;
                    }

                    for (var ny = y - 1; ny <= y + 1; ny++)
                    {
                        for (var nx = x - 1; nx <= x + 1; nx++)
                        {
                            if (level.InBounds(nx, ny) && level[nx, ny] == Terrain.Empty)
                            {
                                level[nx, ny] = Terrain.Wall;
                            }
                        }
                    }
                }
            }

            // Nothing passable may touch the map edge.
            for (var x = 0; x < level.Width; x++)
            {
                SealEdge(level, x, 0);
                SealEdge(level, x, level.Height - 1);
            }

            for (var y = 0; y < level.Height; y++)
            {
                SealEdge(level, 0, y);
                SealEdge(level, level.Width - 1, y);
            }
        }

        private static void SealEdge(Level level, int x, int y)
        {
            if (level[x, y] != Terrain.Empty)
            {
                level[x, y] = Terrain.Wall;
            }
        }

        /// <summary>
        /// A door only makes sense between two walls with open ground either side.
        /// Corridors that ran along a wall leave doors that aren't; those become floor.
        /// </summary>
        private static void FixDoors(Level level)
        {
            for (var y = 1; y < level.Height - 1; y++)
            {
                for (var x = 1; x < level.Width - 1; x++)
                {
                    if (level[x, y] != Terrain.Door)
                    {
                        continue;
                    }

                    bool Solid(int cx, int cy) => !level[cx, cy].IsPassable();
                    bool Open(int cx, int cy) => level[cx, cy].IsPassable();

                    var horizontal = Open(x - 1, y) && Open(x + 1, y) && Solid(x, y - 1) && Solid(x, y + 1);
                    var vertical = Open(x, y - 1) && Open(x, y + 1) && Solid(x - 1, y) && Solid(x + 1, y);
                    if (!horizontal && !vertical)
                    {
                        level[x, y] = Terrain.Floor;
                    }
                }
            }
        }

        private static void PlaceStairs(Level level, Rng rng)
        {
            var start = rng.Pick(level.Rooms);
            level.Entrance = RandomInterior(level, rng, start);
            level[level.X(level.Entrance), level.Y(level.Entrance)] = Terrain.Entrance;

            // The exit goes in the room that takes longest to walk to.
            var distance = PathFinder.Distances(level.Width, level.Height, level.PassableMask(), level.Entrance);
            var best = -1;
            var bestDistance = -1;
            foreach (var room in level.Rooms)
            {
                var cell = level.Cell(room.CenterX, room.CenterY);
                if (!room.Equals(start) && distance[cell] > bestDistance)
                {
                    bestDistance = distance[cell];
                    best = cell;
                }
            }

            level.Exit = best;
            level[level.X(best), level.Y(best)] = Terrain.Exit;
        }

        private static int RandomInterior(Level level, Rng rng, Room room)
        {
            var x = rng.IntRange(room.Left + 1, room.Right - 1);
            var y = rng.IntRange(room.Top + 1, room.Bottom - 1);
            return level.Cell(x, y);
        }
    }
}
