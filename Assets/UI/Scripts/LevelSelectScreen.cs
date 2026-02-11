using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 그리드 기반 레벨 선택 화면. LevelManifest + LevelRecords 사용.
    /// </summary>
    public class LevelSelectScreen : MonoBehaviour, IUIScreen
    {
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GameObject levelCellPrefab;
        [SerializeField] private Button backButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private int columns = 5;

        private UIScreenRouter _router;
        private LevelManifest _manifest;
        private LevelLoader _loader;
        private readonly List<LevelSelectCell> _cells = new List<LevelSelectCell>();

        public void BindRouter(UIScreenRouter router)
        {
            _router = router;
            _manifest = router != null ? router.LevelManifest : null;
            _loader = router != null ? router.LevelLoader : null;
        }

        private void Awake()
        {
            if (gridLayout != null)
                gridLayout.constraintCount = columns;
        }

        private void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => _router?.ShowSettings());

            if (_manifest == null && AppRouter.Instance != null)
            {
                _manifest = AppRouter.Instance.LevelManifest;
                _loader = AppRouter.Instance.LevelLoader;
            }
            BuildGrid();
        }

        private void OnEnable()
        {
            if (_manifest == null && AppRouter.Instance != null)
            {
                _manifest = AppRouter.Instance.LevelManifest;
                _loader = AppRouter.Instance.LevelLoader;
            }
            BuildGrid();
        }

        private void OnBack()
        {
            _router?.ShowHome();
        }

        private void BuildGrid()
        {
            if (gridContainer == null) return;
            bool usePrefab = levelCellPrefab != null && levelCellPrefab.GetComponent<LevelSelectCell>() != null;

            int maxLevels = _manifest != null ? _manifest.Count : 20;

            if (_cells.Count == maxLevels)
            {
                RefreshAllCells(maxLevels);
                return;
            }

            if (_cells.Count > maxLevels)
            {
                for (int i = _cells.Count - 1; i >= maxLevels; i--)
                {
                    if (_cells[i] != null && _cells[i].gameObject != null)
                        Destroy(_cells[i].gameObject);
                    _cells.RemoveAt(i);
                }
                RefreshAllCells(maxLevels);
                return;
            }

            for (int i = _cells.Count; i < maxLevels; i++)
            {
                int levelId = i + 1;
                GameObject cellGo;
                LevelSelectCell cell;
                if (usePrefab)
                {
                    cellGo = Instantiate(levelCellPrefab, gridContainer);
                    cell = cellGo.GetComponent<LevelSelectCell>();
                    if (cell == null)
                    {
                        cell = cellGo.AddComponent<LevelSelectCell>();
                        cell.AssignReferencesFromChildren();
                    }
                    else
                    {
                        cell.AssignReferencesFromChildren();
                    }
                }
                else
                {
                    cellGo = CreateLevelCellRuntime(gridContainer);
                    cell = cellGo.GetComponent<LevelSelectCell>();
                }
                _cells.Add(cell);
            }
            RefreshAllCells(maxLevels);
        }

        /// <summary>LevelCell 프리팹에 스크립트가 깨져 있을 때 런타임에 셀 하나 생성. "Missing script" 로그 방지.</summary>
        private static GameObject CreateLevelCellRuntime(Transform parent)
        {
            var root = new GameObject("LevelCell");
            root.transform.SetParent(parent, false);

            var rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);

            var bg = root.AddComponent<Image>();
            bg.color = UIStyleConstants.PanelBase;

            var btn = root.AddComponent<Button>();

            var numGo = new GameObject("Number");
            numGo.transform.SetParent(root.transform, false);
            var numRect = numGo.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0.2f, 0.4f);
            numRect.anchorMax = new Vector2(0.8f, 0.8f);
            numRect.offsetMin = numRect.offsetMax = Vector2.zero;
            var numText = numGo.AddComponent<Text>();
            numText.text = "1";
            numText.fontSize = 42;
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
            checkGo.SetActive(false);

            var timeGo = new GameObject("BestTime");
            timeGo.transform.SetParent(root.transform, false);
            var timeRect = timeGo.AddComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0.1f, 0.05f);
            timeRect.anchorMax = new Vector2(0.9f, 0.35f);
            timeRect.offsetMin = timeRect.offsetMax = Vector2.zero;
            var timeText = timeGo.AddComponent<Text>();
            timeText.text = "";
            timeText.fontSize = 24;
            timeText.alignment = TextAnchor.MiddleCenter;

            var cell = root.AddComponent<LevelSelectCell>();
            cell.AssignReferencesFromChildren();
            return root;
        }

        private void RefreshAllCells(int maxLevels)
        {
            int unlocked = LevelRecords.LastUnlockedLevelId(maxLevels);
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell == null) continue;
                int levelId = i + 1;
                bool isLocked = levelId > unlocked;
                bool isCleared = LevelRecords.IsCleared(levelId);
                float bestTime = LevelRecords.GetBestTime(levelId);
                cell.OnClicked = () => OnLevelClicked(levelId);
                cell.Setup(levelId, isLocked, isCleared, bestTime);
            }
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
            _router?.StartLevel(levelId);
        }
    }

    /// <summary>그리드 셀 하나. 레벨 번호, 잠금, 클리어 표시.</summary>
    public class LevelSelectCell : MonoBehaviour
    {
        [SerializeField] private Text numberText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject clearedCheckmark;
        [SerializeField] private Text bestTimeText;
        [SerializeField] private Button button;

        public System.Action OnClicked;

        /// <summary>프리팹에 스크립트 참조가 깨졌을 때 런타임에 자식으로부터 참조 복구.</summary>
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
}
