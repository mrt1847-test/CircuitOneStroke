using System;

namespace CircuitOneStroke.Services
{
    /// <summary>
    /// 인터스티셜·리워드 광고 통합 인터페이스. AdMob/Unity Ads 등 교체 가능.
    /// </summary>
    public interface IAdService
    {
        bool IsInterstitialReady(AdPlacement placement);
        bool IsRewardedReady(AdPlacement placement);

        void ShowInterstitial(AdPlacement placement, Action onClosed, Action<string> onFailed);
        void ShowRewarded(AdPlacement placement, Action onRewarded, Action onClosed, Action<string> onFailed);
    }
}
