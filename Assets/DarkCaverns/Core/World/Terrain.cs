// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

namespace DarkCaverns.Core.World
{
    public enum Terrain : byte
    {
        Empty,      // solid rock outside the map
        Wall,
        Floor,
        Door,
        OpenDoor,
        Entrance,   // up stairs
        Exit,       // down stairs
        Water,
    }

    public static class TerrainInfo
    {
        public static bool IsPassable(this Terrain t) =>
            t == Terrain.Floor || t == Terrain.Door || t == Terrain.OpenDoor
            || t == Terrain.Entrance || t == Terrain.Exit || t == Terrain.Water;

        /// <summary>Closed doors and walls block line of sight.</summary>
        public static bool BlocksSight(this Terrain t) =>
            t == Terrain.Empty || t == Terrain.Wall || t == Terrain.Door;
    }
}
