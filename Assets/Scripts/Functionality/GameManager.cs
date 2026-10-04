using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Linq;

public class GameManager : MonoBehaviour
{
  [Header("Pages")]
  [SerializeField] internal GameObject LoadingPage;
  [SerializeField] internal GameObject GamePage;

  [Header("ScriptRef")]
  [SerializeField] SocketIOManager socketManager;
  [SerializeField] internal UiManager uiManager;
  [SerializeField] AudioManager audioManager;


  [Header("Texts")]
  [SerializeField] private TMP_Text LoadingPage_text;
  [SerializeField] private TMP_Text NextRoundCount_text;
  [SerializeField] private TMP_Text TotalPlayer_text;
  [SerializeField] private TMP_Text minBet_text;
  [SerializeField] private TMP_Text maxBet_text;
  // [SerializeField] private TMP_Text WinChance_text;
  // [SerializeField] private TMP_Text ResponseMult_text;

  [Header("Options Text")]
  [SerializeField] private OptionPrefab TwoSixTxt;
  [SerializeField] private OptionPrefab SevenTxt;
  [SerializeField] private OptionPrefab EightTwelveTxt;

  [SerializeField] private OptionPrefab OptionQTxt;
  [SerializeField] private OptionPrefab OptionWTxt;
  [SerializeField] private OptionPrefab OptionETxt;
  [SerializeField] private OptionPrefab OptionRTxt;
  [SerializeField] private OptionPrefab OptionTTxt;
  [SerializeField] private OptionPrefab OptionYTxt;
  [SerializeField] private OptionPrefab OptionUTxt;
  [SerializeField] private OptionPrefab OptionITxt;
  [SerializeField] private OptionPrefab OptionOTxt;
  [SerializeField] private OptionPrefab OptionPTxt;
  [SerializeField] private List<OptionPrefab> AllOptions;

  [Header("Jili")]
  [SerializeField] private TMP_Text Timer_text;
  [SerializeField] private Image CircleTimerFill;

  [Header("Chip")]
  [SerializeField] private GameObject DealersPoint;
  [SerializeField] private List<Sprite> PlayerChipSprite;
  [SerializeField] private List<Sprite> otherChipSprite;
  [SerializeField] private GameObject chipPrefab;
  [SerializeField] private int initialCount = 20;
  [SerializeField] private Transform poolParent;

  private readonly List<GameObject> pool = new List<GameObject>();

  [Header("popup")]
  [SerializeField] private Button BetBlocker;
  [SerializeField] private GameObject BlockerObj;
  [SerializeField] private TMP_Text BlockerText;
  [SerializeField] private Transform popStart;
  [SerializeField] private Transform popCenter;
  [SerializeField] private Transform popEnd;
  [SerializeField] private float moveY = 60f;
  [SerializeField] private float duration = 0.8f;

  private Vector3 startPos;
  private Tween coinTween;


  [SerializeField] private float moveDuration = 0.5f;
  [SerializeField] private float holdDuration = 1f;
  private Coroutine animRoutine;
  [Header("Dice Animation")]
  [SerializeField] private DiceResultmanager DiceAnimator;
  [SerializeField] internal List<Sprite> FirstDiceSprite;
  [SerializeField] internal List<Sprite> SecondDiceSprite;
  [Header("Result ")]
  [SerializeField] private ImageAnimation PlayerWinAnimation;
  [Header("Animations ")]
  [SerializeField] private ImageAnimation Extrapay;
  [SerializeField] private ImageAnimation Betlocked;
  [SerializeField] private ImageAnimation Plesebetnow;
  [SerializeField] internal List<int> LeaderboadrdShow = new List<int>();
  // Off while the scene has no leaderboard UI; data is still received and logged
  [SerializeField] internal bool showLeaderboard = false;

  [Header("Extra Pay ")]
  [SerializeField] private GameObject ExtarPayObject;
  [SerializeField] private GameObject Bonusparent;
  public float popScale = 1.15f;
  public float animTime = 0.15f;

  internal int BetCounter;
  internal int MultiplierCounter;
  internal string currentRoom;
  internal double currentTotalBet = 0;
  private double currentWin;
  private double animationduration = 2f;
  internal bool isAuto = false;
  internal bool isSinglePlayer = false;
  private Coroutine StartGameCorutine;
  private Coroutine EndGameCorutine;

  private Vector3 endPos = new Vector3(0, -10, 0);


  private List<ChipData> PlayerChips = new List<ChipData>();
  internal List<ChipData> OtherPlayerChips = new List<ChipData>();


  void Awake()
  {

    for (int i = 0; i < initialCount; i++)
      AddChip();
  }
  private void Start()
  {
    BetCounter = 0;
    LoadingPage.SetActive(false);
    GamePage.SetActive(true);

    BetBlocker.onClick.RemoveAllListeners();
    BetBlocker.onClick.AddListener(() => PlayPopup("This Round is alredy closed.\nPlease wait for next round."));
  }


