#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// 선택한 Canvas를 Screen Space - Overlay로 바꿉니다.
    /// 씬 뷰에서 UI가 3D 오브젝트처럼 보이거나 Game 뷰에 안 맞을 때 사용하세요.
    /// </summary>
    public static class FixCanvasToOverlay
    {
        [MenuItem("Circuit One-Stroke/UI/Fix Canvas to Portrait (1080x1920)", false)]
        public static void FixCanvasToPortraitInScene()
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("씬에 Canvas가 없습니다.");
                return;
            }
            ApplyPortraitToCanvas(canvas);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("캔버스 세로(1080x1920) 적용됨: " + canvas.gameObject.name);
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
            ApplyPortraitToCanvas(canvas);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Canvas: Overlay + 세로 1080x1920 적용됨.");
        }

        private static void ApplyPortraitToCanvas(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;

            var rect = canvas.GetComponent<RectTransform>();
            if (rect != null)
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
