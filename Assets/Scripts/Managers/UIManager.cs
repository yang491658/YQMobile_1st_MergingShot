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
    public static UIManager Instance { private set; get; }

    public event System.Action<bool> OnOpenUI;

    [Header("InGame UI")]
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private TextMeshProUGUI playTimeText;
    private float playTime = 0f;
    [SerializeField] private TextMeshProUGUI scoreNum;
	[SerializeField] private Image nextImage;

	[Header("InGame UI / Timer")]
	[SerializeField] private Slider timerSlider;
	[SerializeField] private Image timerImage;
	[SerializeField] private TextMeshProUGUI timerTitle;
	[Space]
	[SerializeField] private float shakeSpeed = 50f;
	[SerializeField] private float shakeAmount = 8f;
	[SerializeField] private Vector2 textSize = new Vector2(0f, 120f);
	private Vector2 timerPos0;
	private int lastCountSfx = -1;

	[Header("Setting UI")]
    [SerializeField] private GameObject settingUI;
    [SerializeField] private TextMeshProUGUI settingScoreNum;

    [Header("Sound UI")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Image bgmIcon;
    [SerializeField] private Image sfxIcon;
    [SerializeField] private List<Sprite> bgmIcons = new List<Sprite>();
    [SerializeField] private List<Sprite> sfxIcons = new List<Sprite>();

    [Header("Confirm UI")]
    [SerializeField] private GameObject confirmUI;
    [SerializeField] private TextMeshProUGUI confirmTitle;
    private System.Action confirmAction;

	[Header("Help UI")]
	[SerializeField] private GameObject helpUI;
	[SerializeField] private List<PlanetSlot> helpPlanets = new List<PlanetSlot>();

	[Header("Result UI")]
    [SerializeField] private GameObject resultUI;
    [SerializeField] private TextMeshProUGUI resultScoreNum;

    [Header("Detail UI")]
    [SerializeField] private GameObject detailUI;
    [SerializeField] private TextMeshProUGUI detailScoreNum;
    [SerializeField] private List<PlanetSlot> detailPlanets = new List<PlanetSlot>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (inGameUI == null)
            inGameUI = GameObject.Find("InGameUI");
        if (playTimeText == null)
            playTimeText = GameObject.Find("InGameUI/Score/PlayTimeText")?.GetComponent<TextMeshProUGUI>();
        if (scoreNum == null)
            scoreNum = GameObject.Find("InGameUI/Score/ScoreNum")?.GetComponent<TextMeshProUGUI>();
		if (nextImage == null)
			nextImage = GameObject.Find("InGameUI/Next/NextImage").GetComponent<Image>();

		if (timerSlider == null)
			timerSlider = GameObject.Find("InGameUI/Timer").GetComponentInChildren<Slider>();
		if (timerImage == null)
			timerImage = GameObject.Find("InGameUI/Timer").GetComponentInChildren<Image>();
		if (timerTitle == null)
			timerTitle = GameObject.Find("InGameUI/Timer").GetComponentInChildren<TextMeshProUGUI>();

		if (settingUI == null)
            settingUI = GameObject.Find("SettingUI");
        if (settingScoreNum == null)
            settingScoreNum = GameObject.Find("SettingUI/Box/Score/ScoreNum")?.GetComponent<TextMeshProUGUI>();

        if (bgmSlider == null)
            bgmSlider = GameObject.Find("BGM/BgmSlider")?.GetComponent<Slider>();
        if (sfxSlider == null)
            sfxSlider = GameObject.Find("SFX/SfxSlider")?.GetComponent<Slider>();
        if (bgmIcon == null)
            bgmIcon = GameObject.Find("BGM/BgmBtn/BgmIcon")?.GetComponent<Image>();
        if (sfxIcon == null)
            sfxIcon = GameObject.Find("SFX/SfxBtn/SfxIcon")?.GetComponent<Image>();

        bgmIcons.Clear();
        LoadSprite(bgmIcons, "White Music");
        LoadSprite(bgmIcons, "White Music Off");
        sfxIcons.Clear();
        LoadSprite(sfxIcons, "White Sound On");
        LoadSprite(sfxIcons, "White Sound Icon");
        LoadSprite(sfxIcons, "White Sound Off 2");

        if (confirmUI == null)
            confirmUI = GameObject.Find("ConfirmUI");
        if (confirmTitle == null)
            confirmTitle = GameObject.Find("ConfirmUI/Box/ConfirmTitle")?.GetComponent<TextMeshProUGUI>();

		if (helpUI == null)
			helpUI = GameObject.Find("HelpUI");
		if (helpPlanets == null || helpPlanets.Count == 0)
			foreach (Transform child in GameObject.Find("HelpUI/Planets").transform)
				helpPlanets.Add(new PlanetSlot(child.gameObject));

		if (resultUI == null)
            resultUI = GameObject.Find("ResultUI");
        if (resultScoreNum == null)
            resultScoreNum = GameObject.Find("ResultUI/Score/ScoreNum")?.GetComponent<TextMeshProUGUI>();

        if (detailUI == null)
            detailUI = GameObject.Find("DetailUI");
        if (detailScoreNum == null)
            detailScoreNum = GameObject.Find("DetailUI/Score/ScoreNum").GetComponent<TextMeshProUGUI>();
        if (detailPlanets == null || detailPlanets.Count == 0)
            foreach (Transform child in GameObject.Find("DetailUI/Planets").transform)
                detailPlanets.Add(new PlanetSlot(child.gameObject));
    }

    private void LoadSprite(List<Sprite> _list, string _sprite)
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
        UpdateScore(GameManager.Instance.GetScore());
		UpdateNext(EntityManager.Instance?.GetNextSR());

		timerPos0 = ((RectTransform)timerSlider.transform).anchoredPosition;
	}

    private void Update()
    {
        if (GameManager.Instance.IsGameOver) return;

        playTime += Time.unscaledDeltaTime;
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

    #region 오픈
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

        inGameUI.SetActive(!_on);
        settingUI.SetActive(_on);
        OnOpenUI?.Invoke(_on);
    }

    public void OpenConfirm(bool _on, string _text = null, System.Action _action = null, bool _pass = false)
    {
        if (confirmUI == null) return;

        if (!_pass)
        {
            confirmUI.SetActive(_on);
            if (_on)
            {
                confirmTitle.text = $"{_text}하시겠습니까?";
                confirmAction = _action;
            }
        }

        if (!_on)
        {
            confirmTitle.text = string.Empty;
            confirmAction = null;
        }

        if (_pass) _action?.Invoke();
	}

	public void OpenHelp(bool _on)
	{
		if (helpUI == null) return;

		helpUI.SetActive(_on);
		for (int i = 0; i < helpPlanets.Count; i++)
		{
			var u = EntityManager.Instance?.GetDatas()[i];

			helpPlanets[i].go.name = u.Name;
			helpPlanets[i].image.sprite = u.Image;
			helpPlanets[i].text.text = u.Name;
		}
		OnOpenUI?.Invoke(_on);
	}

	public void OpenResult(bool _on)
    {
        if (resultUI == null) return;

        inGameUI.SetActive(!_on);
        resultUI.SetActive(_on);
        OnOpenUI?.Invoke(_on);
    }

    public void OpenDetail(bool _on)
    {
        if (detailUI == null) return;

        detailUI.SetActive(_on);
        for (int i = 0; i < detailPlanets.Count; i++)
        {
            var u = EntityManager.Instance?.GetDatas()[i];

            detailPlanets[i].go.name = u.Name;
            detailPlanets[i].image.sprite = u.Image;
            detailPlanets[i].text.text = EntityManager.Instance?.GetCount(u.ID).ToString("×00");
        }
    }
    #endregion

    #region 업데이트
    public void ResetUI() => playTime = 0;

    public void UpdatePlayTime()
    {
        int total = Mathf.FloorToInt(playTime);
        string s = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        playTimeText.text = s;
    }

    public void UpdateScore(int _score)
    {
        string s = _score.ToString("0000");
        scoreNum.text = s;
        settingScoreNum.text = s;
        resultScoreNum.text = s;
    }

	private void UpdateNext(Sprite _sprite) => nextImage.sprite = _sprite;

	private void UpdateTimer(float _timer, float _max)
	{
		bool isReady = HandleManager.Instance?.GetReady() != null;
		bool showSlider = isReady && _max >= 3f;
		timerSlider.gameObject.SetActive(showSlider);
		if (!showSlider)
		{
			timerTitle.gameObject.SetActive(false);
			((RectTransform)timerSlider.transform).anchoredPosition = timerPos0;
			timerImage.color = Color.white;
			timerTitle.color = Color.white;
			lastCountSfx = -1;
			return;
		}

		float remain = Mathf.Clamp(_max - _timer, 0f, _max);
		timerSlider.value = Mathf.Clamp01(remain / _max);

		var rt = (RectTransform)timerSlider.transform;

		if (remain > 3f || remain <= 0f)
		{
			timerTitle.gameObject.SetActive(false);
			rt.anchoredPosition = timerPos0;
			timerImage.color = Color.white;
			timerTitle.color = Color.white;
			lastCountSfx = -1;
			return;
		}

		int current = Mathf.Clamp(Mathf.CeilToInt(remain), 1, 3);
		if (current != lastCountSfx)
		{
			SoundManager.Instance?.PlaySFX("Count");
			lastCountSfx = current;
		}

		bool showText = _max >= 8f;
		timerTitle.gameObject.SetActive(showText);
		if (showText)
		{
			timerTitle.text = current.ToString();
			float frac = remain - Mathf.Floor(remain);
			float size = Mathf.Sin(frac * Mathf.PI) * textSize.y;
			timerTitle.fontSize = size;
		}

		float t = Mathf.InverseLerp(3f, 0f, remain);
		Color color = Color.Lerp(Color.white, Color.red, t);
		timerImage.color = color;
		if (showText) timerTitle.color = color;

		float intensity = 1f - Mathf.Clamp01(remain / 3f);
		float amp = shakeAmount * intensity * intensity;
		float sx = Mathf.Sign(Mathf.Sin(Time.unscaledTime * shakeSpeed));
		float sy = Mathf.Sign(Mathf.Cos(Time.unscaledTime * shakeSpeed));
		rt.anchoredPosition = timerPos0 + new Vector2(sx, sy) * amp;
	}

	public void UpdateVolume(SoundType _type, float _volume)
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
        UpdateSoundIcon();
    }

    public void UpdateSoundIcon()
    {
        if (bgmIcons.Count >= 2)
            bgmIcon.sprite = SoundManager.Instance.IsBGMMuted() ? bgmIcons[1] : bgmIcons[0];

        if (sfxIcons.Count >= 3)
        {
            if (SoundManager.Instance.IsSFXMuted())
                sfxIcon.sprite = sfxIcons[2];
            else if (SoundManager.Instance?.GetSFXVolume() < 0.2f)
                sfxIcon.sprite = sfxIcons[1];
            else
                sfxIcon.sprite = sfxIcons[0];
        }
    }
    #endregion

    #region 버튼
    public void OnClickSetting() => OpenSetting(true);

    public void OnClickClose() => OpenUI(false);
    public void OnClickBGM() => SoundManager.Instance?.ToggleBGM();
    public void OnClickSFX() => SoundManager.Instance?.ToggleSFX();

    public void OnClickReplay() => OpenConfirm(true, "다시", GameManager.Instance.Replay);
    public void OnClickQuit() => OpenConfirm(true, "종료", GameManager.Instance.Quit);

    public void OnClickOkay()
    {
        var action = confirmAction;
        OpenConfirm(false);
        action?.Invoke();
    }
    public void OnClickCancel() => OpenConfirm(false);

    public void OnClickReplayDirect() => OpenConfirm(true, "다시", GameManager.Instance.Replay, true);
    public void OnClickQuitDirect() => OpenConfirm(true, "종료", GameManager.Instance.Quit, true);

	public void OnClickHelp() => OpenHelp(true);

    public void OnClickDetail() => OpenDetail(true);
    public void OnClickBack() => OpenDetail(false);
    #endregion

    #region SET
    public void SetMargin(float _margin)
    {
        var ig = inGameUI.GetComponent<RectTransform>();
        ig.offsetMax = new Vector3(ig.offsetMax.x, -_margin);

        var de = detailUI.GetComponent<RectTransform>();
        de.offsetMax = new Vector3(de.offsetMax.x, -_margin);
    }
    #endregion

    #region GET
    public bool GetOnSetting() => settingUI.activeSelf;
    public bool GetOnConfirm() => confirmUI.activeSelf;
    public bool GetOnHelp() => helpUI.activeSelf;
    public bool GetOnResult() => resultUI.activeSelf;
    public bool GetOnDetail() => detailUI.activeSelf;
    #endregion
}
