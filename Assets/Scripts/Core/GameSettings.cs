using System;
using UnityEngine;

namespace CircuitOneStroke.Core
{
    /// <summary>이동 불가 시 동작. RejectOnly=진입만 막음, ImmediateFail=스트로크 즉시 실패.</summary>
    public enum FailFeedbackMode
    {
        RejectOnly,
        ImmediateFail
    }

    /// <summary>햅틱 강도.</summary>
    public enum HapticsStrength
    {
        Light,
        Normal
    }

    /// <summary>선 두께.</summary>
    public enum LineThickness
    {
        Thin,
        Normal,
        Thick
    }

    /// <summary>노드 크기.</summary>
    public enum NodeSize
    {
        Small,
        Normal,
        Large
    }

    /// <summary>언어 (스캐폴딩).</summary>
    public enum Language
    {
        System,
        Korean,
        English
    }

    /// <summary>JSON 직렬화용 설정 데이터.</summary>
    [Serializable]
    public class GameSettingsData
    {
        // Audio
        public bool musicEnabled = true;
        public float musicVolume = 0.8f;
        public bool sfxEnabled = true;
        public float sfxVolume = 0.9f;

        // Haptics
        public bool hapticsEnabled = true;
        public int hapticsStrength = (int)HapticsStrength.Normal;

        // Controls / Input
        public float snapAssist = 0.7f;
        public float dragSensitivity = 1f;
        public bool rejectFeedbackEnabled = true;
        public bool confirmExitFromGame = true;

        // Visuals
        public int lineThickness = (int)LineThickness.Normal;
        public int nodeSize = (int)NodeSize.Normal;
        public bool showIconAndText = true;

        // Accessibility
        public bool colorBlindMode = false;
        public bool highContrastUI = false;
        public bool largeText = false;

        // Language
        public int language = (int)Language.System;
    }

    /// <summary>
    /// PlayerPrefs JSON 기반 설정. 소리·진동·Fail 모드·접근성·비주얼 등.
    /// OnChanged 이벤트로 실시간 반영.
    /// </summary>
    public class GameSettings
    {
        private const string KeyJson = "GAME_SETTINGS_JSON";
        private const string KeyFailMode = "CircuitOneStroke_FailMode";
        private const string KeySound = "CircuitOneStroke_Sound";
        private const string KeyVibrate = "CircuitOneStroke_Vibrate";

        public static GameSettings Instance { get; } = new GameSettings();

        public GameSettingsData Data { get; private set; }

        /// <summary>설정 변경 시 발생. Apply() 또는 저장 시 호출.</summary>
        public event Action<GameSettingsData> OnChanged;

        private GameSettings()
        {
            Load();
        }

        public void Load()
        {
            var json = PlayerPrefs.GetString(KeyJson, null);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    Data = JsonUtility.FromJson<GameSettingsData>(json);
                    return;
                }
                catch { }
            }

            Data = new GameSettingsData();

            // Migrate legacy keys
            if (PlayerPrefs.HasKey(KeySound))
            {
                bool legacy = PlayerPrefs.GetInt(KeySound, 1) != 0;
                Data.musicEnabled = Data.sfxEnabled = legacy;
            }
            if (PlayerPrefs.HasKey(KeyVibrate))
            {
                Data.hapticsEnabled = PlayerPrefs.GetInt(KeyVibrate, 1) != 0;
            }
            if (PlayerPrefs.HasKey(KeyFailMode))
            {
                // FailMode is kept separate; not in Data for now to avoid breaking existing code
            }

            Save();
        }

        public void Save()
        {
            if (Data == null) Data = new GameSettingsData();
            PlayerPrefs.SetString(KeyJson, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
            OnChanged?.Invoke(Data);
        }

        /// <summary>설정을 게임 시스템에 반영. OnChanged도 발생.</summary>
        public void Apply()
        {
            Save();
        }

        // --- Audio ---
        public bool MusicEnabled { get => Data.musicEnabled; set { Data.musicEnabled = value; Save(); } }
        public float MusicVolume { get => Data.musicVolume; set { Data.musicVolume = Mathf.Clamp01(value); Save(); } }
        public bool SfxEnabled { get => Data.sfxEnabled; set { Data.sfxEnabled = value; Save(); } }
        public float SfxVolume { get => Data.sfxVolume; set { Data.sfxVolume = Mathf.Clamp01(value); Save(); } }

        // --- Haptics ---
        public bool HapticsEnabled { get => Data.hapticsEnabled; set { Data.hapticsEnabled = value; Save(); } }
        public HapticsStrength HapticsStrengthValue
        {
            get => (HapticsStrength)Mathf.Clamp(Data.hapticsStrength, 0, 1);
            set { Data.hapticsStrength = (int)value; Save(); }
        }

        // --- Controls ---
        public float SnapAssist { get => Data.snapAssist; set { Data.snapAssist = Mathf.Clamp01(value); Save(); } }
        public float DragSensitivity { get => Data.dragSensitivity; set { Data.dragSensitivity = Mathf.Max(0.1f, value); Save(); } }
        public bool RejectFeedbackEnabled { get => Data.rejectFeedbackEnabled; set { Data.rejectFeedbackEnabled = value; Save(); } }
        public bool ConfirmExitFromGame { get => Data.confirmExitFromGame; set { Data.confirmExitFromGame = value; Save(); } }

        // --- Visuals ---
        public LineThickness LineThicknessValue { get => (LineThickness)Mathf.Clamp(Data.lineThickness, 0, 2); set { Data.lineThickness = (int)value; Save(); } }
        public NodeSize NodeSizeValue { get => (NodeSize)Mathf.Clamp(Data.nodeSize, 0, 2); set { Data.nodeSize = (int)value; Save(); } }
        public bool ShowIconAndText { get => Data.showIconAndText; set { Data.showIconAndText = value; Save(); } }

        // --- Accessibility ---
        public bool ColorBlindMode { get => Data.colorBlindMode; set { Data.colorBlindMode = value; Save(); } }
        public bool HighContrastUI { get => Data.highContrastUI; set { Data.highContrastUI = value; Save(); } }
        public bool LargeText { get => Data.largeText; set { Data.largeText = value; Save(); } }

        // --- Language ---
        public Language LanguageValue { get => (Language)Mathf.Clamp(Data.language, 0, 2); set { Data.language = (int)value; Save(); } }

        // --- Legacy (FailMode) ---
        public FailFeedbackMode FailMode
        {
            get => (FailFeedbackMode)PlayerPrefs.GetInt(KeyFailMode, (int)FailFeedbackMode.RejectOnly);
            set => PlayerPrefs.SetInt(KeyFailMode, (int)value);
        }

        /// <summary>레거시 호환: SoundEnabled = SFX 쪽으로 매핑. 설정 UI는 music/sfx 분리.</summary>
        public static bool SoundEnabled
        {
            get => Instance.Data.sfxEnabled;
            set => Instance.SfxEnabled = value;
        }

        /// <summary>레거시 호환: VibrateEnabled.</summary>
        public static bool VibrateEnabled
        {
            get => Instance.Data.hapticsEnabled;
            set => Instance.HapticsEnabled = value;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool DevBypassRewardedOnUnavailable => true;
#else
        public static bool DevBypassRewardedOnUnavailable => false;
#endif
    }
}
