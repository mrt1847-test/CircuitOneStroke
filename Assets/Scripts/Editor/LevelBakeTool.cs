#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
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
        private bool _useGridRangeGenerator;
        private int _minNodes = 16;
        private int _maxNodes = 25;
        private int _maxStatesExpandedBudget = 200000;
        private int _maxMillisBudget = 100;
        private int _maxSolutionsBudget = 50;
        private bool _advancedFoldout;
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
            _useGridRangeGenerator = EditorGUILayout.Toggle("Use Grid Range (16–25 nodes)", _useGridRangeGenerator);
            if (_useGridRangeGenerator)
            {
                EditorGUI.indentLevel++;
                _minNodes = Mathf.Clamp(EditorGUILayout.IntField("Min Nodes", _minNodes), 16, 25);
                _maxNodes = Mathf.Clamp(EditorGUILayout.IntField("Max Nodes", _maxNodes), 16, 25);
                if (_minNodes > _maxNodes) _maxNodes = _minNodes;
                _maxStatesExpandedBudget = EditorGUILayout.IntField("Max States Expanded", _maxStatesExpandedBudget);
                _maxMillisBudget = EditorGUILayout.IntField("Max Millis (per level)", _maxMillisBudget);
                _maxSolutionsBudget = EditorGUILayout.IntField("Max Solutions (budget)", _maxSolutionsBudget);
                EditorGUI.indentLevel--;
            }
            else
                _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            _useIncludeSwitchOverride = EditorGUILayout.Toggle("Override Include Switch", _useIncludeSwitchOverride);
            if (_useIncludeSwitchOverride)
                _includeSwitchOverride = EditorGUILayout.Toggle("  Include Switch", _includeSwitchOverride);

            _advancedFoldout = EditorGUILayout.Foldout(_advancedFoldout, "Advanced filter parameters");
            if (_advancedFoldout)
            {
                EditorGUI.indentLevel++;
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

            EditorGUILayout.EndScrollView();
        }

        private bool PassesFilterCustom(DifficultyTier tier, SolverResult result)
        {
            if (!result.solvable || result.solutionCount < _solutionCountMin)
                return false;
            int maxCount = tier == DifficultyTier.Easy ? _solutionCountMaxEasy : tier == DifficultyTier.Medium ? _solutionCountMaxMedium : _solutionCountMaxHard;
            if (result.solutionCount > maxCount)
                return false;
            float branchMin = tier == DifficultyTier.Easy ? _earlyBranchingMinEasy : tier == DifficultyTier.Medium ? _earlyBranchingMinMedium : _earlyBranchingMinHard;
            if (result.earlyBranching < branchMin)
                return false;
            float depthMin = tier == DifficultyTier.Easy ? _deadEndDepthMinEasy : tier == DifficultyTier.Medium ? _deadEndDepthMinMedium : _deadEndDepthMinHard;
            float depthMax = tier == DifficultyTier.Easy ? _deadEndDepthMaxEasy : tier == DifficultyTier.Medium ? _deadEndDepthMaxMedium : _deadEndDepthMaxHard;
            if (result.deadEndDepthAvg < depthMin || result.deadEndDepthAvg > depthMax)
                return false;
            return true;
        }

        private void DoGenerateAndSave()
        {
            if (_useGridRangeGenerator)
            {
                DoGenerateAndSaveGridRange();
                return;
            }

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

            while (savedLevels.Count < _targetCount)
            {
                var genResult = LevelGenerator.GenerateWithMetadata(_tier, seed, includeSwitch);
                var result = LevelSolver.Solve(genResult.level, LevelSolver.MaxSolutionsDefault, LevelSolver.MaxStatesExpandedDefault);

                bool pass = useCustomFilter ? PassesFilterCustom(_tier, result) : LevelGenerator.PassesFilter(_tier, result);
                if (pass)
                {
                    genResult.level.levelId = levelId;
                    string assetPath = $"{_outputFolder}/Level_{levelId}.asset";
                    AssetDatabase.CreateAsset(genResult.level, assetPath);

                    string metaPath = $"{_outputFolder}/Level_{levelId}_meta.json";
                    string metaJson = $"{{\"seed\":{seed},\"templateName\":\"{genResult.templateName}\",\"tier\":\"{_tier}\",\"solutionCount\":{result.solutionCount},\"nodesExpanded\":{result.nodesExpanded},\"earlyBranching\":{result.earlyBranching:F2},\"deadEndDepthAvg\":{result.deadEndDepthAvg:F2}}}";
                    File.WriteAllText(metaPath, metaJson);

                    savedLevels.Add(genResult.level);
                    levelId++;
                    Debug.Log($"Saved level {savedLevels.Count}: seed={seed}, template={genResult.templateName}, solutions={result.solutionCount}");
                }
                else
                {
                    Object.DestroyImmediate(genResult.level);
                }
                seed++;
            }

            LevelManifest manifest = EnsureManifest();
            if (manifest != null)
            {
                manifest.levels = savedLevels.ToArray();
                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssets();
            }
            AssetDatabase.Refresh();
            Debug.Log($"Level Bake complete: {savedLevels.Count} levels saved to {_outputFolder}. Manifest at {ManifestResourcesPath} (Resources).");
        }

        private void DoGenerateAndSaveGridRange()
        {
            EnsureFolderExists("Assets/Resources");
            EnsureFolderExists("Assets/Resources/Levels");
            string tierFolder = $"Assets/Resources/Levels/Generated/{_tier}";
            EnsureFolderExists("Assets/Resources/Levels/Generated");
            EnsureFolderExists(tierFolder);

            var savedLevels = new List<LevelData>();
            int seed = _seedStart;
            int levelId = 1;

            while (savedLevels.Count < _targetCount)
            {
                var level = GridRangeGenerator.Generate(_minNodes, _maxNodes, _tier, seed);
                var result = LevelSolver.Solve(level, _maxSolutionsBudget, _maxStatesExpandedBudget, _maxMillisBudget);

                bool pass = result.solvableWithinBudget && result.solutionsFoundWithinBudget >= 1;
                if (result.status == SolverStatus.BudgetExceeded && result.solutionsFoundWithinBudget == 0)
                    pass = false;

                if (pass)
                {
                    level.levelId = levelId;
                    string assetPath = $"{tierFolder}/Level_{levelId}.asset";
                    AssetDatabase.CreateAsset(level, assetPath);

                    int n = level.nodes != null ? level.nodes.Length : 0;
                    var (rows, cols) = GridRangeGenerator.GetGridSize(n);
                    string metaJson = $"{{\"seed\":{seed},\"tier\":\"{_tier}\",\"nodeCount\":{n},\"gridRows\":{rows},\"gridCols\":{cols},\"solutionsFoundWithinBudget\":{result.solutionsFoundWithinBudget},\"nodesExpanded\":{result.nodesExpanded},\"earlyBranchingApprox\":{result.earlyBranchingApprox:F2},\"deadEndDepthAvgApprox\":{result.deadEndDepthAvgApprox:F2},\"status\":\"{result.status}\"}}";
                    string metaPath = $"{tierFolder}/Level_{levelId}_meta.json";
                    File.WriteAllText(metaPath, metaJson);

                    savedLevels.Add(level);
                    levelId++;
                    Debug.Log($"Saved Grid level {savedLevels.Count}: N={n}, seed={seed}, solutionsFound={result.solutionsFoundWithinBudget}, expanded={result.nodesExpanded}");
                }
                else
                {
                    Object.DestroyImmediate(level);
                }
                seed++;
            }

            LevelManifest manifest = EnsureManifestForTier(_tier);
            if (manifest != null)
            {
                manifest.levels = savedLevels.ToArray();
                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssets();
            }
            AssetDatabase.Refresh();
            Debug.Log($"Grid Range Bake complete: {savedLevels.Count} levels saved to {tierFolder}. Manifest at {GetManifestPathForTier(_tier)} (Resources).");
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

        private static string GetManifestPathForTier(DifficultyTier tier)
        {
            return $"Assets/Resources/Levels/GeneratedLevelManifest_{tier}.asset";
        }

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

        private LevelManifest EnsureManifestForTier(DifficultyTier tier)
        {
            EnsureFolderExists("Assets/Resources");
            EnsureFolderExists("Assets/Resources/Levels");
            string path = GetManifestPathForTier(tier);
            var manifest = AssetDatabase.LoadAssetAtPath<LevelManifest>(path);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<LevelManifest>();
                manifest.levels = new LevelData[0];
                AssetDatabase.CreateAsset(manifest, path);
            }
            return manifest;
        }
    }
}
#endif
