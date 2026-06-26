using System;
using UnityEngine;

namespace FantasyVR.Combat
{
    /// <summary>
    /// Local player health. In M1 the player is never damaged (missing is harmless); potions heal.
    /// <see cref="Damage"/> is kept for future difficulty modes.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        /// <summary>Raised when health changes. Args: (current, max).</summary>
        public event Action<float, float> OnHealthChanged;

        public float Current { get; private set; }
        public float Max { get; private set; } = 100f;

        public float Normalized => Max > 0f ? Current / Max : 0f;

        public void Initialize(float max, float start)
        {
            Max = Mathf.Max(1f, max);
            Current = Mathf.Clamp(start, 0f, Max);
            OnHealthChanged?.Invoke(Current, Max);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            Current = Mathf.Min(Max, Current + amount);
            OnHealthChanged?.Invoke(Current, Max);
        }

        public void Damage(float amount)
        {
            if (amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            OnHealthChanged?.Invoke(Current, Max);
        }
    }
}
