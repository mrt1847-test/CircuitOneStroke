using System;
using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Solver;

namespace CircuitOneStroke.Generation
{
    /// <summary>난이도 구간. 다이오드/게이트/스위치 개수·확률에 반영.</summary>
    public enum DifficultyTier
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>Options for base level generation (no diodes/gates unless added later).</summary>
    public struct GenerationOptions
    {
        public bool IncludeSwitch;
        /// <summary>If &gt; 0, up to this many nodes can be Switch (indices chosen internally).</summary>
        public int SwitchCount;
        /// <summary>When true, reject too-linear graphs (e.g. for Normal/Hard).</summary>
        public bool RequireNormalHardVariety;
    }

    /// <summary>
    /// Generates LevelData from templates with seed-based reproducibility.
    /// Generator supports N in [4..GeneratorMaxN]; solver exact limit is separate.
    /// </summary>
    public static class LevelGenerator
    {
        /// <summary>Maximum node count for generated levels. Generator is decoupled from LevelSolver.</summary>
        public const int GeneratorMaxN = 25;
        /// <summary>Legacy alias; use GeneratorMaxN for new code.</summary>
        public const int MaxNodesAllowed = GeneratorMaxN;
        private const float MinNodeDistance = 0.5f;
        private const float JitterMaxFractionOfAvgEdge = 0.05f;
        private const int LayoutRetryCount = 25;
        private const int GateTradeoffRetryCount = 15;
        private const int BaseGraphValidateRetries = 35;
        private const int AestheticCandidateCount = 15;
        private const int MaxHubsAllowed = 1;
        private const int MaxDegreeAllowed = 5;
        private const float TooLinearFractionThreshold = 0.85f;

        /// <summary>
        /// Result of generation; includes level and template name for metadata.
        /// </summary>
        public struct GenerateResult
        {
            public LevelData level;
            public string templateName;
        }

