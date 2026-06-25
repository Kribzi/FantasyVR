# Combat (`FantasyVR.Combat`)

Combat orchestration and player state (all local / single-player).

- `CombatDirector` - runs one encounter end-to-end: enemy rise -> spawner begin -> route slice
  hits/misses/potions -> on enemy death build `CombatResult` and show scoreboard.
- `PlayerHealth` - health + `Heal()`/`Damage()` (Damage unused in M1 per no-penalty pillar).

Slicing lives in the `Slicing/` subfolder (`FantasyVR.Combat.Slicing`).

See `Docs/04_CombatSystem.md`.
