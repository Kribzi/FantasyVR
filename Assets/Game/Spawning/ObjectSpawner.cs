using System;
using System.Collections.Generic;
using FantasyVR.Config;
using UnityEngine;

namespace FantasyVR.Spawning
{
    /// <summary>
    /// Streams sliceables (and occasional potions) toward the player along lanes, driven by a
    /// difficulty ramp. Uses pooled objects only; tracks active objects so <see cref="Stop"/> can
    /// return everything cleanly between matches. No per-frame allocations.
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        [Header("Pools")]
        [SerializeField, Tooltip("Pool of damage sliceables.")]
        SliceablePooler m_SliceablePool;

        [SerializeField, Tooltip("Pool of potion flasks.")]
        PotionPooler m_PotionPool;

        [Header("Lane field")]
        [SerializeField, Tooltip("Lane spawn/target offsets, local to the rig root.")]
        LaneLayout m_Lanes;

        [SerializeField, Tooltip("Root the lane offsets are relative to (defaults to this transform).")]
        Transform m_RigRoot;

        [Header("Slice tuning")]
        [SerializeField, Tooltip("Half-angle tolerance (degrees) for required-angle slices.")]
        float m_AngleToleranceDeg = 35f;

        /// <summary>Raised on a valid slice. Args: (object, angleCorrect).</summary>
        public event Action<SliceableObject, bool> OnSliced;

        /// <summary>Raised when an object expires unsliced (harmless miss).</summary>
        public event Action<SliceableObject> OnMissed;

        /// <summary>Raised whenever a new object is spawned.</summary>
        public event Action<SliceableObject> OnSpawned;

        /// <summary>Raised the moment an enemy ultimate volley is launched (for telegraph/feedback).</summary>
        public event Action OnUltimate;

        /// <summary>Normalised difficulty progress (0..1), set by the director each frame.</summary>
        public float Progress { get; set; }

