using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FantasyVR.Player
{
    /// <summary>
    /// Snaps the XR rig so the player's head returns to a fixed start pose. Players drift off the
    /// intended centre over a session (room-scale walking, an imperfect spawn), so a controller button
    /// lets them re-centre at any time. Can also re-centre once on spawn to fix an off-centre start.
    /// Re-centring is horizontal + yaw only by default, so the player keeps their real standing height
    /// and never gets a nauseating vertical jolt.
    /// </summary>
    public class PlayerRecenter : MonoBehaviour
    {
        [Header("Start pose")]
        [SerializeField, Tooltip("Marker the head snaps to (position + yaw). Defaults to this transform if unset.")]
        Transform m_StartAnchor;

        [SerializeField, Tooltip("Also match the anchor's height. Off by default so the player keeps their real standing/seated height and only re-centres horizontally + facing.")]
        bool m_AlignHeight;

        [SerializeField, Tooltip("Re-centre automatically the first time tracking is live (fixes an off-centre spawn).")]
        bool m_RecenterOnStart = true;

        [Header("Input")]
        [SerializeField, Tooltip("Optional explicit re-centre action. If left empty, a built-in binding on both controllers' primary (A / X) buttons is used, so it works with no extra wiring.")]
        InputActionProperty m_RecenterAction;

        XROrigin m_Origin;
        InputAction m_DefaultAction;

        void Awake()
        {
            if (m_StartAnchor == null)
                m_StartAnchor = transform;
        }

        void OnEnable()
        {
            InputAction action = ResolveAction();
            if (action != null)
            {
                action.performed += OnRecenterPerformed;
                action.Enable();
            }
        }

        void OnDisable()
        {
            InputAction action = ResolveAction();
            if (action != null)
                action.performed -= OnRecenterPerformed;
        }

        void OnDestroy()
        {
            m_DefaultAction?.Dispose();
        }

        // Uses the inspector-assigned action when present; otherwise lazily builds a default that fires
        // on either controller's primary (A / X) button so a re-centre always has a button to press.
        InputAction ResolveAction()
        {
            if (m_RecenterAction.action != null)
                return m_RecenterAction.action;

            if (m_DefaultAction == null)
            {
                m_DefaultAction = new InputAction("Recenter", InputActionType.Button);
                m_DefaultAction.AddBinding("<XRController>{LeftHand}/primaryButton");
                m_DefaultAction.AddBinding("<XRController>{RightHand}/primaryButton");
            }
            return m_DefaultAction;
        }

        void Start()
        {
            m_Origin = FindFirstObjectByType<XROrigin>();
            if (m_RecenterOnStart)
                StartCoroutine(RecenterWhenTrackingReady());
        }

        void OnRecenterPerformed(InputAction.CallbackContext _) => Recenter();

        IEnumerator RecenterWhenTrackingReady()
        {
            // The HMD pose is often zero for the first frame(s); wait until the camera reports a real
            // offset (or give up after a short window) so the auto re-centre uses a valid head pose.
            const float timeout = 2f;
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                if (m_Origin != null && m_Origin.Camera != null &&
                    m_Origin.Camera.transform.localPosition.sqrMagnitude > 0.0001f)
                    break;
                elapsed += Time.deltaTime;
                yield return null;
            }
            Recenter();
        }

        /// <summary>Snap the rig so the camera sits at the start anchor's position and facing.</summary>
        public void Recenter()
        {
            if (m_Origin == null)
                m_Origin = FindFirstObjectByType<XROrigin>();
            if (m_Origin == null || m_Origin.Camera == null || m_StartAnchor == null)
                return;

            Transform cam = m_Origin.Camera.transform;
            Vector3 up = m_Origin.transform.up;

            // 1) Yaw: rotate the rig about the camera so the head ends up facing the anchor's forward.
            Vector3 camFwd = Vector3.ProjectOnPlane(cam.forward, up);
            Vector3 targetFwd = Vector3.ProjectOnPlane(m_StartAnchor.forward, up);
            if (camFwd.sqrMagnitude > 0.0001f && targetFwd.sqrMagnitude > 0.0001f)
            {
                float yaw = Vector3.SignedAngle(camFwd, targetFwd, up);
                m_Origin.RotateAroundCameraUsingOriginUp(yaw);
            }

            // 2) Position: move so the head sits at the anchor. Preserve real head height unless asked.
            Vector3 dest = m_StartAnchor.position;
            if (!m_AlignHeight)
                dest.y = cam.position.y;
            m_Origin.MoveCameraToWorldLocation(dest);
        }
    }
}
