using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;


public class UIManager : MonoBehaviour
{
  [SerializeField] private AudioManager audioController;
  [SerializeField] private SocketIOManager socketManager;
  [SerializeField] private JSFunctCalls jsFunctCalls;
  [SerializeField] private GameManager gameManager;

  private void Awake()
  {
    if (jsFunctCalls != null)
      jsFunctCalls.RegisterVisibilityListener(gameObject.name);

    ApplyCursor();
    SetupChipSelectorBackdrop();
    SetupSideMenu();
  }

  private void Start()
  {
    CollapseCoinsInstant();
    Bind(coinSelectorButton, ToggleCoins, false);
    BindCoins();
    BindInfoPage();
    BindPopups();
    BindBetActions();
  }

  private void Update()
  {
    // Polled so the icon follows the real state: Escape, a refused request, the deferred switch
    if (Screen.fullScreen == isFullscreen) return;
    isFullscreen = Screen.fullScreen;
    ApplyExpandShrinkSprites();
  }

  // Called by the host page through the JS bridge (SendMessage)
  private void OnFocusChanged(string value)
  {
    bool focused = value == "1";
    Debug.Log("UNITY FOCUS CHANGED: " + value + " (focused: " + focused + ")");
    audioController?.SetMuteAll(!focused);
    socketManager?.HandleFocusChange(focused);
  }

  // The coin selector has its own sound, so it opts out of the click
  internal void Bind(Button button, UnityAction action, bool clickSound = true)
  {
    if (!button) return;
    button.onClick.RemoveAllListeners();
    if (clickSound) button.onClick.AddListener(PlayButtonSound);
    button.onClick.AddListener(action);
  }

  private void PlayButtonSound()
  {
    if (audioController) audioController.PlayButton();
  }

