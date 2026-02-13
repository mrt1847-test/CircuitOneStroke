using System;
using System.Collections.Generic;
using System.Diagnostics;
using CircuitOneStroke.Data;
using CircuitOneStroke.Solver;
using UnityEngine;
using Random = System.Random;

namespace CircuitOneStroke.Generation
{
    public struct GenerateParams
    {
        public int NodeCountMin;
        public int NodeCountMax;
        public int TargetSolutionsMin;
        public int TargetSolutionsMax;
        public DifficultyTier Difficulty;
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

    public struct GenerationStats
    {
        public int finalN;
        public int attempts;
        public long timeMs;
        public int solutionStraightRun;
        public float perimeterBias;
        public int decoyEdgeCount;
        public float mcSuccessRate;
    }

    /// <summary>
    /// Solution-first + incremental expansion generator:
    /// start N=3, insert nodes into the solution path, then add bounded decoys with rollback checks.
    /// </summary>
    public static class BackboneFirstGenerator
    {
        private const int DefaultMaxAttemptsPerNode = 30;
        private const int DefaultMaxTotalAttempts = 240;
        private const int DefaultRuntimeTimeBudgetMs = 180;
        private const int DefaultEditorTimeBudgetMs = 3000;
        private const int DefaultSeedRetryCount = 6;
        private const float DefaultMaxDecoyPerNodeMedium = 1.35f;
        private const float DefaultMaxDecoyPerNodeHard = 2.0f;
        private const int DefaultTrialsK = 80;
        private const float ForcedRatioRollbackThreshold = 0.74f;
        private const float HardSuccessRateRollbackThreshold = 0.42f;
        private const int MaxNodeDegree = 5;
        private const int MaxLayoutRetries = 12;
        private const float JitterFractionOfAvgEdge = 0.04f;

        public static LevelData Generate(GenerateParams p)
        {
            var rng = new Random(p.Seed);
            int targetN = rng.Next(p.NodeCountMin, p.NodeCountMax + 1);
            var opts = new GenerationOptions
            {
                IncludeSwitch = false,
                SwitchCount = 0,
                RequireNormalHardVariety = p.Difficulty == DifficultyTier.Hard
            };

            if (!GenerateSolutionFirst(targetN, p.Seed, opts, out LevelData level, out _))
                throw new InvalidOperationException($"GenerateSolutionFirst failed for targetN={targetN}, seed={p.Seed}");
            return level;
        }

