# 10 - M2 Handoff (coins, victory/continue flow, dedicated combat scene)

Continuity doc for a fresh chat. Read `AGENTS.md` first, then this. M1 is done, committed, pushed, and
verified on a Quest 3 device. This doc captures the **current state** and the **next batch of work**
the player wants. Keep it tight; deep design lives in `Docs/01`-`08`, M1 details in `Docs/09`.

## Current state (M1, on disk + on device)

- Core combat loop works on Quest 3: skeleton rises -> sliceables/potions stream -> tip-velocity slicing
  (lighter wrist flicks register) -> combo-scaled damage -> enemy dies -> scoreboard.
- Enemy **ultimate**: every 10s a "slash formation" line of boxes (one of 4 patterns: left/right vertical,
  two diagonals) you clear in one swipe, then a second line ~2s later. All tunable in `CombatConfig`.
- Combo double-count bug fixed (angle bonus now only applies to angle-gated objects, which are currently
  disabled for the first enemy).
- Android/Quest build is green. Build via MCP `manage_build` (Android, `Builds/Android/FantasyVR.apk`,
  options `["auto_run","compress_lz4"]`). **After editing scripts, force a recompile before building**
  (`refresh_unity` force/all/request) or you get "script class layout incompatible editor vs player".

### Key files (all under `Assets/Game/`, asm `FantasyVR.Game`, ns `FantasyVR`)

- Flow: `Flow/GameFlowManager.cs` (Boot->Combat->Scoreboard->Town; `StartCombat`, `ShowScoreboard`,
  `GoToTown` is a stub that loops back to combat), `Flow/GameState.cs`.
- Combat: `Combat/CombatDirector.cs` (orchestrator), `Combat/PlayerHealth.cs`
  (`Initialize/Heal/Damage/OnHealthChanged` - **`Damage` is currently never called**).
- Enemy: `Enemy/EnemySkeleton.cs` (`OnRiseComplete`, `OnDamaged`, `OnDied`; death tween then `OnDied`).
- Spawning: `Spawning/ObjectSpawner.cs` (pools, lanes, ultimate), `SliceableObject.cs`, `PotionFlask.cs`,
  `SliceablePooler.cs`/`PotionPooler.cs` (subclass template `XRMultiplayer.Pooler`).
- Scoring: `Scoring/ComboSystem.cs`, `Scoring/ScoreTracker.cs`, `Scoring/CombatResult.cs`.
- UI: `UI/CombatHUD.cs`, `UI/ScoreboardUI.cs` (already titles the panel "VICTORY"; Play Again + Return to
  Town buttons via template `XRMultiplayer.TextButton.UpdateButton`).
- Config SOs: `ScriptableObjects/CombatConfig.asset`, `EnemyConfig.asset`, `ComboConfig.asset`,
  `LaneLayout.asset` (code in `Config/`).

### Scene (current)

- Active build scene: `Assets/Scenes/Dungeon.unity` (index 0; `SampleScene` index 1, `BasicScene` disabled).
  Combat lives under root `FantasyVR_Combat` (CombatRig, Enemy with skeleton swordsman, Spawner, Systems,
  Left/RightBlade, SliceDebris, CombatHUD, Scoreboard, GameFlowManager).
- **Birds ambience**: GameObject `Audio (Ambient Sounds)` in Dungeon.unity, clip
  `Assets/VRMPAssets/Audio/SFXClips/spring-birds-loop-with-low-cut-new-jersey-6267.mp3`.
- Useful existing SFX: `Assets/VRMPAssets/Audio/SFXClips/CoinSplash.wav` (coins),
  `.../HoverSound.wav`, `.../Button_22_Click.wav` (UI).

## What the player wants next (M2)

1. **Dedicated combat scene.** Move the fight into its own new scene (e.g.
   `Assets/Scenes/Combat.unity`) instead of `SampleScene`. Keep the **birds chirping** ambient audio from
   the template. Decide: either build the new scene from the template's XR rig + the `FantasyVR_Combat`
   root, or additively load combat. Keep the XR Interaction Setup (MP Variant) rig so hands/controllers
   and BladeController binding still work. Add the new scene to Build Settings (index 0 for instant boot)
   and keep it Quest-correct.

