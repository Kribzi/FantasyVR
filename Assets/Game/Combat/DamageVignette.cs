using UnityEngine;
using UnityEngine.UI;

namespace FantasyVR.Combat
{
    /// <summary>
    /// Classic "you got hit" feedback: the screen edges flash red whenever the player loses health.
    /// Builds its own world-space canvas parented to the HMD camera with a runtime-generated radial
    /// vignette sprite (transparent centre, solid edges), so it needs no manual UI wiring and reads
    /// the same in both eyes. The flash fades out smoothly to avoid a jarring strobe.
    /// </summary>
    public class DamageVignette : MonoBehaviour
    {
        [SerializeField, Tooltip("Player health to watch. Auto-found if left empty.")]
        PlayerHealth m_Health;

        [SerializeField, Tooltip("Vignette tint.")]
        Color m_Color = new Color(0.75f, 0.02f, 0.02f, 1f);

        [SerializeField, Range(0f, 1f), Tooltip("Peak opacity of the flash.")]
        float m_MaxAlpha = 0.55f;

        [SerializeField, Tooltip("Seconds for a flash to fade back to clear.")]
        float m_FadeDuration = 0.6f;

        [SerializeField, Tooltip("Metres in front of the eyes the vignette plane sits.")]
        float m_Distance = 0.4f;

        Camera m_Cam;
        Image m_Image;
        float m_Alpha;
        float m_PrevHealth = -1f;

        void Start()
        {
            if (m_Health == null)
                m_Health = FindFirstObjectByType<PlayerHealth>();

            BuildUI();

            if (m_Health != null)
                m_Health.OnHealthChanged += OnHealthChanged;
        }

        void OnDestroy()
        {
            if (m_Health != null)
                m_Health.OnHealthChanged -= OnHealthChanged;
        }

        void BuildUI()
        {
            m_Cam = Camera.main;
            Transform parent = m_Cam != null ? m_Cam.transform : transform;

            var canvasGo = new GameObject("DamageVignetteCanvas");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, m_Distance);
            canvasGo.transform.localRotation = Quaternion.identity;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 2f);

            // Scale the 2x2 canvas so it more than covers the camera frustum at m_Distance (1.7x margin
            // so the red edges always sit just outside the visible field rather than inside it).
            float fov = m_Cam != null ? m_Cam.fieldOfView : 80f;
            float coverH = 2f * m_Distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * 1.7f;
            canvasGo.transform.localScale = Vector3.one * (coverH / 2f);

            var imgGo = new GameObject("Vignette");
            imgGo.transform.SetParent(canvasGo.transform, false);
            m_Image = imgGo.AddComponent<Image>();
            RectTransform irt = m_Image.rectTransform;
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;

            m_Image.sprite = CreateVignetteSprite();
            m_Image.raycastTarget = false;
            m_Image.color = new Color(m_Color.r, m_Color.g, m_Color.b, 0f);
        }

        // White RGB with a radial alpha ramp (clear centre -> opaque corners). Image.color tints it red.
        Sprite CreateVignetteSprite()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            Vector2 centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxDist = centre.magnitude;
            const float inner = 0.55f; // start of the red, as a fraction of the half-diagonal
            const float outer = 1.0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), centre) / maxDist;
                    float a = Mathf.Clamp01((d - inner) / (outer - inner));
                    a = a * a; // ease so the centre stays clear longer
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        void OnHealthChanged(float current, float max)
        {
            // Only flash on an actual decrease; ignore the initial set and any heals.
            if (m_PrevHealth >= 0f && current < m_PrevHealth - 0.001f)
                m_Alpha = m_MaxAlpha;
            m_PrevHealth = current;
        }

        void Update()
        {
            if (m_Image == null || m_Alpha <= 0f)
                return;

            float fade = m_FadeDuration > 0f ? m_MaxAlpha / m_FadeDuration : m_MaxAlpha;
            m_Alpha = Mathf.Max(0f, m_Alpha - fade * Time.deltaTime);

            Color c = m_Image.color;
            c.a = m_Alpha;
            m_Image.color = c;
        }
    }
}
