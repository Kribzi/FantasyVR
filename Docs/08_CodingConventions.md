# 08 - Coding Conventions

## IMPORTANT: this project does NOT use ECS/DOTS

There is a global/user rule set that describes a heavy Unity ECS / Entities (1.3.14) workflow
(IComponentData, ISystem, Baker, SystemAPI, etc.). **Those rules do not apply to this project.**

- This project has **no** `com.unity.entities` package installed (check
  [Packages/manifest.json](../Packages/manifest.json)).
- It is built on the VR Multiplayer Template, which is entirely **GameObject + MonoBehaviour +
  Netcode for GameObjects**.
- We deliberately chose GameObject/MonoBehaviour for all gameplay (see `README.md` locked decisions).
- Do not introduce ECS, `IComponentData`, `ISystem`, Bakers, or `SystemAPI` here. If a future task
  genuinely needs DOTS, it must be a separate, explicitly-approved decision (ECS does not integrate
  with Netcode for GameObjects anyway).

If the ECS rules ever conflict with this document, **this document wins for this repository.**

## Architecture style

- **MonoBehaviour** components for behavior, composed on GameObjects/prefabs.
- **ScriptableObject** assets for tunable data/config (combat, enemy, combo). Designers tune without
  recompiling.
- **Events / C# Actions** (or `UnityEvent` where inspector wiring helps) for decoupling systems
  (e.g. `EnemySkeleton.OnDied`, `ComboSystem.OnComboChanged`).
- Prefer small, single-responsibility components over god objects. `CombatDirector` orchestrates;
  individual systems do one thing.
- Object pooling for anything spawned frequently - reuse `XRMultiplayer.Pooler`.

## Project structure & assemblies

- Our code lives in `Assets/Game/` only. Do not edit `Assets/VRMPAssets/` (template) unless strictly
  necessary; prefer extending/subclassing.
- Assembly: `FantasyVR.Game` (`Assets/Game/FantasyVR.Game.asmdef`). It references `VRMP` and the XR /
  Netcode / Input / TMP assemblies so we can call template code.
- One primary type per file; file name matches the type name.

## Namespaces

- Root: `FantasyVR`.
- Sub-namespace follows folder, e.g.:
  - `FantasyVR.Flow`, `FantasyVR.Combat`, `FantasyVR.Combat.Slicing`, `FantasyVR.Enemy`,
    `FantasyVR.Spawning`, `FantasyVR.Scoring`, `FantasyVR.UI`, `FantasyVR.Town`, `FantasyVR.Config`.

## Naming

- Types: `PascalCase` (`CombatDirector`, `SliceableObject`).
- Methods/properties: `PascalCase`. Local vars/parameters: `camelCase`.
- Private serialized fields: `[SerializeField] Type m_FieldName;` (matches the template's
  `m_`-prefixed style for consistency across the codebase).
- Constants: `k_ConstantName` (template style) or `PascalCase` for public consts.
- ScriptableObject config assets: descriptive asset names under `Assets/Game/ScriptableObjects/`.

## Patterns to reuse from the template

- Connection state: subscribe to `XRINetworkGameManager.Connected` (a `BindableVariable<bool>`).
- Pooling: subclass `XRMultiplayer.Pooler`; use `GetItem()` / `ReturnItem()`.
- Dynamic UI buttons: `XRMultiplayer.TextButton.UpdateButton(action, label)`.
- Logging: the template uses `XRMultiplayer.Utils.Log/LogWarning/LogError`. Use standard
  `Debug.Log`/our own tag for game code; keep logs out of per-frame hot paths.
- Area transitions: XRI `TeleportationProvider`, mirroring `MiniGameManager.TeleportToArea`.

## Performance rules (Quest-first - see `06_QuestPublishing.md`)

- **Zero per-frame allocations** in combat hot paths (slicing, spawning, scoring). No LINQ, no
  `foreach` over allocating enumerables, no `new` in `Update`.
- Pool all spawned objects and VFX.
- Cache component references in `Awake`/`Start`; never `GetComponent` in `Update`.
- Mind transparent overdraw (trails/slice VFX). Share materials; enable GPU instancing.
- Avoid heavy physics; combat uses trigger/overlap detection with a capped active-object count.

## XR specifics

- Support both controllers and hand tracking (the rig provides both). Blades bind to whichever is
  active; see `04_CombatSystem.md`.
- Use the Input System and XRI; don't poll legacy input.
- Keep the player stationary during combat (comfort); locomotion is a town/hub concern handled by the
  template.

## Comments & docs

- XML doc comments on public types/members that other systems consume.
- Comments explain intent/constraints, not the obvious. Update the relevant `Docs/` file when a system
  design changes.

## Testing

- Use Multiplayer Play Mode for hub testing; `LocalOnly` sessions for quick networking checks.
- Validate combat performance on-device early and often (Profiler connected to Quest).
