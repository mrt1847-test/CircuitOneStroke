#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CircuitOneStroke.Data;
using CircuitOneStroke.Generation;
using CircuitOneStroke.Solver;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// Editor-only tests for Level Factory v2: generate levels, ensure SolverV2 doesn't timeout,
    /// and a reasonable fraction pass acceptance within attempt budget.
    /// </summary>
    public static class LevelFactoryV2Tests
    {
        private const int TestLevelCount = 10;
        private const int MaxAttemptsPerLevel = 150;
        private const float MinPassRate = 0.5f;

        [MenuItem("Tools/Circuit One-Stroke/Run Level Factory V2 Tests")]
        public static void RunTests()
        {
            int passed = 0;
            int timeoutCount = 0;
            var settings = SolverSettings.Default;
            settings.TimeBudgetMs = 200;
            settings.MaxSolutionsCap = 6;

            for (int i = 0; i < TestLevelCount; i++)
            {
                bool accepted = false;
                for (int attempt = 0; attempt < MaxAttemptsPerLevel && !accepted; attempt++)
                {
                    var p = GenerateParams.Default;
                    p.Seed = 1000 + i * 100 + attempt;
                    p.NodeCountMin = 16;
                    p.NodeCountMax = 22;
                    LevelData level = null;
                    try
                    {
                        level = BackboneFirstGenerator.Generate(p);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Generate failed: {ex.Message}");
                        continue;
                    }
                    var outcome = LevelSolverV2.Evaluate(level, settings);
                    if (outcome.Status == SolverV2Status.Timeout)
                        timeoutCount++;
                    if (outcome.Status == SolverV2Status.Feasible &&
                        outcome.SolutionsFoundCapped >= 1 && outcome.SolutionsFoundCapped <= 5 &&
                        outcome.DecisionPoints >= 1 && outcome.AvgBranching <= 2.5f &&
                        AestheticEvaluator.Accept(level, 2, 0.22f, 0.45f))
                    {
                        passed++;
                        accepted = true;
                    }
                    Object.DestroyImmediate(level);
                }
            }

            float rate = TestLevelCount > 0 ? (float)passed / TestLevelCount : 0f;
            if (timeoutCount > TestLevelCount)
                Debug.LogError($"Level Factory V2 Tests: Too many timeouts ({timeoutCount}). Consider increasing time budget.");
            else if (rate < MinPassRate)
                Debug.LogWarning($"Level Factory V2 Tests: Pass rate {rate:P0} (passed={passed}, target>={MinPassRate:P0}). Tune thresholds or attempts.");
            else
                Debug.Log($"Level Factory V2 Tests: Passed {passed}/{TestLevelCount} ({rate:P0}), timeouts in run: {timeoutCount}. OK.");
        }
    }
}
#endif
