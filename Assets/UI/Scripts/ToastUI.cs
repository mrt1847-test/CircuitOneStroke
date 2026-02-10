using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// GameFeedback.OnToastRequested 구독, 짧은 메시지 표시.
    /// </summary>
    public class ToastUI : MonoBehaviour
    {
        [SerializeField] private Text toastText;
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private CanvasGroup canvasGroup;

        private float _hideTime;

        private void OnEnable()
        {
            GameFeedback.OnToastRequested += ShowToast;
        }

        private void OnDisable()
        {
            GameFeedback.OnToastRequested -= ShowToast;
        }

        private void Update()
        {
            if (toastText != null && toastText.gameObject.activeSelf && Time.time >= _hideTime)
                toastText.gameObject.SetActive(false);
        }

        private void ShowToast(string message)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            _hideTime = Time.time + displayDuration;
        }
    }
}
