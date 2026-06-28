using System.Collections.Generic;
using UnityEngine;

namespace FantasyVR.Combat.Slicing
{
    /// <summary>
    /// Spawns two physics-driven hemisphere halves when an orb is sliced: the orb appears to split
    /// along the blade's cut, the halves fly apart and fall to the ground. Fully self-contained and
    /// allocation-free after warm-up: it builds its own hemisphere mesh and a fixed pool of pieces,
    /// recycling the oldest when the pool is exhausted. Quest-friendly (small meshes, capped count).
    /// </summary>
    public class SliceDebrisSystem : MonoBehaviour
    {
        public static SliceDebrisSystem Instance { get; private set; }

        [Header("Pool")]
        [SerializeField, Tooltip("Number of half-pieces kept alive (2 per slice). Oldest is recycled when exhausted.")]
        int m_PoolSize = 32;

        [SerializeField, Tooltip("Seconds a half lives before it is recycled (shrinks out over the last fraction).")]
        float m_Lifetime = 3f;

        [Header("Launch")]
        [SerializeField, Tooltip("Sideways speed each half gets along the cut (m/s).")]
        float m_SeparationSpeed = 1.4f;

        [SerializeField, Tooltip("Upward pop added to each half (m/s).")]
        float m_UpwardPop = 0.7f;

        [SerializeField, Tooltip("Fraction of the orb's travel velocity the halves inherit.")]
        float m_InheritFactor = 0.35f;

        [SerializeField, Tooltip("Random tumble (deg/s magnitude).")]
        float m_Spin = 540f;

        [Header("Physics")]
        [SerializeField, Tooltip("Mass of a half-piece rigidbody.")]
        float m_Mass = 0.2f;

        [SerializeField, Range(0f, 1f), Tooltip("Bounciness of the halves on the floor.")]
        float m_Bounciness = 0.35f;

        Mesh m_HemisphereMesh;
        PhysicsMaterial m_PhysicsMaterial;
        Transform m_Cam;

        Transform[] m_Tr;
        Rigidbody[] m_Rb;
        MeshRenderer[] m_Mr;
        float[] m_Life;
        float[] m_BaseScale;
        bool[] m_Active;
        int m_Next;

