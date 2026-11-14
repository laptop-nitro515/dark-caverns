// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using DarkCaverns.Core.Mechanics;
using DarkCaverns.Core.Random;
using DarkCaverns.Core.World;

namespace DarkCaverns.Core.Game
{
    /// <summary>
    /// One run of the game: the seed, the floors visited so far, and where the
    /// hero is. Has no Unity dependency, so it can be driven by tests.
    /// </summary>
    public sealed class GameSession
    {
        private readonly Dictionary<int, Level> floors = new Dictionary<int, Level>();
        private readonly Rng rng;
        private bool[] fovScratch;
        private int recallTurns;

        public ulong Seed { get; }
        public Level Level { get; private set; }
        public int Depth => Level.Depth;
        public bool InTown => Level.IsTown;

        /// <summary>The dungeon region the hero is in, or null in town.</summary>
        public Region Region => InTown ? null : Region.ForDepth(Depth);

        /// <summary>Deepest floor reached; Word of Recall returns here.</summary>
        public int MaxDepth { get; private set; }

        /// <summary>True while a Word of Recall is waiting to take effect.</summary>
        public bool RecallActive => recallTurns > 0;

        /// <summary>Name of where the hero is, for the HUD.</summary>
        public string LocationName => InTown ? "Town" : Region.Name;

        /// <summary>Sprite sheet in Resources/Sprites for the current floor.</summary>
        public string Tileset => InTown ? TownTileset : Region.Tileset;

        public const string TownTileset = "tiles_city";
        public int HeroCell { get; private set; }
        public int Turn { get; private set; }

        /// <summary>
        /// How far the hero sees, in cells. Moria-style light: a torch gives a
        /// small radius, a lantern a larger one. Fixed until light sources exist.
        /// </summary>
        public int LightRadius { get; set; } = 6;

        /// <summary>Messages for the game log, oldest first. The UI drains it.</summary>
        public List<string> Messages { get; } = new List<string>();

        /// <summary>Raised after the hero arrives on a different floor.</summary>
        public event Action LevelChanged;

        /// <summary>Raised when the hero walks into a store's doorway.</summary>
        public event Action<Shop> ShopEntered;

        /// <summary>A new game starts in town, as in Moria.</summary>
        public GameSession(ulong seed, int startDepth = 0)
        {
            Seed = seed;
            rng = new Rng(seed ^ 0x5E55107UL);
            EnterFloor(startDepth, Arrival.UpStairs);
        }

        private enum Arrival
        {
            UpStairs,   // beside the up stairs, coming down
            DownStairs, // beside the down stairs, coming up
        }

        public int HeroX => Level.X(HeroCell);
        public int HeroY => Level.Y(HeroCell);

        /// <summary>Tries to step one cell. Returns true if a turn was spent.</summary>
        public bool TryMove(int dx, int dy)
        {
            if ((dx == 0 && dy == 0) || Math.Abs(dx) > 1 || Math.Abs(dy) > 1)
            {
                return false;
            }

            int nx = HeroX + dx, ny = HeroY + dy;
            if (!Level.InBounds(nx, ny) || !Level[nx, ny].IsPassable())
            {
                return false;
            }

            MoveHeroTo(Level.Cell(nx, ny));
            var level = Level;
            EndTurn();
            if (Level != level)
            {
                return true; // recall fired this turn
            }

            switch (Level.Map[HeroCell])
            {
                case Terrain.Exit:
                    Descend();
                    break;
                case Terrain.Entrance:
                    Ascend();
                    break;
                case Terrain.ShopEntrance:
                    var shop = Level.Shops[HeroCell];
                    Messages.Add($"You enter the {shop.Name()}.");
                    ShopEntered?.Invoke(shop);
                    break;
            }

            return true;
        }

        /// <summary>Spends a turn in place.</summary>
        public void Rest()
        {
            EndTurn();
        }

        /// <summary>
        /// Reads a Word of Recall. After 15 + 1d20 turns it lifts the hero from the
        /// dungeon to town, or from town down to the deepest floor reached, as in
        /// Moria. Reading another while one is active cancels it.
        /// </summary>
        public void ReadWordOfRecall()
        {
            if (RecallActive)
            {
                recallTurns = 0;
                Messages.Add("A tension leaves the air around you.");
                return;
            }

            recallTurns = 15 + rng.IntRange(1, 20);
            Messages.Add("The air about you becomes charged.");
        }

