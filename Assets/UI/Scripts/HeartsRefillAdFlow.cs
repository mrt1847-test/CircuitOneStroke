using System;
using UnityEngine;
using CircuitOneStroke.Core;
using CircuitOneStroke.Services;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// "Watch Ad to Refill Hearts" 흐름을 한 곳에서 처리합니다.
    /// GameHUD와 OutOfHeartsScreen에서 중복되던 로직을 공통화합니다.
    /// </summary>
    public static class HeartsRefillAdFlow
    {
        /// <summary>
        /// 리워드 광고로 하트 리필 시도.
        /// </summary>
        /// <param name="levelIndex">광고 의사결정용 레벨 인덱스 (0-based).</param>
        /// <param name="adServiceComponent">씬에 배치된 광고 컴포넌트 (선택).</param>
        /// <param name="onSuccess">리필 성공 또는 DevBypass 시 (게임 재개/화면 전환 호출).</param>
        /// <param name="onClosedOrFailed">광고 표시 불가/실패/닫기 시 (폴백 화면 전환 등).</param>
        public static void Run(
            int levelIndex,
            MonoBehaviour adServiceComponent,
            Action onSuccess,
            Action onClosedOrFailed)
        {
            if (AdDecisionService.Instance == null || HeartsManager.Instance == null)
            {
                onClosedOrFailed?.Invoke();
                return;
            }

            if (!AdDecisionService.Instance.CanShow(AdPlacement.Rewarded_HeartsRefill, userInitiated: true, levelIndex))
            {
                if (GameSettings.DevBypassRewardedOnUnavailable)
                {
                    HeartsManager.Instance.RefillFull();
                    UIServices.GetFlow()?.ResumeLastIntent();
                    onSuccess?.Invoke();
                }
                else
                    GameFeedback.RequestToast("광고를 불러오지 못했습니다. 잠시 후 다시 시도");
                onClosedOrFailed?.Invoke();
                return;
            }

            var service = UIServices.GetAdService(adServiceComponent);
            if (service == null || !service.IsRewardedReady(AdPlacement.Rewarded_HeartsRefill))
            {
                if (GameSettings.DevBypassRewardedOnUnavailable)
                {
                    HeartsManager.Instance.RefillFull();
                    UIServices.GetFlow()?.ResumeLastIntent();
                    onSuccess?.Invoke();
                }
                else
                    GameFeedback.RequestToast("광고를 불러오지 못했습니다. 잠시 후 다시 시도");
                onClosedOrFailed?.Invoke();
                return;
            }

            service.ShowRewarded(
                AdPlacement.Rewarded_HeartsRefill,
                onRewarded: () =>
                {
                    HeartsManager.Instance?.RefillFull();
                    AdDecisionService.Instance?.RecordShown(AdPlacement.Rewarded_HeartsRefill);
                    UIServices.GetFlow()?.ResumeLastIntent();
                    onSuccess?.Invoke();
                },
                onClosed: () =>
                {
                    UIServices.GetFlow()?.ResumeLastIntent();
                    onSuccess?.Invoke();
                },
                onFailed: _ =>
                {
                    UIServices.GetFlow()?.ResumeLastIntent();
                    onClosedOrFailed?.Invoke();
                }
            );
        }
    }
}
