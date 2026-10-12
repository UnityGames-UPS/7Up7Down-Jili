# Side Panel (Menu) — Porting Guide

How the side menu in 32 Cards works and what to copy to get it running in another game. Assumes the scene UI (panel, buttons, close button, sprites) has already been moved to the new project and only the code and inspector wiring remain.

All of the menu code lives in `Assets/Scripts/UI/UIManager.cs` (class `UiManager`). There is no separate side-panel script.

## 1. What there is

There are two independent copies of the same menu, one per page. They share the animation settings and the button actions, but each has its own fields, state and methods.

| | Lobby (HomePage) | GamePage |
|---|---|---|
| Open button | `MenuButton` | `MenuButtonGP` |
| Panel root | `sidepanel` | `sidepanelGP` |
| Close button | `sidepanelCloseButton` | `sidepanelGPCloseButton` |
| Open-state parent for the menu button | `lobbyMenuButtonOpenParent` | `gpMenuButtonOpenParent` |
| Animated buttons (in order) | `GameRules`, `Sound`, `Music`, `ExpandShrink`, `Home` | `GameRulesGP`, `HistoryGP`, `SoundGP`, `MusicGP`, `ExpandShrinkGP`, `HomeGP` |
| Collapsed Y | `lobbyButtonsCollapsedY` | `gpButtonsCollapsedY` |
| Methods | `ToggleMenu`, `ExpandMenu`, `RetractMenu`, `SetLobbyButtonsCollapsedImmediate`, `AnimateLobbyButtonsOpen`, `AnimateLobbyButtonsClose` | `ToggleMenuGP`, `ExpandMenuGP`, `RetractMenuGP`, `SetGPButtonsCollapsedImmediate`, `AnimateGPButtonsOpen`, `AnimateGPButtonsClose` |

The lobby also has a `History` button, but it is not part of the menu list and is not animated.

Shared: `MenuButtonsDuration` (0.5s in script), the six sprites (`SoundOnSprite`, `SoundOffSprite`, `MusicOnSprite`, `MusicOffSprite`, `ExpandSprite`, `ShrinkSprite`), and the flags `isSound`, `isMusic`, `isExpanded`.

`MenuButtonsDuration` and the two collapsed-Y values are serialized, so the scene may hold values different from the script defaults (0.5 / 440 / 440). Copy them from the inspector in this project, not from the script.

## 2. Dependencies

- **DOTween** (`DG.Tweening`) — `DOLocalMoveY`, `DOSizeDelta`, `DOVirtual.DelayedCall`, `DOKill`.
- **TextMeshPro** — only for the Expand/Shrink label.
- **`AudioManager`** — `PlaySFX(SoundEffect.ButtonClick)`, `ToggleSoundMute(bool)`, `ToggleMusicMute(bool)`.
- **`JSFunctCalls`** + `Assets/Plugins/webgl/CustomJsLib.jslib` — fullscreen only (see 6.4).
- **Popup system in `UiManager`** — `OpenPopup(GameObject)` for Game Rules and History.
- **`SocketIOManager`** — `CloseSocket()` and `EmitLeaveRoom()` for the two Home buttons.

## 3. Scene requirements

These are what the code assumes about the hierarchy. I did not open the scene or prefab, so check each one against what you moved over.

1. **Place the buttons at their open positions in the scene.** `Awake` reads each button's `localPosition` and `sizeDelta` as the "open" pose, then immediately collapses them.
2. **All animated buttons of one menu share a parent** (or parents with the same local origin). `CollapsedY` is a single local-space Y that every button collapses to.
3. **No `VerticalLayoutGroup` / `ContentSizeFitter` driving these buttons.** The code tweens `localPosition.y` and `sizeDelta` directly; a layout group would overwrite both.
4. **Button contents must survive height 0.** Closing tweens the height to 0 while keeping the width. Icons and labels inside each button need to stretch with the parent or be clipped by a mask, otherwise they stay visible on top of the collapsed stack.
5. **Each button's `targetGraphic` must be set.** Clicks are enabled and disabled through `btn.targetGraphic.raycastTarget`, not through `interactable`.
6. **Sound, Music and Expand/Shrink need an `Image` on the button object itself** — the sprite swap uses `GetComponent<Image>()` on the button.
7. **Expand/Shrink label must be child index 0** of the button and have a `TMP_Text`. The code sets it to `"Expand"` / `"Shrink"`.
8. **`…MenuButtonOpenParent`** is an empty transform that sits above the panel in draw order (see section 5).
9. **The close button** is a normal `Button`; the code only sets `interactable` and `onClick`.