        private void EndTurn()
        {
            Turn++;
            if (recallTurns > 0 && --recallTurns == 0)
            {
                Recall();
            }
        }

        private void Recall()
        {
            if (!InTown)
            {
                Messages.Add("You feel yourself yanked upwards!");
                EnterFloor(0, Arrival.DownStairs);
            }
            else if (MaxDepth > 0)
            {
                Messages.Add("You feel yourself yanked downwards!");
                EnterFloor(MaxDepth, Arrival.UpStairs);
            }
            else
            {
                Messages.Add("The air settles. You have not been anywhere to return to.");
            }
        }

        /// <summary>Path to a cell the hero has seen, for click-to-move. Null if none.</summary>
        public List<int> PathTo(int target)
        {
            if (target < 0 || target >= Level.Length || !Level.Visited[target])
            {
                return null;
            }

            // Only walk through cells the hero knows about.
            var known = Level.PassableMask();
            for (var i = 0; i < known.Length; i++)
            {
                known[i] &= Level.Visited[i];
            }

            return PathFinder.FindPath(Level.Width, Level.Height, known, HeroCell, target);
        }

        public void Descend()
        {
            if (Depth >= Region.DeepestFloor)
            {
                Messages.Add("There is nothing deeper than this.");
                return;
            }

            EnterFloor(Depth + 1, Arrival.UpStairs);
        }

        public void Ascend()
        {
            if (InTown)
            {
                return;
            }

            EnterFloor(Depth - 1, Arrival.DownStairs);
        }

        private void EnterFloor(int depth, Arrival arrival)
        {
            if (!floors.TryGetValue(depth, out var level))
            {
                level = depth == 0 ? TownGenerator.Generate(Seed) : LevelGenerator.Generate(Seed, depth);
                floors[depth] = level;
            }

            Level = level;
            fovScratch = new bool[level.Length];
            MaxDepth = Math.Max(MaxDepth, depth);

            // Arrive next to the stairs, not on them, so stepping off and on again is a choice.
            var stairs = arrival == Arrival.DownStairs || level.Entrance < 0 ? level.Exit : level.Entrance;
            HeroCell = FreeNeighbour(level, stairs);

            if (level.IsTown)
            {
                // The town is lit: everything in it is known.
                for (var i = 0; i < level.Length; i++)
                {
                    level.Visited[i] = true;
                }

                Messages.Add("You are in the town. The stairs lead down into the Dark Caverns.");
            }
            else
            {
                var region = Region.ForDepth(depth);
                var floorInRegion = depth - region.FirstFloor + 1;
                Messages.Add($"{region.Name}, floor {floorInRegion} of {Region.FloorsPerRegion} (depth {depth}).");
                if (Region.IsBossFloor(depth))
                {
                    Messages.Add("Something powerful guards this floor.");
                }
            }

            UpdateFieldOfView();
            LevelChanged?.Invoke();
        }

        private static int FreeNeighbour(Level level, int cell)
        {
            int x = level.X(cell), y = level.Y(cell);
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (level.InBounds(x + dx, y + dy))
                    {
                        var t = level[x + dx, y + dy];
                        if (t == Terrain.Floor || t == Terrain.Grass)
                        {
                            return level.Cell(x + dx, y + dy);
                        }
                    }
                }
            }

            return cell;
        }

        private void MoveHeroTo(int cell)
        {
            // Doors stand open only while someone is in the doorway, as in SPD.
            if (Level.Map[HeroCell] == Terrain.OpenDoor)
            {
                Level.Map[HeroCell] = Terrain.Door;
            }

            HeroCell = cell;
            if (Level.Map[HeroCell] == Terrain.Door)
            {
                Level.Map[HeroCell] = Terrain.OpenDoor;
            }

            UpdateFieldOfView();
        }

        public void UpdateFieldOfView()
        {
            // The town is in daylight; below, the hero sees only as far as their light.
            var sight = InTown ? ShadowCaster.MaxDistance : LightRadius;
            ShadowCaster.CastShadow(HeroX, HeroY, Level.Width, fovScratch, Level.SightBlockers(), sight);
            for (var i = 0; i < Level.Length; i++)
            {
                Level.Visible[i] = fovScratch[i];
                if (fovScratch[i])
                {
                    Level.Visited[i] = true;
                }
            }
        }
    }
}
