using System.Collections.Generic;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;
using CircuitOneStroke.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// Map-style level select screen:
    /// top HUD + vertical winding level path + central Play button.
    /// </summary>
    public class LevelSelectScreen : MonoBehaviour, IUIScreen
    {
        [Header("Top Bar")]
        [SerializeField] private Button profileButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Text heartsText;
        [SerializeField] private Text coinsText;

        [Header("Map")]
        [SerializeField] private ScrollRect mapScrollRect;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private GameObject mapNodePrefab;
        [SerializeField] private float nodeVerticalSpacing = 130f;
        [SerializeField] private float mapWaveAmplitude = 120f;
        [SerializeField] private float mapTopPadding = 220f;
        [SerializeField] private float mapBottomPadding = 260f;

        [Header("Play CTA")]
        [SerializeField] private Text selectedLevelText;
        [SerializeField] private Button playButton;
        [SerializeField] private Text playButtonLabel;

        private UIScreenRouter _router;
        private LevelManifest _manifest;
        private LevelLoader _loader;
        private readonly List<LevelMapNodeCell> _mapNodes = new List<LevelMapNodeCell>();
        private int _selectedLevelId = 1;
        private bool _eventsBound;
        private CircuitOneStrokeTheme _theme;

        public void BindRouter(UIScreenRouter router)
        {
            _router = router;
            _manifest = router != null ? router.LevelManifest : null;
            _loader = router != null ? router.LevelLoader : null;
        }

        private void Awake()
        {
            ResolveTheme();
            EnsureRuntimeMapUI();
        }

        private void Start()
        {
            BindUiEvents();
            RefreshHud();
            BuildMap();
        }

        private void OnDestroy()
        {
            if (HeartsManager.Instance != null)
                HeartsManager.Instance.OnHeartsChanged -= OnHeartsChanged;
        }

        private void OnEnable()
        {
            EnsureDependencies();
            RefreshHud();
            BuildMap();
        }

        private void BindUiEvents()
        {
            if (_eventsBound) return;
            _eventsBound = true;

            if (profileButton != null)
                profileButton.onClick.AddListener(() => _router?.ShowHome());
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => _router?.ShowSettings());
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (HeartsManager.Instance != null)
                HeartsManager.Instance.OnHeartsChanged += OnHeartsChanged;
        }

        private void EnsureDependencies()
        {
            if (_manifest == null && AppRouter.Instance != null)
            {
                _manifest = AppRouter.Instance.LevelManifest;
                _loader = AppRouter.Instance.LevelLoader;
            }
            ResolveTheme();
        }

        private void ResolveTheme()
        {
            if (_theme != null) return;
            var localApplier = GetComponent<ThemeApplier>();
            if (localApplier != null && localApplier.Theme != null)
            {
                _theme = localApplier.Theme;
                return;
            }
            var parentApplier = GetComponentInParent<ThemeApplier>();
            if (parentApplier != null)
                _theme = parentApplier.Theme;
        }

        private void OnHeartsChanged(int _)
        {
            RefreshHud();
        }

        private void OnPlayClicked()
        {
            OnLevelClicked(_selectedLevelId);
        }

        private void BuildMap()
        {
            if (mapContent == null) return;
            EnsureDependencies();

            int maxLevels = _manifest != null ? Mathf.Max(1, _manifest.Count) : 20;
            int unlocked = LevelRecords.LastUnlockedLevelId(maxLevels);
            int startSelection = LevelRecords.LastPlayedLevelId;
            if (startSelection <= 0) startSelection = unlocked;
            _selectedLevelId = Mathf.Clamp(_selectedLevelId <= 0 ? startSelection : _selectedLevelId, 1, maxLevels);

            if (_mapNodes.Count > maxLevels)
            {
                for (int i = _mapNodes.Count - 1; i >= maxLevels; i--)
                {
                    if (_mapNodes[i] != null) Destroy(_mapNodes[i].gameObject);
                    _mapNodes.RemoveAt(i);
                }
            }

            for (int i = _mapNodes.Count; i < maxLevels; i++)
            {
                GameObject go = mapNodePrefab != null ? Instantiate(mapNodePrefab, mapContent) : CreateMapNodeRuntime(mapContent, i + 1);
                var node = go.GetComponent<LevelMapNodeCell>();
                if (node == null)
                {
                    node = go.AddComponent<LevelMapNodeCell>();
                    node.AssignFromChildren();
                }
                _mapNodes.Add(node);
            }

            float contentHeight = mapTopPadding + mapBottomPadding + Mathf.Max(0, maxLevels - 1) * nodeVerticalSpacing;
            mapContent.sizeDelta = new Vector2(mapContent.sizeDelta.x, contentHeight);

            for (int i = 0; i < _mapNodes.Count; i++)
            {
                int levelId = i + 1;
                bool isLocked = levelId > unlocked;
                bool isCleared = LevelRecords.IsCleared(levelId);
                bool isSelected = levelId == _selectedLevelId;
                Vector2 pos = ComputeMapNodePosition(i, maxLevels);
                _mapNodes[i].Setup(levelId, isLocked, isCleared, isSelected, pos, () => SelectLevel(levelId));
            }

            RefreshPlayCta(maxLevels, unlocked);
            ScrollToLevel(_selectedLevelId, maxLevels);
        }

        private void RefreshHud()
        {
            int h = HeartsManager.Instance != null ? HeartsManager.Instance.Hearts : 0;
            int max = HeartsManager.Instance != null ? HeartsManager.Instance.MaxHearts : 0;
            if (heartsText != null)
                heartsText.text = $"{h} {(h >= max && max > 0 ? "FULL" : "")}".Trim();
            if (coinsText != null)
            {
                // Placeholder economy value until a dedicated coin wallet system is added.
                int pseudoCoins = Mathf.Max(0, LevelRecords.LastPlayedLevelId * 3);
                coinsText.text = pseudoCoins.ToString();
            }
        }

        private void SelectLevel(int levelId)
        {
            int maxLevels = _manifest != null ? Mathf.Max(1, _manifest.Count) : 20;
            int unlocked = LevelRecords.LastUnlockedLevelId(maxLevels);
            if (levelId > unlocked) return;
            _selectedLevelId = levelId;
            BuildMap();
        }

        private void RefreshPlayCta(int maxLevels, int unlocked)
        {
            if (selectedLevelText != null)
                selectedLevelText.text = _selectedLevelId.ToString();
            if (playButtonLabel != null)
                playButtonLabel.text = "PLAY";
            if (playButton != null)
                playButton.interactable = _selectedLevelId <= unlocked && _selectedLevelId <= maxLevels;
        }

        private Vector2 ComputeMapNodePosition(int index, int maxLevels)
        {
            float y = -mapTopPadding - index * nodeVerticalSpacing;
            float t = maxLevels > 1 ? index / (float)(maxLevels - 1) : 0f;
            float wave = Mathf.Sin(t * Mathf.PI * 2.6f + 0.35f) * mapWaveAmplitude;
            return new Vector2(wave, y);
        }

        private void ScrollToLevel(int levelId, int maxLevels)
        {
            if (mapScrollRect == null || maxLevels <= 1) return;
            float t = Mathf.Clamp01((levelId - 1) / (float)(maxLevels - 1));
            mapScrollRect.verticalNormalizedPosition = 1f - t;
        }

        private void OnLevelClicked(int levelId)
        {
            int maxLevels = _manifest != null ? _manifest.Count : 20;
            int unlocked = LevelRecords.LastUnlockedLevelId(maxLevels);
            if (levelId > unlocked) return;

            if (AppRouter.Instance != null)
            {
                if (HeartsManager.Instance == null || !HeartsManager.Instance.CanStartAttempt())
                {
                    AppRouter.Instance.ShowOutOfHearts(OutOfHeartsContext.FromLevelSelect);
                    return;
                }
                AppRouter.Instance.RequestStartLevel(levelId);
                return;
            }
            if (HeartsManager.Instance == null || !HeartsManager.Instance.CanStartAttempt())
            {
                _router?.ShowOutOfHearts(OutOfHeartsContext.FromLevelSelect);
                return;
            }
            if (ScreenRouter.Instance != null)
            {
                ScreenRouter.Instance.EnterGame(levelId);
                return;
            }
            _router?.StartLevel(levelId);
        }

        private void EnsureRuntimeMapUI()
        {
            if (mapScrollRect != null && mapContent != null && playButton != null)
                return;

            var rootRect = transform as RectTransform;
            if (rootRect == null) return;

            if (profileButton == null)
                profileButton = CreateButtonRuntime("ProfileButton", rootRect, "Me", new Vector2(0.08f, 0.95f), new Vector2(140, 80));
            if (settingsButton == null)
                settingsButton = CreateButtonRuntime("SettingsButton", rootRect, "SET", new Vector2(0.92f, 0.95f), new Vector2(140, 80));
            if (heartsText == null)
                heartsText = CreateHudTextRuntime("HeartsText", rootRect, "5 FULL", new Vector2(0.70f, 0.95f), 34);
            if (coinsText == null)
                coinsText = CreateHudTextRuntime("CoinsText", rootRect, "25", new Vector2(0.83f, 0.95f), 34);

            if (mapScrollRect == null || mapContent == null)
            {
                var mapRoot = new GameObject("MapScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
                var mapRootRect = mapRoot.GetComponent<RectTransform>();
                mapRootRect.SetParent(rootRect, false);
                mapRootRect.anchorMin = new Vector2(0f, 0.15f);
                mapRootRect.anchorMax = new Vector2(1f, 0.90f);
                mapRootRect.offsetMin = Vector2.zero;
                mapRootRect.offsetMax = Vector2.zero;
                var bg = mapRoot.GetComponent<Image>();
                bg.color = _theme != null ? _theme.background : new Color(1f, 0.94f, 0.78f, 0.95f);
                if (_theme != null && _theme.panelSprite != null) bg.sprite = _theme.panelSprite;

                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
                var vpRect = viewport.GetComponent<RectTransform>();
                vpRect.SetParent(mapRootRect, false);
                vpRect.anchorMin = Vector2.zero;
                vpRect.anchorMax = Vector2.one;
                vpRect.offsetMin = vpRect.offsetMax = Vector2.zero;
                var vpImage = viewport.GetComponent<Image>();
                vpImage.color = Color.clear;
                viewport.GetComponent<Mask>().showMaskGraphic = false;

                var content = new GameObject("Content", typeof(RectTransform));
                var contentRect = content.GetComponent<RectTransform>();
                contentRect.SetParent(vpRect, false);
                contentRect.anchorMin = new Vector2(0.5f, 1f);
                contentRect.anchorMax = new Vector2(0.5f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.sizeDelta = new Vector2(700, 2200);
                contentRect.anchoredPosition = Vector2.zero;

                mapScrollRect = mapRoot.GetComponent<ScrollRect>();
                mapScrollRect.horizontal = false;
                mapScrollRect.vertical = true;
                mapScrollRect.viewport = vpRect;
                mapScrollRect.content = contentRect;
                mapScrollRect.movementType = ScrollRect.MovementType.Clamped;
                mapScrollRect.scrollSensitivity = 24f;
                mapContent = contentRect;
            }

            if (selectedLevelText == null)
                selectedLevelText = CreateHudTextRuntime("SelectedLevelText", rootRect, "1", new Vector2(0.5f, 0.10f), 64);
            if (playButton == null)
                playButton = CreateButtonRuntime("PlayButton", rootRect, "PLAY", new Vector2(0.5f, 0.04f), new Vector2(280, 100));
            if (playButtonLabel == null && playButton != null)
                playButtonLabel = playButton.GetComponentInChildren<Text>();

            ApplyThemeToRuntimeMapUI();
        }

        private void ApplyThemeToRuntimeMapUI()
        {
            ApplyButtonTheme(profileButton);
            ApplyButtonTheme(settingsButton);
            ApplyButtonTheme(playButton);
            ApplyTextTheme(heartsText, true);
            ApplyTextTheme(coinsText, true);
            ApplyTextTheme(selectedLevelText, true);
            ApplyTextTheme(playButtonLabel, false);
        }

        private void ApplyButtonTheme(Button btn)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                if (_theme != null && _theme.buttonSprite != null)
                {
                    img.sprite = _theme.buttonSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
                else
                {
                    img.color = _theme != null ? _theme.primary : UIStyleConstants.Primary;
                }
            }
            var txt = btn.GetComponentInChildren<Text>();
            ApplyTextTheme(txt, false);
        }

        private void ApplyTextTheme(Text text, bool primary)
        {
            if (text == null) return;
            if (_theme != null && _theme.font != null)
                text.font = _theme.font;
            text.color = _theme != null
                ? (primary ? _theme.textPrimary : _theme.textOnAccent)
                : (primary ? UIStyleConstants.TextPrimary : UIStyleConstants.TextOnAccent);
        }

        private Button CreateButtonRuntime(string name, RectTransform parent, string text, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var img = go.GetComponent<Image>();
            if (_theme != null && _theme.buttonSprite != null)
            {
                img.sprite = _theme.buttonSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = _theme != null ? _theme.primary : UIStyleConstants.Primary;
            }
            var btn = go.GetComponent<Button>();

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var t = textGo.GetComponent<Text>();
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = _theme != null && _theme.font != null ? _theme.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 40;
            t.color = _theme != null ? _theme.textOnAccent : Color.white;
            return btn;
        }

        private Text CreateHudTextRuntime(string name, RectTransform parent, string text, Vector2 anchor, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220, 70);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = _theme != null && _theme.font != null ? _theme.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = _theme != null ? _theme.textPrimary : new Color(0.16f, 0.2f, 0.32f, 1f);
            return t;
        }

        private GameObject CreateMapNodeRuntime(RectTransform parent, int levelId)
        {
            var root = new GameObject($"MapNode_{levelId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LevelMapNodeCell));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(150, 110);
            var img = root.GetComponent<Image>();
            if (_theme != null && _theme.buttonSprite != null)
            {
                img.sprite = _theme.buttonSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.56f, 0.56f, 0.58f, 1f);
            }

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = levelId.ToString();
            label.font = _theme != null && _theme.font != null ? _theme.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 44;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = _theme != null ? _theme.textOnAccent : Color.white;

            var lockGo = new GameObject("Lock", typeof(RectTransform), typeof(Image));
            var lockRect = lockGo.GetComponent<RectTransform>();
            lockRect.SetParent(rect, false);
            lockRect.anchorMin = new Vector2(0.1f, 0.1f);
            lockRect.anchorMax = new Vector2(0.9f, 0.9f);
            lockRect.offsetMin = lockRect.offsetMax = Vector2.zero;
            var lockImg = lockGo.GetComponent<Image>();
            lockImg.color = new Color(0f, 0f, 0f, 0.6f);
            lockGo.SetActive(false);

            var selectedGo = new GameObject("SelectedRing", typeof(RectTransform), typeof(Image));
            var selectedRect = selectedGo.GetComponent<RectTransform>();
            selectedRect.SetParent(rect, false);
            selectedRect.anchorMin = new Vector2(-0.06f, -0.06f);
            selectedRect.anchorMax = new Vector2(1.06f, 1.06f);
            selectedRect.offsetMin = selectedRect.offsetMax = Vector2.zero;
            var selectedImg = selectedGo.GetComponent<Image>();
            selectedImg.color = new Color(1f, 0.74f, 0.18f, 0.55f);
            selectedGo.transform.SetAsFirstSibling();
            selectedGo.SetActive(false);

            var cell = root.GetComponent<LevelMapNodeCell>();
            cell.Assign(root.GetComponent<Button>(), img, label, lockGo, selectedGo);
            return root;
        }
    }

    /// <summary>Legacy grid cell kept for old prefabs/editor tools.</summary>
    public class LevelSelectCell : MonoBehaviour
    {
        [SerializeField] private Text numberText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject clearedCheckmark;
        [SerializeField] private Text bestTimeText;
        [SerializeField] private Button button;

        public System.Action OnClicked;

        public void AssignReferencesFromChildren()
        {
            if (numberText != null && lockOverlay != null && clearedCheckmark != null && bestTimeText != null && button != null)
                return;
            var t = transform;
            if (numberText == null)
            {
                var num = t.Find("Number");
                if (num != null) numberText = num.GetComponent<Text>();
            }
            if (lockOverlay == null) lockOverlay = t.Find("LockOverlay")?.gameObject;
            if (clearedCheckmark == null) clearedCheckmark = t.Find("ClearedCheckmark")?.gameObject;
            if (bestTimeText == null)
            {
                var time = t.Find("BestTime");
                if (time != null) bestTimeText = time.GetComponent<Text>();
            }
            if (button == null) button = GetComponent<Button>();
        }

        public void Setup(int levelId, bool isLocked, bool isCleared, float bestTime)
        {
            if (numberText != null)
                numberText.text = levelId.ToString();

            if (lockOverlay != null)
                lockOverlay.SetActive(isLocked);

            if (clearedCheckmark != null)
                clearedCheckmark.SetActive(isCleared);

            if (bestTimeText != null)
            {
                if (isCleared && bestTime < float.MaxValue && bestTime > 0)
                    bestTimeText.text = $"{bestTime:F1}s";
                else
                    bestTimeText.text = "";
            }

            if (button != null)
            {
                button.interactable = !isLocked;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClicked?.Invoke());
            }
        }
    }

    public class LevelMapNodeCell : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image bgImage;
        [SerializeField] private Text label;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject selectedRing;

        public void Assign(Button btn, Image bg, Text labelText, GameObject lockGo, GameObject selectedGo)
        {
            button = btn;
            bgImage = bg;
            label = labelText;
            lockOverlay = lockGo;
            selectedRing = selectedGo;
        }

        public void AssignFromChildren()
        {
            if (button == null) button = GetComponent<Button>();
            if (bgImage == null) bgImage = GetComponent<Image>();
            if (label == null) label = transform.Find("Label")?.GetComponent<Text>();
            if (lockOverlay == null) lockOverlay = transform.Find("Lock")?.gameObject;
            if (selectedRing == null) selectedRing = transform.Find("SelectedRing")?.gameObject;
        }

        public void Setup(int levelId, bool isLocked, bool isCleared, bool isSelected, Vector2 anchoredPos, System.Action onClick)
        {
            var rt = transform as RectTransform;
            if (rt != null) rt.anchoredPosition = anchoredPos;

            if (label != null) label.text = levelId.ToString();
            if (lockOverlay != null) lockOverlay.SetActive(isLocked);
            if (selectedRing != null) selectedRing.SetActive(isSelected);
            if (bgImage != null)
            {
                if (isSelected) bgImage.color = new Color(0.95f, 0.67f, 0.12f, 1f);
                else if (isLocked) bgImage.color = new Color(0.42f, 0.42f, 0.44f, 1f);
                else if (isCleared) bgImage.color = new Color(0.37f, 0.60f, 0.86f, 1f);
                else bgImage.color = new Color(0.56f, 0.56f, 0.58f, 1f);
            }

            if (button != null)
            {
                button.interactable = !isLocked;
                button.onClick.RemoveAllListeners();
                if (!isLocked)
                    button.onClick.AddListener(() => onClick?.Invoke());
            }
        }
    }
}
