using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Banco de sons da v5 (docs/jam/SFX_V5.md): cada nome tem variações, categoria do mix, volume, variação de tom,
// limite de vozes e intervalo mínimo. Montado por Tools > Audio > Montar SoundBank v5; os números se ajustam aqui no
// Inspector (o montador só troca os clipes e não mexe no que já foi ajustado).
[CreateAssetMenu(menuName = "Defesa Civil/Sound Bank", fileName = "SoundBank")]
public class SoundBank : ScriptableObject
{
    public const string ResourcePath = "Audio/SoundBank";

    [Header("MIXER (grupos Musica, SFX, Ambiente e UI)")]
    public AudioMixer mixer;

    [Header("SONS")]
    public List<SoundEntry> entries = new List<SoundEntry>();

    [Header("CLIPES ANTIGOS (quem ainda toca o clipe velho ganha o som novo)")]
    public List<LegacySound> legacy = new List<LegacySound>();

    private Dictionary<string, SoundEntry> byId;
    private Dictionary<AudioClip, LegacySound> byClip;

    public SoundEntry Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (byId == null || byId.Count != entries.Count) BuildIndex();
        return byId.TryGetValue(id, out SoundEntry entry) ? entry : null;
    }

    public bool TryLegacy(AudioClip clip, out LegacySound link)
    {
        link = null;
        if (clip == null) return false;

        if (byClip == null) BuildIndex();
        return byClip.TryGetValue(clip, out link) && Find(link.id) != null;
    }

    private void BuildIndex()
    {
        byId = new Dictionary<string, SoundEntry>();
        foreach (SoundEntry entry in entries)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.id)) byId[entry.id] = entry;
        }

        byClip = new Dictionary<AudioClip, LegacySound>();
        foreach (LegacySound link in legacy)
        {
            if (link != null && link.clip != null) byClip[link.clip] = link;
        }
    }

    private void OnValidate()
    {
        byId = null;
        byClip = null;
    }
}

[System.Serializable]
public class SoundEntry
{
    public string id;
    public SoundCategory category = SoundCategory.Gameplay;
    public AudioClip[] clips = new AudioClip[0];

    [Header("VOLUME E TOM")]
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Variação de volume para baixo, em dB (cada toque sorteia entre 0 e este valor)")]
    [Range(0f, 6f)] public float volumeJitterDb = 1.5f;
    [Tooltip("Variação de tom, em semitons para cima e para baixo")]
    [Range(0f, 4f)] public float pitchJitter = .6f;

    [Header("VOZES")]
    [Tooltip("Quantas cópias deste som tocam ao mesmo tempo; a mais velha cede a vez")]
    [Range(1, 8)] public int maxVoices = 3;
    [Tooltip("Intervalo mínimo entre dois toques, em segundos (vários pedidos no mesmo instante viram um só)")]
    public float cooldown = .05f;

    [Header("COMPORTAMENTO")]
    public bool loop;
    [Tooltip("Abaixa a música e o ambiente enquanto toca (sons grandes)")]
    public bool ducks;
    [Tooltip("Escada de tom: cada toque logo depois do outro sobe estes semitons (moedas); 0 desliga")]
    [Range(0f, 3f)] public float ladderStep;
    [Range(1, 24)] public int ladderMax = 12;
    [Tooltip("Sem toque por este tempo, a escada volta ao primeiro degrau")]
    public float ladderReset = .6f;
}

[System.Serializable]
public class LegacySound
{
    public AudioClip clip;
    public string id;
    [Range(0f, 2f)] public float volume = 1f;
}
