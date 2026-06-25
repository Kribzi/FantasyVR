# Flow (`FantasyVR.Flow`)

Top-level game state machine and area transitions.

- `GameFlowManager` - drives `Boot -> Combat -> Scoreboard -> (Combat | Town)` and `Town -> Combat`.
  Activates/deactivates area roots, teleports the player (XRI `TeleportationProvider`), enters Combat
  directly on first launch. Entry points: `StartCombat()`, `ShowScoreboard(CombatResult)`, `GoToTown()`.
- `GameState` enum.

See `Docs/03_Architecture.md`.
