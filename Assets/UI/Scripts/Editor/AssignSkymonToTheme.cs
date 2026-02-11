#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// Skymon Icons (skymon-icons-white) PNG를 CircuitOneStrokeTheme 아이콘 슬롯에 할당합니다.
    /// Kenney 폰트는 테마의 font 필드를 수동으로 할당하거나 별도 메뉴로 연결하세요.
    /// </summary>
    public static class AssignSkymonToTheme
    {
        private const string ThemePath = "Assets/UI/Theme/CircuitOneStrokeTheme.asset";
        private const string SkymonBase = "Assets/Art/UI/SkymonIcons/skymon-icons-white";

        [MenuItem("Circuit One-Stroke/UI/Assign Skymon Icons to Theme")]
        public static void Assign()
        {
            var theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>(ThemePath);
            if (theme == null)
            {
                Debug.LogError("CircuitOneStrokeTheme not found at " + ThemePath);
                return;
            }

            var so = new SerializedObject(theme);

            AssignSprite(so, "iconPlay", $"{SkymonBase}/rocket.png");
            AssignSprite(so, "iconPause", $"{SkymonBase}/pause.png");
            AssignSprite(so, "iconSettings", $"{SkymonBase}/settings.png");
            AssignSprite(so, "iconRetry", $"{SkymonBase}/spiral.png");
            AssignSprite(so, "iconLevel", $"{SkymonBase}/puzzle.png");
            AssignSprite(so, "iconBack", $"{SkymonBase}/arrow-big-left.png");
            AssignSprite(so, "iconShop", $"{SkymonBase}/rewards.png");
            AssignSprite(so, "iconHome", $"{SkymonBase}/home.png");

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            Debug.Log("Skymon Icons assigned to CircuitOneStrokeTheme (Assets/Art/UI/SkymonIcons).");
        }

        private static void AssignSprite(SerializedObject so, string propName, string path)
        {
            var prop = so.FindProperty(propName);
            if (prop == null) return;

            var sprite = LoadSprite(path);
            if (sprite != null)
                prop.objectReferenceValue = sprite;
            else
                Debug.LogWarning($"AssignSkymonToTheme: Could not load {path}");
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
