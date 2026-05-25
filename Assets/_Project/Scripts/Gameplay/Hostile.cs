using UnityEngine;
using Vanguard.TD.Data;

namespace Vanguard.TD.Gameplay
{
    /// <summary>
    /// Top-level hostile facade. Holds the blueprint reference and forwards
    /// commands to <see cref="HostileLocomotion"/> / <see cref="HostileVitality"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HostileLocomotion))]
    [RequireComponent(typeof(HostileVitality))]
    public sealed class Hostile : MonoBehaviour
    {
        public HostileBlueprint Blueprint { get; private set; }

        HostileLocomotion _move;
        HostileVitality   _vital;
        bool _retired;

        public float Progress => _move != null ? _move.Progress : 0f;
        public bool  Down     => _vital != null && _vital.Down;

        void Awake()
        {
            _move  = GetComponent<HostileLocomotion>();
            _vital = GetComponent<HostileVitality>();
        }

        public void Spin(HostileBlueprint bp, Vector3[] route)
        {
            Blueprint = bp;
            _retired  = false;
            _move .Spin(route, bp.pace);
            _vital.Spin(bp.maxIntegrity);
        }

        public void Damage(float amount) => _vital?.Damage(amount);
        public void Chill(float factor, float seconds) => _move?.Chill(factor, seconds);

        /// <summary>Single-entry retirement — guards against double pool-return.</summary>
        public void Retire()
        {
            if (_retired) return;
            _retired = true;
            HostilePool.Live?.Return(this);
        }
    }
}