        /// <summary>
        /// Generate a single level. Deterministic for given seed.
        /// </summary>
        public static GenerateResult GenerateWithMetadata(DifficultyTier tier, int seed, bool? includeSwitchOverride = null, int maxNodesAllowed = MaxNodesAllowed)
        {
            var rng = new System.Random(seed);

            // 1) Pick random template
            int templateIndex = rng.Next(LevelTemplates.All.Length);
            ref LevelTemplate t = ref LevelTemplates.All[templateIndex];
            string templateName = t.name;
            int n = t.nodeCount;
            if (n > maxNodesAllowed)
                n = maxNodesAllowed;

            // 2) Permute node IDs
            int[] perm = new int[n];
            for (int i = 0; i < n; i++)
                perm[i] = i;
            Shuffle(perm, rng);
            int[] invPerm = new int[n];
            for (int i = 0; i < n; i++)
                invPerm[perm[i]] = i;

            // 3) Switch: Easy default false, Medium/Hard true (or override)
            bool includeSwitch = includeSwitchOverride ?? (tier != DifficultyTier.Easy);
            int switchOutputNodeId = -1;
            if (includeSwitch && t.switchCandidates != null && t.switchCandidates.Count > 0)
            {
                int switchTemplateIndex = t.switchCandidates[rng.Next(t.switchCandidates.Count)];
                switchOutputNodeId = perm[switchTemplateIndex];
            }

            // 5) Diodes: Easy 0..1, Medium 1..2, Hard 2..3
            var diodeCandidatesPerm = PermuteEdgeList(t.diodeCandidates, perm);
            int diodeCount = tier == DifficultyTier.Easy ? rng.Next(0, 2) : tier == DifficultyTier.Medium ? rng.Next(1, 3) : rng.Next(2, 4);
            diodeCount = Math.Min(diodeCount, diodeCandidatesPerm.Count);
            var chosenDiodes = PickRandomSubset(diodeCandidatesPerm, diodeCount, rng);
            var diodeSet = new HashSet<(int a, int b)>();
            foreach (var e in chosenDiodes)
                diodeSet.Add(e);

            // 6) Gates: Easy 0; Medium 2..3; Hard 3..5. Tradeoff: after toggle, at least one opens and one closes.
            var gateCandidatesPerm = PermuteEdgeList(t.gateCandidates, perm);
            int gateCount = tier == DifficultyTier.Easy ? 0 : tier == DifficultyTier.Medium ? rng.Next(2, 4) : rng.Next(3, 6);
            gateCount = Math.Min(gateCount, gateCandidatesPerm.Count);
            List<(int a, int b)> chosenGates = null;
            bool[] initialOpen = null;
            for (int k = 0; k < GateTradeoffRetryCount; k++)
            {
                chosenGates = PickRandomSubset(gateCandidatesPerm, gateCount, rng);
                initialOpen = new bool[chosenGates.Count];
                for (int i = 0; i < chosenGates.Count; i++)
                    initialOpen[i] = tier == DifficultyTier.Medium ? rng.NextDouble() < 0.5 : rng.NextDouble() < 0.6;
                if (gateCount == 0 || !includeSwitch)
                    break;
                bool hasOpen = false, hasClosed = false;
                for (int i = 0; i < chosenGates.Count; i++)
                {
                    if (initialOpen[i]) hasOpen = true;
                    else hasClosed = true;
                }
                bool afterToggleOpen = false, afterToggleClosed = false;
                for (int i = 0; i < chosenGates.Count; i++)
                {
                    bool after = !initialOpen[i];
                    if (after) afterToggleOpen = true;
                    else afterToggleClosed = true;
                }
                if (hasOpen && hasClosed && afterToggleOpen && afterToggleClosed)
                    break;
            }
            if (chosenGates == null)
                chosenGates = new List<(int, int)>();
            if (initialOpen == null)
                initialOpen = Array.Empty<bool>();

            // 7) Build edges (template edges after permutation)
            var edgeList = new List<EdgeData>();
            int edgeId = 0;
            foreach (var (a, b) in t.edges)
            {
                int pa = perm[a];
                int pb = perm[b];
                if (pa >= n || pb >= n) continue;
                var e = new EdgeData { id = edgeId++, a = pa, b = pb };
                if (diodeSet.Contains((pa, pb)) || diodeSet.Contains((pb, pa)))
                {
                    bool atob = rng.NextDouble() < 0.5;
                    e.diode = atob ? DiodeMode.AtoB : DiodeMode.BtoA;
                }
                for (int i = 0; i < chosenGates.Count; i++)
                {
                    if ((chosenGates[i].a == pa && chosenGates[i].b == pb) || (chosenGates[i].a == pb && chosenGates[i].b == pa))
                    {
                        e.gateGroupId = 1;
                        e.initialGateOpen = initialOpen[i];
                        break;
                    }
                }
                edgeList.Add(e);
            }

            // 8) Layout stage: predefined layout + slot permutation + jitter &lt;= 5% avg edge length; reject if AestheticEvaluator fails
            Vector2[] positions = PlaceNodesWithLayout(n, rng, edgeList);

            // Build nodes (positions indexed by output node id 0..n-1)
            var nodeList = new List<NodeData>();
            for (int j = 0; j < n; j++)
            {
                var pos = positions[j];
                bool isSwitch = (j == switchOutputNodeId);
                nodeList.Add(new NodeData
                {
                    id = j,
                    pos = pos,
                    nodeType = isSwitch ? NodeType.Switch : NodeType.Bulb,
                    switchGroupId = isSwitch ? 1 : 0
                });
            }

            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.levelId = 0;
            level.nodes = nodeList.ToArray();
            level.edges = edgeList.ToArray();
            return new GenerateResult { level = level, templateName = templateName };
        }

        /// <summary>
        /// Generate a single level (convenience; template name not returned).
        /// </summary>
        public static LevelData Generate(DifficultyTier tier, int seed, bool? includeSwitchOverride = null, int maxNodesAllowed = MaxNodesAllowed)
        {
            return GenerateWithMetadata(tier, seed, includeSwitchOverride, maxNodesAllowed).level;
        }

        private const int MaxBaseTooHardRetries = 6;

