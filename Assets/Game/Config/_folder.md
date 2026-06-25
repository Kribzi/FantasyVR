# Config (`FantasyVR.Config`)

ScriptableObject definitions for tunable data (the classes; asset instances go in
`Assets/Game/ScriptableObjects/`).

- `CombatConfig` - enemy HP, match length, spawn-interval/speed curves, potion chance + heal,
  angle-required probability, player health.
- `EnemyConfig` - rise duration, hit reaction, death timing.
- `ComboConfig` - tier thresholds + multipliers, correct-angle bonus.
- `LaneLayout` (optional) - lane spawn/target points.

See `Docs/04_CombatSystem.md`.
