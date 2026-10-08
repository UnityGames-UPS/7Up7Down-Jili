using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;


public class UiManager : MonoBehaviour
{
  [SerializeField]
  private AudioManager audioController;
  [SerializeField]
  private SocketIOManager socketManager;
  [SerializeField]
  private JSFunctCalls jsFunctCalls;

  [Header("Cursor")]
  [SerializeField] private Texture2D cursorTexture;
  [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
  [Tooltip("Cursor size in pixels. Leave at 0 to use the texture's own size.")]
  [SerializeField] private Vector2Int cursorSize = Vector2Int.zero;

  private void Awake()
  {
    if (jsFunctCalls != null)
      jsFunctCalls.RegisterVisibilityListener(gameObject.name);

    ApplyCursor();
    SetupChipSelectorBackdrop();
  }

  private void ApplyCursor()
  {
    if (cursorTexture == null) return;

    Texture2D texture = cursorTexture;
    Vector2 hotspot = cursorHotspot;

    bool resize = cursorSize.x > 0 && cursorSize.y > 0
      && (cursorSize.x != cursorTexture.width || cursorSize.y != cursorTexture.height);
    if (resize)
    {
      texture = ResizeTexture(cursorTexture, cursorSize.x, cursorSize.y);
      // hotspot is authored in source-texture pixels
      hotspot = new Vector2(cursorHotspot.x * cursorSize.x / cursorTexture.width,
                            cursorHotspot.y * cursorSize.y / cursorTexture.height);
    }

    // ForceSoftware keeps the cursor at the texture's pixel size, ignoring the OS pointer-size setting
    Cursor.SetCursor(texture, hotspot, CursorMode.ForceSoftware);
  }

  // Cursor.SetCursor has no size argument, so the texture itself is rescaled
  private static Texture2D ResizeTexture(Texture2D source, int width, int height)
  {
    RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
    RenderTexture previous = RenderTexture.active;
    Graphics.Blit(source, rt);
    RenderTexture.active = rt;

    Texture2D resized = new Texture2D(width, height, TextureFormat.RGBA32, false);
    resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    resized.Apply();

    RenderTexture.active = previous;
    RenderTexture.ReleaseTemporary(rt);
    return resized;
  }

  public void OnFocusChanged(string value)
  {
    bool focused = value == "1";
    Debug.Log("UNITY FOCUS CHANGED: " + value + " (focused: " + focused + ")");
    audioController?.SetMuteAll(focused ? !isSound : true);
    socketManager?.HandleFocusChange(focused);
  }

  [Header("Screens UI")]
  [SerializeField] private GameObject HomeScreen_Object;
  [SerializeField] private GameObject GameScreen_Object;


  [Header("Main Buttons")]
  [SerializeField] private Button HistoryMain_button;
  [SerializeField] private Button CasualGame_button;
  [SerializeField] private Button NoviceGame_button;
  [SerializeField] private Button ExpertGame_button;
  [SerializeField] private Button HighRollerGame_button;

  [Header("info page")]
  [SerializeField] private Button MenuInGame_button;
  [SerializeField] private Button History_button;
  [SerializeField] private Button Info_button;
  [SerializeField] private Button Sound_button;
  [SerializeField] private Button Music_button;
  [SerializeField] private Button SoundMute_button;
  [SerializeField] private Button MusicMute_button;
  [SerializeField] private Button Home_button;
  [SerializeField] private Button Exit_Button;
  [SerializeField] private Button YesHome_button;
  [SerializeField] private Button NoHome_button;

  [SerializeField] private GameObject MenuPanel_Object;
  [SerializeField] private GameObject MenuPanelContainer_Object;
  [SerializeField] private GameObject Homebutton_Object;

  [SerializeField] private Button HistoryClose_button;

  [SerializeField] private Button InfoClose_button;

  [SerializeField] private Button InfoLeft_button;

  [SerializeField] private Button InfoRight_button;

  [SerializeField] private List<GameObject> InfoPages_Objects;
  [SerializeField] private List<GameObject> InfoActive_Objects;
  private int currentInfoPage = 0;

  private bool IsMenuPanelOpen = false;


  [Header("Game Rules")]
  [SerializeField] internal List<TMP_Text> PayoutText;
  [SerializeField] private GameObject LeaderBoard;
  [SerializeField] private GameObject Diamond;









  [Header("Popus UI")]
  [SerializeField]
  private GameObject MainPopup_Object;
  [SerializeField]
  private GameObject PaytablePopup_Object;
  [SerializeField] private GameObject GameQuitPopup;
  [SerializeField] private GameObject HistoryPopup_Object;
  [SerializeField] private GameObject InfoPopup_Object;



  [Header("Settings Popup")]
  [SerializeField]
  private GameObject SettingsPopup_Object;
  [SerializeField]
  private Button SettingsExit_Button;
  [SerializeField]
  private Button Sound_Button;
  [SerializeField]
  private Button Music_Button;

  [SerializeField]
  private GameObject MusicOn_Object;
  [SerializeField]
  private GameObject MusicOff_Object;
  [SerializeField]
  private GameObject SoundOn_Object;
  [SerializeField]
  private GameObject SoundOff_Object;
  [Header("RoadMap Popup")]

  [SerializeField] private GameObject RoadMapPopup;
  [SerializeField] private Button CloseRoadmap;
  [SerializeField] private Button OpenRoadMapBtn;
  [SerializeField] private Button OpenRoadMapBtn2;
  [Header("Disconnection Popup")]
  [SerializeField]
  private Button CloseDisconnect_Button;
  [SerializeField]
  private GameObject DisconnectPopup_Object;

  [Header("AnotherDevice Popup")]
  [SerializeField]
  private Button CloseAD_Button;
  [SerializeField]
  private GameObject ADPopup_Object;

  [Header("Reconnection Popup")]
  [SerializeField]
  private TMP_Text reconnect_Text;
  [SerializeField]
  private GameObject ReconnectPopup_Object;

  [Header("LowBalance Popup")]
  [SerializeField]
  private Button LBExit_Button;
  [SerializeField]
  private GameObject LBPopup_Object;
  [Header("History Popup")]
  [SerializeField]
  private GameObject Pageparent;

  [SerializeField] private GameObject HistoryPrefab;
  [SerializeField] private TMP_Text HistoryNav;
  [SerializeField] private int CurrentHistoryPage;
  [SerializeField] private int MaxHistoryPage;
  [SerializeField] private Button HistoryLeft;
  [SerializeField] private Button HistoryRight;

  [Header("Quit Popup")]
  [SerializeField]
  private GameObject ExitButton;
  [SerializeField]
  private GameObject QuitPopup_Object;
  [SerializeField]
  private Button YesQuit_Button;
  [SerializeField]
  private Button NoQuit_Button;
  [SerializeField]
  private Button CrossQuit_Button;

  [SerializeField]
  internal GameObject touchDisable;
  [SerializeField]
  private Button Settings_Button;
  [SerializeField]
  private Button Paytable_Button;
  [SerializeField]
  private Button PaytableExit_Button;
  [SerializeField]
  private Button GameExit_Button;
  [SerializeField]
  private GameManager gameManager;

  bool isExit;
  bool isMusic;
  bool isSound;

  private bool isExpanded = false;



  [Header("stats")]
  [SerializeField] private List<StatsPrefab> LineStats;
  [SerializeField] private GameObject StatsPref;
  [SerializeField] private Transform StatsParent;
  [SerializeField] private Image FillAb;
  [SerializeField] private TMP_Text TwoSixPercentage;
  [SerializeField] private TMP_Text SevenPercentage;
  [SerializeField] private TMP_Text EightTwelvePercentage;
  [SerializeField] private TMP_Text TotalRounds;
  [SerializeField] internal List<Sprite> DiceSprites;
  [SerializeField] internal List<Sprite> Resultcolor;
  // Add these fields to your class
  [SerializeField] private List<StatsList> verticalColumns;
  [SerializeField] private Transform VerticalGridParent; // Assign your new grid parent in inspector
  private List<DiceStatsPrefab> gridStats = new List<DiceStatsPrefab>();
  private const int MAX_GRID = 105;


  [Header("coins")]
  [SerializeField] internal GameObject chipPanel;
  [SerializeField] internal Chip coinSelector;
  [SerializeField] internal Button coinSelectorBtn;
  [SerializeField] internal List<Chip> Coins;
  // Full-screen button behind the fan; clicking it closes the selector
  [SerializeField] private Button chipSelectorBackdrop;
  [SerializeField] private float backdropAlpha = 0.1f;
  [SerializeField] private float backdropFadeDuration = 0.2f;
  private Image backdropImage;

  [Header("Chip Fan")]
  [SerializeField] private float fanRadius = 300f;
  // Degrees, 0 = right, 90 = up; chips spread evenly from start to end
  [SerializeField] private float fanStartAngle = 150f;
  [SerializeField] private float fanEndAngle = 30f;
  [SerializeField] private float fanOpenDuration = 0.2f;
  [SerializeField] private float fanCloseDuration = 0.15f;
  [SerializeField] private float fanStagger = 0.02f;
  [SerializeField] private Ease fanOpenEase = Ease.OutBack;
  [SerializeField] private Ease fanCloseEase = Ease.InBack;

  [Header("Chipoptions")]
  [SerializeField] internal GameObject Repeatpanel;
  [SerializeField] internal Button Repeatbtn;
  [SerializeField] internal GameObject chiOptionpanel;
  [SerializeField] internal Button Undubtn;
  [SerializeField] internal Button Canclebtn;
  [SerializeField] internal Button Doublebtn;
  [SerializeField] internal Button AutoBtn;
  [SerializeField] internal Button StopAutoBtn;
  [SerializeField] internal Button StartBtn;
  [SerializeField] internal TMP_Text NetBet;
  [Header("player data")]
  [SerializeField] internal PlayerData MainPlayers;
  [Header("SetBetLimit  data")]
  [SerializeField] internal GameObject BetLimitPanel;
  [SerializeField] internal List<Button> LimitBtn;
  [SerializeField] internal Button OpenLimit;
  [SerializeField] internal Sprite SelectedSprite;
  [SerializeField] internal Sprite NotSelectedSprite;

  [SerializeField] internal Button MultiplayerBtn;
  [SerializeField] internal Button SinglePlayerBtn;

  [SerializeField] private Transform openPosition;
  [SerializeField] private Transform closedPosition;

  [Header("Menu Panel")]
  [SerializeField] private float slideDuration = 0.3f;
  private bool isAnimating = false;
  private Vector3 originalPosition;

  private RectTransform buttonRect;

  private bool isOpen = false;

  private int uiSelectedCoin = 0;

  private void Start()
  {
    CollapseCoinsInstant();
    if (coinSelectorBtn) coinSelectorBtn.onClick.RemoveAllListeners();
    if (coinSelectorBtn) coinSelectorBtn.onClick.AddListener(delegate { ToggleCoins(); });

    if (Paytable_Button) Paytable_Button.onClick.RemoveAllListeners();
    if (Paytable_Button) Paytable_Button.onClick.AddListener(delegate { OpenPopup(PaytablePopup_Object); });

    if (PaytableExit_Button) PaytableExit_Button.onClick.RemoveAllListeners();
    if (PaytableExit_Button) PaytableExit_Button.onClick.AddListener(delegate { ClosePopup(PaytablePopup_Object); });

    if (Settings_Button) Settings_Button.onClick.RemoveAllListeners();
    if (Settings_Button) Settings_Button.onClick.AddListener(delegate { OpenPopup(SettingsPopup_Object); });

    if (SettingsExit_Button) SettingsExit_Button.onClick.RemoveAllListeners();
    if (SettingsExit_Button) SettingsExit_Button.onClick.AddListener(delegate { ClosePopup(SettingsPopup_Object); });

    if (MusicOn_Object) MusicOn_Object.SetActive(true);
    if (MusicOff_Object) MusicOff_Object.SetActive(false);

    if (SoundOn_Object) SoundOn_Object.SetActive(true);
    if (SoundOff_Object) SoundOff_Object.SetActive(false);

    if (GameExit_Button) GameExit_Button.onClick.RemoveAllListeners();
    if (GameExit_Button) GameExit_Button.onClick.AddListener(delegate
    {
      OpenPopup(QuitPopup_Object);
    });

    if (NoQuit_Button) NoQuit_Button.onClick.RemoveAllListeners();
    if (NoQuit_Button) NoQuit_Button.onClick.AddListener(delegate
    {
      if (!isExit)
      {
        ClosePopup(QuitPopup_Object);
      }
    });

    if (CrossQuit_Button) CrossQuit_Button.onClick.RemoveAllListeners();
    if (CrossQuit_Button) CrossQuit_Button.onClick.AddListener(delegate
    {
      if (!isExit)
      {
        ClosePopup(QuitPopup_Object);
      }
    });

    if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
    if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); });

    if (YesQuit_Button) YesQuit_Button.onClick.RemoveAllListeners();
    if (YesQuit_Button) YesQuit_Button.onClick.AddListener(delegate
    {
      CallOnExitFunction();
    });

    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(delegate { CallOnExitFunction(); });

    if (CloseAD_Button) CloseAD_Button.onClick.RemoveAllListeners();
    if (CloseAD_Button) CloseAD_Button.onClick.AddListener(CallOnExitFunction);



    if (audioController) audioController.ToggleMute(false);

    isMusic = true;
    isSound = true;

    if (Sound_Button) Sound_Button.onClick.RemoveAllListeners();
    if (Sound_Button) Sound_Button.onClick.AddListener(ToggleSound);

    if (Music_Button) Music_Button.onClick.RemoveAllListeners();
    if (Music_Button) Music_Button.onClick.AddListener(ToggleMusic);


    if (HistoryMain_button) HistoryMain_button.onClick.RemoveAllListeners();
    if (HistoryMain_button) HistoryMain_button.onClick.AddListener(delegate { OpenPopup(HistoryPopup_Object); HistorypageOpen(); });

    // if (MenuInGame_button) MenuInGame_button.onClick.RemoveAllListeners();
    // if (MenuInGame_button) MenuInGame_button.onClick.AddListener(delegate { OpenPopup(InfoPopup_Object); });

    if (CasualGame_button) CasualGame_button.onClick.RemoveAllListeners();
    if (CasualGame_button) CasualGame_button.onClick.AddListener(delegate { ResetMenuPanel(true); GameScreen_Object.SetActive(true); });

    if (NoviceGame_button) NoviceGame_button.onClick.RemoveAllListeners();
    if (NoviceGame_button) NoviceGame_button.onClick.AddListener(delegate { ResetMenuPanel(true); GameScreen_Object.SetActive(true); });

    if (ExpertGame_button) ExpertGame_button.onClick.RemoveAllListeners();
    if (ExpertGame_button) ExpertGame_button.onClick.AddListener(delegate { ResetMenuPanel(true); GameScreen_Object.SetActive(true); });

    if (HighRollerGame_button) HighRollerGame_button.onClick.RemoveAllListeners();
    if (HighRollerGame_button) HighRollerGame_button.onClick.AddListener(delegate { ResetMenuPanel(true); GameScreen_Object.SetActive(true); });

    if (Info_button) Info_button.onClick.RemoveAllListeners();
    if (Info_button) Info_button.onClick.AddListener(delegate { OpenPopup(InfoPopup_Object); });

    if (History_button) History_button.onClick.RemoveAllListeners();
    if (History_button) History_button.onClick.AddListener(delegate { OpenPopup(HistoryPopup_Object); HistorypageOpen(); });

    if (Sound_button) Sound_button.onClick.RemoveAllListeners();
    if (Sound_button) Sound_button.onClick.AddListener(delegate { ToggleSound(); });

    if (SoundMute_button) SoundMute_button.onClick.RemoveAllListeners();
    if (SoundMute_button) SoundMute_button.onClick.AddListener(delegate { ToggleSound(); });

    if (Music_button) Music_button.onClick.RemoveAllListeners();
    if (Music_button) Music_button.onClick.AddListener(delegate { ToggleSound(); });

    if (MusicMute_button) MusicMute_button.onClick.RemoveAllListeners();
    if (MusicMute_button) MusicMute_button.onClick.AddListener(delegate { ToggleSound(); });

    if (Home_button) Home_button.onClick.RemoveAllListeners();
    if (Home_button) Home_button.onClick.AddListener(delegate { OpenPopup(GameQuitPopup); });
    if (Exit_Button) Exit_Button.onClick.RemoveAllListeners();
    if (Exit_Button) Exit_Button.onClick.AddListener(delegate { OpenPopup(QuitPopup_Object); });

    if (YesHome_button) YesHome_button.onClick.RemoveAllListeners();
    if (YesHome_button) YesHome_button.onClick.AddListener(delegate { ClosePopup(GameQuitPopup); socketManager.SendHome(); ResetMenuPanel(false); });

    if (NoHome_button) NoHome_button.onClick.RemoveAllListeners();
    if (NoHome_button) NoHome_button.onClick.AddListener(delegate { ClosePopup(GameQuitPopup); });

    if (InfoLeft_button) InfoLeft_button.onClick.RemoveAllListeners();
    if (InfoLeft_button) InfoLeft_button.onClick.AddListener(delegate { GoToPreviousInfoPage(); });

    if (InfoRight_button) InfoRight_button.onClick.RemoveAllListeners();
    if (InfoRight_button) InfoRight_button.onClick.AddListener(delegate { GoToNextInfoPage(); });

    if (InfoClose_button) InfoClose_button.onClick.RemoveAllListeners();
    if (InfoClose_button) InfoClose_button.onClick.AddListener(delegate { ClosePopup(InfoPopup_Object); });

    if (HistoryClose_button) HistoryClose_button.onClick.RemoveAllListeners();
    if (HistoryClose_button) HistoryClose_button.onClick.AddListener(delegate { ClosePopup(HistoryPopup_Object); });
    if (OpenRoadMapBtn) OpenRoadMapBtn.onClick.RemoveAllListeners();
    if (OpenRoadMapBtn) OpenRoadMapBtn.onClick.AddListener(delegate { OpenPopup(RoadMapPopup); });
    if (OpenRoadMapBtn2) OpenRoadMapBtn2.onClick.RemoveAllListeners();
    if (OpenRoadMapBtn2) OpenRoadMapBtn2.onClick.AddListener(delegate { OpenPopup(RoadMapPopup); });
    if (CloseRoadmap) CloseRoadmap.onClick.RemoveAllListeners();
    if (CloseRoadmap) CloseRoadmap.onClick.AddListener(delegate { ClosePopup(RoadMapPopup); });

    if (OpenLimit) OpenLimit.onClick.RemoveAllListeners();
    if (OpenLimit) OpenLimit.onClick.AddListener(delegate { ToggleBetLimitPanel(); });

    if (MultiplayerBtn) MultiplayerBtn.onClick.RemoveAllListeners();
    if (MultiplayerBtn) MultiplayerBtn.onClick.AddListener(delegate { OnMultiplayerMode(); MultiplayerBtn.interactable = false; SinglePlayerBtn.interactable = true; });
    if (SinglePlayerBtn) SinglePlayerBtn.onClick.RemoveAllListeners();
    if (SinglePlayerBtn) SinglePlayerBtn.onClick.AddListener(delegate { OnSinglePlayerMode(); MultiplayerBtn.interactable = true; SinglePlayerBtn.interactable = false; });

    Repeatbtn.onClick.RemoveAllListeners();
    Repeatbtn.onClick.AddListener(delegate { gameManager.RequestRepeat(); });

    Undubtn.onClick.RemoveAllListeners();
    Undubtn.onClick.AddListener(delegate { socketManager.SendUndo(); });

    Canclebtn.onClick.RemoveAllListeners();
    Canclebtn.onClick.AddListener(delegate { socketManager.SendCancle(); });

    Doublebtn.onClick.RemoveAllListeners();
    Doublebtn.onClick.AddListener(delegate { socketManager.SendDouble(); });

    AutoBtn.onClick.RemoveAllListeners();
    AutoBtn.onClick.AddListener(delegate
    {
      StartBtn.interactable = false;
      gameManager.SetAuto(true);
    });

    StartBtn.onClick.RemoveAllListeners();
    StartBtn.onClick.AddListener(delegate { socketManager.SendStart(); });

    StopAutoBtn.onClick.RemoveAllListeners();
    StopAutoBtn.onClick.AddListener(delegate
    {
      StartBtn.interactable = true;
      gameManager.SetAuto(false);
    });

    foreach (Button btn in new[] { Repeatbtn, Undubtn, Canclebtn, Doublebtn, AutoBtn, StartBtn, StopAutoBtn })
    {
      if (!btn.GetComponent<ButtonAnimator>()) btn.gameObject.AddComponent<ButtonAnimator>();
    }

    // HistoryLeft.onClick.RemoveAllListeners();
    // HistoryLeft.onClick.AddListener(delegate { if (CurrentHistoryPage - 1 > 0) socketManager.SendHistory(CurrentHistoryPage - 1); });

    // HistoryRight.onClick.RemoveAllListeners();
    // HistoryRight.onClick.AddListener(delegate { if (CurrentHistoryPage + 1 < MaxHistoryPage) socketManager.SendHistory(CurrentHistoryPage + 1); });
    MultiplayerBtn.interactable = false;
    MultiplayerBtn.image.sprite = SelectedSprite;
  }




  #region Everytheing else

  public void ResetMenuPanel(bool IsGameScreen)
  {
    // MenuPanel_Object.SetActive(false);
    if (IsGameScreen)
    {
      Homebutton_Object.SetActive(true);
      //  MenuPanelContainer_Object.transform.localPosition = new Vector2(56, 394);
      MenuPanelContainer_Object.GetComponent<RectTransform>().anchoredPosition = new Vector2(56, 394);
      //  MenuPanel_Object.transform.SetParent(GameScreen_Object.transform, true);
      // int lastIndex = GameScreen_Object.transform.childCount - 1;
      // MenuPanel_Object.transform.SetSiblingIndex(lastIndex - 1);

    }
    else
    {
      Homebutton_Object.SetActive(false);
      // MenuPanelContainer_Object.transform.localPosition = new Vector2(56, 221);
      MenuPanelContainer_Object.GetComponent<RectTransform>().anchoredPosition = new Vector2(56, 221);
      //  MenuPanel_Object.transform.SetParent(HomeScreen_Object.transform, true);
      //   int lastIndex = HomeScreen_Object.transform.childCount - 1;
      // MenuPanel_Object.transform.SetSiblingIndex(lastIndex - 1);
      //
    }
  }


  public void ToggleMenuPanel()
  {
    if (isAnimating) return;

    if (IsMenuPanelOpen)
    {
      CloseMenuPanel();
    }
    else
    {
      OpenMenuPanel();
    }
  }

  private void OpenMenuPanel()
  {
    if (isAnimating) return;
    StartCoroutine(SlideIn());
  }

  private void CloseMenuPanel()
  {
    if (isAnimating) return;
    StartCoroutine(SlideOut());
  }

  private IEnumerator SlideIn()
  {
    isAnimating = true;
    MenuPanel_Object.SetActive(true);

    // Slide Menu Panel: from -1200 to -800 (moving RIGHT)
    Vector3 panelStartPos = new Vector3(-1200, MenuPanel_Object.transform.localPosition.y, MenuPanel_Object.transform.localPosition.z);
    Vector3 panelEndPos = new Vector3(-800, MenuPanel_Object.transform.localPosition.y, MenuPanel_Object.transform.localPosition.z);
    MenuPanel_Object.transform.localPosition = panelStartPos;

    // Slide Menu Button: from 0 to -80 (moving LEFT - opposite direction)
    Vector2 buttonStartPos = new Vector2(0, buttonRect.anchoredPosition.y);
    Vector2 buttonEndPos = new Vector2(-80, buttonRect.anchoredPosition.y);
    buttonRect.anchoredPosition = buttonStartPos;

    float elapsed = 0;
    while (elapsed < slideDuration)
    {
      elapsed += Time.deltaTime;
      float t = elapsed / slideDuration;

      MenuPanel_Object.transform.localPosition = Vector3.Lerp(panelStartPos, panelEndPos, t);
      buttonRect.anchoredPosition = Vector2.Lerp(buttonStartPos, buttonEndPos, t);

      yield return null;
    }

    MenuPanel_Object.transform.localPosition = panelEndPos;
    buttonRect.anchoredPosition = buttonEndPos;
    IsMenuPanelOpen = true;
    isAnimating = false;
  }

  private IEnumerator SlideOut()
  {
    isAnimating = true;

    // Slide Menu Panel: from -800 to -1200 (moving LEFT)
    Vector3 panelStartPos = new Vector3(-800, MenuPanel_Object.transform.localPosition.y, MenuPanel_Object.transform.localPosition.z);
    Vector3 panelEndPos = new Vector3(-1200, MenuPanel_Object.transform.localPosition.y, MenuPanel_Object.transform.localPosition.z);

    // Slide Menu Button: from -80 to 0 (moving RIGHT - opposite direction)
    Vector2 buttonStartPos = new Vector2(-80, buttonRect.anchoredPosition.y);
    Vector2 buttonEndPos = new Vector2(0, buttonRect.anchoredPosition.y);

    float elapsed = 0;
    while (elapsed < slideDuration)
    {
      elapsed += Time.deltaTime;
      float t = elapsed / slideDuration;

      MenuPanel_Object.transform.localPosition = Vector3.Lerp(panelStartPos, panelEndPos, t);
      buttonRect.anchoredPosition = Vector2.Lerp(buttonStartPos, buttonEndPos, t);

      yield return null;
    }

    MenuPanel_Object.transform.localPosition = panelEndPos;
    buttonRect.anchoredPosition = buttonEndPos;
    MenuPanel_Object.SetActive(false);
    IsMenuPanelOpen = false;
    isAnimating = false;
  }

  internal void SetgameRulePanel()
  {
    if (socketManager?.roomData?.stats == null) return;

    int invalidStats = 0;
    string firstInvalidStat = null;

    foreach (string statJson in socketManager.roomData.stats)
    {
      // Parse the JSON string
      DiceData diceData = JsonUtility.FromJson<DiceData>(statJson);

      // The server can send 0/0 entries for rounds that have no dice result
      bool hasDice = diceData != null && diceData.dice1 >= 1 && diceData.dice1 <= 6
          && diceData.dice2 >= 1 && diceData.dice2 <= 6;
      if (hasDice)
      {

        // Access your values
        int dice1 = diceData.dice1;
        int dice2 = diceData.dice2;
        bool isBonus = diceData.isBonus;
        int total = dice1 + dice2;
        Sprite winColor;
        if (total == 7)
        {
          winColor = Resultcolor[1];
        }
        else if (total < 7)
        {
          winColor = Resultcolor[0];
        }
        else
        {
          winColor = Resultcolor[2];
        }
        UpdateStats(total, DiceSprites[dice1 - 1], DiceSprites[dice2 - 1], isBonus);
      }
      else
      {
        invalidStats++;
        if (firstInvalidStat == null) firstInvalidStat = statJson;
      }
    }

    if (invalidStats > 0)
    {
      Debug.LogError("[JOIN_LEVEL] stats: skipped " + invalidStats + " of " + socketManager.roomData.stats.Count
          + " entries with dice outside 1-6, e.g. " + firstInvalidStat);
    }

    List<List<int>> allLevels = new List<List<int>>
    {
        socketManager.initialData.bets.level_1,
        socketManager.initialData.bets.level_2,
        socketManager.initialData.bets.level_3,
        socketManager.initialData.bets.level_4,
        socketManager.initialData.bets.level_5,
        socketManager.initialData.bets.level_6
    };



    for (int i = 0; i < LimitBtn.Count && i < allLevels.Count; i++)
    {
      int levelIndex = i;
      List<int> currentLevel = allLevels[levelIndex];

      if (currentLevel == null || currentLevel.Count == 0) continue;

      // Get min and max bet for this level
      int minBet = currentLevel[0];
      int maxBet = currentLevel[currentLevel.Count - 1];

      // Format the text
      string formattedText = FormatBetRange(minBet, maxBet);

      // Find the text component in the button's children
      TMP_Text buttonText = GetButtonTextComponent(LimitBtn[levelIndex]);
      if (buttonText != null)
      {
        buttonText.text = formattedText;
      }

      // Add click listener
      int levelNum = levelIndex + 1; // For level_1, level_2 etc.
      LimitBtn[levelIndex].onClick.RemoveAllListeners();
      LimitBtn[levelIndex].onClick.AddListener(() => OnLimitButtonClicked(levelNum));
    }

  }
  private string FormatBetRange(int minBet, int maxBet)
  {
    string minFormatted = FormatNumber(minBet);
    string maxFormatted = FormatNumber(maxBet);

    // If min and max are the same (only one bet amount), just show that amount
    if (minBet == maxBet)
    {
      return minFormatted;
    }

    // Otherwise show the range
    return $"{minFormatted} - {maxFormatted}";
  }
  private void OnLimitButtonClicked(int levelNumber)
  {
    if (gameManager.currentRoom != socketManager.initialData.levels[levelNumber - 1])
    {
      foreach (var txt in LimitBtn)
      {
        TMP_Text butT = GetButtonTextComponent(txt);
        if (butT != null)
        {
          butT.color = Color.white;
        }
      }
      TMP_Text butTe = GetButtonTextComponent(LimitBtn[levelNumber - 1]);
      if (butTe != null)
      {
        butTe.color = Color.yellow;
      }
      socketManager.SendHome();
      gameManager.currentRoom = socketManager.initialData.levels[levelNumber - 1];
      ToggleBetLimitPanel();
    }
  }

  private TMP_Text GetButtonTextComponent(Button button)
  {


    // Otherwise find in children
    TMP_Text textComponent = button.GetComponentInChildren<TMP_Text>();
    if (textComponent != null) return textComponent;

    // Try to find TextMeshPro component
    return button.GetComponentInChildren<TMP_Text>();
  }
  internal string FormatNumber(int number)
  {
    if (number >= 1000)
    {
      int thousands = number / 1000;
      return $"{thousands}K";
    }
    return number.ToString();
  }
  internal void LowBalPopup()
  {
    OpenPopup(LBPopup_Object);
  }

  internal void DisconnectionPopup()
  {
    if (!isExit)
    {
      OpenPopup(DisconnectPopup_Object);
    }
  }

  internal void ReconnectionPopup()
  {
    OpenPopup(ReconnectPopup_Object);
  }

  internal void CheckAndClosePopups()
  {
    if (ReconnectPopup_Object.activeInHierarchy)
    {
      ClosePopup(ReconnectPopup_Object);
    }
    if (DisconnectPopup_Object.activeInHierarchy)
    {
      ClosePopup(DisconnectPopup_Object);
    }
  }



  internal void ADfunction()
  {
    OpenPopup(ADPopup_Object);
  }


  private void CallOnExitFunction()
  {
    StartCoroutine(socketManager.CloseSocket());
    isExit = true;
    audioController.PlayButtonAudio();
  }


  internal void HistorypageOpen()
  {
    socketManager.SendHistory(1);
  }

  internal void OpenPopup(GameObject Popup)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (Popup) Popup.SetActive(true);
    if (MainPopup_Object) MainPopup_Object.SetActive(true);
  }

  internal void ClosePopup(GameObject Popup)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (Popup) Popup.SetActive(false);
    if (MainPopup_Object) MainPopup_Object.SetActive(false);
  }

  private void ToggleMusic()
  {
    isMusic = !isMusic;
    if (isMusic)
    {
      Music_button.gameObject.SetActive(true);
      MusicMute_button.gameObject.SetActive(false);
      audioController.ToggleMute(false, "bg");
    }
    else
    {
      Music_button.gameObject.SetActive(false);
      MusicMute_button.gameObject.SetActive(true);
      audioController.ToggleMute(true, "bg");
    }
  }

  private void UrlButtons(string url)
  {
    Application.OpenURL(url);
  }

  private void ToggleSound()
  {
    ToggleMusic();
    isSound = !isSound;
    if (isSound)
    {
      Sound_button.gameObject.SetActive(true);
      SoundMute_button.gameObject.SetActive(false);
      if (audioController) audioController.ToggleMute(false, "button");
      if (audioController) audioController.ToggleMute(false, "wl");
      if (audioController) audioController.ToggleMute(false, "win");
      if (audioController) audioController.ToggleMute(false, "bet");

    }
    else
    {
      Sound_button.gameObject.SetActive(false);
      SoundMute_button.gameObject.SetActive(true);
      if (audioController) audioController.ToggleMute(true, "button");
      if (audioController) audioController.ToggleMute(true, "wl");
      if (audioController) audioController.ToggleMute(true, "win");
      if (audioController) audioController.ToggleMute(true, "bet");
    }
  }

  private void UpdateInfoUI()
  {
    for (int i = 0; i < InfoPages_Objects.Count; i++)
      InfoPages_Objects[i].SetActive(i == currentInfoPage);

    for (int i = 0; i < InfoActive_Objects.Count; i++)
      InfoActive_Objects[i].SetActive(i == currentInfoPage);

  }

  private void GoToPreviousInfoPage()
  {
    if (audioController) audioController.PlayButtonAudio();
    currentInfoPage--;
    if (currentInfoPage < 0)
      currentInfoPage = InfoPages_Objects.Count - 1;

    UpdateInfoUI();
  }

  private void GoToNextInfoPage()
  {
    if (audioController) audioController.PlayButtonAudio();
    currentInfoPage++;
    if (currentInfoPage >= InfoPages_Objects.Count)
      currentInfoPage = 0;

    UpdateInfoUI();
  }

  private void ToggleCoins()
  {
    if (isExpanded)
      RetractCoins();
    else
      ExpandCoins();
  }

  private void SetupChipSelectorBackdrop()
  {
    if (!chipSelectorBackdrop) return;

    backdropImage = chipSelectorBackdrop.GetComponent<Image>();
    SetBackdropAlpha(0f);
    chipSelectorBackdrop.gameObject.SetActive(false);
    chipSelectorBackdrop.onClick.AddListener(RetractCoins);
  }

  private void SetBackdropAlpha(float alpha)
  {
    if (!backdropImage) return;

    Color color = backdropImage.color;
    color.a = alpha;
    backdropImage.color = color;
  }

  private void FadeBackdrop(bool show)
  {
    if (!backdropImage) return;

    backdropImage.DOKill();
    // Stops blocking clicks as soon as the close starts, not when the fade ends
    backdropImage.raycastTarget = show;

    if (show)
    {
      chipSelectorBackdrop.gameObject.SetActive(true);
      backdropImage.DOFade(backdropAlpha, backdropFadeDuration);
    }
    else
    {
      backdropImage.DOFade(0f, backdropFadeDuration)
          .OnComplete(() => chipSelectorBackdrop.gameObject.SetActive(false));
    }
  }

  private void ExpandCoins()
  {
    if (audioController) audioController.PlayWLAudio("openChip");
    FadeBackdrop(true);

    Vector3 center = coinSelector.transform.localPosition;
    int expandCount = Coins.Count - 1;
    int arcIndex = 0;

    for (int i = 0; i < Coins.Count; i++)
    {
      if (i == uiSelectedCoin)
        continue;

      Chip coin = Coins[i];
      coin.transform.DOKill();
      coin.gameObject.SetActive(true);

      float t = expandCount > 1 ? (float)arcIndex / (expandCount - 1) : 0.5f;
      float rad = Mathf.Lerp(fanStartAngle, fanEndAngle, t) * Mathf.Deg2Rad;
      Vector3 targetPos = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * fanRadius;

      coin.transform.DOLocalMove(targetPos, fanOpenDuration)
          .SetEase(fanOpenEase)
          .SetDelay(arcIndex * fanStagger);
      arcIndex++;
    }

    isExpanded = true;
  }

  private void RetractCoins()
  {
    if (!isExpanded) return;

    if (audioController) audioController.PlayWLAudio("coinSelect");
    FadeBackdrop(false);

    Vector3 center = coinSelector.transform.localPosition;

    for (int i = 0; i < Coins.Count; i++)
    {
      if (i == uiSelectedCoin)
        continue;

      Chip coin = Coins[i];
      coin.transform.DOKill();
      coin.transform.DOLocalMove(center, fanCloseDuration)
          .SetEase(fanCloseEase)
          .OnComplete(() => coin.gameObject.SetActive(false));
    }

    ShowRepeatIfNoBet();
    isExpanded = false;
  }

  private void CollapseCoinsInstant()
  {
    Vector3 center = coinSelector.transform.localPosition;

    for (int i = 0; i < Coins.Count; i++)
    {
      if (i == uiSelectedCoin)
        continue;

      Coins[i].transform.localPosition = center;
      Coins[i].gameObject.SetActive(false);
    }

    ShowRepeatIfNoBet();
    isExpanded = false;
  }

  private void ShowRepeatIfNoBet()
  {
    if (gameManager.currentTotalBet <= 0)
      Repeatpanel.SetActive(true);
  }

  public void OnCoinSelected(Button selectedCoin)
  {
    int newSelectedIndex = -1;
    for (int i = 0; i < Coins.Count; i++)
    {
      if (Coins[i].gameObject == selectedCoin.gameObject)
      {
        newSelectedIndex = i;
        break;
      }
    }

    coinSelector.chipImage.sprite = selectedCoin.image.sprite;

    TMP_Text selectorText = coinSelector.GetComponentInChildren<TMP_Text>();
    TMP_Text selectedText = selectedCoin.GetComponentInChildren<TMP_Text>();
    selectorText.text = selectedText.text;

    Chip selectorChip = coinSelector.GetComponent<Chip>();
    Chip selectedChip = selectedCoin.GetComponent<Chip>();

    int tempIndex = selectorChip.chipIndex;
    selectorChip.chipIndex = selectedChip.chipIndex;
    selectedChip.chipIndex = tempIndex;

    string tempchip = selectorChip.chipAmount;
    selectorChip.chipAmount = selectedChip.chipAmount;
    selectedChip.chipAmount = tempchip;

    uiSelectedCoin = newSelectedIndex;

    RetractCoins();
    for (int i = 0; i < Coins.Count; i++)
    {
      Coins[i].gameObject.SetActive(true);
    }
    selectedCoin.gameObject.SetActive(false);

    // The picked chip is skipped by the retract, so park it under the main chip for its next fan-out
    selectedCoin.transform.DOKill();
    selectedCoin.transform.localPosition = coinSelector.transform.localPosition;
  }

  #endregion




  #region Stats Panel





  internal void UpdateStats(int diceTotal, Sprite diceOne, Sprite diceTwo, bool isStar)
  {
    Sprite colorindex = Resultcolor[GetColorIndex(diceTotal)];
    UpdateLineStats(diceTotal.ToString(), diceOne, diceTwo, colorindex, isStar);
    UpdateGridStats(diceTotal.ToString(), colorindex, isStar);
    AddToVerticalGrid(diceTotal, colorindex, isStar);
    CalculateAndShowPercentage();

  }

  private void UpdateLineStats(string diceTotal, Sprite diceOne, Sprite diceTwo, Sprite colorindex, bool isStar)
  {
    int activeCount = 0;

    for (int i = 0; i < LineStats.Count; i++)
      if (LineStats[i].gameObject.activeSelf)
        activeCount++;

    if (activeCount < LineStats.Count)
    {
      activeCount++;
      LineStats[activeCount - 1].gameObject.SetActive(true);
    }

    for (int i = activeCount - 1; i > 0; i--)
      LineStats[i].CopyFrom(LineStats[i - 1]);

    LineStats[0].SetData(diceTotal, diceOne, diceTwo, colorindex, isStar, true);

    for (int i = 1; i < activeCount; i++)
      LineStats[i].HighBg.SetActive(false);
  }



  private void UpdateGridStats(string diceTotal, Sprite colorindex, bool isStar)
  {
    if (gridStats.Count >= MAX_GRID)
    {
      Destroy(gridStats[0].gameObject);
      gridStats.RemoveAt(0);
    }

    DiceStatsPrefab stat = Instantiate(StatsPref, StatsParent).GetComponent<DiceStatsPrefab>();
    stat.SetData(diceTotal, colorindex, isStar);

    gridStats.Add(stat);
  }
  private int currentColumn = 0;
  private int currentRow = 0;

  private void AddToVerticalGrid(int diceTotal, Sprite colorSprite, bool isStar)
  {
    int colorIndex = GetColorIndex(diceTotal);

    // Check if we should stay in same column
    bool sameColor = false;
    if (currentRow > 0 && currentColumn < verticalColumns.Count)
    {
      var lastCell = verticalColumns[currentColumn].row[currentRow - 1];
      if (lastCell.colorIndex == colorIndex)
      {
        sameColor = true;
      }
    }

    // If same color and column not full, add to same column
    if (sameColor && currentRow < 7)
    {
      // Add to current column, next row
      AddCell(currentColumn, currentRow, diceTotal, colorSprite, isStar, colorIndex);
      currentRow++;
    }
    else
    {
      // Move to next column
      currentColumn++;
      currentRow = 0;

      // If we exceed column limit, shift everything left
      if (currentColumn >= 15)
      {
        ShiftColumnsLeft();
        currentColumn = 14; // Last column
      }

      // Add to new column
      AddCell(currentColumn, currentRow, diceTotal, colorSprite, isStar, colorIndex);
      currentRow++;
    }
  }

  private void AddCell(int col, int row, int diceTotal, Sprite colorSprite, bool isStar, int colorIndex)
  {
    if (col < verticalColumns.Count && row < verticalColumns[col].row.Count)
    {
      var cell = verticalColumns[col].row[row];
      cell.SetData(diceTotal.ToString(), colorSprite, isStar);
      cell.colorIndex = colorIndex;
      cell.gameObject.SetActive(true);
    }
  }

  private void ShiftColumnsLeft()
  {
    // Shift all columns left by 1
    for (int col = 1; col < verticalColumns.Count; col++)
    {
      for (int row = 0; row < verticalColumns[col].row.Count; row++)
      {
        // Copy data from column to column-1
        var sourceCell = verticalColumns[col].row[row];
        var targetCell = verticalColumns[col - 1].row[row];

        targetCell.SetData(sourceCell.diceTotals, sourceCell.WinColor.sprite, sourceCell.winner);
        targetCell.colorIndex = sourceCell.colorIndex;
        targetCell.gameObject.SetActive(sourceCell.gameObject.activeSelf);
      }
    }

    // Clear last column
    int lastCol = verticalColumns.Count - 1;
    for (int row = 0; row < verticalColumns[lastCol].row.Count; row++)
    {
      var cell = verticalColumns[lastCol].row[row];
      cell.gameObject.SetActive(false);
      cell.colorIndex = -1;
    }
  }

  internal int GetColorIndex(int diceTotal)
  {
    if (diceTotal >= 2 && diceTotal <= 6) return 0;
    if (diceTotal == 7) return 1;
    return 2;
  }


  internal void CalculateAndShowPercentage()
  {
    var stats = GetStats();
    int total = stats.Count;

    //  Debug.Log($"[Percentage] Total Stats = {total}");
    if (total == 0) return;

    int seven = 0;
    int two = 0;
    int eight = 0;

    foreach (var s in stats)
    {
      if (s.WinStats == 7)
        seven++;

      else if (s.WinStats > 7)
        two++;
      else
        eight++;
    }

    float twoP = (two * 100f) / total;
    float sevenP = (seven * 100f) / total;
    float eightP = (eight * 100f) / total;

    TwoSixPercentage.text = twoP.ToString("0") + "%";
    SevenPercentage.text = sevenP.ToString("0") + "%";
    EightTwelvePercentage.text = eightP.ToString("0") + "%";
    TotalRounds.text = "Calculated from last " + total.ToString() + " rounds.";


  }




  private List<DiceStatsPrefab> GetStats()
  {
    List<DiceStatsPrefab> list = new List<DiceStatsPrefab>();

    for (int i = 0; i < StatsParent.childCount; i++)
    {
      var s = StatsParent.GetChild(i).GetComponent<DiceStatsPrefab>();
      if (s != null && s.gameObject.activeSelf)
        list.Add(s);
    }

    return list;
  }

  internal void setCoins(bool istrue)
  {
    chipPanel.SetActive(istrue);
  }
  internal int currentNetBet = 0;

  internal void SetNetBetPanel(int totalbet)
  {
    if (totalbet == 0) currentNetBet = 0;
    else
    {
      currentNetBet += totalbet; // add or subtract automatically
    }
    // Clamp to 0 (no negative values)
    if (currentNetBet < 0)
      currentNetBet = 0;

    NetBet.text = currentNetBet.ToString();
  }
  internal void SetChipoption(bool istrue, bool db = true, bool canc = true, bool undo = true)
  {
    chiOptionpanel.SetActive(istrue);
    Doublebtn.gameObject.SetActive(db);
    Canclebtn.gameObject.SetActive(canc);
    Undubtn.gameObject.SetActive(undo);
  }
  #endregion


  internal void HideBetLimitPanel()
  {
    isOpen = false;

    Transform target = isOpen ? openPosition : closedPosition;

    BetLimitPanel.transform.DOMove(target.position, 0.3f)
        .SetEase(isOpen ? Ease.OutCubic : Ease.InCubic);
  }
  void ToggleBetLimitPanel()
  {
    isOpen = !isOpen;

    Transform target = isOpen ? openPosition : closedPosition;

    BetLimitPanel.transform.DOMove(target.position, 0.3f)
        .SetEase(isOpen ? Ease.OutCubic : Ease.InCubic);
  }
  void OnMultiplayerMode()
  {
    socketManager.SendModeSelection("multiple");
    ToggleBetLimitPanel();
    if (LeaderBoard) LeaderBoard.SetActive(true);
    Diamond.SetActive(false);
    StartBtn.gameObject.SetActive(false);
    gameManager.isSinglePlayer = false;
    gameManager.ResetAllBetOptions();
    gameManager.ResetTimer();
    SetNetBetPanel(0);
    SinglePlayerBtn.image.sprite = NotSelectedSprite;
    MultiplayerBtn.image.sprite = SelectedSprite;
  }
  void OnSinglePlayerMode()
  {
    socketManager.SendModeSelection("single");
    ToggleBetLimitPanel();
    if (LeaderBoard) LeaderBoard.SetActive(false);
    Diamond.SetActive(true);
    StartBtn.gameObject.SetActive(true);
    gameManager.isSinglePlayer = true;
    gameManager.ResetAllBetOptions();
    SetNetBetPanel(0);
    SinglePlayerBtn.image.sprite = SelectedSprite;
    MultiplayerBtn.image.sprite = NotSelectedSprite;
  }

  internal void SetBetActionButtons(bool interactable)
  {
    Undubtn.interactable = interactable;
    Canclebtn.interactable = interactable;
    Doublebtn.interactable = interactable;
  }

  // Again, Auto and Auto Stop share one position, so exactly one is shown
  internal void SetRepeatSlot(bool autoOn, bool offerAuto, bool canRepeat)
  {
    StopAutoBtn.gameObject.SetActive(autoOn);
    AutoBtn.gameObject.SetActive(!autoOn && offerAuto);
    Repeatbtn.gameObject.SetActive(!autoOn && !offerAuto);
    Repeatbtn.interactable = canRepeat;
  }


}
// Add this class anywhere in your file
[System.Serializable]
public class DiceData
{
  public int dice1;
  public int dice2;
  public bool isBonus;
}
[System.Serializable]
public class StatsList
{
  [SerializeField] internal List<DiceStatsPrefab> row = new List<DiceStatsPrefab>();
}
