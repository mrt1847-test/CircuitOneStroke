using UnityEngine;

namespace CircuitOneStroke.Services
{
    /// <summary>
    /// 구매 권한. PlayerPrefs 영속화. IAP 성공 시 GrantNoAds() 호출.
    /// NoAds는 강제 광고(인터스티셜/배너)만 제거. 리워드 광고는 유지.
    /// </summary>
    public class PurchaseEntitlements
    {
        private const string KeyNoAds = "ENTITLE_NO_ADS";

        public static PurchaseEntitlements Instance { get; } = new PurchaseEntitlements();

        /// <summary>강제 광고(인터스티셜/배너) 제거 권한. 기본 false.</summary>
        public bool HasNoAds { get; private set; }

        private PurchaseEntitlements()
        {
            Load();
        }

        public void Load()
        {
            HasNoAds = PlayerPrefs.GetInt(KeyNoAds, 0) != 0;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(KeyNoAds, HasNoAds ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>디버그 토글용. Release에서는 DebugPanel에서만 호출.</summary>
        public void SetNoAds(bool enabled)
        {
            HasNoAds = enabled;
            Save();
        }

        /// <summary>IAP 성공 시 호출. NoAds 권한 부여.</summary>
        public void GrantNoAds()
        {
            HasNoAds = true;
            Save();
        }

        /// <summary>테스트/개발용. NoAds 권한 해제.</summary>
        public void RevokeNoAds()
        {
            HasNoAds = false;
            Save();
        }
    }
}
