using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Grafo de waypoints sobre a malha de ruas (R38).
    ///
    /// As ruas são as faixas de largura 'street' entre os polígonos (tratados pela caixa envolvente, já que a
    /// malha é de retângulos alinhados) e a rua de contorno dentro do Limiter. Cada rua tem uma linha central
    /// horizontal ou vertical. Cruzamentos de linhas centrais viram waypoints "Junction". Os waypoints de entrada
    /// das estruturas ficam sobre as linhas centrais horizontais. Ao longo de cada linha central, cada waypoint
    /// é ligado ao seguinte: são vizinhos imediatos pela rua.
    /// </summary>
    public static class StreetGraph
    {
        public const string JunctionType = "Junction";
        const float Step = 0.02f, OnLine = 0.03f;

        public class Line
        {
            public bool horizontal; public float c, from, to;
            public bool Contains(Vector2 p) => horizontal
                ? Mathf.Abs(p.y - c) <= OnLine && p.x >= from - OnLine && p.x <= to + OnLine
                : Mathf.Abs(p.x - c) <= OnLine && p.y >= from - OnLine && p.y <= to + OnLine;
            public float Along(Vector2 p) => horizontal ? p.x : p.y;
        }

        public class Result
        {
            public readonly List<Line> lines = new List<Line>();
            public readonly List<Vector2> junctions = new List<Vector2>();
            /// <summary>Pares (chave, chave) ordenados, sem repetição.</summary>
            public readonly List<(string a, string b)> links = new List<(string, string)>();
            /// <summary>Entradas que não caíram sobre nenhuma linha central.</summary>
            public readonly List<string> offStreet = new List<string>();
        }

        public static string JunctionKey(string id) => JunctionType + " " + id;

        /// <summary>Linhas centrais das ruas (segmentos contínuos livres de polígonos).</summary>
        public static List<Line> Lines(IList<Rect> rects, Rect limiter, float street)
        {
            float h = street / 2f;
            var lines = new List<Line>();
            var ys = new SortedSet<float>(); var xs = new SortedSet<float>();
            foreach (var r in rects)
            {
                ys.Add(R(r.yMin - h)); ys.Add(R(r.yMax + h));
                xs.Add(R(r.xMin - h)); xs.Add(R(r.xMax + h));
            }
            bool FreeH(float x, float y) => x >= limiter.xMin + h - 1e-3f && x <= limiter.xMax - h + 1e-3f
                && !rects.Any(r => x > r.xMin + 1e-4f && x < r.xMax - 1e-4f && y + h - 0.01f > r.yMin && y - h + 0.01f < r.yMax);
            bool FreeV(float x, float y) => y >= limiter.yMin + h - 1e-3f && y <= limiter.yMax - h + 1e-3f
                && !rects.Any(r => y > r.yMin + 1e-4f && y < r.yMax - 1e-4f && x + h - 0.01f > r.xMin && x - h + 0.01f < r.xMax);

            foreach (var y in ys)
            {
                if (y - h < limiter.yMin - 1e-3f || y + h > limiter.yMax + 1e-3f) continue;
                Scan(limiter.xMin + h, limiter.xMax - h, x => FreeH(x, y), (a, b) => lines.Add(new Line { horizontal = true, c = y, from = a, to = b }), street);
            }
            foreach (var x in xs)
            {
                if (x - h < limiter.xMin - 1e-3f || x + h > limiter.xMax + 1e-3f) continue;
                Scan(limiter.yMin + h, limiter.yMax - h, y => FreeV(x, y), (a, b) => lines.Add(new Line { horizontal = false, c = x, from = a, to = b }), street);
            }
            return lines;
        }

        static float R(float v) => Mathf.Round(v * 1000f) / 1000f;

        static void Scan(float from, float to, System.Func<float, bool> free, System.Action<float, float> emit, float street)
        {
            float start = float.NaN, last = float.NaN;
            int n = Mathf.CeilToInt((to - from) / Step);
            for (int i = 0; i <= n; i++)
            {
                float v = Mathf.Min(from + i * Step, to);
                if (free(v)) { if (float.IsNaN(start)) start = v; last = v; }
                else if (!float.IsNaN(start)) { if (last - start > street + 0.05f) emit(start, last); start = float.NaN; }
            }
            if (!float.IsNaN(start) && last - start > street + 0.05f) emit(start, last);
        }

        /// <summary>
        /// Monta o grafo. 'entries' são (chave, posição) dos waypoints de entrada; 'junctionIds' recebe a posição de
        /// cada cruzamento e devolve a chave dele.
        /// </summary>
        public static Result Build(IList<Rect> rects, Rect limiter, float street, IList<(string key, Vector2 p)> entries, System.Func<Vector2, string> junctionKey)
        {
            var res = new Result();
            res.lines.AddRange(Lines(rects, limiter, street));
            var hs = res.lines.Where(l => l.horizontal).ToList();
            var vs = res.lines.Where(l => !l.horizontal).ToList();
            foreach (var hl in hs)
                foreach (var vl in vs)
                {
                    var p = new Vector2(vl.c, hl.c);
                    if (hl.Contains(p) && vl.Contains(p) && !res.junctions.Any(j => (j - p).sqrMagnitude < 1e-4f)) res.junctions.Add(p);
                }
            res.junctions.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            var points = res.junctions.Select(j => (key: junctionKey(j), p: j)).Concat(entries).ToList();
            foreach (var e in entries) if (!res.lines.Any(l => l.horizontal && l.Contains(e.p))) res.offStreet.Add(e.key);

            var set = new HashSet<(string, string)>();
            foreach (var line in res.lines)
            {
                // posição arredondada a 1 cm: entrada e cruzamento no mesmo ponto empatam e são ordenados pela chave
                var on = points.Where(pt => line.Contains(pt.p)).OrderBy(pt => Mathf.Round(line.Along(pt.p) * 100f)).ThenBy(pt => pt.key, System.StringComparer.Ordinal).ToList();
                for (int i = 1; i < on.Count; i++)
                {
                    string a = on[i - 1].key, b = on[i].key;
                    if (a == b) continue;
                    var pair = string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
                    if (set.Add(pair)) res.links.Add(pair);
                }
            }
            res.links.Sort((x, y) => { int c = string.CompareOrdinal(x.a, y.a); return c != 0 ? c : string.CompareOrdinal(x.b, y.b); });
            return res;
        }

        /// <summary>Retângulos (caixa envolvente) dos polígonos do layout.</summary>
        public static List<Rect> Rects(CityLayout l) =>
            l.polygons.Where(p => p.points.Count >= 3).Select(p => CityGeometry.BoundingBox(new[] { (IList<Vector2>)CityGeometry.ToVectors(p.points) })).ToList();

        public static string EntryKey(StructureData s) => s.type + " " + s.id;

        /// <summary>Calcula junctions e links do layout a partir da geometria e das entradas já definidas.</summary>
        public static void Fill(CityLayout l)
        {
            var geo = LayoutValidator.BuildGeo(l);
            var entries = l.structures.Select(s => (EntryKey(s), (Vector2)s.entry)).ToList();
            var ids = new Dictionary<Vector2, string>();
            var r = Build(Rects(l), geo.limiter, l.streetWidth, entries, p =>
            {
                if (!ids.TryGetValue(p, out var id)) { id = (ids.Count + 1).ToString("000"); ids[p] = id; }
                return JunctionKey(id);
            });
            l.junctions = r.junctions.Select(p => new WaypointNode { id = ids[p], position = p }).ToList();
            l.links = r.links.Select(x => new WaypointLink { a = x.a, b = x.b }).ToList();
        }
    }
}
