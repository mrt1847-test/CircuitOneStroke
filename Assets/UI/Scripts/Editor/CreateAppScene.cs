#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CircuitOneStroke.UI;
using CircuitOneStroke.UI.Theme;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// Creates AppScene with tab-based flow: HomeScreenRoot, ShopScreenRoot, SettingsScreenRoot, GameScreenRoot, BottomNavBar.
    /// Sets AppScene as first scene in Build Settings.
    /// CHECKLIST after run: Canvas > SafeAreaPanel > MainShellRoot > (HomeScreenRoot, ShopScreenRoot, SettingsScreenRoot, BottomNavBar); GameScreenRoot; ScreenRouter on Canvas.
    /// Gameplay hierarchy is auto-created under GameScreenRoot during AppScene generation.
    /// </summary>
    public static class CreateAppScene
    {
        private const string ScenePath = "Assets/Scenes/AppScene.unity";

        [MenuItem("Circuit One-Stroke/Create AppScene (Tab Flow + Set First Build)")]
        public static void Create()
        {
            CreateInternal(applyKenneyTheme: false);
        }

        [MenuItem("Circuit One-Stroke/Create AppScene (Tab Flow + Apply Kenney Theme + Set First Build)")]
        public static void CreateWithKenneyTheme()
        {
            CreateInternal(applyKenneyTheme: true);
        }

        private static void CreateInternal(bool applyKenneyTheme)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EnsureGameplayPrefabs();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.orthographic = true;
                cam.orthographicSize = 6.5f;
                cam.transform.position = new Vector3(0, 0, -10);
                cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esGo.AddComponent<StandaloneInputModule>();
#endif
            }

            var theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>("Assets/UI/Theme/CircuitOneStrokeTheme.asset");
            var canvas = CreateCanvas();
            var safeArea = CreateSafeArea(canvas.transform);
            var mainShellRoot = CreateMainShellRoot(safeArea, theme);
            var homeRoot = CreateHomeScreenRoot(mainShellRoot);
            var shopRoot = CreateShopScreenRoot(mainShellRoot);
            var settingsRoot = CreateSettingsScreenRoot(mainShellRoot);
            var bottomNav = CreateBottomNavBar(mainShellRoot, theme);
            var gameRoot = CreateGameScreenRoot(safeArea, cam, theme);

            // 퍼즐(노드/엣지)은 카메라가 그려야 하므로 Game을 씬 루트로 이동. GameScene처럼 Canvas와 형제로 두어야 카메라가 그림.
            Transform gameTransform = gameRoot.transform.Find("Game");
            GameObject gameWorldRoot = null;
            if (gameTransform != null)
            {
                gameTransform.SetParent(null, true);
                gameWorldRoot = gameTransform.gameObject;
                gameWorldRoot.SetActive(false);
            }

            var overlayRoot = CreateMainShellHierarchy.CreateOverlayRoot(safeArea, theme);
            var overlayManager = overlayRoot != null ? overlayRoot.GetComponentInChildren<OverlayManager>() : null;

            var themeApplier = canvas.gameObject.AddComponent<ThemeApplier>();
            var themeSo = new SerializedObject(themeApplier);
            themeSo.FindProperty("theme").objectReferenceValue = theme;
            themeSo.ApplyModifiedPropertiesWithoutUndo();

            var screenRouter = canvas.gameObject.AddComponent<ScreenRouter>();
            var so = new SerializedObject(screenRouter);
            so.FindProperty("homeScreenRoot").objectReferenceValue = homeRoot;
            so.FindProperty("shopScreenRoot").objectReferenceValue = shopRoot;
            so.FindProperty("settingsScreenRoot").objectReferenceValue = settingsRoot;
            so.FindProperty("gameScreenRoot").objectReferenceValue = gameRoot;
            so.FindProperty("bottomNavBar").objectReferenceValue = bottomNav;
            so.FindProperty("useAppRouterForGame").boolValue = true;
            if (gameWorldRoot != null)
            {
                so.FindProperty("worldRoot").objectReferenceValue = gameWorldRoot;
                var touchInput = gameWorldRoot.GetComponentInChildren<CircuitOneStroke.Input.TouchInputController>(true);
                if (touchInput != null) so.FindProperty("touchInputController").objectReferenceValue = touchInput;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // AppRouter에 게임플레이 참조 연결 (없으면 레벨 로드가 되지 않아 퍼즐이 안 보임)
            if (gameTransform == null) gameTransform = GameObject.Find("Game")?.transform;
            var levelLoader = gameTransform != null ? gameTransform.GetComponent<CircuitOneStroke.Core.LevelLoader>() : null;
            var gameFlowController = gameTransform != null ? gameTransform.Find("GameFlowController")?.GetComponent<CircuitOneStroke.Core.GameFlowController>() : null;

            var appRouterGo = new GameObject("AppRouter");
            appRouterGo.transform.SetParent(canvas.transform, false);
            var appRouter = appRouterGo.AddComponent<AppRouter>();
            var appSo = new SerializedObject(appRouter);
            appSo.FindProperty("mainShellRoot").objectReferenceValue = mainShellRoot.gameObject;
            appSo.FindProperty("gameRoot").objectReferenceValue = gameRoot;
            if (gameWorldRoot != null) appSo.FindProperty("gameWorldRoot").objectReferenceValue = gameWorldRoot;
            appSo.FindProperty("homeTabView").objectReferenceValue = homeRoot;
            appSo.FindProperty("shopTabView").objectReferenceValue = shopRoot;
            appSo.FindProperty("settingsTabView").objectReferenceValue = settingsRoot;
            appSo.FindProperty("bottomNavBar").objectReferenceValue = null; // 탭 클릭은 ScreenRouter가 처리, Bind(ScreenRouter)만 지원
            appSo.FindProperty("levelManifest").objectReferenceValue = Resources.Load<LevelManifest>("Levels/GeneratedLevelManifest");
            if (levelLoader != null) appSo.FindProperty("levelLoader").objectReferenceValue = levelLoader;
            if (gameFlowController != null) appSo.FindProperty("gameFlowController").objectReferenceValue = gameFlowController;
            if (overlayManager != null) appSo.FindProperty("overlayManager").objectReferenceValue = overlayManager;
            appSo.ApplyModifiedPropertiesWithoutUndo();

            gameRoot.SetActive(false);
            if (applyKenneyTheme)
                AssignKenneyToTheme.Assign();
            if (theme != null) themeApplier.Apply(theme);
            EditorSceneManager.SaveScene(scene, ScenePath);
            SetAppSceneFirstInBuildSettings();
            if (applyKenneyTheme)
                Debug.Log("AppScene created at " + ScenePath + " with Kenney theme applied. Set as first build scene.");
            else
                Debug.Log("AppScene created at " + ScenePath + " without modifying theme assets. Set as first build scene.");
        }

        /// <summary>열려 있는 앱씬에서 퍼즐이 안 보이거나 레벨이 안 로드될 때: AppRouter에 LevelLoader/GameFlowController 연결, GameScreenRoot 배경 투명 처리.</summary>
        [MenuItem("Circuit One-Stroke/Repair AppScene (Wire AppRouter + Show Puzzle)")]
        public static void RepairAppSceneReferences()
        {
            var appRouter = Object.FindFirstObjectByType<CircuitOneStroke.UI.AppRouter>();
            if (appRouter == null)
            {
                Debug.LogWarning("AppRouter not found in scene. Open AppScene first.");
                return;
            }
            var appSo = new SerializedObject(appRouter);
            var gameRoot = appSo.FindProperty("gameRoot").objectReferenceValue as GameObject;
            if (gameRoot == null)
            {
                Debug.LogWarning("AppRouter.gameRoot not set. Assign in Inspector or run Create AppScene.");
                return;
            }
            var gameTransform = gameRoot.transform.Find("Game");
            if (gameTransform == null)
                gameTransform = appRouter.transform.parent != null ? appRouter.transform.parent.Find("Game") : null;
            if (gameTransform == null)
                gameTransform = GameObject.Find("Game")?.transform;
            var levelLoader = gameTransform != null ? gameTransform.GetComponent<CircuitOneStroke.Core.LevelLoader>() : null;
            var gameFlowController = gameTransform != null ? gameTransform.Find("GameFlowController")?.GetComponent<CircuitOneStroke.Core.GameFlowController>() : null;
            if (levelLoader != null) appSo.FindProperty("levelLoader").objectReferenceValue = levelLoader;
            if (gameFlowController != null) { appSo.FindProperty("gameFlowController").objectReferenceValue = gameFlowController; }

            // Game이 아직 GameScreenRoot 안에 있으면 씬 루트로 옮겨 퍼즐이 카메라에 그려지도록 함 (GameScene과 동일)
            if (gameTransform != null && gameTransform.parent == gameRoot.transform)
            {
                gameTransform.SetParent(null, true);
                appSo.FindProperty("gameWorldRoot").objectReferenceValue = gameTransform.gameObject;
            }
            var gameWorldRootRef = appSo.FindProperty("gameWorldRoot");
            if (gameWorldRootRef != null && gameWorldRootRef.objectReferenceValue == null && gameTransform != null)
                gameWorldRootRef.objectReferenceValue = gameTransform.gameObject;
            appSo.ApplyModifiedPropertiesWithoutUndo();

            var gsrImage = gameRoot.GetComponent<Image>();
            if (gsrImage != null && gsrImage.color != Color.clear)
            {
                gsrImage.color = Color.clear;
                gsrImage.raycastTarget = false;
            }

            var screenRouter = appRouter.transform.parent != null ? appRouter.transform.parent.GetComponent<ScreenRouter>() : null;
            if (screenRouter != null && gameTransform != null)
            {
                var srSo = new SerializedObject(screenRouter);
                srSo.FindProperty("worldRoot").objectReferenceValue = gameTransform.gameObject;
                var touchInput = gameTransform.GetComponentInChildren<CircuitOneStroke.Input.TouchInputController>(true);
                if (touchInput != null) srSo.FindProperty("touchInputController").objectReferenceValue = touchInput;
                srSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var overlayMgr = Object.FindFirstObjectByType<OverlayManager>();
            if (overlayMgr != null)
            {
                appSo.FindProperty("overlayManager").objectReferenceValue = overlayMgr;
                appSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(appRouter);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("Repair AppScene: AppRouter + ScreenRouter wired; Game at scene root. Save scene and Play to see puzzle and use buttons.");
        }

        private static GameObject CreateCanvas()
        {
            var go = new GameObject("Canvas");
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
            go.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static Transform CreateSafeArea(Transform parent)
        {
            var go = new GameObject("SafeAreaPanel");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go.transform;
        }

        private static GameObject CreateMainShellRoot(Transform parent, CircuitOneStrokeTheme theme)
        {
            var go = new GameObject("MainShellRoot");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject CreateHomeScreenRoot(GameObject mainShell)
        {
            var go = new GameObject("HomeScreenRoot");
            go.transform.SetParent(mainShell.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.12f);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            AddPanelImage(go, new Color(0.12f, 0.12f, 0.18f));
            var homePanelRole = go.AddComponent<ThemeRole>();
            homePanelRole.role = ThemeRole.Role.Panel;

            // AppScene 홈: 제목 + 레벨 그리드만. Back/Continue/Settings는 GameScene 스타일이므로 넣지 않음 (설정은 하단 탭에서).
            var title = NewText(go.transform, "Level Select", 48);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.94f);
            titleRect.anchorMax = new Vector2(0.5f, 0.94f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(400, 80);

            // 레벨 그리드만 사용. LevelSelectScreen 프리팹의 Back/Settings 버튼은 App에서는 숨김.
            GameObject levelSelectGo = null;
            var levelSelectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Screens/LevelSelectScreen.prefab");
            if (levelSelectPrefab != null)
            {
                levelSelectGo = (GameObject)PrefabUtility.InstantiatePrefab(levelSelectPrefab, go.transform);
                levelSelectGo.name = "LevelSelectScreen";
                var lsRect = levelSelectGo.GetComponent<RectTransform>();
                if (lsRect != null)
                {
                    lsRect.anchorMin = new Vector2(0.05f, 0.02f);
                    lsRect.anchorMax = new Vector2(0.95f, 0.90f);
                    lsRect.offsetMin = Vector2.zero;
                    lsRect.offsetMax = Vector2.zero;
                }
                var levelSelectScreen = levelSelectGo.GetComponent<LevelSelectScreen>();
                if (levelSelectScreen != null)
                {
                    var screenSo = new SerializedObject(levelSelectScreen);
                    var backProp = screenSo.FindProperty("backButton");
                    var settingsProp = screenSo.FindProperty("settingsButton");
                    if (backProp != null && backProp.objectReferenceValue is Component backComp)
                        backComp.gameObject.SetActive(false);
                    if (settingsProp != null && settingsProp.objectReferenceValue is Component setComp)
                        setComp.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning("LevelSelectScreen.prefab not found. Create via Circuit One-Stroke > UI > Create Screen Prefabs first.");
            }

            var homeTabView = go.AddComponent<HomeTabView>();
            var homeTabSo = new SerializedObject(homeTabView);
            homeTabSo.FindProperty("continueButton").objectReferenceValue = null;
            homeTabSo.FindProperty("levelSelectScreen").objectReferenceValue = levelSelectGo != null ? levelSelectGo.GetComponent<LevelSelectScreen>() : null;
            homeTabSo.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static GameObject CreateShopScreenRoot(GameObject mainShell)
        {
            var go = new GameObject("ShopScreenRoot");
            go.transform.SetParent(mainShell.transform, false);
            go.SetActive(false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.12f);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            AddPanelImage(go, new Color(0.1f, 0.14f, 0.2f));
            var shopPanelRole = go.AddComponent<ThemeRole>();
            shopPanelRole.role = ThemeRole.Role.Panel;

            var title = NewText(go.transform, "Shop", 48);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.92f);
            titleRect.anchorMax = new Vector2(0.5f, 0.92f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(400, 80);

            var noAds = NewText(go.transform, "Remove Ads (NoAds product – TODO IAP)", 28);
            var noAdsRect = noAds.GetComponent<RectTransform>();
            noAdsRect.anchorMin = new Vector2(0.5f, 0.5f);
            noAdsRect.anchorMax = new Vector2(0.5f, 0.5f);
            noAdsRect.pivot = new Vector2(0.5f, 0.5f);
            noAdsRect.anchoredPosition = Vector2.zero;
            noAdsRect.sizeDelta = new Vector2(600, 120);
            return go;
        }

        private static GameObject CreateSettingsScreenRoot(GameObject mainShell)
        {
            var go = new GameObject("SettingsScreenRoot");
            go.transform.SetParent(mainShell.transform, false);
            go.SetActive(false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.12f);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            AddPanelImage(go, new Color(0.12f, 0.1f, 0.18f));
            var settingsPanelRole = go.AddComponent<ThemeRole>();
            settingsPanelRole.role = ThemeRole.Role.Panel;

            var title = NewText(go.transform, "Settings", 48);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.92f);
            titleRect.anchorMax = new Vector2(0.5f, 0.92f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(400, 80);

            var content = new GameObject("SettingsContent");
            content.transform.SetParent(go.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.2f);
            contentRect.anchorMax = new Vector2(0.9f, 0.85f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var gs = GameSettings.Instance;
            float y = 0.9f;
            AddToggleRow(content.transform, "BGM", gs != null && gs.Data.musicEnabled, v => { if (GameSettings.Instance != null) GameSettings.Instance.MusicEnabled = v; }, ref y);
            AddToggleRow(content.transform, "SFX", gs != null && gs.Data.sfxEnabled, v => { if (GameSettings.Instance != null) GameSettings.Instance.SfxEnabled = v; }, ref y);
            AddToggleRow(content.transform, "Vibration", gs != null && gs.Data.hapticsEnabled, v => { if (GameSettings.Instance != null) GameSettings.Instance.HapticsEnabled = v; }, ref y);
            var langLabel = NewText(content.transform, "Language", 24);
            SetRect(langLabel.rectTransform, 0, y - 0.12f, 1, y);
            var dropdown = NewDropdown(content.transform, new[] { "System", "Korean", "English" }, gs != null ? Mathf.Clamp(gs.Data.language, 0, 2) : 0, i => { if (GameSettings.Instance != null) GameSettings.Instance.LanguageValue = (Language)i; });
            SetRect(dropdown.GetComponent<RectTransform>(), 0.4f, y - 0.2f, 0.9f, y - 0.08f);
            return go;
        }

        private static void AddToggleRow(Transform parent, string label, bool isOn, System.Action<bool> onChanged, ref float y)
        {
            var row = new GameObject("Row_" + label);
            row.transform.SetParent(parent, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, y - 0.1f);
            rowRect.anchorMax = new Vector2(1, y);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;
            var t = NewText(row.transform, label, 28);
            t.rectTransform.anchorMin = new Vector2(0, 0);
            t.rectTransform.anchorMax = new Vector2(0.6f, 1);
            t.rectTransform.offsetMin = Vector2.zero;
            t.rectTransform.offsetMax = Vector2.zero;
            var toggleGo = new GameObject("Toggle");
            toggleGo.transform.SetParent(row.transform, false);
            var toggleRect = toggleGo.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.7f, 0.2f);
            toggleRect.anchorMax = new Vector2(0.95f, 0.8f);
            toggleRect.offsetMin = Vector2.zero;
            toggleRect.offsetMax = Vector2.zero;
            var toggle = toggleGo.AddComponent<Toggle>();
            var bg = toggleGo.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f);
            toggle.targetGraphic = bg;
            var check = new GameObject("Checkmark");
            check.transform.SetParent(toggleGo.transform, false);
            var checkImg = check.AddComponent<Image>();
            checkImg.color = new Color(0.3f, 0.8f, 0.4f);
            var checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkRect.offsetMin = checkRect.offsetMax = Vector2.zero;
            toggle.graphic = checkImg;
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            y -= 0.12f;
        }

        private static Dropdown NewDropdown(Transform parent, string[] options, int value, System.Action<int> onChanged)
        {
            var go = new GameObject("Dropdown");
            go.transform.SetParent(parent, false);
            var dd = go.AddComponent<Dropdown>();
            var template = new List<Dropdown.OptionData>();
            foreach (var o in options) template.Add(new Dropdown.OptionData(o));
            dd.ClearOptions();
            dd.AddOptions(template);
            dd.value = value;
            dd.RefreshShownValue();
            dd.onValueChanged.AddListener(i => onChanged?.Invoke(i));
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.28f);
            return dd;
        }

        private static void SetRect(RectTransform r, float minX, float minY, float maxX, float maxY)
        {
            r.anchorMin = new Vector2(minX, minY);
            r.anchorMax = new Vector2(maxX, maxY);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private static GameObject CreateBottomNavBar(GameObject mainShell, CircuitOneStrokeTheme theme)
        {
            var go = new GameObject("BottomNavBar");
            go.transform.SetParent(mainShell.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0.12f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = theme != null ? theme.primary : new Color(0.2f, 0.25f, 0.35f);
            var footerRole = go.AddComponent<ThemeRole>();
            footerRole.role = ThemeRole.Role.FooterBar;

            Button homeBtn = null, shopBtn = null, settingsBtn = null;
            GameObject homeHi = null, shopHi = null, settingsHi = null;
            for (int i = 0; i < 3; i++)
            {
                var label = i == 0 ? "Home" : (i == 1 ? "Shop" : "Settings");
                var btnGo = NewButton(go.transform, label);
                var btnRole = btnGo.gameObject.AddComponent<ThemeRole>();
                btnRole.role = ThemeRole.Role.Button;
                var btnRect = btnGo.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(i / 3f, 0);
                btnRect.anchorMax = new Vector2((i + 1) / 3f, 1);
                btnRect.offsetMin = btnRect.offsetMax = Vector2.zero;
                var hi = new GameObject("Highlight");
                hi.transform.SetParent(btnGo.transform, false);
                var hiRect = hi.AddComponent<RectTransform>();
                hiRect.anchorMin = Vector2.zero;
                hiRect.anchorMax = Vector2.one;
                hiRect.offsetMin = new Vector2(4, 4);
                hiRect.offsetMax = new Vector2(-4, -4);
                var hiImg = hi.AddComponent<Image>();
                hiImg.color = new Color(1f, 0.95f, 0.7f, 0.35f);
                hi.SetActive(i == 0);
                if (i == 0) { homeBtn = btnGo; homeHi = hi; }
                else if (i == 1) { shopBtn = btnGo; shopHi = hi; }
                else { settingsBtn = btnGo; settingsHi = hi; }
            }

            var nav = go.AddComponent<BottomNavBar>();
            var navSo = new SerializedObject(nav);
            navSo.FindProperty("homeButton").objectReferenceValue = homeBtn;
            navSo.FindProperty("shopButton").objectReferenceValue = shopBtn;
            navSo.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            navSo.FindProperty("homeHighlight").objectReferenceValue = homeHi;
            navSo.FindProperty("shopHighlight").objectReferenceValue = shopHi;
            navSo.FindProperty("settingsHighlight").objectReferenceValue = settingsHi;
            navSo.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        private static GameObject CreateGameScreenRoot(Transform parent, Camera cam, CircuitOneStrokeTheme theme)
        {
            var go = new GameObject("GameScreenRoot");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            // 배경을 검은색으로 두면 퍼즐(카메라가 그리는 월드)을 가립니다. 투명 + 레이캐스트 끔으로 퍼즐이 보이고 터치도 전달.
            var bgImg = go.AddComponent<Image>();
            bgImg.color = Color.clear;
            bgImg.raycastTarget = false;

            FillGameScreenRoot.Fill(go.transform, cam, theme);
            return go;
        }

        private static void AddPanelImage(GameObject go, Color c)
        {
            var img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = true;
        }

        private static UnityEngine.UI.Text NewText(Transform parent, string text, int fontSize)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return t;
        }

        private static Button NewButton(Transform parent, string label)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.28f, 0.38f);
            var btn = go.AddComponent<Button>();
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var t = textGo.AddComponent<UnityEngine.UI.Text>();
            t.text = label;
            t.fontSize = 28;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return btn;
        }

        private static void SetAppSceneFirstInBuildSettings()
        {
            var appScene = new EditorBuildSettingsScene(ScenePath, true);
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath);
            list.Insert(0, appScene);
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void EnsureGameplayPrefabs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NodeView.prefab") == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EdgeView.prefab") == null)
            {
                GameplayPrefabFactory.EnsureNodeAndEdgePrefabs();
            }
        }
    }
}
#endif
