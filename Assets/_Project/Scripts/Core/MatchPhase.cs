namespace Vanguard.TD.Core
{
    /// <summary>
    /// One of the discrete phases of a match. Drives every reactive UI component.
    /// </summary>
    public enum MatchPhase
    {
        Title,        // main menu visible
        Loadout,      // player places turrets, AI builds swarm
        Engagement,   // swarm advances, turrets fire
        Aftermath,    // round finished, awarding credits, scaling AI budget
        Endgame       // win or loss screen
    }
}