## 4. Open / close animation

### Setup (`Awake`)

```csharp
menuButtons = new List<Button> { GameRules, Sound, Music, ExpandShrink, Home };

menuButtonsRects.Clear();
menuButtonsPositions.Clear();
menuButtonsSizes.Clear();
foreach (var btn in menuButtons)
{
  if (btn == null) continue;
  var rect = btn.GetComponent<RectTransform>();
  if (rect == null) continue;
  menuButtonsRects.Add(rect);
  menuButtonsPositions.Add(rect.localPosition);   // open position
  menuButtonsSizes.Add(rect.sizeDelta);           // open size
  if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = false;
}

SetLobbyButtonsCollapsedImmediate();

MenuButton.onClick.RemoveAllListeners();
MenuButton.onClick.AddListener(ToggleMenu);
lobbyMenuButtonOriginalParent = MenuButton != null ? MenuButton.transform.parent : null;
lobbyMenuButtonOriginalSiblingIndex = MenuButton != null ? MenuButton.transform.GetSiblingIndex() : 0;

if (sidepanelCloseButton) sidepanelCloseButton.onClick.RemoveAllListeners();
if (sidepanelCloseButton) sidepanelCloseButton.onClick.AddListener(RetractMenu);
if (sidepanelCloseButton) sidepanelCloseButton.interactable = false;
```

State needed for this block:

```csharp
private List<Button> menuButtons;
private bool isLobbyMenuExpanded = false;
private readonly List<RectTransform> menuButtonsRects = new List<RectTransform>();
private readonly List<Vector3> menuButtonsPositions = new List<Vector3>();
private readonly List<Vector2> menuButtonsSizes = new List<Vector2>();
private Transform lobbyMenuButtonOriginalParent;
private int lobbyMenuButtonOriginalSiblingIndex;
private Tween lobbyMenuToggleTween;
```

### Collapsed pose

Every button sits at the same Y (`CollapsedY`) with height 0 and its original width.

```csharp
private void SetLobbyButtonsCollapsedImmediate()
{
  for (int i = 0; i < menuButtonsRects.Count; i++)
  {
    var rect = menuButtonsRects[i];
    if (rect == null) continue;
    rect.localPosition = new Vector3(rect.localPosition.x, lobbyButtonsCollapsedY, rect.localPosition.z);
    rect.sizeDelta = new Vector2(menuButtonsSizes[i].x, 0f);
  }
}
```

### Open

Each button moves from `CollapsedY` to its cached Y and grows from height 0 to its cached size, linear, over `MenuButtonsDuration`. Raycasts are turned on at the start of the tween.

```csharp
private void AnimateLobbyButtonsOpen()
{
  for (int i = 0; i < menuButtonsRects.Count; i++)
  {
    var rect = menuButtonsRects[i];
    if (rect == null) continue;
    rect.DOKill();
    rect.DOLocalMoveY(menuButtonsPositions[i].y, MenuButtonsDuration).SetEase(Ease.Linear);
    rect.DOSizeDelta(menuButtonsSizes[i], MenuButtonsDuration).SetEase(Ease.Linear);
  }
  if (menuButtons != null)
  {
    foreach (var btn in menuButtons)
    {
      if (btn == null) continue;
      if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = true;
    }
  }
}

private void ExpandMenu()
{
  audioController.PlaySFX(SoundEffect.ButtonClick);
  sidepanel.SetActive(true); // show panel immediately
  if (sidepanelCloseButton) sidepanelCloseButton.interactable = false;
  AnimateLobbyButtonsOpen();
  if (MenuButton != null && lobbyMenuButtonOpenParent != null)
  {
    MenuButton.transform.SetParent(lobbyMenuButtonOpenParent, true);
  }

  lobbyMenuToggleTween?.Kill();
  lobbyMenuToggleTween = DOVirtual.DelayedCall(MenuButtonsDuration, () =>
  {
    if (sidepanelCloseButton) sidepanelCloseButton.interactable = true;
  });

  isLobbyMenuExpanded = true;
}
```

### Close

