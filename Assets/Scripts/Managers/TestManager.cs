using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct SliderConfig
{
    public TextMeshProUGUI TMP;
    public Slider slider;
    public int value;
    public int minValue;
    public int maxValue;
    public string format;

    public SliderConfig(int _value, int _min, int _max, string _format)
    {
        TMP = null;
        slider = null;
        value = _value;
        minValue = _min;
        maxValue = _max;
        format = _format;
    }
}

public class TestManager : MonoBehaviour
{
    public static TestManager Instance { private set; get; }

    [Header("Game Test")]
    [SerializeField][Min(0)] private int testCount = 0;
    [SerializeField][Min(0)] private int maxScore = 0;
    private int totalScore = 0;
    [SerializeField][Min(0)] private int averageScore = 0;
    [SerializeField] private bool isAuto = false;
    [SerializeField][Min(1f)] private float autoReplay = 1f;
    private Coroutine autoRoutine;

    [Header("Sound Test")]
    [SerializeField] private bool bgmPause = false;

    [Header("Test UI")]
    [SerializeField] private GameObject testUI;
    [Space]
    [SerializeField] private SliderConfig gameSpeed = new SliderConfig(1, 1, 10, "배속 × {0}");
    [Space]
    [SerializeField] private TextMeshProUGUI testCountNum;
    [SerializeField] private TextMeshProUGUI maxScoreNum;
    [SerializeField] private TextMeshProUGUI averageScoreNum;
    [Space]
    [SerializeField] private SliderConfig timeLimit = new SliderConfig(10, 1, 10, "시간제한 : {0}");
    [SerializeField] private SliderConfig angleRange = new SliderConfig(30, 0, 45, "각도범위 : {0}");
    [SerializeField] private SliderConfig shotPower = new SliderConfig(15, 0, 20, "발사파워 : {0}");

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (testUI == null)
            testUI = GameObject.Find("TestUI");

        if (gameSpeed.TMP == null)
            gameSpeed.TMP = GameObject.Find("TestUI/GameSpeed/TestText")?.GetComponent<TextMeshProUGUI>();
        if (gameSpeed.slider == null)
            gameSpeed.slider = GameObject.Find("TestUI/GameSpeed/TestSlider")?.GetComponent<Slider>();

