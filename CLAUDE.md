# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

Unity **6000.3.24f1** WebGL client for **7 Up 7 Down**, a JILI-style multiplayer dice betting game
(`productName: "7 up 7 down jilli"`). The client is a thin presentation layer: **all game logic,
RNG, balance and payouts live on the backend**, reached over Socket.IO. The client only renders
state and emits intents.

### Game rules (as implemented here)

Two dice are rolled each round. Bets are placed during a countdown, then locked.

- **Main bets** (`main_bets`): `number_2_6` ("7 Down"), `number_7` ("Lucky 7"), `number_8_12` ("7 Up").
  Classic odds are 1:1 / 4:1 / 1:1, but **payouts are always read from the server**
  (`GameData.wagers.*.payout`), never hardcoded.
- **Side bets** (`side_bets`): exact total — `s_2`…`s_6`, `s_8`…`s_12` (no `s_7`; that is `number_7`).
- **Bonus**: the server may broadcast per-option multipliers (`game:bonus`) once betting closes.
  `GameManager.OnBonus` holds them until the Extra Pay banner reveals them as pooled "xN" badges
  (`BonusManager`).
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

## Code style

Claude writes most of the game code here, so keep it easy for both of us to read back.

- **Comments: small and meaningful.** One short line saying *why* or what is non-obvious. No big
  block comments, no banners, no restating what the code already says, no change-log comments
  ("added for X", "fixed Y").
- Prefer a clear name over a comment. If a method needs a paragraph to explain, split it.
- Delete dead code instead of commenting it out — git has the history.
- Match the surrounding naming/idiom unless the task is to refactor that file.

## Build / run

There is no CLI build script, no test suite, and no lint config in this repo.

- **Run**: open the project in Unity 6000.3.24f1, load `Assets/Scenes/GameScene.unity`, press Play.
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
   │      ├── BetOptionView (Assets/Scripts/Prefab/BetOptionView.cs) — one per bet spot; no manager refs, raises `Clicked`
   │      ├── ChipManager (Assets/Scripts/Functionality/ChipManager.cs) — chip pool (`GenericObjectPool<Chip>`), sprites, all chip animation tuning
   │      ├── BonusManager (Assets/Scripts/Functionality/BonusManager.cs) — bonus badge pool (`GenericObjectPool<BonusPrefab>`) and reveal tuning
   │      ├── LevelSelector (Assets/Scripts/UI/LevelSelector.cs) — sliding level + single/multiple mode panel; mode UI changes only on the `PLAYER_MODE` reply
   │      └── RoadMapManager (Assets/Scripts/Functionality/RoadMapManager.cs) — result list, top stat strip, both roadmap grids, percentages, its own fading popup
   ├── UiManager   (Assets/Scripts/UI/UIManager.cs)          — popups, menus, history
   ├── StartupPage (Assets/Scripts/Functionality/StartupPage.cs)— one-way splash/loading shown over the running game
   ├── AudioManager, DiceResultmanager, ImageAnimation, OrientationChange
   └── JSFunctCalls (Assets/Scripts/JS/JSFunctCalls.cs)      — C# ↔ browser bridge
