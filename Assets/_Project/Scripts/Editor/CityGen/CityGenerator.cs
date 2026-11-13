using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Gera um CityLayout determinístico a partir de uma semente (docs/plan-citygen.md, seção a).
    /// Quarteirões retangulares em faixas, ruas de 0,8 u, praças na faixa sul, cidade centrada em 'center'.
    /// </summary>
    public static class CityGenerator
    {
        public const float Street = 0.8f;        // R25 / decisão: todas as ruas 0,8 u
        public const float Margin = 0.3f;        // folga interna do quarteirão
        public const float MaxGap = 0.5f;        // decisão: espaço entre estruturas 0–0,5 u
        public const float CurbOffset = 0.3f;    // decoração rente à borda (R21: ≤ 0,5)
        public const float SlotCurb = 0.2f;      // árvore de calçada entre casas
        public const float PlazaHeight = 4.5f;
        public const float Chamfer = 0.6f;       // praças em octógono (forma livre, R24)
        public const float SlotChance = 0.35f;
        public const float TopDecoMaxHeight = 1.6f;
        public const float InteriorShareMax = 0.2f;

        class Item
        {
            public string type;          // House/Guard/Warehouse ou null para vaga de árvore
            public CatalogSprite sprite; // estrutura ou decoração (vaga)
            public float gap;            // espaço antes do item
            public float Width => type == null ? sprite.widthU + 0.2f : sprite.widthU;
        }

        class Block
        {
            public string district;
            public readonly List<Item> south = new List<Item>(), north = new List<Item>();
            public float x, y;
            static float RowWidth(List<Item> row) => row.Sum(i => i.Width) + row.Skip(1).Sum(i => i.gap);
            static float RowHeight(List<Item> row) => row.Where(i => i.type != null).Select(i => i.sprite.heightU).DefaultIfEmpty(0f).Max();
            public float W => Mathf.Max(RowWidth(south), RowWidth(north)) + 2 * Margin;
            public float RowStep => RowHeight(south) * 0.5f;
            public float H => north.Count > 0 ? Margin + RowStep + RowHeight(north) + Margin : Margin + RowHeight(south) + Margin;
            public Rect Rect => new Rect(x, y, W, H);
        }

        public static CityLayout Generate(int seed, Vector2 center, SpriteCatalog cat, string generatedAt)
        {
            var rng = new System.Random(seed);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            T Pick<T>(IList<T> list) => list[rng.Next(list.Count)];

            // 1. casas por bairro (R01, R09)
            int n = rng.Next(50, 81);
            int nSub = Mathf.RoundToInt(0.60f * n), nCom = Mathf.RoundToInt(0.25f * n), nInd = n - nSub - nCom;
            var counts = new Dictionary<string, int> { { District.Suburban, nSub }, { District.Commercial, nCom }, { District.Industrial, nInd } };

            // 2. quarteirões de um bairro só (R10): 1 ou 2 fileiras, 3–5 casas por fileira
            var districtOrder = new List<string> { District.Suburban, District.Commercial, District.Industrial };
            Shuffle(districtOrder, rng); // bairros em posição sorteada
            var blocks = new List<Block>();
            foreach (var d in districtOrder)
            {
                var mine = new List<Block>();
                int left = counts[d];
                while (left > 0)
                {
                    var b = new Block { district = d };
                    int rows = rng.Next(1, 3);
                    for (int r = 0; r < rows && left > 0; r++)
                    {
                        var row = r == 0 ? b.south : b.north;
                        int per = Mathf.Min(rng.Next(3, 6), left);
                        for (int k = 0; k < per; k++)
                        {
                            row.Add(new Item { type = StructureType.House, sprite = Pick(cat.HousePool(d)), gap = Range(0f, MaxGap) });
                            left--;
                            // árvore de calçada entre casas, só na fileira sul (rente à borda)
                            if (r == 0 && k < per - 1 && rng.NextDouble() < SlotChance)
                                row.Add(new Item { type = null, sprite = Pick(cat.Decoration), gap = Range(0f, MaxGap) });
                        }
                    }
                    mine.Add(b);
                }
                Shuffle(mine, rng);
                blocks.AddRange(mine);
            }

            // 3. Guard num quarteirão industrial (R33)
            var indBlocks = blocks.Where(b => b.district == District.Industrial).ToList();
            Pick(indBlocks).south.Add(new Item { type = StructureType.Guard, sprite = Pick(cat.GuardWarehouse), gap = Range(0f, MaxGap) });

            // 4. faixas (bandas) com proporção ~16:9
            float area = blocks.Sum(b => (b.W + Street) * (b.H + Street));
            float targetW = Mathf.Sqrt(area * 16f / 9f);
            var bands = new List<List<Block>>();
            var cur = new List<Block>(); float cx = 0f;
            foreach (var b in blocks)
            {
                if (cur.Count > 0 && cx + b.W > targetW) { bands.Add(cur); cur = new List<Block>(); cx = 0f; }
                cur.Add(b); cx += b.W + Street;
            }
            if (cur.Count > 0) bands.Add(cur);

            // 5. Warehouses nos quarteirões mais próximos de cantos opostos (R32)
            var wareSprites = new[] { Pick(cat.GuardWarehouse), Pick(cat.GuardWarehouse) };
            var wareGaps = new[] { Range(0f, MaxGap), Range(0f, MaxGap) };
            int pairChoice = rng.Next(2);
            int plazaCount = rng.Next(2, 5); // decisão: 2–4 praças
            Block[] wareAt = null;
            List<Rect> plazas = null;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                plazas = Place(bands, plazaCount);
                var bb = Bounds(blocks, plazas);
                var cornersA = pairChoice == 0 ? new[] { new Vector2(bb.xMin, bb.yMin), new Vector2(bb.xMax, bb.yMax) }
                                               : new[] { new Vector2(bb.xMin, bb.yMax), new Vector2(bb.xMax, bb.yMin) };
                var target = cornersA.Select(c => blocks.OrderBy(b => CornerDistance(c, b.Rect)).ThenBy(b => blocks.IndexOf(b)).First()).ToArray();
                if (target[0] == target[1]) { pairChoice = 1 - pairChoice; continue; }
                if (wareAt != null && wareAt[0] == target[0] && wareAt[1] == target[1]) break;
                if (wareAt != null) foreach (var b in wareAt) b.south.RemoveAll(i => i.type == StructureType.Warehouse);
                wareAt = target;
                for (int k = 0; k < 2; k++)
                    wareAt[k].south.Add(new Item { type = StructureType.Warehouse, sprite = wareSprites[k], gap = wareGaps[k] });
            }
            plazas = Place(bands, plazaCount);

            // 6. monta o layout (coordenadas locais; depois centraliza)
            var layout = new CityLayout { seed = seed, generatedAt = generatedAt, streetWidth = Street, center = center };
            int pid = 0; var ids = new Dictionary<string, int>();
            string NextId(string t) { ids[t] = ids.TryGetValue(t, out var v) ? v + 1 : 1; return ids[t].ToString("000"); }

            int nDeco = rng.Next(2 * n, 3 * n + 1); // R19
            var slotDecos = new List<DecorationData>();
            var topDecos = new List<DecorationData>();
            var smallDeco = cat.Decoration.Where(d => d.heightU <= TopDecoMaxHeight).ToList();
            if (smallDeco.Count == 0) smallDeco = cat.Decoration;

            foreach (var b in blocks)
            {
                var poly = RectPoly(b.Rect, (++pid).ToString("000"), PolygonKind.Block, b.district);
                layout.polygons.Add(poly);
                PlaceRow(b.south, b.x + Margin, b.y + Margin, b.y + SlotCurb, poly.id, layout, slotDecos, NextId);
                PlaceRow(b.north, b.x + Margin, b.y + Margin + b.RowStep, b.y + Margin + b.RowStep, poly.id, layout, slotDecos, NextId);
                int nTop = Mathf.FloorToInt(b.W / 3f);
                for (int j = 0; j < nTop; j++)
                {
                    var cs = Pick(smallDeco);
                    float x = b.x + b.W * (j + 0.5f) / nTop + Range(-0.3f, 0.3f);
                    x = Mathf.Clamp(x, b.x + cs.widthU / 2f + 0.05f, b.x + b.W - cs.widthU / 2f - 0.05f);
                    topDecos.Add(new DecorationData { polygonId = poly.id, sprite = cs.ToRef(), bottomCenter = new Vec2(x, b.y + b.H - CurbOffset) });
                }
            }
            while (slotDecos.Count + topDecos.Count > nDeco && topDecos.Count > 0) topDecos.RemoveAt(topDecos.Count - 1);
            while (slotDecos.Count > nDeco) slotDecos.RemoveAt(slotDecos.Count - 1);

            var plazaPolys = new List<PolygonData>();
            foreach (var r in plazas)
            {
                var p = OctagonPoly(r, (++pid).ToString("000"));
                layout.polygons.Add(p); plazaPolys.Add(p);
            }
            int remaining = nDeco - slotDecos.Count - topDecos.Count;
            int interior = Mathf.Min(remaining, Mathf.FloorToInt(InteriorShareMax * nDeco));
            var plazaDecos = new List<DecorationData>();
            for (int k = 0; k < remaining; k++)
            {
                int pi = rng.Next(plazas.Count);
                var r = plazas[pi];
                var cs = Pick(cat.Decoration);
                float x = Range(r.xMin + Chamfer + cs.widthU / 2f + 0.05f, r.xMax - Chamfer - cs.widthU / 2f - 0.05f);
                float y;
                if (k < interior) y = Range(r.yMin + 0.9f, r.yMax - 0.9f);
                else y = rng.Next(2) == 0 ? r.yMin + CurbOffset : r.yMax - CurbOffset;
                plazaDecos.Add(new DecorationData { polygonId = plazaPolys[pi].id, sprite = cs.ToRef(), bottomCenter = new Vec2(x, y) });
            }
            foreach (var d in slotDecos.Concat(topDecos).Concat(plazaDecos)) { d.id = NextId("Tree"); layout.decorations.Add(d); }

            // 7. centraliza a caixa dos polígonos em 'center' (R34)
            var bbAll = CityGeometry.BoundingBox(layout.polygons.Select(p => (IList<Vector2>)CityGeometry.ToVectors(p.points)));
            Vector2 off = center - bbAll.center;
            foreach (var p in layout.polygons) for (int i = 0; i < p.points.Count; i++) p.points[i] = (Vector2)p.points[i] + off;
            foreach (var s in layout.structures) s.bottomCenter = (Vector2)s.bottomCenter + off;
            foreach (var d in layout.decorations) d.bottomCenter = (Vector2)d.bottomCenter + off;

            // 8. waypoints de entrada (R14)
            var geo = LayoutValidator.BuildGeo(layout);
            foreach (var s in layout.structures)
            {
                var e = LayoutValidator.ExpectedEntry(geo, s.bottomCenter);
                s.entry = e ?? (Vector2)s.bottomCenter;
            }
            return layout;
        }

        static void PlaceRow(List<Item> row, float x0, float houseY, float slotY, string polyId, CityLayout layout, List<DecorationData> slotDecos, Func<string, string> nextId)
        {
            float cursor = x0;
            for (int i = 0; i < row.Count; i++)
            {
                var it = row[i];
                if (i > 0) cursor += it.gap;
                float cx = cursor + it.Width / 2f;
                if (it.type == null)
                    slotDecos.Add(new DecorationData { polygonId = polyId, sprite = it.sprite.ToRef(), bottomCenter = new Vec2(cx, slotY) });
                else
                    layout.structures.Add(new StructureData
                    {
                        id = nextId(it.type), type = it.type, district = it.sprite.category, polygonId = polyId,
                        sprite = it.sprite.ToRef(), bottomCenter = new Vec2(cx, houseY)
                    });
                cursor += it.Width;
            }
        }

        /// <summary>Posiciona as faixas de baixo para cima e devolve os retângulos das praças (faixa sul).</summary>
        static List<Rect> Place(List<List<Block>> bands, int k)
        {
            float y = 0f, width = 0f;
            foreach (var band in bands)
            {
                float x = 0f, h = 0f;
                foreach (var b in band) { b.x = x; b.y = y; x += b.W + Street; h = Mathf.Max(h, b.H); }
                width = Mathf.Max(width, x - Street);
                y += h + Street;
            }
            // 2–4 praças na faixa sul (decisão), com ruas de 0,8 u entre elas
            float pw = (width - Street * (k - 1)) / k;
            var plazas = new List<Rect>();
            for (int i = 0; i < k; i++) plazas.Add(new Rect(i * (pw + Street), -Street - PlazaHeight, pw, PlazaHeight));
            return plazas;
        }

        static Rect Bounds(List<Block> blocks, List<Rect> plazas)
        {
            var all = blocks.Select(b => b.Rect).Concat(plazas).ToList();
            return Rect.MinMaxRect(all.Min(r => r.xMin), all.Min(r => r.yMin), all.Max(r => r.xMax), all.Max(r => r.yMax));
        }

        static float CornerDistance(Vector2 c, Rect r)
        {
            float dx = Mathf.Max(r.xMin - c.x, 0f, c.x - r.xMax), dy = Mathf.Max(r.yMin - c.y, 0f, c.y - r.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static PolygonData RectPoly(Rect r, string id, string kind, string district)
        {
            var p = new PolygonData { id = id, kind = kind, district = district };
            p.points.Add(new Vec2(r.xMin, r.yMin)); p.points.Add(new Vec2(r.xMax, r.yMin));
            p.points.Add(new Vec2(r.xMax, r.yMax)); p.points.Add(new Vec2(r.xMin, r.yMax));
            return p;
        }

        static PolygonData OctagonPoly(Rect r, string id)
        {
            float c = Chamfer;
            var p = new PolygonData { id = id, kind = PolygonKind.Plaza, district = District.None };
            p.points.Add(new Vec2(r.xMin + c, r.yMin)); p.points.Add(new Vec2(r.xMax - c, r.yMin));
            p.points.Add(new Vec2(r.xMax, r.yMin + c)); p.points.Add(new Vec2(r.xMax, r.yMax - c));
            p.points.Add(new Vec2(r.xMax - c, r.yMax)); p.points.Add(new Vec2(r.xMin + c, r.yMax));
            p.points.Add(new Vec2(r.xMin, r.yMax - c)); p.points.Add(new Vec2(r.xMin, r.yMin + c));
            return p;
        }

        static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (list[i], list[j]) = (list[j], list[i]); }
        }

        /// <summary>Gera com sementes novas até obter um layout sem erros (máx. 20 tentativas).</summary>
        public static CityLayout GenerateValid(Vector2 center, SpriteCatalog cat, out ValidationReport report, int? baseSeed = null)
        {
            var seeds = new System.Random(baseSeed ?? Environment.TickCount);
            CityLayout layout = null; report = null;
            for (int i = 0; i < 20; i++)
            {
                int seed = seeds.Next(1, int.MaxValue);
                layout = Generate(seed, center, cat, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                report = LayoutValidator.Validate(layout, cat);
                if (report.errors == 0) return layout;
            }
            return layout;
        }
    }
}
