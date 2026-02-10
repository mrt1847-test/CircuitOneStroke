using UnityEngine;

namespace CircuitOneStroke.Core
{
    /// <summary>레벨별 클리어·최단 시간·퍼펙트 여부를 PlayerPrefs에 저장.</summary>
    public static class LevelRecords
    {
        private const string PrefixClear = "CircuitOneStroke_Clear_";
        private const string PrefixTime = "CircuitOneStroke_Time_";
        private const string PrefixPerfect = "CircuitOneStroke_Perfect_";
        private const string KeyLastPlayed = "CircuitOneStroke_LastPlayedLevel";

        /// <summary>해당 레벨 클리어 여부.</summary>
        public static bool IsCleared(int levelId)
        {
            return PlayerPrefs.GetInt(PrefixClear + levelId, 0) != 0;
        }

        /// <summary>레벨 클리어로 기록.</summary>
        public static void SetCleared(int levelId)
        {
            PlayerPrefs.SetInt(PrefixClear + levelId, 1);
            PlayerPrefs.Save();
        }

        /// <summary>저장된 최단 클리어 시간. 없으면 float.MaxValue.</summary>
        public static float GetBestTime(int levelId)
        {
            return PlayerPrefs.GetFloat(PrefixTime + levelId, float.MaxValue);
        }

        /// <summary>이번 시간이 기존보다 짧을 때만 갱신.</summary>
        public static void SetBestTime(int levelId, float time)
        {
            float prev = GetBestTime(levelId);
            if (time < prev)
                PlayerPrefs.SetFloat(PrefixTime + levelId, time);
            PlayerPrefs.Save();
        }

        /// <summary>퍼펙트(추가 조건) 달성 여부. 확장용.</summary>
        public static bool IsPerfect(int levelId)
        {
            return PlayerPrefs.GetInt(PrefixPerfect + levelId, 0) != 0;
        }

        /// <summary>퍼펙트 달성으로 기록.</summary>
        public static void SetPerfect(int levelId)
        {
            PlayerPrefs.SetInt(PrefixPerfect + levelId, 1);
            PlayerPrefs.Save();
        }

        /// <summary>마지막 플레이한 레벨 ID. Continue용.</summary>
        public static int LastPlayedLevelId
        {
            get => PlayerPrefs.GetInt(KeyLastPlayed, 1);
            set { PlayerPrefs.SetInt(KeyLastPlayed, value); PlayerPrefs.Save(); }
        }

        /// <summary>마지막으로 언락된 레벨 (클리어한 최대 ID + 1).</summary>
        public static int LastUnlockedLevelId(int maxLevelCount)
        {
            for (int i = 1; i <= maxLevelCount; i++)
                if (!IsCleared(i)) return i;
            return maxLevelCount;
        }
    }
}
