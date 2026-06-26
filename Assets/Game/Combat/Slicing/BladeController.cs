using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace FantasyVR.Combat.Slicing
{
    /// <summary>
    /// One per hand. Binds to the local XR rig's controller or tracked-hand anchor, follows its pose,
    /// and computes a smoothed velocity used for slice detection. Exposes <see cref="IsSlicing"/> and
    /// <see cref="SliceDirection"/> consumed by <see cref="FantasyVR.Spawning.SliceableObject"/>.
    /// Binding mirrors XRHandPoseReplicator.SetupLocalHands().
    /// </summary>
    public class BladeController : MonoBehaviour
    {
        [SerializeField, Tooltip("Which local rig hand this blade follows.")]
        Handedness m_Hand = Handedness.Right;

        [SerializeField, Tooltip("Minimum blade-tip speed (m/s) for a touch to count as a slice rather than a graze.")]
        float m_MinSliceSpeed = 0.8f;

        [SerializeField, Tooltip("Forward distance (m) from the hand anchor to the point velocity is sampled at. Sampling at the blade tip means a wrist flick (which barely moves the hand but whips the tip) still registers as a fast slice.")]
        float m_TipDistance = 0.7f;

        [Header("Feedback")]
        [SerializeField, Tooltip("Optional blade trail; enabled only while slicing fast enough.")]
        TrailRenderer m_Trail;

        [SerializeField, Range(0f, 1f), Tooltip("Haptic amplitude on a confirmed slice.")]
        float m_HapticAmplitude = 0.5f;

        [SerializeField, Tooltip("Haptic duration (s) on a confirmed slice.")]
        float m_HapticDuration = 0.06f;

        const int k_VelocitySamples = 3;

        XROrigin m_Origin;
        XRInputModalityManager m_Modality;
        Transform m_ControllerAnchor;
        Transform m_HandAnchor;
        HapticImpulsePlayer m_Haptics;

        readonly Vector3[] m_VelocitySamples = new Vector3[k_VelocitySamples];
        int m_SampleIndex;
        Vector3 m_PrevPos;
        bool m_Bound;

        /// <summary>Smoothed world-space velocity of the blade tip.</summary>
        public Vector3 Velocity { get; private set; }

        /// <summary>Normalised slice direction (movement direction this frame).</summary>
        public Vector3 SliceDirection { get; private set; }

        /// <summary>True when the blade is moving fast enough to slice.</summary>
        public bool IsSlicing => m_Bound && Velocity.sqrMagnitude >= m_MinSliceSpeed * m_MinSliceSpeed;

        public Handedness Hand => m_Hand;

        void Start()
        {
            Bind();
        }

        void Bind()
        {
            m_Origin = FindFirstObjectByType<XROrigin>();
            if (m_Origin == null)
                return;

            m_Origin.TryGetComponent(out m_Modality);
            if (m_Modality == null)
                return;

            GameObject controllerGo = m_Hand == Handedness.Left ? m_Modality.leftController : m_Modality.rightController;
            GameObject handGo = m_Hand == Handedness.Left ? m_Modality.leftHand : m_Modality.rightHand;

            if (controllerGo != null)
            {
                m_ControllerAnchor = controllerGo.transform;
                m_Haptics = controllerGo.GetComponentInChildren<HapticImpulsePlayer>(true);
            }

            if (handGo != null)
            {
                var skeleton = handGo.GetComponentInChildren<XRHandSkeletonDriver>(true);
                m_HandAnchor = skeleton != null ? skeleton.rootTransform : handGo.transform;
            }

            m_Bound = m_ControllerAnchor != null || m_HandAnchor != null;
            if (m_Bound)
            {
                Transform anchor = ActiveAnchor();
                if (anchor != null)
                {
                    transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                    m_PrevPos = TipPosition();
                }
            }
        }

        /// <summary>World position of the point we sample velocity at (out along the blade, not at the hand).</summary>
        Vector3 TipPosition() => transform.TransformPoint(new Vector3(0f, 0f, m_TipDistance));

        Transform ActiveAnchor()
        {
            // Prefer whichever anchor GameObject is currently active (modality manager toggles these).
            if (m_ControllerAnchor != null && m_ControllerAnchor.gameObject.activeInHierarchy)
                return m_ControllerAnchor;
            if (m_HandAnchor != null && m_HandAnchor.gameObject.activeInHierarchy)
                return m_HandAnchor;
            if (m_ControllerAnchor != null)
                return m_ControllerAnchor;
            return m_HandAnchor;
        }

        void Update()
        {
            if (!m_Bound)
            {
                Bind();
                return;
            }

            Transform anchor = ActiveAnchor();
            if (anchor == null)
                return;

            transform.SetPositionAndRotation(anchor.position, anchor.rotation);

            float dt = Time.deltaTime;
            Vector3 tipPos = TipPosition();
            if (dt > 0f)
            {
                m_VelocitySamples[m_SampleIndex] = (tipPos - m_PrevPos) / dt;
                m_SampleIndex = (m_SampleIndex + 1) % k_VelocitySamples;

                Vector3 sum = Vector3.zero;
                for (int i = 0; i < k_VelocitySamples; i++)
                    sum += m_VelocitySamples[i];
                Velocity = sum / k_VelocitySamples;

                float mag = Velocity.magnitude;
                SliceDirection = mag > 0.0001f ? Velocity / mag : Vector3.zero;
            }
            m_PrevPos = tipPos;

            if (m_Trail != null && m_Trail.emitting != IsSlicing)
                m_Trail.emitting = IsSlicing;
        }

        /// <summary>Fire a haptic pulse on the bound controller (no-op for tracked hands / missing player).</summary>
        public void PlaySliceHaptics()
        {
            if (m_Haptics != null)
                m_Haptics.SendHapticImpulse(m_HapticAmplitude, m_HapticDuration);
        }
    }
}
