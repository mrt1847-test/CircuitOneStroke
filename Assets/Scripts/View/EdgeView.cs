using CircuitOneStroke.Core;
using CircuitOneStroke.Data;
using UnityEngine;

namespace CircuitOneStroke.View
{
    [RequireComponent(typeof(LineRenderer))]
    public class EdgeView : MonoBehaviour
    {
        public int EdgeId { get; private set; }

        [Header("Base")]
        [SerializeField] private Color wireColor = new Color(0.24f, 0.31f, 0.42f, 0.95f);
        [SerializeField] private Color wireHighlightColor = new Color(0.45f, 0.83f, 0.98f, 1f);
        [SerializeField] private Color gateClosedColor = new Color(0.70f, 0.28f, 0.28f, 1f);
        [SerializeField] private Color diodeRejectColor = new Color(0.95f, 0.36f, 0.28f, 1f);
        [SerializeField] private Color diodeMarkerColor = new Color(0.98f, 0.84f, 0.34f, 1f);

        [Header("Energized")]
        [SerializeField] private Color energizedA = new Color(0.52f, 0.88f, 1f, 1f);
        [SerializeField] private Color energizedB = new Color(1.00f, 0.96f, 0.72f, 1f);
        [SerializeField] private float energizedPulseSpeed = 8.0f;

        private LineRenderer _lr;
        private LineRenderer _lrSegmentA;
        private LineRenderer _lrSegmentB;
        private GameObject _gateSegmentsRoot;

        private Vector2 _posA;
        private Vector2 _posB;
        private DiodeMode _diode;
        private int _gateGroupId;
        private LevelRuntime _runtime;
        private bool _lastGateOpen;
        private bool _showReject;
        private float _rejectEndTime;
        private bool _hintModeActive;
        private bool _hintCandidate;
        private bool _curveForReadability;
        private readonly Vector3[] _curvePoints = new Vector3[9];
        private Transform _diodeMarker;
        private GameObject _gateClosedMarker;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>() ?? GetComponentInChildren<LineRenderer>();
            if (_lr == null) return;

            if (_lr.material == null)
            {
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                if (shader != null) _lr.material = new Material(shader);
            }

            _lr.positionCount = 2;
            _lr.useWorldSpace = true;
            if (_lr.startWidth < 0.06f) _lr.startWidth = 0.075f;
            if (_lr.endWidth < 0.06f) _lr.endWidth = 0.075f;
            _lr.numCapVertices = 5;
            _lr.numCornerVertices = 4;

            var r = _lr.GetComponent<Renderer>();
            if (r != null) r.sortingOrder = ViewRenderingConstants.OrderEdges;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged += OnSettingsChanged;

            if (_lr != null && _runtime != null)
            {
                if (_lr.material == null)
                {
                    var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                    if (shader != null) _lr.material = new Material(shader);
                }
                _lr.enabled = true;
                UpdateLinePositions();
                UpdateVisual();
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged -= OnSettingsChanged;
        }

        private void OnSettingsChanged(GameSettingsData _) => UpdateVisual();

        public void Setup(int edgeId, Vector2 posA, Vector2 posB, DiodeMode diode, int gateGroupId, bool initialGateOpen, LevelRuntime runtime, bool curveForReadability = false)
        {
            EdgeId = edgeId;
            _posA = posA;
            _posB = posB;
            _diode = diode;
            _gateGroupId = gateGroupId;
            _runtime = runtime;
            _curveForReadability = curveForReadability;
            _lastGateOpen = gateGroupId < 0 || initialGateOpen;
            _showReject = false;

            if (_diodeMarker != null) { Destroy(_diodeMarker.gameObject); _diodeMarker = null; }
            if (_gateClosedMarker != null) { Destroy(_gateClosedMarker); _gateClosedMarker = null; }
            if (_gateSegmentsRoot != null) { Destroy(_gateSegmentsRoot); _gateSegmentsRoot = null; }

            if (diode != DiodeMode.None)
                CreateDiodeMarker(diode);
            if (gateGroupId >= 0)
                CreateGateMarker();

            UpdateLinePositions();
            UpdateVisual();
        }

        public void SetMoveHint(bool isCandidate, bool hintModeActive)
        {
            _hintCandidate = isCandidate;
            _hintModeActive = hintModeActive;
            UpdateVisual();
        }

