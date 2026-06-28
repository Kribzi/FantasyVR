using UnityEngine;

namespace FantasyVR.Config
{
    /// <summary>
    /// Match-level tuning for one combat encounter. Curves are sampled by normalised difficulty
    /// progress (0 = match start, 1 = enemy almost dead). Designers tune everything here.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "FantasyVR/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        [Header("Enemy")]
        [SerializeField, Tooltip("Enemy hit points for the encounter.")]
        float m_EnemyMaxHealth = 1000f;

        [SerializeField, Tooltip("Rough target match length in seconds (used for the time-based difficulty fallback).")]
        float m_MatchTargetDuration = 90f;

        [SerializeField, Tooltip("Seconds after the enemy finishes rising before objects start flying (a get-ready window to look at the enemy and surroundings).")]
        float m_PreCombatDelay = 5f;

        [Header("Damage")]
        [SerializeField, Tooltip("Base damage a single slice deals before the combo multiplier is applied.")]
        float m_BaseDamagePerHit = 10f;

        [SerializeField, Tooltip("Base score a single slice awards before the combo multiplier is applied.")]
        float m_BaseScorePerHit = 100f;

        [Header("Spawning (sampled by difficulty progress 0..1)")]
        [SerializeField, Tooltip("Seconds between spawns over difficulty progress. Lower = harder.")]
        AnimationCurve m_SpawnInterval = AnimationCurve.Linear(0f, 1.1f, 1f, 0.45f);

        [SerializeField, Tooltip("Object travel speed (m/s) over difficulty progress.")]
        AnimationCurve m_ObjectSpeed = AnimationCurve.Linear(0f, 2.2f, 1f, 4.0f);

        [SerializeField, Tooltip("Probability (0..1) a spawned object requires a specific cut angle, over difficulty progress.")]
        AnimationCurve m_AngleRequiredProbability = AnimationCurve.Linear(0f, 0.0f, 1f, 0.5f);

        [Header("Potions")]
        [SerializeField, Range(0f, 1f), Tooltip("Chance per spawn tick to spawn a potion (subject to min interval).")]
        float m_PotionChance = 0.12f;

        [SerializeField, Tooltip("Minimum seconds between potion spawns.")]
        float m_PotionMinInterval = 6f;

        [SerializeField, Tooltip("Health restored when a potion is sliced.")]
        float m_PotionHealAmount = 20f;

        [Header("Enemy ultimate (slash formation)")]
        [SerializeField, Tooltip("Seconds between enemy ultimate attacks. <= 0 disables the ultimate.")]
        float m_UltimateInterval = 10f;

        [SerializeField, Tooltip("Wind-up time: the enemy plays its attack animation and normal spawns pause for this long before the first slash wave launches.")]
        float m_UltimateTelegraphLead = 0.9f;

        [SerializeField, Tooltip("Extra seconds normal spawns stay paused after the final slash wave, so the player can finish clearing the ultimate before regular objects resume.")]
        float m_UltimatePauseAfter = 1.0f;

        [SerializeField, Tooltip("Seconds after the first slash wave before the second wave arrives.")]
        float m_UltimateSecondWaveDelay = 2f;

        [SerializeField, Tooltip("Number of boxes per slash line. They are evenly spaced along the line so one clean swipe clears them all.")]
        int m_UltimateSlashBoxCount = 5;

        [SerializeField, Tooltip("Travel-speed multiplier applied to ultimate objects (relative to the current spawn speed).")]
        float m_UltimateSpeedMultiplier = 1.15f;

        [Header("Ultimate slash region (rig-local)")]
        [SerializeField, Tooltip("Half-width (m) of the slash region: left/right reach of the line.")]
        float m_UltimateSlashHalfWidth = 0.45f;

        [SerializeField, Tooltip("Bottom of the slash region (m), rig-local Y.")]
        float m_UltimateSlashYBottom = 1.0f;

        [SerializeField, Tooltip("Top of the slash region (m), rig-local Y.")]
        float m_UltimateSlashYTop = 1.55f;

        [SerializeField, Tooltip("Spawn depth (m) the slash line starts at, rig-local Z (toward the enemy).")]
        float m_UltimateSlashSpawnZ = 4.0f;

        [SerializeField, Tooltip("Target depth (m) the slash line flies to, rig-local Z (within the player's reach).")]
        float m_UltimateSlashTargetZ = 0.35f;

