using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Best.SocketIO;
using Best.SocketIO.Events;

public class SocketIOManager : MonoBehaviour
{
  [SerializeField] internal GameManager gameManager;
  [SerializeField] internal StartupPage startupPage;
  [SerializeField] private UiManager uiManager;
  [SerializeField] internal LogToggles logs = new LogToggles();

  // Payload of the last JOIN_LEVEL / PLAYER_MODE reply
  internal JoinLevelPayload roomData;
  internal GameData initialData = null;
  internal Player playerdata = null;

  internal bool isResultdone = false;
  // protected string nameSpace="game"; //BackendChanges
  protected string nameSpace = "playground-multiplayer"; //BackendChanges
  private Socket gameSocket; //BackendChanges


  private SocketManager manager;


  protected string SocketURI = null;
  protected string TestSocketURI = "https://devrealtime.dingdinghouse.com/";
  // protected string TestSocketURI = "http://localhost:5000/";
  private string savedToken;

  [SerializeField] internal JSFunctCalls JSManager;
  [SerializeField]
  private string testToken;

  internal bool isLoaded = false;

  internal bool SetInit = false;

  private bool isConnected = false; //Back2 Start
  private bool hasEverConnected = false;

  private float lastPongTime = 0f;
  private float pingInterval = 2f;
  private float pongTimeout = 3f;
  private bool waitingForPong = false;
  private int missedPongs = 0;
  private const int MaxMissedPongs = 5;
  internal bool loadingPageLoading = false;
  internal bool NormalStart = false;
  internal bool DontDisplayDisconected = false;
  private Coroutine PingRoutine; //Back2 end
  [SerializeField] private GameObject RaycastBlocker;

  private void Awake()
  {
    Application.runInBackground = true;
    //Debug.unityLogger.logEnabled = false;
    isLoaded = false;
    SetInit = false;

  }

  private void Start()
  {
    //OpenWebsocket();
    OpenSocket();
  }
  void CloseGame()
  {
    if (logs.connection) Debug.Log("[SOCKET] Closing game");
    StartCoroutine(CloseSocket());
  }


  void ReceiveAuthToken(string jsonData)
  {
    if (logs.connection) Debug.Log("[AUTH] Received: " + jsonData);

    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
    SocketURI = data.socketURL;
    myAuth = data.cookie;
    nameSpace = data.nameSpace;
  }

  string myAuth = null;

  private void OpenSocket()
  {
    //Create and setup SocketOptions
    SocketOptions options = new SocketOptions();
    options.AutoConnect = false;
    options.Reconnection = false;
    options.Timeout = TimeSpan.FromSeconds(3);
    options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket; //BackendChanges

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("authToken");
        StartCoroutine(WaitForAuthToken(options));
#else
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = testToken,
      };
    };
    options.Auth = authFunction;
    savedToken = testToken;
    SetupSocketManager(options);
