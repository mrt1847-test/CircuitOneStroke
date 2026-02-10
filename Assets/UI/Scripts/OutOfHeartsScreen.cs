using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Services;

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
            if (!AdDecisionService.Instance.CanShow(AdPlacement.Rewarded_HeartsRefill, userInitiated: true, levelIndex))
            {
                if (GameSettings.DevBypassRewardedOnUnavailable)
                {
                    HeartsManager.Instance.RefillFull();
                    (GameFlowController.Instance ?? FindObjectOfType<GameFlowController>())?.ResumeLastIntent();
                }
                else
                    GameFeedback.RequestToast("광고를 불러오지 못했습니다. 잠시 후 다시 시도");
                return;
            }
            var service = AdServiceRegistry.Instance ?? adServiceComponent as IAdService ?? FindObjectOfType<AdServiceMock>();
            if (service == null || !service.IsRewardedReady(AdPlacement.Rewarded_HeartsRefill))
            {
                if (GameSettings.DevBypassRewardedOnUnavailable)
                {
                    HeartsManager.Instance.RefillFull();
                    (GameFlowController.Instance ?? FindObjectOfType<GameFlowController>())?.ResumeLastIntent();
                }
                else
                    GameFeedback.RequestToast("광고를 불러오지 못했습니다. 잠시 후 다시 시도");
                return;
            }
            service.ShowRewarded(
                AdPlacement.Rewarded_HeartsRefill,
                onRewarded: () =>
                {
                    HeartsManager.Instance.RefillFull();
                    AdDecisionService.Instance.RecordShown(AdPlacement.Rewarded_HeartsRefill);
                    var flow = GameFlowController.Instance ?? FindObjectOfType<GameFlowController>();
                    if (flow != null)
                        flow.ResumeLastIntent();
                    else
                        _router?.GoBack();
                },
                onClosed: () =>
                {
                    var flow = GameFlowController.Instance ?? FindObjectOfType<GameFlowController>();
                    if (flow != null)
                        flow.ResumeLastIntent();
                    else
                        _router?.GoBack();
                },
                onFailed: _ =>
                {
                    var flow = GameFlowController.Instance ?? FindObjectOfType<GameFlowController>();
                    if (flow != null)
                        flow.ResumeLastIntent();
                    else
                        _router?.GoBack();
                }
            );
        }

        private void OnBackClicked()
        {
            _router?.GoBack();
        }
    }
}
