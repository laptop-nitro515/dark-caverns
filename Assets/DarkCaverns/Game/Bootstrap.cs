// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using UnityEngine;

namespace DarkCaverns.Game
{
    /// <summary>
    /// Starts the game in whatever scene is open when Play is pressed, so the
    /// project needs no scene setup yet. A title scene replaces this later.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            if (Object.FindAnyObjectByType<GameController>() == null)
            {
                GameController.Create();
            }
        }
    }
}
