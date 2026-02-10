using UnityEngine;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.View
{
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

        private SpriteRenderer _sr;
        private NodeType _nodeType;
        private bool _visited;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Setup(int nodeId, Vector2 pos, NodeType nodeType)
        {
            NodeId = nodeId;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            _nodeType = nodeType;
            _visited = false;
            ApplyVisual();
        }

        public void SetVisited(bool visited)
        {
            _visited = visited;
            ApplyVisual();
        }

        public void SetHighlight(bool highlight)
        {
            if (_nodeType == NodeType.Switch)
                _sr.color = highlight ? switchHighlightColor : switchColor;
        }

        private void ApplyVisual()
        {
            if (_nodeType == NodeType.Bulb)
                _sr.color = _visited ? bulbOnColor : bulbOffColor;
            else
                _sr.color = switchColor;
        }
    }
}
