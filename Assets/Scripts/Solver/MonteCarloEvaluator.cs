using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Solver
{
    /// <summary>
    /// Estimates level difficulty via Monte Carlo: uniform random start at a Bulb,
    /// uniform random valid moves. Success = all bulbs visited exactly once. No gates in simulation (evaluateWithGates=false).
    /// </summary>
    public static class MonteCarloEvaluator
    {
        public struct EvaluationStats
        {
            public float successRate;
            public float avgStartSuccessRate;
            public float bestStartSuccessRate;
            public float p80StartSuccessRate;
            public float forcedRatio;
            public float corridorVisualRatio;
            public float topEdgesShare;
            public float diodeUsageRate;
            public float avgDiodeUseCountOnSuccess;
            public int forcedSteps;
            public int decisionSteps;
            public int successes;
            public int visualDominantSteps;
            public int visualDecisionSteps;
        }

        /// <summary>
        /// Estimate success rate (0..1) over K trials with given seed. Start node is chosen uniformly among Bulb nodes.
        /// </summary>
        public static float EstimateSuccessRate(LevelData level, int trialsK, int seed)
        {
            RunTrials(level, trialsK, seed, out int successes, null, null, null, out _);
            return trialsK > 0 ? (successes / (float)trialsK) : 0f;
        }

        /// <summary>
        /// Run trials and optionally accumulate directed edge (from, to) traversal counts for diode tuning.
        /// </summary>
        public static float RunTrialsWithEdgeCounts(LevelData level, int trialsK, int seed,
            out Dictionary<(int from, int to), int> directedEdgeCounts)
        {
            directedEdgeCounts = new Dictionary<(int from, int to), int>();
            RunTrials(level, trialsK, seed, out int successes, directedEdgeCounts, null, null, out _);
            return trialsK > 0 ? (successes / (float)trialsK) : 0f;
        }

        /// <summary>
        /// Run trials and accumulate directed edge counts only for successful trials.
        /// </summary>
        public static float RunTrialsWithSuccessEdgeCounts(LevelData level, int trialsK, int seed,
            out Dictionary<(int from, int to), int> directedEdgeCountsSuccess)
        {
            directedEdgeCountsSuccess = new Dictionary<(int from, int to), int>();
            RunTrials(level, trialsK, seed, out int successes, null, directedEdgeCountsSuccess, null, out _);
            return trialsK > 0 ? (successes / (float)trialsK) : 0f;
        }

        /// <summary>
        /// Estimate success rate and diagnostics: forcedRatio = (steps where legalMoves==1) / decisionSteps.
        /// </summary>
        public static float EstimateSuccessRateWithDiagnostics(LevelData level, int trialsK, int seed,
            out int forcedSteps, out int decisionSteps)
        {
            forcedSteps = 0;
            decisionSteps = 0;
            var diag = new DiagnosticsAccumulator();
            RunTrials(level, trialsK, seed, out int successes, null, null, diag, out _);
            forcedSteps = diag.forcedSteps;
            decisionSteps = diag.decisionSteps;
            return trialsK > 0 ? (successes / (float)trialsK) : 0f;
        }

        /// <summary>
        /// Full evaluation: success rate + start-point distribution + corridor proxies.
        /// </summary>
        public static EvaluationStats EvaluateDetailed(LevelData level, int trialsK, int seed)
        {
            var stats = new EvaluationStats();
            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0 || trialsK <= 0)
                return stats;

            var accumulator = new DiagnosticsAccumulator { collectVisualDominance = true };
            var successEdgeCounts = new Dictionary<(int from, int to), int>();
            RunTrials(level, trialsK, seed, out int successes, null, successEdgeCounts, accumulator, statsOut: out var startRates);

            stats.successes = successes;
            stats.successRate = successes / (float)trialsK;
            stats.forcedSteps = accumulator.forcedSteps;
            stats.decisionSteps = accumulator.decisionSteps;
            stats.forcedRatio = accumulator.decisionSteps > 0 ? accumulator.forcedSteps / (float)accumulator.decisionSteps : 0f;
            stats.visualDominantSteps = accumulator.visualDominantSteps;
            stats.visualDecisionSteps = accumulator.visualDecisionSteps;
            stats.corridorVisualRatio = accumulator.visualDecisionSteps > 0
                ? accumulator.visualDominantSteps / (float)accumulator.visualDecisionSteps
                : 0f;

            stats.diodeUsageRate = accumulator.successesWithDiodeUse > 0 && successes > 0
                ? accumulator.successesWithDiodeUse / (float)successes
                : 0f;
            stats.avgDiodeUseCountOnSuccess = successes > 0
                ? accumulator.diodeUseCountOnSuccessTotal / (float)successes
                : 0f;

            if (startRates != null && startRates.Count > 0)
            {
                float sum = 0f;
                float best = 0f;
                var sorted = new List<float>(startRates.Count);
                foreach (float r in startRates)
                {
                    sum += r;
                    if (r > best) best = r;
                    sorted.Add(r);
                }

                sorted.Sort();
                int p80Index = Mathf.Clamp(Mathf.CeilToInt(sorted.Count * 0.8f) - 1, 0, sorted.Count - 1);
                stats.avgStartSuccessRate = sum / sorted.Count;
                stats.bestStartSuccessRate = best;
                stats.p80StartSuccessRate = sorted[p80Index];
            }

            stats.topEdgesShare = ComputeTopEdgesShare(successEdgeCounts, 0.10f);
            return stats;
        }

        private sealed class DiagnosticsAccumulator
        {
            public int forcedSteps;
            public int decisionSteps;
            public bool collectVisualDominance;
            public int visualDominantSteps;
            public int visualDecisionSteps;
            public int successesWithDiodeUse;
            public int diodeUseCountOnSuccessTotal;
            public const float VisualMarginThreshold = 0.22f;
        }

        private static void RunTrials(LevelData level, int trialsK, int seed,
            out int successes,
            Dictionary<(int from, int to), int> directedEdgeCountsAll,
            Dictionary<(int from, int to), int> directedEdgeCountsSuccess,
            DiagnosticsAccumulator diag,
            out List<float> statsOut)
        {
            successes = 0;
            statsOut = null;
            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0 || trialsK <= 0)
                return;

            int n = level.nodes.Length;
            var bulbIds = new List<int>();
            var nodeTypeBulb = new bool[n];
            for (int i = 0; i < level.nodes.Length; i++)
            {
                var nd = level.nodes[i];
                if (nd.id < 0 || nd.id >= n) continue;
                bool isBulb = nd.nodeType == NodeType.Bulb;
                nodeTypeBulb[nd.id] = isBulb;
                if (isBulb) bulbIds.Add(nd.id);
            }
            if (bulbIds.Count == 0) return;
            statsOut = new List<float>(bulbIds.Count);

            var adj = BuildAdjacency(level, n);
            if (adj == null) return;
            var positions = BuildPositions(level, n);
            float avgEdgeLength = ComputeAverageEdgeLength(level, positions);
            var centroid = ComputeCentroid(positions, n);
            float maxCentroidRadius = ComputeMaxCentroidRadius(positions, centroid, n);

            var rng = new System.Random(seed);
            var startTrials = new int[n];
            var startSuccesses = new int[n];
            for (int t = 0; t < trialsK; t++)
            {
                int startId = bulbIds[rng.Next(bulbIds.Count)];
                startTrials[startId]++;
                bool success = RunOneTrial(
                    level, n, adj, nodeTypeBulb, bulbIds.Count, startId, rng,
                    directedEdgeCountsAll, diag, positions, centroid, maxCentroidRadius, avgEdgeLength,
                    out int diodeUsesThisTrial, out var trialEdgeSteps);

                if (success)
                {
                    successes++;
                    startSuccesses[startId]++;
                    if (directedEdgeCountsSuccess != null)
                    {
                        foreach (var step in trialEdgeSteps)
                        {
                            if (!directedEdgeCountsSuccess.TryGetValue(step, out int c)) c = 0;
                            directedEdgeCountsSuccess[step] = c + 1;
                        }
                    }
                    if (diag != null && diodeUsesThisTrial > 0) diag.successesWithDiodeUse++;
                    if (diag != null) diag.diodeUseCountOnSuccessTotal += diodeUsesThisTrial;
                }
            }

            for (int i = 0; i < bulbIds.Count; i++)
            {
                int id = bulbIds[i];
                if (startTrials[id] <= 0) continue;
                statsOut.Add(startSuccesses[id] / (float)startTrials[id]);
            }
        }

        private static List<(int neighbor, EdgeData edge)>[] BuildAdjacency(LevelData level, int n)
        {
            var adj = new List<(int neighbor, EdgeData edge)>[n];
            var blocked = new bool[n];
            for (int i = 0; i < n; i++)
                adj[i] = new List<(int neighbor, EdgeData edge)>();
            for (int i = 0; i < level.nodes.Length; i++)
            {
                var node = level.nodes[i];
                if (node != null && node.id >= 0 && node.id < n && node.nodeType == NodeType.Blocked)
                    blocked[node.id] = true;
            }
            foreach (var e in level.edges)
            {
                if (e.a >= 0 && e.a < n && e.b >= 0 && e.b < n)
                {
                    if (blocked[e.a] || blocked[e.b]) continue;
                    adj[e.a].Add((e.b, e));
                    adj[e.b].Add((e.a, e));
                }
            }
            return adj;
        }

        private static bool RunOneTrial(LevelData level, int n,
            List<(int neighbor, EdgeData edge)>[] adj,
            bool[] nodeTypeBulb, int totalBulbs, int startId, System.Random rng,
            Dictionary<(int from, int to), int> directedEdgeCountsAll, DiagnosticsAccumulator diag,
            IReadOnlyList<Vector2> positions, Vector2 centroid, float maxCentroidRadius, float avgEdgeLength,
            out int diodeUsesThisTrial, out List<(int from, int to)> directedSteps)
        {
            diodeUsesThisTrial = 0;
            directedSteps = new List<(int from, int to)>(n);
            var pathSet = new HashSet<int>();
            var visitedBulbs = new HashSet<int>();
            int current = startId;
            int previous = -1;
            pathSet.Add(current);
            if (nodeTypeBulb[current]) visitedBulbs.Add(current);

            int maxSteps = n;
            int steps = 0;
            while (visitedBulbs.Count < totalBulbs)
            {
                if (++steps > maxSteps)
                    return false;

                var valid = new List<(int next, EdgeData edge)>();
                foreach (var (neighbor, edge) in adj[current])
                {
                    if (pathSet.Contains(neighbor)) continue;
                    bool fromAtoB = (current == edge.a && neighbor == edge.b);
                    if (edge.diode == DiodeMode.AtoB && !fromAtoB) continue;
                    if (edge.diode == DiodeMode.BtoA && fromAtoB) continue;
                    valid.Add((neighbor, edge));
                }
                if (valid.Count == 0) return false;

                if (diag != null)
                {
                    diag.decisionSteps++;
                    if (valid.Count == 1) diag.forcedSteps++;
                    if (diag.collectVisualDominance && valid.Count >= 2)
                    {
                        diag.visualDecisionSteps++;
                        float best = float.MinValue;
                        float second = float.MinValue;
                        for (int i = 0; i < valid.Count; i++)
                        {
                            float s = ComputeVisualMoveScore(previous, current, valid[i].next, positions, centroid, maxCentroidRadius, avgEdgeLength);
                            if (s > best)
                            {
                                second = best;
                                best = s;
                            }
                            else if (s > second)
                            {
                                second = s;
                            }
                        }
                        if (best - second >= DiagnosticsAccumulator.VisualMarginThreshold)
                            diag.visualDominantSteps++;
                    }
                }

                var chosen = valid[rng.Next(valid.Count)];
                if (directedEdgeCountsAll != null)
                {
                    var key = (current, chosen.next);
                    if (!directedEdgeCountsAll.TryGetValue(key, out int c)) c = 0;
                    directedEdgeCountsAll[key] = c + 1;
                }
                directedSteps.Add((current, chosen.next));
                if (chosen.edge.diode != DiodeMode.None) diodeUsesThisTrial++;
                previous = current;
                current = chosen.next;
                pathSet.Add(current);
                if (nodeTypeBulb[current]) visitedBulbs.Add(current);
            }
            return true;
        }

        private static float ComputeVisualMoveScore(
            int previous, int current, int next,
            IReadOnlyList<Vector2> positions, Vector2 centroid, float maxCentroidRadius, float avgEdgeLength)
        {
            if (positions == null || current < 0 || next < 0 || current >= positions.Count || next >= positions.Count)
                return 0f;

            Vector2 currentPos = positions[current];
            Vector2 nextPos = positions[next];
            Vector2 dir = (nextPos - currentPos).normalized;

            float straightness = 0.5f;
            if (previous >= 0 && previous < positions.Count && previous != current)
            {
                Vector2 prevDir = (currentPos - positions[previous]).normalized;
                straightness = (Vector2.Dot(prevDir, dir) + 1f) * 0.5f;
            }

            float length = Vector2.Distance(currentPos, nextPos);
            float normalizedLen = avgEdgeLength > 1e-4f ? Mathf.Clamp(length / avgEdgeLength, 0.5f, 1.8f) : 1f;
            float edgeLengthScore = 1f - Mathf.Abs(normalizedLen - 1f) * 0.8f;

            float centroidDist = Vector2.Distance(nextPos, centroid);
            float centrality = maxCentroidRadius > 1e-4f
                ? Mathf.Clamp01(1f - centroidDist / maxCentroidRadius)
                : 0f;
            float crossZonePenalty = centrality;

            return straightness * 0.72f + edgeLengthScore * 0.36f - crossZonePenalty * 0.35f;
        }

        private static Vector2[] BuildPositions(LevelData level, int n)
        {
            var positions = new Vector2[n];
            for (int i = 0; i < level.nodes.Length; i++)
            {
                var node = level.nodes[i];
                if (node.id >= 0 && node.id < n)
                    positions[node.id] = node.pos;
            }
            return positions;
        }

        private static float ComputeAverageEdgeLength(LevelData level, IReadOnlyList<Vector2> positions)
        {
            if (level?.edges == null || positions == null || level.edges.Length == 0) return 1f;
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < level.edges.Length; i++)
            {
                var e = level.edges[i];
                if (e.a >= 0 && e.a < positions.Count && e.b >= 0 && e.b < positions.Count)
                {
                    sum += Vector2.Distance(positions[e.a], positions[e.b]);
                    count++;
                }
            }
            return count > 0 ? Mathf.Max(0.1f, sum / count) : 1f;
        }

        private static Vector2 ComputeCentroid(IReadOnlyList<Vector2> positions, int n)
        {
            if (positions == null || n <= 0) return Vector2.zero;
            Vector2 c = Vector2.zero;
            for (int i = 0; i < n; i++) c += positions[i];
            return c / n;
        }

        private static float ComputeMaxCentroidRadius(IReadOnlyList<Vector2> positions, Vector2 centroid, int n)
        {
            if (positions == null || n <= 0) return 1f;
            float maxR = 0f;
            for (int i = 0; i < n; i++)
            {
                float d = Vector2.Distance(positions[i], centroid);
                if (d > maxR) maxR = d;
            }
            return Mathf.Max(0.1f, maxR);
        }

        private static float ComputeTopEdgesShare(Dictionary<(int from, int to), int> successDirectedEdgeCounts, float topFraction)
        {
            if (successDirectedEdgeCounts == null || successDirectedEdgeCounts.Count == 0)
                return 0f;

            var undirectedCounts = new Dictionary<(int a, int b), int>();
            int total = 0;
            foreach (var kv in successDirectedEdgeCounts)
            {
                int a = Math.Min(kv.Key.from, kv.Key.to);
                int b = Math.Max(kv.Key.from, kv.Key.to);
                var key = (a, b);
                if (!undirectedCounts.TryGetValue(key, out int c)) c = 0;
                undirectedCounts[key] = c + kv.Value;
                total += kv.Value;
            }
            if (undirectedCounts.Count == 0 || total <= 0) return 0f;

            int topCount = Mathf.Max(1, Mathf.CeilToInt(undirectedCounts.Count * Mathf.Clamp01(topFraction)));
            int topSum = undirectedCounts.Values.OrderByDescending(v => v).Take(topCount).Sum();
            return topSum / (float)total;
        }
    }
}
