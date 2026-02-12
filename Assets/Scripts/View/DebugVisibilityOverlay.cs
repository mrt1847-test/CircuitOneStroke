#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// 노드/엣지 타입·상태를 화면에 표시하는 디버그 오버레이.
    /// Console 없이 즉시 상태 확인 가능. F3 토글.
    /// </summary>
    public class DebugVisibilityOverlay : MonoBehaviour
    {
        public static bool Enabled
        {
            get => _enabled;
            set { _enabled = value; _instance?.Rebuild(); }
        }
        private static bool _enabled;
        private static DebugVisibilityOverlay _instance;

        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private Camera cam;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;

        private Canvas _canvas;
        private readonly List<GameObject> _labels = new List<GameObject>();

        private void Awake()
        {
            _instance = this;
            if (cam == null) cam = Camera.main;
            if (levelLoader == null) levelLoader = FindFirstObjectByType<LevelLoader>();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(toggleKey))
            {
                _enabled = !_enabled;
                Rebuild();
            }
            if (_enabled)
                RefreshLabels();
        }

        public void Rebuild()
        {
            ClearLabels();
            if (!_enabled || levelLoader == null || cam == null) return;

            EnsureCanvas();
            var data = levelLoader.LevelData;
            var runtime = levelLoader.Runtime;
            if (data?.nodes == null) return;

            var nodePos = new Dictionary<int, Vector2>();
            foreach (var nd in data.nodes)
                nodePos[nd.id] = nd.pos;

            foreach (var nd in data.nodes)
            {
                string text;
                if (nd.nodeType == NodeType.Bulb)
                    text = runtime != null && runtime.VisitedBulbs.Contains(nd.id) ? "Bulb(On)" : "Bulb(Off)";
                else if (nd.nodeType == NodeType.Switch)
                    text = "Switch";
                else
                    text = "Blocked";
                CreateLabel(nodePos[nd.id], text, new Color(0.9f, 0.95f, 1f, 0.95f));
            }

            if (data.edges != null && runtime != null)
            {
                foreach (var ed in data.edges)
                {
                    if (!nodePos.TryGetValue(ed.a, out var pa) || !nodePos.TryGetValue(ed.b, out var pb))
                        continue;
                    var mid = Vector2.Lerp(pa, pb, 0.5f);
                    string text = ed.diode != DiodeMode.None
                        ? $"Diode({(ed.diode == DiodeMode.AtoB ? "A→B" : "B→A")})"
                        : ed.gateGroupId >= 0
                            ? (runtime.IsGateOpen(ed.id) ? "Gate(Open)" : "Gate(Closed)")
                            : "Wire";
                    CreateLabel(mid, text, new Color(0.85f, 0.9f, 0.95f, 0.9f));
                }
            }
        }

        private void RefreshLabels()
        {
            if (levelLoader == null || cam == null || _labels.Count == 0) return;
            var data = levelLoader.LevelData;
            var runtime = levelLoader.Runtime;
            if (data?.nodes == null) return;

            var nodePos = new Dictionary<int, Vector2>();
            foreach (var nd in data.nodes)
                nodePos[nd.id] = nd.pos;

            int i = 0;
            foreach (var nd in data.nodes)
            {
                if (i >= _labels.Count) break;
                string text;
                if (nd.nodeType == NodeType.Bulb)
                    text = runtime != null && runtime.VisitedBulbs.Contains(nd.id) ? "Bulb(On)" : "Bulb(Off)";
                else if (nd.nodeType == NodeType.Switch)
                    text = "Switch";
                else
                    text = "Blocked";
                UpdateLabel(_labels[i], nodePos[nd.id], text);
                i++;
            }

            if (data.edges != null && runtime != null)
            {
                foreach (var ed in data.edges)
                {
                    if (i >= _labels.Count) break;
                    if (!nodePos.TryGetValue(ed.a, out var pa) || !nodePos.TryGetValue(ed.b, out var pb))
                        continue;
                    var mid = Vector2.Lerp(pa, pb, 0.5f);
                    string text = ed.diode != DiodeMode.None
                        ? $"Diode({(ed.diode == DiodeMode.AtoB ? "A→B" : "B→A")})"
                        : ed.gateGroupId >= 0
                            ? (runtime.IsGateOpen(ed.id) ? "Gate(Open)" : "Gate(Closed)")
                            : "Wire";
                    UpdateLabel(_labels[i], mid, text);
                    i++;
                }
            }
        }

        private void EnsureCanvas()
        {
            if (_canvas != null) return;
            var go = new GameObject("DebugVisibilityOverlay");
            go.transform.SetParent(transform);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = ViewRenderingConstants.OrderDebugOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            go.AddComponent<GraphicRaycaster>();
        }

        private void CreateLabel(Vector2 worldPos, string text, Color color)
        {
            if (_canvas == null) return;
            var go = new GameObject("DbgLabel");
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120, 24);
            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 11;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            UpdateLabel(go, worldPos, text);
            _labels.Add(go);
        }

        private void UpdateLabel(GameObject go, Vector2 worldPos, string text)
        {
            if (go == null || cam == null) return;
            var rt = go.GetComponent<RectTransform>();
            var label = go.GetComponent<Text>();
            if (rt == null || label == null) return;
            label.text = text;
            var sp = cam.WorldToScreenPoint(new Vector3(worldPos.x, worldPos.y, 0f));
            rt.anchoredPosition = new Vector2(sp.x - Screen.width * 0.5f, sp.y - Screen.height * 0.5f);
        }

        private void ClearLabels()
        {
            foreach (var go in _labels)
            {
                if (go != null) Destroy(go);
            }
            _labels.Clear();
        }
    }
}

#endif
