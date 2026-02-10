#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// KenneySciFi PNG 폴더 하위 텍스처를 Sprite (2D and UI)로 일괄 설정합니다.
    /// 테마에 스프라이트를 할당하려면 PNG가 Sprite 타입이어야 합니다.
    /// </summary>
    public static class SetKenneySprites
    {
        private const string KenneyPngPath = "Assets/Art/UI/KenneySciFi/PNG";

        [MenuItem("Circuit One-Stroke/UI/Set KenneySciFi PNGs as Sprites")]
        public static void SetAsSprites()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art/UI/KenneySciFi") ||
                !AssetDatabase.IsValidFolder("Assets/Art/UI/KenneySciFi/PNG"))
            {
                Debug.LogWarning("KenneySciFi/PNG folder not found. Import Kenney UI pack to Assets/Art/UI/KenneySciFi first.");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { KenneyPngPath });
            int count = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (importer.textureType == TextureImporterType.Sprite) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsToUnits = 100;
                importer.SaveAndReimport();
                count++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Set {count} texture(s) under {KenneyPngPath} to Sprite (2D and UI). Already sprites were skipped.");
        }
    }
}
#endif
