using System;
using System.Collections.Generic;
using System.Diagnostics;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Solver
{
    /// <summary>V2 solver outcome: feasibility, capped solution count, and metrics for 16–25 nodes.</summary>
    public enum SolverV2Status
    {
        Unsat,
        Feasible,
        Timeout
    }

    /// <summary>Result of LevelSolverV2.Evaluate. Editor-safe.</summary>
    public struct SolverOutcome
    {
        public SolverV2Status Status;
        /// <summary>0..cap, or cap+1 meaning "more than cap".</summary>
        public int SolutionsFoundCapped;
        /// <summary>One solution path (node indices). Empty if none found.</summary>
        public List<int> OneSolutionPath;
        public int ExpandedStates;
        public int Backtracks;
        public int DecisionPoints;
        public float AvgBranching;
        public int ForcedMovesCount;
        public int FirstSolutionDepth;
        public int GatesUsedCount;
        public int SwitchesUsedCount;
        public int DiodeEdgesUsedCount;
        /// <summary>Average depth until failure when probing alternative moves at decision points. &lt; 0 if not computed.</summary>
        public float AvgTrapDepth;
    }

    /// <summary>Solver settings for V2 evaluation. Time budget and caps.</summary>
    public struct SolverSettings
    {
        public int MaxSolutionsCap;
        public int TimeBudgetMs;
        public bool RequireAllBulbsVisited;
        public bool TreatAllNodesAsUnique;
        public bool ComputeExtraMetrics;
        public int TrapProbeDepthCap;

        public static SolverSettings Default => new SolverSettings
        {
            MaxSolutionsCap = 5,
            TimeBudgetMs = 150,
            RequireAllBulbsVisited = true,
            TreatAllNodesAsUnique = true,
            ComputeExtraMetrics = true,
            TrapProbeDepthCap = 20
        };
    }

    /// <summary>Edge reference for move generation: target index, edge id, gate/direction info.</summary>
    public struct EdgeRef
    {
        public int To;           // node index 0..N-1
        public int EdgeId;
        public int GateGroupId;
        /// <summary>Bit index in gate mask for this edge; -1 if not gated.</summary>
        public int GateBit;
        public bool Directed;
        public int DirFrom;      // node index allowed "from"
        public int DirTo;        // node index allowed "to"
    }

    /// <summary>
    /// Solver V2: 16–25 nodes, fast feasibility, capped solution count, pruning, time budget.
    /// Editor-safe (no UnityEngine except optional Debug.Log in Editor).
    /// </summary>
    public static class LevelSolverV2
    {
        public const int MaxNodesSupported = 25;
        public const int MaxGateBits = 32;
        private const int BudgetCheckInterval = 256;

        /// <summary>Evaluate level: feasibility, capped solutions, metrics. Winning = visit all Bulb nodes exactly once (no revisits).</summary>
        public static SolverOutcome Evaluate(LevelData level, SolverSettings settings)
        {
            var outcome = new SolverOutcome
            {
                Status = SolverV2Status.Unsat,
                SolutionsFoundCapped = 0,
                OneSolutionPath = new List<int>(),
                AvgTrapDepth = -1f
            };

            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0 ||
                level.nodes.Length > MaxNodesSupported)
                return outcome;

            var ctx = BuildContext(level);
            if (ctx == null) return outcome;

            var sw = Stopwatch.StartNew();
            int cap = Math.Max(1, settings.MaxSolutionsCap);

            // Pass 1: find one solution
            var (feasible, path) = FindOneSolution(ctx, settings, sw);
            if (!feasible)
            {
                outcome.Status = ctx.TimedOut ? SolverV2Status.Timeout : SolverV2Status.Unsat;
                return outcome;
            }

            outcome.Status = ctx.TimedOut ? SolverV2Status.Timeout : SolverV2Status.Feasible;
            outcome.OneSolutionPath = path ?? new List<int>();
            outcome.ExpandedStates = ctx.ExpandedStates;
            outcome.Backtracks = ctx.Backtracks;
            outcome.DecisionPoints = ctx.DecisionPoints;
            if (ctx.BranchingAtDecision != null && ctx.BranchingAtDecision.Count > 0)
            {
                int sum = 0;
                foreach (var b in ctx.BranchingAtDecision) sum += b;
                ctx.AvgBranching = (float)sum / ctx.BranchingAtDecision.Count;
            }
            outcome.AvgBranching = ctx.AvgBranching;
            outcome.ForcedMovesCount = ctx.ForcedMovesCount;
            outcome.FirstSolutionDepth = ctx.FirstSolutionDepth;
            outcome.GatesUsedCount = ctx.GatesUsedInSolution;
            outcome.SwitchesUsedCount = ctx.SwitchesUsedInSolution;
            outcome.DiodeEdgesUsedCount = ctx.DiodeEdgesUsedInSolution;

            if (ctx.TimedOut)
            {
                outcome.SolutionsFoundCapped = ctx.SolutionsFound;
                return outcome;
            }

            // Pass 2: capped count with memo
            ctx.ResetForCount();
            int count = CountSolutionsCapped(ctx, cap, settings, sw);
            outcome.SolutionsFoundCapped = count;

            if (settings.ComputeExtraMetrics && path != null && path.Count > 0 && !ctx.TimedOut)
            {
                float avgTrap = ComputeAvgTrapDepth(ctx, path, settings.TrapProbeDepthCap, settings.TimeBudgetMs, sw);
                if (avgTrap >= 0) outcome.AvgTrapDepth = avgTrap;
            }

            return outcome;
        }

        private sealed class SolverContext
        {
            public int N;
            public uint BulbMask;
            public List<EdgeRef>[] Adj;
            public int[] SwitchGroupPerNode;  // -1 or group id
            public uint InitialGateMask;
            public Dictionary<int, int> GroupIdToMask;  // groupId -> bit mask (bits for edges in that group)
            public int NumGateBits;

            public int ExpandedStates;
            public int Backtracks;
            public int DecisionPoints;
            public float AvgBranching;
            public int ForcedMovesCount;
            public int FirstSolutionDepth;
            public int GatesUsedInSolution;
            public int SwitchesUsedInSolution;
            public int DiodeEdgesUsedInSolution;
            public int SolutionsFound;
            public bool TimedOut;
            public int TimeBudgetMs;
            public long StartTicks;
            public Stopwatch Stopwatch;

            public List<int> BranchingAtDecision = new List<int>();
            public HashSet<(int pos, uint visited, uint gate)> Memo = new HashSet<(int, uint, uint)>();
            public Dictionary<(int pos, uint visited, uint gate), int> CountMemo = new Dictionary<(int, uint, uint), int>();

            public void ResetForCount()
            {
                SolutionsFound = 0;
                ExpandedStates = 0;
                Backtracks = 0;
                DecisionPoints = 0;
                BranchingAtDecision.Clear();
                CountMemo.Clear();
            }
        }

        private static SolverContext BuildContext(LevelData level)
        {
            int n = level.nodes.Length;
            var nodeIdToIndex = new Dictionary<int, int>();
            var indexToNodeId = new int[n];
            uint bulbMask = 0;
            var switchGroupPerNode = new int[n];

            for (int i = 0; i < n; i++)
            {
                var nd = level.nodes[i];
                nodeIdToIndex[nd.id] = i;
                indexToNodeId[i] = nd.id;
                if (nd.nodeType == NodeType.Bulb)
                    bulbMask |= 1u << i;
                switchGroupPerNode[i] = nd.nodeType == NodeType.Switch ? nd.switchGroupId : -1;
            }

            // Gate bits: one bit per gated edge (same as LevelSolver). Cap at 32.
            var gateEdgeIds = new List<int>();
            foreach (var e in level.edges)
                if (e.gateGroupId >= 0) gateEdgeIds.Add(e.id);
            gateEdgeIds.Sort();
            if (gateEdgeIds.Count > MaxGateBits) return null;
            var edgeIdToGateBit = new Dictionary<int, int>();
            for (int i = 0; i < gateEdgeIds.Count; i++)
                edgeIdToGateBit[gateEdgeIds[i]] = i;

            uint initialGateMask = 0;
            var groupIdToMask = new Dictionary<int, int>();
            foreach (var e in level.edges)
            {
                if (e.gateGroupId >= 0 && edgeIdToGateBit.TryGetValue(e.id, out int bit))
                {
                    if (e.initialGateOpen)
                        initialGateMask |= 1u << bit;
                    if (!groupIdToMask.ContainsKey(e.gateGroupId))
                        groupIdToMask[e.gateGroupId] = 0;
                    groupIdToMask[e.gateGroupId] |= 1 << bit;
                }
            }

            var adj = new List<EdgeRef>[n];
            for (int i = 0; i < n; i++) adj[i] = new List<EdgeRef>();

            foreach (var e in level.edges)
            {
                if (!nodeIdToIndex.TryGetValue(e.a, out int ia) || !nodeIdToIndex.TryGetValue(e.b, out int ib))
                    continue;
                bool directed = e.diode != DiodeMode.None;
                int dirFrom = e.diode == DiodeMode.AtoB ? ia : (e.diode == DiodeMode.BtoA ? ib : ia);
                int dirTo = e.diode == DiodeMode.AtoB ? ib : (e.diode == DiodeMode.BtoA ? ia : ib);
                int gid = e.gateGroupId;
                int bit = e.gateGroupId >= 0 && edgeIdToGateBit.TryGetValue(e.id, out int b) ? b : -1;

                var refA = new EdgeRef { To = ib, EdgeId = e.id, GateGroupId = gid, GateBit = bit, Directed = directed, DirFrom = dirFrom, DirTo = dirTo };
                var refB = new EdgeRef { To = ia, EdgeId = e.id, GateGroupId = gid, GateBit = bit, Directed = directed, DirFrom = dirFrom, DirTo = dirTo };
                adj[ia].Add(refA);
                adj[ib].Add(refB);
            }

            return new SolverContext
            {
                N = n,
                BulbMask = bulbMask,
                Adj = adj,
                SwitchGroupPerNode = switchGroupPerNode,
                InitialGateMask = initialGateMask,
                GroupIdToMask = groupIdToMask,
                NumGateBits = gateEdgeIds.Count
            };
        }

        private static bool CheckBudget(SolverContext ctx)
        {
            if (ctx.TimeBudgetMs <= 0) return true;
            long elapsed = ctx.Stopwatch != null ? ctx.Stopwatch.ElapsedMilliseconds : (Environment.TickCount - ctx.StartTicks);
            if (elapsed >= ctx.TimeBudgetMs)
            {
                ctx.TimedOut = true;
                return false;
            }
            return true;
        }

        private static uint ToggleGroup(SolverContext ctx, uint gateMask, int groupId)
        {
            if (groupId < 0 || ctx.GroupIdToMask == null) return gateMask;
            int mask = ctx.GroupIdToMask.TryGetValue(groupId, out int m) ? m : 0;
            return (uint)((int)gateMask ^ mask);
        }

        private static bool GateOpen(SolverContext ctx, uint gateMask, EdgeRef e)
        {
            if (e.GateBit < 0) return true;
            return (gateMask & (1u << e.GateBit)) != 0;
        }

        /// <summary>Get allowed moves from state, ordered: forced first, then to unvisited bulbs, then by lower exit count.</summary>
        private static List<(int to, EdgeRef e)> GetAllowedMoves(SolverContext ctx, int pos, uint visited, uint gateMask)
        {
            var moves = new List<(int to, EdgeRef e)>();
            foreach (var e in ctx.Adj[pos])
            {
                if ((visited & (1u << e.To)) != 0) continue;
                if (e.GateGroupId >= 0 && !GateOpen(ctx, gateMask, e)) continue;
                if (e.Directed && pos != e.DirFrom) continue;
                moves.Add((e.To, e));
            }
            if (moves.Count == 0) return moves;

            // Order: forced first; then prefer unvisited bulbs; then by fewer exits (heuristic).
            int forced = moves.Count == 1 ? 1 : 0;
            moves.Sort((a, b) =>
            {
                bool aBulb = (ctx.BulbMask & (1u << a.to)) != 0;
                bool bBulb = (ctx.BulbMask & (1u << b.to)) != 0;
                if (aBulb != bBulb) return aBulb ? -1 : 1;
                int exitsA = CountExits(ctx, a.to, visited, gateMask);
                int exitsB = CountExits(ctx, b.to, visited, gateMask);
                return exitsA.CompareTo(exitsB);
            });
            return moves;
        }

        private static int CountExits(SolverContext ctx, int from, uint visited, uint gateMask)
        {
            int c = 0;
            foreach (var e in ctx.Adj[from])
            {
                if ((visited & (1u << e.To)) != 0) continue;
                if (e.GateGroupId >= 0 && !GateOpen(ctx, gateMask, e)) continue;
                if (e.Directed && from != e.DirFrom) continue;
                c++;
            }
            return c;
        }

        private static bool Prune(SolverContext ctx, int pos, uint visited, uint gateMask)
        {
            uint unvisitedBulbs = ctx.BulbMask & ~visited;
            if (unvisitedBulbs == 0) return false;

            // (1) Dead required node: any unvisited bulb with 0 potential incoming
            for (int i = 0; i < ctx.N; i++)
            {
                if ((unvisitedBulbs & (1u << i)) == 0) continue;
                int incoming = 0;
                foreach (var e in ctx.Adj[i])
                {
                    if ((visited & (1u << e.To)) != 0) continue;
                    if (e.GateGroupId >= 0 && !GateOpen(ctx, gateMask, e)) continue;
                    if (e.Directed && e.To != e.DirFrom) continue;
                    incoming++;
                }
                if (incoming == 0) return true;
            }

            // (2) Reachability: BFS from pos over unvisited (+ current), all unvisited bulbs reachable?
            var reachable = new bool[ctx.N];
            var q = new Queue<int>();
            q.Enqueue(pos);
            reachable[pos] = true;
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                foreach (var e in ctx.Adj[u])
                {
                    if ((visited & (1u << e.To)) != 0 && e.To != pos) continue;
                    if (e.GateGroupId >= 0 && !GateOpen(ctx, gateMask, e)) continue;
                    if (e.Directed && u != e.DirFrom) continue;
                    if (!reachable[e.To]) { reachable[e.To] = true; q.Enqueue(e.To); }
                }
            }
            for (int i = 0; i < ctx.N; i++)
                if ((unvisitedBulbs & (1u << i)) != 0 && !reachable[i])
                    return true;

            // (3) Forced endpoints: unvisited bulbs with degree 1 in remaining graph
            int forcedEndpoints = 0;
            for (int i = 0; i < ctx.N; i++)
            {
                if ((unvisitedBulbs & (1u << i)) == 0) continue;
                int deg = 0;
                foreach (var e in ctx.Adj[i])
                {
                    if ((visited & (1u << e.To)) != 0) continue;
                    if (e.GateGroupId >= 0 && !GateOpen(ctx, gateMask, e)) continue;
                    if (e.Directed && e.To != e.DirFrom) continue;
                    deg++;
                }
                if (deg == 1) forcedEndpoints++;
            }
            if (forcedEndpoints > 4) return true;

            return false;
        }

        private static (bool feasible, List<int> path) FindOneSolution(SolverContext ctx, SolverSettings settings, Stopwatch sw)
        {
            ctx.Stopwatch = sw;
            ctx.StartTicks = Environment.TickCount;
            ctx.TimeBudgetMs = settings.TimeBudgetMs;
            ctx.ExpandedStates = 0;
            ctx.Backtracks = 0;
            ctx.DecisionPoints = 0;
            ctx.BranchingAtDecision.Clear();
            ctx.ForcedMovesCount = 0;
            ctx.FirstSolutionDepth = -1;
            ctx.GatesUsedInSolution = 0;
            ctx.SwitchesUsedInSolution = 0;
            ctx.DiodeEdgesUsedInSolution = 0;
            ctx.SolutionsFound = 0;
            ctx.TimedOut = false;

            uint allVisited = (1u << ctx.N) - 1;
            bool requireAllBulbs = settings.RequireAllBulbsVisited;
            uint goalMask = requireAllBulbs ? ctx.BulbMask : allVisited;
            var path = new List<int>();

            for (int start = 0; start < ctx.N && !ctx.TimedOut; start++)
            {
                uint visited = 1u << start;
                uint gate = ctx.InitialGateMask;
                if (ctx.SwitchGroupPerNode[start] >= 0)
                    gate = ToggleGroup(ctx, gate, ctx.SwitchGroupPerNode[start]);
                path.Clear();
                path.Add(start);
                if (DfsFindOne(ctx, start, visited, gate, 0, path, goalMask, allVisited, requireAllBulbs, settings))
                    return (true, new List<int>(path));
            }
            return (false, null);
        }

        private static bool DfsFindOne(SolverContext ctx, int pos, uint visited, uint gateMask, int depth,
            List<int> path, uint goalMask, uint allVisited, bool requireAllBulbs, SolverSettings settings)
        {
            ctx.ExpandedStates++;
            if (ctx.ExpandedStates % BudgetCheckInterval == 0 && !CheckBudget(ctx))
                return false;

            bool goal = requireAllBulbs ? ((visited & goalMask) == goalMask) : (visited == allVisited);
            if (goal)
            {
                ctx.SolutionsFound = 1;
                ctx.FirstSolutionDepth = depth;
                TrackSolutionMetrics(ctx, path);
                return true;
            }

            if (Prune(ctx, pos, visited, gateMask)) { ctx.Backtracks++; return false; }

            var moves = GetAllowedMoves(ctx, pos, visited, gateMask);
            if (moves.Count == 0) { ctx.Backtracks++; return false; }
            if (moves.Count == 1) ctx.ForcedMovesCount++;
            else
            {
                ctx.DecisionPoints++;
                ctx.BranchingAtDecision.Add(moves.Count);
            }

            foreach (var (to, e) in moves)
            {
                uint newVisited = visited | (1u << to);
                uint newGate = gateMask;
                if (ctx.SwitchGroupPerNode[to] >= 0)
                    newGate = ToggleGroup(ctx, gateMask, ctx.SwitchGroupPerNode[to]);
                path.Add(to);
                if (DfsFindOne(ctx, to, newVisited, newGate, depth + 1, path, goalMask, allVisited, requireAllBulbs, settings))
                    return true;
                path.RemoveAt(path.Count - 1);
                ctx.Backtracks++;
            }
            return false;
        }

        private static void TrackSolutionMetrics(SolverContext ctx, List<int> path)
        {
            ctx.GatesUsedInSolution = 0;
            ctx.SwitchesUsedInSolution = 0;
            ctx.DiodeEdgesUsedInSolution = 0;
            for (int i = 0; i < path.Count; i++)
                if (ctx.SwitchGroupPerNode[path[i]] >= 0) ctx.SwitchesUsedInSolution++;
            for (int i = 0; i < path.Count - 1; i++)
            {
                int a = path[i], b = path[i + 1];
                foreach (var e in ctx.Adj[a])
                {
                    if (e.To != b) continue;
                    if (e.GateBit >= 0) ctx.GatesUsedInSolution++;
                    if (e.Directed) ctx.DiodeEdgesUsedInSolution++;
                    break;
                }
            }
        }

        private static int CountSolutionsCapped(SolverContext ctx, int cap, SolverSettings settings, Stopwatch sw)
        {
            ctx.Stopwatch = sw;
            ctx.TimeBudgetMs = settings.TimeBudgetMs;
            ctx.CountMemo.Clear();
            uint allVisited = (1u << ctx.N) - 1;
            uint goalMask = settings.RequireAllBulbsVisited ? ctx.BulbMask : allVisited;

            for (int start = 0; start < ctx.N && !ctx.TimedOut; start++)
            {
                uint visited = 1u << start;
                uint gate = ctx.InitialGateMask;
                if (ctx.SwitchGroupPerNode[start] >= 0)
                    gate = ToggleGroup(ctx, gate, ctx.SwitchGroupPerNode[start]);
                int add = DfsCount(ctx, start, visited, gate, goalMask, allVisited, cap, settings);
                ctx.SolutionsFound += add;
                if (ctx.SolutionsFound > cap) return cap + 1;
            }
            return ctx.SolutionsFound;
        }

        private static int DfsCount(SolverContext ctx, int pos, uint visited, uint gateMask, uint goalMask, uint allVisited, int cap, SolverSettings settings)
        {
            var key = (pos, visited, gateMask);
            if (ctx.CountMemo.TryGetValue(key, out int cached))
                return cached;

            ctx.ExpandedStates++;
            if (ctx.ExpandedStates % BudgetCheckInterval == 0 && !CheckBudget(ctx))
                return 0;

            bool goal = settings.RequireAllBulbsVisited ? ((visited & goalMask) == goalMask) : (visited == allVisited);
            if (goal)
            {
                ctx.CountMemo[key] = 1;
                return 1;
            }

            if (Prune(ctx, pos, visited, gateMask))
            {
                ctx.CountMemo[key] = 0;
                return 0;
            }

            var moves = GetAllowedMoves(ctx, pos, visited, gateMask);
            if (moves.Count == 0)
            {
                ctx.CountMemo[key] = 0;
                return 0;
            }
            if (moves.Count > 1) ctx.DecisionPoints++;

            int total = 0;
            foreach (var (to, e) in moves)
            {
                uint newVisited = visited | (1u << to);
                uint newGate = gateMask;
                if (ctx.SwitchGroupPerNode[to] >= 0)
                    newGate = ToggleGroup(ctx, gateMask, ctx.SwitchGroupPerNode[to]);
                int count = DfsCount(ctx, to, newVisited, newGate, goalMask, allVisited, cap, settings);
                total += count;
                if (total > cap)
                {
                    ctx.CountMemo[key] = cap + 1;
                    return cap + 1;
                }
            }
            ctx.CountMemo[key] = total;
            return total;
        }

        private static float ComputeAvgTrapDepth(SolverContext ctx, List<int> solutionPath, int depthCap, int timeBudgetMs, Stopwatch sw)
        {
            if (solutionPath == null || solutionPath.Count < 2) return -1f;
            var depths = new List<int>();
            uint visited = 1u << solutionPath[0];
            uint gate = ctx.InitialGateMask;
            if (ctx.SwitchGroupPerNode[solutionPath[0]] >= 0)
                gate = ToggleGroup(ctx, gate, ctx.SwitchGroupPerNode[solutionPath[0]]);

            for (int step = 0; step < solutionPath.Count - 1 && depths.Count < 20; step++)
            {
                if (sw.ElapsedMilliseconds >= timeBudgetMs * 2 / 3) break;
                int pos = solutionPath[step];
                var moves = GetAllowedMoves(ctx, pos, visited, gate);
                if (moves.Count <= 1) continue;
                int solutionNext = solutionPath[step + 1];
                foreach (var (to, e) in moves)
                {
                    if (to == solutionNext) continue;
                    uint nv = visited | (1u << to);
                    uint ng = gate;
                    if (ctx.SwitchGroupPerNode[to] >= 0)
                        ng = ToggleGroup(ctx, gate, ctx.SwitchGroupPerNode[to]);
                    int d = TrapProbeDepth(ctx, to, nv, ng, depthCap, sw, timeBudgetMs);
                    if (d >= 0) depths.Add(d);
                    if (depths.Count >= 10) break;
                }
                int next = solutionPath[step + 1];
                visited |= 1u << next;
                if (ctx.SwitchGroupPerNode[next] >= 0)
                    gate = ToggleGroup(ctx, gate, ctx.SwitchGroupPerNode[next]);
            }
            if (depths.Count == 0) return -1f;
            float sum = 0;
            foreach (var d in depths) sum += d;
            return sum / depths.Count;
        }

        private static int TrapProbeDepth(SolverContext ctx, int pos, uint visited, uint gateMask, int depthCap, Stopwatch sw, int timeBudgetMs)
        {
            if (depthCap <= 0) return 0;
            for (int d = 0; d < depthCap; d++)
            {
                if (sw.ElapsedMilliseconds >= timeBudgetMs) return -1;
                var moves = GetAllowedMoves(ctx, pos, visited, gateMask);
                if (moves.Count == 0) return d;
                if (moves.Count > 1) return d;
                var (to, e) = moves[0];
                visited |= 1u << to;
                if (ctx.SwitchGroupPerNode[to] >= 0)
                    gateMask = ToggleGroup(ctx, gateMask, ctx.SwitchGroupPerNode[to]);
                pos = to;
            }
            return depthCap;
        }

        /// <summary>Derive initial gate mask from LevelData (per-edge initialGateOpen). Rule: for each gate group, use majority open; if tie, first edge wins.</summary>
        public static uint GetInitialGateMaskFromLevel(LevelData level, Dictionary<int, int> edgeIdToGateBit, Dictionary<int, int> groupIdToMask)
        {
            if (level?.edges == null || edgeIdToGateBit == null) return 0;
            uint mask = 0;
            var groupOpenCount = new Dictionary<int, int>();
            var groupCloseCount = new Dictionary<int, int>();
            foreach (var e in level.edges)
            {
                if (e.gateGroupId < 0 || !edgeIdToGateBit.TryGetValue(e.id, out int bit)) continue;
                if (e.initialGateOpen)
                    groupOpenCount[e.gateGroupId] = groupOpenCount.GetValueOrDefault(e.gateGroupId) + 1;
                else
                    groupCloseCount[e.gateGroupId] = groupCloseCount.GetValueOrDefault(e.gateGroupId) + 1;
            }
            foreach (var e in level.edges)
            {
                if (e.gateGroupId < 0 || !edgeIdToGateBit.TryGetValue(e.id, out int bit)) continue;
                int open = groupOpenCount.GetValueOrDefault(e.gateGroupId);
                int close = groupCloseCount.GetValueOrDefault(e.gateGroupId);
                bool openDefault = open >= close;
                if (openDefault && (mask & (1u << bit)) == 0)
                    mask |= 1u << bit;
                else if (!openDefault)
                    mask &= ~(1u << bit);
            }
            return mask;
        }
    }
}
