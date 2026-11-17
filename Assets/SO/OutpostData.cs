using UnityEngine;

// Guarita por quarteirão (GUARITAS.md, v5A): o ritmo do despacho sozinho, a obra, onde a guarita fica e a dica.
// Os quarteirões são os mesmos do para-raio (LightningRodData.blocks). Preço e crescimento ficam no UpgradeData
// "outpost" (Resources/Upgrades/Outpost.asset); o máximo é um por quarteirão com casa.
// Asset em Resources/Outpost/OutpostData; o menu Tools/Jam/Guarita/Criar dados volta aos padrões.
[CreateAssetMenu(fileName = "OutpostData", menuName = "Scriptable Objects/OutpostData")]
public class OutpostData : ScriptableObject
{
    public const string ResourcePath = "Outpost/OutpostData";

    [Header("DISPATCH")]
    [Tooltip("Segundos entre um despacho automático e o próximo da mesma guarita (era o autoDispatchInterval da Automação 4).")]
    [Min(0.1f)] public float dispatchInterval = 1f;
    [Tooltip("Segundos que o chamado espera antes de a guarita despachar: dá para ver o pino aparecer e a guarita reagir.")]
    [Min(0f)] public float reactionDelay = 1.5f;

    [Header("BUILD")]
    [Tooltip("Segundos de obra entre pagar e a guarita ficar pronta.")]
    public float buildTime = 3f;

    [Header("BOOTH")]
    [Tooltip("Distância do pé da guarita aos lados do quarteirão, em unidades do mundo (fica na calçada de dentro).")]
    public float cornerInset = 0.9f;

    [Header("PREVIEW")]
    [Tooltip("Opacidade do preenchimento do quarteirão com o mouse em cima (modo de escolha).")]
    [Range(0f, 1f)] public float fillAlpha = 0.22f;

    [Header("TIP")]
    [Tooltip("Segundos da dica que aparece em cima da primeira guarita pronta da partida.")]
    public float tipTime = 7f;

    private static OutpostData cached;

    // O asset de Resources ou, se ainda não foi criado, uma cópia com os padrões.
    public static OutpostData Load()
    {
        if (cached != null) return cached;
        cached = Resources.Load<OutpostData>(ResourcePath);
        if (cached == null) cached = CreateInstance<OutpostData>();
        return cached;
    }
}
