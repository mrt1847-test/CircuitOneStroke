using UnityEngine;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// 음악 + SFX 볼륨/뮤트 관리. GameSettings.OnChanged 구독.
    /// SFX는 GameFeedback에서 PlaySfx로 재생 요청.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        public static AudioManager Instance { get; private set; }

        public AudioSource MusicSource => musicSource;
        public AudioSource SfxSource => sfxSource;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            ApplySettings(GameSettings.Instance.Data);
            GameSettings.Instance.OnChanged += ApplySettings;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameSettings.Instance.OnChanged -= ApplySettings;
        }

        private void ApplySettings(GameSettingsData data)
        {
            if (data == null) return;
            if (musicSource != null)
            {
                musicSource.mute = !data.musicEnabled;
                musicSource.volume = data.musicVolume;
            }
            if (sfxSource != null)
            {
                sfxSource.mute = !data.sfxEnabled;
                sfxSource.volume = data.sfxVolume;
            }
        }

        /// <summary>SFX 재생. GameFeedback에서 호출.</summary>
        public void PlaySfx(AudioClip clip)
        {
            if (sfxSource != null && clip != null && GameSettings.Instance.Data.sfxEnabled)
                sfxSource.PlayOneShot(clip);
        }
    }
}