        private void CreateGateMarker()
        {
            _gateClosedMarker = new GameObject("GateMarker");
            _gateClosedMarker.transform.SetParent(transform);
            var mid = Vector2.Lerp(_posA, _posB, 0.5f);
            _gateClosedMarker.transform.position = new Vector3(mid.x, mid.y, -0.07f);
            float scale = Mathf.Max(ViewRenderingConstants.GateMarkerMinScale, 0.32f);

            var bgGo = new GameObject("GateBg");
            bgGo.transform.SetParent(_gateClosedMarker.transform, false);
            var bg = bgGo.AddComponent<SpriteRenderer>();
            bg.sprite = ProceduralSprites.Circle;
            bg.color = new Color(0.10f, 0.18f, 0.26f, 0.96f);
            bg.sortingOrder = ViewRenderingConstants.OrderGateMarker - 1;
            bgGo.transform.localScale = Vector3.one * 1.4f;

            var lockGo = new GameObject("GateLock");
            lockGo.transform.SetParent(_gateClosedMarker.transform, false);
            var sr = lockGo.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.GateLock;
            sr.color = gateClosedColor;
            sr.sortingOrder = ViewRenderingConstants.OrderGateMarker;

            _gateClosedMarker.transform.localScale = Vector3.one * scale;
            _gateClosedMarker.SetActive(false);
        }

        public void SetRejectFlash(bool on)
        {
            _showReject = on;
            _rejectEndTime = Time.time + 0.4f;
            UpdateVisual();
        }

        private void CreateDiodeMarker(DiodeMode diode)
        {
            var go = new GameObject("DiodeMarker");
            _diodeMarker = go.transform;
            _diodeMarker.SetParent(transform);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.DiodeTriangleBar;
            sr.color = diodeMarkerColor;
            sr.sortingOrder = ViewRenderingConstants.OrderDiodeMarker;

            Vector2 from;
            Vector2 to;
            if (diode == DiodeMode.AtoB) { from = _posA; to = _posB; }
            else { from = _posB; to = _posA; }

            var mid = Vector2.Lerp(from, to, 0.5f);
            _diodeMarker.position = new Vector3(mid.x, mid.y, -0.05f);

            var dir = (to - from).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _diodeMarker.rotation = Quaternion.Euler(0f, 0f, angle);
            _diodeMarker.localScale = Vector3.one * Mathf.Max(ViewRenderingConstants.DiodeMarkerMinScale, 0.28f);
        }

        private void UpdateLinePositions()
        {
            if (_lr == null) return;

            bool gateClosed = _gateGroupId >= 0 && _runtime != null && !_runtime.IsGateOpen(EdgeId) && !_showReject;
            _lr.enabled = !gateClosed;

            if (gateClosed)
            {
                float gap = 0.36f;
                var mid = Vector2.Lerp(_posA, _posB, 0.5f);
                var dir = (_posB - _posA).normalized;
                var gapStart = mid - dir * gap;
                var gapEnd = mid + dir * gap;

                EnsureGateSegments();

                _lrSegmentA.positionCount = 2;
                _lrSegmentA.SetPosition(0, new Vector3(_posA.x, _posA.y, 0f));
                _lrSegmentA.SetPosition(1, new Vector3(gapStart.x, gapStart.y, 0f));

                _lrSegmentB.positionCount = 2;
                _lrSegmentB.SetPosition(0, new Vector3(gapEnd.x, gapEnd.y, 0f));
                _lrSegmentB.SetPosition(1, new Vector3(_posB.x, _posB.y, 0f));
                return;
            }

            if (_gateSegmentsRoot != null) _gateSegmentsRoot.SetActive(false);

            if (_curveForReadability && !_showReject)
                DrawCurve(_lr, _posA, _posB);
            else
            {
                _lr.positionCount = 2;
                _lr.SetPosition(0, new Vector3(_posA.x, _posA.y, 0f));
                _lr.SetPosition(1, new Vector3(_posB.x, _posB.y, 0f));
            }
        }

        private void DrawCurve(LineRenderer lr, Vector2 a, Vector2 b)
        {
            Vector2 mid = Vector2.Lerp(a, b, 0.5f);
            Vector2 dir = (b - a).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float sign = (EdgeId % 2 == 0) ? 1f : -1f;
            float offset = Mathf.Clamp(Vector2.Distance(a, b) * 0.13f, 0.08f, 0.34f);
            Vector2 control = mid + perp * offset * sign;

            lr.positionCount = _curvePoints.Length;
            for (int i = 0; i < _curvePoints.Length; i++)
            {
                float t = i / (float)(_curvePoints.Length - 1);
                Vector2 p = (1f - t) * (1f - t) * a + 2f * (1f - t) * t * control + t * t * b;
                _curvePoints[i] = new Vector3(p.x, p.y, 0f);
                lr.SetPosition(i, _curvePoints[i]);
            }
        }

        private void EnsureGateSegments()
        {
            if (_lrSegmentA != null && _lrSegmentB != null)
            {
                _gateSegmentsRoot.SetActive(true);
                return;
            }

            _gateSegmentsRoot = new GameObject("GateSegments");
            _gateSegmentsRoot.transform.SetParent(transform);

            _lrSegmentA = CreateSegmentLR("SegA");
            _lrSegmentB = CreateSegmentLR("SegB");
        }

