using UnityEngine;

[ExecuteAlways]
public class AutoBackground : MonoBehaviour
{
    private Camera cam;
    private SpriteRenderer sr;

    private int lastW, lastH;
    private float lastAspect, lastOrthoSize;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (enabled) Fit();
    }
#endif

    private void Awake()
    {
        cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
        Fit();
    }

    private void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH ||
            !Mathf.Approximately(cam.aspect, lastAspect) ||
            !Mathf.Approximately(cam.orthographicSize, lastOrthoSize))
            Fit();
    }

    private void OnEnable()
    {
        Fit();
    }

    private void Fit()
    {
        if (cam == null || !cam.orthographic || sr.sprite == null) return;

        lastW = Screen.width;
        lastH = Screen.height;
        lastAspect = cam.aspect;
        lastOrthoSize = cam.orthographicSize;

        var sp = sr.sprite;
        float ppu = sp.pixelsPerUnit;
        if (ppu <= 0f) return;

        float worldH = cam.orthographicSize * 2f;
        float worldW = worldH * cam.aspect;

        float spriteW = sp.rect.width / ppu;
        float spriteH = sp.rect.height / ppu;
        if (spriteW <= 0f || spriteH <= 0f) return;

        var parent = transform.parent;
        Vector3 parentLossy = (parent != null) ? parent.lossyScale : Vector3.one;
        float invX = (parentLossy.x == 0f) ? 1f : parentLossy.x;
        float invY = (parentLossy.y == 0f) ? 1f : parentLossy.y;

        float localX = (worldW / spriteW) / invX;
        float localY = (worldH / spriteH) / invY;
        transform.localScale = new Vector3(localX, localY, transform.localScale.z);

        var b = sr.bounds;
        Vector3 camCenter = cam.transform.position;
        Vector3 delta = new Vector3(camCenter.x - b.center.x, camCenter.y - b.center.y, 0f);
        transform.position += delta;
    }
}