        /// <summary>
        /// Generate a level with N from difficulty profile and diode tuning to hit target success rate band.
        /// Returns level and metadata; measuredRate and diodeCount for debugging. Start node in evaluation is any Bulb (uniform).
        /// </summary>
        public static LevelData GenerateWithSuccessRateTarget(DifficultyTier tier, int seed,
            out int N, out float measuredRate, out int diodeCount, out int retries)
        {
            N = 0;
            measuredRate = 0f;
            diodeCount = 0;
            retries = 0;
            DifficultyProfile.GetNRange(tier, out int nMin, out int nMax);
            var rng = new System.Random(seed);
            for (int r = 0; r < MaxBaseTooHardRetries; r++)
            {
                retries = r;
                int useSeed = seed + r * 10000;
                int n = nMin + rng.Next(Math.Max(0, nMax - nMin + 1));
                n = Mathf.Clamp(n, 4, GeneratorMaxN);
                var opts = new GenerationOptions
                {
                    IncludeSwitch = tier != DifficultyTier.Easy,
                    SwitchCount = tier == DifficultyTier.Easy ? 0 : 1,
                    RequireNormalHardVariety = tier != DifficultyTier.Easy
                };
                LevelData baseLevel = GenerateBase(n, useSeed, opts);
                if (baseLevel == null)
                    continue;
                int trialsK = DifficultyProfile.GetTrialsK(tier, n);
                float rate = MonteCarloEvaluator.EstimateSuccessRate(baseLevel, trialsK, useSeed + 1);
                DifficultyProfile.GetTargetRate(tier, out float target, out float band);
                if (rate < target - band)
                {
                    UnityEngine.Object.DestroyImmediate(baseLevel);
                    continue;
                }
                N = n;
                if (rate >= target - band && rate <= target + band)
                {
                    measuredRate = rate;
                    diodeCount = 0;
#if UNITY_EDITOR
                    UnityEngine.Debug.Log($"[LevelGenerator] difficulty={tier}, N={n}, target={target:F2}, measuredRate={rate:F3}, diodeCount=0, seed={useSeed}, retries={r}");
#endif
                    return baseLevel;
                }
                var tuneResult = DiodeTuner.TuneDiodes(baseLevel, tier, useSeed + 2, trialsK);
                UnityEngine.Object.DestroyImmediate(baseLevel);
                measuredRate = tuneResult.measuredRate;
                diodeCount = tuneResult.diodeCount;
#if UNITY_EDITOR
                UnityEngine.Debug.Log($"[LevelGenerator] difficulty={tier}, N={n}, target={target:F2}, measuredRate={tuneResult.measuredRate:F3}, diodeCount={tuneResult.diodeCount}, seed={useSeed}, retries={r}");
#endif
                if (tuneResult.level != null && tuneResult.measuredRate > 0f)
                    return tuneResult.level;
                if (tuneResult.level != null)
                    UnityEngine.Object.DestroyImmediate(tuneResult.level);
            }
            return null;
        }

        /// <summary>
        /// Generate a base level with exactly N nodes, no diodes, no gates (optional switches via opts).
        /// Node ids are 0..N-1. Generates up to AestheticCandidateCount valid candidates and returns best by aesthetic score.
        /// </summary>
        public static LevelData GenerateBase(int N, int seed, GenerationOptions opts)
        {
            N = Mathf.Clamp(N, 4, GeneratorMaxN);
            LevelData best = null;
            float bestScore = float.MinValue;
            int validCount = 0;
            for (int attempt = 0; attempt < BaseGraphValidateRetries && validCount < AestheticCandidateCount; attempt++)
            {
                int useSeed = seed + attempt * 1000;
                var rngAttempt = new System.Random(useSeed);
                LevelData level = GenerateBaseInternal(N, useSeed, opts, rngAttempt);
                if (level != null && ValidateBaseGraph(level, opts.RequireNormalHardVariety))
                {
                    validCount++;
                    float score = AestheticEvaluator.Score(level);
                    if (score > bestScore)
                    {
                        if (best != null) UnityEngine.Object.DestroyImmediate(best);
                        best = level;
                        bestScore = score;
                    }
                    else
                        UnityEngine.Object.DestroyImmediate(level);
                }
                else if (level != null)
                    UnityEngine.Object.DestroyImmediate(level);
            }
            return best;
        }

