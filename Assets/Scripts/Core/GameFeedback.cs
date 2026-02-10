using System;
using UnityEngine;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// 이동 성공/리젝트/실패/클리어 시 사운드·진동 재생. 싱글톤으로 UI·입력에서 참조.
    /// </summary>
    public class GameFeedback : MonoBehaviour
    {
        /// <summary>토스트 메시지 표시 요청. 구독 시 Toast UI에서 표시.</summary>
        public static event Action<string> OnToastRequested;

        /// <summary>토스트 메시지 요청 (예: "Invalid move").</summary>
        public static void RequestToast(string message) => OnToastRequested?.Invoke(message);
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip moveOkClip;
        [SerializeField] private AudioClip rejectClip;
        [SerializeField] private AudioClip failClip;
        [SerializeField] private AudioClip successClip;

        public static GameFeedback Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void PlaySfx(AudioClip clip)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(clip);
            else if (GameSettings.SoundEnabled && audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        /// <summary>이동 성공 시. GameSettings.SfxEnabled 반영.</summary>
        public void PlayMoveOk()
        {
            if (moveOkClip != null) PlaySfx(moveOkClip);
        }

        /// <summary>이동 불가(리젝트) 시. 설정에 따라 진동.</summary>
        public void PlayReject()
        {
            if (rejectClip != null) PlaySfx(rejectClip);
            if (HapticsManager.Instance != null)
                HapticsManager.Instance.PulseReject();
            else if (GameSettings.VibrateEnabled)
#if UNITY_ANDROID && !UNITY_EDITOR
                Handheld.Vibrate();
#endif
        }

        /// <summary>재방문 등 규칙 위반(즉시 실패) 시.</summary>
        public void PlayFail()
        {
            if (failClip != null) PlaySfx(failClip);
            if (HapticsManager.Instance != null)
                HapticsManager.Instance.PulseFail();
            else if (GameSettings.VibrateEnabled)
#if UNITY_ANDROID && !UNITY_EDITOR
                Handheld.Vibrate();
#endif
        }

        /// <summary>클리어 시.</summary>
        public void PlaySuccess()
        {
            if (successClip != null) PlaySfx(successClip);
            if (HapticsManager.Instance != null)
                HapticsManager.Instance.PulseWin();
        }
    }
}
