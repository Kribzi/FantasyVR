using UnityEngine;

namespace FantasyVR.Config
{
    /// <summary>
    /// Tuning for the skeleton enemy intro, hit reaction, and death timing.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "FantasyVR/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Rise intro")]
        [SerializeField, Tooltip("Seconds for the skeleton to climb out of the ground before combat unlocks.")]
        float m_RiseDuration = 2.0f;

        [SerializeField, Tooltip("How far below its anchor the skeleton starts (metres). It rises this distance.")]
        float m_RiseDepth = 2.0f;

        [Header("Hit reaction")]
        [SerializeField, Tooltip("Seconds the hit-flash / stagger feedback lasts.")]
        float m_HitReactionDuration = 0.12f;

        [Header("Death")]
        [SerializeField, Tooltip("Seconds of death sequence before the scoreboard is shown.")]
        float m_DeathDuration = 1.5f;

        public float RiseDuration => m_RiseDuration;
        public float RiseDepth => m_RiseDepth;
        public float HitReactionDuration => m_HitReactionDuration;
        public float DeathDuration => m_DeathDuration;
    }
}
