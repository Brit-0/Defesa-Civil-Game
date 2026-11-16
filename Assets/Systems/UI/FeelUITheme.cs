using UnityEngine;

//Os sprites de UI que os painéis do Agente C usam, para ficarem com a cara do resto do jogo.
//Por enquanto só RoundRectangle, Circle, Button e Button2 de Assets/Arts/UI (pedido do Pietro, 16/11).
//O asset mora em Assets/Systems/UI/Resources, porque Assets/Arts/UI não é pasta Resources;
//o menu Tools/Jam/Create Feel Prefabs cria o asset que falta e preenche os campos vazios.
[CreateAssetMenu(fileName = "FeelUITheme", menuName = "Scriptable Objects/FeelUITheme")]
public class FeelUITheme : ScriptableObject
{
    public const string ResourcePath = "FeelUITheme";

    [Header("PANELS (RoundRectangle, Sliced)")]
    public Sprite roundRectangle;
    //Tamanho do canto dos painéis: menor que 1 aumenta o canto, maior que 1 diminui
    public float pixelsPerUnitMultiplier = 1f;

    [Header("ROUND (Circle, Simple)")]
    public Sprite circle;

    [Header("ROUND BUTTONS (Button: aro fino; Button2: aro grosso, o do + e do - da cena)")]
    public Sprite button;
    public Sprite buttonFilled;
    public Sprite button2;
    public Sprite button2Filled;

#if UNITY_EDITOR
    private const string SpritesFolder = "Assets/Arts/UI/";

    private void Reset()
    {
        AutoFillSprites();
    }

    //Liga cada campo ao sprite de Assets/Arts/UI
    [ContextMenu("Preencher sprites de Assets/Arts/UI")]
    public void AutoFillSprites()
    {
        //Só preenche campo vazio: sprite trocado à mão no Inspector fica como está
        Fill(ref roundRectangle, "RoundRectangle", "RoundRectangle");
        Fill(ref circle, "Circle", "Circle");
        Fill(ref button, "Button", "Button_0");
        Fill(ref buttonFilled, "Button", "Button_1");
        Fill(ref button2, "Button2", "Button2_0");
        Fill(ref button2Filled, "Button2", "Button2_1");

        UnityEditor.EditorUtility.SetDirty(this);
    }

    //Button e Button2 são folhas com dois sprites: procura pelo nome do sprite dentro do arquivo
    private static void Fill(ref Sprite field, string fileName, string spriteName)
    {
        if (field != null) return;

        string path = SpritesFolder + fileName + ".png";

        foreach (Object asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
            {
                field = sprite;
                return;
            }
        }

        Debug.LogWarning("FeelUITheme: não achei o sprite " + spriteName + " em " + path);
    }
#endif
}
