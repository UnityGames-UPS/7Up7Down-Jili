using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

using UnityEngine.Networking;

using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;

using System.Runtime.Serialization;
using Best.HTTP.Shared;

public class SocketIOManager : MonoBehaviour
{
    [SerializeField]
    internal GameManager gameManager;

    [SerializeField]
    private UiManager uiManager;

    internal Root roomData;
    internal Root gameLoopData;
    internal Root gameCashOut;
    internal Root BetChipData;
    internal Root OtherChipData;
    internal Root CashoutData;
    internal Root doubleBetData;
    internal Root HistoryPageData;
    internal Root ReturnHome;
    internal Root TotalPlayerCountData;
    internal Root TimeRemaining;
    internal Root CardDelt;
    internal Root FlushData;
    internal GameData initialData = null;
    // internal Payload resultData = null;
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
    protected string gameID = "ml-ab";
    //protected string gameID = "";

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
        Debug.Log("Unity: Closing Game");
        StartCoroutine(CloseSocket());
    }


    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log("Received data: " + jsonData);

        // Parse the JSON data
        var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
        SocketURI = data.socketURL;
        myAuth = data.cookie;
        //  nameSpace = data.nameSpace;
        // Proceed with connecting to the server using myAuth and socketURL
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


        //   Application.ExternalCall("window.parent.postMessage", "authToken", "*");

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("authToken");
        StartCoroutine(WaitForAuthToken(options));
#else
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = testToken,
                // gameId = gameID
            };
        };
        options.Auth = authFunction;
        savedToken = testToken;
        // Proceed with connecting to the server
        SetupSocketManager(options);