#endif
  }

  private IEnumerator WaitForAuthToken(SocketOptions options)
  {
    // Wait until myAuth is not null
    while (myAuth == null)
    {
      if (logs.connection) Debug.Log("[AUTH] Waiting for token");
      yield return null;
    }
    while (SocketURI == null)
    {
      if (logs.connection) Debug.Log("[AUTH] Waiting for socket URL");
      yield return null;
    }
    // Once myAuth is set, configure the authFunction
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = myAuth,
      };
    };
    options.Auth = authFunction;
    savedToken = myAuth;
    if (logs.connection) Debug.Log("[AUTH] Token configured: " + myAuth);

    // Proceed with connecting to the server
    SetupSocketManager(options);
    yield return null;
  }

  private void SetupSocketManager(SocketOptions options)
  {
    // Create and setup SocketManager
#if UNITY_EDITOR
    this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
        this.manager = new SocketManager(new Uri(SocketURI), options);
#endif
    if (string.IsNullOrEmpty(nameSpace))
    {  //BackendChanges Start
      gameSocket = this.manager.Socket;
    }
    else
    {
      print("nameSpace: " + nameSpace);
      gameSocket = this.manager.GetSocket("/" + nameSpace);
    }
    // Set subscriptions
    gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    gameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected);
    gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
    //gameSocket.On<string>("message", OnListenEvent);
    gameSocket.On<string>("game:init", ManageInitData);
    gameSocket.On<string>("game:bet_placed", ManageOtherPlayerbets);
    gameSocket.On<string>("game:round_start", OnGameLoopStarted);
    gameSocket.On<string>("game:cashout", OnCashout);
    gameSocket.On<string>("game:lobby_count", OnLobbyCount);
    gameSocket.On<string>("game:betting_timer", OnListenTimeEvent);
    gameSocket.On<string>("game:dice_result", OnDiceResult);
    gameSocket.On<string>("game:bonus", OnGameBonus);
    gameSocket.On<string>("pong", OnPongReceived);
    gameSocket.On<string>("balance:sync", OnBalanceSync);
    manager.Open();
  }

  // Connected event handler implementation
  void OnConnected(ConnectResponse resp) //Back2 Start
  {
    if (logs.connection) Debug.Log("[SOCKET] Connected");

    if (hasEverConnected)
    {
      uiManager.CheckAndClosePopups();
    }

    isConnected = true;
    hasEverConnected = true;
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    SendPing();
  } //Back2 end

  private void OnPongReceived(string data) //Back2 Start
  {
    // Debug.Log("✅ Received pong from server.");
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    //  Debug.Log($"⏱️ Updated last pong time: {lastPongTime}");
    //  Debug.Log($"📦 Pong payload: {data}");
  } //Back2 end

  private void OnDisconnected() //Back2 Start
  {
    Debug.LogWarning("⚠️ Disconnected from server.");
    isConnected = false;
    uiManager.DisconnectionPopup();
    ResetPingRoutine();
  } //Back2 end
  private void OnError(Error err)
  {
    Debug.LogError("[ERROR] Socket error: " + err);
    if (err != null && !string.IsNullOrEmpty(err.message) && err.message.Contains("Session expired"))
    {
      Debug.LogWarning("Session expired detected");
      OnDisconnected();
#if UNITY_WEBGL && !UNITY_EDITOR
      if (JSManager != null) JSManager.SendCustomMessage("session_expired");
#endif
    }
    else
    {
#if UNITY_WEBGL && !UNITY_EDITOR
      if (JSManager != null) JSManager.SendCustomMessage("error");
#endif
    }
  }
  private void OnListenTimeEvent(string data)
  {
    gameManager.OnGameLoaded();
    LogEvent(logs.timer, "[game:betting_timer]", data);
    var timer = JsonUtility.FromJson<BettingTimerEvent>(data);
    gameManager.SetBetTimer(timer.timeRemaining);
  }
  private void OnDiceResult(string data)
  {
    gameManager.OnGameLoaded();
    LogEvent(logs.round, "[game:dice_result]", data);
    gameManager.ManageResult(JsonUtility.FromJson<DiceResultEvent>(data));
  }

  void OnGameBonus(string data)
  {
    LogEvent(logs.round, "[game:bonus]", data);
    var bonusEvent = JsonConvert.DeserializeObject<BonusEvent>(data);
    foreach (var b in bonusEvent.bonus)
    {
      gameManager.ManageBonus(b.Value, b.Key);
    }
  }
  private void OnSocketState(bool state)
  {
    if (state)
    {
      if (logs.connection) Debug.Log("[SOCKET] State: " + state);
    }
    else
    {

    }
  }
  private void OnSocketError(string data)
  {
    Debug.Log("Received error with data: " + data);
  }
  private void OnSocketAlert(string data)
  {
    //        Debug.Log("Received alert with data: " + data);
  }

  private bool hasFocus = true;
  private float focusLostTime = 0f;
  private Coroutine focusCheckRoutine;
  private float maxBackgroundTime = 60f;
  private bool isExiting = false;
  private bool isBeingDestroyed = false;

  private void OnDestroy()
  {
    isBeingDestroyed = true;
  }

  internal void HandleFocusChange(bool focus)
  {
    hasFocus = focus;

    if (!focus)
    {
      focusLostTime = Time.time;
      if (focusCheckRoutine == null && !isExiting && !isBeingDestroyed)
        focusCheckRoutine = StartCoroutine(FocusTimeoutCheck());
    }
    else
    {
      if (focusCheckRoutine != null)
      {
        StopCoroutine(focusCheckRoutine);
        focusCheckRoutine = null;
      }
    }
  }

  private IEnumerator FocusTimeoutCheck()
  {
    while (!hasFocus && !isExiting && !isBeingDestroyed)
    {
      if (Time.time - focusLostTime >= maxBackgroundTime)
      {
        Debug.LogWarning("[SOCKET] Background timeout — closing connection");
        isConnected = false;
        ResetPingRoutine();

        if (gameSocket != null)
        {
          try { gameSocket.Disconnect(); }
          catch (Exception e) { Debug.LogWarning($"[SOCKET] Focus close error: {e.Message}"); }
        }

        if (uiManager != null) uiManager.DisconnectionPopup();
        focusCheckRoutine = null;
        yield break;
      }

      yield return new WaitForSecondsRealtime(1f);
    }

    focusCheckRoutine = null;
  }

  private void OnBalanceSync(string data)
  {
    BalanceSyncPayload syncPayload = JsonConvert.DeserializeObject<BalanceSyncPayload>(data);
    if (syncPayload == null) return;

    if (playerdata == null) playerdata = new Player();
    playerdata.balance = syncPayload.balance;

    if (gameManager != null)
    {
      gameManager.UpdatePlayerbalance(syncPayload.balance.ToString());
    }
  }

  private void OnSocketOtherDevice(string data)
  {
    Debug.Log("Received Device Error with data: " + data);
    uiManager.ADfunction();
  }

  private void SendPing() //Back2 Start
  {
    ResetPingRoutine();
    PingRoutine = StartCoroutine(PingCheck());
  }

  void ResetPingRoutine()
  {
    if (PingRoutine != null)
    {
      StopCoroutine(PingRoutine);
    }
    PingRoutine = null;
  }

  private IEnumerator PingCheck()
  {
    while (true)
    {
      //  Debug.Log($"🟡 PingCheck | waitingForPong: {waitingForPong}, missedPongs: {missedPongs}, timeSinceLastPong: {Time.time - lastPongTime}");

      if (missedPongs == 0)
      {
        uiManager.CheckAndClosePopups();
      }

      // If waiting for pong, and timeout passed
      if (waitingForPong)
      {
        if (missedPongs == 2)
        {
          uiManager.ReconnectionPopup();
        }
        missedPongs++;
        //  Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

        if (missedPongs >= MaxMissedPongs)
        {
          //  Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
          isConnected = false;
          uiManager.DisconnectionPopup();
          yield break;
        }
      }

      // Send next ping
      waitingForPong = true;
      lastPongTime = Time.time;
      //  Debug.Log("📤 Sending ping...");
      SendDataWithNamespace("ping");
      yield return new WaitForSeconds(pingInterval);
    }
  } //Back2 end
  private void AliveRequest()
  {
    SendDataWithNamespace("YES I AM ALIVE");
  }

  private void SendDataWithNamespace(string eventName, string json = null)
  {
    // Send the message
    if (gameSocket != null && gameSocket.IsOpen) //BackendChanges
    {
      if (json != null)
      {
        gameSocket.Emit(eventName, json);
        Debug.Log("JSON data sent: " + json);
      }
      else
      {
        gameSocket.Emit(eventName);
      }
    }
    else
    {
      Debug.LogWarning("Socket is not connected.");
    }
  }

  internal IEnumerator CloseSocket() //Back2 Start
  {
    RaycastBlocker.SetActive(true);
    ResetPingRoutine();

    if (logs.connection) Debug.Log("[SOCKET] Closing");

    manager?.Close();
    manager = null;

    yield return new WaitForSeconds(0.5f);

    if (logs.connection) Debug.Log("[SOCKET] Closed");

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); //Telling the react platform user wants to quit and go back to homepage
#endif
  }

  // Prints the raw JSON, then one "path: value" line per field
  void LogEvent(bool enabled, string tag, string json)
  {
    if (!enabled) return;

    string fields = "";
    try
    {
      if (JToken.Parse(json) is JContainer container)
        fields = string.Join("\n", container.Descendants().OfType<JValue>().Select(v => v.Path + ": " + v));
    }
    catch (Exception)
    {
      fields = "(not JSON)";
    }
    Debug.Log(tag + " " + json + "\n" + fields);
  }

  void ManageInitData(string jsonObject)
  {
    InitEvent myData = null;
    try
    {
      myData = JsonConvert.DeserializeObject<InitEvent>(jsonObject);
    }
    catch (Exception ex)
    {
      Debug.LogError("Failed to deserialize JSON. Exception: " + ex.Message + "\nJSON: " + jsonObject);
      return;
    }

    if (myData == null)
    {
      Debug.LogError("ParseResponse: myData is null. JSON = " + jsonObject);
      return;
    }
    LogEvent(logs.init, "[game:init]", jsonObject);

    string id = myData.id;
    initialData = myData.gameData;
    playerdata = myData.player;

    SetInitialData();

    // The client joins the first level itself so rounds are already running behind the startup page
    if (string.IsNullOrEmpty(gameManager.currentRoom)) gameManager.currentRoom = initialData.levels[0];
    EmitJoinLevel(gameManager.currentRoom);

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnEnter");
#endif
  }

  private void SetInitialData()
  {
    isLoaded = true;
    gameManager.SetInitialData();
    gameManager.SetOptionData();
    RaycastBlocker.SetActive(false);
  }

  private void SendRequest<TPayload>(string type, TPayload payload, Action<string> onAck, bool log)
  {
    string json = JsonConvert.SerializeObject(new Request<TPayload> { type = type, payload = payload });
    LogEvent(log, $"[{type}] sent:", json);
    gameSocket.ExpectAcknowledgement<string>(onAck).Emit("request", json);
  }

  internal void SendModeSelection(string mode)
  {
    StartCoroutine(gameManager.ShowLoadingPage("Switch Mode"));
    SendRequest("PLAYER_MODE", new PlayerModePayload { playerMode = mode }, OnModeChange, logs.requests);
  }

  internal void EmitJoinLevel(string Room) =>
    SendRequest("JOIN_LEVEL", new LevelPayload { level = Room }, OnRoomEnter, logs.requests);

  internal void BetPlaced(int amountIndex, string betType, string betOption) =>
    SendRequest("PLACE_BET", new BetPayload { amountIndex = amountIndex, betType = betType, betOption = betOption }, OnBetAcknowledged, logs.bets);

  internal void SendUndo() => SendRequest("UNDO_BET", new EmptyPayload(), OnUndo, logs.betActions);

  internal void SendRepeat() => SendRequest("REPEAT_BET", new EmptyPayload(), OnRepeat, logs.betActions);

  internal void SendCancle() => SendRequest("CANCEL_BET", new EmptyPayload(), OnCancel, logs.betActions);

  internal void SendDouble() => SendRequest("DOUBLE_BET", new EmptyPayload(), OnDouble, logs.betActions);

  internal void SendStart() => SendRequest("START_GAME", new EmptyPayload(), OnStart, logs.requests);

  internal void SendHome()
  {
    loadingPageLoading = false;
    NormalStart = false;
    DontDisplayDisconected = true;
    SendRequest("HOME", new EmptyPayload(), OnHome, logs.requests);
  }

  internal void SendHistory(int Pages) =>
    SendRequest("BET_HISTORY", new HistoryPayload { page = Pages }, OnHistory, logs.requests);

  void ApplyBalance(double balance)
  {
    playerdata.balance = balance;
    gameManager.UpdatePlayerbalance(balance.ToString());
  }

  void OnHome(string json)
  {
    gameManager.ClearAllBets();
    LogEvent(logs.requests, "[HOME] reply:", json);
    var reply = JsonUtility.FromJson<Reply<HomePayload>>(json);
    playerdata.balance = reply.payload.balance;
    StartCoroutine(gameManager.ShowLoadingPage("Changing Game Hall.."));
    gameManager.GamePage.SetActive(true);
    EmitJoinLevel(gameManager.currentRoom);
  }

  void OnHistory(string json)
  {
    // Not bound to UI yet: only logged until the dice history layout is built.
    LogEvent(logs.requests, "[BET_HISTORY] reply:", json);
  }

  void OnStart(string json)
  {
    LogEvent(logs.bets, "[START_GAME] reply:", json);
  }

  void OnDouble(string json)
  {
    LogEvent(logs.betActions, "[DOUBLE_BET] reply:", json);
    var reply = JsonUtility.FromJson<Reply<DoubleBetPayload>>(json);
    if (reply.success)
    {
      gameManager.OnBetsDoubled(reply.payload.bets);
      ApplyBalance(reply.payload.balance);
      gameManager.currentTotalBet = reply.payload.totalBet;
    }
    else
    {
      gameManager.OnBetRejected(reply.payload?.message);
    }
  }

  void OnRepeat(string json)
  {
    LogEvent(logs.betActions, "[REPEAT_BET] reply:", json);
    var reply = JsonUtility.FromJson<Reply<RepeatBetPayload>>(json);
    gameManager.OnRepeatReply();
    if (reply.success)
    {
      gameManager.OnBetsRepeated(reply.payload.bets);
      ApplyBalance(reply.payload.balance);
      gameManager.currentTotalBet = reply.payload.totalBet;
      if (gameManager.isAuto && gameManager.isSinglePlayer) SendStart();
    }
    else
    {
      if (gameManager.isAuto) gameManager.SetAuto(false);
      gameManager.OnBetRejected(reply.payload?.message);
    }
  }

  void OnCancel(string json)
  {
    LogEvent(logs.betActions, "[CANCEL_BET] reply:", json);
    var reply = JsonUtility.FromJson<Reply<CancelBetPayload>>(json);
    if (reply.success)
    {
      gameManager.OnBetsCancelled();
      ApplyBalance(reply.payload.balance);
      gameManager.currentTotalBet = 0;
    }
  }

  void OnUndo(string json)
  {
    LogEvent(logs.betActions, "[UNDO_BET] reply:", json);
    var reply = JsonUtility.FromJson<Reply<UndoBetPayload>>(json);
    if (reply.success)
    {
      gameManager.OnBetUndone(reply.payload.bet.betOption, reply.payload.refundAmount);
      ApplyBalance(reply.payload.balance);
      gameManager.currentTotalBet = reply.payload.totalBet;
    }
  }

  void OnRoomEnter(string json)
  {
    LogEvent(logs.requests, "[JOIN_LEVEL] reply:", json);
    var reply = JsonUtility.FromJson<Reply<JoinLevelPayload>>(json);
    if (reply.success == false)
    {
      Debug.LogError("[JOIN_LEVEL] failed: " + json);
      return;
    }
    roomData = reply.payload;
    startupPage.SetCanLoadFull(true);
    gameManager.SetCoinData();
    uiManager.SetgameRulePanel();
    gameManager.SetOtherplayerData(roomData.leaderboards);
  }

  // Reply shape is still being agreed with backend; read as a JOIN_LEVEL reply until then
  void OnModeChange(string json)
  {
    LogEvent(logs.requests, "[PLAYER_MODE] reply:", json);
    var reply = JsonUtility.FromJson<Reply<JoinLevelPayload>>(json);
    if (reply.success == false) return;

    roomData = reply.payload;
    gameManager.SetCoinData();
    uiManager.SetgameRulePanel();
    gameManager.SetOtherplayerData(roomData.leaderboards);
  }

  // Broadcast for every bet in the room, the player's own included
  void ManageOtherPlayerbets(string data)
  {
    LogEvent(logs.otherBets, "[game:bet_placed]", data);
    gameManager.OnOpponentBetPlaced(JsonUtility.FromJson<Bet>(data));
  }

  void OnGameLoopStarted(string json)
  {
    if (!loadingPageLoading)
    {
      loadingPageLoading = true;
      gameManager.OnGameLoaded();
    }
    NormalStart = true;
    LogEvent(logs.round, "[game:round_start]", json);
    gameManager.OnGameLoopStart();
  }

  void OnCashout(string data)
  {
    LogEvent(logs.cashout, "[game:cashout]", data);
    gameManager.ManagePayouts(JsonConvert.DeserializeObject<CashoutEvent>(data));
  }

  void OnLobbyCount(string data)
  {
    LogEvent(logs.lobby, "[game:lobby_count]", data);
    var lobbyCount = JsonUtility.FromJson<LobbyCountEvent>(data);
    gameManager.SetPlayerCountOnReturn(lobbyCount.lobby, playerdata.balance);
  }

  private void OnBetAcknowledged(string data)
  {
    LogEvent(logs.bets, "[PLACE_BET] reply:", data);
    var reply = JsonUtility.FromJson<Reply<PlaceBetPayload>>(data);
    if (reply.success)
    {
      gameManager.OnPlayerBetPlaced(reply.payload);
      ApplyBalance(reply.payload.balance);
      gameManager.currentTotalBet = reply.payload.totalBet;
    }
    else
    {
      gameManager.OnBetRejected(reply.payload?.message);
    }
  }
}

