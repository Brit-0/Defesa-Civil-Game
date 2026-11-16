using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Luz 2D da cidade (v6, 16/11/2026). O mundo é feito de Images de UI: com material iluminado elas recebem a cor
    /// das Light2D, mas o Canvas não escreve o normal map no buffer de normais do Renderer 2D. Por isso cada Image do
    /// mundo ganha um filho [N] com SpriteRenderer e o shader "DefesaCivil/Sprite Normals Only", que só escreve as
    /// normais do mesmo sprite, no mesmo lugar: a Image passa a ter o relevo do normal map sob as luzes pontuais.
    /// Luzes: global fria (ambiente), um "sol" pontual muito longe a noroeste (age como luz direcional com normal
    /// map), postes, Guarda e depósitos. Tudo idempotente (R29). Aqui também fica o enquadramento da câmera
    /// (Limiter R27, zoom inicial e limites, centro R34).
    /// </summary>
    public partial class CityBuilder
    {
        public const string LightsName = CitySceneRefs.CgPrefix + "Lights";
        public const string NormalsName = "[N]";
        public const string GlowName = "[G]";
        public const string LightingDir = "Assets/_Project/Data/Lighting";
        const string HouseMatPath = "Assets/Shaders & Materials/HouseMAT.mat";
        const string LitSsuShader = "Sprite Shaders Ultimate/2D Lit URP SSU";

        // paleta da luz: fim de tarde, ambiente frio e sol quente baixo
        static readonly Color AmbientColor = new Color(0.88f, 0.91f, 1.00f);
        const float AmbientIntensity = 0.68f;
        static readonly Color SunColor = new Color(1.00f, 0.93f, 0.80f);
        const float SunIntensity = 0.5f, SunDistance = 120f, SunHeight = 140f;
        /// <summary>De onde vem o sol (noroeste). As sombras assadas do chão (CityGround) vão para o lado oposto.</summary>
        internal static readonly Vector2 SunDirection = new Vector2(-0.55f, 0.85f);
        static readonly Color LampColor = new Color(1.00f, 0.80f, 0.50f);
        static readonly Color GuardColor = new Color(0.72f, 0.86f, 1.00f);
        static readonly Color DepotColor = new Color(1.00f, 0.84f, 0.45f);

        const float BloomThreshold = 1.08f, BloomIntensity = 0.4f, BloomScatter = 0.6f;

        static readonly System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        static readonly Vector3[] corners = new Vector3[4];

        struct LightSpec
        {
            public Vector2 pos; public float inner, outer, intensity, normalDistance; public Color color;
        }

        static Material LitMaterial(string name, string shader)
        {
            string path = $"{LightingDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            if (!AssetDatabase.IsValidFolder(LightingDir)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Lighting");
            m = new Material(Shader.Find(shader)) { name = name };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        public static Material WorldLitMaterial => LitMaterial("WorldLitMAT", "Universal Render Pipeline/2D/Sprite-Lit-Default");
        public static Material NormalsOnlyMaterial => LitMaterial("NormalsOnlyMAT", "DefesaCivil/Sprite Normals Only");
        public static Material GlowMaterial => LitMaterial("GlowMAT", "DefesaCivil/Sprite Glow");

        Material litCache, normalsCache, glowCache;
        Material Glow => glowCache ? glowCache : glowCache = GlowMaterial;
        Material Lit => litCache ? litCache : litCache = WorldLitMaterial;
        Material Normals => normalsCache ? normalsCache : normalsCache = NormalsOnlyMaterial;

        /// <summary>Casa: espelho [N] da Image "Visual" (chamado por BuildStructures, conta junto com a casa).</summary>
        bool LightHouse(Transform t)
        {
            var visual = t.Find("Visual");
            var img = visual ? visual.GetComponent<Image>() : null;
            return img != null && EnsureNormals(img, t.GetComponent<Canvas>(), Normals);
        }

        /// <summary>Decoração: material iluminado e espelho [N] (chamado por BuildDecorations, conta junto com ela).</summary>
        bool LightDecoration(Image img)
        {
            bool changed = false;
            if (img.material != Lit) { Record(img); img.material = Lit; changed = true; }
            if (EnsureNormals(img, img.GetComponent<Canvas>(), Normals)) changed = true;
            if (EnsureGlow(img, img.GetComponent<Canvas>())) changed = true;
            return changed;
        }

        /// <summary>
        /// Filho [G]: o brilho da lâmpada (quadros _G da mesma folha), somado por cima e fora da luz 2D.
        /// Semáforo (3 quadros) ganha o TrafficLightGlow, com a fase alternada pela posição.
        /// </summary>
        bool EnsureGlow(Image img, Canvas canvas)
        {
            var existing = img.transform.Find(GlowName);
            string path = img.sprite ? AssetDatabase.GetAssetPath(img.sprite) : null;
            var frames = string.IsNullOrEmpty(path) || !img.sprite.name.EndsWith("_S") ? null
                : PixelPropsImporter.GlowFrames(path, img.sprite.name.Substring(0, img.sprite.name.Length - 2));
            if (frames == null || frames.Count == 0 || canvas == null)
            {
                if (existing == null) return false;
                Destroy(existing.gameObject, "sprite sem brilho");
                return true;
            }
            bool changed = EnsureNormals(img, frames[0], canvas.sortingLayerID, canvas.sortingOrder + 1, Glow, GlowName);
            var go = img.transform.Find(GlowName).gameObject;
            var cycle = go.GetComponent<TrafficLightGlow>();
            if (frames.Count < 3)
            {
                if (cycle) { if (undo) Undo.DestroyObjectImmediate(cycle); else Object.DestroyImmediate(cycle); changed = true; }
                return changed;
            }
            if (!cycle) { cycle = Add<TrafficLightGlow>(go); changed = true; }
            // semáforos de lados opostos da avenida ficam em fases opostas
            var p = img.transform.position;
            float offset = (Mathf.RoundToInt(p.x / 4f) + Mathf.RoundToInt(p.y / 4f)) % 2 == 0 ? 0f : (cycle.greenTime + cycle.yellowTime + cycle.redTime) / 2f;
            if (cycle.green != frames[0] || cycle.yellow != frames[1] || cycle.red != frames[2] || !Approx(cycle.offset, offset))
            {
                Record(cycle);
                cycle.green = frames[0]; cycle.yellow = frames[1]; cycle.red = frames[2]; cycle.offset = offset;
                changed = true;
            }
            return changed;
        }

        /// <summary>Partes globais: HouseMAT iluminado, chão, luzes, bloom e zoom da câmera (um item na contagem).</summary>
        void BuildLighting()
        {
            var lit = Lit;
            var normals = Normals;
            bool changed = false;

            // casas: o HouseMAT (SSU) passa para a variante iluminada do SSU; rachadura e tinta do Feel continuam
            var houseMat = AssetDatabase.LoadAssetAtPath<Material>(HouseMatPath);
            var litSsu = Shader.Find(LitSsuShader);
            if (houseMat && litSsu && houseMat.shader != litSsu)
            {
                Record(houseMat);
                houseMat.shader = litSsu;
                EditorUtility.SetDirty(houseMat);
                changed = true;
            }

            var ground = refs.uiContainer.Find(GroundName)?.GetComponent<RawImage>();
            if (ground && ground.material != lit) { Record(ground); ground.material = lit; changed = true; }
            // normais planas no chão inteiro: sem elas o sol (luz com normal map) não chega ao chão
            var groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CityGround.TexturePath);
            if (ground && groundSprite && EnsureNormals(ground, groundSprite, ground.canvas.sortingLayerID, short.MinValue, normals)) changed = true;

            if (BuildLights()) changed = true;
            if (TuneBloom()) changed = true;
            if (TuneCameraZoom()) changed = true;
            Changed(changed);
        }

        /// <summary>Filho [N]: SpriteRenderer só de normais, do tamanho e no lugar exatos da Image.</summary>
        bool EnsureNormals(Image img, Canvas canvas, Material mat)
            => img.sprite != null && canvas != null && EnsureNormals(img, img.sprite, canvas.sortingLayerID, canvas.sortingOrder, mat);

        bool EnsureNormals(Graphic img, Sprite sprite, int layer, int order, Material mat, string childName = NormalsName)
        {
            bool changed = false;
            var t = img.transform.Find(childName);
            if (t == null)
            {
                var go = new GameObject(childName, typeof(SpriteRenderer));
                Created(go);
                go.layer = img.gameObject.layer;
                go.transform.SetParent(img.transform, false);
                t = go.transform;
                changed = true;
            }
            var sr = t.GetComponent<SpriteRenderer>();
            img.rectTransform.GetWorldCorners(corners);
            var b = sprite.bounds;
            var ls = img.transform.lossyScale;
            float sx = (corners[2].x - corners[0].x) / b.size.x, sy = (corners[2].y - corners[0].y) / b.size.y;
            var scale = new Vector3(sx / ls.x, sy / ls.y, 1f);
            var center = (corners[0] + corners[2]) / 2f;
            var pos = new Vector3(center.x - b.center.x * sx, center.y - b.center.y * sy, img.transform.position.z);
            if (sr.sprite != sprite || sr.sharedMaterial != mat || sr.sortingLayerID != layer || sr.sortingOrder != order
                || (t.localScale - scale).sqrMagnitude > 1e-6f || (t.position - pos).sqrMagnitude > 1e-8f)
            {
                Record(sr); Record(t);
                sr.sprite = sprite;
                sr.sharedMaterial = mat;
                sr.sortingLayerID = layer;
                sr.sortingOrder = order;
                t.localScale = scale;
                t.position = pos;
                changed = true;
            }
            return changed;
        }

        /// <summary>Luz global (ambiente) e as luzes pontuais "[CG] Light ..." sob "[CG] Lights", na raiz da cena.</summary>
        bool BuildLights()
        {
            bool changed = false;
            var all = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include);
            var global = all.FirstOrDefault(l => l.lightType == Light2D.LightType.Global && !l.name.StartsWith(CitySceneRefs.CgPrefix));
            if (global == null) { res.log.Add("sem Global Light 2D na cena"); return false; }
            var layers = (int[])typeof(Light2D).GetField("m_ApplyToSortingLayers", Private).GetValue(global);
            if (!Approx(global.intensity, AmbientIntensity) || global.color != AmbientColor)
            {
                Record(global);
                global.intensity = AmbientIntensity; global.color = AmbientColor;
                changed = true;
            }

            var specs = new Dictionary<string, LightSpec>();
            var bb = LayoutValidator.BuildGeo(layout).limiter;
            specs["Sun"] = new LightSpec { pos = bb.center + SunDirection.normalized * SunDistance, inner = 2f * SunDistance + bb.size.magnitude, outer = 2f * SunDistance + bb.size.magnitude + 1f, intensity = SunIntensity, normalDistance = SunHeight, color = SunColor };
            foreach (var d in layout.decorations.Where(d => d.sprite.name.StartsWith("Poste")))
            {
                var cs = cat.Get(d.sprite);
                if (cs == null) continue;
                // a poça de luz fica no chão, embaixo da luminária
                specs["Lamp " + d.id] = new LightSpec { pos = (Vector2)d.bottomCenter + new Vector2(0f, 0.3f), inner = 0.4f, outer = 3.2f, intensity = 1.3f, normalDistance = 1.2f, color = LampColor };
            }
            foreach (var s in layout.structures.Where(s => s.type != StructureType.House))
            {
                bool guard = s.type == StructureType.Guard;
                specs[s.type + " " + s.id] = new LightSpec { pos = (Vector2)s.bottomCenter + new Vector2(0f, guard ? 1.2f : 1f), inner = 0.5f, outer = guard ? 6f : 4f, intensity = guard ? 0.9f : 0.8f, normalDistance = 2f, color = guard ? GuardColor : DepotColor };
            }

            var root = SceneRoot(LightsName, ref changed);
            var existing = new Dictionary<string, Light2D>();
            foreach (Transform c in root) { var l = c.GetComponent<Light2D>(); if (l) existing[c.name] = l; }
            foreach (var kv in specs)
            {
                string name = CitySceneRefs.CgPrefix + "Light " + kv.Key;
                if (!existing.TryGetValue(name, out var light))
                {
                    var go = new GameObject(name);
                    Created(go);
                    go.transform.SetParent(root, false);
                    light = Add<Light2D>(go);
                    res.created++;
                    changed = true;
                }
                existing.Remove(name);
                if (ApplyLight(light, kv.Value, layers)) changed = true;
            }
            foreach (var kv in existing) { Destroy(kv.Value.gameObject, "luz fora do layout"); changed = true; }
            return changed;
        }

        /// <summary>Bloom leve no Global Volume da cena: só o que já brilha (janelas, postes, fogo) ganha halo.</summary>
        bool TuneBloom()
        {
            var volume = Object.FindAnyObjectByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Include);
            if (volume == null || volume.sharedProfile == null || !volume.sharedProfile.TryGet(out Bloom bloom)) return false;
            if (Approx(bloom.threshold.value, BloomThreshold) && Approx(bloom.intensity.value, BloomIntensity) && Approx(bloom.scatter.value, BloomScatter)
                && bloom.threshold.overrideState && bloom.intensity.overrideState && bloom.scatter.overrideState)
                return false;
            Record(volume.sharedProfile);
            bloom.threshold.Override(BloomThreshold);
            bloom.intensity.Override(BloomIntensity);
            bloom.scatter.Override(BloomScatter);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(volume.sharedProfile);
            return true;
        }

        // zoom do CameraController (valores da cena; o código fica intocado, R28): mapa v6 visto de mais longe; 12 (16/11,
        // pedido do Pietro: zoom inicial mais aberto) põe a Guarda e os dois Armazéns na primeira tela
        const float StartZoom = 12f, MinZoom = 3f, MaxZoom = 16f, ZoomStep = 0.3f;

        bool TuneCameraZoom()
        {
            var cc = refs.mainCamera.GetComponent<CameraController>();
            if (cc == null) return false;
            var so = new SerializedObject(cc);
            var fields = new[] { ("zoom", StartZoom), ("minZoom", MinZoom), ("maxZoom", MaxZoom), ("zoomMult", ZoomStep) };
            bool changed = false;
            foreach (var (name, value) in fields)
            {
                var p = so.FindProperty(name);
                if (p != null && !Approx(p.floatValue, value)) { p.floatValue = value; changed = true; }
            }
            if (changed)
            {
                if (undo) so.ApplyModifiedProperties(); else so.ApplyModifiedPropertiesWithoutUndo();
            }
            return changed;
        }

        Transform SceneRoot(string name, ref bool changed)
        {
            var scene = refs.houses.gameObject.scene;
            var go = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
            if (go == null)
            {
                go = new GameObject(name);
                Created(go);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                res.created++;
                changed = true;
            }
            return go.transform;
        }

        bool ApplyLight(Light2D l, LightSpec s, int[] layers)
        {
            var f = typeof(Light2D);
            var quality = (Light2D.NormalMapQuality)f.GetField("m_NormalMapQuality", Private).GetValue(l);
            float dist = (float)f.GetField("m_NormalMapDistance", Private).GetValue(l);
            var lay = (int[])f.GetField("m_ApplyToSortingLayers", Private).GetValue(l);
            if (l.lightType == Light2D.LightType.Point && Approx(l.pointLightInnerRadius, s.inner) && Approx(l.pointLightOuterRadius, s.outer)
                && Approx(l.intensity, s.intensity) && l.color == s.color && quality == Light2D.NormalMapQuality.Accurate && Approx(dist, s.normalDistance)
                && lay != null && lay.SequenceEqual(layers) && ((Vector2)l.transform.position - s.pos).sqrMagnitude < 1e-8f)
                return false;
            Record(l); Record(l.transform);
            l.lightType = Light2D.LightType.Point;
            l.pointLightInnerRadius = s.inner;
            l.pointLightOuterRadius = s.outer;
            l.intensity = s.intensity;
            l.color = s.color;
            f.GetField("m_NormalMapQuality", Private).SetValue(l, Light2D.NormalMapQuality.Accurate);
            f.GetField("m_NormalMapDistance", Private).SetValue(l, s.normalDistance);
            f.GetField("m_ApplyToSortingLayers", Private).SetValue(l, layers.ToArray());
            l.transform.position = new Vector3(s.pos.x, s.pos.y, 0f);
            EditorUtility.SetDirty(l);
            return true;
        }

        // ---------- Limiter (R27) ----------

        void FitLimiter()
        {
            var bb = PolygonBox();
            float m = layout.limiterMargin; // rua de contorno (R27)
            bb = Rect.MinMaxRect(bb.xMin - m, bb.yMin - m, bb.xMax + m, bb.yMax + m);
            var t = refs.limiter.transform;
            var spriteSize = refs.limiter.sprite.bounds.size;
            var parentScale = t.parent ? t.parent.lossyScale : Vector3.one;
            var pos = new Vector3(bb.center.x, bb.center.y, t.position.z);
            var scale = new Vector3(bb.width / spriteSize.x / parentScale.x, bb.height / spriteSize.y / parentScale.y, t.localScale.z);
            bool changed = (t.position - pos).sqrMagnitude > 1e-10f || (t.localScale - scale).sqrMagnitude > 1e-10f;
            if (changed)
            {
                Record(t);
                t.position = pos; t.localScale = scale;
            }
            Changed(changed);
        }

        // ---------- chão assado (v5) ----------

        public const string GroundName = CitySceneRefs.CgPrefix + "Ground";

        // ---------- enquadramento dos chãos (R34) ----------

        public static float RequiredOrtho(Rect bb, Vector2 center)
        {
            float halfW = Mathf.Max(Mathf.Abs(bb.xMin - center.x), Mathf.Abs(bb.xMax - center.x));
            float halfH = Mathf.Max(Mathf.Abs(bb.yMin - center.y), Mathf.Abs(bb.yMax - center.y));
            return Mathf.Max(halfH, halfW / DisplayAspect);
        }

        void FitPolygonFraming()
        {
            Vector2 c = refs.mainCamera.transform.position;
            float ortho = RequiredOrtho(PolygonBox(), c);
            bool changed = false;
            if (!Approx(refs.polygonCamera.orthographicSize, ortho))
            {
                Record(refs.polygonCamera);
                refs.polygonCamera.orthographicSize = ortho;
                changed = true;
            }
            var rt = refs.polygonsDisplay;
            var parentScale = rt.parent.lossyScale;
            var size = new Vector2(2f * ortho * DisplayAspect / parentScale.x, 2f * ortho / parentScale.y);
            var half = new Vector2(0.5f, 0.5f);
            var pos = new Vector3(c.x, c.y, rt.position.z);
            if (rt.anchorMin != half || rt.anchorMax != half || rt.pivot != half || !Approx(rt.sizeDelta, size) || ((Vector2)rt.position - c).sqrMagnitude > 1e-8f)
            {
                Record(rt);
                rt.anchorMin = half; rt.anchorMax = half; rt.pivot = half;
                rt.sizeDelta = size;
                rt.position = pos;
                changed = true;
            }
            Changed(changed);
        }
    }
}
