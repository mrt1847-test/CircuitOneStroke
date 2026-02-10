using UnityEngine;

namespace CircuitOneStroke.Services
{
    /// <summary>
    /// 광고 표시 여부 결정. NoAds, requiresUserOptIn, 쿨다운, minLevel 적용.
    /// </summary>
    public class AdDecisionService
    {
        public static AdDecisionService Instance { get; } = new AdDecisionService();

        private readonly System.Collections.Generic.Dictionary<AdPlacement, float> _lastShowTime = new System.Collections.Generic.Dictionary<AdPlacement, float>();

        /// <summary>
        /// placement 광고를 표시해도 되는지.
        /// requiresUserOptIn 배치는 userInitiated=true(버튼 클릭)에서만 true 반환.
        /// </summary>
        /// <param name="placement">광고 배치</param>
        /// <param name="userInitiated">사용자가 버튼 클릭 등으로 명시적으로 요청한 경우 true</param>
        /// <param name="currentLevelIndex">현재 레벨 인덱스(0-based)</param>
        public bool CanShow(AdPlacement placement, bool userInitiated, int currentLevelIndex = 0)
        {
            var config = AdPlacementConfig.Instance != null
                ? AdPlacementConfig.Instance.GetConfig(placement)
                : AdPlacementConfig.GetDefaultConfig(placement);

            // 1) removableByNoAds && HasNoAds => false
            if (config.removableByNoAds && PurchaseEntitlements.Instance.HasNoAds)
                return false;

            // 2) requiresUserOptIn && !userInitiated => false
            if (config.requiresUserOptIn && !userInitiated)
                return false;

            // 3) Cooldown
            if (config.cooldownSeconds > 0 && _lastShowTime.TryGetValue(placement, out var last) &&
                Time.time - last < config.cooldownSeconds)
                return false;

            // 4) minLevelIndex
            if (currentLevelIndex < config.minLevelIndex)
                return false;

            return true;
        }

        /// <summary>광고 표시 완료 시 호출. 쿨다운 기록.</summary>
        public void RecordShown(AdPlacement placement)
        {
            _lastShowTime[placement] = Time.time;
        }
    }
}
