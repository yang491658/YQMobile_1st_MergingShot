using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class AutoUICanvas : MonoBehaviour
{
    [SerializeField] private Vector2 res = new Vector2(1080, 1920);
    private CanvasScaler scaler;
    private int lastW, lastH;

    private void Awake()
    {
        Init();
        Apply(true);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (scaler == null) Init();
        Apply(false);
    }

    private void Init()
    {
        scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = res;
    }

    private void Apply(bool _force)
    {
        if (scaler == null) return;

        int w = Screen.width;
        int h = Screen.height;
        if (h == 0) return;
        if (!_force && w == lastW && h == lastH) return;
        lastW = w; lastH = h;

        float current = (float)w / h;
        float refAspect = res.x / res.y;
        scaler.matchWidthOrHeight = current < refAspect ? 0f : 1f;
    }
}
