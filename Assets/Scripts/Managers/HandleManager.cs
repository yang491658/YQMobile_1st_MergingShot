using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class HandleManager : MonoBehaviour
{
    public static HandleManager Instance { private set; get; }

    private Camera cam => Camera.main;
    private LayerMask layer => LayerMask.GetMask("Unit");

    [Header("Drag")]
    [SerializeField][Min(0f)] private float maxDrag = 5f;
    private const float drag = 0.15f;
    private bool canDrag;
    private bool isDragging;
    private Vector3 dragStart;
    private Vector3 dragCurrent;
    private bool isOverUI;

    [Header("Unit")]
    [SerializeField] private UnitSystem ready;
    [SerializeField] private UnitSystem hovered;
    [SerializeField] private UnitSystem selected;

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
    [SerializeField] private float powerCoef = 3f;
    private float launchTimer = 0f;
    [SerializeField][Min(0.01f)] private float timeLimit = 10f;
    [SerializeField][Range(0f, 90f)] private float angleLimit = 45f;
    public event System.Action<float, float> OnChangeTimer;

#if UNITY_EDITOR
    [Header("Mark")]
    [SerializeField] private float markDuration = 1f;
    [SerializeField] private float markRadius = 0.5f;
    [SerializeField] private int markSegment = 24;
    private readonly List<Vector3> marks = new();
    private readonly List<float> markTimes = new();
    private readonly List<Color> markColors = new();
    private readonly List<Vector3> dragPath = new();
#endif

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (dotPrefab == null)
            dotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AimDot.prefab");

        if (line == null) line = GameObject.Find("AimLine").GetComponent<LineRenderer>();
        if (ring == null) ring = GameObject.Find("AimRing").GetComponent<LineRenderer>();
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
            launchTimer += Time.deltaTime;
            OnChangeTimer?.Invoke(launchTimer, timeLimit);
            if (launchTimer >= timeLimit) AutoFire();
        }

#if UNITY_EDITOR
        HandleMouse();
        DrawDebug();
#else
        HandleTouch();
#endif
    }

#if UNITY_EDITOR
    private void HandleMouse()
    {
        HoverOn(Input.mousePosition);

        if (Input.GetMouseButtonDown(0)) HandleBegin(Input.mousePosition);
        else if (Input.GetMouseButton(0)) HandleMove(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0)) HandleEnd(Input.mousePosition);

        if (Input.GetMouseButton(1)) OnRightClick(ScreenToWorld(Input.mousePosition));
        if (Input.GetMouseButtonDown(2)) OnMiddleClick(ScreenToWorld(Input.mousePosition));
    }
