using System;
using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;
using Random = System.Random;

namespace CircuitOneStroke.Generation
{
    /// <summary>
    /// Grid-based level generator for N=16..25. Uses 4x4, 5x4, or 5x5 grids with serpentine
    /// active-cell selection so at least one Hamiltonian path (the backbone) is guaranteed.
    /// </summary>
    public static class GridRangeGenerator
    {
        public const int MinNodesDefault = 16;
        public const int MaxNodesDefault = 25;
        private const float CellSize = 1.2f;
        private const float JitterFraction = 0.03f;
        private const int GateTradeoffRetries = 15;

        /// <summary>Returns (rows, cols) for grid. 4x4 (16), 5x4 (20), 5x5 (25).</summary>
        public static (int rows, int cols) GetGridSize(int nodeCount)
        {
            if (nodeCount <= 16) return (4, 4);
            if (nodeCount <= 20) return (5, 4);
            return (5, 5);
        }

        /// <summary>Serpentine order over grid: row0 L→R, row1 R→L, etc. Returns list of (row, col).</summary>
        public static List<(int r, int c)> SerpentineOrder(int rows, int cols)
        {
            var order = new List<(int, int)>();
            for (int r = 0; r < rows; r++)
            {
                if (r % 2 == 0)
                    for (int c = 0; c < cols; c++)
                        order.Add((r, c));
                else
                    for (int c = cols - 1; c >= 0; c--)
                        order.Add((r, c));
            }
            return order;
        }

        /// <summary>Grid cell center in world space. Origin at grid center.</summary>
        public static Vector2 CellToWorld(int row, int col, int rows, int cols)
        {
            float x = (col - (cols - 1) * 0.5f) * CellSize;
            float y = ((rows - 1) * 0.5f - row) * CellSize;
            return new Vector2(x, y);
        }

