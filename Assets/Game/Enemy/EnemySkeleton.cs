using System;
using System.Collections;
using FantasyVR.Config;
using UnityEngine;

namespace FantasyVR.Enemy
{
    /// <summary>
    /// The skeleton enemy. Rises out of the ground, takes damage from sliced objects, plays a hit
    /// reaction, and dies. All combat is local; no networking. Timing comes from <see cref="EnemyConfig"/>.
    /// </summary>
    public class EnemySkeleton : MonoBehaviour
    {
        public enum State { Idle, Rising, Active, Dying, Dead }

        [SerializeField, Tooltip("Rise/hit/death timing.")]
        EnemyConfig m_Config;

        [SerializeField, Tooltip("Transform moved during the rise (defaults to this transform).")]
        Transform m_Body;

        [SerializeField, Tooltip("Optional renderer flashed on hit.")]
        Renderer m_FlashRenderer;

        [SerializeField, Tooltip("Colour briefly applied on hit.")]
        Color m_HitFlashColor = Color.red;

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

        MaterialPropertyBlock m_Mpb;
        Color m_BaseColor;
        float m_FlashTimer;
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            if (m_Body == null)
                m_Body = transform;
            if (m_FlashRenderer != null)
            {
                m_Mpb = new MaterialPropertyBlock();
                m_FlashRenderer.GetPropertyBlock(m_Mpb);
                m_BaseColor = m_FlashRenderer.sharedMaterial != null && m_FlashRenderer.sharedMaterial.HasProperty(k_BaseColorId)
                    ? m_FlashRenderer.sharedMaterial.GetColor(k_BaseColorId)
                    : Color.white;
            }
        }

        /// <summary>Reset HP and state for a fresh encounter.</summary>
        public void Initialize(float maxHealth)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            Health = MaxHealth;
            CurrentState = State.Idle;
            StopAllCoroutines();
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
            float depth = m_Config != null ? m_Config.RiseDepth : 2f;
            float duration = m_Config != null ? m_Config.RiseDuration : 2f;

            Vector3 topLocal = m_Body.localPosition;
            Vector3 startLocal = topLocal + Vector3.down * depth;
            m_Body.localPosition = startLocal;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float n = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                // Ease-out for a weighty climb.
                float eased = 1f - (1f - n) * (1f - n);
                m_Body.localPosition = Vector3.LerpUnclamped(startLocal, topLocal, eased);
                yield return null;
            }
            m_Body.localPosition = topLocal;

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
            TriggerHitFlash();

            if (Health <= 0f)
                Die();
        }

        void TriggerHitFlash()
        {
            if (m_FlashRenderer == null) return;
            m_FlashTimer = m_Config != null ? m_Config.HitReactionDuration : 0.12f;
            ApplyFlashColor(m_HitFlashColor);
        }

        void ApplyFlashColor(Color c)
        {
            if (m_FlashRenderer == null) return;
            m_FlashRenderer.GetPropertyBlock(m_Mpb);
            m_Mpb.SetColor(k_BaseColorId, c);
            m_FlashRenderer.SetPropertyBlock(m_Mpb);
        }

        void Update()
        {
            if (m_FlashTimer > 0f)
            {
                m_FlashTimer -= Time.deltaTime;
                if (m_FlashTimer <= 0f)
                    ApplyFlashColor(m_BaseColor);
            }
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
