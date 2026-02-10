using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI
{
    /// <summary>라벨 + 토글 행.</summary>
    public class ToggleRow : MonoBehaviour
    {
        [SerializeField] private Text labelText;
        [SerializeField] private Toggle toggle;

        public string Label { get => labelText != null ? labelText.text : ""; set { if (labelText != null) labelText.text = value; } }
        public bool IsOn { get => toggle != null && toggle.isOn; set { if (toggle != null) toggle.isOn = value; } }
        public Toggle.ToggleEvent OnValueChanged => toggle != null ? toggle.onValueChanged : null;
    }

    /// <summary>라벨 + 슬라이더 + 값 텍스트 행.</summary>
    public class SliderRow : MonoBehaviour
    {
        [SerializeField] private Text labelText;
        [SerializeField] private Slider slider;
        [SerializeField] private Text valueText;

        public string Label { get => labelText != null ? labelText.text : ""; set { if (labelText != null) labelText.text = value; } }
        public float Value { get => slider != null ? slider.value : 0; set { if (slider != null) slider.value = value; RefreshValueText(); } }
        public float MinValue { get => slider != null ? slider.minValue : 0; set { if (slider != null) slider.minValue = value; } }
        public float MaxValue { get => slider != null ? slider.maxValue : 1; set { if (slider != null) slider.maxValue = value; } }
        public bool Interactable { get => slider != null && slider.interactable; set { if (slider != null) slider.interactable = value; } }
        public Slider.SliderEvent OnValueChanged => slider != null ? slider.onValueChanged : null;

        public void RefreshValueText()
        {
            if (valueText != null && slider != null)
                valueText.text = slider.wholeNumbers ? ((int)slider.value).ToString() : slider.value.ToString("F1");
        }
    }

    /// <summary>라벨 + 드롭다운 행.</summary>
    public class DropdownRow : MonoBehaviour
    {
        [SerializeField] private Text labelText;
        [SerializeField] private Dropdown dropdown;

        public string Label { get => labelText != null ? labelText.text : ""; set { if (labelText != null) labelText.text = value; } }
        public int Value { get => dropdown != null ? dropdown.value : 0; set { if (dropdown != null) dropdown.value = value; } }
        public Dropdown.DropdownEvent OnValueChanged => dropdown != null ? dropdown.onValueChanged : null;
    }

    /// <summary>전체 너비 버튼 행.</summary>
    public class ButtonRow : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text buttonText;

        public string Label { get => buttonText != null ? buttonText.text : ""; set { if (buttonText != null) buttonText.text = value; } }
        public Button Button => button;
    }
}
