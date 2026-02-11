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
        /// Single score for layout quality (higher = better). Penalizes crossings, low min distance, high edge-length CV.
        /// </summary>
        public static float Score(IReadOnlyList<EdgeData> edges, IReadOnlyList<Vector2> positions, int nodeCount)
        {
            if (edges == null || positions == null || nodeCount <= 0) return float.MinValue;
            int crossings = CountCrossings(edges, positions, nodeCount);
            float minDist = MinNodeDistance(positions, nodeCount);
            float avgLen = AverageEdgeLength(edges, positions);
            float cv = EdgeLengthCV(edges, positions);
            if (avgLen < 0.01f) avgLen = 1f;
            float distScore = minDist / Mathf.Max(0.25f, avgLen * MinNodeDistanceScaleDefault);
            float score = 100f - crossings * 30f + distScore * 20f - cv * 40f;
            return score;
        }

        /// <summary>
        /// Score for LevelData. Higher = better layout.
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
            return Score(level.edges, positions, n);
        }
    }
}
