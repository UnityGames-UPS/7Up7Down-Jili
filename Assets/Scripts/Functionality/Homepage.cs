
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Homepage : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] SocketIOManager socketManager;
    [SerializeField] AudioManager audioManager;

    [Header("Player Details")]
    [SerializeField] private TMP_Text Playername;
    [SerializeField] internal TMP_Text PlayerBalance;
    [SerializeField] private Image PlayerImage;
    [Header("top Panel Details")]
    [SerializeField] internal TMP_Text TotalPlayerCount;

    [Header("Casual Room")]
    [SerializeField] Button Casualbutton;
    [SerializeField] private TMP_Text CMinBet;
    [SerializeField] private TMP_Text CMaxBet;
    [SerializeField] internal TMP_Text CPlayerCount;

    [Header("Novice Room")]
    [SerializeField] Button Novicebutton;
    [SerializeField] private TMP_Text NMinBet;
    [SerializeField] private TMP_Text NMaxBet;
    [SerializeField] internal TMP_Text NPlayerCount;

    [Header("Expert Room")]
    [SerializeField] Button Expertbutton;
    [SerializeField] private TMP_Text EMinBet;
    [SerializeField] private TMP_Text EMaxBet;
    [SerializeField] internal TMP_Text EPlayerCount;

    [Header("HighRoller Room")]
    [SerializeField] Button HighRollerbutton;
    [SerializeField] private TMP_Text HMinBet;
    [SerializeField] private TMP_Text HMaxBet;
    [SerializeField] internal TMP_Text HPlayerCount;

    internal void SetInitHomedata(GameData gameData)
    {

        // if (Casualbutton)
        // {

        //     Casualbutton.onClick.RemoveAllListeners();
        //     Casualbutton.onClick.AddListener(() => OnClickARoom(gameData.levels[0]));
        // }
        // if (Novicebutton)
        // {

        //     Novicebutton.onClick.RemoveAllListeners();
        //     Novicebutton.onClick.AddListener(() => OnClickARoom(gameData.levels[1]));
        // }
        // if (Expertbutton)
        // {

        //     Expertbutton.onClick.RemoveAllListeners();
        //     Expertbutton.onClick.AddListener(() => OnClickARoom(gameData.levels[2]));
        // }
        // if (HighRollerbutton)
        // {

        //     HighRollerbutton.onClick.RemoveAllListeners();
        //     HighRollerbutton.onClick.AddListener(() => OnClickARoom(gameData.levels[3]));
        // }

        // CMinBet.text = gameData.bets.casual[0].ToString();
        // CMaxBet.text = gameData.bets.casual[gameData.bets.casual.Count - 1].ToString();
        // CPlayerCount.text = gameData.lobby.casual.ToString() + "Players";


        // NMinBet.text = gameData.bets.novice[0].ToString();
        // NMaxBet.text = gameData.bets.novice[gameData.bets.novice.Count - 1].ToString();
        // NPlayerCount.text = gameData.lobby.novice.ToString() + "Players";


        // EMinBet.text = gameData.bets.expert[0].ToString();
        // EMaxBet.text = gameData.bets.expert[gameData.bets.expert.Count - 1].ToString();
        // EPlayerCount.text = gameData.lobby.expert.ToString() + "Players";


        // HMinBet.text = gameData.bets.high_roller[0].ToString();
        // HMaxBet.text = gameData.bets.high_roller[gameData.bets.high_roller.Count - 1].ToString();
        // HPlayerCount.text = gameData.lobby.high_roller.ToString() + "Players";

        // TotalPlayerCount.text = (gameData.lobby.casual + gameData.lobby.novice + gameData.lobby.expert + gameData.lobby.high_roller).ToString();

    }
    internal void setPlayerData(Player player)
    {
        // PlayerBalance.text = player.balance.ToString();
        // Playername.text = player.username.ToString();
    }




    internal void OnClickARoom(string room)
    {
        // audioManager.PlayButtonAudio();
        // socketManager.SendRoomSelection(room);
        // gameManager.currentRoom = room;
        // gameManager.SetLoadingPage(true);
    }


}
