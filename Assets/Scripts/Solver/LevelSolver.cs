using System;
using System.Collections.Generic;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Solver
{
    /// <summary>N>12 예산 평가 시 완료 여부.</summary>
    public enum SolverStatus
    {
        Completed,
        BudgetExceeded
    }

    /// <summary>
    /// Solver 실행 결과: 풀기 가능 여부, 해 개수, 탐색량, 난이도 메트릭(초기 분기·막다른 깊이).
    /// N>12일 때는 solutionsFoundWithinBudget·status 사용.
    /// </summary>
    public struct SolverResult
    {
        public bool solvable;
        public int solutionCount;
        public int nodesExpanded;
        public float earlyBranching;
        public float deadEndDepthAvg;
        /// <summary>예산 내에서 풀기 가능(최소 1해 발견). N>12에서 사용.</summary>
        public bool solvableWithinBudget;
        /// <summary>예산 내에서 찾은 해 개수. N>12에서는 정확한 총 개수 아님.</summary>
        public int solutionsFoundWithinBudget;
        public SolverStatus status;
        /// <summary>N>12 샘플링 기반 근사. 그 외는 earlyBranching와 동일.</summary>
        public float earlyBranchingApprox;
        /// <summary>N>12 샘플링 기반 근사. 그 외는 deadEndDepthAvg와 동일.</summary>
        public float deadEndDepthAvgApprox;
    }

    /// <summary>
    /// Evaluates LevelData with DFS: solvability, solution count, and metrics for level filtering.
    /// State: (currentNodeId, visitedMask, gateMask). Visit-once for all nodes (Bulb + Switch).
    /// </summary>
    public static class LevelSolver
    {
        /// <summary>이 노드 수를 초과하는 레벨은 검증하지 않음. 25까지 지원 (N>12는 예산 평가).</summary>
        public const int MaxNodesSupported = 25;
        /// <summary>N>12일 때 예산 모드로 전환하는 노드 수 한계.</summary>
        public const int BudgetedEvaluationThreshold = 12;

        public const int MaxSolutionsDefault = 100;
        public const int MaxStatesExpandedDefault = 200000;
        /// <summary>N>12 예산 평가 시 해 개수 상한 (정확한 총 개수는 세지 않음).</summary>
        public const int MaxSolutionsBudgetedDefault = 50;
        /// <summary>N>12 예산 평가 시 시간 상한(ms). 0이면 무제한.</summary>
        public const int MaxMillisBudgetedDefault = 100;

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
            /// <summary>gateGroupId → bitmask of gate bits in that group (runtime ToggleGateGroup와 동일 규칙).</summary>
            public Dictionary<int, int> groupIdToMask;
            public int maxSolutions;
            public int maxStatesExpanded;
            public int solutionsFound;
            public int statesExpanded;
            public bool aborted;
            public List<float> branchingAtDepth;
            public List<int> deadEndDepths;
            public long startTicks;
            public int maxMillis;
        }

        /// <summary>풀기 가능 여부·메트릭 반환. N>12이면 maxMillis·maxSolutions로 예산 평가.</summary>
        public static SolverResult Solve(LevelData levelData, int maxSolutions = MaxSolutionsDefault, int maxStatesExpanded = MaxStatesExpandedDefault, int maxMillis = 0)
        {
            var result = new SolverResult
            {
                solvable = false, solutionCount = 0, nodesExpanded = 0, earlyBranching = 0f, deadEndDepthAvg = 0f,
                solvableWithinBudget = false, solutionsFoundWithinBudget = 0, status = SolverStatus.Completed,
                earlyBranchingApprox = 0f, deadEndDepthAvgApprox = 0f
            };
            if (levelData?.nodes == null || levelData.edges == null || levelData.nodes.Length == 0 || levelData.nodes.Length > MaxNodesSupported)
                return result;

            int n = levelData.nodes.Length;
            bool budgeted = n > BudgetedEvaluationThreshold;
            if (budgeted && maxMillis <= 0)
                maxMillis = MaxMillisBudgetedDefault;

            var ctx = BuildContext(levelData, maxSolutions, maxStatesExpanded);
            if (ctx == null)
                return result;
            ctx.maxMillis = maxMillis;
            ctx.startTicks = Environment.TickCount64;

            ctx.branchingAtDepth = new List<float>();
            ctx.deadEndDepths = new List<int>();

            for (int startIndex = 0; startIndex < ctx.n && !ctx.aborted; startIndex++)
            {
                if (ctx.maxMillis > 0 && (Environment.TickCount64 - ctx.startTicks) >= ctx.maxMillis)
                {
                    ctx.aborted = true;
                    break;
                }
                int startNodeId = ctx.indexToNodeId[startIndex];
                int visited = 1 << startIndex;
                int gateMask = ctx.initialGateMask;
                if (ctx.nodeIsSwitch[startIndex])
                    gateMask = ToggleGroupMask(gateMask, GetGroupMask(ctx, ctx.switchGroupId[startIndex]));
                Dfs(ctx, startNodeId, visited, gateMask, 0);
            }

            result.solvable = ctx.solutionsFound > 0 && !ctx.aborted;
            result.solutionCount = ctx.solutionsFound;
            result.solutionsFoundWithinBudget = ctx.solutionsFound;
            result.solvableWithinBudget = ctx.solutionsFound > 0;
            result.nodesExpanded = ctx.statesExpanded;
            result.status = ctx.aborted ? SolverStatus.BudgetExceeded : SolverStatus.Completed;
            if (budgeted && ctx.statesExpanded >= expansionCap && ctx.solutionsFound == 0)
                result.solvable = false;

            if (ctx.branchingAtDepth.Count > 0)
            {
                float sum = 0;
                foreach (var b in ctx.branchingAtDepth)
                    sum += b;
                result.earlyBranching = result.earlyBranchingApprox = sum / ctx.branchingAtDepth.Count;
            }
            if (ctx.deadEndDepths.Count > 0)
            {
                float sum = 0;
                foreach (var d in ctx.deadEndDepths)
                    sum += d;
                result.deadEndDepthAvg = result.deadEndDepthAvgApprox = sum / ctx.deadEndDepths.Count;
            }

            return result;
        }

        private static int ToggleGroupMask(int gateMask, int groupMask)
        {
            return gateMask ^ groupMask;
        }

        /// <summary>switchGroupId에 해당하는 게이트 그룹의 비트 마스크 반환. 런타임 ToggleGateGroup(groupId)와 동일 그룹만 토글.</summary>
        private static int GetGroupMask(SolverContext ctx, int groupId)
        {
            if (groupId < 0 || ctx.groupIdToMask == null) return 0;
            return ctx.groupIdToMask.TryGetValue(groupId, out int mask) ? mask : 0;
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

            int initialGateMask = 0;
            var groupIdToMask = new Dictionary<int, int>();
            foreach (var e in level.edges)
            {
                if (e.gateGroupId >= 0 && edgeIdToGateBit.TryGetValue(e.id, out int bit))
                {
                    if (e.initialGateOpen)
                        initialGateMask |= 1 << bit;
                    if (!groupIdToMask.ContainsKey(e.gateGroupId))
                        groupIdToMask[e.gateGroupId] = 0;
                    groupIdToMask[e.gateGroupId] |= 1 << bit;
                }
            }

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
                groupIdToMask = groupIdToMask,
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
            if (ctx.maxMillis > 0 && (Environment.TickCount64 - ctx.startTicks) >= ctx.maxMillis)
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
                    newGate = ToggleGroupMask(gateMask, GetGroupMask(ctx, ctx.switchGroupId[nextIndex]));
                Dfs(ctx, neighborId, newVisited, newGate, depth + 1);
            }
        }
    }
}
