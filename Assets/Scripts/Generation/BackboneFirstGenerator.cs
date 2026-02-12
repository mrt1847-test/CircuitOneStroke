using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using CircuitOneStroke.Data;
using CircuitOneStroke.Solver;

namespace CircuitOneStroke.Generation
{
    /// <summary>Parameters for backbone-first level generation (16–25 nodes).</summary>
    public struct GenerateParams
    {
        public int NodeCountMin;
        public int NodeCountMax;
        public int TargetSolutionsMin;
        public int TargetSolutionsMax;
        public DifficultyTier Difficulty;
        /// <summary>Target average degree range (e.g. 2.4–3.2).</summary>
        public float TargetAvgDegreeMin;
        public float TargetAvgDegreeMax;
        public int Seed;

        public static GenerateParams Default => new GenerateParams
        {
            NodeCountMin = 16,
            NodeCountMax = 25,
            TargetSolutionsMin = 2,
            TargetSolutionsMax = 5,
            Difficulty = DifficultyTier.Medium,
            TargetAvgDegreeMin = 2.4f,
            TargetAvgDegreeMax = 3.2f,
            Seed = 0
        };
    }

    /// <summary>
    /// Backbone-first generator: builds a solution path first, then adds decoy edges for meaningful choices.
    /// Targets 16–25 nodes; decoys should fail later (3–8 steps), not instant reject.
    /// </summary>
    public static class BackboneFirstGenerator
    {
        private const int LayoutRetryCount = 8;
        private const float JitterFractionOfAvgEdge = 0.04f;
        private const int MaxAttemptsAddDecoy = 200;
        private const int GateTradeoffRetries = 15;

