using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;

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

            BuildGrid();
        }

        private void OnEnable()
        {
            BuildGrid();
        }

        private void OnBack()
        {
            _router?.ShowHome();
        }

        private void BuildGrid()
        {
            if (gridContainer == null || levelCellPrefab == null) return;

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
                var cellGo = Instantiate(levelCellPrefab, gridContainer);
                var cell = cellGo.GetComponent<LevelSelectCell>();
                if (cell == null)
                    cell = cellGo.AddComponent<LevelSelectCell>();
                _cells.Add(cell);
            }
            RefreshAllCells(maxLevels);
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
