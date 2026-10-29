#if UNITY_EDITOR
using UnityEditor;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Gallery pictures (Resources/Art/Images, written by Tools/music/import_group_b.py) keep their real size: the default
    /// "non power of two → nearest" import squeezed every painting to a square (640×480 became 512×512), so the gallery
    /// frames, the easel grid and the UI all had the wrong proportions.
    /// </summary>
    public sealed class ArtImagePostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/Images/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
#endif
