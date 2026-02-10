using UnityEngine;

namespace CircuitOneStroke.Core
{
    public enum FailFeedbackMode
    {
        RejectOnly,
        ImmediateFail
    }

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

        public static void Save() => PlayerPrefs.Save();
    }
}