        [Header("Ultimate: Horizontal Sweep (Grunt)")]
        [SerializeField, Tooltip("Boxes per horizontal sweep row. Spaced across the width so one big horizontal swipe clears them.")]
        int m_SweepBoxCount = 7;

        [SerializeField, Tooltip("Half-width (m) of the sweep row, rig-local X.")]
        float m_SweepHalfWidth = 0.7f;

        [SerializeField, Tooltip("Height (m) of the first (low) sweep row, rig-local Y.")]
        float m_SweepYLow = 1.0f;

        [SerializeField, Tooltip("Height (m) of the second (high) sweep row, rig-local Y.")]
        float m_SweepYHigh = 1.45f;

        [Header("Ultimate: Rapid Volley (Archer)")]
        [SerializeField, Tooltip("Number of shots in a rapid volley.")]
        int m_VolleyCount = 8;

        [SerializeField, Tooltip("Seconds between volley shots.")]
        float m_VolleyInterval = 0.28f;

        [Header("Ultimate: Radial Fan (Mage)")]
        [SerializeField, Tooltip("Orbs in a radial fan arc.")]
        int m_FanCount = 7;

        [SerializeField, Tooltip("Half-width (m) of the fan arc, rig-local X.")]
        float m_FanArcHalfWidth = 0.75f;

        [Header("Player health")]
        [SerializeField, Tooltip("Maximum player health.")]
        float m_PlayerMaxHealth = 100f;

        [SerializeField, Tooltip("Player health at the start of a match.")]
        float m_PlayerStartHealth = 100f;

        [SerializeField, Tooltip("Damage the player takes when a damage object reaches them unsliced. <= 0 makes misses harmless (old behaviour).")]
        float m_PlayerMissDamage = 10f;

        public float EnemyMaxHealth => m_EnemyMaxHealth;
        public float MatchTargetDuration => m_MatchTargetDuration;
        public float PreCombatDelay => Mathf.Max(0f, m_PreCombatDelay);
        public float BaseDamagePerHit => m_BaseDamagePerHit;
        public float BaseScorePerHit => m_BaseScorePerHit;
        public float PotionChance => m_PotionChance;
        public float PotionMinInterval => m_PotionMinInterval;
        public float PotionHealAmount => m_PotionHealAmount;
        public float PlayerMaxHealth => m_PlayerMaxHealth;
        public float PlayerStartHealth => m_PlayerStartHealth;
        public float PlayerMissDamage => Mathf.Max(0f, m_PlayerMissDamage);
        public float UltimateInterval => m_UltimateInterval;
        public float UltimateTelegraphLead => Mathf.Max(0f, m_UltimateTelegraphLead);
        public float UltimatePauseAfter => Mathf.Max(0f, m_UltimatePauseAfter);
        public float UltimateSecondWaveDelay => Mathf.Max(0f, m_UltimateSecondWaveDelay);
        public int UltimateSlashBoxCount => Mathf.Max(1, m_UltimateSlashBoxCount);
        public float UltimateSpeedMultiplier => Mathf.Max(0.1f, m_UltimateSpeedMultiplier);
        public float UltimateSlashHalfWidth => m_UltimateSlashHalfWidth;
        public float UltimateSlashYBottom => m_UltimateSlashYBottom;
        public float UltimateSlashYTop => m_UltimateSlashYTop;
        public float UltimateSlashSpawnZ => m_UltimateSlashSpawnZ;
        public float UltimateSlashTargetZ => m_UltimateSlashTargetZ;
        public int SweepBoxCount => Mathf.Max(1, m_SweepBoxCount);
        public float SweepHalfWidth => m_SweepHalfWidth;
        public float SweepYLow => m_SweepYLow;
        public float SweepYHigh => m_SweepYHigh;
        public int VolleyCount => Mathf.Max(1, m_VolleyCount);
        public float VolleyInterval => Mathf.Max(0.02f, m_VolleyInterval);
        public int FanCount => Mathf.Max(1, m_FanCount);
        public float FanArcHalfWidth => m_FanArcHalfWidth;

        public float SampleSpawnInterval(float progress) => Mathf.Max(0.05f, m_SpawnInterval.Evaluate(progress));
        public float SampleObjectSpeed(float progress) => Mathf.Max(0.1f, m_ObjectSpeed.Evaluate(progress));
        public float SampleAngleRequiredProbability(float progress) => Mathf.Clamp01(m_AngleRequiredProbability.Evaluate(progress));
    }
}
