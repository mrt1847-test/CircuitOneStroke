using UnityEngine;
using UnityEngine.UI;

namespace CircuitOneStroke.UI.Theme
{
    /// <summary>
    /// UI 요소의 테마 역할. ThemeApplier가 이 역할에 맞는 스프라이트/색상을 적용합니다.
    /// </summary>
    public class ThemeRole : MonoBehaviour
    {
        public enum Role
        {
            None,
            Background,
            Panel,
            Button,
            ButtonPressed,
            SliderBackground,
            SliderFill,
            ToggleBackground,
            ToggleCheck,
            Icon
        }

        public Role role = Role.None;

        private void Reset()
        {
            if (TryGetComponent<Image>(out _))
                role = Role.Panel;
        }
    }
}
