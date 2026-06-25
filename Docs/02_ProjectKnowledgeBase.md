# 02 - Project Knowledge Base (the existing template)

This project is built on Unity's **VR Multiplayer Template**. Everything under `Assets/VRMPAssets/`
is template code (assembly `VRMP`, namespace `XRMultiplayer`). Do not modify template files unless
necessary; build our game on top of it in `Assets/Game/`. This doc lists what already exists and the
exact APIs to reuse so we never reinvent solved problems.

## Engine & packages

- Unity `6000.4.2f1`.
- Source of truth for packages: [Packages/manifest.json](../Packages/manifest.json).

Key installed packages and what we use them for:

| Package | Version | Used for |
| --- | --- | --- |
| `com.unity.xr.interaction.toolkit` | 3.4.0 | XR rig, interactors, hands/controllers, UI input. |
| `com.unity.xr.hands` | 1.7.3 | Hand tracking joints (incl. velocity APIs). |
| `com.unity.xr.openxr` | 1.16.1 | OpenXR runtime (PCVR + base). |
| `com.unity.xr.androidxr-openxr` | 1.2.0 | Meta Quest / Android XR OpenXR feature set. |
| `com.unity.netcode.gameobjects` | 2.11.0 | Networking (town hub). GameObject-based, NOT ECS. |
| `com.unity.services.multiplayer` | 2.1.3 | Sessions, lobby, relay (Distributed Authority). |
| `com.unity.services.authentication` | 3.6.1 | Anonymous sign-in. |
| `com.unity.services.vivox` | 16.10.0 | Voice chat in the hub. |
| `com.unity.render-pipelines.universal` | 17.4.0 | URP (mobile/VR rendering). |
| `com.unity.inputsystem` | 1.19.0 | Input. |
| `com.unity.multiplayer.playmode` | 2.0.2 | Multi-instance testing in editor. |

There is **no** `com.unity.entities` package. This project does not use ECS/DOTS.

## Scenes

| Scene | Path | Role |
| --- | --- | --- |
| SampleScene | `Assets/Scenes/SampleScene.unity` | Active build scene. Full template hub: environment, managers, networking, UI, projectile pool, teleport anchors. This is where we build. |
| BasicScene | `Assets/Scenes/BasicScene.unity` | Disabled in build. Minimal sandbox. |

The template uses a **single-scene architecture**. There are no `NetworkManager.SceneManager.LoadScene`
calls in template scripts; match/area changes are done with **in-scene state + teleports**, not scene
swaps. We follow this model (Town and Combat are areas/states within one scene, at least for M1).

## Connection & session flow

The bootstrap chain (all under `Assets/VRMPAssets/Scripts/Network/NetworkManagers/`):

1. `NetworkManagerVRMultiplayer.cs` - subclass of NGO `NetworkManager`. Sets config in `Awake()`.
   Player prefab is `XRI_Network_Player_Avatar.prefab`. Scene management is enabled but unused by code.
2. `XRINetworkGameManager.cs` - singleton orchestrator. Authenticates on `Awake()`, exposes connection
   state.
3. `AuthenticationManager.cs` - `UnityServices.InitializeAsync()` + `SignInAnonymouslyAsync()`.
4. `SessionManager.cs` - `SessionType.DistributedAuthority` (UGS lobby + relay) or `LocalOnly`
   (direct IP). `QuickJoinLobby()`, `JoinLobby()`, `CreateSession()`, `LeaveSession()`.

Reusable connection APIs:
- Am I online? `XRINetworkGameManager.Connected.Value` (a `BindableVariable<bool>` - subscribe to it).
- Join a room: `XRINetworkGameManager.Instance.QuickJoinLobby()` or via `LobbyUI`.
- Local player reference: `XRINetworkPlayer.LocalPlayer` with `head`, `leftHand`, `rightHand`
  transforms.
- Toggle objects by online/offline state: `ConnectionToggler`
  (`Assets/VRMPAssets/Scripts/Player/ConnectionToggler.cs`) - `objectsToEnableOnline` /
  `objectsToEnableOffline` arrays.

## Object pooling (reuse for combat spawns)

`Assets/VRMPAssets/Scripts/Helpers/Pooler.cs` wraps `UnityEngine.Pool.ObjectPool<GameObject>`.

API:
- `GameObject GetItem()` - get/create an active instance.
- `void ReturnItem(GameObject item)` - deactivate and return to pool.
- Config fields: `m_SpawnPrefab`, `m_DefaultCapacity` (30), `m_MaxCapacity` (1000),
  `m_ParentUnderTransform`.

`PoolerProjectiles.cs` is an empty subclass (semantic alias). The SampleScene has a `Pool_Projectiles`
instance using `Assets/VRMPAssets/Prefabs/SphereProjectile.prefab`.

Usage pattern (from `NetworkProjectileLauncher.cs` / `Gameplay/TargetPractice/Projectile.cs`):

```csharp
GameObject go = pooler.GetItem();
go.transform.SetPositionAndRotation(spawnPos, spawnRot);
// configure movement (e.g. Rigidbody velocity)
// when consumed/expired:
pooler.ReturnItem(go);
```

This is client-side and non-networked - exactly what we want for local, high-frequency combat objects.
We will create our own `Pooler` subclass(es) for sliceable objects and potions.