        /// <summary>Generate LevelData for 16..25 nodes with grid layout and backbone guarantee.</summary>
        public static LevelData Generate(int minNodes = MinNodesDefault, int maxNodes = MaxNodesDefault,
            DifficultyTier tier = DifficultyTier.Medium, int seed = 0)
        {
            var rng = new Random(seed);
            int N = rng.Next(minNodes, maxNodes + 1);
            var (rows, cols) = GetGridSize(N);
            int totalSlots = rows * cols;
            N = Mathf.Clamp(N, 1, totalSlots);

            var serpentine = SerpentineOrder(rows, cols);
            var activeCells = new List<(int r, int c)>();
            for (int i = 0; i < N; i++)
                activeCells.Add(serpentine[i]);

            // (r,c) -> nodeId [0..N-1]
            var cellToNode = new Dictionary<(int r, int c), int>();
            for (int i = 0; i < activeCells.Count; i++)
                cellToNode[activeCells[i]] = i;

            // Positions with optional jitter
            float jitterMax = CellSize * JitterFraction;
            var positions = new Vector2[N];
            for (int i = 0; i < N; i++)
            {
                var (r, c) = activeCells[i];
                var pos = CellToWorld(r, c, rows, cols);
                pos.x += (float)(rng.NextDouble() * 2 - 1) * jitterMax;
                pos.y += (float)(rng.NextDouble() * 2 - 1) * jitterMax;
                positions[i] = pos;
            }

            // 4-neighborhood edges (only between active cells)
            var edgeSet = new HashSet<(int a, int b)>();
            int[] dr = { 0, 0, 1, -1 };
            int[] dc = { 1, -1, 0, 0 };
            for (int i = 0; i < N; i++)
            {
                var (r, c) = activeCells[i];
                for (int d = 0; d < 4; d++)
                {
                    int nr = r + dr[d], nc = c + dc[d];
                    if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                    if (!cellToNode.TryGetValue((nr, nc), out int j)) continue;
                    int a = Mathf.Min(i, j);
                    int b = Mathf.Max(i, j);
                    edgeSet.Add((a, b));
                }
            }

            var edgeList = new List<(int a, int b)>(edgeSet);

            // Backbone: edges (0,1), (1,2), ... (N-2, N-1) — must stay traversable
            var backboneSet = new HashSet<(int a, int b)>();
            for (int i = 0; i < N - 1; i++)
                backboneSet.Add((i, i + 1));

            // Diodes: Easy 0..1, Medium 1..3, Hard 2..5. Prefer non-backbone.
            var nonBackboneEdges = new List<(int a, int b)>();
            foreach (var e in edgeList)
                if (!backboneSet.Contains(e)) nonBackboneEdges.Add(e);
            int diodeCount = tier == DifficultyTier.Easy ? rng.Next(0, 2)
                : tier == DifficultyTier.Medium ? rng.Next(1, 4) : rng.Next(2, 6);
            diodeCount = Mathf.Min(diodeCount, edgeList.Count);
            var chosenDiodes = PickRandomEdgesPreferNonBackbone(edgeList, nonBackboneEdges, backboneSet, diodeCount, rng);
            var diodeDir = new Dictionary<(int a, int b), bool>(); // true = AtoB (a->b)
            foreach (var (a, b) in chosenDiodes)
            {
                bool atob = backboneSet.Contains((a, b)) ? (b == a + 1) : rng.NextDouble() < 0.5;
                diodeDir[(a, b)] = atob;
            }

            // Switch: N<=18 optional (Medium/Hard), N>18 max 1. Pick one node (not 0 or N-1 for simplicity)
            bool includeSwitch = tier != DifficultyTier.Easy && (N <= 18 ? rng.NextDouble() < 0.7 : true);
            int switchNodeId = -1;
            if (includeSwitch && N >= 2)
                switchNodeId = rng.Next(1, N); // 1..N-1

            // Gates: 3..6 Medium, 5..10 Hard. gateGroupId=1. Only on non-backbone. Tradeoff.
            int gateCount = tier == DifficultyTier.Easy ? 0
                : tier == DifficultyTier.Medium ? rng.Next(3, 7) : rng.Next(5, 11);
            gateCount = Mathf.Min(gateCount, nonBackboneEdges.Count);
            var chosenGates = new List<(int a, int b)>();
            bool[] initialOpen = Array.Empty<bool>();
            for (int k = 0; k < GateTradeoffRetries; k++)
            {
                chosenGates = PickRandomSubset(nonBackboneEdges, gateCount, rng);
                initialOpen = new bool[chosenGates.Count];
                for (int i = 0; i < chosenGates.Count; i++)
                    initialOpen[i] = tier == DifficultyTier.Medium ? rng.NextDouble() < 0.5 : rng.NextDouble() < 0.4;
                if (gateCount == 0 || !includeSwitch) break;
                bool hasOpen = false, hasClosed = false;
                for (int i = 0; i < chosenGates.Count; i++)
                {
                    if (initialOpen[i]) hasOpen = true; else hasClosed = true;
                }
                bool afterOpen = false, afterClosed = false;
                for (int i = 0; i < chosenGates.Count; i++)
                {
                    if (!initialOpen[i]) afterOpen = true; else afterClosed = true;
                }
                if (hasOpen && hasClosed && afterOpen && afterClosed) break;
            }

            // Build EdgeData list
            int edgeId = 0;
            var outEdges = new List<EdgeData>();
            foreach (var (a, b) in edgeList)
            {
                var e = new EdgeData { id = edgeId++, a = a, b = b };
                if (diodeDir.TryGetValue((a, b), out bool atob))
                    e.diode = atob ? DiodeMode.AtoB : DiodeMode.BtoA;
                for (int i = 0; i < chosenGates.Count; i++)
                {
                    if ((chosenGates[i].a == a && chosenGates[i].b == b) || (chosenGates[i].a == b && chosenGates[i].b == a))
                    {
                        e.gateGroupId = 1;
                        e.initialGateOpen = initialOpen[i];
                        break;
                    }
                }
                outEdges.Add(e);
            }

            // Build NodeData list
            var nodes = new List<NodeData>();
            for (int i = 0; i < N; i++)
            {
                nodes.Add(new NodeData
                {
                    id = i,
                    pos = positions[i],
                    nodeType = i == switchNodeId ? NodeType.Switch : NodeType.Bulb,
                    switchGroupId = i == switchNodeId ? 1 : 0
                });
            }

            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelId = 0;
            level.nodes = nodes.ToArray();
            level.edges = outEdges.ToArray();
            return level;
        }

        private static List<(int a, int b)> PickRandomEdgesPreferNonBackbone(
            List<(int a, int b)> all, List<(int a, int b)> nonBackbone,
            HashSet<(int a, int b)> backbone, int count, Random rng)
        {
            var result = new List<(int a, int b)>();
            if (count <= 0) return result;
            if (nonBackbone.Count >= count)
            {
                result = PickRandomSubset(nonBackbone, count, rng);
                return result;
            }
            result.AddRange(PickRandomSubset(nonBackbone, nonBackbone.Count, rng));
            int need = count - result.Count;
            var backboneList = new List<(int a, int b)>(backbone);
            var extra = PickRandomSubset(backboneList, Mathf.Min(need, backboneList.Count), rng);
            result.AddRange(extra);
            return result;
        }

        private static List<(int a, int b)> PickRandomSubset(List<(int a, int b)> source, int count, Random rng)
        {
            if (count >= source.Count) return new List<(int a, int b)>(source);
            var indices = new List<int>();
            for (int i = 0; i < source.Count; i++) indices.Add(i);
            for (int i = 0; i < count; i++)
            {
                int j = rng.Next(i, indices.Count);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            var result = new List<(int a, int b)>();
            for (int i = 0; i < count; i++)
                result.Add(source[indices[i]]);
            return result;
        }
    }
}
