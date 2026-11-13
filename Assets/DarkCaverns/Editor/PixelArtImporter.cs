// Dark Caverns
// SPDX-License-Identifier: GPL-3.0-or-later

using UnityEditor;
using UnityEngine;

namespace DarkCaverns.Editor
{
    /// <summary>
    /// Imports the pixel art sheets crisp: no filtering, compression or mipmaps.
    /// The game slices them into sprites at runtime, so no per-sprite import data is needed.
    /// </summary>
    public sealed class PixelArtImporter : AssetPostprocessor
    {
        private const string SpriteFolder = "Assets/DarkCaverns/Resources/Sprites/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(SpriteFolder))
            {
                return;
            }

            var importer = (TextureImporter) assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
