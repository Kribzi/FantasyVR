using System;
using FantasyVR.Scoring;
using TMPro;
using UnityEngine;
using XRMultiplayer;

namespace FantasyVR.UI
{
    /// <summary>
    /// World-space post-combat scoreboard. Shows the encounter stats and offers Play Again (primary)
    /// and Return to Town (stub). Buttons are wired with the template's <see cref="TextButton"/>.
    /// </summary>
    public class ScoreboardUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField, Tooltip("Root toggled on/off when showing/hiding the scoreboard.")]
        GameObject m_Root;

        [Header("Text")]
        [SerializeField] TMP_Text m_TitleText;
        [SerializeField] TMP_Text m_ScoreText;
        [SerializeField] TMP_Text m_StatsText;

        [Header("Buttons")]
        [SerializeField] TextButton m_PlayAgainButton;
        [SerializeField] TextButton m_ReturnToTownButton;

        void Awake()
        {
            if (m_Root == null)
                m_Root = gameObject;
        }

        public void Hide()
        {
            if (m_Root != null)
                m_Root.SetActive(false);
        }

        /// <summary>Display the result and wire the two actions.</summary>
        public void Show(CombatResult result, Action onPlayAgain, Action onReturnToTown)
        {
            if (m_Root != null)
                m_Root.SetActive(true);

            if (m_TitleText != null)
                m_TitleText.text = "VICTORY";

            if (m_ScoreText != null)
                m_ScoreText.text = result.Score.ToString();

            if (m_StatsText != null)
            {
                m_StatsText.text =
                    $"Best Combo: {result.HighestCombo}\n" +
                    $"Accuracy: {result.Accuracy * 100f:0}%\n" +
                    $"Sliced: {result.ObjectsSliced}/{result.ObjectsSpawned}\n" +
                    $"Potions: {result.PotionsCollected}\n" +
                    $"Damage: {result.DamageDealt:0}\n" +
                    $"Time: {result.Duration:0.0}s";
            }

            if (m_PlayAgainButton != null && m_PlayAgainButton.button != null)
                m_PlayAgainButton.UpdateButton(() => onPlayAgain?.Invoke(), "Play Again");

            if (m_ReturnToTownButton != null && m_ReturnToTownButton.button != null)
                m_ReturnToTownButton.UpdateButton(() => onReturnToTown?.Invoke(), "Return to Town");
        }
    }
}
