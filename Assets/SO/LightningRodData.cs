using System.Collections.Generic;
using UnityEngine;

// Um quarteirão onde dá para pôr um para-raio: um polígono "Block" do city_layout.json (todos são retângulos).
[System.Serializable]
public class RodBlockSlot
{
    public string id = "";
    public string district = "";
    public Vector2 min;
    public Vector2 max;

    public Rect Area => Rect.MinMaxRect(min.x, min.y, max.x, max.y);
}

// Para-raio por quarteirão (PARARAIO.md): onde dá para construir, quanto ele puxa o raio e quanto a obra demora.
// Preço e quantidade ficam no UpgradeData "rod" (Resources/Upgrades/Rod.asset: custo, crescimento e maxLevel).
// Asset em Resources/LightningRod/LightningRodData; o menu Tools/Jam/Para-raio/Criar dados lê os quarteirões do mapa.
[CreateAssetMenu(fileName = "LightningRodData", menuName = "Scriptable Objects/LightningRodData")]
public class LightningRodData : ScriptableObject
{
    public const string ResourcePath = "LightningRod/LightningRodData";

    [Header("BLOCKS")]
    [Tooltip("Quarteirões do mapa v6 (polígonos Block do city_layout.json, em unidades do mundo). Os sem casa ficam de fora no jogo.")]
    public List<RodBlockSlot> blocks = new();
    [Tooltip("Folga em volta do quarteirão, em unidades, para a base de uma casa contar como dele (as ruas entre quarteirões têm 1,2 u).")]
    public float houseMargin = 0.4f;

    [Header("STRIKES")]
    [Tooltip("Peso de cada casa intacta de quarteirão protegido no sorteio do raio da tempestade (1 = o raio cai onde cairia). Com 6, um para-raio absorve de 1 a 3 raios por partida (docs/jam/sim/pararaio.py); com 1, só 0,5.")]
    [Min(1f)] public float attraction = 6f;

    [Header("BUILD")]
    [Tooltip("Segundos de obra entre pagar e o para-raio aparecer no telhado.")]
    public float buildTime = 3f;

    [Header("PREVIEW")]
    [Tooltip("Espaço entre os traços da borda do quarteirão na prévia, em unidades do mundo.")]
    public float dashSpacing = 0.7f;
    [Tooltip("Lado de cada traço da borda, em unidades do mundo.")]
    public float dashSize = 0.3f;
    [Tooltip("Opacidade do preenchimento do quarteirão com o mouse em cima (modo de escolha).")]
    [Range(0f, 1f)] public float fillAlpha = 0.22f;

    private static LightningRodData cached;

    // O asset de Resources ou, se ainda não foi criado, uma cópia com os padrões (sem quarteirões: o para-raio desliga).
    public static LightningRodData Load()
    {
        if (cached != null) return cached;
        cached = Resources.Load<LightningRodData>(ResourcePath);
        if (cached == null) cached = CreateInstance<LightningRodData>();
        return cached;
    }
}
