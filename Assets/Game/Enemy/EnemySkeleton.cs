using System;
using System.Collections;
using FantasyVR.Config;
using UnityEngine;

namespace FantasyVR.Enemy
{
    /// <summary>
    /// The logical combat enemy. Holds health/state and the rise/death timing, and delegates all
    /// animation and hit feedback to the currently bound <see cref="EnemyVariant"/> (the visible
    /// skeleton model). The combat director swaps the variant each encounter so one enemy object can
    /// wear any model. All combat is local; no networking. Timing comes from <see cref="EnemyConfig"/>.
    /// </summary>
    public class EnemySkeleton : MonoBehaviour
    {
        public enum State { Idle, Rising, Active, Dying, Dead }

        [SerializeField, Tooltip("Rise/hit/death timing.")]
        EnemyConfig m_Config;

        [SerializeField, Tooltip("Transform moved during the fallback rise tween (defaults to this transform). With a Spawn animation wired on the variant, this stays put and the clip does the crawl-up.")]
        Transform m_Body;

        [SerializeField, Tooltip("Active model variant. Set at runtime by the combat director; a serialized default works for single-enemy scenes.")]
        EnemyVariant m_Variant;

        /// <summary>Raised when the rise intro finishes and combat can begin.</summary>
        public event Action OnRiseComplete;

        /// <summary>Raised on each damage application. Args: (current, max).</summary>
        public event Action<float, float> OnDamaged;

        /// <summary>Raised when the enemy dies.</summary>
        public event Action OnDied;

        public State CurrentState { get; private set; } = State.Idle;
        public float Health { get; private set; }
        public float MaxHealth { get; private set; } = 1000f;
        public float HealthNormalized => MaxHealth > 0f ? Mathf.Clamp01(Health / MaxHealth) : 0f;

        Vector3 m_BodyStartLocalPos;
        Quaternion m_BodyStartLocalRot;
        bool m_CachedStart;

        void Awake()
        {
            if (m_Body == null)
                m_Body = transform;
            CacheStart();
        }

        void CacheStart()
        {
            if (m_CachedStart) return;
            m_BodyStartLocalPos = m_Body.localPosition;
            m_BodyStartLocalRot = m_Body.localRotation;
            m_CachedStart = true;
        }

        /// <summary>Bind the model that will play this encounter's animations.</summary>
        public void SetVariant(EnemyVariant variant) => m_Variant = variant;

        /// <summary>Reset HP, state, transform, and the model pose for a fresh (or re-used) encounter.</summary>
        public void Initialize(float maxHealth)
        {
            CacheStart();
            StopAllCoroutines();

            MaxHealth = Mathf.Max(1f, maxHealth);
            Health = MaxHealth;
            CurrentState = State.Idle;

            // Put the anchor back where it started and return the model to a clean alive pose so a
            // re-used enemy (Play Again / gauntlet loop) does not stay in its dead pose or offset.
            m_Body.localPosition = m_BodyStartLocalPos;
            m_Body.localRotation = m_BodyStartLocalRot;
            if (m_Variant != null)
                m_Variant.ResetToAlive();
        }

        /// <summary>Play the climb-out-of-ground intro. Raises <see cref="OnRiseComplete"/> when done.</summary>
        public void Rise()
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(RiseRoutine());
        }

        IEnumerator RiseRoutine()
        {
            CurrentState = State.Rising;
            float duration = m_Config != null ? m_Config.RiseDuration : 2f;

            // Spawn at the correct final position immediately; the Spawn animation does the crawl-up.
            m_Body.localPosition = m_BodyStartLocalPos;
            m_Body.localRotation = m_BodyStartLocalRot;

            if (m_Variant != null && m_Variant.HasSpawnAnimation)
            {
                m_Variant.PlaySpawn();
                yield return new WaitForSeconds(duration);
            }
            else
            {
                // Fallback when no Spawn clip is wired: tween the anchor up from underground.
                float depth = m_Config != null ? m_Config.RiseDepth : 2f;
                Vector3 topLocal = m_BodyStartLocalPos;
                Vector3 startLocal = topLocal + Vector3.down * depth;
                m_Body.localPosition = startLocal;

                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    float n = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                    float eased = 1f - (1f - n) * (1f - n);
                    m_Body.localPosition = Vector3.LerpUnclamped(startLocal, topLocal, eased);
                    yield return null;
                }
                m_Body.localPosition = topLocal;
            }

            CurrentState = State.Active;
            OnRiseComplete?.Invoke();
        }

        /// <summary>Apply damage from a sliced object. Ignored unless the enemy is Active.</summary>
        public void ApplyDamage(float amount)
        {
            if (CurrentState != State.Active || amount <= 0f)
                return;

            Health = Mathf.Max(0f, Health - amount);
            OnDamaged?.Invoke(Health, MaxHealth);

            if (m_Variant != null)
            {
                m_Variant.Flash(m_Config != null ? m_Config.HitReactionDuration : 0.12f);
                if (Health > 0f)
                    m_Variant.PlayHit();
            }

            if (Health <= 0f)
                Die();
        }

        /// <summary>Play the wind-up attack animation that telegraphs an incoming ultimate.</summary>
        public void PlayAttack()
        {
            if (CurrentState != State.Active)
                return;
            if (m_Variant != null)
                m_Variant.PlayAttack();
        }

        void Die()
        {
            if (CurrentState == State.Dying || CurrentState == State.Dead)
                return;
            CurrentState = State.Dying;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            float duration = m_Config != null ? m_Config.DeathDuration : 1.5f;

            // With a rigged model, let the death animation carry the moment instead of the tween.
            if (m_Variant != null && m_Variant.HasDieAnimation)
            {
                m_Variant.PlayDie();
                yield return new WaitForSeconds(duration);
                CurrentState = State.Dead;
                OnDied?.Invoke();
                yield break;
            }

            Vector3 startLocal = m_Body.localPosition;
            float depth = m_Config != null ? m_Config.RiseDepth : 2f;
            Vector3 endLocal = startLocal + Vector3.down * depth;
            Quaternion startRot = m_Body.localRotation;
            Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, 80f);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float n = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                m_Body.localPosition = Vector3.LerpUnclamped(startLocal, endLocal, n * n);
                m_Body.localRotation = Quaternion.SlerpUnclamped(startRot, endRot, n);
                yield return null;
            }

            CurrentState = State.Dead;
            OnDied?.Invoke();
        }
    }
}
