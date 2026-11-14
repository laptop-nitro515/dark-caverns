// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using DarkCaverns.Core.World;
using UnityEngine;
using UnityEngine.Tilemaps;
// UnityEngine also has a Terrain type; this file means the dungeon one.
using Terrain = DarkCaverns.Core.World.Terrain;

namespace DarkCaverns.Game
{
    /// <summary>
    /// Draws a Level on a Tilemap using an SPD region tileset, and shades it
    /// by what the hero can see now, has seen before, or has never seen.
    /// </summary>
    public sealed class LevelView : MonoBehaviour
    {
        // Indices into SPD's 16x16 tile sheets (see SPD's tiles/DungeonTileSheet.java, flat walls).
        private const int Floor = 0;
        private const int FloorDeco = 1;
        private const int Entrance = 16;
        private const int Exit = 17;
        private const int Water = 32 + 15;
        private const int FlatWall = 48;
        private const int FlatWallDeco = 49;
        private const int FlatDoor = 56;
        private const int FlatDoorOpen = 57;

        private static readonly Color Remembered = new Color(0.35f, 0.35f, 0.45f);

        private readonly Dictionary<int, Tile> tiles = new Dictionary<int, Tile>();
        private Tilemap tilemap;
        private SpriteSheet sheet;
        private string sheetName;
        private Level level;

        public static LevelView Create(Transform parent)
        {
            var grid = new GameObject("Grid", typeof(Grid));
            grid.transform.SetParent(parent, false);

            var map = new GameObject("Terrain", typeof(Tilemap), typeof(TilemapRenderer));
            map.transform.SetParent(grid.transform, false);
            map.GetComponent<TilemapRenderer>().sortingOrder = 0;

            var view = map.AddComponent<LevelView>();
            view.tilemap = map.GetComponent<Tilemap>();
            return view;
        }

        /// <summary>Unity position of the centre of a cell. Level rows run down, Unity's y runs up.</summary>
        public Vector3 CellCenter(int x, int y) => new Vector3(x + 0.5f, level.Height - 1 - y + 0.5f, 0f);

        public bool TryWorldToCell(Vector3 world, out int x, out int y)
        {
            x = Mathf.FloorToInt(world.x);
            y = level.Height - 1 - Mathf.FloorToInt(world.y);
            return level.InBounds(x, y);
        }

        public void Show(Level newLevel, string tileset)
        {
            level = newLevel;
            if (sheetName != tileset)
            {
                sheetName = tileset;
                sheet = new SpriteSheet(tileset, 16, 16, new Vector2(0.5f, 0.5f));
                tiles.Clear();
            }

            tilemap.ClearAllTiles();
            Refresh();
        }

        /// <summary>Redraws terrain and fog. Cheap enough to call every turn at these map sizes.</summary>
        public void Refresh()
        {
            for (var y = 0; y < level.Height; y++)
            {
                for (var x = 0; x < level.Width; x++)
                {
                    var cell = level.Cell(x, y);
                    var pos = new Vector3Int(x, level.Height - 1 - y, 0);
                    if (!level.Visited[cell] || level.Map[cell] == Terrain.Empty)
                    {
                        tilemap.SetTile(pos, null);
                        continue;
                    }

                    tilemap.SetTile(pos, TileFor(TileIndex(x, y, level.Map[cell])));
                    tilemap.SetColor(pos, level.Visible[cell] ? Color.white : Remembered);
                }
            }
        }

        private int TileIndex(int x, int y, Terrain terrain)
        {
            // Cheap per-cell variation so large areas don't look tiled. Stable for a given cell.
            var deco = ((x * 73856093) ^ (y * 19349663) ^ level.Depth) % 11 == 0;
            switch (terrain)
            {
                case Terrain.Wall: return deco ? FlatWallDeco : FlatWall;
                case Terrain.Door: return FlatDoor;
                case Terrain.OpenDoor: return FlatDoorOpen;
                case Terrain.Entrance: return Entrance;
                case Terrain.Exit: return Exit;
                case Terrain.Water: return Water;
                default: return deco ? FloorDeco : Floor;
            }
        }

        private Tile TileFor(int index)
        {
            if (!tiles.TryGetValue(index, out var tile))
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sheet[index];
                tile.flags = TileFlags.None; // let SetColor tint it for fog of war
                tiles[index] = tile;
            }

            return tile;
        }
    }
}
