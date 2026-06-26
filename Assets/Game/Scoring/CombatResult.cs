namespace FantasyVR.Scoring
{
    /// <summary>
    /// Plain data describing the outcome of one combat encounter, handed to the scoreboard UI.
    /// </summary>
    public struct CombatResult
    {
        public int Score;
        public float DamageDealt;
        public int ObjectsSpawned;
        public int ObjectsSliced;
        public int HighestCombo;
        public int PotionsCollected;
        public float Duration;

        /// <summary>Slice accuracy as a 0..1 fraction of spawned (non-potion) objects that were sliced.</summary>
        public float Accuracy => ObjectsSpawned > 0 ? (float)ObjectsSliced / ObjectsSpawned : 0f;
    }
}