        /// <summary>Generate a single level. Deterministic for given seed.</summary>
        public static LevelData Generate(GenerateParams p)
        {
            var rng = new Random(p.Seed);
            int N = rng.Next(p.NodeCountMin, p.NodeCountMax + 1);
            N = Mathf.Clamp(N, 16, LevelSolver.MaxNodesSupported);

            // Stage 1: Topology
            int switchCount = p.Difficulty == DifficultyTier.Easy ? rng.Next(0, 2) :
                p.Difficulty == DifficultyTier.Medium ? rng.Next(1, 3) : rng.Next(2, 4);
            switchCount = Mathf.Min(switchCount, N - 1);

            var backbone = new List<int>();
            for (int i = 0; i < N; i++) backbone.Add(i);

            var edgeSet = new HashSet<(int a, int b)>();
            for (int i = 0; i < N - 1; i++)
            {
                int a = backbone[i], b = backbone[i + 1];
                edgeSet.Add((Math.Min(a, b), Math.Max(a, b)));
            }

            float targetAvg = (p.TargetAvgDegreeMin + p.TargetAvgDegreeMax) * 0.5f;
            int targetTotalDegree = Mathf.RoundToInt(N * targetAvg);
            int currentTotal = 2 * (N - 1);
            int maxHubs = p.Difficulty == DifficultyTier.Hard ? 2 : 1;
            var degree = new int[N];
            for (int i = 0; i < N - 1; i++) { degree[i]++; degree[i + 1]++; }

            for (int attempt = 0; attempt < MaxAttemptsAddDecoy && currentTotal < targetTotalDegree + N; attempt++)
            {
                int i = rng.Next(0, N);
                int j = rng.Next(0, N);
                if (Math.Abs(i - j) < 3) continue;
                int a = Math.Min(i, j), b = Math.Max(i, j);
                if (edgeSet.Contains((a, b))) continue;
                int newDegA = degree[a] + 1;
                int newDegB = degree[b] + 1;
                if (newDegA > 4 || newDegB > 4) continue;
                int hubCount = 0;
                for (int k = 0; k < N; k++)
                    if (degree[k] >= 4) hubCount++;
                if ((newDegA == 4 || newDegB == 4) && hubCount >= maxHubs) continue;
                edgeSet.Add((a, b));
                degree[a]++; degree[b]++;
                currentTotal += 2;
            }

            var edgeList = new List<(int a, int b)>(edgeSet);
            var backboneSet = new HashSet<(int a, int b)>();
            for (int i = 0; i < N - 1; i++)
                backboneSet.Add((backbone[i], backbone[i + 1]));

            // Switch positions (not 0, not N-1 for clarity)
            var switchIndices = new HashSet<int>();
            while (switchIndices.Count < switchCount)
            {
                int idx = rng.Next(1, N);
                switchIndices.Add(idx);
            }

            // Stage 2: Layout
            var layouts = LayoutTemplates.GetLayoutsForNodeCountV2(N);
            if (layouts == null || layouts.Count == 0)
                layouts = new List<LayoutTemplate> { new LayoutTemplate { name = "Ring", nodeCount = N, slots = RingFallback(N) } };

            Vector2[] positions = null;
            for (int tryLayout = 0; tryLayout < LayoutRetryCount; tryLayout++)
            {
                var layout = layouts[rng.Next(layouts.Count)];
                if (layout.slots == null || layout.slots.Length < N) continue;
                positions = new Vector2[N];
                var perm = new int[N];
                for (int i = 0; i < N; i++) perm[i] = i;
                Shuffle(perm, rng);
                for (int i = 0; i < N; i++)
                    positions[i] = layout.slots[perm[i]];

                float avgLen = 1.5f;
                if (edgeList.Count > 0)
                {
                    float sum = 0;
                    foreach (var (aa, bb) in edgeList)
                        sum += Vector2.Distance(positions[aa], positions[bb]);
                    avgLen = sum / edgeList.Count;
                }
                float jitter = Mathf.Max(0.05f, avgLen * JitterFractionOfAvgEdge);
                for (int i = 0; i < N; i++)
                {
                    positions[i].x += (float)(rng.NextDouble() * 2 - 1) * jitter;
                    positions[i].y += (float)(rng.NextDouble() * 2 - 1) * jitter;
                }

                var edgesForEval = new List<EdgeData>();
                int eid = 0;
                foreach (var (aa, bb) in edgeList)
                    edgesForEval.Add(new EdgeData { id = eid++, a = aa, b = bb });
                if (AestheticEvaluator.MinNodeDistance(positions, N) < 0.4f) continue;
                if (!AestheticEvaluator.Accept(edgesForEval, positions, N, 2, 0.22f, 0.4f))
                    continue;
                break;
            }

            if (positions == null)
            {
                positions = new Vector2[N];
                for (int i = 0; i < N; i++)
                {
                    float angle = (2f * Mathf.PI * i / N) - Mathf.PI / 2f;
                    positions[i] = new Vector2(3.5f * Mathf.Cos(angle), 3.5f * Mathf.Sin(angle));
                }
            }

            // Build EdgeData with gates and diodes
            int gateGroupCount = p.Difficulty == DifficultyTier.Easy ? 0 : p.Difficulty == DifficultyTier.Medium ? rng.Next(1, 3) : rng.Next(2, 4);
            var nonBackbone = new List<(int a, int b)>();
            foreach (var e in edgeList)
                if (!backboneSet.Contains((e.a, e.b)) && !backboneSet.Contains((e.b, e.a)))
                    nonBackbone.Add(e);

            var gatedEdges = new List<(int a, int b)>();
            bool[] initialOpen = Array.Empty<bool>();
            if (gateGroupCount > 0 && nonBackbone.Count > 0)
            {
                int gateCount = Mathf.Min(nonBackbone.Count, rng.Next(2, 6));
                for (int k = 0; k < GateTradeoffRetries; k++)
                {
                    gatedEdges = PickRandomSubset(nonBackbone, gateCount, rng);
                    initialOpen = new bool[gatedEdges.Count];
                    for (int i = 0; i < gatedEdges.Count; i++)
                        initialOpen[i] = rng.NextDouble() < 0.5;
                    if (gateCount == 0) break;
                    bool hasOpen = false, hasClosed = false;
                    for (int i = 0; i < gatedEdges.Count; i++)
                    {
                        if (initialOpen[i]) hasOpen = true; else hasClosed = true;
                    }
                    if (hasOpen && hasClosed) break;
                }
            }

            int diodeCount = p.Difficulty == DifficultyTier.Easy ? rng.Next(0, 2) : p.Difficulty == DifficultyTier.Medium ? rng.Next(1, 4) : rng.Next(2, 5);
            diodeCount = Mathf.Min(diodeCount, edgeList.Count);
            var diodeEdges = PickRandomSubset(edgeList, diodeCount, rng);
            var diodeDir = new Dictionary<(int a, int b), bool>();
            foreach (var (a, b) in diodeEdges)
            {
                bool atob = backboneSet.Contains((a, b)) ? (b == a + 1) : rng.NextDouble() < 0.5;
                diodeDir[(a, b)] = atob;
            }

            int edgeId = 0;
            var outEdges = new List<EdgeData>();
            foreach (var (a, b) in edgeList)
            {
                var ed = new EdgeData { id = edgeId++, a = a, b = b };
                if (diodeDir.TryGetValue((a, b), out bool atob))
                    ed.diode = atob ? DiodeMode.AtoB : DiodeMode.BtoA;
                for (int g = 0; g < gatedEdges.Count; g++)
                {
                    if ((gatedEdges[g].a == a && gatedEdges[g].b == b) || (gatedEdges[g].a == b && gatedEdges[g].b == a))
                    {
                        ed.gateGroupId = 1;
                        ed.initialGateOpen = initialOpen[g];
                        break;
                    }
                }
                outEdges.Add(ed);
            }

            var nodes = new List<NodeData>();
            for (int i = 0; i < N; i++)
            {
                nodes.Add(new NodeData
                {
                    id = i,
                    pos = positions[i],
                    nodeType = switchIndices.Contains(i) ? NodeType.Switch : NodeType.Bulb,
                    switchGroupId = switchIndices.Contains(i) ? 1 : 0
                });
            }

            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelId = 0;
            level.nodes = nodes.ToArray();
            level.edges = outEdges.ToArray();
            return level;
        }

        private static Vector2[] RingFallback(int n)
        {
            const float r = 3.5f;
            var s = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float angle = (2f * Mathf.PI * i / n) - Mathf.PI / 2f;
                s[i] = new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
            }
            return s;
        }

        private static void Shuffle(int[] a, Random rng)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (a[i], a[j]) = (a[j], a[i]);
            }
        }

        private static List<(int a, int b)> PickRandomSubset(List<(int a, int b)> source, int count, Random rng)
        {
            if (count >= source.Count) return new List<(int a, int b)>(source);
            var indices = new List<int>();
            for (int i = 0; i < source.Count; i++) indices.Add(i);
            for (int i = 0; i < count; i++)
            {
                int j = rng.Next(i, indices.Count);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            var result = new List<(int a, int b)>();
            for (int i = 0; i < count; i++)
                result.Add(source[indices[i]]);
            return result;
        }
    }
}
