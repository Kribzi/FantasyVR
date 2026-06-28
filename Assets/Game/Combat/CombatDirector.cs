using System;
using System.Collections;
using FantasyVR.Combat.Loot;
using FantasyVR.Config;
using FantasyVR.Enemy;
using FantasyVR.Flow;
using FantasyVR.Scoring;
using FantasyVR.Spawning;
using UnityEngine;

namespace FantasyVR.Combat
{
    /// <summary>
    /// Orchestrates the combat gauntlet: rise an enemy -> get-ready window -> spawn stream + ultimates
    /// -> route slices to damage/heal/combo/score. Defeating an enemy auto-advances to the next, harder
    /// one in the same scene (no screen between); the player dying shows the FAILURE scoreboard, and
    /// Play Again retries the same enemy. Reads tuning from <see cref="CombatConfig"/> /
    /// <see cref="EncounterDefinition"/>.
    /// </summary>
    public class CombatDirector : MonoBehaviour
    {
        /// <summary>One roster slot: an encounter definition paired with the scene model that wears it.</summary>
        [Serializable]
        public class EncounterEntry
        {
            public EncounterDefinition definition;
            public EnemyVariant variant;
        }

        [Header("Config")]
        [SerializeField] CombatConfig m_Config;

        [Header("Systems")]
        [SerializeField] EnemySkeleton m_Enemy;
        [SerializeField] ObjectSpawner m_Spawner;
        [SerializeField] ComboSystem m_Combo;
        [SerializeField] ScoreTracker m_Score;
        [SerializeField] PlayerHealth m_PlayerHealth;

        [SerializeField, Tooltip("Spawns the gold-coin reward burst when an enemy is defeated.")]
        CoinBurstSystem m_Coins;

        [SerializeField, Tooltip("Height above the enemy origin the coins burst from (chest height).")]
        float m_CoinBurstHeight = 1.2f;

        [Header("Flow")]
        [SerializeField] GameFlowManager m_Flow;

        [Header("Gauntlet roster (ordered, easiest first)")]
        [SerializeField, Tooltip("Enemies fought in order. After the last one, the gauntlet loops with scaled-up stats.")]
        EncounterEntry[] m_Encounters;

        [SerializeField, Tooltip("Per-loop enemy health multiplier applied each time the roster loops past the last enemy.")]
        float m_LoopHealthScale = 1.35f;

        [SerializeField, Tooltip("Per-loop object speed multiplier applied each time the roster loops.")]
        float m_LoopSpeedScale = 1.1f;

        [SerializeField, Tooltip("Per-loop spawn/ultimate interval multiplier (< 1 = faster) applied each time the roster loops.")]
        float m_LoopIntervalScale = 0.9f;

