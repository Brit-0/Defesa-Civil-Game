using UnityEngine;
using UnityEngine.InputSystem;

//Cursor do jogo: seta, mão em cima de casa e de botão (o Tweener chama SetHover e Reset) e mão apertando
//enquanto o botão esquerdo está baixo. Com o kit pixel usa os cursores do FeelUITheme; sem ele, os campos abaixo.
//Nasce sozinho na primeira cena e vive entre recargas: não precisa estar na cena.
public class CursorController : MonoBehaviour
{
    public static CursorController main;

    [SerializeField] Texture2D cursorNormal;
    [SerializeField] Texture2D cursorHover;

    private bool hover;
    private bool pressed;
    private Texture2D current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        main = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (main != null) return;

        FeelUITheme px = FeelUI.Pixel;
        if (px == null || px.cursorNormal == null) return;

        GameObject go = new GameObject("CursorController");
        DontDestroyOnLoad(go);
        go.AddComponent<CursorController>();
    }

    private void Awake()
    {
        if (main != null && main != this)
        {
            Destroy(this);
            return;
        }

        main = this;
        Apply();
    }

    private void OnDestroy()
    {
        if (main == this) main = null;
    }

    public static void Reset()
    {
        if (main == null) return;

        main.hover = false;
        main.Apply();
    }

    public static void SetHover()
    {
        if (main == null) return;

        main.hover = true;
        main.Apply();
    }

    private void Update()
    {
        bool down = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (down == pressed) return;

        pressed = down;
        Apply();
    }

    private void Apply()
    {
        FeelUITheme px = FeelUI.Pixel;
        Texture2D texture;
        Vector2 hotspot;

        if (px != null && px.cursorNormal != null)
        {
            texture = !hover ? px.cursorNormal : pressed && px.cursorPressed != null ? px.cursorPressed : px.cursorHover;
            hotspot = texture == px.cursorNormal ? px.cursorHotspot : px.cursorHandHotspot;
        }
        else
        {
            texture = hover ? cursorHover : cursorNormal;
            hotspot = Vector2.zero;
        }

        if (texture == current) return;

        current = texture;
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
    }
}
