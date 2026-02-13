using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
        /// <summary>true면 Console에 [AppScene] 로그 출력. 버튼 무응답/퍼즐 미노출 디버깅용.</summary>
        public static bool DebugAppScene = true;
        [Header("Debug")]
        [Tooltip("체크 해제하면 [AppScene] 로그를 끔. 버튼/퍼즐 문제 해결 후 false 권장.")]
        [SerializeField] private bool enableAppSceneDebugLog = true;

        [Header("Roots")]
        [SerializeField] private GameObject mainShellRoot;
        [SerializeField] private GameObject gameRoot;
        /// <summary>퍼즐(노드/엣지)이 그려지는 월드 루트. Canvas 밖에 두어 카메라가 그리도록 함. 없으면 gameRoot 안에서 찾음.</summary>
        [SerializeField] private GameObject gameWorldRoot;
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
        /// <summary>true면 결과(승/패/OutOfHearts)는 OverlayManager로 표시. false면 GameHUD 자체 패널 사용.</summary>
        public bool UseOverlayForResult => overlayManager != null;
        /// <summary>퍼즐이 그려지는 Game 오브젝트. 런타임에 없으면 "Game" 이름으로 찾음.</summary>
        public GameObject GameWorldRoot
        {
            get
            {
                if (gameWorldRoot != null) return gameWorldRoot;
                if (_cachedGameWorld != null) return _cachedGameWorld;
                var go = GameObject.Find("Game");
                if (go != null) _cachedGameWorld = go;
                return _cachedGameWorld;
            }
        }
        private GameObject _cachedGameWorld;
        private bool IsTransitioning => TransitionManager.Instance != null && TransitionManager.Instance.IsTransitioning;
        private bool _lastBuildSucceeded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (overlayManager == null) overlayManager = GetComponentInChildren<OverlayManager>();
            if (levelLoader == null) levelLoader = FindFirstObjectByType<LevelLoader>();
            if (levelManifest == null) levelManifest = Resources.Load<LevelManifest>("Levels/GeneratedLevelManifest");
            if (gameFlowController == null) gameFlowController = FindFirstObjectByType<GameFlowController>();
            if (gameWorldRoot == null)
            {
                var go = GameObject.Find("Game");
                if (go != null) _cachedGameWorld = go;
            }
            DebugAppScene = enableAppSceneDebugLog;
            if (DebugAppScene)
            {
                Debug.Log($"[AppScene] AppRouter.Awake: levelLoader={levelLoader != null}, gameFlowController={gameFlowController != null}, gameWorldRoot={gameWorldRoot != null}, cachedGame={_cachedGameWorld != null}, mainShellRoot={mainShellRoot != null}, gameRoot={gameRoot != null}");
            }
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
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
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

        /// <summary>ScreenRouter가 탭 전환할 때 호출. AppRouter의 CurrentTab/하이라이트 동기화.</summary>
        public void SyncTabFromScreen(ScreenRouter.ScreenId screenId)
        {
            MainTab t = screenId == ScreenRouter.ScreenId.HomeTab ? MainTab.Home
                : screenId == ScreenRouter.ScreenId.ShopTab ? MainTab.Shop : MainTab.Settings;
            CurrentTab = t;
            if (homeTabView != null) homeTabView.SetActive(t == MainTab.Home);
            if (shopTabView != null) shopTabView.SetActive(t == MainTab.Shop);
            if (settingsTabView != null) settingsTabView.SetActive(t == MainTab.Settings);
            bottomNavBar?.SetSelectedTab(t);
        }

        public void ShowMainShell(MainTab tab)
        {
            CurrentMode = ScreenMode.MainShell;
            ScreenRouter.Instance?.SetGameInputEnabled(false);
            if (mainShellRoot != null) mainShellRoot.SetActive(true);
            if (gameRoot != null)
            {
                gameRoot.SetActive(false);
                var img = gameRoot.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.enabled = true;
            }
            if (GameWorldRoot != null) GameWorldRoot.SetActive(false);
            ShowTab(tab);
        }

        public void RequestStartLevel(int levelId)
        {
            int max = levelManifest != null ? Mathf.Max(1, levelManifest.Count) : 20;
            int unlocked = LevelRecords.LastUnlockedLevelId(max);
            if (levelId > unlocked)
            {
                ShowToast($"Level {unlocked}을(를) 먼저 클리어하세요.");
                levelId = unlocked;
            }

            if (DebugAppScene) Debug.Log($"[AppScene] RequestStartLevel(levelId={levelId})");
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
            int fromLoader = levelLoader?.LevelData != null ? levelLoader.LevelData.levelId : 0;
            int fromCurrent = CurrentLevelId;
            int fromPrefs = LevelRecords.LastPlayedLevelId;
            int currentId = Mathf.Max(fromLoader, Mathf.Max(fromCurrent, fromPrefs));
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
            // Safety net: if ad SDK callback ordering is inconsistent, keep game playable.
            if (HeartsManager.Instance != null && !HeartsManager.Instance.CanStartAttempt())
                HeartsManager.Instance.RefillFull();
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
                    if (!HeartsManager.Instance.CanStartAttempt())
                    {
                        overlayManager?.ShowOutOfHearts(OutOfHeartsContext.FromLevelSelect, OnRefillThenResume, () => { });
                        return;
                    }
                    StartCoroutine(RunTransitionThenEnterGame(intent.levelId));
                    break;
                case IntentType.RetryLevel:
                    if (!HeartsManager.Instance.CanStartAttempt())
                    {
                        overlayManager?.ShowOutOfHearts(OutOfHeartsContext.FromResultLose, OnRefillThenResume, () => { });
                        return;
                    }
                    StartCoroutine(RunTransitionThenEnterGame(intent.levelId));
                    break;
                case IntentType.NextLevel:
                    if (!HeartsManager.Instance.CanStartAttempt())
                    {
                        overlayManager?.ShowOutOfHearts(OutOfHeartsContext.FromResultWin, OnRefillThenResume, () => { });
                        return;
                    }
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

            if (!_lastBuildSucceeded)
            {
                ShowToast("Level data missing. Rebuild GeneratedLevelManifest.");
                yield break;
            }
            EnterGame(levelId);
        }

        private IEnumerator BuildLevelJob(int levelId)
        {
            if (DebugAppScene) Debug.Log($"[AppScene] BuildLevelJob(levelId={levelId}) levelLoader={levelLoader != null}, gameFlowController={gameFlowController != null}");
            _lastBuildSucceeded = false;

            if (levelLoader == null)
            {
                if (DebugAppScene) Debug.LogWarning("[AppScene] BuildLevelJob: levelLoader is null, level will NOT load.");
                yield break;
            }

            bool hasManifestData = levelManifest != null && levelManifest.GetLevel(levelId - 1) != null;
            bool hasResourceData = Resources.Load<LevelData>($"Levels/Level_{levelId}") != null;
            if (!hasManifestData && !hasResourceData)
            {
                int manifestCount = levelManifest != null ? levelManifest.Count : -1;
                Debug.LogWarning($"[AppScene] BuildLevelJob: no LevelData for levelId={levelId}. manifestCount={manifestCount}, resourcePath=Levels/Level_{levelId}");
                yield break;
            }

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

            _lastBuildSucceeded = levelLoader.LevelData != null && levelLoader.Runtime != null;
            if (!_lastBuildSucceeded && DebugAppScene)
            {
                var loadedId = levelLoader.LevelData != null ? levelLoader.LevelData.levelId : -1;
                Debug.LogWarning($"[AppScene] BuildLevelJob: load finished but runtime is invalid. loadedLevelId={loadedId}, targetLevelId={levelId}, runtime={(levelLoader.Runtime != null)}");
            }

            CurrentLevelId = levelId;
        }

        public void EnterGame(int levelId)
        {
            var gw = GameWorldRoot;
            if (DebugAppScene) Debug.Log($"[AppScene] EnterGame(levelId={levelId}) GameWorldRoot={gw != null}, active={gw != null && gw.activeSelf}, gameRoot={gameRoot != null}");
            CurrentLevelId = levelId;
            CurrentMode = ScreenMode.GamePlay;
            if (mainShellRoot != null) mainShellRoot.SetActive(false);
            if (gw != null)
            {
                gw.SetActive(true);
                gw.layer = 0;
                SetLayerRecursively(gw.transform, 0);
                if (DebugAppScene)
                {
                    var nodesTr = gw.transform.Find("Nodes");
                    int nodeCount = nodesTr != null ? nodesTr.childCount : -1;
                    var cam = Camera.main;
                    bool camSeesDefault = cam != null && (cam.cullingMask & (1 << 0)) != 0;
                    Debug.Log($"[AppScene] EnterGame: Game 활성화, Nodes={nodeCount}, Game.layer=Default(0), Camera.main culls Default={camSeesDefault}. (퍼즐 안 보이면 카메라 Culling Mask에 Default 체크 확인)");
                }
            }
            if (gameRoot != null)
            {
                gameRoot.SetActive(true);
                var img = gameRoot.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.color = Color.clear;
                    img.raycastTarget = false;
                    img.enabled = false;
                }
            }
            ScreenRouter.Instance?.SetGameInputEnabled(true);
        }

        private static void SetLayerRecursively(Transform t, int layer)
        {
            if (t == null) return;
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i), layer);
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
        [SerializeField] private UnityEngine.UI.Button homeButton;
        [SerializeField] private UnityEngine.UI.Button shopButton;
        [SerializeField] private UnityEngine.UI.Button settingsButton;
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
