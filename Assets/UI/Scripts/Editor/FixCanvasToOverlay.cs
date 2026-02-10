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

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Canvas set to Screen Space - Overlay. Check Game view.");
        }
    }
}
#endif
