using System;
using System.Collections.Generic;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Solver
{
    /// <summary>
    /// Result of solving a level: solvability and quality metrics for filter rules.
    /// </summary>
    public struct SolverResult
    {
        public bool solvable;
        public int solutionCount;
        public int nodesExpanded;
        public float earlyBranching;
        public float deadEndDepthAvg;
    }

    /// <summary>
    /// Evaluates LevelData with DFS: solvability, solution count, and metrics for level filtering.
    /// State: (currentNodeId, visitedMask, gateMask). Visit-once for all nodes (Bulb + Switch).
    /// </summary>
    public static class LevelSolver
    {
        public const int MaxSolutionsDefault = 100;
        public const int MaxStatesExpandedDefault = 200000;

        private sealed class SolverContext
        {
            public LevelData level;
            public int n;
            public Dictionary<int, List<(int neighbor, EdgeData edge)>> adj;
            public Dictionary<int, int> edgeIdToGateBit;
            public int[] nodeIdToIndex;
            public int[] indexToNodeId;
            public bool[] nodeIsSwitch;
            public int[] switchGroupId;
            public int allVisitedMask;
            public int initialGateMask;
            public int group1Mask;
            public int maxSolutions;
            public int maxStatesExpanded;
            public int solutionsFound;
            public int statesExpanded;
            public bool aborted;
            public List<float> branchingAtDepth;
            public List<int> deadEndDepths;
        }

        public static SolverResult Solve(LevelData levelData, int maxSolutions = MaxSolutionsDefault, int maxStatesExpanded = MaxStatesExpandedDefault)
        {
            var result = new SolverResult { solvable = false, solutionCount = 0, nodesExpanded = 0, earlyBranching = 0f, deadEndDepthAvg = 0f };
            if (levelData?.nodes == null || levelData.edges == null || levelData.nodes.Length == 0 || levelData.nodes.Length > 10)
                return result;

            var ctx = BuildContext(levelData, maxSolutions, maxStatesExpanded);
            if (ctx == null)
                return result;

            ctx.branchingAtDepth = new List<float>();
            ctx.deadEndDepths = new List<int>();

            // Try starting from every node (each node must be visited in some solution)
            for (int startIndex = 0; startIndex < ctx.n && !ctx.aborted; startIndex++)
            {
                int startNodeId = ctx.indexToNodeId[startIndex];
                int visited = 1 << startIndex;
                int gateMask = ctx.initialGateMask;
                if (ctx.nodeIsSwitch[startIndex])
                    gateMask = ToggleGroupMask(gateMask, ctx.group1Mask);
                Dfs(ctx, startNodeId, visited, gateMask, 0);
            }

            result.solvable = ctx.solutionsFound > 0 && !ctx.aborted;
            result.solutionCount = ctx.solutionsFound;
            result.nodesExpanded = ctx.statesExpanded;

            if (ctx.branchingAtDepth.Count > 0)
            {
                float sum = 0;
                foreach (var b in ctx.branchingAtDepth)
                    sum += b;
                result.earlyBranching = sum / ctx.branchingAtDepth.Count;
            }
            if (ctx.deadEndDepths.Count > 0)
            {
                float sum = 0;
                foreach (var d in ctx.deadEndDepths)
                    sum += d;
                result.deadEndDepthAvg = sum / ctx.deadEndDepths.Count;
            }

            return result;
        }

        private static int ToggleGroupMask(int gateMask, int groupMask)
        {
            return gateMask ^ groupMask;
        }

        private static SolverContext BuildContext(LevelData level, int maxSolutions, int maxStatesExpanded)
        {
            int n = level.nodes.Length;
            var nodeIdToIndex = new Dictionary<int, int>();
            var indexToNodeId = new int[n];
            for (int i = 0; i < n; i++)
            {
                int id = level.nodes[i].id;
                nodeIdToIndex[id] = i;
                indexToNodeId[i] = id;
            }

            var adj = new Dictionary<int, List<(int neighbor, EdgeData edge)>>();
            foreach (var e in level.edges)
            {
                if (!nodeIdToIndex.ContainsKey(e.a) || !nodeIdToIndex.ContainsKey(e.b))
                    continue;
                if (!adj.ContainsKey(e.a))
                    adj[e.a] = new List<(int, EdgeData)>();
                adj[e.a].Add((e.b, e));
                if (!adj.ContainsKey(e.b))
                    adj[e.b] = new List<(int, EdgeData)>();
                adj[e.b].Add((e.a, e));
            }

            var gateEdgeIdsInOrder = new List<int>();
            foreach (var e in level.edges)
            {
                if (e.gateGroupId >= 0)
                    gateEdgeIdsInOrder.Add(e.id);
            }
            gateEdgeIdsInOrder.Sort();
            var edgeIdToGateBit = new Dictionary<int, int>();
            for (int i = 0; i < gateEdgeIdsInOrder.Count; i++)
                edgeIdToGateBit[gateEdgeIdsInOrder[i]] = i;

            int numGateBits = gateEdgeIdsInOrder.Count;
            int initialGateMask = 0;
            foreach (var e in level.edges)
            {
                if (e.gateGroupId >= 0 && edgeIdToGateBit.TryGetValue(e.id, out int bit))
                {
                    if (e.initialGateOpen)
                        initialGateMask |= 1 << bit;
                }
            }
            int group1Mask = (1 << numGateBits) - 1;

            var nodeIsSwitch = new bool[n];
            var switchGroupId = new int[n];
            for (int i = 0; i < n; i++)
            {
                var node = level.nodes[i];
                nodeIsSwitch[i] = node.nodeType == NodeType.Switch;
                switchGroupId[i] = node.switchGroupId;
            }

            int allVisitedMask = (1 << n) - 1;

            return new SolverContext
            {
                level = level,
                n = n,
                adj = adj,
                edgeIdToGateBit = edgeIdToGateBit,
                nodeIdToIndex = nodeIdToIndex,
                indexToNodeId = indexToNodeId,
                nodeIsSwitch = nodeIsSwitch,
                switchGroupId = switchGroupId,
                allVisitedMask = allVisitedMask,
                initialGateMask = initialGateMask,
                group1Mask = group1Mask,
                maxSolutions = maxSolutions,
                maxStatesExpanded = maxStatesExpanded,
                solutionsFound = 0,
                statesExpanded = 0,
                aborted = false
            };
        }

        private static void Dfs(SolverContext ctx, int currentNodeId, int visitedMask, int gateMask, int depth)
        {
            ctx.statesExpanded++;
            if (ctx.statesExpanded > ctx.maxStatesExpanded || ctx.solutionsFound >= ctx.maxSolutions)
            {
                ctx.aborted = true;
                return;
            }

            if (visitedMask == ctx.allVisitedMask)
            {
                ctx.solutionsFound++;
                return;
            }

            if (!ctx.adj.TryGetValue(currentNodeId, out var neighbors))
            {
                if (visitedMask != ctx.allVisitedMask)
                    ctx.deadEndDepths.Add(depth);
                return;
            }

            int currentIndex = ctx.nodeIdToIndex[currentNodeId];
            int validCount = 0;
            foreach (var (neighborId, edge) in neighbors)
            {
                int nextIndex = ctx.nodeIdToIndex[neighborId];
                if ((visitedMask & (1 << nextIndex)) != 0)
                    continue;
                if (edge.gateGroupId >= 0)
                {
                    if (ctx.edgeIdToGateBit.TryGetValue(edge.id, out int bit) && (gateMask & (1 << bit)) == 0)
                        continue;
                }
                bool fromAtoB = (currentNodeId == edge.a && neighborId == edge.b);
                if (edge.diode == DiodeMode.AtoB && !fromAtoB)
                    continue;
                if (edge.diode == DiodeMode.BtoA && fromAtoB)
                    continue;
                validCount++;
            }

            if (depth < 5)
                ctx.branchingAtDepth.Add(validCount);
            if (validCount == 0)
            {
                ctx.deadEndDepths.Add(depth);
                return;
            }

            foreach (var (neighborId, edge) in neighbors)
            {
                if (ctx.aborted)
                    return;
                int nextIndex = ctx.nodeIdToIndex[neighborId];
                if ((visitedMask & (1 << nextIndex)) != 0)
                    continue;
                if (edge.gateGroupId >= 0)
                {
                    if (ctx.edgeIdToGateBit.TryGetValue(edge.id, out int bit) && (gateMask & (1 << bit)) == 0)
                        continue;
                }
                bool fromAtoB = (currentNodeId == edge.a && neighborId == edge.b);
                if (edge.diode == DiodeMode.AtoB && !fromAtoB)
                    continue;
                if (edge.diode == DiodeMode.BtoA && fromAtoB)
                    continue;

                int newVisited = visitedMask | (1 << nextIndex);
                int newGate = gateMask;
                if (ctx.nodeIsSwitch[nextIndex] && ctx.switchGroupId[nextIndex] >= 0)
                    newGate = ToggleGroupMask(gateMask, ctx.group1Mask);
                Dfs(ctx, neighborId, newVisited, newGate, depth + 1);
            }
        }
    }
}
