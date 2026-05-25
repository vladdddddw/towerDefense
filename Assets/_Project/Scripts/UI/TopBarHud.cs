using UnityEngine;
using TMPro;
using Vanguard.TD.Core;
using Vanguard.TD.Economy;
using Vanguard.TD.Map;

namespace Vanguard.TD.UI
{
    /// <summary>
    /// Header strip: credits, reactor integrity, round counter, phase label.
    /// Subscribes to all relevant events instead of polling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopBarHud : MonoBehaviour
    {
        public static TopBarHud Live { get; private set; }

        public TextMeshProUGUI creditsLabel;
        public TextMeshProUGUI integrityLabel;
        public TextMeshProUGUI roundLabel;
        public TextMeshProUGUI phaseLabel;

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
        }

        void Start()
        {
            if (CreditLedger.Live != null)
            {
                CreditLedger.Live.CreditsChanged += SetCredits;
                SetCredits(CreditLedger.Live.Credits);
            }
            if (Reactor.Live != null)
            {
                Reactor.Live.IntegrityChanged += SetIntegrity;
                SetIntegrity(Reactor.Live.Integrity);
            }
            if (GameFlow.Live != null)
            {
                GameFlow.Live.PhaseChanged += SetPhase;
                SetPhase(GameFlow.Live.Phase);
            }
            UpdateRound();
        }

        void Update()
        {
            // Round number is cheap polling — no event needed
            UpdateRound();
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        void SetCredits(int v)
        {
            if (creditsLabel) creditsLabel.text = $"Золото: {v}";
        }

        void SetIntegrity(int v)
        {
            if (integrityLabel) integrityLabel.text = $"База: {v} HP";
        }

        void UpdateRound()
        {
            if (roundLabel && GameDirector.Live != null)
                roundLabel.text = $"Раунд: {GameDirector.Live.Round}/{GameDirector.Live.Total}";
        }

        void SetPhase(MatchPhase p)
        {
            if (!phaseLabel) return;
            phaseLabel.text = p switch
            {
                MatchPhase.Loadout    => "[ Підготовка ]",
                MatchPhase.Engagement => "[ Битва ]",
                MatchPhase.Aftermath  => "[ Кінець раунду ]",
                MatchPhase.Endgame    => "[ Гра завершена ]",
                _                     => ""
            };
        }
    }
}
