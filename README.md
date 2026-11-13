# Dark Caverns

A roguelike for desktop and mobile that combines **Umoria** and **Shattered Pixel
Dungeon**: Moria's town, stores, races, spellbooks and deep descent, with Pixel
Dungeon's sprite art, compact levels, item systems and touch-friendly controls.
Built in Unity 6 LTS and C#.

This is early work. Milestone 1 is a walkable dungeon: generated floors drawn with
SPD's tiles, field of view and fog of war, stairs through all 40 floors.

## Playing in the editor

1. Install Unity 6 LTS (6000.0) with Unity Hub.
2. In Unity Hub choose **Add > Add project from disk** and pick this folder.
3. Open the project. If Unity asks to enable the new Input System backend, say yes;
   the editor restarts.
4. Press **Play** in any scene. The game starts itself; no scene setup is needed yet.

Controls: arrow keys, WASD or the numpad to move (Q, E, Z, C or numpad 7, 9, 1, 3 for
diagonals), space or numpad 5 to wait, click or tap a place you have seen to walk
there. Walk onto the down stairs to descend.

## Layout

```
Assets/DarkCaverns/
  Core/        Game rules in plain C#, no UnityEngine references (DarkCaverns.Core.asmdef)
    Random/    Seeded RNG and Moria-style dice
    World/     Terrain, levels, the 5 regions of 8 floors, level generation
    Mechanics/ Field of view (ported from SPD) and pathfinding
    Game/      GameSession: one run of the game
  Game/        Unity layer: tilemap and sprite views, input, camera, HUD
  Editor/      Import settings for the pixel art
  Resources/Sprites/  Sprite sheets from Shattered Pixel Dungeon
Dotnet/        .NET projects that build and test Core outside Unity
```

## Testing the game logic without Unity

Requires the .NET 8 SDK.

```sh
dotnet test Dotnet/DarkCaverns.Core.Tests
```

## Design

The game design (regions, town, races, magic, items, monsters and milestones) is in
the project's design document, "Return to Moria: combined game design".

## License

Dark Caverns is free software under the GNU General Public License v3.0 or later.
See [LICENSE](LICENSE) and [CREDITS.md](CREDITS.md) for the projects it builds on.
