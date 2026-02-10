using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// GameFeedback.OnToastRequested 구독, 짧은 메시지 표시. 페이드 인/아웃 지원.
    /// </summary>
    public class ToastUI : MonoBehaviour
    {
        [SerializeField] private Text toastText;
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.18f;

        private Coroutine _activeToast;

        private void OnEnable()
        {
            GameFeedback.OnToastRequested += ShowToast;
        }

        private void OnDisable()
        {
            GameFeedback.OnToastRequested -= ShowToast;
        }

        private void ShowToast(string message)
        {
            if (toastText == null) return;
            if (_activeToast != null)
                StopCoroutine(_activeToast);

            toastText.text = message;
            toastText.gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                _activeToast = StartCoroutine(ToastWithFadeCoroutine());
            }
            else
            {
                _activeToast = StartCoroutine(ToastSimpleCoroutine());
            }
        }

        private IEnumerator ToastWithFadeCoroutine()
        {
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(displayDuration);

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            toastText.gameObject.SetActive(false);
            _activeToast = null;
        }

        private IEnumerator ToastSimpleCoroutine()
        {
            yield return new WaitForSecondsRealtime(displayDuration);
            toastText.gameObject.SetActive(false);
            _activeToast = null;
        }
    }
}
