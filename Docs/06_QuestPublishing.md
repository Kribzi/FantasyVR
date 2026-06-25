# 06 - Publishing (Meta Quest Store primary, Steam/PCVR secondary)

The end goal is a published, multiplayer-capable title on the Meta Quest Store. PCVR/Steam is a
secondary target we keep viable by staying on OpenXR. This doc is the shipping checklist and the
performance contract the whole team builds against.

## Target hardware

- **Primary:** Meta Quest 2 / 3 / 3S (standalone Android, mobile GPU). Quest 3/3S is the comfortable
  baseline; Quest 2 is the floor to validate against.
- **Secondary:** PCVR via OpenXR (SteamVR / Oculus PC runtime).

## Performance contract (Quest)

Treat these as hard budgets; combat must hold framerate with many objects in flight.

- **Frame rate:** 72 Hz minimum, target 90 Hz (Quest 3). Never ship below 72 with drops.
- **CPU/GPU frame time:** stay within the headset's budget (~13.8 ms at 72 Hz). Profile on device.
- **Draw calls / SetPass:** keep low; use the URP **Performant** settings, static/dynamic batching,
  GPU instancing, and texture atlases. Combat objects share materials.
- **Stereo rendering:** Single Pass Instanced.
- **Foveated rendering:** enable Fixed Foveated Rendering (FFR) on Quest.
- **Polys/overdraw:** mind transparent overdraw (slice VFX, trails) - the biggest mobile-VR killer.
  Keep particles bounded and pooled.
- **Memory/GC:** pool everything in combat (objects, VFX). Zero per-frame allocations in the hot path.
- **Physics:** combat uses trigger overlaps, not heavy rigidbody simulation; cap active object count.

Tools: Unity Profiler (connected to device), Frame Debugger, OVR Metrics Tool / Meta's perf overlays,
URP Rendering Debugger.

## Project / build configuration (Quest, Android)

- Build target: **Android**, ARM64, IL2CPP, Vulkan graphics API (Quest), .NET runtime per Unity 6
  defaults.
- XR: OpenXR enabled for Android with the **Meta/Android XR** feature groups
  (`com.unity.xr.androidxr-openxr`, `com.unity.xr.openxr`). Interaction profiles for Quest Touch +
  hand tracking enabled. Settings live under `Assets/XR/Settings/` (OpenXR Package/Editor Settings).
- Color space: **Linear**. URP asset: use the performant variant
  (`Assets/VRMPAssets/Settings/URP-Performant.asset`) for Quest.
- Minimum/target Android API level per current Meta requirements (verify against the latest Meta Quest
  submission docs at submission time).
- Orientation/splash/icons: provide app icon set; configure the VR splash.
- Scripting backend IL2CPP, managed stripping at a safe level (validate no reflection breakage in UGS).

## Meta Quest Store submission checklist

Verify against Meta's current developer docs at submission time (requirements change):

- **Account/org:** Meta Quest developer org set up, app created in the Developer Dashboard, data-use
  checkup completed.
- **Packaging:** signed Android App Bundle/APK, correct package name + version code/name, 64-bit.
- **Comfort & VR conformance:** passes Meta's Virtual Reality Checks (VRCs) - tracking, performance,
  no nausea-inducing forced movement without comfort options, proper boundary/guardian behavior,
  graceful focus/loss (pause on headset removal).
- **Permissions:** declare only what's used (microphone for voice chat is the notable one - justify it;
  the template uses Vivox). Hand-tracking permission as needed.
- **Multiplayer/online:** privacy policy, age rating, and any platform requirements for online play and
  voice chat. Vivox/UGS service configuration in the Unity Dashboard (project linked, services
  enabled).
- **Store assets:** title, descriptions, key art, screenshots, trailer, content rating (IARC), comfort
  rating.
- **Stability:** no crashes on launch/standby/resume; clean shutdown; reconnect handling for the hub.
- **Performance:** sustained target framerate on the minimum supported device.

## Comfort & input (applies to both platforms)

- Provide comfort options (snap turn / smooth turn toggle, vignette on locomotion) - the template's
  locomotion stack already includes these via XRI; surface the toggles in `PlayerOptions`.
- Combat is stationary (player stands and slices) which is inherently comfortable - keep it that way.
- Support both controllers and hand tracking for slicing (the rig supports both; blades bind to either,
  see `04_CombatSystem.md`).
- Pause/handle headset-off (focus lost) and recenter.

## Steam / PCVR (secondary)

We stay PCVR-viable by remaining on OpenXR:
- Add a **Windows (PC) standalone** build target with OpenXR enabled for desktop and the relevant
  interaction profiles (SteamVR runtime / Oculus). Most code is platform-agnostic.
- Differences to handle: higher perf budget (can raise quality), desktop input edge cases, Steam-only
  features are out of scope unless we add the Steamworks SDK later (achievements, friends).
- Keep platform-specific bits behind the template's `PlatformUnderstanding` /
  `NetworkedPlatformSpecificVisuals` pattern where they already exist.
- Don't let PCVR-only assumptions creep into combat tuning (hand velocity, reach) - tune for Quest
  first, validate on PCVR.

## Pre-launch QA pass (high level)

- Performance captured on Quest 2 and Quest 3 (sustained framerate during peak object count).
- Full loop tested on-device: boot -> combat -> scoreboard -> play again -> town -> start match.
- Multiplayer hub tested with 2+ real headsets (join, voice, see avatars, leave).
- Standby/resume, headset-off, and disconnect/reconnect all handled gracefully.
- No allocations spikes / hitches in combat (Profiler clean).
