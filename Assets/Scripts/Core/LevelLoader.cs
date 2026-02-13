using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.View;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// LevelData瑜?濡쒕뱶???고??꽷룹긽??湲곌퀎瑜?留뚮뱾怨? ?몃뱶/?ｌ? 酉곕? ?앹꽦.
    /// ?ъ뿉???몃뱶 猷⑦듃쨌?ｌ? 猷⑦듃쨌?꾨━?뮤톁trokeRenderer 李몄“ 蹂댁쑀.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] private LevelData levelData;
        [SerializeField] private Transform nodesRoot;
        [SerializeField] private Transform edgesRoot;
        [SerializeField] private GameObject nodeViewPrefab;
        [SerializeField] private GameObject edgeViewPrefab;
        [SerializeField] private StrokeRenderer strokeRenderer;
        [Header("Camera Framing")]
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private bool autoFramePuzzle = true;
        [SerializeField] private float framePaddingWorld = 0.8f;
        [SerializeField] private float frameSizeMultiplier = 1.12f;
        [Header("Move Hint Visuals")]
        [SerializeField] private bool emphasizeLegalEdgesWhileDrawing = true;
        [SerializeField] private bool dimNonCandidateEdges = true;
        [Header("Edge Routing")]
        [SerializeField] private bool separateParallelOverlaps = true;
        [SerializeField] [Range(0.06f, 0.56f)] private float routingLaneSpacing = 0.40f;
        [SerializeField] [Range(0.08f, 0.64f)] private float routingNearParallelDistance = 0.48f;
        [SerializeField] [Range(0.20f, 1.20f)] private float routingMinOverlap = 0.48f;
        [SerializeField] [Range(0.20f, 2.00f)] private float routingMinStubNodeSizeMultiplier = 2.00f;

        private LevelRuntime _runtime;
        private GameStateMachine _stateMachine;
        private NodeView[] _nodeViews;
        private EdgeView[] _edgeViews;

        public LevelRuntime Runtime => _runtime;
        public GameStateMachine StateMachine => _stateMachine;
        public LevelData LevelData => levelData;
        public event Action<GameStateMachine> OnStateMachineChanged;

        /// <summary>諛⑸Ц???꾧뎄??留욎떠 ?몃뱶 酉곗쓽 ?쒓컖 ?곹깭 媛깆떊.</summary>
        public void RefreshNodeViews()
        {
            if (_nodeViews == null || _runtime == null) return;
            var visited = _runtime.VisitedBulbs;
            foreach (var nv in _nodeViews)
                nv.SetVisited(visited.Contains(nv.NodeId));
        }

        /// <summary>edgeId???대떦?섎뒗 EdgeView 諛섑솚. 由ъ젥???뚮옒???깆뿉 ?ъ슜.</summary>
        public EdgeView GetEdgeView(int edgeId)
        {
            if (_edgeViews == null) return null;
            foreach (var ev in _edgeViews)
                if (ev != null && ev.EdgeId == edgeId) return ev;
            return null;
        }

        /// <summary>nodeId???대떦?섎뒗 NodeView 諛섑솚. 瑗щ━ ?섏씠?쇱씠???깆뿉 ?ъ슜.</summary>
        public NodeView GetNodeView(int nodeId)
        {
            if (_nodeViews == null) return null;
            foreach (var nv in _nodeViews)
                if (nv != null && nv.NodeId == nodeId) return nv;
            return null;
        }

        /// <summary>
        /// Highlight currently legal move candidates and dim the rest.
        /// </summary>
        public void SetMoveHints(int currentNodeId, IReadOnlyList<int> candidateNodeIds, IReadOnlyList<int> candidateEdgeIds)
        {
            HashSet<int> nodeSet = null;
            HashSet<int> edgeSet = null;
            if (candidateNodeIds != null)
            {
                nodeSet = new HashSet<int>(candidateNodeIds);
                if (currentNodeId >= 0) nodeSet.Add(currentNodeId);
            }
            if (candidateEdgeIds != null)
                edgeSet = new HashSet<int>(candidateEdgeIds);

            bool enableHints = nodeSet != null && nodeSet.Count > 0;
            if (_nodeViews != null)
            {
                foreach (var nv in _nodeViews)
                {
                    if (nv == null) continue;
                    bool isCandidate = enableHints && nodeSet.Contains(nv.NodeId);
                    nv.SetMoveHint(isCandidate, enableHints);
                }
            }
            if (_edgeViews != null)
            {
                foreach (var ev in _edgeViews)
                {
                    if (ev == null) continue;
                    ev.SetHintStyle(dimNonCandidateEdges, emphasizeLegalEdgesWhileDrawing);
                    bool isCandidate = enableHints && edgeSet != null && edgeSet.Contains(ev.EdgeId);
                    bool edgeHintActive = enableHints && (emphasizeLegalEdgesWhileDrawing || dimNonCandidateEdges);
                    ev.SetMoveHint(isCandidate, edgeHintActive);
                }
            }
        }

        /// <summary>吏??LevelData濡?援먯껜 ???꾩옱 ?덈꺼 ?щ줈??</summary>
        public void LoadLevel(LevelData data)
        {
            levelData = data;
            LoadCurrent();
        }

        /// <summary>吏??LevelData濡?援먯껜 ??LoadCurrentCoroutine ?ㅽ뻾. TransitionManager??</summary>
        public IEnumerator LoadLevelCoroutine(LevelData data)
        {
            levelData = data;
            yield return LoadCurrentCoroutine();
        }

        /// <summary>Resources/Levels/Level_{levelId} 濡쒕뱶 ??LoadLevelCoroutine ?ㅽ뻾.</summary>
        public IEnumerator LoadLevelCoroutine(int levelId)
        {
            var data = Resources.Load<LevelData>($"Levels/Level_{levelId}");
            if (data != null)
            {
                levelData = data;
                yield return LoadCurrentCoroutine();
            }
        }

        /// <summary>Resources/Levels/Level_{levelId} 濡쒕뱶 ???곸슜.</summary>
        public void LoadLevel(int levelId)
        {
            var data = Resources.Load<LevelData>($"Levels/Level_{levelId}");
            if (data != null)
                LoadLevel(data);
        }

        /// <summary>?꾩옱 levelData濡??고??꽷룹긽?쒓린怨꽷룸끂???ｌ? 酉??ш뎄??</summary>
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
            AutoFramePuzzleCamera();
        }

        /// <summary>?꾪솚?? Yield between phases to prevent frame spikes.</summary>
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

            AutoFramePuzzleCamera();
            yield return null;

            RefreshNodeViews();
            yield return null;
        }

        /// <summary>湲곗〈 ?몃뱶/?ｌ? 酉??쒓굅 諛?諛곗뿴 珥덇린??</summary>
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

        /// <summary>levelData.nodes 湲곗??쇰줈 ?몃뱶 酉??몄뒪?댁뒪 ?앹꽦.</summary>
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

        /// <summary>levelData.edges 湲곗??쇰줈 ?ｌ? 酉??몄뒪?댁뒪 ?앹꽦. 寃뚯씠???ㅼ씠?ㅻ뱶 ?뺣낫 ?꾨떖.</summary>
        private void SpawnEdges()
        {
            if (levelData.edges == null || edgeViewPrefab == null || edgesRoot == null) return;
            _edgeViews = new EdgeView[levelData.edges.Length];
            float effectiveLaneSpacing = Mathf.Max(0.36f, routingLaneSpacing);
            float effectiveNearParallelDistance = Mathf.Max(effectiveLaneSpacing * 0.9f, routingNearParallelDistance);
            float nodeVisualDiameter = EstimateNodeVisualDiameterWorld();
            float effectiveMinStubFromCenter = Mathf.Max(0.20f, nodeVisualDiameter * routingMinStubNodeSizeMultiplier);
            var routedByEdgeId = separateParallelOverlaps
                ? OctilinearEdgeRouter.BuildRoutes(levelData, effectiveLaneSpacing, effectiveNearParallelDistance, routingMinOverlap, effectiveMinStubFromCenter)
                : null;
            for (int i = 0; i < levelData.edges.Length; i++)
            {
                var ed = levelData.edges[i];
                var posA = GetNodePos(ed.a);
                var posB = GetNodePos(ed.b);
                var go = Instantiate(edgeViewPrefab, Vector3.zero, Quaternion.identity, edgesRoot);
                var ev = go.GetComponent<EdgeView>();
                if (ev != null)
                {
                    Vector2[] route = null;
                    routedByEdgeId?.TryGetValue(ed.id, out route);
                    ev.Setup(ed.id, posA, posB, ed.diode, ed.gateGroupId, ed.initialGateOpen, _runtime, route);
                    _edgeViews[i] = ev;
                }
            }
        }

        /// <summary>?ｌ? ?ㅽ룿 ???몃뱶 ?꾩튂 議고쉶?? (?고???罹먯떆??濡쒕뱶 ???ъ슜 媛??</summary>
        private Vector2 GetNodePos(int nodeId)
        {
            if (levelData?.nodes == null) return Vector2.zero;
            foreach (var n in levelData.nodes)
                if (n.id == nodeId) return n.pos;
            return Vector2.zero;
        }

        private float EstimateNodeVisualDiameterWorld()
        {
            float diameter = 1f;
            if (nodeViewPrefab != null)
            {
                var sr = nodeViewPrefab.GetComponent<SpriteRenderer>() ?? nodeViewPrefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    float spriteSize = sr.sprite != null
                        ? Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y)
                        : 1f;
                    float spriteScale = Mathf.Max(Mathf.Abs(sr.transform.localScale.x), Mathf.Abs(sr.transform.localScale.y));
                    diameter = Mathf.Max(0.1f, spriteSize * spriteScale);
                }

                float prefabScale = Mathf.Max(Mathf.Abs(nodeViewPrefab.transform.localScale.x), Mathf.Abs(nodeViewPrefab.transform.localScale.y));
                diameter *= Mathf.Max(0.01f, prefabScale);
            }

            float nodeSizeScale = 1f;
            if (GameSettings.Instance?.Data != null)
            {
                nodeSizeScale = GameSettings.Instance.NodeSizeValue switch
                {
                    NodeSize.Small => 0.85f,
                    NodeSize.Large => 1.2f,
                    _ => 1f
                };
            }

            return Mathf.Max(0.1f, diameter * nodeSizeScale);
        }

        /// <summary>levelData??GameFlowController/RequestStartLevel?먯꽌 ?ㅼ젙. ?먮룞 濡쒕뱶 ?놁쓬.</summary>
        private void Start()
        {
        }

        private void AutoFramePuzzleCamera()
        {
            if (!autoFramePuzzle || levelData?.nodes == null || levelData.nodes.Length == 0)
                return;

            var cam = gameplayCamera != null ? gameplayCamera : Camera.main;
            if (cam == null || !cam.orthographic)
                return;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < levelData.nodes.Length; i++)
            {
                Vector2 p = levelData.nodes[i].pos;
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }

            float pad = Mathf.Max(0f, framePaddingWorld);
            minX -= pad;
            maxX += pad;
            minY -= pad;
            maxY += pad;

            float width = Mathf.Max(0.1f, maxX - minX);
            float height = Mathf.Max(0.1f, maxY - minY);
            float aspect = Mathf.Max(0.1f, cam.aspect);

            float sizeForHeight = height * 0.5f;
            float sizeForWidth = (width * 0.5f) / aspect;
            float targetSize = Mathf.Max(sizeForHeight, sizeForWidth) * Mathf.Max(1f, frameSizeMultiplier);

            Vector3 camPos = cam.transform.position;
            cam.transform.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, camPos.z);
            cam.orthographicSize = Mathf.Max(2.5f, targetSize);
        }
    }
}
