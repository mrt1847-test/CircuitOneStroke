using System.Collections.Generic;
using UnityEngine;

namespace CircuitOneStroke.Generation
{
    public struct LayoutTemplate
    {
        public string name;
        public int nodeCount;
        public Vector2[] slots;
    }

    /// <summary>
    /// Layout slot families. LevelGenerator applies per-attempt transforms (rotation/shear/warp/jitter).
    /// </summary>
    public static class LayoutTemplates
    {
        private const float RingRadius = 3.5f;
        private const float GridSpacing = 1.35f;
        private const int MinTemplateNodeCount = 4;
        private const int GridSparseMinNodeCount = 8;
        private const int TwoClusterMinNodeCount = 9;
        private const int PentagonSpiralMinNodeCount = 10;
        private const int LadderMinNodeCount = 12;
        private const int KnightExactNodeCount = 16;

        public static List<LayoutTemplate> GetLayoutsForNodeCount(int n)
        {
            var list = new List<LayoutTemplate>();
            if (n < MinTemplateNodeCount) return list;

            // Keep this exact layout only at matching node count to preserve authored geometry.
            if (n == KnightExactNodeCount)
                list.Add(new LayoutTemplate { name = "KnightTour4x4Exact", nodeCount = n, slots = KnightTour4x4ExactLayout() });

            // Core families available for all supported sizes.
            list.Add(new LayoutTemplate { name = "Ring", nodeCount = n, slots = Ring(n) });
            list.Add(new LayoutTemplate { name = "StarSpoke", nodeCount = n, slots = StarSpokeLayout(n) });
            list.Add(new LayoutTemplate { name = "DoubleRing", nodeCount = n, slots = DoubleRingLayout(n) });
            list.Add(new LayoutTemplate { name = "ConcentricPolygon", nodeCount = n, slots = ConcentricPolygonLayout(n) });
            if (n >= PentagonSpiralMinNodeCount)
                list.Add(new LayoutTemplate { name = "PentagonSpiral", nodeCount = n, slots = PentagonSpiralLayout(n) });
            list.Add(new LayoutTemplate { name = "Layered", nodeCount = n, slots = LayeredLayout(n) });

            // Higher-N helpers for branch-friendly slot dispersion.
            if (n >= GridSparseMinNodeCount)
                list.Add(new LayoutTemplate { name = "GridSparse", nodeCount = n, slots = GridSparse(n) });
            if (n >= TwoClusterMinNodeCount)
                list.Add(new LayoutTemplate { name = "TwoCluster", nodeCount = n, slots = TwoClusters(n) });
            if (n >= LadderMinNodeCount)
                list.Add(new LayoutTemplate { name = "Ladder", nodeCount = n, slots = Ladder(n) });

            return list;
        }

        public static List<LayoutTemplate> GetLayoutsForNodeCountV2(int n)
        {
            return GetLayoutsForNodeCount(n);
        }

        private static Vector2[] Ring(int n, float radius = RingRadius)
        {
            var slots = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float angle = (2f * Mathf.PI * i / n) - Mathf.PI / 2f;
                slots[i] = new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
            }
            return slots;
        }