#endif

    private void HandleTouch()
    {
        if (Input.touchCount == 0) return;
        Touch t = Input.GetTouch(0);

        if (t.phase == TouchPhase.Began && !IsOverUI(t.fingerId))
            HandleBegin(t.position, t.fingerId);
        else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            HandleMove(t.position);
        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            HandleEnd(t.position);
    }

    #region 판정
    private bool IsOverUI(int _fingerID = -1)
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(_fingerID);

    private Vector3 ScreenToWorld(Vector3 _screenPos)
    {
        var p = _screenPos;
        p.z = Mathf.Max(-cam.transform.position.z, cam.nearClipPlane);
        return cam.ScreenToWorldPoint(p);
    }

    private bool CanSelect(Collider2D _col)
    {
        if (layer == 0) return true;

        UnitSystem _unit = null;
        bool hasUnit = _col != null && _col.TryGetComponent(out _unit);
        bool isReady = hasUnit && (_unit == ready);

        var rb = isReady ? _unit.GetRB() : null;
        bool isIdle = rb != null && rb.linearVelocity.sqrMagnitude <= 0.01f;

        return isReady && isIdle;
    }
    #endregion

    #region 구분
    private void HandleBegin(Vector3 _pos, int _fingerID = -1)
    {
        if (IsOverUI(_fingerID))
        {
            isOverUI = true;
            return;
        }
        else isOverUI = false;

        Vector3 worldPos = ScreenToWorld(_pos);
        Collider2D hit = Physics2D.OverlapPoint(worldPos, layer);

        if (CanSelect(hit))
        {
            canDrag = true;
            isDragging = false;
            dragStart = worldPos;
            dragCurrent = dragStart;
#if UNITY_EDITOR
            dragPath.Clear();
            dragPath.Add(dragStart);
#endif
        }
        else
        {
            canDrag = false;
            isDragging = false;
#if UNITY_EDITOR
            dragPath.Clear();
#endif
        }
    }

    private void HandleMove(Vector3 _pos)
    {
        if (isOverUI) return;
        if (!canDrag) return;

        Vector3 worldPos = ScreenToWorld(_pos);
        float distance = Vector3.Distance(dragStart, worldPos);

        if (!isDragging && distance >= drag)
        {
            isDragging = true;
            OnDragBegin(dragStart);
        }

        if (isDragging)
        {
            dragCurrent = ClampDrag(dragStart, worldPos);
            OnDragMove(dragStart, dragCurrent);
#if UNITY_EDITOR
            dragPath.Add(dragCurrent);
#endif
        }
    }

    private void HandleEnd(Vector3 _pos)
    {
        if (isOverUI)
        {
            isOverUI = false;
            canDrag = false;
            isDragging = false;
#if UNITY_EDITOR
            dragPath.Clear();
#endif
            return;
        }

        if (!canDrag)
        {
            canDrag = false;
            isDragging = false;
#if UNITY_EDITOR
            dragPath.Clear();
#endif
            return;
        }

        Vector3 worldPos = ScreenToWorld(_pos);

        if (isDragging)
        {
            worldPos = ClampDrag(dragStart, worldPos);
            float distance = Vector3.Distance(dragStart, worldPos);
            if (distance >= drag)
            {
                isDragging = false;
                OnDragEnd(dragStart, worldPos);
#if UNITY_EDITOR
                dragPath.Clear();
#endif
                return;
            }
        }

        canDrag = false;
        isDragging = false;
#if UNITY_EDITOR
        dragPath.Clear();
#endif
    }

    private Vector3 ClampDrag(Vector3 _start, Vector3 _current)
    {
        if (maxDrag <= 0f) return _current;
        Vector3 delta = _current - _start;
        return _start + Vector3.ClampMagnitude(delta, maxDrag);
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
        float dist = Mathf.Min(dirRaw.magnitude, maxDrag);
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
        float dist = maxDrag;
        Vector2 endWorld = startWorld - dir * dist;
        Vector2 endScreen = cam.WorldToScreenPoint(endWorld);

        HandleBegin(startScreen);
        HandleMove(endScreen);
        HandleEnd(endScreen);
    }
    #endregion

    #region 동작
    private void OnDragBegin(Vector3 _pos)
    {
        ShowAim(true);

        Collider2D col = Physics2D.OverlapPoint(_pos, layer);
        if (col != null && col.TryGetComponent(out UnitSystem _unit))
            selected = _unit;
    }

    private void OnDragMove(Vector3 _start, Vector3 _current)
    {
        UpdateAim(_current);
    }

    private void OnDragEnd(Vector3 _start, Vector3 _end)
    {
        Vector2 dragVec = (Vector2)(_end - _start);
        Vector2 shotDir = -dragVec;

        float dist = Mathf.Min(shotDir.magnitude, maxDrag);
        float angle = Vector2.SignedAngle(Vector2.up, shotDir);

        if (dist > Mathf.Epsilon && shotDir.y > 0f && selected != null && !selected.isFired)
        {
            float clamped = Mathf.Clamp(angle, -angleLimit, angleLimit);
            Vector2 dirClamped = (Vector2)(Quaternion.Euler(0f, 0f, clamped) * Vector2.up);
            Vector2 impulse = dirClamped.normalized * dist * powerCoef;

            selected.Shoot(impulse);
            selected = null;

            EntityManager.Instance?.Respawn();

            launchTimer = 0f;
            OnChangeTimer?.Invoke(launchTimer, timeLimit);
        }

        ShowAim(false);
    }

