namespace CircuitOneStroke.Services
{
    /// <summary>
    /// 광고 배치 유형. removableByNoAds/requiresUserOptIn은 AdPlacementConfig에서 정의.
    /// </summary>
    public enum AdPlacement
    {
        /// <summary>N클리어마다 강제 인터스티셜. NoAds 구매 시 제거됨.</summary>
        Interstitial_EveryNClears,
        /// <summary>하트 리필용 리워드 광고. 버튼 클릭 시에만. NoAds로 제거 불가.</summary>
        Rewarded_HeartsRefill
        // Future: Banner_Home, Interstitial_LevelStart
    }
}
