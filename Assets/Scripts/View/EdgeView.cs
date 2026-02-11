using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// 한 엣지의 선 표시. LineRenderer + 다이오드=큰 삼각형+바 마커, 게이트 닫힘=끊긴 선+락 마커.
    /// 형태로 구분 (색만 의존 금지). 마커는 화면에서 20~28px급으로 인지 가능.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class EdgeView : MonoBehaviour
    {
        public int EdgeId { get; private set; }

        [Header("Colors")]
        [SerializeField] private Color wireColor = new Color(0.25f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color wireHighlightColor = new Color(0.35f, 0.65f, 1f, 1f);
        [SerializeField] private Color gateClosedColor = new Color(0.75f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color diodeRejectColor = new Color(1f, 0.4f, 0.3f, 1f);
        [SerializeField] private Color diodeMarkerColor = new Color(1f, 0.88f, 0.4f, 1f);

        private LineRenderer _lr;
        private LineRenderer _lrSegmentA;
        private LineRenderer _lrSegmentB;
        private GameObject _gateSegmentsRoot;
        private Vector2 _posA;
        private Vector2 _posB;
        private Data.DiodeMode _diode;
        private int _gateGroupId;
        private LevelRuntime _runtime;
        private bool _lastGateOpen;
        private bool _showReject;
        private float _rejectEndTime;
        private Transform _diodeMarker;
        private GameObject _gateClosedMarker;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.positionCount = 2;
            _lr.useWorldSpace = true;
            if (_lr.startWidth < 0.2f) _lr.startWidth = 0.22f;
            if (_lr.endWidth < 0.2f) _lr.endWidth = 0.22f;
            var r = _lr.GetComponent<Renderer>();
            if (r != null)
                r.sortingOrder = ViewRenderingConstants.OrderEdges;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged += OnSettingsChanged;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged -= OnSettingsChanged;
        }

        private void OnSettingsChanged(GameSettingsData _) => UpdateVisual();

        public void Setup(int edgeId, Vector2 posA, Vector2 posB, Data.DiodeMode diode, int gateGroupId, bool initialGateOpen, LevelRuntime runtime)
        {
            EdgeId = edgeId;
            _posA = posA;
            _posB = posB;
            _diode = diode;
            _gateGroupId = gateGroupId;
            _runtime = runtime;
            _lastGateOpen = gateGroupId < 0 || initialGateOpen;
            _showReject = false;

            if (_diodeMarker != null) { Destroy(_diodeMarker.gameObject); _diodeMarker = null; }
            if (_gateClosedMarker != null) { Destroy(_gateClosedMarker); _gateClosedMarker = null; }
            if (_gateSegmentsRoot != null) { Destroy(_gateSegmentsRoot); _gateSegmentsRoot = null; }

            if (diode != Data.DiodeMode.None)
                CreateDiodeMarker(diode);
            if (gateGroupId >= 0)
                CreateGateMarker();

            UpdateLinePositions();
            UpdateVisual();
        }

        /// <summary>게이트 닫힘: 배경 원(선 가림) + 락 마커. 끊김/잠금 느낌.</summary>
        private void CreateGateMarker()
        {
            _gateClosedMarker = new GameObject("GateMarker");
            _gateClosedMarker.transform.SetParent(transform);
            var mid = Vector2.Lerp(_posA, _posB, 0.5f);
            _gateClosedMarker.transform.position = new Vector3(mid.x, mid.y, -0.07f);
            float scale = Mathf.Max(ViewRenderingConstants.GateMarkerMinScale, 0.4f);

            var bgGo = new GameObject("GateBg");
            bgGo.transform.SetParent(_gateClosedMarker.transform, false);
            var bg = bgGo.AddComponent<SpriteRenderer>();
            bg.sprite = ProceduralSprites.Circle;
            bg.color = new Color(0.15f, 0.12f, 0.18f, 0.98f);
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

        /// <summary>다이오드: 큰 삼각형+바 마커. 화면에서 20~28px급. 형태로 1초 내 인지.</summary>
        private void CreateDiodeMarker(Data.DiodeMode diode)
        {
            var go = new GameObject("DiodeMarker");
            _diodeMarker = go.transform;
            _diodeMarker.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.DiodeTriangleBar;
            sr.color = diodeMarkerColor;
            sr.sortingOrder = ViewRenderingConstants.OrderDiodeMarker;
            Vector2 from, to;
            if (diode == Data.DiodeMode.AtoB) { from = _posA; to = _posB; }
            else { from = _posB; to = _posA; }
            var mid = Vector2.Lerp(from, to, 0.5f);
            _diodeMarker.position = new Vector3(mid.x, mid.y, -0.05f);
            var dir = (to - from).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _diodeMarker.rotation = Quaternion.Euler(0, 0, angle);
            _diodeMarker.localScale = Vector3.one * Mathf.Max(ViewRenderingConstants.DiodeMarkerMinScale, 0.4f);
        }

        /// <summary>게이트 닫힘 시 선을 끊김(gap)으로 표시. A---[gap]---B. 두 개 LineRenderer로 실제 끊김.</summary>
        private void UpdateLinePositions()
        {
            bool gateClosed = _gateGroupId >= 0 && _runtime != null && !_runtime.IsGateOpen(EdgeId) && !_showReject;
            _lr.enabled = !gateClosed;
            if (gateClosed)
            {
                float gap = 0.55f;
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
            }
            else
            {
                if (_gateSegmentsRoot != null) _gateSegmentsRoot.SetActive(false);
                _lr.positionCount = 2;
                _lr.SetPosition(0, new Vector3(_posA.x, _posA.y, 0f));
                _lr.SetPosition(1, new Vector3(_posB.x, _posB.y, 0f));
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
        }

        private static bool IsOldDarkHue(Color c)
        {
            Color.RGBToHSV(c, out float h, out _, out _);
            return h >= 0.5f && h <= 0.9f || (c.r >= 0.8f && c.g >= 0.7f && c.b < 0.8f);
        }

        private void UpdateVisual()
        {
            bool colorBlind = GameSettings.Instance?.Data?.colorBlindMode ?? false;
            Color rejectCol = colorBlind ? new Color(0.9f, 0.5f, 0.1f, 1f) : diodeRejectColor;
            Color gateCol = colorBlind ? new Color(0.3f, 0.4f, 0.7f, 1f) : gateClosedColor;
            Color wire = IsOldDarkHue(wireColor) ? new Color(0.25f, 0.55f, 0.95f, 1f) : wireColor;

            if (_showReject)
            {
                UpdateLinePositions();
                _lr.positionCount = 2;
                _lr.SetPosition(0, new Vector3(_posA.x, _posA.y, 0f));
                _lr.SetPosition(1, new Vector3(_posB.x, _posB.y, 0f));
                _lr.startColor = _lr.endColor = rejectCol;
                if (_gateClosedMarker != null) _gateClosedMarker.SetActive(false);
                return;
            }

            bool gateClosed = _gateGroupId >= 0 && _runtime != null && !_runtime.IsGateOpen(EdgeId);
            Color lineCol = gateClosed ? gateCol : wire;
            _lr.startColor = _lr.endColor = lineCol;
            if (_lr.material != null)
                _lr.material.color = Color.white;

            UpdateLinePositions();

            if (_lrSegmentA != null)
            {
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
