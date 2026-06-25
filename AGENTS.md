# AGENTS.md - FantasyVR

Project guide for AI agents and developers. Read this first, then the knowledge base in `Docs/`.

## What this is

A VR rhythm-combat game for **Meta Quest** (PCVR/Steam secondary). Drop straight into a fight: a
skeleton rises, objects stream toward you, slice them with hand "swords" to deal combo-scaled damage,
potions heal, missing is harmless. Enemy dies -> scoreboard -> Play Again or Return to a shared
multiplayer **Town hub**. Full design lives in [Docs/01_GameDesignDocument.md](Docs/01_GameDesignDocument.md).

## Tech stack (the real one)

- Unity `6000.4.2f1`.
- Built on Unity's **VR Multiplayer Template** (`Assets/VRMPAssets/`, assembly `VRMP`, namespace
  `XRMultiplayer`).
- **Netcode for GameObjects** + Unity Gaming Services (Sessions/Lobby/Relay, Distributed Authority) +
  Vivox voice.
- XR: OpenXR + AndroidXR (Quest) + XR Interaction Toolkit 3.4, hand tracking.
- URP for rendering. Active build scene: `Assets/Scenes/SampleScene.unity`.

## Critical rules (do not violate)

1. **No ECS/DOTS.** This project is 100% GameObject + MonoBehaviour + ScriptableObject. There is no
   `com.unity.entities` package. Any global/user rule describing Entities/ISystem/Baker/SystemAPI
   **does not apply here** - ignore it for this repo. See
   [Docs/08_CodingConventions.md](Docs/08_CodingConventions.md).
2. **Combat is single-player and local.** No networking in the combat simulation. Networking only
   happens in the Town hub via the template's stack. See [Docs/05_Multiplayer.md](Docs/05_Multiplayer.md).
3. **Build on top of the template; don't fork it.** Our code lives in `Assets/Game/` (assembly
   `FantasyVR.Game`, namespace `FantasyVR`). Avoid editing `Assets/VRMPAssets/`; extend/subclass
   instead.
4. **Reuse template systems** - do not reinvent: `Pooler` (pooling),
   `XRINetworkGameManager`/`SessionManager` (connection), `TextButton` (UI buttons), XRI
   `TeleportationProvider` (area moves). Exact APIs in
   [Docs/02_ProjectKnowledgeBase.md](Docs/02_ProjectKnowledgeBase.md).
5. **Quest-first performance.** Pool everything, zero per-frame allocations in combat, watch transparent
   overdraw, single-pass instanced, FFR. See [Docs/06_QuestPublishing.md](Docs/06_QuestPublishing.md).

## Where things go

```
Assets/Game/            our code (assembly FantasyVR.Game, namespace FantasyVR)
  Flow/ Combat/ Combat/Slicing/ Enemy/ Spawning/ Scoring/ UI/ Town/ Config/
  ScriptableObjects/ Prefabs/
Assets/VRMPAssets/      template (do not edit; reference via VRMP assembly)
Docs/                   knowledge base (source of truth)
```

Conventions: one type per file, `PascalCase` types, `[SerializeField] m_field` style (matches
template), config in ScriptableObjects. Full details in
[Docs/08_CodingConventions.md](Docs/08_CodingConventions.md).

## Current status & what to do next

- Done: knowledge base (`Docs/`) and `Assets/Game/` scaffold + assembly.
- Next: **M1 - core combat loop** (skeleton rises -> objects fly -> slice for combo-scaled damage ->
  potions heal -> enemy dies -> scoreboard). Implementation-ready spec in
  [Docs/04_CombatSystem.md](Docs/04_CombatSystem.md); task list in
  [Docs/07_Roadmap.md](Docs/07_Roadmap.md).

## Tooling notes

- No Unity MCP server is configured in this workspace; agents work from files on disk. Creating
  scenes/prefabs and wiring components in the Editor is a manual step - docs specify exactly what to
  wire.
- Multiplayer Play Mode (`com.unity.multiplayer.playmode`) is available for in-editor multi-client hub
  testing; `LocalOnly` sessions for quick networking checks.
