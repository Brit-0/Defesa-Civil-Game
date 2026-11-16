using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Gera um CityLayout determinístico a partir de uma semente.
    ///
    /// Desenho da cidade (v5, 16/11/2026):
    /// - Malha alinhada de 5 colunas x 4 faixas: todas as ruas na mesma largura, retas de ponta a ponta, e rua de
    ///   contorno.
    /// - Cada quarteirão tem duas fileiras de estruturas: a de baixo com fachada para a rua ao sul (sprites _S) e a de
    ///   cima com fachada para a rua ao norte (sprites _N), com um quintal entre elas.
    /// - Centro cívico: a Guarda fica no meio do mapa, sozinha na fileira sul da praça cívica e de frente para o
    ///   parque, do outro lado da rua do meio, para as equipes chegarem a qualquer bairro pelo caminho mais curto.
    ///   Comércio em volta do centro, os dois depósitos ao norte, a meia distância da Guarda, indústria nos cantos
    ///   de cima, bosque no sudeste e bairros residenciais no restante. O chão é assado em pixel art (CityGround).
    /// - Nenhum sprite da pasta Carros. Decoração: árvores, arbustos, flores, cercas, canteiros, postes, semáforos,
    ///   equipamentos industriais.
    /// - Waypoints: um de entrada por estrutura (na rua da fachada), um por cruzamento, ligados aos vizinhos pela rua.
    /// </summary>
    public static class CityGenerator
    {
        public const float Street = 1.2f;
        public const float Setback = 0.12f;      // parte visível da estrutura até a rua da fachada
        public const float TopOverhang = 0.6f;   // quanto uma decoração pode passar da borda norte do quarteirão
        public const float YardMin = 1.3f;       // quintal mínimo entre as duas fileiras
        const string Res = "Res", Com = "Com", Ind = "Ind", IndGuard = "IndGuard", Depot = "Depot", Park = "Park", Woods = "Woods", Civic = "Civic";
        /// <summary>Sprites reservados (v5): a Guarda é o centro de comando e os Armazéns são galpões; nenhuma casa usa.</summary>
        const string GuardSprite = "Fabrica_A";
        static readonly string[] WarehouseSprites = { "Fabrica_S", "Fabrica_K" };
        static readonly HashSet<string> Landmarks = new HashSet<string> { GuardSprite, "Fabrica_S", "Fabrica_K" };

        class Block
        {
            public Rect r; public string role; public string polyId; public int row;
            public bool westEnd, eastItem, westItem;
            public float MarginL => westItem ? 1.15f : 0.4f;
            public float MarginR => eastItem ? 0.85f : 0.4f;
            public float RowMaxH => (r.height - 2 * Setback - YardMin) / 2f;
            public readonly List<Rect> structs = new List<Rect>();
            public readonly List<Rect> southRow = new List<Rect>(), northRow = new List<Rect>();
            /// <summary>Decorações "sólidas" (cercas, canteiros, contêineres, painéis): não se sobrepõem entre si.</summary>
            public readonly List<Rect> solids = new List<Rect>();
            public readonly List<Slot> southSlots = new List<Slot>(), northSlots = new List<Slot>();

            /// <summary>Topo das estruturas da fileira sul que cruzam [xa, xb] (ou a base do quarteirão).</summary>
            public float SouthTop(float xa, float xb)
            {
                float top = r.yMin;
                foreach (var s in southRow) if (s.xMax > xa && s.xMin < xb) top = Mathf.Max(top, s.yMax);
                return top;
            }

            /// <summary>Base das estruturas da fileira norte que cruzam [xa, xb] (ou o topo do quarteirão).</summary>
            public float NorthBottom(float xa, float xb)
            {
                float b = r.yMax;
                foreach (var s in northRow) if (s.xMax > xa && s.xMin < xb) b = Mathf.Min(b, s.yMin);
                return b;
            }
        }

        class Slot
        {
            public CatalogSprite cs; public string type;
            public float gapBefore, jitter, x;
            public bool IsStructure => type != null;
        }

        class Deck
        {
            readonly List<CatalogSprite> all; readonly System.Random rng; List<CatalogSprite> left = new List<CatalogSprite>();
            public Deck(IEnumerable<CatalogSprite> items, System.Random rng) { all = items.Where(i => i != null).ToList(); this.rng = rng; }
            public IReadOnlyList<CatalogSprite> All => all;
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
            public readonly Dictionary<string, int> ids = new Dictionary<string, int>();
            public readonly Dictionary<string, int> target = new Dictionary<string, int>();
            public readonly Dictionary<string, int> placed = new Dictionary<string, int>();
            public readonly Dictionary<string, float> gap = new Dictionary<string, float>();
            public Deck sub, comWide, comTower, ind, yardTrees, parkTrees, pines, bushes, flowers, smallRocks, bigRocks, monuments, stumps, mushrooms, palms, indProps, indBack;

            public float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            public bool Chance(float p) => rng.NextDouble() < p;
            public T Pick<T>(IList<T> l) => l[rng.Next(l.Count)];
            public string NextId(string t) { ids[t] = ids.TryGetValue(t, out var v) ? v + 1 : 1; return ids[t].ToString("000"); }
            public CatalogSprite T(string texture) => cat.ByTexture(texture);
            public Deck D(params string[] textures) => new Deck(textures.Select(T), rng);
            public Deck P(string category, params string[] prefixes) => new Deck(prefixes.SelectMany(p => cat.ByPrefix(category, p)).Distinct(), rng);
            /// <summary>Sprite desenhado para a estrutura: o próprio _S ou a variante _N.</summary>
            public CatalogSprite Draw(CatalogSprite s, bool north) => north ? cat.North(s) : s;
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
            PlanDensity(g);
            foreach (var b in g.blocks)
            {
                switch (b.role)
                {
                    case Res: FillResidential(g, b); break;
                    case Com: FillCommercial(g, b); break;
                    case Ind: case IndGuard: FillIndustrial(g, b); break;
                    case Civic: FillCivic(g, b); break;
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
            Res => District.Suburban, Com => District.Commercial, Civic => District.Commercial, Ind => District.Industrial, IndGuard => District.Industrial, Depot => District.Industrial, _ => District.None
        };

        // ------------------------------------------------------------------ baralhos de sprites

        static void BuildDecks(Gen g)
        {
            bool HasNorth(CatalogSprite c) => g.cat.North(c) != null;
            g.sub = new Deck(g.cat.Suburban.Where(HasNorth), g.rng);
            g.comWide = new Deck(g.P("Commercial", "Predio_", "Arranha_Ceu_").All.Where(HasNorth), g.rng);
            g.comTower = new Deck(g.P("Commercial", "Fundo_").All.Where(HasNorth), g.rng);
            g.ind = new Deck(g.cat.Industrial.Where(HasNorth).Where(c => !Landmarks.Contains(c.texture)), g.rng);
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
            g.indProps = g.D("Chamine", "Chamine_Baixa", "Chamine_Listrada", "Caixa_Dagua", "Conteiner", "Tanque", "Turbina_Eolica");
            g.indBack = g.D("Painel_Solar", "Cacamba_Lixo", "Barreira_Concreto", "Arbusto_Grande", "Conteiner", "Painel_Solar_Plano", "Grade_Obra", "Cone");
        }

        // ------------------------------------------------------------------ malha de quarteirões

        static float R1(float v) => Mathf.Round(v * 10f) / 10f;

        /// <summary>
        /// Malha v5: 5 colunas x 4 faixas com as ruas alinhadas de ponta a ponta. Na coluna do meio ficam casas
        /// (sul), o parque, a praça cívica com a Guarda (de frente para o parque, na rua do meio) e casas (norte).
        /// </summary>
        static void BuildBlocks(Gen g)
        {
            float S = Street;
            float wOut = R1(g.Range(7.0f, 7.4f)), wIn = R1(g.Range(7.6f, 8.1f)), pw = R1(g.Range(12.4f, 13.2f));
            float d = R1(g.Range(7.2f, 7.5f));
            var w = new[] { wOut, wIn, wIn, pw, wIn, wIn, wOut };
            int Cols = w.Length, Mid = Cols / 2;
            var x = new float[Cols];
            for (int i = 1; i < Cols; i++) x[i] = x[i - 1] + w[i - 1] + S;
            const int Rows = 6;
            var y = new float[Rows];
            for (int i = 1; i < Rows; i++) y[i] = y[i - 1] + d + S;

            // faixa 0 = sul, faixa 5 = norte; coluna 0 = oeste, coluna 6 = leste (v6: 7x6, o dobro da área da v5)
            var plan = new[]
            {
                new[] { Woods, Res, Res, Res, Res, Woods, Woods },
                new[] { Res, Park, Res, Res, Res, Res, Woods },
                new[] { Res, Res, Com, Park, Com, Res, Res },
                new[] { Res, Com, Com, Civic, Com, Com, Res },
                new[] { Ind, Res, Depot, Com, Depot, Res, Ind },
                new[] { Ind, Ind, Res, Res, Res, Ind, Ind },
            };
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Cols; col++)
                {
                    string role = plan[row][col];
                    var b = new Block { r = Rect.MinMaxRect(x[col], y[row], x[col] + w[col], y[row] + d), role = role, row = row };
                    // o Warehouse fica na ponta do quarteirão mais perto do centro
                    b.westEnd = role == Depot && col > Mid;
                    // esquinas voltadas para as duas ruas que ladeiam a coluna do centro recebem semáforo ou placa
                    if (role != Park && role != Woods && role != Depot)
                    {
                        b.eastItem = col == Mid - 1 || col == Mid;
                        if (col == Mid + 1) g.Chance(0.3f); // sorteio antigo, mantido para a semente dar o mesmo bairro
                        b.westItem = col == Mid || col == Mid + 1;
                    }
                    g.blocks.Add(b);
                }
        }

        // ------------------------------------------------------------------ densidade (R01, R09)

        /// <summary>Espaço que sobra numa fileira para casas comuns, descontando o que é reservado.</summary>
        static float RowAvail(Block b, bool north)
        {
            float a = b.r.width - b.MarginL - b.MarginR;
            if (!north && (b.role == Depot || b.role == Civic)) return 0f;
            if (!north && b.role == IndGuard) a -= 3.4f;
            return Mathf.Max(0f, a);
        }

        /// <summary>
        /// Distribui ~150–165 casas entre os bairros (58/26/16) e calcula o espaço médio entre casas de cada bairro
        /// para que a frente disponível acomode esse número. O que sobra vira quintal, árvores e equipamentos.
        /// </summary>
        static void PlanDensity(Gen g)
        {
            int n = g.rng.Next(150, 166);
            int nS = Mathf.RoundToInt(0.58f * n), nC = Mathf.RoundToInt(0.26f * n);
            g.target[District.Suburban] = nS; g.target[District.Commercial] = nC; g.target[District.Industrial] = n - nS - nC;
            var minGap = new Dictionary<string, float> { { District.Suburban, 0.4f }, { District.Commercial, 0.25f }, { District.Industrial, 0.55f } };
            foreach (var d in g.target.Keys.ToList())
            {
                var blocks = g.blocks.Where(b => DistrictOf(b.role) == d && b.role != Park && b.role != Woods).ToList();
                float front = blocks.Sum(b => RowAvail(b, false) + RowAvail(b, true));
                var pool = d == District.Suburban ? g.sub.All : d == District.Commercial ? g.comWide.All.Concat(g.comTower.All).ToList() : g.ind.All;
                float maxH = blocks.Count > 0 ? blocks.Min(b => b.RowMaxH) : 3f;
                var fit = pool.Where(c => g.cat.North(c) != null && c.visHU <= maxH && g.cat.North(c).visHU <= maxH).ToList();
                float avgW = (fit.Count > 0 ? fit : pool.ToList()).Average(c => c.visWU);
                g.gap[d] = Mathf.Clamp(front / Mathf.Max(1, g.target[d]) - avgW, minGap[d], 4.5f);
                g.placed[d] = 0;
            }
        }

        // ------------------------------------------------------------------ utilitários de posicionamento

        static StructureData AddStructure(Gen g, Block b, CatalogSprite draw, string type, bool north, float baseX, float baseY)
        {
            var s = new StructureData
            {
                id = g.NextId(type), type = type, facing = north ? Facing.North : Facing.South, district = draw.category,
                polygonId = b.polyId, sprite = draw.ToRef(), bottomCenter = new Vec2(baseX, baseY)
            };
            g.layout.structures.Add(s);
            var rect = draw.VisibleRect(new Vector2(baseX, baseY));
            b.structs.Add(rect);
            (north ? b.northRow : b.southRow).Add(rect);
            return s;
        }

        /// <summary>
        /// Adiciona uma decoração se couber: base dentro do quarteirão, copa sem passar muito da borda norte e
        /// sem cobrir estrutura que esteja à sua frente (R36).
        /// </summary>
        static bool AddDeco(Gen g, Block b, CatalogSprite cs, float baseX, float baseY, float overhang = TopOverhang, bool solid = false, float maxTop = float.MaxValue)
        {
            if (cs == null) return false;
            float half = cs.visWU / 2f;
            if (baseX - half < b.r.xMin + 0.03f || baseX + half > b.r.xMax - 0.03f) return false;
            if (baseY < b.r.yMin + 0.03f || baseY > b.r.yMax - 0.05f) return false;
            if (baseY + cs.visHU > Mathf.Min(b.r.yMax + overhang, maxTop)) return false;
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
                // tronco/pé não nasce de dentro de uma cerca, canteiro ou estrutura
                foreach (var o in b.solids.Concat(b.structs))
                    if (baseX > o.xMin - 0.02f && baseX < o.xMax + 0.02f && baseY > o.yMin + 0.05f && baseY < o.yMax) return false;
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

        /// <summary>Base Y da estrutura/decoração de fileira: rente à rua sul (fileira S) ou à rua norte (fileira N).</summary>
        static float RowBase(Block b, bool north, CatalogSprite cs, float jitter) =>
            north ? b.r.yMax - Setback - jitter - cs.visHU : b.r.yMin + Setback + jitter;

        static void PlaceRow(Gen g, Block b, List<Slot> slots, bool north)
        {
            foreach (var s in slots.Where(s => s.IsStructure)) AddStructure(g, b, s.cs, s.type, north, s.x, RowBase(b, north, s.cs, s.jitter));
            foreach (var s in slots.Where(s => !s.IsStructure)) AddDeco(g, b, s.cs, s.x, RowBase(b, north, s.cs, s.jitter), 10f, true);
            (north ? b.northSlots : b.southSlots).AddRange(slots);
        }

        /// <summary>
        /// Enche uma fileira com casas do bairro (sprites _S ou _N conforme a fileira), respeitando a altura máxima
        /// da fileira e o espaço médio planejado. 'preset' entra na frente (Guarda, Warehouse, contêineres).
        /// </summary>
        static List<Slot> FillRow(Gen g, Block b, bool north, string district, Func<CatalogSprite> pickDeck, float avail, List<Slot> preset = null,
            Func<float, Slot> between = null)
        {
            var slots = preset ?? new List<Slot>();
            float used = slots.Sum(s => s.cs.visWU) + slots.Skip(1).Sum(s => s.gapBefore);
            float maxH = b.RowMaxH;
            for (int guard = 0; guard < 40 && g.placed[district] < g.target[district] + 1; guard++)
            {
                float gp = slots.Count > 0 ? g.gap[district] * g.Range(0.75f, 1.25f) : 0f;
                if (between != null && slots.Count > 0 && slots[slots.Count - 1].IsStructure)
                {
                    var mid = between(gp);
                    if (mid != null && used + mid.gapBefore + mid.cs.visWU + 0.3f + 1.5f <= avail)
                    {
                        slots.Add(mid); used += mid.gapBefore + mid.cs.visWU;
                        gp = 0.3f;
                    }
                }
                CatalogSprite draw = null;
                for (int tries = 0; tries < 40 && draw == null; tries++)
                {
                    var s = pickDeck();
                    var d = s == null ? null : g.Draw(s, north);
                    if (d != null && d.visHU <= maxH && d.visWU + gp <= avail - used) draw = d;
                }
                if (draw == null) break;
                slots.Add(new Slot { cs = draw, type = StructureType.House, gapBefore = gp, jitter = g.Range(0f, 0.06f) });
                used += draw.visWU + gp;
                g.placed[district]++;
            }
            while (slots.Count > 0 && !slots[slots.Count - 1].IsStructure && preset == null) slots.RemoveAt(slots.Count - 1);
            return slots;
        }

        // ------------------------------------------------------------------ decoração comum

        /// <summary>Árvore/arbusto nos vãos entre estruturas de uma fileira e flores ou vaso na frente.</summary>
        static void GapPlanting(Gen g, Block b, List<Slot> slots, bool north, Deck trees, bool lamps)
        {
            for (int i = 1; i < slots.Count; i++)
            {
                var a = slots[i - 1]; var c = slots[i];
                if (!a.IsStructure || !c.IsStructure) continue;
                float xa = a.x + a.cs.visWU / 2f, xb = c.x - c.cs.visWU / 2f, gw = xb - xa, xm = (xa + xb) / 2f;
                if (gw < 0.6f) continue;
                // árvore no lote, ao lado da casa
                if (gw >= 0.95f && g.Chance(0.8f))
                {
                    var tree = trees.NextWhere(t => t.visWU <= gw - 0.08f);
                    if (tree != null)
                    {
                        float baseY = north ? b.NorthBottom(xa, xb) - tree.visHU * 0.25f + g.Range(-0.2f, 0.2f) : b.r.yMin + g.Range(0.9f, 1.5f);
                        if (north) baseY = Mathf.Min(baseY, b.r.yMax + TopOverhang - tree.visHU);
                        AddDeco(g, b, tree, xm + g.Range(-0.1f, 0.1f), baseY, TopOverhang, false, north ? float.MaxValue : b.NorthBottom(xa, xb) + 0.6f);
                    }
                }
                // na frente, perto da calçada
                var front = (lamps && !north && g.Chance(0.4f) ? g.D("Poste_Novo") : g.D("Flor_Margarida", "Flor_Sino", "Flor_Tulipa", "Planta_Baixa", "Planta_Alta", "Folhagem", "Vaso", "Arbusto_Pequeno"))
                    .NextWhere(t => t.visWU <= gw - 0.08f);
                if (front != null && g.Chance(0.75f))
                {
                    float baseY = north ? b.r.yMax - 0.05f - front.visHU * 0.6f : b.r.yMin + g.Range(0.08f, 0.35f);
                    AddDeco(g, b, front, xm + g.Range(-0.06f, 0.06f), baseY, 10f);
                }
            }
        }

        /// <summary>Quintal entre as fileiras: cerca ou sebe dividindo os lotes e árvores atrás das casas.</summary>
        static void Yard(Gen g, Block b, Deck trees, string divider, float treeChance)
        {
            float ya = b.SouthTop(b.r.xMin, b.r.xMax), yb = b.NorthBottom(b.r.xMin, b.r.xMax);
            // divisória no meio do quintal
            if (divider == "fence" || divider == "high")
            {
                var fence = g.T(divider == "high" ? "Cerca_Alta" : g.Pick(new[] { "Cerca_Tabuas", "Cerca_Rustica", "Cerca_Trilho", "Cerca_Tabuas_Dupla" }));
                float yMid = MidYard(b) - fence.visHU / 2f;
                FenceLine(g, b, fence, yMid, 0.25f);
            }
            else if (divider == "hedge")
            {
                for (float x = b.r.xMin + 0.9f + g.Range(0f, 0.5f); x < b.r.xMax - 0.9f; x += g.Range(1.3f, 1.8f))
                {
                    var bush = g.bushes.Next();
                    AddDeco(g, b, bush, x, MidYard(b, x) - bush.visHU / 2f + g.Range(-0.1f, 0.1f));
                }
            }
            // árvores atrás das casas da fileira sul (podem encobrir o fundo das casas da fileira norte)
            foreach (var s in b.southSlots.Where(s => s.IsStructure))
            {
                if (!g.Chance(treeChance)) continue;
                float x = s.x + g.Range(-0.35f, 0.35f) * s.cs.visWU;
                var tree = trees.NextWhere(t =>
                {
                    float top = b.SouthTop(x - t.visWU / 2f, x + t.visWU / 2f);
                    return top + 0.06f + t.visHU <= b.NorthBottom(x - t.visWU / 2f, x + t.visWU / 2f) + 0.9f;
                }) ?? g.bushes.Next();
                float baseY = b.SouthTop(x - tree.visWU / 2f, x + tree.visWU / 2f) + g.Range(0.06f, 0.25f);
                AddDeco(g, b, tree, x, baseY, TopOverhang, false, b.NorthBottom(x - tree.visWU / 2f, x + tree.visWU / 2f) + 0.9f);
            }
            // flores no quintal
            int beds = Mathf.Max(1, Mathf.FloorToInt(b.r.width / 3.5f));
            for (int i = 0; i < beds; i++)
            {
                if (!g.Chance(0.85f)) continue;
                float x = g.Range(b.r.xMin + 1.0f, b.r.xMax - 1.0f);
                var f = g.flowers.Next();
                float y0 = b.SouthTop(x - 1f, x + 1f) + 0.15f, y1 = b.NorthBottom(x - 1f, x + 1f) - f.visHU - 0.1f;
                if (y1 <= y0) continue;
                float yb2 = g.Range(y0, y1);
                int count = g.rng.Next(2, 4);
                for (int k = 0; k < count; k++) AddDeco(g, b, f, x + (k - (count - 1) / 2f) * 0.5f, yb2 + g.Range(-0.08f, 0.08f));
            }
        }

        static float MidYard(Block b, float? x = null)
        {
            float xa = x.HasValue ? x.Value - 0.5f : b.r.xMin, xb = x.HasValue ? x.Value + 0.5f : b.r.xMax;
            return (b.SouthTop(xa, xb) + b.NorthBottom(xa, xb)) / 2f;
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
        static void CornerGarden(Gen g, Block b, List<Slot> slots, bool north, Deck trees)
        {
            if (slots.Count == 0) return;
            var first = slots[0]; var last = slots[slots.Count - 1];
            float leftW = first.x - first.cs.visWU / 2f - b.r.xMin, rightW = b.r.xMax - (last.x + last.cs.visWU / 2f);
            foreach (var (xa, w) in new[] { (b.r.xMin, leftW), (b.r.xMax - rightW, rightW) })
            {
                if (w < 1.15f) continue;
                for (float x = xa + 0.65f; x <= xa + w - 0.6f; x += g.Range(1.1f, 1.4f))
                {
                    var tree = trees.NextWhere(t => t.visWU <= Mathf.Min(1.5f, w - 0.1f));
                    if (tree == null) continue;
                    float baseY = north ? b.r.yMax + TopOverhang - tree.visHU - g.Range(0.1f, 0.6f) : b.r.yMin + g.Range(0.3f, 1.2f);
                    AddDeco(g, b, tree, x, baseY, TopOverhang, false, north ? float.MaxValue : b.NorthBottom(xa, xa + w) + 0.6f);
                }
            }
        }

        // ------------------------------------------------------------------ residencial

        static void FillResidential(Gen g, Block b)
        {
            foreach (bool north in new[] { false, true })
            {
                var slots = FillRow(g, b, north, District.Suburban, () => g.sub.Next(), RowAvail(b, north));
                LayoutRow(g, slots, b.r.xMin + b.MarginL, b.r.xMax - b.MarginR, g.gap[District.Suburban] * 0.6f);
                PlaceRow(g, b, slots, north);
            }
            GapPlanting(g, b, b.southSlots, false, g.yardTrees, true);
            GapPlanting(g, b, b.northSlots, true, g.yardTrees, false);
            Yard(g, b, g.yardTrees, g.Chance(0.55f) ? "fence" : "hedge", 0.75f);
            CornerGarden(g, b, b.southSlots, false, g.yardTrees);
            CornerGarden(g, b, b.northSlots, true, g.yardTrees);
            // canteiro grande em alguns quintais
            if (g.Chance(0.35f))
            {
                var planter = g.T(g.Chance(0.6f) ? "Jardineira" : "Cercado");
                float x = g.Range(b.r.xMin + 1.6f, b.r.xMax - 1.6f);
                AddDeco(g, b, planter, x, MidYard(b, x) - planter.visHU / 2f, 0f, true);
            }
        }

        // ------------------------------------------------------------------ comercial

        static void FillCommercial(Gen g, Block b)
        {
            foreach (bool north in new[] { false, true }) CommercialRow(g, b, north);
            var streetTrees = g.D("Arvore_Redonda", "Arvore_Cone", "Arvore_Simples", "Arvore_Alta", "Arvore_Quintal");
            GapPlanting(g, b, b.southSlots, false, streetTrees, true);
            GapPlanting(g, b, b.northSlots, true, streetTrees, false);
            Yard(g, b, streetTrees, "hedge", 0.45f);
            CornerGarden(g, b, b.southSlots, false, streetTrees);
            CornerGarden(g, b, b.northSlots, true, streetTrees);
        }

        static void CommercialRow(Gen g, Block b, bool north)
        {
            CatalogSprite Pick() => g.Chance(0.7f) ? g.comWide.Next() : g.comTower.Next();
            {
                // entre os prédios: canteiro, palmeira ou guarda-sol de vez em quando
                Slot Between(float gp)
                {
                    if (!g.Chance(0.3f)) return null;
                    var p = north ? g.Pick(new[] { g.T("Jardineira"), g.T("Vaso") }) : g.Pick(new[] { g.T("Guarda_Sol"), g.T("Jardineira"), g.T("Vaso") });
                    return p == null ? null : new Slot { cs = p, gapBefore = Mathf.Max(gp, 0.35f), jitter = 0.1f };
                }
                var slots = FillRow(g, b, north, District.Commercial, Pick, RowAvail(b, north), null, Between);
                LayoutRow(g, slots, b.r.xMin + b.MarginL, b.r.xMax - b.MarginR, g.gap[District.Commercial] * 0.8f);
                PlaceRow(g, b, slots, north);
            }
        }

        // ------------------------------------------------------------------ praça cívica (Guarda)

        /// <summary>
        /// A Guarda sozinha no meio da fileira sul, de frente para o parque; dos lados, praça simétrica com postes,
        /// canteiros e palmeiras; atrás, a fileira norte de comércio.
        /// </summary>
        static void FillCivic(Gen g, Block b)
        {
            float cx = b.r.center.x;
            var hq = g.T(GuardSprite) is { } a && a.visHU <= b.RowMaxH ? a
                : g.ind.NextWhere(c => c.visWU >= 2.6f && c.visHU <= b.RowMaxH) ?? g.ind.NextWhere(c => c.visHU <= b.RowMaxH);
            var hqSlot = new Slot { cs = hq, type = StructureType.Guard, x = cx };
            PlaceRow(g, b, new List<Slot> { hqSlot }, false);
            CommercialRow(g, b, true);

            var lamp = g.T("Poste_Novo_Duplo") ?? g.T("Poste_Curvo_Duplo");
            var planter = g.T("Jardineira");
            var palm = g.palms.Next();
            float half = hq.visWU / 2f;
            foreach (var sx in new[] { -1f, 1f })
            {
                // poste e canteiro colados à sede, palmeiras em fila até a ponta do quarteirão
                AddDeco(g, b, lamp, cx + sx * (half + 0.45f), b.r.yMin + 0.08f, 10f, true);
                AddDeco(g, b, planter, cx + sx * (half + 1.25f), b.r.yMin + 0.15f, 10f, true);
                for (float dx = half + 2.3f; dx < b.r.width / 2f - 0.7f; dx += 1.7f)
                    AddDeco(g, b, palm, cx + sx * dx, b.r.yMin + 0.2f, 10f);
                // segunda fila, no meio da praça
                for (float dx = half + 1.4f; dx < b.r.width / 2f - 0.9f; dx += 2.2f)
                {
                    var t = g.parkTrees.NextWhere(c => c.visWU <= 1.4f);
                    float yb = MidYard(b, cx + sx * dx) - 0.4f;
                    AddDeco(g, b, t, cx + sx * dx, yb, 0f, false, b.NorthBottom(cx + sx * dx - 0.7f, cx + sx * dx + 0.7f) - 0.1f);
                }
            }
            CornerGarden(g, b, b.northSlots, true, g.D("Arvore_Redonda", "Arvore_Cone", "Arvore_Simples"));
        }

        // ------------------------------------------------------------------ industrial

        static Slot IndustrialProp(Gen g, Block b, float gp)
        {
            if (!g.Chance(0.4f)) return null;
            var p = g.indProps.NextWhere(c => c.visHU <= b.RowMaxH);
            return p == null ? null : new Slot { cs = p, gapBefore = Mathf.Max(0.3f, gp * 0.5f), jitter = g.Range(0.05f, 0.25f) };
        }

        static void FillIndustrial(Gen g, Block b)
        {
            foreach (bool north in new[] { false, true })
            {
                List<Slot> preset = null;
                if (!north && b.role == IndGuard)
                {
                    var hq = g.ind.NextWhere(c => c.visWU >= 2.6f && c.visHU <= b.RowMaxH) ?? g.ind.NextWhere(c => c.visHU <= b.RowMaxH);
                    preset = new List<Slot> { new Slot { cs = hq, type = StructureType.Guard } };
                }
                float avail = b.r.width - b.MarginL - b.MarginR;
                var slots = FillRow(g, b, north, District.Industrial, () => g.ind.Next(), avail, preset, gp => IndustrialProp(g, b, gp));
                // a Guarda fica no meio da fileira, de frente para o parque
                if (preset != null && slots.Count >= 3) { var hqSlot = slots[0]; slots.RemoveAt(0); hqSlot.gapBefore = slots[0].gapBefore; slots.Insert(slots.Count / 2, hqSlot); }
                // sobra na frente: mais um equipamento
                float used = slots.Sum(s => s.cs.visWU) + slots.Skip(1).Sum(s => s.gapBefore);
                var extra = g.indProps.NextWhere(c => c.visHU <= b.RowMaxH && c.visWU + 0.45f <= avail - used);
                if (extra != null) slots.Add(new Slot { cs = extra, gapBefore = 0.45f, jitter = g.Range(0.05f, 0.25f) });
                LayoutRow(g, slots, b.r.xMin + b.MarginL, b.r.xMax - b.MarginR, g.gap[District.Industrial] * 0.6f);
                PlaceRow(g, b, slots, north);
            }
            // pátio entre as fileiras: painéis solares, tanques e caçambas
            for (float x = b.r.xMin + 1.4f + g.Range(0f, 0.8f); x < b.r.xMax - 1.2f;)
            {
                var p = g.indBack.Next();
                float ya = b.SouthTop(x - p.visWU / 2f, x + p.visWU / 2f) + 0.12f, yb = b.NorthBottom(x - p.visWU / 2f, x + p.visWU / 2f) - 0.1f;
                if (yb - ya >= p.visHU) AddDeco(g, b, p, x, ya + (yb - ya - p.visHU) * g.Range(0.3f, 0.7f), 0f, true);
                x += p.visWU + g.Range(0.2f, 0.7f);
            }
            CornerGarden(g, b, b.southSlots, false, g.pines);
            CornerGarden(g, b, b.northSlots, true, g.pines);
        }

        // ------------------------------------------------------------------ depósito

        static void FillDepot(Gen g, Block b)
        {
            // fileira sul: Warehouse + pátio de contêineres e tanque
            var wh = WarehouseSprites.Select(g.T).FirstOrDefault(c => c != null && c.visHU <= b.RowMaxH && !g.layout.structures.Any(s => s.sprite.guid == c.guid && s.sprite.name == c.name))
                ?? g.ind.NextWhere(c => c.visWU >= 2.7f && c.visHU <= b.RowMaxH) ?? g.ind.NextWhere(c => c.visHU <= b.RowMaxH);
            var yard = new List<Slot>
            {
                new Slot { cs = g.T("Conteiner"), gapBefore = 0.4f, jitter = g.Range(0.1f, 0.3f) },
                new Slot { cs = g.T("Conteiner"), gapBefore = 0.12f, jitter = g.Range(0.1f, 0.3f) },
                new Slot { cs = g.T("Tanque"), gapBefore = 0.4f, jitter = g.Range(0.15f, 0.4f) },
            };
            yard.RemoveAll(s => s.cs == null || s.cs.visHU > b.RowMaxH);
            var house = new Slot { cs = wh, type = StructureType.Warehouse };
            var south = new List<Slot>();
            if (b.westEnd) { south.Add(house); south.AddRange(yard); }
            else { yard.Reverse(); foreach (var s in yard) s.gapBefore = 0.3f; south.AddRange(yard); house.gapBefore = 0.4f; south.Add(house); }
            south[0].gapBefore = 0f;
            while (south.Sum(s => s.cs.visWU) + south.Skip(1).Sum(s => s.gapBefore) > b.r.width - b.MarginL - b.MarginR && south.Count > 1)
                south.Remove(south.First(s => !s.IsStructure));
            LayoutRow(g, south, b.r.xMin + b.MarginL, b.r.xMax - b.MarginR, 0.6f);
            PlaceRow(g, b, south, false);
            // fileira norte: galpões industriais de frente para a rua de cima
            var north = FillRow(g, b, true, District.Industrial, () => g.ind.Next(), RowAvail(b, true), null, gp => IndustrialProp(g, b, gp));
            LayoutRow(g, north, b.r.xMin + b.MarginL, b.r.xMax - b.MarginR, g.gap[District.Industrial] * 0.6f);
            PlaceRow(g, b, north, true);
            // cerca alta fechando o pátio
            Yard(g, b, g.pines, "high", 0f);
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
            AddDeco(g, b, kiosk, cx - ksx * 4.1f, cy + 2.2f, 10f, true);
            AddDeco(g, b, kiosk, cx + ksx * 4.1f, cy - 4.0f, 10f, true);
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
            // postes nas entradas
            var lamp = g.T("Poste_Novo_Duplo") ?? g.T("Poste_Curvo_Duplo");
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
            var sem = g.T("Semaforo_Novo") ?? g.T("Semaforo"); var stop = g.T("Placa_Pare_Nova") ?? g.T("Placa_Pare");
            var lamp = g.T("Poste_Novo") ?? g.T("Poste_Curvo"); var sign = g.T("Placa_Rua_Nova") ?? g.T("Placa_Rua");
            foreach (var b in g.blocks)
            {
                // v7: semáforo dos dois lados das duas avenidas do centro, em todo cruzamento
                if (b.eastItem)
                {
                    g.Chance(0.6f); // sorteio antigo (poste ou nada), mantido para a semente
                    if (sem != null) AddDeco(g, b, sem, b.r.xMax - sem.visWU / 2f - 0.14f, b.r.yMin + 0.05f, 10f);
                }
                if (b.westItem && sem != null) AddDeco(g, b, sem, b.r.xMin + sem.visWU / 2f + 0.14f, b.r.yMin + 0.05f, 10f);
                // nas outras esquinas dos bairros de casas e comércio: placa de pare à esquerda e placa de rua à direita
                if (b.role != Res && b.role != Com) continue;
                if (!b.westItem && stop != null && g.Chance(0.5f)) AddDeco(g, b, stop, b.r.xMin + stop.visWU / 2f + 0.06f, b.r.yMin + 0.05f, 10f);
                if (!b.eastItem && sign != null && g.Chance(0.5f)) AddDeco(g, b, sign, b.r.xMax - sign.visWU / 2f - 0.06f, b.r.yMin + 0.05f, 10f);
            }
        }

        // ------------------------------------------------------------------ fechamento

        static void Finish(Gen g, Vector2 center)
        {
            var l = g.layout;
            // garantia final de oclusão coerente (R36)
            var rects = l.structures.Select(s => (rect: g.cat.Get(s.sprite).VisibleRect(s.bottomCenter), baseY: s.bottomCenter.y)).ToList();
            l.decorations.RemoveAll(d =>
            {
                var rect = g.cat.Get(d.sprite).VisibleRect(d.bottomCenter);
                return rects.Any(s => d.bottomCenter.y > s.baseY + 0.02f
                    && Mathf.Min(rect.xMax, s.rect.xMax) - Mathf.Max(rect.xMin, s.rect.xMin) > 0.06f
                    && Mathf.Min(rect.yMax, s.rect.yMax) - Mathf.Max(rect.yMin, s.rect.yMin) > 0.06f);
            });
            foreach (var d in l.decorations) d.id = g.NextId("Tree");

            // a câmera começa no meio do quarteirão da Guarda (R34): o primeiro desastre cai ali e fica à vista
            var bb = CityGeometry.BoundingBox(l.polygons.Select(p => (IList<Vector2>)CityGeometry.ToVectors(p.points)));
            var civic = g.blocks.FirstOrDefault(b => b.role == Civic);
            Vector2 off = center - (civic != null ? civic.r.center + new Vector2(0f, LayoutValidator.FocusLift) : bb.center);
            foreach (var p in l.polygons) for (int i = 0; i < p.points.Count; i++) p.points[i] = (Vector2)p.points[i] + off;
            foreach (var s in l.structures) s.bottomCenter = (Vector2)s.bottomCenter + off;
            foreach (var d in l.decorations) d.bottomCenter = (Vector2)d.bottomCenter + off;

            // waypoints de entrada (R14) e grafo de ruas (R38)
            var geo = LayoutValidator.BuildGeo(l);
            foreach (var s in l.structures)
            {
                var e = LayoutValidator.ExpectedEntry(geo, s.bottomCenter, s.facing, l.streetWidth);
                s.entry = e ?? (Vector2)s.bottomCenter;
            }
            StreetGraph.Fill(l);
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
