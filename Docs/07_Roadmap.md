# 07 - Roadmap

Milestones are ordered to get a fun, testable combat loop first, then layer flow, multiplayer hub,
polish, and shipping. Each milestone lists concrete tasks. Check items off as completed.

## M0 - Foundations (this docs pass)

- [x] Verify project stack and lock decisions (`README.md`).
- [x] Knowledge base + architecture docs (`01`-`08`).
- [x] Code scaffold: `Assets/Game/` folders + `FantasyVR.Game.asmdef`.
- [ ] (Recommended next) Initialize git and commit the template + docs as a baseline.

## M1 - Core combat loop - IMPLEMENTED (device build in progress)

Goal: in VR, the skeleton rises, objects fly in, slicing with both hands deals combo-scaled damage,
potions heal, missing is harmless, the enemy dies, a scoreboard appears.

See [09_M1_Implementation_Handoff.md](09_M1_Implementation_Handoff.md) for the full snapshot.

- [x] Config ScriptableObjects: `CombatConfig`, `EnemyConfig`, `ComboConfig` (+ `LaneLayout`).
- [x] `BladeController` + `Blade`: bind to local rig hands/controllers, compute velocity, blade
      collider + trail + haptics.
- [x] `SliceableObject`: trigger detection, speed gating, optional required angle, pooled. (slice VFX
      deferred to M2)
- [x] `PotionFlask`: heal-on-slice variant.
- [x] `ObjectSpawner` + `SliceablePooler`/`PotionPooler` (subclass `XRMultiplayer.Pooler`): lane
      spawning, difficulty ramp.
- [x] `ComboSystem` + `ScoreTracker` + `CombatResult`.
- [x] `PlayerHealth`.
- [x] `EnemySkeleton`: rise/climb intro, HP, hit reaction, death.
- [x] `CombatDirector`: orchestrate the encounter end-to-end.
- [x] Minimal in-combat HUD (enemy HP, combo/multiplier, score, player HP).
- [x] Minimal `ScoreboardUI` with Play Again / Return to Town buttons (Town is a stub).
- [x] Built a Combat area in `SampleScene`; wired prefabs. Verified in-editor Play mode.
- [~] Play on device - **blocked**: first Android Build And Run failed on an OpenXR Project Validation
      NRE; needs fixing (see doc 09 "Immediate next actions").
- [ ] First tuning pass (slice speed threshold, spawn rates, combo tiers) - after it runs on device.

Acceptance: see `04_CombatSystem.md` "What M1 must deliver".

## M2 - Combat feel & content polish

- [ ] Juice pass: trails, slice halves, impact SFX, haptics, screen/HUD feedback, enemy death.
- [ ] Skeleton art/animation pass (rise, idle, hit, death).
- [ ] Arena environment: grassy outdoor landscape, lighting, skybox.
- [ ] Audio: music bed, slice/combo/potion/enemy SFX.
- [ ] Difficulty curve tuning for ~60-120s sessions.
- [ ] Angle-required objects: clear visual language for required cut direction.

## M3 - Full game flow

- [ ] `GameFlowManager` state machine: Boot -> Combat -> Scoreboard -> (Combat | Town).
- [ ] Instant-action on first launch (straight into combat, no session required).
- [ ] Polished `ScoreboardUI`: full stats (score, best combo, accuracy, potions, duration), Play Again
      as primary/default focus.
- [ ] Area transitions via teleport (mirror `MiniGameManager.TeleportToArea`).

## M4 - Town hub (multiplayer)

- [ ] Build the town square environment as a Town area/root.
- [ ] `TownController`: ensure connection via `XRINetworkGameManager`; reuse session/lobby/voice.
- [ ] Prominent **Start Match** interaction -> `GameFlowManager.StartCombat()`.
- [ ] Entering/leaving combat without disturbing the session.
- [ ] Verify avatars, name tags, and voice in the hub with 2+ players (Multiplayer Play Mode + device).

## M5 - Quest hardening & store prep

- [ ] Android/OpenXR/AndroidXR build config validated (see `06_QuestPublishing.md`).
- [ ] Performance pass on Quest 2 & 3 (sustained framerate at peak object count, FFR, single-pass
      instanced, batching).
- [ ] Comfort options surfaced; headset-off/standby/resume/recenter handled.
- [ ] UGS/Vivox dashboard configuration for production.
- [ ] Store assets (icon, screenshots, trailer, descriptions, ratings) and VRC conformance pass.
- [ ] Submit to Meta Quest Store.

## M6 - Secondary target & post-launch (nice-to-have)

- [ ] PCVR/Steam build target (desktop OpenXR), validate full loop.
- [ ] Optional competitive multiplayer combat (shared scoreboard via networked scores - see
      `05_Multiplayer.md` extension path 1).
- [ ] Progression / variety: more enemies, biomes, object types, cosmetics.

## Suggested cadence

Each milestone should end in something playable/testable on device. Prefer many small, verified steps
over large unverified ones. Keep all tuning in ScriptableObjects so iteration is fast.
