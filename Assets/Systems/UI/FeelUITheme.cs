using UnityEngine;

//Os sprites de UI que os painéis do Agente C usam, para ficarem com a cara do resto do jogo.
//Desde 16/11 (pedido do Pietro) a UI é toda em pixel art: o kit de Assets/Arts/UI/Pixel, gerado por
//Tools/PixelUI/pixel_kit.py com uma paleta fixa de 21 cores. Os sprites antigos (RoundRectangle, Circle,
//Button e Button2) ficam como reserva: desligando usePixelArt tudo volta a ser como antes.
//O asset mora em Assets/Systems/UI/Resources, porque Assets/Arts/UI não é pasta Resources;
//o menu Tools/Jam/Create Feel Prefabs cria o asset que falta e preenche os campos vazios.
[CreateAssetMenu(fileName = "FeelUITheme", menuName = "Scriptable Objects/FeelUITheme")]
public class FeelUITheme : ScriptableObject
{
    public const string ResourcePath = "FeelUITheme";
    //O referencePixelsPerUnit dos CanvasScaler da cena
    private const float CanvasReferencePixelsPerUnit = 100f;

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

    [Header("PIXEL ART (Assets/Arts/UI/Pixel)")]
    public bool usePixelArt = true;
    //Unidades do Canvas (tela de 1920x1080) por pixel do sprite: 3 deixa o pixel inteiro e quadrado em 1080p
    public float pixelScale = 3f;
    //Veste em runtime o HUD que mora na cena (aba de melhorias, guarda, dinheiro e botões), sem editar a cena
    public bool skinSceneHud = true;

    [Header("PIXEL: PANELS (Sliced)")]
    public Sprite window;
    public Sprite inset;
    public Sprite goldFrame;
    //Em cinza, para tingir pela cor do Image (tipo do desastre, vitória e derrota, barra)
    public Sprite tintFrame;
    public Sprite ribbon;
    public Sprite barFrame;
    public Sprite barFill;

    [Header("PIXEL: BUTTONS (Sliced; máscara branca para clarão e brilho)")]
    public Sprite buttonNormal;
    public Sprite buttonHover;
    public Sprite buttonPressed;
    public Sprite buttonDisabled;
    public Sprite buttonMask;
    public Sprite primaryNormal;
    public Sprite primaryHover;
    public Sprite primaryPressed;

    [Header("PIXEL: ROUND BUTTONS (Simple)")]
    public Sprite roundNormal;
    public Sprite roundHover;
    public Sprite roundPressed;
    public Sprite roundMask;

    [Header("PIXEL: DISCS")]
    public Sprite disc;
    public Sprite dotOn;
    public Sprite dotOff;
    //Aro dourado em volta da casa da dica
    public Sprite ring;
    //Aro fino (Mk_Ring, 32x32, traço de 3 px) do tutorial e das dicas: sempre Simple e quadrado, nunca Sliced (#531)
    public Sprite thinRing;

    [Header("PIXEL: ICONS (16x16; emblema 32x32)")]
    public Sprite iconCoin;
    public Sprite iconChaos;
    public Sprite iconEarthquake;
    public Sprite iconStorm;
    public Sprite iconFire;
    public Sprite iconHouse;
    public Sprite iconClock;
    public Sprite iconStar;
    public Sprite iconPause;
    public Sprite iconClose;
    public Sprite iconArrowUp;
    public Sprite iconArrowDown;
    public Sprite emblem;

    [Header("PIXEL: CURSOR (32x32, pixel dobrado)")]
    public Texture2D cursorNormal;
    public Texture2D cursorHover;
    public Texture2D cursorPressed;
    public Vector2 cursorHotspot = new Vector2(2f, 2f);
    public Vector2 cursorHandHotspot = new Vector2(13f, 2f);

    //Ícones 64x64 do Pixel Lab (Aseprite) para o que o script não desenha. O nome do arquivo é a chave:
    //Assets/Arts/UI/Pixel/Icons/<chave>.png. A lista, os prompts e a paleta estão em docs/jam/ICONES_FALTANDO.md.
    //O PixelIconImporter (Editor) importa com Filter Point e preenche esta lista sozinho quando o PNG chega.
    public const string LabIconsFolder = "Assets/Arts/UI/Pixel/Icons/";
    //Chaves que o código já procura (upgrade_ + o id do UpgradeData)
    public static readonly string[] LabIconKeys =
    {
        "upgrade_workers", "upgrade_speed", "upgrade_tools", "upgrade_cart",
        "upgrade_reward", "upgrade_automation", "upgrade_income", "upgrade_goal"
    };

    [System.Serializable]
    public struct LabIcon
    {
        public string key;
        public Sprite sprite;
    }

