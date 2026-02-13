#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// Ensures gameplay view prefabs used by LevelLoader exist.
    /// </summary>
    public static class GameplayPrefabFactory
    {
        private const string PrefabFolder = "Assets/Prefabs";
        private const string NodePrefabPath = "Assets/Prefabs/NodeView.prefab";
        private const string EdgePrefabPath = "Assets/Prefabs/EdgeView.prefab";

        public static void EnsureNodeAndEdgePrefabs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(NodePrefabPath) == null)
                CreateNodePrefab();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EdgePrefabPath) == null)
                CreateEdgePrefab();
        }

        private static GameObject CreateNodePrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var go = new GameObject("NodeView");
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            var circleSprite = CreateCircleSprite();
            if (sr != null && circleSprite != null)
            {
                sr.sprite = circleSprite;
                sr.color = new Color(0.72f, 0.76f, 0.88f, 1f);
            }

            var col = go.AddComponent<CircleCollider2D>();
            if (col != null)
                col.radius = 0.5f;

            go.AddComponent<CircuitOneStroke.View.NodeView>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, NodePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateEdgePrefab()
        {
            var go = new GameObject("EdgeView");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = 0.22f;
            go.AddComponent<CircuitOneStroke.View.EdgeView>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, EdgePrefabPath);
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
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    tex.SetPixel(x, y, d <= radius ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
#endif
