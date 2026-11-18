using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    
    [SerializeField] private SpriteRenderer limiter;
    private Camera cam;

    private static bool isAnimating;

    [SerializeField] private float zoom, zoomMult, minZoom, maxZoom, velocity = 0f, smoothTime = .25f;

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        cam = Camera.main;
        cam.orthographicSize = 10;
    }

    private void Update()
    {
        Zoom();
    }

    public static void Focus(Vector3 targetPos)
    {
        isAnimating = true;
        
        Instance.zoom = 3f;
        Instance.cam.transform.DOMove(targetPos, 1f).SetEase(Ease.OutQuart);
        Instance.cam.DOOrthoSize(3, 1f).SetEase(Ease.OutQuart).OnComplete(() =>
        {
            isAnimating = false;
        });
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
}
