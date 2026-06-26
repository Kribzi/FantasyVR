# 09 - M1 Implementation Handoff (state as of first device build)

This is a continuity document so a fresh chat can pick up immediately. It records exactly what was
built for **M1 (core combat loop)**, how it is wired in the scene, the build/platform state, known
issues to debug, and the MCP workflow quirks discovered. Read `AGENTS.md` and `Docs/01`-`08` for the
design; this doc is the "what currently exists on disk and in the scene" snapshot.

## TL;DR status

- **M1 core combat loop is implemented, compiles clean, and was verified in-editor** (enemy rises ->
  objects stream -> pooling works -> kill -> scoreboard with stats -> HUD hides). No errors originate
  from `FantasyVR` code.
- **Platform switched to Android**; player settings are Quest-correct. A **Build And Run was attempted
  and FAILED** with an OpenXR validation error (see "Known issues / debug now"). That is the immediate
  thing to fix to get a running APK on the Quest 3.
- A **Quest 3** is connected over USB and authorized (adb sees `2G0YC5ZH1S0348`, model `Quest_3`).

## Immediate next actions (debug priority)

1. **Fix the failed Android build.** Last build report: `errors:1`, `output_path:
   D:/UnityProjects/FantasyVR/FantasyVR/Builds/Android/FantasyVR.apk`, `total_size_mb: 0.0` (no APK
   produced). Console showed:
   - `NullReferenceException` in `com.unity.xr.openxr/Runtime/OpenXRProjectValidation.cs:489` (build-time
     OpenXR **Project Validation**). Most likely cause: an OpenXR setting that fails validation for the
     Android target. Check **Project Settings -> XR Plug-in Management -> OpenXR -> Android tab**: ensure
     the **Meta Quest** feature group is enabled and at least one **interaction profile** is present
     (e.g. *Oculus Touch Controller Profile*, plus *Hand Interaction Profile* / *Meta Hand Tracking
     Aim* if using hands). Also open **Project Validation** and "Fix All". The NRE itself can be a
     validation-rule bug triggered by a missing/edge-case setting; resolving the underlying validation
     issue usually clears it.
   - `Diagnostics Data is enabled, and it requires Debug Symbols to be SymbolTable or Full ...` - this
     is a **warning**, not the failure. Optional: set **Player Settings -> Publishing Settings -> Debug
     Symbols** appropriately, or disable Diagnostics if undesired.
2. After it builds, confirm it **auto-launches** on device (Build And Run installs + launches). If
   building APK only, deploy manually with Unity's bundled adb:
   - adb at: `D:\UnityEditor\6000.4.2f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`
   - `adb install -r <apk>` then `adb shell monkey -p com.fantasyvr.game -c android.intent.category.LAUNCHER 1`
   - The app auto-resumes when the headset is put on (Quest pauses on proximity-off).
3. Then begin the **first tuning/juice pass** (see "Improvement backlog").

## Where everything lives

- Code: `Assets/Game/` - assembly **`FantasyVR.Game`** (`Assets/Game/FantasyVR.Game.asmdef`), namespace
  root **`FantasyVR`**. References: `VRMP`, XRI, XR CoreUtils, XR Hands, Netcode, Input System,
  TextMeshPro, **UnityEngine.UI** (added for `Slider`/`Button`).
- Config SO assets: `Assets/Game/ScriptableObjects/` (`CombatConfig`, `EnemyConfig`, `ComboConfig`,
  `LaneLayout`).
- Prefabs: `Assets/Game/Prefabs/` (`Sliceable_Damage.prefab`, `Potion_Heal.prefab`) and
  `Assets/Game/Prefabs/Materials/` (`Mat_Enemy`, `Mat_Sliceable`, `Mat_Potion`, `Mat_Angle`,
  `Mat_Blade` - URP/Lit).
- Scene: `Assets/Scenes/SampleScene.unity` (active build scene, index 0; `BasicScene` disabled).

## Scripts and responsibilities (all created in M1)

Config (`FantasyVR.Config`):
- `CombatConfig` - enemy HP, match duration, base damage/score per hit, **AnimationCurves** sampled by
  difficulty progress 0..1 (`SampleSpawnInterval`, `SampleObjectSpeed`, `SampleAngleRequiredProbability`),
  potion chance/min-interval/heal, player max/start health.
- `EnemyConfig` - rise duration, rise depth, hit-reaction duration, death duration.
- `ComboConfig` - tier thresholds + multipliers (`GetTier`, `GetMultiplier`), angle-correct bonus.
- `LaneLayout` - array of lanes (`spawnOffset`/`targetOffset` local to the combat rig).

