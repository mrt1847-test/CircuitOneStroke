using System;
using System.Collections;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.View;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// LevelData를 로드해 런타임·상태 기계를 만들고, 노드/엣지 뷰를 생성.
    /// 씬에서 노드 루트·엣지 루트·프리팹·StrokeRenderer 참조 보유.
    /// </summary>
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
        public event Action<GameStateMachine> OnStateMachineChanged;

        /// <summary>방문한 전구에 맞춰 노드 뷰의 시각 상태 갱신.</summary>
        public void RefreshNodeViews()
        {
            if (_nodeViews == null || _runtime == null) return;
            var visited = _runtime.VisitedBulbs;
            foreach (var nv in _nodeViews)
                nv.SetVisited(visited.Contains(nv.NodeId));
        }

        /// <summary>edgeId에 해당하는 EdgeView 반환. 리젝트 플래시 등에 사용.</summary>
        public EdgeView GetEdgeView(int edgeId)
        {
            if (_edgeViews == null) return null;
            foreach (var ev in _edgeViews)
                if (ev != null && ev.EdgeId == edgeId) return ev;
            return null;
        }

        /// <summary>지정 LevelData로 교체 후 현재 레벨 재로드.</summary>
        public void LoadLevel(LevelData data)
        {
            levelData = data;
            LoadCurrent();
        }

        /// <summary>지정 LevelData로 교체 후 LoadCurrentCoroutine 실행. TransitionManager용.</summary>
        public IEnumerator LoadLevelCoroutine(LevelData data)
        {
            levelData = data;
            yield return LoadCurrentCoroutine();
        }

        /// <summary>Resources/Levels/Level_{levelId} 로드 후 LoadLevelCoroutine 실행.</summary>
        public IEnumerator LoadLevelCoroutine(int levelId)
        {
            var data = Resources.Load<LevelData>($"Levels/Level_{levelId}");
            if (data != null)
            {
                levelData = data;
                yield return LoadCurrentCoroutine();
            }
        }

        /// <summary>Resources/Levels/Level_{levelId} 로드 후 적용.</summary>
        public void LoadLevel(int levelId)
        {
            var data = Resources.Load<LevelData>($"Levels/Level_{levelId}");
            if (data != null)
                LoadLevel(data);
        }

        /// <summary>현재 levelData로 런타임·상태기계·노드/엣지 뷰 재구성.</summary>
        public void LoadCurrent()
        {
            if (levelData == null) return;
            Clear();
            _runtime = new LevelRuntime();
            _runtime.Load(levelData);
            _stateMachine = new GameStateMachine(_runtime);
            OnStateMachineChanged?.Invoke(_stateMachine);
            if (strokeRenderer != null) strokeRenderer.Bind(_runtime);
            SpawnNodes();
            SpawnEdges();
        }

        /// <summary>전환용. Yield between phases to prevent frame spikes.</summary>
        public IEnumerator LoadCurrentCoroutine()
        {
            if (levelData == null) yield break;

            Clear();
            yield return null;

            _runtime = new LevelRuntime();
            _runtime.Load(levelData);
            _stateMachine = new GameStateMachine(_runtime);
            OnStateMachineChanged?.Invoke(_stateMachine);
            if (strokeRenderer != null) strokeRenderer.Bind(_runtime);
            SpawnNodes();
            yield return null;

            SpawnEdges();
            yield return null;

            RefreshNodeViews();
            yield return null;
        }

        /// <summary>기존 노드/엣지 뷰 제거 및 배열 초기화.</summary>
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

        /// <summary>levelData.nodes 기준으로 노드 뷰 인스턴스 생성.</summary>
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

        /// <summary>levelData.edges 기준으로 엣지 뷰 인스턴스 생성. 게이트/다이오드 정보 전달.</summary>
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

        /// <summary>엣지 스폰 시 노드 위치 조회용. (런타임 캐시는 로드 후 사용 가능)</summary>
        private Vector2 GetNodePos(int nodeId)
        {
            if (levelData?.nodes == null) return Vector2.zero;
            foreach (var n in levelData.nodes)
                if (n.id == nodeId) return n.pos;
            return Vector2.zero;
        }

        /// <summary>levelData는 GameFlowController/RequestStartLevel에서 설정. 자동 로드 없음.</summary>
        private void Start()
        {
        }
    }
}