    [Header("PIXEL LAB ICONS (64x64, Assets/Arts/UI/Pixel/Icons)")]
    public LabIcon[] labIcons = new LabIcon[0];
    //Lado do ícone do Pixel Lab na tela de 1080p, em unidades do Canvas
    public float labIconSize = 64f;

    //Arte v2 dos chamados, do selo de concluído e do realce da Base e do Armazém (docs/jam/ICONES_V2.md).
    //64x64: mostrar a 1:1 (64 px numa tela de 1080) ou 2x; halos 64x32 a 3x no pé do prédio.
    public const string CallIconsFolder = "Assets/Arts/UI/Chamados/";

    [Header("CHAMADOS V2 (64x64, Assets/Arts/UI/Chamados)")]
    public Sprite callRepair;
    public Sprite callFire;
    public Sprite callStorm;
    [Tooltip("Pino do mapa do quarteirão alagado (casa na água, #524); o balão do chamado usa o BlockDisasterData.callFloodIcon")]
    public Sprite callFloodPin;
    public Sprite callPower;
    public Sprite callSupply;
    [Tooltip("Selo inteiro (disco verde com check): sozinho, cor branca")]
    public Sprite callDone;
    [Tooltip("Só o check, por cima de um disco verde que já exista")]
    public Sprite callDoneCheck;
    public Sprite landmarkBase;
    public Sprite landmarkDepot;
    public Sprite highlightArrow;
    public Sprite[] haloBase = new Sprite[0];
    public Sprite[] haloDepot = new Sprite[0];

    //O ícone do Pixel Lab com essa chave, ou null enquanto o PNG não existe
    public Sprite LabIconFor(string key)
    {
        if (labIcons == null || string.IsNullOrEmpty(key)) return null;

        foreach (LabIcon icon in labIcons)
        {
            if (icon.sprite != null && icon.key == key) return icon.sprite;
        }

        return null;
    }

    //Pixel art ligada e o kit importado
    public bool Pixel => usePixelArt && window != null;

    //pixelsPerUnitMultiplier que faz 1 pixel do sprite virar pixelScale unidades do Canvas
    public float MultiplierFor(Sprite sprite)
    {
        if (sprite == null || pixelScale <= 0f) return 1f;
        return CanvasReferencePixelsPerUnit / (sprite.pixelsPerUnit * pixelScale);
    }

    //Ícone de reserva por tipo de desastre, quando o DisasterData não traz o próprio
    public Sprite IconFor(DisasterType type)
    {
        switch (type)
        {
            case DisasterType.Storm: return iconStorm;
            case DisasterType.Fire: return iconFire;
            default: return iconEarthquake;
        }
    }

    //Branco com o formato do sprite, para clarão e brilho por cima (o sprite pixel já vem colorido)
    public Sprite SilhouetteOf(Sprite sprite)
    {
        if (sprite == null) return null;
        if (sprite == buttonNormal || sprite == buttonHover || sprite == buttonDisabled
            || sprite == primaryNormal || sprite == primaryHover) return buttonMask != null ? buttonMask : sprite;
        if (sprite == roundNormal || sprite == roundHover) return roundMask != null ? roundMask : sprite;
        return sprite;
    }

#if UNITY_EDITOR
    private const string SpritesFolder = "Assets/Arts/UI/";
    private const string PixelFolder = "Assets/Arts/UI/Pixel/";

    private void Reset()
    {
        AutoFillSprites();
    }

    //Liga cada campo ao sprite de Assets/Arts/UI e de Assets/Arts/UI/Pixel
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

