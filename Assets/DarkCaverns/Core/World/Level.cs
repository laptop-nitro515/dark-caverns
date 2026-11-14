// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;

namespace DarkCaverns.Core.World
{
    /// <summary>A single dungeon floor: a grid of terrain plus what the player has seen.</summary>
    public sealed class Level
    {
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public int Length => Width * Height;

        public Terrain[] Map { get; }

        /// <summary>Cells the hero has ever seen.</summary>
        public bool[] Visited { get; }

        /// <summary>Cells the hero can see right now.</summary>
        public bool[] Visible { get; }

        public int Entrance { get; set; } = -1;
        public int Exit { get; set; } = -1;

        public List<Room> Rooms { get; } = new List<Room>();

        /// <summary>Store entrances in town, by cell.</summary>
        public Dictionary<int, Shop> Shops { get; } = new Dictionary<int, Shop>();

        /// <summary>Depth 0 is the town above the dungeon.</summary>
        public bool IsTown => Depth == 0;

        public Level(int width, int height, int depth)
        {
            if (width < 3 || height < 3)
            {
                throw new ArgumentException("Level must be at least 3x3");
            }

            Width = width;
            Height = height;
            Depth = depth;
            Map = new Terrain[Length];
            Visited = new bool[Length];
            Visible = new bool[Length];
        }

        public int Cell(int x, int y) => x + y * Width;
        public int X(int cell) => cell % Width;
        public int Y(int cell) => cell / Width;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public Terrain this[int x, int y]
        {
            get => Map[Cell(x, y)];
            set => Map[Cell(x, y)] = value;
        }

        public bool Passable(int cell) => Map[cell].IsPassable();

        public bool[] SightBlockers()
        {
            var blocking = new bool[Length];
            for (var i = 0; i < Length; i++)
            {
                blocking[i] = Map[i].BlocksSight();
            }

            return blocking;
        }

        public bool[] PassableMask()
        {
            var passable = new bool[Length];
            for (var i = 0; i < Length; i++)
            {
                passable[i] = Map[i].IsPassable();
            }

            return passable;
        }
    }

    /// <summary>A rectangular room. Bounds include its walls.</summary>
    public readonly struct Room
    {
        public readonly int Left, Top, Right, Bottom;

        public Room(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int CenterX => (Left + Right) / 2;
        public int CenterY => (Top + Bottom) / 2;

        public bool Intersects(Room other, int margin) =>
            Left - margin <= other.Right && other.Left - margin <= Right
            && Top - margin <= other.Bottom && other.Top - margin <= Bottom;

        public bool ContainsInterior(int x, int y) => x > Left && x < Right && y > Top && y < Bottom;
    }
}
