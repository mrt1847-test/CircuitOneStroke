#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CircuitOneStroke.Data;
using CircuitOneStroke.Generation;
using CircuitOneStroke.Solver;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// Editor window to generate levels from templates with solver filtering, then save as assets and update manifest.
    /// </summary>
    public class LevelBakeTool : EditorWindow
    {
        private DifficultyTier _tier = DifficultyTier.Easy;
        private int _targetCount = 20;
        private int _seedStart = 1;
        private string _outputFolder = "Assets/Levels/Generated";
        private bool _includeSwitchOverride;
        private bool _useIncludeSwitchOverride;
        private bool _useAutoTunedGenerator;
        private bool _syncFallbackResourcesLevels = true;
        private bool _clearPreviousFallbackBeforeSync = true;
        private float _forbiddenNodePercent = 0f;
        private bool _advancedFoldout;
        private int _maxAttempts = 6000;
        private int _solutionCountMin = 1;
        private int _solutionCountMaxEasy = 80;
        private int _solutionCountMaxMedium = 120;
        private int _solutionCountMaxHard = 200;
        private float _earlyBranchingMinEasy = 1.4f;
        private float _earlyBranchingMinMedium = 1.7f;
        private float _earlyBranchingMinHard = 2.0f;
        private float _deadEndDepthMinEasy = 2f;
        private float _deadEndDepthMaxEasy = 6f;
        private float _deadEndDepthMinMedium = 3f;
        private float _deadEndDepthMaxMedium = 7f;
        private float _deadEndDepthMinHard = 4f;
        private float _deadEndDepthMaxHard = 8f;
        private Vector2 _scrollPos;

        [MenuItem("Circuit One-Stroke/Level Bake Tool")]
        public static void ShowWindow()
        {
            var win = GetWindow<LevelBakeTool>("Level Bake");
            win.minSize = new Vector2(320, 360);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Level Bake Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _tier = (DifficultyTier)EditorGUILayout.EnumPopup("Tier", _tier);
            _targetCount = Mathf.Max(1, EditorGUILayout.IntField("Target Count", _targetCount));
            _seedStart = EditorGUILayout.IntField("Seed Start", _seedStart);
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            _useAutoTunedGenerator = EditorGUILayout.Toggle("Use Auto-Tuned Generator (slow)", _useAutoTunedGenerator);
            _forbiddenNodePercent = EditorGUILayout.Slider("Forbidden Node %", _forbiddenNodePercent, 0f, 45f);
            _syncFallbackResourcesLevels = EditorGUILayout.Toggle("Sync Resources/Levels/Level_x", _syncFallbackResourcesLevels);
            if (_syncFallbackResourcesLevels)
            {
                EditorGUI.indentLevel++;
                _clearPreviousFallbackBeforeSync = EditorGUILayout.Toggle("  Clear previous fallback assets", _clearPreviousFallbackBeforeSync);
                EditorGUI.indentLevel--;
            }
            _useIncludeSwitchOverride = EditorGUILayout.Toggle("Override Include Switch", _useIncludeSwitchOverride);
            if (_useIncludeSwitchOverride)
                _includeSwitchOverride = EditorGUILayout.Toggle("  Include Switch", _includeSwitchOverride);

            _advancedFoldout = EditorGUILayout.Foldout(_advancedFoldout, "Advanced filter parameters");
            if (_advancedFoldout)
            {
                EditorGUI.indentLevel++;
                _maxAttempts = Mathf.Max(100, EditorGUILayout.IntField("Max attempts", _maxAttempts));
                _solutionCountMin = EditorGUILayout.IntField("Solution count min", _solutionCountMin);
                _solutionCountMaxEasy = EditorGUILayout.IntField("Solution count max (Easy)", _solutionCountMaxEasy);
                _solutionCountMaxMedium = EditorGUILayout.IntField("Solution count max (Medium)", _solutionCountMaxMedium);
                _solutionCountMaxHard = EditorGUILayout.IntField("Solution count max (Hard)", _solutionCountMaxHard);
                _earlyBranchingMinEasy = EditorGUILayout.FloatField("Early branching min (Easy)", _earlyBranchingMinEasy);
                _earlyBranchingMinMedium = EditorGUILayout.FloatField("Early branching min (Medium)", _earlyBranchingMinMedium);
                _earlyBranchingMinHard = EditorGUILayout.FloatField("Early branching min (Hard)", _earlyBranchingMinHard);
                _deadEndDepthMinEasy = EditorGUILayout.FloatField("Dead-end depth min (Easy)", _deadEndDepthMinEasy);
                _deadEndDepthMaxEasy = EditorGUILayout.FloatField("Dead-end depth max (Easy)", _deadEndDepthMaxEasy);
                _deadEndDepthMinMedium = EditorGUILayout.FloatField("Dead-end depth min (Medium)", _deadEndDepthMinMedium);
                _deadEndDepthMaxMedium = EditorGUILayout.FloatField("Dead-end depth max (Medium)", _deadEndDepthMaxMedium);
                _deadEndDepthMinHard = EditorGUILayout.FloatField("Dead-end depth min (Hard)", _deadEndDepthMinHard);
                _deadEndDepthMaxHard = EditorGUILayout.FloatField("Dead-end depth max (Hard)", _deadEndDepthMaxHard);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Generate & Save", GUILayout.Height(32)))
                DoGenerateAndSave();
            if (GUILayout.Button("Ping Runtime Manifest", GUILayout.Height(22)))
            {
                var manifest = EnsureManifest();
                if (manifest != null)
                {
                    EditorGUIUtility.PingObject(manifest);
                    Selection.activeObject = manifest;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private bool PassesFilterCustom(DifficultyTier tier, SolverResult result)
        {
            return string.IsNullOrEmpty(GetRejectReasonCustom(tier, result));
        }

        private string GetRejectReasonDefault(SolverResult result)
        {
            if (!result.solvableWithinBudget) return "default_not_solved_within_budget";
            if (result.solutionsFoundWithinBudget < 1) return "default_zero_solutions_within_budget";
            return null;
        }

        private string GetRejectReasonCustom(DifficultyTier tier, SolverResult result)
        {
            bool solvable = result.solvable || result.solvableWithinBudget;
            int solutions = result.solutionCount > 0 ? result.solutionCount : result.solutionsFoundWithinBudget;
            if (!solvable) return "custom_unsolvable";
            if (solutions < _solutionCountMin) return "custom_solution_count_too_low";
            int maxCount = tier == DifficultyTier.Easy ? _solutionCountMaxEasy : tier == DifficultyTier.Medium ? _solutionCountMaxMedium : _solutionCountMaxHard;
            if (solutions > maxCount) return "custom_solution_count_too_high";
            float branchMin = tier == DifficultyTier.Easy ? _earlyBranchingMinEasy : tier == DifficultyTier.Medium ? _earlyBranchingMinMedium : _earlyBranchingMinHard;
            if (result.earlyBranching < branchMin) return "custom_early_branching_too_low";
            float depthMin = tier == DifficultyTier.Easy ? _deadEndDepthMinEasy : tier == DifficultyTier.Medium ? _deadEndDepthMinMedium : _deadEndDepthMinHard;
            float depthMax = tier == DifficultyTier.Easy ? _deadEndDepthMaxEasy : tier == DifficultyTier.Medium ? _deadEndDepthMaxMedium : _deadEndDepthMaxHard;
            if (result.deadEndDepthAvg < depthMin) return "custom_dead_end_depth_too_low";
            if (result.deadEndDepthAvg > depthMax) return "custom_dead_end_depth_too_high";
            return null;
        }

        private void DoGenerateAndSave()
        {
            if (string.IsNullOrEmpty(_outputFolder))
            {
                Debug.LogError("Output folder is empty.");
                return;
            }

            EnsureFolderExists(_outputFolder);

            bool? includeSwitch = _useIncludeSwitchOverride ? (bool?)_includeSwitchOverride : null;
            var savedLevels = new List<LevelData>();
            int seed = _seedStart;
            int levelId = 1;
            bool useCustomFilter = _advancedFoldout;
            int attempts = 0;
            int maxAttempts = Mathf.Max(_targetCount * 50, _maxAttempts);
            int rejectedUnsolvable = 0;
            int rejectedFilter = 0;
            var failReasons = new Dictionary<string, int>();

            try
            {
                while (savedLevels.Count < _targetCount && attempts < maxAttempts)
                {
                    attempts++;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Level Bake",
                        $"Generating... saved {savedLevels.Count}/{_targetCount}, attempts {attempts}/{maxAttempts}",
                        attempts / (float)maxAttempts))
                    {
                        Debug.LogWarning("[LevelBakeTool] Cancelled by user.");
                        break;
                    }

                    LevelData level = null;
                    string templateName = "AutoTuned";
                    if (_useAutoTunedGenerator)
                    {
                        level = LevelGenerator.GenerateWithSuccessRateTarget(_tier, seed, out int tunedN, out _, out _, out _, _forbiddenNodePercent * 0.01f);
                        templateName = $"AutoTuned_N{tunedN}";
                    }
                    else
                    {
                        DifficultyProfile.GetNRange(_tier, out int rangeMin, out int rangeMax);
                        var rng = new System.Random(seed);
                        int pickedNodeCount = rangeMin + rng.Next(Mathf.Max(1, rangeMax - rangeMin + 1));
                        bool includeSwitchValue = _useIncludeSwitchOverride ? includeSwitch.GetValueOrDefault() : (_tier != DifficultyTier.Easy);
                        var opts = new GenerationOptions
                        {
                            IncludeSwitch = includeSwitchValue,
                            SwitchCount = includeSwitchValue ? 1 : 0,
                            RequireNormalHardVariety = _tier != DifficultyTier.Easy,
                            ForbiddenNodeRatio = _forbiddenNodePercent * 0.01f
                        };
                        level = LevelGenerator.GenerateBase(pickedNodeCount, seed, opts);
                        templateName = $"Base_N{pickedNodeCount}";
                    }
                    if (level == null)
                    {
                        string primaryReject = LevelGenerator.LastGenerateBasePrimaryRejectReason;
                        string summaryReject = LevelGenerator.LastGenerateBaseRejectSummary;
                        if (!string.IsNullOrEmpty(primaryReject) && !string.Equals(primaryReject, "none", System.StringComparison.Ordinal))
                            CountFailReason(failReasons, $"generator_null:{primaryReject}");
                        else
                            CountFailReason(failReasons, "generator_returned_null");
                        if (!string.IsNullOrEmpty(summaryReject) && !string.Equals(summaryReject, "none", System.StringComparison.Ordinal) && attempts <= 8)
                            Debug.Log($"[LevelBakeTool] generator_null_reject_summary: {summaryReject}");
                        seed++;
                        continue;
                    }

                    // Always enforce difficulty node-count range in bake output.
                    DifficultyProfile.GetNRange(_tier, out int enforceMin, out int enforceMax);
                    int generatedNodeCount = level.nodes != null ? level.nodes.Length : 0;
                    if (generatedNodeCount < enforceMin || generatedNodeCount > enforceMax)
                    {
                        CountFailReason(failReasons, $"node_count_out_of_range({generatedNodeCount}, expected {enforceMin}-{enforceMax})");
                        Object.DestroyImmediate(level);
                        seed++;
                        continue;
                    }

                    if (_useAutoTunedGenerator && _useIncludeSwitchOverride && includeSwitch.HasValue)
                        ApplySwitchOverride(level, includeSwitch.Value, seed);

                    var result = LevelSolver.Solve(level, LevelSolver.MaxSolutionsDefault, LevelSolver.MaxStatesExpandedDefault, 120);

                    string filterRejectReason = useCustomFilter
                        ? GetRejectReasonCustom(_tier, result)
                        : GetRejectReasonDefault(result);
                    bool pass = string.IsNullOrEmpty(filterRejectReason);
                    if (pass)
                    {
                        level.levelId = levelId;
                        string assetPath = $"{_outputFolder}/Level_{levelId}.asset";
                        if (AssetDatabase.LoadAssetAtPath<LevelData>(assetPath) != null)
                            AssetDatabase.DeleteAsset(assetPath);
                        AssetDatabase.CreateAsset(level, assetPath);

                        string metaPath = $"{_outputFolder}/Level_{levelId}_meta.json";
                        int solutions = result.solutionCount > 0 ? result.solutionCount : result.solutionsFoundWithinBudget;
                        string metaJson = $"{{\"seed\":{seed},\"templateName\":\"{templateName}\",\"tier\":\"{_tier}\",\"solutionCount\":{solutions},\"nodesExpanded\":{result.nodesExpanded},\"earlyBranching\":{result.earlyBranching:F2},\"deadEndDepthAvg\":{result.deadEndDepthAvg:F2},\"forbiddenNodePercent\":{_forbiddenNodePercent:F1}}}";
                        File.WriteAllText(metaPath, metaJson);

                        savedLevels.Add(level);
                        levelId++;
                        Debug.Log($"Saved level {savedLevels.Count}: seed={seed}, template={templateName}, solutions={solutions}");
                    }
                    else
                    {
                        bool solvable = result.solvable || result.solvableWithinBudget;
                        if (!solvable) rejectedUnsolvable++;
                        else rejectedFilter++;
                        if (!solvable)
                            CountFailReason(failReasons, "solver_unsolvable_within_budget");
                        else if (!string.IsNullOrEmpty(filterRejectReason))
                            CountFailReason(failReasons, filterRejectReason);
                        else
                            CountFailReason(failReasons, "custom_or_default_filter_rejected");
                        Object.DestroyImmediate(level);
                    }
                    seed++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (savedLevels.Count < _targetCount)
            {
                string top3 = BuildTopFailReasonsSummary(failReasons, attempts, 3);
                Debug.LogWarning($"[LevelBakeTool] Stopped before target. saved={savedLevels.Count}/{_targetCount}, attempts={attempts}/{maxAttempts}, unsolvableRejects={rejectedUnsolvable}, filterRejects={rejectedFilter}, topFailReasons={top3}. Relax filters or raise Max attempts.");
            }

            LevelManifest manifest = EnsureManifest();
            if (manifest != null)
            {
                manifest.levels = savedLevels.ToArray();
                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssets();
            }
            if (_syncFallbackResourcesLevels)
                SyncFallbackResourcesLevels(savedLevels, _clearPreviousFallbackBeforeSync);
            AssetDatabase.Refresh();
            Debug.Log($"Level Bake complete: {savedLevels.Count} levels saved to {_outputFolder}. Runtime manifest updated at {ManifestResourcesPath}.");
        }

        private void EnsureFolderExists(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    string parent = current;
                    string newFolder = parts[i];
                    if (!AssetDatabase.IsValidFolder(parent))
                    {
                        string[] parentParts = parent.Split('/');
                        string p = parentParts[0];
                        for (int j = 1; j < parentParts.Length; j++)
                        {
                            string pNext = p + "/" + parentParts[j];
                            if (!AssetDatabase.IsValidFolder(pNext))
                                AssetDatabase.CreateFolder(p, parentParts[j]);
                            p = pNext;
                        }
                    }
                    AssetDatabase.CreateFolder(parent, newFolder);
                }
                current = next;
            }
        }

        private const string ManifestResourcesPath = "Assets/Resources/Levels/GeneratedLevelManifest.asset";
        private const string RuntimeFallbackFolder = "Assets/Resources/Levels";

        private LevelManifest EnsureManifest()
        {
            EnsureFolderExists("Assets/Resources");
            EnsureFolderExists("Assets/Resources/Levels");
            var manifest = AssetDatabase.LoadAssetAtPath<LevelManifest>(ManifestResourcesPath);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<LevelManifest>();
                manifest.levels = new LevelData[0];
                AssetDatabase.CreateAsset(manifest, ManifestResourcesPath);
            }
            return manifest;
        }

        private void SyncFallbackResourcesLevels(List<LevelData> sourceLevels, bool clearPrevious)
        {
            EnsureFolderExists(RuntimeFallbackFolder);
            if (clearPrevious)
            {
                string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { RuntimeFallbackFolder });
                var rx = new Regex(@"[/\\]Level_\d+\.asset$", RegexOptions.IgnoreCase);
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!rx.IsMatch(path)) continue;
                    AssetDatabase.DeleteAsset(path);
                }
            }

            for (int i = 0; i < sourceLevels.Count; i++)
            {
                LevelData src = sourceLevels[i];
                if (src == null) continue;
                int id = i + 1;
                string assetPath = $"{RuntimeFallbackFolder}/Level_{id}.asset";
                if (AssetDatabase.LoadAssetAtPath<LevelData>(assetPath) != null)
                    AssetDatabase.DeleteAsset(assetPath);

                var clone = Object.Instantiate(src);
                clone.levelId = id;
                AssetDatabase.CreateAsset(clone, assetPath);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelBakeTool] Synced fallback resources: {sourceLevels.Count} level assets in {RuntimeFallbackFolder}.");
        }

        private static void ApplySwitchOverride(LevelData level, bool includeSwitch, int seed)
        {
            if (level?.nodes == null || level.nodes.Length == 0) return;

            if (!includeSwitch)
            {
                for (int i = 0; i < level.nodes.Length; i++)
                {
                    if (level.nodes[i].nodeType == NodeType.Switch)
                    {
                        level.nodes[i].nodeType = NodeType.Bulb;
                        level.nodes[i].switchGroupId = 0;
                    }
                }
                return;
            }

            bool hasSwitch = false;
            for (int i = 0; i < level.nodes.Length; i++)
            {
                if (level.nodes[i].nodeType == NodeType.Switch)
                {
                    hasSwitch = true;
                    break;
                }
            }
            if (hasSwitch) return;

            var rng = new System.Random(seed ^ 0x5f3759df);
            int pick = rng.Next(level.nodes.Length);
            level.nodes[pick].nodeType = NodeType.Switch;
            level.nodes[pick].switchGroupId = 1;
        }

        private static void CountFailReason(Dictionary<string, int> counter, string reason)
        {
            if (counter == null || string.IsNullOrEmpty(reason)) return;
            if (!counter.TryGetValue(reason, out int count)) count = 0;
            counter[reason] = count + 1;
        }

        private static string BuildTopFailReasonsSummary(Dictionary<string, int> counter, int attempts, int topK)
        {
            if (counter == null || counter.Count == 0 || attempts <= 0) return "none";
            var ranked = counter
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .Take(Mathf.Max(1, topK))
                .ToList();
            var parts = new List<string>(ranked.Count);
            for (int i = 0; i < ranked.Count; i++)
            {
                float ratio = ranked[i].Value * 100f / attempts;
                parts.Add($"{ranked[i].Key}:{ranked[i].Value}({ratio:F1}%)");
            }
            return string.Join(", ", parts);
        }
    }
}
#endif
