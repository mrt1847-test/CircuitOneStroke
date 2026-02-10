using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 홈 화면. Continue/Play, Level Select, Settings, Shop.
    /// </summary>
    public class HomeScreen : MonoBehaviour, IUIScreen
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private HeartBar heartBar;

        private UIScreenRouter _router;

        public void BindRouter(UIScreenRouter router)
        {
            _router = router;
        }

        private void Start()
        {
            if (titleText != null)
                titleText.text = "Circuit One-Stroke";

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinue);
            if (levelSelectButton != null)
                levelSelectButton.onClick.AddListener(OnLevelSelect);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettings);
            if (shopButton != null)
                shopButton.onClick.AddListener(OnShop);

            RefreshHearts();
            HeartsManager.Instance.OnHeartsChanged += OnHeartsChanged;
        }

        private void OnDestroy()
        {
            HeartsManager.Instance.OnHeartsChanged -= OnHeartsChanged;
        }

        private void OnEnable()
        {
            RefreshHearts();
        }

        private void OnHeartsChanged(int _) => RefreshHearts();

        private void RefreshHearts()
        {
            if (heartBar != null)
                heartBar.SetHearts(HeartsManager.Instance.Hearts, HeartsManager.Instance.MaxHearts);
        }

        private void OnContinue()
        {
            if (_router == null) return;
            int last = LevelRecords.LastPlayedLevelId;
            int max = 20;
            var manifest = _router.LevelManifest;
            if (manifest != null) max = manifest.Count;
            int levelId = last > 0 ? Mathf.Clamp(last, 1, Mathf.Max(1, max)) : LevelRecords.LastUnlockedLevelId(max);
            if (levelId <= 0) levelId = 1;
            _router.StartLevel(levelId);
        }

        private void OnLevelSelect()
        {
            _router?.ShowLevelSelect();
        }

        private void OnSettings()
        {
            _router?.ShowSettings();
        }

        private void OnShop()
        {
            _router?.ShowShop();
        }
    }
}
