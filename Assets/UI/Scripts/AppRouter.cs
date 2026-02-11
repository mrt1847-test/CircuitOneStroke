using System;
using System.Collections;
using UnityEngine;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;
using CircuitOneStroke.Services;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// Single source of truth: MainShell (tabs) vs GamePlay, overlays, back, guards.
    /// </summary>
    public class AppRouter : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject mainShellRoot;
        [SerializeField] private GameObject gameRoot;
        [SerializeField] private Transform mainShellContentRoot;

        [Header("Tab Content (Home = Level Select)")]
        [SerializeField] private GameObject homeTabView;
        [SerializeField] private GameObject shopTabView;
        [SerializeField] private GameObject settingsTabView;

        [Header("Bottom Nav")]
        [SerializeField] private MainShellNavBar bottomNavBar;

        [Header("Dependencies")]
        [SerializeField] private OverlayManager overlayManager;
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private LevelManifest levelManifest;
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private MonoBehaviour adServiceComponent;

        public static AppRouter Instance { get; private set; }

        public ScreenMode CurrentMode { get; private set; } = ScreenMode.MainShell;
        public MainTab CurrentTab { get; private set; } = MainTab.Home;
        public int CurrentLevelId { get; private set; }
        public LastIntent LastIntent { get; private set; }
        public LevelManifest LevelManifest => levelManifest;
        public LevelLoader LevelLoader => levelLoader;

        private bool IsTransitioning => TransitionManager.Instance != null && TransitionManager.Instance.IsTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (overlayManager == null) overlayManager = GetComponentInChildren<OverlayManager>();
            if (levelLoader == null) levelLoader = FindFirstObjectByType<LevelLoader>();
            if (levelManifest == null) levelManifest = Resources.Load<LevelManifest>("Levels/GeneratedLevelManifest");
            if (gameFlowController == null) gameFlowController = FindFirstObjectByType<GameFlowController>();
        }

        private void Start()
        {
            Boot();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                HandleBack();
        }

        public void Boot()
        {
            ShowMainShell(MainTab.Home);
            bottomNavBar?.Bind(tab => ShowTab(tab));
        }

        public void ShowTab(MainTab tab)
        {
            CurrentTab = tab;
            if (homeTabView != null) homeTabView.SetActive(tab == MainTab.Home);
            if (shopTabView != null) shopTabView.SetActive(tab == MainTab.Shop);
            if (settingsTabView != null) settingsTabView.SetActive(tab == MainTab.Settings);
            bottomNavBar?.SetSelectedTab(tab);
        }

        public void ShowMainShell(MainTab tab)
        {
            CurrentMode = ScreenMode.MainShell;
            if (mainShellRoot != null) mainShellRoot.SetActive(true);
            if (gameRoot != null) gameRoot.SetActive(false);
            ShowTab(tab);
        }

        public void RequestStartLevel(int levelId)
        {
            LastIntent = new LastIntent { type = IntentType.StartLevel, levelId = levelId };
            if (gameFlowController != null) gameFlowController.SetLastIntent(LastIntent);
            if (!HeartsManager.Instance.CanStartAttempt())
            {
                overlayManager?.ShowOutOfHearts(OutOfHeartsContext.FromLevelSelect, OnRefillThenResume, () => { });
                return;
            }
            StartCoroutine(RunTransitionThenEnterGame(levelId));
        }

        public void RequestRetry()
        {
            int currentId = levelLoader?.LevelData != null ? levelLoader.LevelData.levelId : CurrentLevelId;
            if (currentId <= 0) currentId = 1;
            LastIntent = new LastIntent { type = IntentType.RetryLevel, levelId = currentId };
            if (gameFlowController != null) gameFlowController.SetLastIntent(LastIntent);
            if (!HeartsManager.Instance.CanStartAttempt())
            {
                overlayManager?.ShowOutOfHearts(OutOfHeartsContext.FromResultLose, OnRefillThenResume, () => { });
                return;
            }
            StartCoroutine(RunTransitionThenEnterGame(currentId));
        }

        public void RequestNext()
        {
            int currentId = levelLoader?.LevelData != null ? levelLoader.LevelData.levelId : CurrentLevelId;
            if (currentId <= 0) currentId = 1;
            int nextId = levelManifest != null ? levelManifest.GetNextLevelId(currentId) : currentId + 1;
            LastIntent = new LastIntent { type = IntentType.NextLevel, levelId = nextId };
            if (gameFlowController != null) gameFlowController.SetLastIntent(LastIntent);
            if (!HeartsManager.Instance.CanStartAttempt())
            {
                overlayManager?.ShowOutOfHearts(OutOfHeartsContext.FromResultWin, OnRefillThenResume, () => { });
                return;
            }
            StartCoroutine(TryInterstitialThenEnterGame(nextId));
        }

        private void OnRefillThenResume()
        {
            overlayManager?.HideResult();
            overlayManager?.HideOutOfHearts();
            // RefillFull is done inside HeartsRefillAdFlow before this callback
            ResumeLastIntent();
        }

        public void ResumeLastIntent()
        {
            overlayManager?.HideResult();
            overlayManager?.HideOutOfHearts();
            var intent = LastIntent;
            switch (intent.type)
            {
                case IntentType.StartLevel:
                    StartCoroutine(RunTransitionThenEnterGame(intent.levelId));
                    break;
                case IntentType.RetryLevel:
                    StartCoroutine(RunTransitionThenEnterGame(intent.levelId));
                    break;
                case IntentType.NextLevel:
                    StartCoroutine(TryInterstitialThenEnterGame(intent.levelId));
                    break;
            }
        }

        private IEnumerator TryInterstitialThenEnterGame(int levelId)
        {
            var service = AdServiceRegistry.Instance ?? FindFirstObjectByType<AdServiceMock>() as IAdService;
            int levelIndex = Mathf.Max(0, levelId - 2);
            var config = AdPlacementConfig.Instance?.GetConfig(AdPlacement.Interstitial_EveryNClears)
                ?? AdPlacementConfig.GetDefaultConfig(AdPlacement.Interstitial_EveryNClears);
            int n = config.frequencyN <= 0 ? 3 : config.frequencyN;
            bool shouldShow = InterstitialTracker.Instance != null && InterstitialTracker.Instance.LevelsClearedSinceLastInterstitial >= n &&
                InterstitialTracker.Instance.CanAttemptInterstitial() &&
                AdDecisionService.Instance != null && AdDecisionService.Instance.CanShow(AdPlacement.Interstitial_EveryNClears, false, levelIndex) &&
                service != null && service.IsInterstitialReady(AdPlacement.Interstitial_EveryNClears);

            if (shouldShow && service != null)
            {
                bool done = false;
                service.ShowInterstitial(AdPlacement.Interstitial_EveryNClears,
                    onClosed: () =>
                    {
                        if (InterstitialTracker.Instance != null) InterstitialTracker.Instance.ResetAfterInterstitialShown();
                        if (AdDecisionService.Instance != null) AdDecisionService.Instance.RecordShown(AdPlacement.Interstitial_EveryNClears);
                        done = true;
                    },
                    onFailed: _ => { done = true; });
                while (!done) yield return null;
            }
            yield return RunTransitionThenEnterGame(levelId);
        }

        private IEnumerator RunTransitionThenEnterGame(int levelId)
        {
            IEnumerator job = BuildLevelJob(levelId);
            if (TransitionManager.Instance != null)
                yield return TransitionManager.Instance.RunTransition(job);
            else
                yield return job;
            EnterGame(levelId);
        }

        private IEnumerator BuildLevelJob(int levelId)
        {
            if (levelLoader == null) yield break;
            LevelRecords.LastPlayedLevelId = levelId;
            overlayManager?.HideAllExceptToast();

            if (gameFlowController != null)
            {
                yield return gameFlowController.BuildLevelCoroutine(levelId);
            }
            else
            {
                LevelData data = levelManifest != null ? levelManifest.GetLevel(levelId - 1) : null;
                if (data == null) data = Resources.Load<LevelData>($"Levels/Level_{levelId}");
                if (data != null)
                    yield return levelLoader.LoadLevelCoroutine(data);
                if (levelLoader.StateMachine != null)
                    levelLoader.StateMachine.ResetToIdle();
            }
            CurrentLevelId = levelId;
        }

        public void EnterGame(int levelId)
        {
            CurrentLevelId = levelId;
            CurrentMode = ScreenMode.GamePlay;
            if (mainShellRoot != null) mainShellRoot.SetActive(false);
            if (gameRoot != null) gameRoot.SetActive(true);
        }

        public void ExitGameToHomeTab()
        {
            overlayManager?.HideAllExceptToast();
            ShowMainShell(MainTab.Home);
        }

        /// <summary>From GamePlay: show CONFIRM_EXIT_GAME if enabled, else exit to home.</summary>
        public void RequestExitGame()
        {
            if (CurrentMode != ScreenMode.GamePlay) return;
            if (Core.GameSettings.Instance != null && Core.GameSettings.Instance.ConfirmExitFromGame)
                overlayManager?.ShowConfirmExit(ExitGameToHomeTab);
            else
                ExitGameToHomeTab();
        }

        public void OnLevelComplete()
        {
            int levelId = levelLoader?.LevelData != null ? levelLoader.LevelData.levelId : CurrentLevelId;
            overlayManager?.ShowResultWin(levelId,
                onNext: () =>
                {
                    if (!HeartsManager.Instance.CanStartAttempt())
                        overlayManager?.ShowOutOfHearts(OutOfHeartsContext.FromResultWin, OnRefillThenResume, () => { });
                    else
                        RequestNext();
                },
                onLevelSelect: ExitGameToHomeTab);
        }

        public void OnHardFail(FailReason reason)
        {
            int hearts = HeartsManager.Instance != null ? HeartsManager.Instance.Hearts : 0;
            overlayManager?.ShowResultLose(hearts,
                onRetry: RequestRetry,
                onLevelSelect: ExitGameToHomeTab,
                onWatchAd: () => RunHeartsRefillAdFlow(OutOfHeartsContext.FromResultLose),
                showWatchAdButton: hearts == 0);
        }

        private void RunHeartsRefillAdFlow(OutOfHeartsContext ctx)
        {
            int levelIndex = CurrentLevelId > 0 ? Mathf.Max(0, CurrentLevelId - 1) : 0;
            HeartsRefillAdFlow.Run(levelIndex, adServiceComponent, OnRefillThenResume, () => { });
        }

        public void ShowOutOfHearts(OutOfHeartsContext ctx)
        {
            Action onBack = ctx == OutOfHeartsContext.FromLevelSelect || ctx == OutOfHeartsContext.FromHome
                ? (Action)(() => overlayManager?.HideOutOfHearts())
                : ExitGameToHomeTab;
            overlayManager?.ShowOutOfHearts(ctx, () => RunHeartsRefillAdFlow(ctx), onBack);
        }

        public void ShowToast(string msg)
        {
            overlayManager?.ShowToast(msg);
        }

        public void HandleBack()
        {
            if (IsTransitioning) return;
            if (overlayManager != null && overlayManager.IsResultVisible)
            {
                ExitGameToHomeTab();
                return;
            }
            if (overlayManager != null && overlayManager.IsOutOfHeartsVisible)
            {
                if (CurrentMode == ScreenMode.MainShell)
                    overlayManager.HideOutOfHearts();
                else
                    ExitGameToHomeTab();
                return;
            }
            if (overlayManager != null && overlayManager.IsConfirmExitVisible)
            {
                overlayManager.HideAllExceptToast();
                return;
            }
            if (CurrentMode == ScreenMode.GamePlay)
            {
                if (Core.GameSettings.Instance != null && Core.GameSettings.Instance.ConfirmExitFromGame)
                    overlayManager?.ShowConfirmExit(ExitGameToHomeTab);
                else
                    ExitGameToHomeTab();
                return;
            }
            if (CurrentTab != MainTab.Home)
            {
                ShowTab(MainTab.Home);
                return;
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>Bottom tab bar: Home / Shop / Settings. Highlights selected tab.</summary>
    public class MainShellNavBar : MonoBehaviour
    {
        [SerializeField] private Button homeButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject homeHighlight;
        [SerializeField] private GameObject shopHighlight;
        [SerializeField] private GameObject settingsHighlight;

        public void SetSelectedTab(MainTab tab)
        {
            if (homeHighlight != null) homeHighlight.SetActive(tab == MainTab.Home);
            if (shopHighlight != null) shopHighlight.SetActive(tab == MainTab.Shop);
            if (settingsHighlight != null) settingsHighlight.SetActive(tab == MainTab.Settings);
            // TODO: icon/label tint per tab
        }

        public void Bind(Action<MainTab> onTabSelected)
        {
            if (homeButton != null) homeButton.onClick.AddListener(() => onTabSelected?.Invoke(MainTab.Home));
            if (shopButton != null) shopButton.onClick.AddListener(() => onTabSelected?.Invoke(MainTab.Shop));
            if (settingsButton != null) settingsButton.onClick.AddListener(() => onTabSelected?.Invoke(MainTab.Settings));
        }
    }
}