Slicing (`FantasyVR.Combat.Slicing`):
- `Handedness` enum (Left=0, Right=1).
- `BladeController` - one per hand; binds to local rig via `XROrigin` + `XRInputModalityManager`,
  follows the active controller/hand anchor, computes smoothed `Velocity`, exposes `IsSlicing`
  (speed >= `m_MinSliceSpeed`, default 1.5) and `SliceDirection`; drives trail; `PlaySliceHaptics()`
  via `HapticImpulsePlayer` (controllers only).
- `Blade` - trigger collider child that exposes its owning `BladeController`.

Spawning (`FantasyVR.Spawning`):
- `SliceableObject` - kinematic flight along a lane; trigger + speed-gated detection in
  `OnTriggerEnter` (finds `Blade` via `GetComponentInParent`); optional required cut angle (wrong angle
  = no credit, object survives); harmless expiry -> miss. `Kind => Damage`.
- `PotionFlask : SliceableObject` - `Kind => Potion`.
- `SliceablePooler` / `PotionPooler` - subclasses of `XRMultiplayer.Pooler`.
- `ObjectSpawner` - owns both pools + `LaneLayout`; `Begin(config)`/`Stop()`; Update-driven timer
  (no per-frame allocs); `Progress` set by director; events `OnSliced(obj,angleCorrect)`,
  `OnMissed(obj)`, `OnSpawned(obj)`; tracks active list; returns to correct pool by `Kind`.

Scoring (`FantasyVR.Scoring`):
- `ComboSystem` - `RegisterHit(angleCorrect)`, `RegisterMiss()`, `Combo`/`Multiplier`/`Tier`,
  `OnComboChanged(combo,multiplier)`.
- `ScoreTracker` - `BeginMatch`, `AddSpawn/AddHit/AddMiss/AddPotion`, `BuildResult()`,
  `OnScoreChanged(score)`.
- `CombatResult` - struct (Score, DamageDealt, ObjectsSpawned, ObjectsSliced, HighestCombo,
  PotionsCollected, Duration, Accuracy).

Combat (`FantasyVR.Combat`):
- `PlayerHealth` - `Initialize(max,start)`, `Heal`, `Damage` (unused in M1), `OnHealthChanged`.
- `CombatDirector` - orchestrator. `StartCombat()` resets systems, `enemy.Initialize`+`Rise()`;
  on `OnRiseComplete` -> `spawner.Begin`; each Update feeds `spawner.Progress = 1 - enemy.HealthNormalized`;
  routes slices: Damage -> combo hit + `enemy.ApplyDamage(base*mult)` + score; Potion -> `Heal` + combo
  hit + potion count; Damage miss -> `combo.RegisterMiss()`; on `OnDied` -> `spawner.Stop()` +
  `flow.ShowScoreboard(result)`.

Enemy (`FantasyVR.Enemy`):
- `EnemySkeleton` - states Idle/Rising/Active/Dying/Dead; `Rise()` climbs up over `RiseDuration`;
  `ApplyDamage` (only when Active) with hit flash via `MaterialPropertyBlock`; death tween; events
  `OnRiseComplete`, `OnDamaged(cur,max)`, `OnDied`.

Flow (`FantasyVR.Flow`):
- `GameState` enum (Boot, Combat, Scoreboard, Town).
- `GameFlowManager` - `m_AutoStartCombat=true`; `StartCombat()` activates combat root + HUD, hides
  scoreboard; `ShowScoreboard(result)` hides HUD + shows scoreboard; `GoToTown()` is an **M1 stub**
  (logs and loops back into combat).

UI (`FantasyVR.UI`):
- `CombatHUD` - subscribes to enemy/combo/health/score events; enemy HP slider, combo + multiplier
  text, player HP slider, score text.
- `ScoreboardUI` - world-space panel; `Show(result, onPlayAgain, onReturnToTown)` / `Hide()`; buttons
  wired via template `XRMultiplayer.TextButton.UpdateButton`.

## Scene wiring (`SampleScene` -> root `FantasyVR_Combat`)

Player XR rig is at world `(0,0,-12)` facing `+Z`. Hierarchy built and wired (all private
`[SerializeField] m_*` fields set):

