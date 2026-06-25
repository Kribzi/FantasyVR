# Town (`FantasyVR.Town`)

Shared multiplayer hub (reuses the template's session/lobby/voice/avatar stack).

- `TownController` - ensures connection via `XRINetworkGameManager`, presents a prominent **Start
  Match** interaction that calls `GameFlowManager.StartCombat()`.

The only place networking matters for gameplay. See `Docs/05_Multiplayer.md`.
