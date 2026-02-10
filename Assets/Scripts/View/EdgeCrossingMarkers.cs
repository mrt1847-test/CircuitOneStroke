using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Core;
using CircuitOneStroke.Generation;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// Optionally draws a small "bridge" marker at each edge crossing to indicate non-connection.
    /// Keeps edges visually straight; markers clarify that crossing edges do not connect.
    /// </summary>
    public class EdgeCrossingMarkers : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private float markerScale = 0.25f;
        [SerializeField] private Color markerColor = new Color(0.4f, 0.4f, 0.5f, 0.9f);

        private LevelData _lastLevel;
        private readonly List<GameObject> _markers = new List<GameObject>();

        private void Update()
        {
            if (levelLoader == null) return;
            LevelData level = levelLoader.LevelData;
            if (level == _lastLevel) return;
            _lastLevel = level;
            Rebuild();
        }

        private void Rebuild()
        {
            foreach (var m in _markers)
            {
                if (m != null) Destroy(m);
            }
            _markers.Clear();

            if (_lastLevel?.nodes == null || _lastLevel.edges == null) return;
            int n = _lastLevel.nodes.Length;
            var positions = new List<Vector2>();
            for (int i = 0; i < n; i++)
                positions.Add(Vector2.zero);
            foreach (var nd in _lastLevel.nodes)
            {
                if (nd.id >= 0 && nd.id < n)
                    positions[nd.id] = nd.pos;
            }

            var points = AestheticEvaluator.GetCrossingPoints(_lastLevel.edges, positions, n);
            foreach (Vector2 pt in points)
            {
                var go = new GameObject("BridgeMarker");
                go.transform.SetParent(transform);
                go.transform.position = new Vector3(pt.x, pt.y, -0.06f);
                go.transform.localScale = Vector3.one * markerScale;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreateBridgeSprite();
                sr.color = markerColor;
                sr.sortingOrder = 0;
                _markers.Add(go);
            }
        }

        private static Sprite CreateBridgeSprite()
        {
            const int size = 16;
            var tex = new Texture2D(size, size);
            float cx = size * 0.5f;
            float r = size * 0.4f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
                    tex.SetPixel(x, y, d <= r ? Color.white : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
