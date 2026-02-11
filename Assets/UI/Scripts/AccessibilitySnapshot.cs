using UnityEngine;
using CircuitOneStroke.Core;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 씬 진입 후 접근성 상태 스냅샷 및 적용 대상 컴포넌트 수 로깅.
    /// LargeText/HighContrast 적용 범위 점검용. 한 번만 실행하려면 runOnce = true.
    /// </summary>
    public class AccessibilitySnapshot : MonoBehaviour
    {
        [SerializeField] private bool runOnce = true;
        [SerializeField] private float delayFrames = 2f;

        private bool _done;

        private void Start()
        {
            if (runOnce && _done) return;
            if (delayFrames > 0)
                StartCoroutine(LogAfterDelay());
            else
                LogSnapshot();
        }

        private System.Collections.IEnumerator LogAfterDelay()
        {
            for (int i = 0; i < (int)delayFrames; i++)
                yield return null;
            LogSnapshot();
            _done = true;
        }

        private void LogSnapshot()
        {
            bool largeText = GameSettings.Instance?.Data?.largeText ?? false;
            bool highContrast = GameSettings.Instance?.Data?.highContrastUI ?? false;

            int themeAppliers = 0;
            int textScalers = 0;
#if UNITY_2023_1_OR_NEWER
            foreach (var t in FindObjectsByType<ThemeApplier>(FindObjectsSortMode.None))
                if (t != null) themeAppliers++;
            foreach (var s in FindObjectsByType<AccessibilityTextScaler>(FindObjectsSortMode.None))
                if (s != null) textScalers++;
#else
            foreach (var t in FindObjectsOfType<ThemeApplier>())
                if (t != null) themeAppliers++;
            foreach (var s in FindObjectsOfType<AccessibilityTextScaler>())
                if (s != null) textScalers++;
#endif

            Debug.Log($"[Accessibility] Snapshot: LargeText={largeText}, HighContrast={highContrast} | ThemeApplier targets={themeAppliers}, AccessibilityTextScaler count={textScalers}");
            _done = true;
        }
    }
}
