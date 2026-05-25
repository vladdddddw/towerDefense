using System;
using UnityEngine;

namespace Vanguard.TD.Economy
{
    /// <summary>
    /// Tracks defender credits and adversary swarm budget. Pure data + events,
    /// no UI knowledge.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CreditLedger : MonoBehaviour
    {
        public static CreditLedger Live { get; private set; }

        [SerializeField] int startingCredits   = 300;
        [SerializeField] int startingBudget    = 200;
        [SerializeField] int budgetGrowth      = 50;

        public int Credits { get; private set; }
        public int SwarmBudget { get; private set; }

        public event Action<int> CreditsChanged;
        public event Action<int> SwarmBudgetChanged;

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        public void Reset()
        {
            Credits     = startingCredits;
            SwarmBudget = startingBudget;
            CreditsChanged?.Invoke(Credits);
            SwarmBudgetChanged?.Invoke(SwarmBudget);
        }

        /// <returns>false if not enough credits — caller should refuse the action.</returns>
        public bool TryDebit(int amount)
        {
            if (amount > Credits) return false;
            Credits -= amount;
            CreditsChanged?.Invoke(Credits);
            return true;
        }

        public void Credit(int amount)
        {
            Credits += amount;
            CreditsChanged?.Invoke(Credits);
            Core.SfxRack.Coin();
        }

        public void ExpandSwarmBudget()
        {
            SwarmBudget += budgetGrowth;
            SwarmBudgetChanged?.Invoke(SwarmBudget);
        }
    }
}
