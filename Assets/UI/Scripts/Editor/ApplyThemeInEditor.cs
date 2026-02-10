#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CircuitOneStroke.UI;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// 에디터에서 플레이 없이 선택한 ScreenRoot/ThemeApplier에 테마를 적용합니다.
    /// 테마 슬롯에 Kenney/Skymon 스프라이트를 넣은 뒤 여기서 적용하면 씬에 바로 반영됩니다.
    /// </summary>
    public static class ApplyThemeInEditor
    {
        [MenuItem("Circuit One-Stroke/UI/Apply Theme to Selected", true)]
        private static bool ValidateApplyThemeToSelected()
        {
            var go = Selection.activeGameObject;
            return go != null && (go.GetComponent<ScreenRoot>() != null || go.GetComponent<ThemeApplier>() != null);
        }

        [MenuItem("Circuit One-Stroke/UI/Apply Theme to Selected", false)]
        public static void ApplyThemeToSelected()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;

            CircuitOneStrokeTheme theme = null;
            var screenRoot = go.GetComponent<ScreenRoot>();
            if (screenRoot != null)
            {
                var so = new SerializedObject(screenRoot);
                var themeProp = so.FindProperty("theme");
                if (themeProp != null)
                    theme = themeProp.objectReferenceValue as CircuitOneStrokeTheme;
            }

            if (theme == null)
            {
                var applier = go.GetComponent<ThemeApplier>();
                if (applier != null)
                    theme = applier.Theme;
            }

            if (theme == null)
            {
                Debug.LogWarning("Apply Theme to Selected: No theme assigned on ScreenRoot or ThemeApplier.");
                return;
            }

            var appliers = go.GetComponentsInChildren<ThemeApplier>(true);
            foreach (var applier in appliers)
            {
                applier.Theme = theme;
                applier.Apply(theme);
                if (PrefabUtility.IsPartOfPrefabInstance(applier.gameObject))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(applier);
            }

            Debug.Log($"Applied theme to {appliers.Length} ThemeApplier(s) under {go.name}.");
        }
    }
}
#endif
