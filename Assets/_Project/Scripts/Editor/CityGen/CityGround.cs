using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Chão da cidade assado em pixel art (v5): uma textura a 32 px por unidade, do tamanho do Limiter, com asfalto,
    /// calçada com meio-fio, faixa tracejada no meio das ruas, faixas de pedestre no centro, piso de cada bairro
    /// (grama, lajotas, concreto, praça), caminhos do parque, pátio hachurado na frente da Guarda e dos Armazéns e
    /// sombra de contato embaixo de cada sprite. Feito só com código e a paleta abaixo: nenhuma arte nova.
    /// O CityBuilder mostra a textura no "[CG] Ground" (RawImage logo acima do PolygonsDisplay).
    /// </summary>
    public static class CityGround
    {
        public const string TexturePath = "Assets/_Project/Data/Cities/city_ground.png";
        public const int Ppu = 32;
        const float Sidewalk = 0.25f;

        static readonly Color32 Asphalt = new Color32(62, 66, 80, 255), AsphaltLo = new Color32(55, 59, 72, 255), AsphaltHi = new Color32(71, 75, 90, 255);
        static readonly Color32 Walk = new Color32(184, 178, 166, 255), WalkSeam = new Color32(163, 157, 146, 255), Curb = new Color32(128, 124, 120, 255);
        static readonly Color32 Dash = new Color32(226, 208, 140, 255), Zebra = new Color32(222, 222, 214, 255);
        static readonly Color32 Lawn = new Color32(122, 164, 86, 255), LawnLo = new Color32(108, 150, 76, 255), LawnHi = new Color32(138, 178, 96, 255);
        static readonly Color32 Park = new Color32(104, 158, 82, 255), ParkLo = new Color32(92, 144, 74, 255), ParkHi = new Color32(120, 172, 92, 255);
        static readonly Color32 Sand = new Color32(216, 194, 146, 255), SandEdge = new Color32(186, 164, 118, 255);
        static readonly Color32 Forest = new Color32(78, 122, 70, 255), ForestLo = new Color32(66, 106, 62, 255), ForestHi = new Color32(92, 136, 78, 255);
        static readonly Color32 Tile = new Color32(200, 188, 166, 255), TileAlt = new Color32(192, 180, 158, 255), Grout = new Color32(174, 162, 142, 255);
        static readonly Color32 Concrete = new Color32(144, 146, 152, 255), Joint = new Color32(124, 126, 134, 255), Stain = new Color32(132, 134, 140, 255);
        static readonly Color32 Plaza = new Color32(210, 204, 192, 255), PlazaAlt = new Color32(200, 194, 182, 255), Terracotta = new Color32(190, 108, 78, 255);
        static readonly Color32 HatchGuard = new Color32(232, 128, 52, 255), HatchDepot = new Color32(230, 190, 64, 255);

        enum Zone { Suburban, Commercial, Industrial, Park, Woods, Civic }

        class Canvas
        {
            public int W, H; public Rect area; public Color32[] px;
            public int X(float wx) => Mathf.FloorToInt((wx - area.xMin) * Ppu);
            public int Y(float wy) => Mathf.FloorToInt((wy - area.yMin) * Ppu);
            public float WX(int x) => area.xMin + (x + 0.5f) / Ppu;
            public float WY(int y) => area.yMin + (y + 0.5f) / Ppu;
            public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < W && y < H) px[y * W + x] = c; }
            public void Mul(int x, int y, float f)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                var c = px[y * W + x];
                px[y * W + x] = new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), 255);
            }
            /// <summary>Percorre os pixels com centro dentro do retângulo (mundo).</summary>
            public IEnumerable<(int x, int y)> In(Rect r)
            {
                int xa = Mathf.Max(0, Mathf.CeilToInt((r.xMin - area.xMin) * Ppu - 0.5f)), xb = Mathf.Min(W - 1, Mathf.FloorToInt((r.xMax - area.xMin) * Ppu - 0.5f));
                int ya = Mathf.Max(0, Mathf.CeilToInt((r.yMin - area.yMin) * Ppu - 0.5f)), yb = Mathf.Min(H - 1, Mathf.FloorToInt((r.yMax - area.yMin) * Ppu - 0.5f));
                for (int y = ya; y <= yb; y++) for (int x = xa; x <= xb; x++) yield return (x, y);
            }
        }

        static uint Hash(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }

        /// <summary>Base com pontos claros e escuros espalhados (textura de pixel art).</summary>
        static Color32 Noise(int x, int y, int salt, Color32 b, Color32 lo, Color32 hi, int loPct, int hiPct)
        {
            // manchas de 2x2 para o ruído não ficar "chiado"
            uint h = Hash(x >> 1, y >> 1, salt) % 100;
            return h < loPct ? lo : h >= 100 - hiPct ? hi : b;
        }

        /// <summary>Monta a textura do chão. Determinística: o mesmo layout dá os mesmos bytes.</summary>
        public static Texture2D Render(CityLayout l, SpriteCatalog cat)
        {
            var geo = LayoutValidator.BuildGeo(l);
            var cv = new Canvas { area = geo.limiter };
            cv.W = Mathf.RoundToInt(cv.area.width * Ppu); cv.H = Mathf.RoundToInt(cv.area.height * Ppu);
            cv.px = new Color32[cv.W * cv.H];
            float h = l.streetWidth / 2f;

            // 1. asfalto
            for (int y = 0; y < cv.H; y++) for (int x = 0; x < cv.W; x++) cv.px[y * cv.W + x] = Noise(x, y, 1, Asphalt, AsphaltLo, AsphaltHi, 7, 5);

            var blocks = l.polygons.Where(p => p.points.Count >= 3).Select(p => (poly: p, r: CityGeometry.BoundingBox(new[] { (IList<Vector2>)CityGeometry.ToVectors(p.points) }), zone: ZoneOf(l, p))).ToList();

            // 2. calçadas: anel em volta de cada quarteirão, cantos arredondados, juntas a cada 12 px e meio-fio
            foreach (var b in blocks)
            {
                var outer = new Rect(b.r.xMin - Sidewalk, b.r.yMin - Sidewalk, b.r.width + 2 * Sidewalk, b.r.height + 2 * Sidewalk);
                foreach (var (x, y) in cv.In(outer))
                {
                    float wx = cv.WX(x), wy = cv.WY(y);
                    float dx = Mathf.Max(b.r.xMin - wx, 0f, wx - b.r.xMax), dy = Mathf.Max(b.r.yMin - wy, 0f, wy - b.r.yMax);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > Sidewalk) continue;
                    bool horizontalSide = dy > dx;
                    bool seam = horizontalSide ? x % 12 == 0 : y % 12 == 0;
                    cv.Set(x, y, d > Sidewalk - 1.5f / Ppu ? Curb : seam ? WalkSeam : Walk);
                }
            }

            // 3. piso de cada quarteirão
            foreach (var b in blocks) PaintBlock(cv, l, b.poly, b.r, b.zone);

            // 4. faixa tracejada no meio das ruas, interrompida nos cruzamentos
            var rects = blocks.Select(b => b.r).ToList();
            var lines = StreetGraph.Lines(rects, geo.limiter, l.streetWidth);
            var junctions = l.junctions.Select(j => (Vector2)j.position).ToList();
            foreach (var line in lines)
            {
                int c = line.horizontal ? cv.Y(line.c) : cv.X(line.c);
                int a = line.horizontal ? cv.X(line.from) : cv.Y(line.from), z = line.horizontal ? cv.X(line.to) : cv.Y(line.to);
                for (int t = a; t <= z; t++)
                {
                    if ((t % 20) >= 10) continue;
                    float along = line.horizontal ? cv.WX(t) : cv.WY(t);
                    bool nearJunction = junctions.Any(j => line.Contains(j) && Mathf.Abs(line.Along(j) - along) < h + 0.15f);
                    if (nearJunction || along < line.from + h || along > line.to - h) continue;
                    for (int k = -1; k <= 0; k++) { if (line.horizontal) cv.Set(t, c + k, Dash); else cv.Set(c + k, t, Dash); }
                }
            }

            // 5. faixas de pedestre nos cruzamentos do centro (em volta de comércio e da praça cívica)
            var downtown = blocks.Where(b => b.zone == Zone.Commercial || b.zone == Zone.Civic).Select(b => b.r).ToList();
            foreach (var j in junctions)
            {
                if (!downtown.Any(r => j.x > r.xMin - h - 0.05f && j.x < r.xMax + h + 0.05f && j.y > r.yMin - h - 0.05f && j.y < r.yMax + h + 0.05f)) continue;
                foreach (var dir in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
                {
                    var probe = j + dir * (h + 0.3f);
                    if (!lines.Any(ln => ln.Contains(probe) && ln.horizontal == (dir.y == 0f))) continue;
                    float half = h - Sidewalk - 0.04f;
                    Rect band = dir.y == 0f
                        ? Rect.MinMaxRect(Mathf.Min(j.x + dir.x * (h + 0.06f), j.x + dir.x * (h + 0.46f)), j.y - half, Mathf.Max(j.x + dir.x * (h + 0.06f), j.x + dir.x * (h + 0.46f)), j.y + half)
                        : Rect.MinMaxRect(j.x - half, Mathf.Min(j.y + dir.y * (h + 0.06f), j.y + dir.y * (h + 0.46f)), j.x + half, Mathf.Max(j.y + dir.y * (h + 0.06f), j.y + dir.y * (h + 0.46f)));
                    foreach (var (x, y) in cv.In(band))
                        if (((dir.y == 0f ? y : x) % 6) < 3) cv.Set(x, y, Zebra);
                }
            }

            // 6. pátio hachurado na rua em frente à Guarda (laranja) e aos Armazéns (amarelo)
            foreach (var s in l.structures.Where(s => s.type != StructureType.House))
            {
                var cs = cat.Get(s.sprite); if (cs == null) continue;
                var vr = cs.VisibleRect(s.bottomCenter);
                bool north = s.facing == Facing.North;
                var block = blocks.FirstOrDefault(b => b.poly.id == s.polygonId).r;
                float edge = north ? block.yMax + Sidewalk : block.yMin - Sidewalk;
                float far = north ? block.yMax + h - 0.04f : block.yMin - h + 0.04f;
                var box = Rect.MinMaxRect(vr.xMin + 0.1f, Mathf.Min(edge, far), vr.xMax - 0.1f, Mathf.Max(edge, far));
                var col = s.type == StructureType.Guard ? HatchGuard : HatchDepot;
                int bx0 = cv.X(box.xMin), bx1 = cv.X(box.xMax), by0 = cv.Y(box.yMin), by1 = cv.Y(box.yMax);
                foreach (var (x, y) in cv.In(box))
                {
                    bool border = x <= bx0 + 1 || x >= bx1 - 1 || y <= by0 + 1 || y >= by1 - 1;
                    if (border || ((x + y) % 10) < 4) cv.Set(x, y, col);
                }
            }

            // 7. heliponto na praça cívica, atrás da Guarda: disco escuro, aro claro e o H amarelo
            foreach (var hq in l.structures.Where(s => s.type == StructureType.Guard))
            {
                var cs = cat.Get(hq.sprite); if (cs == null) continue;
                var gr = cs.VisibleRect(hq.bottomCenter);
                var block = blocks.FirstOrDefault(b => b.poly.id == hq.polygonId).r;
                float back = l.structures.Where(s => s.polygonId == hq.polygonId && s.facing == Facing.North)
                    .Select(s => s.bottomCenter.y).DefaultIfEmpty(block.yMax).Min();
                float rad = Mathf.Min(0.62f, (back - gr.yMax) / 2f - 0.08f);
                if (rad < 0.35f) continue;
                var c = new Vector2(gr.center.x, (gr.yMax + back) / 2f);
                foreach (var (x, y) in cv.In(new Rect(c.x - rad, c.y - rad, 2 * rad, 2 * rad)))
                {
                    float wx = cv.WX(x) - c.x, wy = cv.WY(y) - c.y, d = Mathf.Sqrt(wx * wx + wy * wy) * Ppu, rp = rad * Ppu;
                    if (d > rp) continue;
                    float hs = rp * 0.45f;
                    bool letter = Mathf.Abs(wy * Ppu) <= hs && (Mathf.Abs(Mathf.Abs(wx * Ppu) - hs * 0.6f) <= 1.5f || Mathf.Abs(wy * Ppu) <= 1.5f && Mathf.Abs(wx * Ppu) <= hs * 0.6f);
                    cv.Set(x, y, d > rp - 2f ? Walk : d > rp - 4f ? AsphaltLo : letter ? HatchDepot : Asphalt);
                }
            }

            // 8. sombra de contato: silhueta de cada sprite deslocada 2 px para a direita e para baixo
            foreach (var (sprite, baseCenter) in l.structures.Select(s => (s.sprite, s.bottomCenter)).Concat(l.decorations.Select(d => (d.sprite, d.bottomCenter))))
            {
                var cs = cat.Get(sprite);
                if (cs == null || cs.pixels == null) continue;
                Vector2 c = cs.RectCenterFor(baseCenter);
                int ox = cv.X(c.x - cs.widthU / 2f) + 2, oy = cv.Y(c.y - cs.heightU / 2f) - 2;
                // só a faixa de baixo da silhueta (até 10 px acima da base) faz sombra no chão
                int baseRow = Mathf.RoundToInt(cs.padBottomU * cs.pxW / Mathf.Max(0.01f, cs.widthU));
                for (int y = 0; y < Mathf.Min(cs.pxH, baseRow + 10); y++)
                    for (int x = 0; x < cs.pxW; x++)
                        if (cs.pixels[y * cs.pxW + x].a > 128) cv.Mul(ox + x, oy + y, 0.8f);
            }

            var tex = new Texture2D(cv.W, cv.H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(cv.px);
            tex.Apply();
            return tex;
        }

        static Zone ZoneOf(CityLayout l, PolygonData p)
        {
            if (l.structures.Any(s => s.polygonId == p.id && s.type == StructureType.Guard)) return Zone.Civic;
            if (p.kind == PolygonKind.Plaza)
                return l.decorations.Count(d => d.polygonId == p.id && d.sprite.name.StartsWith("Pinheiro")) > 6 ? Zone.Woods : Zone.Park;
            return p.district == District.Commercial ? Zone.Commercial : p.district == District.Industrial ? Zone.Industrial : Zone.Suburban;
        }

        static void PaintBlock(Canvas cv, CityLayout l, PolygonData p, Rect r, Zone zone)
        {
            int x0 = cv.X(r.xMin), y0 = cv.Y(r.yMin);
            // caminhos do parque: cruz e praça oval em volta do monumento (as mesmas áreas livres do FillPark)
            float cx = r.center.x, cy = r.center.y;
            foreach (var (x, y) in cv.In(r))
            {
                int lx = x - x0, ly = y - y0;
                Color32 c;
                switch (zone)
                {
                    case Zone.Commercial:
                    {
                        int row = ly / 16; int sx = (lx + (row % 2) * 8) % 16;
                        c = sx == 0 || ly % 16 == 0 ? Grout : ((lx + (row % 2) * 8) / 16 + row) % 2 == 0 ? Tile : TileAlt;
                        break;
                    }
                    case Zone.Industrial:
                    {
                        bool joint = lx % 32 == 0 || ly % 32 == 0;
                        uint slab = Hash(lx / 32, ly / 32, 7);
                        int sxc = (int)(slab % 24) + 4, syc = (int)((slab >> 8) % 24) + 4, rad = (int)((slab >> 16) % 4) + 3;
                        bool stain = slab % 5 == 0 && (lx % 32 - sxc) * (lx % 32 - sxc) + (ly % 32 - syc) * (ly % 32 - syc) < rad * rad;
                        c = joint ? Joint : stain ? Stain : Noise(x, y, 3, Concrete, Joint, Concrete, 3, 0);
                        break;
                    }
                    case Zone.Civic:
                    {
                        bool border = lx < 3 || ly < 3 || x > cv.X(r.xMax) - 4 || y > cv.Y(r.yMax) - 4;
                        c = border ? Terracotta : (lx % 16 == 0 || ly % 16 == 0) ? PlazaAlt : ((lx / 16 + ly / 16) % 2 == 0 ? Plaza : PlazaAlt);
                        break;
                    }
                    case Zone.Park:
                    {
                        float wx = cv.WX(x), wy = cv.WY(y);
                        float ex = (wx - cx) / 2.7f, ey = (wy - (cy - 0.2f)) / 1.9f;
                        float e = ex * ex + ey * ey;
                        bool path = Mathf.Abs(wx - cx) < 0.7f || Mathf.Abs(wy - cy) < 0.55f || e < 1f;
                        bool edge = path && (Mathf.Abs(Mathf.Abs(wx - cx) - 0.7f) < 1.5f / Ppu && e >= 1f && Mathf.Abs(wy - cy) >= 0.55f
                            || Mathf.Abs(Mathf.Abs(wy - cy) - 0.55f) < 1.5f / Ppu && e >= 1f && Mathf.Abs(wx - cx) >= 0.7f
                            || e < 1f && e > 0.93f && Mathf.Abs(wx - cx) >= 0.7f && Mathf.Abs(wy - cy) >= 0.55f);
                        c = edge ? SandEdge : path ? Sand : Noise(x, y, 5, Park, ParkLo, ParkHi, 9, 6);
                        break;
                    }
                    case Zone.Woods: c = Noise(x, y, 6, Forest, ForestLo, ForestHi, 14, 8); break;
                    default: c = Noise(x, y, 4, Lawn, LawnLo, LawnHi, 9, 6); break;
                }
                cv.Set(x, y, c);
            }
        }

        /// <summary>
        /// Grava o PNG (só se os bytes mudaram) com importação de pixel art: Point, sem compressão, sem mipmap.
        /// Devolve a textura importada.
        /// </summary>
        public static Texture2D Bake(CityLayout l, SpriteCatalog cat)
        {
            var tex = Render(l, cat);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            string full = Path.GetFullPath(TexturePath);
            if (!File.Exists(full) || !File.ReadAllBytes(full).SequenceEqual(png))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, png);
                AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            if (imp != null && (imp.filterMode != FilterMode.Point || imp.textureCompression != TextureImporterCompression.Uncompressed
                || imp.mipmapEnabled || imp.npotScale != TextureImporterNPOTScale.None || imp.maxTextureSize < 2048 || imp.wrapMode != TextureWrapMode.Clamp))
            {
                imp.textureType = TextureImporterType.Default;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.maxTextureSize = 2048;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }
    }
}
