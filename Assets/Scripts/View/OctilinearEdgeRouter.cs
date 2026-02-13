using System.Collections.Generic;
using CircuitOneStroke.Data;
using UnityEngine;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// Minimal router focused on three constraints:
    /// 1) 8-direction fan-out from node center with minimum straight stem.
    /// 2) No overlap with other paths or non-endpoint nodes. Orthogonal crossings are allowed.
    /// 3) No right-angle bends in a route polyline.
    /// </summary>
    public static class OctilinearEdgeRouter
    {
        private struct Incident
        {
            public int edgeId;
            public bool atStart;
            public int nodeId;
            public int preferredBin;
        }

        private struct EdgeWork
        {
            public int edgeId;
            public int a;
            public int b;
            public Vector2 pA;
            public Vector2 pB;
            public int binA;
            public int binB;
            public Vector2 sA;
            public Vector2 sB;
            public bool horizontalPreferred;
            public float laneBase;
        }

        private struct Segment
        {
            public int edgeId;
            public Vector2 a;
            public Vector2 b;
        }

        public static Dictionary<int, Vector2[]> BuildRoutes(
            LevelData level,
            float laneSpacing = 0.40f,
            float nearParallelDistance = 0.48f,
            float minOverlap = 0.48f,
            float minStubFromNodeCenter = 1.0f)
        {
            var routes = new Dictionary<int, Vector2[]>();
            if (level?.nodes == null || level.edges == null)
                return routes;

            int nodeCount = level.nodes.Length;
            var posByNode = new Vector2[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                int id = level.nodes[i].id;
                if (id >= 0 && id < nodeCount)
                    posByNode[id] = level.nodes[i].pos;
            }

            var incidentsByNode = BuildIncidents(level, posByNode, nodeCount);
            var assignedPortByIncident = AssignUniquePorts(incidentsByNode);

            float stubLen = Mathf.Max(Mathf.Max(0.24f, minStubFromNodeCenter), laneSpacing * 0.70f);
            var works = BuildEdgeWorks(level, posByNode, nodeCount, assignedPortByIncident, stubLen);

            var placed = new List<Segment>(works.Count * 6);
            float nodeClearance = Mathf.Max(0.45f, Mathf.Max(laneSpacing * 0.75f, minStubFromNodeCenter * 0.35f));
            int[] rings = { 0, 1, -1, 2, -2, 3, -3, 4, -4, 5, -5, 6, -6, 7, -7, 8, -8 };

            for (int i = 0; i < works.Count; i++)
            {
                var w = works[i];
                Vector2[] bestFeasible = null;
                float bestFeasibleScore = float.MaxValue;
                Vector2[] bestFallback = null;
                float bestFallbackScore = float.MaxValue;

                for (int pass = 0; pass < 2; pass++)
                {
                    bool allowFanOffsets = pass == 1;
                    for (int o = 0; o < 2; o++)
                    {
                        bool horizontal = (o == 0) ? w.horizontalPreferred : !w.horizontalPreferred;
                        for (int r = 0; r < rings.Length; r++)
                        {
                            float lane = w.laneBase + rings[r] * laneSpacing;
                            var candidates = BuildRouteCandidates(w, lane, horizontal, stubLen, allowFanOffsets);
                            for (int c = 0; c < candidates.Count; c++)
                            {
                                var candidate = candidates[c];
                                if (candidate == null || candidate.Length < 2)
                                    continue;

                            bool rightOk = NoRightAngles(candidate);
                            bool stemOk = HasRequiredStems(candidate, w.pA, w.pB, stubLen);
                            int overlapCount = CountParallelOverlaps(candidate, placed, nearParallelDistance, minOverlap);
                            int nodeViol = CountNodeClearanceViolations(candidate, w.a, w.b, posByNode, nodeClearance);
                            float score = RouteLength(candidate) * 0.02f + Mathf.Abs(rings[r]) * 0.8f + ((o == 0) ? 0f : 0.5f);

                            // Hard constraints for requirement #2/#3.
                            bool feasible = stemOk && rightOk && overlapCount == 0 && nodeViol == 0;
                            if (feasible && score < bestFeasibleScore)
                            {
                                bestFeasibleScore = score;
                                bestFeasible = candidate;
                            }

                            // Keep fallback only for extremely constrained layouts.
                            float fallbackScore = score + (stemOk ? 0f : 10000f) + (rightOk ? 0f : 1000f) + overlapCount * 100f + nodeViol * 100f;
                            if (fallbackScore < bestFallbackScore)
                            {
                                bestFallbackScore = fallbackScore;
                                bestFallback = candidate;
                            }
                            }
                        }
                    }

                    // Open fanOffset ±1 only if fanOffset=0 pass found no feasible candidate.
                    if (bestFeasible != null)
                        break;
                }

                Vector2[] best = bestFeasible ?? bestFallback;
                if (best == null)
                {
                    // Hard fallback must still preserve requirement #1:
                    // keep center-origin 8-direction stems before connecting.
                    best = BuildRouteCandidate(w, w.laneBase, w.horizontalPreferred, 0f, 0f, 0, 0, stubLen);
                    if (best == null || best.Length < 2)
                        best = new[] { w.pA, w.sA, w.sB, w.pB };
                }
                if (!HasRequiredStems(best, w.pA, w.pB, stubLen))
                    best = new[] { w.pA, w.sA, w.sB, w.pB };

                routes[w.edgeId] = best;
                AppendSegments(placed, w.edgeId, best);
            }

            foreach (var e in level.edges)
            {
                if (routes.ContainsKey(e.id)) continue;
                Vector2 p0 = (e.a >= 0 && e.a < nodeCount) ? posByNode[e.a] : Vector2.zero;
                Vector2 p1 = (e.b >= 0 && e.b < nodeCount) ? posByNode[e.b] : Vector2.zero;
                routes[e.id] = new[] { p0, p1 };
            }

            return routes;
        }

        private static List<Vector2[]> BuildRouteCandidates(EdgeWork w, float lane, bool horizontal, float stubLen, bool allowFanOffsets)
        {
            var list = new List<Vector2[]>(64);
            float[] advances = { 0f, stubLen * 0.22f, stubLen * 0.44f, stubLen * 0.66f };
            int[] fanOffsets = allowFanOffsets ? new[] { 0, -1, 1 } : new[] { 0 };

            for (int fa = 0; fa < fanOffsets.Length; fa++)
            {
                for (int fb = 0; fb < fanOffsets.Length; fb++)
                {
                    for (int i = 0; i < advances.Length; i++)
                    {
                        for (int j = 0; j < advances.Length; j++)
                        {
                            var c = BuildRouteCandidate(
                                w,
                                lane,
                                horizontal,
                                advances[i],
                                advances[j],
                                fanOffsets[fa],
                                fanOffsets[fb],
                                stubLen);
                            if (c != null) list.Add(c);
                        }
                    }
                }
            }
            return list;
        }

        private static Vector2[] BuildRouteCandidate(
            EdgeWork w,
            float lane,
            bool horizontal,
            float advanceA,
            float advanceB,
            int fanOffsetA,
            int fanOffsetB,
            float stubLen)
        {
            const float eps = 1e-4f;
            int binA = Mod8(w.binA + fanOffsetA);
            int binB = Mod8(w.binB + fanOffsetB);
            Vector2 dirA = DirectionFromBin(binA);
            Vector2 dirB = DirectionFromBin(binB);
            Vector2 sA = w.pA + dirA * stubLen;
            Vector2 sB = w.pB + dirB * stubLen;
            Vector2 qA = sA + dirA * Mathf.Max(0f, advanceA);
            Vector2 qB = sB + dirB * Mathf.Max(0f, advanceB);
            var pts = new List<Vector2>(10) { w.pA, sA };
            if ((qA - sA).sqrMagnitude > eps * eps) pts.Add(qA);

            if (horizontal)
            {
                float sx = SignNonZero(qB.x - qA.x, 1f);
                float legA = Mathf.Abs(lane - qA.y);
                float legB = Mathf.Abs(lane - qB.y);
                Vector2 cA = new Vector2(qA.x + sx * legA, lane);
                Vector2 cB = new Vector2(qB.x - sx * legB, lane);
                pts.Add(cA);
                pts.Add(cB);
            }
            else
            {
                float sy = SignNonZero(qB.y - qA.y, 1f);
                float legA = Mathf.Abs(lane - qA.x);
                float legB = Mathf.Abs(lane - qB.x);
                Vector2 cA = new Vector2(lane, qA.y + sy * legA);
                Vector2 cB = new Vector2(lane, qB.y - sy * legB);
                pts.Add(cA);
                pts.Add(cB);
            }

            if ((qB - sB).sqrMagnitude > eps * eps) pts.Add(qB);
            pts.Add(sB);
            pts.Add(w.pB);
            return SimplifyPolyline(pts, eps, keepEnds: 2);
        }

        private static bool HasRequiredStems(Vector2[] route, Vector2 startCenter, Vector2 endCenter, float minStem)
        {
            const float eps = 1e-4f;
            if (route == null || route.Length < 4) return false;

            Vector2 startVec = route[1] - route[0];
            if (startVec.sqrMagnitude <= eps * eps)
                return false;
            if (startVec.magnitude + eps < minStem)
                return false;
            if (!IsOctilinearDirection(startVec))
                return false;

            Vector2 endVec = route[route.Length - 2] - route[route.Length - 1];
            if (endVec.sqrMagnitude <= eps * eps)
                return false;
            if (endVec.magnitude + eps < minStem)
                return false;
            if (!IsOctilinearDirection(endVec))
                return false;

            if ((route[0] - startCenter).sqrMagnitude > 1e-3f)
                return false;
            if ((route[route.Length - 1] - endCenter).sqrMagnitude > 1e-3f)
                return false;

            return true;
        }

        private static bool IsOctilinearDirection(Vector2 v)
        {
            if (v.sqrMagnitude < 1e-8f) return false;
            Vector2 u = v.normalized;
            float bestDot = -1f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 d = DirectionFromBin(i);
                float dot = Vector2.Dot(u, d);
                if (dot > bestDot) bestDot = dot;
            }
            return bestDot >= 0.995f;
        }

        private static Dictionary<int, List<Incident>> BuildIncidents(LevelData level, Vector2[] posByNode, int nodeCount)
        {
            var byNode = new Dictionary<int, List<Incident>>();
            for (int i = 0; i < level.edges.Length; i++)
            {
                var e = level.edges[i];
                if (e.a < 0 || e.b < 0 || e.a >= nodeCount || e.b >= nodeCount || e.a == e.b)
                    continue;

                Vector2 a = posByNode[e.a];
                Vector2 b = posByNode[e.b];
                AddIncident(byNode, new Incident
                {
                    edgeId = e.id,
                    atStart = true,
                    nodeId = e.a,
                    preferredBin = DirectionBin(b - a)
                });
                AddIncident(byNode, new Incident
                {
                    edgeId = e.id,
                    atStart = false,
                    nodeId = e.b,
                    preferredBin = DirectionBin(a - b)
                });
            }
            return byNode;
        }

        private static Dictionary<long, int> AssignUniquePorts(Dictionary<int, List<Incident>> incidentsByNode)
        {
            var result = new Dictionary<long, int>();
            foreach (var kv in incidentsByNode)
            {
                var list = kv.Value;
                if (list == null || list.Count == 0) continue;

                list.Sort((x, y) =>
                {
                    int c = x.preferredBin.CompareTo(y.preferredBin);
                    if (c != 0) return c;
                    return x.edgeId.CompareTo(y.edgeId);
                });

                var used = new HashSet<int>();
                for (int i = 0; i < list.Count; i++)
                {
                    int chosen = PickClosestUnusedBin(list[i].preferredBin, used);
                    used.Add(chosen);
                    result[IncidentKey(list[i].edgeId, list[i].atStart)] = chosen;
                }
            }
            return result;
        }

        private static List<EdgeWork> BuildEdgeWorks(
            LevelData level,
            Vector2[] posByNode,
            int nodeCount,
            Dictionary<long, int> assignedPortByIncident,
            float stubLen)
        {
            var works = new List<EdgeWork>(level.edges.Length);
            for (int i = 0; i < level.edges.Length; i++)
            {
                var e = level.edges[i];
                if (e.a < 0 || e.b < 0 || e.a >= nodeCount || e.b >= nodeCount || e.a == e.b)
                    continue;

                Vector2 pA = posByNode[e.a];
                Vector2 pB = posByNode[e.b];
                int binA = GetAssignedBin(assignedPortByIncident, e.id, true, DirectionBin(pB - pA));
                int binB = GetAssignedBin(assignedPortByIncident, e.id, false, DirectionBin(pA - pB));

                Vector2 sA = pA + DirectionFromBin(binA) * stubLen;
                Vector2 sB = pB + DirectionFromBin(binB) * stubLen;
                Vector2 d = sB - sA;
                bool h = Mathf.Abs(d.x) >= Mathf.Abs(d.y);
                float laneBase = h ? 0.5f * (sA.y + sB.y) : 0.5f * (sA.x + sB.x);

                works.Add(new EdgeWork
                {
                    edgeId = e.id,
                    a = e.a,
                    b = e.b,
                    pA = pA,
                    pB = pB,
                    binA = binA,
                    binB = binB,
                    sA = sA,
                    sB = sB,
                    horizontalPreferred = h,
                    laneBase = laneBase
                });
            }
            return works;
        }

        private static bool NoRightAngles(Vector2[] route)
        {
            const float eps = 1e-4f;
            if (route == null || route.Length < 3) return true;
            for (int i = 1; i < route.Length - 1; i++)
            {
                Vector2 a = route[i - 1];
                Vector2 b = route[i];
                Vector2 c = route[i + 1];
                Vector2 u = b - a;
                Vector2 v = c - b;
                if (u.sqrMagnitude <= eps * eps || v.sqrMagnitude <= eps * eps) continue;
                float dot = Vector2.Dot(u.normalized, v.normalized);
                if (Mathf.Abs(dot) < 0.06f) return false;
            }
            return true;
        }

        private static int CountNodeClearanceViolations(Vector2[] route, int aId, int bId, Vector2[] posByNode, float clearance)
        {
            int count = 0;
            float c2 = clearance * clearance;
            for (int i = 0; i < posByNode.Length; i++)
            {
                if (i == aId || i == bId) continue;
                Vector2 p = posByNode[i];
                float d = DistancePointToPolyline(p, route);
                if (d * d < c2) count++;
            }
            return count;
        }

        private static int CountParallelOverlaps(Vector2[] route, List<Segment> placed, float nearParallelDistance, float minOverlap)
        {
            int count = 0;
            if (route == null || route.Length < 2) return 0;
            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector2 a0 = route[i];
                Vector2 a1 = route[i + 1];
                Vector2 da = a1 - a0;
                if (da.sqrMagnitude < 1e-8f) continue;
                Vector2 ua = da.normalized;

                for (int j = 0; j < placed.Count; j++)
                {
                    Vector2 b0 = placed[j].a;
                    Vector2 b1 = placed[j].b;
                    Vector2 db = b1 - b0;
                    if (db.sqrMagnitude < 1e-8f) continue;
                    Vector2 ub = db.normalized;

                    float cross = Mathf.Abs(ua.x * ub.y - ua.y * ub.x);
                    if (cross > 0.06f)
                        continue; // non-parallel crossing allowed

                    float lineDist = DistancePointToLine(a0, b0, b1);
                    if (lineDist > nearParallelDistance * 0.65f)
                        continue;

                    float overlap = ProjectionOverlap(a0, a1, b0, b1, ua);
                    if (overlap > minOverlap)
                        count++;
                }
            }
            return count;
        }

        private static void AppendSegments(List<Segment> segments, int edgeId, Vector2[] route)
        {
            if (route == null || route.Length < 2) return;
            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector2 a = route[i];
                Vector2 b = route[i + 1];
                if ((b - a).sqrMagnitude < 1e-8f) continue;
                segments.Add(new Segment { edgeId = edgeId, a = a, b = b });
            }
        }

        private static float RouteLength(Vector2[] route)
        {
            if (route == null || route.Length < 2) return 0f;
            float len = 0f;
            for (int i = 0; i < route.Length - 1; i++)
                len += Vector2.Distance(route[i], route[i + 1]);
            return len;
        }

        private static Vector2[] SimplifyPolyline(List<Vector2> points, float eps, int keepEnds = 0)
        {
            var outPts = new List<Vector2>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                if (outPts.Count > 0 && (points[i] - outPts[outPts.Count - 1]).sqrMagnitude <= eps * eps)
                    continue;
                outPts.Add(points[i]);
            }

            int k = 1;
            while (k < outPts.Count - 1)
            {
                if (k <= keepEnds || k >= outPts.Count - 1 - keepEnds)
                {
                    k++;
                    continue;
                }

                Vector2 a = outPts[k - 1];
                Vector2 b = outPts[k];
                Vector2 c = outPts[k + 1];
                Vector2 ab = b - a;
                Vector2 bc = c - b;
                if (ab.sqrMagnitude <= eps * eps || bc.sqrMagnitude <= eps * eps)
                {
                    outPts.RemoveAt(k);
                    continue;
                }

                float cross = ab.x * bc.y - ab.y * bc.x;
                if (Mathf.Abs(cross) <= eps && Vector2.Dot(ab, bc) > 0f)
                {
                    outPts.RemoveAt(k);
                    continue;
                }
                k++;
            }

            if (outPts.Count < 2)
                return points.ToArray();
            return outPts.ToArray();
        }

        private static float ProjectionOverlap(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1, Vector2 axis)
        {
            float at0 = Vector2.Dot(a0, axis);
            float at1 = Vector2.Dot(a1, axis);
            float bt0 = Vector2.Dot(b0, axis);
            float bt1 = Vector2.Dot(b1, axis);
            float amin = Mathf.Min(at0, at1);
            float amax = Mathf.Max(at0, at1);
            float bmin = Mathf.Min(bt0, bt1);
            float bmax = Mathf.Max(bt0, bt1);
            return Mathf.Min(amax, bmax) - Mathf.Max(amin, bmin);
        }

        private static float DistancePointToPolyline(Vector2 p, Vector2[] polyline)
        {
            if (polyline == null || polyline.Length < 2) return float.MaxValue;
            float best = float.MaxValue;
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                float d = DistancePointToSegment(p, polyline[i], polyline[i + 1]);
                if (d < best) best = d;
            }
            return best;
        }

        private static float DistancePointToLine(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len = ab.magnitude;
            if (len < 1e-8f) return Vector2.Distance(p, a);
            return Mathf.Abs((p.x - a.x) * ab.y - (p.y - a.y) * ab.x) / len;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-8f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            Vector2 proj = a + ab * t;
            return Vector2.Distance(p, proj);
        }

        private static int PickClosestUnusedBin(int preferredBin, HashSet<int> used)
        {
            int best = Mod8(preferredBin);
            int bestCost = int.MaxValue;
            for (int bin = 0; bin < 8; bin++)
            {
                if (used.Contains(bin)) continue;
                int d = CircularDist(bin, preferredBin);
                if (d < bestCost)
                {
                    best = bin;
                    bestCost = d;
                }
            }
            return best;
        }

        private static int CircularDist(int a, int b)
        {
            int d = Mathf.Abs(Mod8(a) - Mod8(b));
            return Mathf.Min(d, 8 - d);
        }

        private static void AddIncident(Dictionary<int, List<Incident>> byNode, Incident incident)
        {
            if (!byNode.TryGetValue(incident.nodeId, out var list))
            {
                list = new List<Incident>();
                byNode[incident.nodeId] = list;
            }
            list.Add(incident);
        }

        private static int GetAssignedBin(Dictionary<long, int> map, int edgeId, bool atStart, int fallback)
        {
            return map.TryGetValue(IncidentKey(edgeId, atStart), out int bin) ? bin : Mod8(fallback);
        }

        private static long IncidentKey(int edgeId, bool atStart)
        {
            return ((long)edgeId << 1) | (atStart ? 0L : 1L);
        }

        private static int DirectionBin(Vector2 dir)
        {
            if (dir.sqrMagnitude <= 1e-8f) return 0;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            return Mathf.RoundToInt(angle / 45f) % 8;
        }

        private static Vector2 DirectionFromBin(int bin)
        {
            float angle = Mod8(bin) * 45f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private static int Mod8(int v)
        {
            int m = v % 8;
            return m < 0 ? m + 8 : m;
        }

        private static float SignNonZero(float v, float fallback)
        {
            if (v > 1e-4f) return 1f;
            if (v < -1e-4f) return -1f;
            return fallback;
        }
    }
}
