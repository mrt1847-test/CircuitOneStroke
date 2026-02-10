using System.Collections.Generic;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// Adjacency list: for each node id, list of (neighborNodeId, edgeData).
    /// </summary>
    public class GraphModel
    {
        private readonly Dictionary<int, List<(int neighborId, EdgeData edge)>> _adjacency = new Dictionary<int, List<(int, EdgeData)>>();

        public GraphModel(LevelData levelData)
        {
            if (levelData?.nodes == null || levelData.edges == null)
                return;

            foreach (var n in levelData.nodes)
                _adjacency[n.id] = new List<(int, EdgeData)>();

            foreach (var e in levelData.edges)
            {
                if (!_adjacency.TryGetValue(e.a, out var listA))
                    continue;
                listA.Add((e.b, e));

                if (!_adjacency.TryGetValue(e.b, out var listB))
                    continue;
                listB.Add((e.a, e));
            }
        }

        public IReadOnlyList<(int neighborId, EdgeData edge)> GetNeighbors(int nodeId)
        {
            return _adjacency.TryGetValue(nodeId, out var list) ? list : null;
        }

        public bool TryGetEdge(int fromNodeId, int toNodeId, out EdgeData edge)
        {
            edge = null;
            var neighbors = GetNeighbors(fromNodeId);
            if (neighbors == null) return false;
            foreach (var (neighborId, e) in neighbors)
            {
                if (neighborId == toNodeId)
                {
                    edge = e;
                    return true;
                }
            }
            return false;
        }
    }
}