// Console log categories; warnings and errors are never gated
[Serializable]
public class LogToggles
{
  public bool connection = true;
  public bool init = true;
  public bool requests = true;
  public bool round = true;
  public bool timer = false;
  public bool bets = true;
  // Undo, Again, Clear and Double requests and their replies
  public bool betActions = true;
  public bool otherBets = false;
  public bool cashout = true;
  public bool leaderboard = true;
  public bool lobby = false;
}

// Outbound "request" envelope; payload classes below hold only what each action sends
[Serializable]
public class Request<TPayload>
{
  public string type;
  public TPayload payload;
}

[Serializable]
public class EmptyPayload { }

[Serializable]
public class LevelPayload
{
  public string level;
}

[Serializable]
public class PlayerModePayload
{
  public string playerMode;
}

[Serializable]
public class HistoryPayload
{
  public int page;
}

// Ack envelope of every "request" reply
[Serializable]
public class Reply<TPayload>
{
  public bool success;
  public TPayload payload;
}

// A rejected request is expected to carry only the reason
[Serializable]
public class ReplyPayload
{
  public string message;
}

[Serializable]
public class JoinLevelPayload : ReplyPayload
{
  public string roomId;
  public string oldRoomId;
  public string level;
  public int playerCount;
  public List<Bet> bets;
  // Each entry is itself a JSON string, parsed as DiceData
  public List<string> stats;
  public Leaderboards leaderboards;
  public RoundState roundState;
}

