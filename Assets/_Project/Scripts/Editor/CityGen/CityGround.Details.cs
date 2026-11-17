using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Miudezas do chão (v6, 16/11/2026): manchas largas no gramado, terra batida, tufos, florzinhas, folhas no bosque,
    /// remendos, rachaduras, manchas de óleo e bueiros no asfalto, lajotas gastas na calçada e, por cima, flores, capim
    /// e pedrinhas do próprio pack carimbados no chão, e as sombras projetadas de tudo o que fica parado.
    /// Tudo é pixel da textura assada: densidade e sombra sem custo em jogo.
    /// Determinístico (Hash), então o mesmo layout continua dando os mesmos bytes.
    /// </summary>
    public static partial class CityGround
    {
        static readonly Color32 Dirt = new Color32(150, 122, 86, 255), DirtLo = new Color32(134, 108, 76, 255), DirtHi = new Color32(166, 138, 100, 255);
        static readonly Color32 Leaf = new Color32(120, 100, 62, 255), LeafHi = new Color32(142, 118, 70, 255);
        static readonly Color32 SandLo = new Color32(204, 182, 136, 255);
        static readonly Color32 WalkLo = new Color32(174, 168, 156, 255), WalkHi = new Color32(194, 188, 176, 255);
        static readonly Color32 Patch = new Color32(58, 62, 75, 255), PatchEdge = new Color32(50, 53, 64, 255), Crack = new Color32(42, 45, 55, 255);
        static readonly Color32 Rim = new Color32(104, 106, 116, 255), Lid = new Color32(78, 81, 94, 255), Grate = new Color32(64, 67, 80, 255);
        static readonly Color32[] Petals =
        {
            new Color32(240, 238, 226, 255), new Color32(242, 212, 96, 255), new Color32(232, 146, 176, 255),
            new Color32(172, 156, 228, 255), new Color32(220, 92, 84, 255),
        };

        // miudezas do pack por piso, no tamanho nativo: só as baixas (flores, plantinhas, cogumelos), que ficam bem no chão
        static readonly string[] LawnSmalls = { "Flor_Margarida", "Flor_Sino", "Flor_Tulipa", "Planta_Baixa" };
        static readonly string[] ParkSmalls = { "Flor_Margarida", "Flor_Sino", "Flor_Tulipa" };
        static readonly string[] WoodsSmalls = { "Cogumelo", "Cogumelo_Grupo", "Flor_Sino" };
        const float SmallMaxH = 1.05f;

        static Color32 Shade(Color32 c, float f) =>
            new Color32((byte)Mathf.Min(255f, c.r * f), (byte)Mathf.Min(255f, c.g * f), (byte)Mathf.Min(255f, c.b * f), 255);

        /// <summary>Ruído de valor (0..1) numa grade de cell px, suavizado: manchas grandes e macias.</summary>
        static float Value(int x, int y, int cell, int salt)
        {
            int gx = Mathf.FloorToInt((float)x / cell), gy = Mathf.FloorToInt((float)y / cell);
            float fx = (x - gx * cell) / (float)cell, fy = (y - gy * cell) / (float)cell;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float V(int a, int b) => (Hash(a, b, salt) & 0xFFFF) / 65535f;
            return Mathf.Lerp(Mathf.Lerp(V(gx, gy), V(gx + 1, gy), fx), Mathf.Lerp(V(gx, gy + 1), V(gx + 1, gy + 1), fx), fy);
        }

        /// <summary>Gramado vivo: três tons em manchas largas, terra batida com borda pontilhada, tufos em V e florzinhas.</summary>
        static Color32 Grass(int x, int y, int salt, Color32 b, Color32 lo, Color32 hi, int loPct, int hiPct, bool woods)
        {
            var c = Noise(x, y, salt, b, lo, hi, loPct, hiPct);
            float v = Value(x, y, 40, salt + 100);
            if (v < 0.3f) c = Shade(c, 0.93f); else if (v > 0.74f) c = Shade(c, 1.05f);
            float dirt = Value(x, y, 18, salt + 200);
            if (dirt > 0.88f) return Noise(x, y, salt + 3, woods ? Leaf : Dirt, DirtLo, woods ? LeafHi : DirtHi, 18, 10);
            if (dirt > 0.86f && ((x + y) & 1) == 0) return woods ? Leaf : DirtLo;
            uint rate = woods ? 14u : 9u;
            bool Tuft(int tx, int ty) => Hash(tx, ty, salt + 300) % 1000 < rate;
            if (Tuft(x, y) || Tuft(x + 1, y - 1) || Tuft(x - 1, y - 1)) return Shade(lo, 0.86f);
            uint f = Hash(x, y, salt + 400) % 1000;
            if (woods) { if (f < 30) return f % 2 == 0 ? Leaf : LeafHi; }
            else if (f < 3) return Petals[(int)(Hash(x, y, salt + 500) % (uint)Petals.Length)];
            return c;
        }

        /// <summary>Asfalto: tom variando em manchas largas (chamado pixel a pixel no passo 1).</summary>
        static Color32 AsphaltAt(int x, int y)
        {
            var c = Noise(x, y, 1, Asphalt, AsphaltLo, AsphaltHi, 7, 5);
            float v = Value(x, y, 64, 11);
            return v < 0.28f ? Shade(c, 0.95f) : v > 0.78f ? Shade(c, 1.04f) : c;
        }

        /// <summary>Desgaste do asfalto: remendos, rachaduras e manchas de óleo. Calçadas e quarteirões cobrem o que cair fora da rua.</summary>
        static void StreetWear(Canvas cv)
        {
            for (int gy = 0; gy < cv.H; gy += 56)
                for (int gx = 0; gx < cv.W; gx += 56)
                {
                    uint h = Hash(gx, gy, 41);
                    if (h % 5 != 0) continue;
                    int w = 10 + (int)((h >> 4) % 14), hh = 8 + (int)((h >> 9) % 10), x0 = gx + (int)((h >> 14) % 40), y0 = gy + (int)((h >> 20) % 44);
                    for (int y = y0; y < y0 + hh; y++)
                        for (int x = x0; x < x0 + w; x++)
                            cv.Set(x, y, x == x0 || y == y0 || x == x0 + w - 1 || y == y0 + hh - 1 ? PatchEdge : Noise(x, y, 42, Patch, AsphaltLo, Patch, 6, 0));
                }
            for (int gy = 0; gy < cv.H; gy += 36)
                for (int gx = 0; gx < cv.W; gx += 36)
                {
                    uint h = Hash(gx, gy, 43);
                    if (h % 3 != 0) continue;
                    int x = gx + (int)((h >> 4) % 36), y = gy + (int)((h >> 10) % 36), n = 6 + (int)((h >> 16) % 13);
                    bool horiz = ((h >> 24) & 1) == 0;
                    for (int i = 0; i < n; i++)
                    {
                        cv.Set(x, y, Crack);
                        uint s = Hash(x, y, 44) % 4;
                        if (horiz) { x++; if (s == 0) y++; else if (s == 1) y--; }
                        else { y++; if (s == 0) x++; else if (s == 1) x--; }
                    }
                }
            for (int gy = 0; gy < cv.H; gy += 48)
                for (int gx = 0; gx < cv.W; gx += 48)
                {
                    uint h = Hash(gx, gy, 45);
                    if (h % 4 != 0) continue;
                    int cx = gx + (int)((h >> 4) % 48), cy = gy + (int)((h >> 10) % 48), r = 2 + (int)((h >> 16) % 3);
                    for (int dy = -r; dy <= r; dy++)
                        for (int dx = -r; dx <= r; dx++)
                            if (dx * dx + dy * dy <= r * r && Hash(cx + dx, cy + dy, 46) % 3 != 0) cv.Mul(cx + dx, cy + dy, 0.84f);
                }
        }

        /// <summary>Bueiros redondos na faixa de rolamento, a cada ~6,5 u, longe dos cruzamentos.</summary>
        static void Manholes(Canvas cv, List<StreetGraph.Line> lines, List<Vector2> junctions, float h)
        {
            foreach (var line in lines)
            {
                uint seed = Hash(Mathf.RoundToInt(line.from * 10f), Mathf.RoundToInt(line.c * 10f), 47);
                int k = 0;
                for (float a = line.from + h + 1.2f + seed % 300 / 100f; a < line.to - h - 1f; a += 6.5f, k++)
                {
                    if (junctions.Any(j => line.Contains(j) && Mathf.Abs(line.Along(j) - a) < h + 0.8f)) continue;
                    float off = (k % 2 == 0 ? 1f : -1f) * (h - Sidewalk) * 0.5f;
                    var p = line.horizontal ? new Vector2(a, line.c + off) : new Vector2(line.c + off, a);
                    int cx = cv.X(p.x), cy = cv.Y(p.y);
                    for (int dy = -5; dy <= 5; dy++)
                        for (int dx = -5; dx <= 5; dx++)
                        {
                            int d2 = dx * dx + dy * dy;
                            if (d2 > 25) continue;
                            cv.Set(cx + dx, cy + dy, d2 > 16 ? Rim : ((dx + dy) & 1) == 0 && d2 > 4 ? Grate : Lid);
                        }
                }
            }
        }

        /// <summary>
        /// Sombras projetadas (regra em ShadowModel, a mesma da sombra da expansão no Play): cada pixel opaco do sprite,
        /// a uma altura h acima da base, escurece o chão em base + ShadowModel.Project(h). Nos prédios só a fachada é
        /// altura e o telhado acima dela é fundo, então a sombra é a faixa da fachada embaixo e a lateral à direita, do
        /// tamanho do prédio (antes o telhado contava como altura e a sombra saía o dobro mais comprida); árvore, poste e
        /// placa são altura até a ponta. Mais escura no pé, mais clara na ponta, borda amaciada em 1 px; sombras que se
        /// cruzam ficam com a mais forte. Decoração que o terreno da expansão esconde não deixa sombra.
        /// </summary>
        static void CastShadows(Canvas cv, CityLayout l, SpriteCatalog cat, HashSet<string> hidden)
        {
            var shade = new float[cv.W * cv.H];
            var casters = l.structures.Select(s => (s.sprite, s.bottomCenter, building: true))
                .Concat(l.decorations.Where(d => !hidden.Contains(d.id)).Select(d => (d.sprite, d.bottomCenter, building: false)));
            foreach (var (sprite, baseCenter, building) in casters)
            {
                var cs = cat.Get(sprite);
                if (cs == null || cs.pixels == null) continue;
                Vector2 c = cs.RectCenterFor(baseCenter);
                int ox = cv.X(c.x - cs.widthU / 2f), oy = cv.Y(c.y - cs.heightU / 2f);
                int baseRow = Mathf.RoundToInt(cs.padBottomU * cs.pxW / Mathf.Max(0.01f, cs.widthU));
                float hMax = Mathf.Max(1f, cs.pxH - baseRow);
                float facade = building ? cs.visHU * Ppu * ShadowModel.FacadeFraction : float.MaxValue;
                for (int y = baseRow; y < cs.pxH; y++)
                {
                    float hgt = y - baseRow, s = ShadowModel.Strength(hgt, hMax);
                    Vector2 p = ShadowModel.Project(hgt, facade);
                    int gy = oy + baseRow + Mathf.RoundToInt(p.y);
                    if (gy < 0 || gy >= cv.H) continue;
                    int dx = Mathf.RoundToInt(p.x);
                    for (int x = 0; x < cs.pxW; x++)
                    {
                        if (cs.pixels[y * cs.pxW + x].a <= 128) continue;
                        int gx = ox + x + dx;
                        if (gx < 0 || gx >= cv.W) continue;
                        int i = gy * cv.W + gx;
                        if (shade[i] < s) shade[i] = s;
                    }
                }
            }
            // sombra de contato: elipse no pé de cada decoração, para a sombra da copa não sair solta do tronco fino
            foreach (var d in l.decorations)
            {
                var cs = cat.Get(d.sprite);
                if (cs == null || hidden.Contains(d.id)) continue;
                float rx = Mathf.Clamp(cs.visWU * 0.36f, 0.1f, 0.6f), ry = Mathf.Clamp(rx * 0.38f, 0.05f, 0.16f);
                Vector2 c = d.bottomCenter + ShadowModel.Offset.normalized * ry * 0.8f;
                foreach (var (x, y) in cv.In(new Rect(c.x - rx, c.y - ry, 2 * rx, 2 * ry)))
                {
                    float ex = (cv.WX(x) - c.x) / rx, ey = (cv.WY(y) - c.y) / ry;
                    if (ex * ex + ey * ey > 1f) continue;
                    int i = y * cv.W + x;
                    if (shade[i] < ShadowModel.Foot) shade[i] = ShadowModel.Foot;
                }
            }
            // borda macia: metade do valor do pixel e metade da média 3x3 em volta
            for (int y = 0; y < cv.H; y++)
                for (int x = 0; x < cv.W; x++)
                {
                    float sum = 0f;
                    for (int j = -1; j <= 1; j++)
                        for (int k = -1; k <= 1; k++)
                        {
                            int xx = Mathf.Clamp(x + k, 0, cv.W - 1), yy = Mathf.Clamp(y + j, 0, cv.H - 1);
                            sum += shade[yy * cv.W + xx];
                        }
                    float v = (shade[y * cv.W + x] + sum / 9f) / 2f;
                    // degraus de 0,04 para continuar com cara de pixel art
                    v = Mathf.Round(v / 0.04f) * 0.04f;
                    if (v > 0f) cv.Mul(x, y, 1f - v);
                }
        }

        static bool Sandy(Color32 c) => c.r > 185 && c.g > 160 && c.b < 160;

        /// <summary>
        /// Carimba flores, capim e pedrinhas do pack no gramado, no parque e no bosque, com sombrinha de contato.
        /// Não cai embaixo de estruturas, no pé das decorações, em caminhos de areia nem em cima de outro carimbo.
        /// </summary>
        static void ScatterSmalls(Canvas cv, CityLayout l, SpriteCatalog cat, List<(PolygonData poly, Rect r, Zone zone)> blocks)
        {
            List<CatalogSprite> Pool(string[] names) => names.Select(cat.ByTexture).Where(c => c != null && c.pixels != null && c.visHU <= SmallMaxH).ToList();
            var lawn = Pool(LawnSmalls); var park = Pool(ParkSmalls); var woods = Pool(WoodsSmalls);
            var taken = new List<Rect>();
            foreach (var s in l.structures) { var cs = cat.Get(s.sprite); if (cs != null) taken.Add(cs.VisibleRect(s.bottomCenter)); }
            foreach (var d in l.decorations)
            {
                var cs = cat.Get(d.sprite); if (cs == null) continue;
                var r = cs.VisibleRect(d.bottomCenter);
                taken.Add(new Rect(r.xMin, r.yMin - 0.05f, r.width, Mathf.Min(r.height, 0.45f)));
            }
            for (int bi = 0; bi < blocks.Count; bi++)
            {
                var (_, br, zone) = blocks[bi];
                var pool = zone == Zone.Suburban ? lawn : zone == Zone.Park ? park : zone == Zone.Woods ? woods : null;
                if (pool == null || pool.Count == 0) continue;
                float density = zone == Zone.Suburban ? 0.35f : zone == Zone.Park ? 0.5f : 0.4f;
                int n = Mathf.RoundToInt(br.width * br.height * density);
                for (int i = 0; i < n; i++)
                {
                    var cs = pool[(int)(Hash(i, bi, 53) % (uint)pool.Count)];
                    float x = br.xMin + 0.15f + cs.visWU / 2f + Hash(i, bi, 51) % 10000 / 10000f * Mathf.Max(0f, br.width - 0.3f - cs.visWU);
                    float y = br.yMin + 0.1f + Hash(i, bi, 52) % 10000 / 10000f * Mathf.Max(0f, br.height - 0.2f - cs.visHU);
                    var rect = cs.VisibleRect(new Vector2(x, y));
                    var grown = new Rect(rect.xMin - 0.08f, rect.yMin - 0.08f, rect.width + 0.16f, rect.height + 0.16f);
                    if (taken.Any(t => t.Overlaps(grown))) continue;
                    if (Sandy(Pixel(cv, rect.xMin, y)) || Sandy(Pixel(cv, rect.center.x, y)) || Sandy(Pixel(cv, rect.xMax, y))) continue;
                    taken.Add(rect);
                    Stamp(cv, cs, new Vector2(x, y));
                }
            }
        }

        static Color32 Pixel(Canvas cv, float wx, float wy)
        {
            int x = Mathf.Clamp(cv.X(wx), 0, cv.W - 1), y = Mathf.Clamp(cv.Y(wy), 0, cv.H - 1);
            return cv.px[y * cv.W + x];
        }

        /// <summary>Copia os pixels opacos do sprite para o chão, com a mesma sombra deslocada do passo 8.</summary>
        static void Stamp(Canvas cv, CatalogSprite cs, Vector2 baseCenter)
        {
            Vector2 c = cs.RectCenterFor(baseCenter);
            int ox = cv.X(c.x - cs.widthU / 2f), oy = cv.Y(c.y - cs.heightU / 2f);
            int baseRow = Mathf.RoundToInt(cs.padBottomU * cs.pxW / Mathf.Max(0.01f, cs.widthU));
            for (int y = 0; y < Mathf.Min(cs.pxH, baseRow + 6); y++)
                for (int x = 0; x < cs.pxW; x++)
                    if (cs.pixels[y * cs.pxW + x].a > 128) cv.Mul(ox + x + 1, oy + y - 1, 0.82f);
            for (int y = 0; y < cs.pxH; y++)
                for (int x = 0; x < cs.pxW; x++)
                {
                    var p = cs.pixels[y * cs.pxW + x];
                    if (p.a > 128) cv.Set(ox + x, oy + y, new Color32(p.r, p.g, p.b, 255));
                }
        }

        static readonly Color32 Soil = new Color32(112, 84, 60, 255), SoilLo = new Color32(96, 72, 52, 255);

        /// <summary>
        /// Decorações que o terreno da expansão esconde no Play (BuildLot.Clears, com o pé no meio da borda de baixo do
        /// retângulo, como no jogo): o chão não ganha a sombra nem o canteiro delas. O recuo da base nova é o da Guarda
        /// (e o do Armazém) do layout, o mesmo que o ExpansionManager mede na cena.
        /// </summary>
        public static HashSet<string> HiddenByLots(CityLayout l, SpriteCatalog cat)
        {
            var hidden = new HashSet<string>();
            var lots = ExpansionData.Load().lots;
            if (lots == null) return hidden;
            float Recess(string type) => l.structures.Where(s => s.type == type).Select(s => s.bottomCenter.y - s.entry.y).DefaultIfEmpty(float.NaN).First();
            foreach (var slot in lots)
            {
                if (slot == null) continue;
                float recess = Recess(slot.kind == LotKind.Base ? StructureType.Guard : StructureType.Warehouse);
                if (float.IsNaN(recess)) continue;
                foreach (var d in l.decorations)
                {
                    var cs = cat.Get(d.sprite);
                    if (cs == null) continue;
                    Vector2 c = cs.RectCenterFor(d.bottomCenter);
                    if (BuildLot.Clears(slot.entry, recess, new Vector2(c.x, c.y - cs.heightU / 2f))) hidden.Add(d.id);
                }
            }
            return hidden;
        }

        static bool IsPlant(string texture) =>
            texture.StartsWith("Arvore") || texture.StartsWith("Carvalho") || texture.StartsWith("Pinheiro") || texture.StartsWith("Palmeira")
            || texture.StartsWith("Arbusto") || texture.StartsWith("Folhagem") || texture.StartsWith("Planta_") || texture.StartsWith("Capim") || texture.StartsWith("Flor_");

        /// <summary>
        /// Planta só nasce em terra: no pé de cada árvore, arbusto ou flor em chão duro (lajota do comércio, concreto da
        /// indústria, praça da Guarda), um canteiro de terra com meio-fio, como os da calçada de verdade.
        /// </summary>
        static void Planters(Canvas cv, CityLayout l, SpriteCatalog cat, List<(PolygonData poly, Rect r, Zone zone)> blocks, HashSet<string> hidden)
        {
            var hard = new HashSet<string>(blocks.Where(b => b.zone == Zone.Commercial || b.zone == Zone.Industrial || b.zone == Zone.Civic).Select(b => b.poly.id));
            foreach (var d in l.decorations)
            {
                if (!hard.Contains(d.polygonId) || hidden.Contains(d.id)) continue;
                var cs = cat.Get(d.sprite);
                if (cs == null || !IsPlant(cs.texture)) continue;
                float hw = Mathf.Max(0.22f, cs.visWU * 0.42f), hh = Mathf.Clamp(cs.visWU * 0.14f, 0.12f, 0.2f);
                var box = new Rect(d.bottomCenter.x - hw, d.bottomCenter.y - hh * 0.8f, 2 * hw, 2 * hh);
                int x0 = cv.X(box.xMin), x1 = cv.X(box.xMax) - 1, y0 = cv.Y(box.yMin), y1 = cv.Y(box.yMax) - 1;
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        int ex = Mathf.Min(x - x0, x1 - x), ey = Mathf.Min(y - y0, y1 - y);
                        if (ex == 0 && ey == 0) continue; // canto arredondado
                        bool rim = ex <= 1 || ey <= 1;
                        cv.Set(x, y, rim ? (ey == 0 && y == y0 ? Shade(Curb, 0.85f) : Curb) : Hash(x, y, 61) % 6 == 0 ? SoilLo : Soil);
                    }
            }
        }
    }
}