  #region  DataSetup
  internal void SetInitialData()
  {
    SetPlayerData(socketManager.playerdata);
  }
  internal IEnumerator ShowLoadingPage(string loadingPageText, int activeTime = 4)
  {

    LoadingPage_text.text = loadingPageText;
    LoadingPage.SetActive(true);
    yield return new WaitForSeconds(activeTime);
    LoadingPage.SetActive(false);

  }
  internal void SetLoadingPage(bool isActive)
  {


    LoadingPage.SetActive(isActive);
    if (isActive) StartCoroutine(ManageloadingPageText());
  }
  IEnumerator ManageloadingPageText()
  {
    for (int i = 0; i < 10; i++)
    {
      if (i < 8) LoadingPage_text.text = "Joining A Room.....";
      else LoadingPage_text.text = "Waiting For New Round To Start.....";

      yield return new WaitForSeconds(1f);
    }
  }
  internal void OnGameLoaded()
  {
    //  GamePage.SetActive(true);

    // SetOptionData();
    //  SetCoinData();
    SetLoadingPage(false);

  }
  internal void SetOptionData()
  {
    AllOptions[2].SetData(2, "2-6", socketManager.initialData.wagers.main_bets.number_2_6.payout, "main_bets");
    AllOptions[1].SetData(1, "7", socketManager.initialData.wagers.main_bets.number_7.payout, "main_bets");
    AllOptions[0].SetData(0, "8-12", socketManager.initialData.wagers.main_bets.number_8_12.payout, "main_bets");
    OptionQTxt.SetData(3, "2", socketManager.initialData.wagers.side_bets.s_2.payout, "side_bets");
    OptionWTxt.SetData(4, "3", socketManager.initialData.wagers.side_bets.s_3.payout, "side_bets");
    OptionETxt.SetData(5, "4", socketManager.initialData.wagers.side_bets.s_4.payout, "side_bets");
    OptionRTxt.SetData(6, "5", socketManager.initialData.wagers.side_bets.s_5.payout, "side_bets");
    OptionTTxt.SetData(7, "6", socketManager.initialData.wagers.side_bets.s_6.payout, "side_bets");
    OptionYTxt.SetData(8, "8", socketManager.initialData.wagers.side_bets.s_8.payout, "side_bets");
    OptionUTxt.SetData(9, "9", socketManager.initialData.wagers.side_bets.s_9.payout, "side_bets");
    OptionITxt.SetData(10, "10", socketManager.initialData.wagers.side_bets.s_10.payout, "side_bets");
    OptionOTxt.SetData(11, "11", socketManager.initialData.wagers.side_bets.s_11.payout, "side_bets");
    OptionPTxt.SetData(12, "12", socketManager.initialData.wagers.side_bets.s_12.payout, "side_bets");

  }

  internal void SetCoinData()
  {
    ResetCoinsToDefault();
    TotalPlayer_text.text = socketManager.roomData.payload.playerCount.ToString();
    string room = currentRoom;
    List<int> data = null;

    switch (room)
    {
      case "level_1":
        data = socketManager.initialData.bets.level_1;
        break;

      case "level_2":
        data = socketManager.initialData.bets.level_2;
        break;

      case "level_3":
        data = socketManager.initialData.bets.level_3;
        break;

      case "level_4":
        data = socketManager.initialData.bets.level_4;
        break;
      case "level_5":
        data = socketManager.initialData.bets.level_5;
        break;
      case "level_6":
        data = socketManager.initialData.bets.level_6;
        break;
    }

    if (data == null) return;

    uiManager.coinSelector.Chiptext.text = uiManager.FormatNumber(data[0]);
    uiManager.coinSelector.chipAmount = data[0].ToString();
    uiManager.coinSelector.chipIndex = 0;
    minBet_text.text = uiManager.FormatNumber(data[0]);
    maxBet_text.text = uiManager.FormatNumber(data[data.Count - 1]);
    foreach (var txt in uiManager.PayoutText)
    {
      txt.text = data[data.Count - 1].ToString();

    }

    for (int i = 0; i < uiManager.Coins.Count; i++)
    {
      uiManager.Coins[i].Chiptext.text = uiManager.FormatNumber(data[i]);
      uiManager.Coins[i].chipIndex = i;
      uiManager.Coins[i].chipAmount = data[i].ToString();
    }

  }
  public void ResetCoinsToDefault()
  {
    // Force select 0 index
    uiManager.coinSelector.chipImage.sprite = PlayerChipSprite[0];

    // Reset all coins visuals
    for (int i = 0; i < uiManager.Coins.Count; i++)
    {
      uiManager.Coins[i].chipImage.sprite = PlayerChipSprite[i];
    }

  }


  void SetPlayerData(Player player)
  {
    uiManager.MainPlayers.SetData(player.username, player.balance.ToString(), uiManager.UserIcons[0]);
  }
  // Leaderboards arrive with the JOIN_LEVEL ack, the PLAYER_MODE ack and every game:cashout
  internal void SetOtherplayerData(Leaderboards leaderboard)
  {
    if (leaderboard == null)
    {
      if (socketManager.logs.leaderboard) Debug.Log("[LEADERBOARD] none in payload");
      return;
    }

    if (socketManager.logs.leaderboard) Debug.Log("[LEADERBOARD] " + DescribeLeaderboard(leaderboard));
    if (!showLeaderboard) return;

    string mainPlayerName = uiManager.MainPlayers.playername.text;
    Sprite mainPlayerIcon = uiManager.MainPlayers.PlayerIcon.sprite;

    // One WinnerPlayers slot per top winner; unused slots stay hidden
    foreach (var item in uiManager.WinnerPlayers)
      item.gameObject.SetActive(false);

    if (leaderboard.winners == null) return;

    int winnersCount = Mathf.Min(leaderboard.winners.Count, uiManager.WinnerPlayers.Count);

    for (int i = 0; i < winnersCount; i++)
    {
      Winner win = leaderboard.winners[i];

      // Other players have no avatar from the server, so they get a random icon
      Sprite iconToUse = (win.username == mainPlayerName)
          ? mainPlayerIcon
          : uiManager.UserIcons[UnityEngine.Random.Range(0, uiManager.UserIcons.Count)];

      uiManager.WinnerPlayers[i].SetData(
          win.username,
          win.totalWins.ToString(),
          iconToUse
      );

      uiManager.WinnerPlayers[i].gameObject.SetActive(true);
    }
  }

  string DescribeLeaderboard(Leaderboards leaderboard)
  {
    string winners = leaderboard.winners == null ? "none"
        : string.Join(", ", leaderboard.winners.Select(w => w.username + "=" + w.totalWins));
    string richest = leaderboard.richest == null ? "none"
        : string.Join(", ", leaderboard.richest.Select(r => r.username + "=" + r.balance));
    return "winners: " + winners + " | richest: " + richest;
  }




  #endregion




  #region GamePlay


