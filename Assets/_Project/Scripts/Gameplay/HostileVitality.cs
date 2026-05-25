using UnityEngine;
using UnityEngine.UI;
using Vanguard.TD.Core;
using Vanguard.TD.Economy;
using Vanguard.TD.Waves;

namespace Vanguard.TD.Gameplay
{
    /// <summary>
    /// Integrity bar + death handling. Bar shrinks via RectTransform.anchorMax
    /// (more reliable in World-Space than UI.Slider's fillRect mechanism).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Hostile))]
    public sealed class HostileVitality : MonoBehaviour
    {
        [SerializeField] Image fillImg;   // assigned by HostileFactory on prefab build

        Hostile _host;
        float   _max;
        float   _now;

        public bool Down { get; private set; }

        void Awake()
        {
            _host = GetComponent<Hostile>();
            if (fillImg == null)
            {
                var t = transform.Find("IntegrityCanvas/Bar");
                if (t != null) fillImg = t.GetComponent<Image>();
            }
        }

        public void Spin(float maxIntegrity)
        {
            _max  = maxIntegrity;
            _now  = maxIntegrity;
            Down  = false;
            Paint(1f);
        }

        public void Damage(float amount)
        {
            if (Down) return;
            _now = Mathf.Max(0f, _now - amount);
            Paint(_max > 0f ? _now / _max : 0f);
            if (_now <= 0f) Expire();
        }

        void Paint(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (fillImg == null) return;

            // Visual width = anchorMax.x
            var rt = fillImg.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(ratio, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Color stages: cyan (full) → amber → red
            fillImg.color =
                ratio > 0.6f ? new Color(0.10f, 0.92f, 0.95f) :  // cyan
                ratio > 0.3f ? new Color(0.99f, 0.75f, 0.10f) :  // amber
                               new Color(0.95f, 0.18f, 0.15f);   // red
        }

        void Expire()
        {
            if (Down) return;
            Down = true;
            if (_host != null && _host.Blueprint != null)
                CreditLedger.Live?.Credit(_host.Blueprint.payout);
            SwarmDirector.Live?.NotifyDowned();
            SfxRack.Scrap();
            _host?.Retire();
        }
    }
}
