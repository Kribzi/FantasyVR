# Scoring (`FantasyVR.Scoring`)

- `ComboSystem` - consecutive-hit combo, multiplier tiers (from `ComboConfig`), resets on miss;
  `OnComboChanged`.
- `ScoreTracker` - score, damage dealt, accuracy stats, potions, highest combo; `BuildResult()`.
- `CombatResult` - plain data passed to the scoreboard.

Keep scoring decoupled from the simulation so it can later emit networked results (see
`Docs/05_Multiplayer.md`). See `Docs/04_CombatSystem.md`.