[Serializable]
public class RoundState
{
  public string roundId;
  public long startedAt;
  public long bettingEndTime;
  public long serverTime;
  public int timeRemaining;
  public string phase;
}

[Serializable]
public class PlaceBetPayload : ReplyPayload
{
  public string username;
  public string betId;
  public string betOption;
  public int amount;
  public int totalBet;
  public double balance;
}

[Serializable]
public class UndoBetPayload : ReplyPayload
{
  public int refundAmount;
  public int totalBet;
  public double balance;
  public Bet bet;
}

[Serializable]
public class DoubleBetPayload : ReplyPayload
{
  public int totalBet;
  public double balance;
  public List<DoubledBet> bets;
}

[Serializable]
public class RepeatBetPayload : ReplyPayload
{
  public int totalBet;
  public double balance;
  public List<Bet> bets;
}

[Serializable]
public class CancelBetPayload : ReplyPayload
{
  // Total refunded across the cancelled bets
  public int amount;
  public double balance;
  public List<Bet> bets;
}

[Serializable]
public class HomePayload : ReplyPayload
{
  public double balance;
}

// One placed bet: the game:bet_placed broadcast and the entries of undo, repeat and cancel replies
[Serializable]
public class Bet
{
  public string betId;
  public string betType;
  public string betOption;
  public int amount;
  public string username;
  public string userId;
  public string level;
  public string sessionId;
  public int betIndex;
}

