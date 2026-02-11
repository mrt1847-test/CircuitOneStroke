using System.Collections;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// 한 노드의 2D 표시. SpriteRenderer + Collider2D 필요.
    /// 전구=BulbShape 아이콘 + On/Off 글로우, 스위치=SwitchLever 아이콘. 형태로 구분.
    /// 스프라이트가 null이면 procedural fallback 적용 (가시성 보장).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class NodeView : MonoBehaviour
    {
        public int NodeId { get; private set; }

        [Header("Bulb")]
        [SerializeField] private Color bulbOffColor = new Color(0.25f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color bulbOnColor = new Color(0.35f, 0.65f, 1f, 1f);
        [Header("Switch")]
        [SerializeField] private Color switchColor = new Color(0.55f, 0.45f, 0.85f, 1f);
        [SerializeField] private Color switchHighlightColor = new Color(0.7f, 0.6f, 1f, 1f);
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

        /// <summary>스프라이트가 null이면 procedural fallback. 가시성 보장.</summary>
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

        /// <summary>전구=BulbShape, 스위치=SwitchLever. 형태로 구분.</summary>
        private void EnsureSpriteAndIcon()
        {
            if (_sr == null) return;
            EnsureBaseSprite();

            if (_iconSr == null)
            {
                var iconGo = new GameObject("NodeIcon");
                iconGo.transform.SetParent(transform, false);
                iconGo.transform.localPosition = Vector3.zero;
                iconGo.transform.localScale = Vector3.one * 0.65f;
                _iconSr = iconGo.AddComponent<SpriteRenderer>();
                _iconSr.sortingOrder = ViewRenderingConstants.OrderNodeIcon;
                _iconSr.color = Color.white;
            }

            if (_nodeType == NodeType.Bulb)
                _iconSr.sprite = ProceduralSprites.BulbShape;
            else
                _iconSr.sprite = ProceduralSprites.SwitchLever;
        }

        /// <summary>LevelLoader가 스폰 시 호출.</summary>
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

        /// <summary>전구 방문 여부.</summary>
        public void SetVisited(bool visited)
        {
            _visited = visited;
            ApplyVisual();
        }

        /// <summary>스위치 강조 토글.</summary>
        public void SetHighlight(bool highlight)
        {
            if (_sr == null) return;
            if (_nodeType == NodeType.Switch)
            {
                Color sc = IsBluePurpleHue(switchColor) ? new Color(0.55f, 0.45f, 0.85f, 1f) : switchColor;
                Color hi = IsBluePurpleHue(switchHighlightColor) ? new Color(0.7f, 0.6f, 1f, 1f) : switchHighlightColor;
                _sr.color = highlight ? hi : sc;
            }
        }

        /// <summary>Paused 시 재개 지점(꼬리 노드) 표시. 켜면 스케일 펄스, 끄면 원래 스케일 복원.</summary>
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

        private static bool IsBluePurpleHue(Color c)
        {
            Color.RGBToHSV(c, out float h, out _, out _);
            return h >= 0.5f && h <= 0.9f;
        }

        /// <summary>전구=방문 시 켜진 색, 스위치=고정 색. 베이스+아이콘 동기화.</summary>
        private void ApplyVisual()
        {
            if (_sr == null) return;
            if (_nodeType == NodeType.Bulb)
            {
                Color off = IsBluePurpleHue(bulbOffColor) ? new Color(0.25f, 0.55f, 0.95f, 1f) : bulbOffColor;
                Color on = IsBluePurpleHue(bulbOnColor) ? new Color(0.35f, 0.65f, 1f, 1f) : bulbOnColor;
                _sr.color = _visited ? on : off;
                if (_iconSr != null)
                    _iconSr.color = _visited ? new Color(1f, 1f, 0.95f, 1f) : new Color(0.9f, 0.9f, 1f, 1f);
            }
            else
            {
                Color sc = IsBluePurpleHue(switchColor) ? new Color(0.55f, 0.45f, 0.85f, 1f) : switchColor;
                _sr.color = sc;
                if (_iconSr != null)
                    _iconSr.color = Color.white;
            }
        }
    }
}
