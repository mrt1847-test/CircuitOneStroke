#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// 메뉴로 기본 CircuitOneStrokeTheme 에셋 생성. UI/Theme 폴더 및 Theme/Prefabs 폴더 자동 생성.
    /// </summary>
    public static class CreateDefaultTheme
    {
        private const string ThemePath = "Assets/UI/Theme/CircuitOneStrokeTheme.asset";

        /// <summary>Theme가 없을 때만 생성. 있으면 선택만 함.</summary>
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

        /// <summary>Assets/UI, UI/Theme, UI/Prefabs 폴더가 없으면 생성.</summary>
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