2. **Victory + coins on enemy defeat.** On `EnemySkeleton.OnDied`:
   - Show **"Victory"** (the scoreboard already shows VICTORY; this may become a lighter victory banner +
     a **Continue** button rather than the full scoreboard - see #4).
   - Award **coins**. Amount should scale with performance (score/combo/accuracy from `CombatResult`).

3. **Coin drop + magnet feel (the juice piece - make it feel great).**
   - Coins **explode out** from the enemy on death in a **small** burst (slight upward + outward impulse,
     scatter to the ground near the enemy). Not a huge explosion.
   - After ~**1.2s** (tunable), coins are **sucked up off the ground and fly to the player's waist**
     (accelerating ease-in, slight arc, converge to a point ~waist height on the XR rig), with a satisfying
     pickup pop per coin (stagger them) and `CoinSplash.wav`/chime. Tune count, spread, delay, suck speed.
   - Waist target: derive from the local rig - `XROrigin` camera position projected down to ~waist
     (e.g. camera world pos minus ~0.6-0.7m on Y), or a dedicated waist anchor under the rig. Reuse the
     pooling pattern (`XRMultiplayer.Pooler` subclass `CoinPooler`) - no per-frame allocs, Quest-first.
   - Suggested new pieces: `Coin.cs` (state: Scatter -> Wait -> Magnet -> Collected), `CoinFountain.cs`
     (spawns/aims the burst, drives magnet to waist), `CoinPooler.cs`, a `Coin` prefab + material, and a
     wallet (`CurrencyManager` or a `PlayerWallet` SO) that persists coins across fights/into Town.

4. **Continue -> harder enemy.** After victory, a **"Continue"** button appears **in front of the player**;
   pressing it starts the **next, harder** encounter. Implement difficulty scaling (e.g. an encounter/wave
   index in `GameFlowManager`/`CombatDirector` that raises `EnemyMaxHealth` and shifts the `CombatConfig`
   curves - faster spawns/speeds, enable angle-gated objects at higher tiers, stronger/again ultimate).
   Button via template `TextButton`, world-space, placed at a comfortable reach in front of the rig.

5. **Death -> scoreboard -> return to town.** If the **player dies**, show the existing scoreboard
   (`ShowScoreboard`) and then Return to Town.
   - **GAP to resolve first:** the player currently **cannot die**. `PlayerHealth.Damage` is never called;
     missing objects is harmless and the ultimate does not damage the player. To get a death state, decide
     a damage model (e.g. enemy ultimate/objects that reach the player deal damage, or a timer/enemy-attack)
     and wire `PlayerHealth.Damage` -> `OnHealthChanged` -> at 0 HP, `GameFlowManager` shows scoreboard
     then `GoToTown`. Confirm with the player how they want to take damage.

## Third-party assets (FANTASTIC Dungeon Pack)

- The combat now boots into a dungeon: `Assets/Scenes/Dungeon.unity` (build index 0), built on the
  Tidal Flask **FANTASTIC Dungeon Pack** (URP version). The player spawns in front of the throne and
  the fight starts immediately.
- The pack is **git-ignored** to keep the repo lean (`Assets/Fantastic Dungeon Pack/` imported folder
  and `Assets/FANTASTIC - Dungeon Pack/` raw download). **On a fresh clone you must re-import it** or
  the dungeon will show missing meshes/materials/lightmaps (combat itself is unaffected - it lives in
  `Assets/Game/`).
  - Re-import: double-click `Assets/FANTASTIC - Dungeon Pack/URP_FANTASTIC_Dungeon_Pack_U6-1.unitypackage`
    (the `.unitypackage` is the kept-on-disk re-import source; it is also git-ignored). Use the **URP**
    package, not Standard, or materials will be pink.
- `Assets/Game/Prefabs/FantasyVR_Combat.prefab` is the whole combat stack as a prefab (rig-independent;
  blades bind to the XR rig at runtime). Reuse it to drop combat into any scene.
- **Dungeon.unity layout**: duplicated from `demoscene_dungeon_level_1_dungeon` (no baked lightmaps, so
  the look transfers). The demo Camera/AudioListener was removed (the XR rig provides both). Added roots:
  XR rig (`XR Origin Hands (XR Rig) MP Template Variant`), `FantasyVR_Combat`, an `EventSystem`
  (`EventSystem` + `XRUIInputModule`) for the world-space combat UI, the birds `Audio (Ambient Sounds)`,
  and the template offline managers (`Network Manager VR Multiplayer`, `XRI Network Game Manager`,
  `Permissions Manager`) - the last three are required or the rig's town UI throws
  `NullReferenceException` (PlayerOptions/PlayerListUI) offline.
- **Spawn point gotcha**: the rig's start position is NOT just its transform - `CharacterResetter` (on the
  XR rig root) teleports the player to its serialized `offlinePosition` on `Start()`. For Dungeon.unity
  that field is `(-2, 4.2, -17.69)`: the player spawns in the lower hall facing **-Z (toward the throne)**,
  so the view is the enemy in the foreground with the throne/dais rising directly behind it. The teleport
  preserves the rig's authored yaw (XRI `MatchOrientation.WorldSpaceUp`), so the rig transform is also set
  to yaw 180; keep both in sync if you re-aim. To move the spawn, change `offlinePosition` (and the rig
  yaw), not just the rig transform.
