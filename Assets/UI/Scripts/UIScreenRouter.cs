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
        public event Action<Screen> OnScreenChanged;

        private readonly Dictionary<Screen, GameObject> _instantiated = new Dictionary<Screen, GameObject>();
        private Screen _screenBeforeOverlay;
        private const int DefaultMaxLevels = 20;

        private void Awake()
        {
            if (screenContainer == null)
                screenContainer = transform;
        }

        private void Start()
        {
            ShowHome();
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
            HideAllScreens();
            SetScreenActive(Screen.GameHUD, true);
            CurrentScreen = Screen.GameHUD;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        public void ShowSettings()
        {
            _screenBeforeOverlay = CurrentScreen;
            HideAllScreens();
            SetScreenActive(Screen.Settings, true);
            CurrentScreen = Screen.Settings;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        public void ShowShop()
        {
            _screenBeforeOverlay = CurrentScreen;
            HideAllScreens();
            SetScreenActive(Screen.Shop, true);
            CurrentScreen = Screen.Shop;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        /// <summary>Settings/Shop에서 Back 시 이전 화면으로.</summary>
        public void GoBack()
        {
            var target = _screenBeforeOverlay;
            if (target == Screen.Settings || target == Screen.Shop || target == Screen.OutOfHearts)
                target = Screen.Home;
            _screenBeforeOverlay = Screen.Home;

            HideAllScreens();
            SetScreenActive(target, true);
            CurrentScreen = target;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        public void ShowOutOfHearts()
        {
            _screenBeforeOverlay = CurrentScreen;
            HideAllScreens();
            SetScreenActive(Screen.OutOfHearts, true);
            CurrentScreen = Screen.OutOfHearts;
            OnScreenChanged?.Invoke(CurrentScreen);
        }

        /// <summary>Continue/Play: 마지막 플레이 레벨 로드 후 GameHUD.</summary>
        public void StartContinue()
        {
            int last = LevelRecords.LastPlayedLevelId;
            int max = levelManifest != null ? levelManifest.Count : DefaultMaxLevels;
            int levelId = Mathf.Clamp(last, 1, Mathf.Max(1, max));
            StartLevel(levelId);
        }

        /// <summary>레벨 로드 후 GameHUD 표시.</summary>
        public void StartLevel(int levelId)
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
            {
                TransitionManager.Instance.RunTransition(StartLevelRoutine(work));
            }
            else if (work != null)
            {
                StartCoroutine(StartLevelRoutine(work));
            }
            else
            {
                DoShowGameHUD();
            }
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
