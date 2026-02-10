using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// Canvas가 Game 뷰 해상도로 RectTransform을 덮어쓰는 것을 막고, 가로가 더 길면 세로(1080×1920)로 강제.
    /// 프레임 끝에서 적용해 Canvas 갱신 이후에 실행. ExecuteAlways로 에디터에서도 동작.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(RectTransform))]
    public class ForcePortraitCanvas : MonoBehaviour
    {
        private const float PortraitWidth = 1080f;
        private const float PortraitHeight = 1920f;

        private RectTransform _rect;

        private void OnEnable()
        {
            _rect = GetComponent<RectTransform>();
            if (Application.isPlaying)
                StartCoroutine(ApplyAtEndOfFrame());
        }

        private void LateUpdate()
        {
            ApplyIfLandscape();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            ApplyIfLandscape();
        }
#endif

        private IEnumerator ApplyAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            ApplyIfLandscape();
        }

        private void ApplyIfLandscape()
        {
            if (_rect == null) return;
            if (_rect.sizeDelta.x <= _rect.sizeDelta.y) return;

            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = new Vector2(PortraitWidth, PortraitHeight);
            transform.localScale = Vector3.one;
        }
    }
}
