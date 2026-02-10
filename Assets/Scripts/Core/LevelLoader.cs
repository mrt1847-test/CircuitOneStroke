using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.View;

namespace CircuitOneStroke.Core
{
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] private LevelData levelData;
        [SerializeField] private Transform nodesRoot;
        [SerializeField] private Transform edgesRoot;
        [SerializeField] private GameObject nodeViewPrefab;
        [SerializeField] private GameObject edgeViewPrefab;
        [SerializeField] private StrokeRenderer strokeRenderer;

        private LevelRuntime _runtime;
        private GameStateMachine _stateMachine;
        private NodeView[] _nodeViews;
        private EdgeView[] _edgeViews;

        public LevelRuntime Runtime => _runtime;
        public GameStateMachine StateMachine => _stateMachine;
        public LevelData LevelData => levelData;

        public void RefreshNodeViews()
        {
            if (_nodeViews == null || _runtime == null) return;
            var visited = _runtime.VisitedBulbs;
            foreach (var nv in _nodeViews)
                nv.SetVisited(visited.Contains(nv.NodeId));
        }

        public EdgeView GetEdgeView(int edgeId)
        {
            if (_edgeViews == null) return null;
            foreach (var ev in _edgeViews)
                if (ev != null && ev.EdgeId == edgeId) return ev;
            return null;
        }

        public void LoadLevel(LevelData data)
        {
            levelData = data;
            LoadCurrent();
        }

        public void LoadLevel(int levelId)
        {
            var data = Resources.Load<LevelData>($"Levels/Level_{levelId}");
            if (data != null)
                LoadLevel(data);
        }

        public void LoadCurrent()
        {
            if (levelData == null) return;

            Clear();

            _runtime = new LevelRuntime();
            _runtime.Load(levelData);
            _stateMachine = new GameStateMachine(_runtime);

            if (strokeRenderer != null)
                strokeRenderer.Bind(_runtime);

            SpawnNodes();
            SpawnEdges();
        }

        private void Clear()
        {
            if (nodesRoot != null)
            {
                for (int i = nodesRoot.childCount - 1; i >= 0; i--)
                    Destroy(nodesRoot.GetChild(i).gameObject);
            }
            if (edgesRoot != null)
            {
                for (int i = edgesRoot.childCount - 1; i >= 0; i--)
                    Destroy(edgesRoot.GetChild(i).gameObject);
            }
            _nodeViews = null;
            _edgeViews = null;
        }

        private void SpawnNodes()
        {
            if (levelData.nodes == null || nodeViewPrefab == null || nodesRoot == null) return;
            _nodeViews = new NodeView[levelData.nodes.Length];
            for (int i = 0; i < levelData.nodes.Length; i++)
            {
                var nd = levelData.nodes[i];
                var go = Instantiate(nodeViewPrefab, new Vector3(nd.pos.x, nd.pos.y, 0f), Quaternion.identity, nodesRoot);
                var nv = go.GetComponent<NodeView>();
                if (nv != null)
                {
                    nv.Setup(nd.id, nd.pos, nd.nodeType);
                    _nodeViews[i] = nv;
                }
            }
        }

        private void SpawnEdges()
        {
            if (levelData.edges == null || edgeViewPrefab == null || edgesRoot == null) return;
            _edgeViews = new EdgeView[levelData.edges.Length];
            for (int i = 0; i < levelData.edges.Length; i++)
            {
                var ed = levelData.edges[i];
                var posA = GetNodePos(ed.a);
                var posB = GetNodePos(ed.b);
                var go = Instantiate(edgeViewPrefab, Vector3.zero, Quaternion.identity, edgesRoot);
                var ev = go.GetComponent<EdgeView>();
                if (ev != null)
                {
                    ev.Setup(ed.id, posA, posB, ed.diode, ed.gateGroupId, ed.initialGateOpen, _runtime);
                    _edgeViews[i] = ev;
                }
            }
        }

        private Vector2 GetNodePos(int nodeId)
        {
            if (levelData?.nodes == null) return Vector2.zero;
            foreach (var n in levelData.nodes)
                if (n.id == nodeId) return n.pos;
            return Vector2.zero;
        }

        private void Start()
        {
            if (levelData == null)
                levelData = Resources.Load<LevelData>("Levels/Level_1");
            if (levelData != null)
                LoadCurrent();
        }
    }
}
