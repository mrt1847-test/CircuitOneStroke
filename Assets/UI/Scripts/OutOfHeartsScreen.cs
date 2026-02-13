using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 하트 0일 때 표시되는 전체 화면. Watch Ad + Back to Home.
    /// </summary>
    public class OutOfHeartsScreen : MonoBehaviour, IUIScreen
    {
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button backButton;
        [SerializeField] private MonoBehaviour adServiceComponent;

        private UIScreenRouter _router;
        private CircuitOneStrokeTheme _theme;

        public void BindRouter(UIScreenRouter router)
        {
            _router = router;
        }

        private void Start()
        {
            if (watchAdButton != null)
                watchAdButton.onClick.AddListener(OnWatchAdClicked);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            ResolveTheme();
            ApplyTheme();
        }

        private void OnWatchAdClicked()
        {
            int levelIndex = Mathf.Max(0, LevelRecords.LastPlayedLevelId - 1);
            void leaveScreen() => _router?.GoBack();
            HeartsRefillAdFlow.Run(levelIndex, adServiceComponent, leaveScreen, leaveScreen);
        }

        private void OnBackClicked()
        {
            _router?.GoBack();
        }

        private void ResolveTheme()
        {
            if (_theme != null) return;
            var localApplier = GetComponent<ThemeApplier>();
            if (localApplier != null && localApplier.Theme != null)
            {
                _theme = localApplier.Theme;
                return;
            }
            var parentApplier = GetComponentInParent<ThemeApplier>();
            if (parentApplier != null)
                _theme = parentApplier.Theme;
        }

        private void ApplyTheme()
        {
            ApplyButtonTheme(watchAdButton, _theme != null ? _theme.warning : UIStyleConstants.Warning);
            ApplyButtonTheme(backButton, _theme != null ? _theme.primary : UIStyleConstants.Primary);

            var texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (_theme != null && _theme.font != null)
                    texts[i].font = _theme.font;
                texts[i].color = _theme != null ? _theme.textPrimary : UIStyleConstants.TextPrimary;
            }
        }

        private void ApplyButtonTheme(Button button, Color fallbackColor)
        {
            if (button == null) return;
            var img = button.GetComponent<Image>();
            if (img != null)
            {
                if (_theme != null && _theme.buttonSprite != null)
                {
                    img.sprite = _theme.buttonSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
                else
                {
                    img.color = fallbackColor;
                }
            }
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                if (_theme != null && _theme.font != null) text.font = _theme.font;
                text.color = _theme != null ? _theme.textOnAccent : UIStyleConstants.TextOnAccent;
            }
        }
    }
}
