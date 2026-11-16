using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Semáforo de 3 lâmpadas: troca o quadro de brilho (verde, amarelo, vermelho) do SpriteRenderer "[G]".
    /// O ciclo roda pelo relógio do jogo; a fase (offset) alterna entre as ruas que se cruzam.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TrafficLightGlow : MonoBehaviour
    {
        [Header("QUADROS")]
        public Sprite green;
        public Sprite yellow;
        public Sprite red;

        [Header("TEMPOS (s)")]
        public float greenTime = 5f;
        public float yellowTime = 1.5f;
        public float redTime = 6.5f;
        /// <summary>Atraso do ciclo: metade do ciclo para a rua que cruza.</summary>
        public float offset;

        SpriteRenderer sr;
        int last = -1;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            float cycle = greenTime + yellowTime + redTime;
            if (cycle <= 0f) return;
            float t = Mathf.Repeat(Time.time + offset, cycle);
            int i = t < greenTime ? 0 : t < greenTime + yellowTime ? 1 : 2;
            if (i == last) return;
            last = i;
            sr.sprite = i == 0 ? green : i == 1 ? yellow : red;
        }
    }
}
