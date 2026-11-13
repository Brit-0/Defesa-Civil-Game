using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>Validação do city_layout.json (regras marcadas como L em docs/plan-citygen.md).</summary>
    public static class LayoutValidator
    {
        public const float SizeTolerance = 0.01f;
        public const float CenterTolerance = 0.5f;
        public const float StreetTolerance = 0.02f;
        const float OverlapMin = 0.06f;

        public class Geo
        {
            public readonly Dictionary<string, List<Vector2>> polys = new Dictionary<string, List<Vector2>>();
            public Rect bbox;
            /// <summary>Caixa dos polígonos expandida pela rua de contorno (R27).</summary>
            public Rect limiter;
            public IEnumerable<IList<Vector2>> All => polys.Values;
        }

        public static Geo BuildGeo(CityLayout l)
        {
            var g = new Geo();
            foreach (var p in l.polygons) g.polys[p.id] = CityGeometry.ToVectors(p.points);
            g.bbox = CityGeometry.BoundingBox(g.All);
            float m = l.limiterMargin;
            g.limiter = Rect.MinMaxRect(g.bbox.xMin - m, g.bbox.yMin - m, g.bbox.xMax + m, g.bbox.yMax + m);
            return g;
        }

        /// <summary>Cantos inferiores da base visível com centro em 'baseCenter'.</summary>
        public static (Vector2 left, Vector2 right) BottomCorners(Vector2 baseCenter, float visWidthU) =>
            (new Vector2(baseCenter.x - visWidthU / 2f, baseCenter.y), new Vector2(baseCenter.x + visWidthU / 2f, baseCenter.y));

        /// <summary>Id do polígono que contém os dois pontos, ou null.</summary>
        public static string ContainingPolygon(Geo g, Vector2 a, Vector2 b)
        {
            foreach (var kv in g.polys)
                if (kv.Value.Count >= 3 && CityGeometry.PointInPolygon(a, kv.Value) && CityGeometry.PointInPolygon(b, kv.Value)) return kv.Key;
            return null;
        }

        /// <summary>Waypoint de entrada esperado (R14) ou null se não há rua ao sul (R13).</summary>
        public static Vector2? ExpectedEntry(Geo g, Vector2 baseCenter)
        {
            if (!CityGeometry.SouthStreetSegment(baseCenter, g.All, g.limiter.yMin, out var top, out var bottom)) return null;
            return new Vector2(baseCenter.x, (top + bottom) / 2f);
        }

        static bool Overlap(Rect a, Rect b) =>
            Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > OverlapMin && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > OverlapMin;

        public static ValidationReport Validate(CityLayout l, SpriteCatalog cat)
        {
            var r = new ValidationReport { mode = "layout" };
            var g = BuildGeo(l);
            float street = l.streetWidth;
            r.notes.Add($"R08 (parcial): semente {l.seed}; 'não escolhido à mão' não é automatizável.");

            foreach (var dup in l.polygons.GroupBy(p => p.id).Where(x => x.Count() > 1)) r.Add("R29", "Polygon " + dup.Key, Vector2.zero, "id de polígono repetido no JSON");
            foreach (var dup in l.structures.GroupBy(s => s.type + " " + s.id).Where(x => x.Count() > 1)) r.Add("R29", dup.Key, Vector2.zero, "id de estrutura repetido no JSON");
            foreach (var dup in l.decorations.GroupBy(d => d.id).Where(x => x.Count() > 1)) r.Add("R29", "Tree " + dup.Key, Vector2.zero, "id de decoração repetido no JSON");

            var houses = l.structures.Where(s => s.type == StructureType.House).ToList();
            int nHouse = houses.Count;
            int nGuard = l.structures.Count(s => s.type == StructureType.Guard);
            int nWare = l.structures.Count(s => s.type == StructureType.Warehouse);
            if (nHouse < 50 || nHouse > 80) r.Add("R01", "Houses", Vector2.zero, $"{nHouse} House (esperado 50–80)");
            if (nGuard != 1) r.Add("R02", "Houses", Vector2.zero, $"{nGuard} Guard (esperado 1)");
            if (nWare != 2) r.Add("R03", "Houses", Vector2.zero, $"{nWare} Warehouse (esperado 2)");

            // estruturas
            var structPoly = new Dictionary<StructureData, string>();
            var houseDistrict = new Dictionary<StructureData, string>();
            var structRects = new List<(string obj, Rect rect, float baseY)>();
            foreach (var s in l.structures)
            {
                string obj = s.type + " " + s.id;
                Vector2 bc = s.bottomCenter;
                if (s.type != StructureType.House && s.type != StructureType.Guard && s.type != StructureType.Warehouse)
                    r.Add("R04", obj, bc, $"tipo desconhecido '{s.type}'");
                if (!s.sprite.name.EndsWith("_S")) r.Add("R05", obj, bc, $"sprite '{s.sprite.name}' não termina em _S");
                var cs = cat.Get(s.sprite);
                if (s.type == StructureType.House)
                {
                    if (!cat.InHousePool(s.sprite, out var d)) r.Add("R06", obj, bc, $"sprite '{s.sprite.name}' fora do pool de House");
                    else
                    {
                        houseDistrict[s] = d;
                        if (s.district != d) r.Add("R06", obj, bc, $"district '{s.district}' difere da pasta do sprite '{d}'");
                    }
                }
                else if (!cat.InGuardWarehousePool(s.sprite)) r.Add("R07", obj, bc, $"sprite '{s.sprite.name}' fora de Industrial/Fabrica_*_S");
                if (cs == null) { r.Add("R11", obj, bc, "sprite inexistente: tamanho nativo indeterminável"); continue; }

                structRects.Add((obj, cs.VisibleRect(bc), bc.y));
                var (a, b) = BottomCorners(bc, cs.visWU);
                var pid = ContainingPolygon(g, a, b);
                if (pid == null) r.Add("R12", obj, bc, "cantos inferiores não estão no mesmo quarteirão");
                else structPoly[s] = pid;

                var expected = ExpectedEntry(g, bc);
                if (expected == null) r.Add("R13", obj, bc, "sem rua ao sul dentro do Limiter");
                else
                {
                    Vector2 e = s.entry;
                    if (Vector2.Distance(e, expected.Value) > SizeTolerance) r.Add("R14", obj, e, $"entry {s.entry} difere do esperado {(Vec2)expected.Value}");
                    if (g.All.Any(p => p.Count >= 3 && CityGeometry.PointInPolygon(e, p, 0f) && CityGeometry.DistanceToBoundary(e, p) > CityGeometry.Eps))
                        r.Add("R14", obj, e, "entry dentro de um polígono");
                }
            }

            // R37: estruturas não se sobrepõem (parte visível)
            for (int i = 0; i < structRects.Count; i++)
                for (int j = i + 1; j < structRects.Count; j++)
                    if (Overlap(structRects[i].rect, structRects[j].rect))
                        r.Add("R37", structRects[i].obj + " × " + structRects[j].obj, structRects[i].rect.center, "partes visíveis das estruturas se sobrepõem");

            // R09 proporção
            if (nHouse > 0)
            {
                var target = new Dictionary<string, float> { { District.Suburban, 60f }, { District.Commercial, 25f }, { District.Industrial, 15f } };
                foreach (var kv in target)
                {
                    float pct = 100f * houseDistrict.Values.Count(d => d == kv.Key) / nHouse;
                    if (Mathf.Abs(pct - kv.Value) > 5f + 1e-3f) r.Add("R09", kv.Key, Vector2.zero, $"{pct:0.#}% (esperado {kv.Value}% ±5)");
                }
            }

            // quarteirões = polígonos com estruturas
            var blocks = new HashSet<string>(structPoly.Values);
            foreach (var pid in blocks)
            {
                var hs = houses.Where(h => structPoly.TryGetValue(h, out var p) && p == pid && houseDistrict.ContainsKey(h)).ToList();
                if (hs.Count == 0) continue;
                float best = hs.GroupBy(h => houseDistrict[h]).Max(x => x.Count()) / (float)hs.Count;
                if (best < 0.8f - 1e-4f) r.Add("R10", "Polygon " + pid, Centroid(g.polys[pid]), $"maior pasta = {best:P0} das House (mín. 80%)");
            }

            // decorações
            int nDeco = l.decorations.Count;
            if (nDeco < 2 * nHouse) r.Add("R19", "Trees", Vector2.zero, $"{nDeco} decorações para {nHouse} House (mínimo {2 * nHouse})");
            foreach (var d in l.decorations)
            {
                string obj = "Tree " + d.id;
                Vector2 bc = d.bottomCenter;
                if (!d.sprite.name.EndsWith("_S") || !cat.InDecorationPool(d.sprite)) r.Add("R17", obj, bc, $"sprite '{d.sprite.name}' fora do pool de decoração");
                var cs = cat.Get(d.sprite);
                if (cs == null) { r.Add("R18", obj, bc, "sprite inexistente: tamanho nativo indeterminável"); continue; }
                var (a, b) = BottomCorners(bc, cs.visWU);
                if (ContainingPolygon(g, a, b) == null) { r.Add("R20", obj, bc, "cantos inferiores não estão no mesmo polígono"); continue; }
                // R36: decoração é sempre desenhada sobre as estruturas; só pode cobrir uma estrutura se estiver à frente dela
                var rect = cs.VisibleRect(bc);
                foreach (var sr in structRects)
                    if (bc.y > sr.baseY + 0.02f && Overlap(rect, sr.rect))
                    {
                        r.Add("R36", obj, bc, $"cobre {sr.obj} estando atrás dela (base y {bc.y:0.##} > {sr.baseY:0.##})");
                        break;
                    }
            }

            // polígonos
            foreach (var kv in g.polys)
            {
                bool isBlock = blocks.Contains(kv.Key);
                if (isBlock && !CityGeometry.IsAxisAlignedRectangle(kv.Value)) r.Add("R23", "Polygon " + kv.Key, Centroid(kv.Value), "quarteirão não é retângulo de 4 vértices alinhado a X/Y");
                if (!isBlock && kv.Value.Count < 3) r.Add("R24", "Polygon " + kv.Key, Centroid(kv.Value), "polígono decorativo com menos de 3 vértices");
            }
            var ids = g.polys.Keys.Where(k => g.polys[k].Count >= 3).ToList();
            for (int i = 0; i < ids.Count; i++)
                for (int j = i + 1; j < ids.Count; j++)
                {
                    float dist = CityGeometry.PolygonDistance(g.polys[ids[i]], g.polys[ids[j]]);
                    if (dist < street - 1e-3f) r.Add("R25", $"Polygon {ids[i]} × {ids[j]}", Centroid(g.polys[ids[i]]), dist <= CityGeometry.Eps ? "polígonos se sobrepõem" : $"distância {dist:0.###} u (mín. {street:0.##})");
                }
            CheckUniformStreets(r, g, street);

            // R32: Warehouses nos quarteirões mais próximos de cantos opostos
            var wares = l.structures.Where(s => s.type == StructureType.Warehouse && structPoly.ContainsKey(s)).ToList();
            if (wares.Count == 2 && blocks.Count > 0)
            {
                var bb = g.bbox;
                string Near(Vector2 c) => blocks.OrderBy(pid => CornerDistance(c, g.polys[pid])).ThenBy(pid => pid).First();
                string sw = Near(new Vector2(bb.xMin, bb.yMin)), ne = Near(new Vector2(bb.xMax, bb.yMax));
                string nw = Near(new Vector2(bb.xMin, bb.yMax)), se = Near(new Vector2(bb.xMax, bb.yMin));
                var set = new HashSet<string> { structPoly[wares[0]], structPoly[wares[1]] };
                if (!(set.SetEquals(new[] { sw, ne }) && sw != ne) && !(set.SetEquals(new[] { nw, se }) && nw != se))
                    r.Add("R32", "Warehouse", wares[0].bottomCenter, $"Warehouses em {string.Join(",", set)}; cantos SW/NE={sw}/{ne}, NW/SE={nw}/{se}");
            }

            // R33: Guard em quarteirão industrial
            foreach (var guard in l.structures.Where(s => s.type == StructureType.Guard && structPoly.ContainsKey(s)))
            {
                var pid = structPoly[guard];
                var hs = houses.Where(h => structPoly.TryGetValue(h, out var p) && p == pid && houseDistrict.ContainsKey(h)).ToList();
                float ind = hs.Count == 0 ? 0f : hs.Count(h => houseDistrict[h] == District.Industrial) / (float)hs.Count;
                if (ind < 0.8f - 1e-4f) r.Add("R33", "Guard " + guard.id, guard.bottomCenter, $"quarteirão {pid} com {ind:P0} de House Industrial (mín. 80%)");
            }

            // R34 (parte de layout): centro da caixa = centro registrado (câmera inicial)
            if (g.polys.Count > 0 && Vector2.Distance(g.bbox.center, l.center) > CenterTolerance)
                r.Add("R34", "Polygons", g.bbox.center, $"centro da caixa {(Vec2)g.bbox.center} difere de center {l.center} (tol. 0,5)");

            r.Finish();
            return r;
        }

        /// <summary>
        /// R35: de qualquer ponto de um lado de polígono, andando para fora na perpendicular, encontra-se outro
        /// polígono ou a borda do Limiter exatamente a 'street' de distância; ou o lado está na borda do Limiter;
        /// ou o ponto está na boca de uma rua transversal cuja largura também é 'street'.
        /// </summary>
        static void CheckUniformStreets(ValidationReport r, Geo g, float street)
        {
            const float step = 0.5f, inset = 0.1f, off = 0.002f;
            foreach (var kv in g.polys)
            {
                var poly = kv.Value;
                if (poly.Count < 3) continue;
                float sign = CityGeometry.SignedArea(poly) >= 0f ? 1f : -1f;
                bool reported = false;
                for (int i = 0, j = poly.Count - 1; i < poly.Count && !reported; j = i++)
                {
                    Vector2 a = poly[j], b = poly[i];
                    float len = Vector2.Distance(a, b);
                    if (len < 2f * inset + 1e-3f) continue;
                    Vector2 t = (b - a) / len;
                    Vector2 n = new Vector2(t.y, -t.x) * sign; // normal para fora
                    for (float d = inset; d <= len - inset + 1e-4f; d += step)
                    {
                        Vector2 p = a + t * d, o = p + n * off;
                        if (o.x <= g.limiter.xMin || o.x >= g.limiter.xMax || o.y <= g.limiter.yMin || o.y >= g.limiter.yMax) continue; // lado na borda do Limiter
                        float gap = CityGeometry.RayDistance(o, n, g.All, g.limiter) + off;
                        if (gap <= street + StreetTolerance) continue; // largura certa (menor que 'street' é R25)
                        Vector2 q = p + n * (street + 0.1f);
                        float lateral = CityGeometry.RayDistance(q, t, g.All, g.limiter) + CityGeometry.RayDistance(q, -t, g.All, g.limiter);
                        if (Mathf.Abs(lateral - street) <= StreetTolerance) continue; // boca de rua transversal
                        r.Add("R35", "Polygon " + kv.Key, p, $"rua com {gap:0.##} u de largura neste ponto (esperado {street:0.##})");
                        reported = true;
                        break;
                    }
                }
            }
        }

        static float CornerDistance(Vector2 corner, IList<Vector2> poly) =>
            CityGeometry.PointInPolygon(corner, poly) ? 0f : CityGeometry.DistanceToBoundary(corner, poly);

        public static Vector2 Centroid(IList<Vector2> poly)
        {
            var c = Vector2.zero;
            foreach (var p in poly) c += p;
            return poly.Count > 0 ? c / poly.Count : c;
        }
    }
}
