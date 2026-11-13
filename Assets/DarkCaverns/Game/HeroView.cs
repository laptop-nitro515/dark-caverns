// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using UnityEngine;

namespace DarkCaverns.Game
{
    /// <summary>The hero's sprite: SPD's warrior sheet, idle and run animations, smooth steps.</summary>
    public sealed class HeroView : MonoBehaviour
    {
        // SPD's HeroSprite: 12x15 frames, one row per armour tier.
        private static readonly int[] IdleFrames = { 0, 0, 0, 1, 0, 0, 1, 1 };
        private static readonly int[] RunFrames = { 2, 3, 4, 5, 6, 7 };
        private const float IdleFps = 1f;
        private const float RunFps = 20f;

        private SpriteRenderer spriteRenderer;
        private SpriteSheet sheet;
        private int framesPerTier;
        private int tier;
        private Vector3 from;
        private Vector3 to;
        private float moveTime;
        private float moveDuration;
        private float animTime;

        public bool IsMoving => moveTime < moveDuration;

        public static HeroView Create(Transform parent)
        {
            var go = new GameObject("Hero", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<HeroView>();
            view.spriteRenderer = go.GetComponent<SpriteRenderer>();
            view.spriteRenderer.sortingOrder = 10;

            // Feet at the bottom of the cell, as in SPD.
            view.sheet = new SpriteSheet("warrior", 12, 15, new Vector2(0.5f, 0f));
            view.framesPerTier = view.sheet.Columns;
            return view;
        }

        /// <summary>Armour tier 0 to 6 picks the sheet row, as SPD does.</summary>
        public int Tier
        {
            get => tier;
            set => tier = Mathf.Clamp(value, 0, 6);
        }

        public void Place(Vector3 cellCenter)
        {
            from = to = Feet(cellCenter);
            transform.position = to;
            moveTime = moveDuration = 0f;
        }

        public void StepTo(Vector3 cellCenter, float duration)
        {
            from = transform.position;
            to = Feet(cellCenter);
            if (to.x != from.x)
            {
                spriteRenderer.flipX = to.x < from.x;
            }

            moveTime = 0f;
            moveDuration = Mathf.Max(0.0001f, duration);
        }

        private static Vector3 Feet(Vector3 cellCenter) => cellCenter + new Vector3(0f, -0.5f, 0f);

        private void Update()
        {
            animTime += Time.deltaTime;

            if (IsMoving)
            {
                moveTime += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(moveTime / moveDuration));
                SetFrame(RunFrames[(int) (animTime * RunFps) % RunFrames.Length]);
            }
            else
            {
                SetFrame(IdleFrames[(int) (animTime * IdleFps) % IdleFrames.Length]);
            }
        }

        private void SetFrame(int frame)
        {
            spriteRenderer.sprite = sheet[tier * framesPerTier + frame];
        }
    }
}
