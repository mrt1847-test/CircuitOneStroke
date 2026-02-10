using UnityEngine;

namespace CircuitOneStroke.Services
{
    /// <summary>광고 유형.</summary>
    public enum AdType
    {
        Interstitial,
        Rewarded,
        Banner
    }

    /// <summary>
    /// 배치별 광고 설정. ScriptableObject 또는 정적 기본값.
    /// </summary>
    [CreateAssetMenu(fileName = "AdPlacementConfig", menuName = "Circuit One-Stroke/Ad Placement Config")]
    public class AdPlacementConfig : ScriptableObject
    {
        [System.Serializable]
        public class PlacementEntry
        {
            public AdPlacement placement;
            public AdType adType;
            [Tooltip("NoAds 구매 시 이 배치 광고 비표시")]
            public bool removableByNoAds = true;
            [Tooltip("버튼 클릭 등 사용자 명시적 액션에서만 표시")]
            public bool requiresUserOptIn = false;
            [Tooltip("동일 배치 재표시 전 쿨다운(초)")]
            public float cooldownSeconds = 0f;
            [Tooltip("최소 레벨 인덱스(0-based). 그 미만 레벨에서는 표시 안 함")]
            public int minLevelIndex = 0;
            [Tooltip("인터스티셜 Every N Clears용. N 클리어마다 표시")]
            public int frequencyN = 3;
        }

        [SerializeField] private PlacementEntry[] entries;

        private static AdPlacementConfig _instance;
        public static AdPlacementConfig Instance => _instance != null ? _instance : _instance = Resources.Load<AdPlacementConfig>("AdPlacementConfig");

        public PlacementEntry GetConfig(AdPlacement placement)
        {
            if (entries != null)
            {
                foreach (var e in entries)
                    if (e.placement == placement) return e;
            }
            return GetDefaultConfig(placement);
        }

        public static PlacementEntry GetDefaultConfig(AdPlacement placement)
        {
            return placement switch
            {
                AdPlacement.Interstitial_EveryNClears => new PlacementEntry
                {
                    placement = placement,
                    adType = AdType.Interstitial,
                    removableByNoAds = true,
                    requiresUserOptIn = false,
                    cooldownSeconds = 0f,
                    minLevelIndex = 0,
                    frequencyN = 3
                },
                AdPlacement.Rewarded_HeartsRefill => new PlacementEntry
                {
                    placement = placement,
                    adType = AdType.Rewarded,
                    removableByNoAds = false,
                    requiresUserOptIn = true,
                    cooldownSeconds = 0f,
                    minLevelIndex = 0,
                    frequencyN = 0
                },
                _ => new PlacementEntry { placement = placement, adType = AdType.Interstitial }
            };
        }
    }
}
