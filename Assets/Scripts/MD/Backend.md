# 7 Up 7 Down (Multiplayer) - 

This document outlines the events, actions, and features for the multiplayer **7 Up 7 Down** (Variant: `ML-7U7DJ`) game to assist with Unity client integration.

## 1. Game Flow & Phases

The game operates on a continuous loop managed by the server. The Unity client should listen to the events and update the UI accordingly.

1. **Round Start (Betting Phase):** The server starts a new round, selects random bonus multipliers for certain betting options, and opens betting.
2. **Betting Timer:** The server periodically syncs the remaining time. Players can place, double, repeat, or undo bets.
3. **Round End (Betting Closed):** The betting phase closes. The UI should disable betting controls and wait for the dice roll.
4. **Dice Result Phase:** The server rolls the dice, calculates wins, and sends the results to the client. The Unity client plays the dice roll animation, highlights the winning zones, and shows the player's winnings.

---

## 2. Server-to-Client Events (Listen to these)

The Unity client needs to register socket event listeners for the following events:

| Event Name | Description |
| :--- | :--- |
| `game:round_start` | Triggered when a new round begins. Clear the board and enable betting controls. |
| `game:bonus` | Sent at the start of a round to assign random bonus multipliers (e.g., 2x, 3x) to specific betting options. Highlight these on the UI. |
| `game:betting_timer` | Sends the remaining time for the betting phase. Update the timer UI clock. |
| `game:bet_placed` | Broadcasted when any player in the room successfully places a bet. Use this to render chips on the board from other players. |
| `game:bet_cancel` | Broadcasted if a player cancels their bet. |
| `game:round_end` | Betting time is over. Show "No More Bets" and disable betting controls. |
| `game:dice_result` | Contains the final `dice1` and `dice2` values, winning options, and the player's win amount. Trigger dice animations and payout sequences. |
| `game:cashout_timer` / `game:cashout` | (If applicable) Used if players need a specific time window to cashout winnings. |

---

## 3. Client-to-Server Actions (Emit these)

When the player interacts with the UI, the Unity client must emit these actions (via standard game action payload structure):

| Action Type | Purpose |
| :--- | :--- |
| `START_GAME` | Call when the client is fully loaded to initialize the game state. |
| `JOIN_LEVEL` | Used to join a specific betting limit room (e.g., `level_1`, `level_2`). |
| `PLACE_BET` | Sent when the player taps a betting zone to drop a chip. Requires `betType` and `amountIndex`. |
| `UNDO_BET` | Undoes the very last chip placed in the current betting phase. |
| `DOUBLE_BET` | Doubles the player's current bets on the table. |
| `REPEAT_BET` | Places the exact same bets the player had in the previous round. |
| `CANCEL_BET` | Clears all unconfirmed/placed bets for the current round. |
| `BET_HISTORY` | Requests the player's past betting history (for UI modals/logs). |
| `PLAYER_MODE` | Toggles UI state (e.g., single-player focused view vs. multiplayer view). |
| `HOME` | Leaves the room/game. |

---

## 4. Betting Options (Bet Types & Keys)

The game board consists of **Main Bets** and **Side Bets**. These string keys must be used when sending a `PLACE_BET` action and when reading the `game:bonus` / `game:dice_result` payloads.

### Main Bets
*   `number_2_6`: 7 Down (Wins if dice total is between 2 and 6)
*   `number_7`: Exactly 7 (Wins if dice total is exactly 7)
*   `number_8_12`: 7 Up (Wins if dice total is between 8 and 12)

### Side Bets (Exact Number Bets)
Players can bet on the exact sum of the dice:
*   `s_2`, `s_3`, `s_4`, `s_5`, `s_6`, `s_8`, `s_9`, `s_10`, `s_11`, `s_12`

---

## 5. Key Features to Implement in Unity

1. **Chip Management:** 
   * Local chip placement (optimistic UI update).
   * Network chip rendering (rendering chips from `game:bet_placed` from other users).
2. **Dynamic Bonus Multipliers:** 
   * When `game:bonus` arrives, highlight the specific zones with glowing FX and show the multiplier text (e.g., "3X").
3. **Dice Physics/Animation:** 
   * When `game:dice_result` arrives, the result is predetermined. The Unity client must throw the 3D dice and manipulate their final resting rotation to match `dice1` and `dice2`.
4. **Win Celebrations:** 
   * Read the `winAmount` from `game:dice_result`. If `winAmount > 0`, trigger coin shower/celebration animations and highlight the winning bet zones.
