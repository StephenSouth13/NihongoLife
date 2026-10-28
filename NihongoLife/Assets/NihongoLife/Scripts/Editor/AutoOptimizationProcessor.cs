using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NihongoLife.Editor
{
    /// <summary>
    /// Tự động tối ưu hóa toàn bộ Asset (Hình ảnh, Âm thanh, Mô hình 3D) ngay khi vừa đưa vào Unity.
    /// Hoạt động hoàn toàn ngầm (Không sinh Menu, Không cần bấm tay) - Tuân thủ tuyệt đối quy tắc dự án.
    /// </summary>
    public class AutoOptimizationProcessor : AssetPostprocessor
    {
        // 1. Tự động nén Hình ảnh (Textures)
        void OnPreprocessTexture()
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            bool ui = textureImporter.textureType == TextureImporterType.Sprite || textureImporter.textureType == TextureImporterType.GUI;
            bool keepSharp = ui || textureImporter.textureType == TextureImporterType.NormalMap || textureImporter.textureType == TextureImporterType.Lightmap;

            // Nếu là UI thì giữ nguyên chất lượng, nếu là Texture 3D thì nén mạnh
            if (!ui)
            {
                textureImporter.crunchedCompression = !keepSharp;
                textureImporter.compressionQuality = 50; // Giảm dung lượng tới 70% mà mắt thường khó nhận ra
                textureImporter.mipmapEnabled = true;    // Tối ưu RAM khi nhìn xa
            }

            // WebGL player only. Desktop and Android keep their own platform settings.
            textureImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "WebGL",
                overridden = true,
                maxTextureSize = 1024,
                resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
                format = TextureImporterFormat.Automatic,
                textureCompression = TextureImporterCompression.Compressed,
                crunchedCompression = !keepSharp,
                compressionQuality = ui ? 60 : 50
            });
        }

        // 2. Tự động nén Âm thanh (Audio)
        void OnPreprocessAudio()
        {
            AudioImporter audioImporter = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = audioImporter.defaultSampleSettings;

            // Chuyển toàn bộ âm thanh thành Mono (giảm 50% dung lượng) trừ khi là nhạc nền (BGM)
            if (!assetPath.ToLower().Contains("bgm") && !assetPath.ToLower().Contains("music"))
            {
                audioImporter.forceToMono = true;
            }

            settings.loadType = AudioClipLoadType.CompressedInMemory; // Tiết kiệm RAM
            // Nhạc nền và âm thanh môi trường dài (Soundscape): phát kiểu Streaming, không nạp cả bài vào RAM.
            string lower = assetPath.ToLower();
            bool music = lower.Contains("/audio/music/") || lower.Contains("/audio/ambience/") || lower.Contains("/bgm/") || lower.Contains("theme") || lower.Contains("jingle");
            if (music) settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.5f; // Chất lượng vừa đủ nghe rõ

            audioImporter.defaultSampleSettings = settings;

            long bytes = 0;
            try { bytes = new FileInfo(assetPath).Length; } catch (IOException) { bytes = 0; }
            var web = settings;
            web.compressionFormat = AudioCompressionFormat.Vorbis;
            web.quality = music ? 0.45f : 0.38f;
            web.loadType = music || bytes > 700_000
                ? AudioClipLoadType.Streaming
                : AudioClipLoadType.CompressedInMemory;
            web.preloadAudioData = false;
            if (!music)
            {
                web.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                web.sampleRateOverride = 22050;
            }
            audioImporter.SetOverrideSampleSettings("WebGL", web);
        }

        // 3. Tự động tối ưu Mô hình 3D (Models)
        void OnPreprocessModel()
        {
            ModelImporter modelImporter = (ModelImporter)assetImporter;

            // Tắt Import Camera và Light từ file 3D (thường gây lỗi rác Scene)
            modelImporter.importCameras = false;
            modelImporter.importLights = false;

            // Nén Mesh để giảm dung lượng file Build
            modelImporter.meshCompression = ModelImporterMeshCompression.Medium;
            
            // Nếu model không có animation thì tắt Rig để tối ưu CPU
            if (!assetPath.ToLower().Contains("anim") && !assetPath.ToLower().Contains("character"))
            {
                modelImporter.animationType = ModelImporterAnimationType.None;
            }
        }
    }

    /// <summary>
    /// Tự động can thiệp vào quá trình Build Game để dọn rác và ép cấu hình nhẹ nhất.
    /// </summary>
    public class PreBuildOptimizer : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[AutoOptimizer] Đang tự động cấu hình tối ưu hóa trước khi Build...");

            // Ép hệ thống dọn dẹp RAM dư thừa
            Resources.UnloadUnusedAssets();

            // Tự động bật Code Stripping mức Cao Nhất (Xóa toàn bộ Code thư viện không xài để giảm Size)
            NamedBuildTarget named = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
            if (named != NamedBuildTarget.Unknown)
                PlayerSettings.SetManagedStrippingLevel(named, ManagedStrippingLevel.High);

            Debug.Log("[AutoOptimizer] Đã ép Code Stripping lên HIGH. Build sẽ rất nhẹ!");
        }
    }
}
