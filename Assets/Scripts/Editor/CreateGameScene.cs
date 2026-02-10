#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// 메뉴로 GameScene 한 번에 구성. UIRoot(ScreenRoot+Router), Game(LevelLoader), Canvas·HUD 등.
    /// 앱은 HomeScreen에서 시작.
    /// </summary>
    public static class CreateGameScene
    {
        /// <summary>Level_1 없으면 생성 후, 씬·UIRoot·Game·HUD 구성. Create Screen Prefabs 먼저 실행 권장.</summary>
        [MenuItem("Circuit One-Stroke/Create Game Scene")]
        public static void CreateScene()
        {
            if (Resources.Load<CircuitOneStroke.Data.LevelData>("Levels/Level_1") == null)
                CreateDefaultLevel.CreateLevel1();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            CreateScreenPrefabs.CreateAll();

            // Create prefabs before NewScene so NodeView/EdgeView OnEnable don't run in an empty scene context.
            var nodePrefab = CreateNodePrefab();
            var edgePrefab = CreateEdgePrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.orthographic = true;
                cam.orthographicSize = 6.5f;
                cam.transform.position = new Vector3(0, 0, -10);
                cam.backgroundColor = Color.white;
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            var gameRoot = new GameObject("Game");
            var loader = gameRoot.AddComponent<CircuitOneStroke.Core.LevelLoader>();

            var nodesRoot = new GameObject("Nodes");
            nodesRoot.transform.SetParent(gameRoot.transform, false);
            var edgesRoot = new GameObject("Edges");
            edgesRoot.transform.SetParent(gameRoot.transform, false);

            var strokeGo = new GameObject("StrokeRenderer");
            strokeGo.transform.SetParent(gameRoot.transform, false);
            var lr = strokeGo.AddComponent<LineRenderer>();
            lr.positionCount = 0;
            var strokeRenderer = strokeGo.AddComponent<CircuitOneStroke.View.StrokeRenderer>();

            var feedbackGo = new GameObject("GameFeedback");
            feedbackGo.transform.SetParent(gameRoot.transform, false);
            var audioSource = feedbackGo.AddComponent<AudioSource>();
            feedbackGo.AddComponent<CircuitOneStroke.Core.GameFeedback>();
            var feedbackSo = new SerializedObject(feedbackGo.GetComponent<CircuitOneStroke.Core.GameFeedback>());
            feedbackSo.FindProperty("audioSource").objectReferenceValue = audioSource;
            feedbackSo.ApplyModifiedPropertiesWithoutUndo();

            var audioMgrGo = new GameObject("AudioManager");
            audioMgrGo.transform.SetParent(gameRoot.transform, false);
            var audioMgr = audioMgrGo.AddComponent<CircuitOneStroke.Core.AudioManager>();
            var amSo = new SerializedObject(audioMgr);
            amSo.FindProperty("sfxSource").objectReferenceValue = audioSource;
            amSo.ApplyModifiedPropertiesWithoutUndo();

            var hapticsGo = new GameObject("HapticsManager");
            hapticsGo.transform.SetParent(gameRoot.transform, false);
            hapticsGo.AddComponent<CircuitOneStroke.Core.HapticsManager>();

            var input = gameRoot.AddComponent<CircuitOneStroke.Input.TouchInputController>();
            var so = new SerializedObject(loader);
            so.FindProperty("nodesRoot").objectReferenceValue = nodesRoot.transform;
            so.FindProperty("edgesRoot").objectReferenceValue = edgesRoot.transform;
            so.FindProperty("strokeRenderer").objectReferenceValue = strokeRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            var inputSo = new SerializedObject(input);
            inputSo.FindProperty("levelLoader").objectReferenceValue = loader;
            inputSo.FindProperty("mainCamera").objectReferenceValue = cam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            so.FindProperty("nodeViewPrefab").objectReferenceValue = nodePrefab;
            so.FindProperty("edgeViewPrefab").objectReferenceValue = edgePrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            var adMock = new GameObject("AdServiceMock");
            adMock.transform.SetParent(gameRoot.transform, false);
            adMock.AddComponent<CircuitOneStroke.Services.AdServiceMock>();

            var flowGo = new GameObject("GameFlowController");
            flowGo.transform.SetParent(gameRoot.transform, false);
            var flow = flowGo.AddComponent<CircuitOneStroke.Core.GameFlowController>();

            var theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>("Assets/UI/Theme/CircuitOneStrokeTheme.asset");
            var canvas = CreateUIRoot(theme);
            var screenContainer = canvas.transform.Find("SafeAreaPanel/ScreenRoot/ScreenContainer");
            if (screenContainer == null) screenContainer = canvas.transform;

            var hud = new GameObject("GameHUD");
            hud.transform.SetParent(screenContainer, false);
            hud.SetActive(false);
            var hudRect = hud.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = hudRect.offsetMax = Vector2.zero;
            var gameHud = hud.AddComponent<CircuitOneStroke.UI.GameHUD>();

            const float topBarHeight = 200f;
            const float bottomBarHeight = 180f;

            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(hud.transform, false);
            var topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.anchoredPosition = Vector2.zero;
            topBarRect.sizeDelta = new Vector2(0, topBarHeight);
            var topBarImg = topBar.AddComponent<Image>();
            topBarImg.color = theme != null ? theme.primary : UIStyleConstants.Primary;
            topBarImg.raycastTarget = true;
            var topBarRole = topBar.AddComponent<ThemeRole>();
            topBarRole.role = ThemeRole.Role.HeaderBar;

            var levelLabel = new GameObject("LevelLabel").AddComponent<Text>();
            levelLabel.transform.SetParent(topBar.transform, false);
            var llRect = levelLabel.GetComponent<RectTransform>();
            llRect.anchorMin = new Vector2(0, 0);
            llRect.anchorMax = new Vector2(0.5f, 1);
            llRect.offsetMin = new Vector2(24, 0);
            llRect.offsetMax = new Vector2(-12, 0);
            levelLabel.text = "LEVEL 1";
            levelLabel.fontSize = 68;
            levelLabel.fontStyle = FontStyle.Bold;
            levelLabel.alignment = TextAnchor.MiddleLeft;
            levelLabel.color = Color.white;
            levelLabel.gameObject.AddComponent<ThemeTextRole>();

            var settingsBtn = CreateButton("II", Color.white);
            settingsBtn.name = "SettingsButton";
            settingsBtn.transform.SetParent(topBar.transform, false);
            var sbRect = settingsBtn.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1, 0.5f);
            sbRect.anchorMax = new Vector2(1, 0.5f);
            sbRect.pivot = new Vector2(1, 0.5f);
            sbRect.anchoredPosition = new Vector2(-28, 0);
            sbRect.sizeDelta = new Vector2(96, 96);
            var sbText = settingsBtn.GetComponentInChildren<Text>();
            if (sbText != null) { sbText.text = "II"; sbText.fontSize = 64; sbText.color = Color.white; sbText.gameObject.AddComponent<ThemeTextRole>(); }

            var heartsDisplay = new GameObject("HeartsDisplay");
            heartsDisplay.transform.SetParent(topBar.transform, false);
            var hdRect = heartsDisplay.AddComponent<RectTransform>();
            hdRect.anchorMin = new Vector2(0.5f, 0);
            hdRect.anchorMax = new Vector2(0.5f, 1);
            hdRect.pivot = new Vector2(0.5f, 0.5f);
            hdRect.anchoredPosition = Vector2.zero;
            hdRect.sizeDelta = new Vector2(260, 70);
            var heartBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/HeartBar.prefab");
            GameObject heartBarGo = null;
            if (heartBarPrefab != null)
            {
                heartBarGo = (GameObject)PrefabUtility.InstantiatePrefab(heartBarPrefab);
                heartBarGo.transform.SetParent(heartsDisplay.transform, false);
                var hbRect = heartBarGo.GetComponent<RectTransform>();
                if (hbRect != null) { hbRect.anchorMin = Vector2.zero; hbRect.anchorMax = Vector2.one; hbRect.offsetMin = hbRect.offsetMax = Vector2.zero; }
            }
            var heartsText = new GameObject("HeartsText").AddComponent<Text>();
            heartsText.transform.SetParent(heartsDisplay.transform, false);
            heartsText.text = "\u2665 5/5";
            heartsText.fontSize = 64;
            heartsText.alignment = TextAnchor.MiddleCenter;
            heartsText.color = Color.white;
            var htRect = heartsText.GetComponent<RectTransform>();
            htRect.anchorMin = Vector2.zero;
            htRect.anchorMax = Vector2.one;
            htRect.offsetMin = htRect.offsetMax = Vector2.zero;
            if (heartBarGo != null) heartsText.gameObject.SetActive(false);

            var bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(hud.transform, false);
            var botBarRect = bottomBar.AddComponent<RectTransform>();
            botBarRect.anchorMin = new Vector2(0, 0);
            botBarRect.anchorMax = new Vector2(1, 0);
            botBarRect.pivot = new Vector2(0.5f, 0);
            botBarRect.anchoredPosition = Vector2.zero;
            botBarRect.sizeDelta = new Vector2(0, bottomBarHeight);
            var botBarImg = bottomBar.AddComponent<Image>();
            botBarImg.color = theme != null ? theme.primary : UIStyleConstants.Primary;
            botBarImg.raycastTarget = true;
            var botBarRole = bottomBar.AddComponent<ThemeRole>();
            botBarRole.role = ThemeRole.Role.FooterBar;

            var undoBtn = CreateButton("↶", Color.white);
            undoBtn.name = "UndoButton";
            undoBtn.transform.SetParent(bottomBar.transform, false);
            SetRect(undoBtn, 0, 0, 1f/3f, 1);
            var undoText = undoBtn.GetComponentInChildren<Text>();
            if (undoText != null) { undoText.text = "Undo"; undoText.fontSize = 84; undoText.color = Color.white; undoText.gameObject.AddComponent<ThemeTextRole>(); }
            var backBtn = CreateButton("Back", Color.white);
            backBtn.name = "BackButton";
            backBtn.transform.SetParent(bottomBar.transform, false);
            SetRect(backBtn, 1f/3f, 0, 2f/3f, 1);
            var backText = backBtn.GetComponentInChildren<Text>();
            if (backText != null) { backText.text = "Back"; backText.fontSize = 84; backText.color = Color.white; backText.gameObject.AddComponent<ThemeTextRole>(); }
            var hintBtn = CreateButton("Hint", Color.white);
            hintBtn.name = "HintButton";
            hintBtn.transform.SetParent(bottomBar.transform, false);
            SetRect(hintBtn, 2f/3f, 0, 1, 1);
            var hintText = hintBtn.GetComponentInChildren<Text>();
            if (hintText != null) { hintText.text = "Hint"; hintText.fontSize = 84; hintText.color = Color.white; hintText.gameObject.AddComponent<ThemeTextRole>(); }
            for (int i = 1; i <= 2; i++)
            {
                var sep = new GameObject("Sep");
                sep.transform.SetParent(bottomBar.transform, false);
                var sepRect = sep.AddComponent<RectTransform>();
                sepRect.anchorMin = new Vector2(i / 3f, 0);
                sepRect.anchorMax = new Vector2(i / 3f, 1);
                sepRect.offsetMin = new Vector2(-1, 8);
                sepRect.offsetMax = new Vector2(1, -8);
                var sepImg = sep.AddComponent<Image>();
                sepImg.color = new Color(1f, 1f, 1f, 0.5f);
            }

            var successPanel = new GameObject("SuccessPanel");
            successPanel.transform.SetParent(hud.transform, false);
            var spRect = successPanel.AddComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 0.5f);
            spRect.anchorMax = new Vector2(0.5f, 0.5f);
            spRect.sizeDelta = new Vector2(870, 390);
            spRect.anchoredPosition = Vector2.zero;
            var spBg = successPanel.AddComponent<Image>();
            spBg.color = theme != null ? new Color(theme.panelBase.r, theme.panelBase.g, theme.panelBase.b, 0.96f) : new Color(0.96f, 0.96f, 0.98f, 0.96f);
            spBg.raycastTarget = true;
            var spText = new GameObject("Text").AddComponent<Text>();
            spText.transform.SetParent(successPanel.transform, false);
            spText.text = "Clear!";
            spText.fontSize = 128;
            spText.alignment = TextAnchor.MiddleCenter;
            spText.color = new Color(1f, 1f, 1f, 1f);
            var spTextRect = spText.GetComponent<RectTransform>();
            spTextRect.anchorMin = new Vector2(0, 0.52f);
            spTextRect.anchorMax = new Vector2(1, 1);
            spTextRect.offsetMin = spTextRect.offsetMax = Vector2.zero;
            var nextLevelBtn = CreateButton("Next Level", theme != null ? theme.primary : UIStyleConstants.Primary);
            nextLevelBtn.transform.SetParent(successPanel.transform, false);
            SetRect(nextLevelBtn, 0.2f, 0.08f, 0.8f, 0.45f);
            var nextLevelText = nextLevelBtn.GetComponentInChildren<Text>();
            if (nextLevelText != null) nextLevelText.fontSize = 64;
            successPanel.SetActive(false);

            var failPanel = new GameObject("FailPanel");
            failPanel.transform.SetParent(hud.transform, false);
            var fpRect = failPanel.AddComponent<RectTransform>();
            fpRect.anchorMin = new Vector2(0.5f, 0.5f);
            fpRect.anchorMax = new Vector2(0.5f, 0.5f);
            fpRect.sizeDelta = new Vector2(870, 390);
            fpRect.anchoredPosition = new Vector2(0, -50);
            var fpText = new GameObject("Message").AddComponent<Text>();
            fpText.transform.SetParent(failPanel.transform, false);
            fpText.text = "Try again?";
            fpText.fontSize = 88;
            fpText.alignment = TextAnchor.MiddleCenter;
            var fpTextRect = fpText.GetComponent<RectTransform>();
            fpTextRect.anchorMin = new Vector2(0, 0.6f);
            fpTextRect.anchorMax = new Vector2(1, 1);
            fpTextRect.offsetMin = fpTextRect.offsetMax = Vector2.zero;
            fpTextRect.anchoredPosition = Vector2.zero;

            var retryBtn = CreateButton("Retry", new Color(0.2f, 0.6f, 0.3f));
            retryBtn.transform.SetParent(failPanel.transform, false);
            SetRect(retryBtn, 0.1f, 0.1f, 0.45f, 0.5f);
            var retryText = retryBtn.GetComponentInChildren<Text>(); if (retryText != null) retryText.fontSize = 56;
            var homeBtn = CreateButton("Home", new Color(0.4f, 0.4f, 0.4f));
            homeBtn.transform.SetParent(failPanel.transform, false);
            SetRect(homeBtn, 0.55f, 0.1f, 0.9f, 0.5f);
            var homeText = homeBtn.GetComponentInChildren<Text>(); if (homeText != null) homeText.fontSize = 56;
            var watchAdBtn = CreateButton("Watch Ad to Refill", new Color(0.8f, 0.5f, 0.1f));
            watchAdBtn.transform.SetParent(failPanel.transform, false);
            SetRect(watchAdBtn, 0.1f, 0.1f, 0.45f, 0.5f);
            var watchAdText = watchAdBtn.GetComponentInChildren<Text>(); if (watchAdText != null) watchAdText.fontSize = 56;
            watchAdBtn.SetActive(false);
            failPanel.SetActive(false);

            var outOfHeartsPanel = new GameObject("OutOfHeartsPanel");
            outOfHeartsPanel.transform.SetParent(hud.transform, false);
            var oohRect = outOfHeartsPanel.AddComponent<RectTransform>();
            oohRect.anchorMin = Vector2.zero;
            oohRect.anchorMax = Vector2.one;
            oohRect.offsetMin = oohRect.offsetMax = Vector2.zero;
            var oohImage = outOfHeartsPanel.AddComponent<Image>();
            oohImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            var oohText = new GameObject("Message").AddComponent<Text>();
            oohText.transform.SetParent(outOfHeartsPanel.transform, false);
            oohText.text = "Out of hearts!\n0/5";
            oohText.fontSize = 96;
            oohText.alignment = TextAnchor.MiddleCenter;
            var oohTextRect = oohText.GetComponent<RectTransform>();
            oohTextRect.anchorMin = new Vector2(0.2f, 0.6f);
            oohTextRect.anchorMax = new Vector2(0.8f, 0.9f);
            oohTextRect.offsetMin = oohTextRect.offsetMax = Vector2.zero;
            var oohWatchBtn = CreateButton("Watch Ad to Refill", new Color(0.2f, 0.7f, 0.3f));
            oohWatchBtn.transform.SetParent(outOfHeartsPanel.transform, false);
            SetRect(oohWatchBtn, 0.25f, 0.3f, 0.75f, 0.45f);
            var oohWatchText = oohWatchBtn.GetComponentInChildren<Text>(); if (oohWatchText != null) oohWatchText.fontSize = 56;
            var oohBackBtn = CreateButton("Back to Menu", new Color(0.4f, 0.4f, 0.4f));
            oohBackBtn.transform.SetParent(outOfHeartsPanel.transform, false);
            SetRect(oohBackBtn, 0.25f, 0.15f, 0.75f, 0.28f);
            var oohBackText = oohBackBtn.GetComponentInChildren<Text>(); if (oohBackText != null) oohBackText.fontSize = 56;
            outOfHeartsPanel.SetActive(false);

            var toastGo = new GameObject("ToastUI");
            toastGo.transform.SetParent(screenContainer, false);
            var toastRect = toastGo.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.2f);
            toastRect.anchorMax = new Vector2(0.5f, 0.2f);
            toastRect.sizeDelta = new Vector2(630, 132);
            toastRect.anchoredPosition = Vector2.zero;
            var toastUI = toastGo.AddComponent<CircuitOneStroke.UI.ToastUI>();
            var toastText = new GameObject("Text").AddComponent<Text>();
            toastText.transform.SetParent(toastGo.transform, false);
            toastText.text = "";
            toastText.fontSize = 64;
            toastText.alignment = TextAnchor.MiddleCenter;
            var toastTextRect = toastText.GetComponent<RectTransform>();
            toastTextRect.anchorMin = Vector2.zero;
            toastTextRect.anchorMax = Vector2.one;
            toastTextRect.offsetMin = toastTextRect.offsetMax = Vector2.zero;
            toastText.gameObject.SetActive(false);
            var toastSo = new SerializedObject(toastUI);
            toastSo.FindProperty("toastText").objectReferenceValue = toastText;
            toastSo.ApplyModifiedPropertiesWithoutUndo();

            var hudSo = new SerializedObject(gameHud);
            hudSo.FindProperty("levelLoader").objectReferenceValue = loader;
            var manifest = Resources.Load<CircuitOneStroke.Data.LevelManifest>("Levels/GeneratedLevelManifest");
            if (manifest != null) hudSo.FindProperty("levelManifest").objectReferenceValue = manifest;
            hudSo.FindProperty("adServiceComponent").objectReferenceValue = adMock.GetComponent<CircuitOneStroke.Services.AdServiceMock>();
            hudSo.FindProperty("nextLevelButton").objectReferenceValue = nextLevelBtn.GetComponent<Button>();
            hudSo.FindProperty("heartsDisplay").objectReferenceValue = heartsDisplay;
            hudSo.FindProperty("heartsText").objectReferenceValue = heartsText;
            hudSo.FindProperty("heartBar").objectReferenceValue = heartBarGo != null ? heartBarGo.GetComponent<CircuitOneStroke.UI.HeartBar>() : null;
            hudSo.FindProperty("settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
            hudSo.FindProperty("successPanel").objectReferenceValue = successPanel;
            hudSo.FindProperty("failPanel").objectReferenceValue = failPanel;
            hudSo.FindProperty("failMessageText").objectReferenceValue = fpText;
            hudSo.FindProperty("retryButton").objectReferenceValue = retryBtn.GetComponent<Button>();
            hudSo.FindProperty("homeButton").objectReferenceValue = homeBtn.GetComponent<Button>();
            hudSo.FindProperty("watchAdButton").objectReferenceValue = watchAdBtn.GetComponent<Button>();
            hudSo.FindProperty("outOfHeartsPanel").objectReferenceValue = outOfHeartsPanel;
            hudSo.FindProperty("outOfHeartsWatchAdButton").objectReferenceValue = oohWatchBtn.GetComponent<Button>();
            hudSo.FindProperty("outOfHeartsBackButton").objectReferenceValue = oohBackBtn.GetComponent<Button>();
            hudSo.FindProperty("levelLabel").objectReferenceValue = levelLabel;
            if (hudSo.FindProperty("backButton") != null)
                hudSo.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            var router = canvas.GetComponentInChildren<CircuitOneStroke.UI.UIScreenRouter>();
            if (router != null)
            {
                var routerSo = new SerializedObject(router);
                routerSo.FindProperty("homeScreenPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Screens/HomeScreen.prefab");
                routerSo.FindProperty("levelSelectScreenPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Screens/LevelSelectScreen.prefab");
                routerSo.FindProperty("gameHUDScreenInstance").objectReferenceValue = hud;
                routerSo.FindProperty("settingsScreenPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Screens/SettingsScreen.prefab");
                routerSo.FindProperty("shopScreenPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Screens/ShopScreen.prefab");
                routerSo.FindProperty("outOfHeartsScreenPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Screens/OutOfHeartsScreen.prefab");
                routerSo.FindProperty("levelLoader").objectReferenceValue = loader;
                routerSo.FindProperty("levelManifest").objectReferenceValue = manifest;
                routerSo.FindProperty("theme").objectReferenceValue = theme;
                routerSo.FindProperty("gameHUDRef").objectReferenceValue = gameHud;
                routerSo.FindProperty("initialScreen").enumValueIndex = (int)CircuitOneStroke.UI.UIScreenRouter.Screen.LevelSelect;
                routerSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("router").objectReferenceValue = router;
            flowSo.FindProperty("levelLoader").objectReferenceValue = loader;
            flowSo.FindProperty("levelManifest").objectReferenceValue = manifest;
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
            AssetDatabase.Refresh();
            Debug.Log("Created Assets/Scenes/GameScene.unity - app starts on LevelSelect (puzzle-style). Run level from grid.");
        }

        private static GameObject CreateUIRoot(CircuitOneStrokeTheme theme)
        {
            var canvas = new GameObject("UIRoot");
            var canvasRect = canvas.AddComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1080, 1920);
            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<GraphicRaycaster>();
            var scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;

            var safeArea = new GameObject("SafeAreaPanel");
            safeArea.transform.SetParent(canvas.transform, false);
            var saRect = safeArea.AddComponent<RectTransform>();
            saRect.anchorMin = Vector2.zero;
            saRect.anchorMax = Vector2.one;
            saRect.offsetMin = saRect.offsetMax = Vector2.zero;
            safeArea.AddComponent<CircuitOneStroke.UI.SafeArea>();

            var screenRoot = new GameObject("ScreenRoot");
            screenRoot.transform.SetParent(safeArea.transform, false);
            var srRect = screenRoot.AddComponent<RectTransform>();
            srRect.anchorMin = Vector2.zero;
            srRect.anchorMax = Vector2.one;
            srRect.offsetMin = srRect.offsetMax = Vector2.zero;
            var srImg = screenRoot.AddComponent<Image>();
            srImg.color = theme != null ? theme.background : UIStyleConstants.Background;
            srImg.raycastTarget = true;
            var srComp = screenRoot.AddComponent<CircuitOneStroke.UI.ScreenRoot>();
            var srSo = new SerializedObject(srComp);
            srSo.FindProperty("theme").objectReferenceValue = theme;
            srSo.ApplyModifiedPropertiesWithoutUndo();

            var container = new GameObject("ScreenContainer");
            container.transform.SetParent(screenRoot.transform, false);
            var contRect = container.AddComponent<RectTransform>();
            contRect.anchorMin = Vector2.zero;
            contRect.anchorMax = Vector2.one;
            contRect.offsetMin = contRect.offsetMax = Vector2.zero;

            var routerGo = new GameObject("UIScreenRouter");
            routerGo.transform.SetParent(canvas.transform, false);
            var router = routerGo.AddComponent<CircuitOneStroke.UI.UIScreenRouter>();
            var rSo = new SerializedObject(router);
            rSo.FindProperty("screenContainer").objectReferenceValue = contRect;
            rSo.ApplyModifiedPropertiesWithoutUndo();

            return canvas;
        }

        private static GameObject CreateNodePrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            // Use empty GameObject + components instead of Quad to avoid MeshRenderer/MeshFilter teardown side effects in editor.
            var go = new GameObject("NodeView");
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            if (sr != null)
            {
                var circleSprite = CreateCircleSprite();
            if (circleSprite != null)
                sr.sprite = circleSprite;
            sr.color = new Color(0.72f, 0.76f, 0.88f, 1f);
            }

            var col = go.AddComponent<CircleCollider2D>();
            if (col != null)
                col.radius = 0.5f;

            go.AddComponent<CircuitOneStroke.View.NodeView>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/NodeView.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateEdgePrefab()
        {
            var go = new GameObject("EdgeView");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = 0.22f;
            go.AddComponent<CircuitOneStroke.View.EdgeView>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/EdgeView.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateButton(string label, Color color)
        {
            var go = new GameObject(label + "Button");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140, 48);
            var img = go.AddComponent<Image>();
            img.color = color;
            go.AddComponent<Button>();
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return go;
        }

        private static void SetRect(GameObject go, float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.45f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    tex.SetPixel(x, y, d <= radius ? Color.white : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
#endif
