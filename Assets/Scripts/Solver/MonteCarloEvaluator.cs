using System;
using System.Collections.Generic;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Solver
{
    /// <summary>
    /// Estimates level difficulty via Monte Carlo: uniform random start at a Bulb,
    /// uniform random valid moves. Success = all bulbs visited exactly once. No gates in simulation (evaluateWithGates=false).
    /// </summary>
    public static class MonteCarloEvaluator
    {
        /// <summary>
        /// Estimate success rate (0..1) over K trials with given seed. Start node is chosen uniformly among Bulb nodes.
        /// </summary>
        public static float EstimateSuccessRate(LevelData level, int trialsK, int seed)
        {
            RunTrials(level, trialsK, seed, out int successes, null);
            return trialsK > 0 ? (successes / (float)trialsK) : 0f;
        }

        /// <summary>
        /// Run trials and optionally accumulate directed edge (from, to) traversal counts for diode tuning.
        /// </summary>
        public static float RunTrialsWithEdgeCounts(LevelData level, int trialsK, int seed,
            out Dictionary<(int from, int to), int> directedEdgeCounts)
        {
            directedEdgeCounts = new Dictionary<(int from, int to), int>();
            RunTrials(level, trialsK, seed, out int successes, directedEdgeCounts);
            return trialsK > 0 ? (successes / (float)trialsK) : 0f;
        }

        private static void RunTrials(LevelData level, int trialsK, int seed,
            out int successes, Dictionary<(int from, int to), int> directedEdgeCounts)
        {
            successes = 0;
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

            var adj = BuildAdjacency(level, n);
            if (adj == null) return;

            var rng = new Random(seed);
            for (int t = 0; t < trialsK; t++)
            {
                int startId = bulbIds[rng.Next(bulbIds.Count)];
                bool success = RunOneTrial(level, n, adj, nodeTypeBulb, bulbIds.Count, startId, rng, directedEdgeCounts);
                if (success) successes++;
            }
        }

        private static List<(int neighbor, EdgeData edge)>[] BuildAdjacency(LevelData level, int n)
        {
            var adj = new List<(int neighbor, EdgeData edge)>[n];
            for (int i = 0; i < n; i++)
                adj[i] = new List<(int neighbor, EdgeData edge)>();
            foreach (var e in level.edges)
            {
                if (e.a >= 0 && e.a < n && e.b >= 0 && e.b < n)
                {
                    adj[e.a].Add((e.b, e));
                    adj[e.b].Add((e.a, e));
                }
            }
            return adj;
        }

        private static bool RunOneTrial(LevelData level, int n,
            List<(int neighbor, EdgeData edge)>[] adj,
            bool[] nodeTypeBulb, int totalBulbs, int startId, Random rng,
            Dictionary<(int from, int to), int> directedEdgeCounts)
        {
            var pathSet = new HashSet<int>();
            var visitedBulbs = new HashSet<int>();
            int current = startId;
            pathSet.Add(current);
            if (nodeTypeBulb[current]) visitedBulbs.Add(current);

            while (visitedBulbs.Count < totalBulbs)
            {
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
                var chosen = valid[rng.Next(valid.Count)];
                if (directedEdgeCounts != null)
                {
                    var key = (current, chosen.next);
                    if (!directedEdgeCounts.TryGetValue(key, out int c)) c = 0;
                    directedEdgeCounts[key] = c + 1;
                }
                current = chosen.next;
                pathSet.Add(current);
                if (nodeTypeBulb[current]) visitedBulbs.Add(current);
            }
            return true;
        }
    }
}