        bool m_Active;
        bool m_Subscribed;
        int m_EncounterIndex;
        int m_LoopCount;
        Coroutine m_PreCombatRoutine;

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
                m_Spawner.OnUltimateTelegraph += HandleUltimateTelegraph;
            }
            if (m_Enemy != null)
            {
                m_Enemy.OnRiseComplete += HandleRiseComplete;
                m_Enemy.OnDied += HandleEnemyDied;
            }
            if (m_PlayerHealth != null)
                m_PlayerHealth.OnHealthChanged += HandlePlayerHealthChanged;
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
                m_Spawner.OnUltimateTelegraph -= HandleUltimateTelegraph;
            }
            if (m_Enemy != null)
            {
                m_Enemy.OnRiseComplete -= HandleRiseComplete;
                m_Enemy.OnDied -= HandleEnemyDied;
            }
            if (m_PlayerHealth != null)
                m_PlayerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
            m_Subscribed = false;
        }

        /// <summary>Start (or retry, after a death) the CURRENT encounter from scratch.</summary>
        public void StartCombat()
        {
            Subscribe();
            BeginEncounter(freshRun: true);
        }

        EncounterEntry CurrentEntry =>
            (m_Encounters != null && m_Encounters.Length > 0)
                ? m_Encounters[Mathf.Clamp(m_EncounterIndex, 0, m_Encounters.Length - 1)]
                : null;

        /// <summary>
        /// Configure and rise the current enemy. <paramref name="freshRun"/> resets the score (a brand
        /// new run / Play Again); a gauntlet advance keeps the running score but refills health and combo.
        /// </summary>
        void BeginEncounter(bool freshRun)
        {
            if (m_PreCombatRoutine != null)
            {
                StopCoroutine(m_PreCombatRoutine);
                m_PreCombatRoutine = null;
            }

            m_Active = false;
            if (m_Spawner != null)
                m_Spawner.Stop();

            if (m_PlayerHealth != null && m_Config != null)
                m_PlayerHealth.Initialize(m_Config.PlayerMaxHealth, m_Config.PlayerStartHealth);
            if (m_Combo != null)
                m_Combo.ResetCombo();
            if (freshRun && m_Score != null)
                m_Score.BeginMatch();

            EncounterEntry entry = CurrentEntry;
            EncounterDefinition def = entry != null ? entry.definition : null;

            ConfigureVariants(entry);

            float loopHealth = Mathf.Pow(Mathf.Max(1f, m_LoopHealthScale), m_LoopCount);
            float loopSpeed = Mathf.Pow(Mathf.Max(1f, m_LoopSpeedScale), m_LoopCount);
            float loopInterval = Mathf.Pow(Mathf.Clamp(m_LoopIntervalScale, 0.1f, 1f), m_LoopCount);

            if (m_Spawner != null)
            {
                float spawnIntervalScale = (def != null ? def.SpawnIntervalScale : 1f) * loopInterval;
                float speedScale = (def != null ? def.ObjectSpeedScale : 1f) * loopSpeed;
                float ultIntervalScale = (def != null ? def.UltimateIntervalScale : 1f) * loopInterval;
                m_Spawner.ConfigureEncounter(spawnIntervalScale, speedScale, ultIntervalScale,
                    def != null ? def.UltimatePatterns : null);
            }

            if (m_Enemy != null)
            {
                if (entry != null && entry.variant != null)
                    m_Enemy.SetVariant(entry.variant);
                float baseHp = def != null ? def.MaxHealth : (m_Config != null ? m_Config.EnemyMaxHealth : 1000f);
                m_Enemy.Initialize(baseHp * loopHealth);
                m_Enemy.Rise();
            }
        }

        /// <summary>Show only the current encounter's model; hide the rest.</summary>
        void ConfigureVariants(EncounterEntry current)
        {
            if (m_Encounters == null) return;
            for (int i = 0; i < m_Encounters.Length; i++)
            {
                EncounterEntry e = m_Encounters[i];
                if (e != null && e.variant != null)
                    e.variant.SetShown(e == current);
            }
        }

        void HandleRiseComplete()
        {
            m_Active = true;

            float delay = m_Config != null ? m_Config.PreCombatDelay : 0f;
            if (delay > 0f)
                m_PreCombatRoutine = StartCoroutine(PreCombatThenSpawn(delay));
            else if (m_Spawner != null)
                m_Spawner.Begin(m_Config);
        }

        IEnumerator PreCombatThenSpawn(float delay)
        {
            // Get-ready window: the enemy stands and idles so the player can size it up and the arena.
            yield return new WaitForSeconds(delay);
            if (m_Spawner != null)
                m_Spawner.Begin(m_Config);
            m_PreCombatRoutine = null;
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

        void HandleUltimateTelegraph()
        {
            if (m_Enemy != null)
                m_Enemy.PlayAttack();
        }

        void HandleMissed(SliceableObject obj)
        {
            if (obj == null) return;

            // Potions are harmless to miss; a damage object that reaches the player unsliced breaks the
            // combo and wounds the player (enough unanswered hits ends the run).
            if (obj.Kind == SliceableKind.Damage)
            {
                if (m_Combo != null)
                    m_Combo.RegisterMiss();
                if (m_Score != null)
                    m_Score.AddMiss();
                if (m_PlayerHealth != null && m_Config != null && m_Config.PlayerMissDamage > 0f)
                    m_PlayerHealth.Damage(m_Config.PlayerMissDamage);
            }
        }

        void HandlePlayerHealthChanged(float current, float max)
        {
            if (m_Active && current <= 0f)
                HandlePlayerDeath();
        }

        void HandlePlayerDeath()
        {
            m_Active = false;
            if (m_PreCombatRoutine != null)
            {
                StopCoroutine(m_PreCombatRoutine);
                m_PreCombatRoutine = null;
            }
            if (m_Spawner != null)
                m_Spawner.Stop();

            // Failure: show the scoreboard. Play Again retries the SAME enemy (index unchanged).
            CombatResult result = m_Score != null ? m_Score.BuildResult() : default;
            if (m_Flow != null)
                m_Flow.ShowFailure(result);
        }

        void HandleEnemyDied()
        {
            // Victory: the death animation has already played (OnDied fires after it). Shower the player
            // with reward coins, then present the VICTORY banner; its "Fight" button advances the gauntlet.
            m_Active = false;
            if (m_PreCombatRoutine != null)
            {
                StopCoroutine(m_PreCombatRoutine);
                m_PreCombatRoutine = null;
            }
            if (m_Spawner != null)
                m_Spawner.Stop();

            if (m_Coins != null && m_Enemy != null)
                m_Coins.Burst(m_Enemy.transform.position + Vector3.up * m_CoinBurstHeight);

            CombatResult result = m_Score != null ? m_Score.BuildResult() : default;
            if (m_Flow != null)
            {
                m_Flow.ShowVictory(result, ContinueToNextEncounter);
            }
            else
            {
                ContinueToNextEncounter();
            }
        }

        void ContinueToNextEncounter()
        {
            AdvanceEncounter();
            BeginEncounter(freshRun: false);
        }

        void AdvanceEncounter()
        {
            if (m_Encounters == null || m_Encounters.Length == 0)
                return;

            m_EncounterIndex++;
            if (m_EncounterIndex >= m_Encounters.Length)
            {
                m_EncounterIndex = 0;
                m_LoopCount++;
            }
        }
    }
}