#endif
    }

    private IEnumerator WaitForAuthToken(SocketOptions options)
    {
        // Wait until myAuth is not null
        while (myAuth == null)
        {
            Debug.Log("My Auth is null");
            yield return null;
        }
        while (SocketURI == null)
        {
            Debug.Log("My Socket is null");
            yield return null;
        }
        Debug.Log("My Auth is not null");
        // Once myAuth is set, configure the authFunction
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = myAuth,
                // gameId = gameID
            };
        };
        options.Auth = authFunction;
        savedToken = myAuth;
        Debug.Log("Auth function configured with token: " + myAuth);

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
        gameSocket.On<string>("game:round_end", OnGameLoopEnd);
        gameSocket.On<string>("game:cashout", OnCashout);
        gameSocket.On<string>("game:lobby_count", OnLobbyCount);
        gameSocket.On<string>("game:betting_timer", OnListenTimeEvent);
        gameSocket.On<string>("game:dice_result", OnListenCardEvent);
        gameSocket.On<bool>("socketState", OnSocketState);
        gameSocket.On<string>("internalError", OnSocketError);
        gameSocket.On<string>("alert", OnSocketAlert);
        gameSocket.On<string>("AnotherDevice", OnSocketOtherDevice);
        gameSocket.On<string>("pong", OnPongReceived);
        manager.Open();
    }

    // Connected event handler implementation
    void OnConnected(ConnectResponse resp) //Back2 Start
    {
        Debug.Log("✅ Connected to server.");

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
        Debug.LogError("Socket Error Message: " + err);
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("error");
#endif
    }
    private void OnListenTimeEvent(string data)
    {
        gameManager.OnGameLoaded();
        Debug.Log("Received timer:/n " + data);
        //  ParseResponse(data);
        TimeRemaining = JsonUtility.FromJson<Root>(data);
        gameManager.SetBetTimer();
    }
    private void OnListenCardEvent(string data)
    {
        gameManager.OnGameLoaded();
        Debug.Log("Received Dice Result:/n " + data);
        //  ParseResponse(data);
        CardDelt = JsonUtility.FromJson<Root>(data);
        gameManager.ManageResult(CardDelt);
    }


    private void OnSocketState(bool state)
    {
        if (state)
        {
            Debug.Log("my state is " + state);
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
    private bool isFocused = true;
    private Coroutine focusCheckCoroutine;
    private bool disconnectionShown = false;   // <- NEW

    void OnApplicationFocus(bool focus)
    {
        Debug.Log("Focus: " + focus);
        isFocused = focus;

        if (!focus)
        {
            // Start checking after losing focus
            if (focusCheckCoroutine == null && !disconnectionShown)
                focusCheckCoroutine = StartCoroutine(IsNotInFocus());
        }
        else
        {
            // If popup already shown, do NOT cancel anything
            if (disconnectionShown) return;

            // Otherwise cancel coroutine when focus returns
            if (focusCheckCoroutine != null)
            {
                StopCoroutine(focusCheckCoroutine);
                focusCheckCoroutine = null;
            }
        }
    }

    IEnumerator IsNotInFocus()
    {
        yield return new WaitForSeconds(120f); // 2 seconds, change as required

        // If still not focused AND popup not shown
        if (!isFocused && !disconnectionShown)
        {
            // disconnectionShown = true;  // Prevent future runs
            //  uiManager.DisconnectionPopup();
            Debug.Log("Disconnected: No Focus for 120 seconds");
        }

        focusCheckCoroutine = null;
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

    internal void ReactNativeCallOnFailedToConnect() //BackendChanges
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("onExit");
#endif
    }

    internal IEnumerator CloseSocket() //Back2 Start
    {
        RaycastBlocker.SetActive(true);
        ResetPingRoutine();

        Debug.Log("Closing Socket");

        manager?.Close();
        manager = null;

        Debug.Log("Waiting for socket to close");

        yield return new WaitForSeconds(0.5f);

        Debug.Log("Socket Closed");

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); //Telling the react platform user wants to quit and go back to homepage
#endif
    }
    public void Reconnect()
    {
        Debug.Log("Reconnecting using saved token...");

        SocketOptions options = new SocketOptions();
        options.AutoConnect = false;
        options.Reconnection = false;
        options.Timeout = TimeSpan.FromSeconds(3);
        options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

        // Use saved token here
        options.Auth = (manager, socket) =>
        {
            return new { token = savedToken };
        };
        SetupSocketManager(options);
        //         // Close old manager if any
        //         manager?.Close();

        // #if UNITY_EDITOR
        //         manager = new SocketManager(new Uri(TestSocketURI), options);
        // #else
        //     manager = new SocketManager(new Uri(SocketURI), options);
        // #endif

        //         // Get correct namespace
        //         if (string.IsNullOrEmpty(nameSpace))
        //             gameSocket = manager.Socket;
        //         else
        //             gameSocket = manager.GetSocket("/" + nameSpace);

        //         manager.Open();
        //         DontDisplayDisconected = false;
    }

    void ManageInitData(string jsonObject)
    {
        Root myData = null;
        try
        {
            myData = JsonConvert.DeserializeObject<Root>(jsonObject);
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
        Debug.Log("ParseResponse: " + jsonObject);

        string id = myData.id;
        initialData = myData.gameData;
        playerdata = myData.player;

        setInitialData();
        gameManager.currentRoom = initialData.levels[0];
        SendRoomSelection(initialData.levels[0]);
        // if (initialData.bets != null)
        // else
        //     Debug.LogWarning("initData: bets list is null.");

#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager.SendCustomMessage("OnEnter");
#endif
    }




    private void setInitialData()
    {
        isLoaded = true;
        gameManager.SetInitialData();
        gameManager.SetOptionData();
        RaycastBlocker.SetActive(false);
        Application.ExternalCall("window.parent.postMessage", "OnEnter", "*");
#if UNITY_WEBGL && !UNITY_EDITOR //BackendChanges
            Application.ExternalEval(@"
            if(window.ReactNativeWebView){
            window.ReactNativeWebView.postMessage('OnEnter');
            }
            ");
#endif
    }

    internal void SendModeSelection(string mode)
    {
        Debug.Log("*** send Data ***" + mode);
        SendRoom message = new SendRoom();
        message.payload = new Payload();
        message.type = "PLAYER_MODE";
        message.payload.playerMode = mode;

        string json = JsonUtility.ToJson(message);
        Debug.Log("*** send Data ***" + json);
        // SendDataWithNamespace("request", json);
        gameSocket.ExpectAcknowledgement<string>(OnModeChange).Emit("request", json);
    }
    internal void SendRoomSelection(string Room)
    {
        Debug.Log("*** send Data ***" + Room);
        SendRoom message = new SendRoom();
        message.payload = new Payload();
        message.type = "JOIN_LEVEL";
        message.payload.level = Room;

        string json = JsonUtility.ToJson(message);
        Debug.Log("*** send Data ***" + json);
        // SendDataWithNamespace("request", json);
        gameSocket.ExpectAcknowledgement<string>(OnRoomEnter).Emit("request", json);
    }
    internal void BetPlaced(int amountIndex, string betType, string betOption)
    {
        double chipValue;

        if (double.TryParse(uiManager.coinSelector.Chiptext.text, out chipValue))
        {
            Debug.Log("XXXXXXXX" + chipValue + "    " + playerdata.balance);
            if (chipValue > playerdata.balance)
            {
                gameManager.PlayPopup("Low Balance");
                // Low balance logic here
                Debug.Log("Insufficient balance");

                return;
            }
        }
        BetMessage message = new BetMessage();
        message.type = "PLACE_BET";
        message.payload = new BetPayload();

        message.payload.amountIndex = amountIndex;
        message.payload.betType = betType;
        message.payload.betOption = betOption;

        string json = JsonUtility.ToJson(message);
        Debug.Log("Bet JSON: " + json);

        gameSocket.ExpectAcknowledgement<string>(OnBetAcknowledged).Emit("request", json);
    }
    internal void SendUndo()
    {
        SendRoom message = new SendRoom();
        message.payload = new Payload();
        message.type = "UNDO_BET";
        // message.payload.level = Room;

        string json = JsonUtility.ToJson(message);

        //  SendDataWithNamespace("request", json);
        gameSocket.ExpectAcknowledgement<string>(OnUndo).Emit("request", json);
    }
    internal void SendRepeat()
    {
        SendRoom message = new SendRoom();
        message.payload = new Payload();
        message.type = "REPEAT_BET";
        // message.payload.level = Room;

        string json = JsonUtility.ToJson(message);

        //  SendDataWithNamespace("request", json);
        gameSocket.ExpectAcknowledgement<string>(OnRepeat).Emit("request", json);
    }
    internal void SendCancle()
    {

        SendRoom message = new SendRoom();
        message.payload = new Payload();
        message.type = "CANCEL_BET";
        // message.payload.level = Room;

        string json = JsonUtility.ToJson(message);

        //  SendDataWithNamespace("request", json);
        gameSocket.ExpectAcknowledgement<string>(OnCancle).Emit("request", json);
    }
    internal void SendDouble()
    {
        SendRoom message = new SendRoom();
        message.payload = new Payload();
        message.type = "DOUBLE_BET";
        // message.payload.level = Room;

        string json = JsonUtility.ToJson(message);

        // SendDataWithNamespace("request", json);
        gameSocket.ExpectAcknowledgement<string>(OnDouble).Emit("request", json);
    }
    internal void SendStart()
    {
        SendRoom message = new SendRoom();
        message.payload = new Payload();
        message.type = "START_GAME";
        // message.payload.level = Room;

        string json = JsonUtility.ToJson(message);

        // SendDataWithNamespace("request", json);
        gameSocket.ExpectAcknowledgement<string>(OnStart).Emit("request", json);
    }
    internal void SendHome()
    {
        SendRoom message = new SendRoom();
        message.payload = new Payload();
        message.type = "HOME";
        // message.payload.level = Room;
        loadingPageLoading = false;
        NormalStart = false;
        DontDisplayDisconected = true;
        string json = JsonUtility.ToJson(message);
        Debug.Log("Return home: " + json);
        // SendDataWithNamespace("request", json);
        gameSocket.ExpectAcknowledgement<string>(OnHome).Emit("request", json);
    }

    internal void SendHistory(int Pages)
    {
        SendRoom message = new SendRoom();
        message.payload = new Payload();
        message.type = "BET_HISTORY";
        message.payload.page = Pages;

        string json = JsonUtility.ToJson(message);
        Debug.Log("Hestory sent: " + json);

        // SendDataWithNamespace("request", json);
        gameSocket.ExpectAcknowledgement<string>(OnHistory).Emit("request", json);
    }

    void OnHome(string json)
    {
        gameManager.ClearAllBets();
        Debug.Log("Home Receved: " + json);
        ReturnHome = JsonUtility.FromJson<Root>(json);
        gameManager.SetPlayerCountOnReturn(ReturnHome.payload.lobby, ReturnHome.payload.balance);
        playerdata.balance = ReturnHome.payload.balance;
        StartCoroutine(gameManager.ShowLoadingPage("Loading...."));
        gameManager.GamePage.SetActive(false);
        gameManager.HomePage.SetActive(true);
        //  Invoke(nameof(Reconnect), 0.2f);

    }
    void OnHistory(string json)
    {
        Debug.Log("**History Receved**" + json);
        HistoryPageData = JsonUtility.FromJson<Root>(json);
        uiManager.SetHistoryPage(HistoryPageData.payload);
    }
    void OnStart(string json)
    {
        Debug.Log(json);
        // doubleBetData = JsonUtility.FromJson<Root>(json);
        // if (doubleBetData.success)
        // {
        //     gameManager.DoubleBets(doubleBetData.payload.bets);
        //     gameManager.UpdatePlayerbalance(doubleBetData.payload.balance.ToString());
        //     playerdata.balance = doubleBetData.payload.balance;
        //     gameManager.currentTotalBet = doubleBetData.payload.totalBet;
        // }
        // else
        // {
        //     gameManager.PlayPopup(doubleBetData.payload.message);
        // }
    }
    void OnDouble(string json)
    {
        Debug.Log(json);
        doubleBetData = JsonUtility.FromJson<Root>(json);
        if (doubleBetData.success)
        {
            gameManager.DoubleBets(doubleBetData.payload.bets);
            gameManager.UpdatePlayerbalance(doubleBetData.payload.balance.ToString());
            playerdata.balance = doubleBetData.payload.balance;
            gameManager.currentTotalBet = doubleBetData.payload.totalBet;
        }
        else
        {
            gameManager.PlayPopup(doubleBetData.payload.message);
        }
    }
    void OnRepeat(string json)
    {
        Debug.Log("RepeatBet" + json);
        doubleBetData = JsonUtility.FromJson<Root>(json);
        if (doubleBetData.success)
        {
            gameManager.RepeAtBet(doubleBetData.payload.bets);
            gameManager.UpdatePlayerbalance(doubleBetData.payload.balance.ToString());
            playerdata.balance = doubleBetData.payload.balance;
            gameManager.currentTotalBet = doubleBetData.payload.totalBet;
            uiManager.ToggleRepeteAuto(true);
            if (gameManager.isAuto && gameManager.isSinglePlayer) SendStart();
        }
        else
        {
            //gameManager.PlayPopup(doubleBetData.payload.message);
        }
    }
    void OnCancle(string json)
    {

        Debug.Log("cancle :" + json);
        doubleBetData = JsonUtility.FromJson<Root>(json);
        if (doubleBetData.success)
        {
            gameManager.CancleBets();
            gameManager.UpdatePlayerbalance(doubleBetData.payload.balance.ToString());
            Debug.Log("cancle :" + doubleBetData.payload.balance.ToString());
            playerdata.balance = doubleBetData.payload.balance;
            gameManager.currentTotalBet = 0;
            //  uiManager.SetChipoption(false);
        }
    }
    void OnUndo(string json)
    {
        Debug.Log("undo :" + json);
        Debug.Log(json);
        doubleBetData = JsonUtility.FromJson<Root>(json);
        if (doubleBetData.success)
        {
            gameManager.UnduBets(doubleBetData.payload.bet.betId);
            gameManager.UpdatePlayerbalance(doubleBetData.payload.balance.ToString());
            playerdata.balance = doubleBetData.payload.balance;
            gameManager.currentTotalBet = doubleBetData.payload.totalBet;
            //  if (doubleBetData.payload.totalBet == 0) uiManager.SetChipoption(false);
        }
    }
    void OnRoomEnter(string json)
    {
        Debug.Log(json);
        roomData = JsonUtility.FromJson<Root>(json);
        if (roomData.success == false)
        {
            StartCoroutine(gameManager.ShowLoadingPage("Loading...."));
            gameManager.HomePage.SetActive(true);
            // gameManager.LoadingPage.SetActive(false);
            gameManager.GamePage.SetActive(false);
            return;

        }
        gameManager.SetCoinData();
        uiManager.SetgameRulePanel();
        gameManager.SetOtherplayerData(roomData.payload.leaderboards);
    }
    void OnModeChange(string json)
    {
        Debug.Log(json);
        roomData = JsonUtility.FromJson<Root>(json);
        if (roomData.success == false)
        {
            StartCoroutine(gameManager.ShowLoadingPage("Loading...."));
            //gameManager.HomePage.SetActive(true);
            // gameManager.LoadingPage.SetActive(false);
            // gameManager.GamePage.SetActive(false);
            return;

        }
        gameManager.SetCoinData();
        uiManager.SetgameRulePanel();
        gameManager.SetOtherplayerData(roomData.payload.leaderboards);
    }

    void ManageOtherPlayerbets(string data)
    {
        Debug.Log("Bet Placed OtherPlayer\n" + data);
        OtherChipData = JsonUtility.FromJson<Root>(data);
        gameManager.ManageBrodcastBetsOtherPlayers(OtherChipData);

    }
    void OnGameLoopStarted(string json)
    {
        if (!loadingPageLoading)
        {
            loadingPageLoading = true;
            gameManager.OnGameLoaded();
        }
        NormalStart = true;
        Debug.Log("Loop Started\n" + json);
        gameLoopData = JsonUtility.FromJson<Root>(json);
        gameManager.OnGameLoopStart();
    }
    void OnGameLoopEnd(string data)
    {
        gameLoopData = JsonUtility.FromJson<Root>(data);

        Debug.Log("Loop end\n" + data);


        // if (!NormalStart)
        // {
        //     gameManager.GamePage.SetActive(true);
        //     gameManager.SetLoadingPage(false);
        //     gameManager.HomePage.SetActive(false);
        //     gameManager.StartGameMidway();
        // }

        gameManager.EndLoop();



        // Only start game AFTER validating
    }
    void OnCashout(string data)
    {
        Debug.Log("CashOut\n" + data);
        // CashoutData = JsonUtility.FromJson<Root>(data);
        CashoutData = JsonConvert.DeserializeObject<Root>(data);

        gameManager.ManagePayouts();

    }
    void OnLobbyCount(string data)
    {
        Debug.Log("playerount\n" + data);
        TotalPlayerCountData = JsonUtility.FromJson<Root>(data);
        gameManager.SetPlayerCountOnReturn(TotalPlayerCountData.lobby, playerdata.balance);

    }
    private void OnBetAcknowledged(string data)
    {

        Debug.Log("Bet Acknowledgement: " + data);
        BetChipData = JsonUtility.FromJson<Root>(data);
        if (BetChipData.success)
        {
            gameManager.ManageBrodcastBetsPlayer();
            gameManager.UpdatePlayerbalance(BetChipData.payload.balance.ToString());
            gameManager.currentTotalBet = BetChipData.payload.totalBet;
            playerdata.balance = BetChipData.payload.balance;
        }
        else
        {
            gameManager.PlayPopup(BetChipData.payload.message);
        }


    }


}

[Serializable]
public class SendMode
{
    public string playerMode
;
}

[Serializable]
public class SendRoom
{
    public string type;
    public Payload payload;
}

[Serializable]
public class Payload
{
    public string level;
    public string playerMode
;

    public string roomId;

    public int playerCount;
    public Leaderboards leaderboards;

    public List<string> stats;
    public string betId;
    public string betOption;
    public string message;
    public int amount;
    public int totalBet;


    public int balance;
    public List<Bet> bets;


    public int refundAmount;
    public Bet bet;

    public int page;

    public List<History> history;
    public Meta meta;

    public Lobby lobby;


}
[System.Serializable]
public class Bet
{
    public int oldAmount;
    public int newAmount;
    public string betId;
    public string betType;
    public string betOption;
    public int delta;
    public int amount;
}


[System.Serializable]
public class BetPayload
{
    public int amountIndex;
    public string betType;
    public string betOption;
}

[System.Serializable]
public class BetMessage
{
    public string type;
    public BetPayload payload;
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


public class S1115
{
    public double payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class S15
{
    public double payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class S1620
{
    public double payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class S2125
{
    public double payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class S2630
{
    public double payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class S3135
{
    public double payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class S3640
{
    public int payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class S4153
{
    public int payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class S610
{
    public double payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}
public class Andar
{
    public List<double> payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class Bahar
{
    public List<double> payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
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

public class First1Andar
{
    public List<double> payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class First1Bahar
{
    public List<double> payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

public class First3
{
    public Payout payout { get; set; }
    public MaxBetLimit max_bet_limit { get; set; }
}

// public class GameData
// {
//     public List<string> betOptions { get; set; }
//     public int roundInterval { get; set; }
//     public int cashoutInterval { get; set; }
//     public int middleCardLimit { get; set; }
//     public int statsLimit { get; set; }
//     public Bets bets { get; set; }
//     public List<string> levels { get; set; }
//     public Wagers wagers { get; set; }
//     public Lobby lobby { get; set; }
//     public HandCode handCode { get; set; }
//     public Leaderboards leaderboards { get; set; }
//     public List<object> stats { get; set; }
// }




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
    public int straight_flush;
    public int straight;
    public int flush;

    public int win;
    public double balance;
    public string username;
    public string userId;
    public Dictionary<string, int> betWins;
}

[Serializable]
public class Player
{
    public double balance;
    public string username;
}
[Serializable]
public class Root
{
    public string id;
    public GameData gameData;
    public Player player;

    public string roundId;
    public string matchSide;
    public MiddleCard middleCard;
    public List<AndarCard> andarCards;
    public List<BaharCard> baharCards;
    public int firstThreeResult;
    // public Leaderboards leaderboards;

    public bool success;
    public Payload payload;
    public int playerCount;

    public long startedAt;


    public string username;
    public string betId;
    public string betType;
    public string betOption;
    public int amount;
    public List<Payout> payouts;

    public Lobby lobby;


    //new

    public long serverTime;
    public long bettingEndTime;
    public int timeRemaining;



    public Card card;
    public string side;
    public int cardsDealt;

    public int dice1;
    public int dice2;
    public int sum;


    public List<Card> cards;
    public Leaderboards leaderboards;



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
public class AndarCard
{
    public string suit;
    public string rank;
    public string color;
}
[Serializable]
public class BaharCard
{
    public string suit;
    public string rank;
    public string color;
}
[Serializable]
public class MiddleCard
{
    public string suit;
    public string rank;
    public string color;
}
[Serializable]
public class History
{
    public string user_id;
    public string bet_amount;
    public string win_amount;
    public string bet_type;
    public string bet_option;
    public string round_id;
    public string bet_id;
    public string level;
    public string middle_card;
    public int cards_dealt;
    public string match_side;
    public string matching_card;
    public DateTime created_at;

    // Parsed objects
    public Card middleCardParsed;
    public Card matchingCardParsed;
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
public class Card
{
    public string color;
    public string suit;
    public string rank;
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




