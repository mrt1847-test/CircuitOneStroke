using System;
using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Solver;

namespace CircuitOneStroke.Generation
{
    public enum DifficultyTier
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Generates LevelData from templates with seed-based reproducibility.
    /// </summary>
    public static class LevelGenerator
    {
        /// <summary>Solver와 동일 제한 공유. 확장 시 LevelSolver.MaxNodesSupported와 함께 변경.</summary>
        public const int MaxNodesAllowed = LevelSolver.MaxNodesSupported;
        private const float MinNodeDistance = 0.5f;
        private const float JitterMaxFractionOfAvgEdge = 0.05f;
        private const int LayoutRetryCount = 25;
        private const int GateTradeoffRetryCount = 15;

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
