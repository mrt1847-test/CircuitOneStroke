using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI
{
    public class LevelSelectUI : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text levelLabel;
        [SerializeField] private int minLevelId = 1;
        [SerializeField] private int maxLevelId = 20;

        private int _currentLevelId = 1;

        private void Start()
        {
            if (levelLoader != null && levelLoader.LevelData != null)
                _currentLevelId = levelLoader.LevelData.levelId;

            if (prevButton != null)
                prevButton.onClick.AddListener(OnPrev);
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNext);
            Refresh();
        }

        private void OnPrev()
        {
            _currentLevelId = Mathf.Max(minLevelId, _currentLevelId - 1);
            LoadAndRefresh();
        }

        private void OnNext()
        {
            _currentLevelId = Mathf.Min(maxLevelId, _currentLevelId + 1);
            LoadAndRefresh();
        }

        private void LoadAndRefresh()
        {
            if (levelLoader != null)
                levelLoader.LoadLevel(_currentLevelId);
            Refresh();
        }

        private void Refresh()
        {
            if (levelLabel != null)
                levelLabel.text = $"Level {_currentLevelId}";
            if (prevButton != null)
                prevButton.interactable = _currentLevelId > minLevelId;
            if (nextButton != null)
                nextButton.interactable = _currentLevelId < maxLevelId;
        }
    }
}