  internal void SetBetTimer()
  {

    BetBlocker.gameObject.SetActive(false);

    foreach (var obj in AllOptions)
    {

      obj.BlackTransParent.SetActive(false);

    }

    int time = socketManager.TimeRemaining.timeRemaining;
    time--;
    StartBetTimer(time);

    if (time == 5)
    {
      audioManager.PlayGirlAudio("timeisrunning");
    }
    else if (time == 0)
    {
      // BetBlocker.gameObject.SetActive(true);
      audioManager.PlayGirlAudio("nomorebets");

      BetBlocker.gameObject.SetActive(true);
      foreach (var obj in AllOptions)
      {
        if (obj.PlayerbetPos.gameObject.activeInHierarchy)
        {
          obj.BlackTransParent.SetActive(false);
        }
        else
        {
          obj.BlackTransParent.SetActive(true);
        }
      }
      // audioManager.PlayWLAudio("betDone");
      Betlocked.StopAnimation();
      Betlocked.gameObject.SetActive(true);
      Betlocked.StartAnimation();
    }
    if (time % 5 == 4)
    {


    }
  }
  internal void OnGameLoopStart()
  {
    foreach (var item in PlayerChips)
    {
      if (item.chip != null)
      {
        ReturnChip(item.chip.GetComponent<Chip>());
      }
    }
    PlayerChips.Clear();

    // Clear other players' chips
    foreach (var item in OtherPlayerChips)
    {
      if (item.chip != null)
      {
        ReturnChip(item.chip.GetComponent<Chip>());
      }
    }
    OtherPlayerChips.Clear();
    REsetAllBetObject();
    audioManager.PlayWLAudio("betNow");
    Plesebetnow.StopAnimation();
    Plesebetnow.gameObject.SetActive(true);
    Plesebetnow.StartAnimation();

    if (isAuto) socketManager.SendRepeat();
    else uiManager.ToggleRepeteAuto(false);
    ResetBonusUI();
  }
  private Tween timerTween;
  private int maxBetTime = -1;
  private int lastTime = int.MaxValue;
  public void StartBetTimer(int time)
  {
    ResetBonusUI();
    CircleTimerFill.gameObject.transform.parent.gameObject.SetActive(true);
    // First packet decides max timer
    if (maxBetTime == -1)
      maxBetTime = time;

    Timer_text.text = time.ToString();
    // Ignore if server sends a bigger time later
    if (time > lastTime)
      return;

    lastTime = time;

    float targetFill = (float)time / maxBetTime;

    // Stop previous animation
    timerTween?.Kill();
    // Smooth animation
    timerTween = CircleTimerFill
        .DOFillAmount(targetFill, 1f)
        .SetEase(Ease.Linear);
  }

  internal void ResetTimer()
  {
    maxBetTime = -1;
    lastTime = int.MaxValue;
    CircleTimerFill.fillAmount = 1f;
  }
  internal void ManageResult(Root diceResult)
  {
    uiManager.HideBetLimitPanel();
    if (uiManager.currentNetBet == 0) uiManager.ToggleRepeteAuto(false);
    else uiManager.AutoBtn.gameObject.SetActive(true);
    audioManager.PlayWLAudio("shakingDice");
    DiceAnimator.StartAnimation(FirstDiceSprite[diceResult.dice1 - 1], SecondDiceSprite[diceResult.dice2 - 1]);
    CircleTimerFill.gameObject.transform.parent.gameObject.SetActive(false);
    ResetTimer();
    StartCoroutine(ManageAfterResult(diceResult));


  }
  IEnumerator ManageAfterResult(Root diceResult)
  {
    yield return new WaitForSeconds(3f);
    int total = diceResult.dice1 + diceResult.dice2;
    audioManager.StopWLAaudio();
    uiManager.UpdateStats(total, uiManager.DiceSprites[diceResult.dice1 - 1], uiManager.DiceSprites[diceResult.dice2 - 1], true);
    if (total == 7)
    {
      AllOptions[1].BlackTransParent.SetActive(false);
      playWin(AllOptions[1]);
    }
    else if (total < 7)
    {
      AllOptions[2].BlackTransParent.SetActive(false);
      AllOptions[total + 1].BlackTransParent.SetActive(false);
      playWin(AllOptions[2]);
      playWin(AllOptions[total + 1]);

    }
    else
    {
      AllOptions[0].BlackTransParent.SetActive(false);

      AllOptions[total].BlackTransParent.SetActive(false);
      playWin(AllOptions[0]);
      playWin(AllOptions[total]);
    }

  }
  void playWin(OptionPrefab item)
  {
    if (item.PlayerbetPos.gameObject.activeInHierarchy)
    {
      item.winAnimation.gameObject.SetActive(true);
      item.winAnimation.StopAnimation();
      item.winAnimation.StartAnimation();
    }
  }
  internal void EndLoop()
  {

    EndGameCorutine = StartCoroutine(GameLoop());
  }

  IEnumerator GameLoop()
  {
    if (StartGameCorutine != null)
    {
      StopCoroutine(StartGameCorutine);
      StartGameCorutine = null;
    }
    uiManager.CalculateAndShowPercentage();
    PlayWinAnimations();

    yield return new WaitForSeconds(2f);

    if (currentWin >= 1)
    {
      int winInt = Mathf.FloorToInt((float)currentWin);

      PlayPopup("You Won\n" + winInt);
      playtheCoin("+" + winInt);
    }

    currentWin = 0;
    ResetAllBetUI();

  }

  internal void ManagePayouts()
  {
    StartCoroutine(ManagePayout());
  }
  IEnumerator ManagePayout()
  {
    foreach (var item in AllOptions)
    {
      // item.BG.SetActive(true);
      item.BlackTransParent.SetActive(true);

    }
    // MoveAllChipstohome();
    // yield return new WaitForSeconds(1f);
    yield return new WaitForSeconds(2f);
    ManagePayments(socketManager.CashoutData.payouts);
    yield return new WaitForSeconds(2f);
    DistributeAllPayout();
    foreach (var op in AllOptions)
    {
      op.ResetOptionUI();
      op.BlackTransParent.SetActive(false);
    }
    SetOtherplayerData(socketManager.CashoutData.leaderboards);
    uiManager.SetNetBetPanel(0);
    yield return new WaitForSeconds(2f);
    // foreach (var op in AllOptions)
    // {
    //     op.ResetOptionUI();
    //     op.BlackTransParent.SetActive(false);
    // }
    if (isSinglePlayer && isAuto) socketManager.SendRepeat();
  }

