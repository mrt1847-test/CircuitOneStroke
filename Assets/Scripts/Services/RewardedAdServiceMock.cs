using System;
using System.Collections;
using UnityEngine;

namespace CircuitOneStroke.Services
{
    /// <summary>
    /// 에디터/개발용 리워드 광고 목(mock). 1초 후 성공 시뮬레이션.
    /// 실제 AdMob/Unity Ads로 교체 가능.
    /// </summary>
    public class RewardedAdServiceMock : MonoBehaviour, IRewardedAdService
    {
        [SerializeField] private float simulateDelaySeconds = 1f;

        public bool IsReady => true;

        public void Show(Action onRewarded, Action onClosed, Action<string> onFailed)
        {
            StartCoroutine(SimulateShow(onRewarded, onClosed, onFailed));
        }

        private IEnumerator SimulateShow(Action onRewarded, Action onClosed, Action<string> onFailed)
        {
            yield return new WaitForSeconds(simulateDelaySeconds);
            onRewarded?.Invoke();
            onClosed?.Invoke();
        }
    }
}
