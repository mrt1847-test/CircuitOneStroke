using UnityEngine;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.View
{
    [RequireComponent(typeof(LineRenderer))]
    public class StrokeRenderer : MonoBehaviour
    {
        [SerializeField] private Color strokeColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private float lineWidth = 0.15f;

        private LineRenderer _lr;
        private LevelRuntime _runtime;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.startColor = _lr.endColor = strokeColor;
            _lr.startWidth = _lr.endWidth = lineWidth;
        }

        public void Bind(LevelRuntime runtime)
        {
            _runtime = runtime;
        }

        private void Update()
        {
            if (_runtime == null || _runtime.StrokeNodes == null || _runtime.StrokeNodes.Count == 0)
            {
                _lr.positionCount = 0;
                return;
            }

            var nodes = _runtime.LevelData?.nodes;
            if (nodes == null) return;

            _lr.positionCount = _runtime.StrokeNodes.Count;
            for (int i = 0; i < _runtime.StrokeNodes.Count; i++)
            {
                int id = _runtime.StrokeNodes[i];
                Vector2 pos = Vector2.zero;
                foreach (var n in nodes)
                    if (n.id == id) { pos = n.pos; break; }
                _lr.SetPosition(i, new Vector3(pos.x, pos.y, -0.1f));
            }
        }
    }
}
