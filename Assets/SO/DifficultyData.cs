using System;
using UnityEngine;

// Multiplicadores de cada dificuldade do menu inicial (docs/jam/CONSERTOS_V7B1.md), por cima dos números do
// BalanceData e do Centro de Operações. Normal fica tudo em 1: o jogo como está.
[Serializable]
public class DifficultySettings
{
    [Tooltip("Intervalo entre desastres: > 1 = menos desastres, < 1 = mais")]
    public float intervalMultiplier = 1f;
    [Tooltip("Velocidade da rampa (casas por desastre e intervalo ao longo da partida): > 1 = a dificuldade cresce antes")]
    public float rampSpeed = 1f;
    [Tooltip("Custo do Centro de Operações (a meta)")]
    public float goalCostMultiplier = 1f;
}

[CreateAssetMenu(fileName = "DifficultyData", menuName = "Scriptable Objects/DifficultyData")]
public class DifficultyData : ScriptableObject
{
    public const string ResourcePath = "Difficulty/DifficultyData";

    [Header("FÁCIL")]
    public DifficultySettings easy = new() { intervalMultiplier = 1.15f, rampSpeed = 1f, goalCostMultiplier = .85f };
    [Header("NORMAL (tudo 1 = o jogo como está)")]
    public DifficultySettings normal = new();
    [Header("DIFÍCIL")]
    public DifficultySettings hard = new() { intervalMultiplier = .9f, rampSpeed = 1.2f, goalCostMultiplier = 1.15f };

    [Header("LIMITES")]
    [Tooltip("O multiplicador do intervalo nunca leva abaixo disto (nem sobe uma curva que já vinha abaixo)")]
    public float intervalFloor = 10f;

    public DifficultySettings Get(DifficultyLevel level)
    {
        return level switch
        {
            DifficultyLevel.Easy => easy,
            DifficultyLevel.Hard => hard,
            _ => normal,
        };
    }

    private static DifficultyData instance;

    // O asset de Resources ou, sem ele, uma cópia com os padrões (o jogo roda igual).
    public static DifficultyData Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = Resources.Load<DifficultyData>(ResourcePath);
            if (instance == null)
            {
                Debug.LogWarning($"DifficultyData não encontrado em Resources/{ResourcePath}: usando os valores padrão da classe.");
                instance = CreateInstance<DifficultyData>();
            }
            return instance;
        }
    }
}
