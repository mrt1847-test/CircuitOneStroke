using UnityEngine;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// 이동 성공/리젝트/실패/클리어 시 사운드·진동 재생. 싱글톤으로 UI·입력에서 참조.
    /// </summary>
    public class GameFeedback : MonoBehaviour
    {
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

        /// <summary>이동 성공 시. GameSettings.SoundEnabled 반영.</summary>
        public void PlayMoveOk()
        {
            if (GameSettings.SoundEnabled && audioSource != null && moveOkClip != null)
                audioSource.PlayOneShot(moveOkClip);
        }

        /// <summary>이동 불가(리젝트) 시. 설정에 따라 진동.</summary>
        public void PlayReject()
        {
            if (GameSettings.SoundEnabled && audioSource != null && rejectClip != null)
                audioSource.PlayOneShot(rejectClip);
            if (GameSettings.VibrateEnabled)
                Handheld.Vibrate();
        }

        /// <summary>재방문 등 규칙 위반(즉시 실패) 시.</summary>
        public void PlayFail()
        {
            if (GameSettings.SoundEnabled && audioSource != null && failClip != null)
                audioSource.PlayOneShot(failClip);
            if (GameSettings.VibrateEnabled)
                Handheld.Vibrate();
        }

        /// <summary>클리어 시.</summary>
        public void PlaySuccess()
        {
            if (GameSettings.SoundEnabled && audioSource != null && successClip != null)
                audioSource.PlayOneShot(successClip);
        }
    }
}