        if (testCountNum == null)
            testCountNum = GameObject.Find("TestUI/TestCount/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (maxScoreNum == null)
            maxScoreNum = GameObject.Find("TestUI/MaxScore/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (averageScoreNum == null)
            averageScoreNum = GameObject.Find("TestUI/AverageScore/TestNum")?.GetComponent<TextMeshProUGUI>();

        if (timeLimit.TMP == null)
            timeLimit.TMP = GameObject.Find("TestUI/TimeLimit/TestText")?.GetComponent<TextMeshProUGUI>();
        if (timeLimit.slider == null)
            timeLimit.slider = GameObject.Find("TestUI/TimeLimit/TestSlider")?.GetComponent<Slider>();
        if (angleRange.TMP == null)
            angleRange.TMP = GameObject.Find("TestUI/AngleRange/TestText")?.GetComponent<TextMeshProUGUI>();
        if (angleRange.slider == null)
            angleRange.slider = GameObject.Find("TestUI/AngleRange/TestSlider")?.GetComponent<Slider>();
        if (shotPower.TMP == null)
            shotPower.TMP = GameObject.Find("TestUI/ShotPower/TestText")?.GetComponent<TextMeshProUGUI>();
        if (shotPower.slider == null)
            shotPower.slider = GameObject.Find("TestUI/ShotPower/TestSlider")?.GetComponent<Slider>();
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

        testUI.SetActive(false);
    }

    private void Start()
    {
        SoundManager.Instance?.ToggleBGM();

        AutoPlay();
        UpdateTestUI();
    }

    private void Update()
    {
        #region 게임 테스트
        if (Input.GetKeyDown(KeyCode.P))
            GameManager.Instance?.Pause(!GameManager.Instance.IsPaused);
        if (Input.GetKeyDown(KeyCode.G))
            GameManager.Instance?.GameOver();
        if (Input.GetKeyDown(KeyCode.R))
            GameManager.Instance?.Replay();
        if (Input.GetKeyDown(KeyCode.Q))
            GameManager.Instance?.Quit();

        if (Input.GetKeyDown(KeyCode.O))
            AutoPlay();
        if (isAuto)
            if (GameManager.Instance.IsGameOver && autoRoutine == null)
                autoRoutine = StartCoroutine(AutoReplay());
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
            if (Input.GetKeyDown(key))
            {
                UnitSystem unit = EntityManager.Instance?.Spawn(i);
                unit.Shoot(Vector2.up * shotPower.value);
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            UnitSystem unit = HandleManager.Instance?.GetReady();
            float angle = Random.Range(-angleRange.value, angleRange.value);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.up;
            if (unit != null)
            {
                unit.Shoot(dir * shotPower.value);
                EntityManager.Instance?.Respawn();
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
            EntityManager.Instance?.Spawn();

        if (Input.GetKeyDown(KeyCode.Delete))
            EntityManager.Instance?.DespawnAll();
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
        if (Input.GetKeyDown(KeyCode.BackQuote))
            OnClickTest();
        #endregion
    }

    private void AutoPlay()
    {
        isAuto = !isAuto;
    }

    private IEnumerator AutoReplay()
    {
        if (EntityManager.Instance?.GetCount(EntityManager.Instance.GetFinal()) > 0)
            yield return null;

        yield return new WaitForSecondsRealtime(autoReplay);
        if (GameManager.Instance.IsGameOver)
        {
            int score = GameManager.Instance.GetScore();

            totalScore += score;
            maxScore = Mathf.Max(score, maxScore);
            averageScore = totalScore / ++testCount;

            UpdateTestUI();

            GameManager.Instance?.Replay();
        }
        autoRoutine = null;
    }

    #region 테스트 UI
    private void OnEnable()
    {
        InitSlider(gameSpeed, ChangeGameSpeed);
        InitSlider(timeLimit, ChangeTimeLimit);
        InitSlider(angleRange, ChangeAngleRange);
        InitSlider(shotPower, ChangeShotPower);
    }

    private void OnDisable()
    {
        gameSpeed.slider.onValueChanged.RemoveListener(ChangeGameSpeed);
        timeLimit.slider.onValueChanged.RemoveListener(ChangeTimeLimit);
        angleRange.slider.onValueChanged.RemoveListener(ChangeAngleRange);
        shotPower.slider.onValueChanged.RemoveListener(ChangeShotPower);
    }

    private void InitSlider(SliderConfig _config, UnityEngine.Events.UnityAction<float> _action)
    {
        if (_config.slider == null) return;

        _config.slider.minValue = _config.minValue;
        _config.slider.maxValue = _config.maxValue;
        _config.slider.wholeNumbers = true;

        float v = _config.value;
        if (v < _config.minValue) v = _config.minValue;
        else if (v > _config.maxValue) v = _config.maxValue;
        _config.slider.value = v;

        _action.Invoke(_config.slider.value);
        _config.slider.onValueChanged.AddListener(_action);
    }

    private int ChangeSlider(float _value, SliderConfig _config)
    {
        int v = Mathf.RoundToInt(_value);
        if (v < _config.minValue) v = _config.minValue;
        else if (v > _config.maxValue) v = _config.maxValue;
        return v;
    }

    private void ApplySlider(ref SliderConfig _config, float _value, System.Action<int> _afterAction = null)
    {
        _config.value = ChangeSlider(_value, _config);

        if (string.IsNullOrEmpty(_config.format))
            _config.TMP.text = _config.value.ToString();
        else
            _config.TMP.text = string.Format(_config.format, _config.value);

        _afterAction?.Invoke(_config.value);
    }

    private void UpdateSliderUI(SliderConfig _config)
    {
        if (string.IsNullOrEmpty(_config.format))
            _config.TMP.text = _config.value.ToString();
        else
            _config.TMP.text = string.Format(_config.format, _config.value);

        _config.slider.value = _config.value;
    }
    private void ChangeGameSpeed(float _value) => ApplySlider(ref gameSpeed, _value, v => GameManager.Instance?.SetSpeed(v));
    private void ChangeTimeLimit(float _value) => ApplySlider(ref timeLimit, _value, v => HandleManager.Instance?.SetTimeLimit(v));
    private void ChangeAngleRange(float _value) => ApplySlider(ref angleRange, _value);
    private void ChangeShotPower(float _value) => ApplySlider(ref shotPower, _value);

    private void UpdateTestUI()
    {
        testCountNum.text = testCount.ToString();
        maxScoreNum.text = maxScore.ToString();
        averageScoreNum.text = averageScore.ToString();

        UpdateSliderUI(gameSpeed);
        UpdateSliderUI(timeLimit);
        UpdateSliderUI(angleRange);
        UpdateSliderUI(shotPower);
    }

    public void OnClickTest()
    {
        testUI.SetActive(!testUI.activeSelf);
        UpdateTestUI();
    }
    public void OnClickReset()
    {
        testCount = 0;
        maxScore = 0;
        totalScore = 0;
        averageScore = 0;

        UpdateTestUI();
    }
    public void OnClickReplay() => GameManager.Instance?.Replay();
    #endregion
}
