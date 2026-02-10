using UnityEngine;

namespace CircuitOneStroke.UI.Theme
{
    /// <summary>
    /// Circuit One-Stroke UI 스타일 상수 (회로/사이버 테마).
    /// 테마 에셋이 없을 때 폴백으로 사용됩니다.
    /// </summary>
    public static class UIStyleConstants
    {
        // 배경 — 매우 어두운 네이비/블랙 (회로판)
        public static readonly Color Background = new Color(0.06f, 0.07f, 0.12f, 1f);
        public static readonly Color PanelBase = new Color(0.08f, 0.10f, 0.16f, 0.95f);
        public static readonly Color PanelBorder = new Color(0.15f, 0.18f, 0.25f, 1f);

        // 강조 — 네온 틸 (primary)
        public static readonly Color Primary = new Color(0f, 0.85f, 0.75f, 1f);
        public static readonly Color PrimaryDim = new Color(0f, 0.55f, 0.50f, 1f);

        // 보조 — 소프트 블루 (secondary)
        public static readonly Color Secondary = new Color(0.35f, 0.55f, 0.85f, 1f);
        public static readonly Color SecondaryDim = new Color(0.25f, 0.40f, 0.60f, 1f);

        // 경고/실패 — 앰버·레드
        public static readonly Color Warning = new Color(1f, 0.75f, 0.2f, 1f);
        public static readonly Color Danger = new Color(0.95f, 0.25f, 0.2f, 1f);

        // 텍스트
        public static readonly Color TextPrimary = new Color(0.95f, 0.97f, 1f, 1f);
        public static readonly Color TextSecondary = new Color(0.65f, 0.72f, 0.85f, 1f);
        public static readonly Color TextOnAccent = new Color(0.06f, 0.08f, 0.12f, 1f);
    }
}
