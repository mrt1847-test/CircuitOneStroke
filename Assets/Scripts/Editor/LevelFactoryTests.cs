#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CircuitOneStroke.Data;
using CircuitOneStroke.Generation;
using CircuitOneStroke.Solver;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// Editor-only tests for Level Factory: generate levels, ensure solver stays within budget,
    /// and a reasonable fraction pass acceptance within attempt budget.
    /// </summary>
    public static class LevelFactoryTests
    {
        private const int TestLevelCount = 10;
        private const int MaxAttemptsPerLevel = 150;
        private const float MinPassRate = 0.5f;

        [MenuItem("Tools/Circuit One-Stroke/Run Level Factory Tests")]
        public static void RunTests()
        {
            int passed = 0;
            int timeoutCount = 0;
            int maxSolutions = 6;
            int maxStatesExpanded = 250000;
            int maxMillis = 200;

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
                    var outcome = LevelSolver.Solve(level, maxSolutions, maxStatesExpanded, maxMillis);
                    if (outcome.status == SolverStatus.BudgetExceeded)
                        timeoutCount++;
                    if (outcome.solvableWithinBudget &&
                        outcome.solutionsFoundWithinBudget >= 1 && outcome.solutionsFoundWithinBudget <= 5 &&
                        outcome.earlyBranching >= 1.0f && outcome.earlyBranching <= 2.5f &&
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
                Debug.LogError($"Level Factory Tests: Too many timeouts ({timeoutCount}). Consider increasing time budget.");
            else if (rate < MinPassRate)
                Debug.LogWarning($"Level Factory Tests: Pass rate {rate:P0} (passed={passed}, target>={MinPassRate:P0}). Tune thresholds or attempts.");
            else
                Debug.Log($"Level Factory Tests: Passed {passed}/{TestLevelCount} ({rate:P0}), timeouts in run: {timeoutCount}. OK.");
        }
    }
}
#endif
