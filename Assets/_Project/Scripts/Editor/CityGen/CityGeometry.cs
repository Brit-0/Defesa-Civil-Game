using System.Collections.Generic;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>Funções geométricas 2D usadas pelo Generate, Validate e Build. Unidades de mundo (u).</summary>
    public static class CityGeometry
    {
        public const float Eps = 1e-4f;

        public static List<Vector2> ToVectors(List<Vec2> pts)
        {
            var r = new List<Vector2>(pts.Count);
            foreach (var p in pts) r.Add(p);
            return r;
        }

        /// <summary>Ponto dentro do polígono; a borda conta como dentro (tolerância eps).</summary>
        public static bool PointInPolygon(Vector2 p, IList<Vector2> poly, float eps = Eps)
        {
            if (DistanceToBoundary(p, poly) <= eps) return true;
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                var a = poly[i]; var b = poly[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        public static float DistancePointSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-12f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + t * ab);
        }

        public static float DistanceToBoundary(Vector2 p, IList<Vector2> poly)
        {
            float d = float.MaxValue;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                d = Mathf.Min(d, DistancePointSegment(p, poly[j], poly[i]));
            return d;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        public static float SignedArea(IList<Vector2> poly)
        {
            float a = 0f;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++) a += poly[j].x * poly[i].y - poly[i].x * poly[j].y;
            return a / 2f;
        }

        /// <summary>
        /// Distância, a partir de 'origin' na direção unitária 'dir', até a primeira aresta de polígono ou até
        /// sair de 'limiter' (o que vier primeiro).
        /// </summary>
        public static float RayDistance(Vector2 origin, Vector2 dir, IEnumerable<IList<Vector2>> polys, Rect limiter)
        {
            float best = float.MaxValue;
            if (dir.x > 1e-6f) best = Mathf.Min(best, (limiter.xMax - origin.x) / dir.x);
            if (dir.x < -1e-6f) best = Mathf.Min(best, (limiter.xMin - origin.x) / dir.x);
            if (dir.y > 1e-6f) best = Mathf.Min(best, (limiter.yMax - origin.y) / dir.y);
            if (dir.y < -1e-6f) best = Mathf.Min(best, (limiter.yMin - origin.y) / dir.y);
            best = Mathf.Max(best, 0f);
            foreach (var poly in polys)
                for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                {
                    Vector2 a = poly[j], e = poly[i] - poly[j];
                    float den = Cross(dir, e);
                    if (Mathf.Abs(den) < 1e-9f) continue;
                    float t = Cross(a - origin, e) / den;
                    float u = Cross(a - origin, dir) / den;
                    if (t >= 0f && u >= -1e-5f && u <= 1f + 1e-5f && t < best) best = t;
                }
            return best;
        }


        public static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float d1 = Cross(q2 - q1, p1 - q1), d2 = Cross(q2 - q1, p2 - q1);
            float d3 = Cross(p2 - p1, q1 - p1), d4 = Cross(p2 - p1, q2 - p1);
            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) return true;
            return DistancePointSegment(p1, q1, q2) < Eps || DistancePointSegment(p2, q1, q2) < Eps
                || DistancePointSegment(q1, p1, p2) < Eps || DistancePointSegment(q2, p1, p2) < Eps;
        }

        public static float SegmentDistance(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            if (SegmentsIntersect(p1, p2, q1, q2)) return 0f;
            return Mathf.Min(Mathf.Min(DistancePointSegment(p1, q1, q2), DistancePointSegment(p2, q1, q2)),
                             Mathf.Min(DistancePointSegment(q1, p1, p2), DistancePointSegment(q2, p1, p2)));
        }

        /// <summary>True se os polígonos se tocam, cruzam ou um contém o outro.</summary>
        public static bool PolygonsOverlap(IList<Vector2> a, IList<Vector2> b) => PolygonDistance(a, b) <= Eps;

        /// <summary>Distância mínima entre dois polígonos (0 se se sobrepõem).</summary>
        public static float PolygonDistance(IList<Vector2> a, IList<Vector2> b)
        {
            if (PointInPolygon(a[0], b) || PointInPolygon(b[0], a)) return 0f;
            float d = float.MaxValue;
            for (int i = 0, j = a.Count - 1; i < a.Count; j = i++)
                for (int k = 0, l = b.Count - 1; k < b.Count; l = k++)
                    d = Mathf.Min(d, SegmentDistance(a[j], a[i], b[l], b[k]));
            return d;
        }

        public static bool IsAxisAlignedRectangle(IList<Vector2> poly, float eps = 1e-3f)
        {
            if (poly.Count != 4) return false;
            for (int i = 0; i < 4; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % 4]; var c = poly[(i + 2) % 4];
                bool horizontal = Mathf.Abs(a.y - b.y) <= eps && Mathf.Abs(a.x - b.x) > eps;
                bool vertical = Mathf.Abs(a.x - b.x) <= eps && Mathf.Abs(a.y - b.y) > eps;
                if (!horizontal && !vertical) return false;
                bool nextHorizontal = Mathf.Abs(b.y - c.y) <= eps;
                if (horizontal == nextHorizontal) return false;
            }
            return true;
        }

        public static Rect BoundingBox(IEnumerable<IList<Vector2>> polys)
        {
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            bool any = false;
            foreach (var poly in polys)
                foreach (var p in poly)
                {
                    any = true;
                    xMin = Mathf.Min(xMin, p.x); yMin = Mathf.Min(yMin, p.y);
                    xMax = Mathf.Max(xMax, p.x); yMax = Mathf.Max(yMax, p.y);
                }
            return any ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : new Rect();
        }

        /// <summary>Intervalos (yMin, yMax) em que a reta vertical x está dentro do polígono.</summary>
        public static List<Vector2> VerticalIntervals(float x, IList<Vector2> poly)
        {
            var ys = new List<float>();
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                var a = poly[j]; var b = poly[i];
                if (Mathf.Abs(a.x - b.x) < 1e-7f) continue;
                float lo = Mathf.Min(a.x, b.x), hi = Mathf.Max(a.x, b.x);
                if (x < lo || x >= hi) continue; // semiaberto: não conta vértice duas vezes
                float t = (x - a.x) / (b.x - a.x);
                ys.Add(a.y + t * (b.y - a.y));
            }
            ys.Sort();
            var r = new List<Vector2>();
            for (int i = 0; i + 1 < ys.Count; i += 2) r.Add(new Vector2(ys[i], ys[i + 1]));
            return r;
        }

        /// <summary>
        /// R13/R14: a partir de 'start' (dentro de um polígono), desce na vertical e devolve o primeiro trecho
        /// de rua [bottom, top] abaixo do polígono que contém 'start', limitado pelo fundo do Limiter.
        /// </summary>
        public static bool SouthStreetSegment(Vector2 start, IEnumerable<IList<Vector2>> polys, float limiterYMin, out float top, out float bottom)
        {
            top = bottom = float.NaN;
            var intervals = new List<Vector2>();
            foreach (var poly in polys) intervals.AddRange(VerticalIntervals(start.x, poly));
            intervals.Sort((a, b) => a.x.CompareTo(b.x));
            var merged = new List<Vector2>();
            foreach (var iv in intervals)
            {
                if (merged.Count > 0 && iv.x <= merged[merged.Count - 1].y + Eps)
                    merged[merged.Count - 1] = new Vector2(merged[merged.Count - 1].x, Mathf.Max(merged[merged.Count - 1].y, iv.y));
                else merged.Add(iv);
            }
            int idx = merged.FindIndex(iv => start.y >= iv.x - Eps && start.y <= iv.y + Eps);
            if (idx < 0) return false;
            top = merged[idx].x;
            bottom = Mathf.Max(idx > 0 ? merged[idx - 1].y : limiterYMin, limiterYMin);
            return top - bottom > Eps;
        }
    }
}
