#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Services;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 디버그 패널. Editor/Development Build에서만 컴파일·표시.
    /// Release 빌드에서는 이 스크립트가 제거되므로 씬에 DebugPanel을 넣지 않거나 비활성화 권장.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        [SerializeField] private Toggle noAdsToggle;
        [SerializeField] private Text statusText;
        [SerializeField] private Button simulateLevelClearButton;
        [SerializeField] private Button setHeartsZeroButton;
        [SerializeField] private Button simulateHeavyLoadButton;

        private void Start()
        {
            RefreshStatus();

            if (noAdsToggle != null)
            {
                noAdsToggle.isOn = PurchaseEntitlements.Instance.HasNoAds;
                noAdsToggle.onValueChanged.AddListener(OnNoAdsToggleChanged);
            }

            if (simulateLevelClearButton != null)
                simulateLevelClearButton.onClick.AddListener(OnSimulateLevelClear);

            if (setHeartsZeroButton != null)
                setHeartsZeroButton.onClick.AddListener(OnSetHeartsZero);
            if (simulateHeavyLoadButton != null)
                simulateHeavyLoadButton.onClick.AddListener(OnSimulateHeavyLoad);
        }

        private void OnNoAdsToggleChanged(bool isOn)
        {
            PurchaseEntitlements.Instance.SetNoAds(isOn);
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (statusText != null)
            {
                var ent = PurchaseEntitlements.Instance;
                int hearts = HeartsManager.Instance.Hearts;
                int clears = InterstitialTracker.Instance.LevelsClearedSinceLastInterstitial;
                statusText.text = $"HasNoAds: {ent.HasNoAds}\nHearts: {hearts}/5\nClears since interstitial: {clears}";
            }
        }

        private void OnSimulateLevelClear()
        {
            InterstitialTracker.Instance.IncrementOnLevelClear();
            RefreshStatus();
        }

        private void OnSetHeartsZero()
        {
            HeartsManager.Instance.SetHearts(0);
            RefreshStatus();
        }

        private void OnSimulateHeavyLoad()
        {
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.RunTransition(SimulateHeavyLoadCoroutine());
            }
        }

        private IEnumerator SimulateHeavyLoadCoroutine()
        {
            yield return new WaitForSecondsRealtime(1f);
        }

        private void Update()
        {
            if (noAdsToggle != null && noAdsToggle.isOn != PurchaseEntitlements.Instance.HasNoAds)
                noAdsToggle.isOn = PurchaseEntitlements.Instance.HasNoAds;
            RefreshStatus();
        }
    }
}

#endif
