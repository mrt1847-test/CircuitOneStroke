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

        private GameStateMachine _stateMachine;

        private void Start()
        {
            if (levelLoader == null) levelLoader = FindObjectOfType<LevelLoader>();
            if (levelManifest == null) levelManifest = Resources.Load<LevelManifest>("Levels/GeneratedLevelManifest");

            HeartsManager.Instance.OnHeartsChanged += OnHeartsChanged;
            HeartsManager.Instance.OnOutOfHearts += OnOutOfHearts;

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

            RefreshHeartsDisplay();
            RefreshVisibility();
            UpdateLevelLabel();
        }

        private void OnDestroy()
        {
            HeartsManager.Instance.OnHeartsChanged -= OnHeartsChanged;
            HeartsManager.Instance.OnOutOfHearts -= OnOutOfHearts;
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

        private void OnSettingsClicked()
        {
            router?.ShowSettings();
        }

        private void RefreshHeartsDisplay()
        {
            int h = HeartsManager.Instance.Hearts;
            int max = HeartsManager.Instance.MaxHearts;
            if (heartBar != null)
                heartBar.SetHearts(h, max);
            else if (heartsText != null)
                heartsText.text = $"{h}/{max}";
            if (heartsDisplay != null)
                heartsDisplay.SetActive(true);
        }

        private void RefreshVisibility()
        {
            var state = levelLoader?.StateMachine?.State ?? GameState.Idle;
            if (successPanel != null) successPanel.SetActive(state == GameState.LevelComplete);
            if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(state == GameState.LevelComplete);
            if (failPanel != null) failPanel.SetActive(state == GameState.LevelFailed);
            if (outOfHeartsPanel != null) outOfHeartsPanel.SetActive(state == GameState.OutOfHearts);

            if (state == GameState.LevelFailed)
            {
                UpdateFailMessage();
                RefreshFailDialogButtons();
            }
        }

        private void UpdateFailMessage()
        {
            if (failMessageText == null) return;
            if (HeartsManager.Instance.Hearts > 0)
                failMessageText.text = "Try again?";
            else
                failMessageText.text = "Out of hearts. Watch an ad to refill.";
        }

        private void RefreshFailDialogButtons()
        {
            bool hasHearts = HeartsManager.Instance.CanStartAttempt();
            if (retryButton != null)
            {
                retryButton.interactable = hasHearts;
                retryButton.gameObject.SetActive(true);
            }
            if (watchAdButton != null)
                watchAdButton.gameObject.SetActive(!hasHearts);
        }

        /// <summary>재시도 시 CanStartAttempt이면 LoadCurrent(transition), 아니면 OutOfHearts.</summary>
        private void OnRetryClicked()
        {
            if (!HeartsManager.Instance.CanStartAttempt())
            {
                if (_stateMachine != null)
                    _stateMachine.SetState(GameState.OutOfHearts);
                RefreshVisibility();
                return;
            }
            if (levelLoader != null && TransitionManager.Instance != null)
            {
                TransitionManager.Instance.RunTransition(levelLoader.LoadCurrentCoroutine());
            }
            else if (levelLoader != null)
            {
                levelLoader.LoadCurrent();
            }
            RefreshVisibility();
        }

        private void OnHomeClicked()
        {
            router?.ShowHome();
        }

        /// <summary>Watch Ad to Refill - opt-in only (button click). NoAds로 제거 불가.</summary>
        private void OnWatchAdClicked()
        {
            int levelIndex = levelLoader?.LevelData != null ? Mathf.Max(0, levelLoader.LevelData.levelId - 1) : 0;
            if (!AdDecisionService.Instance.CanShow(AdPlacement.Rewarded_HeartsRefill, userInitiated: true, levelIndex))
            {
                HeartsManager.Instance.RefillFull();
                TransitionAfterRefill();
                return;
            }
            var service = GetAdService();
            if (service == null || !service.IsRewardedReady(AdPlacement.Rewarded_HeartsRefill))
            {
                HeartsManager.Instance.RefillFull();
                TransitionAfterRefill();
                return;
            }
            service.ShowRewarded(
                AdPlacement.Rewarded_HeartsRefill,
                onRewarded: () =>
                {
                    HeartsManager.Instance.RefillFull();
                    AdDecisionService.Instance.RecordShown(AdPlacement.Rewarded_HeartsRefill);
                },
                onClosed: () => TransitionAfterRefill(),
                onFailed: _ => TransitionAfterRefill()
            );
        }

        /// <summary>Next Level - N클리어마다 인터스티셜(NoAds 구매 시 비표시).</summary>
        private void OnNextLevelClicked()
        {
            var service = GetAdService();
            int levelIndex = levelLoader?.LevelData != null ? Mathf.Max(0, levelLoader.LevelData.levelId - 1) : 0;
            var config = AdPlacementConfig.Instance?.GetConfig(AdPlacement.Interstitial_EveryNClears)
                ?? AdPlacementConfig.GetDefaultConfig(AdPlacement.Interstitial_EveryNClears);
            int n = config.frequencyN <= 0 ? 3 : config.frequencyN;

            bool shouldShowInterstitial = InterstitialTracker.Instance.LevelsClearedSinceLastInterstitial >= n &&
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
                    onFailed: _ => LoadNextLevel()
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
            yield return TransitionManager.Instance.RunTransition(loadWork);
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

        private IAdService GetAdService()
        {
            if (adServiceComponent != null && adServiceComponent is IAdService s)
                return s;
            return FindObjectOfType<AdServiceMock>();
        }
    }
}
