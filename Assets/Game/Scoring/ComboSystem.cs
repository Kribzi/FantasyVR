using System;
using FantasyVR.Config;
using UnityEngine;

namespace FantasyVR.Scoring
{
    /// <summary>
    /// Tracks consecutive hits and the resulting multiplier tier. Resets on a miss.
    /// Local, single-player; raises <see cref="OnComboChanged"/> for the HUD.
    /// </summary>
    public class ComboSystem : MonoBehaviour
    {
        [SerializeField, Tooltip("Tier thresholds + multipliers used to derive the current multiplier.")]
        ComboConfig m_Config;

        /// <summary>Raised whenever the combo or multiplier changes. Args: (combo, multiplier).</summary>
        public event Action<int, float> OnComboChanged;

        public int Combo { get; private set; }
        public int Tier { get; private set; }
        public float Multiplier { get; private set; } = 1f;
        public int HighestCombo { get; private set; }

        public void Configure(ComboConfig config)
        {
            m_Config = config;
            ResetCombo();
        }

        public void ResetCombo()
        {
            Combo = 0;
            HighestCombo = 0;
            Recompute();
        }

        /// <summary>Register a successful slice. Correct-angle hits add a bonus increment.</summary>
        public void RegisterHit(bool angleCorrect)
        {
            int increment = 1;
            if (angleCorrect && m_Config != null)
                increment += m_Config.AngleCorrectBonus;

            Combo += increment;
            if (Combo > HighestCombo)
                HighestCombo = Combo;

            Recompute();
        }

        /// <summary>Register a miss. Resets the combo (never damages the player).</summary>
        public void RegisterMiss()
        {
            if (Combo == 0) return;
            Combo = 0;
            Recompute();
        }

        void Recompute()
        {
            if (m_Config != null)
            {
                Tier = m_Config.GetTier(Combo);
                Multiplier = m_Config.GetMultiplier(Combo);
            }
            else
            {
                Tier = 0;
                Multiplier = 1f;
            }
            OnComboChanged?.Invoke(Combo, Multiplier);
        }
    }
}
