using System;
using UnityEngine;
using CircuitOneStroke.Core;
using CircuitOneStroke.Services;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// "Watch Ad to Refill Hearts" ?ë¦„????ê³³ì—??ì²˜ë¦¬?©ë‹ˆ??
    /// GameHUD?€ OutOfHeartsScreen?ì„œ ì¤‘ë³µ?˜ë˜ ë¡œì§??ê³µí†µ?”í•©?ˆë‹¤.
    /// </summary>
    public static class HeartsRefillAdFlow
    {
        /// <summary>
        /// ë¦¬ì›Œ??ê´‘ê³ ë¡??˜íŠ¸ ë¦¬í•„ ?œë„.
        /// </summary>
        /// <param name="levelIndex">ê´‘ê³  ?˜ì‚¬ê²°ì •???ˆë²¨ ?¸ë±??(0-based).</param>
        /// <param name="adServiceComponent">?¬ì— ë°°ì¹˜??ê´‘ê³  ì»´í¬?ŒíŠ¸ (? íƒ).</param>
        /// <param name="onSuccess">ë¦¬í•„ ?±ê³µ ?ëŠ” DevBypass ??(ê²Œì„ ?¬ê°œ/?”ë©´ ?„í™˜ ?¸ì¶œ).</param>
        /// <param name="onClosedOrFailed">ê´‘ê³  ?œì‹œ ë¶ˆê?/?¤íŒ¨/?«ê¸° ??(?´ë°± ?”ë©´ ?„í™˜ ??.</param>
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
                    onSuccess?.Invoke();
                    return;
                }
                else
                    GameFeedback.RequestToast("ê´‘ê³ ë¥?ë¶ˆëŸ¬?¤ì? ëª»í–ˆ?µë‹ˆ?? ? ì‹œ ???¤ì‹œ ?œë„");
                onClosedOrFailed?.Invoke();
                return;
            }

            var service = UIServices.GetAdService(adServiceComponent);
            if (service == null || !service.IsRewardedReady(AdPlacement.Rewarded_HeartsRefill))
            {
                if (GameSettings.DevBypassRewardedOnUnavailable)
                {
                    HeartsManager.Instance.RefillFull();
                    onSuccess?.Invoke();
                    return;
                }
                else
                    GameFeedback.RequestToast("ê´‘ê³ ë¥?ë¶ˆëŸ¬?¤ì? ëª»í–ˆ?µë‹ˆ?? ? ì‹œ ???¤ì‹œ ?œë„");
                onClosedOrFailed?.Invoke();
                return;
            }

            bool rewarded = false;
            bool finished = false;

            void FinishSuccess()
            {
                if (finished) return;
                finished = true;
                onSuccess?.Invoke();
            }

            void FinishFail()
            {
                if (finished) return;
                finished = true;
                onClosedOrFailed?.Invoke();
            }

            service.ShowRewarded(
                AdPlacement.Rewarded_HeartsRefill,
                onRewarded: () =>
                {
                    rewarded = true;
                    HeartsManager.Instance?.RefillFull();
                    AdDecisionService.Instance?.RecordShown(AdPlacement.Rewarded_HeartsRefill);
                    FinishSuccess();
                },
                onClosed: () =>
                {
                    if (finished) return;
                    if (!rewarded)
                    {
                        // Some ad SDK wrappers only report close. Keep playability by granting on close fallback.
                        HeartsManager.Instance?.RefillFull();
                    }
                    FinishSuccess();
                },
                onFailed: _ =>
                {
                    GameFeedback.RequestToast("ê´‘ê³ ë¥?ë¶ˆëŸ¬?¤ì? ëª»í–ˆ?µë‹ˆ?? ? ì‹œ ???¤ì‹œ ?œë„??ì£¼ì„¸??");
                    FinishFail();
                }
            );
        }
    }
}

