using UnityEngine;

namespace Vanguard.TD.Data
{
    /// <summary>
    /// Pure data describing a hostile class (drone): health, speed, AI budget cost, payouts.
    /// </summary>
    [CreateAssetMenu(menuName = "Vanguard/Hostile Blueprint", fileName = "HostileBlueprint")]
    public sealed class HostileBlueprint : ScriptableObject
    {
        [Header("Identity")]
        public string codename = "Scout";

        [Header("Vitality & motion")]
        public int   maxIntegrity = 40;
        public float pace         = 3.0f;

        [Header("Threat")]
        public int reactorDamage = 1;

        [Header("Economy")]
        public int budgetCost = 10;   // AI swarm budget
        public int payout     = 5;    // credits dropped on destruction

        [Header("Resistances")]
        public bool chillImmune = false;

        [Header("Runtime prefab (created at boot)")]
        public GameObject prefab;
    }
}
