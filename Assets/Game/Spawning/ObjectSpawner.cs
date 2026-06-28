using System;
using System.Collections;
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

        /// <summary>Raised when the enemy begins winding up its ultimate, before any projectile launches
        /// (drives the attack animation and the normal-spawn pause).</summary>
        public event Action OnUltimateTelegraph;

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
        bool m_UltimateInProgress;
        bool m_NormalSpawnPaused;

        // Per-encounter difficulty scalars + ultimate pattern set (set by the director before Begin).
        float m_SpawnIntervalScale = 1f;
        float m_ObjectSpeedScale = 1f;
        float m_UltimateIntervalScale = 1f;
        UltimatePattern[] m_Patterns;
        int m_PatternCursor;

        static readonly UltimatePattern[] k_DefaultPatterns = { UltimatePattern.SlashLine };

        /// <summary>
        /// Set the difficulty scalars and ultimate pattern set for the upcoming encounter. Call before
        /// <see cref="Begin"/>. Scalars multiply the shared <see cref="CombatConfig"/> curves.
        /// </summary>
        public void ConfigureEncounter(float spawnIntervalScale, float objectSpeedScale,
            float ultimateIntervalScale, UltimatePattern[] patterns)
        {
            m_SpawnIntervalScale = Mathf.Max(0.05f, spawnIntervalScale);
            m_ObjectSpeedScale = Mathf.Max(0.1f, objectSpeedScale);
            m_UltimateIntervalScale = Mathf.Max(0.1f, ultimateIntervalScale);
            m_Patterns = (patterns != null && patterns.Length > 0) ? patterns : k_DefaultPatterns;
            m_PatternCursor = 0;
        }

        float CurrentObjectSpeed() => m_Config.SampleObjectSpeed(Progress) * m_ObjectSpeedScale;
        float CurrentUltimateInterval() => m_Config.UltimateInterval * m_UltimateIntervalScale;

        public void Begin(CombatConfig config)
        {
            m_Config = config;
            if (m_RigRoot == null)
                m_RigRoot = transform;
            if (m_Patterns == null)
                m_Patterns = k_DefaultPatterns;

            m_Running = true;
            m_SpawnTimer = m_Config != null ? m_Config.SampleSpawnInterval(0f) * m_SpawnIntervalScale : 1f;
            m_TimeSinceLastPotion = 0f;
            m_UltimateTimer = m_Config != null ? CurrentUltimateInterval() : 0f;
            m_UltimateInProgress = false;
            m_NormalSpawnPaused = false;
            Progress = 0f;
        }

        public void Stop()
        {
            m_Running = false;
            m_UltimateInProgress = false;
            m_NormalSpawnPaused = false;
            StopAllCoroutines();
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

            if (m_Config.UltimateInterval > 0f && !m_UltimateInProgress)
            {
                m_UltimateTimer -= dt;
                if (m_UltimateTimer <= 0f)
                {
                    m_UltimateTimer += CurrentUltimateInterval();
                    StartCoroutine(UltimateRoutine());
                }
            }

            // Normal objects are suppressed during the ultimate's telegraph + clear window so the player
            // can focus on the slash formation.
            if (m_NormalSpawnPaused)
                return;

            m_SpawnTimer -= dt;
            if (m_SpawnTimer > 0f)
                return;

            m_SpawnTimer += m_Config.SampleSpawnInterval(Progress) * m_SpawnIntervalScale;
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
        /// Enemy ultimate: telegraph with an attack wind-up while normal objects pause, then run one of
        /// this enemy's signature patterns (slash line, sweep, volley, fan, charged orb, raise-dead, or
        /// the boss combo). Normal spawns resume a short while after. All pooled, allocation-free.
        /// </summary>
        IEnumerator UltimateRoutine()
        {
            m_UltimateInProgress = true;
            m_NormalSpawnPaused = true;

            // Wind-up: enemy plays its attack animation; the player gets a beat with no other objects.
            OnUltimateTelegraph?.Invoke();
            yield return new WaitForSeconds(m_Config.UltimateTelegraphLead);

            yield return RunPattern(NextPattern());

            // Give the player room to finish clearing the ultimate before regular objects stream again.
            yield return new WaitForSeconds(m_Config.UltimatePauseAfter);
            m_NormalSpawnPaused = false;
            m_UltimateInProgress = false;
        }

        UltimatePattern NextPattern()
        {
            UltimatePattern[] set = (m_Patterns != null && m_Patterns.Length > 0) ? m_Patterns : k_DefaultPatterns;
            if (set.Length == 1)
                return set[0];
            // Cycle through the enemy's set so a multi-pattern enemy shows all of its abilities.
            UltimatePattern p = set[m_PatternCursor % set.Length];
            m_PatternCursor++;
            return p;
        }

        IEnumerator RunPattern(UltimatePattern pattern)
        {
            switch (pattern)
            {
                case UltimatePattern.HorizontalSweep: yield return HorizontalSweepRoutine(); break;
                case UltimatePattern.RapidVolley: yield return RapidVolleyRoutine(); break;
                case UltimatePattern.RadialFan: yield return RadialFanRoutine(); break;
                case UltimatePattern.ChargedOrb: yield return ChargedOrbRoutine(); break;
                case UltimatePattern.RaiseDead: yield return RaiseDeadRoutine(); break;
                case UltimatePattern.BossCycle: yield return BossCycleRoutine(); break;
                default: yield return SlashLineRoutine(); break;
            }
        }

        // --- Slash line (Swordsman): two diagonal/vertical lines, each cleared in one swipe. ---
        IEnumerator SlashLineRoutine()
        {
            SpawnSlashWave(UnityEngine.Random.Range(0, k_SlashPatterns.Length));
            OnUltimate?.Invoke();
            yield return new WaitForSeconds(m_Config.UltimateSecondWaveDelay);
            SpawnSlashWave(UnityEngine.Random.Range(0, k_SlashPatterns.Length));
        }

        /// <summary>Spawn a single slash line of boxes, evenly spaced along the chosen diagonal/vertical.</summary>
        void SpawnSlashWave(int patternIndex)
        {
            var pattern = k_SlashPatterns[Mathf.Clamp(patternIndex, 0, k_SlashPatterns.Length - 1)];
            Vector2 start = CornerXY(pattern.start);
            Vector2 end = CornerXY(pattern.end);

            int count = m_Config.UltimateSlashBoxCount;
            float speed = CurrentObjectSpeed() * m_Config.UltimateSpeedMultiplier;
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

        // --- Horizontal sweep (Grunt slam): a low row then a higher row; each cleared by a wide swipe. ---
        IEnumerator HorizontalSweepRoutine()
        {
            SpawnHorizontalRow(m_Config.SweepYLow);
            OnUltimate?.Invoke();
            yield return new WaitForSeconds(m_Config.UltimateSecondWaveDelay);
            SpawnHorizontalRow(m_Config.SweepYHigh);
        }

        void SpawnHorizontalRow(float y)
        {
            int count = m_Config.SweepBoxCount;
            float hw = m_Config.SweepHalfWidth;
            float speed = CurrentObjectSpeed() * m_Config.UltimateSpeedMultiplier;
            float spawnZ = m_Config.UltimateSlashSpawnZ;
            float targetZ = m_Config.UltimateSlashTargetZ;

            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0.5f : (float)i / (count - 1);
                float x = Mathf.Lerp(-hw, hw, t);
                Vector3 spawnPos = m_RigRoot.TransformPoint(new Vector3(x, y, spawnZ));
                Vector3 targetPos = m_RigRoot.TransformPoint(new Vector3(x, y, targetZ));
                LaunchFromPool(m_SliceablePool, spawnPos, targetPos, speed, false, Vector3.up);
            }
        }

        // --- Rapid volley (Archer): fast single shots alternating left/right. ---
        IEnumerator RapidVolleyRoutine()
        {
            int count = m_Config.VolleyCount;
            float interval = m_Config.VolleyInterval;
            float speed = CurrentObjectSpeed() * m_Config.UltimateSpeedMultiplier;
            float hw = m_Config.UltimateSlashHalfWidth;
            float yb = m_Config.UltimateSlashYBottom;
            float yt = m_Config.UltimateSlashYTop;
            float spawnZ = m_Config.UltimateSlashSpawnZ;
            float targetZ = m_Config.UltimateSlashTargetZ;

            for (int i = 0; i < count; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * hw;
                float y = UnityEngine.Random.Range(yb, yt);
                Vector3 spawnPos = m_RigRoot.TransformPoint(new Vector3(x, y, spawnZ));
                Vector3 targetPos = m_RigRoot.TransformPoint(new Vector3(x, y, targetZ));
                LaunchFromPool(m_SliceablePool, spawnPos, targetPos, speed, false, Vector3.up);
                if (i == 0)
                    OnUltimate?.Invoke();
                yield return new WaitForSeconds(interval);
            }
        }

        // --- Radial fan (Mage): an arc of orbs that fan out and converge together. ---
        IEnumerator RadialFanRoutine()
        {
            SpawnRadialFan();
            OnUltimate?.Invoke();
            yield break;
        }

        void SpawnRadialFan()
        {
            int count = m_Config.FanCount;
            float half = m_Config.FanArcHalfWidth;
            float yb = m_Config.UltimateSlashYBottom;
            float yt = m_Config.UltimateSlashYTop;
            float ymid = (yb + yt) * 0.5f;
            float speed = CurrentObjectSpeed() * m_Config.UltimateSpeedMultiplier;
            float spawnZ = m_Config.UltimateSlashSpawnZ;
            float targetZ = m_Config.UltimateSlashTargetZ;

            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0.5f : (float)i / (count - 1);
                float x = Mathf.Lerp(-half, half, t);
                // Arc: edges sit lower than the centre so the fan reads as a curved spray.
                float y = Mathf.Lerp(yt, yb, Mathf.Abs(0.5f - t) * 2f);
                Vector3 spawnPos = m_RigRoot.TransformPoint(new Vector3(x, y, spawnZ));
                // Converge toward a point in front of the player so the orbs sweep inward together.
                Vector3 targetPos = m_RigRoot.TransformPoint(new Vector3(x * 0.25f, ymid, targetZ));
                LaunchFromPool(m_SliceablePool, spawnPos, targetPos, speed, false, Vector3.up);
            }
        }

        // --- Charged orb (Mage): a single slow, angle-gated orb you must cut at the right angle. ---
        IEnumerator ChargedOrbRoutine()
        {
            float yb = m_Config.UltimateSlashYBottom;
            float yt = m_Config.UltimateSlashYTop;
            float ymid = (yb + yt) * 0.5f;
            float speed = CurrentObjectSpeed() * 0.8f;
            float spawnZ = m_Config.UltimateSlashSpawnZ;
            float targetZ = m_Config.UltimateSlashTargetZ;

            Vector3 spawnPos = m_RigRoot.TransformPoint(new Vector3(0f, ymid, spawnZ));
            Vector3 targetPos = m_RigRoot.TransformPoint(new Vector3(0f, ymid, targetZ));
            Vector3 dir = m_RigRoot.TransformDirection(k_AngleDirs[UnityEngine.Random.Range(0, k_AngleDirs.Length)]);
            LaunchFromPool(m_SliceablePool, spawnPos, targetPos, speed, true, dir);
            OnUltimate?.Invoke();
            yield break;
        }

        // --- Raise dead (King): simultaneous multi-lane clusters, two waves. ---
        IEnumerator RaiseDeadRoutine()
        {
            SpawnAllLanesBurst();
            OnUltimate?.Invoke();
            yield return new WaitForSeconds(m_Config.UltimateSecondWaveDelay);
            SpawnAllLanesBurst();
        }

        void SpawnAllLanesBurst()
        {
            float speed = CurrentObjectSpeed() * m_Config.UltimateSpeedMultiplier;
            for (int i = 0; i < m_Lanes.LaneCount; i++)
            {
                LaneLayout.Lane lane = m_Lanes.GetLane(i);
                Vector3 spawnPos = m_RigRoot.TransformPoint(lane.spawnOffset);
                Vector3 targetPos = m_RigRoot.TransformPoint(lane.targetOffset);
                LaunchFromPool(m_SliceablePool, spawnPos, targetPos, speed, false, Vector3.up);
            }
        }

        // --- Boss cycle (King): slash line -> radial fan -> rapid volley in sequence. ---
        IEnumerator BossCycleRoutine()
        {
            yield return SlashLineRoutine();
            yield return new WaitForSeconds(m_Config.UltimateSecondWaveDelay);
            SpawnRadialFan();
            OnUltimate?.Invoke();
            yield return new WaitForSeconds(m_Config.UltimateSecondWaveDelay);
            yield return RapidVolleyRoutine();
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

            float speed = CurrentObjectSpeed();
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