- **Combat is anchored on the far (hall) side of the enemy.** The `FantasyVR_Combat` root is at
  `(-2, 4, -29.69)` rotated yaw 180 so `CombatRig` (the player/combat anchor) lands on the `y=4` hall floor
  at `(-2, 4, -17.69)` facing -Z. The hall floor there is ~1 m below the dais, so combat sits at `y=4`. The
  enemy is at `localPosition (0,0,4.5)`, world `(-2, 4, -22.19)` — grounded on the hall floor. Slicing
  converges on `CombatRig` (not the enemy visual); the health bar lives on `CombatHUD`.
- **Known non-blocking warnings** in Dungeon.unity: "visible additional lights 33 exceeds 32" (the dungeon
  is light-heavy - a Quest perf/lighting pass should cull/merge torch lights or raise the URP additional-
  lights budget); Vivox init failure + hand-tracking-subsystem-missing are editor-only/template noise (no
  voice in combat; hand tracking works on device).
- The combat enemy is now a **skeleton swordsman** from the BitGem **Low Poly Skeleton Crew** pack (see
  next section). The M1 white-capsule placeholder has been replaced.

## Third-party assets (Low Poly Skeleton Crew — BitGem)

- The enemy is a **skeleton swordsman** from the BitGem **Low Poly Skeleton Crew** pack, standing on the
  dungeon floor at `(-2, 4, -22.19)` facing the player. It replaced the M1 white-capsule placeholder.
- The pack is **git-ignored** (`Assets/BitGem/`). **On a fresh clone you must re-import it** from the Unity
  Asset Store or the enemy will appear as a missing-mesh pink figure (combat mechanics still work — slicing
  converges on `CombatRig`, not the enemy visual).
  - Re-import: download "Low Poly Skeleton Crew" from the Asset Store. The imported folder lands at
    `Assets/BitGem/`. No `.unitypackage` backup exists for this pack (unlike the Dungeon Pack).
  - **URP material fix required after import:** the pack ships with Built-in Standard shaders (pink under
    URP). The swordsman material at `Assets/BitGem/Skeleton_Swordsman/Materials/skeleton_swordsman_mat.mat`
    must be converted to **Universal Render Pipeline/Lit** with its original textures:
    - `_BaseMap` / `_MainTex` → `skel_archer_col.tga` (guid `27415e526daa94f7c952dcd10d45b5cf`)
    - `_MetallicGlossMap` → `sleleton_metal_unity.tga` (guid `a7f8c1fe43fb441239fda34d8ce45b03`)
    - `_EmissionMap` → `sleleton_sw_glow.tga` (guid `ba6b31f640f4a46bd965e54bd6eddbd4`)
    Or run **Edit > Rendering > Materials > Convert Selected Built-in Materials to URP** on the mat file.
- **Animator:** the pack's bundled controller is a demo reel (no parameters, cycles through every animation).
  It is **not used**. Instead, a minimal **Idle + Die** controller lives at
  `Assets/Game/Animation/EnemySkeleton.controller` (parameter: `Die` trigger, AnyState → Die transition).
  `EnemySkeleton.cs` now has optional `m_Animator`, `m_DieTrigger`, and `m_HitTrigger` fields; when an
  Animator is wired, the death animation plays instead of the placeholder fall-tween.
- **Enemy grounding:** the Enemy node in `FantasyVR_Combat` is at local `(0, 0, 4.5)` (world `(-2, 4, -22.19)`).
  The skeleton prefab instance is a child of Enemy, scaled `0.812` to ~2.2 m, rotated yaw 180 to face the
  player. Its feet sit on the `y=4` hall floor.
- **Slice debris:** a new `SliceDebrisSystem` (pooled, under `FantasyVR_Combat` root) spawns two hemisphere
  halves along the blade's cut direction when a damage orb is sliced. Halves fly apart with physics, bounce
  on the floor, and shrink out after 3 s. Allocation-free after warm-up, Quest-friendly (32-piece pool).
  Code: `Assets/Game/Combat/Slicing/SliceDebrisSystem.cs`. Wired from `SliceableObject.OnTriggerEnter`.
- **Faster combat:** `CombatConfig.asset` curves updated — orb speed `3.8 → 6.7 m/s` (was `2.2 → 4.0`),
  spawn interval `0.75 → 0.3 s` (was `1.1 → 0.45`). Gameplay feels significantly snappier.

