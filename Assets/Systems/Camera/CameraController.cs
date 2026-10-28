using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController main;
    
    [SerializeField] private SpriteRenderer limiter;
    private float minX, maxX, minY, maxY;
    private Vector3 dragOrigin;
    private Camera cam;
    private Vector3 difference;

    private static bool isAnimating;

    [SerializeField] private float zoom, zoomMult, minZoom, maxZoom, velocity = 0f, smoothTime = .25f;

    void Awake()
    {
        main = this;
    }

    private void Start()
    {
        cam = Camera.main;
        GetBounds();
        cam.orthographicSize = 10;
    }

    private void Update()
    {
        Zoom();
        PanCamera();
    }

    private void PanCamera()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame) 
            dragOrigin = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
         
        if (Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed)
        {
            difference = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position; 
            cam.transform.position = ClampCamera(dragOrigin - difference); 
        }
    }

    public static void Focus(Vector3 targetPos)
    {
        isAnimating = true;
        
        main.zoom = 3f;
        main.cam.transform.DOMove(targetPos, 1f).SetEase(Ease.OutQuart);
        main.cam.DOOrthoSize(3, 1f).SetEase(Ease.OutQuart).OnComplete(() =>
        {
            isAnimating = false;
        });
    }

    private Vector3 ClampCamera(Vector3 targetPos)
    {
        float camHeight = cam.orthographicSize;
        float camWidth = cam.orthographicSize * cam.aspect;

        float minX = this.minX + camWidth;
        float maxX = this.maxX - camWidth;
        float minY = this.minY + camHeight;
        float maxY = this.maxY - camHeight;

        float newX = Mathf.Clamp(targetPos.x, minX, maxX);
        float newY = Mathf.Clamp(targetPos.y, minY, maxY);

        return new Vector3(newX, newY, targetPos.z);
    }

    public void Zoom()
    {
        if (isAnimating) return;
        
        float scroll = Mouse.current.scroll.ReadValue().y;
        zoom -= scroll * zoomMult;
        zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, zoom, ref velocity, smoothTime);
    }

    /*public void IncreaseBounds(Direction direction)
    {
        Vector3 dir = RoomManager.GetVectorFromDirection(direction);

        if (direction == Direction.Right || direction == Direction.Left)
        {
            limiter.transform.localScale += Vector3.right * 5;
            limiter.transform.position += dir * 3;
        }
        else
        {
            limiter.transform.localScale += Vector3.up * 3;
            limiter.transform.position += dir * 1.5f;
        }

        GetBounds();
    }*/

    private void GetBounds()
    {
        minX = limiter.transform.position.x - limiter.bounds.size.x / 2f;
        maxX = limiter.transform.position.x + limiter.bounds.size.x / 2f;

        minY = limiter.transform.position.y - limiter.bounds.size.y / 2f;
        maxY = limiter.transform.position.y + limiter.bounds.size.y / 2f;
    }
}
