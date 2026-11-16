using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Sombra projetada de quem se mexe (trabalhadores): antes da malha da Image, uma cópia da mesma silhueta deitada
    /// no chão, achatada e inclinada para o lado oposto ao sol, na cor da sombra. Sai na mesma chamada de desenho da
    /// Image (mesma textura e material), então custa só 4 vértices. As sombras do que fica parado (casas, árvores,
    /// props) são assadas no chão pelo CityGround, na mesma direção.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class ProjectedShadow : BaseMeshEffect
    {
        [Header("SOMBRA")]
        [Tooltip("Deslocamento da sombra por unidade de altura acima do pé (x para a direita, y negativo = para baixo).")]
        [SerializeField] private Vector2 direction = new Vector2(0.23f, -0.35f);
        [Tooltip("Altura do pé, em fração do retângulo a partir de baixo (margem transparente do sprite).")]
        [SerializeField, Range(0f, 0.5f)] private float foot = 0.04f;
        [SerializeField] private Color color = new Color(0.05f, 0.06f, 0.1f, 0.34f);

        static readonly List<UIVertex> stream = new List<UIVertex>(), doubled = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            stream.Clear(); doubled.Clear();
            vh.GetUIVertexStream(stream);
            var rect = graphic.rectTransform.rect;
            float baseY = rect.yMin + rect.height * foot;
            foreach (var v in stream)
            {
                var s = v;
                float h = v.position.y - baseY;
                s.position = new Vector3(v.position.x + h * direction.x, baseY + h * direction.y, v.position.z);
                var c = color;
                c.a *= v.color.a / 255f;
                s.color = c;
                doubled.Add(s);
            }
            doubled.AddRange(stream);
            vh.Clear();
            vh.AddUIVertexTriangleStream(doubled);
        }
    }
}
