using System;
using UnityEngine;

namespace Vanguard.TD.Core
{
    /// <summary>
    /// Central phase controller. Every system subscribes to <see cref="PhaseChanged"/>
    /// instead of polling — the controller never knows who listens.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFlow : MonoBehaviour
    {
        public static GameFlow Live { get; private set; }

        /// <summary>Current phase. Read-only from outside; mutate via <see cref="Switch"/>.</summary>
        public MatchPhase Phase { get; private set; } = MatchPhase.Title;

        /// <summary>Raised after the phase has been committed.</summary>
        public event Action<MatchPhase> PhaseChanged;

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        public void Switch(MatchPhase next)
        {
            if (Phase == next) return;
            Phase = next;
            PhaseChanged?.Invoke(next);
        }
    }
}
