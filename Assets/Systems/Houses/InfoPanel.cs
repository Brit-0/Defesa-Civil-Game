using DG.Tweening;
using TMPro;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLbl;
    [SerializeField] private TMP_Text descriptionLbl;
    [SerializeField] private TMP_Text specificLbl;


    public void Set(LocationData data)
    {
        nameLbl.text = data.name;
        descriptionLbl.text = data.description;
        specificLbl.text = data.specific;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
