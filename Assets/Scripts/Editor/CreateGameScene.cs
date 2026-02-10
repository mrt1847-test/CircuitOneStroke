#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

namespace CircuitOneStroke.Editor
{
    public static class CreateGameScene
    {
        [MenuItem("Circuit One-Stroke/Create Game Scene")]
        public static void CreateScene()
        {
            if (Resources.Load<CircuitOneStroke.Data.LevelData>("Levels/Level_1") == null)
                CreateDefaultLevel.CreateLevel1();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.transform.position = new Vector3(0, 0, -10);
                cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            }

            var gameRoot = new GameObject("Game");
            var loader = gameRoot.AddComponent<CircuitOneStroke.Core.LevelLoader>();

            var nodesRoot = new GameObject("Nodes");
            nodesRoot.transform.SetParent(gameRoot.transform, false);
            var edgesRoot = new GameObject("Edges");
            edgesRoot.transform.SetParent(gameRoot.transform, false);

            var strokeGo = new GameObject("StrokeRenderer");
            strokeGo.transform.SetParent(gameRoot.transform, false);
            var lr = strokeGo.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.positionCount = 0;
            var strokeRenderer = strokeGo.AddComponent<CircuitOneStroke.View.StrokeRenderer>();

            var feedbackGo = new GameObject("GameFeedback");
            feedbackGo.transform.SetParent(gameRoot.transform, false);
            var audioSource = feedbackGo.AddComponent<AudioSource>();
            feedbackGo.AddComponent<CircuitOneStroke.Core.GameFeedback>();
            var feedbackSo = new SerializedObject(feedbackGo.GetComponent<CircuitOneStroke.Core.GameFeedback>());
            feedbackSo.FindProperty("audioSource").objectReferenceValue = audioSource;
            feedbackSo.ApplyModifiedPropertiesWithoutUndo();

            var input = gameRoot.AddComponent<CircuitOneStroke.Input.TouchInputController>();
            var so = new SerializedObject(loader);
            so.FindProperty("nodesRoot").objectReferenceValue = nodesRoot.transform;
            so.FindProperty("edgesRoot").objectReferenceValue = edgesRoot.transform;
            so.FindProperty("strokeRenderer").objectReferenceValue = strokeRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            var inputSo = new SerializedObject(input);
            inputSo.FindProperty("levelLoader").objectReferenceValue = loader;
            inputSo.FindProperty("mainCamera").objectReferenceValue = cam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            var nodePrefab = CreateNodePrefab();
            var edgePrefab = CreateEdgePrefab();
            so.FindProperty("nodeViewPrefab").objectReferenceValue = nodePrefab;
            so.FindProperty("edgeViewPrefab").objectReferenceValue = edgePrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            var canvas = new GameObject("Canvas");
            var canvasComp = canvas.AddComponent<Canvas>();
            canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.AddComponent<GraphicRaycaster>();

            var hud = new GameObject("GameHUD");
            hud.transform.SetParent(canvas.transform, false);
            var hudRect = hud.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = hudRect.offsetMax = Vector2.zero;
            var gameHud = hud.AddComponent<CircuitOneStroke.UI.GameHUD>();

            var retryBtn = new GameObject("RetryButton");
            retryBtn.transform.SetParent(hud.transform, false);
            var btnRect = retryBtn.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.3f);
            btnRect.anchorMax = new Vector2(0.5f, 0.3f);
            btnRect.sizeDelta = new Vector2(200, 50);
            btnRect.anchoredPosition = Vector2.zero;
            var btnImage = retryBtn.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.6f, 0.3f);
            var btn = retryBtn.AddComponent<Button>();
            var btnText = new GameObject("Text").AddComponent<Text>();
            btnText.transform.SetParent(retryBtn.transform, false);
            btnText.text = "Retry";
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.fontSize = 24;
            var textRect = btnText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            var successPanel = new GameObject("SuccessPanel");
            successPanel.transform.SetParent(hud.transform, false);
            var spRect = successPanel.AddComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 0.5f);
            spRect.anchorMax = new Vector2(0.5f, 0.5f);
            spRect.sizeDelta = new Vector2(300, 80);
            spRect.anchoredPosition = Vector2.zero;
            var spText = successPanel.AddComponent<Text>();
            spText.text = "Clear!";
            spText.fontSize = 36;
            spText.alignment = TextAnchor.MiddleCenter;
            successPanel.SetActive(false);

            var failPanel = new GameObject("FailPanel");
            failPanel.transform.SetParent(hud.transform, false);
            var fpRect = failPanel.AddComponent<RectTransform>();
            fpRect.anchorMin = new Vector2(0.5f, 0.5f);
            fpRect.anchorMax = new Vector2(0.5f, 0.5f);
            fpRect.sizeDelta = new Vector2(300, 80);
            fpRect.anchoredPosition = new Vector2(0, -100);
            var fpText = failPanel.AddComponent<Text>();
            fpText.text = "Failed";
            fpText.fontSize = 36;
            fpText.alignment = TextAnchor.MiddleCenter;
            failPanel.SetActive(false);

            var hudSo = new SerializedObject(gameHud);
            hudSo.FindProperty("levelLoader").objectReferenceValue = loader;
            hudSo.FindProperty("retryButton").objectReferenceValue = retryBtn;
            hudSo.FindProperty("successPanel").objectReferenceValue = successPanel;
            hudSo.FindProperty("failPanel").objectReferenceValue = failPanel;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            loader.LoadLevel(Resources.Load<CircuitOneStroke.Data.LevelData>("Levels/Level_1"));

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
            AssetDatabase.Refresh();
            Debug.Log("Created Assets/Scenes/GameScene.unity - assign Level Data on LevelLoader if needed.");
        }

        private static GameObject CreateNodePrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "NodeView";
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            DestroyImmediate(go.GetComponent<MeshRenderer>());
            DestroyImmediate(go.GetComponent<MeshFilter>());
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = Color.gray;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            go.AddComponent<CircuitOneStroke.View.NodeView>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/NodeView.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateEdgePrefab()
        {
            var go = new GameObject("EdgeView");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = 0.1f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            go.AddComponent<CircuitOneStroke.View.EdgeView>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/EdgeView.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.45f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    tex.SetPixel(x, y, d <= radius ? Color.white : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
#endif