        public static bool GenerateSolutionFirst(int targetN, int seed, GenerationOptions opts, out LevelData level, out GenerationStats stats)
        {
            level = null;
            stats = default;

            // Safety clamp: this generator is coupled to current solver bitmask limits.
            targetN = Mathf.Max(3, targetN);
            targetN = Mathf.Min(targetN, LevelSolver.MaxNodesSupported);

            int maxAttemptsPerNode = opts.MaxAttemptsPerNode > 0 ? opts.MaxAttemptsPerNode : DefaultMaxAttemptsPerNode;
            int maxTotalAttempts = opts.MaxTotalAttempts > 0 ? opts.MaxTotalAttempts : Mathf.Max(DefaultMaxTotalAttempts, targetN * maxAttemptsPerNode);
            int seedRetries = opts.SeedRetryCount > 0 ? opts.SeedRetryCount : DefaultSeedRetryCount;
            bool hardMode = opts.RequireNormalHardVariety;
            float decoyPerNode = opts.MaxDecoysPerNode > 0f
                ? opts.MaxDecoysPerNode
                : (hardMode ? DefaultMaxDecoyPerNodeHard : DefaultMaxDecoyPerNodeMedium);

#if UNITY_EDITOR
            int timeBudgetMs = opts.TimeBudgetMs > 0 ? opts.TimeBudgetMs : DefaultEditorTimeBudgetMs;
            bool debugLog = opts.EnableEditorDebugLog;
#else
            int timeBudgetMs = opts.TimeBudgetMs > 0 ? opts.TimeBudgetMs : DefaultRuntimeTimeBudgetMs;
            const bool debugLog = false;
#endif

            int totalAttempts = 0;
            var globalWatch = Stopwatch.StartNew();

            for (int retry = 0; retry < seedRetries; retry++)
            {
                if (globalWatch.ElapsedMilliseconds > timeBudgetMs)
                    break;

                int useSeed = seed + retry * 1009;
                var rng = new Random(useSeed);
                var solutionPath = new List<int> { 0, 1, 2 };
                var solutionEdgeSet = new HashSet<(int a, int b)>
                {
                    NormalizeEdge(0, 1),
                    NormalizeEdge(1, 2)
                };
                // edgeSet contains both solution edges and optional decoy edges.
                var edgeSet = new HashSet<(int a, int b)>(solutionEdgeSet);
                var degrees = new Dictionary<int, int> { [0] = 1, [1] = 2, [2] = 1 };

                bool expanded = true;
                for (int nextNode = 3; nextNode < targetN; nextNode++)
                {
                    bool inserted = false;
                    int localAttempts = 0;
                    while (!inserted && localAttempts < maxAttemptsPerNode && totalAttempts < maxTotalAttempts && globalWatch.ElapsedMilliseconds <= timeBudgetMs)
                    {
                        localAttempts++;
                        totalAttempts++;
                        int insertAfter = rng.Next(0, solutionPath.Count - 1);
                        int u = solutionPath[insertAfter];
                        int v = solutionPath[insertAfter + 1];

                        if (GetDegree(degrees, u) >= MaxNodeDegree || GetDegree(degrees, v) >= MaxNodeDegree)
                            continue;

                        // Core expansion rule:
                        // pick an existing adjacent pair u-v on solutionPath and replace it with u-nextNode-v.
                        InsertNodeIntoSolutionPath(solutionPath, insertAfter, nextNode);
                        var ux = NormalizeEdge(u, nextNode);
                        var xv = NormalizeEdge(nextNode, v);
                        edgeSet.Add(ux);
                        edgeSet.Add(xv);
                        solutionEdgeSet.Add(ux);
                        solutionEdgeSet.Add(xv);
                        // keep (u,v) as optional decoy by default for richer choices.
                        edgeSet.Add(NormalizeEdge(u, v));
                        IncreaseDegree(degrees, u);
                        IncreaseDegree(degrees, v);
                        degrees[nextNode] = 2;
                        inserted = true;

                        if (debugLog)
                        {
                            UnityEngine.Debug.Log(
                                $"[SolutionFirst] seed={useSeed} N={nextNode + 1} insert=({u},{v}) via={nextNode} totalAttempts={totalAttempts}");
                        }
                    }

                    if (!inserted)
                    {
                        expanded = false;
                        break;
                    }
                }

                if (!expanded)
                    continue;

                var positions = TryPlaceNodes(targetN, edgeSet, useSeed);
                if (positions == null || positions.Length != targetN)
                    continue;

                int straightRun = ComputeLongestStraightRunOnSolution(solutionPath, positions);
                float perimeterBias = ComputePerimeterBias(solutionPath, positions);
                var hullSet = BuildConvexHullSet(positions);

                int decoyBudget = Mathf.Clamp(Mathf.FloorToInt(targetN * decoyPerNode), 0, targetN * 3);
                int decoyAdded = 0;
                var candidates = BuildDecoyCandidates(solutionPath, edgeSet, hullSet, rng);
                for (int i = 0; i < candidates.Count && decoyAdded < decoyBudget && totalAttempts < maxTotalAttempts && globalWatch.ElapsedMilliseconds <= timeBudgetMs; i++)
                {
                    totalAttempts++;
                    var c = candidates[i];
                    if (GetDegree(degrees, c.a) >= MaxNodeDegree || GetDegree(degrees, c.b) >= MaxNodeDegree)
                        continue;
                    if (!TryAddDecoyAndValidate(
                        targetN,
                        edgeSet,
                        degrees,
                        c.a,
                        c.b,
                        useSeed + i * 13,
                        hardMode,
                        out float successRate))
                    {
                        continue;
                    }

                    // Only count decoy when rollback filters allow it.
                    decoyAdded++;
                    if (debugLog)
                    {
                        UnityEngine.Debug.Log(
                            $"[SolutionFirst] seed={useSeed} decoy=({c.a},{c.b}) added={decoyAdded}/{decoyBudget} mc={successRate:F3}");
                    }
                }

                level = BuildLevel(targetN, positions, edgeSet, solutionPath, solutionEdgeSet);
                var finalMc = MonteCarloEvaluator.EvaluateDetailed(level, DefaultTrialsK, useSeed + 333);
                stats = new GenerationStats
                {
                    finalN = targetN,
                    attempts = totalAttempts,
                    timeMs = globalWatch.ElapsedMilliseconds,
                    solutionStraightRun = straightRun,
                    perimeterBias = perimeterBias,
                    decoyEdgeCount = decoyAdded,
                    mcSuccessRate = finalMc.successRate
                };

                if (debugLog)
                {
                    UnityEngine.Debug.Log(
                        $"[SolutionFirst] done seed={useSeed} N={targetN} attempts={stats.attempts} decoys={decoyAdded} straightRun={straightRun} perimeterBias={perimeterBias:F2} timeMs={stats.timeMs}");
                }
                return true;
            }

            stats.finalN = targetN;
            stats.attempts = totalAttempts;
            stats.timeMs = globalWatch.ElapsedMilliseconds;
            return false;
        }