        private LineRenderer CreateSegmentLR(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_gateSegmentsRoot.transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = lr.endWidth = _lr.startWidth;
            lr.startColor = lr.endColor = _lr.startColor;
            lr.material = _lr.material != null ? _lr.material : new Material(Shader.Find("Sprites/Default"));

            var r = lr.GetComponent<Renderer>();
            if (r != null) r.sortingOrder = ViewRenderingConstants.OrderEdges;

            return lr;
        }

        private void Update()
        {
            if (_showReject && Time.time >= _rejectEndTime)
            {
                _showReject = false;
                UpdateVisual();
            }

            if (_runtime != null && _gateGroupId >= 0)
            {
                bool open = _runtime.IsGateOpen(EdgeId);
                if (open != _lastGateOpen)
                {
                    _lastGateOpen = open;
                    UpdateLinePositions();
                    UpdateVisual();
                }
            }

            if (_runtime != null && IsEdgeEnergizedByStroke())
                UpdateVisual();
        }

        private bool IsEdgeEnergizedByStroke()
        {
            if (_runtime?.StrokeNodes == null || _runtime.StrokeNodes.Count < 2 || _runtime.Graph == null)
                return false;

            var stroke = _runtime.StrokeNodes;
            for (int i = 1; i < stroke.Count; i++)
            {
                if (!_runtime.Graph.TryGetEdge(stroke[i - 1], stroke[i], out var edge) || edge == null)
                    continue;
                if (edge.id == EdgeId)
                    return true;
            }

            return false;
        }

        private void UpdateVisual()
        {
            if (_lr == null) return;

            bool colorBlind = GameSettings.Instance?.Data?.colorBlindMode ?? false;
            Color rejectCol = colorBlind ? new Color(0.9f, 0.5f, 0.1f, 1f) : diodeRejectColor;
            Color gateCol = colorBlind ? new Color(0.3f, 0.4f, 0.7f, 1f) : gateClosedColor;

            if (_showReject)
            {
                UpdateLinePositions();
                _lr.positionCount = 2;
                _lr.SetPosition(0, new Vector3(_posA.x, _posA.y, 0f));
                _lr.SetPosition(1, new Vector3(_posB.x, _posB.y, 0f));
                _lr.startColor = _lr.endColor = rejectCol;
                _lr.startWidth = _lr.endWidth = 0.09f;
                if (_gateClosedMarker != null) _gateClosedMarker.SetActive(false);
                return;
            }

            bool gateClosed = _gateGroupId >= 0 && _runtime != null && !_runtime.IsGateOpen(EdgeId);
            bool energized = !gateClosed && IsEdgeEnergizedByStroke();

            Color lineCol;
            if (gateClosed)
            {
                lineCol = gateCol;
            }
            else if (energized)
            {
                float t = Mathf.Sin(Time.time * energizedPulseSpeed + EdgeId * 0.37f) * 0.5f + 0.5f;
                lineCol = Color.Lerp(energizedA, energizedB, t);
            }
            else
            {
                lineCol = wireColor;
            }

            if (_hintModeActive)
            {
                if (_hintCandidate)
                {
                    if (!energized)
                        lineCol = Color.Lerp(lineCol, wireHighlightColor, 0.52f);
                }
                else if (!energized)
                {
                    lineCol = new Color(lineCol.r * 0.60f, lineCol.g * 0.60f, lineCol.b * 0.60f, 0.52f);
                }
            }

            _lr.startColor = _lr.endColor = lineCol;

            float width;
            if (energized) width = 0.105f;
            else if (_hintModeActive) width = _hintCandidate ? 0.085f : 0.064f;
            else width = 0.075f;

            _lr.startWidth = _lr.endWidth = width;
            if (_lr.material != null)
                _lr.material.color = Color.white;

            UpdateLinePositions();

            if (_lrSegmentA != null)
            {
                _lrSegmentA.startWidth = _lrSegmentA.endWidth = _lr.startWidth;
                _lrSegmentB.startWidth = _lrSegmentB.endWidth = _lr.startWidth;
                _lrSegmentA.startColor = _lrSegmentA.endColor = gateCol;
                _lrSegmentB.startColor = _lrSegmentB.endColor = gateCol;
            }

            if (_gateClosedMarker != null)
            {
                _gateClosedMarker.SetActive(gateClosed);
                if (gateClosed)
                {
                    var mid = Vector2.Lerp(_posA, _posB, 0.5f);
                    _gateClosedMarker.transform.position = new Vector3(mid.x, mid.y, -0.07f);
                    var lockSr = _gateClosedMarker.transform.Find("GateLock")?.GetComponent<SpriteRenderer>();
                    if (lockSr != null) lockSr.color = gateCol;
                }
            }
        }
    }
}
