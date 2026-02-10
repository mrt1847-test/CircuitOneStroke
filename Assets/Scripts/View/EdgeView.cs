using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// 한 엣지의 선 표시. LineRenderer + 다이오드 방향 화살표 + 게이트 닫힘 시 X 마커.
    /// 런타임 게이트 상태·리젝트 플래시 반영.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class EdgeView : MonoBehaviour
    {
        public int EdgeId { get; private set; }

        [Header("Colors")]
        [SerializeField] private Color wireColor = new Color(0.3f, 0.3f, 0.4f);
        [SerializeField] private Color wireHighlightColor = new Color(0.5f, 0.5f, 0.8f);
        [SerializeField] private Color gateClosedColor = new Color(0.5f, 0.2f, 0.2f);
        [SerializeField] private Color diodeRejectColor = new Color(1f, 0.3f, 0.2f);
        [SerializeField] private Color diodeArrowColor = new Color(0.9f, 0.7f, 0.2f);

        private LineRenderer _lr;
        private Vector2 _posA;
        private Vector2 _posB;
        private Data.DiodeMode _diode;
        private int _gateGroupId;
        private LevelRuntime _runtime;
        private bool _lastGateOpen;
        private bool _showReject;
        private float _rejectEndTime;
        private Transform _arrowMarker;
        private GameObject _gateClosedMarker;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.positionCount = 2;
            _lr.useWorldSpace = true;
        }

        private void OnEnable()
        {
            GameSettings.Instance.OnChanged += OnSettingsChanged;
        }

        private void OnDisable()
        {
            GameSettings.Instance.OnChanged -= OnSettingsChanged;
        }

        private void OnSettingsChanged(GameSettingsData _) => UpdateVisual();

        /// <summary>LevelLoader가 스폰 시 호출. 위치·다이오드·게이트·런타임 연결 후 시각 구성.</summary>
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

            _lr.SetPosition(0, new Vector3(_posA.x, _posA.y, 0f));
            _lr.SetPosition(1, new Vector3(_posB.x, _posB.y, 0f));

            if (_arrowMarker != null) Destroy(_arrowMarker.gameObject);
            _arrowMarker = null;
            if (_gateClosedMarker != null) Destroy(_gateClosedMarker);
            _gateClosedMarker = null;
            if (diode != Data.DiodeMode.None)
                CreateDiodeArrow(diode);  // arrow shape provides non-color cue for colorblind
            if (gateGroupId >= 0)
                CreateGateMarker();

            UpdateVisual();
        }

        private void CreateGateMarker()
        {
            _gateClosedMarker = new GameObject("GateMarker");
            _gateClosedMarker.transform.SetParent(transform);
            var sr = _gateClosedMarker.AddComponent<SpriteRenderer>();
            sr.sprite = CreateXSprite();
            sr.color = gateClosedColor;
            sr.sortingOrder = 2;
            var mid = Vector2.Lerp(_posA, _posB, 0.5f);
            _gateClosedMarker.transform.position = new Vector3(mid.x, mid.y, -0.08f);
            _gateClosedMarker.transform.localScale = Vector3.one * 0.3f;
            _gateClosedMarker.SetActive(false);
        }

        private static Sprite CreateXSprite()
        {
            var tex = new Texture2D(16, 16);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    bool diag1 = Mathf.Abs(x - y) < 2;
                    bool diag2 = Mathf.Abs(x - (15 - y)) < 2;
                    tex.SetPixel(x, y, (diag1 || diag2) ? Color.white : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        }

        /// <summary>이동 불가(리젝트) 시 잠깐 빨간색 플래시. TouchInputController에서 호출.</summary>
        public void SetRejectFlash(bool on)
        {
            _showReject = on;
            _rejectEndTime = Time.time + 0.4f;
            UpdateVisual();
        }

        private void CreateDiodeArrow(Data.DiodeMode diode)
        {
            var arrowGo = new GameObject("DiodeArrow");
            _arrowMarker = arrowGo.transform;
            _arrowMarker.SetParent(transform);
            var sr = arrowGo.AddComponent<SpriteRenderer>();
            sr.sprite = CreateArrowSprite();
            sr.color = diodeArrowColor;
            sr.sortingOrder = 1;
            Vector2 from, to;
            if (diode == Data.DiodeMode.AtoB) { from = _posA; to = _posB; }
            else { from = _posB; to = _posA; }
            var mid = Vector2.Lerp(from, to, 0.5f);
            _arrowMarker.position = new Vector3(mid.x, mid.y, -0.05f);
            var dir = (to - from).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _arrowMarker.rotation = Quaternion.Euler(0, 0, angle);
            _arrowMarker.localScale = Vector3.one * 0.4f;
        }

        private static Sprite CreateArrowSprite()
        {
            var tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float t = x / 31f;
                    float h = 0.5f - Mathf.Abs(y - 15.5f) / 31f * (t < 0.5f ? 1f : (1f - (t - 0.5f) * 2f));
                    tex.SetPixel(x, y, h >= 0 && h <= 0.5f ? Color.white : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
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
                    UpdateVisual();
                }
            }
        }

        private void UpdateVisual()
        {
            bool colorBlind = GameSettings.Instance?.Data?.colorBlindMode ?? false;
            Color rejectCol = colorBlind ? new Color(0.9f, 0.5f, 0.1f, 1f) : diodeRejectColor;  // orange vs red
            Color gateCol = colorBlind ? new Color(0.3f, 0.4f, 0.7f, 1f) : gateClosedColor;    // blue vs red

            if (_showReject)
            {
                _lr.startColor = _lr.endColor = rejectCol;
                if (_gateClosedMarker != null) _gateClosedMarker.SetActive(false);
                return;
            }
            bool gateClosed = _gateGroupId >= 0 && _runtime != null && !_runtime.IsGateOpen(EdgeId);
            _lr.startColor = _lr.endColor = gateClosed ? gateCol : wireColor;
            if (_gateClosedMarker != null)
            {
                _gateClosedMarker.SetActive(gateClosed);
                if (gateClosed)
                {
                    var mid = Vector2.Lerp(_posA, _posB, 0.5f);
                    _gateClosedMarker.transform.position = new Vector3(mid.x, mid.y, -0.08f);
                    if (_gateClosedMarker.TryGetComponent<SpriteRenderer>(out var sr))
                        sr.color = gateCol;
                }
            }
        }
    }
}
