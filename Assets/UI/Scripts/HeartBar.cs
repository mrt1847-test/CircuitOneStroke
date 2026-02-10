using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.UI.Theme;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 하트 아이콘 바. filled/empty 상태 표시.
    /// GameSettings.showIconAndText: true = icons + "x/5", false = icons only.
    /// </summary>
    public class HeartBar : MonoBehaviour
    {
        [SerializeField] private Image[] heartIcons;
        [SerializeField] private Text heartsText;
        [SerializeField] private Color filledColor = UIStyleConstants.Primary;
        [SerializeField] private Color emptyColor = UIStyleConstants.TextSecondary;

        private void OnEnable()
        {
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged += OnSettingsChanged;
            ApplyShowIconAndText();
        }

        private void OnDisable()
        {
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged -= OnSettingsChanged;
        }

        private void OnSettingsChanged(GameSettingsData _) => ApplyShowIconAndText();

        private void ApplyShowIconAndText()
        {
            if (heartsText != null)
                heartsText.gameObject.SetActive(GameSettings.Instance?.Data?.showIconAndText ?? true);
        }

        public void SetHearts(int current, int max)
        {
            if (heartIcons != null)
            {
                for (int i = 0; i < heartIcons.Length; i++)
                {
                    if (heartIcons[i] != null)
                        heartIcons[i].color = i < current ? filledColor : emptyColor;
                }
            }
            if (heartsText != null && heartsText.gameObject.activeSelf)
                heartsText.text = $"{current}/{max}";
        }
    }
}
