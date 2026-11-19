using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

// Nome do jogo (CONSERTOS_V7C): Please Fix the City no Player Settings, no título do menu e no cartão do tutorial,
// e o título do menu cabe entre os dois emblemas, também na tela 16:10 do itch.io (960x600).
public class GameNameTests
{
    private const string Name = "Please Fix the City";
    private const string MenuPrefab = "Assets/Systems/UI/MainMenu.prefab";

    [Test]
    public void ProductNameIsTheGameName()
    {
        Assert.AreEqual(Name, PlayerSettings.productName);
    }

    [Test]
    public void MenuAndTutorialShowTheNewName()
    {
        SerializedObject menu = MenuSettings();
        string title = menu.FindProperty("title").stringValue;
        StringAssert.Contains(Name.ToUpperInvariant(), title);
        StringAssert.DoesNotContain("DEFESA CIVIL", title);

        GameObject go = new GameObject("GameNameTests");
        try
        {
            SerializedObject tutorial = new SerializedObject(go.AddComponent<Tutorial>());
            Assert.AreEqual(Name.ToUpperInvariant(), tutorial.FindProperty("cardTitle").stringValue);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void MenuTitleFitsBetweenTheEmblems()
    {
        FeelUITheme px = FeelUI.Pixel;
        if (FeelUI.Font == null || px == null || px.emblem == null) Assert.Ignore("sem a fonte ou o kit pixel");

        SerializedObject menu = MenuSettings();
        float titleSize = menu.FindProperty("titleSize").floatValue;
        float offset = menu.FindProperty("emblemOffset").floatValue;
        float emblemHalf = px.emblem.rect.width * FeelUI.PixelSize * menu.FindProperty("emblemScale").floatValue * .5f;
        float bandMargin = menu.FindProperty("bandMargin").floatValue;

        GameObject go = new GameObject("GameNameTests");
        try
        {
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.font = FeelUI.Font;
            label.fontSize = titleSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            float titleHalf = label.GetPreferredValues(Name.ToUpperInvariant(), 0f, 0f).x * .5f;

            //Folga de 40 px entre o título e o emblema; a faixa na 16:10 tem 1080 * 1,6 de largura menos as margens
            Assert.LessOrEqual(titleHalf + 40f, offset - emblemHalf, "título encosta no emblema");
            float bandHalf16x10 = 1080f * 1.6f * .5f - bandMargin;
            Assert.LessOrEqual(offset + emblemHalf, bandHalf16x10, "emblema sai da faixa na tela 16:10");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static SerializedObject MenuSettings()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefab);
        Assert.IsNotNull(prefab, MenuPrefab);
        return new SerializedObject(prefab.GetComponent<MainMenu>());
    }
}