[Serializable]
public class DoubledBet
{
  public string betId;
  public string betType;
  public string betOption;
  public int oldAmount;
  public int newAmount;
  public int delta;
}


[System.Serializable]
public class BetPayload
{
  public int amountIndex;
  public string betType;
  public string betOption;
}

[System.Serializable]
public class Richest
{
  public string username;
  public double balance;
  public int rank;
}
[System.Serializable]
public class Winner
{
  public string username;
  public double totalWins;
  public int rank;
}


[System.Serializable]
public class Bets
{
  public List<int> level_1;
  public List<int> level_2;
  public List<int> level_3;
  public List<int> level_4;
  public List<int> level_5;
  public List<int> level_6;

  public string username { get; set; }
  public int amount { get; set; }
  public string betId { get; set; }
  public string betType { get; set; }
  public string level { get; set; }
  public string userId { get; set; }
  public string betOption { get; set; }
}

[Serializable]
public class Lobby
{
  public int level_1;
  public int level_2;
  public int level_3;
  public int level_4;
  public int level_5;
  public int level_6;
}

[Serializable]
public class Payout
{
  public int win;
  public double balance;
  public string username;
  public string userId;
  public Dictionary<string, int> betWins;
}

[Serializable]
public class BalanceSyncPayload
{
  public double balance;
}

