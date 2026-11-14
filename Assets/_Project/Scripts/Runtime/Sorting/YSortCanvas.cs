using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Ordem de desenho por Y para Houses, Trees e Workers, que ficam em empties diferentes do mesmo Canvas.
    /// Cada objeto tem um Canvas aninhado com Override Sorting; a sortingOrder vem do y da base do desenho
    /// (quanto mais ao sul, maior a ordem e mais à frente). Empate: Houses &lt; Trees &lt; Workers.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class YSortCanvas : MonoBehaviour
    {
        public const int Kind_House = 0, Kind_Tree = 1, Kind_Worker = 2;
        /// <summary>Ordem base da cidade; fica acima do Canvas raiz (0) e abaixo de <see cref="OverlayOrder"/>.</summary>
        public const int BaseOrder = 5000;
        /// <summary>Passos de ordem por unidade de mundo (2 cm).</summary>
        public const int StepsPerUnit = 50;
        /// <summary>Canvas filhos (ex.: painel de informação da estrutura) ficam acima de tudo.</summary>
        public const int OverlayOrder = 10000;

        public int kind;
        /// <summary>Distância (u) da borda inferior do retângulo até a base visível do desenho.</summary>
        public float baseOffsetU;
        /// <summary>Recalcula todo frame (Workers). Estruturas e decorações são estáticas.</summary>
        public bool dynamic;

        static readonly Vector3[] corners = new Vector3[4];
        Canvas canvas;
        int last = int.MinValue;

        public static int OrderFor(float baseY, int kind) => BaseOrder - Mathf.RoundToInt(baseY * StepsPerUnit) * 3 + kind;

        public float BaseY()
        {
            var rt = transform as RectTransform;
            if (rt == null) return transform.position.y;
            rt.GetWorldCorners(corners);
            return corners[0].y + baseOffsetU;
        }

        public int ComputeOrder() => OrderFor(BaseY(), kind);

        /// <summary>Liga o Override Sorting e aplica a ordem do y atual.</summary>
        public void Apply()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas == null) return;
            if (!canvas.overrideSorting) canvas.overrideSorting = true;
            int order = ComputeOrder();
            if (canvas.sortingOrder != order) canvas.sortingOrder = order;
            if (last == int.MinValue && Application.isPlaying)
                foreach (var c in GetComponentsInChildren<Canvas>(true))
                    if (c != canvas && c.overrideSorting) c.sortingOrder = OverlayOrder;
            last = order;
        }

        void OnEnable() => Apply();
        void LateUpdate() { if (dynamic) Apply(); }
    }
}
