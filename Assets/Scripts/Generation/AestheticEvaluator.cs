using System.Collections.Generic;
using UnityEngine;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Generation
{
    /// <summary>
    /// Evaluates layout aesthetics: edge crossings, min node distance, edge length consistency.
    /// Reject ugly layouts to keep levels human-designed and visually consistent.
    /// </summary>
    public static class AestheticEvaluator
    {
        /// <summary>
        /// Default: prefer 0 crossings, allow up to 1.
        /// </summary>
        public const int MaxCrossingsDefault = 1;

        /// <summary>
        /// Min node distance as fraction of layout scale (average edge length). Default 0.25.
        /// </summary>
        public const float MinNodeDistanceScaleDefault = 0.25f;

        /// <summary>
        /// Edge length coefficient of variation (std/mean) threshold. Default 0.35.
        /// </summary>
        public const float MaxEdgeLengthCVDefault = 0.35f;

        /// <summary>
        /// Compute number of edge crossings (segment-segment, ignoring shared endpoints).
        /// positions[nodeId] = position; nodeCount is number of nodes (ids 0..nodeCount-1).
        /// </summary>
        public static int CountCrossings(IReadOnlyList<EdgeData> edges, IReadOnlyList<Vector2> positions, int nodeCount = 0)
        {
            if (edges == null || positions == null) return 0;
            int n = nodeCount > 0 ? nodeCount : positions.Count;
            var posByNode = new Dictionary<int, Vector2>();
            for (int i = 0; i < n && i < positions.Count; i++)
                posByNode[i] = positions[i];
            int crossings = 0;
            for (int i = 0; i < edges.Count; i++)
            {
                if (!posByNode.TryGetValue(edges[i].a, out Vector2 a1) || !posByNode.TryGetValue(edges[i].b, out Vector2 b1))
                    continue;
                for (int j = i + 1; j < edges.Count; j++)
                {
                    if (!posByNode.TryGetValue(edges[j].a, out Vector2 a2) || !posByNode.TryGetValue(edges[j].b, out Vector2 b2))
                        continue;
                    if (SegmentsIntersectExclusive(a1, b1, a2, b2))
                        crossings++;
                }
            }
            return crossings;
        }

        /// <summary>
        /// Segments (a1,b1) and (a2,b2) intersect at an interior point (no shared endpoints).
        /// </summary>
        private static bool SegmentsIntersectExclusive(Vector2 a1, Vector2 b1, Vector2 a2, Vector2 b2)
        {
            if (ShareEndpoint(a1, b1, a2, b2)) return false;
            return SegmentIntersect(a1, b1, a2, b2);
        }

        private static bool ShareEndpoint(Vector2 a1, Vector2 b1, Vector2 a2, Vector2 b2)
        {
            const float eps = 1e-5f;
            if (Vector2.Distance(a1, a2) < eps || Vector2.Distance(a1, b2) < eps ||
                Vector2.Distance(b1, a2) < eps || Vector2.Distance(b1, b2) < eps)
                return true;
            return false;
        }

        private static bool SegmentIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float d1 = Cross2(p3 - p1, p2 - p1);
            float d2 = Cross2(p4 - p1, p2 - p1);
            float d3 = Cross2(p1 - p3, p4 - p3);
            float d4 = Cross2(p2 - p3, p4 - p3);
            if (Mathf.Approximately(d1, 0) || Mathf.Approximately(d2, 0) || Mathf.Approximately(d3, 0) || Mathf.Approximately(d4, 0))
                return false;
            return (d1 > 0 != d2 > 0) && (d3 > 0 != d4 > 0);
        }

        private static float Cross2(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>
        /// If segments (a1,b1) and (a2,b2) intersect at an interior point, returns true and sets out intersection.
        /// </summary>
        public static bool TryGetSegmentIntersection(Vector2 a1, Vector2 b1, Vector2 a2, Vector2 b2, out Vector2 intersection)
        {
            intersection = Vector2.zero;
            if (ShareEndpoint(a1, b1, a2, b2)) return false;
            Vector2 d1 = b1 - a1;
            Vector2 d2 = b2 - a2;
            float cross = d1.x * d2.y - d1.y * d2.x;
            if (Mathf.Approximately(cross, 0)) return false;
            Vector2 w = a1 - a2;
            float t = (w.x * d2.y - w.y * d2.x) / cross;
            float u = (w.x * d1.y - w.y * d1.x) / cross;
            if (t < 0 || t > 1 || u < 0 || u > 1) return false;
            intersection = a1 + t * d1;
            return true;
        }

        /// <summary>
        /// Get all edge crossing points (no shared endpoints). positions[nodeId], edges, nodeCount.
        /// </summary>
        public static List<Vector2> GetCrossingPoints(IReadOnlyList<EdgeData> edges, IReadOnlyList<Vector2> positions, int nodeCount)
        {
            var points = new List<Vector2>();
            if (edges == null || positions == null || nodeCount <= 0) return points;
            int n = Mathf.Min(nodeCount, positions.Count);
            var posByNode = new Dictionary<int, Vector2>();
            for (int i = 0; i < n; i++) posByNode[i] = positions[i];
            for (int i = 0; i < edges.Count; i++)
            {
                if (!posByNode.TryGetValue(edges[i].a, out Vector2 a1) || !posByNode.TryGetValue(edges[i].b, out Vector2 b1))
                    continue;
                for (int j = i + 1; j < edges.Count; j++)
                {
                    if (!posByNode.TryGetValue(edges[j].a, out Vector2 a2) || !posByNode.TryGetValue(edges[j].b, out Vector2 b2))
                        continue;
                    if (TryGetSegmentIntersection(a1, b1, a2, b2, out Vector2 pt))
                        points.Add(pt);
                }
            }
            return points;
        }

        /// <summary>
        /// Minimum distance between any two nodes. If nodeCount > 0, only consider first nodeCount positions.
        /// </summary>
        public static float MinNodeDistance(IReadOnlyList<Vector2> positions, int nodeCount = 0)
        {
            if (positions == null || positions.Count < 2) return float.MaxValue;
            int n = nodeCount > 0 ? Mathf.Min(nodeCount, positions.Count) : positions.Count;
            if (n < 2) return float.MaxValue;
            float min = float.MaxValue;
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    float d = Vector2.Distance(positions[i], positions[j]);
                    if (d < min) min = d;
                }
            return min;
        }

        /// <summary>
        /// Average edge length (for scale).
        /// </summary>
        public static float AverageEdgeLength(IReadOnlyList<EdgeData> edges, IReadOnlyList<Vector2> positions)
        {
            if (edges == null || edges.Count == 0 || positions == null) return 1f;
            float sum = 0;
            int count = 0;
            foreach (var e in edges)
            {
                if (e.a < positions.Count && e.b < positions.Count)
                {
                    sum += Vector2.Distance(positions[e.a], positions[e.b]);
                    count++;
                }
            }
            return count > 0 ? sum / count : 1f;
        }

        /// <summary>
        /// Edge length coefficient of variation (std dev / mean).
        /// </summary>
        public static float EdgeLengthCV(IReadOnlyList<EdgeData> edges, IReadOnlyList<Vector2> positions)
        {
            if (edges == null || edges.Count == 0 || positions == null) return 0f;
            var lengths = new List<float>();
            foreach (var e in edges)
            {
                if (e.a < positions.Count && e.b < positions.Count)
                    lengths.Add(Vector2.Distance(positions[e.a], positions[e.b]));
            }
            if (lengths.Count == 0) return 0f;
            float mean = 0;
            foreach (float L in lengths) mean += L;
            mean /= lengths.Count;
            float var = 0;
            foreach (float L in lengths) var += (L - mean) * (L - mean);
            float std = lengths.Count > 1 ? Mathf.Sqrt(var / (lengths.Count - 1)) : 0f;
            return mean > 1e-6f ? (std / mean) : 0f;
        }

        /// <summary>
        /// Default acceptance: crossings &lt;= 1, minNodeDistance &gt;= threshold (scale-based), edgeLengthCV &lt;= 0.35.
        /// </summary>
        public static bool Accept(
            IReadOnlyList<EdgeData> edges,
            IReadOnlyList<Vector2> positions,
            int nodeCount,
            int maxCrossings = MaxCrossingsDefault,
            float minNodeDistanceScale = MinNodeDistanceScaleDefault,
            float maxEdgeLengthCV = MaxEdgeLengthCVDefault)
        {
            if (positions == null || positions.Count == 0 || nodeCount <= 0) return false;
            int crossings = CountCrossings(edges, positions, nodeCount);
            float minDist = MinNodeDistance(positions, nodeCount);
            float avgLen = AverageEdgeLength(edges, positions);
            float threshold = Mathf.Max(0.4f, avgLen * minNodeDistanceScale);
            float cv = EdgeLengthCV(edges, positions);
            return crossings <= maxCrossings && minDist >= threshold && cv <= maxEdgeLengthCV;
        }

        /// <summary>
        /// Evaluate LevelData (uses node positions and edges). Convenience for generator.
        /// </summary>
        public static bool Accept(LevelData level, int maxCrossings = MaxCrossingsDefault, float minNodeDistanceScale = MinNodeDistanceScaleDefault, float maxEdgeLengthCV = MaxEdgeLengthCVDefault)
        {
            if (level?.nodes == null || level.edges == null) return false;
            int n = level.nodes.Length;
            var positions = new Vector2[n];
            foreach (var nd in level.nodes)
            {
                if (nd.id >= 0 && nd.id < n)
                    positions[nd.id] = nd.pos;
            }
            return Accept(level.edges, positions, n, maxCrossings, minNodeDistanceScale, maxEdgeLengthCV);
        }

        /// <summary>
        /// Per-node minimum angle (degrees) between outgoing edges; returns global minimum. Used for readability / touch separation.
        /// </summary>
        public static float MinAngleSeparationDeg(List<int>[] adj, IReadOnlyList<Vector2> positions, int nodeCount)
        {
            if (adj == null || positions == null || nodeCount <= 0) return 180f;
            float globalMin = 180f;
            int adjLen = Mathf.Min(adj.Length, nodeCount);
            for (int u = 0; u < adjLen; u++)
            {
                var neighbors = adj[u];
                if (neighbors == null || neighbors.Count < 2) continue;
                var angles = new List<float>(neighbors.Count);
                Vector2 pu = positions[u];
                foreach (int v in neighbors)
                {
                    if (v >= positions.Count) continue;
                    Vector2 d = positions[v] - pu;
                    float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    if (a < 0f) a += 360f;
                    angles.Add(a);
                }
                if (angles.Count < 2) continue;
                angles.Sort();
                float nodeMin = 360f;
                for (int i = 0; i < angles.Count; i++)
                {
                    float cur = angles[i];
                    float nxt = (i == angles.Count - 1) ? angles[0] + 360f : angles[i + 1];
                    float gap = nxt - cur;
                    if (gap < nodeMin) nodeMin = gap;
                }
                if (nodeMin < globalMin) globalMin = nodeMin;
            }
            return globalMin;
        }

        /// <summary>
        /// Minimum distance from any edge segment (excluding endpoints) to any third node. Used for readability penalty.
        /// </summary>
        public static float MinEdgeToNodeDistance(IReadOnlyList<EdgeData> edges, IReadOnlyList<Vector2> positions, int nodeCount)
        {
            if (edges == null || positions == null || nodeCount < 3) return float.MaxValue;
            float minD = float.MaxValue;
            for (int e = 0; e < edges.Count; e++)
            {
                int a = edges[e].a, b = edges[e].b;
                if (a >= positions.Count || b >= positions.Count) continue;
                Vector2 pa = positions[a], pb = positions[b];
                for (int k = 0; k < nodeCount; k++)
                {
                    if (k == a || k == b) continue;
                    if (k >= positions.Count) continue;
                    Vector2 pk = positions[k];
                    float t = Mathf.Clamp01(Vector2.Dot(pk - pa, pb - pa) / (Vector2.SqrMagnitude(pb - pa) + 1e-9f));
                    Vector2 closest = Vector2.Lerp(pa, pb, t);
                    float d = Vector2.Distance(pk, closest);
                    if (d < minD) minD = d;
                }
            }
            return minD;
        }

        /// <summary>
        /// Collect edge ids that likely need curved rendering due to crossings or node-clearance issues.
        /// </summary>
        public static HashSet<int> FindReadabilityProblemEdges(LevelData level, float clearanceThreshold)
        {
            var result = new HashSet<int>();
            if (level?.nodes == null || level.edges == null || level.nodes.Length == 0) return result;

            int n = level.nodes.Length;
            var positions = new Vector2[n];
            foreach (var nd in level.nodes)
            {
                if (nd.id >= 0 && nd.id < n)
                    positions[nd.id] = nd.pos;
            }

            // Crossing edges are visual trouble spots.
            for (int i = 0; i < level.edges.Length; i++)
            {
                var e1 = level.edges[i];
                if (e1.a < 0 || e1.a >= n || e1.b < 0 || e1.b >= n) continue;
                Vector2 a1 = positions[e1.a];
                Vector2 b1 = positions[e1.b];
                for (int j = i + 1; j < level.edges.Length; j++)
                {
                    var e2 = level.edges[j];
                    if (e2.a < 0 || e2.a >= n || e2.b < 0 || e2.b >= n) continue;
                    Vector2 a2 = positions[e2.a];
                    Vector2 b2 = positions[e2.b];
                    if (TryGetSegmentIntersection(a1, b1, a2, b2, out _))
                    {
                        result.Add(e1.id);
                        result.Add(e2.id);
                    }
                }
            }

            // Edge-to-node near misses are another source of touch/readability problems.
            for (int i = 0; i < level.edges.Length; i++)
            {
                var e = level.edges[i];
                if (e.a < 0 || e.a >= n || e.b < 0 || e.b >= n) continue;
                Vector2 pa = positions[e.a];
                Vector2 pb = positions[e.b];
                for (int k = 0; k < n; k++)
                {
                    if (k == e.a || k == e.b) continue;
                    float d = DistancePointToSegment(positions[k], pa, pb);
                    if (d < clearanceThreshold)
                    {
                        result.Add(e.id);
                        break;
                    }
                }
            }

            return result;
        }

        public static float DistancePointToSegment(Vector2 point, Vector2 segA, Vector2 segB)
        {
            Vector2 ab = segB - segA;
            float t = Mathf.Clamp01(Vector2.Dot(point - segA, ab) / (Vector2.SqrMagnitude(ab) + 1e-9f));
            Vector2 closest = Vector2.Lerp(segA, segB, t);
            return Vector2.Distance(point, closest);
        }

        // Touch/readability first; crossing weakened. Corridor and branch ratio penalties.
        private const float MinNodeSpacingDefault = 0.62f;
        private const float EdgeClearanceDefault = 0.50f;
        private const float AngleThresholdDegDefault = 28f;
        private const float CrossingPenaltyWeak = 8f;
        private const float SpacingViolationPenalty = 120f;
        private const float ClearanceViolationPenalty = 140f;
        private const float AngleViolationPenalty = 80f;
        private const float CorridorPenaltyPerUnit = 25f;
        private const float BranchLackPenalty = 20f;
        private const float MaxCrossingsCap = 8f;

        /// <summary>
        /// Readability-focused score: strong penalties for spacing, edge-node clearance, angle overlap; weak crossing; corridor and branch lack penalties.
        /// </summary>
        public static float ScoreReadability(IReadOnlyList<EdgeData> edges, IReadOnlyList<Vector2> positions, int nodeCount,
            float minNodeSpacing = MinNodeSpacingDefault, float edgeClearance = EdgeClearanceDefault, float angleThresholdDeg = AngleThresholdDegDefault)
        {
            if (edges == null || positions == null || nodeCount <= 0) return float.MinValue;
            int n = nodeCount;
            int crossings = CountCrossings(edges, positions, n);
            float minDist = MinNodeDistance(positions, n);
            float avgLen = AverageEdgeLength(edges, positions);
            if (avgLen < 0.01f) avgLen = 1f;
            float edgeNodeDist = MinEdgeToNodeDistance(edges, positions, n);
            var adj = BuildAdjFromEdges(edges, n);
            float minAngleDeg = adj != null ? MinAngleSeparationDeg(adj, positions, n) : 180f;
            float branchRatio = 0f;
            int maxDegree2ChainLen = 0;
            if (adj != null)
            {
                int branchNodes = 0;
                for (int i = 0; i < n; i++) if (adj[i].Count >= 3) branchNodes++;
                branchRatio = branchNodes / (float)n;
                maxDegree2ChainLen = LayoutDiagnostics.ComputeMaxDegree2ChainLenFromAdj(adj, n);
            }
            float cv = EdgeLengthCV(edges, positions);
            var lengths = new List<float>();
            foreach (var e in edges)
            {
                if (e.a < positions.Count && e.b < positions.Count)
                    lengths.Add(Vector2.Distance(positions[e.a], positions[e.b]));
            }
            float minLen = lengths.Count > 0 ? lengths[0] : 0f;
            float maxLen = lengths.Count > 0 ? lengths[0] : 0f;
            foreach (float L in lengths) { if (L < minLen) minLen = L; if (L > maxLen) maxLen = L; }
            float extremeLenPenalty = 0f;
            if (avgLen > 0.01f && lengths.Count > 0)
            {
                if (minLen < avgLen * 0.3f) extremeLenPenalty += (0.3f - minLen / avgLen) * 30f;
                if (maxLen > avgLen * 2.5f) extremeLenPenalty += (maxLen / avgLen - 2.5f) * 15f;
            }

            float spacingViolation = Mathf.Max(0f, minNodeSpacing - minDist);
            float clearanceViolation = Mathf.Max(0f, edgeClearance - edgeNodeDist);
            float angleViolation = minAngleDeg < 180f ? Mathf.Max(0f, angleThresholdDeg - minAngleDeg) : 0f;
            float crossingPenalty = Mathf.Min(crossings * CrossingPenaltyWeak, MaxCrossingsCap * CrossingPenaltyWeak);
            float corridorNorm = maxDegree2ChainLen > 0 ? Mathf.Min(1f, maxDegree2ChainLen / 8f) : 0f;
            float branchLack = branchRatio < 0.2f ? (0.2f - branchRatio) : 0f;

            float score = 100f
                - spacingViolation * SpacingViolationPenalty
                - clearanceViolation * ClearanceViolationPenalty
                - angleViolation * AngleViolationPenalty
                - crossingPenalty
                - corridorNorm * CorridorPenaltyPerUnit * 2f
                - branchLack * BranchLackPenalty
                - cv * 25f
                - extremeLenPenalty;

            if (minDist >= minNodeSpacing) score += Mathf.Min(15f, minDist * 5f);
            float spread = 0f;
            if (nodeCount >= 2)
            {
                float cx = 0, cy = 0;
                for (int i = 0; i < nodeCount; i++) { cx += positions[i].x; cy += positions[i].y; }
                cx /= nodeCount; cy /= nodeCount;
                for (int i = 0; i < nodeCount; i++)
                    spread += (positions[i].x - cx) * (positions[i].x - cx) + (positions[i].y - cy) * (positions[i].y - cy);
                spread = Mathf.Sqrt(spread / nodeCount);
            }
            score += Mathf.Min(10f, spread * 0.5f);
            return score;
        }

        private static List<int>[] BuildAdjFromEdges(IReadOnlyList<EdgeData> edges, int n)
        {
            if (edges == null || n <= 0) return null;
            var adj = new List<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new List<int>();
            foreach (var e in edges)
            {
                if (e.a >= 0 && e.a < n && e.b >= 0 && e.b < n && e.a != e.b)
                {
                    if (!adj[e.a].Contains(e.b)) adj[e.a].Add(e.b);
                    if (!adj[e.b].Contains(e.a)) adj[e.b].Add(e.a);
                }
            }
            return adj;
        }

        /// <summary>
        /// Single score for layout quality (higher = better). Uses readability-focused scoring.
        /// </summary>
        public static float Score(IReadOnlyList<EdgeData> edges, IReadOnlyList<Vector2> positions, int nodeCount)
        {
            return ScoreReadability(edges, positions, nodeCount);
        }

        public static float ApplyCorridorProxyPenalty(float baseScore, int nodeCount, float forcedRatio, int maxDegree2ChainLen, float corridorVisualRatio, float topEdgesShare)
        {
            float chainNorm = nodeCount > 0 ? Mathf.Clamp01(maxDegree2ChainLen / (float)nodeCount) : 0f;
            float penalty = 0f;
            penalty += Mathf.Clamp01((forcedRatio - 0.52f) / 0.48f) * 35f;
            penalty += Mathf.Clamp01((corridorVisualRatio - 0.55f) / 0.45f) * 30f;
            penalty += Mathf.Clamp01((topEdgesShare - 0.40f) / 0.60f) * 28f;
            penalty += chainNorm * 22f;
            return baseScore - penalty;
        }

        /// <summary>
        /// Score for LevelData. Higher = better layout. Uses layout metrics for corridor/branch penalties.
        /// </summary>
        public static float Score(LevelData level)
        {
            if (level?.nodes == null || level.edges == null) return float.MinValue;
            int n = level.nodes.Length;
            var positions = new Vector2[n];
            foreach (var nd in level.nodes)
            {
                if (nd.id >= 0 && nd.id < n)
                    positions[nd.id] = nd.pos;
            }
            return ScoreReadability(level.edges, positions, n);
        }
    }
}
