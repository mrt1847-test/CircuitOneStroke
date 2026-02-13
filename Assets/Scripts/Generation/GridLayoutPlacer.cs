using System;
using System.Collections.Generic;
using CircuitOneStroke.Data;
using UnityEngine;

namespace CircuitOneStroke.Generation
{
    /// <summary>
    /// Snap node positions to a grid and optimize with local random swap.
    /// Topology (edge list) is unchanged; only node coordinates are moved.
    /// </summary>
    public static class GridLayoutPlacer
    {
        public struct Options
        {
            public int MinGridSize;
            public int MaxGridSize;
            public bool EnableStaggeredRows;
            public int SwapAttemptsMin;
            public int SwapAttemptsMax;
            public int TimeBudgetMs;
            public float Padding;
            public float LongEdgeFactor;
            public float ClearanceThreshold;
            public float AngleThresholdDeg;
            public float CrossingWeight;
            public float LongEdgeWeight;
            public float ClearanceWeight;
            public float AngleCrowdingWeight;
            public string LayoutName;

            public static Options Default => new Options
            {
                MinGridSize = 6,
                MaxGridSize = 10,
                EnableStaggeredRows = true,
                SwapAttemptsMin = 500,
                SwapAttemptsMax = 2000,
                TimeBudgetMs = 12,
                Padding = 0.06f,
                LongEdgeFactor = 1.65f,
                ClearanceThreshold = 0.50f,
                AngleThresholdDeg = 26f,
                CrossingWeight = 220f,
                LongEdgeWeight = 85f,
                ClearanceWeight = 150f,
                AngleCrowdingWeight = 90f,
                LayoutName = null
            };
        }

        public struct LayoutScore
        {
            public int crossingCount;
            public float longEdgeShare;
            public int clearanceViolations;
            public float angleCrowding;
            public float total;
        }

        private struct CandidateResult
        {
            public Vector2[] snapped;
            public LayoutScore score;
            public bool valid;
            public int cols;
            public int rows;
            public bool staggered;
        }

        public static bool TrySnapAndOptimize(
            IReadOnlyList<Vector2> initialPositions,
            IReadOnlyList<EdgeData> edges,
            System.Random rng,
            Options options,
            out Vector2[] snappedPositions,
            out LayoutScore before,
            out LayoutScore after)
        {
            snappedPositions = null;
            before = default;
            after = default;
            if (initialPositions == null || edges == null) return false;
            int n = initialPositions.Count;
            if (n <= 0) return false;

            options.MinGridSize = Mathf.Clamp(options.MinGridSize, 2, 32);
            options.MaxGridSize = Mathf.Clamp(options.MaxGridSize, options.MinGridSize, 32);

            var baseline = new Vector2[n];
            for (int i = 0; i < n; i++) baseline[i] = initialPositions[i];
            before = Evaluate(edges, baseline, options);

            CandidateResult best = default;
            for (int g = options.MinGridSize; g <= options.MaxGridSize; g++)
            {
                var regular = EvaluateGridCandidate(baseline, edges, rng, options, g, g, staggered: false);
                if (regular.valid && (!best.valid || regular.score.total < best.score.total))
                    best = regular;

                if (options.EnableStaggeredRows)
                {
                    var staggered = EvaluateGridCandidate(baseline, edges, rng, options, g, g, staggered: true);
                    if (staggered.valid && (!best.valid || staggered.score.total < best.score.total))
                        best = staggered;
                }
            }

            if (!best.valid)
                return false;

            snappedPositions = best.snapped;
            after = best.score;
            return true;
        }

        private static CandidateResult EvaluateGridCandidate(
            Vector2[] initialPositions,
            IReadOnlyList<EdgeData> edges,
            System.Random rng,
            Options options,
            int cols,
            int rows,
            bool staggered)
        {
            int n = initialPositions.Length;
            var cells = BuildGridCells(initialPositions, cols, rows, staggered, options.Padding);
            if (cells.Count < n)
                return default;

            var assignment = BuildInitialAssignment(initialPositions, cells, options.LayoutName);
            var snapped = ApplyAssignment(assignment, cells);
            var current = Evaluate(edges, snapped, options);

            int attempts = Mathf.Clamp(n * 70, options.SwapAttemptsMin, options.SwapAttemptsMax);
            int budget = Mathf.Max(0, options.TimeBudgetMs);
            long startTicks = DateTime.UtcNow.Ticks;
            long budgetTicks = budget * TimeSpan.TicksPerMillisecond;

            for (int i = 0; i < attempts; i++)
            {
                if (budget > 0 && DateTime.UtcNow.Ticks - startTicks > budgetTicks)
                    break;
                int a = rng.Next(n);
                int b = rng.Next(n);
                if (a == b) continue;

                (assignment[a], assignment[b]) = (assignment[b], assignment[a]);
                var candidatePos = ApplyAssignment(assignment, cells);
                var candidateScore = Evaluate(edges, candidatePos, options);
                if (candidateScore.total + 1e-5f < current.total)
                {
                    snapped = candidatePos;
                    current = candidateScore;
                }
                else
                {
                    (assignment[a], assignment[b]) = (assignment[b], assignment[a]);
                }
            }

            return new CandidateResult
            {
                snapped = snapped,
                score = current,
                valid = true,
                cols = cols,
                rows = rows,
                staggered = staggered
            };
        }

