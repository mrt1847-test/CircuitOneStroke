using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// 한 노드의 2D 표시. SpriteRenderer + Collider2D 필요. 전구=미방문/방문 색, 스위치=고정 색.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class NodeView : MonoBehaviour
    {
        public int NodeId { get; private set; }

        [Header("Bulb")] // 라이트 테마: 흰 배경 + 블루 노드 (참고 스크린샷)
        [SerializeField] private Color bulbOffColor = new Color(0.25f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color bulbOnColor = new Color(0.35f, 0.65f, 1f, 1f);
        [Header("Switch")]
        [SerializeField] private Color switchColor = new Color(0.25f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color switchHighlightColor = new Color(0.4f, 0.7f, 1f, 1f);
        [Header("Touch (mobile)")]
        [Tooltip("Collider radius = sprite half-size * this. >1 for easier touch on small nodes (e.g. 25-node grid).")]
        [SerializeField] private float colliderRadiusScale = 1.35f;

        private SpriteRenderer _sr;
        private NodeType _nodeType;
        private bool _visited;

        private void Awake()
        {
            if (!Application.isPlaying)
                return;
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null)
                _sr = GetComponentInChildren<SpriteRenderer>();
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
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>LevelLoader가 스폰 시 호출. id·위치·타입 설정 후 시각 적용. 터치용 콜라이더 확대 적용.</summary>
        public void Setup(int nodeId, Vector2 pos, NodeType nodeType)
        {
            if (_sr == null)
                _sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            NodeId = nodeId;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            _nodeType = nodeType;
            _visited = false;
            ApplyNodeSizeScale();
            ApplyVisual();
            if (colliderRadiusScale > 0 && _sr != null && _sr.sprite != null && TryGetComponent<CircleCollider2D>(out var col))
            {
                float size = Mathf.Max(_sr.sprite.bounds.size.x, _sr.sprite.bounds.size.y) * 0.5f;
                col.radius = Mathf.Max(0.2f, size * colliderRadiusScale);
            }
        }

        /// <summary>전구 방문 여부. LevelLoader.RefreshNodeViews에서 호출.</summary>
        public void SetVisited(bool visited)
        {
            _visited = visited;
            ApplyVisual();
        }

        /// <summary>스위치일 때만. 강조 색 토글.</summary>
        public void SetHighlight(bool highlight)
        {
            if (_sr == null) return;
            if (_nodeType == NodeType.Switch)
            {
                Color sc = IsBluePurpleHue(switchColor) ? new Color(0.25f, 0.55f, 0.95f, 1f) : switchColor;
                Color hi = IsBluePurpleHue(switchHighlightColor) ? new Color(0.4f, 0.7f, 1f, 1f) : switchHighlightColor;
                _sr.color = highlight ? hi : sc;
            }
        }

        /// <summary>HSV Hue가 파랑~보라 구간이면 true. 남색 배경과 유사해서 골드로 덮어쓸 때 씀.</summary>
        private static bool IsBluePurpleHue(Color c)
        {
            Color.RGBToHSV(c, out float h, out _, out _);
            return h >= 0.5f && h <= 0.9f;
        }

        /// <summary>전구=방문 시 켜진 색, 스위치=고정 색. 라이트 테마: 블루 계열로 통일.</summary>
        private void ApplyVisual()
        {
            if (_sr == null) return;
            if (_nodeType == NodeType.Bulb)
            {
                Color off = IsBluePurpleHue(bulbOffColor) ? new Color(0.25f, 0.55f, 0.95f, 1f) : bulbOffColor;
                Color on = IsBluePurpleHue(bulbOnColor) ? new Color(0.35f, 0.65f, 1f, 1f) : bulbOnColor;
                _sr.color = _visited ? on : off;
            }
            else
            {
                Color sc = IsBluePurpleHue(switchColor) ? new Color(0.25f, 0.55f, 0.95f, 1f) : switchColor;
                _sr.color = sc;
            }
        }
    }
}
