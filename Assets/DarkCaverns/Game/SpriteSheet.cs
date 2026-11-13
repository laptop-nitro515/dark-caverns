// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using UnityEngine;

namespace DarkCaverns.Game
{
    /// <summary>
    /// A texture from Resources/Sprites cut into equal frames, counted left to right
    /// and top to bottom like SPD's TextureFilm.
    /// </summary>
    public sealed class SpriteSheet
    {
        public const float PixelsPerUnit = 16f;

        private readonly Texture2D texture;
        private readonly int frameWidth;
        private readonly int frameHeight;
        private readonly Vector2 pivot;
        private readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();

        public SpriteSheet(string name, int frameWidth, int frameHeight, Vector2 pivot)
        {
            texture = Resources.Load<Texture2D>("Sprites/" + name);
            if (texture == null)
            {
                throw new MissingReferenceException("Missing sprite sheet Resources/Sprites/" + name);
            }

            texture.filterMode = FilterMode.Point;
            this.frameWidth = frameWidth;
            this.frameHeight = frameHeight;
            this.pivot = pivot;
        }

        public int Columns => texture.width / frameWidth;

        public Sprite this[int index]
        {
            get
            {
                if (!cache.TryGetValue(index, out var sprite))
                {
                    var col = index % Columns;
                    var row = index / Columns;

                    // Unity textures start at the bottom-left; SPD's frames count from the top.
                    var rect = new Rect(col * frameWidth, texture.height - (row + 1) * frameHeight, frameWidth, frameHeight);
                    sprite = Sprite.Create(texture, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                    sprite.name = texture.name + "_" + index;
                    cache[index] = sprite;
                }

                return sprite;
            }
        }
    }
}
