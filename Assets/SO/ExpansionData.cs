using System.Collections.Generic;
using UnityEngine;

public enum LotKind { Base, Depot }

// Um lugar de construção: a entrada fica na linha central da rua, como o "entry" das casas no city_layout.json.
// O prédio nasce encostado nela, na mesma distância que a Guarda (ou o Armazém) original tem da sua entrada.
[System.Serializable]
public class LotSlot
{
    public string label = "OESTE";
    public LotKind kind;
    public Vector2 entry;
}

// Terrenos de expansão (EXPANSAO.md): onde dá para construir base da Guarda e armazém novos, e quanto a obra demora.
// Preço e quantidade ficam nos UpgradeData base2 e depot2 (custo, crescimento e maxLevel).
// Asset em Resources/Expansion/ExpansionData; sem ele valem os padrões daqui. Menu: Tools/Jam/Expansão/Criar dados.
[CreateAssetMenu(fileName = "ExpansionData", menuName = "Scriptable Objects/ExpansionData")]
public class ExpansionData : ScriptableObject
{
    public const string ResourcePath = "Expansion/ExpansionData";

    [Header("LOTS")]
    [Tooltip("Mapa v6: praça a oeste (polígono 009) e mata a leste (014), fileira de baixo de cada uma, de frente para a rua y = -22,25. Medido no city_layout.json: com as duas bases a caminhada média de uma casa até a base cai de 28,4 para 21,8 u.")]
    public List<LotSlot> lots = DefaultLots();

    [Header("BUILD")]
    [Tooltip("Segundos de obra entre pagar e o prédio aparecer.")]
    public float buildTime = 3f;

    [Header("PREVIEW")]
    [Tooltip("O anel de alcance passa por esta fração das casas que a base nova atenderia (0,5 = metade delas dentro). No mapa v6 dá 13 u no OESTE e 16 u no LESTE, mais ou menos o meio do caminho até a Guarda.")]
    [Range(0.3f, 1f)] public float reachPercentile = 0.5f;
    [Tooltip("Espaço entre os pontos do anel e da linha de prévia, em unidades do mundo.")]
    public float dotSpacing = 0.7f;
    [Tooltip("Lado de cada ponto da prévia, em unidades do mundo.")]
    public float dotSize = 0.3f;

    public static List<LotSlot> DefaultLots() => new()
    {
        new LotSlot { label = "OESTE", kind = LotKind.Base, entry = new Vector2(-21.95f, -22.2486f) },
        new LotSlot { label = "OESTE", kind = LotKind.Depot, entry = new Vector2(-25.55f, -22.2486f) },
        new LotSlot { label = "LESTE", kind = LotKind.Base, entry = new Vector2(23.75f, -22.2486f) },
        new LotSlot { label = "LESTE", kind = LotKind.Depot, entry = new Vector2(27.35f, -22.2486f) },
    };

    private static ExpansionData cached;

    // O asset de Resources ou, se ainda não foi criado, uma cópia com os padrões.
    public static ExpansionData Load()
    {
        if (cached != null) return cached;
        cached = Resources.Load<ExpansionData>(ResourcePath);
        if (cached == null) cached = CreateInstance<ExpansionData>();
        return cached;
    }
}
