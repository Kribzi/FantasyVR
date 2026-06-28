using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace FantasyVR.Combat.Loot
{
    /// <summary>
    /// Spawns a burst of pooled coins from a point (the slain enemy) that explode outward and then get
    /// vacuumed toward the player's waist. Drives the satisfying "kill -> loot rains in" moment on a
    /// victory. The waist point is derived live from the HMD camera so it tracks the player.
    /// </summary>
    public class CoinBurstSystem : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField, Tooltip("Pool of coin objects (each needs a Coin component).")]
        CoinPooler m_Pool;

        [Header("Burst")]
        [SerializeField, Tooltip("Coins per kill.")]
        int m_CoinCount = 20;

        [SerializeField, Tooltip("Min/max initial burst speed (m/s).")]
        Vector2 m_BurstSpeed = new Vector2(2.5f, 4.5f);

        [SerializeField, Tooltip("How long coins fly outward before the vacuum kicks in (s).")]
        Vector2 m_BurstDuration = new Vector2(0.35f, 0.6f);

        [SerializeField, Tooltip("Random position jitter around the spawn origin (m).")]
        float m_OriginJitter = 0.2f;

        [Header("Target")]
        [SerializeField, Tooltip("Metres below the eyes the coins are sucked toward (the waist).")]
        float m_WaistDrop = 0.85f;

        [Header("Audio")]
        [SerializeField, Tooltip("Optional source for the burst sound.")]
        AudioSource m_Audio;

        [SerializeField, Tooltip("Optional clip played when a burst happens.")]
        AudioClip m_BurstClip;

        XROrigin m_Origin;
        readonly List<Coin> m_Active = new List<Coin>(32);

        /// <summary>World position the coins vacuum toward: the player's waist, tracked live.</summary>
        public Vector3 WaistPoint
        {
            get
            {
                if (m_Origin == null)
                    m_Origin = FindFirstObjectByType<XROrigin>();
                if (m_Origin != null && m_Origin.Camera != null)
                {
                    Vector3 p = m_Origin.Camera.transform.position;
                    return new Vector3(p.x, p.y - m_WaistDrop, p.z);
                }
                return transform.position;
            }
        }

        /// <summary>Explode a burst of coins from <paramref name="origin"/> (e.g. the enemy's chest).</summary>
        public void Burst(Vector3 origin)
        {
            if (m_Pool == null)
                return;

            for (int i = 0; i < m_CoinCount; i++)
            {
                GameObject go = m_Pool.GetItem();
                if (go == null)
                    continue;
                if (!go.TryGetComponent(out Coin coin))
                {
                    m_Pool.ReturnItem(go);
                    continue;
                }

                // Bias the burst upward and outward so coins fountain up before being pulled in.
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.7f + 0.5f;
                dir.Normalize();

                float speed = Random.Range(m_BurstSpeed.x, m_BurstSpeed.y);
                float burst = Random.Range(m_BurstDuration.x, m_BurstDuration.y);
                Vector3 pos = origin + Random.insideUnitSphere * m_OriginJitter;

                coin.Launch(this, pos, dir * speed, burst);
                m_Active.Add(coin);
            }

            if (m_Audio != null && m_BurstClip != null)
                m_Audio.PlayOneShot(m_BurstClip);
        }

        /// <summary>Called by a coin once it reaches the waist; returns it to the pool.</summary>
        public void ReturnCoin(Coin coin)
        {
            m_Active.Remove(coin);
            if (m_Pool != null)
                m_Pool.ReturnItem(coin.gameObject);
            else
                coin.gameObject.SetActive(false);
        }
    }
}
