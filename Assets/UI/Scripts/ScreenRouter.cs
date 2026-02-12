using System;
using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// Tab-based navigation: HomeTab / ShopTab / SettingsTab (with BottomNav) and Game (no BottomNav).
    /// Delegates to AppRouter when present for level load, hearts, overlays.
    /// CHECKLIST (hierarchy): Canvas > SafeAreaPanel > MainShellRoot > (HomeScreenRoot, ShopScreenRoot, SettingsScreenRoot, BottomNavBar); GameScreenRoot; ScreenRouter on Canvas. Copy LevelLoader+GameHUD into GameScreenRoot for gameplay.
    /// </summary>
    public class ScreenRouter : MonoBehaviour
    {
        public enum ScreenId
        {
            HomeTab,
            ShopTab,
            SettingsTab,
            Game
        }

        [Header("Screen roots (under Canvas)")]
        [SerializeField] private GameObject homeScreenRoot;
        [SerializeField] private GameObject shopScreenRoot;
        [SerializeField] private GameObject settingsScreenRoot;
        [SerializeField] private GameObject gameScreenRoot;

        [Header("Bottom nav (under Canvas, NOT under GameScreenRoot)")]
        [SerializeField] private BottomNavBar bottomNavBar;

        [Header("Optional: enable only in Game")]
        [SerializeField] private CircuitOneStroke.Input.TouchInputController touchInputController;
        [SerializeField] private GameObject worldRoot;

        [Header("Optional: level load via AppRouter")]
        [SerializeField] private bool useAppRouterForGame = true;

        public static ScreenRouter Instance { get; private set; }
        public ScreenId CurrentScreen { get; private set; } = ScreenId.HomeTab;
        public bool IsGameActive => CurrentScreen == ScreenId.Game;

        /// <summary>게임 터치 입력 켜기/끄기. AppRouter가 EnterGame/ShowMainShell 시 호출.</summary>
        public void SetGameInputEnabled(bool enabled)
        {
            if (touchInputController != null) touchInputController.enabled = enabled;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            var safe = transform.Find("SafeAreaPanel");
            var main = safe != null ? safe.Find("MainShellRoot") : null;
            if (homeScreenRoot == null) homeScreenRoot = main != null ? main.Find("HomeScreenRoot")?.gameObject : transform.Find("HomeScreenRoot")?.gameObject;
            if (shopScreenRoot == null) shopScreenRoot = main != null ? main.Find("ShopScreenRoot")?.gameObject : transform.Find("ShopScreenRoot")?.gameObject;
            if (settingsScreenRoot == null) settingsScreenRoot = main != null ? main.Find("SettingsScreenRoot")?.gameObject : transform.Find("SettingsScreenRoot")?.gameObject;
            if (gameScreenRoot == null) gameScreenRoot = safe != null ? safe.Find("GameScreenRoot")?.gameObject : transform.Find("GameScreenRoot")?.gameObject;
            if (bottomNavBar == null && main != null) bottomNavBar = main.Find("BottomNavBar")?.GetComponent<BottomNavBar>();
            if (touchInputController == null) touchInputController = FindFirstObjectByType<CircuitOneStroke.Input.TouchInputController>();
            if (AppRouter.DebugAppScene)
            {
                Debug.Log($"[AppScene] ScreenRouter.Awake: home={homeScreenRoot != null}, shop={shopScreenRoot != null}, settings={settingsScreenRoot != null}, gameScreenRoot={gameScreenRoot != null}, bottomNavBar={bottomNavBar != null}, safe={safe != null}, main={main != null}");
            }
        }

        private void Start()
        {
            if (worldRoot == null && AppRouter.Instance != null) worldRoot = AppRouter.Instance.GameWorldRoot;
            if (AppRouter.DebugAppScene) Debug.Log($"[AppScene] ScreenRouter.Start: worldRoot={worldRoot != null}, binding bottomNavBar={bottomNavBar != null}");
            ShowTab(ScreenId.HomeTab);
            if (bottomNavBar != null)
                bottomNavBar.Bind(this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Switch among Home/Shop/Settings; BottomNav stays visible and selected state updated.</summary>
        public void ShowTab(ScreenId tab)
        {
            if (AppRouter.DebugAppScene) Debug.Log($"[AppScene] ScreenRouter.ShowTab(tab={tab})");
            if (tab == ScreenId.Game) return;
            CurrentScreen = tab;
            SetTabRootsActive(tab);
            if (gameScreenRoot != null) gameScreenRoot.SetActive(false);
            if (bottomNavBar != null)
            {
                bottomNavBar.gameObject.SetActive(true);
                bottomNavBar.SetSelected(tab);
            }
            if (touchInputController != null) touchInputController.enabled = false;
            if (worldRoot != null) worldRoot.SetActive(false);
            if (AppRouter.Instance != null) AppRouter.Instance.SyncTabFromScreen(tab);
        }

        /// <summary>Hide BottomNav, show GameScreenRoot, load level (via AppRouter if useAppRouterForGame).</summary>
        public void EnterGame(int levelId)
        {
            if (AppRouter.DebugAppScene) Debug.Log($"[AppScene] ScreenRouter.EnterGame(levelId={levelId}) useAppRouter={useAppRouterForGame}, AppRouter.Instance={AppRouter.Instance != null}, worldRoot={worldRoot != null}");
            CurrentScreen = ScreenId.Game;
            if (homeScreenRoot != null) homeScreenRoot.SetActive(false);
            if (shopScreenRoot != null) shopScreenRoot.SetActive(false);
            if (settingsScreenRoot != null) settingsScreenRoot.SetActive(false);
            if (gameScreenRoot != null) gameScreenRoot.SetActive(true);
            if (bottomNavBar != null) bottomNavBar.gameObject.SetActive(false);
            if (touchInputController != null) touchInputController.enabled = true;
            if (worldRoot != null) worldRoot.SetActive(true);

            if (useAppRouterForGame && AppRouter.Instance != null)
                AppRouter.Instance.RequestStartLevel(levelId);
            else
                TryLoadLevelDirect(levelId);
        }

        /// <summary>Hide GameScreenRoot, show BottomNav + HomeTab.</summary>
        public void ExitGameToHome()
        {
            if (touchInputController != null) touchInputController.enabled = false;
            if (AppRouter.Instance != null)
            {
                AppRouter.Instance.ExitGameToHomeTab();
                CurrentScreen = ScreenId.HomeTab;
                SetTabRootsActive(ScreenId.HomeTab);
                if (bottomNavBar != null) { bottomNavBar.gameObject.SetActive(true); bottomNavBar.SetSelected(ScreenId.HomeTab); }
                return;
            }
            CurrentScreen = ScreenId.HomeTab;
            if (gameScreenRoot != null) gameScreenRoot.SetActive(false);
            SetTabRootsActive(ScreenId.HomeTab);
            if (bottomNavBar != null) { bottomNavBar.gameObject.SetActive(true); bottomNavBar.SetSelected(ScreenId.HomeTab); }
        }

        private void SetTabRootsActive(ScreenId tab)
        {
            if (homeScreenRoot != null) homeScreenRoot.SetActive(tab == ScreenId.HomeTab);
            if (shopScreenRoot != null) shopScreenRoot.SetActive(tab == ScreenId.ShopTab);
            if (settingsScreenRoot != null) settingsScreenRoot.SetActive(tab == ScreenId.SettingsTab);
        }

        private void TryLoadLevelDirect(int levelId)
        {
            var loader = FindFirstObjectByType<LevelLoader>();
            var manifest = Resources.Load<LevelManifest>("Levels/GeneratedLevelManifest");
            if (loader != null && manifest != null)
            {
                var data = manifest.GetLevel(levelId - 1);
                if (data != null)
                    loader.LoadLevel(data);
            }
        }
    }
}
