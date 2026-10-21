using UnityEditor;

namespace NihongoLife.Editor
{
    /// <summary>
    /// Interface icons and coin art (Resources/UI/Icons) import as crisp, uncompressed sprites: no mipmaps, no crunch,
    /// alpha as transparency. Runs after AutoOptimizationProcessor (which would crunch them as 3D textures).
    /// </summary>
    public sealed class UiIconImporter : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 100;

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Resources/UI/Icons/") && !path.Contains("/Resources/UI/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.crunchedCompression = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
            importer.spritePixelsPerUnit = 100f;
            // 9-slice borders from the file name: round_16 / stroke_16 → 17 px, shadow_22 → radius + blur.
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            var m = System.Text.RegularExpressions.Regex.Match(file, @"^(round|stroke|shadow)_(\d+)$");
            if (m.Success)
            {
                int r = int.Parse(m.Groups[2].Value);
                float border = m.Groups[1].Value == "shadow" ? r + 26 + 4 : r + 1;
                importer.spriteBorder = new UnityEngine.Vector4(border, border, border, border);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect; // sliced sprites need full-rect meshes
                importer.SetTextureSettings(settings);
            }
        }
    }
}
