#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// Canvas render/scaler fix helpers.
    /// Safe mode updates render mode + scaler only.
    /// Legacy mode also forces Canvas RectTransform to 1080x1920.
    /// </summary>
    public static class FixCanvasToOverlay
    {
        [MenuItem("Circuit One-Stroke/UI/Fix Canvas to Portrait (1080x1920)", false)]
        public static void FixCanvasToPortraitInScene()
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found in scene.");
                return;
            }
            ApplyPortraitToCanvas(canvas, forceCanvasRect: false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Canvas portrait scaler applied safely (RectTransform unchanged): " + canvas.gameObject.name);
        }

        [MenuItem("Circuit One-Stroke/UI/Fix Canvas to Portrait (1080x1920, Force Rect Legacy)", false)]
        public static void FixCanvasToPortraitInSceneLegacy()
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found in scene.");
                return;
            }
            ApplyPortraitToCanvas(canvas, forceCanvasRect: true);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Canvas portrait scaler + forced RectTransform applied (legacy): " + canvas.gameObject.name);
        }

        [MenuItem("Circuit One-Stroke/UI/Fix Canvas to Screen Space Overlay", true)]
        private static bool ValidateFixCanvas()
        {
            var go = Selection.activeGameObject;
            if (go == null) return false;
            return go.GetComponent<Canvas>() != null || go.GetComponentInParent<Canvas>() != null;
        }

        [MenuItem("Circuit One-Stroke/UI/Fix Canvas to Screen Space Overlay", false)]
        public static void FixCanvas()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;

            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
                canvas = go.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("Select a Canvas or an object under a Canvas.");
                return;
            }
            ApplyPortraitToCanvas(canvas, forceCanvasRect: false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Canvas: Overlay + safe portrait scaler applied.");
        }

        private static void ApplyPortraitToCanvas(Canvas canvas, bool forceCanvasRect)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var rect = canvas.GetComponent<RectTransform>();
            if (forceCanvasRect && rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(1080, 1920);
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
#endif
