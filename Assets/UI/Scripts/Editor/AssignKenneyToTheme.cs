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
        private const string KenneyFontPath = "Assets/Art/UI/KenneySciFi/Font/Kenney Future Narrow.ttf";

        [MenuItem("Circuit One-Stroke/UI/Assign Kenney Sci-Fi to Theme")]
        public static void Assign()
        {
            SetKenneySprites.SetAsSprites();
            AssetDatabase.Refresh();

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
            AssignSprite(so, "sliderFillSprite", $"{KenneyBase}/Grey/Default/bar_round_gloss_small_m.png");
            AssignSprite(so, "toggleBackgroundSprite", $"{KenneyBase}/Grey/Default/bar_square_small_square.png");
            AssignSprite(so, "toggleCheckSprite", $"{KenneyBase}/Grey/Default/button_square_header_small_square.png");

            var font = AssetDatabase.LoadAssetAtPath<Font>(KenneyFontPath);
            if (font != null)
            {
                var fontProp = so.FindProperty("font");
                if (fontProp != null) fontProp.objectReferenceValue = font;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            Debug.Log("Kenney Sci-Fi sprites and font assigned to CircuitOneStrokeTheme.");
        }

        [MenuItem("Circuit One-Stroke/UI/Apply Kenney Theme (Full: Assign + Recreate Prefabs)")]
        public static void AssignAndRecreatePrefabs()
        {
            Assign();
            CreateScreenPrefabs.CreateAll();
            Debug.Log("Kenney theme applied and screen prefabs recreated. Play the game to see the Sci-Fi UI.");
        }

        /// <summary>Kenney 테마 할당 후 열린 씬의 Canvas에 테마 적용. 한 번에 Kenney UI 적용.</summary>
        [MenuItem("Circuit One-Stroke/UI/Apply Kenney UI (Theme + Current Scene)")]
        public static void ApplyKenneyToCurrentScene()
        {
            Assign();
            var theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>(ThemePath);
            if (theme == null) { Debug.LogWarning("Theme not found."); return; }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) { Debug.LogWarning("No Canvas in scene. Open AppScene or a scene with Canvas."); return; }

            var applier = canvas.GetComponent<ThemeApplier>();
            if (applier == null) applier = canvas.gameObject.AddComponent<ThemeApplier>();
            var so = new SerializedObject(applier);
            so.FindProperty("theme").objectReferenceValue = theme;
            so.ApplyModifiedPropertiesWithoutUndo();
            applier.Apply(theme);

            var all = canvas.GetComponentsInChildren<ThemeApplier>(true);
            foreach (var a in all)
            {
                if (a == applier) continue;
                a.Theme = theme;
                a.Apply(theme);
            }
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty == false)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("Kenney UI applied: theme assigned and applied to Canvas and all ThemeAppliers in scene.");
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

        private static readonly string[] ColorFallbacks = { "Blue", "Green", "Grey", "Yellow", "Red" };

        private static Sprite LoadSprite(string path)
        {
            var fileName = System.IO.Path.GetFileName(path);
            var nameOnly = System.IO.Path.GetFileNameWithoutExtension(fileName);
            var preferredDir = "";
            var slash = path.LastIndexOf('/');
            if (slash > 0)
            {
                var pathWithoutFile = path.Substring(0, slash);
                var lastSlash = pathWithoutFile.LastIndexOf('/');
                preferredDir = lastSlash >= 0 ? pathWithoutFile.Substring(lastSlash + 1) : pathWithoutFile;
            }
            // 1) 파일명으로 KenneyBase 아래 검색
            var guids = AssetDatabase.FindAssets($"{nameOnly} t:Texture2D", new[] { KenneyBase });
            string resolvedPath = null;
            string fallbackPath = null;
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (!p.EndsWith(fileName, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(preferredDir))
                    resolvedPath = p;
                else if (p.IndexOf(preferredDir, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    resolvedPath = p;
                    break;
                }
                else if (fallbackPath == null)
                    fallbackPath = p;
            }
            if (resolvedPath == null) resolvedPath = fallbackPath;
            if (resolvedPath != null)
            {
                var obj = AssetDatabase.LoadAssetAtPath<Sprite>(resolvedPath);
                if (obj != null) return obj;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(resolvedPath))
                {
                    if (o is Sprite s) return s;
                }
            }
            // 2) 고정 경로 직접 시도
            var direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (direct != null) return direct;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o is Sprite s) return s;
            }
            // 3) Blue 등 특정 색이 없을 수 있음 → 같은 파일명으로 다른 색 폴더 시도 (Default/Double 유지)
            var pathLower = path.Replace('\\', '/');
            foreach (var color in ColorFallbacks)
            {
                var altPath = pathLower.Contains("/Default/")
                    ? $"{KenneyBase}/{color}/Default/{fileName}"
                    : pathLower.Contains("/Double/")
                        ? $"{KenneyBase}/{color}/Double/{fileName}"
                        : null;
                if (altPath == null || altPath == path) continue;
                var alt = AssetDatabase.LoadAssetAtPath<Sprite>(altPath);
                if (alt != null) return alt;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(altPath))
                {
                    if (o is Sprite s) return s;
                }
            }
            return null;
        }
    }
}
#endif
