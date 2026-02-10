using UnityEngine;

namespace CircuitOneStroke.UI.Theme
{
    /// <summary>
    /// Circuit One-Stroke 전역 UI 테마 (uGUI).
    /// Kenney Sci-Fi 스프라이트와 Skymon 아이콘 참조 + 색상/폰트.
    /// 스프라이트가 비어 있으면 테마 색상으로 플레이스홀더 표시.
    /// </summary>
    [CreateAssetMenu(fileName = "CircuitOneStrokeTheme", menuName = "Circuit One-Stroke/UI Theme", order = 1)]
    public class CircuitOneStrokeTheme : ScriptableObject
    {
        [Header("Colors")]
        public Color background = UIStyleConstants.Background;
        public Color panelBase = UIStyleConstants.PanelBase;
        public Color panelBorder = UIStyleConstants.PanelBorder;
        public Color primary = UIStyleConstants.Primary;
        public Color primaryDim = UIStyleConstants.PrimaryDim;
        public Color secondary = UIStyleConstants.Secondary;
        public Color secondaryDim = UIStyleConstants.SecondaryDim;
        public Color warning = UIStyleConstants.Warning;
        public Color danger = UIStyleConstants.Danger;
        public Color textPrimary = UIStyleConstants.TextPrimary;
        public Color textSecondary = UIStyleConstants.TextSecondary;
        public Color textOnAccent = UIStyleConstants.TextOnAccent;

        [Header("Font (optional)")]
        public Font font;

        [Header("Kenney Sci-Fi — Panels/Buttons/Progress (optional)")]
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public Sprite buttonPressedSprite;
        public Sprite sliderBackgroundSprite;
        public Sprite sliderFillSprite;
        public Sprite toggleBackgroundSprite;
        public Sprite toggleCheckSprite;

        [Header("Skymon Icons — optional white PNGs")]
        public Sprite iconPlay;
        public Sprite iconPause;
        public Sprite iconSettings;
        public Sprite iconRetry;
        public Sprite iconLevel;
        public Sprite iconBack;

        [Header("High Contrast (Accessibility)")]
        public Color highContrastBackground = new Color(0.02f, 0.03f, 0.06f, 1f);
        public Color highContrastTextPrimary = Color.white;
        public Color highContrastPanelBorder = new Color(0.4f, 0.5f, 0.7f, 1f);

        /// <summary>패널용 색. 스프라이트 있으면 white, 없으면 panelBase.</summary>
        public Color GetPanelColor() => panelSprite != null ? Color.white : panelBase;
        /// <summary>버튼용 색. 스프라이트 있으면 white, 없으면 primary.</summary>
        public Color GetButtonColor() => buttonSprite != null ? Color.white : primary;
        public Color GetButtonPressedColor() => buttonPressedSprite != null ? Color.white : primaryDim;
        public Color GetSliderBackgroundColor() => sliderBackgroundSprite != null ? Color.white : panelBorder;
        public Color GetSliderFillColor() => sliderFillSprite != null ? Color.white : primary;
    }
}
