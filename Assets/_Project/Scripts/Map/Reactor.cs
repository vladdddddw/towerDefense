using System;
using UnityEngine;
using Vanguard.TD.Core;

namespace Vanguard.TD.Map
{
    /// <summary>
    /// The objective the defender protects. Loses integrity each time a hostile
    /// reaches its position. Raises events for HUD + game over flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Reactor : MonoBehaviour
    {
        public static Reactor Live { get; private set; }

        [SerializeField] int peakIntegrity = 20;
        public int Integrity { get; private set; }
        public int Peak => peakIntegrity;

        public event Action<int> IntegrityChanged;
        public event Action      Collapsed;

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
            Integrity = peakIntegrity;
            IntegrityChanged?.Invoke(Integrity);
        }

        public void Damage(int amount)
        {
            if (Integrity <= 0) return;
            Integrity = Mathf.Max(0, Integrity - amount);
            IntegrityChanged?.Invoke(Integrity);
            SfxRack.Hit();
            if (Integrity <= 0) Collapsed?.Invoke();
        }
    }
}
