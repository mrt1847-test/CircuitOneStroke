using UnityEngine;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// Safe area (노치/홈 인디케이터) 적용. RectTransform의 anchor를 safe area에 맞춤.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            var area = Screen.safeArea;
            if (area != _lastSafeArea)
                ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (_rect == null) return;
            float w = Mathf.Max(Screen.width, 1f);
            float h = Mathf.Max(Screen.height, 1f);
            _lastSafeArea = Screen.safeArea;
            var anchorMin = new Vector2(_lastSafeArea.xMin / w, _lastSafeArea.yMin / h);
            var anchorMax = new Vector2(_lastSafeArea.xMax / w, _lastSafeArea.yMax / h);
            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
