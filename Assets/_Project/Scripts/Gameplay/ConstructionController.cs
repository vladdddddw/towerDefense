using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Vanguard.TD.Core;
using Vanguard.TD.Data;
using Vanguard.TD.Economy;
using Vanguard.TD.Map;

namespace Vanguard.TD.Gameplay
{
    /// <summary>
    /// Handles mouse input for: turret placement, range display, deselection.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConstructionController : MonoBehaviour
    {
        public static ConstructionController Live { get; private set; }

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
            Physics2D.queriesHitTriggers = true; // safety
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Right-click: cancel selection + hide range
            if (mouse.rightButton.wasPressedThisFrame)
            {
                UI.TurretRack.Live?.Clear();
                RangeIndicator.Live?.Conceal();
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame) return;

            // Resolve world position from screen
            Vector2 screen = mouse.position.ReadValue();
            float depth = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            world.z = 0f;

            // Click-on-turret check FIRST — even if hovering UI we still want this
            var hit = ProbeTurret(world);
            if (hit != null)
            {
                RangeIndicator.Live?.Display(hit.transform.position, hit.Blueprint.reach);
                return;
            }

            RangeIndicator.Live?.Conceal();

            // From here on, the click is for placement — UI must NOT be in the way
            if (HoverUI()) return;

            var phase = GameFlow.Live != null ? GameFlow.Live.Phase : MatchPhase.Title;
            if (phase != MatchPhase.Loadout && phase != MatchPhase.Engagement) return;

            var coord = BoardLayout.Live.ToCoord(world);
            if (!BoardLayout.Live.CanBuildAt(coord)) return;

            var bp = UI.TurretRack.Live?.Chosen;
            if (bp == null) return;

            Construct(bp, coord);
        }

        public void Construct(TurretBlueprint bp, Vector2Int coord)
        {
            if (bp == null || bp.turretPrefab == null) return;
            if (!BoardLayout.Live.CanBuildAt(coord)) return;

            // Hard gate: refuse if the defender literally can't afford it.
            // Both the explicit credit check AND the TryDebit guard protect
            // against accidental double-spends or race conditions.
            if (CreditLedger.Live == null) return;
            if (CreditLedger.Live.Credits < bp.credits) return;
            if (!CreditLedger.Live.TryDebit(bp.credits)) return;

            var pos = BoardLayout.Live.ToWorld(coord);
            var go  = Instantiate(bp.turretPrefab, pos, Quaternion.identity);
            go.SetActive(true);

            if (go.GetComponent<Collider2D>() == null)
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.size = Vector2.one * 0.85f;
                col.isTrigger = true;
            }

            var turret = go.GetComponent<Turret>() ?? go.AddComponent<Turret>();
            turret.Spin(bp, coord);
            BoardLayout.Live.MarkOccupied(coord, true);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        static Turret ProbeTurret(Vector3 world)
        {
            Turret best = null;
            float bestDist = 0.7f;
            foreach (var t in Object.FindObjectsByType<Turret>(FindObjectsSortMode.None))
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                float d = Vector2.Distance(t.transform.position, world);
                if (d < bestDist) { best = t; bestDist = d; }
            }
            return best;
        }

        static bool HoverUI()
        {
            var es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }
    }
}
