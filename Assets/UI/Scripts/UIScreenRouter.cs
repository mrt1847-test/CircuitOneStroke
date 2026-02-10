using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 화면 스왑 관리. 단일 Canvas 하위에서 스크린 프리팹을 교체.
    /// ResultDialog와 OutOfHeartsPanel 상호 배타.
    /// </summary>
    public class UIScreenRouter : MonoBehaviour
    {
        public enum Screen
        {
            Home,
            LevelSelect,
            GameHUD,
            Settings,
            Shop,
            OutOfHearts
        }

        public enum BaseScreen
        {
            HOME,
            LEVEL_SELECT,
            GAME
        }

        [Header("Screen Prefabs")]
        [SerializeField] private GameObject homeScreenPrefab;
        [SerializeField] private GameObject levelSelectScreenPrefab;
        [SerializeField] private GameObject gameHUDScreenPrefab;
        [SerializeField] private GameObject gameHUDScreenInstance; // optional: in-scene HUD
        [SerializeField] private GameObject settingsScreenPrefab;
        [SerializeField] private GameObject shopScreenPrefab;
        [SerializeField] private GameObject outOfHeartsScreenPrefab;

        [Header("Container")]
        [SerializeField] private Transform screenContainer;

        [Header("References")]
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private LevelManifest levelManifest;
        [SerializeField] private CircuitOneStroke.UI.Theme.CircuitOneStrokeTheme theme;

        public Screen CurrentScreen { get; private set; } = Screen.Home;
        public BaseScreen CurrentBaseScreen =>
            CurrentScreen == Screen.Home ? BaseScreen.HOME :
            CurrentScreen == Screen.LevelSelect ? BaseScreen.LEVEL_SELECT :
            CurrentScreen == Screen.GameHUD ? BaseScreen.GAME : BaseScreen.HOME;
        public event Action<Screen> OnScreenChanged;

        private readonly Dictionary<Screen, GameObject> _instantiated = new Dictionary<Screen, GameObject>();
        private readonly Stack<Screen> _history = new Stack<Screen>();
        private bool _resultDialogVisible;
        private bool _outOfHeartsVisible;
        private OutOfHeartsContext _outOfHeartsContext;
        private const int DefaultMaxLevels = 20;

        [Header("Overlay References")]
        [SerializeField] private GameHUD gameHUDRef;

        private void Awake()
        {
            if (screenContainer == null)
                screenContainer = transform;
            if (gameHUDRef == null)
                gameHUDRef = FindObjectOfType<GameHUD>();
        }

        private void Start()
        {
            var flow = GameFlowController.Instance ?? FindObjectOfType<GameFlowController>();
            if (flow != null)
                flow.Boot();
            else
                ShowHome();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                HandleAndroidBack();
        }

        private GameObject GetOrInstantiate(Screen screen)
        {
            if (_instantiated.TryGetValue(screen, out var go) && go != null)
                return go;

            // GameHUD: use in-scene instance if provided
            if (screen == Screen.GameHUD && gameHUDScreenInstance != null)
            {
                go = gameHUDScreenInstance;
                if (screenContainer != null && go.transform.parent != screenContainer)
                    go.transform.SetParent(screenContainer, false);
                _instantiated[screen] = go;
                if (go.TryGetComponent<IUIScreen>(out var uiScreen))
                    uiScreen.BindRouter(this);
                return go;
            }

            var prefab = GetPrefab(screen);
            if (prefab == null) return null;

            go = Instantiate(prefab, screenContainer);
            go.name = screen.ToString();
            _instantiated[screen] = go;

            if (go.TryGetComponent<IUIScreen>(out var uiScreen2))
                uiScreen2.BindRouter(this);

            return go;
        }

        private GameObject GetPrefab(Screen screen)
        {
            return screen switch
            {
                Screen.Home => homeScreenPrefab,
                Screen.LevelSelect => levelSelectScreenPrefab,
                Screen.GameHUD => gameHUDScreenPrefab,
                Screen.Settings => settingsScreenPrefab,
                Screen.Shop => shopScreenPrefab,
                Screen.OutOfHearts => outOfHeartsScreenPrefab,
                _ => null
            };
        }

        private void SetScreenActive(Screen screen, bool active)
        {
            var go = GetOrInstantiate(screen);
            if (go != null)
                go.SetActive(active);
        }

        private void HideAllScreens()
        {
            foreach (var kv in _instantiated)
                if (kv.Value != null)
                    kv.Value.SetActive(false);
        }

        public void ShowHome()
        {
            _history.Clear();
            _outOfHeartsVisible = false;
            _resultDialogVisible = false;
            HideAllScreens();
            SetScreenActive(Screen.Home, true);
            CurrentScreen = Screen.Home;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        public void ShowLevelSelect()
        {
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.RunTransition(ShowLevelSelectCoroutine());
                return;
            }
            DoShowLevelSelect();
        }

        private IEnumerator ShowLevelSelectCoroutine()
        {
            yield return null;
            DoShowLevelSelect();
        }

        private void DoShowLevelSelect()
        {
            _history.Clear();
            _outOfHeartsVisible = false;
            _resultDialogVisible = false;
            HideAllScreens();
            SetScreenActive(Screen.LevelSelect, true);
            CurrentScreen = Screen.LevelSelect;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        public void ShowGameHUD()
        {
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.RunTransition(ShowGameHUDCoroutine());
                return;
            }
            DoShowGameHUD();
        }

        private IEnumerator ShowGameHUDCoroutine()
        {
            yield return null;
            DoShowGameHUD();
        }

        private void DoShowGameHUD()
        {
            _history.Clear();
            _outOfHeartsVisible = false;
            HideAllScreens();
            SetScreenActive(Screen.GameHUD, true);
            CurrentScreen = Screen.GameHUD;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        public void ShowSettings()
        {
            _history.Push(CurrentScreen);
            HideAllScreens();
            SetScreenActive(Screen.Settings, true);
            CurrentScreen = Screen.Settings;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        public void ShowShop()
        {
            _history.Push(CurrentScreen);
            HideAllScreens();
            SetScreenActive(Screen.Shop, true);
            CurrentScreen = Screen.Shop;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        /// <summary>Settings/Shop/OutOfHearts에서 Back 시 이전 화면으로. 히스토리 스택 기반.</summary>
        public void GoBack()
        {
            var target = _history.Count > 0 ? _history.Pop() : Screen.Home;
            if (target == Screen.Settings || target == Screen.Shop || target == Screen.OutOfHearts)
                target = Screen.Home;

            if (CurrentScreen == Screen.OutOfHearts)
                _outOfHeartsVisible = false;

            HideAllScreens();
            SetScreenActive(target, true);
            CurrentScreen = target;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        public void ShowOutOfHearts(OutOfHeartsContext ctx)
        {
            if (TransitionManager.Instance != null && TransitionManager.Instance.IsTransitioning)
                return;

            _outOfHeartsContext = ctx;
            _resultDialogVisible = false;
            _outOfHeartsVisible = true;
            if (gameHUDRef != null)
                gameHUDRef.SetResultDialogVisible(false);

            _history.Push(CurrentScreen);
            HideAllScreens();
            SetScreenActive(Screen.OutOfHearts, true);
            CurrentScreen = Screen.OutOfHearts;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        public void ShowResultWin(int currentLevelId)
        {
            _outOfHeartsVisible = false;
            _resultDialogVisible = true;
            if (gameHUDRef != null)
                gameHUDRef.SetResultDialogVisible(true);

            if (CurrentScreen != Screen.GameHUD)
            {
                HideAllScreens();
                SetScreenActive(Screen.GameHUD, true);
                CurrentScreen = Screen.GameHUD;
                OnScreenChanged?.Invoke(CurrentScreen);
            }
        }

        public void ShowResultLose()
        {
            _outOfHeartsVisible = false;
            _resultDialogVisible = true;
            if (gameHUDRef != null)
                gameHUDRef.SetResultDialogVisible(true);

            if (CurrentScreen != Screen.GameHUD)
            {
                HideAllScreens();
                SetScreenActive(Screen.GameHUD, true);
                CurrentScreen = Screen.GameHUD;
                OnScreenChanged?.Invoke(CurrentScreen);
            }
        }

        public void HideAllOverlays()
        {
            _resultDialogVisible = false;
            _outOfHeartsVisible = false;
            if (gameHUDRef != null)
                gameHUDRef.SetResultDialogVisible(false);
        }

        public void ShowToast(string msg)
        {
            GameFeedback.RequestToast(msg);
        }

        private void HandleAndroidBack()
        {
            if (TransitionManager.Instance != null && TransitionManager.Instance.IsTransitioning)
                return;

            if (_outOfHeartsVisible)
            {
                GoBack();
                return;
            }
            if (CurrentScreen == Screen.Settings || CurrentScreen == Screen.Shop)
            {
                GoBack();
                return;
            }
            if (CurrentScreen == Screen.GameHUD && _resultDialogVisible)
            {
                HideAllOverlays();
                if (gameHUDRef != null)
                    gameHUDRef.HideResultAndResetState();
                return;
            }
            if (CurrentScreen == Screen.GameHUD)
            {
                ShowHome();
                return;
            }
            if (CurrentScreen == Screen.LevelSelect)
            {
                ShowHome();
                return;
            }
            if (CurrentScreen == Screen.Home)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        public void ShowGame()
        {
            ShowGameHUD();
        }

        /// <summary>Continue/Play: GameFlowController.RequestStartLevel로 위임.</summary>
        public void StartContinue()
        {
            var flow = GameFlowController.Instance ?? FindObjectOfType<GameFlowController>();
            if (flow != null)
            {
                int last = LevelRecords.LastPlayedLevelId;
                int max = levelManifest != null ? levelManifest.Count : DefaultMaxLevels;
                int levelId = Mathf.Clamp(last, 1, Mathf.Max(1, max));
                flow.RequestStartLevel(levelId);
                return;
            }
            int id = LevelRecords.LastPlayedLevelId;
            if (levelManifest != null && levelManifest.Count > 0)
                id = Mathf.Clamp(id, 1, levelManifest.Count);
            StartLevelLegacy(id);
        }

        /// <summary>GameFlowController 있으면 RequestStartLevel로 위임, 없으면 레거시.</summary>
        public void StartLevel(int levelId)
        {
            var flow = GameFlowController.Instance ?? FindObjectOfType<GameFlowController>();
            if (flow != null)
            {
                flow.RequestStartLevel(levelId);
                return;
            }
            StartLevelLegacy(levelId);
        }

        private void StartLevelLegacy(int levelId)
        {
            if (levelLoader == null) return;
            LevelRecords.LastPlayedLevelId = levelId;
            IEnumerator work = null;
            if (levelManifest != null)
            {
                var data = levelManifest.GetLevel(levelId - 1);
                if (data != null)
                    work = levelLoader.LoadLevelCoroutine(data);
            }
            if (work == null)
                work = levelLoader.LoadLevelCoroutine(levelId);
            if (work != null && TransitionManager.Instance != null)
                TransitionManager.Instance.RunTransition(StartLevelRoutine(work));
            else if (work != null)
                StartCoroutine(StartLevelRoutine(work));
            else
                DoShowGameHUD();
        }

        private IEnumerator StartLevelRoutine(IEnumerator loadWork)
        {
            if (loadWork != null)
            {
                while (loadWork.MoveNext())
                    yield return loadWork.Current;
            }
            DoShowGameHUD();
        }

        public LevelLoader LevelLoader => levelLoader;
        public LevelManifest LevelManifest => levelManifest;
    }

    public interface IUIScreen
    {
        void BindRouter(UIScreenRouter router);
    }
}
