using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Solver;

namespace CircuitOneStroke.Generation
{
    /// <summary>
    /// Layout and corridor diagnostics shared by generator/tuner.
    /// </summary>
    public static class LayoutDiagnostics
    {
        public static float ComputeBranchRatio(LevelData level)
        {
            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0) return 0f;
            int n = level.nodes.Length;
            var adj = BuildAdjacency(level, n);
            int branchNodes = 0;
            for (int i = 0; i < n; i++)
                if (adj[i].Count >= 3) branchNodes++;
            return branchNodes / (float)n;
        }

        public static int ComputeMaxDegree2ChainLen(LevelData level)
        {
            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0) return 0;
            int n = level.nodes.Length;
            var adj = BuildAdjacency(level, n);
            return ComputeMaxDegree2ChainLenFromAdj(adj, n);
        }

        internal static int ComputeMaxDegree2ChainLenFromAdj(List<int>[] adj, int n)
        {
            int[] deg = new int[n];
            for (int i = 0; i < n; i++) deg[i] = adj[i].Count;

            bool allDeg2 = true;
            for (int i = 0; i < n; i++)
            {
                if (deg[i] != 2) { allDeg2 = false; break; }
            }
            if (allDeg2) return n;

            var used = new HashSet<(int, int)>();
            int maxLen = 0;
            for (int s = 0; s < n; s++)
            {
                if (deg[s] == 2) continue;
                foreach (int next in adj[s])
                {
                    int a = Mathf.Min(s, next);
                    int b = Mathf.Max(s, next);
                    if (used.Contains((a, b))) continue;

                    int prev = s;
                    int cur = next;
                    int len = 0;
                    while (deg[cur] == 2)
                    {
                        len++;
                        int n0 = adj[cur][0];
                        int n1 = adj[cur][1];
                        int nxt = (n0 == prev) ? n1 : n0;
                        used.Add((Mathf.Min(prev, cur), Mathf.Max(prev, cur)));
                        prev = cur;
                        cur = nxt;
                    }

                    used.Add((Mathf.Min(prev, cur), Mathf.Max(prev, cur)));
                    if (len > maxLen) maxLen = len;
                }
            }
            return maxLen;
        }

        public static float ComputeMinEdgeNodeClearance(LevelData level)
        {
            if (level?.nodes == null || level.edges == null || level.nodes.Length < 3) return float.MaxValue;
            var positions = LevelPositions(level);
            return AestheticEvaluator.MinEdgeToNodeDistance(level.edges, positions, level.nodes.Length);
        }

        public static float ComputeMinAngleSeparationDeg(LevelData level)
        {
            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0) return 180f;
            int n = level.nodes.Length;
            var positions = LevelPositions(level);
            var adj = BuildAdjacency(level, n);
            return AestheticEvaluator.MinAngleSeparationDeg(adj, positions, n);
        }

        public static LayoutDiagnosticsResult ComputeLayoutMetrics(LevelData level)
        {
            var r = new LayoutDiagnosticsResult();
            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0) return r;

            int n = level.nodes.Length;
            var positions = LevelPositions(level);
            r.branchRatio = ComputeBranchRatio(level);
            r.maxDegree2ChainLen = ComputeMaxDegree2ChainLen(level);
            r.minNodeDist = AestheticEvaluator.MinNodeDistance(positions, n);
            r.minEdgeNodeClearance = AestheticEvaluator.MinEdgeToNodeDistance(level.edges, positions, n);
            r.minAngleSeparationDeg = ComputeMinAngleSeparationDeg(level);
            r.forcedRatio = -1f;
            return r;
        }

        public static LayoutDiagnosticsResult ComputeFull(LevelData level, int trialsK, int seed)
        {
            var r = ComputeLayoutMetrics(level);
            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0 || trialsK <= 0)
                return r;

            var stats = MonteCarloEvaluator.EvaluateDetailed(level, trialsK, seed);
            r.successRate = stats.successRate;
            r.avgStartSuccessRate = stats.avgStartSuccessRate;
            r.bestStartSuccessRate = stats.bestStartSuccessRate;
            r.p80StartSuccessRate = stats.p80StartSuccessRate;
            r.forcedRatio = stats.forcedRatio;
            r.corridorVisualRatio = stats.corridorVisualRatio;
            r.topEdgesShare = stats.topEdgesShare;
            r.diodeUsageRate = stats.diodeUsageRate;
            r.avgDiodeUseCountOnSuccess = stats.avgDiodeUseCountOnSuccess;
            return r;
        }

        private static Vector2[] LevelPositions(LevelData level)
        {
            var p = new Vector2[level.nodes.Length];
            for (int i = 0; i < level.nodes.Length; i++)
            {
                var nd = level.nodes[i];
                if (nd.id >= 0 && nd.id < p.Length)
                    p[nd.id] = nd.pos;
            }
            return p;
        }

        private static List<int>[] BuildAdjacency(LevelData level, int n)
        {
            var adj = new List<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new List<int>();
            foreach (var e in level.edges)
            {
                if (e.a >= 0 && e.a < n && e.b >= 0 && e.b < n && e.a != e.b)
                {
                    if (!adj[e.a].Contains(e.b)) adj[e.a].Add(e.b);
                    if (!adj[e.b].Contains(e.a)) adj[e.b].Add(e.a);
                }
            }
            return adj;
        }

        public struct LayoutDiagnosticsResult
        {
            public float branchRatio;
            public int maxDegree2ChainLen;
            public float minNodeDist;
            public float minEdgeNodeClearance;
            public float minAngleSeparationDeg;
            public float forcedRatio;
            public float successRate;
            public float avgStartSuccessRate;
            public float bestStartSuccessRate;
            public float p80StartSuccessRate;
            public float corridorVisualRatio;
            public float topEdgesShare;
            public float diodeUsageRate;
            public float avgDiodeUseCountOnSuccess;
        }
    }
}
