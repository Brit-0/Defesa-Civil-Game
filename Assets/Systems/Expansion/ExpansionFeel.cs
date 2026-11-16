using System.Collections;
using DG.Tweening;
using UnityEngine;

// Resposta visual e sonora da expansão: só escuta ExpansionEvents (a compra já toca pelo GameAudio e a recusa pelo
// ActionDenied). Aviso ao entrar no modo de escolher terreno, marteladas durante a obra e o prédio novo subindo
// do chão com um estalo e o som de pronto. Fica no mesmo objeto da ExpansionManager (ExpansionBootstrap põe).
public class ExpansionFeel : MonoBehaviour
{
    [Header("SOUND")]
    [SerializeField] private float hammerInterval = 0.6f;
    [SerializeField, Range(0f, 1f)] private float hammerVolume = 0.7f;
    [SerializeField, Range(0f, 0.5f)] private float hammerPitchVariation = 0.15f;

    [Header("POP")]
    [SerializeField] private float popDuration = 0.5f;
    [SerializeField] private Vector2 popStartScale = new(0.85f, 0.1f);

    private void OnEnable()
    {
        ExpansionEvents.PlacementChanged += OnPlacementChanged;
        ExpansionEvents.BuildStarted += OnBuildStarted;
        ExpansionEvents.BuildFinished += OnBuildFinished;
        ExpansionEvents.LocationBuilt += OnLocationBuilt;
    }

    private void OnDisable()
    {
        ExpansionEvents.PlacementChanged -= OnPlacementChanged;
        ExpansionEvents.BuildStarted -= OnBuildStarted;
        ExpansionEvents.BuildFinished -= OnBuildFinished;
        ExpansionEvents.LocationBuilt -= OnLocationBuilt;
        StopAllCoroutines();
    }

    private void OnPlacementChanged(LotKind? kind)
    {
        if (kind.HasValue && AudioManager.Sfx != null) AudioManager.PlaySfx(AudioManager.Sfx.popup);
    }

    private void OnBuildStarted(BuildLot lot) => StartCoroutine(Hammer(lot));

    private IEnumerator Hammer(BuildLot lot)
    {
        var wait = new WaitForSeconds(hammerInterval);
        while (lot != null && lot.State == LotState.Building)
        {
            if (AudioManager.Sfx != null) AudioManager.PlaySfx(AudioManager.Sfx.hammer, hammerVolume, hammerPitchVariation);
            yield return wait;
        }
    }

    private void OnBuildFinished(BuildLot lot)
    {
        if (AudioManager.Sfx != null) AudioManager.PlaySfx(AudioManager.Sfx.repaired);
    }

    // Sobe do chão com um estalo (a base visível fica parada: o pivô das estruturas é embaixo).
    private void OnLocationBuilt(Location location)
    {
        if (location == null) return;
        Transform t = location.transform;
        Vector3 scale = t.localScale;
        t.localScale = new Vector3(scale.x * popStartScale.x, scale.y * popStartScale.y, scale.z);
        t.DOScale(scale, popDuration).SetEase(Ease.OutBack).SetLink(location.gameObject);
    }
}
