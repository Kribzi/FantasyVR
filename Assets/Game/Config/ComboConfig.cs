using UnityEngine;

namespace FantasyVR.Config
{
    /// <summary>
    /// Tuning for the combo / multiplier system. Tiers are defined by ascending hit-count
    /// thresholds, each mapping to a damage/score multiplier. All values are designer-editable.
    /// </summary>
    [CreateAssetMenu(fileName = "ComboConfig", menuName = "FantasyVR/Combo Config")]
    public class ComboConfig : ScriptableObject
    {
        [Header("Tiers (ascending). Thresholds are minimum hit counts for each multiplier.")]
        [SerializeField, Tooltip("Minimum combo (consecutive hits) required to enter each tier. Must be ascending and start at 0.")]
        int[] m_TierThresholds = { 0, 5, 10, 20 };

        [SerializeField, Tooltip("Damage/score multiplier for each tier, parallel to Tier Thresholds.")]
        float[] m_TierMultipliers = { 1f, 2f, 4f, 8f };

        [Header("Bonus")]
        [SerializeField, Tooltip("Extra combo increment granted when a required-angle object is sliced correctly.")]
        int m_AngleCorrectBonus = 1;

        public int AngleCorrectBonus => m_AngleCorrectBonus;

        /// <summary>Returns the tier index (0-based) for the given combo count.</summary>
        public int GetTier(int combo)
        {
            int tier = 0;
            for (int i = 0; i < m_TierThresholds.Length; i++)
            {
                if (combo >= m_TierThresholds[i])
                    tier = i;
                else
                    break;
            }
            return tier;
        }

        /// <summary>Returns the multiplier for the given combo count.</summary>
        public float GetMultiplier(int combo)
        {
            int tier = GetTier(combo);
            if (m_TierMultipliers == null || m_TierMultipliers.Length == 0)
                return 1f;
            tier = Mathf.Clamp(tier, 0, m_TierMultipliers.Length - 1);
            return m_TierMultipliers[tier];
        }

        public int TierCount => m_TierMultipliers != null ? m_TierMultipliers.Length : 0;
    }
}