  void DistributeAllPayout()
  {
    DistributePayouts(socketManager.CashoutData.payouts);
  }
  void ManagePayments(List<Payout> payouts)
  {
    Dictionary<string, int> playerBets = new Dictionary<string, int>();
    Dictionary<string, int> otherBets = new Dictionary<string, int>();
    string currentPlayer = uiManager.MainPlayers.playername.text;

    foreach (var payout in payouts)
    {
      if (payout.betWins == null) continue;

      var targetDict = payout.username == currentPlayer ? playerBets : otherBets;

      foreach (var bet in payout.betWins)
      {
        if (targetDict.ContainsKey(bet.Key))
          targetDict[bet.Key] += bet.Value;
        else
          targetDict.Add(bet.Key, bet.Value);
      }
    }

    // Player payouts to PlayerbetPos
    foreach (var bet in playerBets)
    {
      OptionPrefab op = FindOption(bet.Key);
      SpawnPayoutChips(bet.Value, op.PlayerbetPos.transform, op, true);
      //  op.ResetOptionUI();
    }

    // Other player payouts to OtherPlayerbetPos
    foreach (var bet in otherBets)
    {
      OptionPrefab op = FindOption(bet.Key);
      SpawnPayoutChips(bet.Value, op.OtherPlayerbetPos.transform, op, false);
      //  op.ResetOptionUI();
    }
  }
  void SpawnPayoutChips(int amount, Transform target, OptionPrefab op, bool isPlayer)
  {
    if (amount <= 0) return;

    List<int> chips = BreakAmountIntoChips(amount, FindRoom());

    foreach (int chipAmount in chips)
    {
      Chip chip = GetChip();
      Sprite sprite = isPlayer ? PlayerChipSprite[0] : otherChipSprite[0];
      chip.SetData(sprite, chipAmount.ToString(), 0);

      RectTransform rt = chip.GetComponent<RectTransform>();
      rt.SetParent(poolParent);

      // Spawn at DealerPoint
      rt.position = DealersPoint.transform.position;
      rt.localScale = Vector3.one;

      // Move from DealerPoint to option prefab and disappear
      rt.DOMove(target.position, 1f).SetEase(Ease.InQuad).OnComplete(() =>
      {
        // Add to option prefab text
        if (isPlayer)
          op.AddPlayerChip(chipAmount, sprite);
        else
          op.AddOtherPlayerChip(chipAmount, sprite);

        ReturnChip(chip);
        // op.ResetOptionUI();
      });
    }
  }
  public void DistributePayouts(List<Payout> payouts)
  {
    string currentPlayer = uiManager.MainPlayers.playername.text;

    foreach (var payout in payouts)
    {
      if (payout.betWins == null) continue;

      Transform target = FindPlayerTransform(payout.username);
      if (target == null) target = TotalPlayer_text.transform;

      bool isCurrentPlayer = payout.username == currentPlayer;

      // Move chips from each winning option to the player
      foreach (var bet in payout.betWins)
      {
        OptionPrefab op = FindOption(bet.Key);
        if (op == null) continue;

        // Get the start position (where the chips are currently displayed)
        Transform startPos = isCurrentPlayer ? op.PlayerbetPos.transform : op.OtherPlayerbetPos.transform;
        int winAmount = bet.Value;

        // Spawn chips that move from option to player
        MoveChipsFromOptionToPlayer(startPos, target, winAmount, isCurrentPlayer);
      }

      // Update player balance for current player
      if (isCurrentPlayer)
      {
        // Parse balance - remove any non-numeric characters (like "Rs")
        string balanceText = uiManager.MainPlayers.playerBalence.text;
        string numericBalance = new string(balanceText.Where(c => char.IsDigit(c) || c == '.').ToArray());

        int oldBalance = int.Parse(numericBalance);
        double newBalance = payout.balance;
        currentWin = newBalance - oldBalance;
        uiManager.MainPlayers.playerBalence.text = payout.balance.ToString();
        socketManager.playerdata.balance = payout.balance;

        if (currentWin >= 1)
        {
          int winInt = Mathf.FloorToInt((float)currentWin);
          PlayPopup("You Won\n" + winInt);
          playtheCoin("+" + winInt);
        }
      }
    }
  }
  void MoveChipsFromOptionToPlayer(Transform startPos, Transform target, int amount, bool isPlayer)
  {
    if (amount <= 0) return;

    List<int> chips = BreakAmountIntoChips(amount, FindRoom());

    foreach (int chipAmount in chips)
    {
      Chip chip = GetChip();
      Sprite sprite = isPlayer ? PlayerChipSprite[0] : otherChipSprite[0];
      chip.SetData(sprite, chipAmount.ToString(), 0);

      RectTransform rt = chip.GetComponent<RectTransform>();
      rt.SetParent(poolParent);
      rt.position = startPos.position;
      rt.localScale = Vector3.one;

      // Move from option position to player target
      rt.DOMove(target.position, 0.6f).SetEase(Ease.OutQuad).OnComplete(() =>
      {
        ReturnChip(chip);
      });
    }
  }


  internal void REsetAllBetObject()
  {
    foreach (var obj in AllOptions)
    {
      obj.PlayerbetPos.gameObject.SetActive(false);
      obj.OtherPlayerbetPos.gameObject.SetActive(false);
      obj.ResetOptionUI();
    }

  }



  void HighlightOption(int index)
  {
    for (int i = 0; i < AllOptions.Count; i++)
    {
      bool active = (i == index);

      AllOptions[i].BlackTransParent.SetActive(active);
      // AllOptions[i].BG.SetActive(!active);
    }
  }




