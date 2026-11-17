using UnityEngine;
using UnityEngine.UI;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Sombra de construção que nasce no Play (base e armazém da expansão): o chão assado não a tem. Mesma regra da
    /// assada pelo CityGround (ShadowModel): a silhueta do sprite com a fachada inclinada para baixo e para a direita e
    /// o telhado só deslocado, da força do pé à da ponta. Fica numa Image sem Canvas próprio logo acima do chão, então
    /// casas, árvores e trabalhadores (Canvas com ordem por Y) passam por cima dela, como passam pela assada.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class StructureShadow : BaseMeshEffect
    {
        [Tooltip("Da borda de baixo do retângulo até a base visível do desenho, em unidades do retângulo")]
        [SerializeField] private float baseOffset;

        static readonly Vector3[] corners = new Vector3[4];

        /// <summary>Põe a sombra do "Visual" da construção em 'parent' (sem Canvas próprio, por cima do chão).</summary>
        public static StructureShadow Cast(Component building, Transform parent)
        {
            Transform visual = building != null ? building.transform.Find("Visual") : null;
            Image source = visual != null ? visual.GetComponent<Image>() : null;
            if (source == null || source.sprite == null || parent == null) return null;

            var rect = (RectTransform)new GameObject("Sombra", typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            source.rectTransform.GetWorldCorners(corners);
            float scaleX = parent.lossyScale.x != 0f ? parent.lossyScale.x : 1f, scaleY = parent.lossyScale.y != 0f ? parent.lossyScale.y : 1f;
            rect.position = (corners[0] + corners[2]) * 0.5f;
            rect.sizeDelta = new Vector2((corners[2].x - corners[0].x) / scaleX, (corners[2].y - corners[0].y) / scaleY);

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = source.sprite;
            image.raycastTarget = false;
            var shadow = rect.gameObject.AddComponent<StructureShadow>();
            YSortCanvas sort = building.GetComponentInChildren<YSortCanvas>();
            shadow.baseOffset = sort != null ? sort.baseOffsetU / scaleY : 0f;
            shadow.graphic.SetVerticesDirty();
            return shadow;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || !(graphic is Image image) || image.sprite == null) return;

            Sprite sprite = image.sprite;
            Rect r = graphic.rectTransform.rect;
            Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            float baseY = r.yMin + baseOffset, top = r.yMax - baseY;
            float facade = Mathf.Max(0f, VisibleTop(sprite, r) - baseY) * ShadowModel.FacadeFraction;

            // Faixas da base do retângulo à fachada e da fachada ao topo: a sombra é linear em cada uma
            float[] rows = facade < top ? new[] { r.yMin - baseY, facade, top } : new[] { r.yMin - baseY, top };
            vh.Clear();
            foreach (float h in rows)
            {
                float v = Mathf.Lerp(uv.y, uv.w, (h + baseY - r.yMin) / Mathf.Max(0.0001f, r.height));
                Vector2 p = ShadowModel.Project(h, facade);
                Color32 color = new Color(0f, 0f, 0f, ShadowModel.Strength(Mathf.Max(0f, h), top));
                vh.AddVert(new Vector3(r.xMin + p.x, baseY + p.y), color, new Vector2(uv.x, v));
                vh.AddVert(new Vector3(r.xMax + p.x, baseY + p.y), color, new Vector2(uv.z, v));
            }
            for (int i = 0; i + 1 < rows.Length; i++)
            {
                int a = i * 2;
                vh.AddTriangle(a, a + 2, a + 3);
                vh.AddTriangle(a, a + 3, a + 1);
            }
        }

        // Topo do desenho (sem a margem transparente) pela malha justa do sprite, no espaço do retângulo
        static float VisibleTop(Sprite sprite, Rect r)
        {
            float maxY = float.MinValue;
            foreach (Vector2 v in sprite.vertices) maxY = Mathf.Max(maxY, v.y);
            if (maxY == float.MinValue) return r.yMax;
            float px = maxY * sprite.pixelsPerUnit + sprite.pivot.y;
            return r.yMin + Mathf.Clamp01(px / sprite.rect.height) * r.height;
        }
    }
}