```
FantasyVR_Combat
  CombatRig            (pos 0,0,-12, identity rot; lane offsets are relative to this)
    Enemy              (local 0,0,4.5 -> world 0,0,-7.5)  [EnemySkeleton]
      Body             (capsule placeholder; m_Body + m_FlashRenderer)
    Spawner            [ObjectSpawner; m_RigRoot=CombatRig, m_Lanes=LaneLayout]
      SliceablePool    [SliceablePooler; m_SpawnPrefab=Sliceable_Damage, ParentUnderTransform]
      PotionPool       [PotionPooler;   m_SpawnPrefab=Potion_Heal,      ParentUnderTransform]
  Systems              [ComboSystem, ScoreTracker, PlayerHealth, CombatDirector]
  RightBlade           [BladeController hand=Right] -> Blade (trigger box) + BladeVisual + Tip(TrailRenderer)
  LeftBlade            [BladeController hand=Left]  -> Blade (...)
  CombatHUD            (world canvas, pos 0,2.4,-7.5, **identity rotation**, scale 0.003)
  Scoreboard           (world canvas, pos 0,1.6,-10.3, **identity rotation**, scale 0.0035,
                        GraphicRaycaster + TrackedDeviceGraphicRaycaster; Play Again primary/blue, Return to Town)
  GameFlowManager      [GameFlowManager; m_CombatRoot=CombatRig, m_HudRoot=CombatHUD, m_Scoreboard, m_Director]
```

