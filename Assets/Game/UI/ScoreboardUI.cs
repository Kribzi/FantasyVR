using System;
using FantasyVR.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using XRMultiplayer;

namespace FantasyVR.UI
{
    /// <summary>
    /// World-space post-combat panel. On a win it reads as a floating "VICTORY" banner up top, the run
    /// stats off to the right, and a single crossed-swords "Fight" button low in front of the player.
    /// On a loss it shows "FAILURE" with Play Again / Return to Town. Buttons use the template
    /// <see cref="TextButton"/>; the primary button auto-centres when it is the only one shown.
    /// </summary>
    public class ScoreboardUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField, Tooltip("Root toggled on/off when showing/hiding.")]
        GameObject m_Root;

        [Header("Text")]
        [SerializeField] TMP_Text m_TitleText;
        [SerializeField] TMP_Text m_ScoreText;
        [SerializeField] TMP_Text m_StatsText;

        [Header("Buttons")]
        [SerializeField, FormerlySerializedAs("m_PlayAgainButton"),
         Tooltip("Primary action (Fight on a win, Play Again on a loss).")]
        TextButton m_PrimaryButton;

        [SerializeField, FormerlySerializedAs("m_ReturnToTownButton"),
         Tooltip("Secondary action (Return to Town); hidden when not supplied.")]
        TextButton m_SecondaryButton;

        [Header("Button layout (anchored)")]
        [SerializeField, Tooltip("Vertical position of the button row.")]
        float m_ButtonsY = -210f;

        [SerializeField, Tooltip("Horizontal offset of each button when two are shown.")]
        float m_ButtonSpacingX = 170f;

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

        /// <summary>Show the victory banner with a single "Fight" button that starts the next bout.</summary>
        public void ShowVictory(CombatResult result, Action onFight)
        {
            Show(result, victory: true, primaryLabel: "Fight", onPrimary: onFight);
        }

        /// <summary>Show the failure panel with Play Again (primary) and Return to Town (secondary).</summary>
        public void ShowFailure(CombatResult result, Action onPlayAgain, Action onReturnToTown)
        {
            Show(result, victory: false, primaryLabel: "Play Again", onPrimary: onPlayAgain,
                secondaryLabel: "Return to Town", onSecondary: onReturnToTown);
        }

        /// <summary>Core display routine. A null <paramref name="onSecondary"/> hides the second button.</summary>
        public void Show(CombatResult result, bool victory, string primaryLabel, Action onPrimary,
            string secondaryLabel = null, Action onSecondary = null)
        {
            if (m_Root != null)
                m_Root.SetActive(true);

            if (m_TitleText != null)
                m_TitleText.text = victory ? "VICTORY" : "FAILURE";

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

            bool hasSecondary = onSecondary != null && !string.IsNullOrEmpty(secondaryLabel);

            if (m_PrimaryButton != null && m_PrimaryButton.button != null)
            {
                m_PrimaryButton.button.gameObject.SetActive(true);
                m_PrimaryButton.UpdateButton(() => onPrimary?.Invoke(), primaryLabel);
                SetButtonX(m_PrimaryButton, hasSecondary ? -m_ButtonSpacingX : 0f);
            }

            if (m_SecondaryButton != null && m_SecondaryButton.button != null)
            {
                m_SecondaryButton.button.gameObject.SetActive(hasSecondary);
                if (hasSecondary)
                {
                    m_SecondaryButton.UpdateButton(() => onSecondary.Invoke(), secondaryLabel);
                    SetButtonX(m_SecondaryButton, m_ButtonSpacingX);
                }
            }
        }

        void SetButtonX(TextButton button, float x)
        {
            if (button == null || button.button == null)
                return;
            var rt = button.button.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = new Vector2(x, m_ButtonsY);
        }
    }
}
