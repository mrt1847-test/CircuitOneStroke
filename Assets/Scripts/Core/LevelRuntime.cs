using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Core
{
    public class LevelRuntime
    {
        public LevelData LevelData { get; private set; }
        public GraphModel Graph { get; private set; }

        public int CurrentNodeId { get; set; }
        public HashSet<int> VisitedBulbs { get; } = new HashSet<int>();
        /// <summary>순서 보존용. 방문 체크는 StrokeContains 사용.</summary>
        public List<int> StrokeNodes { get; } = new List<int>();
        private readonly HashSet<int> _strokeNodeSet = new HashSet<int>();

        public int TotalBulbCount { get; private set; }

        private Dictionary<int, NodeData> _nodeById = new Dictionary<int, NodeData>();

        public Dictionary<int, bool> GateOpenByEdgeId { get; } = new Dictionary<int, bool>();
        public Dictionary<int, List<int>> GateGroupToEdgeIds { get; } = new Dictionary<int, List<int>>();

        public NodeData GetNode(int nodeId)
        {
            return _nodeById != null && _nodeById.TryGetValue(nodeId, out var node) ? node : null;
        }

        public Vector2 GetNodePosition(int nodeId)
        {
            var n = GetNode(nodeId);
            return n != null ? n.pos : Vector2.zero;
        }

        public bool StrokeContains(int nodeId) => _strokeNodeSet != null && _strokeNodeSet.Contains(nodeId);

        public void ClearStrokeNodes()
        {
            StrokeNodes.Clear();
            _strokeNodeSet?.Clear();
        }

        public void AddStrokeNode(int nodeId)
        {
            StrokeNodes.Add(nodeId);
            _strokeNodeSet?.Add(nodeId);
        }

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

        public bool IsGateOpen(int edgeId)
        {
            return GateOpenByEdgeId.TryGetValue(edgeId, out var open) && open;
        }
    }
}
