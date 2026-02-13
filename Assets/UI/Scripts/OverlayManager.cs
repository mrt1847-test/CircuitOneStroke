using System;
using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// Overlay visibility and exclusion. RESULT_* and OUT_OF_HEARTS never visible at same time.
    /// </summary>
    public class OverlayManager : MonoBehaviour
    {
        [Header("Result (Win/Lose)")]
        [SerializeField] private GameObject resultDialogRoot;
        [SerializeField] private GameObject resultWinContent;
        [SerializeField] private GameObject resultLoseContent;
        [SerializeField] private Button resultWinNextButton;
        [SerializeField] private Button resultWinLevelSelectButton;
        [SerializeField] private Button resultLoseRetryButton;
        [SerializeField] private Button resultLoseLevelSelectButton;
        [SerializeField] private Button resultLoseWatchAdButton;
        [SerializeField] private Text resultLoseMessageText;

        [Header("Out of Hearts")]
        [SerializeField] private GameObject outOfHeartsPanel;
        [SerializeField] private Button outOfHeartsWatchAdButton;
        [SerializeField] private Button outOfHeartsBackButton;

        [Header("Confirm Exit")]
        [SerializeField] private GameObject confirmExitDialog;
        [SerializeField] private Button confirmExitConfirmButton;
        [SerializeField] private Button confirmExitCancelButton;

        private bool _resultVisible;
        private bool _outOfHeartsVisible;
        private bool _confirmExitVisible;
        private Action _onConfirmExit;
        private CircuitOneStrokeTheme _theme;

        public bool IsResultVisible => _resultVisible;
        public bool IsOutOfHeartsVisible => _outOfHeartsVisible;
        public bool IsConfirmExitVisible => _confirmExitVisible;

        private void Awake()
        {
            if (resultWinNextButton != null) resultWinNextButton.onClick.AddListener(OnResultWinNext);
            if (resultWinLevelSelectButton != null) resultWinLevelSelectButton.onClick.AddListener(OnResultWinLevelSelect);
            if (resultLoseRetryButton != null) resultLoseRetryButton.onClick.AddListener(OnResultLoseRetry);
            if (resultLoseLevelSelectButton != null) resultLoseLevelSelectButton.onClick.AddListener(OnResultLoseLevelSelect);
            if (resultLoseWatchAdButton != null) resultLoseWatchAdButton.onClick.AddListener(OnResultLoseWatchAd);
            if (outOfHeartsWatchAdButton != null) outOfHeartsWatchAdButton.onClick.AddListener(OnOutOfHeartsWatchAd);
            if (outOfHeartsBackButton != null) outOfHeartsBackButton.onClick.AddListener(OnOutOfHeartsBack);
            if (confirmExitConfirmButton != null) confirmExitConfirmButton.onClick.AddListener(OnConfirmExitConfirm);
            if (confirmExitCancelButton != null) confirmExitCancelButton.onClick.AddListener(OnConfirmExitCancel);

            ResolveTheme();
            ApplyPopupTheme();

            HideResult();
            HideOutOfHearts();
            if (confirmExitDialog != null) confirmExitDialog.SetActive(false);
        }

        /// <summary>반투명 어두운 배경으로 설정. 씬에서 흰색(0.96,0.96,0.98)이면 흰 화면만 보이므로 런타임에 고정.</summary>
        private static readonly Color ResultOverlayBackground = new Color(0f, 0f, 0f, 0.6f);

        /// <summary>패널 내 버튼이 겹치지 않게 위치 보정, 텍스트 가독성 확보. 기존 ResultDialog Win/Lose 콘텐츠를 그대로 쓰되 표시만 보정.</summary>
        private static readonly Color ResultPanelTextColor = new Color(0.12f, 0.12f, 0.18f, 1f);

        public void ShowResultWin(int levelId, Action onNext, Action onLevelSelect)
        {
            ApplyPopupTheme();
            HideOutOfHearts();
            _resultVisible = true;
            if (resultDialogRoot != null)
            {
                resultDialogRoot.SetActive(true);
                var img = resultDialogRoot.GetComponent<Image>();
                if (img != null) img.color = ResultOverlayBackground;
            }
            if (resultWinContent != null)
            {
                resultWinContent.SetActive(true);
                ApplyResultWinLayout();
            }
            if (resultLoseContent != null) resultLoseContent.SetActive(false);
            _resultWinNext = onNext;
            _resultWinLevelSelect = onLevelSelect;
        }

        public void ShowResultLose(int hearts, Action onRetry, Action onLevelSelect, Action onWatchAd, bool showWatchAdButton)
        {
            ApplyPopupTheme();
            HideOutOfHearts();
            _resultVisible = true;
            if (resultDialogRoot != null)
            {
                resultDialogRoot.SetActive(true);
                var img = resultDialogRoot.GetComponent<Image>();
                if (img != null) img.color = ResultOverlayBackground;
            }
            if (resultWinContent != null) resultWinContent.SetActive(false);
            if (resultLoseContent != null)
            {
                resultLoseContent.SetActive(true);
                if (resultLoseMessageText != null)
                {
                    resultLoseMessageText.text = hearts > 0 ? "Try again?" : "Out of hearts. Watch an ad to refill.";
                    resultLoseMessageText.color = ResultPanelTextColor;
                }
                ApplyResultLoseLayout();
            }
            if (resultLoseRetryButton != null)
            {
                resultLoseRetryButton.gameObject.SetActive(true);
                resultLoseRetryButton.interactable = hearts > 0;
            }
            if (resultLoseWatchAdButton != null) resultLoseWatchAdButton.gameObject.SetActive(showWatchAdButton);
            _resultLoseRetry = onRetry;
            _resultLoseLevelSelect = onLevelSelect;
            _resultLoseWatchAd = onWatchAd;
        }

        private void ApplyResultWinLayout()
        {
            if (resultWinContent == null) return;
            ApplyPanelSizing(resultWinContent);
            var panelRt = resultWinContent.GetComponent<RectTransform>();
            float panelH = panelRt != null ? panelRt.rect.height : 360f;
            float y = panelH * 0.18f;
            // Next / Level Select 두 버튼이 겹쳐 있으면 한 개만 보이므로, Y 위치로 위·아래 분리
            if (resultWinNextButton != null)
            {
                var rt = resultWinNextButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(260f, 64f);
                    rt.anchoredPosition = new Vector2(0f, y);
                }
                SetButtonLabel(resultWinNextButton, "Next");
            }
            if (resultWinLevelSelectButton != null)
            {
                var rt = resultWinLevelSelectButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(260f, 64f);
                    rt.anchoredPosition = new Vector2(0f, -y);
                }
                SetButtonLabel(resultWinLevelSelectButton, "Level Select");
            }
            EnsureTextVisibleIn(resultWinContent);
        }

        private void ApplyResultLoseLayout()
        {
            if (resultLoseContent == null) return;
            ApplyPanelSizing(resultLoseContent);
            var panelRt = resultLoseContent.GetComponent<RectTransform>();
            float panelH = panelRt != null ? panelRt.rect.height : 360f;
            float y = panelH * 0.30f;
            if (resultLoseMessageText != null)
            {
                var rt = resultLoseMessageText.GetComponent<RectTransform>();
                if (rt != null) { rt.anchoredPosition = new Vector2(0f, y); rt.sizeDelta = new Vector2(420f, 72f); }
                y -= 86f;
            }
            if (resultLoseRetryButton != null)
            {
                var rt = resultLoseRetryButton.GetComponent<RectTransform>();
                if (rt != null) { rt.sizeDelta = new Vector2(280f, 60f); rt.anchoredPosition = new Vector2(0f, y); }
                y -= 68f;
                SetButtonLabel(resultLoseRetryButton, "Retry");
            }
            if (resultLoseLevelSelectButton != null)
            {
                var rt = resultLoseLevelSelectButton.GetComponent<RectTransform>();
                if (rt != null) { rt.sizeDelta = new Vector2(280f, 60f); rt.anchoredPosition = new Vector2(0f, y); }
                y -= 68f;
                SetButtonLabel(resultLoseLevelSelectButton, "Level Select");
            }
            if (resultLoseWatchAdButton != null)
            {
                var rt = resultLoseWatchAdButton.GetComponent<RectTransform>();
                if (rt != null) { rt.sizeDelta = new Vector2(280f, 60f); rt.anchoredPosition = new Vector2(0f, y); }
                SetButtonLabel(resultLoseWatchAdButton, "Watch Ad");
            }
            EnsureTextVisibleIn(resultLoseContent);
        }

        private void ApplyPanelSizing(GameObject panel)
        {
            if (panel == null) return;
            var panelRt = panel.GetComponent<RectTransform>();
            if (panelRt == null) return;

            Vector2 baseSize = new Vector2(Screen.width, Screen.height);
            if (resultDialogRoot != null)
            {
                var rootRt = resultDialogRoot.GetComponent<RectTransform>();
                if (rootRt != null && rootRt.rect.width > 0f && rootRt.rect.height > 0f)
                    baseSize = rootRt.rect.size;
            }

            float width = Mathf.Clamp(baseSize.x * 0.72f, 520f, 900f);
            float height = Mathf.Clamp(baseSize.y * 0.42f, 340f, 620f);
            panelRt.sizeDelta = new Vector2(width, height);
            panelRt.anchoredPosition = Vector2.zero;
        }

        private static void SetButtonLabel(Button btn, string label)
        {
            if (btn == null || string.IsNullOrEmpty(label)) return;
            var text = btn.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                var rt = text.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // 기존 버튼 텍스트가 하단 얇은 밴드(0.1~0.45)에 고정돼 있어 잘리는 문제를 방지.
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }
                EnsureTextRenders(text);
            }
        }

        /// <summary>폰트가 null이면 Text가 아무것도 그리지 않음. 런타임에 내장 폰트 할당.</summary>
        private static void EnsureTextRenders(Text text)
        {
            if (text == null) return;
            if (text.font == null)
            {
                var fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (fallback != null) text.font = fallback;
            }
            if (text.fontSize <= 0) text.fontSize = 22;
        }

        private static void EnsureTextVisibleIn(GameObject root)
        {
            if (root == null) return;
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                EnsureTextRenders(text);
                if (text.color.grayscale > 0.85f)
                    text.color = ResultPanelTextColor;
            }
        }

        private Action _resultWinNext;
        private Action _resultWinLevelSelect;
        private Action _resultLoseRetry;
        private Action _resultLoseLevelSelect;
        private Action _resultLoseWatchAd;

        private void OnResultWinNext() { _resultWinNext?.Invoke(); }
        private void OnResultWinLevelSelect() { _resultWinLevelSelect?.Invoke(); }
        private void OnResultLoseRetry() { _resultLoseRetry?.Invoke(); }
        private void OnResultLoseLevelSelect() { _resultLoseLevelSelect?.Invoke(); }
        private void OnResultLoseWatchAd() { _resultLoseWatchAd?.Invoke(); }

        public void ShowOutOfHearts(OutOfHeartsContext ctx, Action onWatchAd, Action onBack)
        {
            ApplyPopupTheme();
            HideResult();
            _outOfHeartsVisible = true;
            if (outOfHeartsPanel != null) outOfHeartsPanel.SetActive(true);
            _outOfHeartsWatchAd = onWatchAd;
            _outOfHeartsBack = onBack;
        }

        private Action _outOfHeartsWatchAd;
        private Action _outOfHeartsBack;

        private void OnOutOfHeartsWatchAd() => _outOfHeartsWatchAd?.Invoke();
        private void OnOutOfHeartsBack() => _outOfHeartsBack?.Invoke();

        public void ShowConfirmExit(Action onConfirmExit)
        {
            ApplyPopupTheme();
            _onConfirmExit = onConfirmExit;
            _confirmExitVisible = true;
            if (confirmExitDialog != null) confirmExitDialog.SetActive(true);
        }

        private void OnConfirmExitConfirm()
        {
            _confirmExitVisible = false;
            if (confirmExitDialog != null) confirmExitDialog.SetActive(false);
            _onConfirmExit?.Invoke();
            _onConfirmExit = null;
        }

        private void OnConfirmExitCancel()
        {
            _confirmExitVisible = false;
            if (confirmExitDialog != null) confirmExitDialog.SetActive(false);
            _onConfirmExit = null;
        }

        public void HideResult()
        {
            _resultVisible = false;
            if (resultDialogRoot != null) resultDialogRoot.SetActive(false);
        }

        public void HideOutOfHearts()
        {
            _outOfHeartsVisible = false;
            if (outOfHeartsPanel != null) outOfHeartsPanel.SetActive(false);
        }

        public void HideAllExceptToast()
        {
            HideResult();
            HideOutOfHearts();
            _confirmExitVisible = false;
            if (confirmExitDialog != null) confirmExitDialog.SetActive(false);
        }

        public void ShowToast(string msg)
        {
            GameFeedback.RequestToast(msg);
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

        private void ApplyPopupTheme()
        {
            ResolveTheme();

            ApplyOverlayBackground(resultDialogRoot, ResultOverlayBackground);
            ApplyPanelTheme(resultWinContent);
            ApplyPanelTheme(resultLoseContent);
            ApplyButtonTheme(resultWinNextButton);
            ApplyButtonTheme(resultWinLevelSelectButton);
            ApplyButtonTheme(resultLoseRetryButton);
            ApplyButtonTheme(resultLoseLevelSelectButton);
            ApplyButtonTheme(resultLoseWatchAdButton, _theme != null ? _theme.warning : UIStyleConstants.Warning);
            ApplyTextTheme(resultLoseMessageText, accent: false);
            ApplyTextThemeRecursive(resultWinContent, accent: false);
            ApplyTextThemeRecursive(resultLoseContent, accent: false);

            ApplyOverlayBackground(outOfHeartsPanel, new Color(0f, 0f, 0f, 0.72f));
            ApplyButtonTheme(outOfHeartsWatchAdButton);
            ApplyButtonTheme(outOfHeartsBackButton);
            ApplyTextThemeRecursive(outOfHeartsPanel, accent: false);

            ApplyOverlayBackground(confirmExitDialog, new Color(0f, 0f, 0f, 0.72f));
            ApplyButtonTheme(confirmExitConfirmButton, _theme != null ? _theme.danger : UIStyleConstants.Danger);
            ApplyButtonTheme(confirmExitCancelButton);
            ApplyTextThemeRecursive(confirmExitDialog, accent: false);
        }

        private void ApplyOverlayBackground(GameObject root, Color fallbackColor)
        {
            if (root == null) return;
            var img = root.GetComponent<Image>();
            if (img == null) return;
            img.sprite = null;
            img.type = Image.Type.Simple;
            img.color = fallbackColor;
        }

        private void ApplyPanelTheme(GameObject panel)
        {
            if (panel == null) return;
            var img = panel.GetComponent<Image>();
            if (img == null) return;
            if (_theme != null && _theme.panelSprite != null)
            {
                img.sprite = _theme.panelSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = _theme != null ? _theme.panelBase : UIStyleConstants.PanelBase;
            }
        }

        private void ApplyButtonTheme(Button button, Color? flatColor = null)
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
                    img.color = flatColor ?? (_theme != null ? _theme.primary : UIStyleConstants.Primary);
                }
            }

            var text = button.GetComponentInChildren<Text>(true);
            ApplyTextTheme(text, accent: true);
        }

        private void ApplyTextTheme(Text text, bool accent)
        {
            if (text == null) return;
            if (_theme != null && _theme.font != null)
                text.font = _theme.font;
            text.color = _theme != null
                ? (accent ? _theme.textOnAccent : _theme.textPrimary)
                : (accent ? UIStyleConstants.TextOnAccent : UIStyleConstants.TextPrimary);
            EnsureTextRenders(text);
        }

        private void ApplyTextThemeRecursive(GameObject root, bool accent)
        {
            if (root == null) return;
            var texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
                ApplyTextTheme(texts[i], accent);
        }
    }
}
