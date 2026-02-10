using UnityEngine;

namespace CircuitOneStroke.Core
{
    public static class LevelRecords
    {
        private const string PrefixClear = "CircuitOneStroke_Clear_";
        private const string PrefixTime = "CircuitOneStroke_Time_";
        private const string PrefixPerfect = "CircuitOneStroke_Perfect_";

        public static bool IsCleared(int levelId)
        {
            return PlayerPrefs.GetInt(PrefixClear + levelId, 0) != 0;
        }

        public static void SetCleared(int levelId)
        {
            PlayerPrefs.SetInt(PrefixClear + levelId, 1);
            PlayerPrefs.Save();
        }

        public static float GetBestTime(int levelId)
        {
            return PlayerPrefs.GetFloat(PrefixTime + levelId, float.MaxValue);
        }

        public static void SetBestTime(int levelId, float time)
        {
            float prev = GetBestTime(levelId);
            if (time < prev)
                PlayerPrefs.SetFloat(PrefixTime + levelId, time);
            PlayerPrefs.Save();
        }

        public static bool IsPerfect(int levelId)
        {
            return PlayerPrefs.GetInt(PrefixPerfect + levelId, 0) != 0;
        }

        public static void SetPerfect(int levelId)
        {
            PlayerPrefs.SetInt(PrefixPerfect + levelId, 1);
            PlayerPrefs.Save();
        }
    }
}
