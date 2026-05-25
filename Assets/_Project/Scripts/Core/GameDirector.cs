using System.Collections;
using UnityEngine;
using Vanguard.TD.Economy;
using Vanguard.TD.Gameplay;
using Vanguard.TD.Map;
using Vanguard.TD.Waves;

namespace Vanguard.TD.Core
{
    /// <summary>
    /// Top-level coordinator. Owns round counter, hooks reactor death, and
    /// orchestrates phase transitions across all systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameDirector : MonoBehaviour
    {
        public static GameDirector Live { get; private set; }

        [SerializeField] int totalRounds = 10;
        public int Round { get; private set; }
        public int Total => totalRounds;

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
        }

        void Start()
        {
            // Wait one frame so other singletons can initialize
            StartCoroutine(BootRoutine());
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        IEnumerator BootRoutine()
        {
            yield return null;

            HostilePool.Live?.Boot();
            CreditLedger.Live?.Reset();
            Reactor.Live?.Reset();

            if (Reactor.Live != null) Reactor.Live.Collapsed += OnReactorLost;

            Round = 0;
            GameFlow.Live?.Switch(MatchPhase.Loadout);
        }

        public void IgniteWave()
        {
            if (GameFlow.Live == null || GameFlow.Live.Phase != MatchPhase.Loadout) return;

            Round++;
            var composer = FindAnyObjectByType<SwarmComposer>();
            var queue    = composer != null
                ? composer.Compose(Round, CreditLedger.Live.SwarmBudget)
                : null;

            GameFlow.Live.Switch(MatchPhase.Engagement);
            SwarmDirector.Live?.Launch(queue, BoardLayout.Live != null ? BoardLayout.Live.route : null);
        }

        public void RoundFinished()
        {
            if (GameFlow.Live == null) return;
            GameFlow.Live.Switch(MatchPhase.Aftermath);

            CreditLedger.Live?.ExpandSwarmBudget();

            if (Round >= totalRounds)
            {
                EndMatch(victory: true);
                return;
            }
            StartCoroutine(NextRoundRoutine());
        }

        IEnumerator NextRoundRoutine()
        {
            yield return new WaitForSeconds(1.0f);
            GameFlow.Live?.Switch(MatchPhase.Loadout);
        }

        void OnReactorLost() => EndMatch(victory: false);

        public void EndMatch(bool victory)
        {
            SwarmDirector.Live?.Halt();
            HostilePool.Live?.Sweep();
            GameFlow.Live?.Switch(MatchPhase.Endgame);
            if (victory) SfxRack.Victory();
            else         SfxRack.Defeat();
            UI.VictoryPanel.Live?.Reveal(victory);
        }

        public void Restart()
        {
            SwarmDirector.Live?.Halt();
            HostilePool.Live?.Sweep();
            // Destroy all turrets
            foreach (var t in FindObjectsByType<Turret>(FindObjectsSortMode.None))
                Destroy(t.gameObject);

            CreditLedger.Live?.Reset();
            Reactor.Live?.Reset();
            UI.VictoryPanel.Live?.Hide();
            Round = 0;
            GameFlow.Live?.Switch(MatchPhase.Loadout);
        }
    }
}