## Combat feel & VR setup (latest session)

These are live in `Dungeon.unity` + the shared config assets (verified in-editor; not yet device-tested).

- **Player moved back + projectiles fly longer.** `CombatRig` moved back 2 m (world `(-2,4,-15.69)`), the
  XR rig `CharacterResetter.offlinePosition` moved with it to `(-2, 4.6, -15.69)`. The enemy stays pinned
  at world `(-2,4,-22.19)` (local Z bumped `4.5 → 6.5`). `LaneLayout` spawn offsets and
  `CombatConfig.m_UltimateSlashSpawnZ` were both extended `4.0 → 6.0` so objects still originate at the
  enemy and travel the longer distance to the player (more reaction time).
- **Player Y raised.** `offlinePosition.y` `4.2 → 4.6` (floor is `y≈4.0`, so the player now stands ~0.6 m
  above it — a taller vantage). To re-tune height, change only `CharacterResetter.offlinePosition.y` on the
  XR rig in `Dungeon.unity`. (On device, real head-tracking height adds on top of this base.)
- **Locomotion disabled, roomscale kept.** Under the XR rig `Locomotion`, the `Move`, `Turn`, and `Climb`
  GameObjects are deactivated (no stick movement/turning). `Teleportation` is **kept active on purpose** —
  `CharacterResetter` finds its `TeleportationProvider` via `GetComponentInChildren` (active-only) to teleport
  the player to spawn, so disabling it NREs and breaks spawning. The dungeon has **0 TeleportationAreas/
  Anchors**, so the player still can't teleport anywhere. `Gravity` stays on. Roomscale head/hand tracking is
  untouched, so physical walking still maps into the game.
- **Misses now hurt (player can die).** A damage object that reaches the player unsliced deals
  `CombatConfig.m_PlayerMissDamage` (default `10`). At 0 HP, `CombatDirector` stops the spawner and shows the
  scoreboard (loss). Potions are still harmless to miss. (Verified: with no hand tracking in-editor, all
  objects miss → player dies → scoreboard, no exceptions.) This also closes the M2 "player can't die" gap.
- **Ultimate now telegraphs + has a focus window.** The ultimate is a coroutine in `ObjectSpawner`:
  1) fires `OnUltimateTelegraph` → `EnemySkeleton.PlayAttack()` (plays the `Attack` state =
  `Skeleton_Swordman_Frenzied_Slash`) and **pauses normal spawns**; 2) after
  `CombatConfig.m_UltimateTelegraphLead` (0.9 s) launches the first slash wave; 3) second wave after
  `m_UltimateSecondWaveDelay`; 4) normals resume after `m_UltimatePauseAfter` (1.0 s). So the player only
  faces the slash formation during the ultimate. New animator state `Attack` (+ `Attack` trigger,
  AnyState→Attack, Attack→Idle) added to `Assets/Game/Animation/EnemySkeleton.controller`;
  `EnemySkeleton.m_AttackTrigger = "Attack"`.

## Reuse, don't reinvent (from template, per AGENTS.md)

- `XRMultiplayer.Pooler` for all coin/object pooling. `XRMultiplayer.TextButton.UpdateButton` for buttons.
- `Unity.XR.CoreUtils.XROrigin` to find the local rig/camera for the waist target (BladeController already
  binds via `XROrigin` + `XRInputModalityManager`).
- Combat stays **single-player/local** (no networking). Town hub networking is later (M4).

## Open decisions for the next chat to confirm with the player

- New scene name + whether combat is single-scene or additively loaded over a town/base scene.
- Coin economy: coins-per-kill formula, do coins persist (PlayerPrefs vs SO) and where they're spent.
- Damage model for player death (#5 gap).
- Victory UX: lightweight banner + Continue for wins, full scoreboard only on death? Or scoreboard always?

## MCP / build quickrefs

- Set active instance after connect: `set_active_instance {"instance":"FantasyVR@<hash>"}`; list via
  `mcpforunity://instances`. The link drops during builds/domain reloads - wait and reconnect.
- Read errors: `read_console` (types `["error"]`). Check `CombatConfig` layout after edits via
  `execute_code` before building.
- Build is async: `manage_build` action `build`, poll `status` with the returned `job_id` (also confirm by
  APK timestamp at `Builds/Android/FantasyVR.apk`). Unity adb at
  `D:\UnityEditor\6000.4.2f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`.
  Quest 3 id `2G0YC5ZH1S0348`. Shell is PowerShell (`;` not `&&`; call exes with `&`).
