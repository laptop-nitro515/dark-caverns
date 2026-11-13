// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using DarkCaverns.Core.Mechanics;
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
        private bool[] fovScratch;

        public ulong Seed { get; }
        public Level Level { get; private set; }
        public int Depth => Level.Depth;
        public Region Region => Region.ForDepth(Depth);
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

        public GameSession(ulong seed, int startDepth = 1)
        {
            Seed = seed;
            EnterFloor(startDepth, arriveAtExit: false);
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
            Turn++;

            var here = Level.Map[HeroCell];
            if (here == Terrain.Exit)
            {
                Descend();
            }
            else if (here == Terrain.Entrance)
            {
                Ascend();
            }

            return true;
        }

        /// <summary>Spends a turn in place.</summary>
        public void Rest()
        {
            Turn++;
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

            EnterFloor(Depth + 1, arriveAtExit: false);
        }

        public void Ascend()
        {
            if (Depth <= 1)
            {
                // The town arrives in milestone 2.
                Messages.Add("Daylight is above you, but the town is not built yet.");
                return;
            }

            EnterFloor(Depth - 1, arriveAtExit: true);
        }

        private void EnterFloor(int depth, bool arriveAtExit)
        {
            if (!floors.TryGetValue(depth, out var level))
            {
                level = LevelGenerator.Generate(Seed, depth);
                floors[depth] = level;
            }

            Level = level;
            fovScratch = new bool[level.Length];

            // Arrive next to the stairs, not on them, so stepping off and on again is a choice.
            var stairs = arriveAtExit ? level.Exit : level.Entrance;
            HeroCell = FreeNeighbour(level, stairs);
            UpdateFieldOfView();

            var region = Region.ForDepth(depth);
            var floorInRegion = depth - region.FirstFloor + 1;
            Messages.Add($"{region.Name}, floor {floorInRegion} of {Region.FloorsPerRegion} (depth {depth}).");
            if (Region.IsBossFloor(depth))
            {
                Messages.Add("Something powerful guards this floor.");
            }

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
                        if (t == Terrain.Floor)
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
            ShadowCaster.CastShadow(HeroX, HeroY, Level.Width, fovScratch, Level.SightBlockers(), LightRadius);
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