        private static LevelData BuildLevel(
            int n,
            IReadOnlyList<Vector2> positions,
            HashSet<(int a, int b)> edgeSet,
            IReadOnlyList<int> solutionPath,
            HashSet<(int a, int b)> solutionEdgeSet)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelId = 0;
            level.nodes = new NodeData[n];
            for (int i = 0; i < n; i++)
            {
                level.nodes[i] = new NodeData
                {
                    id = i,
                    pos = positions[i],
                    nodeType = NodeType.Bulb,
                    switchGroupId = 0
                };
            }

            var edges = new List<EdgeData>(edgeSet.Count);
            int edgeId = 0;
            foreach (var (a, b) in edgeSet)
            {
                edges.Add(new EdgeData
                {
                    id = edgeId++,
                    a = a,
                    b = b,
                    diode = DiodeMode.None,
                    gateGroupId = -1,
                    initialGateOpen = true
                });
            }
            level.edges = edges.ToArray();

            level.solutionPath = new List<int>(solutionPath);
            level.solutionEdgesUndirected = new List<Vector2Int>(solutionEdgeSet.Count);
            foreach (var (a, b) in solutionEdgeSet)
                level.solutionEdgesUndirected.Add(new Vector2Int(a, b));

            return level;
        }

        private static void InsertNodeIntoSolutionPath(List<int> path, int insertAfter, int newNodeId)
        {
            path.Insert(insertAfter + 1, newNodeId);
        }

        private static List<(int a, int b)> BuildDecoyCandidates(
            IReadOnlyList<int> solutionPath,
            HashSet<(int a, int b)> edgeSet,
            HashSet<int> hullSet,
            Random rng)
        {
            var pathIndex = new Dictionary<int, int>();
            for (int i = 0; i < solutionPath.Count; i++)
                pathIndex[solutionPath[i]] = i;

            var list = new List<(int a, int b, float score)>();
            for (int i = 0; i < solutionPath.Count; i++)
            {
                int a = solutionPath[i];
                for (int j = i + 1; j < solutionPath.Count; j++)
                {
                    int b = solutionPath[j];
                    var e = NormalizeEdge(a, b);
                    if (edgeSet.Contains(e))
                        continue;
                    int d = Math.Abs(pathIndex[a] - pathIndex[b]);
                    if (d < 2 || d > 5)
                        continue;

                    float score = 1f;
                    if (d == 2) score += 0.30f;
                    if (d == 3) score += 0.15f;
                    if (hullSet.Contains(a) && hullSet.Contains(b) && d <= 2)
                        score -= 0.45f;
                    score += (float)rng.NextDouble() * 0.2f;
                    list.Add((a, b, score));
                }
            }

            list.Sort((x, y) => y.score.CompareTo(x.score));
            var result = new List<(int a, int b)>(list.Count);
            for (int i = 0; i < list.Count; i++)
                result.Add((list[i].a, list[i].b));
            return result;
        }