The reverse tween. Raycasts are turned off at the start, and the panel is deactivated only after the tween has finished.

```csharp
private void AnimateLobbyButtonsClose()
{
  for (int i = 0; i < menuButtonsRects.Count; i++)
  {
    var rect = menuButtonsRects[i];
    if (rect == null) continue;
    rect.DOKill();
    rect.DOLocalMoveY(lobbyButtonsCollapsedY, MenuButtonsDuration).SetEase(Ease.Linear);
    rect.DOSizeDelta(new Vector2(menuButtonsSizes[i].x, 0f), MenuButtonsDuration).SetEase(Ease.Linear);
  }
  if (menuButtons != null)
  {
    foreach (var btn in menuButtons)
    {
      if (btn == null) continue;
      if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = false;
    }
  }
}

private void RetractMenu()
{
  audioController.PlaySFX(SoundEffect.ButtonClick);
  if (sidepanelCloseButton) sidepanelCloseButton.interactable = false;
  AnimateLobbyButtonsClose();
  if (MenuButton != null && lobbyMenuButtonOriginalParent != null)
  {
    MenuButton.transform.SetParent(lobbyMenuButtonOriginalParent, true);
    MenuButton.transform.SetSiblingIndex(lobbyMenuButtonOriginalSiblingIndex);
  }
  lobbyMenuToggleTween?.Kill();
  lobbyMenuToggleTween = DOVirtual.DelayedCall(MenuButtonsDuration, () =>
  {
    sidepanel.SetActive(false);
  });

  isLobbyMenuExpanded = false;
}

private void ToggleMenu()
{
  if (isLobbyMenuExpanded)
    RetractMenu();
  else
    ExpandMenu();
}
```

The GamePage versions are the same code with the `GP` fields substituted (`UIManager.cs` lines 1255–1351).

## 5. Close button and menu-button reparenting

### Close button

- Wired to `RetractMenu` / `RetractMenuGP` in `Awake`.
- Starts non-interactable.
- On open: stays non-interactable until `MenuButtonsDuration` has elapsed, then becomes interactable. This stops a close being triggered mid-open.
- On close: set non-interactable immediately; the panel is deactivated after `MenuButtonsDuration`.
- Both delays use one tween handle per menu (`lobbyMenuToggleTween` / `gpMenuToggleTween`), killed at the start of every expand or retract. A fast open-then-close therefore cannot leave a stale callback that re-enables the close button or hides a panel that was just reopened.

### Moving the open button to another parent

- `Awake` stores the menu button's original parent and sibling index.
- On expand: `MenuButton.transform.SetParent(lobbyMenuButtonOpenParent, true)`. `worldPositionStays` is `true`, so the button does not move on screen; only its place in the hierarchy (and so its draw and raycast order) changes.
- On retract: it is put back under the original parent and `SetSiblingIndex` restores its original slot.
- If the open parent is not assigned, the reparent is skipped and the menu still opens.

My reading of the intent: the open parent sits above the panel so the menu button stays visible and clickable while the panel is up, and tapping it again closes the menu through `ToggleMenu`. Set up the open parent in the new scene accordingly.

The reparent happens instantly at the start of the tween in both directions, not at the end.

## 6. Button functionality

All listeners are assigned in `AssignButtonListeners()` (`UIManager.cs` lines 294–361), each preceded by `RemoveAllListeners()`. Any `onClick` entries set in the inspector on these buttons are wiped at runtime.

| Button | Lobby | GamePage |
|---|---|---|
| Game Rules | `OpenPopup(InfoPopup_Object)` | same |
| History | `OpenHistoryPopup()` (button is outside the menu) | `OpenHistoryPopup()` |
| Sound | `ToggleSound()` | same |
| Music | `ToggleMusic()` | same |
| Expand/Shrink | `ToggleExpandShrink()` | same |
| Home | `ClientExited()` — leaves the game entirely | `StartCoroutine(GoHomeButton())` — leaves the table, back to lobby |

None of these close the menu except GamePage Home.

### 6.1 Game Rules

`OpenPopup(InfoPopup_Object)` plays the click SFX, fades in the shared popup background, scales the popup from 0 to 1 (`Ease.OutBack`, 0.35s) and resets the rules pager to page 0. To port it you need `OpenPopup` / `ClosePopup`, `mainPopupBGImage` setup from `InitializePopupViews()`, and the pager (`UpdateInfoUI`, `GoToPreviousInfoPage`, `GoToNextInfoPage`, `InfoPages_Objects`, `InfoPageNumberText`, `InfoLeft_button`, `InfoRight_button`, `InfoClose_button`).

