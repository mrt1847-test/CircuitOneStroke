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
        [SerializeField] private Button simulateFailButton;
        [SerializeField] private Button setHeartsZeroButton;
        [SerializeField] private Button refillHeartsButton;
        [SerializeField] private Button simulateHeavyLoadButton;

        private void Start()
        {
            RefreshStatus();

            if (noAdsToggle != null && PurchaseEntitlements.Instance != null)
            {
                noAdsToggle.isOn = PurchaseEntitlements.Instance.HasNoAds;
                noAdsToggle.onValueChanged.AddListener(OnNoAdsToggleChanged);
            }

            if (simulateLevelClearButton != null)
                simulateLevelClearButton.onClick.AddListener(OnSimulateLevelClear);
            if (simulateFailButton != null)
                simulateFailButton.onClick.AddListener(OnSimulateFail);
            if (setHeartsZeroButton != null)
                setHeartsZeroButton.onClick.AddListener(OnSetHeartsZero);
            if (refillHeartsButton != null)
                refillHeartsButton.onClick.AddListener(OnRefillHearts);
            if (simulateHeavyLoadButton != null)
                simulateHeavyLoadButton.onClick.AddListener(OnSimulateHeavyLoad);
        }

        private void OnNoAdsToggleChanged(bool isOn)
        {
            if (PurchaseEntitlements.Instance != null)
                PurchaseEntitlements.Instance.SetNoAds(isOn);
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (statusText == null) return;
            var ent = PurchaseEntitlements.Instance;
            int hearts = HeartsManager.Instance != null ? HeartsManager.Instance.Hearts : 0;
            int clears = InterstitialTracker.Instance != null ? InterstitialTracker.Instance.LevelsClearedSinceLastInterstitial : 0;
            statusText.text = ent != null
                ? $"HasNoAds: {ent.HasNoAds}\nHearts: {hearts}/5\nClears since interstitial: {clears}"
                : $"Hearts: {hearts}/5\nClears: {clears}";
        }

        private void OnSimulateLevelClear()
        {
            if (InterstitialTracker.Instance != null)
                InterstitialTracker.Instance.IncrementOnLevelClear();
            RefreshStatus();
        }

        private void OnSetHeartsZero()
        {
            if (HeartsManager.Instance != null)
                HeartsManager.Instance.SetHearts(0);
            RefreshStatus();
        }

        private void OnRefillHearts()
        {
            if (HeartsManager.Instance != null)
                HeartsManager.Instance.RefillFull();
            RefreshStatus();
        }

        private void OnSimulateFail()
        {
            var loader = FindFirstObjectByType<LevelLoader>();
            if (loader?.StateMachine != null && loader.StateMachine.State == GameState.Drawing)
                loader.StateMachine.OnHardFail("debug");
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
            if (noAdsToggle != null && PurchaseEntitlements.Instance != null && noAdsToggle.isOn != PurchaseEntitlements.Instance.HasNoAds)
                noAdsToggle.isOn = PurchaseEntitlements.Instance.HasNoAds;
            RefreshStatus();
        }
    }
}

#endif
