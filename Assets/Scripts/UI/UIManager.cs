using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Unity.VisualScripting.Dependencies.Sqlite;

public class UiManager : MonoBehaviour
{
    [SerializeField]
    private SocketIOManager socketManager;

    [Header("Screens UI")]
    [SerializeField] private GameObject HomeScreen_Object;
    [SerializeField] private GameObject GameScreen_Object;


    [Header("Andar Bahar Main Buttons")]
    [SerializeField] private Button HistoryMain_button;
    [SerializeField] private Button MenuMain_button;
    [SerializeField] private Button Level2_button;
    [SerializeField] private Button Level3_button;
    [SerializeField] private Button Level4_button;
    [SerializeField] private Button Level5_button;
    [SerializeField] private Button Level6_button;


    [Header("Andar Bahar")]
    [SerializeField] private Button MenuInGame_button;
    [SerializeField] private Button History_button;
    [SerializeField] private Button Info_button;
    [SerializeField] private Button Sound_button;
    [SerializeField] private Button Music_button;
    [SerializeField] private Button SoundMute_button;
    [SerializeField] private Button MusicMute_button;
    [SerializeField] private Button Home_button;
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


    //  [Header("Andar Bahar Menu Buttons")]
    // [SerializeField] private Button MenuInGame_button;
    // [SerializeField] private Button InfoInGame_button;
    // [SerializeField] private Button SoundInGame_button;
    // [SerializeField] private Button MusicInGame_button;
    // [SerializeField] private Button HomeInGame_button;











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

    [Header("Quit Popup")]
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

    [SerializeField]
    private AudioManager audioController;
    bool isExit;
    bool isMusic;
    bool isSound;
    
    [Header("Andar Bahar GameScreen btns")]
    [SerializeField] private Button SettingGame_button;
    [SerializeField] private Button InfoGame_button;
    [SerializeField] private Button SoundGame_button;
    [SerializeField] private Button MusicGame_button;
    [SerializeField] private Button SoundMuteGame_button;
    [SerializeField] private Button MusicMuteGame_button;
    [SerializeField] private Button HomeGame_button;

    [Header("Andar Bahar GameScreens Objects")]
    [SerializeField] private GameObject SettingsGame_Object;



    [SerializeField] private Scaleandfade scaleandfade;

    
     