        readonly List<SliceableObject> m_Active = new List<SliceableObject>(64);
        static readonly Vector3[] k_AngleDirs =
        {
            Vector3.up, Vector3.right, new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f)
        };

        CombatConfig m_Config;
        bool m_Running;
        float m_SpawnTimer;
        float m_TimeSinceLastPotion;
        float m_UltimateTimer;
        bool m_SecondWavePending;
        float m_SecondWaveTimer;
        int m_SecondWavePattern;

        public void Begin(CombatConfig config)
        {
            m_Config = config;
            if (m_RigRoot == null)
                m_RigRoot = transform;

            m_Running = true;
            m_SpawnTimer = m_Config != null ? m_Config.SampleSpawnInterval(0f) : 1f;
            m_TimeSinceLastPotion = 0f;
            m_UltimateTimer = m_Config != null ? m_Config.UltimateInterval : 0f;
            m_SecondWavePending = false;
            m_SecondWaveTimer = 0f;
            Progress = 0f;
        }

        public void Stop()
        {
            m_Running = false;
            m_SecondWavePending = false;
            for (int i = m_Active.Count - 1; i >= 0; i--)
                ReturnToPool(m_Active[i]);
            m_Active.Clear();
        }

        void Update()
        {
            if (!m_Running || m_Config == null || m_Lanes == null || m_Lanes.LaneCount == 0)
                return;

            float dt = Time.deltaTime;
            m_TimeSinceLastPotion += dt;

            // Deliver the delayed second slash wave even if the ultimate cadence itself is disabled mid-run.
            if (m_SecondWavePending)
            {
                m_SecondWaveTimer -= dt;
                if (m_SecondWaveTimer <= 0f)
                {
                    m_SecondWavePending = false;
                    SpawnSlashWave(m_SecondWavePattern);
                }
            }

            if (m_Config.UltimateInterval > 0f)
            {
                m_UltimateTimer -= dt;
                if (m_UltimateTimer <= 0f)
                {
                    m_UltimateTimer += m_Config.UltimateInterval;
                    SpawnUltimate();
                }
            }

            m_SpawnTimer -= dt;
            if (m_SpawnTimer > 0f)
                return;

            m_SpawnTimer += m_Config.SampleSpawnInterval(Progress);
            SpawnOne();
        }

        // Slash-line patterns: each is a (start corner -> end corner) in the rig-local X/Y plane.
        // 0: top-left -> bottom-left   (left vertical)
        // 1: top-left -> bottom-right  (diagonal)
        // 2: top-right -> bottom-right (right vertical)
        // 3: top-right -> bottom-left  (diagonal)
        enum SlashCorner { TopLeft, TopRight, BottomLeft, BottomRight }
        static readonly (SlashCorner start, SlashCorner end)[] k_SlashPatterns =
        {
            (SlashCorner.TopLeft, SlashCorner.BottomLeft),
            (SlashCorner.TopLeft, SlashCorner.BottomRight),
            (SlashCorner.TopRight, SlashCorner.BottomRight),
            (SlashCorner.TopRight, SlashCorner.BottomLeft),
        };

        /// <summary>
        /// Enemy ultimate: launch a line of boxes in a slash formation (one clean swipe clears them all),
        /// then a second slash line a configurable beat later. All plain damage objects (no angle gate).
        /// </summary>
        void SpawnUltimate()
        {
            SpawnSlashWave(UnityEngine.Random.Range(0, k_SlashPatterns.Length));

            m_SecondWavePattern = UnityEngine.Random.Range(0, k_SlashPatterns.Length);
            m_SecondWaveTimer = m_Config.UltimateSecondWaveDelay;
            m_SecondWavePending = true;

            OnUltimate?.Invoke();
        }

        /// <summary>Spawn a single slash line of boxes, evenly spaced along the chosen diagonal/vertical.</summary>
        void SpawnSlashWave(int patternIndex)
        {
            var pattern = k_SlashPatterns[Mathf.Clamp(patternIndex, 0, k_SlashPatterns.Length - 1)];
            Vector2 start = CornerXY(pattern.start);
            Vector2 end = CornerXY(pattern.end);

            int count = m_Config.UltimateSlashBoxCount;
            float speed = m_Config.SampleObjectSpeed(Progress) * m_Config.UltimateSpeedMultiplier;
            float spawnZ = m_Config.UltimateSlashSpawnZ;
            float targetZ = m_Config.UltimateSlashTargetZ;

            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0.5f : (float)i / (count - 1);
                float x = Mathf.Lerp(start.x, end.x, t);
                float y = Mathf.Lerp(start.y, end.y, t);

                Vector3 spawnPos = m_RigRoot.TransformPoint(new Vector3(x, y, spawnZ));
                Vector3 targetPos = m_RigRoot.TransformPoint(new Vector3(x, y, targetZ));
                LaunchFromPool(m_SliceablePool, spawnPos, targetPos, speed, false, Vector3.up);
            }
        }

        Vector2 CornerXY(SlashCorner corner)
        {
            float hw = m_Config.UltimateSlashHalfWidth;
            float yb = m_Config.UltimateSlashYBottom;
            float yt = m_Config.UltimateSlashYTop;
            switch (corner)
            {
                case SlashCorner.TopLeft: return new Vector2(-hw, yt);
                case SlashCorner.TopRight: return new Vector2(hw, yt);
                case SlashCorner.BottomLeft: return new Vector2(-hw, yb);
                default: return new Vector2(hw, yb); // BottomRight
            }
        }

        void SpawnOne()
        {
            bool spawnPotion = m_TimeSinceLastPotion >= m_Config.PotionMinInterval
                               && UnityEngine.Random.value < m_Config.PotionChance;

            float speed = m_Config.SampleObjectSpeed(Progress);
            int laneIndex = UnityEngine.Random.Range(0, m_Lanes.LaneCount);
            LaneLayout.Lane lane = m_Lanes.GetLane(laneIndex);
            Vector3 spawnPos = m_RigRoot.TransformPoint(lane.spawnOffset);
            Vector3 targetPos = m_RigRoot.TransformPoint(lane.targetOffset);

            if (spawnPotion)
            {
                m_TimeSinceLastPotion = 0f;
                LaunchFromPool(m_PotionPool, spawnPos, targetPos, speed, false, Vector3.up);
            }
            else
            {
                bool requiresAngle = UnityEngine.Random.value < m_Config.SampleAngleRequiredProbability(Progress);
                Vector3 dir = requiresAngle
                    ? m_RigRoot.TransformDirection(k_AngleDirs[UnityEngine.Random.Range(0, k_AngleDirs.Length)])
                    : Vector3.up;
                LaunchFromPool(m_SliceablePool, spawnPos, targetPos, speed, requiresAngle, dir);
            }
        }

        void LaunchFromPool(XRMultiplayer.Pooler pool, Vector3 spawnPos, Vector3 targetPos, float speed,
            bool requiresAngle, Vector3 requiredDir)
        {
            if (pool == null) return;

            GameObject go = pool.GetItem();
            if (!go.TryGetComponent(out SliceableObject sliceable))
            {
                pool.ReturnItem(go);
                return;
            }

            sliceable.Launch(this, spawnPos, targetPos, speed, requiresAngle, requiredDir, m_AngleToleranceDeg);
            m_Active.Add(sliceable);
            OnSpawned?.Invoke(sliceable);
        }

        /// <summary>Called by a sliceable when it is validly sliced.</summary>
        public void ReportSliced(SliceableObject obj, bool angleCorrect)
        {
            m_Active.Remove(obj);
            OnSliced?.Invoke(obj, angleCorrect);
            ReturnToPool(obj);
        }

        /// <summary>Called by a sliceable when it expires unsliced.</summary>
        public void ReportMissed(SliceableObject obj)
        {
            m_Active.Remove(obj);
            OnMissed?.Invoke(obj);
            ReturnToPool(obj);
        }

        void ReturnToPool(SliceableObject obj)
        {
            if (obj == null) return;
            XRMultiplayer.Pooler pool = obj.Kind == SliceableKind.Potion ? (XRMultiplayer.Pooler)m_PotionPool : m_SliceablePool;
            if (pool != null)
                pool.ReturnItem(obj.gameObject);
            else
                obj.gameObject.SetActive(false);
        }
    }
}
