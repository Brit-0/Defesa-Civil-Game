using UnityEngine;

// Põe a aba no Canvas dela toda vez que ela liga (v7B-2): a aba da cena começa desligada, e com o objeto desligado
// o Unity não guarda o Override Sorting; sem isso a seta e o aro do tutorial voltavam a passar por cima dela.
public class HudTabSorting : MonoBehaviour
{
    public int order = HudTabs.TabOrder;

    private void OnEnable() => HudTabs.Raise(gameObject, order);
}
