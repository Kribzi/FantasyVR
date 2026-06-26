using UnityEngine;

namespace FantasyVR.Combat.Slicing
{
    /// <summary>
    /// The physical blade collider attached under a <see cref="BladeController"/>. Carries no slice
    /// logic itself; it simply exposes its owning controller so a sliceable can read speed/direction.
    /// Requires a trigger collider; sliceables detect this component on overlap.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Blade : MonoBehaviour
    {
        [SerializeField, Tooltip("Owning controller. Auto-resolved from parents if left empty.")]
        BladeController m_Controller;

        public BladeController Controller => m_Controller;

        void Awake()
        {
            if (m_Controller == null)
                m_Controller = GetComponentInParent<BladeController>();
        }
    }
}
