#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using CircuitOneStroke.UI;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// Adds MainShell + GameRoot + OverlayRoot hierarchy and AppRouter to existing UIRoot.
    /// Run after Create Game Scene. Then assign prefabs (Home/Shop/Settings tab content, GameHUD, etc.) in Inspector.
    /// 디자인: CircuitOneStrokeTheme 사용. Kenney Sci-Fi(패널/버튼) → "Circuit One-Stroke/UI/Assign Kenney Sci-Fi to Theme" 실행.
    /// Skymon 아이콘(탭 아이콘) → "Circuit One-Stroke/UI/Assign Skymon Icons to Theme" 실행 후 Create MainShell 실행.
    /// </summary>
    public static class CreateMainShellHierarchy
    {
        [MenuItem("Circuit One-Stroke/Create MainShell + GameRoot + OverlayRoot")]
        public static void CreateHierarchy()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("No Canvas in scene. Run 'Create Game Scene' first.");
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeAreaPanel");
            if (safeArea == null)
                safeArea = canvas.transform.childCount > 0 ? canvas.transform.GetChild(0) : null;
            if (safeArea == null)
            {
                var go = new GameObject("SafeAreaPanel");
                go.transform.SetParent(canvas.transform, false);
                go.AddComponent<RectTransform>().anchorMin = Vector2.zero;
                go.GetComponent<RectTransform>().anchorMax = Vector2.one;
                go.GetComponent<RectTransform>().offsetMin = go.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                safeArea = go.transform;
            }

            var theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>("Assets/UI/Theme/CircuitOneStrokeTheme.asset");

            // MainShellRoot
            var mainShellRoot = safeArea.Find("MainShellRoot");
            if (mainShellRoot == null)
            {
                mainShellRoot = CreateMainShellRoot(safeArea, theme);
            }

            // GameRoot (disabled)
            var gameRoot = safeArea.Find("GameRoot");
            if (gameRoot == null)
            {
                gameRoot = new GameObject("GameRoot").transform;
                gameRoot.SetParent(safeArea, false);
                var grRect = gameRoot.gameObject.AddComponent<RectTransform>();
                grRect.anchorMin = Vector2.zero;
                grRect.anchorMax = Vector2.one;
                grRect.offsetMin = grRect.offsetMax = Vector2.zero;
                gameRoot.gameObject.SetActive(false);
                var goHud = new GameObject("GameHUDRoot");
                goHud.transform.SetParent(gameRoot, false);
                goHud.AddComponent<RectTransform>().anchorMin = Vector2.zero;
                goHud.GetComponent<RectTransform>().anchorMax = Vector2.one;
                goHud.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                goHud.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                // TODO: Assign existing GameHUD prefab/instance as child of GameHUDRoot
            }

            // OverlayRoot
            var overlayRoot = safeArea.Find("OverlayRoot");
            if (overlayRoot == null)
            {
                overlayRoot = CreateOverlayRoot(safeArea, theme);
            }

            // AppRouter
            Transform appRouterT = canvas.transform.Find("AppRouter");
            GameObject appRouterGo = appRouterT != null ? appRouterT.gameObject : null;
            if (appRouterGo == null)
            {
                appRouterGo = new GameObject("AppRouter");
                appRouterGo.transform.SetParent(canvas.transform, false);
            }
            var appRouter = appRouterGo.GetComponent<AppRouter>();
            if (appRouter == null) appRouter = appRouterGo.AddComponent<AppRouter>();

            var overlayMgr = overlayRoot.GetComponentInChildren<OverlayManager>();
            var navBar = mainShellRoot.GetComponentInChildren<MainShellNavBar>();

            var so = new SerializedObject(appRouter);
            so.FindProperty("mainShellRoot").objectReferenceValue = mainShellRoot.gameObject;
            so.FindProperty("gameRoot").objectReferenceValue = gameRoot.gameObject;
            so.FindProperty("mainShellContentRoot").objectReferenceValue = mainShellRoot.Find("MainShellContentRoot");
            so.FindProperty("bottomNavBar").objectReferenceValue = navBar;
            so.FindProperty("overlayManager").objectReferenceValue = overlayMgr;
            so.FindProperty("levelLoader").objectReferenceValue = Object.FindFirstObjectByType<CircuitOneStroke.Core.LevelLoader>();
            so.FindProperty("levelManifest").objectReferenceValue = Resources.Load<CircuitOneStroke.Data.LevelManifest>("Levels/GeneratedLevelManifest");
            so.FindProperty("gameFlowController").objectReferenceValue = Object.FindFirstObjectByType<CircuitOneStroke.Core.GameFlowController>();
            so.FindProperty("adServiceComponent").objectReferenceValue = Object.FindFirstObjectByType<CircuitOneStroke.Services.AdServiceMock>();
            // TODO: Assign homeTabView, shopTabView, settingsTabView (instantiate from Screen prefabs or placeholders)
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(appRouterGo);
            Debug.Log("MainShell + GameRoot + OverlayRoot created. Assign Home/Shop/Settings tab content and GameHUD in AppRouter Inspector.");
        }

        private static Transform CreateMainShellRoot(Transform parent, CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("MainShellRoot");
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var contentRoot = new GameObject("MainShellContentRoot");
            contentRoot.transform.SetParent(root.transform, false);
            var cr = contentRoot.AddComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 0.12f);
            cr.anchorMax = Vector2.one;
            cr.offsetMin = cr.offsetMax = Vector2.zero;

            var bottomNav = new GameObject("BottomNavBar");
            bottomNav.transform.SetParent(root.transform, false);
            var bnRect = bottomNav.AddComponent<RectTransform>();
            bnRect.anchorMin = new Vector2(0, 0);
            bnRect.anchorMax = new Vector2(1, 0.12f);
            bnRect.offsetMin = bnRect.offsetMax = Vector2.zero;
            var bnImg = bottomNav.AddComponent<Image>();
            bnImg.color = theme != null ? theme.primary : UIStyleConstants.Primary;
            var bnRole = bottomNav.AddComponent<ThemeRole>();
            bnRole.role = ThemeRole.Role.FooterBar;
            var navBar = bottomNav.AddComponent<MainShellNavBar>();
            Button homeBtn = null, shopBtn = null, settingsBtn = null;
            Sprite[] tabIcons = null;
            if (theme != null)
            {
                tabIcons = new[] { theme.iconHome ?? theme.iconLevel, theme.iconShop, theme.iconSettings };
            }
            for (int i = 0; i < 3; i++)
            {
                var label = i == 0 ? "Home" : (i == 1 ? "Shop" : "Settings");
                var btn = CreateThemedTabButton(label, theme, tabIcons != null && i < tabIcons.Length ? tabIcons[i] : null, useAccentText: true);
                btn.name = label + "Tab";
                btn.transform.SetParent(bottomNav.transform, false);
                var r = btn.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(i / 3f, 0);
                r.anchorMax = new Vector2((i + 1) / 3f, 1);
                r.offsetMin = r.offsetMax = Vector2.zero;
                var b = btn.GetComponent<Button>();
                if (i == 0) homeBtn = b; else if (i == 1) shopBtn = b; else settingsBtn = b;
            }
            var mainShellApplier = root.AddComponent<ThemeApplier>();
            var applierSo = new SerializedObject(mainShellApplier);
            applierSo.FindProperty("theme").objectReferenceValue = theme;
            applierSo.ApplyModifiedPropertiesWithoutUndo();
            if (theme != null) mainShellApplier.Apply(theme);
            var navSo = new SerializedObject(navBar);
            navSo.FindProperty("homeButton").objectReferenceValue = homeBtn;
            navSo.FindProperty("shopButton").objectReferenceValue = shopBtn;
            navSo.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            navSo.ApplyModifiedPropertiesWithoutUndo();

            return root.transform;
        }

        /// <summary>OverlayRoot + OverlayManager 생성. CreateAppScene 등에서 재사용.</summary>
        public static Transform CreateOverlayRoot(Transform parent, CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("OverlayRoot");
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var overlayMgrGo = new GameObject("OverlayManager");
            overlayMgrGo.transform.SetParent(root.transform, false);
            overlayMgrGo.AddComponent<RectTransform>().anchorMin = Vector2.zero;
            overlayMgrGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            overlayMgrGo.GetComponent<RectTransform>().offsetMin = overlayMgrGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var overlayMgr = overlayMgrGo.AddComponent<OverlayManager>();

            var resultRoot = new GameObject("ResultDialog");
            resultRoot.transform.SetParent(overlayMgrGo.transform, false);
            resultRoot.AddComponent<RectTransform>().anchorMin = Vector2.zero;
            resultRoot.GetComponent<RectTransform>().anchorMax = Vector2.one;
            resultRoot.GetComponent<RectTransform>().offsetMin = resultRoot.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var resultBg = resultRoot.AddComponent<Image>();
            resultBg.color = new Color(0, 0, 0, 0.6f);
            resultRoot.SetActive(false);
            var winContent = new GameObject("WinContent");
            winContent.transform.SetParent(resultRoot.transform, false);
            var wcRect = winContent.AddComponent<RectTransform>();
            wcRect.anchorMin = wcRect.anchorMax = new Vector2(0.5f, 0.5f);
            wcRect.sizeDelta = new Vector2(400, 300);
            var winPanelImg = winContent.AddComponent<Image>();
            winPanelImg.color = theme != null ? theme.panelBase : UIStyleConstants.PanelBase;
            var winPanelRole = winContent.AddComponent<ThemeRole>();
            winPanelRole.role = ThemeRole.Role.Panel;
            var nextBtn = CreateThemedButton("Next", theme, useAccentText: false);
            nextBtn.transform.SetParent(winContent.transform, false);
            var levelSelectBtn = CreateThemedButton("Level Select", theme, useAccentText: false);
            levelSelectBtn.transform.SetParent(winContent.transform, false);
            var loseContent = new GameObject("LoseContent");
            loseContent.transform.SetParent(resultRoot.transform, false);
            var lcRect = loseContent.AddComponent<RectTransform>();
            lcRect.anchorMin = lcRect.anchorMax = new Vector2(0.5f, 0.5f);
            lcRect.sizeDelta = new Vector2(400, 300);
            var losePanelImg = loseContent.AddComponent<Image>();
            losePanelImg.color = theme != null ? theme.panelBase : UIStyleConstants.PanelBase;
            var losePanelRole = loseContent.AddComponent<ThemeRole>();
            losePanelRole.role = ThemeRole.Role.Panel;
            var loseMsg = new GameObject("Message").AddComponent<Text>();
            loseMsg.transform.SetParent(loseContent.transform, false);
            loseMsg.text = "Try again?";
            if (theme != null && theme.font != null) loseMsg.font = theme.font;
            loseMsg.fontSize = 28;
            loseMsg.alignment = TextAnchor.MiddleCenter;
            var loseMsgRole = loseMsg.gameObject.AddComponent<ThemeTextRole>();
            loseMsgRole.useAccentColor = false;
            var loseRetry = CreateThemedButton("Retry", theme, useAccentText: false);
            loseRetry.transform.SetParent(loseContent.transform, false);
            var loseLevelSelect = CreateThemedButton("Level Select", theme, useAccentText: false);
            loseLevelSelect.transform.SetParent(loseContent.transform, false);
            var loseWatchAd = CreateButton("Watch Ad", theme != null ? theme.warning : UIStyleConstants.Warning);
            loseWatchAd.transform.SetParent(loseContent.transform, false);
            loseContent.SetActive(false);

            var oohPanel = new GameObject("OutOfHeartsPanel");
            oohPanel.transform.SetParent(overlayMgrGo.transform, false);
            var oohRect = oohPanel.AddComponent<RectTransform>();
            oohRect.anchorMin = Vector2.zero;
            oohRect.anchorMax = Vector2.one;
            oohRect.offsetMin = oohRect.offsetMax = Vector2.zero;
            var oohImg = oohPanel.AddComponent<Image>();
            oohImg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            var oohWatch = CreateThemedButton("Watch Ad to Refill", theme, useAccentText: false);
            oohWatch.transform.SetParent(oohPanel.transform, false);
            var oohBack = CreateThemedButton("Back", theme, useAccentText: false);
            oohBack.transform.SetParent(oohPanel.transform, false);
            oohPanel.SetActive(false);

            var confirmExit = new GameObject("ConfirmExitDialog");
            confirmExit.transform.SetParent(overlayMgrGo.transform, false);
            confirmExit.AddComponent<RectTransform>().anchorMin = Vector2.zero;
            confirmExit.GetComponent<RectTransform>().anchorMax = Vector2.one;
            confirmExit.GetComponent<RectTransform>().offsetMin = confirmExit.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var ceImg = confirmExit.AddComponent<Image>();
            ceImg.color = new Color(0, 0, 0, 0.6f);
            var confirmBtn = CreateButton("Exit", theme != null ? theme.danger : UIStyleConstants.Danger);
            confirmBtn.transform.SetParent(confirmExit.transform, false);
            var cancelBtn = CreateThemedButton("Cancel", theme, useAccentText: false);
            cancelBtn.transform.SetParent(confirmExit.transform, false);
            confirmExit.SetActive(false);

            var overlayApplier = overlayMgrGo.AddComponent<ThemeApplier>();
            var oaSo = new SerializedObject(overlayApplier);
            oaSo.FindProperty("theme").objectReferenceValue = theme;
            oaSo.ApplyModifiedPropertiesWithoutUndo();
            if (theme != null) overlayApplier.Apply(theme);

            var omSo = new SerializedObject(overlayMgr);
            omSo.FindProperty("resultDialogRoot").objectReferenceValue = resultRoot;
            omSo.FindProperty("resultWinContent").objectReferenceValue = winContent;
            omSo.FindProperty("resultLoseContent").objectReferenceValue = loseContent;
            omSo.FindProperty("resultWinNextButton").objectReferenceValue = nextBtn.GetComponent<Button>();
            omSo.FindProperty("resultWinLevelSelectButton").objectReferenceValue = levelSelectBtn.GetComponent<Button>();
            omSo.FindProperty("resultLoseMessageText").objectReferenceValue = loseMsg;
            omSo.FindProperty("resultLoseRetryButton").objectReferenceValue = loseRetry.GetComponent<Button>();
            omSo.FindProperty("resultLoseLevelSelectButton").objectReferenceValue = loseLevelSelect.GetComponent<Button>();
            omSo.FindProperty("resultLoseWatchAdButton").objectReferenceValue = loseWatchAd.GetComponent<Button>();
            omSo.FindProperty("outOfHeartsPanel").objectReferenceValue = oohPanel;
            omSo.FindProperty("outOfHeartsWatchAdButton").objectReferenceValue = oohWatch.GetComponent<Button>();
            omSo.FindProperty("outOfHeartsBackButton").objectReferenceValue = oohBack.GetComponent<Button>();
            omSo.FindProperty("confirmExitDialog").objectReferenceValue = confirmExit;
            omSo.FindProperty("confirmExitConfirmButton").objectReferenceValue = confirmBtn.GetComponent<Button>();
            omSo.FindProperty("confirmExitCancelButton").objectReferenceValue = cancelBtn.GetComponent<Button>();
            omSo.ApplyModifiedPropertiesWithoutUndo();

            return root.transform;
        }

        private static GameObject CreateButton(string label, Color color)
        {
            var go = new GameObject(label + "Button");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140, 48);
            go.AddComponent<Image>().color = color;
            go.AddComponent<Button>();
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return go;
        }

        /// <summary>ThemeRole(Button) + ThemeTextRole(optional accent). ThemeApplier로 테마 색/폰트 적용.</summary>
        private static GameObject CreateThemedButton(string label, CircuitOneStrokeTheme theme, bool useAccentText)
        {
            return CreateThemedTabButton(label, theme, null, useAccentText);
        }

        /// <summary>탭 버튼: 선택적 Skymon 아이콘(테마에서) + 라벨. 테마에 iconHome/iconShop/iconSettings 있으면 사용.</summary>
        private static GameObject CreateThemedTabButton(string label, CircuitOneStrokeTheme theme, Sprite iconSprite, bool useAccentText)
        {
            var go = new GameObject(label + "Button");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140, 48);
            var img = go.AddComponent<Image>();
            img.color = theme != null ? theme.primary : UIStyleConstants.Primary;
            var role = go.AddComponent<ThemeRole>();
            role.role = ThemeRole.Role.Button;
            go.AddComponent<Button>();

            if (iconSprite != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                var iconRect = iconGo.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.55f);
                iconRect.anchorMax = new Vector2(0.5f, 0.55f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(48, 48);
                iconRect.anchoredPosition = Vector2.zero;
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;
                var iconRole = iconGo.AddComponent<ThemeRole>();
                iconRole.role = ThemeRole.Role.Icon;
            }

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.1f);
            textRect.anchorMax = new Vector2(1, 0.45f);
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = (theme != null ? theme.textOnAccent : UIStyleConstants.TextOnAccent);
            if (theme != null && theme.font != null) text.font = theme.font;
            var textRole = textGo.AddComponent<ThemeTextRole>();
            textRole.useAccentColor = useAccentText;
            return go;
        }
    }
}
#endif
