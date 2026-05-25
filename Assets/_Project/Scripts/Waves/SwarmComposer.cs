using System.Collections.Generic;
using UnityEngine;
using Vanguard.TD.Data;

namespace Vanguard.TD.Waves
{
    /// <summary>
    /// AI adversary. Builds a queue of hostiles within the available budget,
    /// progressively introducing harder enemy classes per round.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SwarmComposer : MonoBehaviour
    {
        public HostileBlueprint scoutBP;
        public HostileBlueprint tankBP;
        public HostileBlueprint phaseBP;

        public Queue<HostileBlueprint> Compose(int round, int budget)
        {
            var queue = new Queue<HostileBlueprint>();
            if (scoutBP == null) return queue;

            int spent = 0;
            int safety = 0;
            while (spent + scoutBP.budgetCost <= budget && safety++ < 250)
            {
                var pick = scoutBP;
                float roll = Random.value;

                // Difficulty curve
                if (round >= 5 && phaseBP != null && roll < 0.25f)            pick = phaseBP;
                else if (round >= 3 && tankBP != null && roll < 0.55f)        pick = tankBP;

                if (spent + pick.budgetCost > budget) pick = scoutBP;
                queue.Enqueue(pick);
                spent += pick.budgetCost;
            }
            return queue;
        }
    }
}
