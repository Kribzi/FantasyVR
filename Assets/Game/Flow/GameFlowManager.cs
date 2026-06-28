using System;
using FantasyVR.Combat;
using FantasyVR.Scoring;
using FantasyVR.UI;
using UnityEngine;

namespace FantasyVR.Flow
{
    /// <summary>
    /// Top-level state machine for M1. On launch it drops the player straight into combat
    /// (instant-action pillar), shows the scoreboard on enemy death, and handles Play Again /
    /// Return to Town. Town is a stub in M1.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] CombatDirector m_Director;
        [SerializeField] ScoreboardUI m_Scoreboard;

        [Header("Area roots")]
        [SerializeField, Tooltip("Root holding the combat rig, enemy, spawner, HUD.")]
        GameObject m_CombatRoot;

        [SerializeField, Tooltip("In-combat HUD root (hidden on the scoreboard).")]
        GameObject m_HudRoot;

        [SerializeField, Tooltip("Town area root (stub in M1).")]
        GameObject m_TownRoot;

        [Header("Startup")]
        [SerializeField, Tooltip("Start combat automatically on launch.")]
        bool m_AutoStartCombat = true;

        public GameState State { get; private set; } = GameState.Boot;

        void Start()
        {
            if (m_Scoreboard != null)
                m_Scoreboard.Hide();
            if (m_TownRoot != null)
                m_TownRoot.SetActive(false);

            if (m_AutoStartCombat)
                StartCombat();
        }

        /// <summary>Enter (or restart) a combat encounter.</summary>
        public void StartCombat()
        {
            State = GameState.Combat;

            if (m_Scoreboard != null)
                m_Scoreboard.Hide();
            if (m_TownRoot != null)
                m_TownRoot.SetActive(false);
            if (m_CombatRoot != null)
                m_CombatRoot.SetActive(true);
            if (m_HudRoot != null)
                m_HudRoot.SetActive(true);

            if (m_Director != null)
                m_Director.StartCombat();
        }

        /// <summary>Player died: show the FAILURE panel. Play Again retries the same enemy.</summary>
        public void ShowFailure(CombatResult result)
        {
            State = GameState.Scoreboard;

            if (m_HudRoot != null)
                m_HudRoot.SetActive(false);
            if (m_Scoreboard != null)
                m_Scoreboard.ShowFailure(result, StartCombat, GoToTown);
        }

        /// <summary>Enemy defeated: show the VICTORY banner. The single "Fight" button resumes the HUD
        /// and runs <paramref name="onContinue"/> (advance to the next, harder enemy).</summary>
        public void ShowVictory(CombatResult result, Action onContinue)
        {
            State = GameState.Scoreboard;

            if (m_HudRoot != null)
                m_HudRoot.SetActive(false);
            if (m_Scoreboard != null)
                m_Scoreboard.ShowVictory(result, () =>
                {
                    ResumeCombatUI();
                    onContinue?.Invoke();
                });
        }

        void ResumeCombatUI()
        {
            State = GameState.Combat;
            if (m_Scoreboard != null)
                m_Scoreboard.Hide();
            if (m_CombatRoot != null)
                m_CombatRoot.SetActive(true);
            if (m_HudRoot != null)
                m_HudRoot.SetActive(true);
        }

        /// <summary>Return to the town hub. Stubbed in M1: just restarts combat with a log.</summary>
        public void GoToTown()
        {
            State = GameState.Town;
            Debug.Log("[FantasyVR] Return to Town requested (Town hub is a stub in M1).");

            if (m_Scoreboard != null)
                m_Scoreboard.Hide();

            // M1 stub: no town scene yet, so loop straight back into another fight.
            StartCombat();
        }
    }
}
