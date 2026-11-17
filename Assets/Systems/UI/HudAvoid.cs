using UnityEngine;

//Faixas de dica presas embaixo da tela (TutorialTips e o balão do Tutorial) sobem para não cobrir o painel da equipe
//(GuardTab, canto de baixo à direita) quando ele está aberto (lista Para Arrumar, item 3).
public static class HudAvoid
{
    private static RectTransform guardTab;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        guardTab = null;
    }

    private static RectTransform GuardTab
    {
        get
        {
            if (guardTab == null && FeelUI.HudCanvas != null) guardTab = FeelUI.FindChild(FeelUI.HudCanvas.transform, "GuardTab") as RectTransform;
            return guardTab;
        }
    }

    //anchoredPosition.y para quem tem âncora embaixo: o pedido (shownY) ou o mínimo para ficar acima do GuardTab
    //aberto, se os dois se cruzam na horizontal. Não muda nada com o painel fechado.
    public static float ShownY(RectTransform rect, float shownY, float gap = 18f)
    {
        RectTransform tab = GuardTab;
        RectTransform parent = rect != null ? rect.parent as RectTransform : null;
        if (tab == null || parent == null || !tab.gameObject.activeInHierarchy) return shownY;

        Vector3[] corners = new Vector3[4];
        tab.GetWorldCorners(corners);
        Vector2 tabMin = parent.InverseTransformPoint(corners[0]);
        Vector2 tabMax = parent.InverseTransformPoint(corners[2]);

        rect.GetWorldCorners(corners);
        float left = parent.InverseTransformPoint(corners[0]).x;
        float right = parent.InverseTransformPoint(corners[2]).x;
        if (right <= tabMin.x - gap || left >= tabMax.x + gap) return shownY;

        float bottomOfParent = parent.rect.yMin;
        float needed = tabMax.y - bottomOfParent + gap + rect.rect.height * rect.pivot.y;
        return Mathf.Max(shownY, needed);
    }
}
