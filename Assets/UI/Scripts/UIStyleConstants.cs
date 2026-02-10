using UnityEngine;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI.Theme
{
    /// <summary>
    /// 라이트 테마: 흰 배경, 그린 상·하단 바, 블루 게임 요소. 테마 에셋 없을 때 폴백.
    /// </summary>
    public static class UIStyleConstants
    {
        // 배경 — 흰색 (게임 영역·화면)
        public static readonly Color Background = new Color(1f, 1f, 1f, 1f);
        public static readonly Color PanelBase = new Color(0.96f, 0.96f, 0.98f, 1f);
        public static readonly Color PanelBorder = new Color(0.88f, 0.88f, 0.92f, 1f);

        // 상·하단 바·버튼 — 밝은 그린
        public static readonly Color Primary = new Color(0.18f, 0.75f, 0.42f, 1f);
        public static readonly Color PrimaryDim = new Color(0.14f, 0.6f, 0.35f, 1f);

        // 게임 요소(노드·선) — 블루
        public static readonly Color Secondary = new Color(0.25f, 0.55f, 0.95f, 1f);
        public static readonly Color SecondaryDim = new Color(0.2f, 0.45f, 0.85f, 1f);
        /// <summary>미연결/비활성 선용 연회색.</summary>
        public static readonly Color WireInactive = new Color(0.85f, 0.85f, 0.88f, 1f);

        // 경고/실패
        public static readonly Color Warning = new Color(1f, 0.75f, 0.2f, 1f);
        public static readonly Color Danger = new Color(0.95f, 0.25f, 0.2f, 1f);

        // 텍스트 — 흰 배경 위에는 진한 색, 그린 위에는 흰색
        public static readonly Color TextPrimary = new Color(0.12f, 0.12f, 0.18f, 1f);
        public static readonly Color TextSecondary = new Color(0.45f, 0.45f, 0.55f, 1f);
        public static readonly Color TextOnAccent = new Color(1f, 1f, 1f, 1f);

        public static float FontScale => GameSettings.Instance?.Data?.largeText == true ? 1.15f : 1f;
    }
}
