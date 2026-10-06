using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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

  [Header("Bet Options")]
  // Order must match the server's betOptions: 8-12, 7, 2-6, then totals 2-6 and 8-12
  [FormerlySerializedAs("AllOptions")]
  [SerializeField] private List<BetOptionView> betOptions;
  private readonly Dictionary<string, BetOptionView> betOptionsByKey = new Dictionary<string, BetOptionView>();

  [Header("Jili")]
  [SerializeField] private TMP_Text Timer_text;
  [SerializeField] private Image CircleTimerFill;

  [Header("Chip")]
  [SerializeField] private ChipManager chipManager;


  [Header("popup")]
  // Raycast-blocking overlay over the bet views while betting is closed
  [SerializeField] private GameObject BetBlocker;
  [SerializeField] private MessagePopup messagePopup;
  private bool bettingClosed;
  // Player had chips down when betting last closed: offers Auto now and Again next round
  private bool hadBetsAtClose;
  // Again was pressed and its reply has not arrived yet
  private bool repeatPending;

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
  // Parent of the two banners above; kept click-through so they never cover the bet views
  [SerializeField] private CanvasGroup roundBannerGroup;
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

  private Vector3 endPos = new Vector3(0, -10, 0);


  private readonly List<(string betId, string username, BetOptionView option)> opponentBets =
    new List<(string betId, string username, BetOptionView option)>();


  void Awake()
  {
    foreach (var option in betOptions) option.Clicked += OnBetOptionClicked;
    BetBlocker.SetActive(false);
    if (roundBannerGroup) roundBannerGroup.blocksRaycasts = false;
  }
  private void Start()
  {
    BetCounter = 0;
    LoadingPage.SetActive(false);
    GamePage.SetActive(true);
    RefreshBetButtons();
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
    var main = socketManager.initialData.wagers.main_bets;
    var side = socketManager.initialData.wagers.side_bets;

    // Same order as betOptions and the server's betOptions array
    string[] labels = { "8-12", "7", "2-6", "2", "3", "4", "5", "6", "8", "9", "10", "11", "12" };
    List<int>[] payouts =
    {
      main.number_8_12.payout, main.number_7.payout, main.number_2_6.payout,
      side.s_2.payout, side.s_3.payout, side.s_4.payout, side.s_5.payout, side.s_6.payout,
      side.s_8.payout, side.s_9.payout, side.s_10.payout, side.s_11.payout, side.s_12.payout
    };

    betOptionsByKey.Clear();
    for (int i = 0; i < betOptions.Count; i++)
    {
      string betKey = socketManager.initialData.betOptions[i];
      betOptions[i].Setup(betKey, i < 3 ? "main_bets" : "side_bets", labels[i], payouts[i]);
      betOptionsByKey[betKey] = betOptions[i];
    }
  }

  internal void SetCoinData()
  {
    ResetCoinsToDefault();
    TotalPlayer_text.text = socketManager.roomData.playerCount.ToString();

    List<int> data = CurrentRoomChips();
    if (data == null) return;

    chipManager.SetDenominations(data);

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
    uiManager.coinSelector.chipImage.sprite = chipManager.GetPlayerSprite(0);

    for (int i = 0; i < uiManager.Coins.Count; i++)
    {
      uiManager.Coins[i].chipImage.sprite = chipManager.GetPlayerSprite(i);
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

    // string mainPlayerName = uiManager.MainPlayers.playername.text;
    // Sprite mainPlayerIcon = uiManager.MainPlayers.PlayerIcon.sprite;

    // // One WinnerPlayers slot per top winner; unused slots stay hidden
    // foreach (var item in uiManager.WinnerPlayers)
    //   item.gameObject.SetActive(false);

    // if (leaderboard.winners == null) return;

    // int winnersCount = Mathf.Min(leaderboard.winners.Count, uiManager.WinnerPlayers.Count);

    // for (int i = 0; i < winnersCount; i++)
    // {
    //   Winner win = leaderboard.winners[i];

    //   // Other players have no avatar from the server, so they get a random icon
    //   Sprite iconToUse = (win.username == mainPlayerName)
    //       ? mainPlayerIcon
    //       : uiManager.UserIcons[UnityEngine.Random.Range(0, uiManager.UserIcons.Count)];

    //   uiManager.WinnerPlayers[i].SetData(
    //       win.username,
    //       win.totalWins.ToString(),
    //       iconToUse
    //   );

    //   uiManager.WinnerPlayers[i].gameObject.SetActive(true);
    // }
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


  void SetBettingClosed(bool closed)
  {
    bettingClosed = closed;
    BetBlocker.SetActive(closed);
    if (closed)
    {
      if (messagePopup) messagePopup.Hide();
      hadBetsAtClose = PlayerHasBets();
      if (!hadBetsAtClose) isAuto = false;
    }
    RefreshBetButtons();
  }

  bool PlayerHasBets() => betOptions.Any(o => o.HasPlayerBet);

  void RefreshBetButtons()
  {
    bool hasBets = PlayerHasBets();
    uiManager.SetBetActionButtons(!bettingClosed && hasBets);

    // Auto takes over from Again as soon as the player has chips down
    bool offerAuto = hasBets || (bettingClosed && hadBetsAtClose);
    bool canRepeat = !bettingClosed && hadBetsAtClose && !repeatPending;
    uiManager.SetRepeatSlot(isAuto, offerAuto, canRepeat);
  }

  internal void RequestRepeat()
  {
    if (repeatPending) return;
    repeatPending = true;
    RefreshBetButtons();
    socketManager.SendRepeat();
  }

  internal void OnRepeatReply()
  {
    repeatPending = false;
    RefreshBetButtons();
  }

  internal void SetAuto(bool on)
  {
    isAuto = on;
    RefreshBetButtons();
  }

  internal void SetBetTimer(int timeRemaining)
  {
    SetBettingClosed(false);
    foreach (var option in betOptions) option.SetDimmed(false);

    int time = timeRemaining - 1;
    StartBetTimer(time);

    if (time == 5)
    {
      audioManager.PlayGirlAudio("timeisrunning");
    }
    else if (time == 0)
    {
      audioManager.PlayGirlAudio("nomorebets");

      SetBettingClosed(true);
      foreach (var option in betOptions) option.SetDimmed(!option.HasPlayerBet);
      PlayRoundBanner(Betlocked);
    }
  }

  void PlayRoundBanner(ImageAnimation banner)
  {
    if (roundBannerGroup) roundBannerGroup.blocksRaycasts = false;
    banner.StopAnimation();
    banner.gameObject.SetActive(true);
    banner.StartAnimation();
  }

  internal void OnGameLoopStart()
  {
    repeatPending = false;
    SetBettingClosed(false);
    chipManager.ReturnAllItemsToPool();
    opponentBets.Clear();
    ResetAllBetOptions();
    if (messagePopup) messagePopup.Hide();
    audioManager.PlayWLAudio("betNow");
    PlayRoundBanner(Plesebetnow);

    if (isAuto) RequestRepeat();
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
  internal void ManageResult(DiceResultEvent diceResult)
  {
    uiManager.HideBetLimitPanel();
    audioManager.PlayWLAudio("shakingDice");
    DiceAnimator.StartAnimation(FirstDiceSprite[diceResult.dice1 - 1], SecondDiceSprite[diceResult.dice2 - 1]);
    CircleTimerFill.gameObject.transform.parent.gameObject.SetActive(false);
    ResetTimer();
    StartCoroutine(ManageAfterResult(diceResult));


  }
  IEnumerator ManageAfterResult(DiceResultEvent diceResult)
  {
    yield return new WaitForSeconds(3f);
    int total = diceResult.dice1 + diceResult.dice2;
    audioManager.StopWLAaudio();
    uiManager.UpdateStats(total, uiManager.DiceSprites[diceResult.dice1 - 1], uiManager.DiceSprites[diceResult.dice2 - 1], true);

    // betOptions order: 8-12, 7, 2-6, then totals 2-6 and 8-12
    if (total == 7)
    {
      ShowWin(betOptions[1]);
    }
    else if (total < 7)
    {
      ShowWin(betOptions[2]);
      ShowWin(betOptions[total + 1]);
    }
    else
    {
      ShowWin(betOptions[0]);
      ShowWin(betOptions[total]);
    }
  }

  void ShowWin(BetOptionView option)
  {
    option.SetDimmed(false);
    if (option.HasPlayerBet) option.PlayWin();
  }

  internal void ManagePayouts(CashoutEvent cashout)
  {
    StartCoroutine(ManagePayout(cashout));
  }
  IEnumerator ManagePayout(CashoutEvent cashout)
  {
    foreach (var option in betOptions) option.SetDimmed(true);

    yield return new WaitForSeconds(2f);
    ManagePayments(cashout.payouts);
    yield return new WaitForSeconds(2f);
    DistributePayouts(cashout.payouts);
    foreach (var option in betOptions)
    {
      option.ClearAllBets();
      option.SetDimmed(false);
    }
    SetOtherplayerData(cashout.leaderboards);
    uiManager.SetNetBetPanel(0);
    yield return new WaitForSeconds(2f);
    if (isSinglePlayer && isAuto) socketManager.SendRepeat();
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

    foreach (var bet in playerBets) PayOntoOption(bet.Key, bet.Value, true);
    foreach (var bet in otherBets) PayOntoOption(bet.Key, bet.Value, false);
  }

  // Dealer pays winnings onto the option before they are collected by the winners
  void PayOntoOption(string betKey, int amount, bool isPlayer)
  {
    if (!TryGetOption(betKey, out BetOptionView option)) return;

    if (isPlayer)
    {
      chipManager.PayFromDealer(option.PlayerReferenceChip, amount, true, chipAmount =>
      {
        option.AddPlayerBet(chipAmount);
        RefreshPlayerChip(option);
      });
    }
    else
    {
      chipManager.PayFromDealer(option.OpponentReferenceChip, amount, false, chipAmount =>
      {
        option.AddOpponentBet(chipAmount);
        RefreshOpponentChip(option);
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

      foreach (var bet in payout.betWins)
      {
        if (!TryGetOption(bet.Key, out BetOptionView option)) continue;

        ChipReference reference = isCurrentPlayer ? option.PlayerReferenceChip : option.OpponentReferenceChip;
        chipManager.CollectToPlayer(reference, target.position, bet.Value, isCurrentPlayer);
      }

      // Update player balance for current player
      if (isCurrentPlayer)
      {
        // Parse balance - remove any non-numeric characters (like "Rs")
        string balanceText = uiManager.MainPlayers.playerBalence.text;
        string numericBalance = new string(balanceText.Where(c => char.IsDigit(c) || c == '.').ToArray());

        double oldBalance = double.Parse(numericBalance, System.Globalization.CultureInfo.InvariantCulture);
        double newBalance = payout.balance;
        currentWin = newBalance - oldBalance;
        uiManager.MainPlayers.playerBalence.text = payout.balance.ToString();
        socketManager.playerdata.balance = payout.balance;

        if (currentWin >= 1)
        {
          int winInt = Mathf.FloorToInt((float)currentWin);
          playtheCoin("+" + winInt);
        }
      }
    }
  }


  internal void ResetAllBetOptions()
  {
    foreach (var option in betOptions) option.ClearAllBets();
    RefreshBetButtons();
  }

  #endregion




  #region  beting
  void OnBetOptionClicked(BetOptionView option)
  {
    if (bettingClosed) return;
    if (double.TryParse(uiManager.coinSelector.chipAmount, out double chipValue) && chipValue > socketManager.playerdata.balance)
    {
      ShowMessage("Low Balance");
      return;
    }
    socketManager.BetPlaced(uiManager.coinSelector.chipIndex, option.BetType, option.BetKey);
  }

  // Server said no: a late bet just re-locks the table, anything else is shown to the player
  internal void OnBetRejected(string message)
  {
    if (string.IsNullOrEmpty(message)) return;

    if (message.IndexOf("betting closed", System.StringComparison.OrdinalIgnoreCase) >= 0)
    {
      SetBettingClosed(true);
      return;
    }
    ShowMessage(message);
  }

  void ShowMessage(string message)
  {
    if (messagePopup) messagePopup.Show(message);
  }

  internal void OnPlayerBetPlaced(PlaceBetPayload bet)
  {
    if (!TryGetOption(bet.betOption, out BetOptionView option)) return;

    uiManager.SetChipoption(true);
    PlacePlayerBet(option, bet.amount);
    RefreshBetButtons();
  }

  // Totals change now; the reference chip catches up when the flying chip lands
  void PlacePlayerBet(BetOptionView option, int amount)
  {
    option.AddPlayerBet(amount);
    uiManager.SetNetBetPanel(amount);
    chipManager.DropPlayerChips(option.PlayerReferenceChip, amount, () => RefreshPlayerChip(option));
  }

  internal void OnOpponentBetPlaced(Bet bet)
  {
    if (bet.username == uiManager.MainPlayers.playername.text) return;
    if (!TryGetOption(bet.betOption, out BetOptionView option)) return;

    // A negative amount is that player's bet being cancelled
    if (bet.amount < 0)
    {
      option.AddOpponentBet(bet.amount);
      RefreshOpponentChip(option);
      opponentBets.RemoveAll(b => b.betId == bet.betId);
      return;
    }

    option.AddOpponentBet(bet.amount);
    opponentBets.Add((bet.betId, bet.username, option));
    chipManager.FlyOpponentChips(option.OpponentReferenceChip, bet.amount, () => RefreshOpponentChip(option));
    HighlightLeaderboardBets();
  }

  // Highlights bets made by the leaderboard players the user tapped
  void HighlightLeaderboardBets()
  {
    if (!showLeaderboard) return;
    foreach (var bet in opponentBets)
    {
      foreach (int index in LeaderboadrdShow)
      {
        if (bet.username == uiManager.WinnerPlayers[index].playername.text) bet.option.ShowLeaderboardHighlight();
      }
    }
  }
  #endregion


  #region Manage Result and reset
  void StopWinAnimations()
  {
    foreach (var option in betOptions) option.StopWin();
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
  #endregion






  #region  manage BEt Double bet cancle &&& undo
  internal void OnBetsRepeated(List<Bet> bets)
  {
    foreach (var bet in bets)
    {
      if (TryGetOption(bet.betOption, out BetOptionView option)) PlacePlayerBet(option, bet.amount);
    }
    uiManager.SetChipoption(true);
    RefreshBetButtons();
  }

  internal void OnBetsDoubled(List<DoubledBet> bets)
  {
    foreach (var bet in bets)
    {
      if (bet.delta > 0 && TryGetOption(bet.betOption, out BetOptionView option)) PlacePlayerBet(option, bet.oldAmount);
    }
    RefreshBetButtons();
  }

  internal void OnBetsCancelled()
  {
    foreach (var option in betOptions) option.ClearPlayerBet();
    uiManager.SetNetBetPanel(0);
    StopAutoIfNoBets();
  }

  internal void OnBetUndone(string betOption, int refundAmount)
  {
    if (TryGetOption(betOption, out BetOptionView option))
    {
      option.AddPlayerBet(-refundAmount);
      RefreshPlayerChip(option);
    }
    uiManager.SetNetBetPanel(-refundAmount);
    StopAutoIfNoBets();
  }

  // Taking every chip back leaves auto with nothing to repeat
  void StopAutoIfNoBets()
  {
    if (!PlayerHasBets()) isAuto = false;
    RefreshBetButtons();
  }

  internal void ClearAllBets()
  {
    chipManager.ReturnAllItemsToPool();
    opponentBets.Clear();
    ResetAllBetOptions();
    uiManager.SetNetBetPanel(0);
  }
  #endregion


  #region helper
  internal void UpdatePlayerbalance(string balance)
  {
    uiManager.MainPlayers.playerBalence.text = balance;
  }
  List<int> CurrentRoomChips()
  {
    var bets = socketManager.initialData.bets;
    switch (currentRoom)
    {
      case "level_1": return bets.level_1;
      case "level_2": return bets.level_2;
      case "level_3": return bets.level_3;
      case "level_4": return bets.level_4;
      case "level_5": return bets.level_5;
      case "level_6": return bets.level_6;
      default: return null;
    }
  }

  bool TryGetOption(string betKey, out BetOptionView option)
  {
    if (betKey != null && betOptionsByKey.TryGetValue(betKey, out option)) return true;

    option = null;
    Debug.LogWarning("[BET] unknown bet option: " + betKey);
    return false;
  }

  void RefreshPlayerChip(BetOptionView option) =>
    option.ShowPlayerChip(chipManager.GetSprite(option.PlayerBet, true));

  void RefreshOpponentChip(BetOptionView option) =>
    option.ShowOpponentChip(chipManager.GetSprite(option.OpponentBet, false));

  internal void playtheCoin(string winamount)
  {
    PlayerWinAnimation.gameObject.SetActive(true);
    PlayerWinAnimation.StopAnimation();
    PlayerWinAnimation.StartAnimation();
  }


  #endregion
  internal void SetPlayerCountOnReturn(Lobby lobby, double playerbalance)
  {

  }
  
  internal void ManageBonus(int amount, string option)
  {
    if (!TryGetOption(option, out BetOptionView betOption)) return;
    GameObject obj = Instantiate(ExtarPayObject, Bonusparent.transform);
    obj.transform.position = betOption.transform.position;
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
