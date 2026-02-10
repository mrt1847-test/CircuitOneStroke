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
    /// Level Factory v2: Generate 16–25 node levels with SolverV2 + BackboneFirstGenerator,
    /// score/filter, save to Resources and update LevelManifest. Heavy work in Editor only.
    /// </summary>
    public class LevelBakeWindow : EditorWindow
    {
        private string _packName = "V2Pack";
        private int _countToGenerate = 50;
        private int _seed = 1;
        private int _nodeCountMin = 16;
        private int _nodeCountMax = 25;
        private DifficultyTier _difficulty = DifficultyTier.Medium;
        private int _targetSolutionsMin = 2;
        private int _targetSolutionsMax = 5;
        private int _solverTimeBudgetMs = 150;
        private string _outputFolder = "";
        private int _maxAttemptsPerLevel = 200;
        private int _decisionPointsMin = 2;
        private int _decisionPointsMax = 6;
        private float _avgBranchingMax = 2.3f;
        private float _avgTrapDepthMin = 3f;
        private bool _requireTrapDepth = false;
        private int _maxCrossings = 1;
        private Vector2 _scrollPos;

        private int _attempted;
        private int _passed;
        private int _rejectedUnsat;
        private int _rejectedTimeout;
        private int _rejectedTooFewSolutions;
        private int _rejectedTooManySolutions;
        private int _rejectedMetrics;
        private int _rejectedAesthetics;

        private const string ManifestResourcesPath = "Assets/Resources/Levels/GeneratedLevelManifest.asset";
        private const string GeneratedBasePath = "Assets/Resources/Levels/Generated";

        [MenuItem("Tools/Circuit One-Stroke/Level Bake")]
        public static void ShowWindow()
        {
            var win = GetWindow<LevelBakeWindow>("Level Bake");
            win.minSize = new Vector2(360, 420);
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_outputFolder))
                _outputFolder = $"{GeneratedBasePath}/{_packName}";
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.LabelField("Level Bake (Factory v2)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _packName = EditorGUILayout.TextField("Pack Name", _packName);
            _countToGenerate = Mathf.Max(1, EditorGUILayout.IntField("Count to Generate", _countToGenerate));
            EditorGUILayout.BeginHorizontal();
            _seed = EditorGUILayout.IntField("Seed", _seed);
            if (GUILayout.Button("Randomize", GUILayout.Width(80)))
                _seed = Random.Range(0, 1000000);
            EditorGUILayout.EndHorizontal();

            _nodeCountMin = Mathf.Clamp(EditorGUILayout.IntField("Node Count Min", _nodeCountMin), 16, 25);
            _nodeCountMax = Mathf.Clamp(EditorGUILayout.IntField("Node Count Max", _nodeCountMax), 16, 25);
            if (_nodeCountMin > _nodeCountMax) _nodeCountMax = _nodeCountMin;

            _difficulty = (DifficultyTier)EditorGUILayout.EnumPopup("Difficulty", _difficulty);
            _targetSolutionsMin = Mathf.Clamp(EditorGUILayout.IntField("Target Solutions Min", _targetSolutionsMin), 1, 10);
            _targetSolutionsMax = Mathf.Clamp(EditorGUILayout.IntField("Target Solutions Max", _targetSolutionsMax), 1, 10);
            if (_targetSolutionsMin > _targetSolutionsMax) _targetSolutionsMax = _targetSolutionsMin;

            _solverTimeBudgetMs = Mathf.Max(50, EditorGUILayout.IntField("Solver Time Budget (ms)", _solverTimeBudgetMs));
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            if (string.IsNullOrEmpty(_outputFolder))
                _outputFolder = $"{GeneratedBasePath}/{_packName}";

            _maxAttemptsPerLevel = Mathf.Max(1, EditorGUILayout.IntField("Max Attempts Per Level", _maxAttemptsPerLevel));
            _decisionPointsMin = EditorGUILayout.IntField("Decision Points Min", _decisionPointsMin);
            _decisionPointsMax = EditorGUILayout.IntField("Decision Points Max", _decisionPointsMax);
            _avgBranchingMax = EditorGUILayout.FloatField("Avg Branching Max", _avgBranchingMax);
            _requireTrapDepth = EditorGUILayout.Toggle("Require Avg Trap Depth", _requireTrapDepth);
            _avgTrapDepthMin = EditorGUILayout.FloatField("Avg Trap Depth Min", _avgTrapDepthMin);
            _maxCrossings = EditorGUILayout.IntField("Max Crossings (aesthetics)", _maxCrossings);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Bake", GUILayout.Height(32)))
                DoBake(false);
            if (GUILayout.Button("Dry Run (no save)", GUILayout.Height(24)))
                DoBake(true);
            if (GUILayout.Button("Open Output Folder", GUILayout.Height(22)))
                OpenOutputFolder();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Last run stats", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Attempted: {_attempted}, Passed: {_passed}");
            EditorGUILayout.LabelField($"Rejected: Unsat={_rejectedUnsat}, Timeout={_rejectedTimeout}, TooFew={_rejectedTooFewSolutions}, TooMany={_rejectedTooManySolutions}, Metrics={_rejectedMetrics}, Aesthetics={_rejectedAesthetics}");

            EditorGUILayout.EndScrollView();
        }

        /// <summary>TODO: Optional validation — "switch matters" (freeze gateMask, disable toggles → Unsat or lower count); "diodes contribute" (remove diodes → count&gt;cap or metrics drop).</summary>
        private bool PassesAcceptance(LevelData level, SolverOutcome outcome)
        {
            if (outcome.Status == SolverV2Status.Timeout) return false;
            if (outcome.Status == SolverV2Status.Unsat) return false;
            if (outcome.SolutionsFoundCapped < _targetSolutionsMin) return false;
            if (outcome.SolutionsFoundCapped > _targetSolutionsMax && outcome.SolutionsFoundCapped != _targetSolutionsMax + 1) return false;
            if (outcome.SolutionsFoundCapped == _targetSolutionsMax + 1) return false; // over cap
            if (outcome.DecisionPoints < _decisionPointsMin || outcome.DecisionPoints > _decisionPointsMax) return false;
            if (outcome.AvgBranching > _avgBranchingMax) return false;
            if (_requireTrapDepth && outcome.AvgTrapDepth >= 0 && outcome.AvgTrapDepth < _avgTrapDepthMin) return false;
            if (!AestheticEvaluator.Accept(level, _maxCrossings, 0.22f, 0.4f)) return false;
            return true;
        }

        private void DoBake(bool dryRun)
        {
            _attempted = 0;
            _passed = 0;
            _rejectedUnsat = 0;
            _rejectedTimeout = 0;
            _rejectedTooFewSolutions = 0;
            _rejectedTooManySolutions = 0;
            _rejectedMetrics = 0;
            _rejectedAesthetics = 0;

            var settings = new SolverSettings
            {
                MaxSolutionsCap = _targetSolutionsMax + 1,
                TimeBudgetMs = _solverTimeBudgetMs,
                RequireAllBulbsVisited = true,
                TreatAllNodesAsUnique = true,
                ComputeExtraMetrics = true,
                TrapProbeDepthCap = 20
            };

            var savedLevels = new List<LevelData>();
            int levelIndex = 1;
            int baseSeed = _seed;

            for (int i = 0; i < _countToGenerate; i++)
            {
                bool accepted = false;
                for (int attempt = 0; attempt < _maxAttemptsPerLevel && !accepted; attempt++)
                {
                    _attempted++;
                    var p = new GenerateParams
                    {
                        NodeCountMin = _nodeCountMin,
                        NodeCountMax = _nodeCountMax,
                        TargetSolutionsMin = _targetSolutionsMin,
                        TargetSolutionsMax = _targetSolutionsMax,
                        Difficulty = _difficulty,
                        TargetAvgDegreeMin = 2.4f,
                        TargetAvgDegreeMax = 3.2f,
                        Seed = baseSeed + i * 1000 + attempt
                    };
                    LevelData candidate = null;
                    try
                    {
                        candidate = BackboneFirstGenerator.Generate(p);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Generate failed: {ex.Message}");
                        continue;
                    }
                    var outcome = LevelSolverV2.Evaluate(candidate, settings);

                    if (outcome.Status == SolverV2Status.Unsat) { _rejectedUnsat++; Object.DestroyImmediate(candidate); continue; }
                    if (outcome.Status == SolverV2Status.Timeout) { _rejectedTimeout++; Object.DestroyImmediate(candidate); continue; }
                    if (outcome.SolutionsFoundCapped < _targetSolutionsMin) { _rejectedTooFewSolutions++; Object.DestroyImmediate(candidate); continue; }
                    if (outcome.SolutionsFoundCapped > _targetSolutionsMax) { _rejectedTooManySolutions++; Object.DestroyImmediate(candidate); continue; }
                    if (outcome.DecisionPoints < _decisionPointsMin || outcome.DecisionPoints > _decisionPointsMax || outcome.AvgBranching > _avgBranchingMax)
                    { _rejectedMetrics++; Object.DestroyImmediate(candidate); continue; }
                    if (_requireTrapDepth && outcome.AvgTrapDepth >= 0 && outcome.AvgTrapDepth < _avgTrapDepthMin)
                    { _rejectedMetrics++; Object.DestroyImmediate(candidate); continue; }
                    if (!AestheticEvaluator.Accept(candidate, _maxCrossings, 0.22f, 0.4f))
                    { _rejectedAesthetics++; Object.DestroyImmediate(candidate); continue; }

                    accepted = true;
                    _passed++;
                    candidate.levelId = levelIndex;
                    if (!dryRun)
                    {
                        EnsureFolderExists(_outputFolder);
                        string assetName = $"Level_{_packName}_{levelIndex}_{p.Seed}";
                        string assetPath = $"{_outputFolder}/{assetName}.asset";
                        AssetDatabase.CreateAsset(candidate, assetPath);
                        savedLevels.Add(candidate);
                    }
                    else
                    {
                        Object.DestroyImmediate(candidate);
                    }
                    levelIndex++;
                    break;
                }
                if (!accepted && (i + 1) <= _countToGenerate)
                    Debug.LogWarning($"Could not generate level {i + 1} after {_maxAttemptsPerLevel} attempts.");
            }

            if (!dryRun && savedLevels.Count > 0)
            {
                LevelManifest manifest = EnsureManifest();
                if (manifest != null)
                {
                    var existing = manifest.levels != null ? new List<LevelData>(manifest.levels) : new List<LevelData>();
                    existing.AddRange(savedLevels);
                    manifest.levels = existing.ToArray();
                    EditorUtility.SetDirty(manifest);
                    AssetDatabase.SaveAssets();
                }
                AssetDatabase.Refresh();
                Debug.Log($"Level Bake complete: {savedLevels.Count} levels saved to {_outputFolder}. Manifest: {ManifestResourcesPath}");
            }
            else if (dryRun)
            {
                Debug.Log($"Dry Run: Passed {_passed} of {_attempted} attempts.");
            }
        }

        private void OpenOutputFolder()
        {
            string path = _outputFolder;
            if (path.StartsWith("Assets/"))
                path = Path.Combine(Application.dataPath, "..", path);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
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
    }
}
#endif
