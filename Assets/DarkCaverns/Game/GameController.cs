// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using DarkCaverns.Core.Game;
using DarkCaverns.Core.World;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DarkCaverns.Game
{
    /// <summary>
    /// Runs a GameSession and connects it to the views and to input:
    /// arrow keys, WASD or numpad to step (diagonals on the numpad or Q/E/Z/C),
    /// space or numpad 5 to wait, R for Word of Recall, and click or tap a seen cell to walk there.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        private const float StepSeconds = 0.11f;
        private const float KeyRepeatDelay = 0.25f;
        private const int MaxLogLines = 5;

        private GameSession session;
        private LevelView levelView;
        private HeroView heroView;
        private Camera cam;
        private readonly Queue<int> walkPath = new Queue<int>();
        private readonly List<string> log = new List<string>();
        private float stepCooldown;
        private Vector2Int heldDirection;
        private float heldTime;
        private GUIStyle logStyle;
        private Shop? openShop;

        public static GameController Create()
        {
            var root = new GameObject("Dark Caverns");
            return root.AddComponent<GameController>();
        }

        private void Start()
        {
            cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                cam.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 7f; // about 14 cells tall, close to SPD's default zoom
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            levelView = LevelView.Create(transform);
            heroView = HeroView.Create(transform);

            var seed = (ulong) DateTime.UtcNow.Ticks;
            session = new GameSession(seed);
            session.LevelChanged += OnLevelChanged;
            session.ShopEntered += shop =>
            {
                openShop = shop;
                walkPath.Clear();
            };
            OnLevelChanged();
            Debug.Log($"Dark Caverns: new game, seed {seed}");
        }

        private void OnLevelChanged()
        {
            if (levelView == null)
            {
                return;
            }

            walkPath.Clear();
            openShop = null;
            levelView.Show(session.Level, session.Tileset);
            heroView.Place(levelView.CellCenter(session.HeroX, session.HeroY));
            SnapCamera();
        }

        private void Update()
        {
            stepCooldown -= Time.deltaTime;

            if (openShop != null)
            {
                // The store window is modal; any key or click closes it.
                if (ClosePressed() || TryReadPointer(out _))
                {
                    openShop = null;
                }

                DrainMessages();
                FollowHero();
                return;
            }

            if (RecallPressed())
            {
                // Stands in for reading a Scroll of Word of Recall until items exist.
                walkPath.Clear();
                session.ReadWordOfRecall();
            }

            var direction = ReadDirection(out var pressedThisFrame);
            if (direction != Vector2Int.zero)
            {
                walkPath.Clear();
                heldTime = pressedThisFrame || direction != heldDirection ? 0f : heldTime + Time.deltaTime;
                heldDirection = direction;

                // One step per press, then repeat while held.
                if ((pressedThisFrame || heldTime > KeyRepeatDelay) && stepCooldown <= 0f)
                {
                    Step(direction.x, direction.y);
                }
            }
            else
            {
                heldDirection = Vector2Int.zero;
                if (WaitPressed() && stepCooldown <= 0f)
                {
                    walkPath.Clear();
                    session.Rest();
                    stepCooldown = StepSeconds;
                }
                else if (TryReadPointer(out var screen))
                {
                    PlanWalk(screen);
                }
                else if (walkPath.Count > 0 && stepCooldown <= 0f)
                {
                    var next = walkPath.Dequeue();
                    var level = session.Level;
                    Step(level.X(next) - session.HeroX, level.Y(next) - session.HeroY);
                }
            }

            DrainMessages();
            FollowHero();
        }

        private void Step(int dx, int dy)
        {
            var levelBefore = session.Level;
            if (!session.TryMove(dx, dy))
            {
                walkPath.Clear();
                return;
            }

            stepCooldown = StepSeconds;
            if (session.Level == levelBefore)
            {
                heroView.StepTo(levelView.CellCenter(session.HeroX, session.HeroY), StepSeconds);
                levelView.Refresh();
            }
        }

        private void PlanWalk(Vector2 screen)
        {
            var world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            if (!levelView.TryWorldToCell(world, out var x, out var y))
            {
                return;
            }

            walkPath.Clear();
            var path = session.PathTo(session.Level.Cell(x, y));
            if (path == null)
            {
                return;
            }

            foreach (var cell in path)
            {
                walkPath.Enqueue(cell);
            }
        }

        private void DrainMessages()
        {
            if (session.Messages.Count == 0)
            {
                return;
            }

            log.AddRange(session.Messages);
            session.Messages.Clear();
            if (log.Count > MaxLogLines)
            {
                log.RemoveRange(0, log.Count - MaxLogLines);
            }
        }

        private void FollowHero()
        {
            var target = heroView.transform.position + new Vector3(0f, 0.5f, -10f);
            cam.transform.position = Vector3.Lerp(cam.transform.position, target, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        private void SnapCamera()
        {
            cam.transform.position = heroView.transform.position + new Vector3(0f, 0.5f, -10f);
        }

        private void OnGUI()
        {
            if (session == null)
            {
                return;
            }

            if (logStyle == null)
            {
                logStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 45), richText = false };
                logStyle.normal.textColor = Color.white;
            }

            var line = logStyle.fontSize + 6;
            var where = session.InTown ? "Town" : $"{session.LocationName}  ·  Depth {session.Depth}";
            var recall = session.RecallActive ? "  ·  Recall is charging" : "";
            GUI.Label(new Rect(12, 8, Screen.width - 24, line),
                $"{where}  ·  Turn {session.Turn}{recall}", logStyle);

            if (openShop != null)
            {
                DrawShopWindow(openShop.Value, line);
            }

            for (var i = 0; i < log.Count; i++)
            {
                GUI.Label(new Rect(12, Screen.height - (log.Count - i) * line - 8, Screen.width - 24, line), log[i], logStyle);
            }
        }

        private void DrawShopWindow(Shop shop, int line)
        {
            var w = Mathf.Min(Screen.width - 32, 520);
            var h = line * 6;
            var rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(rect, GUIContent.none);
            GUI.Box(rect, GUIContent.none); // twice for a darker panel on the default skin

            var inner = new Rect(rect.x + 16, rect.y + 12, rect.width - 32, line);
            GUI.Label(inner, $"{(int) shop}. {shop.Name()}", logStyle);
            inner.y += line * 1.5f;
            GUI.Label(inner, "The shelves are empty for now.", logStyle);
            inner.y += line;
            GUI.Label(inner, "Stock arrives when items are added.", logStyle);
            inner.y += line * 1.5f;
            GUI.Label(inner, "Press any key or click to leave.", logStyle);
        }

        // ---- Input -------------------------------------------------------------------------

#if ENABLE_INPUT_SYSTEM
        private static Vector2Int ReadDirection(out bool pressedThisFrame)
        {
            pressedThisFrame = false;
            var k = Keyboard.current;
            if (k == null)
            {
                return Vector2Int.zero;
            }

            foreach (var (key, dir) in KeyBindings)
            {
                if (k[key].wasPressedThisFrame)
                {
                    pressedThisFrame = true;
                    return dir;
                }
            }

            foreach (var (key, dir) in KeyBindings)
            {
                if (k[key].isPressed)
                {
                    return dir;
                }
            }

            return Vector2Int.zero;
        }

        private static readonly (Key key, Vector2Int dir)[] KeyBindings =
        {
            (Key.UpArrow, new Vector2Int(0, -1)), (Key.W, new Vector2Int(0, -1)), (Key.Numpad8, new Vector2Int(0, -1)),
            (Key.DownArrow, new Vector2Int(0, 1)), (Key.S, new Vector2Int(0, 1)), (Key.Numpad2, new Vector2Int(0, 1)),
            (Key.LeftArrow, new Vector2Int(-1, 0)), (Key.A, new Vector2Int(-1, 0)), (Key.Numpad4, new Vector2Int(-1, 0)),
            (Key.RightArrow, new Vector2Int(1, 0)), (Key.D, new Vector2Int(1, 0)), (Key.Numpad6, new Vector2Int(1, 0)),
            (Key.Numpad7, new Vector2Int(-1, -1)), (Key.Q, new Vector2Int(-1, -1)),
            (Key.Numpad9, new Vector2Int(1, -1)), (Key.E, new Vector2Int(1, -1)),
            (Key.Numpad1, new Vector2Int(-1, 1)), (Key.Z, new Vector2Int(-1, 1)),
            (Key.Numpad3, new Vector2Int(1, 1)), (Key.C, new Vector2Int(1, 1)),
        };

        private static bool WaitPressed()
        {
            var k = Keyboard.current;
            return k != null && (k.spaceKey.wasPressedThisFrame || k.numpad5Key.wasPressedThisFrame || k.periodKey.wasPressedThisFrame);
        }

        private static bool RecallPressed()
        {
            var k = Keyboard.current;
            return k != null && k.rKey.wasPressedThisFrame;
        }

        private static bool ClosePressed()
        {
            var k = Keyboard.current;
            return k != null && k.anyKey.wasPressedThisFrame;
        }

        private static bool TryReadPointer(out Vector2 screen)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screen = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screen = Mouse.current.position.ReadValue();
                return true;
            }

            screen = default;
            return false;
        }
