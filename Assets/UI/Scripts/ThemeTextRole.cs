using UnityEngine;
using UnityEngine.UI;

namespace CircuitOneStroke.UI.Theme
{
    /// <summary>
    /// Text에 붙이면 ThemeApplier가 그린 바 등 강조 배경 위의 텍스트로 보고 textOnAccent(흰색) 적용.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class ThemeTextRole : MonoBehaviour
    {
        [Tooltip("true면 테마의 textOnAccent(그린 바 위 흰 텍스트) 적용")]
        public bool useAccentColor = true;
    }
}