  private static void SetButtonSprite(Button button, Sprite sprite)
  {
    if (button && button.image && sprite) button.image.sprite = sprite;
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

  [Header("Cursor")]
  [SerializeField] private Texture2D cursorTexture;
  [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
  [Tooltip("Editor-only cursor size in pixels. Builds use the texture's imported size, which must stay 32 or under.")]
  [SerializeField] private Vector2Int cursorSize = Vector2Int.zero;

  private void ApplyCursor()
  {
    if (cursorTexture == null) return;

    Texture2D texture = cursorTexture;
    Vector2 hotspot = cursorHotspot;

    // Builds keep the imported size: browsers drop a CSS cursor over 32px near the viewport edge
    bool resize = Application.isEditor && cursorSize.x > 0 && cursorSize.y > 0
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

  [Header("Side Menu")]
  [SerializeField] private Button menuButton;
  [SerializeField] private GameObject sidePanel;
  [SerializeField] private Button sidePanelCloseButton;
  // Optional: a parent drawn above the panel, so the menu button stays on top while the menu is open
  [SerializeField] private Transform menuButtonOpenParent;
  [SerializeField] private Button infoButton;
  [SerializeField] private Button soundButton;
  [SerializeField] private Button musicButton;
  [SerializeField] private Button expandShrinkButton;
  [SerializeField] private Button homeButton;
  [SerializeField] private float menuButtonsDuration = 0.5f;
  // Local Y inside the buttons' parent that every button folds up to
  [SerializeField] private float buttonsCollapsedY = 440f;

  [Header("Side Menu Sprites")]
  [SerializeField] private Sprite soundOnSprite;
  [SerializeField] private Sprite soundOffSprite;
  [SerializeField] private Sprite musicOnSprite;
  [SerializeField] private Sprite musicOffSprite;
  [SerializeField] private Sprite expandSprite;
  [SerializeField] private Sprite shrinkSprite;

  private List<Button> menuButtons;
  private readonly List<RectTransform> menuButtonsRects = new List<RectTransform>();
  private readonly List<Vector3> menuButtonsPositions = new List<Vector3>();
  private readonly List<Vector2> menuButtonsSizes = new List<Vector2>();
  private Transform menuButtonOriginalParent;
  private int menuButtonOriginalSiblingIndex;
  private Tween menuToggleTween;
  private TMP_Text expandShrinkLabel;
  private bool isMenuOpen = false;
  private bool isFullscreen = false;
  private bool isMusic = true;
  private bool isSound = true;

  private void SetupSideMenu()
  {
    menuButtons = new List<Button> { infoButton, soundButton, musicButton, expandShrinkButton, homeButton };
    menuButtons.RemoveAll(btn => btn == null);

    // The scene holds the buttons in their open pose; it is cached here, then folded away
    foreach (Button btn in menuButtons)
    {
      RectTransform rect = btn.GetComponent<RectTransform>();
      menuButtonsRects.Add(rect);
      menuButtonsPositions.Add(rect.localPosition);
      menuButtonsSizes.Add(rect.sizeDelta);
      rect.localPosition = new Vector3(rect.localPosition.x, buttonsCollapsedY, rect.localPosition.z);
      rect.sizeDelta = new Vector2(rect.sizeDelta.x, 0f);
      if (btn.targetGraphic) btn.targetGraphic.raycastTarget = false;
    }
    if (sidePanel) sidePanel.SetActive(false);

    if (menuButton)
    {
      Bind(menuButton, ToggleMenu);
      menuButtonOriginalParent = menuButton.transform.parent;
      menuButtonOriginalSiblingIndex = menuButton.transform.GetSiblingIndex();
    }

    if (sidePanelCloseButton)
    {
      Bind(sidePanelCloseButton, ToggleMenu);
      sidePanelCloseButton.interactable = false;
    }

    Bind(infoButton, OpenInfoPage);
    Bind(soundButton, ToggleSound);
    Bind(musicButton, ToggleMusic);
    Bind(homeButton, OpenQuitPopup);

    if (expandShrinkButton)
    {
      expandShrinkButton.onClick.RemoveAllListeners();
      expandShrinkLabel = expandShrinkButton.GetComponentInChildren<TMP_Text>(true);

      // WebGL defers a fullscreen request to the next input event, so it is asked for on press and granted on release
      EventTrigger.Entry press = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
      press.callback.AddListener(delegate { ToggleFullscreen(); });
      expandShrinkButton.gameObject.AddComponent<EventTrigger>().triggers.Add(press);
    }
    ApplyExpandShrinkSprites();
  }

  private void ToggleMenu()
  {
    if (isMenuOpen)
      RetractMenu();
    else
      ExpandMenu();
  }

  private void ExpandMenu()
  {
    isMenuOpen = true;
    if (sidePanel) sidePanel.SetActive(true);
    if (sidePanelCloseButton) sidePanelCloseButton.interactable = false;
    AnimateMenuButtons(true);
    if (menuButton && menuButtonOpenParent) menuButton.transform.SetParent(menuButtonOpenParent, true);

    // Closing is only allowed once the open tween has finished
    menuToggleTween?.Kill();
    menuToggleTween = DOVirtual.DelayedCall(menuButtonsDuration, () =>
    {
      if (sidePanelCloseButton) sidePanelCloseButton.interactable = true;
    });
  }

  private void RetractMenu()
  {
    isMenuOpen = false;
    if (sidePanelCloseButton) sidePanelCloseButton.interactable = false;
    AnimateMenuButtons(false);
    if (menuButton && menuButtonOpenParent)
    {
      menuButton.transform.SetParent(menuButtonOriginalParent, true);
      menuButton.transform.SetSiblingIndex(menuButtonOriginalSiblingIndex);
    }

    menuToggleTween?.Kill();
    menuToggleTween = DOVirtual.DelayedCall(menuButtonsDuration, () =>
    {
      if (sidePanel) sidePanel.SetActive(false);
    });
  }

  private void AnimateMenuButtons(bool open)
  {
    for (int i = 0; i < menuButtonsRects.Count; i++)
    {
      RectTransform rect = menuButtonsRects[i];
      float targetY = open ? menuButtonsPositions[i].y : buttonsCollapsedY;
      Vector2 targetSize = open ? menuButtonsSizes[i] : new Vector2(menuButtonsSizes[i].x, 0f);

      rect.DOKill();
      rect.DOLocalMoveY(targetY, menuButtonsDuration).SetEase(Ease.Linear);
      rect.DOSizeDelta(targetSize, menuButtonsDuration).SetEase(Ease.Linear);

      Graphic graphic = menuButtons[i].targetGraphic;
      if (graphic) graphic.raycastTarget = open;
    }
  }

  private void ToggleSound()
  {
    isSound = !isSound;
    SetButtonSprite(soundButton, isSound ? soundOnSprite : soundOffSprite);
    if (audioController) audioController.ToggleSoundMute(!isSound);
  }

  private void ToggleMusic()
  {
    isMusic = !isMusic;
    SetButtonSprite(musicButton, isMusic ? musicOnSprite : musicOffSprite);
    if (audioController) audioController.ToggleMusicMute(!isMusic);
  }

  private void ToggleFullscreen()
  {
    PlayButtonSound();
    Screen.fullScreen = !Screen.fullScreen;
  }

  private void ApplyExpandShrinkSprites()
  {
    SetButtonSprite(expandShrinkButton, isFullscreen ? shrinkSprite : expandSprite);
    if (expandShrinkLabel) expandShrinkLabel.text = isFullscreen ? "Shrink" : "Expand";
  }

  [Header("Info Page")]
  // Lives under the game page, so it never uses the popup root
  [SerializeField] private GameObject infoPage;
  [SerializeField] private Button infoCloseButton;
  // One per row of the betting-limit table; every row shows the current level's limit
  [SerializeField] private List<TMP_Text> infoMaxBetTexts;

  private void BindInfoPage()
  {
    Bind(infoCloseButton, CloseInfoPage);
  }

  private void OpenInfoPage()
  {
    RetractMenu();
    if (infoPage) infoPage.SetActive(true);
  }

  private void CloseInfoPage()
  {
    if (infoPage) infoPage.SetActive(false);
  }

  internal void SetInfoMaxBet(int maxBet)
  {
    foreach (TMP_Text text in infoMaxBetTexts)
    {
      if (text) text.text = maxBet.ToString();
    }
  }

  [Header("Popups")]
  // Parent of the three popups and their overlay, drawn above every page
  [SerializeField] private GameObject popupRoot;
  [SerializeField] private GameObject quitPopup;
  [SerializeField] private Button quitYesButton;
  [SerializeField] private Button quitNoButton;
  [SerializeField] private GameObject reconnectPopup;
  [SerializeField] private GameObject disconnectPopup;
  [SerializeField] private Button disconnectExitButton;

  private void BindPopups()
  {
    Bind(quitYesButton, ExitGame);
    Bind(quitNoButton, CloseQuitPopup);
    Bind(disconnectExitButton, ExitGame);
  }

  private static bool IsOpen(GameObject popup) => popup && popup.activeSelf;

  // The root stays on for as long as any of its popups is showing
  private void SetPopup(GameObject popup, bool show)
  {
    if (popup) popup.SetActive(show);
    if (popupRoot) popupRoot.SetActive(IsOpen(quitPopup) || IsOpen(reconnectPopup) || IsOpen(disconnectPopup));
  }

  private void OpenQuitPopup()
  {
    RetractMenu();
    SetPopup(quitPopup, true);
  }

  private void CloseQuitPopup()
  {
    if (socketManager.IsExiting) return;

    SetPopup(quitPopup, false);
  }

  internal void ReconnectionPopup()
  {
    if (socketManager.IsExiting) return;

    SetPopup(reconnectPopup, true);
  }

  internal void DisconnectionPopup()
  {
    // Closing the game drops the socket on purpose
    if (socketManager.IsExiting) return;

    // Its only way out is exiting, so nothing else stays stacked under it
    if (quitPopup) quitPopup.SetActive(false);
    if (reconnectPopup) reconnectPopup.SetActive(false);
    SetPopup(disconnectPopup, true);
  }

  internal void CheckAndClosePopups()
  {
    if (IsOpen(reconnectPopup)) SetPopup(reconnectPopup, false);
    if (IsOpen(disconnectPopup)) SetPopup(disconnectPopup, false);
  }

  private void ExitGame()
  {
    socketManager.CloseGame();
  }

  [Header("Chip Selector")]
  [SerializeField] internal Chip coinSelector;
  [SerializeField] private Button coinSelectorButton;
  [SerializeField] internal List<Chip> coins;
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

  private int uiSelectedCoin = 0;
  private bool isExpanded = false;

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
    if (audioController) audioController.PlayChipSelector();
    FadeBackdrop(true);

    Vector3 center = coinSelector.transform.localPosition;
    int expandCount = coins.Count - 1;
    int arcIndex = 0;

    for (int i = 0; i < coins.Count; i++)
    {
      if (i == uiSelectedCoin)
        continue;

      Chip coin = coins[i];
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

    if (audioController) audioController.PlayChipSelector();
    FadeBackdrop(false);

    Vector3 center = coinSelector.transform.localPosition;

    for (int i = 0; i < coins.Count; i++)
    {
      if (i == uiSelectedCoin)
        continue;

      Chip coin = coins[i];
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

    for (int i = 0; i < coins.Count; i++)
    {
      if (i == uiSelectedCoin)
        continue;

      coins[i].transform.localPosition = center;
      coins[i].gameObject.SetActive(false);
    }

    ShowRepeatIfNoBet();
    isExpanded = false;
  }

  private void ShowRepeatIfNoBet()
  {
    if (gameManager.currentTotalBet <= 0)
      repeatPanel.SetActive(true);
  }

  private void BindCoins()
  {
    foreach (Chip coin in coins)
    {
      Button button = coin.GetComponent<Button>();
      if (!button) continue;

      // A fresh event drops any onClick still wired in the Inspector, so a pick never fires twice
      button.onClick = new Button.ButtonClickedEvent();
      button.onClick.AddListener(() => OnCoinSelected(button));
    }
  }

  private void OnCoinSelected(Button selectedCoin)
  {
    int newSelectedIndex = -1;
    for (int i = 0; i < coins.Count; i++)
    {
      if (coins[i].gameObject == selectedCoin.gameObject)
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

    // Copy, never swap: each coin keeps its own index, or its label and the index sent on PLACE_BET drift apart
    selectorChip.chipIndex = selectedChip.chipIndex;
    selectorChip.chipAmount = selectedChip.chipAmount;

    uiSelectedCoin = newSelectedIndex;

    RetractCoins();
    for (int i = 0; i < coins.Count; i++)
    {
      coins[i].gameObject.SetActive(true);
    }
    selectedCoin.gameObject.SetActive(false);

    // The picked chip is skipped by the retract, so park it under the main chip for its next fan-out
    selectedCoin.transform.DOKill();
    selectedCoin.transform.localPosition = coinSelector.transform.localPosition;
  }

  [Header("Bet Actions")]
  [SerializeField] private GameObject repeatPanel;
  [SerializeField] private Button repeatButton;
  [SerializeField] private GameObject betActionsPanel;
  [SerializeField] private Button undoButton;
  [SerializeField] private Button cancelButton;
  [SerializeField] private Button doubleButton;
  [SerializeField] private Button autoButton;
  [SerializeField] private Button stopAutoButton;
  [SerializeField] internal Button startButton;
  [SerializeField] private TMP_Text netBetText;
  [SerializeField] private GameObject singleModeBadge;

  private int currentNetBet = 0;

  private void BindBetActions()
  {
    Bind(repeatButton, () => gameManager.RequestRepeat());
    Bind(undoButton, () => socketManager.SendUndo());
    Bind(cancelButton, () => socketManager.SendCancle());
    Bind(doubleButton, () => socketManager.SendDouble());
    Bind(autoButton, () => gameManager.SetAuto(true));
    Bind(stopAutoButton, () => gameManager.SetAuto(false));
    Bind(startButton, () => gameManager.StartRound());

    foreach (Button btn in new[] { repeatButton, undoButton, cancelButton, doubleButton, autoButton, startButton, stopAutoButton })
    {
      if (!btn.GetComponent<ButtonAnimator>()) btn.gameObject.AddComponent<ButtonAnimator>();
    }
  }

  // A delta is added to the running total; 0 resets it
  internal void SetNetBetPanel(int totalbet)
  {
    if (totalbet == 0) currentNetBet = 0;
    else currentNetBet += totalbet;

    if (currentNetBet < 0)
      currentNetBet = 0;

    netBetText.text = currentNetBet.ToString();
  }

  internal void ShowBetActions(bool istrue, bool db = true, bool canc = true, bool undo = true)
  {
    betActionsPanel.SetActive(istrue);
    doubleButton.gameObject.SetActive(db);
    cancelButton.gameObject.SetActive(canc);
    undoButton.gameObject.SetActive(undo);
  }

  internal void SetSinglePlayerUi(bool single)
  {
    singleModeBadge.SetActive(single);
    startButton.gameObject.SetActive(single);
  }

  internal void SetBetActionButtons(bool interactable)
  {
    undoButton.interactable = interactable;
    cancelButton.interactable = interactable;
    doubleButton.interactable = interactable;
  }

  // Again, Auto and Auto Stop share one position, so exactly one is shown
  internal void SetRepeatSlot(bool autoOn, bool offerAuto, bool canRepeat)
  {
    stopAutoButton.gameObject.SetActive(autoOn);
    autoButton.gameObject.SetActive(!autoOn && offerAuto);
    repeatButton.gameObject.SetActive(!autoOn && !offerAuto);
    repeatButton.interactable = canRepeat;
  }

  [Header("Player")]
  [SerializeField] internal PlayerData mainPlayer;
}
