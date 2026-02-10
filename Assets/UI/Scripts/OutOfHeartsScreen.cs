using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 하트 0일 때 표시되는 전체 화면. Watch Ad + Back to Home.
    /// </summary>
    public class OutOfHeartsScreen : MonoBehaviour, IUIScreen
    {
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button backButton;
        [SerializeField] private MonoBehaviour adServiceComponent;

        private UIScreenRouter _router;

        public void BindRouter(UIScreenRouter router)
        {
            _router = router;
        }

        private void Start()
        {
            if (watchAdButton != null)
                watchAdButton.onClick.AddListener(OnWatchAdClicked);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnWatchAdClicked()
        {
            int levelIndex = Mathf.Max(0, LevelRecords.LastPlayedLevelId - 1);
            void leaveScreen() => _router?.GoBack();
            HeartsRefillAdFlow.Run(levelIndex, adServiceComponent, leaveScreen, leaveScreen);
        }

        private void OnBackClicked()
        {
            _router?.GoBack();
        }
    }
}