### 6.2 History

```csharp
private void OpenHistoryPopup()
{
  if (betHistoryManager != null) betHistoryManager.OpenPanel();
  OpenPopup(HistoryPopup_Object);
}
```

`BetHistoryManager.OpenPanel()` resets to page 1 and requests it from the server. This is game-specific; replace it with the new game's history source or drop the button from the `menuButtonsGP` list.

### 6.3 Sound and Music

```csharp
private void ToggleSound()
{
  audioController?.PlaySFX(SoundEffect.ButtonClick);
  isSound = !isSound;
  Sprite soundSprite = isSound ? SoundOnSprite : SoundOffSprite;
  if (Sound != null && soundSprite != null)
  {
    var img = Sound.GetComponent<Image>();
    if (img != null) img.sprite = soundSprite;
  }
  if (SoundGP != null && soundSprite != null)
  {
    var img = SoundGP.GetComponent<Image>();
    if (img != null) img.sprite = soundSprite;
  }
  bool mute = !isSound;
  if (audioController) audioController.ToggleSoundMute(mute);
}
```

`ToggleMusic()` is the same shape with `isMusic`, the music sprites, `Music` / `MusicGP` and `ToggleMusicMute(!isMusic)`.

- One flag drives both pages, so the lobby and GamePage icons always match.
- `isSound` and `isMusic` are set to `true` in `AssignButtonListeners()`. They are not saved; every launch starts with both on.
- The click SFX is played before the flag flips, so muting plays one last click and unmuting is silent.
- `AudioManager.ToggleSoundMute` sets `mute` on each SFX `AudioSource` and makes `PlaySFX` return early. `ToggleMusicMute` mutes `bgMusicSource` only. If the new game has a different audio manager, these two methods are the whole contract.

### 6.4 Expand / Shrink (fullscreen)

```csharp
private void ToggleExpandShrink()
{
  audioController?.PlaySFX(SoundEffect.ButtonClick);
  isExpanded = !isExpanded;
  ApplyExpandShrinkSprites();
  if (jsFunctCalls != null)
  {
    if (isExpanded)
      jsFunctCalls.RequestExpandGame();
    else
      jsFunctCalls.RequestShrinkGame();
  }
}

// Called by the JS fullscreen change listener when the user exits fullscreen externally (e.g. Escape key)
public void OnFullscreenChanged(string value)
{
  isExpanded = value == "1";
  ApplyExpandShrinkSprites();
}
```

`ApplyExpandShrinkSprites()` sets the sprite (`ShrinkSprite` when expanded, `ExpandSprite` otherwise) and the child-0 label (`"Shrink"` / `"Expand"`) on both `ExpandShrink` and `ExpandShrinkGP`.

To port:

1. Copy `JSFunctCalls.cs` — `RequestExpandGame`, `RequestShrinkGame`, `RegisterFullscreenListener` and their `DllImport`s (`RequestFullscreen`, `ExitFullscreen`, `RegisterFullscreenChangeListener`).
2. Make sure the new project's `.jslib` defines those three functions. Here they are in `Assets/Plugins/webgl/CustomJsLib.jslib`. I did not open that file, so confirm the names there.
3. Keep this call in `Awake`: `jsFunctCalls.RegisterFullscreenListener(gameObject.name);`
4. `OnFullscreenChanged` must stay `public`, keep that exact name, and live on the GameObject whose name was registered — the browser calls it by name with `"1"` or `"0"`. Renaming the GameObject or moving the method to another object breaks the callback silently.

In the Editor both requests only log; the sprite and label still toggle.

### 6.5 Home

**Lobby — `ClientExited()`**

```csharp
private void ClientExited()
{
  audioController?.PlaySFX(SoundEffect.ButtonClick);
  isExit = true;
  StartCoroutine(socketManager.CloseSocket());
}
```

`SocketIOManager.CloseSocket()` turns on the raycast blocker, closes the socket, waits 0.5s, then in WebGL sends `"OnExit"` to the host app. `isExit` suppresses the disconnect and reconnect popups during shutdown. The disconnect popup's close button calls the same method.

