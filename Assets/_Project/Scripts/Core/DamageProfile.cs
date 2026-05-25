namespace Vanguard.TD.Core
{
    /// <summary>
    /// Kind of damage delivered on impact. Used by <c>Bolt</c> to branch behavior.
    /// </summary>
    public enum DamageProfile
    {
        Direct,   // single-target hit
        Splash,   // AoE around impact point
        Chill     // primary damage + movement slow
    }
}
