using UnityEngine;

namespace CircuitOneStroke.Services
{
    /// <summary>
    /// N클리어마다 인터스티셜 표시를 위한 클리어 횟수 추적.
    /// 실패 시 backoff로 재시도 폭주 방지.
    /// </summary>
    public class InterstitialTracker
    {
        public static InterstitialTracker Instance { get; } = new InterstitialTracker();

        /// <summary>실패 후 재시도 전 대기 시간(초). 실패 N회 시 N * backoff 적용.</summary>
        private const float FailureBackoffSeconds = 30f;

        public int LevelsClearedSinceLastInterstitial { get; private set; }

        private float _lastAttemptTime;
        private int _failureCount;

        public void IncrementOnLevelClear()
        {
            LevelsClearedSinceLastInterstitial++;
        }

        public void ResetAfterInterstitialShown()
        {
            LevelsClearedSinceLastInterstitial = 0;
            _failureCount = 0;
        }

        /// <summary>인터스티셜 표시 시도 가능 여부. 실패 후 backoff 이내면 false.</summary>
        public bool CanAttemptInterstitial()
        {
            if (_failureCount == 0) return true;
            float backoff = FailureBackoffSeconds * _failureCount;
            return Time.realtimeSinceStartup - _lastAttemptTime >= backoff;
        }

        /// <summary>인터스티셜 표시 실패 시 호출. 재시도 지연 적용.</summary>
        public void RecordInterstitialFailure()
        {
            _failureCount++;
            _lastAttemptTime = Time.realtimeSinceStartup;
        }
    }
}
