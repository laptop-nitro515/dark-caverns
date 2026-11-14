// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using DarkCaverns.Core.Random;

namespace DarkCaverns.Core.World
{
    /// <summary>
    /// Builds the town above the dungeon, laid out as in Moria: six store buildings
    /// in two rows of three, each with its door facing the street that runs between
    /// the rows, and the stairs down somewhere on that street. The same seed always
    /// gives the same town.
    /// </summary>
    public static class TownGenerator
    {
        public const int Width = 44;
        public const int Height = 26;

        private const int Columns = 3;
        private const int Rows = 2;
        private const int BlockWidth = (Width - 2) / Columns;   // 14
        private const int StreetTop = 11;                       // first row of the main street
        private const int StreetBottom = 14;                    // last row of the main street

        public static Level Generate(ulong gameSeed)
        {
            var rng = new Rng(gameSeed ^ 0x70776E5EEDUL);
            var town = new Level(Width, Height, 0);

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var edge = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                    town[x, y] = edge ? Terrain.Wall : (rng.Chance(0.18f) ? Terrain.Grass : Terrain.Floor);
                }
            }

            // Moria shuffles which store stands on which lot.
            var shops = new List<Shop>
            {
                Shop.GeneralStore, Shop.Armoury, Shop.Weaponsmith, Shop.Temple, Shop.Alchemist, Shop.MagicShop,
            };
            rng.Shuffle(shops);

            for (var row = 0; row < Rows; row++)
            {
                for (var col = 0; col < Columns; col++)
                {
                    BuildStore(town, rng, shops[row * Columns + col], col, row);
                }
            }

            // Stairs down go on the main street, clear of any doorway.
            int sx, sy;
            do
            {
                sx = rng.IntRange(3, Width - 4);
                sy = rng.IntRange(StreetTop, StreetBottom);
            }
            while (town[sx, sy - 1] == Terrain.ShopEntrance || town[sx, sy + 1] == Terrain.ShopEntrance);

            town.Exit = town.Cell(sx, sy);
            town[sx, sy] = Terrain.Exit;
            return town;
        }

        private static void BuildStore(Level town, Rng rng, Shop shop, int col, int row)
        {
            var w = rng.IntRange(7, 10);
            var h = rng.IntRange(5, 7);
            var lotLeft = 1 + col * BlockWidth;
            var left = lotLeft + rng.IntRange(1, BlockWidth - w - 1);

            // Top-row stores sit above the street, bottom-row stores below it,
            // with at least one row of open ground between building and street.
            var top = row == 0
                ? rng.IntRange(2, StreetTop - h - 1)
                : rng.IntRange(StreetBottom + 2, Height - h - 2);

            for (var y = top; y < top + h; y++)
            {
                for (var x = left; x < left + w; x++)
                {
                    town[x, y] = Terrain.Wall;
                }
            }

            var room = new Room(left, top, left + w - 1, top + h - 1);
            town.Rooms.Add(room);

            var doorX = rng.IntRange(left + 1, left + w - 2);
            var doorY = row == 0 ? top + h - 1 : top;
            var door = town.Cell(doorX, doorY);
            town[doorX, doorY] = Terrain.ShopEntrance;
            town.Shops[door] = shop;
        }
    }
}