        private static Vector2[] StarSpokeLayout(int n)
        {
            var slots = new List<Vector2>(n);
            int centers = n >= 14 ? 2 : 1;
            if (centers == 1)
            {
                slots.Add(Vector2.zero);
            }
            else
            {
                float c = 0.7f;
                slots.Add(new Vector2(-c, 0f));
                slots.Add(new Vector2(c, 0f));
            }

            int remaining = n - slots.Count;
            int arms = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(n) * 1.8f), 5, 10);
            float inner = RingRadius * 0.55f;
            float outer = RingRadius * 1.02f;
            for (int i = 0; i < remaining; i++)
            {
                int arm = i % arms;
                float lane = (i / (float)remaining);
                float a = (2f * Mathf.PI * arm / arms) - Mathf.PI / 2f;
                float r = Mathf.Lerp(inner, outer, lane * 0.9f + 0.1f);
                slots.Add(new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a)));
            }
            return slots.ToArray();
        }

        private static Vector2[] DoubleRingLayout(int n)
        {
            // Two radial bands: outer for readability, inner for controlled complexity.
            int outerCount = Mathf.Clamp(Mathf.RoundToInt(n * 0.62f), 4, n - 2);
            int innerCount = n - outerCount;
            float outerR = RingRadius;
            float innerR = RingRadius * 0.56f;

            var slots = new List<Vector2>(n);
            for (int i = 0; i < outerCount; i++)
            {
                float a = (2f * Mathf.PI * i / outerCount) - Mathf.PI / 2f;
                slots.Add(new Vector2(outerR * Mathf.Cos(a), outerR * Mathf.Sin(a)));
            }
            for (int i = 0; i < innerCount; i++)
            {
                float a = (2f * Mathf.PI * i / Mathf.Max(1, innerCount)) - Mathf.PI / 2f + Mathf.PI / Mathf.Max(3, outerCount);
                slots.Add(new Vector2(innerR * Mathf.Cos(a), innerR * Mathf.Sin(a)));
            }
            return slots.ToArray();
        }

        private static Vector2[] ConcentricPolygonLayout(int n)
        {
            int outer = Mathf.Clamp(Mathf.RoundToInt(n * 0.55f), 4, n - 2);
            int middle = Mathf.Clamp(Mathf.RoundToInt(n * 0.25f), 2, n - outer - 1);
            int inner = n - outer - middle;
            float r0 = RingRadius * 1.04f;
            float r1 = RingRadius * 0.68f;
            float r2 = RingRadius * 0.32f;

            var slots = new List<Vector2>(n);
            AddPolygon(slots, outer, r0, 0f);
            AddPolygon(slots, middle, r1, 0.3f);
            AddPolygon(slots, inner, r2, 0.5f);
            return slots.ToArray();
        }

        /// <summary>
        /// Nested pentagon silhouette with per-ring phase shift so the shape feels like a pentagonal spiral.
        /// Inspired by hand-drawn nested pentagon path layouts.
        /// </summary>
        private static Vector2[] PentagonSpiralLayout(int n)
        {
            var slots = new List<Vector2>(n);
            if (n <= 0) return slots.ToArray();

            int maxRings = Mathf.Max(1, Mathf.CeilToInt(n / 5f));
            float outerR = RingRadius * 1.06f;
            float ringGap = RingRadius * 0.28f;
            float phaseStep = 0.34f;
            int remaining = n;

            for (int ring = 0; ring < maxRings && remaining > 0; ring++)
            {
                float radius = Mathf.Max(RingRadius * 0.20f, outerR - ringGap * ring);
                int countOnRing = Mathf.Min(5, remaining);
                float phase = ring * phaseStep;

                for (int i = 0; i < countOnRing; i++)
                {
                    float a = (2f * Mathf.PI * i / Mathf.Max(1, countOnRing)) - Mathf.PI / 2f + phase;
                    slots.Add(new Vector2(radius * Mathf.Cos(a), radius * Mathf.Sin(a)));
                }
                remaining -= countOnRing;
            }

            if (slots.Count > n)
                slots.RemoveRange(n, slots.Count - n);
            FillMissingSlots(slots, n, RingRadius * 0.18f);
            return slots.GetRange(0, n).ToArray();
        }

        private static void AddPolygon(List<Vector2> slots, int count, float radius, float phase)
        {
            if (count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                float a = (2f * Mathf.PI * i / count) - Mathf.PI / 2f + phase;
                slots.Add(new Vector2(radius * Mathf.Cos(a), radius * Mathf.Sin(a)));
            }
        }

        private static Vector2[] LayeredLayout(int n)
        {
            // Horizontal bands improve local separation and reduce long diagonal crowding.
            int layers = n < 10 ? 2 : 3;
            int top = Mathf.Max(2, Mathf.RoundToInt(n * 0.28f));
            int middle = layers == 3 ? Mathf.Max(2, Mathf.RoundToInt(n * 0.42f)) : n - top;
            int bottom = Mathf.Max(1, n - top - middle);

            float yTop = GridSpacing * 1.6f;
            float yMid = 0f;
            float yBot = -GridSpacing * 1.6f;

            var slots = new List<Vector2>(n);
            AddHorizontalLayer(slots, top, yTop, GridSpacing * 1.3f);
            if (layers == 3) AddHorizontalLayer(slots, middle, yMid, GridSpacing * 1.15f);
            AddHorizontalLayer(slots, bottom, yBot, GridSpacing * 1.25f);

            while (slots.Count > n) slots.RemoveAt(slots.Count - 1);
            FillMissingSlots(slots, n, GridSpacing * 0.35f);
            return slots.GetRange(0, n).ToArray();
        }

        // Avoid stacking missing nodes at (0,0). If a layout under-fills, append a tiny ring near center.
        private static void FillMissingSlots(List<Vector2> slots, int targetCount, float fillRadius)
        {
            if (slots == null) return;
            if (slots.Count >= targetCount) return;
            int missing = targetCount - slots.Count;
            for (int i = 0; i < missing; i++)
            {
                float a = (2f * Mathf.PI * i / Mathf.Max(1, missing)) - Mathf.PI / 2f;
                slots.Add(new Vector2(fillRadius * Mathf.Cos(a), fillRadius * Mathf.Sin(a)));
            }
        }

        private static void AddHorizontalLayer(List<Vector2> slots, int count, float y, float spacing)
        {
            if (count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) * 0.5f) * spacing;
                slots.Add(new Vector2(x, y));
            }
        }

        private static Vector2[] GridSparse(int n)
        {
            int rows = n <= 12 ? 3 : n <= 20 ? 4 : 5;
            int cols = Mathf.CeilToInt(n / (float)rows);
            var s = new Vector2[n];
            float w = GridSpacing * 1.2f;
            int idx = 0;
            for (int r = 0; r < rows && idx < n; r++)
            {
                for (int c = 0; c < cols && idx < n; c++)
                {
                    s[idx++] = new Vector2((c - (cols - 1) * 0.5f) * w, ((rows - 1) * 0.5f - r) * w);
                }
            }
            return s;
        }

        private static Vector2[] Ladder(int n)
        {
            int cols = Mathf.CeilToInt(n / 2f);
            float w = GridSpacing * 1.35f;
            var s = new Vector2[n];
            int idx = 0;
            for (int c = 0; c < cols && idx < n; c++)
            {
                float x = (c - (cols - 1) * 0.5f) * w;
                s[idx++] = new Vector2(x, w * 0.55f);
                if (idx < n) s[idx++] = new Vector2(x, -w * 0.55f);
            }
            return s;
        }

        private static Vector2[] TwoClusters(int n)
        {
            float gap = GridSpacing * 2.1f;
            int leftCount = n / 2;
            int rightCount = n - leftCount;
            float clusterR = RingRadius * 0.52f;

            var s = new Vector2[n];
            int idx = 0;
            for (int i = 0; i < leftCount; i++)
            {
                float angle = 2f * Mathf.PI * i / Mathf.Max(1, leftCount) - Mathf.PI / 2f;
                s[idx++] = new Vector2(-gap * 0.5f + clusterR * Mathf.Cos(angle), clusterR * Mathf.Sin(angle));
            }
            for (int i = 0; i < rightCount; i++)
            {
                float angle = 2f * Mathf.PI * i / Mathf.Max(1, rightCount) - Mathf.PI / 2f;
                s[idx++] = new Vector2(gap * 0.5f + clusterR * Mathf.Cos(angle), clusterR * Mathf.Sin(angle));
            }
            return s;
        }

        /// <summary>
        /// Exact 4x4 board coordinates for the knight graph template.
        /// Node id is row-major: id = row * 4 + col.
        /// </summary>
        private static Vector2[] KnightTour4x4ExactLayout()
        {
            const int rows = 4;
            const int cols = 4;
            float step = GridSpacing * 1.05f;
            float x0 = -((cols - 1) * 0.5f) * step;
            float y0 = ((rows - 1) * 0.5f) * step;

            var s = new Vector2[rows * cols];
            int id = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    s[id++] = new Vector2(x0 + c * step, y0 - r * step);
                }
            }
            return s;
        }
    }
}
