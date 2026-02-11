using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Services;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// NoAds 구매 UI. IAP 연동 시 PurchaseEntitlements.SetHasNoAds(true) 호출.
    /// Remove Forced Ads: 인터스티셜 제거. 리워드 광고(하트 충전)는 옵션으로 유지.
    /// </summary>
    public class ShopPanel : MonoBehaviour, IUIScreen
    {
        [SerializeField] private Button noAdsPurchaseButton;
        [SerializeField] private Text noAdsDescriptionText;
        [SerializeField] private Button backButton;

        private UIScreenRouter _router;

        public void BindRouter(UIScreenRouter router)
        {
            _router = router;
        }

        private void Start()
        {
            if (noAdsDescriptionText != null)
                noAdsDescriptionText.text = IAPCopyConstants.NoAdsProductDesc;

            if (noAdsPurchaseButton != null)
                noAdsPurchaseButton.onClick.AddListener(OnNoAdsPurchaseClicked);
            if (backButton != null)
                backButton.onClick.AddListener(() =>
                {
                    if (AppRouter.Instance != null) AppRouter.Instance.ShowTab(MainTab.Home);
                    else _router?.GoBack();
                });
        }

        private void OnNoAdsPurchaseClicked()
        {
            // TODO: IAP 흐름 호출. 성공 시 GrantNoAds() 호출.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (PurchaseEntitlements.Instance != null)
                PurchaseEntitlements.Instance.GrantNoAds();
#endif
        }
    }
}