[Serializable]
public class Player
{
  public double balance;
  public string username;
}
[Serializable]
public class InitEvent
{
  public string id;
  public GameData gameData;
  public Player player;
}

[Serializable]
public class RoundStartEvent
{
  public string roundId;
  public long startedAt;
  public long bettingEndTime;
  public long serverTime;
  public int playerCount;
}

[Serializable]
public class BettingTimerEvent
{
  public string roundId;
  public long serverTime;
  public long bettingEndTime;
  public int timeRemaining;
}

[Serializable]
public class BonusEvent
{
  public string roundId;
  public Dictionary<string, int> bonus;
}

[Serializable]
public class DiceResultEvent
{
  public string roundId;
  public int dice1;
  public int dice2;
  public int sum;
  public string matchSide;
}

[Serializable]
public class CashoutEvent
{
  public List<Payout> payouts;
  public Leaderboards leaderboards;
}

[Serializable]
public class LobbyCountEvent
{
  public Lobby lobby;
  public int totalCount;
}
[System.Serializable]
public class SideBets
{

  public S2 s_2;
  public S3 s_3;
  public S4 s_4;
  public S5 s_5;
  public S6 s_6;
  public S8 s_8;
  public S9 s_9;
  public S10 s_10;
  public S11 s_11;
  public S12 s_12;
}