        private static bool TryAddDecoyAndValidate(
            int nodeCount,
            HashSet<(int a, int b)> edgeSet,
            Dictionary<int, int> degrees,
            int a,
            int b,
            int seed,
            bool hardMode,
            out float successRate)
        {
            successRate = 0f;
            var e = NormalizeEdge(a, b);
            if (edgeSet.Contains(e))
                return false;

            edgeSet.Add(e);
            IncreaseDegree(degrees, a);
            IncreaseDegree(degrees, b);

            LevelData probe = BuildProbeLevel(nodeCount, edgeSet);
            var mc = MonteCarloEvaluator.EvaluateDetailed(probe, DefaultTrialsK, seed);
            UnityEngine.Object.DestroyImmediate(probe);

            // Rollback policy:
            // 1) reject if forced-move ratio spikes (too corridor-like),
            // 2) in hard mode reject if random-policy success is too high (too readable/easy).
            bool rollback = mc.forcedRatio >= ForcedRatioRollbackThreshold;
            if (!rollback && hardMode && mc.successRate > HardSuccessRateRollbackThreshold)
                rollback = true;

            if (rollback)
            {
                edgeSet.Remove(e);
                DecreaseDegree(degrees, a);
                DecreaseDegree(degrees, b);
                return false;
            }

            successRate = mc.successRate;
            return true;
        }

        private static LevelData BuildProbeLevel(int n, HashSet<(int a, int b)> edgeSet)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.nodes = new NodeData[n];
            for (int i = 0; i < n; i++)
                level.nodes[i] = new NodeData { id = i, pos = Vector2.zero, nodeType = NodeType.Bulb };

            var edges = new List<EdgeData>(edgeSet.Count);
            int edgeId = 0;
            foreach (var (a, b) in edgeSet)
                edges.Add(new EdgeData { id = edgeId++, a = a, b = b });
            level.edges = edges.ToArray();
            return level;
        }

        private static Vector2[] TryPlaceNodes(int n, HashSet<(int a, int b)> edgeSet, int seed)
        {
            var rng = new Random(seed ^ 0x575757);
            var edgesForEval = new List<EdgeData>(edgeSet.Count);
            int edgeId = 0;
            foreach (var (a, b) in edgeSet)
                edgesForEval.Add(new EdgeData { id = edgeId++, a = a, b = b });

            var layouts = LayoutTemplates.GetLayoutsForNodeCountV2(n);
            if (layouts == null || layouts.Count == 0)
                return RingFallback(n);

            for (int t = 0; t < MaxLayoutRetries; t++)
            {
                var layout = layouts[rng.Next(layouts.Count)];
                if (layout.slots == null || layout.slots.Length < n)
                {
                    LayoutTemplateTelemetry.RecordTry(layout.name, false, "slots_short");
                    continue;
                }

                var pos = new Vector2[n];
                var perm = new int[n];
                for (int i = 0; i < n; i++) perm[i] = i;
                Shuffle(perm, rng);
                for (int i = 0; i < n; i++) pos[i] = layout.slots[perm[i]];

                float avgLen = 1.5f;
                if (edgesForEval.Count > 0)
                    avgLen = Mathf.Max(0.1f, AestheticEvaluator.AverageEdgeLength(edgesForEval, pos));
                float jitter = avgLen * JitterFractionOfAvgEdge;
                for (int i = 0; i < n; i++)
                {
                    pos[i].x += (float)(rng.NextDouble() * 2 - 1) * jitter;
                    pos[i].y += (float)(rng.NextDouble() * 2 - 1) * jitter;
                }

                float minDist = AestheticEvaluator.MinNodeDistance(pos, n);
                if (minDist < 0.35f)
                {
                    LayoutTemplateTelemetry.RecordTry(layout.name, false, "min_node_distance");
                    continue;
                }
                // Accept uses crossings/spacing/edge-length consistency heuristics.
                if (!AestheticEvaluator.Accept(edgesForEval, pos, n, 2, 0.22f, 0.45f))
                {
                    LayoutTemplateTelemetry.RecordTry(layout.name, false, "aesthetic_reject");
                    continue;
                }
                LayoutTemplateTelemetry.RecordTry(layout.name, true);
                LayoutTemplateTelemetry.RecordChosen(layout.name);
                LayoutTemplateTelemetry.DumpPeriodicIfNeeded("BackboneFirstGenerator");
                return pos;
            }

            LayoutTemplateTelemetry.RecordFallback("BackboneFirstGenerator.TryPlaceNodes", n);
            LayoutTemplateTelemetry.DumpPeriodicIfNeeded("BackboneFirstGenerator");
            // Last-resort placement to keep generation progressing.
            return RingFallback(n);
        }

