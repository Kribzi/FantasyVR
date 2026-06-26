namespace FantasyVR.Spawning
{
    /// <summary>
    /// A sliceable that heals the player instead of damaging the enemy. Missing it is harmless.
    /// All flight/slice behaviour is inherited from <see cref="SliceableObject"/>.
    /// </summary>
    public class PotionFlask : SliceableObject
    {
        public override SliceableKind Kind => SliceableKind.Potion;
    }
}
