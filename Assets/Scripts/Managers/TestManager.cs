#if UNITY_EDITOR
using System.Collections;
using UnityEngine;

public class TestManager : MonoBehaviour
{
    public static TestManager Instance { private set; get; }

    [Header("Game Test")]
    [SerializeField] private int testCount = 1;
    [SerializeField][Min(1f)] private float autoDelay = 1f;
    private bool isAuto = false;
    private Coroutine autoRoutine;

    [Header("Sound Test")]
    [SerializeField] private bool bgmPause = false;

    [Header("Spawn Test")]
    [SerializeField][Range(0f, 45f)] float angleRange = 30f;
    [SerializeField][Range(0f, 20f)] float shotPower = 15f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        #region 게임 테스트
        if (Input.GetKeyDown(KeyCode.P))
            GameManager.Instance?.Pause(!GameManager.Instance.IsPaused);
        if (Input.GetKeyDown(KeyCode.G))
            GameManager.Instance?.GameOver();

        if (Input.GetKeyDown(KeyCode.O))
        {
            isAuto = !isAuto;

            AutoPlay();
            GameManager.Instance?.Replay();
        }
        if (isAuto)
            if (GameManager.Instance.IsGameOver && autoRoutine == null)
                autoRoutine = StartCoroutine(AutoReplay());

        if (Input.GetKeyDown(KeyCode.R))
            GameManager.Instance?.Replay();
        if (Input.GetKeyDown(KeyCode.Q))
            GameManager.Instance?.Quit();
        #endregion

        #region 사운드 테스트
        if (Input.GetKeyDown(KeyCode.B))
        {
            bgmPause = !bgmPause;
            SoundManager.Instance?.PauseSound(bgmPause);
        }
        if (Input.GetKeyDown(KeyCode.M))
            SoundManager.Instance?.ToggleBGM();
        if (Input.GetKeyDown(KeyCode.N))
            SoundManager.Instance?.ToggleSFX();
        #endregion

        #region 엔티티 테스트
        for (int i = 1; i <= 10; i++)
        {
            KeyCode key = (i == 10) ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha0 + i);
            if (Input.GetKey(key))
            {
                UnitSystem unit = EntityManager.Instance?.Spawn(i);
                unit.Shoot(Vector2.up * shotPower);
                break;
            }
        }

        if (Input.GetKey(KeyCode.W))
        {
            UnitSystem unit = HandleManager.Instance?.GetReady();
            float angle = Random.Range(-angleRange, angleRange);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.up;
            if (unit != null)
            {
                unit.Shoot(dir * shotPower);
                EntityManager.Instance?.Respawn();
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
            EntityManager.Instance?.Spawn();

        if (Input.GetKeyDown(KeyCode.D))
        {
            GameManager.Instance?.ResetScore();
            EntityManager.Instance?.ResetCount();
            EntityManager.Instance?.DespawnAll();
        }
        #endregion

        #region UI 테스트
        if (Input.GetKeyDown(KeyCode.Z))
            UIManager.Instance?.OpenSetting(!UIManager.Instance.GetOnSetting());
        if (Input.GetKeyDown(KeyCode.X))
            UIManager.Instance?.OpenConfirm(!UIManager.Instance.GetOnConfirm());
        if (Input.GetKeyDown(KeyCode.C))
            UIManager.Instance?.OpenHelp(!UIManager.Instance.GetOnHelp());
        if (Input.GetKeyDown(KeyCode.V))
            UIManager.Instance?.OpenResult(!UIManager.Instance.GetOnResult());
        if (Input.GetKeyDown(KeyCode.B))
            UIManager.Instance?.OpenDetail(!UIManager.Instance.GetOnDetail());
        #endregion
    }

    private void AutoPlay()
    {
        if (!isAuto)
        {
            HandleManager.Instance?.SetTimeLimit(0.01f);
            isAuto= true;
        }
        else
        {
            HandleManager.Instance?.SetTimeLimit(10f);
            isAuto= false;
        }
    }

    private IEnumerator AutoReplay()
    {
        if (EntityManager.Instance?.GetCount(EntityManager.Instance.GetFinal()) > 0)
            yield return null;

        yield return new WaitForSecondsRealtime(autoDelay);
        if (GameManager.Instance.IsGameOver)
        {
            testCount++;
            GameManager.Instance?.Replay();
        }
        autoRoutine = null;
    }
}
#endif
