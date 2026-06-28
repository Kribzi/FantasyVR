using UnityEngine;

namespace FantasyVR.Combat.Loot
{
    /// <summary>
    /// A pooled reward coin. Bursts out of the dead enemy on a short ballistic arc, then gets vacuumed
    /// toward the player's waist with increasing speed before being collected (returned to the pool).
    /// Pure transform motion, allocation-free.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        [SerializeField, Tooltip("Visual child spun for sparkle (optional).")]
        Transform m_Visual;

        [SerializeField, Tooltip("Spin speed, degrees/second.")]
        float m_SpinSpeed = 540f;

        [SerializeField, Tooltip("Downward pull during the burst arc (m/s^2).")]
        float m_Gravity = 7f;

        [SerializeField, Tooltip("How quickly the vacuum pull accelerates (m/s^2).")]
        float m_VacuumAcceleration = 22f;

        [SerializeField, Tooltip("Distance to the waist at which the coin counts as collected (m).")]
        float m_CollectRadius = 0.12f;

        CoinBurstSystem m_Owner;
        Vector3 m_Velocity;
        float m_BurstTimer;
        float m_VacuumSpeed;
        bool m_Vacuum;
        bool m_Active;

        /// <summary>Begin a burst-then-vacuum flight from <paramref name="position"/>.</summary>
        public void Launch(CoinBurstSystem owner, Vector3 position, Vector3 velocity, float burstDuration)
        {
            m_Owner = owner;
            transform.position = position;
            transform.rotation = Random.rotation;
            m_Velocity = velocity;
            m_BurstTimer = burstDuration;
            m_VacuumSpeed = 0f;
            m_Vacuum = false;
            m_Active = true;
        }

        void Update()
        {
            if (!m_Active)
                return;

            float dt = Time.deltaTime;

            if (m_Visual != null)
                m_Visual.Rotate(Vector3.up, m_SpinSpeed * dt, Space.Self);

            if (!m_Vacuum)
            {
                m_Velocity += Vector3.down * (m_Gravity * dt);
                transform.position += m_Velocity * dt;
                m_BurstTimer -= dt;
                if (m_BurstTimer <= 0f)
                    m_Vacuum = true;
                return;
            }

            Vector3 target = m_Owner != null ? m_Owner.WaistPoint : transform.position;
            Vector3 toTarget = target - transform.position;
            float dist = toTarget.magnitude;

            m_VacuumSpeed += m_VacuumAcceleration * dt;
            float step = m_VacuumSpeed * dt;
            if (dist <= step || dist <= m_CollectRadius)
            {
                Collect();
                return;
            }

            transform.position += (toTarget / dist) * step;
        }

        void Collect()
        {
            if (!m_Active)
                return;
            m_Active = false;
            if (m_Owner != null)
                m_Owner.ReturnCoin(this);
            else
                gameObject.SetActive(false);
        }
    }
}
