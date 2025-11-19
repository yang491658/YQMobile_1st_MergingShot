using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class AutoUICanvas : MonoBehaviour
{
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
        scaler.referenceResolution = AutoCamera.RefResolution;
        scaler.matchWidthOrHeight = current < AutoCamera.RefAspect ? 0f : 1f;
    }
}
