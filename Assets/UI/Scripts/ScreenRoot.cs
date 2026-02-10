using UnityEngine;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 화면 루트에 붙이면 배경색 + 테마를 자식에게 적용합니다.
    /// Canvas 하위에 두고 Theme 참조를 넣으면 전체 화면에 테마가 적용됩니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ScreenRoot : MonoBehaviour
    {
        [SerializeField] private CircuitOneStrokeTheme theme;
        [SerializeField] private bool applyThemeToChildren = true;

        private void Awake()
        {
            if (theme == null) return;

            if (TryGetComponent<UnityEngine.UI.Image>(out var img))
            {
                img.color = theme.background;
                img.raycastTarget = true;
            }

            var applier = GetComponent<ThemeApplier>();
            if (applier == null)
                applier = gameObject.AddComponent<ThemeApplier>();
            applier.Theme = theme;
            applier.Apply(theme);

            if (applyThemeToChildren)
            {
                foreach (var child in GetComponentsInChildren<ThemeApplier>(true))
                {
                    if (child == applier) continue;
                    child.Theme = theme;
                    child.Apply(theme);
                }
            }
        }
    }
}
