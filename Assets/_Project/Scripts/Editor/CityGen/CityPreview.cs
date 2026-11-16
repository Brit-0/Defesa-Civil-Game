using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Prévia em PNG de um layout, sem tocar na cena: chãos, estruturas e decorações na mesma ordem de desenho
    /// do jogo (Houses por Y, depois Trees por Y). 32 px por unidade.
    /// </summary>
    public static class CityPreview
    {
        static readonly Color32 StreetColor = new Color32(26, 23, 34, 255);
        static readonly Color32 GroundColor = new Color32(86, 86, 86, 255);

        public static void Render(CityLayout l, SpriteCatalog cat, string pngPath, int ppu = 32, bool waypointGraph = false)
        {
            var geo = LayoutValidator.BuildGeo(l);
            var area = geo.limiter;
            int W = Mathf.CeilToInt(area.width * ppu), H = Mathf.CeilToInt(area.height * ppu);
            var img = new Color32[W * H];
            for (int i = 0; i < img.Length; i++) img[i] = StreetColor;

            foreach (var poly in geo.All)
            {
                if (poly.Count < 3) continue;
                var bb = CityGeometry.BoundingBox(new[] { (IList<Vector2>)poly });
                int xa = Mathf.Max(0, Mathf.FloorToInt((bb.xMin - area.xMin) * ppu)), xb = Mathf.Min(W - 1, Mathf.CeilToInt((bb.xMax - area.xMin) * ppu));
                int ya = Mathf.Max(0, Mathf.FloorToInt((bb.yMin - area.yMin) * ppu)), yb = Mathf.Min(H - 1, Mathf.CeilToInt((bb.yMax - area.yMin) * ppu));
                for (int y = ya; y <= yb; y++)
                    for (int x = xa; x <= xb; x++)
                    {
                        var p = new Vector2(area.xMin + (x + 0.5f) / ppu, area.yMin + (y + 0.5f) / ppu);
                        if (CityGeometry.PointInPolygon(p, poly, 0f)) img[y * W + x] = GroundColor;
                    }
            }

            void Draw(SpriteRef sr, Vector2 baseCenter)
            {
                var cs = cat.Get(sr);
                if (cs == null || cs.pixels == null) return;
                Vector2 rc = cs.RectCenterFor(baseCenter);
                float scale = ppu / SpriteCatalog.PixelsPerUnit;
                int ox = Mathf.RoundToInt((rc.x - cs.widthU / 2f - area.xMin) * ppu), oy = Mathf.RoundToInt((rc.y - cs.heightU / 2f - area.yMin) * ppu);
                int dw = Mathf.RoundToInt(cs.pxW * scale), dh = Mathf.RoundToInt(cs.pxH * scale);
                for (int y = 0; y < dh; y++)
                {
                    int ty = oy + y; if (ty < 0 || ty >= H) continue;
                    int sy = Mathf.Min(cs.pxH - 1, (int)(y / scale));
                    for (int x = 0; x < dw; x++)
                    {
                        int tx = ox + x; if (tx < 0 || tx >= W) continue;
                        var c = cs.pixels[sy * cs.pxW + Mathf.Min(cs.pxW - 1, (int)(x / scale))];
                        if (c.a < 20) continue;
                        img[ty * W + tx] = new Color32(c.r, c.g, c.b, 255);
                    }
                }
            }

            foreach (var s in l.structures.OrderByDescending(s => s.bottomCenter.y).ThenBy(s => s.bottomCenter.x)) Draw(s.sprite, s.bottomCenter);
            foreach (var d in l.decorations.OrderByDescending(d => d.bottomCenter.y).ThenBy(d => d.bottomCenter.x)) Draw(d.sprite, d.bottomCenter);

            if (waypointGraph)
            {
                var pos = l.structures.ToDictionary(StreetGraph.EntryKey, s => (Vector2)s.entry);
                foreach (var j in l.junctions) pos[StreetGraph.JunctionKey(j.id)] = j.position;
                var lineC = new Color32(255, 214, 64, 255); var entryC = new Color32(80, 220, 255, 255); var juncC = new Color32(255, 90, 90, 255);
                void Px(int x, int y, Color32 c) { if (x >= 0 && x < W && y >= 0 && y < H) img[y * W + x] = c; }
                (int, int) ToPx(Vector2 p) => (Mathf.RoundToInt((p.x - area.xMin) * ppu), Mathf.RoundToInt((p.y - area.yMin) * ppu));
                foreach (var k in l.links)
                {
                    if (!pos.TryGetValue(k.a, out var a) || !pos.TryGetValue(k.b, out var b)) continue;
                    var (x0, y0) = ToPx(a); var (x1, y1) = ToPx(b);
                    int n = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
                    for (int i = 0; i <= n; i++) { float tt = n == 0 ? 0 : i / (float)n; int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, tt)), y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, tt)); Px(x, y, lineC); Px(x, y + 1, lineC); }
                }
                foreach (var kv in pos)
                {
                    var (cx, cy) = ToPx(kv.Value);
                    var c = kv.Key.StartsWith(StreetGraph.JunctionType) ? juncC : entryC;
                    for (int dy = -3; dy <= 3; dy++) for (int dx = -3; dx <= 3; dx++) if (dx * dx + dy * dy <= 9) Px(cx + dx, cy + dy, c);
                }
            }
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.SetPixels32(img); tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(pngPath));
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
