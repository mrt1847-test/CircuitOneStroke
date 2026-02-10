#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    public static class CreateDefaultTheme
    {
        private const string ThemePath = "Assets/UI/Theme/CircuitOneStrokeTheme.asset";

        [MenuItem("Circuit One-Stroke/UI/Create Default Theme")]
        public static void CreateTheme()
        {
            EnsureFolders();
            var existing = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>(ThemePath);
            if (existing != null)
            {
                Debug.Log("Theme already exists: " + ThemePath);
                Selection.activeObject = existing;
                return;
            }
            var theme = ScriptableObject.CreateInstance<CircuitOneStrokeTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created theme: " + ThemePath);
            Selection.activeObject = theme;
        }

        public static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI"))
                AssetDatabase.CreateFolder("Assets", "UI");
            if (!AssetDatabase.IsValidFolder("Assets/UI/Theme"))
                AssetDatabase.CreateFolder("Assets/UI", "Theme");
            if (!AssetDatabase.IsValidFolder("Assets/UI/Prefabs"))
                AssetDatabase.CreateFolder("Assets/UI", "Prefabs");
        }
    }
}
#endif
