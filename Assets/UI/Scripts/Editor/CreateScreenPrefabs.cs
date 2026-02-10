#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
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
            titleRect.anchorMin = new Vector2(0.2f, 0.75f);
            titleRect.anchorMax = new Vector2(0.8f, 0.88f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
            var titleText = title.AddComponent<Text>();
            titleText.text = "Settings";
            titleText.fontSize = 32;
            titleText.alignment = TextAnchor.MiddleCenter;

            var soundToggle = CreateToggle("Sound", theme);
            soundToggle.transform.SetParent(root.transform, false);
            SetRect(soundToggle, 0.2f, 0.55f, 0.8f, 0.65f);

            var vibrateToggle = CreateToggle("Vibrate", theme);
            vibrateToggle.transform.SetParent(root.transform, false);
            SetRect(vibrateToggle, 0.2f, 0.42f, 0.8f, 0.52f);

            var hardModeToggle = CreateToggle("Hard Mode (Immediate Fail)", theme);
            hardModeToggle.transform.SetParent(root.transform, false);
            SetRect(hardModeToggle, 0.2f, 0.29f, 0.8f, 0.39f);

            var so = new SerializedObject(panel);
            so.FindProperty("soundToggle").objectReferenceValue = soundToggle.GetComponent<Toggle>();
            so.FindProperty("vibrateToggle").objectReferenceValue = vibrateToggle.GetComponent<Toggle>();
            so.FindProperty("hardModeToggle").objectReferenceValue = hardModeToggle.GetComponent<Toggle>();
            so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ScreensPath + "/SettingsScreen.prefab");
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
