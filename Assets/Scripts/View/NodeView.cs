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

        [Header("Bulb")]
        [SerializeField] private Color bulbOffColor = Color.gray;
        [SerializeField] private Color bulbOnColor = Color.yellow;
        [Header("Switch")]
        [SerializeField] private Color switchColor = new Color(0.6f, 0.4f, 0.2f);
        [SerializeField] private Color switchHighlightColor = new Color(0.9f, 0.7f, 0.3f);
        [Header("Touch (mobile)")]
        [Tooltip("Collider radius = sprite half-size * this. >1 for easier touch on small nodes (e.g. 25-node grid).")]
        [SerializeField] private float colliderRadiusScale = 1.35f;

        private SpriteRenderer _sr;
        private NodeType _nodeType;
        private bool _visited;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            GameSettings.Instance.OnChanged += OnSettingsChanged;
            ApplyNodeSizeScale();
        }

        private void OnDisable()
        {
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
            if (_nodeType == NodeType.Switch)
                _sr.color = highlight ? switchHighlightColor : switchColor;
        }

        /// <summary>전구=방문 시 켜진 색, 스위치=고정 색.</summary>
        private void ApplyVisual()
        {
            if (_nodeType == NodeType.Bulb)
                _sr.color = _visited ? bulbOnColor : bulbOffColor;
            else
                _sr.color = switchColor;
        }
    }
}