    private void Start()
    {


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
            Debug.Log("Quit event: pressed Big_X button");

        });

        if (NoQuit_Button) NoQuit_Button.onClick.RemoveAllListeners();
        if (NoQuit_Button) NoQuit_Button.onClick.AddListener(delegate
        {
            if (!isExit)
            {
                ClosePopup(QuitPopup_Object);
                Debug.Log("quit event: pressed NO Button ");
            }
        });

        if (CrossQuit_Button) CrossQuit_Button.onClick.RemoveAllListeners();
        if (CrossQuit_Button) CrossQuit_Button.onClick.AddListener(delegate
        {
            if (!isExit)
            {
                ClosePopup(QuitPopup_Object);
                Debug.Log("quit event: pressed Small_X Button ");

            }
        });

        if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
        if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); });

        if (YesQuit_Button) YesQuit_Button.onClick.RemoveAllListeners();
        if (YesQuit_Button) YesQuit_Button.onClick.AddListener(delegate
        {
            CallOnExitFunction();
            Debug.Log("quit event: pressed YES Button ");

        });

        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener((delegate { CallOnExitFunction(); socketManager.ReactNativeCallOnFailedToConnect(); }));

        if (CloseAD_Button) CloseAD_Button.onClick.RemoveAllListeners();
        if (CloseAD_Button) CloseAD_Button.onClick.AddListener(CallOnExitFunction);



        if (audioController) audioController.ToggleMute(false);

        isMusic = true;
        isSound = true;

        if (Sound_Button) Sound_Button.onClick.RemoveAllListeners();
        if (Sound_Button) Sound_Button.onClick.AddListener(ToggleSound);

        if (Music_Button) Music_Button.onClick.RemoveAllListeners();
        if (Music_Button) Music_Button.onClick.AddListener(ToggleMusic);


        // Andar Bahar 
        if (HistoryMain_button) HistoryMain_button.onClick.RemoveAllListeners();
        if (HistoryMain_button) HistoryMain_button.onClick.AddListener(delegate { OpenPopup(HistoryPopup_Object); });

        if (MenuMain_button) MenuMain_button.onClick.RemoveAllListeners();
        if (MenuMain_button) MenuMain_button.onClick.AddListener(delegate { ResetMenuPanel(false); ToggleMenuPanel(); });

        if (MenuInGame_button) MenuInGame_button.onClick.RemoveAllListeners();
        if (MenuInGame_button) MenuInGame_button.onClick.AddListener(delegate { ResetMenuPanel(true); ToggleMenuPanel(); });

        if (Level2_button) Level2_button.onClick.RemoveAllListeners();
        if (Level2_button) Level2_button.onClick.AddListener(delegate { ResetMenuPanel(true); GameScreen_Object.SetActive(true);GameStartAnim(); });

        if (Level3_button) Level3_button.onClick.RemoveAllListeners();
        if (Level3_button) Level3_button.onClick.AddListener(delegate { ResetMenuPanel(true); GameScreen_Object.SetActive(true); GameStartAnim(); });

        if (Level4_button) Level4_button.onClick.RemoveAllListeners();
        if (Level4_button) Level4_button.onClick.AddListener(delegate { ResetMenuPanel(true); GameScreen_Object.SetActive(true); GameStartAnim(); });

        if (Level5_button) Level5_button.onClick.RemoveAllListeners();
        if (Level5_button) Level5_button.onClick.AddListener(delegate { ResetMenuPanel(true); GameScreen_Object.SetActive(true);GameStartAnim();});

        if (Level6_button) Level6_button.onClick.RemoveAllListeners();
        if (Level6_button) Level6_button.onClick.AddListener(delegate { ResetMenuPanel(true); GameScreen_Object.SetActive(true); GameStartAnim(); });


        if (Info_button) Info_button.onClick.RemoveAllListeners();
        if (Info_button) Info_button.onClick.AddListener(delegate { OpenPopup(InfoPopup_Object); MenuPanel_Object.SetActive(false); });

        if (History_button) History_button.onClick.RemoveAllListeners();
        if (History_button) History_button.onClick.AddListener(delegate { OpenPopup(HistoryPopup_Object); MenuPanel_Object.SetActive(false); });

        if (Sound_button) Sound_button.onClick.RemoveAllListeners();
        if (Sound_button) Sound_button.onClick.AddListener(delegate { ToggleSound(); });

        if (SoundMute_button) SoundMute_button.onClick.RemoveAllListeners();
        if (SoundMute_button) SoundMute_button.onClick.AddListener(delegate { ToggleSound(); });

        if (Music_button) Music_button.onClick.RemoveAllListeners();
        if (Music_button) Music_button.onClick.AddListener(delegate { ToggleMusic(); });

        if (MusicMute_button) MusicMute_button.onClick.RemoveAllListeners();
        if (MusicMute_button) MusicMute_button.onClick.AddListener(delegate { ToggleMusic(); });

        if (Home_button) Home_button.onClick.RemoveAllListeners();
        if (Home_button) Home_button.onClick.AddListener(delegate { OpenPopup(GameQuitPopup); });

        if (YesHome_button) YesHome_button.onClick.RemoveAllListeners();
        if (YesHome_button) YesHome_button.onClick.AddListener(delegate { ClosePopup(GameQuitPopup); HomeScreen_Object.SetActive(true); GameScreen_Object.SetActive(false); ResetMenuPanel(false); });

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



        if (InfoGame_button) InfoGame_button.onClick.RemoveAllListeners();
        if (InfoGame_button) InfoGame_button.onClick.AddListener(delegate { OpenPopup(InfoPopup_Object); SettingsGame_Object.SetActive(false); });


        if (SoundGame_button) SoundGame_button.onClick.RemoveAllListeners();
        if (SoundGame_button) SoundGame_button.onClick.AddListener(delegate { ToggleGameSound(); });

        if (SoundMuteGame_button) SoundMuteGame_button.onClick.RemoveAllListeners();
        if (SoundMuteGame_button) SoundMuteGame_button.onClick.AddListener(delegate { ToggleGameSound(); });

        if (MusicGame_button) MusicGame_button.onClick.RemoveAllListeners();
        if (MusicGame_button) MusicGame_button.onClick.AddListener(delegate { ToggleGameMusic(); });

        if (MusicMuteGame_button) MusicMuteGame_button.onClick.RemoveAllListeners();
        if (MusicMuteGame_button) MusicMuteGame_button.onClick.AddListener(delegate { ToggleGameMusic(); });

        if (HomeGame_button) HomeGame_button.onClick.RemoveAllListeners();
        if (HomeGame_button) HomeGame_button.onClick.AddListener(delegate { SettingsGame_Object.SetActive(false); GameScreen_Object.SetActive(false); HomeScreen_Object.SetActive(true); });

        if (SettingGame_button) SettingGame_button.onClick.RemoveAllListeners();
        if (SettingGame_button) SettingGame_button.onClick.AddListener(delegate { ToggleSetting(); });







    }

    private void ToggleSetting()
    {
       SettingsGame_Object.SetActive(!SettingsGame_Object.activeSelf);
    }
    private void GameStartAnim()
    {
        scaleandfade.DoSequence();
        
    }





    private void UpdateFrequency(float value)
    {
        Mathf.Clamp(value, 0.2f, 2);
    }

    private void ResetMenuPanel(bool IsGameScreen)
    {
        MenuPanel_Object.SetActive(false);
        if (IsGameScreen)
        {
            Homebutton_Object.SetActive(true);
            //  MenuPanelContainer_Object.transform.localPosition = new Vector2(56, 394);
            MenuPanelContainer_Object.GetComponent<RectTransform>().anchoredPosition = new Vector2(56, 394);
            MenuPanel_Object.transform.SetParent(GameScreen_Object.transform, true);
            int lastIndex = GameScreen_Object.transform.childCount - 1;
            MenuPanel_Object.transform.SetSiblingIndex(lastIndex - 1);

        }
        else
        {
            Homebutton_Object.SetActive(false);
            // MenuPanelContainer_Object.transform.localPosition = new Vector2(56, 221);
            MenuPanelContainer_Object.GetComponent<RectTransform>().anchoredPosition = new Vector2(56, 221);
            MenuPanel_Object.transform.SetParent(HomeScreen_Object.transform, true);
            int lastIndex = HomeScreen_Object.transform.childCount - 1;
            MenuPanel_Object.transform.SetSiblingIndex(lastIndex - 1);

        }
    }

    private void ToggleMenuPanel()
    {
        if (IsMenuPanelOpen)
        {
            MenuPanel_Object.SetActive(false);
            IsMenuPanelOpen = false;
        }
        else
        {
            MenuPanel_Object.SetActive(true);
            IsMenuPanelOpen = true;
        }
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

     private void ToggleGameMusic()
    {
        isMusic = !isMusic;
        if (isMusic)
        {
            MusicGame_button.gameObject.SetActive(true);
            MusicMuteGame_button.gameObject.SetActive(false);
            audioController.ToggleMute(false, "bg");
        }
        else
        {
            MusicGame_button.gameObject.SetActive(false);
            MusicMuteGame_button.gameObject.SetActive(true);
            audioController.ToggleMute(true, "bg");
        }
    }


    private void UrlButtons(string url)
    {
        Application.OpenURL(url);
    }

    private void ToggleSound()
    {
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
    private void ToggleGameSound()
    {
        isSound = !isSound;
        if (isSound)
        {
            SoundGame_button.gameObject.SetActive(true);
            SoundMuteGame_button.gameObject.SetActive(false);
            if (audioController) audioController.ToggleMute(false, "button");
            if (audioController) audioController.ToggleMute(false, "wl");
            if (audioController) audioController.ToggleMute(false, "win");
            if (audioController) audioController.ToggleMute(false, "bet");

        }
        else
        {
            SoundGame_button.gameObject.SetActive(false);
            SoundMuteGame_button.gameObject.SetActive(true);
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



}