using FantasyVR.Combat.Slicing;
using UnityEngine;

namespace FantasyVR.Spawning
{
    /// <summary>Distinguishes what a sliced object does: damage the enemy or heal the player.</summary>
    public enum SliceableKind
    {
        Damage,
        Potion
    }

    /// <summary>
    /// A pooled object that flies toward the player along a lane. Sliced with a fast-moving
    /// <see cref="Blade"/> it reports a hit; if it expires unsliced it reports a (harmless) miss.
    /// Movement is kinematic and allocation-free. Hit detection is trigger + manual speed gating.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SliceableObject : MonoBehaviour
    {
        [SerializeField, Tooltip("Visual child that spins for readability (optional).")]
        Transform m_Visual;

        [SerializeField, Tooltip("Visual spin speed in degrees/second.")]
        float m_SpinSpeed = 90f;

        [SerializeField, Tooltip("Extra metres travelled past the target before the object expires (miss).")]
        float m_ExpiryMargin = 1.25f;

        [SerializeField, Tooltip("Optional object highlighted when it requires a specific cut angle.")]
        GameObject m_AngleIndicator;

        Rigidbody m_Rigidbody;
        ObjectSpawner m_Owner;
        Vector3 m_Velocity;
        bool m_RequiresAngle;
        Vector3 m_RequiredDir = Vector3.up;
        float m_AngleToleranceDeg = 35f;
        float m_RemainingLife;
        bool m_Consumed;
        Renderer m_VisualRenderer;
        float m_VisualRadius = 0.09f;

        public virtual SliceableKind Kind => SliceableKind.Damage;
        public bool RequiresAngle => m_RequiresAngle;

        protected virtual void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.isKinematic = true;
            m_Rigidbody.useGravity = false;

            if (m_Visual != null)
            {
                m_VisualRenderer = m_Visual.GetComponentInChildren<Renderer>();
                if (m_VisualRenderer != null)
                    m_VisualRadius = Mathf.Max(0.02f, m_VisualRenderer.bounds.extents.x);
            }
        }

        /// <summary>Configure and start a flight. Called by the spawner on every launch.</summary>
        public void Launch(ObjectSpawner owner, Vector3 spawnPos, Vector3 targetPos, float speed,
            bool requiresAngle, Vector3 requiredDir, float angleToleranceDeg)
        {
            m_Owner = owner;
            transform.position = spawnPos;

            Vector3 toTarget = targetPos - spawnPos;
            float dist = toTarget.magnitude;
            m_Velocity = dist > 0.001f ? (toTarget / dist) * speed : Vector3.zero;

            m_RequiresAngle = requiresAngle;
            m_RequiredDir = requiredDir.sqrMagnitude > 0.0001f ? requiredDir.normalized : Vector3.up;
            m_AngleToleranceDeg = angleToleranceDeg;
            m_RemainingLife = (dist + m_ExpiryMargin) / Mathf.Max(0.1f, speed);
            m_Consumed = false;

            if (m_AngleIndicator != null)
                m_AngleIndicator.SetActive(requiresAngle);

            OnLaunched();
        }

        protected virtual void OnLaunched() { }

        void Update()
        {
            if (m_Consumed) return;

            float dt = Time.deltaTime;
            transform.position += m_Velocity * dt;

            if (m_Visual != null)
                m_Visual.Rotate(Vector3.up, m_SpinSpeed * dt, Space.Self);

            m_RemainingLife -= dt;
            if (m_RemainingLife <= 0f)
                Expire();
        }

        void Expire()
        {
            if (m_Consumed) return;
            m_Consumed = true;
            if (m_Owner != null)
                m_Owner.ReportMissed(this);
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_Consumed) return;

            Blade blade = other.GetComponentInParent<Blade>();
            if (blade == null) return;

            BladeController controller = blade.Controller;
            if (controller == null || !controller.IsSlicing) return;

            bool angleCorrect = true;
            if (m_RequiresAngle)
            {
                // A cut counts whether swung along +dir or -dir (you can slash either way along an axis).
                float along = Vector3.Angle(controller.SliceDirection, m_RequiredDir);
                float against = Vector3.Angle(controller.SliceDirection, -m_RequiredDir);
                angleCorrect = Mathf.Min(along, against) <= m_AngleToleranceDeg;
                if (!angleCorrect)
                    return; // wrong angle: no credit, object survives for a re-slice
            }

            m_Consumed = true;
            controller.PlaySliceHaptics();

            // Split the orb into two physics halves along the cut for "sliced in two + fall" juice.
            if (Kind == SliceableKind.Damage && SliceDebrisSystem.Instance != null)
            {
                Material mat = m_VisualRenderer != null ? m_VisualRenderer.sharedMaterial : null;
                SliceDebrisSystem.Instance.Spawn(transform.position, controller.SliceDirection, m_Velocity, mat, m_VisualRadius);
            }

            if (m_Owner != null)
                // The combo bonus is only earned on objects that actually demanded a specific cut
                // angle. A plain object reaching here was cut fine, but must not grant the angle bonus.
                m_Owner.ReportSliced(this, m_RequiresAngle && angleCorrect);
        }
    }
}
