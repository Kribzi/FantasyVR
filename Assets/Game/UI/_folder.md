# UI (`FantasyVR.UI`)

World-space VR UI (TextMeshPro + XRI UI input).

- In-combat HUD - enemy HP, combo/multiplier, player HP.
- `ScoreboardUI` - post-combat stats panel; `Play Again` (primary/default focus) + `Return to Town`,
  wired via `XRMultiplayer.TextButton.UpdateButton(action, label)`.

Model on the template's minigame scoreboard. See `Docs/02_ProjectKnowledgeBase.md` and
`Docs/04_CombatSystem.md`.