#else
        private static Vector2Int ReadDirection(out bool pressedThisFrame)
        {
            foreach (var (key, dir) in KeyBindings)
            {
                if (Input.GetKeyDown(key))
                {
                    pressedThisFrame = true;
                    return dir;
                }
            }

            pressedThisFrame = false;
            foreach (var (key, dir) in KeyBindings)
            {
                if (Input.GetKey(key))
                {
                    return dir;
                }
            }

            return Vector2Int.zero;
        }

        private static readonly (KeyCode key, Vector2Int dir)[] KeyBindings =
        {
            (KeyCode.UpArrow, new Vector2Int(0, -1)), (KeyCode.W, new Vector2Int(0, -1)), (KeyCode.Keypad8, new Vector2Int(0, -1)),
            (KeyCode.DownArrow, new Vector2Int(0, 1)), (KeyCode.S, new Vector2Int(0, 1)), (KeyCode.Keypad2, new Vector2Int(0, 1)),
            (KeyCode.LeftArrow, new Vector2Int(-1, 0)), (KeyCode.A, new Vector2Int(-1, 0)), (KeyCode.Keypad4, new Vector2Int(-1, 0)),
            (KeyCode.RightArrow, new Vector2Int(1, 0)), (KeyCode.D, new Vector2Int(1, 0)), (KeyCode.Keypad6, new Vector2Int(1, 0)),
            (KeyCode.Keypad7, new Vector2Int(-1, -1)), (KeyCode.Q, new Vector2Int(-1, -1)),
            (KeyCode.Keypad9, new Vector2Int(1, -1)), (KeyCode.E, new Vector2Int(1, -1)),
            (KeyCode.Keypad1, new Vector2Int(-1, 1)), (KeyCode.Z, new Vector2Int(-1, 1)),
            (KeyCode.Keypad3, new Vector2Int(1, 1)), (KeyCode.C, new Vector2Int(1, 1)),
        };

        private static bool WaitPressed() =>
            Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Period);

        private static bool RecallPressed() => Input.GetKeyDown(KeyCode.R);

        private static bool ClosePressed() => Input.anyKeyDown && !Input.GetMouseButtonDown(0);

        private static bool TryReadPointer(out Vector2 screen)
        {
            if (Input.GetMouseButtonDown(0))
            {
                screen = Input.mousePosition;
                return true;
            }

            screen = default;
            return false;
        }
#endif
    }
}
