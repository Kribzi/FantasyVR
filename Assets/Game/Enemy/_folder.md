# Enemy (`FantasyVR.Enemy`)

- `EnemySkeleton` - states `Rising -> Active -> Dying`. `Rise()` plays the climb-out-of-ground intro
  (`OnRiseComplete` unlocks combat). `ApplyDamage(float)` reduces HP, plays hit reaction; at <=0
  transitions to death and raises `OnDied`.

See `Docs/04_CombatSystem.md`.
