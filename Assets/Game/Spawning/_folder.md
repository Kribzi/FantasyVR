# Spawning (`FantasyVR.Spawning`)

Streams flying objects toward the player along lanes (local, pooled).

- `ObjectSpawner` - lane-based spawning + difficulty ramp; `Begin(CombatConfig)` / `Stop()`.
- `SliceableObject` - the flying object: trigger detection, speed gating, optional required cut angle,
  slice VFX, pooled.
- `PotionFlask` - heal-on-slice sliceable variant.
- `SliceablePooler` / `PotionPooler` - subclasses of `XRMultiplayer.Pooler`.

Use `Pooler.GetItem()` / `ReturnItem()`. See `Docs/04_CombatSystem.md`.
