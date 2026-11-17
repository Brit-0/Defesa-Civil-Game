using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// No Play, tira o Canvas aninhado de cada árvore e guarda a mesma ordem por Y na ordem de irmãos.
    /// Cada Canvas aninhado custa ao menos uma chamada de desenho: com 579 árvores o mapa passava de 900 por quadro e o
    /// WebGL caía para 37 FPS com a CPU 2x mais lenta (MEGABRAIN, 16/11). Sem o Canvas, as árvores entram no Canvas do
    /// mundo e o UGUI junta as que usam a mesma textura. Árvore x casa não muda: o Visual da casa já fica em
    /// <see cref="YSortCanvas.OverlayOrder"/>. A cena e o gerador seguem com o Canvas (R30 do validador).
    /// </summary>
    public static class TreeCanvasMerge
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode) => Merge();

        /// <summary>Junta as árvores no Canvas do pai; devolve quantas mudaram.</summary>
        public static int Merge()
        {
            var byParent = new Dictionary<Transform, List<(int order, float x, YSortCanvas sort)>>();
            foreach (YSortCanvas sort in Object.FindObjectsByType<YSortCanvas>(FindObjectsInactive.Include))
            {
                if (sort.kind != YSortCanvas.Kind_Tree || sort.dynamic || sort.transform.parent == null) continue;

                Canvas canvas = sort.GetComponent<Canvas>();
                int order = canvas != null && canvas.overrideSorting ? canvas.sortingOrder : sort.ComputeOrder();
                if (!byParent.TryGetValue(sort.transform.parent, out var list)) byParent[sort.transform.parent] = list = new();
                list.Add((order, sort.transform.position.x, sort));
            }

            int merged = 0;
            foreach (var list in byParent.Values)
            {
                // ordem crescente = mais ao norte primeiro = desenhada atrás
                list.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : a.x.CompareTo(b.x));
                foreach (var (_, _, sort) in list)
                {
                    GameObject go = sort.gameObject;
                    go.transform.SetAsLastSibling();
                    Object.DestroyImmediate(sort);
                    Canvas canvas = go.GetComponent<Canvas>();
                    if (canvas != null) Object.DestroyImmediate(canvas);
                    merged++;
                }
            }
            return merged;
        }
    }
}
