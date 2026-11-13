using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Gera um CityLayout determinístico a partir de uma semente.
    ///
    /// Desenho da cidade (v2):
    /// - Malha de 5 faixas horizontais de quarteirões, com TODAS as ruas na mesma largura, mais uma rua de contorno.
    /// - Duas ruas norte-sul atravessam a cidade e ladeiam um parque central (2 faixas de altura).
    /// - Centro comercial em volta do parque, indústria no canto nordeste (com a Guarda), depósitos nos cantos
    ///   noroeste e sudeste, bosque no canto sudoeste e bairros residenciais no restante.
    /// - Estruturas alinhadas pela base VISÍVEL do sprite, rente à rua ao sul, sem se sobrepor.
    /// - Decoração temática por bairro: quintais com árvores e cercas, carros na garagem, postes, semáforos,
    ///   tanques e chaminés na indústria, viaturas na Guarda, contêineres nos depósitos.
    /// </summary>
    public static class CityGenerator
    {
        public const float Street = 0.8f;
        public const float Setback = 0.12f;      // base visível da estrutura até a rua ao sul
        public const float TopOverhang = 0.6f;   // quanto uma decoração pode passar da borda norte do quarteirão
        const string Res = "Res", Com = "Com", Ind = "Ind", IndGuard = "IndGuard", Depot = "Depot", Park = "Park", Woods = "Woods";

        class Block
        {
            public Rect r; public string role; public string polyId; public int row;
            public bool parking, westEnd, eastItem, westItem;
            public float MarginL(float normal) => westItem ? 1.15f : normal;
            public float MarginR(float normal) => eastItem ? (row >= 2 && row <= 4 ? 0.85f : 1.2f) : normal;
            public readonly List<Rect> structs = new List<Rect>();
            /// <summary>Decorações "sólidas" (carros, contêineres, cercas, canteiros): não se sobrepõem entre si.</summary>
            public readonly List<Rect> solids = new List<Rect>();
        }

        class Slot
        {
            public CatalogSprite cs; public string type; public string district;
            public float gapBefore, baseDy, x;
            public bool IsStructure => type != null;
        }

        class Deck
        {
            readonly List<CatalogSprite> all; readonly System.Random rng; List<CatalogSprite> left = new List<CatalogSprite>();
            public Deck(IEnumerable<CatalogSprite> items, System.Random rng) { all = items.Where(i => i != null).ToList(); this.rng = rng; }
            public int Count => all.Count;
            void Refill() { left = all.ToList(); for (int i = left.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (left[i], left[j]) = (left[j], left[i]); } }
            public CatalogSprite Next() => NextWhere(_ => true);
            /// <summary>Próximo sprite do baralho que satisfaz o filtro (sem repetir até esgotar), ou null.</summary>
            public CatalogSprite NextWhere(Func<CatalogSprite, bool> ok)
            {
                if (all.Count == 0) return null;
                for (int pass = 0; pass < 2; pass++)
                {
                    if (left.Count == 0) Refill();
                    int i = left.FindIndex(c => ok(c));
                    if (i >= 0) { var c = left[i]; left.RemoveAt(i); return c; }
                    left.Clear();
                }
                return null;
            }
        }

        class Gen
        {
            public System.Random rng; public SpriteCatalog cat; public CityLayout layout;
            public readonly List<Block> blocks = new List<Block>();
            public readonly List<(Rect rect, float baseY)> structRects = new List<(Rect, float)>();
            public readonly Dictionary<string, int> ids = new Dictionary<string, int>();
            public Deck sub, comWide, comTower, ind, cars, yardTrees, parkTrees, pines, bushes, flowers, smallRocks, bigRocks, monuments, stumps, mushrooms, palms;

            public float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            public bool Chance(float p) => rng.NextDouble() < p;
            public T Pick<T>(IList<T> l) => l[rng.Next(l.Count)];
            public string NextId(string t) { ids[t] = ids.TryGetValue(t, out var v) ? v + 1 : 1; return ids[t].ToString("000"); }
            public CatalogSprite T(string texture) => cat.ByTexture(texture);
            public Deck D(params string[] textures) => new Deck(textures.Select(T), rng);
            public Deck P(string category, params string[] prefixes) => new Deck(prefixes.SelectMany(p => cat.ByPrefix(category, p)).Distinct(), rng);
        }

        public static CityLayout Generate(int seed, Vector2 center, SpriteCatalog cat, string generatedAt)
        {
            var g = new Gen { rng = new System.Random(seed), cat = cat };
            g.layout = new CityLayout { seed = seed, generatedAt = generatedAt, streetWidth = Street, limiterMargin = Street, center = center };
            BuildDecks(g);
            BuildBlocks(g);
            foreach (var b in g.blocks)
            {
                var poly = new PolygonData { id = g.NextId("Polygon"), kind = b.role == Park || b.role == Woods ? PolygonKind.Plaza : PolygonKind.Block, district = DistrictOf(b.role) };
                poly.points.Add(new Vec2(b.r.xMin, b.r.yMin)); poly.points.Add(new Vec2(b.r.xMax, b.r.yMin));
                poly.points.Add(new Vec2(b.r.xMax, b.r.yMax)); poly.points.Add(new Vec2(b.r.xMin, b.r.yMax));
                g.layout.polygons.Add(poly);
                b.polyId = poly.id;
            }
            foreach (var b in g.blocks)
            {
                switch (b.role)
                {
                    case Res: FillResidential(g, b); break;
                    case Com: FillCommercial(g, b); break;
                    case Ind: case IndGuard: FillIndustrial(g, b); break;
                    case Depot: FillDepot(g, b); break;
                    case Park: FillPark(g, b); break;
                    case Woods: FillWoods(g, b); break;
                }
            }
            StreetFurniture(g);
            Finish(g, center);
            return g.layout;
        }

        static string DistrictOf(string role) => role switch
        {
            Res => District.Suburban, Com => District.Commercial, Ind => District.Industrial, IndGuard => District.Industrial, Depot => District.Industrial, _ => District.None
        };

        // ------------------------------------------------------------------ baralhos de sprites

        static void BuildDecks(Gen g)
        {
            g.sub = new Deck(g.cat.Suburban, g.rng);
            g.comWide = g.P("Commercial", "Predio_", "Arranha_Ceu_");
            g.comTower = g.P("Commercial", "Fundo_");
            g.ind = new Deck(g.cat.Industrial, g.rng);
            g.cars = g.D("Sedan", "Sedan_Esportivo", "SUV", "SUV_Luxo", "Hatch_Esportivo", "Picape", "Taxi", "Van");
            g.yardTrees = g.D("Arvore_Quintal", "Arvore_Quintal_Baixa", "Arvore", "Arvore_Redonda", "Arvore_Simples", "Arvore_Alta", "Arvore_Cone", "Arvore_Chapeu", "Arvore_Fina", "Carvalho", "Pinheiro", "Pinheiro_Alto_A");
            g.parkTrees = g.D("Arvore", "Arvore_Alta", "Arvore_Blocos", "Arvore_Chapeu", "Arvore_Cone", "Arvore_Fina", "Arvore_Galhos", "Arvore_Gorda", "Arvore_Redonda", "Arvore_Simples", "Carvalho", "Arvore_Quintal", "Arvore_Quintal_Baixa");
            g.pines = g.P("Nature", "Pinheiro");
            g.bushes = g.D("Arbusto", "Arbusto_Folhas", "Arbusto_Grande", "Arbusto_Pequeno", "Arbusto_Pontudo", "Arbusto_Pontudo_Grande", "Folhagem", "Planta_Alta", "Planta_Baixa", "Capim");
            g.flowers = g.D("Flor_Margarida", "Flor_Sino", "Flor_Tulipa");
            g.smallRocks = g.D("Pedra_A", "Pedra_B", "Pedra_C", "Pedra_E", "Pedra_F", "Pedra_Chata_B", "Pedra_Chata_C");
            g.bigRocks = g.P("Nature", "Rocha_Alta_", "Pedra_Grande_", "Pedra_Topo_");
            g.monuments = g.D("Estatua_Anel", "Estatua_Cabeca", "Estatua_Bloco", "Obelisco");
            g.stumps = g.D("Toco_Quadrado", "Toco_Raizes", "Toco_Redondo", "Toco_Velho", "Tronco_Oco");
            g.mushrooms = g.D("Cogumelo", "Cogumelo_Alto", "Cogumelo_Chapeu", "Cogumelo_Grupo");
            g.palms = g.D("Palmeira", "Palmeira_Cheia", "Palmeira_Cruz", "Palmeira_Curva");
        }

        // ------------------------------------------------------------------ malha de quarteirões

        static float R1(float v) => Mathf.Round(v * 10f) / 10f;

        static void BuildBlocks(Gen g)
        {
            float S = Street;
            float W = R1(g.Range(50f, 54f));
            float pw = R1(g.Range(13.6f, 15.2f));
            float L = R1((W - pw - 2 * S) * g.Range(0.46f, 0.54f));
            float xMidA = L + S, xMidB = xMidA + pw, xRight = xMidB + S;
            const int Rows = 6;
            float[] depth =
            {
                R1(g.Range(4.8f, 5.0f)), R1(g.Range(4.7f, 4.9f)), R1(g.Range(4.5f, 4.7f)),
                R1(g.Range(4.5f, 4.7f)), R1(g.Range(4.8f, 5.0f)), R1(g.Range(5.2f, 5.4f))
            };
            var y = new float[Rows];
            for (int i = 1; i < Rows; i++) y[i] = y[i - 1] + depth[i - 1] + S;

            void Add(int row, float xa, float xb, string role, Action<Block> cfg = null)
            {
                var b = new Block { r = Rect.MinMaxRect(xa, y[row], xb, y[row] + depth[row]), role = role, row = row };
                cfg?.Invoke(b);
                g.blocks.Add(b);
            }
            // corte de um trecho [xa, xb] em 2 quarteirões; evita quase-alinhamento com o corte da faixa de baixo
            float lastCut = float.NaN;
            float Cut(float xa, float xb, float fracMin, float fracMax)
            {
                float c = R1(xa + (xb - xa - S) * g.Range(fracMin, fracMax));
                if (!float.IsNaN(lastCut) && Mathf.Abs(c - lastCut) > 0.01f && Mathf.Abs(c - lastCut) < 2.2f)
                    c = lastCut + (c >= lastCut ? 2.2f : -2.2f);
                c = Mathf.Clamp(c, xa + 6.2f, xb - S - 6.2f);
                lastCut = c;
                return c;
            }
            void Maybe(int row, float xa, float xb, string role, float chance)
            {
                if (g.Chance(chance)) { float c = Cut(xa, xb, 0.4f, 0.6f); Add(row, xa, c, role); Add(row, c + S, xb, role); }
                else Add(row, xa, xb, role);
            }

            // --- faixa esquerda (oeste)
            lastCut = float.NaN;
            float c0 = Cut(0, L, 0.44f, 0.52f); Add(0, 0, c0, Woods); Add(0, c0 + S, L, Res);
            Maybe(1, 0, L, Res, 0.6f);
            float c2 = Cut(0, L, 0.42f, 0.56f); Add(2, 0, c2, Res); Add(2, c2 + S, L, Com);
            float c3 = Cut(0, L, 0.42f, 0.56f); Add(3, 0, c3, Res); Add(3, c3 + S, L, Com);
            Maybe(4, 0, L, Res, 0.6f);
            float c5 = Cut(0, L, 0.46f, 0.54f); Add(5, 0, c5, Depot, b => b.westEnd = true); Add(5, c5 + S, L, Res);

            // --- faixa do meio (parque nas faixas 2 e 3)
            lastCut = float.NaN;
            Maybe(0, xMidA, xMidB, Res, 0.5f);
            Add(1, xMidA, xMidB, Res);
            g.blocks.Add(new Block { r = Rect.MinMaxRect(xMidA, y[2], xMidB, y[3] + depth[3]), role = Park, row = 2 });
            Add(4, xMidA, xMidB, Com, b => b.parking = true);
            lastCut = float.NaN;
            Maybe(5, xMidA, xMidB, Res, 0.5f);

            // --- faixa direita (leste)
            lastCut = float.NaN;
            float e0 = Cut(xRight, W, 0.46f, 0.54f); Add(0, xRight, e0, Res); Add(0, e0 + S, W, Depot);
            Maybe(1, xRight, W, Res, 0.6f);
            float e2 = Cut(xRight, W, 0.44f, 0.58f); Add(2, xRight, e2, Com); Add(2, e2 + S, W, Res);
            float e3 = Cut(xRight, W, 0.44f, 0.58f); Add(3, xRight, e3, Com); Add(3, e3 + S, W, Ind);
            Add(4, xRight, W, Ind);
            Add(5, xRight, W, IndGuard);

            // esquinas voltadas para as duas ruas que atravessam a cidade recebem semáforo ou placa
            foreach (var b in g.blocks)
            {
                if (b.role == Park || b.role == Woods || b.role == Depot) continue;
                b.eastItem = Mathf.Abs(b.r.xMax - L) < 0.01f || Mathf.Abs(b.r.xMax - xMidB) < 0.01f;
                b.westItem = !b.eastItem && Mathf.Abs(b.r.xMin - xRight) < 0.01f && g.Chance(0.3f);
            }
        }

        // ------------------------------------------------------------------ utilitários de posicionamento

        static StructureData AddStructure(Gen g, Block b, CatalogSprite cs, string type, float baseX, float baseY)
        {
            var s = new StructureData
            {
                id = g.NextId(type), type = type, district = cs.category, polygonId = b.polyId, sprite = cs.ToRef(),
                bottomCenter = new Vec2(baseX, baseY)
            };
            g.layout.structures.Add(s);
            var rect = cs.VisibleRect(new Vector2(baseX, baseY));
            b.structs.Add(rect);
            g.structRects.Add((rect, baseY));
            return s;
        }

        /// <summary>Topo mais alto das estruturas do quarteirão que cruzam a faixa [xa, xb] (ou a base do quarteirão).</summary>
        static float TopAt(Block b, float xa, float xb)
        {
            float top = b.r.yMin;
            foreach (var r in b.structs) if (r.xMax > xa && r.xMin < xb) top = Mathf.Max(top, r.yMax);
            return top;
        }

        /// <summary>
        /// Adiciona uma decoração se couber: base dentro do quarteirão, copa sem passar muito da borda norte e
        /// sem cobrir estrutura que esteja à sua frente.
        /// </summary>
        static bool AddDeco(Gen g, Block b, CatalogSprite cs, float baseX, float baseY, float overhang = TopOverhang, bool solid = false)
        {
            if (cs == null) return false;
            float half = cs.visWU / 2f;
            if (baseX - half < b.r.xMin + 0.03f || baseX + half > b.r.xMax - 0.03f) return false;
            if (baseY < b.r.yMin + 0.03f || baseY > b.r.yMax - 0.05f) return false;
            if (baseY + cs.visHU > b.r.yMax + overhang) return false;
            var rect = cs.VisibleRect(new Vector2(baseX, baseY));
            foreach (var s in b.structs)
                if (baseY > s.yMin + 0.02f && rect.xMax > s.xMin + 0.05f && rect.xMin < s.xMax - 0.05f && rect.yMax > s.yMin + 0.05f && rect.yMin < s.yMax - 0.05f)
                    return false;
            if (solid)
            {
                foreach (var o in b.solids.Concat(b.structs))
                    if (rect.xMax > o.xMin + 0.04f && rect.xMin < o.xMax - 0.04f && rect.yMax > o.yMin + 0.04f && rect.yMin < o.yMax - 0.04f) return false;
                b.solids.Add(rect);
            }
            else
            {
                // tronco/pé não nasce de dentro de um carro, canteiro ou cerca
                foreach (var o in b.solids)
                    if (baseX > o.xMin - 0.05f && baseX < o.xMax + 0.05f && baseY > o.yMin + 0.05f && baseY < o.yMax) return false;
            }
            g.layout.decorations.Add(new DecorationData { polygonId = b.polyId, sprite = cs.ToRef(), bottomCenter = new Vec2(baseX, baseY) });
            return true;
        }

        /// <summary>Distribui os itens da fileira entre xa e xb: sobra vai para os vãos (até maxExtra) e depois para as pontas.</summary>
        static void LayoutRow(Gen g, List<Slot> slots, float xa, float xb, float maxExtra)
        {
            if (slots.Count == 0) return;
            float total = slots.Sum(s => s.cs.visWU) + slots.Skip(1).Sum(s => s.gapBefore);
            float leftover = Mathf.Max(0f, xb - xa - total);
            int gaps = slots.Count - 1;
            var extra = new float[slots.Count];
            if (gaps > 0)
            {
                var w = new float[slots.Count]; float ws = 0f;
                for (int i = 1; i < slots.Count; i++) { w[i] = g.Range(0.6f, 1.4f); ws += w[i]; }
                float pool = Mathf.Min(leftover, maxExtra * gaps);
                for (int i = 1; i < slots.Count; i++) extra[i] = Mathf.Min(maxExtra, pool * w[i] / ws);
                leftover -= extra.Sum();
            }
            float cursor = xa + leftover * g.Range(0.35f, 0.65f);
            for (int i = 0; i < slots.Count; i++)
            {
                if (i > 0) cursor += slots[i].gapBefore + extra[i];
                slots[i].x = cursor + slots[i].cs.visWU / 2f;
                cursor += slots[i].cs.visWU;
            }
        }

        static void PlaceRow(Gen g, Block b, List<Slot> slots)
        {
            foreach (var s in slots.Where(s => s.IsStructure)) AddStructure(g, b, s.cs, s.type, s.x, b.r.yMin + Setback + s.baseDy);
            foreach (var s in slots.Where(s => !s.IsStructure)) AddDeco(g, b, s.cs, s.x, b.r.yMin + Setback + s.baseDy, 1.2f, true);
        }

        // ------------------------------------------------------------------ residencial

        const int MaxHouses = 78;
        static int HouseCount(Gen g) => g.layout.structures.Count(s => s.type == StructureType.House);

        static void FillResidential(Gen g, Block b)
        {
            float mL = b.MarginL(0.4f), mR = b.MarginR(0.4f), minGap = 0.4f;
            float avail = b.r.width - mL - mR, used = 0f;
            var slots = new List<Slot>();
            int houses = HouseCount(g);
            for (int guard = 0; guard < 30 && houses < MaxHouses; guard++)
            {
                float gap = slots.Count > 0 ? minGap : 0f;
                var cs = g.sub.NextWhere(c => c.visWU + gap <= avail - used);
                if (cs == null) break;
                slots.Add(new Slot { cs = cs, type = StructureType.House, gapBefore = gap, baseDy = g.Range(0f, 0.1f) });
                used += cs.visWU + gap; houses++;
                if (g.Chance(0.25f))
                {
                    var car = g.cars.NextWhere(c => c.visWU + 0.3f + minGap + 2.2f <= avail - used);
                    if (car != null) { slots.Add(new Slot { cs = car, gapBefore = 0.3f, baseDy = g.Range(0.15f, 0.5f) }); used += car.visWU + 0.3f; }
                }
            }
            while (slots.Count > 0 && !slots[slots.Count - 1].IsStructure) slots.RemoveAt(slots.Count - 1);
            LayoutRow(g, slots, b.r.xMin + mL, b.r.xMax - mR, 0.5f);
            PlaceRow(g, b, slots);

            float y0 = b.r.yMin, y1 = b.r.yMax;
            // vãos entre casas: árvore fina mais ao fundo; vaso, flor, planta ou poste na frente
            for (int i = 1; i < slots.Count; i++)
            {
                var a = slots[i - 1]; var c = slots[i];
                if (!a.IsStructure || !c.IsStructure) continue;
                float xa = a.x + a.cs.visWU / 2f, xb = c.x - c.cs.visWU / 2f, gw = xb - xa, xm = (xa + xb) / 2f;
                if (gw >= 1.0f && g.Chance(0.7f))
                {
                    var tree = g.yardTrees.NextWhere(t => t.visWU <= gw - 0.08f);
                    if (tree != null) AddDeco(g, b, tree, xm, y0 + g.Range(1.0f, 1.8f));
                }
                if (gw >= 0.75f && g.Chance(0.7f))
                {
                    var front = g.D("Flor_Margarida", "Flor_Sino", "Flor_Tulipa", "Planta_Baixa", "Planta_Alta", "Folhagem", "Vaso", "Poste_Curvo").NextWhere(t => t.visWU <= gw - 0.08f);
                    if (front != null) AddDeco(g, b, front, xm + g.Range(-0.06f, 0.06f), y0 + g.Range(0.08f, 0.4f), 10f);
                }
            }
            Backyard(g, b, slots, g.yardTrees, 0.92f, 0.55f);
            // canteiro grande em alguns quintais
            if (g.Chance(0.35f))
            {
                var planter = g.T(g.Chance(0.6f) ? "Jardineira" : "Cercado");
                float x = g.Range(b.r.xMin + 1.6f, b.r.xMax - 1.6f);
                float baseY = TopAt(b, x - planter.visWU / 2f, x + planter.visWU / 2f) + 0.2f;
                if (baseY + planter.visHU <= y1 - 0.1f) AddDeco(g, b, planter, x, baseY, 0f, true);
            }
            // cerca ou sebe nos fundos
            float roll = (float)g.rng.NextDouble();
            if (roll < 0.5f)
            {
                var fence = g.T(g.Pick(new[] { "Cerca_Tabuas", "Cerca_Rustica", "Cerca_Trilho", "Cerca_Tabuas_Dupla" }));
                FenceLine(g, b, fence, y1 - fence.visHU - 0.08f, 0.25f);
            }
            else if (roll < 0.85f) Hedge(g, b, 1.35f, 1.8f);
            CornerGarden(g, b, slots, g.yardTrees);
        }

        /// <summary>Fileira de arbustos rente ao fundo do quarteirão.</summary>
        static void Hedge(Gen g, Block b, float stepMin, float stepMax)
        {
            for (float x = b.r.xMin + 0.9f + g.Range(0f, 0.5f); x < b.r.xMax - 0.9f; x += g.Range(stepMin, stepMax))
            {
                var bush = g.bushes.Next();
                float baseY = Mathf.Max(TopAt(b, x - bush.visWU / 2f, x + bush.visWU / 2f) + 0.08f, b.r.yMax - bush.visHU - g.Range(0.0f, 0.2f));
                AddDeco(g, b, bush, x, baseY);
            }
        }

        /// <summary>Árvores atrás das estruturas (sem cobri-las), arbustos onde não há altura e canteiros de flores.</summary>
        static void Backyard(Gen g, Block b, List<Slot> slots, Deck trees, float treeChance, float secondChance)
        {
            float y1 = b.r.yMax;
            foreach (var s in slots.Where(s => s.IsStructure))
            {
                if (!g.Chance(treeChance)) continue;
                int n = s.cs.visWU > 2.4f && g.Chance(secondChance) ? 2 : 1;
                for (int k = 0; k < n; k++)
                {
                    float x = n == 2 ? s.x + (k == 0 ? -1f : 1f) * g.Range(0.5f, 0.9f) : s.x + g.Range(-0.35f, 0.35f) * s.cs.visWU;
                    var tree = trees.NextWhere(t => TopAt(b, x - t.visWU / 2f, x + t.visWU / 2f) + 0.06f + t.visHU <= y1 + TopOverhang);
                    if (tree == null) tree = g.bushes.NextWhere(t => TopAt(b, x - t.visWU / 2f, x + t.visWU / 2f) + 0.08f + t.visHU <= y1 + TopOverhang);
                    if (tree == null) continue;
                    float top = TopAt(b, x - tree.visWU / 2f, x + tree.visWU / 2f);
                    float room = y1 + TopOverhang - tree.visHU - top - 0.06f;
                    AddDeco(g, b, tree, x, top + 0.06f + g.Range(0f, Mathf.Min(0.5f, Mathf.Max(0f, room))));
                }
            }
            // flores no fundo
            int beds = Mathf.Max(1, Mathf.FloorToInt(b.r.width / 4.5f));
            for (int i = 0; i < beds; i++)
            {
                if (!g.Chance(0.75f)) continue;
                float x = g.Range(b.r.xMin + 1.0f, b.r.xMax - 1.0f);
                float yb = Mathf.Max(TopAt(b, x - 1.0f, x + 1.0f) + 0.15f, y1 - g.Range(1.1f, 1.7f));
                var f = g.flowers.Next();
                int count = g.rng.Next(2, 5);
                for (int k = 0; k < count; k++) AddDeco(g, b, f, x + (k - (count - 1) / 2f) * 0.5f + g.Range(-0.08f, 0.08f), yb + g.Range(-0.12f, 0.12f));
            }
        }

        static void FenceLine(Gen g, Block b, CatalogSprite fence, float baseY, float inset)
        {
            if (fence == null) return;
            float span = b.r.width - 2 * inset;
            int n = Mathf.FloorToInt(span / fence.visWU);
            if (n <= 0) return;
            float start = b.r.xMin + inset + (span - n * fence.visWU) / 2f;
            int open = n >= 3 && g.Chance(0.4f) ? g.rng.Next(1, n - 1) : -1;
            for (int i = 0; i < n; i++)
                if (i != open) AddDeco(g, b, fence, start + (i + 0.5f) * fence.visWU, baseY, TopOverhang, true);
        }

        /// <summary>Jardim nas pontas da fileira: árvores escalonadas e um arbusto na frente.</summary>
        static void CornerGarden(Gen g, Block b, List<Slot> slots, Deck trees, bool skipLeft = false)
        {
            if (slots.Count == 0) return;
            var first = slots[0]; var last = slots[slots.Count - 1];
            float leftW = first.x - first.cs.visWU / 2f - b.r.xMin, rightW = b.r.xMax - (last.x + last.cs.visWU / 2f);
            foreach (var (xa, w) in new[] { (b.r.xMin, skipLeft ? 0f : leftW), (b.r.xMax - rightW, rightW) })
            {
                if (w < 1.15f) continue;
                int k = 0;
                for (float x = xa + 0.65f; x <= xa + w - 0.6f; x += g.Range(1.1f, 1.4f), k++)
                {
                    var tree = trees.NextWhere(t => t.visWU <= Mathf.Min(1.5f, w - 0.1f));
                    if (tree != null) AddDeco(g, b, tree, x, b.r.yMin + (k % 2 == 0 ? g.Range(0.9f, 1.4f) : g.Range(0.3f, 0.6f)));
                }
                if (w >= 1.6f) { var bush = g.bushes.Next(); AddDeco(g, b, bush, xa + w / 2f + g.Range(-0.3f, 0.3f), b.r.yMin + g.Range(0.08f, 0.2f), 10f); }
            }
        }

        // ------------------------------------------------------------------ comercial

        static void FillCommercial(Gen g, Block b)
        {
            float y0 = b.r.yMin, y1 = b.r.yMax;
            float xa = b.r.xMin + b.MarginL(0.45f), xb = b.r.xMax - b.MarginR(0.45f);
            // estacionamento numa ponta, com canteiro de arbustos atrás
            if (b.parking)
            {
                var lot = new List<Slot>();
                float lotW = 0f;
                for (int i = 0; i < 4; i++)
                {
                    var car = g.cars.Next();
                    lot.Add(new Slot { cs = car, gapBefore = 0.35f, baseDy = g.Range(0.2f, 0.4f) });
                    lotW += car.visWU + (i > 0 ? 0.35f : 0f);
                }
                LayoutRow(g, lot, xa + 0.2f, xa + 0.2f + lotW, 0f);
                foreach (var s in lot) AddDeco(g, b, s.cs, s.x, y0 + Setback + s.baseDy, 10f, true);
                for (float x = xa + 0.9f; x < xa + lotW; x += 1.6f) { var bush = g.bushes.Next(); AddDeco(g, b, bush, x, y0 + 3.25f + g.Range(0f, 0.2f)); }
                xa += lotW + 1.4f;
            }
            float avail = xb - xa, used = 0f;
            var slots = new List<Slot>();
            bool plaza = false;
            int houses = HouseCount(g);
            for (int guard = 0; guard < 40 && houses < MaxHouses; guard++)
            {
                float gap = slots.Count > 0 ? g.Range(0.14f, 0.32f) : 0f;
                // pracinha com guarda-sol e palmeira
                if (!plaza && false)
                {
                    var sun = g.T("Guarda_Sol");
                    slots.Add(new Slot { cs = sun, gapBefore = 0.7f, baseDy = g.Range(0.35f, 0.6f) });
                    used += sun.visWU + 0.7f; plaza = true;
                    continue;
                }
                if (slots.Count > 0 && !slots[slots.Count - 1].IsStructure) gap = 0.7f;
                bool tower = g.Chance(0.3f);
                var cs = (tower ? g.comTower : g.comWide).NextWhere(c => c.visWU + gap <= avail - used) ?? g.comTower.NextWhere(c => c.visWU + gap <= avail - used);
                if (cs == null) break;
                slots.Add(new Slot { cs = cs, type = StructureType.House, gapBefore = gap, baseDy = 0f });
                used += cs.visWU + gap; houses++;
            }
            while (slots.Count > 0 && !slots[slots.Count - 1].IsStructure) slots.RemoveAt(slots.Count - 1);
            LayoutRow(g, slots, xa, xb, 0.3f);
            PlaceRow(g, b, slots);
            foreach (var s in slots.Where(s => !s.IsStructure))
            {
                float top = y0 + Setback + s.baseDy + s.cs.visHU;
                var palm = g.palms.NextWhere(p => top + 0.05f + p.visHU <= y1 + TopOverhang);
                if (palm != null) AddDeco(g, b, palm, s.x + g.Range(-0.3f, 0.3f), top + 0.05f);
                var fl = g.flowers.Next();
                AddDeco(g, b, fl, s.x - s.cs.visWU / 2f - 0.2f, y0 + 0.12f);
                AddDeco(g, b, fl, s.x + s.cs.visWU / 2f + 0.2f, y0 + 0.12f);
            }
            // fundos: árvores de rua onde houver altura, senão arbustos e vasos
            Backyard(g, b, slots, g.D("Arvore_Redonda", "Arvore_Cone", "Arvore_Simples", "Arvore_Alta", "Arvore_Quintal"), 0.55f, 0f);
            CornerGarden(g, b, slots, g.D("Arvore_Redonda", "Arvore_Cone", "Arvore_Simples", "Arvore_Alta"), b.parking);
            if (g.Chance(0.7f)) Hedge(g, b, 1.5f, 2.3f);
        }

        // ------------------------------------------------------------------ industrial

        static void FillIndustrial(Gen g, Block b)
        {
            float mL = b.MarginL(0.5f), mR = b.MarginR(0.5f), y1 = b.r.yMax;
            float avail = b.r.width - mL - mR, used = 0f;
            var props = g.D("Chamine", "Chamine_Baixa", "Chamine_Listrada", "Caixa_Dagua", "Conteiner", "Tanque", "Torre_Resfriamento", "Tanque_Grande", "Turbina_Eolica");
            var slots = new List<Slot>();
            if (b.role == IndGuard)
            {
                // Guarda com as viaturas estacionadas ao lado
                var hq = g.ind.NextWhere(c => c.visWU >= 2.6f) ?? g.ind.Next();
                slots.Add(new Slot { cs = hq, type = StructureType.Guard });
                used += hq.visWU;
                foreach (var v in new[] { "Caminhao_Bombeiros", "Ambulancia", "Viatura_Policia" })
                {
                    var car = g.T(v); if (car == null) continue;
                    slots.Add(new Slot { cs = car, gapBefore = 0.3f, baseDy = g.Range(0.1f, 0.25f) });
                    used += car.visWU + 0.3f;
                }
            }
            int houses = HouseCount(g);
            for (int guard = 0; guard < 30 && houses < MaxHouses; guard++)
            {
                float gap = slots.Count > 0 ? 0.55f : 0f;
                var cs = g.ind.NextWhere(c => c.visWU + gap <= avail - used);
                if (cs == null) break;
                slots.Add(new Slot { cs = cs, type = StructureType.House, gapBefore = gap, baseDy = g.Range(0f, 0.05f) });
                used += cs.visWU + gap; houses++;
                if (g.Chance(0.65f))
                {
                    var p = props.NextWhere(c => c.visWU + 0.35f + 0.55f + 2.0f <= avail - used && Setback + 0.3f + c.visHU <= b.r.height + 0.3f);
                    if (p != null) { slots.Add(new Slot { cs = p, gapBefore = 0.35f, baseDy = g.Range(0.05f, 0.3f) }); used += p.visWU + 0.35f; }
                }
            }
            // sobra de frente: mais um equipamento (tanque, chaminé, contêiner…)
            for (int extra = 0; extra < 2; extra++)
            {
                var p = props.NextWhere(c => c.visWU + 0.4f <= avail - used && Setback + 0.3f + c.visHU <= b.r.height + 0.3f);
                if (p == null) break;
                slots.Add(new Slot { cs = p, gapBefore = 0.4f, baseDy = g.Range(0.05f, 0.3f) }); used += p.visWU + 0.4f;
            }
            LayoutRow(g, slots, b.r.xMin + mL, b.r.xMax - mR, 0.7f);
            PlaceRow(g, b, slots);
            CornerGarden(g, b, slots, g.pines);
            // fundos: painéis solares e caçambas onde couber
            var back = g.D("Painel_Solar", "Painel_Solar", "Painel_Solar", "Painel_Solar_Plano", "Cacamba_Lixo");
            for (float x = b.r.xMin + 1.8f + g.Range(0f, 0.8f); x < b.r.xMax - 1.6f;)
            {
                var p = back.Next();
                float top = TopAt(b, x - p.visWU / 2f, x + p.visWU / 2f);
                float baseY = Mathf.Max(top + 0.12f, y1 - p.visHU - g.Range(0.08f, 0.2f));
                if (baseY + p.visHU <= y1 + 0.2f) AddDeco(g, b, p, x, baseY, 0.2f, true);
                x += p.visWU + g.Range(0.3f, 1.2f);
            }
        }

        // ------------------------------------------------------------------ depósito

        static void FillDepot(Gen g, Block b)
        {
            float margin = 0.5f, y0 = b.r.yMin, y1 = b.r.yMax;
            var slots = new List<Slot>();
            var wh = g.ind.NextWhere(c => c.visWU >= 2.7f) ?? g.ind.Next();
            var yard = new List<Slot>
            {
                new Slot { cs = g.T("Conteiner"), gapBefore = 0.4f, baseDy = g.Range(0.1f, 0.3f) },
                new Slot { cs = g.T("Conteiner"), gapBefore = 0.12f, baseDy = g.Range(0.1f, 0.3f) },
                new Slot { cs = g.T(g.Chance(0.5f) ? "Caminhao_Bau" : "Caminhao_Carroceria"), gapBefore = 0.4f, baseDy = g.Range(0.15f, 0.4f) },
            };
            yard.RemoveAll(s => s.cs == null);
            var house = new Slot { cs = wh, type = StructureType.Warehouse };
            if (b.westEnd) { slots.Add(house); slots.AddRange(yard); }
            else { yard.Reverse(); foreach (var s in yard) s.gapBefore = 0.3f; slots.AddRange(yard); house.gapBefore = 0.4f; slots.Add(house); }
            slots[0].gapBefore = 0f;
            while (slots.Sum(s => s.cs.visWU) + slots.Skip(1).Sum(s => s.gapBefore) > b.r.width - 2 * margin && slots.Count > 1)
                slots.Remove(slots.First(s => !s.IsStructure));
            LayoutRow(g, slots, b.r.xMin + margin, b.r.xMax - margin, 0.5f);
            PlaceRow(g, b, slots);
            // pilha de caixas num canto dos fundos e cerca alta fechando o pátio
            var crate = g.T("Caixa");
            var fence = g.T("Cerca_Alta");
            float fenceY = y1 - fence.visHU - 0.08f;
            if (crate != null)
            {
                float xc = b.westEnd ? b.r.xMax - 1.3f : b.r.xMin + 1.3f;
                for (int i = 0; i < 3; i++)
                {
                    float x = xc + (i - 1) * 1.05f * (b.westEnd ? -1f : 1f) + g.Range(-0.05f, 0.05f);
                    float baseY = Mathf.Max(TopAt(b, x - crate.visWU / 2f, x + crate.visWU / 2f) + 0.1f, fenceY - crate.visHU + 0.5f - (i % 2) * 0.35f);
                    AddDeco(g, b, crate, x, baseY, 0.2f);
                }
            }
            FenceLine(g, b, fence, fenceY, 0.2f);
        }

        // ------------------------------------------------------------------ parque central

        static void FillPark(Gen g, Block b)
        {
            float x0 = b.r.xMin, x1 = b.r.xMax, y0 = b.r.yMin, y1 = b.r.yMax, cx = b.r.center.x, cy = b.r.center.y;
            // monumento central com colunas e canteiro de flores em volta
            var mon = g.monuments.Next();
            AddDeco(g, b, mon, cx, cy - mon.visHU / 2f + 0.25f, 10f, true);
            var kiosk = g.T("Guarda_Sol");
            float ksx = g.Chance(0.5f) ? 1f : -1f;
            AddDeco(g, b, kiosk, cx - ksx * 4.1f, cy + 1.7f, 10f, true);
            AddDeco(g, b, kiosk, cx + ksx * 4.1f, cy - 3.6f, 10f, true);
            var col = g.T("Coluna");
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sy in new[] { -1f, 1f })
                    AddDeco(g, b, col, cx + sx * 2.2f, cy + sy * 1.35f - 0.7f, 10f, true);
            var fl = g.flowers.Next();
            for (int i = 0; i < 14; i++)
            {
                float a = i * Mathf.PI * 2f / 14f;
                AddDeco(g, b, fl, cx + Mathf.Cos(a) * 1.5f, cy - 0.4f + Mathf.Sin(a) * 0.95f, 10f);
            }
            // clareira central e dois caminhos em cruz até as entradas
            bool Clear(float x, float y) =>
                Mathf.Abs(x - cx) < 1.0f || Mathf.Abs(y - cy) < 0.85f || ((x - cx) * (x - cx)) / (3.1f * 3.1f) + ((y - cy) * (y - cy)) / (2.3f * 2.3f) < 1f;

            // alameda no contorno, alternando duas espécies, com entradas no meio de cada lado
            var a1 = g.parkTrees.NextWhere(t => t.visWU <= 1.6f); var a2 = g.parkTrees.NextWhere(t => t.visWU <= 1.6f);
            int k = 0;
            for (float x = x0 + 0.9f; x <= x1 - 0.9f; x += 1.55f, k++)
            {
                if (Mathf.Abs(x - cx) < 1.4f) continue;
                var t = k % 2 == 0 ? a1 : a2;
                AddDeco(g, b, t, x + g.Range(-0.08f, 0.08f), y0 + g.Range(0.15f, 0.3f), 10f);
                AddDeco(g, b, t, x + g.Range(-0.08f, 0.08f), y1 - t.visHU + g.Range(0.25f, 0.5f));
            }
            for (float y = y0 + 1.9f; y <= y1 - 3.4f; y += 1.5f, k++)
            {
                if (Mathf.Abs(y - cy) < 1.2f) continue;
                var t = k % 2 == 0 ? a1 : a2;
                AddDeco(g, b, t, x0 + 0.85f + g.Range(-0.08f, 0.08f), y, 10f);
                AddDeco(g, b, t, x1 - 0.85f + g.Range(-0.08f, 0.08f), y, 10f);
            }
            // postes nas quatro entradas
            var lamp = g.T("Poste_Curvo_Duplo");
            foreach (var sx in new[] { -1f, 1f })
            {
                AddDeco(g, b, lamp, cx + sx * 1.75f, y0 + 0.12f, 10f);
                AddDeco(g, b, lamp, cx + sx * 1.75f, y1 - lamp.visHU + 0.3f, 10f);
            }

            // bosques nos quatro canteiros
            var placed = new List<Vector2>();
            int attempts = Mathf.RoundToInt(b.r.width * b.r.height * 3.5f);
            for (int i = 0; i < attempts; i++)
            {
                float x = g.Range(x0 + 2.0f, x1 - 2.0f), y = g.Range(y0 + 1.6f, y1 - 2.0f);
                if (Clear(x, y)) continue;
                float roll = (float)g.rng.NextDouble();
                CatalogSprite cs; float minD;
                if (roll < 0.5f) { cs = g.parkTrees.Next(); minD = 1.15f; }
                else if (roll < 0.74f) { cs = g.bushes.Next(); minD = 0.95f; }
                else if (roll < 0.82f) { cs = g.smallRocks.Next(); minD = 1.1f; }
                else if (roll < 0.92f) { cs = g.flowers.Next(); minD = 0.55f; }
                else if (roll < 0.96f) { cs = g.stumps.Next(); minD = 1.2f; }
                else { cs = g.mushrooms.Next(); minD = 0.6f; }
                if (placed.Any(p => Vector2.Distance(p, new Vector2(x, y)) < minD)) continue;
                if (Clear(x - cs.visWU / 2f, y) || Clear(x + cs.visWU / 2f, y)) continue;
                if (AddDeco(g, b, cs, x, y)) placed.Add(new Vector2(x, y));
            }
        }

        // ------------------------------------------------------------------ bosque do canto

        static void FillWoods(Gen g, Block b)
        {
            var placed = new List<Vector2>();
            int attempts = Mathf.RoundToInt(b.r.width * b.r.height * 6f);
            for (int i = 0; i < attempts; i++)
            {
                float x = g.Range(b.r.xMin + 0.5f, b.r.xMax - 0.5f), y = g.Range(b.r.yMin + 0.12f, b.r.yMax - 0.3f);
                float roll = (float)g.rng.NextDouble();
                CatalogSprite cs; float minD;
                if (roll < 0.6f) { cs = g.pines.Next(); minD = 1.0f; }
                else if (roll < 0.72f) { cs = g.bigRocks.Next(); minD = 1.8f; }
                else if (roll < 0.86f) { cs = g.bushes.Next(); minD = 0.9f; }
                else if (roll < 0.93f) { cs = g.stumps.Next(); minD = 1.1f; }
                else { cs = g.mushrooms.Next(); minD = 0.6f; }
                if (placed.Any(p => Vector2.Distance(p, new Vector2(x, y)) < minD)) continue;
                if (AddDeco(g, b, cs, x, y)) placed.Add(new Vector2(x, y));
            }
        }

        // ------------------------------------------------------------------ mobiliário urbano

        static void StreetFurniture(Gen g)
        {
            var sem = g.T("Semaforo"); var stop = g.T("Placa_Pare"); var lamp = g.T("Poste_Curvo");
            foreach (var b in g.blocks)
            {
                // semáforos nos cruzamentos do centro (faixas do parque e a de cima); poste nas demais esquinas
                if (b.eastItem)
                {
                    var item = b.row >= 2 && b.row <= 4 ? sem : (g.Chance(0.6f) ? lamp : null);
                    if (item != null) AddDeco(g, b, item, b.r.xMax - item.visWU / 2f - 0.08f, b.r.yMin + 0.05f, 10f);
                }
                if (b.westItem && stop != null) AddDeco(g, b, stop, b.r.xMin + stop.visWU / 2f + 0.08f, b.r.yMin + 0.05f, 10f);
            }
        }

        // ------------------------------------------------------------------ fechamento

        static void Finish(Gen g, Vector2 center)
        {
            var l = g.layout;
            // garantia final de oclusão coerente (R36)
            l.decorations.RemoveAll(d =>
            {
                var cs = g.cat.Get(d.sprite);
                var rect = cs.VisibleRect(d.bottomCenter);
                return g.structRects.Any(s => d.bottomCenter.y > s.baseY + 0.02f
                    && Mathf.Min(rect.xMax, s.rect.xMax) - Mathf.Max(rect.xMin, s.rect.xMin) > 0.06f
                    && Mathf.Min(rect.yMax, s.rect.yMax) - Mathf.Max(rect.yMin, s.rect.yMin) > 0.06f);
            });
            foreach (var d in l.decorations) d.id = g.NextId("Tree");

            // centraliza a caixa dos polígonos em 'center' (R34)
            var bb = CityGeometry.BoundingBox(l.polygons.Select(p => (IList<Vector2>)CityGeometry.ToVectors(p.points)));
            Vector2 off = center - bb.center;
            foreach (var p in l.polygons) for (int i = 0; i < p.points.Count; i++) p.points[i] = (Vector2)p.points[i] + off;
            foreach (var s in l.structures) s.bottomCenter = (Vector2)s.bottomCenter + off;
            foreach (var d in l.decorations) d.bottomCenter = (Vector2)d.bottomCenter + off;

            // waypoints de entrada (R14)
            var geo = LayoutValidator.BuildGeo(l);
            foreach (var s in l.structures)
            {
                var e = LayoutValidator.ExpectedEntry(geo, s.bottomCenter);
                s.entry = e ?? (Vector2)s.bottomCenter;
            }
        }

        /// <summary>Gera com sementes novas até obter um layout sem erros (máx. 30 tentativas).</summary>
        public static CityLayout GenerateValid(Vector2 center, SpriteCatalog cat, out ValidationReport report, int? baseSeed = null)
        {
            var seeds = new System.Random(baseSeed ?? Environment.TickCount);
            CityLayout layout = null; report = null;
            for (int i = 0; i < 30; i++)
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
