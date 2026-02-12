using System;
using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Solver;

namespace CircuitOneStroke.Generation
{
    /// <summary>
    /// Tunes diode placement to move success rate into tier band while keeping corridor pressure and diode usage healthy.
    /// </summary>
    public static class DiodeTuner
    {
        public const int MaxDiodeSteps = 12;
        public const int MaxTuneStepsBudget = 80;
        public const int MaxDiodesPerNodeNormal = 2;
        public const int MaxDiodesPerNodeHard = 4;
        public const float MinDiodeUsageRateMedium = 0.25f;
        public const float MinDiodeUsageRateHard = 0.40f;
        private const float CorridorWorsenTolerance = 0.06f;

        public struct TuneResult
        {
            public LevelData level;
            public float measuredRate;
            public int diodeCount;
            public bool inBand;
        }

        public static TuneResult TuneDiodes(LevelData baseLevel, DifficultyTier tier, int seed, int trialsK)
        {
            var result = new TuneResult { level = baseLevel, measuredRate = 0f, diodeCount = 0, inBand = false };
            if (baseLevel?.nodes == null || baseLevel.edges == null)
                return result;

            DifficultyProfile.GetTargetRate(tier, out float target, out float band);
            var baseStats = MonteCarloEvaluator.EvaluateDetailed(baseLevel, trialsK, seed);
            result.measuredRate = baseStats.successRate;
            if (baseStats.successRate >= target - band && baseStats.successRate <= target + band)
            {
                result.inBand = CountDiodes(baseLevel) == 0 || baseStats.diodeUsageRate >= MinUsageRate(tier);
                return result;
            }
            if (baseStats.successRate < target - band)
                return result;

            int n = baseLevel.nodes.Length;
            int maxPerNode = tier == DifficultyTier.Hard ? MaxDiodesPerNodeHard : MaxDiodesPerNodeNormal;
            var diodeCountAtNode = new int[n];
            var touchedNodes = new HashSet<int>();
            float baselineCorridorLoad = CorridorLoad(baseStats);

            LevelData current = CloneLevel(baseLevel);
            int steps = 0;
            while (steps < MaxDiodeSteps && steps < MaxTuneStepsBudget)
            {
                var currentStats = MonteCarloEvaluator.EvaluateDetailed(current, trialsK, seed + steps * 1000);
                float rate = currentStats.successRate;
                if (rate >= target - band && rate <= target + band)
                {
                    result.level = current;
                    result.measuredRate = rate;
                    result.diodeCount = CountDiodes(current);
                    bool usageOk = result.diodeCount == 0 || currentStats.diodeUsageRate >= MinUsageRate(tier);
                    bool corridorOk = CorridorLoad(currentStats) <= baselineCorridorLoad + CorridorWorsenTolerance;
                    result.inBand = usageOk && corridorOk;
                    return result;
                }
                if (rate < target - band)
                    break;

                MonteCarloEvaluator.RunTrialsWithSuccessEdgeCounts(current, trialsK, seed + steps * 1000 + 1, out var edgeCountsSuccess);
                var candidates = new List<(int score, int edgeIndex)>();
                for (int ei = 0; ei < current.edges.Length; ei++)
                {
                    var e = current.edges[ei];
                    if (e.diode != DiodeMode.None) continue;
                    if (diodeCountAtNode[e.a] >= maxPerNode || diodeCountAtNode[e.b] >= maxPerNode) continue;

                    int count = 0;
                    if (edgeCountsSuccess.TryGetValue((e.a, e.b), out int c1)) count += c1;
                    if (edgeCountsSuccess.TryGetValue((e.b, e.a), out int c2)) count += c2;
                    int penalty = (touchedNodes.Contains(e.a) ? 10000 : 0) + (touchedNodes.Contains(e.b) ? 10000 : 0);
                    candidates.Add((Math.Max(0, count - penalty), ei));
                }
                if (candidates.Count == 0) break;
                candidates.Sort((a, b) => b.score.CompareTo(a.score));

                bool placed = false;
                for (int i = 0; i < candidates.Count && !placed; i++)
                {
                    int ei = candidates[i].edgeIndex;
                    if (TryPlaceDiode(current, ei, DiodeMode.BtoA, trialsK, seed + steps * 1000 + 2 + i * 2, tier, baselineCorridorLoad, target - band, out float rateB))
                    {
                        var edge = current.edges[ei];
                        edge.diode = DiodeMode.BtoA;
                        diodeCountAtNode[edge.a]++;
                        diodeCountAtNode[edge.b]++;
                        touchedNodes.Add(edge.a);
                        touchedNodes.Add(edge.b);
                        placed = true;
                        steps++;
                        continue;
                    }
                    if (TryPlaceDiode(current, ei, DiodeMode.AtoB, trialsK, seed + steps * 1000 + 3 + i * 2, tier, baselineCorridorLoad, target - band, out float rateA))
                    {
                        var edge = current.edges[ei];
                        edge.diode = DiodeMode.AtoB;
                        diodeCountAtNode[edge.a]++;
                        diodeCountAtNode[edge.b]++;
                        touchedNodes.Add(edge.a);
                        touchedNodes.Add(edge.b);
                        placed = true;
                        steps++;
                    }
                }

                if (!placed) break;
            }

            var finalStats = MonteCarloEvaluator.EvaluateDetailed(current, trialsK, seed + 9999);
            result.level = current;
            result.measuredRate = finalStats.successRate;
            result.diodeCount = CountDiodes(current);
            bool usageFinalOk = result.diodeCount == 0 || finalStats.diodeUsageRate >= MinUsageRate(tier);
            bool corridorFinalOk = CorridorLoad(finalStats) <= baselineCorridorLoad + CorridorWorsenTolerance;
            result.inBand = DifficultyProfile.IsInBand(result.measuredRate, tier) && usageFinalOk && corridorFinalOk;
            return result;
        }

        private static bool TryPlaceDiode(
            LevelData level,
            int edgeIndex,
            DiodeMode mode,
            int trialsK,
            int seed,
            DifficultyTier tier,
            float baselineCorridorLoad,
            float minRateTarget,
            out float measuredRate)
        {
            measuredRate = 0f;
            var edge = level.edges[edgeIndex];
            var prev = edge.diode;
            edge.diode = mode;
            var stats = MonteCarloEvaluator.EvaluateDetailed(level, trialsK, seed);
            edge.diode = prev;

            measuredRate = stats.successRate;
            if (measuredRate < minRateTarget) return false;
            if (stats.diodeUsageRate < MinUsageRate(tier)) return false;
            if (CorridorLoad(stats) > baselineCorridorLoad + CorridorWorsenTolerance) return false;
            return true;
        }

        private static float CorridorLoad(MonteCarloEvaluator.EvaluationStats stats)
        {
            return stats.forcedRatio + stats.corridorVisualRatio + stats.topEdgesShare;
        }

        private static float MinUsageRate(DifficultyTier tier)
        {
            return tier == DifficultyTier.Hard ? MinDiodeUsageRateHard : MinDiodeUsageRateMedium;
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
