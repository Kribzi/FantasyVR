using FantasyVR.Combat;
using FantasyVR.Enemy;
using FantasyVR.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FantasyVR.UI
{
    /// <summary>
    /// Minimal in-combat HUD: enemy HP bar, current combo/multiplier, and player HP. Driven by
    /// events from the combat systems; no per-frame polling.
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] EnemySkeleton m_Enemy;
        [SerializeField] ComboSystem m_Combo;
        [SerializeField] PlayerHealth m_PlayerHealth;
        [SerializeField] ScoreTracker m_Score;

        [Header("Enemy")]
        [SerializeField] Slider m_EnemyHealthBar;

        [Header("Combo")]
        [SerializeField] TMP_Text m_ComboText;
        [SerializeField] TMP_Text m_MultiplierText;

        [Header("Player")]
        [SerializeField] Slider m_PlayerHealthBar;

        [Header("Score")]
        [SerializeField] TMP_Text m_ScoreText;

        void OnEnable()
        {
            if (m_Enemy != null) m_Enemy.OnDamaged += HandleEnemyHealth;
            if (m_Combo != null) m_Combo.OnComboChanged += HandleCombo;
            if (m_PlayerHealth != null) m_PlayerHealth.OnHealthChanged += HandlePlayerHealth;
            if (m_Score != null) m_Score.OnScoreChanged += HandleScore;
            Refresh();
        }

        void OnDisable()
        {
            if (m_Enemy != null) m_Enemy.OnDamaged -= HandleEnemyHealth;
            if (m_Combo != null) m_Combo.OnComboChanged -= HandleCombo;
            if (m_PlayerHealth != null) m_PlayerHealth.OnHealthChanged -= HandlePlayerHealth;
            if (m_Score != null) m_Score.OnScoreChanged -= HandleScore;
        }

        void Refresh()
        {
            if (m_Enemy != null) HandleEnemyHealth(m_Enemy.Health, m_Enemy.MaxHealth);
            if (m_Combo != null) HandleCombo(m_Combo.Combo, m_Combo.Multiplier);
            if (m_PlayerHealth != null) HandlePlayerHealth(m_PlayerHealth.Current, m_PlayerHealth.Max);
            if (m_Score != null) HandleScore(m_Score.Score);
        }

        void HandleEnemyHealth(float current, float max)
        {
            if (m_EnemyHealthBar != null)
                m_EnemyHealthBar.value = max > 0f ? current / max : 0f;
        }

        void HandleCombo(int combo, float multiplier)
        {
            if (m_ComboText != null)
                m_ComboText.text = combo > 0 ? $"{combo} HITS" : string.Empty;
            if (m_MultiplierText != null)
                m_MultiplierText.text = $"x{multiplier:0.##}";
        }

        void HandlePlayerHealth(float current, float max)
        {
            if (m_PlayerHealthBar != null)
                m_PlayerHealthBar.value = max > 0f ? current / max : 0f;
        }

        void HandleScore(int score)
        {
            if (m_ScoreText != null)
                m_ScoreText.text = score.ToString();
        }
    }
}
