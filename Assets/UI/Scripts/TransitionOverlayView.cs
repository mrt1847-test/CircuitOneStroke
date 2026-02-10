using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// 전환 오버레이 UI. Blocker, Fade, Spinner 구성.
    /// </summary>
    public class TransitionOverlayView : MonoBehaviour
    {
        [SerializeField] private GameObject blocker;
        [SerializeField] private Image fadeImage;
        [SerializeField] private GameObject spinnerRoot;
        [SerializeField] private RectTransform spinnerIcon;
        [SerializeField] private Text loadingText;

        private float _spinnerRotationSpeed = 180f;
        private bool _spinnerRotating;
        private bool _useUnscaledForSpinner;

        public void SetBlocker(bool active)
        {
            if (blocker != null) blocker.SetActive(active);
        }

        public void SetFadeAlpha(float a)
        {
            if (fadeImage != null)
            {
                var c = fadeImage.color;
                c.a = Mathf.Clamp01(a);
                fadeImage.color = c;
            }
        }

        public void SetSpinnerVisible(bool visible)
        {
            if (spinnerRoot != null) spinnerRoot.SetActive(visible);
        }

        public void StartSpinnerRotation(bool useUnscaledTime)
        {
            _spinnerRotating = true;
            _useUnscaledForSpinner = useUnscaledTime;
        }

        public void StopSpinnerRotation()
        {
            _spinnerRotating = false;
        }

        private void Update()
        {
            if (_spinnerRotating && spinnerIcon != null)
            {
                float dt = _useUnscaledForSpinner ? Time.unscaledDeltaTime : Time.deltaTime;
                spinnerIcon.Rotate(0, 0, -_spinnerRotationSpeed * dt);
            }
        }

        /// <summary>프리팹 없을 때 런타임 기본 UI 구성.</summary>
        public void BuildDefaultUI()
        {
            var rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            canvas.overrideSorting = true;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            var blockerGo = new GameObject("Blocker");
            blockerGo.transform.SetParent(transform, false);
            var blockerRect = blockerGo.AddComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = blockerRect.offsetMax = Vector2.zero;
            var blockerImg = blockerGo.AddComponent<Image>();
            blockerImg.color = new Color(0, 0, 0, 0.01f);
            blockerImg.raycastTarget = true;
            blocker = blockerGo;

            var fadeGo = new GameObject("Fade");
            fadeGo.transform.SetParent(transform, false);
            var fadeRect = fadeGo.AddComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
            fadeImage = fadeGo.AddComponent<Image>();
            fadeImage.color = new Color(0.06f, 0.07f, 0.12f, 0f);
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(true);

            var spinnerRootGo = new GameObject("SpinnerRoot");
            spinnerRootGo.transform.SetParent(transform, false);
            var srRect = spinnerRootGo.AddComponent<RectTransform>();
            srRect.anchorMin = srRect.anchorMax = new Vector2(0.5f, 0.5f);
            srRect.pivot = new Vector2(0.5f, 0.5f);
            srRect.anchoredPosition = Vector2.zero;
            srRect.sizeDelta = new Vector2(120, 120);
            spinnerRoot = spinnerRootGo;
            spinnerRoot.SetActive(false);

            var spinnerIconGo = new GameObject("SpinnerIcon");
            spinnerIconGo.transform.SetParent(spinnerRootGo.transform, false);
            var siRect = spinnerIconGo.AddComponent<RectTransform>();
            siRect.anchorMin = Vector2.zero;
            siRect.anchorMax = Vector2.one;
            siRect.offsetMin = siRect.offsetMax = Vector2.zero;
            var siImg = spinnerIconGo.AddComponent<Image>();
            siImg.color = UIStyleConstants.Primary;
            siImg.sprite = CreateCircleSprite();
            siImg.type = Image.Type.Simple;
            spinnerIcon = siRect;

            var loadingTextGo = new GameObject("LoadingText");
            loadingTextGo.transform.SetParent(spinnerRootGo.transform, false);
            var ltRect = loadingTextGo.AddComponent<RectTransform>();
            ltRect.anchorMin = new Vector2(0, -0.3f);
            ltRect.anchorMax = new Vector2(1, 0);
            ltRect.offsetMin = ltRect.offsetMax = Vector2.zero;
            loadingText = loadingTextGo.AddComponent<Text>();
            loadingText.text = "Loading…";
            loadingText.fontSize = 36;
            loadingText.color = UIStyleConstants.Primary;
            loadingText.alignment = TextAnchor.MiddleCenter;
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.4f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = d <= radius ? 1f : (d <= radius + 2 ? 1f - (d - radius) / 2f : 0f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
