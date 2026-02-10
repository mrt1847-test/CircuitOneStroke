using UnityEngine;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// 햅틱 피드백. GameSettings.hapticsEnabled + strength 반영.
    /// Reject/Fail/Win 시 호출.
    /// </summary>
    public class HapticsManager : MonoBehaviour
    {
        public static HapticsManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>이동 불가(리젝트) 시 짧은 진동.</summary>
        public void PulseReject()
        {
            if (!GameSettings.Instance.Data.hapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            var strength = GameSettings.Instance.HapticsStrengthValue;
            float ms = strength == HapticsStrength.Light ? 20 : 40;
            Handheld.Vibrate();
#endif
        }

        /// <summary>실패(재방문 등) 시 진동.</summary>
        public void PulseFail()
        {
            if (!GameSettings.Instance.Data.hapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        /// <summary>클리어 시 진동.</summary>
        public void PulseWin()
        {
            if (!GameSettings.Instance.Data.hapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
