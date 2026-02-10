using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 레벨 선택: prev/next 및 로드. LevelManifest가 할당되면 manifest에서 레벨 사용(생성 레벨은 Resources/Levels/GeneratedLevelManifest 할당 권장). 없으면 Resources Levels/Level_N 로드.
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
            IEnumerator work = null;
            if (levelManifest != null)
            {
                var data = levelManifest.GetLevel(_currentLevelId - 1);
                if (data != null)
                    work = levelLoader.LoadLevelCoroutine(data);
            }
            if (work == null)
                work = levelLoader.LoadLevelCoroutine(_currentLevelId);

            if (work != null && TransitionManager.Instance != null)
            {
                StartCoroutine(LoadAndRefreshRoutine(work));
                return;
            }
            if (work != null)
            {
                StartCoroutine(FinishLoadAndRefresh(work));
                return;
            }
            levelLoader.LoadLevel(_currentLevelId);
            Refresh();
        }

        private IEnumerator LoadAndRefreshRoutine(IEnumerator loadWork)
        {
            yield return TransitionManager.Instance.RunTransition(loadWork);
            Refresh();
        }

        private IEnumerator FinishLoadAndRefresh(IEnumerator loadWork)
        {
            yield return loadWork;
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