**GamePage — `GoHomeButton()`**

```csharp
IEnumerator GoHomeButton()
{
  if (audioController) audioController.PlaySFX(SoundEffect.ButtonClick);
  RetractMenuGP();

  if (dealerController != null) dealerController.ResetImmediate();
  if (betPanelManager != null) betPanelManager.ResetOnJoinIdle();

  loadingPageText.text = "Leaving table....";
  loadingPage.SetActive(true);
  gamePage.SetActive(false);

  yield return new WaitForSecondsRealtime(0.5f);

  currentLevel = "";
  socketManager.EmitLeaveRoom();
}
```

`EmitLeaveRoom()` emits `HOME`; on a successful ack `SocketIOManager.HandleLevelLeaveAck` updates lobby counts and balance and calls `uiManager.OnLeaveLevel()`.

When porting, keep `RetractMenuGP()` as the first step and swap the two reset calls (`dealerController.ResetImmediate()`, `betPanelManager.ResetOnJoinIdle()`) for the new game's equivalents. They run before `gamePage.SetActive(false)` on purpose, so running animations can be cancelled while the page is still active.

## 7. Things to watch for

- **`audioController` is not null-checked in `ExpandMenu` / `RetractMenu` (and the GP versions).** Every other call site uses `?.` or an `if`. An unassigned `audioController` in the new scene throws on the first menu tap.
- **`MenuButton` and `MenuButtonGP` are dereferenced in `Awake` before their null checks** (`MenuButton.onClick.RemoveAllListeners()`). If the new game has only one page, delete the unused menu's block instead of leaving the field empty.
- **GamePage Home plays the click SFX twice** — once in `GoHomeButton`, once inside `RetractMenuGP`.
- **`RetractMenuGP()` is also called when the menu is already closed** (it is unconditional in `GoHomeButton`). It is harmless: it re-runs the close tween and schedules `SetActive(false)`.
- **The lobby menu is never closed automatically.** The only callers of `RetractMenu` are the toggle and the close button. If the lobby menu is open when the player enters a level, it is still open when they come back.
- **Opening Game Rules or History leaves the menu open** behind the popup.
- **Tweens are not killed on destroy.** If the new game unloads the scene while a menu is animating, add a `DOKill` for the button rects and the toggle tween in `OnDestroy`.
- **Time scale.** The button tweens follow `Time.timeScale`; `DOVirtual.DelayedCall` ignores it by default. If the new game pauses with `timeScale = 0`, the buttons freeze mid-tween while the close button still enables and the panel still deactivates on schedule. Add `.SetUpdate(true)` to the two rect tweens if the menu must work while paused.

## 8. Porting checklist

1. Import DOTween and TextMeshPro if they are not already in the project.
2. Copy the serialized fields under both `[Header("SidePanel")]` blocks, the `Sound/Music Sprites` block and the `Animation Settings` block (`UIManager.cs` lines 61–70, 85–95, 103–115).
3. Copy the private state (lines 129–145, plus `isExit`, `isMusic`, `isSound`, `isExpanded`).
4. Copy the two menu setup blocks from `Awake` (lines 173–227) and the fullscreen listener registration (lines 250–251).
5. Copy the menu button listeners from `AssignButtonListeners()` (lines 296–339).
6. Copy the menu methods: lines 887–983 (lobby) and 1255–1351 (GamePage).
7. Copy the button actions: `ToggleSound`, `ToggleMusic`, `ToggleExpandShrink`, `OnFullscreenChanged`, `ApplyExpandShrinkSprites` (lines 1099–1199), `ClientExited`, `OpenHistoryPopup`, `OpenPopup`, `ClosePopup` (lines 1019–1097), `GoHomeButton` (lines 867–885).
8. Replace the game-specific calls: the two reset calls in `GoHomeButton`, `betHistoryManager.OpenPanel()`, and the socket calls behind both Home buttons.
9. In the inspector, assign every field from the table in section 1, the six sprites, and copy `MenuButtonsDuration` and both collapsed-Y values from this project's scene.
10. Create or assign the two `…MenuButtonOpenParent` transforms above their panels.
11. Go through section 3 against the moved hierarchy.
12. Test: open, close with the close button, close with the menu button, double-tap quickly during the animation, each button on both pages, Escape out of fullscreen in a WebGL build, and GamePage Home with the menu open.
