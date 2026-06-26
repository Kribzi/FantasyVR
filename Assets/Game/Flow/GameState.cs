namespace FantasyVR.Flow
{
    /// <summary>Top-level game states. M1 exercises Boot -> Combat -> Scoreboard -> (Combat | Town).</summary>
    public enum GameState
    {
        Boot,
        Combat,
        Scoreboard,
        Town
    }
}
