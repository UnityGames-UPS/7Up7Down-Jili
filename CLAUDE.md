# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

Unity **6000.3.14f1** WebGL client for **7 Up 7 Down**, a JILI-style multiplayer dice betting game
(`productName: "7 up 7 down jilli"`). The client is a thin presentation layer: **all game logic,
RNG, balance and payouts live on the backend**, reached over Socket.IO. The client only renders
state and emits intents.

### Game rules (as implemented here)

Two dice are rolled each round. Bets are placed during a countdown, then locked.

- **Main bets** (`main_bets`): `number_2_6` ("7 Down"), `number_7` ("Lucky 7"), `number_8_12` ("7 Up").
  Classic odds are 1:1 / 4:1 / 1:1, but **payouts are always read from the server**
  (`GameData.wagers.*.payout`), never hardcoded.
- **Side bets** (`side_bets`): exact total — `s_2`…`s_6`, `s_8`…`s_12` (no `s_7`; that is `number_7`).
- **Bonus**: the server may broadcast per-option multipliers before a round (`game:bonus`), rendered
  as floating "xN" badges by `GameManager.ManageBonus`.
- **Levels / rooms**: `level_1`…`level_6`, each with its own chip denominations and max bet limits.
- **Modes**: multiplayer (shared table, other players' chips shown) and single player, plus
  Repeat / Auto-repeat betting.

## Working boundaries (important)

The user handles everything Unity-Editor-side. **Scripts under `Assets/Scripts/` are your scope.**

- Read-only for context, do not modify: `.unity` scenes, `.prefab`, `.asset`, `ProjectSettings/`,
  `Assets/Plugins/webgl/CustomJsLib.jslib`, `index.html`, `Assets/WebGLTemplates/custom/index.html`.
- Those web-glue files (jslib / index.html / socket plumbing) are **deliberately identical across
  the studio's ~70 games** so builds drop into the same React/React-Native platform. Do not
  "improve" them; divergence breaks platform integration.
- Serialized `[SerializeField]` references are wired in the scene. Renaming or reordering a
  serialized field silently breaks the scene — the user has to re-wire it. Adding new serialized
  fields is fine, but say so explicitly so they get assigned in the Inspector.

## Build / run

There is no CLI build script, no test suite, and no lint config in this repo.

- **Run**: open the project in Unity 6000.3.14f1, load `Assets/Scenes/SampleScene.unity`, press Play.
  In Editor, `SocketIOManager` connects to `TestSocketURI` using the Inspector-assigned `testToken`
  (see `#if UNITY_EDITOR` in `SetupSocketManager`). In WebGL builds it instead asks the host page
  for a token via the jslib bridge.
- **Build**: File → Build Settings → WebGL, using the `custom` WebGL template
  (`Assets/WebGLTemplates/custom/`).
- **Compile check**: you cannot compile from here — the user runs the Unity Editor, which
  recompiles on focus, and reports errors back. Write the change, then ask them to check the
  console rather than trying to verify it yourself. The `.csproj` files at the repo root are
  Unity-generated and regenerate — never hand-edit them.

## Architecture

Everything hangs off a handful of scene singletons wired to each other via `[SerializeField]`:

```
SocketIOManager  (Assets/Scripts/APIs/SocketIOManager.cs)   — transport + all DTOs
   ├── GameManager (Assets/Scripts/Functionality/GameManager.cs) — round flow, bets, chips, payouts
   │      └── OptionPrefab (Assets/Scripts/Prefab/OptionPrefab.cs) — one per bet spot
   ├── UiManager   (Assets/Scripts/UI/UIManager.cs)          — popups, menus, stats road map, history
   ├── Homepage    (Assets/Scripts/Functionality/Homepage.cs)— splash/loading before game data lands
   ├── AudioManager, DiceResultmanager, ImageAnimation, OrientationChange
   └── JSFunctCalls (Assets/Scripts/JS/JSFunctCalls.cs)      — C# ↔ browser bridge
```

### Socket protocol

Namespace `playground-multiplayer` on the SocketManager; transport forced to WebSocket;
`Reconnection = false` (reconnect is handled manually).

**Inbound** (`gameSocket.On<string>(...)` in `SetupSocketManager`):
`game:init`, `game:round_start`, `game:betting_timer`, `game:dice_result`, `game:round_end`,
`game:cashout`, `game:bet_placed` (other players), `game:bonus`, `game:lobby_count`,
`balance:sync`, `pong`, `socketState`, `internalError`, `alert`, `AnotherDevice`.

**Outbound**: almost everything is a single `"request"` emit carrying `{type, payload}` JSON, with
the reply delivered through `ExpectAcknowledgement<string>` rather than a separate event.
Types: `JOIN_LEVEL`, `PLAYER_MODE`, `PLACE_BET`, `UNDO_BET`, `REPEAT_BET`, `CANCEL_BET`,
`DOUBLE_BET`, `START_GAME`, `HOME`, `BET_HISTORY`. Plus a bare `ping` emit every 2s.

**Liveness**: `PingCheck` emits `ping` every `pingInterval`; a missing `pong` increments
`missedPongs` → reconnection popup at 2, disconnect popup and give-up at `MaxMissedPongs` (5).

### Round lifecycle

1. `game:init` → `ManageInitData` fills `initialData` (`GameData`) and `playerdata`, then
   `SetInitialData()` + `SetOptionData()` and posts `OnEnter` to the host page.
2. `JOIN_LEVEL` ack → `OnRoomEnter` → `SetCoinData()` (chip denominations for that level),
   rule panel, leaderboards.
3. `game:round_start` → `GameManager.OnGameLoopStart()` clears chips, resets option UI,
   fires "please bet now", auto-repeats if `isAuto`.
4. `game:betting_timer` → `SetBetTimer()` drives the circular timer; at 0 it enables `BetBlocker`
   and dims every option the player has no bet on.
5. `game:dice_result` → `ManageResult()` runs the dice animation, then `ManageAfterResult()`
   highlights the winning option(s) and pushes the result into the stats road map.
6. `game:round_end` → `EndLoop()` → `GameLoop()` cleanup coroutine.
7. `game:cashout` → `ManagePayouts()` → chips animate from options to winners, balances update,
   leaderboards refresh, net-bet panel resets.

### Bet option indexing (easy to get wrong)

`GameManager.AllOptions` is ordered: `[0] 8-12`, `[1] 7`, `[2] 2-6`, then `[3..12]` = totals
2,3,4,5,6,8,9,10,11,12. This must line up with the server's `GameData.betOptions` array, because
`onClickOption` sends `socketManager.initialData.betOptions[optionprefab.Optionindex]`.
Consequences used throughout:

- result resolution: total < 7 → `AllOptions[total + 1]`; total > 7 → `AllOptions[total]`; total 7 → `AllOptions[1]`.
- `FindOption(string)` maps server keys (`s_2`, `number_7`, …) back to list indices, and
  **falls back to `AllOptions[0]` for anything unrecognized** — a typo'd key silently credits "8-12".

### JSON serialization

Both serializers are in play and are not interchangeable:

- `JsonUtility` for most payloads (fast, but ignores properties and cannot do `Dictionary`).
- `JsonConvert` (Newtonsoft) where a payload contains dictionaries — `game:init`, `game:bonus`,
  `game:cashout` (`Payout.betWins` is `Dictionary<string,int>`), `balance:sync`.

`Root` is a single god-DTO reused for *every* event, so most of its fields are null for any given
message; check the event handler to know which subset is populated. Several DTO classes
(`AndarCard`, `MiddleCard`, `First3`, `Andar`/`Bahar`, `matchSide`, …) are dead leftovers from the
base template.

### Base template lineage

The project was forked from an **Andar Bahar** client and retargeted to dice. Socket layer, JS
bridge, WebGL template, focus/visibility handling, chip pooling and menu/history UI came over
unchanged on purpose. Residue to expect: card-based DTOs, `matchSide`/`andarCards` fields,
`AndarHighLight`/`BaharHighLight` objects, `CardDelt` naming for the dice-result payload,
`gameID = "ml-ab"`, and large commented-out blocks. Leave the socket/DTO shape alone unless the
backend actually changed; prune only what you are sure is unused.

### Host-platform contract (React / React-Native WebView)

`JSFunctCalls` → `CustomJsLib.jslib` → host page. Messages the game posts out: `authToken`
(request credentials), `OnEnter`, `OnExit`/`onExit`, `error`, `session_expired`, plus mirrored logs.
The host injects `{socketURL, token, nameSpace}` back via `SendMessage('SocketManager', 'ReceiveAuthToken', …)`.

Browser focus/visibility is bridged through `RegisterVisibilityChangeListener` →
`UiManager.OnFocusChanged("1"/"0")`, which mutes audio and calls
`SocketIOManager.HandleFocusChange`. Backgrounding for more than `maxBackgroundTime` (60s)
deliberately closes the socket and shows the disconnection popup.

`Assets/Scripts/MD/FOCUS_AUDIO_TIMEOUT_AUDIT.md` is the studio-wide runbook for this lifecycle
(focus, audio mute, background timeout, `OnError`, orientation, `balance:sync`). It is the
authority when touching any of that code — audit against it rather than redesigning.

## Known rough edges

Do not "fix" these silently as drive-by changes; flag them first.

- `GameManager.PlayPopup` and `SetPlayerCountOnReturn` are empty stubs — callers all over expect
  popups that never appear.
- `DistributePayouts` recovers the old balance by **string-parsing the balance label** (stripping
  non-digits from `"Rs..."`) to compute `currentWin`; `UpdatePlayerbalance` writes that `"Rs"` prefix.
  Any formatting change to the balance text breaks win calculation.
- Several UI bindings in `UiManager.Start()` are mis-wired copy-paste (`Music_button` and
  `MusicMute_button` both call `ToggleSound()`).
- `OptionPrefab` keeps its own running bet totals (`currentPlayerBetValue`) in parallel with the
  server's — they can drift on undo/cancel paths.

## Current status (as of the `ashutosh` branch)

Core loop is working end-to-end against `devrealtime.dingdinghouse.com`: init, level join, betting
with chip animation, timer, dice result, payouts, bonus multipliers, history, leaderboards, stats
road map, single/multiplayer modes, auto-repeat.

Uncommitted working tree is mostly Editor-side churn (scene, fonts, `.slnx` cleanup, removal of
the old `JSHandler.cs` / duplicate `JSFunctCalls.cs`, audit doc moved to `Assets/Scripts/MD/`);
script changes are cosmetic only.