#if UNITY_EDITOR
    private void OnRightClick(Vector3 _pos)
    {
        AddClick(_pos, Color.yellow);

        if (IsOverUI()) return;

        Collider2D col = Physics2D.OverlapPoint(_pos, layer);

        UnitSystem target = null;
        if (col != null && col.TryGetComponent(out UnitSystem _unit))
            target = _unit;

        if (target == null) return;

        var rb = target.GetRB();
        if (rb != null)
        {
            rb.position = _pos;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        else target.transform.position = _pos;
    }

    private void OnMiddleClick(Vector3 _pos)
    {
        AddClick(_pos, Color.red);

        if (IsOverUI()) return;

        Collider2D col = Physics2D.OverlapPoint(_pos, layer);

        if (col != null && col.TryGetComponent(out UnitSystem _unit))
            EntityManager.Instance?.Despawn(_unit);
    }

    private void AddClick(Vector3 _pos, Color _color)
    {
        marks.Add(_pos);
        markTimes.Add(Time.time + markDuration);
        markColors.Add(_color);
    }

    private void DrawDebug()
    {
        for (int i = markTimes.Count - 1; i >= 0; i--)
        {
            if (Time.time > markTimes[i])
            {
                int last = markTimes.Count - 1;
                (markTimes[i], markTimes[last]) = (markTimes[last], markTimes[i]);
                (marks[i], marks[last]) = (marks[last], marks[i]);
                (markColors[i], markColors[last]) = (markColors[last], markColors[i]);
                markTimes.RemoveAt(last);
                marks.RemoveAt(last);
                markColors.RemoveAt(last);
                continue;
            }

            Vector3 center = marks[i];
            Color c = markColors[i];
            for (int s = 0; s < markSegment; s++)
            {
                float a0 = (Mathf.PI * 2f) * s / markSegment;
                float a1 = (Mathf.PI * 2f) * (s + 1) / markSegment;
                Vector3 p0 = center + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * markRadius;
                Vector3 p1 = center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * markRadius;
                Debug.DrawLine(p0, p1, c);
            }
        }

        if (isDragging)
        {
            Debug.DrawLine(dragStart, dragCurrent, Color.green);

            for (int i = 1; i < dragPath.Count; i++)
                Debug.DrawLine(dragPath[i - 1], dragPath[i], Color.magenta);
        }
    }

    private void HoverOn(Vector2 _pos)
    {
        if (IsOverUI()) return;

        Vector2 world = ScreenToWorld(_pos);
        Collider2D col = Physics2D.OverlapPoint(world, layer);

        if (col != null && col.TryGetComponent(out UnitSystem unit))
        {
            if (unit == hovered) return;
            ClearHover();

            hovered = unit;
            var sr = hovered.GetSR();
            if (sr != null) sr.color = Color.blue;
        }
        else ClearHover();
    }

    private void ClearHover()
    {
        if (hovered == null) return;

        var sr = hovered.GetSR();
        if (sr != null) sr.color = Color.white;
        hovered = null;
    }
#endif
    #endregion

    #region SET
    public void SetReady(UnitSystem _unit = null)
    {
        if (_unit != null)
        { ready = _unit; launchTimer = 0f; }
        else
        {
            var list = EntityManager.Instance?.GetUnits();
            ready = null;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var u = list[i];
                if (u != null && !u.isFired) { ready = u; break; }
            }

            if (ready != null) launchTimer = 0f;
        }
        OnChangeTimer?.Invoke(launchTimer, timeLimit);
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
}
