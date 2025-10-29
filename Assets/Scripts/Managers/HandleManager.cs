using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HandleManager : MonoBehaviour
{
    public static HandleManager Instance { private set; get; }

    private Camera cam => Camera.main;
    private LayerMask unitLayer => LayerMask.GetMask("Unit");
    private bool isDragging;

    [Header("Unit")]
    [SerializeField] private UnitSystem ready;
    [SerializeField] private UnitSystem hovered;
    [SerializeField] private UnitSystem selected;
    private Vector2 dragStart;

    [Header("Aim Dots")]
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private int dotCount = 12;
    [SerializeField] private float dotSpacing = 0.5f;
    private readonly List<Transform> dots = new List<Transform>();

    [Header("Aim Line & Ring")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private LineRenderer ring;
    [SerializeField] private int ringSegments = 64;
    [SerializeField] private float ringRadius = 0.5f;
    private Vector3[] ringUnit;

    [Header("Launch")]
    [SerializeField] private float maxPower = 5f;
    [SerializeField] private float powerCoef = 3f;
    private float timer = 0f;
    [SerializeField][Min(0.01f)] private float timeLimit = 10f;
    [SerializeField][Range(0f, 90f)] private float angleLimit = 45f;
    public event System.Action<float, float> OnChangeTimer;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (dotPrefab == null)
            dotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dot.prefab");

        if (line == null) line = GameObject.Find("Line").GetComponent<LineRenderer>();
        if (ring == null) ring = GameObject.Find("Ring").GetComponent<LineRenderer>();
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (dots.Count == 0)
        {
            for (int i = 0; i < dotCount; i++)
            {
                var dot = Instantiate(dotPrefab, transform);
                dot.SetActive(false);
                dots.Add(dot.transform);
            }
        }

        line.gameObject.SetActive(false);
        line.positionCount = 0;

        ring.gameObject.SetActive(false);
        ring.positionCount = 0;
        ringUnit = new Vector3[ringSegments + 1];
        for (int i = 0; i <= ringSegments; i++)
        {
            float t = (float)i / ringSegments * Mathf.PI * 2f;
            ringUnit[i] = new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.IsPaused) return;

        if (ready == null || ready.isFired)
            SetReady();
        else
        {
            timer += Time.deltaTime;
            OnChangeTimer?.Invoke(timer, timeLimit);
            if (timer >= timeLimit) AutoFire();
        }

#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    #region 클릭
#if UNITY_EDITOR
    private void HandleMouse()
    {
        HoverOn(Input.mousePosition);

        if (Input.GetMouseButtonDown(0)) DragBegin(Input.mousePosition);
        else if (Input.GetMouseButton(0)) DragMove(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0)) DragEnd(Input.mousePosition);

        if (Input.GetMouseButton(1)) MoveTo(Input.mousePosition);

        if (Input.GetMouseButtonDown(2)) RemoveAt(Input.mousePosition);

    }
#endif

    private void HandleTouch()
    {
        if (Input.touchCount == 0) return;
        Touch t = Input.GetTouch(0);

        if (t.phase == TouchPhase.Began)
            DragBegin(t.position, t.fingerId);
        else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            DragMove(t.position);
        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            DragEnd(t.position);
    }

    private bool PointerOverUI(int _fingerID = -1)
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(_fingerID);

    private Vector2 ScreenToWorld(Vector2 _screenPos) => cam.ScreenToWorldPoint(_screenPos);

    private bool CanSelect(UnitSystem _unit)
    {
        var rb = _unit.GetRB();
        return (_unit == ready) && rb != null && rb.linearVelocity.sqrMagnitude <= 0.01f;
    }
    #endregion

    #region 드래그
    private void DragBegin(Vector2 _pos, int _fingerID = -1, bool _ignoreUI = false)
    {
        if (!_ignoreUI && PointerOverUI(_fingerID)) return;

        ShowAim(false);

        Vector2 world = ScreenToWorld(_pos);
        Collider2D col = Physics2D.OverlapPoint(world, unitLayer);

        if (col != null && col.TryGetComponent(out UnitSystem unit) && CanSelect(unit))
        {
            selected = unit;
            dragStart = world;
            isDragging = true;
        }
        else
        {
            selected = null;
            isDragging = false;
        }
    }

    private void DragMove(Vector2 _pos)
    {
        if (!isDragging || selected == null) return;
        UpdateAim(ScreenToWorld(_pos));
    }

    private void DragEnd(Vector2 _pos)
    {
        if (!isDragging || selected == null)
        {
            isDragging = false;
            ShowAim(false);
            return;
        }

        Vector2 endWorld = ScreenToWorld(_pos);
        Vector2 drag = endWorld - dragStart;
        Vector2 shotDir = -drag;

        float dist = Mathf.Min(shotDir.magnitude, maxPower);
        float angle = Vector2.SignedAngle(Vector2.up, shotDir);

        if (dist > Mathf.Epsilon && shotDir.y > 0f)
        {
            float clamped = Mathf.Clamp(angle, -angleLimit, angleLimit);
            Vector2 dirClamped = (Vector2)(Quaternion.Euler(0f, 0f, clamped) * Vector2.up);
            Vector2 impulse = dirClamped.normalized * dist * powerCoef;

            selected.Shoot(impulse);
            ready = null;

            EntityManager.Instance.Respawn();

            timer = 0f;
            OnChangeTimer?.Invoke(timer, timeLimit);
        }

        isDragging = false;
        ShowAim(false);
        selected = null;
    }
    #endregion

    #region 조준
    private void ShowAim(bool _on)
    {
        for (int i = 0; i < dots.Count; i++)
            dots[i].gameObject.SetActive(_on);

        line.gameObject.SetActive(_on);
        if (!_on) line.positionCount = 0;

        ring.gameObject.SetActive(_on);
        if (!_on) ring.positionCount = 0;
    }

    private void UpdateAim(Vector3 _pos)
    {
        if (!isDragging || selected == null) return;

        var rb = selected.GetRB();
        Vector3 start = rb != null ? (Vector3)rb.worldCenterOfMass : selected.transform.position;

        Vector3 dirRaw = (start - _pos);
        float dist = Mathf.Min(dirRaw.magnitude, maxPower);
        if (dist <= Mathf.Epsilon || dirRaw.y <= 0f)
        {
            ShowAim(false);
            return;
        }

        float angle = Vector2.SignedAngle(Vector2.up, dirRaw);
        float clamped = Mathf.Clamp(angle, -angleLimit, angleLimit);
        Vector3 dir = (Quaternion.Euler(0f, 0f, clamped) * Vector3.up) * dist;
        Vector3 ringCenter = start - dir;

        Vector3 step = dir.normalized * dotSpacing;
        int visible = Mathf.Min(Mathf.FloorToInt(dist / dotSpacing), dots.Count);

        Vector3 p = start + step;
        for (int i = 0; i < dots.Count; i++)
        {
            bool on = i < visible;
            dots[i].gameObject.SetActive(on);
            if (on)
            {
                dots[i].position = p;
                p += step;
            }
        }

        line.gameObject.SetActive(true);
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, ringCenter + dir.normalized * ringRadius);

        ring.gameObject.SetActive(true);
        ring.positionCount = ringSegments + 1;
        for (int i = 0; i <= ringSegments; i++)
            ring.SetPosition(i, ringCenter + ringUnit[i] * ringRadius);
    }

    #endregion

    #region 발사
    private void AutoFire()
    {
        var rb = ready.GetRB();
        Vector2 startWorld = rb != null ? rb.worldCenterOfMass : (Vector2)ready.transform.position;
        Vector2 startScreen = cam.WorldToScreenPoint(startWorld);

        float ang = Random.Range(-angleLimit, angleLimit);
        Vector2 dir = (Vector2)(Quaternion.Euler(0f, 0f, ang) * Vector2.up);
        float dist = maxPower;
        Vector2 endWorld = startWorld - dir * dist;
        Vector2 endScreen = cam.WorldToScreenPoint(endWorld);

        DragBegin(startScreen, -1, true);
        DragMove(endScreen);
        DragEnd(endScreen);
    }
    #endregion

    #region SET
    public void SetReady(UnitSystem _unit = null)
    {
        if (_unit != null)
        {
            ready = _unit; timer = 0f;
        }
        else
        {
            var list = EntityManager.Instance.GetUnits();
            ready = null;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var u = list[i];
                if (u != null && !u.isFired) { ready = u; break; }
            }

            if (ready != null) timer = 0f;
        }

        OnChangeTimer?.Invoke(timer, timeLimit);
    }

    public void SetReady(Vector3 _pos)
    {
        if (ready == null || ready.isFired) return;

        var rb = ready.GetRB();
        if (rb != null)
        {
            Vector2 p = (Vector2)_pos;
            rb.position = p;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void SetTimeLimit(float _limit) => timeLimit = _limit;
    #endregion

    #region GET
    public UnitSystem GetReady() => ready;
    #endregion

#if UNITY_EDITOR
    private void HoverOn(Vector2 _pos)
    {
        if (PointerOverUI()) return;

        Vector2 world = ScreenToWorld(_pos);
        Collider2D col = Physics2D.OverlapPoint(world, unitLayer);

        if (col != null && col.TryGetComponent(out UnitSystem unit))
        {
            if (unit == hovered) return;
            ClearHover();

            hovered = unit;
            var sr = hovered.GetSR();
            if (sr != null) sr.color = Color.blue;
        }
        else
        {
            ClearHover();
        }
    }

    private void ClearHover()
    {
        if (hovered == null) return;

        var sr = hovered.GetSR();
        if (sr != null) sr.color = Color.white;
        hovered = null;
    }

    private void MoveTo(Vector2 _pos)
    {
        if (PointerOverUI()) return;

        Vector2 world = ScreenToWorld(_pos);
        Collider2D col = Physics2D.OverlapPoint(world, unitLayer);

        UnitSystem target = null;
        if (col != null && col.TryGetComponent(out UnitSystem unit))
            target = unit;

        if (target == null) return;

        var rb = target.GetRB();
        if (rb != null)
        {
            rb.position = world;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        else
        {
            target.transform.position = world;
        }
    }

    private void RemoveAt(Vector2 _pos)
    {
        if (PointerOverUI()) return;

        Vector2 world = ScreenToWorld(_pos);
        Collider2D col = Physics2D.OverlapPoint(world, unitLayer);

        if (col != null && col.TryGetComponent(out UnitSystem unit))
            EntityManager.Instance.Despawn(unit);
    }
#endif
}
