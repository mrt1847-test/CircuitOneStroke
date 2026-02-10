#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using CircuitOneStroke.UI;
using CircuitOneStroke.UI.Theme;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// Screen prefabs 생성: HomeScreen, LevelSelectScreen, GameHUDScreen, SettingsScreen, ShopScreen, OutOfHeartsScreen.
    /// </summary>
    public static class CreateScreenPrefabs
    {
        private const string ScreensPath = "Assets/UI/Screens";
        private const string PrefabsPath = "Assets/UI/Prefabs";
        private const string ThemePath = "Assets/UI/Theme/CircuitOneStrokeTheme.asset";

        [MenuItem("Circuit One-Stroke/UI/Create Screen Prefabs")]
        public static void CreateAll()
        {
            CreateDefaultTheme.EnsureFolders();
            if (!AssetDatabase.IsValidFolder("Assets/UI/Screens"))
                AssetDatabase.CreateFolder("Assets/UI", "Screens");
            if (!AssetDatabase.IsValidFolder("Assets/UI/Prefabs"))
                AssetDatabase.CreateFolder("Assets/UI", "Prefabs");

            var theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>(ThemePath);
            if (theme == null)
            {
                CreateDefaultTheme.CreateTheme();
                theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>(ThemePath);
            }

            CreateHeartBarPrefab(theme);
            CreateLevelCellPrefab(theme);
            CreateHomeScreenPrefab(theme);
            CreateLevelSelectScreenPrefab(theme);
            CreateSettingsScreenPrefab(theme);
            CreateShopScreenPrefab(theme);
            CreateOutOfHeartsScreenPrefab(theme);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Screen prefabs created. Assign GameHUD prefab separately or use existing scene HUD.");
        }

        private static void CreateHeartBarPrefab(CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("HeartBar");
            root.AddComponent<RectTransform>();
            var heartBar = root.AddComponent<CircuitOneStroke.UI.HeartBar>();

            var hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;

            var icons = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var icon = new GameObject("Heart" + (i + 1)).AddComponent<Image>();
                icon.transform.SetParent(root.transform, false);
                icon.color = theme != null ? theme.primary : UIStyleConstants.Primary;
                icon.sprite = CreateCircleSprite();
                var rect = icon.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(32, 32);
                icons[i] = icon;
            }
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = "5/5";
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;

            var so = new SerializedObject(heartBar);
            so.FindProperty("heartIcons").arraySize = 5;
            for (int i = 0; i < 5; i++)
                so.FindProperty("heartIcons").GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
            so.FindProperty("heartsText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, PrefabsPath + "/HeartBar.prefab");
        }

        private static void CreateLevelCellPrefab(CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("LevelCell");
            var rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);

            var bg = root.AddComponent<Image>();
            bg.color = theme != null ? theme.panelBase : UIStyleConstants.PanelBase;

            var btn = root.AddComponent<Button>();

            var numGo = new GameObject("Number");
            numGo.transform.SetParent(root.transform, false);
            var numRect = numGo.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0.2f, 0.4f);
            numRect.anchorMax = new Vector2(0.8f, 0.8f);
            numRect.offsetMin = numRect.offsetMax = Vector2.zero;
            var numText = numGo.AddComponent<Text>();
            numText.text = "1";
            numText.fontSize = 28;
            numText.alignment = TextAnchor.MiddleCenter;

            var lockGo = new GameObject("LockOverlay");
            lockGo.transform.SetParent(root.transform, false);
            var lockRect = lockGo.AddComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.offsetMin = lockRect.offsetMax = Vector2.zero;
            var lockImg = lockGo.AddComponent<Image>();
            lockImg.color = new Color(0, 0, 0, 0.7f);
            lockImg.raycastTarget = false;
            lockGo.SetActive(false);

            var checkGo = new GameObject("ClearedCheckmark");
            checkGo.transform.SetParent(root.transform, false);
            var checkRect = checkGo.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.7f, 0.7f);
            checkRect.anchorMax = new Vector2(0.95f, 0.95f);
            checkRect.offsetMin = checkRect.offsetMax = Vector2.zero;
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = UIStyleConstants.Primary;
            checkImg.sprite = CreateCircleSprite();
            checkGo.SetActive(false);

            var timeGo = new GameObject("BestTime");
            timeGo.transform.SetParent(root.transform, false);
            var timeRect = timeGo.AddComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0.1f, 0.05f);
            timeRect.anchorMax = new Vector2(0.9f, 0.35f);
            timeRect.offsetMin = timeRect.offsetMax = Vector2.zero;
            var timeText = timeGo.AddComponent<Text>();
            timeText.text = "";
            timeText.fontSize = 12;
            timeText.alignment = TextAnchor.MiddleCenter;

            var cell = root.AddComponent<LevelSelectCell>();
            var cellSo = new SerializedObject(cell);
            cellSo.FindProperty("numberText").objectReferenceValue = numText;
            cellSo.FindProperty("lockOverlay").objectReferenceValue = lockGo;
            cellSo.FindProperty("clearedCheckmark").objectReferenceValue = checkGo;
            cellSo.FindProperty("bestTimeText").objectReferenceValue = timeText;
            cellSo.FindProperty("button").objectReferenceValue = btn;
            cellSo.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, PrefabsPath + "/LevelCell.prefab");
        }

        private static void CreateHomeScreenPrefab(CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("HomeScreen");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            var bg = root.AddComponent<Image>();
            bg.color = theme != null ? theme.background : UIStyleConstants.Background;
            bg.raycastTarget = true;

            var home = root.AddComponent<CircuitOneStroke.UI.HomeScreen>();

            var title = new GameObject("Title");
            title.transform.SetParent(root.transform, false);
            var titleRect = title.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.85f);
            titleRect.anchorMax = new Vector2(0.5f, 0.95f);
            titleRect.sizeDelta = new Vector2(400, 60);
            titleRect.anchoredPosition = Vector2.zero;
            var titleText = title.AddComponent<Text>();
            titleText.text = "Circuit One-Stroke";
            titleText.fontSize = 42;
            titleText.alignment = TextAnchor.MiddleCenter;

            var heartBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "/HeartBar.prefab");
            GameObject heartBarGo;
            if (heartBarPrefab != null)
            {
                heartBarGo = Object.Instantiate(heartBarPrefab);
                heartBarGo.name = "HeartBar";
                heartBarGo.transform.SetParent(root.transform, false);
                var hbRect = heartBarGo.GetComponent<RectTransform>();
                hbRect.anchorMin = new Vector2(0.5f, 0.75f);
                hbRect.anchorMax = new Vector2(0.5f, 0.8f);
                hbRect.sizeDelta = new Vector2(200, 40);
            }
            else
            {
                heartBarGo = new GameObject("HeartBar");
                heartBarGo.transform.SetParent(root.transform, false);
                heartBarGo.AddComponent<RectTransform>();
            }

            var btnContinue = CreateButton("Continue / Play", theme);
            btnContinue.transform.SetParent(root.transform, false);
            SetRect(btnContinue, 0.3f, 0.55f, 0.7f, 0.65f);

            var btnLevelSelect = CreateButton("Level Select", theme);
            btnLevelSelect.transform.SetParent(root.transform, false);
            SetRect(btnLevelSelect, 0.3f, 0.42f, 0.7f, 0.52f);

            var btnSettings = CreateButton("Settings", theme);
            btnSettings.transform.SetParent(root.transform, false);
            SetRect(btnSettings, 0.3f, 0.29f, 0.7f, 0.39f);

            var btnShop = CreateButton("Shop", theme);
            btnShop.transform.SetParent(root.transform, false);
            SetRect(btnShop, 0.3f, 0.16f, 0.7f, 0.26f);

            var so = new SerializedObject(home);
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("continueButton").objectReferenceValue = btnContinue.GetComponent<Button>();
            so.FindProperty("levelSelectButton").objectReferenceValue = btnLevelSelect.GetComponent<Button>();
            so.FindProperty("settingsButton").objectReferenceValue = btnSettings.GetComponent<Button>();
            so.FindProperty("shopButton").objectReferenceValue = btnShop.GetComponent<Button>();
            var hb = heartBarGo.GetComponent<CircuitOneStroke.UI.HeartBar>();
            if (hb != null)
                so.FindProperty("heartBar").objectReferenceValue = hb;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ScreensPath + "/HomeScreen.prefab");
        }

        private static void CreateLevelSelectScreenPrefab(CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("LevelSelectScreen");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            var bg = root.AddComponent<Image>();
            bg.color = theme != null ? theme.background : UIStyleConstants.Background;
            bg.raycastTarget = true;

            var screen = root.AddComponent<CircuitOneStroke.UI.LevelSelectScreen>();

            var backBtn = CreateButton("Back", theme);
            backBtn.transform.SetParent(root.transform, false);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.02f, 0.92f);
            backRect.anchorMax = new Vector2(0.18f, 0.98f);
            backRect.offsetMin = backRect.offsetMax = Vector2.zero;

            var settingsBtn = CreateButton("Settings", theme);
            settingsBtn.transform.SetParent(root.transform, false);
            var setRect = settingsBtn.GetComponent<RectTransform>();
            setRect.anchorMin = new Vector2(0.82f, 0.92f);
            setRect.anchorMax = new Vector2(0.98f, 0.98f);
            setRect.offsetMin = setRect.offsetMax = Vector2.zero;

            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(root.transform, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.05f, 0.1f);
            gridRect.anchorMax = new Vector2(0.95f, 0.88f);
            gridRect.offsetMin = gridRect.offsetMax = Vector2.zero;

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100, 100);
            grid.spacing = new Vector2(12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;

            var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "/LevelCell.prefab");

            var so = new SerializedObject(screen);
            so.FindProperty("gridContainer").objectReferenceValue = gridGo.transform;
            so.FindProperty("levelCellPrefab").objectReferenceValue = cellPrefab;
            so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
            so.FindProperty("settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
            so.FindProperty("gridLayout").objectReferenceValue = grid;
            so.FindProperty("columns").intValue = 5;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ScreensPath + "/LevelSelectScreen.prefab");
        }

        private static void CreateSettingsScreenPrefab(CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("SettingsScreen");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            var bg = root.AddComponent<Image>();
            bg.color = theme != null ? theme.background : UIStyleConstants.Background;
            bg.raycastTarget = true;

            var panel = root.AddComponent<CircuitOneStroke.UI.SettingsPanel>();

            var backBtn = CreateButton("Back", theme);
            backBtn.transform.SetParent(root.transform, false);
            SetRect(backBtn, 0.02f, 0.9f, 0.18f, 0.98f);

            var title = new GameObject("Title");
            title.transform.SetParent(root.transform, false);
            var titleRect = title.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.2f, 0.9f);
            titleRect.anchorMax = new Vector2(0.8f, 0.98f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
            var titleText = title.AddComponent<Text>();
            titleText.text = "Settings";
            titleText.fontSize = 32;
            titleText.alignment = TextAnchor.MiddleCenter;

            // ScrollView
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(root.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 0.88f);
            scrollRect.offsetMin = new Vector2(10, 10);
            scrollRect.offsetMax = new Vector2(-10, -10);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = new Vector2(0, 0);
            contentRect.offsetMax = new Vector2(0, 1200);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRect;
            scroll.content = contentRect;

            void AddSectionHeader(string label)
            {
                var h = CreateSectionHeader(label);
                h.transform.SetParent(content.transform, false);
            }
            GameObject AddToggle(string label, out Toggle t)
            {
                var go = CreateToggle(label, theme);
                go.transform.SetParent(content.transform, false);
                t = go.GetComponent<Toggle>();
                return go;
            }
            GameObject AddSlider(string label, out Slider s, out Text vText)
            {
                var go = CreateSliderRow(label, theme, out s, out vText);
                go.transform.SetParent(content.transform, false);
                return go;
            }
            GameObject AddDropdown(string label, string[] options, out Dropdown d)
            {
                var go = CreateDropdownRow(label, options, theme, out d);
                go.transform.SetParent(content.transform, false);
                return go;
            }
            GameObject AddBtn(string label, out Button b)
            {
                var go = CreateButton(label, theme);
                go.transform.SetParent(content.transform, false);
                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = 48;
                b = go.GetComponent<Button>();
                return go;
            }
            GameObject AddLabel(string text, out Text t)
            {
                var go = new GameObject("Label");
                go.transform.SetParent(content.transform, false);
                var rect = go.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(0, 24);
                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = 40;
                t = go.AddComponent<Text>();
                t.text = text;
                t.fontSize = 14;
                return go;
            }

            AddSectionHeader("Audio & Haptics");
            Toggle musicT, sfxT, hapticsT;
            Slider musicS, sfxS, snapS;
            Text musicVT, sfxVT, snapVT;
            Dropdown hapticsD, nodeD, lineD;
            AddToggle("Music", out musicT);
            AddSlider("Music Volume", out musicS, out musicVT);
            AddToggle("Sound Effects", out sfxT);
            AddSlider("SFX Volume", out sfxS, out sfxVT);
            AddToggle("Haptics (Vibration)", out hapticsT);
            AddDropdown("Haptics Strength", new[] { "Light", "Normal" }, out hapticsD);

            AddSectionHeader("Controls & Visuals");
            AddSlider("Snap Assist (Higher = easier snapping)", out snapS, out snapVT);
            Toggle rejectT, confirmT, hardT, iconT;
            AddToggle("Reject feedback animation", out rejectT);
            AddToggle("Confirm before leaving a level", out confirmT);
            AddToggle("Hard Mode (Immediate Fail)", out hardT);
            AddDropdown("Node Size", new[] { "Small", "Normal", "Large" }, out nodeD);
            AddDropdown("Line Thickness", new[] { "Thin", "Normal", "Thick" }, out lineD);
            AddToggle("Show hearts as icons + text", out iconT);

            AddSectionHeader("Accessibility");
            Toggle colorT, contrastT, largeT;
            AddToggle("Colorblind Mode", out colorT);
            AddToggle("High Contrast UI", out contrastT);
            AddToggle("Large Text", out largeT);

            AddSectionHeader("About & Support");
            Button howBtn, privacyBtn, termsBtn, feedbackBtn;
            AddBtn("How to Play", out howBtn);
            AddBtn("Privacy Policy", out privacyBtn);
            AddBtn("Terms of Service", out termsBtn);
            AddBtn("Send Feedback", out feedbackBtn);
            Text verText;
            AddLabel("Version: " + Application.version, out verText);

            AddSectionHeader("Shop");
            Button noAdsBtn;
            AddBtn("Remove Forced Ads", out noAdsBtn);
            Text shopNote;
            AddLabel("Removes forced ads (interstitial/banners). Optional rewarded ads for bonuses may still be available.", out shopNote);

            var so = new SerializedObject(panel);
            so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("musicToggle").objectReferenceValue = musicT;
            so.FindProperty("musicVolumeSlider").objectReferenceValue = musicS;
            so.FindProperty("musicVolumeValueText").objectReferenceValue = musicVT;
            so.FindProperty("sfxToggle").objectReferenceValue = sfxT;
            so.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxS;
            so.FindProperty("sfxVolumeValueText").objectReferenceValue = sfxVT;
            so.FindProperty("hapticsToggle").objectReferenceValue = hapticsT;
            so.FindProperty("hapticsStrengthDropdown").objectReferenceValue = hapticsD;
            so.FindProperty("snapAssistSlider").objectReferenceValue = snapS;
            so.FindProperty("snapAssistValueText").objectReferenceValue = snapVT;
            so.FindProperty("rejectFeedbackToggle").objectReferenceValue = rejectT;
            so.FindProperty("confirmExitToggle").objectReferenceValue = confirmT;
            so.FindProperty("hardModeToggle").objectReferenceValue = hardT;
            so.FindProperty("nodeSizeDropdown").objectReferenceValue = nodeD;
            so.FindProperty("lineThicknessDropdown").objectReferenceValue = lineD;
            so.FindProperty("showIconAndTextToggle").objectReferenceValue = iconT;
            so.FindProperty("colorBlindToggle").objectReferenceValue = colorT;
            so.FindProperty("highContrastToggle").objectReferenceValue = contrastT;
            so.FindProperty("largeTextToggle").objectReferenceValue = largeT;
            so.FindProperty("howToPlayButton").objectReferenceValue = howBtn;
            so.FindProperty("privacyPolicyButton").objectReferenceValue = privacyBtn;
            so.FindProperty("termsButton").objectReferenceValue = termsBtn;
            so.FindProperty("feedbackButton").objectReferenceValue = feedbackBtn;
            so.FindProperty("versionText").objectReferenceValue = verText;
            so.FindProperty("removeAdsButton").objectReferenceValue = noAdsBtn;
            so.FindProperty("shopNoteText").objectReferenceValue = shopNote;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ScreensPath + "/SettingsScreen.prefab");
        }

        private static GameObject CreateSectionHeader(string label)
        {
            var go = new GameObject("Section_" + label);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 36);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            var t = go.AddComponent<Text>();
            t.text = label;
            t.fontSize = 22;
            t.fontStyle = FontStyle.Bold;
            return go;
        }

        private static GameObject CreateSliderRow(string label, CircuitOneStrokeTheme theme, out Slider slider, out Text valueText)
        {
            var root = new GameObject("SliderRow");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(0, 48);
            var le = root.AddComponent<LayoutElement>();
            le.preferredHeight = 48;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0.5f);
            labelRect.offsetMin = new Vector2(10, 2);
            labelRect.offsetMax = new Vector2(-80, -2);
            var labelT = labelGo.AddComponent<Text>();
            labelT.text = label;
            labelT.fontSize = 16;

            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(root.transform, false);
            var valueRect = valueGo.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.85f, 0);
            valueRect.anchorMax = new Vector2(1, 0.5f);
            valueRect.offsetMin = new Vector2(-70, 2);
            valueRect.offsetMax = new Vector2(-10, -2);
            valueText = valueGo.AddComponent<Text>();
            valueText.text = "100%";
            valueText.fontSize = 14;
            valueText.alignment = TextAnchor.MiddleRight;

            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(root.transform, false);
            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0.5f);
            sliderRect.anchorMax = new Vector2(1, 1);
            sliderRect.offsetMin = new Vector2(10, 2);
            sliderRect.offsetMax = new Vector2(-10, -2);
            slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.8f;
            var sliderBg = new GameObject("Background");
            sliderBg.transform.SetParent(sliderGo.transform, false);
            var bgRect = sliderBg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            var bgImg = sliderBg.AddComponent<Image>();
            bgImg.color = theme != null ? theme.panelBorder : UIStyleConstants.PanelBorder;
            slider.targetGraphic = bgImg;
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(5, 5);
            fillRect.offsetMax = new Vector2(-5, -5);
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = theme != null ? theme.primary : UIStyleConstants.Primary;
            fill.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            fill.GetComponent<RectTransform>().anchorMax = Vector2.one;
            fill.GetComponent<RectTransform>().offsetMin = fill.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.direction = Slider.Direction.LeftToRight;
            return root;
        }

        private static GameObject CreateDropdownRow(string label, string[] options, CircuitOneStrokeTheme theme, out Dropdown dropdown)
        {
            var root = new GameObject("DropdownRow");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(0, 40);
            var le = root.AddComponent<LayoutElement>();
            le.preferredHeight = 40;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = new Vector2(0.6f, 1);
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-10, 0);
            var labelT = labelGo.AddComponent<Text>();
            labelT.text = label;
            labelT.fontSize = 16;

            var ddGo = new GameObject("Dropdown");
            ddGo.transform.SetParent(root.transform, false);
            var ddRect = ddGo.AddComponent<RectTransform>();
            ddRect.anchorMin = new Vector2(0.6f, 0);
            ddRect.anchorMax = Vector2.one;
            ddRect.offsetMin = new Vector2(10, 0);
            ddRect.offsetMax = new Vector2(-10, 0);
            var ddImg = ddGo.AddComponent<Image>();
            ddImg.color = theme != null ? theme.panelBase : UIStyleConstants.PanelBase;
            dropdown = ddGo.AddComponent<Dropdown>();
            var caption = new GameObject("Label");
            caption.transform.SetParent(ddGo.transform, false);
            var capRect = caption.AddComponent<RectTransform>();
            capRect.anchorMin = Vector2.zero;
            capRect.anchorMax = Vector2.one;
            capRect.offsetMin = new Vector2(10, 0);
            capRect.offsetMax = new Vector2(-25, 0);
            var capT = caption.AddComponent<Text>();
            capT.text = options.Length > 0 ? options[0] : "";
            capT.fontSize = 14;
            dropdown.captionText = capT;
            dropdown.targetGraphic = ddImg;

            var template = new GameObject("Template");
            template.transform.SetParent(ddGo.transform, false);
            var tRect = template.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0);
            tRect.anchorMax = new Vector2(1, 0);
            tRect.pivot = new Vector2(0.5f, 1);
            tRect.anchoredPosition = Vector2.zero;
            tRect.sizeDelta = new Vector2(0, 32);
            template.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 1f);
            var templateVp = new GameObject("Viewport");
            templateVp.transform.SetParent(template.transform, false);
            var vpR = templateVp.AddComponent<RectTransform>();
            vpR.anchorMin = Vector2.zero;
            vpR.anchorMax = Vector2.one;
            vpR.offsetMin = vpR.offsetMax = Vector2.zero;
            templateVp.AddComponent<RectMask2D>();
            var templateContent = new GameObject("Content");
            templateContent.transform.SetParent(templateVp.transform, false);
            var contentR = templateContent.AddComponent<RectTransform>();
            contentR.anchorMin = new Vector2(0, 1);
            contentR.anchorMax = new Vector2(1, 1);
            contentR.pivot = new Vector2(0.5f, 1);
            contentR.offsetMin = contentR.offsetMax = Vector2.zero;
            contentR.sizeDelta = new Vector2(0, 32);
            var item = new GameObject("Item");
            item.transform.SetParent(templateContent.transform, false);
            var itemR = item.AddComponent<RectTransform>();
            itemR.anchorMin = Vector2.zero;
            itemR.anchorMax = new Vector2(1, 0);
            itemR.pivot = new Vector2(0.5f, 0.5f);
            itemR.offsetMin = new Vector2(0, 0);
            itemR.offsetMax = new Vector2(0, 32);
            var itemTgl = item.AddComponent<Toggle>();
            var itemBg = item.AddComponent<Image>();
            itemBg.color = Color.clear;
            itemTgl.targetGraphic = itemBg;
            var itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(item.transform, false);
            var ilR = itemLabel.AddComponent<RectTransform>();
            ilR.anchorMin = Vector2.zero;
            ilR.anchorMax = Vector2.one;
            ilR.offsetMin = new Vector2(10, 0);
            ilR.offsetMax = new Vector2(-10, 0);
            var ilT = itemLabel.AddComponent<Text>();
            ilT.text = "Option";
            ilT.fontSize = 14;
            ilT.alignment = TextAnchor.MiddleLeft;
            dropdown.itemText = ilT;
            dropdown.template = tRect;
            dropdown.options.Clear();
            foreach (var opt in options)
                dropdown.options.Add(new Dropdown.OptionData(opt));
            template.SetActive(false);
            return root;
        }

        private static void CreateShopScreenPrefab(CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("ShopScreen");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            var bg = root.AddComponent<Image>();
            bg.color = theme != null ? theme.background : UIStyleConstants.Background;
            bg.raycastTarget = true;

            var panel = root.AddComponent<CircuitOneStroke.UI.ShopPanel>();

            var backBtn = CreateButton("Back", theme);
            backBtn.transform.SetParent(root.transform, false);
            SetRect(backBtn, 0.02f, 0.9f, 0.18f, 0.98f);

            var desc = new GameObject("Description");
            desc.transform.SetParent(root.transform, false);
            var descRect = desc.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.1f, 0.5f);
            descRect.anchorMax = new Vector2(0.9f, 0.75f);
            descRect.offsetMin = descRect.offsetMax = Vector2.zero;
            var descText = desc.AddComponent<Text>();
            descText.text = "Remove Forced Ads: Interstitial ads are removed. Rewarded ads (hearts refill) remain optional.";
            descText.fontSize = 20;
            descText.alignment = TextAnchor.MiddleCenter;
            descText.resizeTextForBestFit = true;

            var noAdsBtn = CreateButton("Remove Ads", theme);
            noAdsBtn.transform.SetParent(root.transform, false);
            SetRect(noAdsBtn, 0.3f, 0.3f, 0.7f, 0.42f);

            var so = new SerializedObject(panel);
            so.FindProperty("noAdsDescriptionText").objectReferenceValue = descText;
            so.FindProperty("noAdsPurchaseButton").objectReferenceValue = noAdsBtn.GetComponent<Button>();
            so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ScreensPath + "/ShopScreen.prefab");
        }

        private static void CreateOutOfHeartsScreenPrefab(CircuitOneStrokeTheme theme)
        {
            var root = new GameObject("OutOfHeartsScreen");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.07f, 0.12f, 0.95f);
            bg.raycastTarget = true;

            var screen = root.AddComponent<CircuitOneStroke.UI.OutOfHeartsScreen>();

            var msg = new GameObject("Message");
            msg.transform.SetParent(root.transform, false);
            var msgRect = msg.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.2f, 0.6f);
            msgRect.anchorMax = new Vector2(0.8f, 0.85f);
            msgRect.offsetMin = msgRect.offsetMax = Vector2.zero;
            var msgText = msg.AddComponent<Text>();
            msgText.text = "Out of hearts!\nWatch an ad to refill.";
            msgText.fontSize = 28;
            msgText.alignment = TextAnchor.MiddleCenter;

            var watchBtn = CreateButton("Watch Ad to Refill", theme);
            watchBtn.transform.SetParent(root.transform, false);
            SetRect(watchBtn, 0.25f, 0.35f, 0.75f, 0.48f);

            var backBtn = CreateButton("Back to Home", theme);
            backBtn.transform.SetParent(root.transform, false);
            SetRect(backBtn, 0.25f, 0.18f, 0.75f, 0.31f);

            var so = new SerializedObject(screen);
            so.FindProperty("watchAdButton").objectReferenceValue = watchBtn.GetComponent<Button>();
            so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ScreensPath + "/OutOfHeartsScreen.prefab");
        }

        private static GameObject CreateButton(string label, CircuitOneStrokeTheme theme)
        {
            var go = new GameObject(label + "Button");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 48);
            var img = go.AddComponent<Image>();
            img.color = theme != null ? theme.primary : UIStyleConstants.Primary;
            go.AddComponent<Button>();
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = theme != null ? theme.textOnAccent : UIStyleConstants.TextOnAccent;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject CreateToggle(string label, CircuitOneStrokeTheme theme)
        {
            var go = new GameObject(label + "Toggle");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 40);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            var toggle = go.AddComponent<Toggle>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = UIStyleConstants.PanelBase;
            toggle.targetGraphic = bgImg;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-50, 0);
            var labelText = labelGo.AddComponent<Text>();
            labelText.text = label;
            labelText.fontSize = 18;

            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(go.transform, false);
            var checkRect = checkGo.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.85f, 0.2f);
            checkRect.anchorMax = new Vector2(0.95f, 0.8f);
            checkRect.offsetMin = checkRect.offsetMax = Vector2.zero;
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = UIStyleConstants.Primary;
            toggle.graphic = checkImg;

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

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("Created: " + path);
        }
    }
}
#endif
