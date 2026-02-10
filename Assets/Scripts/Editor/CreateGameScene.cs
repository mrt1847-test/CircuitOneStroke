#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
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

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.transform.position = new Vector3(0, 0, -10);
                cam.backgroundColor = new Color(0.06f, 0.07f, 0.12f, 1f);
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
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.positionCount = 0;
            var strokeRenderer = strokeGo.AddComponent<CircuitOneStroke.View.StrokeRenderer>();

            var feedbackGo = new GameObject("GameFeedback");
            feedbackGo.transform.SetParent(gameRoot.transform, false);
            var audioSource = feedbackGo.AddComponent<AudioSource>();
            feedbackGo.AddComponent<CircuitOneStroke.Core.GameFeedback>();
            var feedbackSo = new SerializedObject(feedbackGo.GetComponent<CircuitOneStroke.Core.GameFeedback>());
            feedbackSo.FindProperty("audioSource").objectReferenceValue = audioSource;
            feedbackSo.ApplyModifiedPropertiesWithoutUndo();

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

            var nodePrefab = CreateNodePrefab();
            var edgePrefab = CreateEdgePrefab();
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

            var heartsDisplay = new GameObject("HeartsDisplay");
            heartsDisplay.transform.SetParent(hud.transform, false);
            var hdRect = heartsDisplay.AddComponent<RectTransform>();
            hdRect.anchorMin = new Vector2(0, 1);
            hdRect.anchorMax = new Vector2(0, 1);
            hdRect.pivot = new Vector2(0, 1);
            hdRect.anchoredPosition = new Vector2(20, -20);
            hdRect.sizeDelta = new Vector2(180, 36);
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
            heartsText.text = "5/5";
            heartsText.fontSize = 20;
            heartsText.alignment = TextAnchor.MiddleLeft;
            var htRect = heartsText.GetComponent<RectTransform>();
            htRect.anchorMin = Vector2.zero;
            htRect.anchorMax = Vector2.one;
            htRect.offsetMin = htRect.offsetMax = Vector2.zero;
            if (heartBarGo != null) heartsText.gameObject.SetActive(false);

            var settingsBtn = CreateButton("⚙", new Color(0.3f, 0.3f, 0.35f));
            settingsBtn.name = "SettingsButton";
            settingsBtn.transform.SetParent(hud.transform, false);
            var sbRect = settingsBtn.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1, 1);
            sbRect.anchorMax = new Vector2(1, 1);
            sbRect.pivot = new Vector2(1, 1);
            sbRect.anchoredPosition = new Vector2(-20, -20);
            sbRect.sizeDelta = new Vector2(48, 48);
            var sbText = settingsBtn.GetComponentInChildren<Text>();
            if (sbText != null) sbText.text = "⚙";

            var levelLabel = new GameObject("LevelLabel").AddComponent<Text>();
            levelLabel.transform.SetParent(hud.transform, false);
            var llRect = levelLabel.GetComponent<RectTransform>();
            llRect.anchorMin = new Vector2(0.5f, 1);
            llRect.anchorMax = new Vector2(0.5f, 1);
            llRect.pivot = new Vector2(0.5f, 1);
            llRect.anchoredPosition = new Vector2(0, -20);
            llRect.sizeDelta = new Vector2(200, 30);
            levelLabel.text = "Level 1";
            levelLabel.fontSize = 24;
            levelLabel.alignment = TextAnchor.MiddleCenter;

            var successPanel = new GameObject("SuccessPanel");
            successPanel.transform.SetParent(hud.transform, false);
            var spRect = successPanel.AddComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 0.5f);
            spRect.anchorMax = new Vector2(0.5f, 0.5f);
            spRect.sizeDelta = new Vector2(320, 120);
            spRect.anchoredPosition = Vector2.zero;
            var spText = new GameObject("Text").AddComponent<Text>();
            spText.transform.SetParent(successPanel.transform, false);
            spText.text = "Clear!";
            spText.fontSize = 36;
            spText.alignment = TextAnchor.MiddleCenter;
            var spTextRect = spText.GetComponent<RectTransform>();
            spTextRect.anchorMin = new Vector2(0, 0.5f);
            spTextRect.anchorMax = new Vector2(1, 1);
            spTextRect.offsetMin = spTextRect.offsetMax = Vector2.zero;
            var nextLevelBtn = CreateButton("Next Level", new Color(0.2f, 0.6f, 0.3f));
            nextLevelBtn.transform.SetParent(successPanel.transform, false);
            SetRect(nextLevelBtn, 0.2f, 0.1f, 0.8f, 0.45f);
            successPanel.SetActive(false);

            var failPanel = new GameObject("FailPanel");
            failPanel.transform.SetParent(hud.transform, false);
            var fpRect = failPanel.AddComponent<RectTransform>();
            fpRect.anchorMin = new Vector2(0.5f, 0.5f);
            fpRect.anchorMax = new Vector2(0.5f, 0.5f);
            fpRect.sizeDelta = new Vector2(320, 160);
            fpRect.anchoredPosition = new Vector2(0, -50);
            var fpText = new GameObject("Message").AddComponent<Text>();
            fpText.transform.SetParent(failPanel.transform, false);
            fpText.text = "Try again?";
            fpText.fontSize = 28;
            fpText.alignment = TextAnchor.MiddleCenter;
            var fpTextRect = fpText.GetComponent<RectTransform>();
            fpTextRect.anchorMin = new Vector2(0, 0.6f);
            fpTextRect.anchorMax = new Vector2(1, 1);
            fpTextRect.offsetMin = fpTextRect.offsetMax = Vector2.zero;
            fpTextRect.anchoredPosition = Vector2.zero;

            var retryBtn = CreateButton("Retry", new Color(0.2f, 0.6f, 0.3f));
            retryBtn.transform.SetParent(failPanel.transform, false);
            SetRect(retryBtn, 0.1f, 0.1f, 0.45f, 0.5f);
            var homeBtn = CreateButton("Home", new Color(0.4f, 0.4f, 0.4f));
            homeBtn.transform.SetParent(failPanel.transform, false);
            SetRect(homeBtn, 0.55f, 0.1f, 0.9f, 0.5f);
            var watchAdBtn = CreateButton("Watch Ad to Refill", new Color(0.8f, 0.5f, 0.1f));
            watchAdBtn.transform.SetParent(failPanel.transform, false);
            SetRect(watchAdBtn, 0.1f, 0.1f, 0.45f, 0.5f);
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
            oohText.fontSize = 32;
            oohText.alignment = TextAnchor.MiddleCenter;
            var oohTextRect = oohText.GetComponent<RectTransform>();
            oohTextRect.anchorMin = new Vector2(0.2f, 0.6f);
            oohTextRect.anchorMax = new Vector2(0.8f, 0.9f);
            oohTextRect.offsetMin = oohTextRect.offsetMax = Vector2.zero;
            var oohWatchBtn = CreateButton("Watch Ad to Refill", new Color(0.2f, 0.7f, 0.3f));
            oohWatchBtn.transform.SetParent(outOfHeartsPanel.transform, false);
            SetRect(oohWatchBtn, 0.25f, 0.3f, 0.75f, 0.45f);
            var oohBackBtn = CreateButton("Back to Menu", new Color(0.4f, 0.4f, 0.4f));
            oohBackBtn.transform.SetParent(outOfHeartsPanel.transform, false);
            SetRect(oohBackBtn, 0.25f, 0.15f, 0.75f, 0.28f);
            outOfHeartsPanel.SetActive(false);

            var toastGo = new GameObject("ToastUI");
            toastGo.transform.SetParent(screenContainer, false);
            var toastRect = toastGo.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.2f);
            toastRect.anchorMax = new Vector2(0.5f, 0.2f);
            toastRect.sizeDelta = new Vector2(250, 50);
            toastRect.anchoredPosition = Vector2.zero;
            var toastUI = toastGo.AddComponent<CircuitOneStroke.UI.ToastUI>();
            var toastText = new GameObject("Text").AddComponent<Text>();
            toastText.transform.SetParent(toastGo.transform, false);
            toastText.text = "";
            toastText.fontSize = 20;
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
                routerSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("router").objectReferenceValue = router;
            flowSo.FindProperty("levelLoader").objectReferenceValue = loader;
            flowSo.FindProperty("levelManifest").objectReferenceValue = manifest;
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
            AssetDatabase.Refresh();
            Debug.Log("Created Assets/Scenes/GameScene.unity - app starts on HomeScreen. Run level from Continue/LevelSelect.");
        }

        private static GameObject CreateUIRoot(CircuitOneStrokeTheme theme)
        {
            var canvas = new GameObject("UIRoot");
            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<GraphicRaycaster>();
            var scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

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
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "NodeView";
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            DestroyImmediate(go.GetComponent<MeshRenderer>());
            DestroyImmediate(go.GetComponent<MeshFilter>());
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = Color.gray;
            var col = go.AddComponent<CircleCollider2D>();
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
            lr.startWidth = lr.endWidth = 0.1f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            go.AddComponent<CircuitOneStroke.View.EdgeView>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/EdgeView.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateButton(string label, Color color)
        {
            var go = new GameObject(label + "Button");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 40);
            var img = go.AddComponent<Image>();
            img.color = color;
            go.AddComponent<Button>();
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.fontSize = 20;
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
