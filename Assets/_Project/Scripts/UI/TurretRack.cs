using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vanguard.TD.Core;
using Vanguard.TD.Data;
using Vanguard.TD.Economy;

namespace Vanguard.TD.UI
{
    /// <summary>
    /// Right-side rack with turret cards. Click selects, ESC/right-click clears.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurretRack : MonoBehaviour
    {
        public static TurretRack Live { get; private set; }

        [Header("Root panel")]
        public GameObject rackPanel;

        [Header("Cards (button + tint)")]
        public Button pulseCard;
        public Button plasmaCard;
        public Button cryoCard;
        public Button railCard;

        [Header("Blueprints")]
        public TurretBlueprint pulseBP;
        public TurretBlueprint plasmaBP;
        public TurretBlueprint cryoBP;
        public TurretBlueprint railBP;

        [Header("Labels")]
        public TextMeshProUGUI creditsLabel;
        public TextMeshProUGUI chosenLabel;

        public TurretBlueprint Chosen { get; private set; }

        static readonly Color PulseHue  = new(0.05f, 0.55f, 0.85f);
        static readonly Color PlasmaHue = new(0.55f, 0.10f, 0.85f);
        static readonly Color CryoHue   = new(0.10f, 0.80f, 0.95f);
        static readonly Color RailHue   = new(0.85f, 0.20f, 0.35f);
        static readonly Color PickHue   = new(0.95f, 0.85f, 0.20f);

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
        }

        void Start()
        {
            Bind(pulseCard,  pulseBP);
            Bind(plasmaCard, plasmaBP);
            Bind(cryoCard,   cryoBP);
            Bind(railCard,   railBP);

            if (GameFlow.Live != null)
            {
                GameFlow.Live.PhaseChanged += OnPhase;
                OnPhase(GameFlow.Live.Phase);
            }
            if (CreditLedger.Live != null)
            {
                CreditLedger.Live.CreditsChanged += OnCredits;
                OnCredits(CreditLedger.Live.Credits);
            }
            PaintLabel();
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        void Bind(Button btn, TurretBlueprint bp)
        {
            if (btn == null || bp == null) return;
            btn.onClick.AddListener(() => Pick(bp, btn));
        }

        void Pick(TurretBlueprint bp, Button btn)
        {
            // Guard: refuse selection if credits are insufficient (button might
            // be re-enabled by other code or this method called externally).
            if (CreditLedger.Live != null && CreditLedger.Live.Credits < bp.credits) return;

            Chosen = bp;
            Repaint(btn);
            PaintLabel();
            ReapplyAffordability();
        }

        public void Clear()
        {
            Chosen = null;
            Repaint(null);
            PaintLabel();
            ReapplyAffordability();
        }

        // Re-runs the dim/enable pass so cards properly reflect current funds
        // after any Repaint that wrote a fresh alpha=1 color.
        void ReapplyAffordability()
        {
            int v = CreditLedger.Live != null ? CreditLedger.Live.Credits : 0;
            CheckAffordability(pulseCard,  pulseBP,  v);
            CheckAffordability(plasmaCard, plasmaBP, v);
            CheckAffordability(cryoCard,   cryoBP,   v);
            CheckAffordability(railCard,   railBP,   v);
        }

        void Repaint(Button active)
        {
            Tint(pulseCard,  active == pulseCard  ? PickHue : PulseHue);
            Tint(plasmaCard, active == plasmaCard ? PickHue : PlasmaHue);
            Tint(cryoCard,   active == cryoCard   ? PickHue : CryoHue);
            Tint(railCard,   active == railCard   ? PickHue : RailHue);
        }

        static void Tint(Button b, Color c)
        {
            if (b == null) return;
            var img = b.GetComponent<Image>();
            if (img) img.color = c;
        }

        void PaintLabel()
        {
            if (chosenLabel == null) return;
            chosenLabel.text = Chosen != null
                ? $"Обрано:\n\n{Chosen.codename}\n✓"
                : "Обери\nвежу\n↓";
        }

        void OnPhase(MatchPhase p)
        {
            bool show = p == MatchPhase.Loadout || p == MatchPhase.Engagement;
            if (rackPanel) rackPanel.SetActive(show);
            if (p != MatchPhase.Loadout) Clear();
        }

        void OnCredits(int v)
        {
            if (creditsLabel) creditsLabel.text = $"Золото: {v}";

            // Clear first so PaintButtons doesn't overwrite the dim alpha
            // we set below in CheckAffordability.
            if (Chosen != null && v < Chosen.credits)
                Clear();

            CheckAffordability(pulseCard,  pulseBP,  v);
            CheckAffordability(plasmaCard, plasmaBP, v);
            CheckAffordability(cryoCard,   cryoBP,   v);
            CheckAffordability(railCard,   railBP,   v);
        }

        static void CheckAffordability(Button b, TurretBlueprint bp, int credits)
        {
            if (b == null || bp == null) return;
            bool can = credits >= bp.credits;
            b.interactable = can;

            // Visually dim unaffordable cards
            var img = b.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                c.a = can ? 1.0f : 0.40f;
                img.color = c;
            }
        }
    }
}
