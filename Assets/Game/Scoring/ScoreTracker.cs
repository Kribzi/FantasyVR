using System;
using UnityEngine;

namespace FantasyVR.Scoring
{
    /// <summary>
    /// Accumulates per-match statistics and builds the <see cref="CombatResult"/> for the scoreboard.
    /// Pure local state; no allocations on the hot path.
    /// </summary>
    public class ScoreTracker : MonoBehaviour
    {
        /// <summary>Raised when the running score changes. Arg: current score.</summary>
        public event Action<int> OnScoreChanged;

        public int Score { get; private set; }
        public float DamageDealt { get; private set; }
        public int ObjectsSpawned { get; private set; }
        public int ObjectsSliced { get; private set; }
        public int HighestCombo { get; private set; }
        public int PotionsCollected { get; private set; }

        float m_StartTime;

        public void BeginMatch()
        {
            Score = 0;
            DamageDealt = 0f;
            ObjectsSpawned = 0;
            ObjectsSliced = 0;
            HighestCombo = 0;
            PotionsCollected = 0;
            m_StartTime = Time.time;
            OnScoreChanged?.Invoke(Score);
        }

        public void AddSpawn() => ObjectsSpawned++;

        public void AddHit(float damage, float score, int currentCombo)
        {
            ObjectsSliced++;
            DamageDealt += damage;
            Score += Mathf.RoundToInt(score);
            if (currentCombo > HighestCombo)
                HighestCombo = currentCombo;
            OnScoreChanged?.Invoke(Score);
        }

        public void AddMiss()
        {
            // Misses are harmless in M1; tracked implicitly via accuracy (spawned vs sliced).
        }

        public void AddPotion() => PotionsCollected++;

        public CombatResult BuildResult()
        {
            return new CombatResult
            {
                Score = Score,
                DamageDealt = DamageDealt,
                ObjectsSpawned = ObjectsSpawned,
                ObjectsSliced = ObjectsSliced,
                HighestCombo = HighestCombo,
                PotionsCollected = PotionsCollected,
                Duration = Time.time - m_StartTime,
            };
        }
    }
}
