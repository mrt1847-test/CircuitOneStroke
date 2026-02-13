using System.Collections;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// ???몃뱶??2D ??�떆. SpriteRenderer + Collider2D ?꾩슂.
    /// ?꾧뎄=BulbShape ?꾩씠??+ On/Off 湲濡쒖?? ??�쐞�?SwitchLever ?꾩씠?? ?뺥깭�??�щ텇.
    /// ??�봽??�씠?�? null??�??procedural fallback ?곸슜 (媛??�꽦 蹂댁??.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class NodeView : MonoBehaviour
    {
        public int NodeId { get; private set; }

        [Header("Bulb")]
        [SerializeField] private Color bulbOffColor = new Color(0.86f, 0.92f, 0.97f, 1f);
        [SerializeField] private Color bulbOnColor = new Color(0.99f, 0.80f, 0.34f, 1f);
        [Header("Switch")]
        [SerializeField] private Color switchColor = new Color(0.70f, 0.80f, 0.88f, 1f);
        [SerializeField] private Color switchHighlightColor = new Color(0.84f, 0.94f, 1f, 1f);
        [Header("Blocked")]
        [SerializeField] private Color blockedColor = new Color(0.22f, 0.22f, 0.24f, 1f);
        [SerializeField] private Color blockedHintColor = new Color(0.38f, 0.38f, 0.42f, 1f);
        [Header("Touch (mobile)")]
        [Tooltip("Collider radius = sprite half-size * this. >1 for easier touch on small nodes.")]
        [SerializeField] private float colliderRadiusScale = 1.35f;
        [Header("Resume (Paused state)")]
        [SerializeField] private float resumePulseScaleAmount = 0.15f;
        [SerializeField] private float resumePulseSpeed = 4f;

        private SpriteRenderer _sr;
        private SpriteRenderer _iconSr;
        private NodeType _nodeType;
        private bool _visited;
        private bool _resumeHighlight;
        private bool _hintModeActive;
        private bool _hintCandidate;
        private float _baseScaleFromSettings = 1f;
        private Coroutine _resumePulseCoroutine;

        private void Awake()
        {
            if (!Application.isPlaying)
                return;
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null)
                _sr = GetComponentInChildren<SpriteRenderer>();
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

        /// <summary>??�봽??�씠?�? null??�??procedural fallback. 媛??�꽦 蹂댁??</summary>
        private void EnsureBaseSprite()
        {
            if (_sr == null) return;
            if (_sr.sprite == null)
            {
                _sr.sprite = ProceduralSprites.Circle;
                _sr.color = new Color(1f, 1f, 1f, 1f);
            }
            _sr.sortingOrder = ViewRenderingConstants.OrderNodes;
        }

        /// <summary>?꾧뎄=BulbShape, ??�쐞�?SwitchLever. ?뺥깭�??�щ텇.</summary>
        private void EnsureSpriteAndIcon()
        {
            if (_sr == null) return;
            EnsureBaseSprite();

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

        /// <summary>LevelLoader媛 ??�룿 ???몄텧.</summary>
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

        /// <summary>?꾧뎄 諛⑸Ц ???.</summary>
        public void SetVisited(bool visited)
        {
            _visited = visited;
            ApplyVisual();
        }

        /// <summary>??�쐞�?媛뺤???�?.</summary>
        public void SetHighlight(bool highlight)
        {
            if (_sr == null) return;
            if (_nodeType == NodeType.Switch)
            {
                _sr.color = highlight ? switchHighlightColor : switchColor;
            }
        }

        public void SetMoveHint(bool isCandidate, bool hintModeActive)
        {
            _hintCandidate = isCandidate;
            _hintModeActive = hintModeActive;
            ApplyVisual();
        }

        /// <summary>Paused ????�?吏???�щ━ ?몃뱶) ??�떆. ?�쒕????????꾩뒪, ?꾨㈃ ?�?�� ?????蹂듭??</summary>
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

        /// <summary>?꾧뎄=諛⑸Ц ???�쒖�??? ??�쐞�??�좎???? 踰좎????꾩씠????�린??</summary>
        private void ApplyVisual()
        {
            if (_sr == null) return;
            if (_nodeType == NodeType.Bulb)
            {
                Color off = bulbOffColor;
                Color on = bulbOnColor;
                Color baseColor = _visited ? on : off;
                if (_hintModeActive)
                {
                    if (_hintCandidate)
                        baseColor = Color.Lerp(baseColor, Color.white, 0.30f);
                    else
                        baseColor = new Color(baseColor.r * 0.45f, baseColor.g * 0.45f, baseColor.b * 0.45f, 0.28f);
                }
                _sr.color = baseColor;
                if (_iconSr != null)
                {
                    Color icon = _visited ? new Color(0.93f, 0.30f, 0.22f, 1f) : new Color(0.20f, 0.36f, 0.50f, 0.95f);
                    if (_hintModeActive && !_hintCandidate)
                        icon = new Color(icon.r * 0.45f, icon.g * 0.45f, icon.b * 0.45f, 0.28f);
                    _iconSr.color = icon;
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
                        icon = new Color(0.45f, 0.45f, 0.45f, 0.28f);
                    _iconSr.color = icon;
                }
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
                    _iconSr.color = new Color(0f, 0f, 0f, _hintModeActive && !_hintCandidate ? 0.15f : 0.45f);
            }
        }
    }
}

