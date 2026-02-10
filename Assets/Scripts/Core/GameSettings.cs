using UnityEngine;

namespace CircuitOneStroke.Core
{
    /// <summary>이동 불가 시 동작. RejectOnly=진입만 막음, ImmediateFail=스트로크 즉시 실패.</summary>
    public enum FailFeedbackMode
    {
        RejectOnly,
        ImmediateFail
    }

    /// <summary>PlayerPrefs 기반 설정. 소리·진동·Fail 모드.</summary>
    public static class GameSettings
    {
        private const string KeyFailMode = "CircuitOneStroke_FailMode";
        private const string KeySound = "CircuitOneStroke_Sound";
        private const string KeyVibrate = "CircuitOneStroke_Vibrate";

        public static FailFeedbackMode FailMode
        {
            get => (FailFeedbackMode)PlayerPrefs.GetInt(KeyFailMode, (int)FailFeedbackMode.RejectOnly);
            set => PlayerPrefs.SetInt(KeyFailMode, (int)value);
        }

        public static bool SoundEnabled
        {
            get => PlayerPrefs.GetInt(KeySound, 1) != 0;
            set => PlayerPrefs.SetInt(KeySound, value ? 1 : 0);
        }

        public static bool VibrateEnabled
        {
            get => PlayerPrefs.GetInt(KeyVibrate, 1) != 0;
            set => PlayerPrefs.SetInt(KeyVibrate, value ? 1 : 0);
        }

        /// <summary>변경 후 호출 권장. PlayerPrefs.Save().</summary>
        public static void Save() => PlayerPrefs.Save();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>광고 미준비 시 보상 무조건 지급 허용. 프로덕션에서는 false.</summary>
        public static bool DevBypassRewardedOnUnavailable => true;
#else
        public static bool DevBypassRewardedOnUnavailable => false;
#endif
    }
}