        private static Vector2[] RingFallback(int n)
        {
            var pos = new Vector2[n];
            const float r = 3.5f;
            for (int i = 0; i < n; i++)
            {
                float angle = (2f * Mathf.PI * i / n) - Mathf.PI / 2f;
                pos[i] = new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
            }
            return pos;
        }

        private static int ComputeLongestStraightRunOnSolution(IReadOnlyList<int> solutionPath, IReadOnlyList<Vector2> positions, float maxTurnDeg = 16f)
        {
            if (solutionPath == null || positions == null || solutionPath.Count < 3)
                return 0;
            int best = 0;
            int run = 0;
            for (int i = 1; i < solutionPath.Count - 1; i++)
            {
                int prev = solutionPath[i - 1];
                int cur = solutionPath[i];
                int next = solutionPath[i + 1];
                Vector2 d1 = (positions[cur] - positions[prev]).normalized;
                Vector2 d2 = (positions[next] - positions[cur]).normalized;
                float turn = Vector2.Angle(d1, d2);
                if (turn <= maxTurnDeg)
                {
                    run++;
                    if (run > best)
                        best = run;
                }
                else
                {
                    run = 0;
                }
            }
            // run counts interior corners, so convert to approximate node-run length.
            return best + 2;
        }

        private static float ComputePerimeterBias(IReadOnlyList<int> solutionPath, IReadOnlyList<Vector2> positions)
        {
            if (solutionPath == null || positions == null || solutionPath.Count == 0)
                return 0f;
            var hull = BuildConvexHullSet(positions);
            if (hull.Count == 0)
                return 0f;
            int onHull = 0;
            for (int i = 0; i < solutionPath.Count; i++)
                if (hull.Contains(solutionPath[i]))
                    onHull++;
            return onHull / (float)solutionPath.Count;
        }

        private static HashSet<int> BuildConvexHullSet(IReadOnlyList<Vector2> points)
        {
            var hull = new HashSet<int>();
            if (points == null || points.Count < 3)
                return hull;

            var ids = new List<int>(points.Count);
            for (int i = 0; i < points.Count; i++) ids.Add(i);
            ids.Sort((a, b) =>
            {
                int cx = points[a].x.CompareTo(points[b].x);
                return cx != 0 ? cx : points[a].y.CompareTo(points[b].y);
            });

            var lower = new List<int>();
            for (int i = 0; i < ids.Count; i++)
            {
                int id = ids[i];
                while (lower.Count >= 2 && Cross(points[lower[lower.Count - 2]], points[lower[lower.Count - 1]], points[id]) <= 0f)
                    lower.RemoveAt(lower.Count - 1);
                lower.Add(id);
            }

            var upper = new List<int>();
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                int id = ids[i];
                while (upper.Count >= 2 && Cross(points[upper[upper.Count - 2]], points[upper[upper.Count - 1]], points[id]) <= 0f)
                    upper.RemoveAt(upper.Count - 1);
                upper.Add(id);
            }

            for (int i = 0; i < lower.Count; i++) hull.Add(lower[i]);
            for (int i = 0; i < upper.Count; i++) hull.Add(upper[i]);
            return hull;
        }

        private static float Cross(Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 ab = b - a;
            Vector2 ac = c - a;
            return ab.x * ac.y - ab.y * ac.x;
        }

        private static (int a, int b) NormalizeEdge(int a, int b)
        {
            return a < b ? (a, b) : (b, a);
        }

        private static int GetDegree(Dictionary<int, int> degrees, int node)
        {
            return degrees.TryGetValue(node, out int d) ? d : 0;
        }

        private static void IncreaseDegree(Dictionary<int, int> degrees, int node)
        {
            if (!degrees.ContainsKey(node))
                degrees[node] = 0;
            degrees[node]++;
        }

        private static void DecreaseDegree(Dictionary<int, int> degrees, int node)
        {
            if (!degrees.ContainsKey(node))
                return;
            degrees[node] = Mathf.Max(0, degrees[node] - 1);
        }

        private static void Shuffle(int[] array, Random rng)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
