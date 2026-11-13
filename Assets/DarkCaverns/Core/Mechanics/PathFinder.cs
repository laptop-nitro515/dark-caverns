// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;

namespace DarkCaverns.Core.Mechanics
{
    /// <summary>Breadth-first search over a grid with 8-way movement.</summary>
    public static class PathFinder
    {
        private static readonly int[] Dx = { 0, 1, 0, -1, 1, 1, -1, -1 };
        private static readonly int[] Dy = { -1, 0, 1, 0, -1, 1, 1, -1 };

        /// <summary>Steps from <paramref name="from"/> to every cell, or -1 where unreachable.</summary>
        public static int[] Distances(int width, int height, bool[] passable, int from)
        {
            var distance = new int[width * height];
            for (var i = 0; i < distance.Length; i++)
            {
                distance[i] = -1;
            }

            var queue = new Queue<int>();
            distance[from] = 0;
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                int x = cell % width, y = cell / width;
                for (var d = 0; d < 8; d++)
                {
                    int nx = x + Dx[d], ny = y + Dy[d];
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                    {
                        continue;
                    }

                    var next = nx + ny * width;
                    if (distance[next] < 0 && passable[next])
                    {
                        distance[next] = distance[cell] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return distance;
        }

        /// <summary>
        /// Shortest path from <paramref name="from"/> to <paramref name="to"/>, excluding the start.
        /// Returns null if there is none. The target itself need not be passable,
        /// so a path can end on a monster or a closed door.
        /// </summary>
        public static List<int> FindPath(int width, int height, bool[] passable, int from, int to)
        {
            if (from == to)
            {
                return new List<int>();
            }

            // Search backwards from the target so the path reads forwards.
            var mask = (bool[]) passable.Clone();
            mask[to] = true;
            var distance = Distances(width, height, mask, to);
            if (distance[from] < 0)
            {
                return null;
            }

            var path = new List<int>(distance[from]);
            var cell = from;
            while (cell != to)
            {
                int x = cell % width, y = cell / width;
                var best = -1;
                for (var d = 0; d < 8; d++)
                {
                    int nx = x + Dx[d], ny = y + Dy[d];
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                    {
                        continue;
                    }

                    var next = nx + ny * width;
                    if (distance[next] >= 0 && distance[next] == distance[cell] - 1)
                    {
                        best = next;
                        break;
                    }
                }

                cell = best;
                path.Add(cell);
            }

            return path;
        }
    }
}
