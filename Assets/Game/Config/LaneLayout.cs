using System;
using UnityEngine;

namespace FantasyVR.Config
{
    /// <summary>
    /// Defines the lane field for spawning. Each lane has a spawn point (near the enemy) and a
    /// target point (near the player's reach), expressed as local offsets from the combat rig root.
    /// Authored as a ScriptableObject so designers can retune reachable zones without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "LaneLayout", menuName = "FantasyVR/Lane Layout")]
    public class LaneLayout : ScriptableObject
    {
        [Serializable]
        public struct Lane
        {
            [Tooltip("Spawn position, local to the combat rig (near the enemy side, +Z forward toward enemy).")]
            public Vector3 spawnOffset;

            [Tooltip("Target position the object travels to, local to the combat rig (within the player's reach).")]
            public Vector3 targetOffset;
        }

        [SerializeField, Tooltip("All lanes objects can travel along. A 3x3-ish reachable grid is a good default.")]
        Lane[] m_Lanes =
        {
            // Three columns (left/centre/right) x two rows (waist/shoulder), enemy at +Z ~4.5m.
            new Lane { spawnOffset = new Vector3(-0.6f, 1.1f, 4.0f), targetOffset = new Vector3(-0.45f, 1.0f, 0.35f) },
            new Lane { spawnOffset = new Vector3( 0.0f, 1.2f, 4.0f), targetOffset = new Vector3( 0.0f, 1.1f, 0.35f) },
            new Lane { spawnOffset = new Vector3( 0.6f, 1.1f, 4.0f), targetOffset = new Vector3( 0.45f, 1.0f, 0.35f) },
            new Lane { spawnOffset = new Vector3(-0.5f, 1.5f, 4.0f), targetOffset = new Vector3(-0.4f, 1.45f, 0.35f) },
            new Lane { spawnOffset = new Vector3( 0.5f, 1.5f, 4.0f), targetOffset = new Vector3( 0.4f, 1.45f, 0.35f) },
        };

        public int LaneCount => m_Lanes != null ? m_Lanes.Length : 0;

        public Lane GetLane(int index) => m_Lanes[index];
    }
}
