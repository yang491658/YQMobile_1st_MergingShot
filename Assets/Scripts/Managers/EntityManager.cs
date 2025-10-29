using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { private set; get; }

    [Header("Data")]
    [SerializeField] private GameObject unitBase;
    [SerializeField] private UnitData[] unitDatas;
    private readonly Dictionary<int, UnitData> dataDic = new Dictionary<int, UnitData>();

    [Header("Spawn")]
    [SerializeField] private Transform spawnPos;
    [SerializeField] private Collider2D spawnCol;
    [SerializeField] private int respawnID = 1;
    [SerializeField] private float respawnDelay = 3f;
    public event System.Action<Sprite> OnChangeNext;

    private readonly List<Collider2D> cols = new List<Collider2D>();
    private Coroutine respawnRoutine;
    private float respawnTime = 0f;

    [Header("Entity")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform hole;
    [SerializeField] private Transform units;
    [SerializeField] private List<int> unitCounts = new List<int>();
    [SerializeField] private List<UnitSystem> spawned = new List<UnitSystem>();

    [Header("Entity / Standard")]
    [SerializeField] private float holeY = 5f;
    [SerializeField] private float spawnPosY = -6f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spawnPos == null)
            spawnPos = transform.Find("SpawnPos");
        if (spawnCol == null)
            spawnCol = spawnPos.GetComponent<Collider2D>();

        if (unitBase == null)
            unitBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UnitBase.prefab");

        string[] guids = AssetDatabase.FindAssets("t:UnitData", new[] { "Assets/Scripts/ScriptableObjects" });
        var list = new List<UnitData>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var data = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (data != null) list.Add(data);
        }
        unitDatas = list.OrderBy(d => d.unitID).ThenBy(d => d.unitName).ToArray();
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

        dataDic.Clear();
        for (int i = 0; i < unitDatas.Length; i++)
        {
            var d = unitDatas[i];
            if (d != null && !dataDic.ContainsKey(d.unitID))
                dataDic.Add(d.unitID, d);
        }

        SetEntity();
        ResetCount();
    }

    #region 소환
    private UnitData FindByID(int _id) => dataDic.TryGetValue(_id, out var data) ? data : null;

    public UnitSystem Spawn(int _id = 0, Vector2? _pos = null)
    {
        UnitData data = FindByID((_id == 0) ? respawnID : _id);

        if (_id == 0)
        {
            int score = GameManager.Instance.GetTotalScore();
            int n = (score <= 100) ? 3 : (score <= 500 ? 4 : 5);

            respawnID = Random.Range(1, n + 1);
            OnChangeNext?.Invoke(FindByID(respawnID).unitImage);
        }

        if (data == null) return null;

        Vector2 pos = _pos ?? (Vector2)spawnPos.position;

        UnitSystem unit = Instantiate(unitBase, pos, Quaternion.identity, units)
            .GetComponent<UnitSystem>();

        unit.SetData(data.Clone());
        spawned.Add(unit);

        return unit;
    }

    public void Respawn()
    {
        if (respawnRoutine != null) return;
        respawnTime = Time.time + respawnDelay;
        respawnRoutine = StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        while (true)
        {
            bool allInHole = false;
            if (spawned.Count > 0)
            {
                allInHole = true;
                for (int i = 0; i < spawned.Count; i++)
                {
                    var u = spawned[i];
                    if (u == null || !u.inHole) { allInHole = false; break; }
                }
            }
            if (allInHole) break;

            bool timeReady = Time.time >= respawnTime;

            bool unitInSpawn = false;
            if (spawnCol != null)
            {
                cols.Clear();
                spawnCol.Overlap(default, cols);
                for (int i = 0; i < cols.Count; i++)
                {
                    var c = cols[i];
                    if (c != null && c.GetComponent<UnitSystem>() != null) { unitInSpawn = true; break; }
                }
            }

            if (timeReady && !unitInSpawn) break;

            yield return null;
        }
        HandleManager.Instance.SetReady(Spawn());
        respawnRoutine = null;
    }

    public void CancelRespawn()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }
        respawnTime = 0f;
    }
    #endregion

    #region 제거
    public void Despawn(UnitSystem _unit)
    {
        if (_unit == null) return;

        spawned.Remove(_unit);

        _unit.SetOut();
        Destroy(_unit.gameObject);
    }

    public void DespawnAll()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
            Despawn(spawned[i]);
    }
    #endregion

    #region 개수
    public void AddCount(UnitData _data) => unitCounts[_data.unitID - 1]++;

    public void ResetCount()
    {
        unitCounts.Clear();
        for (int i = 0; i < unitDatas.Length; i++) unitCounts.Add(0);
    }
    #endregion

    #region SET
    public void SetEntity()
    {
        if (inGame == null) inGame = GameObject.Find("InGame").transform;
        if (hole == null) hole = FindFirstObjectByType<HoleSystem>().transform;
        if (units == null) units = GameObject.Find("InGame/Units").transform;

        float d = AutoCamera.SizeDelta;

        Vector3 hp = hole.position;
        hp.y = holeY + d;
        hole.position = hp;

        Vector3 sp = spawnPos.position;
        sp.y = spawnPosY - d;
        spawnPos.position = sp;
    }
    #endregion

    #region GET
    public IReadOnlyList<UnitData> GetDatas() => unitDatas;
    public int GetFinal() => unitDatas[unitDatas.Length - 1].unitID;

    public Vector3 GetSpawnPos() => spawnPos.position;
    public Sprite GetNextSR() => FindByID(respawnID).unitImage;

    public IReadOnlyList<UnitSystem> GetUnits() => spawned;
    public int GetCount(int _id) => unitCounts[_id - 1];
    #endregion
}
