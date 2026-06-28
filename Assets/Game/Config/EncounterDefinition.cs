using UnityEngine;

namespace FantasyVR.Config
{
    /// <summary>
    /// The ultimate (telegraphed special) attack patterns an enemy can throw. Each one is a distinct
    /// formation of sliceable projectiles that forces a different reaction. See the spawner for how
    /// each is built.
    /// </summary>
    public enum UltimatePattern
    {
        /// <summary>Diagonal/vertical line cleared in one swipe (Swordsman).</summary>
        SlashLine,
        /// <summary>Wide low horizontal row rushing in - one big horizontal sweep (Grunt slam).</summary>
        HorizontalSweep,
        /// <summary>Fast burst of single objects alternating left/right lanes (Archer rapid shot).</summary>
        RapidVolley,
        /// <summary>An arc of orbs that fan out and converge together - sweeping cuts (Mage radial blast).</summary>
        RadialFan,
        /// <summary>A single slow, angle-gated charged orb - precision under pressure (Mage charge).</summary>
        ChargedOrb,
        /// <summary>Simultaneous multi-lane clusters - a crowd to clear (King raise dead).</summary>
        RaiseDead,
        /// <summary>Boss combo: slash line -> radial fan -> rapid volley in sequence (King).</summary>
        BossCycle
    }

    /// <summary>
    /// Per-enemy encounter tuning for the escalating gauntlet. Pairs with a scene model variant in the
    /// combat director's roster. Difficulty scalars multiply the shared <see cref="CombatConfig"/>
    /// curves so each tier ramps without duplicating curves.
    /// </summary>
    [CreateAssetMenu(fileName = "EncounterDefinition", menuName = "FantasyVR/Encounter Definition")]
    public class EncounterDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Shown on HUD/scoreboard (e.g. \"Skeleton Grunt\").")]
        string m_DisplayName = "Skeleton";

        [SerializeField, Tooltip("Enemy hit points for this encounter (before gauntlet-loop scaling).")]
        float m_MaxHealth = 1000f;

        [Header("Difficulty scalars (multiply the shared CombatConfig curves)")]
        [SerializeField, Tooltip("Scales seconds between normal spawns. < 1 = faster/harder.")]
        float m_SpawnIntervalScale = 1f;

        [SerializeField, Tooltip("Scales object travel speed. > 1 = faster/harder.")]
        float m_ObjectSpeedScale = 1f;

        [SerializeField, Tooltip("Scales seconds between ultimates. < 1 = more frequent ultimates.")]
        float m_UltimateIntervalScale = 1f;

        [Header("Ultimate")]
        [SerializeField, Tooltip("Patterns this enemy chooses from for its ultimate. Empty = SlashLine.")]
        UltimatePattern[] m_UltimatePatterns = { UltimatePattern.SlashLine };

        public string DisplayName => m_DisplayName;
        public float MaxHealth => Mathf.Max(1f, m_MaxHealth);
        public float SpawnIntervalScale => Mathf.Max(0.05f, m_SpawnIntervalScale);
        public float ObjectSpeedScale => Mathf.Max(0.1f, m_ObjectSpeedScale);
        public float UltimateIntervalScale => Mathf.Max(0.1f, m_UltimateIntervalScale);
        public UltimatePattern[] UltimatePatterns => m_UltimatePatterns;
    }
}
