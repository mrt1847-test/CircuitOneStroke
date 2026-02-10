using System;
using System.Collections;
using UnityEngine;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Core
{
    /// <summary>페이드/로딩 오버레이 옵션.</summary>
    public struct TransitionOptions
    {
        public float fadeOutDuration;
        public float fadeInDuration;
        public float spinnerDelay;
        public float minBlackTime;
        public bool useUnscaledTime;
        public float maxTransitionTimeout;

        public static TransitionOptions Default => new TransitionOptions
        {
            fadeOutDuration = 0.20f,
            fadeInDuration = 0.20f,
            spinnerDelay = 0.30f,
            minBlackTime = 0.05f,
            useUnscaledTime = true,
            maxTransitionTimeout = 10f
        };
    }

    /// <summary>
    /// 전역 전환 매니저. 페이드 아웃/인, 로딩 스피너(지연 표시), 입력 차단.
    /// </summary>
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager Instance { get; private set; }

        [SerializeField] private TransitionOverlayView overlayPrefab;

        private TransitionOverlayView _overlay;
        private bool _isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance != null) return;
            var go = new GameObject("TransitionManager");
            go.AddComponent<TransitionManager>();
        }

        /// <summary>전환 중이면 true. 입력 차단·버튼 비활성화용.</summary>
        public bool IsTransitioning => _isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureOverlay();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void EnsureOverlay()
        {
            if (_overlay != null) return;

            var prefab = overlayPrefab != null ? overlayPrefab : Resources.Load<TransitionOverlayView>("TransitionOverlay");
            if (prefab == null)
            {
                var go = new GameObject("TransitionOverlayRuntime");
                _overlay = go.AddComponent<TransitionOverlayView>();
                _overlay.BuildDefaultUI();
            }
            else
            {
                _overlay = Instantiate(prefab);
            }
            _overlay.transform.SetParent(transform);
            DontDestroyOnLoad(_overlay.gameObject);
            _overlay.gameObject.SetActive(false);
        }

        /// <summary>전환 실행. work 완료 후 페이드 인.</summary>
        public Coroutine RunTransition(IEnumerator work, TransitionOptions options = default)
        {
            if (options.fadeOutDuration == 0 && options.fadeInDuration == 0 &&
                options.spinnerDelay == 0 && options.minBlackTime == 0)
                options = TransitionOptions.Default;

            EnsureOverlay();
            return StartCoroutine(RunTransitionCoroutine(work, options));
        }

        private IEnumerator RunTransitionCoroutine(IEnumerator work, TransitionOptions options)
        {
            _isTransitioning = true;
            float dt() => options.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t() => options.useUnscaledTime ? Time.unscaledTime : Time.time;

            _overlay.gameObject.SetActive(true);
            _overlay.SetBlocker(true);
            _overlay.SetFadeAlpha(0f);
            _overlay.SetSpinnerVisible(false);
            _overlay.StopSpinnerRotation();

            // 1) Fade out
            float elapsed = 0f;
            while (elapsed < options.fadeOutDuration)
            {
                elapsed += dt();
                _overlay.SetFadeAlpha(Mathf.Clamp01(elapsed / options.fadeOutDuration));
                yield return null;
            }
            _overlay.SetFadeAlpha(1f);

            float workStartTime = t();
            bool workDone = false;
            Exception workError = null;
            Coroutine workCoroutine = null;

            // 2) Run work (with spinner after delay)
            workCoroutine = StartCoroutine(RunWorkWithSpinner(work, options, workStartTime, () => workDone = true, ex => workError = ex));

            // 3) Wait for work or timeout
            while (!workDone && workError == null && (t() - workStartTime) < options.maxTransitionTimeout)
                yield return null;

            if (workCoroutine != null && !workDone && workError == null && (t() - workStartTime) >= options.maxTransitionTimeout)
            {
                StopCoroutine(workCoroutine);
                Debug.LogError("[TransitionManager] Transition timeout. Load failed.");
                // Could show "Load failed. Retry." dialog here.
            }

            // 4) Ensure min black time
            float blackElapsed = t() - workStartTime;
            if (blackElapsed < options.minBlackTime)
            {
                float wait = options.minBlackTime - blackElapsed;
                if (options.useUnscaledTime)
                    yield return new WaitForSecondsRealtime(wait);
                else
                    yield return new WaitForSeconds(wait);
            }

            _overlay.SetSpinnerVisible(false);
            _overlay.StopSpinnerRotation();

            // 5) Fade in
            elapsed = 0f;
            while (elapsed < options.fadeInDuration)
            {
                elapsed += dt();
                _overlay.SetFadeAlpha(1f - Mathf.Clamp01(elapsed / options.fadeInDuration));
                yield return null;
            }
            _overlay.SetFadeAlpha(0f);
            _overlay.SetBlocker(false);
            _overlay.gameObject.SetActive(false);
            _isTransitioning = false;
        }

        private IEnumerator RunWorkWithSpinner(IEnumerator work, TransitionOptions options, float workStartTime, Action onDone, Action<Exception> onError)
        {
            bool spinnerShown = false;
            bool workComplete = false;
            float t() => options.useUnscaledTime ? Time.unscaledTime : Time.time;

            IEnumerator RunWork()
            {
                try
                {
                    if (work != null)
                        while (work.MoveNext())
                            yield return work.Current;
                }
                catch (Exception ex) { onError?.Invoke(ex); }
                workComplete = true;
            }

            StartCoroutine(RunWork());

            while (!workComplete)
            {
                if (!spinnerShown && (t() - workStartTime) >= options.spinnerDelay)
                {
                    spinnerShown = true;
                    _overlay.SetSpinnerVisible(true);
                    _overlay.StartSpinnerRotation(options.useUnscaledTime);
                }
                yield return null;
            }
            onDone?.Invoke();
        }

        /// <summary>씬 비동기 로드 헬퍼. RunTransition(LoadSceneAsyncCoroutine(name))</summary>
        public static IEnumerator LoadSceneAsyncCoroutine(string sceneName)
        {
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            if (op == null) yield break;
            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;
        }
    }
}
