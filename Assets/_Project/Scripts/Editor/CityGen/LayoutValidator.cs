using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>Validação do city_layout.json (regras marcadas como L em docs/plan-citygen.md).</summary>
    public static class LayoutValidator
    {
        public const float SizeTolerance = 0.01f;
        public const float StreetMin = 0.8f;
        public const float CurbDistance = 0.5f;
        public const float CenterTolerance = 0.5f;

        public class Geo
        {
            public readonly Dictionary<string, List<Vector2>> polys = new Dictionary<string, List<Vector2>>();
            public Rect bbox;
            public IEnumerable<IList<Vector2>> All => polys.Values;
        }

        public static Geo BuildGeo(CityLayout l)
        {
            var g = new Geo();
            foreach (var p in l.polygons) g.polys[p.id] = CityGeometry.ToVectors(p.points);
            g.bbox = CityGeometry.BoundingBox(g.All);
            return g;
        }

        /// <summary>Cantos inferiores do retângulo nativo com centro inferior bc.</summary>
        public static (Vector2 left, Vector2 right) BottomCorners(Vector2 bc, float widthU) =>
            (new Vector2(bc.x - widthU / 2f, bc.y), new Vector2(bc.x + widthU / 2f, bc.y));

        /// <summary>Id do polígono que contém os dois pontos, ou null.</summary>
        public static string ContainingPolygon(Geo g, Vector2 a, Vector2 b)
        {
            foreach (var kv in g.polys)
                if (CityGeometry.PointInPolygon(a, kv.Value) && CityGeometry.PointInPolygon(b, kv.Value)) return kv.Key;
            return null;
        }

        /// <summary>Waypoint de entrada esperado (R14) ou null se não há rua ao sul (R13).</summary>
        public static Vector2? ExpectedEntry(Geo g, Vector2 bottomCenter)
        {
            if (!CityGeometry.SouthStreetSegment(bottomCenter, g.All, g.bbox.yMin, out var top, out var bottom)) return null;
            return new Vector2(bottomCenter.x, (top + bottom) / 2f);
        }

        public static ValidationReport Validate(CityLayout l, SpriteCatalog cat)
        {
            var r = new ValidationReport { mode = "layout" };
            var g = BuildGeo(l);
            r.notes.Add($"R08 (parcial): semente {l.seed}; 'não escolhido à mão' não é automatizável.");

            // ids únicos
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

                var (a, b) = BottomCorners(bc, cs.widthU);
                var pid = ContainingPolygon(g, a, b);
                if (pid == null) r.Add("R12", obj, bc, "cantos inferiores não estão no mesmo quarteirão");
                else structPoly[s] = pid;

                var expected = ExpectedEntry(g, bc);
                if (expected == null) r.Add("R13", obj, bc, "sem rua ao sul dentro do Limiter");
                else
                {
                    Vector2 e = s.entry;
                    if (Vector2.Distance(e, expected.Value) > SizeTolerance) r.Add("R14", obj, e, $"entry {s.entry} difere do esperado {(Vec2)expected.Value}");
                    if (g.All.Any(p => CityGeometry.PointInPolygon(e, p, 0f) && CityGeometry.DistanceToBoundary(e, p) > CityGeometry.Eps))
                        r.Add("R14", obj, e, "entry dentro de um polígono");
                }
            }

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
            if (nDeco < 2 * nHouse || nDeco > 3 * nHouse) r.Add("R19", "Trees", Vector2.zero, $"{nDeco} decorações para {nHouse} House (esperado {2 * nHouse}–{3 * nHouse})");
            int nearCurb = 0;
            foreach (var d in l.decorations)
            {
                string obj = "Tree " + d.id;
                Vector2 bc = d.bottomCenter;
                if (!d.sprite.name.EndsWith("_S") || !cat.InDecorationPool(d.sprite)) r.Add("R17", obj, bc, $"sprite '{d.sprite.name}' fora do pool de decoração");
                var cs = cat.Get(d.sprite);
                if (cs == null) { r.Add("R18", obj, bc, "sprite inexistente: tamanho nativo indeterminável"); continue; }
                var (a, b) = BottomCorners(bc, cs.widthU);
                var pid = ContainingPolygon(g, a, b);
                if (pid == null) { r.Add("R20", obj, bc, "cantos inferiores não estão no mesmo polígono"); continue; }
                if (CityGeometry.DistanceToBoundary(bc, g.polys[pid]) <= CurbDistance + 1e-4f) nearCurb++;
            }
            if (nDeco > 0 && nearCurb < 0.7f * nDeco - 1e-4f) r.Add("R21", "Trees", Vector2.zero, $"{nearCurb}/{nDeco} ({100f * nearCurb / nDeco:0.#}%) a ≤ 0,5 u da borda (mín. 70%)");

            // polígonos
            foreach (var kv in g.polys)
            {
                bool isBlock = blocks.Contains(kv.Key);
                if (isBlock && !CityGeometry.IsAxisAlignedRectangle(kv.Value)) r.Add("R23", "Polygon " + kv.Key, Centroid(kv.Value), "quarteirão não é retângulo de 4 vértices alinhado a X/Y");
                if (!isBlock && kv.Value.Count < 3) r.Add("R24", "Polygon " + kv.Key, Centroid(kv.Value), "polígono decorativo com menos de 3 vértices");
            }
            var ids = g.polys.Keys.ToList();
            for (int i = 0; i < ids.Count; i++)
                for (int j = i + 1; j < ids.Count; j++)
                {
                    float dist = CityGeometry.PolygonDistance(g.polys[ids[i]], g.polys[ids[j]]);
                    if (dist < StreetMin - 1e-3f) r.Add("R25", $"Polygon {ids[i]} × {ids[j]}", Centroid(g.polys[ids[i]]), dist <= CityGeometry.Eps ? "polígonos se sobrepõem" : $"distância {dist:0.###} u (mín. 0,8)");
                }

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
