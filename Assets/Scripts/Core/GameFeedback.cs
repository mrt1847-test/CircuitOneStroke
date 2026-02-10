using UnityEngine;

namespace CircuitOneStroke.Core
{
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

        public void PlayMoveOk()
        {
            if (GameSettings.SoundEnabled && audioSource != null && moveOkClip != null)
                audioSource.PlayOneShot(moveOkClip);
        }

        public void PlayReject()
        {
            if (GameSettings.SoundEnabled && audioSource != null && rejectClip != null)
                audioSource.PlayOneShot(rejectClip);
            if (GameSettings.VibrateEnabled)
                Handheld.Vibrate();
        }

        public void PlayFail()
        {
            if (GameSettings.SoundEnabled && audioSource != null && failClip != null)
                audioSource.PlayOneShot(failClip);
            if (GameSettings.VibrateEnabled)
                Handheld.Vibrate();
        }

        public void PlaySuccess()
        {
            if (GameSettings.SoundEnabled && audioSource != null && successClip != null)
                audioSource.PlayOneShot(successClip);
        }
    }
}
