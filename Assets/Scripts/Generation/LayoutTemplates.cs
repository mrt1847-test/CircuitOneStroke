using System.Collections.Generic;
using UnityEngine;

namespace CircuitOneStroke.Generation
{
    /// <summary>
    /// Predefined coordinate slots per node count for human-designed, polygon/grid-like layouts.
    /// Each template returns Vector2[] slots of length N (no jitter; applied later).
    /// </summary>
    public struct LayoutTemplate
    {
        public string name;
        public int nodeCount;
        public Vector2[] slots;
    }

    /// <summary>노드 수별 레이아웃(링·그리드·클러스터 등) 슬롯 제공. LevelGenerator에서 사용.</summary>
    public static class LayoutTemplates
    {
        private const float RingRadius = 3.5f;
        private const float GridSpacing = 1.4f;

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

        /// <summary>
        /// Returns all layout templates compatible with the given node count.
        /// </summary>
        public static List<LayoutTemplate> GetLayoutsForNodeCount(int n)
        {
            var list = new List<LayoutTemplate>();
            switch (n)
            {
                case 5:
                    list.Add(new LayoutTemplate { name = "Ring5", nodeCount = 5, slots = Ring(5) });
                    break;
                case 6:
                    list.Add(new LayoutTemplate { name = "Ring6", nodeCount = 6, slots = Ring(6) });
                    break;
                case 8:
                    list.Add(new LayoutTemplate { name = "Ring8", nodeCount = 8, slots = Ring(8) });
                    list.Add(new LayoutTemplate { name = "Grid2x4", nodeCount = 8, slots = Grid2x4() });
                    break;
                case 9:
                    list.Add(new LayoutTemplate { name = "Ring9", nodeCount = 9, slots = Ring(9) });
                    list.Add(new LayoutTemplate { name = "Grid3x3", nodeCount = 9, slots = Grid3x3() });
                    list.Add(new LayoutTemplate { name = "Figure8", nodeCount = 9, slots = Figure8() });
                    list.Add(new LayoutTemplate { name = "TwoClusters9", nodeCount = 9, slots = TwoClusters(9) });
                    break;
                case 10:
                    list.Add(new LayoutTemplate { name = "Ring10", nodeCount = 10, slots = Ring(10) });
                    list.Add(new LayoutTemplate { name = "TwoClusters10", nodeCount = 10, slots = TwoClusters(10) });
                    break;
                default:
                    if (n >= 5 && n <= 10)
                        list.Add(new LayoutTemplate { name = "Ring", nodeCount = n, slots = Ring(n) });
                    break;
            }
            return list;
        }

        private static Vector2[] Grid2x4()
        {
            // 2 rows x 4 columns, centered
            var s = new Vector2[8];
            float w = GridSpacing * 1.5f;
            int idx = 0;
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 4; col++)
                    s[idx++] = new Vector2((col - 1.5f) * w, (row == 0 ? 1 : -1) * w);
            return s;
        }

        private static Vector2[] Grid3x3()
        {
            var s = new Vector2[9];
            float w = GridSpacing * 1.2f;
            int idx = 0;
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    s[idx++] = new Vector2((col - 1) * w, (1 - row) * w);
            return s;
        }

        private static Vector2[] Figure8()
        {
            // Figure-8: two lobes (left square, right square) sharing center; 9 nodes total.
            float r = RingRadius * 0.5f;
            float cx = 0.4f;
            return new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(-r - cx, r), new Vector2(-cx, r), new Vector2(-r - cx, -r), new Vector2(-cx, -r),
                new Vector2(r - cx, r), new Vector2(r + cx, r), new Vector2(r - cx, -r), new Vector2(r + cx, -r)
            };
        }

        private static Vector2[] TwoClusters(int n)
        {
            float gap = GridSpacing * 2f;
            int half = n / 2;
            int other = n - half;
            var s = new Vector2[n];
            int idx = 0;
            for (int i = 0; i < half; i++)
            {
                float angle = 2f * Mathf.PI * i / half - Mathf.PI / 2f;
                s[idx++] = new Vector2(-gap * 0.5f + RingRadius * 0.5f * Mathf.Cos(angle), RingRadius * 0.5f * Mathf.Sin(angle));
            }
            for (int i = 0; i < other; i++)
            {
                float angle = 2f * Mathf.PI * i / other - Mathf.PI / 2f;
                s[idx++] = new Vector2(gap * 0.5f + RingRadius * 0.5f * Mathf.Cos(angle), RingRadius * 0.5f * Mathf.Sin(angle));
            }
            return s;
        }
    }
}
