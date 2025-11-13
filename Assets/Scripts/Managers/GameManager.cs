using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { private set; get; }

    [Header("Speed")]
    [SerializeField] private float speed = 1f;

    [Header("Score")]
    [SerializeField] private int score = 0;
    public event System.Action<int> OnChangeScore;

    public bool IsPaused { private set; get; } = false;
    public bool IsGameOver { private set; get; } = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void GameOverReact();
    [DllImport("__Internal")] private static extern void ReplayReact();
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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += LoadGame;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= LoadGame;
    }

    private void LoadGame(Scene _scene, LoadSceneMode _mode)
    {
        Pause(false);
        IsGameOver = false;
        ResetScore();

        SoundManager.Instance?.PlayBGM("Default");

        UIManager.Instance?.ResetPlayTime();
        UIManager.Instance?.OpenUI(false);

        EntityManager.Instance?.CancelRespawn();
        EntityManager.Instance?.ResetCount();
        EntityManager.Instance?.SetEntity();
        EntityManager.Instance?.Spawn(1);
    }

    #region 점수
    public void ScoreUp(int _score = 1)
    {
        score += _score;
        OnChangeScore?.Invoke(score);
    }

    public void ResetScore()
    {
        score = 0;
        OnChangeScore?.Invoke(score);
    }
    #endregion

    #region 진행
    public void Pause(bool _pause)
    {
        if (IsPaused == _pause) return;

        IsPaused = _pause;
        Time.timeScale = _pause ? 0f : speed;
    }

    private void ActWithReward(System.Action _act)
    {
        if (ADManager.Instance != null) ADManager.Instance?.ShowReward(_act);
        else _act?.Invoke();
    }

    public void Replay()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ReplayReact();
#else
        ActWithReward(ReplayGame);
#endif
    }
    private void ReplayGame()
    {
        EntityManager.Instance?.CancelRespawn();
        EntityManager.Instance?.DespawnAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit() => ActWithReward(QuitGame);
    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Pause(true);
        SoundManager.Instance?.GameOver();
        UIManager.Instance?.OpenResult(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        GameOverReact();
#endif
    }
    #endregion

    #region SET
    public void SetSpeed(float _speed)
    {
        if (!IsPaused) Time.timeScale = speed;
    }
    #endregion

    #region GET
    public float GetSpeed() => speed;
    public int GetScore() => score;
    #endregion
}
