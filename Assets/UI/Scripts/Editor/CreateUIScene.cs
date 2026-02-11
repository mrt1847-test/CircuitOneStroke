#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using CircuitOneStroke.UI;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// 현재 씬에 카메라 + Canvas + ScreenRoot를 한 번에 추가합니다.
    /// Game 뷰 "No Camera" 해결 및 UI 테스트용 최소 구성을 만듭니다. (Unity 6 기준)
    /// </summary>
    public static class CreateUIScene
    {
        private const string ThemePath = "Assets/UI/Theme/CircuitOneStrokeTheme.asset";

        [MenuItem("Circuit One-Stroke/UI/Create UI Scene (Camera + Canvas + ScreenRoot)")]
        public static void Create()
        {
            CreateDefaultTheme.EnsureFolders();

            // 1. 카메라 없으면 생성 (Game 뷰 "No Camera" 방지)
            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camGo = new GameObject("Main Camera");
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.transform.position = new Vector3(0, 0, -10);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.14f, 0.16f, 0.24f, 1f);
                camGo.tag = "MainCamera";
                Debug.Log("Main Camera added (required for Game view).");
            }

            // 2. Canvas 없으면 생성
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas");
                var c = canvasGo.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGo.AddComponent<GraphicRaycaster>();
                canvas = c;
                if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var esGo = new GameObject("EventSystem");
                    esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                    esGo.AddComponent<InputSystemUIInputModule>();
#else
                    esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
                }
                Debug.Log("Canvas + EventSystem added.");
            }

            // 3. Canvas 하위에 ScreenRoot 없으면 추가
            var screenRoot = canvas.GetComponentInChildren<ScreenRoot>(true);
            if (screenRoot == null)
            {
                var rootGo = new GameObject("ScreenRoot");
                rootGo.transform.SetParent(canvas.transform, false);
                var rect = rootGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                screenRoot = rootGo.AddComponent<ScreenRoot>();
                var theme = AssetDatabase.LoadAssetAtPath<CircuitOneStrokeTheme>(ThemePath);
                var so = new SerializedObject(screenRoot);
                so.FindProperty("theme").objectReferenceValue = theme;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("ScreenRoot added under Canvas. Assign Theme and add Panel/Button prefabs as children.");
            }

            Selection.activeGameObject = screenRoot != null ? screenRoot.gameObject : canvas.gameObject;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
#endif
