# FantasyVR Knowledge Base

This folder is the single source of truth for the FantasyVR project. Read it before implementing
anything. It is written so that an AI agent (or a new developer) can pick up any feature and build
it quickly, consistently, and in line with the architecture we have committed to.

## What this game is (one paragraph)

FantasyVR is a VR rhythm-combat game for Meta Quest (with PCVR/Steam as a secondary target). You are
dropped straight into a beautiful outdoor arena. A skeleton rises from the ground in front of you and
combat begins. Objects stream toward you through the gap between you and the enemy; you slice them
with your left and right hands (held like swords). Hitting an object - and hitting it at the correct
angle - deals damage to the enemy, scaled by your current combo multiplier. Occasionally a potion
flask flies in; slice it to heal. Missing never hurts you. When the enemy dies you get a scoreboard,
then choose **Play Again** (the prominent button) or **Return to Town** - a shared multiplayer hub
where players hang out before the next match.

## How to use this knowledge base

1. Start with `01_GameDesignDocument.md` to understand the player experience and rules.
2. Read `02_ProjectKnowledgeBase.md` to learn what the existing Unity VR Multiplayer Template already
   gives us (and the exact APIs to reuse - do not reinvent these).
3. Read `03_Architecture.md` for the code structure, namespaces, and how our game layer plugs into the
   template.
4. For feature work, consult the relevant deep-dive doc (`04_CombatSystem.md`, `05_Multiplayer.md`).
5. `06_QuestPublishing.md`, `07_Roadmap.md`, and `08_CodingConventions.md` cover shipping, sequencing,
   and code style.

## Document index

| Doc | Purpose |
| --- | --- |
| [01_GameDesignDocument.md](01_GameDesignDocument.md) | Vision, core loop, combat rules, scoring, flow, scoreboard, town hub. |
| [02_ProjectKnowledgeBase.md](02_ProjectKnowledgeBase.md) | What the VR Multiplayer Template provides and the exact reusable APIs. |
| [03_Architecture.md](03_Architecture.md) | Code architecture, `Assets/Game/` layout, namespaces, `GameFlowManager`, template integration. |
| [04_CombatSystem.md](04_CombatSystem.md) | Detailed design of slicing, spawning, combo, potion, enemy, and the combat director. |
| [05_Multiplayer.md](05_Multiplayer.md) | Instanced combat + shared town model, Distributed Authority, co-op extension path. |
| [06_QuestPublishing.md](06_QuestPublishing.md) | Meta Quest Store build/perf/submission checklist + Steam/PCVR notes. |
| [07_Roadmap.md](07_Roadmap.md) | Milestone backlog. M1 = core combat loop. |
| [08_CodingConventions.md](08_CodingConventions.md) | Project code conventions (and why the global ECS rules do not apply here). |

## Locked decisions (do not relitigate without an explicit request)

- **Combat is single-player instanced.** Each player slices their own arena; there is no networked
  enemy/projectile state. See `05_Multiplayer.md`.
- **The town square is the shared multiplayer hub.** This is where networking actually happens, reusing
  the template's session/lobby/voice stack.
- **Gameplay is GameObject + MonoBehaviour** (template-native), NOT ECS/DOTS. There is no Entities
  package in this project. See `08_CodingConventions.md`.
- **Target platform is Meta Quest** (Android, OpenXR + AndroidXR). PCVR/Steam (desktop OpenXR) is a
  secondary, nice-to-have target.
- **First milestone is the core combat loop** (skeleton rises -> objects fly -> slice to damage ->
  scoreboard), playable in VR. See `07_Roadmap.md`.

## Project facts

- Unity `6000.4.2f1`.
- Built on Unity's **VR Multiplayer Template** (`Assets/VRMPAssets/`, assembly `VRMP`, namespace
  `XRMultiplayer`).
- Active build scene: `Assets/Scenes/SampleScene.unity`.
- Our game code lives under `Assets/Game/` (assembly `FantasyVR.Game`, namespace `FantasyVR`).

## A note on tooling

There is no Unity MCP integration available in this workspace, so agents work from the project files
on disk. If live Editor automation is wanted later, a Unity MCP server must be configured in Cursor's
MCP settings. Creating scenes/prefabs and wiring components is currently a manual step in the Editor;
docs call out exactly what needs wiring.
