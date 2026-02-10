using System;
using UnityEngine;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// 하트(생명) 시스템. Hard Fail 시 소모, 리워드 광고로 충전.
    /// PlayerPrefs로 영속화.
    /// </summary>
    public class HeartsManager
    {
        private const string KeyHeartsCurrent = "HEARTS_CURRENT";

        public static HeartsManager Instance { get; } = new HeartsManager();

        public int MaxHearts { get; } = 5;
        public int Hearts { get; private set; }

        public event Action<int> OnHeartsChanged;
        public event Action OnOutOfHearts;

        private HeartsManager()
        {
            Load();
        }

        public void Load()
        {
            if (PlayerPrefs.HasKey(KeyHeartsCurrent))
                Hearts = Mathf.Clamp(PlayerPrefs.GetInt(KeyHeartsCurrent), 0, MaxHearts);
            else
                Hearts = MaxHearts;
            Save();
        }

        public void Save()
        {
            PlayerPrefs.SetInt(KeyHeartsCurrent, Hearts);
            PlayerPrefs.Save();
        }

        /// <summary>새 시도를 시작할 수 있는지 (하트 1개 이상).</summary>
        public bool CanStartAttempt() => Hearts > 0;

        /// <summary>Hard Fail 시 하트 소모. 즉시 호출되어 Home/LevelSelect 회피 불가.</summary>
        public void ConsumeHeart(int amount = 1)
        {
            if (amount <= 0) return;
            int prev = Hearts;
            Hearts = Mathf.Max(0, Hearts - amount);
            Save();
            OnHeartsChanged?.Invoke(Hearts);
            if (prev > 0 && Hearts == 0)
                OnOutOfHearts?.Invoke();
        }

        /// <summary>리워드 광고 시청 후 전체 충전.</summary>
        public void RefillFull()
        {
            Hearts = MaxHearts;
            Save();
            OnHeartsChanged?.Invoke(Hearts);
        }

        /// <summary>테스트/내부용. Hearts 값 직접 설정.</summary>
        public void SetHearts(int value)
        {
            Hearts = Mathf.Clamp(value, 0, MaxHearts);
            Save();
            OnHeartsChanged?.Invoke(Hearts);
        }
    }
}
