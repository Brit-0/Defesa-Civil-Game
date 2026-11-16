using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Auditoria de escala da decoração (pedido do Pietro 16/11/2026: árvore maior que prédio, arbusto do tamanho de
    /// carro). Mede a altura visível de cada sprite de decoração já na escala do catálogo, que é a mesma do Build, e
    /// compara com a casa média. Rodar pelo menu Tools/City/Auditar Escala ou pelo teste ScaleAuditTests.
    /// </summary>
    public static class ScaleAudit
    {
        /// <summary>Teto de altura visível de cada tipo, em fração da casa média.</summary>
        public static readonly (string kind, string[] prefixes, float limit)[] Limits =
        {
            ("árvore", new[] { "Arvore", "Carvalho", "Pinheiro", "Palmeira" }, 0.65f),
            ("arbusto", new[] { "Arbusto", "Folhagem", "Planta_", "Capim", "Vaso" }, 0.35f),
            ("flor", new[] { "Flor_", "Cogumelo" }, 0.2f),
            ("cone", new[] { "Cone" }, 0.3f),
        };

        public static float HouseHeight(SpriteCatalog cat) => cat.Suburban.Concat(cat.Commercial).Average(c => c.visHU);

        /// <summary>Uma linha por tipo (maior sprite e fração da casa) e as que passam do teto.</summary>
        public static List<string> Check(SpriteCatalog cat, out List<string> failures)
        {
            float house = HouseHeight(cat);
            var lines = new List<string> { $"casa média {house:0.00} u" };
            failures = new List<string>();
            foreach (var (kind, prefixes, limit) in Limits)
            {
                var sprites = cat.Decoration.Where(c => prefixes.Any(p => c.texture.StartsWith(p))).ToList();
                if (sprites.Count == 0) continue;
                var top = sprites.OrderByDescending(c => c.visHU).First();
                lines.Add($"{kind}: {sprites.Count} sprites, média {sprites.Average(c => c.visHU):0.00} u, maior {top.texture} {top.visHU:0.00} u ({top.visHU / house:0.00} da casa, teto {limit})");
                failures.AddRange(sprites.Where(c => c.visHU / house > limit).Select(c => $"{c.texture} {c.visHU / house:0.00} da casa (teto {limit})"));
            }
            return lines;
        }

        [MenuItem("Tools/City/Auditar Escala")]
        static void Menu()
        {
            var lines = Check(SpriteCatalog.Load(true), out var failures);
            Debug.Log("[CityGen] Escala: " + string.Join("; ", lines));
            if (failures.Count == 0) Debug.Log("[CityGen] Escala OK: nenhuma decoração acima do teto.");
            else Debug.LogWarning("[CityGen] Escala: acima do teto: " + string.Join(", ", failures));
        }
    }
}