- CombatDirector wiring confirmed: Config, Enemy, Spawner, Combo, Score, PlayerHealth, Flow all set.
- **UI orientation gotcha (fixed):** world-space canvases must use **identity rotation** (forward = +Z,
  same as the player's look direction) so text reads correctly. An earlier `Euler(0,180,0)` made text
  appear mirrored. Do **not** re-rotate these canvases 180 deg.

## Current config defaults (tunables)

`CombatConfig`: EnemyMaxHealth 1000, MatchTargetDuration 90, BaseDamagePerHit 10, BaseScorePerHit 100,
SpawnInterval curve 1.1s->0.45s, ObjectSpeed 2.2->4.0 m/s, AngleRequiredProb 0->0.5, PotionChance 0.12,
PotionMinInterval 6s, PotionHeal 20, PlayerMaxHealth/Start 100.
`ComboConfig`: thresholds {0,5,10,20} -> multipliers {1,2,4,8}, angle bonus +1.
`EnemyConfig`: RiseDuration 2s, RiseDepth 2m, HitReaction 0.12s, Death 1.5s.
`LaneLayout`: 5 lanes (left/centre/right at waist + two shoulder), spawn z=4 / target z=0.35 (rig-local).

## Verified in-editor (Play mode)

- Enemy rose to `Active`; spawner streamed (21 objects over ~26s); difficulty ramp worked.
- Applying lethal damage -> `Dying` -> death sequence -> `flowState=Scoreboard`, scoreboard visible,
  HUD hidden, `spawner.Stop()` returned all active objects to pools (0 leaked).
- Scoreboard stats populated (Best Combo / Accuracy / Sliced / Potions / Damage / Time).
- Rendered a from-player screenshot confirming UI is upright and readable (not mirrored).
- "Play Again" click path not re-validated live (editor exited play between calls), but uses the same
  `TextButton.UpdateButton` wiring that populated the scoreboard successfully.

## Build / platform state

- Active build target: **Android**. Scene list: `SampleScene` enabled @0.
- Player: bundle id **`com.fantasyvr.game`**, IL2CPP, ARM64, minSdk 30, targetSdk 32. Product `FantasyVR`.
- Android XR: OpenXR loader enabled for Android (`Assets/XR/XRGeneralSettingsPerBuildTarget.asset`).
- PC can't run Quest **Link**, so iteration is via **Build And Run** (APK install+launch over USB). Use
  **Patch / Patch And Run** for faster script-only redeploys.

## Known issues / to debug now

1. **Android build fails on OpenXR Project Validation NRE** (see "Immediate next actions"). Blocker.
2. **Diagnostics/Debug Symbols warning** - cosmetic; optional to address.
3. **UI overlap in edit mode** - HUD and Scoreboard are both active in the editor so they visually
   overlap in screenshots; at runtime they are mutually exclusive (verified). Optional: nudge HUD higher
   / disable Scoreboard root by default in the scene.
4. **Editor Play mode self-exits without an XR runtime** - benign; only affects in-editor testing
   without a headset/Link. On device it's fine.
5. **Combat objects share the Default physics layer** - detection is guarded by component checks, so
   stray trigger overlaps (environment/player) are harmless but do a little extra work. Future: a
   dedicated "Combat" layer + collision matrix.
6. **Haptics only fire on controllers** (hand-tracking has no `HapticImpulsePlayer`). Hand velocity uses
   transform-delta (no `XRHandJoint.TryGetLinearVelocity()` yet).
7. **Town is a stub**; `GoToTown()` loops back into combat (M4 will build the real hub).
8. **Enemy/sliceables/potions are primitive placeholders** (capsule/cubes) - art is M2.
9. Template runtime logs that are NOT our bugs and are safe to ignore for M1: Vivox credentials/server
   (no dashboard config), hand-tracking subsystem missing in editor, Netcode shutdown on play-exit, and
   a teardown-only `WorldCanvas.cs` MissingReference in the template.

## MCP workflow notes (save time next session)

- Unity MCP server id pattern: `FantasyVR@<hash>`. After (re)connect, call `set_active_instance` with
  `{"instance":"FantasyVR@<hash>"}`. Resource `mcpforunity://instances` lists sessions;
  `mcpforunity://editor_state` was NOT available in this setup.
- The MCP link **drops during long blocking operations** (platform switch, builds, domain reloads) and
  during Editor focus loss; just wait and re-fetch instances. The first Android platform switch took a
  few minutes (`isUpdating/isCompiling` true throughout).
- **Tool params use snake_case** that maps to Unity camelCase. Examples learned:
  - `manage_scriptable_object` create: `type_name`, `folder_path`, `asset_name`.
  - `manage_editor` actions include `play`/`stop` (no `get_state`).
  - `manage_components` is mutation-only (`add`/`remove`/`set_property`, args `target`,
    `component_type`); use `execute_code` to read component values.
  - `manage_build` actions: `build`, `status`, `platform`, `settings`, `scenes`, `profiles`, `batch`,
    `cancel`. `build` is async (poll `status`).
  - `find_gameobjects` uses `search_term` and returns instanceIDs only.
- **`execute_code`**: action `execute`; the code is wrapped in a method that **must `return` a value**;
  **no `using` directives** (use fully-qualified type names); `Object` is ambiguous - use
  `UnityEngine.Object.DestroyImmediate`. Lambdas work; local functions are risky (codedom compiler).
- **Setting private `[SerializeField] m_*` fields**: use reflection walking base types
  (`BindingFlags.Instance|NonPublic|Public`) then `EditorSceneManager.MarkAllScenesDirty()` +
  `SaveOpenScenes()`. Enum fields: `System.Enum.ToObject(fieldInfo.FieldType, intValue)`.
- **Screenshots** (URP): create a temp `Camera` + `UniversalAdditionalCameraData`, use
  `UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=rt}` +
  `SubmitRenderRequest`, then `ReadPixels`/`EncodeToPNG`. `Camera.Render()` does NOT work under URP.
  (Helper screenshots were written to `Temp/` which is gitignored.)
- Unity's bundled adb: `D:\UnityEditor\6000.4.2f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`.
- Shell here is **PowerShell** - `&&` is not a separator; run commands separately or use `;`. Invoke exe
  paths with the call operator `& "path\adb.exe" args`.

## VR playtest checklist (once it runs)

Slice with both hands; verify combo multiplier scales damage (x1->x2->x4->x8 at 5/10/20 hits); angle
(cyan-indicator) objects need an axis-matched cut; green potions heal; missing is harmless and resets
combo; enemy death -> scoreboard; Play Again restarts; Return to Town (stub) loops back. Tune
`m_MinSliceSpeed` on the two `*Blade` controllers and the `CombatConfig` curves for feel.

## Improvement backlog (post-build, for next chats)

- M1 tuning pass: slice-speed threshold, spawn rate/speed curves, combo tiers, lane positions/reach.
- Juice (M2): blade trails polish, slice halves/particle VFX (pooled), impact/combo/potion SFX, enemy
  hit/death feedback, HUD multiplier pulse.
- Clear visual language for required-angle objects (directional arrows).
- Real skeleton art + rise/idle/hit/death animation; arena environment + lighting/skybox.
- Dedicated Combat physics layer + collision matrix; consider swept/overlap tests for fast hands.
- Hand-tracking velocity via `XRHandJoint.TryGetLinearVelocity()`; haptics fallback for hands.
- Full GameFlowManager states + polished scoreboard; later the networked Town hub (M4).

## Commit message (drafted, not yet committed)

A full M1 commit message was drafted (see chat). New/modified files: all `Assets/Game/**` scripts +
meta, the 4 config `.asset`s, 2 prefabs + materials, `FantasyVR.Game.asmdef` (added `UnityEngine.UI`),
and `SampleScene.unity`. Incidental: `Packages/manifest.json`, `Packages/packages-lock.json`,
`Assets/VRMPAssets/Tutorial/TutorialMaterials/Video_Material_Unlit.mat`, and Android platform/player
setting changes. Not committed yet.
