#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;
using CircuitOneStroke.View;
using CircuitOneStroke.Input;
using CircuitOneStroke.Services;
using CircuitOneStroke.UI;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// GameScreenRoot 아래에 LevelLoader, 노드/엣지 루트, StrokeRenderer, GameHUD, GameFlowController 등 풀 게임플레이 계층을 채웁니다.
    /// </summary>
    public static class FillGameScreenRoot
    {
        private const string NodePrefabPath = "Assets/Prefabs/NodeView.prefab";
        private const string EdgePrefabPath = "Assets/Prefabs/EdgeView.prefab";
        private const string HeartBarPrefabPath = "Assets/UI/Prefabs/HeartBar.prefab";

        public static void Fill(Transform gameScreenRoot, Camera cam, CircuitOneStrokeTheme theme)
        {
            if (gameScreenRoot == null) return;
            while (gameScreenRoot.childCount > 0)
                Object.DestroyImmediate(gameScreenRoot.GetChild(0).gameObject);

            var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NodePrefabPath);
            var edgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EdgePrefabPath);
            if (nodePrefab == null || edgePrefab == null)
            {
                Debug.LogError("FillGameScreenRoot: NodeView or EdgeView prefab missing. Run 'Circuit One-Stroke > Create Game Scene' once to generate Assets/Prefabs/NodeView.prefab and EdgeView.prefab.");
                return;
            }

            var game = new GameObject("Game");
            game.transform.SetParent(gameScreenRoot, false);
            var loader = game.AddComponent<LevelLoader>();

            var nodesRoot = new GameObject("Nodes");
            nodesRoot.transform.SetParent(game.transform, false);
            var edgesRoot = new GameObject("Edges");
            edgesRoot.transform.SetParent(game.transform, false);

            var strokeGo = new GameObject("StrokeRenderer");
            strokeGo.transform.SetParent(game.transform, false);
            var lr = strokeGo.AddComponent<LineRenderer>();
            lr.positionCount = 0;
            var strokeRenderer = strokeGo.AddComponent<StrokeRenderer>();

            var feedbackGo = new GameObject("GameFeedback");
            feedbackGo.transform.SetParent(game.transform, false);
            var audioSource = feedbackGo.AddComponent<AudioSource>();
            feedbackGo.AddComponent<GameFeedback>();
            var feedbackSo = new SerializedObject(feedbackGo.GetComponent<GameFeedback>());
            feedbackSo.FindProperty("audioSource").objectReferenceValue = audioSource;
            feedbackSo.ApplyModifiedPropertiesWithoutUndo();

            var audioMgrGo = new GameObject("AudioManager");
            audioMgrGo.transform.SetParent(game.transform, false);
            var audioMgr = audioMgrGo.AddComponent<AudioManager>();
            var amSo = new SerializedObject(audioMgr);
            amSo.FindProperty("sfxSource").objectReferenceValue = audioSource;
            amSo.ApplyModifiedPropertiesWithoutUndo();

            var hapticsGo = new GameObject("HapticsManager");
            hapticsGo.transform.SetParent(game.transform, false);
            hapticsGo.AddComponent<HapticsManager>();

            var input = game.AddComponent<TouchInputController>();
            var loaderSo = new SerializedObject(loader);
            loaderSo.FindProperty("nodesRoot").objectReferenceValue = nodesRoot.transform;
            loaderSo.FindProperty("edgesRoot").objectReferenceValue = edgesRoot.transform;
            loaderSo.FindProperty("strokeRenderer").objectReferenceValue = strokeRenderer;
            loaderSo.FindProperty("nodeViewPrefab").objectReferenceValue = nodePrefab;
            loaderSo.FindProperty("edgeViewPrefab").objectReferenceValue = edgePrefab;
            loaderSo.ApplyModifiedPropertiesWithoutUndo();

            var inputSo = new SerializedObject(input);
            inputSo.FindProperty("levelLoader").objectReferenceValue = loader;
            inputSo.FindProperty("mainCamera").objectReferenceValue = cam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            var adMock = new GameObject("AdServiceMock");
            adMock.transform.SetParent(game.transform, false);
            var adMockComp = adMock.AddComponent<AdServiceMock>();

            var flowGo = new GameObject("GameFlowController");
            flowGo.transform.SetParent(game.transform, false);
            var flow = flowGo.AddComponent<GameFlowController>();
            var manifest = Resources.Load<LevelManifest>("Levels/GeneratedLevelManifest");
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("levelLoader").objectReferenceValue = loader;
            flowSo.FindProperty("levelManifest").objectReferenceValue = manifest;
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            var hudRoot = new GameObject("GameHUDRoot");
            hudRoot.transform.SetParent(gameScreenRoot, false);
            var hudRect = hudRoot.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = hudRect.offsetMax = Vector2.zero;

            var hud = new GameObject("GameHUD");
            hud.transform.SetParent(hudRoot.transform, false);
            var hudRect2 = hud.AddComponent<RectTransform>();
            hudRect2.anchorMin = Vector2.zero;
            hudRect2.anchorMax = Vector2.one;
            hudRect2.offsetMin = hudRect2.offsetMax = Vector2.zero;
            var gameHud = hud.AddComponent<GameHUD>();

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
            SetRect(levelLabel.GetComponent<RectTransform>(), 0, 0, 0.5f, 1);
            levelLabel.GetComponent<RectTransform>().offsetMin = new Vector2(24, 0);
            levelLabel.GetComponent<RectTransform>().offsetMax = new Vector2(-12, 0);
            levelLabel.text = "LEVEL 1";
            levelLabel.fontSize = 68;
            levelLabel.fontStyle = FontStyle.Bold;
            levelLabel.alignment = TextAnchor.MiddleLeft;
            levelLabel.color = Color.white;
            levelLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            levelLabel.gameObject.AddComponent<ThemeTextRole>();

            var settingsBtn = CreateBtn("II", Color.white);
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
            GameObject heartBarGo = null;
            var heartBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeartBarPrefabPath);
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
            heartsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetRect(heartsText.GetComponent<RectTransform>(), 0, 0, 1, 1);
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

            var undoBtn = CreateBtn("Undo", Color.white);
            undoBtn.name = "UndoButton";
            undoBtn.transform.SetParent(bottomBar.transform, false);
            SetRect(undoBtn.GetComponent<RectTransform>(), 0, 0, 1f/3f, 1);
            var undoText = undoBtn.GetComponentInChildren<Text>();
            if (undoText != null) { undoText.text = "Undo"; undoText.fontSize = 84; undoText.color = Color.white; undoText.gameObject.AddComponent<ThemeTextRole>(); }

            var backBtn = CreateBtn("Back", Color.white);
            backBtn.name = "BackButton";
            backBtn.transform.SetParent(bottomBar.transform, false);
            SetRect(backBtn.GetComponent<RectTransform>(), 1f/3f, 0, 2f/3f, 1);
            var backText = backBtn.GetComponentInChildren<Text>();
            if (backText != null) { backText.text = "Back"; backText.fontSize = 84; backText.color = Color.white; backText.gameObject.AddComponent<ThemeTextRole>(); }

            var hintBtn = CreateBtn("Hint", Color.white);
            hintBtn.name = "HintButton";
            hintBtn.transform.SetParent(bottomBar.transform, false);
            SetRect(hintBtn.GetComponent<RectTransform>(), 2f/3f, 0, 1, 1);
            var hintText = hintBtn.GetComponentInChildren<Text>();
            if (hintText != null) { hintText.text = "Hint"; hintText.fontSize = 84; hintText.color = Color.white; hintText.gameObject.AddComponent<ThemeTextRole>(); }

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
            spText.color = Color.white;
            spText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetRect(spText.GetComponent<RectTransform>(), 0, 0.52f, 1, 1);
            var nextLevelBtn = CreateBtn("Next Level", theme != null ? theme.primary : UIStyleConstants.Primary);
            nextLevelBtn.transform.SetParent(successPanel.transform, false);
            SetRect(nextLevelBtn.GetComponent<RectTransform>(), 0.2f, 0.08f, 0.8f, 0.45f);
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
            fpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetRect(fpText.GetComponent<RectTransform>(), 0, 0.6f, 1, 1);
            var retryBtn = CreateBtn("Retry", new Color(0.2f, 0.6f, 0.3f));
            retryBtn.transform.SetParent(failPanel.transform, false);
            SetRect(retryBtn.GetComponent<RectTransform>(), 0.1f, 0.1f, 0.45f, 0.5f);
            var retryText = retryBtn.GetComponentInChildren<Text>(); if (retryText != null) retryText.fontSize = 56;
            var homeBtn = CreateBtn("Home", new Color(0.4f, 0.4f, 0.4f));
            homeBtn.transform.SetParent(failPanel.transform, false);
            SetRect(homeBtn.GetComponent<RectTransform>(), 0.55f, 0.1f, 0.9f, 0.5f);
            var homeText = homeBtn.GetComponentInChildren<Text>(); if (homeText != null) homeText.fontSize = 56;
            var watchAdBtn = CreateBtn("Watch Ad to Refill", new Color(0.8f, 0.5f, 0.1f));
            watchAdBtn.transform.SetParent(failPanel.transform, false);
            SetRect(watchAdBtn.GetComponent<RectTransform>(), 0.1f, 0.1f, 0.45f, 0.5f);
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
            oohText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetRect(oohText.GetComponent<RectTransform>(), 0.2f, 0.6f, 0.8f, 0.9f);
            var oohWatchBtn = CreateBtn("Watch Ad to Refill", new Color(0.2f, 0.7f, 0.3f));
            oohWatchBtn.transform.SetParent(outOfHeartsPanel.transform, false);
            SetRect(oohWatchBtn.GetComponent<RectTransform>(), 0.25f, 0.3f, 0.75f, 0.45f);
            var oohWatchText = oohWatchBtn.GetComponentInChildren<Text>(); if (oohWatchText != null) oohWatchText.fontSize = 56;
            var oohBackBtn = CreateBtn("Back to Menu", new Color(0.4f, 0.4f, 0.4f));
            oohBackBtn.transform.SetParent(outOfHeartsPanel.transform, false);
            SetRect(oohBackBtn.GetComponent<RectTransform>(), 0.25f, 0.15f, 0.75f, 0.28f);
            var oohBackText = oohBackBtn.GetComponentInChildren<Text>(); if (oohBackText != null) oohBackText.fontSize = 56;
            outOfHeartsPanel.SetActive(false);

            var hudSo = new SerializedObject(gameHud);
            hudSo.FindProperty("levelLoader").objectReferenceValue = loader;
            if (manifest != null) hudSo.FindProperty("levelManifest").objectReferenceValue = manifest;
            hudSo.FindProperty("adServiceComponent").objectReferenceValue = adMockComp;
            hudSo.FindProperty("nextLevelButton").objectReferenceValue = nextLevelBtn.GetComponent<Button>();
            hudSo.FindProperty("heartsDisplay").objectReferenceValue = heartsDisplay;
            hudSo.FindProperty("heartsText").objectReferenceValue = heartsText;
            hudSo.FindProperty("heartBar").objectReferenceValue = heartBarGo != null ? heartBarGo.GetComponent<HeartBar>() : null;
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
            if (hudSo.FindProperty("undoButton") != null)
                hudSo.FindProperty("undoButton").objectReferenceValue = undoBtn.GetComponent<Button>();
            if (hudSo.FindProperty("hintButton") != null)
                hudSo.FindProperty("hintButton").objectReferenceValue = hintBtn.GetComponent<Button>();
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(gameScreenRoot.gameObject);
            Debug.Log("FillGameScreenRoot: LevelLoader, StrokeRenderer, GameHUD, GameFlowController, TouchInputController, etc. added under GameScreenRoot.");
        }

        [MenuItem("Circuit One-Stroke/Fill GameScreenRoot with Gameplay")]
        public static void FillCurrentSceneGameScreenRoot()
        {
            var root = Object.FindFirstObjectByType<Canvas>()?.transform?.Find("SafeAreaPanel/GameScreenRoot");
            if (root == null) root = Object.FindFirstObjectByType<Canvas>()?.transform?.Find("GameScreenRoot");
            if (root == null)
            {
                var any = GameObject.Find("GameScreenRoot");
                if (any != null) root = any.transform;
            }
            if (root == null)
            {
                Debug.LogWarning("GameScreenRoot not found. Open AppScene or a scene that has Canvas > GameScreenRoot.");
                return;
            }
            var cam = Object.FindFirstObjectByType<Camera>();
            var theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>("Assets/UI/Theme/CircuitOneStrokeTheme.asset");
            Fill(root, cam, theme);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        private static GameObject CreateBtn(string label, Color color)
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return go;
        }

        private static void SetRect(RectTransform rect, float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
