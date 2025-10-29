using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AutoCamera : MonoBehaviour
{
    private Camera cam;
    private int lw, lh;

    [SerializeField] private Vector2 res = new Vector2(1080, 1920);
    [SerializeField] private float baseSize = 12f;
    [SerializeField] private float minSize = 12f;

    public static float SizeDelta { private set; get; } = 0f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        Apply(true);
    }

    private void Update()
    {
        if (Screen.width != lw || Screen.height != lh) Apply(false);
    }

    private void Apply(bool _force)
    {
        int cw = Screen.width;
        int ch = Screen.height;

        if (!_force && cw == lw && ch == lh) return;
        lw = cw; lh = ch;
        if (ch == 0) return;

        float currentAspect = (float)lw / lh;
        float refAspect = res.x / res.y;

        float size = baseSize * (refAspect / currentAspect);
        size = Mathf.Max(size, minSize);
        cam.orthographicSize = size;

        float delta = size - baseSize;
        if (Mathf.Abs(delta - SizeDelta) > 1e-5f)
        {
            SizeDelta = delta;
            EntityManager.Instance.SetEntity();
            HandleManager.Instance.SetReady(EntityManager.Instance.GetSpawnPos());
        }
    }
}
