using System.Collections.Generic;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Core
{
    public class LevelRuntime
    {
        public LevelData LevelData { get; private set; }
        public GraphModel Graph { get; private set; }

        public int CurrentNodeId { get; set; }
        public HashSet<int> VisitedBulbs { get; } = new HashSet<int>();
        public List<int> StrokeNodes { get; } = new List<int>();
        public int TotalBulbCount { get; private set; }

        public Dictionary<int, bool> GateOpenByEdgeId { get; } = new Dictionary<int, bool>();
        public Dictionary<int, List<int>> GateGroupToEdgeIds { get; } = new Dictionary<int, List<int>>();

        public void Load(LevelData levelData)
        {
            LevelData = levelData;
            Graph = new GraphModel(levelData);

            CurrentNodeId = -1;
            VisitedBulbs.Clear();
            StrokeNodes.Clear();

            TotalBulbCount = 0;
            if (levelData.nodes != null)
            {
                foreach (var n in levelData.nodes)
                    if (n.nodeType == NodeType.Bulb)
                        TotalBulbCount++;
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
