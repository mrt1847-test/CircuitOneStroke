using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// Large Text 설정 시 폰트 크기 스케일 적용. designFontSize를 저장해 복합 적용 방지.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class AccessibilityTextScaler : MonoBehaviour
    {
        [Tooltip("디자인 시 폰트 크기. 비워두면 현재 크기를 사용.")]
        [SerializeField] private int designFontSize = 0;

        private Text _text;
        private int _baseSize;

        private void Awake()
        {
            _text = GetComponent<Text>();
            _baseSize = designFontSize > 0 ? designFontSize : _text.fontSize;
        }

        private void OnEnable()
        {
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged += OnSettingsChanged;
            ApplyScale();
        }

        private void OnDisable()
        {
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged -= OnSettingsChanged;
        }

        private void OnSettingsChanged(GameSettingsData _) => ApplyScale();

        public void ApplyScale()
        {
            if (_text == null) return;
            if (_baseSize <= 0) _baseSize = _text.fontSize;
            _text.fontSize = Mathf.RoundToInt(_baseSize * UIStyleConstants.FontScale);
        }
    }
}
