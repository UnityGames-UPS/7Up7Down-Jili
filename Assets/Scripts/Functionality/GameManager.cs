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
  // Betting locks this many seconds before the server's bettingEndTime
  [SerializeField] private float betLockLead = 0.5f;

  [Header("Chip")]
  [SerializeField] private ChipManager chipManager;


  [Header("popup")]
  // Raycast-blocking overlay over the bet views while betting is closed
  [SerializeField] private GameObject BetBlocker;
  [SerializeField] private MessagePopup messagePopup;
  private bool bettingClosed;
  // Player had chips down when betting last closed: offers Again next round
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
  // Parent of the three banners above; kept click-through so they never cover the bet views
  [SerializeField] private CanvasGroup roundBannerGroup;
  // Dims the table behind a banner; the alpha set in the scene is the dimmed alpha
  [SerializeField] private Image roundBannerDim;
  [SerializeField] private float bannerDimFade = 0.15f;
  // How far through the Extra Pay banner the multipliers appear
  [Range(0f, 1f)] [SerializeField] private float extraPayRevealAt = 0.7f;
  // [SerializeField] internal List<int> LeaderboadrdShow = new List<int>();
  // Off while the scene has no leaderboard UI; data is still received and logged
  [SerializeField] internal bool showLeaderboard = false;

  [Header("Win Animation")]
  // Above the table UI; each winning option's win layer is moved here while it plays
  [SerializeField] private Transform winAnimLayer;
  [SerializeField] private WinAnimationSettings winSettings = new WinAnimationSettings();

  [Header("Profit Text")]
  // Floats up from where it sits in the scene when the player's winnings reach the avatar
  [SerializeField] private TMP_Text profitText;
  [SerializeField] private ProfitTextSettings profitSettings = new ProfitTextSettings();
  private Vector2 profitTextHome;

  [Header("Extra Pay ")]
  [SerializeField] private BonusManager bonusManager;
  // Received from game:bonus, waiting for the Extra Pay banner to reveal it
  private Dictionary<string, int> pendingBonus;
  private ImageAnimation activeBanner;
  private float bannerDimAlpha;

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
  // This round's winning options, known once the dice have landed
  private readonly List<BetOptionView> winningOptions = new List<BetOptionView>();


  void Awake()
  {
    foreach (var option in betOptions) option.Clicked += OnBetOptionClicked;
    BetBlocker.SetActive(false);
    if (roundBannerGroup) roundBannerGroup.blocksRaycasts = false;
    if (roundBannerDim) bannerDimAlpha = roundBannerDim.color.a;
    HideRoundBanners();
    if (profitText)
    {
      profitTextHome = profitText.rectTransform.anchoredPosition;
      profitText.gameObject.SetActive(false);
    }
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

    // Auto takes over from Again only while the player has chips down
    bool offerAuto = hasBets;
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

  private string timerRoundId;
  private bool timerRunning;
  // Time.realtimeSinceStartup at which betting locks
  private float betDeadline;
  private float betDuration;
  private int shownSeconds;

  // The countdown runs locally; round_start and every timer tick only correct it
  internal void SyncBetTimer(string roundId, long serverTime, long bettingEndTime)
  {
    float now = Time.realtimeSinceStartup;
    float deadline = now + (bettingEndTime - serverTime) / 1000f - betLockLead;

    if (roundId == timerRoundId)
    {
      // A delayed packet can only overstate the time left, so the earliest deadline is the truest
      betDeadline = Mathf.Min(betDeadline, deadline);
      return;
    }

    timerRoundId = roundId;
    betDeadline = deadline;
    // Full length even when joining mid-round, so the ring starts part-drained
    float fullDuration = socketManager.initialData != null ? socketManager.initialData.roundInterval / 1000f - betLockLead : 0f;
    betDuration = Mathf.Max(fullDuration, deadline - now, 0.01f);
    shownSeconds = -1;
    timerRunning = true;

    SetBettingClosed(false);
    foreach (var option in betOptions) option.SetDimmed(false);
    CircleTimerFill.transform.parent.gameObject.SetActive(true);
  }

  void Update()
  {
    if (!timerRunning) return;

    float remaining = Mathf.Max(0f, betDeadline - Time.realtimeSinceStartup);
    CircleTimerFill.fillAmount = remaining / betDuration;

    int seconds = Mathf.CeilToInt(remaining);
    if (seconds == shownSeconds) return;
    shownSeconds = seconds;
    Timer_text.text = seconds.ToString();

    if (seconds == 5) audioManager.PlayGirlAudio("timeisrunning");
    else if (seconds == 0) LockBetting();
  }

  void LockBetting()
  {
    timerRunning = false;
    audioManager.PlayGirlAudio("nomorebets");
    SetBettingClosed(true);
    foreach (var option in betOptions) option.SetDimmed(!option.HasPlayerBet);
    PlayRoundBanner(Betlocked, TryPlayExtraPay);
  }

  internal void ResetTimer()
  {
    timerRunning = false;
    timerRoundId = null;
    CircleTimerFill.fillAmount = 1f;
  }

  // One banner at a time; the dim stays up when onDone chains straight into the next banner
  void PlayRoundBanner(ImageAnimation banner, System.Action onDone = null, float markAt = 1f, System.Action onMark = null)
  {
    foreach (var other in new[] { Plesebetnow, Betlocked, Extrapay })
    {
      if (other != banner) other.gameObject.SetActive(false);
    }

    ShowBannerDim(true);
    activeBanner = banner;
    banner.gameObject.SetActive(true);
    banner.Play(markAt, onMark, () =>
    {
      banner.gameObject.SetActive(false);
      activeBanner = null;
      onDone?.Invoke();
      if (activeBanner == null) ShowBannerDim(false);
    });
  }

  void HideRoundBanners()
  {
    Plesebetnow.gameObject.SetActive(false);
    Betlocked.gameObject.SetActive(false);
    Extrapay.gameObject.SetActive(false);
    activeBanner = null;

    if (!roundBannerDim) return;
    roundBannerDim.DOKill();
    SetBannerDimAlpha(0f);
    roundBannerDim.gameObject.SetActive(false);
  }

  void ShowBannerDim(bool show)
  {
    if (!roundBannerDim) return;

    roundBannerDim.DOKill();
    if (show) roundBannerDim.gameObject.SetActive(true);
    roundBannerDim.DOFade(show ? bannerDimAlpha : 0f, bannerDimFade).OnComplete(() =>
    {
      if (!show) roundBannerDim.gameObject.SetActive(false);
    });
  }

  void SetBannerDimAlpha(float alpha)
  {
    Color color = roundBannerDim.color;
    color.a = alpha;
    roundBannerDim.color = color;
  }

  internal void OnBonus(Dictionary<string, int> bonus)
  {
    pendingBonus = bonus;
    TryPlayExtraPay();
  }

  // Extra Pay follows Bet Locked, whichever of "banner finished" and "bonus arrived" comes last
  void TryPlayExtraPay()
  {
    if (pendingBonus == null || !bettingClosed) return;
    if (activeBanner == Betlocked || activeBanner == Extrapay) return;
    PlayRoundBanner(Extrapay, null, extraPayRevealAt, RevealBonus);
  }

  void RevealBonus()
  {
    if (pendingBonus == null) return;

    int order = 0;
    foreach (var bonus in pendingBonus)
    {
      if (bonusManager && TryGetOption(bonus.Key, out BetOptionView option))
        bonusManager.Show(option.BonusAnchor, bonus.Value, order++, option.BetType == "main_bets");
    }
    pendingBonus = null;
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

    winningOptions.Clear();
    StopWinAnimations();
    pendingBonus = null;
    if (bonusManager) bonusManager.ReturnAllItemsToPool();
    // LeaderboadrdShow.Clear();
  }

  internal void ManageResult(DiceResultEvent diceResult)
  {
    // Dice must not be covered, and a bonus the banner had no time to reveal still has to show
    HideRoundBanners();
    RevealBonus();
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
    winningOptions.Add(option);
    option.SetDimmed(false);
  }

  internal void ManagePayouts(CashoutEvent cashout)
  {
    StartCoroutine(ManagePayout(cashout));
  }
  IEnumerator ManagePayout(CashoutEvent cashout)
  {
    // Read now: losing options clear their bets while they shrink
    int staked = betOptions.Sum(o => o.PlayerBet);
    double balanceBefore = socketManager.playerdata.balance;

    // Winners stay lit so the dealer's chips land on a bright spot
    foreach (var option in betOptions)
    {
      bool lost = !winningOptions.Contains(option);
      option.SetDimmed(lost);
      if (lost) option.ShrinkBets(chipManager.LoseShrinkDuration, RefreshBetButtons);
      if (bonusManager) bonusManager.Resolve(option.BonusAnchor, !lost);
    }

    ManagePayments(cashout.payouts);
    yield return new WaitForSeconds(2f);
    DistributePayouts(cashout.payouts, staked, balanceBefore);
    foreach (var option in betOptions)
    {
      option.ClearAllBets();
      option.SetDimmed(false);
    }
    RefreshBetButtons();
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
    if (amount <= 0 || !TryGetOption(betKey, out BetOptionView option)) return;

    // betWins excludes the stake, so the landed chip is stake + win
    if (isPlayer)
    {
      int total = option.PlayerBet + amount;
      option.PlayWin(winAnimLayer, winSettings);
      chipManager.PayFromDealer(option.PlayerReferenceChip, amount, true,
        () => option.ShowPlayerWin(total, chipManager.GetSprite(total, true)));
    }
    else
    {
      int total = option.OpponentBet + amount;
      chipManager.PayFromDealer(option.OpponentReferenceChip, amount, false,
        () => option.ShowOpponentWin(total, chipManager.GetSprite(total, false)));
    }
  }
  void DistributePayouts(List<Payout> payouts, int staked, double balanceBefore)
  {
    string currentPlayer = uiManager.MainPlayers.playername.text;

    foreach (var payout in payouts)
    {
      if (payout.betWins == null) continue;

      Transform target = FindPlayerTransform(payout.username);
      if (target == null) target = TotalPlayer_text.transform;

      bool isCurrentPlayer = payout.username == currentPlayer;

      System.Action onArrived = null;
      if (isCurrentPlayer)
      {
        currentWin = payout.balance - balanceBefore;
        double profit = currentWin - staked;
        bool shown = false;
        // Several chips can land together; the text plays for the first
        if (profit > 0) onArrived = () =>
        {
          if (shown) return;
          shown = true;
          PlayProfitText(profit);
        };
      }

      foreach (var bet in payout.betWins)
      {
        if (bet.Value <= 0 || !TryGetOption(bet.Key, out BetOptionView option)) continue;

        ChipReference reference = isCurrentPlayer ? option.PlayerReferenceChip : option.OpponentReferenceChip;
        // The player's own stake leaves the table with the win
        int collected = isCurrentPlayer ? option.PlayerBet + bet.Value : bet.Value;
        chipManager.CollectToPlayer(reference, target.position, collected, isCurrentPlayer, onArrived);
      }

      if (isCurrentPlayer)
      {
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

  void PlayProfitText(double profit)
  {
    if (!profitText) return;

    RectTransform rect = profitText.rectTransform;
    DOTween.Kill(profitText);
    rect.anchoredPosition = profitTextHome;
    profitText.alpha = 0f;
    profitText.text = "+" + profit.ToString("0.##");
    profitText.gameObject.SetActive(true);

    ProfitTextSettings s = profitSettings;
    float riseY = profitTextHome.y + s.riseDistance;
    float driftY = riseY + s.driftDistance;

    Sequence seq = DOTween.Sequence().SetTarget(profitText);
    seq.Append(rect.DOAnchorPosY(riseY, s.riseDuration).SetEase(Ease.OutQuad));
    seq.Join(profitText.DOFade(1f, s.riseDuration));
    seq.Append(rect.DOAnchorPosY(driftY, s.driftDuration).SetEase(Ease.Linear));
    seq.Append(rect.DOAnchorPosY(driftY + s.exitDistance, s.exitDuration).SetEase(Ease.InQuad));
    seq.Join(profitText.DOFade(0f, s.exitDuration));
    seq.OnComplete(() => profitText.gameObject.SetActive(false));
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
    // HighlightLeaderboardBets();
  }

  // Highlights bets made by the leaderboard players the user tapped
  // void HighlightLeaderboardBets()
  // {
  //   if (!showLeaderboard) return;
  //   foreach (var bet in opponentBets)
  //   {
  //     foreach (int index in LeaderboadrdShow)
  //     {
  //       if (bet.username == uiManager.WinnerPlayers[index].playername.text) bet.option.ShowLeaderboardHighlight();
  //     }
  //   }
  // }
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
    PlayerWinAnimation.Play();
  }


  #endregion
  internal void SetPlayerCountOnReturn(Lobby lobby, double playerbalance)
  {

  }
}

// Tuning for the floating "+profit" text; distances are in the text's anchored units
[System.Serializable]
public class ProfitTextSettings
{
  [Header("Rise (fade in)")]
  public float riseDistance = 60f;
  public float riseDuration = 0.2f;

  [Header("Drift")]
  public float driftDistance = 15f;
  public float driftDuration = 0.8f;

  [Header("Exit (fade out)")]
  public float exitDistance = 80f;
  public float exitDuration = 0.25f;
}
