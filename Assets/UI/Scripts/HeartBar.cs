using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 하트 아이콘 바. filled/empty 상태 표시.
    /// </summary>
    public class HeartBar : MonoBehaviour
    {
        [SerializeField] private Image[] heartIcons;
        [SerializeField] private Text heartsText;
        [SerializeField] private Color filledColor = UIStyleConstants.Primary;
        [SerializeField] private Color emptyColor = UIStyleConstants.TextSecondary;

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
            if (heartsText != null)
                heartsText.text = $"{current}/{max}";
        }
    }
}
