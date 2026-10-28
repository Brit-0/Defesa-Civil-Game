using UnityEngine;

public class CursorController : MonoBehaviour
{
    public static CursorController main;

    [SerializeField] Texture2D cursorNormal;
    [SerializeField] Texture2D cursorHover;

    private void Awake()
    {
        main = this;
    }

    public static void Reset()
    {
        Cursor.SetCursor(main.cursorNormal, Vector2.zero, CursorMode.Auto);
    }

    public static void SetHover()
    {
        Cursor.SetCursor(main.cursorHover, Vector2.zero, CursorMode.Auto);
    }
}
