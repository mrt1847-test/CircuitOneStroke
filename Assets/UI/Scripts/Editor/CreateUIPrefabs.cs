#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// 메뉴로 uGUI 프리팹 생성: Panel, Button, ProgressSlider. 테마 참조·ThemeApplier/ThemeRole 부착.
    /// </summary>
    public static class CreateUIPrefabs
    {
        private const string PrefabsPath = "Assets/UI/Prefabs";
        private const string ThemePath = "Assets/UI/Theme/CircuitOneStrokeTheme.asset";

        /// <summary>Theme 없으면 먼저 생성 후, Panel/Button/ProgressSlider 프리팹 저장. 씬에 오브젝트 있어야 SaveAsPrefabAsset 동작.</summary>
        [MenuItem("Circuit One-Stroke/UI/Create UI Prefabs (Panel, Button, Progress)")]
        public static void CreateAll()
        {
            CreateDefaultTheme.EnsureFolders();
            var theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>(ThemePath);
            if (theme == null)
            {
                CreateDefaultTheme.CreateTheme();
                theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>(ThemePath);
            }

            CreatePanelPrefab(theme);
            CreateButtonPrefab(theme);
            CreateProgressSliderPrefab(theme);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreatePanelPrefab(CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("Panel");
            var rect = root.AddComponent<RectTransform>();
            // 부모(ScreenRoot)를 채우도록 스트레치. 작은 패널이 필요하면 씬에서 별도 오브젝트로 조정.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24, 24);
            rect.offsetMax = new Vector2(-24, -24);

            var image = root.AddComponent<Image>();
            image.color = theme != null ? theme.panelBase : UIStyleConstants.PanelBase;
            image.raycastTarget = true;

            var role = root.AddComponent<ThemeRole>();
            role.role = ThemeRole.Role.Panel;

            var applier = root.AddComponent<ThemeApplier>();
            applier.Theme = theme;
            applier.Apply(theme);

            var content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(24, 24);
            contentRect.offsetMax = new Vector2(-24, -24);

            SavePrefab(root, PrefabsPath + "/Panel.prefab");
        }

        private static void CreateButtonPrefab(CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("Button");
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(160, 48);
            rect.anchoredPosition = Vector2.zero;

            var image = root.AddComponent<Image>();
            image.color = theme != null ? theme.primary : UIStyleConstants.Primary;
            image.raycastTarget = true;

            var role = root.AddComponent<ThemeRole>();
            role.role = ThemeRole.Role.Button;

            var button = root.AddComponent<Button>();
            button.targetGraphic = image;

            var applier = root.AddComponent<ThemeApplier>();
            applier.Theme = theme;
            applier.Apply(theme);

            var label = new GameObject("Label");
            label.transform.SetParent(root.transform, false);
            var labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = label.AddComponent<Text>();
            text.text = "Button";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 56;
            text.color = theme != null ? theme.textOnAccent : UIStyleConstants.TextOnAccent;
            if (theme != null && theme.font != null) text.font = theme.font;

            SavePrefab(root, PrefabsPath + "/Button.prefab");
        }

        private static void CreateProgressSliderPrefab(CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("ProgressSlider");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(200, 20);
            rootRect.anchoredPosition = Vector2.zero;

            var bg = new GameObject("Background");
            bg.transform.SetParent(root.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = theme != null ? theme.panelBorder : UIStyleConstants.PanelBorder;
            bgImage.raycastTarget = true;
            var bgRole = bg.AddComponent<ThemeRole>();
            bgRole.role = ThemeRole.Role.SliderBackground;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(root.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5, 4);
            fillAreaRect.offsetMax = new Vector2(-5, -4);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = theme != null ? theme.primary : UIStyleConstants.Primary;
            var fillRole = fill.AddComponent<ThemeRole>();
            fillRole.role = ThemeRole.Role.SliderFill;

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            slider.targetGraphic = bgImage;

            var applier = root.AddComponent<ThemeApplier>();
            applier.Theme = theme;
            applier.Apply(theme);

            SavePrefab(root, PrefabsPath + "/ProgressSlider.prefab");
        }

        private static void SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            if (prefab != null)
                Debug.Log("Created prefab: " + path);
        }
    }
}
#endif
