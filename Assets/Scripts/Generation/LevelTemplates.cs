using System.Collections.Generic;

namespace CircuitOneStroke.Generation
{
    /// <summary>
    /// Base graph template for level generation. All node indices are 0-based consecutive.
    /// </summary>
    public struct LevelTemplate
    {
        public string name;
        public int nodeCount;
        public List<(int a, int b)> edges;
        public List<(int a, int b)> diodeCandidates;
        public List<(int a, int b)> gateCandidates;
        public List<int> switchCandidates;
    }

    /// <summary>
    /// Embedded static data for the 10 level templates. Used by LevelGenerator.
    /// </summary>
    public static class LevelTemplates
    {
        public static readonly LevelTemplate[] All = new LevelTemplate[]
        {
            // T01_Ladder_2x4 (N=8)
            new LevelTemplate
            {
                name = "T01_Ladder_2x4",
                nodeCount = 8,
                edges = new List<(int, int)>
                {
                    (0, 1), (1, 2), (2, 3), (4, 5), (5, 6), (6, 7),
                    (0, 4), (1, 5), (2, 6), (3, 7), (1, 4), (2, 5)
                },
                diodeCandidates = new List<(int, int)> { (1, 4), (2, 5), (0, 4), (3, 7) },
                gateCandidates = new List<(int, int)> { (1, 5), (2, 6), (3, 7) },
                switchCandidates = new List<int> { 3, 4 }
            },
            // T02_TwoTriangles_BridgeSquare (N=9)
            new LevelTemplate
            {
                name = "T02_TwoTriangles_BridgeSquare",
                nodeCount = 9,
                edges = new List<(int, int)>
                {
                    (0, 1), (1, 2), (2, 0), (6, 7), (7, 8), (8, 6),
                    (2, 3), (3, 4), (4, 5), (5, 6), (1, 4), (7, 4)
                },
                diodeCandidates = new List<(int, int)> { (2, 3), (5, 6), (1, 4), (7, 4) },
                gateCandidates = new List<(int, int)> { (1, 4), (7, 4), (3, 4) },
                switchCandidates = new List<int> { 4 }
            },
            // T03_Ring8_WithChords (N=8)
            new LevelTemplate
            {
                name = "T03_Ring8_WithChords",
                nodeCount = 8,
                edges = new List<(int, int)>
                {
                    (0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 7), (7, 0),
                    (0, 3), (4, 7)
                },
                diodeCandidates = new List<(int, int)> { (0, 3), (4, 7), (7, 0) },
                gateCandidates = new List<(int, int)> { (0, 1), (3, 4), (4, 5) },
                switchCandidates = new List<int> { 0, 4 }
            },
            // T04_Bowtie_WithTail (N=8)
            new LevelTemplate
            {
                name = "T04_Bowtie_WithTail",
                nodeCount = 8,
                edges = new List<(int, int)>
                {
                    (0, 1), (1, 2), (2, 0), (2, 3), (3, 4), (4, 5), (5, 3), (3, 6), (6, 7)
                },
                diodeCandidates = new List<(int, int)> { (2, 3), (3, 6) },
                gateCandidates = new List<(int, int)> { (3, 4), (3, 5), (3, 6) },
                switchCandidates = new List<int> { 3 }
            },
            // T05_FigureEight_SquaresShared (N=9)
            new LevelTemplate
            {
                name = "T05_FigureEight_SquaresShared",
                nodeCount = 9,
                edges = new List<(int, int)>
                {
                    (0, 1), (1, 2), (2, 3), (3, 0), (3, 4), (4, 5), (5, 6), (6, 7), (7, 4), (4, 8), (2, 5)
                },
                diodeCandidates = new List<(int, int)> { (3, 4), (4, 8), (2, 5) },
                gateCandidates = new List<(int, int)> { (3, 4), (4, 5), (4, 7) },
                switchCandidates = new List<int> { 4 }
            },
            // T06_HubRing_ControlledHub (N=9)
            new LevelTemplate
            {
                name = "T06_HubRing_ControlledHub",
                nodeCount = 9,
                edges = new List<(int, int)>
                {
                    (1, 2), (2, 3), (3, 4), (4, 5), (5, 1), (0, 1), (0, 3), (0, 5), (5, 6), (6, 7), (7, 8), (8, 3)
                },
                diodeCandidates = new List<(int, int)> { (0, 3), (8, 3), (5, 6) },
                gateCandidates = new List<(int, int)> { (0, 1), (0, 3), (0, 5) },
                switchCandidates = new List<int> { 0 }
            },
            // T07_DiamondChain (N=10)
            new LevelTemplate
            {
                name = "T07_DiamondChain",
                nodeCount = 10,
                edges = new List<(int, int)>
                {
                    (0, 1), (1, 2), (2, 3), (1, 4), (4, 2), (2, 5), (5, 3), (4, 6), (6, 7), (7, 5), (6, 8), (8, 9), (9, 7)
                },
                diodeCandidates = new List<(int, int)> { (1, 4), (2, 5), (6, 7) },
                gateCandidates = new List<(int, int)> { (4, 6), (7, 5), (6, 8) },
                switchCandidates = new List<int> { 4, 5 }
            },
            // T08_SplitMerge_TwoCorridors (N=9)
            new LevelTemplate
            {
                name = "T08_SplitMerge_TwoCorridors",
                nodeCount = 9,
                edges = new List<(int, int)>
                {
                    (0, 1), (1, 2), (2, 8), (0, 3), (3, 4), (4, 5), (5, 8), (2, 5), (1, 4), (3, 6), (6, 7), (7, 5)
                },
                diodeCandidates = new List<(int, int)> { (2, 5), (1, 4), (3, 6) },
                gateCandidates = new List<(int, int)> { (1, 4), (2, 5), (3, 4) },
                switchCandidates = new List<int> { 4 }
            },
            // T09_ThreeLayer_CrossLinks (N=9)
            new LevelTemplate
            {
                name = "T09_ThreeLayer_CrossLinks",
                nodeCount = 9,
                edges = new List<(int, int)>
                {
                    (0, 1), (0, 2), (1, 3), (2, 3), (3, 4), (4, 5), (4, 6), (5, 7), (6, 7), (7, 8), (2, 4), (6, 3)
                },
                diodeCandidates = new List<(int, int)> { (3, 4), (2, 4), (6, 3) },
                gateCandidates = new List<(int, int)> { (2, 4), (6, 3), (4, 6) },
                switchCandidates = new List<int> { 4 }
            },
            // T10_TwoBridges_ChoiceLocks (N=10)
            new LevelTemplate
            {
                name = "T10_TwoBridges_ChoiceLocks",
                nodeCount = 10,
                edges = new List<(int, int)>
                {
                    (0, 1), (1, 2), (2, 3), (3, 0), (3, 4), (4, 5), (5, 6), (6, 7), (7, 8), (8, 9), (9, 6), (2, 7), (1, 8)
                },
                diodeCandidates = new List<(int, int)> { (3, 4), (5, 6), (2, 7), (1, 8) },
                gateCandidates = new List<(int, int)> { (3, 4), (4, 5), (5, 6) },
                switchCandidates = new List<int> { 4, 5 }
            },
            // T11_KnightGraph4x4_Exact (N=16)
            new LevelTemplate
            {
                name = "T11_KnightGraph4x4_Exact",
                nodeCount = 16,
                edges = new List<(int, int)>
                {
                    (0, 6), (0, 9), (1, 7), (1, 8), (1, 10), (2, 4), (2, 9), (2, 11),
                    (3, 5), (3, 10), (4, 10), (4, 13), (5, 11), (5, 12), (5, 14), (6, 8),
                    (6, 13), (6, 15), (7, 9), (7, 14), (8, 14), (9, 15), (10, 12), (11, 13)
                },
                diodeCandidates = new List<(int, int)>(),
                gateCandidates = new List<(int, int)>(),
                switchCandidates = new List<int> { 5, 6, 9, 10 }
            }
        };
    }
}
