using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;
using CircuitOneStroke.Services;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 게임 중 HUD: 하트 표시, 성공/실패/OutOfHearts 패널, Retry/Next Level/Home/Watch Ad 버튼.
    /// </summary>
    public class GameHUD : MonoBehaviour, IUIScreen
    {
        [Header("References")]
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private LevelManifest levelManifest;
        [SerializeField] private MonoBehaviour adServiceComponent;
        [SerializeField] private UIScreenRouter router;

        [Header("Hearts Display")]
        [SerializeField] private GameObject heartsDisplay;
        [SerializeField] private Text heartsText;
        [SerializeField] private HeartBar heartBar;

        [Header("Top Bar")]
        [SerializeField] private Button settingsButton;

        [Header("Success")]
        [SerializeField] private GameObject successPanel;
        [SerializeField] private Button nextLevelButton;

        [Header("Fail Dialog")]
        [SerializeField] private GameObject failPanel;
        [SerializeField] private Text failMessageText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button watchAdButton;

        [Header("Out of Hearts")]
        [SerializeField] private GameObject outOfHeartsPanel;
        [SerializeField] private Button outOfHeartsWatchAdButton;
        [SerializeField] private Button outOfHeartsBackButton;

        [Header("Other")]
        [SerializeField] private Text levelLabel;
        [Header("Bottom Bar (참고 스크린샷)")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button hintButton;

        private GameStateMachine _stateMachine;

        private void Start()
        {
            if (levelLoader == null) levelLoader = FindFirstObjectByType<LevelLoader>();
            if (levelManifest == null) levelManifest = Resources.Load<LevelManifest>("Levels/GeneratedLevelManifest");

            if (HeartsManager.Instance != null)
            {
                HeartsManager.Instance.OnHeartsChanged += OnHeartsChanged;
                HeartsManager.Instance.OnOutOfHearts += OnOutOfHearts;
            }

            if (levelLoader != null)
            {
                levelLoader.OnStateMachineChanged += HandleStateMachineChanged;
                HandleStateMachineChanged(levelLoader.StateMachine);
            }

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            if (homeButton != null)
                homeButton.onClick.AddListener(OnHomeClicked);
            if (watchAdButton != null)
                watchAdButton.onClick.AddListener(OnWatchAdClicked);
            if (outOfHeartsWatchAdButton != null)
                outOfHeartsWatchAdButton.onClick.AddListener(OnWatchAdClicked);
            if (outOfHeartsBackButton != null)
                outOfHeartsBackButton.onClick.AddListener(OnHomeClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            if (backButton != null)
                backButton.onClick.AddListener(OnHomeClicked);
            if (undoButton != null)
                undoButton.onClick.AddListener(OnUndoClicked);
            if (hintButton != null)
                hintButton.onClick.AddListener(OnHintClicked);

            RefreshHeartsDisplay();
            RefreshVisibility();
            UpdateLevelLabel();
        }

        private void OnDestroy()
        {
            if (HeartsManager.Instance != null)
            {
                HeartsManager.Instance.OnHeartsChanged -= OnHeartsChanged;
                HeartsManager.Instance.OnOutOfHearts -= OnOutOfHearts;
            }
            if (levelLoader != null)
                levelLoader.OnStateMachineChanged -= HandleStateMachineChanged;
            if (_stateMachine != null)
                _stateMachine.OnStateChanged -= OnStateChanged;
        }

        private void HandleStateMachineChanged(GameStateMachine stateMachine)
        {
            if (_stateMachine != null)
                _stateMachine.OnStateChanged -= OnStateChanged;

            _stateMachine = stateMachine;

            if (_stateMachine != null)
                _stateMachine.OnStateChanged += OnStateChanged;

            RefreshVisibility();
            UpdateLevelLabel();
        }

        private void OnHeartsChanged(int hearts)
        {
            RefreshHeartsDisplay();
            RefreshFailDialogButtons();
        }

        private void OnOutOfHearts()
        {
            RefreshHeartsDisplay();
        }

        private void UpdateLevelLabel()
        {
            if (levelLabel != null && levelLoader?.LevelData != null)
                levelLabel.text = $"Level {levelLoader.LevelData.levelId}";
        }

        private void OnStateChanged(GameState state)
        {
            RefreshVisibility();
            if (state == GameState.LevelComplete)
                GameFeedback.Instance?.PlaySuccess();
            else if (state == GameState.LevelFailed)
                GameFeedback.Instance?.PlayFail();
        }

        public void BindRouter(UIScreenRouter r)
        {
            router = r;
        }

        public void SetResultDialogVisible(bool visible)
        {
            if (!visible)
            {
                if (successPanel != null) successPanel.SetActive(false);
                if (failPanel != null) failPanel.SetActive(false);
                if (outOfHeartsPanel != null) outOfHeartsPanel.SetActive(false);
            }
            else
            {
                RefreshVisibility();
            }
        }

        public void HideResultAndResetState()
        {
            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel != null) failPanel.SetActive(false);
            if (_stateMachine != null)
                _stateMachine.ResetToIdle();
            RefreshVisibility();
        }

        private void OnSettingsClicked()
        {
            if (AppRouter.Instance != null)
            {
                AppRouter.Instance.ShowTab(MainTab.Settings);
                return;
            }
            router?.ShowSettings();
        }

        private void RefreshHeartsDisplay()
        {
            if (HeartsManager.Instance == null) return;
            int h = HeartsManager.Instance.Hearts;
            int max = HeartsManager.Instance.MaxHearts;
            if (heartBar != null)
                heartBar.SetHearts(h, max);
            else if (heartsText != null)
                heartsText.text = $"\u2665 {h}/{max}";
            if (heartsDisplay != null)
                heartsDisplay.SetActive(true);
        }

        private void RefreshVisibility()
        {
            var state = levelLoader?.StateMachine?.State ?? GameState.Idle;
            bool useOverlayManager = AppRouter.Instance != null && AppRouter.Instance.UseOverlayForResult;
            if (successPanel != null) successPanel.SetActive(!useOverlayManager && state == GameState.LevelComplete);
            if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(!useOverlayManager && state == GameState.LevelComplete);
            if (failPanel != null) failPanel.SetActive(!useOverlayManager && state == GameState.LevelFailed);
            if (outOfHeartsPanel != null) outOfHeartsPanel.SetActive(!useOverlayManager && state == GameState.OutOfHearts);

            if (state == GameState.LevelFailed)
            {
                UpdateFailMessage();
                RefreshFailDialogButtons();
            }
        }

        private void UpdateFailMessage()
        {
            if (failMessageText == null) return;
            if (HeartsManager.Instance == null) { failMessageText.text = "Try again?"; return; }
            if (HeartsManager.Instance.Hearts > 0)
                failMessageText.text = "Try again?";
            else
                failMessageText.text = "Out of hearts. Watch an ad to refill.";
        }

        private void RefreshFailDialogButtons()
        {
            bool hasHearts = HeartsManager.Instance != null && HeartsManager.Instance.CanStartAttempt();
            if (retryButton != null)
            {
                retryButton.interactable = hasHearts;
                retryButton.gameObject.SetActive(true);
            }
            if (watchAdButton != null)
                watchAdButton.gameObject.SetActive(!hasHearts);
        }

        private void OnRetryClicked()
        {
            if (AppRouter.Instance != null)
            {
                AppRouter.Instance.RequestRetry();
                return;
            }
            var flow = UIServices.GetFlow();
            if (flow != null)
            {
                flow.RequestRetryCurrent();
                return;
            }
            if (HeartsManager.Instance == null || !HeartsManager.Instance.CanStartAttempt())
            {
                if (_stateMachine != null)
                    _stateMachine.SetState(GameState.OutOfHearts);
                RefreshVisibility();
                return;
            }
            if (levelLoader != null && TransitionManager.Instance != null)
                TransitionManager.Instance.RunTransition(levelLoader.LoadCurrentCoroutine());
            else if (levelLoader != null)
                levelLoader.LoadCurrent();
            RefreshVisibility();
        }

        private void OnHomeClicked()
        {
            var state = levelLoader?.StateMachine?.State ?? GameState.Idle;
            bool fromResultScreen = (state == GameState.LevelFailed || state == GameState.LevelComplete);
            if (AppRouter.Instance != null)
            {
                if (fromResultScreen)
                    AppRouter.Instance.ExitGameToHomeTab();
                else
                    AppRouter.Instance.RequestExitGame();
                return;
            }
            if (ScreenRouter.Instance != null)
            {
                ScreenRouter.Instance.ExitGameToHome();
                return;
            }
            router?.ShowHome();
        }

        private void OnUndoClicked()
        {
            if (levelLoader?.StateMachine == null || levelLoader.Runtime == null) return;
            if (_stateMachine.State != GameState.Drawing) return;
            if (!levelLoader.Runtime.RemoveLastStrokeNode()) return;
            levelLoader.RefreshNodeViews();
        }

        private void OnHintClicked()
        {
            if (AppRouter.Instance != null)
                AppRouter.Instance.ShowToast("Hint: Connect all bulbs in one stroke.");
            else if (router != null)
                router.ShowToast("Hint: Connect all bulbs in one stroke.");
        }

        /// <summary>Watch Ad to Refill - opt-in only (button click). NoAds로 제거 불가.</summary>
        private void OnWatchAdClicked()
        {
            int levelIndex = levelLoader?.LevelData != null ? Mathf.Max(0, levelLoader.LevelData.levelId - 1) : 0;
            HeartsRefillAdFlow.Run(levelIndex, adServiceComponent, TransitionAfterRefill, TransitionAfterRefill);
        }

        private void OnNextLevelClicked()
        {
            if (AppRouter.Instance != null)
            {
                AppRouter.Instance.RequestNext();
                return;
            }
            var flow = UIServices.GetFlow();
            if (flow != null)
            {
                flow.RequestNextLevel();
                return;
            }
            if (InterstitialTracker.Instance == null || AdDecisionService.Instance == null)
            {
                LoadNextLevel();
                return;
            }
            var service = UIServices.GetAdService(adServiceComponent);
            int levelIndex = levelLoader?.LevelData != null ? Mathf.Max(0, levelLoader.LevelData.levelId - 1) : 0;
            var config = AdPlacementConfig.Instance?.GetConfig(AdPlacement.Interstitial_EveryNClears)
                ?? AdPlacementConfig.GetDefaultConfig(AdPlacement.Interstitial_EveryNClears);
            int n = config.frequencyN <= 0 ? 3 : config.frequencyN;

            bool shouldShowInterstitial = InterstitialTracker.Instance.LevelsClearedSinceLastInterstitial >= n &&
                InterstitialTracker.Instance.CanAttemptInterstitial() &&
                AdDecisionService.Instance.CanShow(AdPlacement.Interstitial_EveryNClears, userInitiated: false, levelIndex) &&
                service != null && service.IsInterstitialReady(AdPlacement.Interstitial_EveryNClears);

            if (shouldShowInterstitial)
            {
                service.ShowInterstitial(
                    AdPlacement.Interstitial_EveryNClears,
                    onClosed: () =>
                    {
                        InterstitialTracker.Instance.ResetAfterInterstitialShown();
                        AdDecisionService.Instance.RecordShown(AdPlacement.Interstitial_EveryNClears);
                        LoadNextLevel();
                    },
                    onFailed: _ =>
                    {
                        InterstitialTracker.Instance.RecordInterstitialFailure();
                        LoadNextLevel();
                    }
                );
            }
            else
            {
                LoadNextLevel();
            }
        }

        private void LoadNextLevel()
        {
            if (levelLoader == null) return;
            int currentId = levelLoader.LevelData != null ? levelLoader.LevelData.levelId : 1;
            IEnumerator work = null;
            if (levelManifest != null)
            {
                var next = levelManifest.GetLevel(currentId);
                if (next != null)
                    work = levelLoader.LoadLevelCoroutine(next);
                else
                    work = levelLoader.LoadLevelCoroutine(currentId + 1);
            }
            else
            {
                work = levelLoader.LoadLevelCoroutine(currentId + 1);
            }
            if (work != null && TransitionManager.Instance != null)
            {
                StartCoroutine(LoadNextLevelWithTransition(work));
            }
            else if (work != null)
            {
                StartCoroutine(work);
                if (_stateMachine != null) _stateMachine.SetState(GameState.Idle);
                RefreshVisibility();
            }
        }

        private IEnumerator LoadNextLevelWithTransition(IEnumerator loadWork)
        {
            if (TransitionManager.Instance != null && loadWork != null)
                yield return TransitionManager.Instance.RunTransition(loadWork);
            else if (loadWork != null)
                yield return loadWork;
            if (_stateMachine != null) _stateMachine.SetState(GameState.Idle);
            RefreshVisibility();
        }

        private void TransitionAfterRefill()
        {
            if (_stateMachine != null)
                _stateMachine.SetState(GameState.Idle);
            if (failPanel != null && failPanel.activeSelf && levelLoader != null)
            {
                if (TransitionManager.Instance != null)
                    TransitionManager.Instance.RunTransition(levelLoader.LoadCurrentCoroutine());
                else
                    levelLoader.LoadCurrent();
            }
            RefreshVisibility();
        }

    }
}
