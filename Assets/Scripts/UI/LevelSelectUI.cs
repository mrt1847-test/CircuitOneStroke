using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// Minimal level select: prev/next and load. If LevelManifest is set, levels are taken from manifest; otherwise from Resources (Levels/Level_N).
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private LevelManifest levelManifest;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text levelLabel;
        [SerializeField] private int minLevelId = 1;
        [SerializeField] private int maxLevelId = 20;

        private int _currentLevelId = 1;

        /// <summary>Manifest 사용 시 1, 아니면 minLevelId.</summary>
        private int EffectiveMin => levelManifest != null ? 1 : minLevelId;
        /// <summary>Manifest 사용 시 manifest 개수, 아니면 maxLevelId.</summary>
        private int EffectiveMax => levelManifest != null ? Mathf.Max(1, levelManifest.Count) : maxLevelId;

        private void Start()
        {
            if (levelLoader != null && levelLoader.LevelData != null)
                _currentLevelId = levelLoader.LevelData.levelId;
            _currentLevelId = Mathf.Clamp(_currentLevelId, EffectiveMin, EffectiveMax);

            if (prevButton != null)
                prevButton.onClick.AddListener(OnPrev);
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNext);
            Refresh();
        }

        private void OnPrev()
        {
            _currentLevelId = Mathf.Max(EffectiveMin, _currentLevelId - 1);
            LoadAndRefresh();
        }

        private void OnNext()
        {
            _currentLevelId = Mathf.Min(EffectiveMax, _currentLevelId + 1);
            LoadAndRefresh();
        }

        private void LoadAndRefresh()
        {
            if (levelLoader == null) { Refresh(); return; }
            if (levelManifest != null)
            {
                LevelData data = levelManifest.GetLevel(_currentLevelId - 1);
                if (data != null)
                    levelLoader.LoadLevel(data);
            }
            else
                levelLoader.LoadLevel(_currentLevelId);
            Refresh();
        }

        private void Refresh()
        {
            if (levelLabel != null)
                levelLabel.text = $"Level {_currentLevelId}";
            if (prevButton != null)
                prevButton.interactable = _currentLevelId > EffectiveMin;
            if (nextButton != null)
                nextButton.interactable = _currentLevelId < EffectiveMax;
        }
    }
}
