# Slicing (`FantasyVR.Combat.Slicing`)

Hand-blade tracking and slice detection.

- `BladeController` (one per hand) - binds to the local XR rig hand/controller, computes per-frame
  velocity (transform delta or `XRHandJoint.TryGetLinearVelocity()`), exposes `IsSlicing` +
  `SliceDirection`, drives blade collider/trail/haptics.
- `Blade` - visual + collider component on the blade.

Bind to the LOCAL rig hands/controllers, not the networked avatar hands. See `Docs/04_CombatSystem.md`
and the hands section of `Docs/02_ProjectKnowledgeBase.md`.
