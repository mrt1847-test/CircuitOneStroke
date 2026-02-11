using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// MainShell Home tab: Level selection grid + Continue button. Calls AppRouter.RequestStartLevel.
    /// </summary>
    public class HomeTabView : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private LevelSelectScreen levelSelectScreen;

        private void Start()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinue);
            if (levelSelectScreen != null)
                levelSelectScreen.BindRouter(null); // Will use AppRouter for level clicks
        }

        private void OnContinue()
        {
            if (AppRouter.Instance == null) return;
            int last = LevelRecords.LastPlayedLevelId;
            int max = 20;
            var manifest = Resources.Load<LevelManifest>("Levels/GeneratedLevelManifest");
            if (manifest != null) max = manifest.Count;
            int levelId = last > 0 ? Mathf.Clamp(last, 1, Mathf.Max(1, max)) : LevelRecords.LastUnlockedLevelId(max);
            if (levelId <= 0) levelId = 1;
            AppRouter.Instance.RequestStartLevel(levelId);
        }
    }
}