        void Awake()
        {
            Instance = this;
            m_HemisphereMesh = BuildHemisphere(12, 6);

            m_PhysicsMaterial = new PhysicsMaterial("SliceDebris")
            {
                bounciness = m_Bounciness,
                dynamicFriction = 0.6f,
                staticFriction = 0.6f,
                bounceCombine = PhysicsMaterialCombine.Maximum
            };

            int n = Mathf.Max(2, m_PoolSize);
            m_Tr = new Transform[n];
            m_Rb = new Rigidbody[n];
            m_Mr = new MeshRenderer[n];
            m_Life = new float[n];
            m_BaseScale = new float[n];
            m_Active = new bool[n];

            for (int i = 0; i < n; i++)
                CreatePiece(i);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void CreatePiece(int i)
        {
            var go = new GameObject("SliceHalf");
            go.transform.SetParent(transform, false);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = m_HemisphereMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.6f;
            col.center = new Vector3(0f, 0.35f, 0f);
            col.material = m_PhysicsMaterial;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = m_Mass;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            go.SetActive(false);

            m_Tr[i] = go.transform;
            m_Rb[i] = rb;
            m_Mr[i] = mr;
            m_Active[i] = false;
        }

        /// <summary>Spawn the two halves of a sliced orb. Safe to call every slice.</summary>
        public void Spawn(Vector3 center, Vector3 sliceDir, Vector3 inheritVelocity, Material material, float radius)
        {
            if (m_Cam == null && Camera.main != null)
                m_Cam = Camera.main.transform;

            // Cut plane normal: perpendicular to both the blade's sweep and the view, so the halves
            // split across the slash as the player sees it.
            Vector3 view = m_Cam != null ? (center - m_Cam.position) : Vector3.forward;
            Vector3 cut = Vector3.Cross(sliceDir.normalized, view.normalized);
            if (cut.sqrMagnitude < 1e-4f)
                cut = Vector3.up;
            cut.Normalize();

            float r = Mathf.Max(0.02f, radius);
            Vector3 inherit = inheritVelocity * m_InheritFactor;

            LaunchHalf(NextIndex(), center, Quaternion.FromToRotation(Vector3.up, cut), material, r,
                inherit + cut * m_SeparationSpeed + Vector3.up * m_UpwardPop);
            LaunchHalf(NextIndex(), center, Quaternion.FromToRotation(Vector3.up, -cut), material, r,
                inherit - cut * m_SeparationSpeed + Vector3.up * (m_UpwardPop * 0.7f));
        }

        int NextIndex()
        {
            int i = m_Next;
            m_Next = (m_Next + 1) % m_Tr.Length;
            return i;
        }

        void LaunchHalf(int i, Vector3 pos, Quaternion rot, Material material, float scale, Vector3 velocity)
        {
            Transform t = m_Tr[i];
            Rigidbody rb = m_Rb[i];

            if (material != null)
                m_Mr[i].sharedMaterial = material;

            t.SetPositionAndRotation(pos, rot * Quaternion.Euler(Random.Range(0f, 360f), 0f, 0f));
            t.localScale = new Vector3(scale, scale, scale);

            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);

            rb.isKinematic = false;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = velocity;
#else
            rb.velocity = velocity;
#endif
            rb.angularVelocity = Random.insideUnitSphere * (m_Spin * Mathf.Deg2Rad);

            m_BaseScale[i] = scale;
            m_Life[i] = m_Lifetime;
            m_Active[i] = true;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < m_Active.Length; i++)
            {
                if (!m_Active[i])
                    continue;

                m_Life[i] -= dt;
                if (m_Life[i] <= 0f)
                {
                    Deactivate(i);
                    continue;
                }

                // Shrink out over the final 0.4s so pieces vanish gracefully instead of popping.
                float fade = m_Life[i] / 0.4f;
                if (fade < 1f)
                {
                    float s = m_BaseScale[i] * Mathf.Clamp01(fade);
                    m_Tr[i].localScale = new Vector3(s, s, s);
                }
            }
        }

        void Deactivate(int i)
        {
            m_Active[i] = false;
            Rigidbody rb = m_Rb[i];
            rb.isKinematic = true;
            m_Tr[i].gameObject.SetActive(false);
        }

        /// <summary>Solid hemisphere: dome along +Y with a flat cap at y=0, radius 1.</summary>
        static Mesh BuildHemisphere(int slices, int stacks)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris = new List<int>();

            // Dome.
            for (int i = 0; i <= stacks; i++)
            {
                float phi = (Mathf.PI * 0.5f) * (i / (float)stacks); // 0 at equator -> PI/2 at pole
                float y = Mathf.Sin(phi);
                float ringR = Mathf.Cos(phi);
                for (int j = 0; j <= slices; j++)
                {
                    float theta = (2f * Mathf.PI) * (j / (float)slices);
                    var v = new Vector3(ringR * Mathf.Cos(theta), y, ringR * Mathf.Sin(theta));
                    verts.Add(v);
                    norms.Add(v.normalized);
                }
            }
            int ringStride = slices + 1;
            for (int i = 0; i < stacks; i++)
            {
                for (int j = 0; j < slices; j++)
                {
                    int a = i * ringStride + j;
                    int b = a + ringStride;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }
            }

            // Flat cap at y=0, facing -Y.
            int center = verts.Count;
            verts.Add(Vector3.zero);
            norms.Add(Vector3.down);
            int capStart = verts.Count;
            for (int j = 0; j <= slices; j++)
            {
                float theta = (2f * Mathf.PI) * (j / (float)slices);
                verts.Add(new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta)));
                norms.Add(Vector3.down);
            }
            for (int j = 0; j < slices; j++)
            {
                tris.Add(center);
                tris.Add(capStart + j);
                tris.Add(capStart + j + 1);
            }

            var mesh = new Mesh { name = "Hemisphere" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