        private static List<Vector2> BuildGridCells(
            IReadOnlyList<Vector2> positions,
            int cols,
            int rows,
            bool staggered,
            float paddingRatio)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < positions.Count; i++)
            {
                var p = positions[i];
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }

            float width = Mathf.Max(0.8f, maxX - minX);
            float height = Mathf.Max(0.8f, maxY - minY);
            float padX = width * Mathf.Clamp01(paddingRatio);
            float padY = height * Mathf.Clamp01(paddingRatio);
            minX -= padX;
            maxX += padX;
            minY -= padY;
            maxY += padY;
            width = Mathf.Max(0.8f, maxX - minX);
            height = Mathf.Max(0.8f, maxY - minY);

            float cellW = cols > 1 ? width / (cols - 1f) : width;
            float cellH = rows > 1 ? height / (rows - 1f) : height;
            float rowOffset = staggered ? cellW * 0.5f : 0f;

            var cells = new List<Vector2>(cols * rows);
            for (int y = 0; y < rows; y++)
            {
                float yPos = minY + (rows <= 1 ? 0f : y * cellH);
                float xOffset = (staggered && (y % 2 == 1)) ? rowOffset : 0f;
                for (int x = 0; x < cols; x++)
                {
                    float xPos = minX + (cols <= 1 ? 0f : x * cellW) + xOffset;
                    if (xPos > maxX + rowOffset) continue;
                    cells.Add(new Vector2(xPos, yPos));
                }
            }
            return cells;
        }

        private static int[] BuildNearestAssignment(IReadOnlyList<Vector2> positions, IReadOnlyList<Vector2> cells)
        {
            int n = positions.Count;
            var assignment = new int[n];
            var used = new bool[cells.Count];
            var order = new List<int>(n);
            for (int i = 0; i < n; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                float ra = positions[a].sqrMagnitude;
                float rb = positions[b].sqrMagnitude;
                return rb.CompareTo(ra);
            });

            for (int oi = 0; oi < n; oi++)
            {
                int node = order[oi];
                float best = float.MaxValue;
                int bestCell = -1;
                for (int c = 0; c < cells.Count; c++)
                {
                    if (used[c]) continue;
                    float d = (positions[node] - cells[c]).sqrMagnitude;
                    if (d < best)
                    {
                        best = d;
                        bestCell = c;
                    }
                }
                if (bestCell < 0) bestCell = 0;
                used[bestCell] = true;
                assignment[node] = bestCell;
            }

            return assignment;
        }

        private static int[] BuildInitialAssignment(IReadOnlyList<Vector2> positions, IReadOnlyList<Vector2> cells, string layoutName)
        {
            if (IsRingLikeLayout(layoutName))
            {
                var ring = BuildRingLikeAssignment(positions, cells);
                if (ring != null) return ring;
            }
            return BuildNearestAssignment(positions, cells);
        }

        private static bool IsRingLikeLayout(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName)) return false;
            return layoutName == "Ring"
                || layoutName == "DoubleRing"
                || layoutName == "ConcentricPolygon"
                || layoutName == "PentagonSpiral"
                || layoutName == "TwoCluster"
                || layoutName == "StarSpoke";
        }

        private static int[] BuildRingLikeAssignment(IReadOnlyList<Vector2> positions, IReadOnlyList<Vector2> cells)
        {
            int n = positions.Count;
            if (n == 0 || cells.Count < n) return null;

            Vector2 center = Vector2.zero;
            for (int i = 0; i < n; i++) center += positions[i];
            center /= n;

            float avgRadius = 0f;
            for (int i = 0; i < n; i++) avgRadius += Vector2.Distance(positions[i], center);
            avgRadius /= Mathf.Max(1, n);

            var nodeOrder = new List<(int id, float angle)>(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 d = positions[i] - center;
                float a = Mathf.Atan2(d.y, d.x);
                nodeOrder.Add((i, a));
            }
            nodeOrder.Sort((a, b) => a.angle.CompareTo(b.angle));

            var candidateCells = new List<(int id, float angle, float radius, float delta)>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2 d = cells[i] - center;
                float r = d.magnitude;
                float a = Mathf.Atan2(d.y, d.x);
                float delta = Mathf.Abs(r - avgRadius);
                candidateCells.Add((i, a, r, delta));
            }

            candidateCells.Sort((a, b) =>
            {
                int c = a.delta.CompareTo(b.delta);
                if (c != 0) return c;
                return a.angle.CompareTo(b.angle);
            });

            int take = Mathf.Min(candidateCells.Count, Mathf.Max(n, n + n / 2));
            var ringBand = new List<(int id, float angle)>(take);
            for (int i = 0; i < take; i++)
                ringBand.Add((candidateCells[i].id, candidateCells[i].angle));
            ringBand.Sort((a, b) => a.angle.CompareTo(b.angle));

            if (ringBand.Count < n)
                return null;

            var assignment = new int[n];
            for (int i = 0; i < n; i++)
            {
                int nodeId = nodeOrder[i].id;
                int bandIndex = (i * ringBand.Count) / n;
                assignment[nodeId] = ringBand[bandIndex].id;
            }
            return assignment;
        }

        private static Vector2[] ApplyAssignment(IReadOnlyList<int> assignment, IReadOnlyList<Vector2> cells)
        {
            var outPos = new Vector2[assignment.Count];
            for (int i = 0; i < assignment.Count; i++)
            {
                int cellIndex = Mathf.Clamp(assignment[i], 0, cells.Count - 1);
                outPos[i] = cells[cellIndex];
            }
            return outPos;
        }

        private static LayoutScore Evaluate(IReadOnlyList<EdgeData> edges, IReadOnlyList<Vector2> positions, Options options)
        {
            int n = positions.Count;
            int crossings = AestheticEvaluator.CountCrossings(edges, positions, n);

            float avgLen = Mathf.Max(0.001f, AestheticEvaluator.AverageEdgeLength(edges, positions));
            float longThreshold = avgLen * Mathf.Max(1.01f, options.LongEdgeFactor);
            int longEdges = 0;
            int edgeCount = 0;
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                if (e.a < 0 || e.b < 0 || e.a >= n || e.b >= n || e.a == e.b) continue;
                edgeCount++;
                float len = Vector2.Distance(positions[e.a], positions[e.b]);
                if (len > longThreshold) longEdges++;
            }
            float longEdgeShare = edgeCount > 0 ? longEdges / (float)edgeCount : 0f;

            int clearanceViolations = 0;
            float clearanceThreshold = Mathf.Max(0.05f, options.ClearanceThreshold);
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                if (e.a < 0 || e.b < 0 || e.a >= n || e.b >= n || e.a == e.b) continue;
                Vector2 a = positions[e.a];
                Vector2 b = positions[e.b];
                for (int node = 0; node < n; node++)
                {
                    if (node == e.a || node == e.b) continue;
                    float d = AestheticEvaluator.DistancePointToSegment(positions[node], a, b);
                    if (d < clearanceThreshold)
                        clearanceViolations++;
                }
            }

            var adjacency = new List<int>[n];
            for (int i = 0; i < n; i++) adjacency[i] = new List<int>();
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                if (e.a < 0 || e.b < 0 || e.a >= n || e.b >= n || e.a == e.b) continue;
                if (!adjacency[e.a].Contains(e.b)) adjacency[e.a].Add(e.b);
                if (!adjacency[e.b].Contains(e.a)) adjacency[e.b].Add(e.a);
            }

            float angleThreshold = Mathf.Clamp(options.AngleThresholdDeg, 4f, 179f);
            float angleCrowding = 0f;
            for (int node = 0; node < n; node++)
            {
                var neighbors = adjacency[node];
                if (neighbors.Count < 2) continue;
                var angles = new List<float>(neighbors.Count);
                Vector2 center = positions[node];
                for (int i = 0; i < neighbors.Count; i++)
                {
                    Vector2 dir = positions[neighbors[i]] - center;
                    float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    if (a < 0f) a += 360f;
                    angles.Add(a);
                }
                angles.Sort();
                for (int i = 0; i < angles.Count; i++)
                {
                    float cur = angles[i];
                    float next = i == angles.Count - 1 ? angles[0] + 360f : angles[i + 1];
                    float gap = next - cur;
                    if (gap < angleThreshold)
                        angleCrowding += (angleThreshold - gap) / angleThreshold;
                }
            }

            float total = crossings * options.CrossingWeight
                + longEdgeShare * options.LongEdgeWeight
                + clearanceViolations * options.ClearanceWeight
                + angleCrowding * options.AngleCrowdingWeight;

            return new LayoutScore
            {
                crossingCount = crossings,
                longEdgeShare = longEdgeShare,
                clearanceViolations = clearanceViolations,
                angleCrowding = angleCrowding,
                total = total
            };
        }
    }
}
