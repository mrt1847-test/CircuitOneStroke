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
            if (!AdDecisionService.Instance.CanShow(AdPlacement.Rewarded_HeartsRefill, userInitiated: true, 0))
            {
                HeartsManager.Instance.RefillFull();
                _router?.GoBack();
                return;
            }
            var service = adServiceComponent as IAdService ?? FindObjectOfType<AdServiceMock>();
            if (service == null || !service.IsRewardedReady(AdPlacement.Rewarded_HeartsRefill))
            {
                HeartsManager.Instance.RefillFull();
                _router?.GoBack();
                return;
            }
            service.ShowRewarded(
                AdPlacement.Rewarded_HeartsRefill,
                onRewarded: () =>
                {
                    HeartsManager.Instance.RefillFull();
                    AdDecisionService.Instance.RecordShown(AdPlacement.Rewarded_HeartsRefill);
                },
                onClosed: () => _router?.GoBack(),
                onFailed: _ => _router?.GoBack()
            );
        }

        private void OnBackClicked()
        {
            _router?.GoBack();
        }
    }
}