        FillPixel(ref window, "Px_Window");
        FillPixel(ref inset, "Px_Inset");
        FillPixel(ref goldFrame, "Px_GoldFrame");
        FillPixel(ref tintFrame, "Px_TintFrame");
        FillPixel(ref ribbon, "Px_Ribbon");
        FillPixel(ref barFrame, "Px_BarFrame");
        FillPixel(ref barFill, "Px_BarFill");
        FillPixel(ref buttonNormal, "Px_Button");
        FillPixel(ref buttonHover, "Px_ButtonHover");
        FillPixel(ref buttonPressed, "Px_ButtonPressed");
        FillPixel(ref buttonDisabled, "Px_ButtonDisabled");
        FillPixel(ref buttonMask, "Px_ButtonMask");
        FillPixel(ref primaryNormal, "Px_Primary");
        FillPixel(ref primaryHover, "Px_PrimaryHover");
        FillPixel(ref primaryPressed, "Px_PrimaryPressed");
        FillPixel(ref roundNormal, "Px_Round");
        FillPixel(ref roundHover, "Px_RoundHover");
        FillPixel(ref roundPressed, "Px_RoundPressed");
        FillPixel(ref roundMask, "Px_RoundMask");
        FillPixel(ref disc, "Px_Disc");
        FillPixel(ref dotOn, "Px_DotOn");
        FillPixel(ref dotOff, "Px_DotOff");
        FillPixel(ref ring, "Px_Ring");
        if (thinRing == null) thinRing = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(SpritesFolder + "Markers/Mk_Ring.png");
        FillPixel(ref iconCoin, "Px_IconCoin");
        FillPixel(ref iconChaos, "Px_IconChaos");
        FillPixel(ref iconEarthquake, "Px_IconQuake");
        FillPixel(ref iconStorm, "Px_IconStorm");
        FillPixel(ref iconFire, "Px_IconFire");
        FillPixel(ref iconHouse, "Px_IconHouse");
        FillPixel(ref iconClock, "Px_IconClock");
        FillPixel(ref iconStar, "Px_IconStar");
        FillPixel(ref iconPause, "Px_IconPause");
        FillPixel(ref iconClose, "Px_IconClose");
        FillPixel(ref iconArrowUp, "Px_IconArrowUp");
        FillPixel(ref iconArrowDown, "Px_IconArrowDown");
        FillPixel(ref emblem, "Px_Emblem");
        FillCursor(ref cursorNormal, "Px_CursorArrow");
        FillCursor(ref cursorHover, "Px_CursorHand");
        FillCursor(ref cursorPressed, "Px_CursorPress");
        FillLabIcons();
        FillCallIcons();

        UnityEditor.EditorUtility.SetDirty(this);
    }

    //Liga os PNGs de Assets/Arts/UI/Chamados (só campo vazio, como os outros)
    [ContextMenu("Preencher ícones de chamado v2")]
    public void FillCallIcons()
    {
        FillCall(ref callRepair, "call_repair");
        FillCall(ref callFire, "call_fire");
        FillCall(ref callStorm, "call_storm");
        FillCall(ref callFloodPin, "call_flood_pin");
        FillCall(ref callPower, "call_power");
        FillCall(ref callSupply, "call_supply");
        FillCall(ref callDone, "call_done");
        FillCall(ref callDoneCheck, "call_done_check");
        FillCall(ref landmarkBase, "landmark_base");
        FillCall(ref landmarkDepot, "landmark_depot");
        FillCall(ref highlightArrow, "highlight_arrow");
        FillFrames(ref haloBase, "highlight_halo_base_");
        FillFrames(ref haloDepot, "highlight_halo_depot_");
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static void FillCall(ref Sprite field, string fileName)
    {
        if (field != null) return;

        field = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CallIconsFolder + fileName + ".png");
        if (field == null) Debug.LogWarning("FeelUITheme: não achei " + CallIconsFolder + fileName + ".png");
    }

    //Quadros _0, _1, ... até o primeiro que faltar
    private static void FillFrames(ref Sprite[] field, string prefix)
    {
        if (field != null && field.Length > 0) return;

        var frames = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; ; i++)
        {
            Sprite frame = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CallIconsFolder + prefix + i + ".png");
            if (frame == null) break;
            frames.Add(frame);
        }
        field = frames.ToArray();
    }

    //Um item por PNG de Assets/Arts/UI/Pixel/Icons, com o nome do arquivo como chave. Devolve se mudou algo.
    public bool FillLabIcons()
    {
        var found = new System.Collections.Generic.List<LabIcon>();
        string folder = LabIconsFolder.TrimEnd('/');

        if (UnityEditor.AssetDatabase.IsValidFolder(folder))
        {
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) found.Add(new LabIcon { key = System.IO.Path.GetFileNameWithoutExtension(path), sprite = sprite });
            }
        }

        found.Sort((a, b) => string.CompareOrdinal(a.key, b.key));

        bool changed = labIcons == null || labIcons.Length != found.Count;
        for (int i = 0; !changed && i < found.Count; i++)
        {
            changed = labIcons[i].key != found[i].key || labIcons[i].sprite != found[i].sprite;
        }

        if (!changed) return false;

        labIcons = found.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
        return true;
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

    private static void FillPixel(ref Sprite field, string fileName)
    {
        if (field != null) return;

        field = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(PixelFolder + fileName + ".png");
        if (field == null) Debug.LogWarning("FeelUITheme: não achei " + PixelFolder + fileName + ".png");
    }

    private static void FillCursor(ref Texture2D field, string fileName)
    {
        if (field != null) return;

        field = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(PixelFolder + fileName + ".png");
        if (field == null) Debug.LogWarning("FeelUITheme: não achei " + PixelFolder + fileName + ".png");
    }
#endif
}
