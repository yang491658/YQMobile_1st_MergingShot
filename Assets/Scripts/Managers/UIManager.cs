using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct PlanetSlot
{
    public GameObject go;
    public Image image;
    public TextMeshProUGUI text;

    public PlanetSlot(GameObject obj)
    {
        go = obj;
        image = null;
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            var img = obj.transform.GetChild(i).GetComponent<Image>();
            if (img != null) { image = img; break; }
        }

        text = obj.GetComponentInChildren<TextMeshProUGUI>();
    }
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public event System.Action<bool> OnOpenUI;

    [Header("InGame UI")]
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI playTimeText;
    private float playTime = 0f;
    [SerializeField] private Image nextImage;

    [Header("InGame UI / Timer")]
    [SerializeField] private Slider timerSlider;
    [SerializeField] private Image timerImage;
    [SerializeField] private TextMeshProUGUI timerText;
    [Space]
    [SerializeField] private float shakeSpeed = 50f;
    [SerializeField] private float shakeAmount = 8f;
    [SerializeField] private Vector2 textSize = new Vector2(0f, 120f);
    private Vector2 timerPos0;
    private int lastCountSfx = -1;

    [Header("Setting UI")]
    [SerializeField] private GameObject settingUI;
    [SerializeField] private TextMeshProUGUI settingScoreText;

    [Header("Sound UI")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Image bgmIcon;
    [SerializeField] private Image sfxIcon;
    [SerializeField] private List<Sprite> bgmIcons = new List<Sprite>();
    [SerializeField] private List<Sprite> sfxIcons = new List<Sprite>();

    [Header("Confirm UI")]
    [SerializeField] private GameObject confirmUI;
    [SerializeField] private TextMeshProUGUI confirmText;
    private System.Action confirmAction;

    [Header("Help UI")]
    [SerializeField] private GameObject helpUI;
    [SerializeField] private List<PlanetSlot> helpPlanets = new List<PlanetSlot>();

    [Header("Game Over UI")]
    [SerializeField] private GameObject resultUI;
    [SerializeField] private TextMeshProUGUI resultScoreText;

    [Header("Result UI")]
    [SerializeField] private GameObject detailUI;
    [SerializeField] private TextMeshProUGUI detailScoreText;
    [SerializeField] private List<PlanetSlot> detailPlanets = new List<PlanetSlot>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (inGameUI == null)
            inGameUI = GameObject.Find("InGameUI");
        if (scoreText == null)
            scoreText = GameObject.Find("InGameUI/Score/ScoreText").GetComponent<TextMeshProUGUI>();
        if (playTimeText == null)
            playTimeText = GameObject.Find("InGameUI/Score/PlayTimeText")?.GetComponent<TextMeshProUGUI>();
        if (nextImage == null)
            nextImage = GameObject.Find("InGameUI/Next/NextImage").GetComponent<Image>();

        if (timerSlider == null)
            timerSlider = GameObject.Find("InGameUI/Timer").GetComponentInChildren<Slider>();
        if (timerImage == null)
            timerImage = GameObject.Find("InGameUI/Timer").GetComponentInChildren<Image>();
        if (timerText == null)
            timerText = GameObject.Find("InGameUI/Timer").GetComponentInChildren<TextMeshProUGUI>();

        if (settingUI == null)
            settingUI = GameObject.Find("SettingUI");
        if (settingScoreText == null)
            settingScoreText = GameObject.Find("SettingUI/Box/Score/ScoreText").GetComponent<TextMeshProUGUI>();

        if (bgmSlider == null)
            bgmSlider = GameObject.Find("BGM/BgmSlider").GetComponent<Slider>();
        if (sfxSlider == null)
            sfxSlider = GameObject.Find("SFX/SfxSlider").GetComponent<Slider>();
        if (bgmIcon == null)
            bgmIcon = GameObject.Find("BGM/BgmBtn/BgmIcon").GetComponent<Image>();
        if (sfxIcon == null)
            sfxIcon = GameObject.Find("SFX/SfxBtn/SfxIcon").GetComponent<Image>();

        bgmIcons.Clear();
        LoadSprite(bgmIcons, "White Music");
        LoadSprite(bgmIcons, "White Music Off");
        sfxIcons.Clear();
        LoadSprite(sfxIcons, "White Sound On");
        LoadSprite(sfxIcons, "White Sound Icon");
        LoadSprite(sfxIcons, "White Sound Off 2");

        if (confirmUI == null)
            confirmUI = GameObject.Find("ConfirmUI");
        if (confirmText == null)
            confirmText = GameObject.Find("ConfirmUI/Box/ConfirmText").GetComponent<TextMeshProUGUI>();

        if (helpUI == null)
            helpUI = GameObject.Find("HelpUI");
        if (helpPlanets == null || helpPlanets.Count == 0)
            foreach (Transform child in GameObject.Find("HelpUI/Planets").transform)
                helpPlanets.Add(new PlanetSlot(child.gameObject));

        if (resultUI == null)
            resultUI = GameObject.Find("ResultUI");
        if (resultScoreText == null)
            resultScoreText = GameObject.Find("ResultUI/Score/ScoreText").GetComponent<TextMeshProUGUI>();

        if (detailUI == null)
            detailUI = GameObject.Find("DetailUI");
        if (detailScoreText == null)
            detailScoreText = GameObject.Find("DetailUI/Score/ScoreText").GetComponent<TextMeshProUGUI>();
        if (detailPlanets == null || detailPlanets.Count == 0)
            foreach (Transform child in GameObject.Find("DetailUI/Planets").transform)
                detailPlanets.Add(new PlanetSlot(child.gameObject));


    }

    private static void LoadSprite(List<Sprite> _list, string _sprite)
    {
        if (string.IsNullOrEmpty(_sprite)) return;
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Imports/Dark UI/Icons" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in assets)
            {
                var s = obj as Sprite;
                if (s != null && s.name == _sprite)
                {
                    _list.Add(s);
                    return;
                }
            }
        }
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
    }

    private void Start()
    {
        UpdateScore(GameManager.Instance.GetTotalScore());
        UpdateNext(EntityManager.Instance.GetNextSR());

        timerPos0 = ((RectTransform)timerSlider.transform).anchoredPosition;
    }

    private void Update()
    {
        if (GameManager.Instance.IsPaused || GameManager.Instance.IsGameOver) return;

        playTime += Time.deltaTime;
        UpdatePlayTime();
    }

    private void OnEnable()
    {
        GameManager.Instance.OnChangeScore += UpdateScore;

        SoundManager.Instance.OnChangeVolume += UpdateVolume;
        bgmSlider.value = SoundManager.Instance.GetBGMVolume();
        bgmSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.value = SoundManager.Instance.GetSFXVolume();
        sfxSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);

        HandleManager.Instance.OnChangeTimer += UpdateTimer;

        EntityManager.Instance.OnChangeNext += UpdateNext;

        OnOpenUI += GameManager.Instance.Pause;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnChangeScore -= UpdateScore;

        SoundManager.Instance.OnChangeVolume -= UpdateVolume;
        bgmSlider.onValueChanged.RemoveListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.onValueChanged.RemoveListener(SoundManager.Instance.SetSFXVolume);

        HandleManager.Instance.OnChangeTimer -= UpdateTimer;

        EntityManager.Instance.OnChangeNext -= UpdateNext;

        OnOpenUI -= GameManager.Instance.Pause;
    }

    #region OPEN
    public void OpenUI(bool _on)
    {
        OpenDetail(_on);
        OpenResult(_on);
        OpenHelp(_on);
        OpenConfirm(_on);
        OpenSetting(_on);
    }

    public void OpenSetting(bool _on)
    {
        if (settingUI == null) return;

        OnOpenUI?.Invoke(_on);

        inGameUI.SetActive(!_on);
        settingUI.SetActive(_on);
    }

    public void OpenConfirm(bool _on, string _text = null, System.Action _action = null, bool _pass = false)
    {
        if (confirmUI == null) return;

        if (!_pass)
        {
            confirmUI.SetActive(_on);
            confirmText.text = $"{_text}하시겠습니까?";
            confirmAction = _action;
        }

        if (!_on) confirmAction = null;

        if (_pass) _action?.Invoke();
    }

    public void OpenHelp(bool _on)
    {
        if (helpUI == null) return;

        OnOpenUI?.Invoke(_on);

        helpUI.SetActive(_on);
        for (int i = 0; i < helpPlanets.Count; i++)
        {
            var u = EntityManager.Instance.GetDatas()[i];

            helpPlanets[i].go.name = u.unitName;
            helpPlanets[i].image.sprite = u.unitImage;
            helpPlanets[i].text.text = u.unitName;
        }
    }
    public void OpenResult(bool _on)
    {
        if (resultUI == null) return;

        OnOpenUI?.Invoke(_on);

        inGameUI.SetActive(!_on);
        settingUI.SetActive(!_on);
        confirmUI.SetActive(!_on);
        helpUI.SetActive(!_on);

        resultUI.SetActive(_on);
    }

    public void OpenDetail(bool _on)
    {
        if (detailUI == null) return;

        detailUI.SetActive(_on);
        for (int i = 0; i < detailPlanets.Count; i++)
        {
            var u = EntityManager.Instance.GetDatas()[i];

            detailPlanets[i].go.name = u.unitName;
            detailPlanets[i].image.sprite = u.unitImage;
            detailPlanets[i].text.text = EntityManager.Instance.GetCount(u.unitID).ToString("×00");
        }
    }
    #endregion

    #region UPDATE
    public void ResetPlayTime() => playTime = 0;

    private void UpdatePlayTime()
    {
        int total = Mathf.FloorToInt(playTime);
        string s = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        playTimeText.text = s;
    }

    public void UpdateScore(int _score)
    {
        scoreText.text = _score.ToString("0000");
        settingScoreText.text = _score.ToString("0000");
        resultScoreText.text = _score.ToString("0000");
        detailScoreText.text = _score.ToString("0000");
    }

    private void UpdateVolume(SoundType _type, float _volume)
    {
        switch (_type)
        {
            case SoundType.BGM:
                if (!Mathf.Approximately(bgmSlider.value, _volume))
                    bgmSlider.value = _volume;
                break;

            case SoundType.SFX:
                if (!Mathf.Approximately(sfxSlider.value, _volume))
                    sfxSlider.value = _volume;
                break;

            default:
                return;
        }
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (bgmIcons.Count >= 2)
            bgmIcon.sprite = SoundManager.Instance.IsBGMMuted() ? bgmIcons[1] : bgmIcons[0];

        if (sfxIcons.Count >= 3)
        {
            if (SoundManager.Instance.IsSFXMuted())
                sfxIcon.sprite = sfxIcons[2];
            else if (SoundManager.Instance.GetSFXVolume() < 0.2f)
                sfxIcon.sprite = sfxIcons[1];
            else
                sfxIcon.sprite = sfxIcons[0];
        }
    }

    private void UpdateNext(Sprite _sprite) => nextImage.sprite = _sprite;

    private void UpdateTimer(float _timer, float _max)
    {
        bool isReady = HandleManager.Instance.GetReady() != null;
        bool showSlider = isReady && _max >= 3f;
        timerSlider.gameObject.SetActive(showSlider);
        if (!showSlider)
        {
            timerText.gameObject.SetActive(false);
            ((RectTransform)timerSlider.transform).anchoredPosition = timerPos0;
            timerImage.color = Color.white;
            timerText.color = Color.white;
            lastCountSfx = -1;
            return;
        }

        float remain = Mathf.Clamp(_max - _timer, 0f, _max);
        timerSlider.value = Mathf.Clamp01(remain / _max);

        var rt = (RectTransform)timerSlider.transform;

        if (remain > 3f || remain <= 0f)
        {
            timerText.gameObject.SetActive(false);
            rt.anchoredPosition = timerPos0;
            timerImage.color = Color.white;
            timerText.color = Color.white;
            lastCountSfx = -1;
            return;
        }

        int current = Mathf.Clamp(Mathf.CeilToInt(remain), 1, 3);
        if (current != lastCountSfx)
        {
            SoundManager.Instance.Count();
            lastCountSfx = current;
        }

        bool showText = _max >= 8f;
        timerText.gameObject.SetActive(showText);
        if (showText)
        {
            timerText.text = current.ToString();
            float frac = remain - Mathf.Floor(remain);
            float size = Mathf.Sin(frac * Mathf.PI) * textSize.y;
            timerText.fontSize = size;
        }

        float t = Mathf.InverseLerp(3f, 0f, remain);
        Color color = Color.Lerp(Color.white, Color.red, t);
        timerImage.color = color;
        if (showText) timerText.color = color;

        float intensity = 1f - Mathf.Clamp01(remain / 3f);
        float amp = shakeAmount * intensity * intensity;
        float sx = Mathf.Sign(Mathf.Sin(Time.unscaledTime * shakeSpeed));
        float sy = Mathf.Sign(Mathf.Cos(Time.unscaledTime * shakeSpeed));
        rt.anchoredPosition = timerPos0 + new Vector2(sx, sy) * amp;
    }
    #endregion

    #region 버튼
    public void OnClickSetting() => OpenSetting(true);
    public void OnClickHelp() => OpenHelp(true);
    public void OnClickClose() => OpenUI(false);

    public void OnClickBGM() => SoundManager.Instance.ToggleBGM();
    public void OnClickSFX() => SoundManager.Instance.ToggleSFX();

    public void OnClickReplay() => OpenConfirm(true, "다시", GameManager.Instance.Replay);
    public void OnClickQuit() => OpenConfirm(true, "종료", GameManager.Instance.Quit);

    public void OnClickReplayByPass() => OpenConfirm(true, "다시", GameManager.Instance.Replay, true);
    public void OnClickQuitByPass() => OpenConfirm(true, "종료", GameManager.Instance.Quit, true);
    public void OnClickDetail() => OpenDetail(true);
    public void OnClickBack() => OpenDetail(false);

    public void OnClickOkay() => confirmAction?.Invoke();
    public void OnClickCancel() => OpenConfirm(false);
    #endregion

    #region SET
    public void SetInGameUI(float _margin)
    {
        var rt = inGameUI.GetComponent<RectTransform>();
        rt.offsetMax = new Vector3(rt.offsetMax.x, -_margin);
    }
    #endregion

    #region GET
    public bool GetOnSetting() => settingUI.activeSelf;
    public bool GetOnConfirm() => confirmUI.activeSelf;
    public bool GetOnResult() => resultUI.activeSelf;
    #endregion
}
