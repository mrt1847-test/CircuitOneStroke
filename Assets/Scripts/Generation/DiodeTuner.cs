using System;
using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Solver;

namespace CircuitOneStroke.Generation
{
    /// <summary>
    /// Tunes diode placement on a base level so Monte Carlo success rate falls within the difficulty band.
    /// Uses edge traversal frequency from Monte Carlo to pick candidates; adds diodes iteratively if too easy.
    /// </summary>
    public static class DiodeTuner
    {
        public const int MaxDiodeSteps = 12;
        public const int MaxDiodesPerNodeNormal = 2;
        public const int MaxDiodesPerNodeHard = 4;

        public struct TuneResult
        {
            public LevelData level;
            public float measuredRate;
            public int diodeCount;
            public bool inBand;
        }

        /// <summary>
        /// If base level is too easy, add diodes to bring success rate into band. If too hard, returns null (caller should regenerate base).
        /// </summary>
        public static TuneResult TuneDiodes(LevelData baseLevel, DifficultyTier tier, int seed, int trialsK)
        {
            var result = new TuneResult { level = baseLevel, measuredRate = 0f, diodeCount = 0, inBand = false };
            if (baseLevel?.nodes == null || baseLevel.edges == null)
                return result;

            float rate = MonteCarloEvaluator.EstimateSuccessRate(baseLevel, trialsK, seed);
            result.measuredRate = rate;
            DifficultyProfile.GetTargetRate(tier, out float target, out float band);
            if (rate >= target - band && rate <= target + band)
            {
                result.inBand = true;
                return result;
            }
            if (rate < target - band)
                return result;

            int n = baseLevel.nodes.Length;
            int maxPerNode = tier == DifficultyTier.Hard ? MaxDiodesPerNodeHard : MaxDiodesPerNodeNormal;
            var diodeCountAtNode = new int[n];
            var touchedNodes = new HashSet<int>();
            LevelData current = CloneLevel(baseLevel);
            int steps = 0;
            while (steps < MaxDiodeSteps)
            {
                rate = MonteCarloEvaluator.EstimateSuccessRate(current, trialsK, seed + steps * 1000);
                if (rate >= target - band && rate <= target + band)
                {
                    result.level = current;
                    result.measuredRate = rate;
                    result.diodeCount = CountDiodes(current);
                    result.inBand = true;
                    return result;
                }
                if (rate < target - band)
                    break;

                MonteCarloEvaluator.RunTrialsWithEdgeCounts(current, trialsK, seed + steps * 1000 + 1, out var edgeCounts);
                var candidates = new List<(int count, int edgeIndex, int from, int to)>();
                for (int ei = 0; ei < current.edges.Length; ei++)
                {
                    var e = current.edges[ei];
                    if (e.diode != DiodeMode.None) continue;
                    if (diodeCountAtNode[e.a] >= maxPerNode || diodeCountAtNode[e.b] >= maxPerNode) continue;
                    int count = 0;
                    if (edgeCounts.TryGetValue((e.a, e.b), out int c1)) count += c1;
                    if (edgeCounts.TryGetValue((e.b, e.a), out int c2)) count += c2;
                    int penalty = (touchedNodes.Contains(e.a) ? 10000 : 0) + (touchedNodes.Contains(e.b) ? 10000 : 0);
                    candidates.Add((Math.Max(0, count - penalty), ei, e.a, e.b));
                }
                if (candidates.Count == 0) break;
                candidates.Sort((a, b) => b.count.CompareTo(a.count));
                bool placed = false;
                for (int i = 0; i < candidates.Count && !placed; i++)
                {
                    int ei = candidates[i].edgeIndex;
                    var edge = current.edges[ei];
                    float rateBtoA = ApplyDiodeAndMeasure(current, ei, true, trialsK, seed + steps * 1000 + 2 + i * 2);
                    if (rateBtoA > 0f && rateBtoA >= target - band)
                    {
                        edge.diode = DiodeMode.BtoA;
                        diodeCountAtNode[edge.a]++;
                        diodeCountAtNode[edge.b]++;
                        touchedNodes.Add(edge.a);
                        touchedNodes.Add(edge.b);
                        placed = true;
                        steps++;
                        break;
                    }
                    float rateAtoB = ApplyDiodeAndMeasure(current, ei, false, trialsK, seed + steps * 1000 + 3 + i * 2);
                    if (rateAtoB > 0f && rateAtoB >= target - band)
                    {
                        edge.diode = DiodeMode.AtoB;
                        diodeCountAtNode[edge.a]++;
                        diodeCountAtNode[edge.b]++;
                        touchedNodes.Add(edge.a);
                        touchedNodes.Add(edge.b);
                        placed = true;
                        steps++;
                        break;
                    }
                }
                if (!placed) break;
            }

            result.level = current;
            result.measuredRate = MonteCarloEvaluator.EstimateSuccessRate(current, trialsK, seed + 9999);
            result.diodeCount = CountDiodes(current);
            result.inBand = DifficultyProfile.IsInBand(result.measuredRate, tier);
            return result;
        }

        private static int FindEdgeIndex(LevelData level, int a, int b)
        {
            for (int i = 0; i < level.edges.Length; i++)
            {
                var e = level.edges[i];
                if ((e.a == a && e.b == b) || (e.a == b && e.b == a))
                    return i;
            }
            return -1;
        }

        private static float ApplyDiodeAndMeasure(LevelData level, int edgeIndex, bool blockAtoB, int trialsK, int seed)
        {
            var e = level.edges[edgeIndex];
            var prev = e.diode;
            e.diode = blockAtoB ? DiodeMode.BtoA : DiodeMode.AtoB;
            float r = MonteCarloEvaluator.EstimateSuccessRate(level, trialsK, seed);
            e.diode = prev;
            return r;
        }

        private static int CountDiodes(LevelData level)
        {
            int c = 0;
            if (level.edges == null) return 0;
            foreach (var e in level.edges)
                if (e.diode != DiodeMode.None) c++;
            return c;
        }

        private static LevelData CloneLevel(LevelData source)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelId = source.levelId;
            level.nodes = new NodeData[source.nodes.Length];
            for (int i = 0; i < source.nodes.Length; i++)
            {
                var n = source.nodes[i];
                level.nodes[i] = new NodeData { id = n.id, pos = n.pos, nodeType = n.nodeType, switchGroupId = n.switchGroupId };
            }
            level.edges = new EdgeData[source.edges.Length];
            for (int i = 0; i < source.edges.Length; i++)
            {
                var e = source.edges[i];
                level.edges[i] = new EdgeData { id = e.id, a = e.a, b = e.b, diode = e.diode, gateGroupId = e.gateGroupId, initialGateOpen = e.initialGateOpen };
            }
            return level;
        }
    }
}