public class Wagers
{

  public MainBets main_bets;
  public SideBets side_bets;
}

[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
  public string nameSpace;
}
[Serializable]
public class Meta
{
  public int total;
  public int page;
  public int limit;
  public int pages;
}



[System.Serializable]
public class BonusMultipliers
{
  public List<int> count;
  public List<int> value;
}

[System.Serializable]
public class GameData
{
  public List<string> betOptions;
  public int roundInterval;
  public int diceInterval;
  public int diceLimit;
  public int statsLimit;
  public Bets bets;
  public List<string> levels;
  public Wagers wagers;
  public Lobby lobby;
  public Leaderboards leaderboards;
  public List<object> stats;
  public BonusMultipliers bonusMultipliers;
}

[System.Serializable]
public class Leaderboards
{
  public List<Richest> richest;
  public List<Winner> winners;
}



[System.Serializable]
public class MainBets
{
  public Number812 number_8_12;
  public Number7 number_7;
  public Number26 number_2_6;
}

[System.Serializable]
public class MaxBetLimit
{
  public int level_1;
  public int level_2;
  public int level_3;
  public int level_4;
  public int level_5;
  public int level_6;
}

[System.Serializable]
public class Number26
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class Number7
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class Number812
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}




[System.Serializable]
public class S10
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class S11
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class S12
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class S2
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class S3
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class S4
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class S5
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class S6
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class S8
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[System.Serializable]
public class S9
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}




