using System.Collections;
using CircuitOneStroke.Core;
using CircuitOneStroke.Data;
using UnityEngine;

namespace CircuitOneStroke.View
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class NodeView : MonoBehaviour
    {
        public int NodeId { get; private set; }

        [Header("Bulb")]
        [SerializeField] private Color bulbOffColor = new Color(0.72f, 0.79f, 0.88f, 1f);
        [SerializeField] private Color bulbOnColor = new Color(1.00f, 0.84f, 0.36f, 1f);
        [SerializeField] private Color bulbVisitedCoreColor = new Color(1.00f, 0.97f, 0.84f, 1f);
        [SerializeField] private Color bulbVisitedHaloColor = new Color(0.44f, 0.88f, 1.00f, 0.62f);

        [Header("Switch")]
        [SerializeField] private Color switchColor = new Color(0.70f, 0.80f, 0.88f, 1f);
        [SerializeField] private Color switchHighlightColor = new Color(0.84f, 0.94f, 1f, 1f);

        [Header("Blocked")]
        [SerializeField] private Color blockedColor = new Color(0.22f, 0.22f, 0.24f, 1f);
        [SerializeField] private Color blockedHintColor = new Color(0.38f, 0.38f, 0.42f, 1f);

        [Header("Touch")]
        [SerializeField] private float colliderRadiusScale = 1.35f;

        [Header("Resume")]
        [SerializeField] private float resumePulseScaleAmount = 0.15f;
        [SerializeField] private float resumePulseSpeed = 4f;

        private SpriteRenderer _sr;
        private SpriteRenderer _iconSr;
        private SpriteRenderer _visitedHaloSr;
        private NodeType _nodeType;
        private bool _visited;
        private bool _resumeHighlight;
        private bool _hintModeActive;
        private bool _hintCandidate;
        private float _baseScaleFromSettings = 1f;
        private float _haloPulsePhase;
        private Coroutine _resumePulseCoroutine;

        private void Awake()
        {
            if (!Application.isPlaying)
                return;
            _sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            EnsureBaseSprite();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged += OnSettingsChanged;
            ApplyNodeSizeScale();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged -= OnSettingsChanged;
        }

        private void Update()
        {
            if (_visitedHaloSr == null || !_visitedHaloSr.gameObject.activeSelf)
                return;

            _haloPulsePhase += Time.deltaTime * 5.8f;
            float t = Mathf.Sin(_haloPulsePhase) * 0.5f + 0.5f;
            float s = 1.52f + t * 0.12f;
            _visitedHaloSr.transform.localScale = new Vector3(s, s, 1f);

            var c = bulbVisitedHaloColor;
            c.a = Mathf.Lerp(0.36f, bulbVisitedHaloColor.a, t);
            _visitedHaloSr.color = c;
        }

        private void OnSettingsChanged(GameSettingsData _) => ApplyNodeSizeScale();

        private void ApplyNodeSizeScale()
        {
            float scale = GameSettings.Instance?.Data != null
                ? GameSettings.Instance.NodeSizeValue switch
                {
                    NodeSize.Small => 0.85f,
                    NodeSize.Large => 1.2f,
                    _ => 1f
                }
                : 1f;

            _baseScaleFromSettings = scale;
            if (!_resumeHighlight)
                transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void EnsureBaseSprite()
        {
            if (_sr == null) return;
            if (_sr.sprite == null)
            {
                _sr.sprite = ProceduralSprites.Circle;
                _sr.color = Color.white;
            }
            _sr.sortingOrder = ViewRenderingConstants.OrderNodes;
        }

        private void EnsureSpriteAndIcon()
        {
            if (_sr == null) return;
            EnsureBaseSprite();

            if (_visitedHaloSr == null)
            {
                var haloGo = new GameObject("VisitedHalo");
                haloGo.transform.SetParent(transform, false);
                haloGo.transform.localPosition = Vector3.zero;

                _visitedHaloSr = haloGo.AddComponent<SpriteRenderer>();
                _visitedHaloSr.sprite = ProceduralSprites.Circle;
                _visitedHaloSr.sortingOrder = ViewRenderingConstants.OrderNodes - 1;
                _visitedHaloSr.color = bulbVisitedHaloColor;
                _visitedHaloSr.transform.localScale = new Vector3(1.55f, 1.55f, 1f);
                _visitedHaloSr.gameObject.SetActive(false);
            }

            if (_iconSr == null)
            {
                var iconGo = new GameObject("NodeIcon");
                iconGo.transform.SetParent(transform, false);
                iconGo.transform.localPosition = Vector3.zero;
                iconGo.transform.localScale = Vector3.one * 0.44f;

                _iconSr = iconGo.AddComponent<SpriteRenderer>();
                _iconSr.sortingOrder = ViewRenderingConstants.OrderNodeIcon;
                _iconSr.color = Color.white;
            }

            if (_nodeType == NodeType.Bulb)
            {
                _iconSr.sprite = ProceduralSprites.Circle;
                _iconSr.transform.localScale = Vector3.one * 0.34f;
            }
            else if (_nodeType == NodeType.Switch)
            {
                _iconSr.sprite = ProceduralSprites.SwitchLever;
                _iconSr.transform.localScale = Vector3.one * 0.50f;
            }
            else
            {
                _iconSr.sprite = ProceduralSprites.Circle;
                _iconSr.transform.localScale = Vector3.one * 0.38f;
            }
        }

        public void Setup(int nodeId, Vector2 pos, NodeType nodeType)
        {
            if (_sr == null)
                _sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

            NodeId = nodeId;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            _nodeType = nodeType;
            _visited = false;

            ApplyNodeSizeScale();
            EnsureSpriteAndIcon();
            ApplyVisual();
            ApplyCollider();
        }

        private void ApplyCollider()
        {
            if (colliderRadiusScale <= 0 || !TryGetComponent<CircleCollider2D>(out var col))
                return;

            float size = 0.5f;
            if (_sr != null && _sr.sprite != null)
                size = Mathf.Max(_sr.sprite.bounds.size.x, _sr.sprite.bounds.size.y) * 0.5f;

            col.radius = Mathf.Max(0.2f, size * colliderRadiusScale);
        }

        public void SetVisited(bool visited)
        {
            _visited = visited;
            ApplyVisual();
        }

        public void SetHighlight(bool highlight)
        {
            if (_sr == null) return;
            if (_nodeType == NodeType.Switch)
                _sr.color = highlight ? switchHighlightColor : switchColor;
        }

        public void SetMoveHint(bool isCandidate, bool hintModeActive)
        {
            _hintCandidate = isCandidate;
            _hintModeActive = hintModeActive;
            ApplyVisual();
        }

        public void SetResumeHighlight(bool on)
        {
            if (_resumeHighlight == on) return;
            _resumeHighlight = on;

            if (_resumePulseCoroutine != null)
            {
                StopCoroutine(_resumePulseCoroutine);
                _resumePulseCoroutine = null;
            }

            if (on)
                _resumePulseCoroutine = StartCoroutine(ResumePulseCoroutine());
            else
                transform.localScale = new Vector3(_baseScaleFromSettings, _baseScaleFromSettings, 1f);
        }

        private IEnumerator ResumePulseCoroutine()
        {
            while (_resumeHighlight)
            {
                float t = Mathf.Sin(Time.time * resumePulseSpeed) * 0.5f + 0.5f;
                float s = _baseScaleFromSettings * (1f + resumePulseScaleAmount * t);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            _resumePulseCoroutine = null;
        }

        private void ApplyVisual()
        {
            if (_sr == null) return;

            if (_nodeType == NodeType.Bulb)
            {
                Color baseColor = _visited ? bulbOnColor : bulbOffColor;
                if (_hintModeActive)
                {
                    if (_hintCandidate)
                        baseColor = Color.Lerp(baseColor, Color.white, _visited ? 0.24f : 0.34f);
                    else
                        baseColor = _visited
                            ? new Color(baseColor.r * 0.90f, baseColor.g * 0.90f, baseColor.b * 0.90f, 0.92f)
                            : new Color(baseColor.r * 0.66f, baseColor.g * 0.66f, baseColor.b * 0.66f, 0.72f);
                }

                _sr.color = baseColor;

                if (_iconSr != null)
                {
                    Color icon = _visited ? bulbVisitedCoreColor : new Color(0.16f, 0.30f, 0.44f, 0.95f);
                    if (_hintModeActive && !_hintCandidate)
                        icon = _visited
                            ? new Color(icon.r * 0.92f, icon.g * 0.92f, icon.b * 0.92f, 0.96f)
                            : new Color(icon.r * 0.62f, icon.g * 0.62f, icon.b * 0.62f, 0.72f);
                    _iconSr.color = icon;
                }

                if (_visitedHaloSr != null)
                {
                    _visitedHaloSr.gameObject.SetActive(_visited);
                    _visitedHaloSr.color = bulbVisitedHaloColor;
                }
            }
            else if (_nodeType == NodeType.Switch)
            {
                Color sc = switchColor;
                if (_hintModeActive)
                {
                    if (_hintCandidate) sc = Color.Lerp(sc, Color.white, 0.25f);
                    else sc = new Color(sc.r * 0.45f, sc.g * 0.45f, sc.b * 0.45f, 0.28f);
                }

                _sr.color = sc;

                if (_iconSr != null)
                {
                    Color icon = Color.white;
                    if (_hintModeActive && !_hintCandidate)
                        icon = new Color(0.55f, 0.55f, 0.55f, 0.62f);
                    _iconSr.color = icon;
                }

                if (_visitedHaloSr != null)
                    _visitedHaloSr.gameObject.SetActive(false);
            }
            else
            {
                Color bc = blockedColor;
                if (_hintModeActive)
                {
                    if (_hintCandidate) bc = blockedHintColor;
                    else bc = new Color(bc.r * 0.5f, bc.g * 0.5f, bc.b * 0.5f, 0.35f);
                }

                _sr.color = bc;

                if (_iconSr != null)
                    _iconSr.color = new Color(0f, 0f, 0f, _hintModeActive && !_hintCandidate ? 0.35f : 0.45f);

                if (_visitedHaloSr != null)
                    _visitedHaloSr.gameObject.SetActive(false);
            }
        }
    }
}
