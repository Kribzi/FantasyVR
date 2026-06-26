using FantasyVR.Config;
using FantasyVR.Enemy;
using FantasyVR.Flow;
using FantasyVR.Scoring;
using FantasyVR.Spawning;
using UnityEngine;

namespace FantasyVR.Combat
{
    /// <summary>
    /// Orchestrates one local combat encounter end-to-end: enemy rise -> spawn stream -> route slices
    /// to damage/heal/combo/score -> enemy death -> scoreboard. Reads all tuning from <see cref="CombatConfig"/>.
    /// </summary>
    public class CombatDirector : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] CombatConfig m_Config;

        [Header("Systems")]
        [SerializeField] EnemySkeleton m_Enemy;
        [SerializeField] ObjectSpawner m_Spawner;
        [SerializeField] ComboSystem m_Combo;
        [SerializeField] ScoreTracker m_Score;
        [SerializeField] PlayerHealth m_PlayerHealth;

        [Header("Flow")]
        [SerializeField] GameFlowManager m_Flow;

        bool m_Active;
        bool m_Subscribed;

        public CombatConfig Config => m_Config;
        public EnemySkeleton Enemy => m_Enemy;
        public ComboSystem Combo => m_Combo;
        public PlayerHealth PlayerHealth => m_PlayerHealth;

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (m_Subscribed) return;
            if (m_Spawner != null)
            {
                m_Spawner.OnSliced += HandleSliced;
                m_Spawner.OnMissed += HandleMissed;
                m_Spawner.OnSpawned += HandleSpawned;
            }
            if (m_Enemy != null)
            {
                m_Enemy.OnRiseComplete += HandleRiseComplete;
                m_Enemy.OnDied += HandleEnemyDied;
            }
            m_Subscribed = true;
        }

        void Unsubscribe()
        {
            if (!m_Subscribed) return;
            if (m_Spawner != null)
            {
                m_Spawner.OnSliced -= HandleSliced;
                m_Spawner.OnMissed -= HandleMissed;
                m_Spawner.OnSpawned -= HandleSpawned;
            }
            if (m_Enemy != null)
            {
                m_Enemy.OnRiseComplete -= HandleRiseComplete;
                m_Enemy.OnDied -= HandleEnemyDied;
            }
            m_Subscribed = false;
        }

        /// <summary>Begin a fresh encounter. Combat stays locked until the enemy finishes rising.</summary>
        public void StartCombat()
        {
            Subscribe();

            if (m_PlayerHealth != null && m_Config != null)
                m_PlayerHealth.Initialize(m_Config.PlayerMaxHealth, m_Config.PlayerStartHealth);
            if (m_Combo != null)
            {
                m_Combo.ResetCombo();
            }
            if (m_Score != null)
                m_Score.BeginMatch();

            m_Active = false; // spawning/scoring unlock on rise complete

            if (m_Enemy != null)
            {
                m_Enemy.Initialize(m_Config != null ? m_Config.EnemyMaxHealth : 1000f);
                m_Enemy.Rise();
            }
        }

        void HandleRiseComplete()
        {
            m_Active = true;
            if (m_Spawner != null)
                m_Spawner.Begin(m_Config);
        }

        void Update()
        {
            if (!m_Active || m_Spawner == null || m_Enemy == null)
                return;

            // Difficulty progress ramps from 0 (full HP) to 1 (enemy dead).
            m_Spawner.Progress = 1f - m_Enemy.HealthNormalized;
        }

        void HandleSpawned(SliceableObject obj)
        {
            if (obj != null && obj.Kind == SliceableKind.Damage && m_Score != null)
                m_Score.AddSpawn();
        }

        void HandleSliced(SliceableObject obj, bool angleCorrect)
        {
            if (obj == null) return;

            if (obj.Kind == SliceableKind.Potion)
            {
                if (m_PlayerHealth != null && m_Config != null)
                    m_PlayerHealth.Heal(m_Config.PotionHealAmount);
                if (m_Score != null)
                    m_Score.AddPotion();
                if (m_Combo != null)
                    m_Combo.RegisterHit(false); // a clean slice still feeds the combo
                return;
            }

            float multiplier = m_Combo != null ? m_Combo.Multiplier : 1f;
            if (m_Combo != null)
            {
                m_Combo.RegisterHit(angleCorrect);
                multiplier = m_Combo.Multiplier;
            }

            float baseDamage = m_Config != null ? m_Config.BaseDamagePerHit : 10f;
            float baseScore = m_Config != null ? m_Config.BaseScorePerHit : 100f;
            float damage = baseDamage * multiplier;

            if (m_Enemy != null)
                m_Enemy.ApplyDamage(damage);
            if (m_Score != null)
                m_Score.AddHit(damage, baseScore * multiplier, m_Combo != null ? m_Combo.Combo : 0);
        }

        void HandleMissed(SliceableObject obj)
        {
            if (obj == null) return;

            // Potions are harmless to miss; damage objects break the combo (no health penalty).
            if (obj.Kind == SliceableKind.Damage)
            {
                if (m_Combo != null)
                    m_Combo.RegisterMiss();
                if (m_Score != null)
                    m_Score.AddMiss();
            }
        }

        void HandleEnemyDied()
        {
            m_Active = false;
            if (m_Spawner != null)
                m_Spawner.Stop();

            CombatResult result = m_Score != null ? m_Score.BuildResult() : default;
            if (m_Flow != null)
                m_Flow.ShowScoreboard(result);
        }
    }
}
