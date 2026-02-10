using System;
using System.Collections;
using UnityEngine;

namespace CircuitOneStroke.Services
{
    /// <summary>
    /// 에디터/개발용 광고 Mock. 인터스티셜·리워드 모두 시뮬레이션.
    /// Awake에서 AdServiceRegistry에 자동 등록.
    /// </summary>
    public class AdServiceMock : MonoBehaviour, IAdService
    {
        [SerializeField] private float simulateDelaySeconds = 1f;

        private void Awake()
        {
            AdServiceRegistry.Instance = this;
        }

        public bool IsInterstitialReady(AdPlacement placement) => true;
        public bool IsRewardedReady(AdPlacement placement) => true;

        public void ShowInterstitial(AdPlacement placement, Action onClosed, Action<string> onFailed)
        {
            StartCoroutine(SimulateInterstitial(onClosed));
        }

        public void ShowRewarded(AdPlacement placement, Action onRewarded, Action onClosed, Action<string> onFailed)
        {
            StartCoroutine(SimulateRewarded(onRewarded, onClosed));
        }

        private IEnumerator SimulateInterstitial(Action onClosed)
        {
            yield return new WaitForSeconds(simulateDelaySeconds);
            onClosed?.Invoke();
        }

        private IEnumerator SimulateRewarded(Action onRewarded, Action onClosed)
        {
            yield return new WaitForSeconds(simulateDelaySeconds);
            onRewarded?.Invoke();
            onClosed?.Invoke();
        }
    }
}
