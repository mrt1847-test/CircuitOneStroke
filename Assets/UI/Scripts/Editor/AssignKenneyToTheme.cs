#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// Kenney Sci-Fi PNG를 CircuitOneStrokeTheme에 할당합니다.
    /// Set KenneySciFi PNGs as Sprites를 먼저 실행하세요.
    /// </summary>
    public static class AssignKenneyToTheme
    {
        private const string ThemePath = "Assets/UI/Theme/CircuitOneStrokeTheme.asset";
        private const string KenneyBase = "Assets/Art/UI/KenneySciFi/PNG";

        [MenuItem("Circuit One-Stroke/UI/Assign Kenney Sci-Fi to Theme")]
        public static void Assign()
        {
            SetKenneySprites.SetAsSprites();

            var theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>(ThemePath);
            if (theme == null)
            {
                CreateDefaultTheme.CreateTheme();
                theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>(ThemePath);
            }
            if (theme == null)
            {
                Debug.LogError("Could not load or create CircuitOneStrokeTheme.");
                return;
            }

            var so = new SerializedObject(theme);

            AssignSprite(so, "panelSprite", $"{KenneyBase}/Extra/Double/panel_rectangle.png");
            AssignSprite(so, "buttonSprite", $"{KenneyBase}/Grey/Default/button_square_header_large_rectangle.png");
            AssignSprite(so, "buttonPressedSprite", $"{KenneyBase}/Grey/Default/button_square_header_notch_rectangle.png");
            AssignSprite(so, "sliderBackgroundSprite", $"{KenneyBase}/Grey/Default/bar_round_gloss_small_m.png");
            AssignSprite(so, "sliderFillSprite", $"{KenneyBase}/Blue/Default/bar_round_gloss_small_m.png");
            AssignSprite(so, "toggleBackgroundSprite", $"{KenneyBase}/Grey/Default/bar_square_small_square.png");
            AssignSprite(so, "toggleCheckSprite", $"{KenneyBase}/Blue/Default/button_square_header_small_square.png");

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            Debug.Log("Kenney Sci-Fi sprites assigned to CircuitOneStrokeTheme.");
        }

        [MenuItem("Circuit One-Stroke/UI/Apply Kenney Theme (Full: Assign + Recreate Prefabs)")]
        public static void AssignAndRecreatePrefabs()
        {
            Assign();
            CreateScreenPrefabs.CreateAll();
            Debug.Log("Kenney theme applied and screen prefabs recreated. Play the game to see the Sci-Fi UI.");
        }

        private static void AssignSprite(SerializedObject so, string propName, string path)
        {
            var prop = so.FindProperty(propName);
            if (prop == null) return;

            var sprite = LoadSprite(path);
            if (sprite != null)
            {
                prop.objectReferenceValue = sprite;
            }
            else
            {
                Debug.LogWarning($"AssignKenneyToTheme: Could not load {path}");
            }
        }

        private static Sprite LoadSprite(string path)
        {
            var obj = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (obj != null) return obj;
            var objs = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var o in objs)
            {
                if (o is Sprite s) return s;
            }
            return null;
        }
    }
}
#endif
