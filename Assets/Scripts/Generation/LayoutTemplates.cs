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

        public static List<LayoutTemplate> GetLayoutsForNodeCount(int n)
        {
            var list = new List<LayoutTemplate>();
            if (n < 4) return list;

            list.Add(new LayoutTemplate { name = "Ring", nodeCount = n, slots = Ring(n) });
            list.Add(new LayoutTemplate { name = "StarSpoke", nodeCount = n, slots = StarSpokeLayout(n) });
            list.Add(new LayoutTemplate { name = "DoubleRing", nodeCount = n, slots = DoubleRingLayout(n) });
            list.Add(new LayoutTemplate { name = "ConcentricPolygon", nodeCount = n, slots = ConcentricPolygonLayout(n) });
            list.Add(new LayoutTemplate { name = "Layered", nodeCount = n, slots = LayeredLayout(n) });

            if (n >= 8)
                list.Add(new LayoutTemplate { name = "GridSparse", nodeCount = n, slots = GridSparse(n) });
            if (n >= 9)
                list.Add(new LayoutTemplate { name = "TwoCluster", nodeCount = n, slots = TwoClusters(n) });
            if (n >= 12)
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
            while (slots.Count < n) slots.Add(Vector2.zero);
            return slots.ToArray();
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
    }
}
