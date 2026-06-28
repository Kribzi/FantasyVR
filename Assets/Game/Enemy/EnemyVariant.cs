using UnityEngine;

namespace FantasyVR.Enemy
{
    /// <summary>
    /// One skeleton model variant (Swordsman, Grunt, Archer, Mage, King). Carries that model's
    /// Animator, its trigger names, and its hit-flash renderer. <see cref="EnemySkeleton"/> binds to
    /// the active variant each encounter and delegates all animation/feedback to it, so a single
    /// logical enemy can wear any model. Allocation-free after Awake (Quest-first).
    /// </summary>
    public class EnemyVariant : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField, Tooltip("Animator on this skeleton model.")]
        Animator m_Animator;

        [SerializeField, Tooltip("Trigger that plays the rise/crawl-out-of-ground intro (the pack's _Spawn clip). Leave blank to fall back to a position tween.")]
        string m_SpawnTrigger = "Spawn";

        [SerializeField, Tooltip("Trigger that plays the ultimate wind-up attack.")]
        string m_AttackTrigger = "Attack";

        [SerializeField, Tooltip("Trigger fired on each hit (optional; leave blank to skip).")]
        string m_HitTrigger = "";

        [SerializeField, Tooltip("Trigger fired on death (must exist on the controller).")]
        string m_DieTrigger = "Die";

        [Header("Hit feedback")]
        [SerializeField, Tooltip("Optional renderer flashed on hit.")]
        Renderer m_FlashRenderer;

        [SerializeField, Tooltip("Colour briefly applied on hit.")]
        Color m_HitFlashColor = Color.red;

        MaterialPropertyBlock m_Mpb;
        Color m_BaseColor = Color.white;
        float m_FlashTimer;
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        public Animator Animator => m_Animator;
        public bool HasSpawnAnimation => m_Animator != null && !string.IsNullOrEmpty(m_SpawnTrigger);
        public bool HasDieAnimation => m_Animator != null && !string.IsNullOrEmpty(m_DieTrigger);

        void Awake()
        {
            if (m_FlashRenderer != null)
            {
                m_Mpb = new MaterialPropertyBlock();
                m_FlashRenderer.GetPropertyBlock(m_Mpb);
                m_BaseColor = m_FlashRenderer.sharedMaterial != null && m_FlashRenderer.sharedMaterial.HasProperty(k_BaseColorId)
                    ? m_FlashRenderer.sharedMaterial.GetColor(k_BaseColorId)
                    : Color.white;
            }
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

        /// <summary>Show or hide this model (only the active encounter's variant is shown).</summary>
        public void SetShown(bool shown)
        {
            if (gameObject.activeSelf != shown)
                gameObject.SetActive(shown);
        }

        public void PlaySpawn() => Trigger(m_SpawnTrigger);
        public void PlayAttack() => Trigger(m_AttackTrigger);
        public void PlayHit() => Trigger(m_HitTrigger);
        public void PlayDie() => Trigger(m_DieTrigger);

        /// <summary>Return the model to a fresh, alive pose so it can be re-used (Play Again / gauntlet loop).</summary>
        public void ResetToAlive()
        {
            if (m_Animator != null && m_Animator.isActiveAndEnabled)
            {
                m_Animator.Rebind();
                m_Animator.Update(0f);
                ResetTriggerSafe(m_DieTrigger);
                ResetTriggerSafe(m_AttackTrigger);
                ResetTriggerSafe(m_HitTrigger);
                ResetTriggerSafe(m_SpawnTrigger);
            }
            m_FlashTimer = 0f;
            if (m_FlashRenderer != null && m_Mpb != null)
                ApplyFlashColor(m_BaseColor);
        }

        public void Flash(float duration)
        {
            if (m_FlashRenderer == null || m_Mpb == null) return;
            m_FlashTimer = Mathf.Max(0f, duration);
            ApplyFlashColor(m_HitFlashColor);
        }

        void Trigger(string triggerName)
        {
            if (m_Animator != null && !string.IsNullOrEmpty(triggerName))
                m_Animator.SetTrigger(triggerName);
        }

        void ResetTriggerSafe(string triggerName)
        {
            if (!string.IsNullOrEmpty(triggerName))
                m_Animator.ResetTrigger(triggerName);
        }

        void ApplyFlashColor(Color c)
        {
            if (m_FlashRenderer == null || m_Mpb == null) return;
            m_FlashRenderer.GetPropertyBlock(m_Mpb);
            m_Mpb.SetColor(k_BaseColorId, c);
            m_FlashRenderer.SetPropertyBlock(m_Mpb);
        }
    }
}
