using System;
using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;

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

            HideResult();
            HideOutOfHearts();
            if (confirmExitDialog != null) confirmExitDialog.SetActive(false);
        }

        public void ShowResultWin(int levelId, Action onNext, Action onLevelSelect)
        {
            HideOutOfHearts();
            _resultVisible = true;
            if (resultDialogRoot != null) resultDialogRoot.SetActive(true);
            if (resultWinContent != null) resultWinContent.SetActive(true);
            if (resultLoseContent != null) resultLoseContent.SetActive(false);
            _resultWinNext = onNext;
            _resultWinLevelSelect = onLevelSelect;
        }

        public void ShowResultLose(int hearts, Action onRetry, Action onLevelSelect, Action onWatchAd, bool showWatchAdButton)
        {
            HideOutOfHearts();
            _resultVisible = true;
            if (resultDialogRoot != null) resultDialogRoot.SetActive(true);
            if (resultWinContent != null) resultWinContent.SetActive(false);
            if (resultLoseContent != null) resultLoseContent.SetActive(true);
            if (resultLoseMessageText != null)
                resultLoseMessageText.text = hearts > 0 ? "Try again?" : "Out of hearts. Watch an ad to refill.";
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
    }
}
