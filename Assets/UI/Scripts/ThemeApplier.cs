using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI.Theme
{
    /// <summary>
    /// 루트 또는 하위에 테마를 적용합니다. 스프라이트가 없으면 색상 플레이스홀더 사용.
    /// </summary>
    public class ThemeApplier : MonoBehaviour
    {
        [SerializeField] private CircuitOneStrokeTheme theme;
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private bool applyToChildren = true;

        public CircuitOneStrokeTheme Theme
        {
            get => theme;
            set => theme = value;
        }

        private void OnEnable()
        {
            if (applyOnEnable)
                Apply(theme);
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged += OnSettingsChanged;
        }

        private void OnDisable()
        {
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged -= OnSettingsChanged;
        }

        private void OnSettingsChanged(GameSettingsData _)
        {
            if (applyOnEnable) Apply(theme);
        }

        public void Apply(CircuitOneStrokeTheme t)
        {
            if (t == null) t = ScriptableObject.CreateInstance<CircuitOneStrokeTheme>();
            theme = t;
            ApplyRecursive(transform, t, applyToChildren);
        }

        public void Apply() => Apply(theme);

#if UNITY_EDITOR
        [ContextMenu("Apply Theme Now")]
        private void ApplyThemeInEditor()
        {
            if (theme != null) Apply(theme);
        }
#endif

        private static void ApplyRecursive(Transform root, CircuitOneStrokeTheme t, bool recurse)
        {
            ApplyToGameObject(root.gameObject, t);
            if (!recurse) return;
            for (int i = 0; i < root.childCount; i++)
                ApplyRecursive(root.GetChild(i), t, true);
        }

        private static void ApplyToGameObject(GameObject go, CircuitOneStrokeTheme t)
        {
            if (go.TryGetComponent<Image>(out var img))
                ApplyToImage(img, t);
            if (go.TryGetComponent<Text>(out var text))
                ApplyToText(text, t);
            if (go.TryGetComponent<Button>(out var btn))
                ApplyToButton(btn, t);
            if (go.TryGetComponent<Slider>(out var slider))
                ApplyToSlider(slider, t);
            if (go.TryGetComponent<Toggle>(out var toggle))
                ApplyToToggle(toggle, t);
        }

        private static void ApplyToImage(Image img, CircuitOneStrokeTheme t)
        {
            if (img == null || t == null) return;
            var role = img.GetComponent<ThemeRole>();
            if (role != null)
            {
                ApplyImageByRole(img, t, role.role);
                return;
            }
            img.color = Color.white;
            if (img.sprite == null && img.type == Image.Type.Simple)
                img.color = t.panelBase;
        }

        private static void ApplyImageByRole(Image img, CircuitOneStrokeTheme t, ThemeRole.Role role)
        {
            switch (role)
            {
                case ThemeRole.Role.Background:
                    img.sprite = t.panelSprite ?? img.sprite;
                    img.color = t.panelSprite != null ? Color.white : (UseHighContrast() ? t.highContrastBackground : t.background);
                    break;
                case ThemeRole.Role.Panel:
                    img.sprite = t.panelSprite ?? img.sprite;
                    img.color = t.panelSprite != null ? Color.white : t.panelBase;
                    break;
                case ThemeRole.Role.Button:
                    img.sprite = t.buttonSprite ?? img.sprite;
                    img.color = t.buttonSprite != null ? Color.white : t.primary;
                    break;
                case ThemeRole.Role.ButtonPressed:
                    img.sprite = t.buttonPressedSprite ?? t.buttonSprite ?? img.sprite;
                    img.color = t.buttonSprite != null ? Color.white : t.primaryDim;
                    break;
                case ThemeRole.Role.SliderBackground:
                    img.sprite = t.sliderBackgroundSprite ?? img.sprite;
                    img.color = t.sliderBackgroundSprite != null ? Color.white : t.panelBorder;
                    break;
                case ThemeRole.Role.SliderFill:
                    img.sprite = t.sliderFillSprite ?? img.sprite;
                    img.color = t.sliderFillSprite != null ? Color.white : t.primary;
                    break;
                case ThemeRole.Role.ToggleBackground:
                    img.sprite = t.toggleBackgroundSprite ?? img.sprite;
                    img.color = t.toggleBackgroundSprite != null ? Color.white : t.panelBorder;
                    break;
                case ThemeRole.Role.ToggleCheck:
                    img.sprite = t.toggleCheckSprite ?? img.sprite;
                    img.color = t.toggleCheckSprite != null ? Color.white : t.primary;
                    break;
                case ThemeRole.Role.Icon:
                    img.color = Color.white;
                    break;
                default:
                    if (img.sprite == null) img.color = t.panelBase;
                    else img.color = Color.white;
                    break;
            }
        }

        private static void ApplyToText(Text text, CircuitOneStrokeTheme t)
        {
            if (text == null || t == null) return;
            text.color = UseHighContrast() ? t.highContrastTextPrimary : t.textPrimary;
            if (t.font != null)
                text.font = t.font;
            var scaler = text.GetComponent<AccessibilityTextScaler>();
            if (scaler == null)
            {
                scaler = text.gameObject.AddComponent<AccessibilityTextScaler>();
                // designFontSize will be captured from current fontSize in Awake
            }
            scaler.ApplyScale();
        }

        private static bool UseHighContrast() => GameSettings.Instance?.Data?.highContrastUI ?? false;

        private static void ApplyToButton(Button btn, CircuitOneStrokeTheme t)
        {
            if (btn == null || t == null) return;
            var img = btn.targetGraphic as Image;
            if (img != null)
            {
                img.sprite = t.buttonSprite ?? img.sprite;
                img.color = t.buttonSprite != null ? Color.white : t.primary;
            }
            var colors = btn.colors;
            colors.normalColor = t.buttonSprite != null ? Color.white : t.primary;
            colors.highlightedColor = t.buttonSprite != null ? new Color(0.9f, 1f, 1f, 1f) : t.primaryDim;
            colors.pressedColor = t.buttonSprite != null ? new Color(0.8f, 0.95f, 0.95f, 1f) : t.primaryDim;
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;
        }

        private static void ApplyToSlider(Slider slider, CircuitOneStrokeTheme t)
        {
            if (slider == null || t == null) return;
            if (slider.fillRect != null && slider.fillRect.TryGetComponent<Image>(out var fillImg))
            {
                fillImg.sprite = t.sliderFillSprite ?? fillImg.sprite;
                fillImg.color = t.sliderFillSprite != null ? Color.white : t.primary;
            }
            if (slider.targetGraphic is Image bgImg)
            {
                bgImg.sprite = t.sliderBackgroundSprite ?? bgImg.sprite;
                bgImg.color = t.sliderBackgroundSprite != null ? Color.white : t.panelBorder;
            }
        }

        private static void ApplyToToggle(Toggle toggle, CircuitOneStrokeTheme t)
        {
            if (toggle == null || t == null) return;
            if (toggle.targetGraphic is Image bgImg)
            {
                bgImg.sprite = t.toggleBackgroundSprite ?? bgImg.sprite;
                bgImg.color = t.toggleBackgroundSprite != null ? Color.white : t.panelBorder;
            }
            if (toggle.graphic is Image checkImg)
            {
                checkImg.sprite = t.toggleCheckSprite ?? checkImg.sprite;
                checkImg.color = t.toggleCheckSprite != null ? Color.white : t.primary;
            }
        }
    }
}
