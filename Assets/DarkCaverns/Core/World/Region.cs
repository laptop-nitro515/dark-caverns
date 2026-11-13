// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System;

namespace DarkCaverns.Core.World
{
    /// <summary>
    /// The 40 floors below town are split into 5 regions of 8. The last floor
    /// of each region holds its boss; the Balrog waits on floor 40.
    /// </summary>
    public sealed class Region
    {
        public const int FloorsPerRegion = 8;
        public const int RegionCount = 5;
        public const int DeepestFloor = FloorsPerRegion * RegionCount;

        public static readonly Region[] All =
        {
            new Region(0, "Upper Mines", "tiles_sewers"),
            new Region(1, "Dwarven Halls", "tiles_prison"),
            new Region(2, "Deep Caverns", "tiles_caves"),
            new Region(3, "Orc Strongholds", "tiles_city"),
            new Region(4, "The Balrog's Pit", "tiles_halls"),
        };

        public int Index { get; }
        public string Name { get; }

        /// <summary>Sprite sheet in Resources/Sprites used to draw this region's floors.</summary>
        public string Tileset { get; }

        private Region(int index, string name, string tileset)
        {
            Index = index;
            Name = name;
            Tileset = tileset;
        }

        public int FirstFloor => Index * FloorsPerRegion + 1;
        public int BossFloor => (Index + 1) * FloorsPerRegion;

        /// <summary>Region for a dungeon floor, 1 to 40.</summary>
        public static Region ForDepth(int depth)
        {
            if (depth < 1 || depth > DeepestFloor)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "Floors run from 1 to " + DeepestFloor);
            }

            return All[(depth - 1) / FloorsPerRegion];
        }

        public static bool IsBossFloor(int depth) => depth % FloorsPerRegion == 0;
    }
}