`NetworkInteractableSpawner` is a different, network-spawn pattern for sandbox interactables - NOT for
fast combat objects. Don't use it for slicing.

## Hands, controllers & velocity

There is **no built-in hand velocity API** in the template. Relevant references:

- `Assets/VRMPAssets/Scripts/Network/NetworkPlayer/XRHandPoseReplicator.cs` - shows how the local rig
  is found: `XROrigin` + `XRInputModalityManager`. In controller mode uses
  `XRInputModalityManager.leftController` / `rightController` transforms; in hand-tracking mode uses
  `XRHandSkeletonDriver.rootTransform`. It syncs finger curl, not velocity.
- `Assets/VRMPAssets/Scripts/Player/JointBasedHand.cs` - remote avatar finger curling (`SetCurl()`),
  visual only.
- `Assets/VRMPAssets/Scripts/Network/NetworkInteractions/NetworkPhysicsInteractable.cs` has a
  `GetWorldVelocity()` that derives velocity from transform deltas - the pattern we copy for blades.

For slicing we will:
1. Track each hand/controller transform every frame from the local XR rig.
2. Compute velocity as `(currentPos - lastPos) / Time.deltaTime` (or use
   `XRHandJoint.TryGetLinearVelocity()` from `com.unity.xr.hands` when in tracked-hand mode).
3. Attach blade colliders to the controller/hand transforms on the XR Origin (NOT the networked hand
   meshes, which are disabled for the local player).

## Player rig & prefabs

- Local XR rig (scene-placed):
  `Assets/VRMPAssets/Prefabs/PrefabVariants/XR Origin Hands (XR Rig) MP Template Variant.prefab`.
  Contains controllers, hand-tracking anchors, locomotion, XRI interactors, and a nested
  `Offline_Player_Avatar`.
- Full player + lobby UI stack: `Assets/VRMPAssets/Prefabs/PlayerPrefabs/XRMPT_XR_Origin_Setup.prefab`.
- Networked player avatar (spawned by NGO): `Assets/VRMPAssets/Prefabs/PlayerPrefabs/XRI_Network_Player_Avatar.prefab`
  with `XRINetworkPlayer`, `XRHandPoseReplicator`, `XRAvatarVisuals`, `XRAvatarIK`, and `head` /
  `leftHand` / `rightHand` transforms.

## UI system

- World-space canvases + TextMeshPro throughout. XRI UI input via `EventSystem` + `XRUIInputModule`.
- Dynamic buttons use the `TextButton` struct in `Assets/VRMPAssets/Scripts/Helpers/Utils.cs`:

```csharp
// TextButton has a Button + TMP_Text; wire it like:
myTextButton.UpdateButton(OnPlayAgain, "Play Again");
myTextButton.UpdateButton(OnReturnToTown, "Return to Town");
```

- Useful UI scripts: `UI/OfflineMenu.cs`, `UI/LobbyList/LobbyUI.cs`, `UI/PlayerOptions.cs`,
  `UI/GreetingBoardUI.cs`, `UI/PopoutUI.cs` (face-locked panel), `UI/IntButtonUI.cs`.
- UI prefab library under `Assets/VRMPAssets/Prefabs/UIPrefabs/`.

## Match state-machine reference: MiniGameManager

`Assets/VRMPAssets/MiniGames/MiniGameScripts/MiniGameManager.cs` is the closest existing pattern to
our combat/scoreboard flow and the best template to learn from:
- A `GameState` enum (PreGame -> InGame -> PostGame) drives the flow.
- World-space scoreboard with TMP text and `ScoreboardSlot` prefabs.
- A dynamic `TextButton` toggles between Join / Wait / countdown.
- `TeleportToArea()` moves players between zones (uses XRI `TeleportationProvider`).
- Post-game auto-resets after a countdown.

We model `GameFlowManager` + the scoreboard UI on this, but our combat itself is local (single-player),
so we will NOT use its networked `NetworkVariable`/RPC scoring for combat.

## Other gameplay scripts worth knowing

Under `Assets/VRMPAssets/Scripts/Gameplay/`:
- `AntiGravityZone.cs`, `SimpleFan.cs` - force volumes (physics reference).
- `NetworkObjectDestroyer.cs` - trigger despawn + local `Pooler` FX.
- `TargetPractice/` - `TargetManager`, `Target`, `Projectile` (pooled projectile + hit detection -
  good reference for our sliceable hit detection).
- `RoomMusic.cs`, `MessageBoard/`, `Drawing/` - hub social features (reusable in Town).

## Quick reference table

| Need | Use |
| --- | --- |
| Am I online? | `XRINetworkGameManager.Connected.Value` |
| Join room | `XRINetworkGameManager.Instance.QuickJoinLobby()` |
| Pool spawn / despawn | `Pooler.GetItem()` / `Pooler.ReturnItem()` |
| Local hand transform | `XRInputModalityManager.leftController/rightController` or hand skeleton roots |
| Hand velocity | Transform delta, or `XRHandJoint.TryGetLinearVelocity()` |
| Match state pattern | `MiniGameManager.GameState` + teleports |
| Dynamic UI button | `TextButton.UpdateButton(action, label)` |
| Local network player | `XRINetworkPlayer.LocalPlayer` (`head`/`leftHand`/`rightHand`) |
