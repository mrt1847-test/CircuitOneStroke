using System;

namespace CircuitOneStroke.Services
{
    /// <summary>
    /// 리워드 광고 서비스 추상화. AdMob/Unity Ads 등 교체 가능.
    /// </summary>
    public interface IRewardedAdService
    {
        /// <summary>광고가 준비되어 표시 가능한지.</summary>
        bool IsReady { get; }

        /// <summary>
        /// 리워드 광고 표시.
        /// onRewarded: 시청 완료 시 (보상 지급 직전)
        /// onClosed: 광고 닫힘 시 (성공/실패 관계없이)
        /// onFailed: 로드/표시 실패 시 (에러 메시지)
        /// </summary>
        void Show(Action onRewarded, Action onClosed, Action<string> onFailed);
    }
}
