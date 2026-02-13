using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// MainShell Home tab: level map view + Continue button. Calls AppRouter.RequestStartLevel.
    /// </summary>
    public class HomeTabView : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private LevelSelectScreen levelSelectScreen;
        private bool _continueBound;

        private void Start()
        {
            ResolveContinueButton();
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinue);
                _continueBound = true;
            }
            if (levelSelectScreen != null)
                levelSelectScreen.BindRouter(null); // Will use AppRouter for level clicks
        }

        private void OnDestroy()
        {
            if (_continueBound && continueButton != null)
                continueButton.onClick.RemoveListener(OnContinue);
        }

        private void ResolveContinueButton()
        {
            if (continueButton != null) return;

            var direct = transform.Find("ContinueButton");
            if (direct != null)
                continueButton = direct.GetComponent<Button>();
            if (continueButton != null) return;

            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (btn == null) continue;
                if (btn.GetComponentInParent<LevelSelectScreen>(true) != null)
                    continue;
                var txt = btn.GetComponentInChildren<Text>(true);
                var value = txt != null ? txt.text : string.Empty;
                if (string.IsNullOrWhiteSpace(value)) continue;
                var normalized = value.Trim().ToUpperInvariant();
                if (normalized == "CONTINUE" || normalized == "CONTINUE / PLAY")
                {
                    continueButton = btn;
                    return;
                }
            }
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
