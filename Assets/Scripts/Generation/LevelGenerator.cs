using System;
using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Solver;

namespace CircuitOneStroke.Generation
{
    /// <summary>?쒖씠??援ш컙. ?ㅼ씠?ㅻ뱶/寃뚯씠???ㅼ쐞移?媛쒖닔쨌?뺣쪧??諛섏쁺.</summary>
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
        /// <summary>Bulb nodes to convert into blocked nodes, as percentage of total node count [0..1].</summary>
        public float ForbiddenNodeRatio;
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
        /// <summary>?섎뱶 由ъ젥: ?몃뱶 理쒖냼 媛꾧꺽 (?붾뱶).</summary>
        private const float MinNodeSpacing = 0.62f;
        /// <summary>?섎뱶 由ъ젥: ?ｌ?-鍮꼌ndpoint ?몃뱶 理쒖냼 嫄곕━.</summary>
        private const float EdgeClearanceSmallN = 0.34f;
        private const float EdgeClearanceMediumN = 0.31f;
        private const float EdgeClearanceLargeN = 0.28f;
        private const float EdgeClearanceXLN = 0.24f;
        /// <summary>?섎뱶 由ъ젥: ?몃뱶蹂??ｌ? 理쒖냼 媛곷룄(??.</summary>
        private const float AngleThresholdSmallN = 18f;
        private const float AngleThresholdMediumN = 16f;
        private const float AngleThresholdLargeN = 15f;
        private const float AngleThresholdXLN = 13f;
        private const float LayoutPlacementMinClearance = 0.34f;
        private const float LayoutPlacementMinAngleDeg = 16f;
        /// <summary>?곸쐞 M媛?以??쒕뜡 ?좏깮.</summary>
        private const int TopCandidatesForRandomPick = 5;
        /// <summary>forcedRatio &gt; ??媛믪씠硫?肄붾━?꾩뼱濡?媛꾩＜?섏뿬 ?ъ깮??</summary>
        private const float ForcedRatioRejectHard = 0.58f;
        private const float ForcedRatioRejectMedium = 0.68f;
        /// <summary>maxDegree2ChainLen &gt; N*??鍮꾩쑉?대㈃ ?ъ깮??</summary>
        private const float MaxChainLenRatioRejectHard = 0.35f;
        private const float MaxChainLenRatioRejectMedium = 0.45f;
        private const float JitterMaxFractionOfAvgEdge = 0.025f;
        private const int LayoutRetryCount = 25;
        private const int GateTradeoffRetryCount = 15;
        private const int BaseGraphValidateRetries = 35;
        private const int AestheticCandidateCount = 15;
        private const float CorridorVisualRatioRejectHard = 0.62f;
        private const float CorridorVisualRatioRejectMedium = 0.72f;
        private const float TopEdgesShareRejectHard = 0.56f;
        private const float TopEdgesShareRejectMedium = 0.66f;
        private const int CorridorScoreTrialsMin = 12;
        private const int CorridorScoreTrialsMax = 36;
        private const int MinPerimeterBreaksForRingLike = 3;
        private const float RingLikeRadiusCvThreshold = 0.22f;
        private const int ForbiddenNodePlacementRetries = 12;
        private const int MinBulbsAfterForbidden = 3;
        private static string _lastGenerateBasePrimaryRejectReason = "none";
        private static string _lastGenerateBaseRejectSummary = "none";

        public static string LastGenerateBasePrimaryRejectReason => _lastGenerateBasePrimaryRejectReason;
        public static string LastGenerateBaseRejectSummary => _lastGenerateBaseRejectSummary;

        /// <summary>true硫??덉씠?꾩썐/?쒗뵆由??꾨낫 ?좏깮 愿???붾쾭洹?濡쒓렇瑜?Console??異쒕젰.</summary>
#if UNITY_EDITOR
        public static bool EnableLayoutDebugLog = true;
#else
        public static bool EnableLayoutDebugLog = false;
#endif

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
        public static GenerateResult GenerateWithMetadata(DifficultyTier tier, int seed, bool? includeSwitchOverride = null, int maxNodesAllowed = MaxNodesAllowed, float forbiddenNodeRatio = 0f)
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
            Vector2[] positions = PlaceNodesWithLayout(n, rng, edgeList, seed, out string layoutName, out bool layoutFromTemplate);
            if (positions == null || positions.Length != n)
                positions = PlaceNodesOnCircleFallback(n, rng);
            if (string.IsNullOrEmpty(templateName) && !string.IsNullOrEmpty(layoutName))
                templateName = layoutName;

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
            if (forbiddenNodeRatio > 0f)
                TryApplyForbiddenNodes(level, forbiddenNodeRatio, seed ^ 0x2a2a2a2a, out _);
            return new GenerateResult { level = level, templateName = templateName };
        }

        /// <summary>
        /// Generate a single level (convenience; template name not returned).
        /// </summary>
        public static LevelData Generate(DifficultyTier tier, int seed, bool? includeSwitchOverride = null, int maxNodesAllowed = MaxNodesAllowed, float forbiddenNodeRatio = 0f)
        {
            return GenerateWithMetadata(tier, seed, includeSwitchOverride, maxNodesAllowed, forbiddenNodeRatio).level;
        }

        private const int MaxBaseTooHardRetries = 6;
        private const int MaxBaseAttempts = 200;
        private const int MaxTotalMs = 5000;

#if UNITY_EDITOR
        private static int _genCallCount = 0;
