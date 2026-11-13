/*
 * Dark Caverns
 *
 * Ported from Shattered Pixel Dungeon's mechanics/ShadowCaster.java
 * Pixel Dungeon
 * Copyright (C) 2012-2015 Oleg Dolya
 * Shattered Pixel Dungeon
 * Copyright (C) 2014-2026 Evan Debenham
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>
 */

using System;

namespace DarkCaverns.Core.Mechanics
{
    /// <summary>
    /// Recursive shadowcasting field of view, with rounded rows so the lit area
    /// is a circle rather than a square.
    /// Based on http://www.roguebasin.com/index.php?title=FOV_using_recursive_shadowcasting
    /// </summary>
    public static class ShadowCaster
    {
        public const int MaxDistance = 20;

        // Longest row at each distance, for a circular field of view.
        private static readonly int[][] Rounding = BuildRounding();

        private static int[][] BuildRounding()
        {
            var rounding = new int[MaxDistance + 1][];
            rounding[0] = new int[1];
            for (var i = 1; i <= MaxDistance; i++)
            {
                rounding[i] = new int[i + 1];
                for (var j = 1; j <= i; j++)
                {
                    // Java's Math.round rounds halves up; Math.Floor(x + 0.5) matches it.
                    rounding[i][j] = (int) Math.Min(j, Math.Floor(i * Math.Cos(Math.Asin(j / (i + 0.5))) + 0.5));
                }
            }

            return rounding;
        }

        public static void CastShadow(int x, int y, int w, bool[] fieldOfView, bool[] blocking, int distance)
        {
            if (distance >= MaxDistance)
            {
                distance = MaxDistance;
            }

            Array.Clear(fieldOfView, 0, fieldOfView.Length);
            fieldOfView[y * w + x] = true;

            if (distance <= 0)
            {
                return;
            }

            // Scan the eight octants, clockwise.
            ScanOctant(distance, fieldOfView, blocking, 1, x, y, w, 0.0, 1.0, +1, -1, false);
            ScanOctant(distance, fieldOfView, blocking, 1, x, y, w, 0.0, 1.0, -1, +1, true);
            ScanOctant(distance, fieldOfView, blocking, 1, x, y, w, 0.0, 1.0, +1, +1, true);
            ScanOctant(distance, fieldOfView, blocking, 1, x, y, w, 0.0, 1.0, +1, +1, false);
            ScanOctant(distance, fieldOfView, blocking, 1, x, y, w, 0.0, 1.0, -1, +1, false);
            ScanOctant(distance, fieldOfView, blocking, 1, x, y, w, 0.0, 1.0, +1, -1, true);
            ScanOctant(distance, fieldOfView, blocking, 1, x, y, w, 0.0, 1.0, -1, -1, true);
            ScanOctant(distance, fieldOfView, blocking, 1, x, y, w, 0.0, 1.0, -1, -1, false);
        }

        // Scans one 45 degree octant. The others come from mirroring in X (mX), Y (mY) and X=Y (mXY).
        private static void ScanOctant(int distance, bool[] fov, bool[] blocking, int row,
            int x, int y, int w, double lSlope, double rSlope, int mX, int mY, bool mXY)
        {
            var inBlocking = false;
            var h = fov.Length / w;

            int[] roundingAtDist;
            if (distance == 2)
            {
                // At distance 2 fill in the corners, or diagonal movement is punished too hard.
                roundingAtDist = (int[]) Rounding[distance].Clone();
                roundingAtDist[2] = 2;
            }
            else
            {
                roundingAtDist = Rounding[distance];
            }

            for (; row <= distance; row++)
            {
                if (rSlope < lSlope)
                {
                    return;
                }

                // Offset by slightly less than 0.5 to account for slopes just touching a cell.
                var start = lSlope == 0 ? 0 : (int) Math.Floor((row - 0.5) * lSlope + 0.499);
                var end = rSlope == 1
                    ? roundingAtDist[row]
                    : Math.Min(roundingAtDist[row], (int) Math.Ceiling((row + 0.5) * rSlope - 0.499));

                for (var col = start; col <= end; col++)
                {
                    // Handles a slope landing one cell further along at the end of a blocking cell.
                    if (col == end && inBlocking && (int) Math.Ceiling((row - 0.5) * rSlope - 0.499) != end)
                    {
                        break;
                    }

                    int cx, cy;
                    if (mXY)
                    {
                        cx = x + mY * row;
                        cy = y + mX * col;
                    }
                    else
                    {
                        cx = x + mX * col;
                        cy = y + mY * row;
                    }

                    // SPD relies on a wall border; here cells off the map simply count as blocking.
                    var offMap = cx < 0 || cy < 0 || cx >= w || cy >= h;
                    var cell = cx + cy * w;
                    if (!offMap)
                    {
                        fov[cell] = true;
                    }

                    if (offMap || blocking[cell])
                    {
                        if (!inBlocking)
                        {
                            inBlocking = true;

                            // Start a new scan one row deeper, ending at the left side of this cell.
                            if (col != start)
                            {
                                ScanOctant(distance, fov, blocking, row + 1, x, y, w, lSlope, (col - 0.5) / (row + 0.5), mX, mY, mXY);
                            }
                        }
                    }
                    else if (inBlocking)
                    {
                        inBlocking = false;

                        // Restrict this scan to the left side of the current cell for later rows.
                        lSlope = (col - 0.5) / (row - 0.5);
                    }
                }

                // A row that ends in a blocking cell finishes this scan.
                if (inBlocking)
                {
                    return;
                }
            }
        }
    }
}
