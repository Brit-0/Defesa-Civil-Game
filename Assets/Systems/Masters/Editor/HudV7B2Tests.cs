using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Testes de editor dos consertos de UI v7B-2 (CONSERTOS_V7B2.md): abas acima dos efeitos do mapa (HudTabs), margens do
// balão do tutorial, barra da meta com o amarelo da altura do trilho, Sair na pausa (fora do WebGL) e os textos da
// Automação e da Coleta de Impostos (ex-Repasse da Prefeitura). Monta a UI num Canvas solto, sem cena.
public class HudV7B2Tests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null) Object.DestroyImmediate(root);
    }

    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);

    // Componente de UI montado num Canvas solto, chamando o Build privado (no modo Edit o Awake não roda)
    private T Built<T>() where T : MonoBehaviour
    {
        root = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
        var holder = new GameObject(typeof(T).Name, typeof(RectTransform));
        holder.transform.SetParent(root.transform, false);
        T component = holder.AddComponent<T>();
        typeof(T).GetMethod("Build", Private).Invoke(component, null);
        return component;
    }

    private static RectTransform Find(Component owner, string objectName)
    {
        foreach (RectTransform rect in owner.GetComponentsInChildren<RectTransform>(true))
            if (rect.name == objectName) return rect;
        return null;
    }

    [Test]
    public void Tabs_AreAboveMapEffects_AndBelowGearPauseAndSettings()
    {
        Assert.Greater(HudTabs.MapEffectOrder, 100, "seta numa casa acima da camada de feedback do HUD");
        Assert.Less(HudTabs.MapEffectOrder, HudTabs.TabOrder, "seta e aro numa casa ficam por baixo das abas");
        Assert.Less(HudTabs.TabOrder, HudTabs.TutorialOrder, "balão do tutorial por cima das outras abas, como antes");
        Assert.Less(HudTabs.TutorialOrder, HudTabs.PointerOrder, "seta num botão do HUD por cima do balão e das abas");
        Assert.AreEqual(HudTabs.PointerOrder, HudTabs.PointerOrderFor(true));
        Assert.AreEqual(HudTabs.MapEffectOrder, HudTabs.PointerOrderFor(false));

        var pause = new GameObject("Pause").AddComponent<PauseMenu>();
        root = pause.gameObject;
        Assert.Greater(HudTabs.FeedbackOrder, HudTabs.TutorialOrder, "confete da compra e Sem dinheiro! por cima da aba clicada");
        Assert.Less(HudTabs.FeedbackOrder, 14000, "engrenagem das configurações (14000) segue por cima");
        Assert.Less(HudTabs.PointerOrder, 14000);
        Assert.Less(HudTabs.PointerOrder, Field<int>(pause, "sortingOrder"), "a pausa cobre as abas");
        CollectionAssert.AreEquivalent(new[] { "UpgradeTab", "GuardTab" }, HudTabs.SceneTabs);
    }

    [Test]
    public void Raise_GivesTheTabItsOwnSortedCanvas_AndKeepsItClickable()
    {
        root = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
        var tab = new GameObject("Tab", typeof(RectTransform));
        tab.transform.SetParent(root.transform, false);

        Canvas canvas = HudTabs.Raise(tab, HudTabs.TabOrder);
        Assert.IsTrue(canvas.overrideSorting);
        Assert.AreEqual(HudTabs.TabOrder, canvas.sortingOrder);
        Assert.IsNotNull(tab.GetComponent<GraphicRaycaster>(), "sem raycaster os botões da aba param de responder");

        Assert.AreSame(canvas, HudTabs.Raise(tab, HudTabs.PointerOrder), "chamar de novo só troca a ordem");
        Assert.AreEqual(HudTabs.PointerOrder, canvas.sortingOrder);

        var effect = new GameObject("Pointer", typeof(RectTransform));
        effect.transform.SetParent(root.transform, false);
        HudTabs.Raise(effect, HudTabs.MapEffectOrder, false);
        Assert.IsNull(effect.GetComponent<GraphicRaycaster>(), "seta e aro não pegam clique");
    }

    [Test]
    public void TutorialBubble_HasRoomAboveTheStep_AndBelowTheSkipButton()
    {
        var view = new GameObject("View").AddComponent<TutorialView>();
        root = view.gameObject;
        Vector2 bubble = Field<Vector2>(view, "bubbleSize");
        float stepTop = Field<float>(view, "stepTop");
        float skipBottom = Field<float>(view, "skipBottom");
        float textSize = Field<float>(view, "textSize");
        float textPaddingY = Field<float>(view, "textPaddingY");
        float border = Field<float>(view, "border");

        Assert.GreaterOrEqual(stepTop - border, 24f, "PASSO 1/15 longe da moldura de cima (era 15 da moldura)");
        Assert.GreaterOrEqual(skipBottom - border, 20f, "PULAR TUTORIAL longe da moldura de baixo (era 7)");
        float skipTop = skipBottom + 40f;
        float okBottom = bubble.y * .5f + 22f - 32f;
        Assert.GreaterOrEqual(okBottom - skipTop, 8f, "PULAR não encosta no ENTENDI");
        Assert.GreaterOrEqual(bubble.y - stepTop - textSize - textPaddingY, 100f, "o texto do passo não perde altura (era 100)");
    }

    [Test]
    public void GoalBar_FillHasTheSameHeightAsTheTrack()
    {
        GoalTracker goal = Built<GoalTracker>();
        RectTransform back = Find(goal, "BarBack");
        RectTransform track = Find(goal, "Track");
        RectTransform fill = Find(goal, "Fill");
        Assert.IsNotNull(back);
        Assert.IsNotNull(track);
        Assert.AreSame(track, fill.parent, "o amarelo enche o trilho, não o retângulo de fora");

        Assert.IsNull(back.GetComponent<Image>().sprite, "moldura lisa: o painel arredondado desenhava mais baixo que o retângulo");
        Assert.IsNull(track.GetComponent<Image>().sprite);
        Assert.IsNull(fill.GetComponent<Image>().sprite);

        fill.anchorMax = new Vector2(.5f, 1f);
        float edge = Field<float>(goal, "barEdge");
        Assert.AreEqual(back.rect.height - edge * 2f, track.rect.height, .01f);
        Assert.AreEqual(track.rect.height, fill.rect.height, .01f, "barra perfeita: amarelo da altura do trilho");
        Assert.AreEqual(track.rect.width * .5f, fill.rect.width, .01f);
        Assert.AreEqual(track.rect.yMin, fill.rect.yMin, .01f);
    }

    [Test]
    public void PauseMenu_HasQuit_OutsideTheBrowser()
    {
        Assert.IsFalse(PauseMenu.QuitAvailable(RuntimePlatform.WebGLPlayer), "no navegador o Quit não fecha nada");
        Assert.IsTrue(PauseMenu.QuitAvailable(RuntimePlatform.WindowsPlayer));
        Assert.IsTrue(PauseMenu.QuitAvailable(RuntimePlatform.OSXPlayer));
        Assert.IsTrue(PauseMenu.QuitAvailable(RuntimePlatform.WindowsEditor));

        PauseMenu pause = Built<PauseMenu>();
        RectTransform quit = Find(pause, "QuitBtn");
        Assert.IsNotNull(quit, "Sair na pausa");
        Assert.AreEqual("SAIR", quit.GetComponentInChildren<TMP_Text>(true).text);

        // As quatro opções empilhadas sem se encostar, dentro da janela e acima da dica do Esc
        string[] order = { "ResumeBtn", "SettingsBtn", "RestartBtn", "QuitBtn" };
        RectTransform panel = (RectTransform)quit.parent;
        for (int i = 1; i < order.Length; i++)
        {
            RectTransform above = Find(pause, order[i - 1]), below = Find(pause, order[i]);
            Assert.Greater(above.anchoredPosition.y - above.rect.height * .5f, below.anchoredPosition.y + below.rect.height * .5f, order[i]);
        }
        RectTransform hint = Find(pause, "Hint");
        float quitBottom = quit.anchoredPosition.y - quit.rect.height * .5f;
        float hintTop = -panel.rect.height * .5f + hint.anchoredPosition.y + hint.rect.height * .5f;
        Assert.Greater(quitBottom, hintTop, "Sair acima da dica do Esc");
    }

    [Test]
    public void UpgradeTexts_AreClear_NotLonger_AndIncomeIsCollectTaxes()
    {
        var automation = AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/Resources/Upgrades/Automation.asset");
        var income = AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/Resources/Upgrades/Income.asset");

        Assert.AreEqual("Coleta de Impostos", income.displayName);
        StringAssert.Contains("impostos", income.description);
        StringAssert.DoesNotContain("epasse", income.description);
        Assert.LessOrEqual(income.description.Length, 117, "não mais longo que o texto antigo");

        foreach (string level in new[] { "Nível 1", "Nível 2", "Nível 3" }) StringAssert.Contains(level, automation.description);
        Assert.LessOrEqual(automation.description.Length, 128, "não mais longo que o texto antigo");

        var feel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Systems/Feel/Feel.prefab");
        var popups = feel.GetComponentInChildren<MoneyPopups>(true);
        StringAssert.Contains("Impostos", Field<string>(popups, "incomeFormat"), "o número que sobe também troca de nome");

        foreach (string doc in Directory.GetFiles("docs/jam", "*.md"))
        {
            string text = File.ReadAllText(doc);
            StringAssert.DoesNotContain("Repasse da Prefeitura", text, doc);
            StringAssert.DoesNotContain("Repasse de Prefeitura", text, doc);
        }
    }
}