#endif

        /// <summary>
        /// Generate a level with N from difficulty profile and diode tuning to hit target success rate band.
        /// Returns level and metadata; measuredRate and diodeCount for debugging. Start node in evaluation is any Bulb (uniform).
        /// </summary>
        public static LevelData GenerateWithSuccessRateTarget(DifficultyTier tier, int seed,
            out int N, out float measuredRate, out int diodeCount, out int retries, float forbiddenNodeRatio = 0f)
        {
            N = 0;
            measuredRate = 0f;
            diodeCount = 0;
            retries = 0;
#if UNITY_EDITOR
            _genCallCount++;
            UnityEngine.Debug.Log($"[GEN] call #{_genCallCount} frame={UnityEngine.Time.frameCount}");
#endif
            var sw = System.Diagnostics.Stopwatch.StartNew();
            DifficultyProfile.GetNRange(tier, out int nMin, out int nMax);
            var rng = new System.Random(seed);
            for (int r = 0; r < MaxBaseTooHardRetries; r++)
            {
                if (sw.ElapsedMilliseconds > MaxTotalMs)
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.LogWarning($"[LevelGenerator] Aborted: total time budget exceeded ({MaxTotalMs}ms). Elapsed={sw.ElapsedMilliseconds}ms.");
#endif
                    return null;
                }

                retries = r;
                int useSeed = seed + r * 10000;
                int n = nMin + rng.Next(Math.Max(0, nMax - nMin + 1));
                n = Mathf.Clamp(n, 4, GeneratorMaxN);
                var opts = new GenerationOptions
                {
                    IncludeSwitch = tier != DifficultyTier.Easy,
                    SwitchCount = tier == DifficultyTier.Easy ? 0 : 1,
                    RequireNormalHardVariety = tier != DifficultyTier.Easy,
                    ForbiddenNodeRatio = forbiddenNodeRatio
                };
                LevelData baseLevel = GenerateBase(n, useSeed, opts);
                if (baseLevel == null)
                    continue;
                int trialsK = DifficultyProfile.GetTrialsK(tier, n);
                var mc = MonteCarloEvaluator.EvaluateDetailed(baseLevel, trialsK, useSeed + 1);
                float rate = mc.successRate;
                float correctedRate = tier == DifficultyTier.Hard ? Mathf.Max(rate, mc.p80StartSuccessRate) : rate;
                var diag = LayoutDiagnostics.ComputeLayoutMetrics(baseLevel);
                diag.forcedRatio = mc.forcedRatio;
#if UNITY_EDITOR
                if (EnableLayoutDebugLog)
                    UnityEngine.Debug.Log($"[Diagnostics] N={n} tier={tier} rate={rate:F3} correctedRate={correctedRate:F3} avgStart={mc.avgStartSuccessRate:F3} bestStart={mc.bestStartSuccessRate:F3} p80Start={mc.p80StartSuccessRate:F3} branchRatio={diag.branchRatio:F2} maxDegree2ChainLen={diag.maxDegree2ChainLen} forcedRatio={mc.forcedRatio:F2} visualRatio={mc.corridorVisualRatio:F2} topEdgesShare={mc.topEdgesShare:F2} diodeUsage={mc.diodeUsageRate:F2} minNodeDist={diag.minNodeDist:F3} minEdgeNodeClearance={diag.minEdgeNodeClearance:F3} minAngleSeparationDeg={diag.minAngleSeparationDeg:F1}");
#endif

                if (tier == DifficultyTier.Hard)
                {
                    int chainLimitHard = Mathf.Max(1, (int)Math.Ceiling(n * MaxChainLenRatioRejectHard));
                    int triggered = 0;
                    if (mc.forcedRatio >= ForcedRatioRejectHard) triggered++;
                    if (diag.maxDegree2ChainLen > chainLimitHard) triggered++;
                    if (mc.corridorVisualRatio >= CorridorVisualRatioRejectHard) triggered++;
                    if (mc.topEdgesShare >= TopEdgesShareRejectHard) triggered++;
                    if (triggered >= 2)
                    {
#if UNITY_EDITOR
                        if (EnableLayoutDebugLog) UnityEngine.Debug.Log($"[LevelGenerator] Reject corridor(Hard): triggered={triggered} forced={mc.forcedRatio:F2} chain={diag.maxDegree2ChainLen} visual={mc.corridorVisualRatio:F2} topShare={mc.topEdgesShare:F2}");
#endif
                        UnityEngine.Object.DestroyImmediate(baseLevel);
                        continue;
                    }
                }
                else if (tier == DifficultyTier.Medium)
                {
                    int chainLimitMedium = Mathf.Max(1, (int)Math.Ceiling(n * MaxChainLenRatioRejectMedium));
                    if (mc.forcedRatio >= ForcedRatioRejectMedium || diag.maxDegree2ChainLen > chainLimitMedium)
                    {
#if UNITY_EDITOR
                        if (EnableLayoutDebugLog) UnityEngine.Debug.Log($"[LevelGenerator] Reject corridor(Medium): forced={mc.forcedRatio:F2}, chain={diag.maxDegree2ChainLen}");
#endif
                        UnityEngine.Object.DestroyImmediate(baseLevel);
                        continue;
                    }
                }
                DifficultyProfile.GetTargetRate(tier, out float target, out float band);
                if (correctedRate < target - band)
                {
                    UnityEngine.Object.DestroyImmediate(baseLevel);
                    continue;
                }
                N = n;
                if (correctedRate >= target - band && correctedRate <= target + band)
                {
                    measuredRate = correctedRate;
                    diodeCount = 0;
#if UNITY_EDITOR
                    UnityEngine.Debug.Log($"[LevelGenerator] difficulty={tier}, N={n}, target={target:F2}, measuredRate={correctedRate:F3}, diodeCount=0, seed={useSeed}, retries={r}");
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
        /// Collects up to AestheticCandidateCount valid candidates, then picks randomly from top-scoring TopCandidatesForRandomPick for diversity.
        /// </summary>
        public static LevelData GenerateBase(int N, int seed, GenerationOptions opts)
        {
            N = Mathf.Clamp(N, 4, GeneratorMaxN);
            var candidates = new List<(LevelData level, float score)>();
            var rejectCounts = new Dictionary<string, int>();
            int maxAttempts = Math.Min(MaxBaseAttempts, BaseGraphValidateRetries);
            var rng = new System.Random(seed);
            for (int attempt = 0; attempt < maxAttempts && candidates.Count < AestheticCandidateCount; attempt++)
            {
                int useSeed = seed + attempt * 1000;
                var rngAttempt = new System.Random(useSeed);
                LevelData level = GenerateBaseInternal(N, useSeed, opts, rngAttempt, out string layoutName, out bool templateApplied);
                if (level == null)
                {
                    CountRejectReason(rejectCounts, "generate_base_internal_null");
                    continue;
                }
                string rejectReason = null;
                if (ValidateBaseGraph(level, opts.RequireNormalHardVariety, out rejectReason))
                {
                    if (opts.RequireNormalHardVariety && IsRingLikeAndPerimeterTooContinuous(level, MinPerimeterBreaksForRingLike, out int perimeterBreaks))
                    {
                        CountRejectReason(rejectCounts, "ring_like_perimeter_breaks");
                        if (EnableLayoutDebugLog && attempt < 8)
                            UnityEngine.Debug.Log($"[LevelGenerator] Reject base candidate N={N} seed={useSeed}: ring_like_perimeter_breaks={perimeterBreaks}<{MinPerimeterBreaksForRingLike}");
                        UnityEngine.Object.DestroyImmediate(level);
                        continue;
                    }

                    if (opts.RequireNormalHardVariety && string.Equals(layoutName, "CircleFallback", StringComparison.Ordinal))
                    {
                        CountRejectReason(rejectCounts, "circle_fallback_forbidden");
                        if (EnableLayoutDebugLog && attempt < 8)
                            UnityEngine.Debug.Log($"[LevelGenerator] Reject base candidate N={N} seed={useSeed}: circle_fallback_forbidden");
                        UnityEngine.Object.DestroyImmediate(level);
                        continue;
                    }

                    if (opts.ForbiddenNodeRatio > 0f)
                    {
                        if (!TryApplyForbiddenNodes(level, opts.ForbiddenNodeRatio, useSeed + 911, out string forbiddenRejectReason))
                        {
                            CountRejectReason(rejectCounts, forbiddenRejectReason);
                            if (EnableLayoutDebugLog && attempt < 8)
                                UnityEngine.Debug.Log($"[LevelGenerator] Reject base candidate N={N} seed={useSeed}: {forbiddenRejectReason}");
                            UnityEngine.Object.DestroyImmediate(level);
                            continue;
                        }
                    }

                    float score = AestheticEvaluator.Score(level);
                    int corridorTrials = Mathf.Clamp(N + 8, CorridorScoreTrialsMin, CorridorScoreTrialsMax);
                    var corridor = MonteCarloEvaluator.EvaluateDetailed(level, corridorTrials, useSeed + 73);
                    var layoutMetrics = LayoutDiagnostics.ComputeLayoutMetrics(level);

                    if (opts.RequireNormalHardVariety)
                    {
                        int chainLimit = Mathf.Max(1, (int)Math.Ceiling(N * MaxChainLenRatioRejectMedium));
                        bool rejectByForced = corridor.forcedRatio >= ForcedRatioRejectMedium;
                        bool rejectByChain = layoutMetrics.maxDegree2ChainLen > chainLimit;
                        bool rejectByVisual = corridor.corridorVisualRatio >= CorridorVisualRatioRejectMedium;
                        bool rejectByTopShare = corridor.topEdgesShare >= TopEdgesShareRejectMedium;
                        int triggered = 0;
                        if (rejectByForced) triggered++;
                        if (rejectByChain) triggered++;
                        if (rejectByVisual) triggered++;
                        if (rejectByTopShare) triggered++;
                        if (triggered >= 2)
                        {
                            CountRejectReason(rejectCounts, "corridor_proxy_reject");
                            if (EnableLayoutDebugLog && attempt < 8)
                                UnityEngine.Debug.Log($"[LevelGenerator] Reject base candidate N={N} seed={useSeed}: corridor triggered={triggered} forced={corridor.forcedRatio:F2} chain={layoutMetrics.maxDegree2ChainLen} visual={corridor.corridorVisualRatio:F2} topShare={corridor.topEdgesShare:F2}");
                            UnityEngine.Object.DestroyImmediate(level);
                            continue;
                        }
                    }

                    score = AestheticEvaluator.ApplyCorridorProxyPenalty(
                        score,
                        N,
                        corridor.forcedRatio,
                        layoutMetrics.maxDegree2ChainLen,
                        corridor.corridorVisualRatio,
                        corridor.topEdgesShare);
                    if (EnableLayoutDebugLog && candidates.Count < 3)
                    {
                        float minD = AestheticEvaluator.MinNodeDistance(LevelPositions(level), level.nodes.Length);
                        int cross = AestheticEvaluator.CountCrossings(level.edges, LevelPositions(level), level.nodes.Length);
                        UnityEngine.Debug.Log($"[Layout] attempt={attempt} N={N} seed={useSeed} layout={layoutName} templateApplied={templateApplied} validCount={candidates.Count + 1} score={score:F2} minDist={minD:F3} crossings={cross} forced={corridor.forcedRatio:F2} visual={corridor.corridorVisualRatio:F2} topShare={corridor.topEdgesShare:F2}");
                    }
                    candidates.Add((level, score));
                }
                else
                {
                    CountRejectReason(rejectCounts, NormalizeRejectReason(rejectReason));
                    if (EnableLayoutDebugLog && attempt < 8)
                        UnityEngine.Debug.Log($"[LevelGenerator] Reject base candidate N={N} seed={useSeed}: {rejectReason}");
                    UnityEngine.Object.DestroyImmediate(level);
                }
            }
            if (candidates.Count == 0)
            {
                _lastGenerateBaseRejectSummary = BuildRejectSummary(rejectCounts, 3, out _lastGenerateBasePrimaryRejectReason);
                return null;
            }
            candidates.Sort((a, b) => b.score.CompareTo(a.score));
            int topCount = Mathf.Min(TopCandidatesForRandomPick, candidates.Count);
            int pickIndex = rng.Next(topCount);
            LevelData chosen = candidates[pickIndex].level;
            for (int i = 0; i < candidates.Count; i++)
                if (i != pickIndex && candidates[i].level != null)
                    UnityEngine.Object.DestroyImmediate(candidates[i].level);
            _lastGenerateBasePrimaryRejectReason = "none";
            _lastGenerateBaseRejectSummary = "none";
            if (EnableLayoutDebugLog && chosen != null)
                UnityEngine.Debug.Log($"[Layout] GenerateBase N={N} seed={seed} candidates={candidates.Count} topM={topCount} picked={pickIndex} score={(chosen != null ? AestheticEvaluator.Score(chosen) : 0f):F2}");
            return chosen;
        }

        private static Vector2[] LevelPositions(LevelData level)
        {
            if (level?.nodes == null) return null;
            var p = new Vector2[level.nodes.Length];
            for (int i = 0; i < level.nodes.Length; i++)
                if (level.nodes[i].id >= 0 && level.nodes[i].id < p.Length)
                    p[level.nodes[i].id] = level.nodes[i].pos;
            return p;
        }

        private static bool IsRingLikeAndPerimeterTooContinuous(LevelData level, int minBreaks, out int perimeterBreaks)
        {
            perimeterBreaks = 0;
            if (level?.nodes == null || level.edges == null) return false;
            int n = level.nodes.Length;
            if (n < 6) return false;

            var positions = LevelPositions(level);
            if (positions == null || positions.Length != n) return false;

            Vector2 center = Vector2.zero;
            for (int i = 0; i < n; i++) center += positions[i];
            center /= n;

            var ringOrder = new List<(int id, float angle, float radius)>(n);
            float radiusSum = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 d = positions[i] - center;
                float radius = d.magnitude;
                float angle = Mathf.Atan2(d.y, d.x);
                ringOrder.Add((i, angle, radius));
                radiusSum += radius;
            }
            if (radiusSum <= 1e-5f) return false;

            float radiusMean = radiusSum / n;
            float var = 0f;
            for (int i = 0; i < n; i++)
            {
                float dr = ringOrder[i].radius - radiusMean;
                var += dr * dr;
            }
            float radiusStd = Mathf.Sqrt(var / n);
            float radiusCv = radiusStd / Mathf.Max(0.001f, radiusMean);
            if (radiusCv > RingLikeRadiusCvThreshold) return false;

            ringOrder.Sort((a, b) => a.angle.CompareTo(b.angle));
            var edgeSet = new HashSet<(int a, int b)>();
            for (int i = 0; i < level.edges.Length; i++)
            {
                int a = level.edges[i].a;
                int b = level.edges[i].b;
                if (a < 0 || a >= n || b < 0 || b >= n || a == b) continue;
                if (a > b) (a, b) = (b, a);
                edgeSet.Add((a, b));
            }

            int perimeterEdges = 0;
            for (int i = 0; i < n; i++)
            {
                int u = ringOrder[i].id;
                int v = ringOrder[(i + 1) % n].id;
                int a = Mathf.Min(u, v);
                int b = Mathf.Max(u, v);
                if (edgeSet.Contains((a, b)))
                    perimeterEdges++;
            }

            perimeterBreaks = n - perimeterEdges;
            return perimeterBreaks < minBreaks;
        }

        private static void CountRejectReason(Dictionary<string, int> map, string reason)
        {
            if (map == null) return;
            string key = string.IsNullOrEmpty(reason) ? "unknown_reject" : reason;
            if (!map.TryGetValue(key, out int count)) count = 0;
            map[key] = count + 1;
        }

        private static string NormalizeRejectReason(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return "unknown_reject";
            if (reason.StartsWith("minNodeDist:", StringComparison.Ordinal)) return "minNodeDist";
            if (reason.StartsWith("edgeClearance:", StringComparison.Ordinal)) return "edgeClearance";
            if (reason.StartsWith("minAngle:", StringComparison.Ordinal)) return "minAngle";
            if (reason.StartsWith("isolated_node:", StringComparison.Ordinal)) return "isolated_node";
            return reason;
        }

        private static string BuildRejectSummary(Dictionary<string, int> map, int topK, out string primary)
        {
            primary = "unknown_reject";
            if (map == null || map.Count == 0) return "none";
            var ranked = new List<KeyValuePair<string, int>>(map);
            ranked.Sort((a, b) =>
            {
                int c = b.Value.CompareTo(a.Value);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Key, b.Key);
            });
            primary = ranked[0].Key;
            int take = Mathf.Min(Mathf.Max(1, topK), ranked.Count);
            var parts = new List<string>(take);
            for (int i = 0; i < take; i++)
                parts.Add($"{ranked[i].Key}:{ranked[i].Value}");
            return string.Join(", ", parts);
        }

        private static bool TryApplyForbiddenNodes(LevelData level, float forbiddenRatio, int seed, out string rejectReason)
        {
            rejectReason = null;
            if (level?.nodes == null || level.nodes.Length == 0) return true;
            int n = level.nodes.Length;
            int targetForbidden = Mathf.Clamp(Mathf.RoundToInt(n * Mathf.Clamp01(forbiddenRatio)), 0, Mathf.Max(0, n - MinBulbsAfterForbidden));
            if (targetForbidden <= 0) return true;

            var bulbNodeIds = new List<int>(n);
            for (int i = 0; i < level.nodes.Length; i++)
            {
                if (level.nodes[i].nodeType == NodeType.Bulb)
                    bulbNodeIds.Add(level.nodes[i].id);
            }
            if (bulbNodeIds.Count <= MinBulbsAfterForbidden)
            {
                rejectReason = "forbidden_not_enough_bulbs";
                return false;
            }
            targetForbidden = Mathf.Min(targetForbidden, bulbNodeIds.Count - MinBulbsAfterForbidden);
            if (targetForbidden <= 0) return true;

            var rng = new System.Random(seed);
            for (int attempt = 0; attempt < ForbiddenNodePlacementRetries; attempt++)
            {
                var picked = PickRandomNodeIds(bulbNodeIds, targetForbidden, rng);
                if (picked == null || picked.Count == 0)
                    continue;

                var backup = new List<(int index, NodeType type, int switchGroup)>(picked.Count);
                for (int i = 0; i < level.nodes.Length; i++)
                {
                    if (!picked.Contains(level.nodes[i].id)) continue;
                    backup.Add((i, level.nodes[i].nodeType, level.nodes[i].switchGroupId));
                    level.nodes[i].nodeType = NodeType.Blocked;
                    level.nodes[i].switchGroupId = 0;
                }

                bool graphOk = ValidatePlayableConnectivityIgnoringBlocked(level);
                bool solverOk = false;
                if (graphOk)
                {
                    var solve = LevelSolver.Solve(level, maxSolutions: 6, maxStatesExpanded: 120000, maxMillis: 120);
                    solverOk = solve.solvableWithinBudget && solve.solutionsFoundWithinBudget >= 1;
                }

                if (graphOk && solverOk)
                    return true;

                for (int i = 0; i < backup.Count; i++)
                {
                    var b = backup[i];
                    level.nodes[b.index].nodeType = b.type;
                    level.nodes[b.index].switchGroupId = b.switchGroup;
                }
            }

            rejectReason = "forbidden_apply_failed";
            return false;
        }

        private static HashSet<int> PickRandomNodeIds(List<int> sourceIds, int count, System.Random rng)
        {
            if (sourceIds == null || sourceIds.Count == 0 || count <= 0) return null;
            count = Mathf.Min(count, sourceIds.Count);
            var indices = new List<int>(sourceIds.Count);
            for (int i = 0; i < sourceIds.Count; i++) indices.Add(i);
            for (int i = 0; i < count; i++)
            {
                int j = rng.Next(i, indices.Count);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            var set = new HashSet<int>();
            for (int i = 0; i < count; i++)
                set.Add(sourceIds[indices[i]]);
            return set;
        }

        private static bool ValidatePlayableConnectivityIgnoringBlocked(LevelData level)
        {
            if (level?.nodes == null || level.nodes.Length == 0 || level.edges == null) return false;
            int n = level.nodes.Length;
            var blocked = new bool[n];
            int bulbCount = 0;
            int startId = -1;
            for (int i = 0; i < level.nodes.Length; i++)
            {
                var nd = level.nodes[i];
                if (nd.id < 0 || nd.id >= n) return false;
                if (nd.nodeType == NodeType.Blocked)
                    blocked[nd.id] = true;
                else if (nd.nodeType == NodeType.Bulb)
                {
                    bulbCount++;
                    if (startId < 0) startId = nd.id;
                }
            }
            if (bulbCount < MinBulbsAfterForbidden || startId < 0) return false;

            var adj = new List<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new List<int>();
            foreach (var e in level.edges)
            {
                if (e.a < 0 || e.a >= n || e.b < 0 || e.b >= n || e.a == e.b) continue;
                if (blocked[e.a] || blocked[e.b]) continue;
                if (!adj[e.a].Contains(e.b)) adj[e.a].Add(e.b);
                if (!adj[e.b].Contains(e.a)) adj[e.b].Add(e.a);
            }

            for (int i = 0; i < n; i++)
            {
                if (blocked[i]) continue;
                if (adj[i].Count == 0) return false;
            }

            var visited = new HashSet<int>();
            var q = new Queue<int>();
            q.Enqueue(startId);
            visited.Add(startId);
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                for (int i = 0; i < adj[u].Count; i++)
                {
                    int v = adj[u][i];
                    if (visited.Add(v))
                        q.Enqueue(v);
                }
            }

            for (int i = 0; i < n; i++)
            {
                if (blocked[i]) continue;
                if (!visited.Contains(i)) return false;
            }
            return true;
        }

        private static float GetEdgeClearanceThreshold(int n)
        {
            if (n <= 10) return EdgeClearanceSmallN;
            if (n <= 14) return EdgeClearanceMediumN;
            if (n <= 18) return EdgeClearanceLargeN;
            return EdgeClearanceXLN;
        }

        private static float GetAngleThresholdDeg(int n)
        {
            if (n <= 10) return AngleThresholdSmallN;
            if (n <= 14) return AngleThresholdMediumN;
            if (n <= 18) return AngleThresholdLargeN;
            return AngleThresholdXLN;
        }

        /// <summary>
        /// Fast sanity checks: connectivity from any Bulb, no isolated nodes, hub cap, optional too-linear reject.
        /// Ignores diode/gate (base graph is undirected for connectivity).
        /// </summary>
        public static bool ValidateBaseGraph(LevelData level, bool requireNormalHardVariety = false)
        {
            return ValidateBaseGraph(level, requireNormalHardVariety, out _);
        }

        public static bool ValidateBaseGraph(LevelData level, bool requireNormalHardVariety, out string rejectReason)
        {
            rejectReason = null;
            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0)
            {
                rejectReason = "null_or_empty_level";
                return false;
            }
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
                if (node.id < 0 || node.id >= n) { rejectReason = "invalid_node_id"; return false; }
                if (adj[node.id].Count < 1) { rejectReason = $"isolated_node:{node.id}"; return false; }
            }
            int firstBulb = -1;
            for (int i = 0; i < level.nodes.Length; i++)
            {
                if (level.nodes[i].nodeType == NodeType.Bulb) { firstBulb = level.nodes[i].id; break; }
            }
            if (firstBulb < 0) { rejectReason = "no_bulb"; return false; }
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
            if (visited.Count != n) { rejectReason = "disconnected"; return false; }

            var metrics = LayoutDiagnostics.ComputeLayoutMetrics(level);
            float clearanceThreshold = GetEdgeClearanceThreshold(n);
            float angleThreshold = GetAngleThresholdDeg(n);
            if (metrics.minNodeDist < MinNodeSpacing) { rejectReason = $"minNodeDist:{metrics.minNodeDist:F3}<{MinNodeSpacing:F3}"; return false; }
            if (metrics.minEdgeNodeClearance < clearanceThreshold) { rejectReason = $"edgeClearance:{metrics.minEdgeNodeClearance:F3}<{clearanceThreshold:F3}"; return false; }
            if (metrics.minAngleSeparationDeg < angleThreshold) { rejectReason = $"minAngle:{metrics.minAngleSeparationDeg:F1}<{angleThreshold:F1}"; return false; }
            return true;
        }

        private static LevelData GenerateBaseInternal(int N, int seed, GenerationOptions opts, System.Random rng, out string usedLayoutName, out bool templateApplied)
        {
            usedLayoutName = null;
            templateApplied = false;
            List<(int a, int b)> edgeList = null;
            string topologyName = null;
            int switchOutputNodeId = -1;
            LevelTemplate? t = null;
            for (int i = 0; i < LevelTemplates.All.Length; i++)
            {
                if (LevelTemplates.All[i].nodeCount == N)
                {
                    t = LevelTemplates.All[i];
                    topologyName = t.Value.name;
                    break;
                }
            }
            if (t.HasValue)
            {
                LevelTemplate tmpl = t.Value;
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
                topologyName = "RandomTopology";
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
            Vector2[] positions = PlaceNodesWithLayout(N, rng, outEdges, seed, out usedLayoutName, out templateApplied);
            if (positions == null || positions.Length != N)
            {
                if (EnableLayoutDebugLog)
                    UnityEngine.Debug.Log($"[Layout] GenerateBaseInternal N={N} seed={seed} topology={topologyName} layout={usedLayoutName} templateApplied={templateApplied} -> fallback circle");
                usedLayoutName = "CircleFallback";
                templateApplied = false;
                positions = PlaceNodesOnCircleFallback(N, rng);
            }
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

        /// <summary>?됯퇏 李⑥닔 3~4 ?좊룄. 遺꾧린 ?뺣낫?섎릺 吏㏃? ?ｌ?(?몃뜳??嫄곕━) ?곗꽑 異붽?.</summary>
        private static List<(int a, int b)> GenerateBaseGraphTopology(int N, System.Random rng)
        {
            var edgeSet = new HashSet<(int a, int b)>();
            for (int i = 0; i < N - 1; i++)
                edgeSet.Add((i, i + 1));
            float targetAvgDegree = N <= 12 ? 3.0f : (N <= 16 ? 3.2f : 3.5f);
            int completeEdgeCount = N * (N - 1) / 2;
            int targetEdges = Mathf.Clamp((int)Math.Round(N * targetAvgDegree * 0.5f), N - 1, completeEdgeCount);
            int targetExtra = Math.Max(0, targetEdges - (N - 1));
            if (N <= 10) targetExtra = Math.Min(targetExtra, N);

            var candidates = new List<(int u, int v)>();
            for (int u = 0; u < N; u++)
                for (int v = u + 2; v < N; v++)
                {
                    if (edgeSet.Contains((u, v))) continue;
                    candidates.Add((u, v));
                }

            // Mix local/long links for non-corridor branching.
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            for (int i = 0; i < candidates.Count && edgeSet.Count < targetEdges && i < targetExtra * 4; i++)
            {
                int u = candidates[i].u, v = candidates[i].v;
                if (edgeSet.Contains((u, v))) continue;
                edgeSet.Add((u, v));
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
        /// When all retries fail, returns null so caller can use circle fallback.
        /// </summary>
        private static Vector2[] PlaceNodesWithLayout(int n, System.Random rng, List<EdgeData> edgeList, int seed, out string usedLayoutName, out bool templateApplied)
        {
            usedLayoutName = null;
            templateApplied = false;
            var layouts = LayoutTemplates.GetLayoutsForNodeCount(n);
            if (layouts == null || layouts.Count == 0)
                return null;

            var templateOrder = new List<int>(layouts.Count);
            for (int i = 0; i < layouts.Count; i++) templateOrder.Add(i);
            for (int i = templateOrder.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (templateOrder[i], templateOrder[j]) = (templateOrder[j], templateOrder[i]);
            }

            var slotPerm = new int[n];
            for (int i = 0; i < n; i++) slotPerm[i] = i;
            var positions = new Vector2[n];
            var transformed = new Vector2[n];
            var accepted = new List<(string name, float score, Vector2[] pos)>();

            foreach (int templateIndex in templateOrder)
            {
                var layout = layouts[templateIndex];
                if (layout.slots == null || layout.slots.Length < n) continue;

                int retriesPerTemplate = Mathf.Max(6, LayoutRetryCount / Mathf.Max(1, layouts.Count));
                for (int tryCount = 0; tryCount < retriesPerTemplate; tryCount++)
                {
                    usedLayoutName = layout.name;
                    templateApplied = true;

                    bool exactLayout = IsExactLayout(layout.name);

                    BuildShapePreservingSlotPermutation(layout.name, n, rng, slotPerm);
                    for (int j = 0; j < n; j++)
                        positions[j] = layout.slots[slotPerm[j]];

                    float avgLen = edgeList != null && edgeList.Count > 0
                        ? AestheticEvaluator.AverageEdgeLength(edgeList, positions)
                        : 1.5f;
                    if (avgLen < 0.1f) avgLen = 1.5f;
                    float jitterMax = exactLayout ? 0f : (JitterMaxFractionOfAvgEdge * avgLen);
                    float rotationDeg = 0f, shearX = 0f, shearY = 0f, radialWarp = 0f;

                    if (exactLayout)
                    {
                        ApplyLayoutVariant(positions, transformed, 0f, 0f, 0f, 0f, rng, 0f);
                    }
                    else
                    {
                        GetLayoutVariantParams(layout.name, rng, out rotationDeg, out shearX, out shearY, out radialWarp);
                        ApplyLayoutVariant(positions, transformed, rotationDeg, shearX, shearY, radialWarp, rng, jitterMax);
                    }

                    float minDist = AestheticEvaluator.MinNodeDistance(transformed, n);
                    if (minDist < MinNodeDistance) continue;
                    float minClearance = edgeList != null ? AestheticEvaluator.MinEdgeToNodeDistance(edgeList, transformed, n) : float.MaxValue;
                    float minAngle = edgeList != null ? ComputeMinAngleSeparationFromEdges(edgeList, transformed, n) : 180f;
                    int crossings = edgeList != null ? AestheticEvaluator.CountCrossings(edgeList, transformed, n) : 0;
                    int maxCrossingsForPlacement = n <= 12 ? 16 : 24;
                    bool placementOk = minClearance >= LayoutPlacementMinClearance
                        && minAngle >= LayoutPlacementMinAngleDeg
                        && crossings <= maxCrossingsForPlacement;

                    if (placementOk)
                    {
                        var outPos = new Vector2[n];
                        for (int j = 0; j < n; j++) outPos[j] = transformed[j];
                        float localScore = AestheticEvaluator.Score(edgeList, outPos, n);
                        accepted.Add((layout.name, localScore, outPos));
                        if (EnableLayoutDebugLog)
                        {
                            if (exactLayout)
                                UnityEngine.Debug.Log($"[Layout] N={n} seed={seed} template={layout.name} exact=true applied=true minDist={minDist:F3} clear={minClearance:F3} angle={minAngle:F1} cross={crossings}");
                            else
                                UnityEngine.Debug.Log($"[Layout] N={n} seed={seed} template={layout.name} rot={rotationDeg:F1} shear=({shearX:F2},{shearY:F2}) warp={radialWarp:F2} applied=true minDist={minDist:F3} clear={minClearance:F3} angle={minAngle:F1} cross={crossings}");
                        }
                    }

                    if (EnableLayoutDebugLog && tryCount == retriesPerTemplate - 1)
                    {
                        if (exactLayout)
                            UnityEngine.Debug.Log($"[Layout] N={n} seed={seed} template={layout.name} exact=true applied=false minDist={minDist:F3} clear={minClearance:F3} angle={minAngle:F1} cross={crossings}");
                        else
                            UnityEngine.Debug.Log($"[Layout] N={n} seed={seed} template={layout.name} rot={rotationDeg:F1} shear=({shearX:F2},{shearY:F2}) warp={radialWarp:F2} applied=false minDist={minDist:F3} clear={minClearance:F3} angle={minAngle:F1} cross={crossings}");
                    }
                }
            }
            if (accepted.Count == 0) return null;

            // Keep the 4x4 knight graph silhouette as-authored when available.
            var exactAccepted = accepted.FindAll(a => IsExactLayout(a.name));
            if (exactAccepted.Count > 0)
            {
                var exactChoice = exactAccepted[rng.Next(exactAccepted.Count)];
                usedLayoutName = exactChoice.name;
                templateApplied = true;
                if (EnableLayoutDebugLog)
                    UnityEngine.Debug.Log($"[Layout] N={n} seed={seed} accepted={accepted.Count} exactPicked={usedLayoutName}");
                return exactChoice.pos;
            }

            accepted.Sort((a, b) => b.score.CompareTo(a.score));
            int topCount = Mathf.Min(6, accepted.Count);
            var top = accepted.GetRange(0, topCount);
            var uniqueNames = new List<string>();
            for (int i = 0; i < top.Count; i++)
            {
                if (!uniqueNames.Contains(top[i].name))
                    uniqueNames.Add(top[i].name);
            }
            string pickedName = uniqueNames[rng.Next(uniqueNames.Count)];
            var sameTemplate = new List<(string name, float score, Vector2[] pos)>();
            for (int i = 0; i < top.Count; i++)
                if (top[i].name == pickedName)
                    sameTemplate.Add(top[i]);
            var chosen = sameTemplate[rng.Next(sameTemplate.Count)];
            usedLayoutName = chosen.name;
            templateApplied = true;
            if (EnableLayoutDebugLog)
                UnityEngine.Debug.Log($"[Layout] N={n} seed={seed} accepted={accepted.Count} top={topCount} pickedTemplate={pickedName}");
            return chosen.pos;
        }

        private static void BuildShapePreservingSlotPermutation(string layoutName, int n, System.Random rng, int[] slotPerm)
        {
            if (slotPerm == null || slotPerm.Length < n) return;
            if (IsExactLayout(layoutName))
            {
                for (int i = 0; i < n; i++) slotPerm[i] = i;
                return;
            }
            bool radial = IsRadialLayout(layoutName);
            if (radial)
            {
                int offset = rng.Next(n);
                bool reverse = rng.NextDouble() < 0.5;
                for (int i = 0; i < n; i++)
                {
                    int idx = reverse ? (offset - i) : (offset + i);
                    idx %= n;
                    if (idx < 0) idx += n;
                    slotPerm[i] = idx;
                }
                return;
            }

            // For non-radial templates keep ordering mostly intact to preserve silhouette.
            bool reverseLinear = rng.NextDouble() < 0.25;
            for (int i = 0; i < n; i++)
                slotPerm[i] = reverseLinear ? (n - 1 - i) : i;

            int localSwaps = Mathf.Clamp(n / 10, 0, 2);
            for (int s = 0; s < localSwaps; s++)
            {
                int a = rng.Next(0, n - 1);
                int b = Mathf.Min(n - 1, a + 1 + rng.Next(2));
                (slotPerm[a], slotPerm[b]) = (slotPerm[b], slotPerm[a]);
            }
        }

        private static bool IsRadialLayout(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName)) return false;
            return layoutName == "Ring"
                || layoutName == "StarSpoke"
                || layoutName == "DoubleRing"
                || layoutName == "ConcentricPolygon"
                || layoutName == "PentagonSpiral"
                || layoutName == "TwoCluster";
        }

        private static bool IsExactLayout(string layoutName)
        {
            return layoutName == "KnightTour4x4Exact";
        }

        private static void GetLayoutVariantParams(string layoutName, System.Random rng, out float rotationDeg, out float shearX, out float shearY, out float radialWarp)
        {
            bool radial = IsRadialLayout(layoutName);
            if (radial)
            {
                rotationDeg = Mathf.Lerp(-18f, 18f, (float)rng.NextDouble());
                shearX = Mathf.Lerp(-0.08f, 0.08f, (float)rng.NextDouble());
                shearY = Mathf.Lerp(-0.05f, 0.05f, (float)rng.NextDouble());
                radialWarp = Mathf.Lerp(-0.06f, 0.10f, (float)rng.NextDouble());
                return;
            }

            rotationDeg = Mathf.Lerp(-10f, 10f, (float)rng.NextDouble());
            shearX = Mathf.Lerp(-0.05f, 0.05f, (float)rng.NextDouble());
            shearY = Mathf.Lerp(-0.03f, 0.03f, (float)rng.NextDouble());
            radialWarp = Mathf.Lerp(-0.04f, 0.06f, (float)rng.NextDouble());
        }

        private static float ComputeMinAngleSeparationFromEdges(IReadOnlyList<EdgeData> edges, IReadOnlyList<Vector2> positions, int n)
        {
            if (edges == null || positions == null || n <= 0) return 180f;
            var adj = new List<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new List<int>();
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                if (e.a >= 0 && e.a < n && e.b >= 0 && e.b < n && e.a != e.b)
                {
                    if (!adj[e.a].Contains(e.b)) adj[e.a].Add(e.b);
                    if (!adj[e.b].Contains(e.a)) adj[e.b].Add(e.a);
                }
            }
            return AestheticEvaluator.MinAngleSeparationDeg(adj, positions, n);
        }

        private static void ApplyLayoutVariant(
            Vector2[] source,
            Vector2[] output,
            float rotationDeg,
            float shearX,
            float shearY,
            float radialWarp,
            System.Random rng,
            float jitterMax)
        {
            int n = source.Length;
            Vector2 centroid = Vector2.zero;
            for (int i = 0; i < n; i++) centroid += source[i];
            centroid /= Mathf.Max(1, n);

            float angle = rotationDeg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(angle);
            float sn = Mathf.Sin(angle);
            float maxRadius = 0.001f;
            for (int i = 0; i < n; i++)
            {
                float r = Vector2.Distance(source[i], centroid);
                if (r > maxRadius) maxRadius = r;
            }

            for (int i = 0; i < n; i++)
            {
                Vector2 p = source[i] - centroid;
                Vector2 rotated = new Vector2(p.x * cs - p.y * sn, p.x * sn + p.y * cs);
                Vector2 sheared = new Vector2(rotated.x + shearX * rotated.y, rotated.y + shearY * rotated.x);

                float normR = Mathf.Clamp01(sheared.magnitude / maxRadius);
                float warpScale = 1f + radialWarp * normR;
                Vector2 warped = sheared * warpScale;

                float jitterX = (float)(rng.NextDouble() * 2 - 1) * jitterMax;
                float jitterY = (float)(rng.NextDouble() * 2 - 1) * jitterMax;
                output[i] = centroid + warped + new Vector2(jitterX, jitterY);
            }
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
