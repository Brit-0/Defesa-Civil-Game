using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Regra única das sombras do que fica parado: a assada no chão (CityGround) e a das construções que nascem no Play
    /// (StructureShadow, expansão) caem no mesmo lugar, com o mesmo tamanho e a mesma força.
    /// O sol vem de cima à esquerda (a mesma luz do CityBuilder): a sombra vai para baixo e para a direita.
    /// Prédio visto de cima em 3/4: só a parede da frente (fachada) é altura; o telhado acima dela é fundo, então a
    /// sombra do telhado só anda o que a fachada manda. Árvore, poste e placa são altura do pé até a ponta.
    /// </summary>
    public static class ShadowModel
    {
        public static readonly Vector2 SunDirection = new Vector2(-0.55f, 0.85f);
        /// <summary>Quanto a sombra anda por unidade de altura.</summary>
        public const float Length = 0.32f;
        /// <summary>Força (quanto escurece o chão) no pé e na ponta.</summary>
        public const float Foot = 0.34f, Tip = 0.16f;
        /// <summary>Altura da fachada em fração da altura visível do prédio (o resto do desenho é telhado).</summary>
        public const float FacadeFraction = 0.4f;

        /// <summary>Deslocamento da sombra por unidade de altura.</summary>
        public static Vector2 Offset => -SunDirection.normalized * Length;

        /// <summary>
        /// Onde cai no chão o ponto do desenho a 'height' acima da base visível (mesma unidade de 'facade'), em relação ao
        /// próprio ponto na base. Até a fachada é altura; acima dela é fundo (telhado) e a sombra só anda a da fachada.
        /// </summary>
        public static Vector2 Project(float height, float facade)
        {
            Vector2 o = Offset;
            if (height <= facade) return o * height;
            return new Vector2(o.x * facade, o.y * facade + (height - facade));
        }

        /// <summary>Força da sombra a 'height' acima da base, de 'Foot' no pé a 'Tip' no topo ('top').</summary>
        public static float Strength(float height, float top) => Mathf.Lerp(Foot, Tip, Mathf.Clamp01(height / Mathf.Max(0.0001f, top)));
    }
}