  private void MoveChip(Transform chip, Transform startPos, Transform endPos, bool disableOnEnd, float duration = 0.4f)
  {
    if (chip == null || startPos == null || endPos == null)
    {
      Debug.LogError("ChipMover: One of the transforms is null!");
      return;
    }

    chip.position = startPos.position;
    chip.DOMove(endPos.position, duration)
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
          if (disableOnEnd)
          {
            ReturnChip(chip.gameObject.GetComponent<Chip>());
          }
        });
  }

  #endregion




  #region  beting
  internal void onClickOption(GameObject option)
  {

    OptionPrefab optionprefab = option.GetComponent<OptionPrefab>();


    int index = uiManager.coinSelector.chipIndex;
    double chipValue;
    if (double.TryParse(uiManager.coinSelector.chipAmount, out chipValue))
    {
      if (chipValue > socketManager.playerdata.balance)
      {
        PlayPopup("Low Balance");
        // Low balance logic here

        return;
      }
    }
    socketManager.BetPlaced(index, optionprefab.VaridontWant, socketManager.initialData.betOptions[optionprefab.Optionindex]);


  }
  internal void ManageBrodcastBetsPlayer()
  {
    if (socketManager?.BetChipData?.payload == null)
    {
      Debug.LogError("ManageBrodcastBetsPlayer: BetChipData or payload is null");
      return;
    }

    uiManager.SetChipoption(true);
    int totalAmount = socketManager.BetChipData.payload.amount;

    List<int> roomChips = FindRoom();
    if (roomChips == null || roomChips.Count == 0)
    {
      Debug.LogError($"ManageBrodcastBetsPlayer: No chip denominations found for room: {currentRoom}");
      return;
    }

    List<int> chipPieces = BreakAmountIntoChips(totalAmount, roomChips);

    OptionPrefab optionOP = FindOption(socketManager.BetChipData.payload.betOption);
    if (optionOP == null)
    {
      Debug.LogError($"ManageBrodcastBetsPlayer: Could not find option for {socketManager.BetChipData.payload.betOption}");
      return;
    }

    // CRITICAL FIX: Check PlayerbetPos and its transform BEFORE using them
    if (optionOP.PlayerbetPos == null)
    {
      Debug.LogError($"ManageBrodcastBetsPlayer: PlayerbetPos is null for option {socketManager.BetChipData.payload.betOption}");
      return;
    }

    Transform targetTransform = optionOP.PlayerbetPos.transform;
    if (targetTransform == null)
    {
      Debug.LogError($"ManageBrodcastBetsPlayer: PlayerbetPos.transform is null for option {socketManager.BetChipData.payload.betOption}");
      return;
    }

    if (optionOP.PlayerbetStartPos == null)
    {
      Debug.LogError($"ManageBrodcastBetsPlayer: PlayerbetStartPos is null for option {socketManager.BetChipData.payload.betOption}");
      return;
    }

    foreach (int chipAmount in chipPieces)
    {
      ChipData data = new ChipData();
      data.betId = socketManager.BetChipData.payload.betId;
      data.amount = chipAmount;

      int index = findChipindex(chipAmount, roomChips);
      string val = chipAmount.ToString();

      // Now call SpawnChip with the validated transform
      data.chip = SpawnChip(
          findChipSprite(chipAmount, roomChips),
          val,
          index,
          optionOP.PlayerbetStartPos,
          optionOP,  // Use the pre-validated transform
          true,
          0.2f
      );

      if (data.chip != null)
      {
        PlayerChips.Add(data);
        UpdateMyBetOnOption(socketManager.BetChipData.payload.betOption, chipAmount);
        uiManager.SetNetBetPanel(chipAmount);
      }
      else
      {
        Debug.LogError($"Failed to spawn chip for amount {chipAmount}");
      }
    }


  }
  // Highlights bets made by the leaderboard players the user tapped
  void ShowtheOtherPlayer()
  {
    if (!showLeaderboard) return;
    foreach (var chip in OtherPlayerChips)
    {
      foreach (int index in LeaderboadrdShow)
      {
        if (chip.username == uiManager.WinnerPlayers[index].playername.text)
        {
          OptionPrefab op = chip.betOptionK;
          op.PurpleBorderAnimation.gameObject.SetActive(true);
        }
      }
    }
  }

  internal void ManageBrodcastBetsOtherPlayers(Root chipdata)
  {
    // Handle cancellation (negative amount)
    if (chipdata.amount < 0)
    {
      ClearOtherPlayerbets(chipdata);
      return;
    }

    // Do not show own chip here
    if (chipdata.username == uiManager.MainPlayers.playername.text)
      return;

    List<int> roomChips = FindRoom();
    int totalAmount = chipdata.amount;

    // Break large amount into individual chips
    List<int> chipPieces = BreakAmountIntoChips(totalAmount, roomChips);

    foreach (int piece in chipPieces)
    {
      int index = findChipindex(piece, roomChips);

      string val = piece.ToString();

      ChipData data = new ChipData();
      data.betId = chipdata.betId;
      data.amount = piece;
      data.username = chipdata.username; // Store username

      data.chip = SpawnChip(
          findOtherPlayerChipSprite(piece, roomChips),
          val,
          index,
          TotalPlayer_text.transform,
          FindOption(chipdata.betOption),
          false
      );
      data.betOptionK = FindOption(chipdata.betOption);
      OtherPlayerChips.Add(data);
    }
    ShowtheOtherPlayer();
  }
  void ClearOtherPlayerbets(Root chipdata)
  {
    // Remove only chips from this specific betId and username
    for (int i = OtherPlayerChips.Count - 1; i >= 0; i--)
    {
      var chips = OtherPlayerChips[i];

      if (chips.betId == chipdata.betId)
      {
        if (chips.chip != null)
        {
          Chip c = chips.chip.GetComponent<Chip>();
          ReturnChip(c);
        }
        OtherPlayerChips.RemoveAt(i);
      }
    }

    // Update the UI for this option (subtract the amount)
    OptionPrefab option = FindOption(chipdata.betOption);
    if (option != null && chipdata.amount < 0)
    {
      // Negative amount means cancellation - subtract from other player's bet
      option.AddOtherPlayerChip(chipdata.amount, null);
    }
  }



  // GameObject SpawnChip(Sprite sprite, string amount, int chipindex, Transform startPoint, OptionPrefab op, float moveTime = 0.4f)
  // {


  //     Chip chip = GetChip();
  //     chip.SetData(sprite, amount, chipindex);

  //     RectTransform chipRT = chip.GetComponent<RectTransform>();
  //     chipRT.SetParent(poolParent);
  //     chipRT.localScale = Vector3.one;

  //     Vector2 size = op.chiparea.rect.size;
  //     Vector2 randomPos = new Vector2(
  //         UnityEngine.Random.Range(-size.x * 0.5f, size.x * 0.5f),
  //         UnityEngine.Random.Range(-size.y * 0.5f, size.y * 0.5f)
  //     );

  //     chipRT.position = startPoint.position;
  //     chipRT.DOMove(op.chiparea.TransformPoint(randomPos), moveTime);

  //     return chip.gameObject;
  // }

  GameObject SpawnChip(Sprite sprite, string amount, int chipindex, Transform startPoint, OptionPrefab op, bool playerbet, float moveTime = 0.4f)
  {
    Chip chip = GetChip();
    chip.SetData(sprite, amount, chipindex);

    RectTransform chipRT = chip.GetComponent<RectTransform>();
    chipRT.SetParent(poolParent);
    chipRT.localScale = Vector3.zero;

    // Start position
    chipRT.position = startPoint.position;
    Vector3 targetPos;
    // Exact target position
    if (playerbet) targetPos = op.PlayerbetPos.transform.position;
    else targetPos = op.OtherPlayerbetPos.transform.position;
    int chipAmount = int.Parse(amount);
    Sequence seq = DOTween.Sequence();

    seq.Append(chipRT.DOScale(1.4f, moveTime * 0.5f).SetEase(Ease.OutBack));
    seq.Join(chipRT.DOMove(targetPos, moveTime).SetEase(Ease.OutQuad));
    seq.Append(chipRT.DOScale(1f, moveTime * 0.2f));
    seq.OnComplete(() =>
{
  if (playerbet)
  {
    // Get the appropriate sprite for this chip amount
    Sprite chipSpriteToShow = GetChipSpriteForAmount(chipAmount, true);

    // Update the bet text and chip sprite on the option prefab
    op.AddPlayerChip(chipAmount, chipSpriteToShow);

    // Return the chip to pool
    ReturnChip(chip);
  }
  else
  {
    // Get the appropriate sprite for other player's chip
    Sprite chipSpriteToShow = GetChipSpriteForAmount(chipAmount, false);

    // For other players, just update the UI
    op.AddOtherPlayerChip(chipAmount, chipSpriteToShow);

    // Return the chip to pool
    ReturnChip(chip);
  }
});
    return chip.gameObject;
  }


  internal Sprite GetChipSpriteForAmount(int amount, bool isPlayer)
  {
    List<int> roomChips = FindRoom();
    if (roomChips == null || roomChips.Count == 0)
      return isPlayer ? PlayerChipSprite[0] : otherChipSprite[0];

    // Find the largest chip denomination that is <= amount
    int selectedDenomination = roomChips[0]; // Start with smallest

    foreach (int chipValue in roomChips)
    {
      if (amount >= chipValue)
      {
        selectedDenomination = chipValue;
      }
      else
      {
        break; // Since roomChips should be sorted ascending
      }
    }

    // Find the index of this denomination
    int index = roomChips.IndexOf(selectedDenomination);

    // Return the appropriate sprite
    return isPlayer ? PlayerChipSprite[index] : otherChipSprite[index];
  }



  #endregion


  #region Manage Result and reset
  void PlayWinAnimations()
  {
    foreach (var item in AllOptions)
    {
      item.winAnimation.StopAnimation();
      // if (item.HighlightedBG.activeInHierarchy)
      // {
      //     // item.winAnimation.StartAnimation();
      // }
    }
  }




  private Chip SpawnChipFromPool()
  {
    Chip chip = GetChip();   // your pool code
    chip.transform.SetParent(poolParent);
    chip.transform.position = DealersPoint.transform.position;
    return chip;
  }

  private void MoveChip(Chip chip, Transform target, bool returnToPool)
  {
    chip.transform.DOMove(target.position, 0.6f)
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
          if (returnToPool)
          {
            ReturnChip(chip);
          }
        });
  }

  // Where a player's payout chips fly to; null means the caller falls back to the player-count label
  private Transform FindPlayerTransform(string playerId)
  {
    if (uiManager.MainPlayers.playername.text == playerId)
      return uiManager.MainPlayers.transform;

    if (showLeaderboard)
    {
      foreach (var p in uiManager.RichestPlayers)
      {
        if (p != null && p.playername.text == playerId) return p.transform;
      }

      foreach (var p in uiManager.WinnerPlayers)
      {
        if (p != null && p.playername.text == playerId) return p.transform;
      }
    }

    return null;
  }




  private void SpawnPayoutChips(float amount, Transform target)
  {
    if (amount <= 0) return;

    Chip chip = SpawnChipFromPool();
    MoveChip(chip, target, true);
  }










  #endregion






  #region  manage BEt Double bet cancle &&& undo
  internal void RepeAtBet(List<Bet> bets)
  {
    // audioManager.PlayWLAudio("double");

    List<int> roomChips = FindRoom(); // chip denominations

    foreach (var bet in bets)
    {

      int amount = bet.amount;

      // Break amount into multiple chips
      List<int> chipPieces = BreakAmountIntoChips(amount, roomChips);

      foreach (int piece in chipPieces)
      {
        ChipData data = new ChipData();
        data.betId = bet.betId;
        data.amount = piece;

        string val = piece.ToString();
        int index = findChipindex(piece, roomChips);

        if (index <= 5)   // your existing condition
        {
          OptionPrefab opts = FindOption(bet.betOption);
          data.chip = SpawnChip(
              findChipSprite(piece, roomChips),
              val,
              index,
              opts.PlayerbetPos.transform,
              opts,

              true
          );

          PlayerChips.Add(data);
        }
        UpdateMyBetOnOption(bet.betOption, piece);

      }
      uiManager.SetNetBetPanel(amount);
    }
    uiManager.SetChipoption(true);
  }
  internal void DoubleBets(List<Bet> bets)
  {
    // audioManager.PlayWLAudio("double");

    List<int> roomChips = FindRoom(); // chip denominations

    foreach (var bet in bets)
    {
      if (bet.delta > 0)
      {
        int amount = bet.oldAmount;

        // Break amount into multiple chips
        List<int> chipPieces = BreakAmountIntoChips(amount, roomChips);

        foreach (int piece in chipPieces)
        {
          ChipData data = new ChipData();
          data.betId = bet.betId;
          data.amount = piece;

          string val = piece.ToString();
          int index = findChipindex(piece, roomChips);

          if (index <= 5)   // your existing condition
          {
            OptionPrefab opts = FindOption(bet.betOption);
            data.chip = SpawnChip(
                findChipSprite(piece, roomChips),
                val,
                index,
                opts.PlayerbetPos.transform,
                opts,

                true
            );

            PlayerChips.Add(data);
          }
          UpdateMyBetOnOption(bet.betOption, piece);
        }
        uiManager.SetNetBetPanel(amount);

      }
    }
  }

  internal void ClearAllBets()
  {
    CancleBets();
    foreach (var chips in OtherPlayerChips)
    {
      Chip c = chips.chip.GetComponent<Chip>();
      if (chips != null)
        ReturnChip(c);
    }
    OtherPlayerChips.Clear();
    ResetAllBetUI();
    uiManager.SetNetBetPanel(0);
  }
  internal void CancleBets()
  {

    ResetPlayerBetUI();

    uiManager.SetNetBetPanel(0);


    foreach (var chips in PlayerChips)
    {
      Chip c = chips.chip.GetComponent<Chip>();
      if (chips != null)
        ReturnChip(c);
    }
    PlayerChips.Clear();
  }
  internal void ResetPlayerBetUI()
  {

    foreach (var opt in AllOptions)
    {
      opt.ResetPlayerUI();
    }
  }

  internal void UnduBets(string betId)
  {
    for (int i = PlayerChips.Count - 1; i >= 0; i--)
    {
      var item = PlayerChips[i];

      if (item.betId == betId)
      {
        int amount = item.amount;
        string option = socketManager.BetChipData.payload.betOption;

        // 1. Return chip
        if (item.chip != null)
        {
          Chip c = item.chip.GetComponent<Chip>();
          ReturnChip(c);
        }

        // 2. Remove from list
        PlayerChips.RemoveAt(i);

        // 3. UPDATE UI
        UpdateMyBetOnOption(option, -amount);
        uiManager.SetNetBetPanel(-amount);

        // 4. Refresh chip display ← ADD THIS LINE
        FindOption(option)?.RefreshChipDisplay();
      }
    }
  }
  internal void UnduBets(string betId, string betOption, int refundAmount)
  {
    // Find and remove the chips with matching betId
    for (int i = PlayerChips.Count - 1; i >= 0; i--)
    {
      var item = PlayerChips[i];

      if (item.betId == betId)
      {
        // 1. Return chip to pool
        if (item.chip != null)
        {
          Chip c = item.chip.GetComponent<Chip>();
          ReturnChip(c);
        }

        // 2. Remove from list
        PlayerChips.RemoveAt(i);
      }
    }

    // 3. Update UI - use AddPlayerChip with negative amount to update both text AND chip display
    OptionPrefab option = FindOption(betOption);
    if (option != null)
    {
      option.AddPlayerChip(-refundAmount, null);
    }

    uiManager.SetNetBetPanel(-refundAmount);
  }
  // internal void UnduBets(int amount, string betOpt)
  // {
  //     OptionPrefab opt = FindOption(betOpt);
  //     opt.AddPlayerChip(-amount, PlayerChipSprite[0]);
  // }
  #endregion



  #region chip Pool







  GameObject AddChip()
  {
    var go = Instantiate(chipPrefab, poolParent);
    go.SetActive(false);
    pool.Add(go);
    return go;
  }

  // internal Chip GetChip()
  // {
  //     // Try to reuse inactive chip
  //     for (int i = 0; i < pool.Count; i++)
  //     {
  //         if (!pool[i].activeInHierarchy)
  //         {
  //             pool[i].SetActive(true);
  //             return pool[i].GetComponent<Chip>();
  //         }
  //     }

  //     // If all are in use, expand pool BEFORE returning
  //     for (int i = 0; i < 10; i++)
  //         AddChip();

  //     // Guaranteed available now
  //     pool[^1].SetActive(true);
  //     return pool[^1].GetComponent<Chip>();
  // }
  int FreeChipCount()
  {
    int free = 0;
    for (int i = 0; i < pool.Count; i++)
      if (!pool[i].activeInHierarchy)
        free++;

    return free;
  }
  void EnsureFreeChips()
  {
    if (FreeChipCount() >= 5)
      return;

    int need = 5 - FreeChipCount();
    int expandCount = Mathf.Max(need, 10);

    for (int i = 0; i < expandCount; i++)
      AddChip();
  }
  internal Chip GetChip()
  {
    // Make sure free chips exist BEFORE using
    EnsureFreeChips();

    for (int i = 0; i < pool.Count; i++)
    {
      if (!pool[i].activeInHierarchy)
      {
        pool[i].SetActive(true);
        return pool[i].GetComponent<Chip>();
      }
    }

    return null; // logically unreachable
  }
  internal void ReturnChip(Chip chip)
  {
    chip.gameObject.SetActive(false);
    chip.transform.SetParent(poolParent);
  }





  #endregion






  #region helper
  List<int> BreakAmountIntoChips(int amount, List<int> chipOptions)
  {
    // ✅ Make a COPY so original list is not modified
    List<int> sortedChips = new List<int>(chipOptions);

    // Sort descending
    sortedChips.Sort((a, b) => b.CompareTo(a));

    List<int> results = new List<int>();

    foreach (int chip in sortedChips)
    {
      while (amount >= chip)
      {
        amount -= chip;
        results.Add(chip);
      }
    }

    return results;
  }


  internal void UpdatePlayerbalance(string balance)
  {
    uiManager.MainPlayers.playerBalence.text = "Rs" + balance;
  }
  Sprite findChipSprite(int amount, List<int> betOptions)
  {
    for (int i = 0; i < betOptions.Count; i++)
    {
      if (betOptions[i] == amount)
      {
        return PlayerChipSprite[i];
      }
    }
    return PlayerChipSprite[0];

  }
  Sprite findOtherPlayerChipSprite(int amount, List<int> betOptions)
  {
    for (int i = 0; i < betOptions.Count; i++)
    {
      if (betOptions[i] == amount)
      {
        return otherChipSprite[i];
      }
    }
    return null;

  }
  int findChipindex(int amount, List<int> betOptions)
  {
    for (int i = 0; i < betOptions.Count; i++)
    {
      if (betOptions[i] == amount)
      {
        return i;
      }
    }
    return 0;

  }
  internal List<int> FindRoom()
  {
    string room = currentRoom;
    List<int> data = null;

    switch (room)
    {
      case "level_1":
        data = socketManager.initialData.bets.level_1;
        break;

      case "level_2":
        data = socketManager.initialData.bets.level_2;
        break;

      case "level_3":
        data = socketManager.initialData.bets.level_3;
        break;

      case "level_4":
        data = socketManager.initialData.bets.level_4;
        break;
      case "level_5":
        data = socketManager.initialData.bets.level_5;
        break;
      case "level_6":
        data = socketManager.initialData.bets.level_6;
        break;
    }
    return data;
  }

  internal OptionPrefab FindOption(string opt)
  {
    // Debug.Log("789 _____________" + opt);
    switch (opt)
    {
      case "s_2":
        return AllOptions[3];

      case "s_3":
        return AllOptions[4];

      case "s_4":
        return AllOptions[5];

      case "s_5":
        return AllOptions[6];

      case "s_6":
        return AllOptions[7];

      case "s_8":
        return AllOptions[8];

      case "s_9":
        return AllOptions[9];

      case "s_10":
        return AllOptions[10];

      case "s_11":
        return AllOptions[11];

      case "s_12":
        return AllOptions[12];

      case "number_8_12":
        return AllOptions[0];

      case "number_7":
        return AllOptions[1];

      case "number_2_6":
        return AllOptions[2];

      default:
        return AllOptions[0];

    }
  }

  internal void PlayPopup(string popupText)
  {
    // BlockerText.text = popupText;

    // if (animRoutine != null)
    //     StopCoroutine(animRoutine);

    // animRoutine = StartCoroutine(PopupRoutine());
  }
  internal void playtheCoin(string winamount)
  {

    // Kill previous animation if running
    coinTween?.Kill();

    PlayerWinAnimation.gameObject.SetActive(true);
    PlayerWinAnimation.StopAnimation();
    PlayerWinAnimation.StartAnimation();
  }

  private IEnumerator PopupRoutine()
  {
    BlockerObj.transform.position = popStart.position;
    yield return Move(BlockerObj.transform, popCenter.position, moveDuration);

    yield return new WaitForSeconds(holdDuration);

    yield return Move(BlockerObj.transform, popEnd.position, moveDuration);
  }

  private IEnumerator Move(Transform target, Vector3 toPos, float duration)
  {
    Vector3 fromPos = target.position;
    float t = 0f;

    while (t < duration)
    {
      t += UnityEngine.Time.deltaTime;
      float lerp = t / duration;
      target.position = Vector3.Lerp(fromPos, toPos, lerp);
      yield return null;
    }

    target.position = toPos;
  }

  internal void UpdateMyBetOnOption(string opt, int amount)
  {
    OptionPrefab option = FindOption(opt);
    if (option == null) return;

    option.MyBetObj.SetActive(true);

    int prev = 0;
    int.TryParse(option.MyBetText.text, out prev);

    int newAmount = prev + amount;
    if (newAmount <= 0) option.MyBetObj.SetActive(false);
    option.MyBetText.text = newAmount.ToString();
  }


  internal void ResetBetUI(OptionPrefab option)
  {
    if (option == null) return;

    // Hide My Bet
    option.MyBetObj.SetActive(false);
    option.MyBetText.text = "0";


  }
  internal void ResetAllBetUI()
  {


    foreach (var opt in AllOptions)
    {
      opt.ResetOptionUI();
    }

  }


  #endregion
  internal void SetPlayerCountOnReturn(Lobby lobby, double playerbalance)
  {

  }
  internal void ManageBonus(int amount, string option)
  {
    Transform spawnPos = FindOption(option).gameObject.transform;
    GameObject obj = Instantiate(ExtarPayObject, Bonusparent.transform);
    obj.transform.position = spawnPos.position;
    obj.GetComponent<BonusPrefab>().SetNumberWithX(amount);
  }
  public void ResetBonusUI()
  {
    LeaderboadrdShow.Clear();
    LeaderboadrdShow.TrimExcess();
    Transform parent = Bonusparent.transform;

    for (int i = parent.childCount - 1; i >= 0; i--)
    {
      Destroy(parent.GetChild(i).gameObject);
    }
  }
}

[System.Serializable]
public class ChipData
{
  public string betId;
  public string username;
  public int amount;

  public OptionPrefab betOptionK;
  public GameObject chip;
}
