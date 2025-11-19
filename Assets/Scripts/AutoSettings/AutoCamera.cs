using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AutoCamera : MonoBehaviour
{
    private Camera cam;
    private int lastW, lastH;

    [SerializeField] private Vector2 res = new Vector2(1080, 1920);
    [SerializeField] private float baseSize = 12f;
    [SerializeField] private float minSize = 12f;

    public static float SizeDelta { private set; get; } = 0f;
    public static Vector2 RefResolution { private set; get; }
    public static float RefAspect { private set; get; }
    public static float OrthoSize { private set; get; }
    public static Rect WorldRect { private set; get; }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            RefResolution = res;
            RefAspect = res.x / res.y;
        }
    }
#endif

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        RefResolution = res;
        RefAspect = res.x / res.y;

        Apply(true);
    }

    private void Update()
    {
        Apply(false);
    }

    private void Apply(bool _force)
    {
        int cw = Screen.width;
        int ch = Screen.height;
        if (!_force && cw == lastW && ch == lastH) return;
        lastW = cw; lastH = ch;
        if (ch == 0) return;

        float currentAspect = (float)lastW / lastH;
        float size = baseSize * (RefAspect / currentAspect);
        size = Mathf.Max(size, minSize);
        cam.orthographicSize = size;

        OrthoSize = size;
        float worldH = size * 2f;
        float worldW = worldH * cam.aspect;
        Vector3 pos = cam.transform.position;
        WorldRect = new Rect(pos.x - worldW * 0.5f, pos.y - worldH * 0.5f, worldW, worldH);

        float delta = size - baseSize;
        if (Mathf.Abs(delta - SizeDelta) > 1e-5f)
        {
            SizeDelta = delta;
            EntityManager.Instance?.SetEntity();
        }
    }
}