```

### Socket protocol

Namespace `playground-multiplayer` on the SocketManager; transport forced to WebSocket;
`Reconnection = false` (reconnect is handled manually).

**Inbound** (`gameSocket.On<string>(...)` in `SetupSocketManager`):
`game:init`, `game:round_start`, `game:betting_timer`, `game:dice_result`,
`game:cashout`, `game:bet_placed` (every bet in the room, the player's own included), `game:bonus`, `game:lobby_count`,
`balance:sync`, `pong`, `socketState`, `internalError`, `alert`, `AnotherDevice`.

**Outbound**: almost everything is a single `"request"` emit carrying `{type, payload}` JSON, with
the reply delivered through `ExpectAcknowledgement<string>` rather than a separate event.
Types: `JOIN_LEVEL`, `PLAYER_MODE`, `PLACE_BET`, `UNDO_BET`, `REPEAT_BET`, `CANCEL_BET`,
`DOUBLE_BET`, `START_GAME`, `HOME`, `BET_HISTORY`. Plus a bare `ping` emit every 2s.

**Liveness**: `PingCheck` emits `ping` every `pingInterval`; a missing `pong` increments
`missedPongs` → reconnection popup at 2, disconnect popup and give-up at `MaxMissedPongs` (5).

### Backend contract (`Assets/Scripts/MD/Backend.md`)

`Backend.md` is the backend team's spec for this game (variant `ML-7U7DJ`) and the authority for
game-specific events, actions and bet keys. It only lists what is specific to 7 Up 7 Down —
studio-common events (`game:init`, `balance:sync`, `game:lobby_count`, `pong`, `socketState`,
`internalError`, `alert`, `AnotherDevice`) are not in it but still apply and must keep working.

Server-defined phase order: `game:round_start` (+ `game:bonus`) → `game:betting_timer` ticks →
`game:round_end` (**betting closed**, "No More Bets") → `game:dice_result` (dice + wins).

Where the client currently differs from the spec (verify before relying on either side):

- `game:bet_cancel` (another player cancelled) — **no listener**; their chips stay on the table.
- `game:cashout_timer` — no listener. Spec marks cashout events "if applicable".
- `game:round_end` is in the spec but the server never sends it, so there is no listener; the
  client closes betting off the timer hitting 0 instead.
- `game:dice_result` is specced to carry winning options and the player's `winAmount`; the real
  payload is only `roundId`, `dice1`, `dice2`, `sum`, `matchSide`, so the client derives wins from `game:cashout` (`Payout.betWins`) plus the balance label.
- Spec expects the dice to be thrown and land on `dice1`/`dice2`, bonus zones to glow with "NX"
  text, and a celebration when `winAmount > 0`.

### Round lifecycle (as the client implements it today)

1. `game:init` → `ManageInitData` fills `initialData` (`GameData`) and `playerdata`, then
   `SetInitialData()` + `SetOptionData()`, emits `JOIN_LEVEL` for `levels[0]` (the client joins
   the first level itself; there is no lobby) and posts `OnEnter` to the host page.
2. `JOIN_LEVEL` ack → `OnRoomEnter` → `SetCoinData()` (chip denominations for that level),
   rule panel, `SetStats()` (road map from `stats`, last result shown on the dice), leaderboards,
   and lets `StartupPage` finish loading. Rounds run muted behind the
   startup page until it is dismissed (Continue, or automatically if "don't show again" is saved).
3. `game:round_start` → `GameManager.OnGameLoopStart()` clears chips and bonus badges, resets
   option UI, fires "please bet now", auto-repeats if `isAuto`.
4. `game:round_start` and every `game:betting_timer` → `SyncBetTimer()`. The countdown runs locally
   in `Update()` from `bettingEndTime - serverTime`; ticks only pull the deadline earlier. At 0
   (`betLockLead` before the server's end) `LockBetting()` enables `BetBlocker`, dims every option
   the player has no bet on and plays the Bet Locked banner.
   `game:bonus` → `OnBonus()` → Extra Pay banner after Bet Locked, badges revealed at
   `extraPayRevealAt`. `PlayRoundBanner` owns the three banners and the dim behind them.
5. `game:dice_result` → `ManageResult()` hides the banners, runs the dice animation, then `ManageAfterResult()`
   highlights the winning option(s). The result is held as `pendingStat`.
6. `game:cashout` → `ManagePayouts()` → the result is added to the road map (or at the next
   `game:round_start` if no cashout came), chips animate from options to winners, balances update,
   leaderboards refresh, net-bet panel resets.

### Road map

`RoadMapManager` keeps the results oldest-first (seeded from the `JOIN_LEVEL` `stats`, capped at
rows × columns) and redraws every view from that list. All-results grid: oldest at top-left, filling
down each column then right. Streak grid: newest streak in the left column, newest result on top; a
streak is consecutive results in the same main category and spills into the next column when the
column is full. A live result's `isBonus` is derived on the client (a winning option had a
`game:bonus` multiplier) because `game:dice_result` does not carry it.

### Bet option indexing (easy to get wrong)

`GameManager.betOptions` is ordered: `[0] 8-12`, `[1] 7`, `[2] 2-6`, then `[3..12]` = totals
2,3,4,5,6,8,9,10,11,12. This must line up with the server's `GameData.betOptions` array, because
`SetOptionData` gives each view `initialData.betOptions[i]` as its `BetKey` (sent on `PLACE_BET`).
Consequences used throughout:

- result resolution: total < 7 → `betOptions[total + 1]`; total > 7 → `betOptions[total]`; total 7 → `betOptions[1]`.
- `TryGetOption(string)` maps server keys (`s_2`, `number_7`, …) back to views; unknown keys are
  logged and skipped.

### JSON serialization

Both serializers are in play and are not interchangeable:

- `JsonUtility` for most payloads (fast, but ignores properties and cannot do `Dictionary`).
- `JsonConvert` (Newtonsoft) where a payload contains dictionaries — `game:init`, `game:bonus`,
  `game:cashout` (`Payout.betWins` is `Dictionary<string,int>`), `balance:sync`.

Each inbound event has its own DTO (`RoundStartEvent`, `BettingTimerEvent`, `DiceResultEvent`, …)
and each ack is a `Reply<TPayload>` (`PlaceBetPayload`, `UndoBetPayload`, …), built from the
captured samples in `Assets/Scripts/JSON/`. `CashoutEvent` and `HomePayload` have no captured
sample yet and only mirror what the handlers read. The `PLAYER_MODE` reply (`ModeChangePayload`)
carries no `stats`, `bets` or `leaderboards`; `roundState` is `null` when switching to `multiple`.

### Single mode

`GameManager.ApplyPlayerMode` resets the table on a confirmed switch. Single mode has no countdown
until the player presses Start (`StartRound` → `START_GAME`), which begins a timed round; a timed
`game:round_start` / `game:betting_timer` before that is treated as stale (`IsStaleTimedEvent`:
logged as an error and ignored). The round ends at payout (`FinishSingleRound`), which reopens betting.

### Base template lineage

The project was forked from an **Andar Bahar** client and retargeted to dice. Socket layer, JS
bridge, WebGL template, focus/visibility handling, chip pooling and menu/history UI came over
unchanged on purpose. Most card residue has been removed (see "Refactor plan"); what is left is
large commented-out blocks.

What stays untouched is the studio-common plumbing: connection/auth, ping/pong, focus handling,
JS bridge.

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
- `BetOptionView` keeps its own running bet totals (`PlayerBet` / `OpponentBet`) in parallel with
  the server's — they are client-side sums of acks and broadcasts, not server truth.

## Refactor plan (agreed direction, not started)

The scripts were written by a previous developer against the old UI. The goal is a full refactor
of `Assets/Scripts/`, done in steps the user asks for — not as drive-by changes.

1. **Match the new UI.** The user has replaced/reworked much of the UI in the scene; scripts still
   reference the old layout. Ask which objects exist now rather than assuming from old field names.
2. **Remove all Andar Bahar / card logic** — mostly done: dead card DTOs, `Root` card fields,
   flush/highlight fields, `AnimationCall.cs` and `gameID` are gone; the dice payload is now
   `DiceResultEvent`, handled by `OnDiceResult`. Still to do:
   - **Bet history** is stubbed: both history buttons open the popup and send `BET_HISTORY`;
     `OnHistory` only logs the reply (`[BET_HISTORY] reply: …`). The card-based `History` DTO,
     `SetHistoryPage` and `HistoryPrefab.SetData` were removed. Rebuild the DTO, row binding and
     page tracking (`CurrentHistoryPage`/`MaxHistoryPage`) from the logged reply and the new UI.
3. **Fix the animation mistakes** left in chip, dice, bonus and payout animations.
4. **Align with `Backend.md`** — close the gaps listed under "Backend contract".

Rules while refactoring:

- Removing or renaming a `[SerializeField]` un-wires it in the scene. List every such field in the
  reply so the user can re-wire or delete it in the Inspector.
- Dropping unused DTO fields is safe for parsing (both serializers ignore unknown JSON keys), but
  keep every field a handler still reads.
- The "Known rough edges" above are in scope for the refactor, but still call each one out when
  touching it.

## Current status (branch `dev-meh`)

Core loop works end-to-end against `devrealtime.dingdinghouse.com`: init, level join, betting with
chip animation, timer, dice result, payouts, bonus multipliers, history, leaderboards, stats road
map, single/multiplayer modes, auto-repeat.

Since `55f12a9` the work has been Editor-side UI replacement (new graphics, old `NewGraphics/` and
Core Sans BR fonts removed, scene reworked). Script code has not yet been brought in line with it —
that is the refactor above.
