using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// 한 레벨의 런타임 상태: 현재 노드, 방문 전구, 스트로크 경로, 게이트 상태, 노드 캐시.
    /// LevelLoader.Load 시 데이터로 초기화되고, GameStateMachine/MoveValidator가 갱신.
    /// </summary>
    public class LevelRuntime
    {
        public LevelData LevelData { get; private set; }
        public GraphModel Graph { get; private set; }

        /// <summary>현재 스트로크에서 서 있는 노드 id. Idle일 때는 -1 등 무의미할 수 있음.</summary>
        public int CurrentNodeId { get; set; }
        /// <summary>이번 스트로크에서 이미 1회 방문한 전구 id 집합. 클리어 조건 = 이 개수 == TotalBulbCount.</summary>
        public HashSet<int> VisitedBulbs { get; } = new HashSet<int>();
        /// <summary>순서 보존용. 방문 체크는 StrokeContains 사용.</summary>
        public List<int> StrokeNodes { get; } = new List<int>();
        private readonly HashSet<int> _strokeNodeSet = new HashSet<int>();

        /// <summary>이 레벨의 전구 개수. 클리어 조건 비교용.</summary>
        public int TotalBulbCount { get; private set; }

        private Dictionary<int, NodeData> _nodeById = new Dictionary<int, NodeData>();

        /// <summary>게이트 엣지별 현재 열림 여부.</summary>
        public Dictionary<int, bool> GateOpenByEdgeId { get; } = new Dictionary<int, bool>();
        /// <summary>게이트 그룹 id → 해당 엣지 id 목록. 스위치 방문 시 이 그룹만 토글.</summary>
        public Dictionary<int, List<int>> GateGroupToEdgeIds { get; } = new Dictionary<int, List<int>>();

        /// <summary>nodeId에 해당하는 NodeData. O(1). 없으면 null.</summary>
        public NodeData GetNode(int nodeId)
        {
            return _nodeById != null && _nodeById.TryGetValue(nodeId, out var node) ? node : null;
        }

        /// <summary>nodeId의 위치(pos). 없으면 Vector2.zero.</summary>
        public Vector2 GetNodePosition(int nodeId)
        {
            var n = GetNode(nodeId);
            return n != null ? n.pos : Vector2.zero;
        }

        /// <summary>이번 스트로크에서 이미 지나간 노드인지. O(1).</summary>
        public bool StrokeContains(int nodeId) => _strokeNodeSet != null && _strokeNodeSet.Contains(nodeId);

        /// <summary>스트로크 경로 초기화. StartStroke 시 호출.</summary>
        public void ClearStrokeNodes()
        {
            StrokeNodes.Clear();
            _strokeNodeSet?.Clear();
        }

        /// <summary>스트로크 경로에 노드 추가. StrokeNodes와 내부 set 동기화.</summary>
        public void AddStrokeNode(int nodeId)
        {
            StrokeNodes.Add(nodeId);
            _strokeNodeSet?.Add(nodeId);
        }

        /// <summary>레벨 데이터로 상태 초기화. 노드 캐시·게이트·전구 개수 구성.</summary>
        public void Load(LevelData levelData)
        {
            LevelData = levelData;
            Graph = new GraphModel(levelData);

            CurrentNodeId = -1;
            VisitedBulbs.Clear();
            StrokeNodes.Clear();
            _strokeNodeSet.Clear();

            _nodeById.Clear();
            TotalBulbCount = 0;
            if (levelData.nodes != null)
            {
                foreach (var n in levelData.nodes)
                {
                    _nodeById[n.id] = n;
                    if (n.nodeType == NodeType.Bulb)
                        TotalBulbCount++;
                }
            }

            GateOpenByEdgeId.Clear();
            GateGroupToEdgeIds.Clear();
            if (levelData.edges != null)
            {
                foreach (var e in levelData.edges)
                {
                    if (e.gateGroupId >= 0)
                    {
                        GateOpenByEdgeId[e.id] = e.initialGateOpen;
                        if (!GateGroupToEdgeIds.ContainsKey(e.gateGroupId))
                            GateGroupToEdgeIds[e.gateGroupId] = new List<int>();
                        GateGroupToEdgeIds[e.gateGroupId].Add(e.id);
                    }
                }
            }
        }

        /// <summary>해당 그룹에 속한 게이트 엣지들의 열림 상태를 모두 반전. 스위치 방문 시 호출.</summary>
        public void ToggleGateGroup(int groupId)
        {
            if (!GateGroupToEdgeIds.TryGetValue(groupId, out var edgeIds))
                return;
            foreach (var edgeId in edgeIds)
            {
                if (GateOpenByEdgeId.ContainsKey(edgeId))
                    GateOpenByEdgeId[edgeId] = !GateOpenByEdgeId[edgeId];
            }
        }

        /// <summary>게이트 엣지가 현재 열려 있으면 true. 일반 엣지는 항상 통과 가능이므로 별도 조회 없음.</summary>
        public bool IsGateOpen(int edgeId)
        {
            return GateOpenByEdgeId.TryGetValue(edgeId, out var open) && open;
        }
    }
}