        /// <summary>
        /// Fast sanity checks: connectivity from any Bulb, no isolated nodes, hub cap, optional too-linear reject.
        /// Ignores diode/gate (base graph is undirected for connectivity).
        /// </summary>
        public static bool ValidateBaseGraph(LevelData level, bool requireNormalHardVariety = false)
        {
            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0)
                return false;
            int n = level.nodes.Length;
            var adj = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
                adj[i] = new List<int>();
            foreach (var e in level.edges)
            {
                if (e.a >= 0 && e.a < n && e.b >= 0 && e.b < n && e.a != e.b)
                {
                    if (!adj[e.a].Contains(e.b)) adj[e.a].Add(e.b);
                    if (!adj[e.b].Contains(e.a)) adj[e.b].Add(e.a);
                }
            }
            foreach (var node in level.nodes)
            {
                if (node.id < 0 || node.id >= n) return false;
                if (adj[node.id].Count < 1) return false;
            }
            int firstBulb = -1;
            for (int i = 0; i < level.nodes.Length; i++)
            {
                if (level.nodes[i].nodeType == NodeType.Bulb) { firstBulb = level.nodes[i].id; break; }
            }
            if (firstBulb < 0) return false;
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(firstBulb);
            visited.Add(firstBulb);
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                foreach (int v in adj[u])
                {
                    if (visited.Add(v)) queue.Enqueue(v);
                }
            }
            if (visited.Count != n) return false;
            int hubs = 0;
            int degreeAtMost2 = 0;
            for (int i = 0; i < n; i++)
            {
                int deg = adj[i].Count;
                if (deg > MaxDegreeAllowed) return false;
                if (deg >= 5) hubs++;
                if (deg <= 2) degreeAtMost2++;
            }
            if (hubs > MaxHubsAllowed) return false;
            if (requireNormalHardVariety && n >= 10 && (degreeAtMost2 / (float)n) > TooLinearFractionThreshold)
                return false;
            return true;
        }

        private static LevelData GenerateBaseInternal(int N, int seed, GenerationOptions opts, System.Random rng)
        {
            List<(int a, int b)> edgeList = null;
            string templateName = null;
            int switchOutputNodeId = -1;
            LevelTemplate? t = null;
            for (int i = 0; i < LevelTemplates.All.Length; i++)
            {
                if (LevelTemplates.All[i].nodeCount == N)
                {
                    t = LevelTemplates.All[i];
                    templateName = t.Value.name;
                    break;
                }
            }
            if (t.HasValue)
            {
                ref LevelTemplate tmpl = ref t.Value;
                int[] perm = new int[N];
                for (int i = 0; i < N; i++) perm[i] = i;
                Shuffle(perm, rng);
                edgeList = new List<(int a, int b)>();
                foreach (var (a, b) in tmpl.edges)
                {
                    if (a < N && b < N)
                        edgeList.Add((perm[a], perm[b]));
                }
                if (opts.IncludeSwitch && opts.SwitchCount > 0 && tmpl.switchCandidates != null && tmpl.switchCandidates.Count > 0)
                {
                    int idx = tmpl.switchCandidates[rng.Next(tmpl.switchCandidates.Count)];
                    switchOutputNodeId = perm[idx];
                }
            }
            if (edgeList == null)
            {
                edgeList = GenerateBaseGraphTopology(N, rng);
                if (opts.IncludeSwitch && opts.SwitchCount > 0 && N >= 2)
                    switchOutputNodeId = rng.Next(1, N);
            }
            if (edgeList == null || edgeList.Count == 0) return null;
            int edgeId = 0;
            var outEdges = new List<EdgeData>();
            foreach (var (a, b) in edgeList)
            {
                outEdges.Add(new EdgeData { id = edgeId++, a = a, b = b });
            }
            Vector2[] positions = PlaceNodesWithLayout(N, rng, outEdges);
            if (positions == null || positions.Length != N)
                positions = PlaceNodesOnCircleFallback(N, rng);
            var nodeList = new List<NodeData>();
            for (int j = 0; j < N; j++)
            {
                nodeList.Add(new NodeData
                {
                    id = j,
                    pos = positions[j],
                    nodeType = (j == switchOutputNodeId) ? NodeType.Switch : NodeType.Bulb,
                    switchGroupId = (j == switchOutputNodeId) ? 1 : 0
                });
            }
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelId = 0;
            level.nodes = nodeList.ToArray();
            level.edges = outEdges.ToArray();
            return level;
        }

        private static List<(int a, int b)> GenerateBaseGraphTopology(int N, System.Random rng)
        {
            var edgeSet = new HashSet<(int a, int b)>();
            for (int i = 0; i < N - 1; i++)
                edgeSet.Add((i, i + 1));
            int[] degree = new int[N];
            for (int i = 0; i < N - 1; i++) { degree[i]++; degree[i + 1]++; }
            int targetExtra = Math.Max(0, (N * 3 / 2) - (N - 1));
            if (N <= 10) targetExtra = Math.Min(targetExtra, N);
            int added = 0;
            int maxAttempts = N * N * 2;
            for (int a = 0; a < maxAttempts && added < targetExtra; a++)
            {
                int i = rng.Next(0, N);
                int j = rng.Next(0, N);
                if (Math.Abs(i - j) < 2) continue;
                int u = Math.Min(i, j);
                int v = Math.Max(i, j);
                if (edgeSet.Contains((u, v))) continue;
                if (degree[u] >= MaxDegreeAllowed || degree[v] >= MaxDegreeAllowed) continue;
                int hubCount = 0;
                for (int k = 0; k < N; k++)
                    if (degree[k] >= 5) hubCount++;
                if ((degree[u] + 1 >= 5 || degree[v] + 1 >= 5) && hubCount >= MaxHubsAllowed) continue;
                edgeSet.Add((u, v));
                degree[u]++; degree[v]++;
                added++;
            }
            var list = new List<(int a, int b)>();
            foreach (var e in edgeSet) list.Add(e);
            return list;
        }

        private static void Shuffle(int[] a, System.Random rng)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (a[i], a[j]) = (a[j], a[i]);
            }
        }

        private static List<(int a, int b)> PermuteEdgeList(List<(int a, int b)> edges, int[] perm)
        {
            var list = new List<(int, int)>();
            foreach (var (a, b) in edges)
            {
                if (a < perm.Length && b < perm.Length)
                    list.Add((perm[a], perm[b]));
            }
            return list;
        }

        private static List<(int a, int b)> PickRandomSubset(List<(int a, int b)> source, int count, System.Random rng)
        {
            if (count >= source.Count)
                return new List<(int, int)>(source);
            var indices = new List<int>();
            for (int i = 0; i < source.Count; i++)
                indices.Add(i);
            for (int i = 0; i < count; i++)
            {
                int j = rng.Next(i, indices.Count);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            var result = new List<(int, int)>();
            for (int i = 0; i < count; i++)
                result.Add(source[indices[i]]);
            return result;
        }

        /// <summary>
        /// Place nodes using a LayoutTemplate: pick layout for nodeCount, map node IDs to slots via permutation,
        /// apply jitter &lt;= 5% of average edge length, enforce min distance, reject if AestheticEvaluator fails.
        /// </summary>
        private static Vector2[] PlaceNodesWithLayout(int n, System.Random rng, List<EdgeData> edgeList)
        {
            var layouts = LayoutTemplates.GetLayoutsForNodeCount(n);
            if (layouts == null || layouts.Count == 0)
                return PlaceNodesOnCircleFallback(n, rng);

            var layout = layouts[rng.Next(layouts.Count)];
            if (layout.slots == null || layout.slots.Length < n)
                return PlaceNodesOnCircleFallback(n, rng);

            var positions = new Vector2[n];
            var slotPerm = new int[n];
            for (int i = 0; i < n; i++) slotPerm[i] = i;

            for (int tryCount = 0; tryCount < LayoutRetryCount; tryCount++)
            {
                Shuffle(slotPerm, rng);
                for (int j = 0; j < n; j++)
                    positions[j] = layout.slots[slotPerm[j]];

                float avgLen = edgeList != null && edgeList.Count > 0
                    ? AestheticEvaluator.AverageEdgeLength(edgeList, positions)
                    : 1.5f;
                if (avgLen < 0.1f) avgLen = 1.5f;
                float jitterMax = JitterMaxFractionOfAvgEdge * avgLen;

                for (int j = 0; j < n; j++)
                {
                    float dx = (float)(rng.NextDouble() * 2 - 1) * jitterMax;
                    float dy = (float)(rng.NextDouble() * 2 - 1) * jitterMax;
                    positions[j].x += dx;
                    positions[j].y += dy;
                }

                float minDist = AestheticEvaluator.MinNodeDistance(positions, n);
                if (minDist < MinNodeDistance)
                    continue;

                if (edgeList != null && AestheticEvaluator.Accept(edgeList, positions, n))
                    return positions;
            }

            return positions;
        }

        private static Vector2[] PlaceNodesOnCircleFallback(int n, System.Random rng)
        {
            const float radius = 3.5f;
            const float jitter = 0.1f;
            var positions = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float angle = (float)(2 * Math.PI * i / n) + (float)(rng.NextDouble() * 2 - 1) * jitter;
                positions[i] = new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
            }
            return positions;
        }

        /// <summary>
        /// Default acceptance rules for generated levels. Used by LevelBakeTool.
        /// </summary>
        public static bool PassesFilter(DifficultyTier tier, SolverResult result)
        {
            if (!result.solvable || result.solutionCount <= 0)
                return false;
            switch (tier)
            {
                case DifficultyTier.Easy:
                    return result.solutionCount >= 1 && result.solutionCount <= 80
                        && result.earlyBranching >= 1.4f
                        && result.deadEndDepthAvg >= 2f && result.deadEndDepthAvg <= 6f;
                case DifficultyTier.Medium:
                    return result.solutionCount >= 1 && result.solutionCount <= 120
                        && result.earlyBranching >= 1.7f
                        && result.deadEndDepthAvg >= 3f && result.deadEndDepthAvg <= 7f;
                case DifficultyTier.Hard:
                    return result.solutionCount >= 1 && result.solutionCount <= 200
                        && result.earlyBranching >= 2.0f
                        && result.deadEndDepthAvg >= 4f && result.deadEndDepthAvg <= 8f;
                default:
                    return false;
            }
        }
    }
}
