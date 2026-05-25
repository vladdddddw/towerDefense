using UnityEngine;

namespace Vanguard.TD.Gameplay
{
    /// <summary>
    /// Drives the hostile along a poly-line route. Handles chill (slow) effect.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Hostile))]
    public sealed class HostileLocomotion : MonoBehaviour
    {
        Vector3[] _route;
        int       _legIndex;
        float     _basePace;
        float     _currentPace;
        float     _chillUntil;

        public float Progress  { get; private set; }
        public bool  RouteDone { get; private set; }

        public void Spin(Vector3[] route, float pace)
        {
            _route       = route;
            _legIndex    = 1;
            _basePace    = pace;
            _currentPace = pace;
            _chillUntil  = 0f;
            Progress     = 0f;
            RouteDone    = false;
            if (route != null && route.Length > 0)
                transform.position = route[0];
        }

        public void Chill(float factor, float seconds)
        {
            // factor 0..1 = slow strength; e.g. 0.5 means 50% reduction
            _currentPace = _basePace * Mathf.Max(0.1f, 1f - factor);
            _chillUntil  = Time.time + seconds;
        }

        void Update()
        {
            if (RouteDone || _route == null || _legIndex >= _route.Length) return;

            if (Time.time > _chillUntil && _currentPace < _basePace)
                _currentPace = _basePace;

            var target = _route[_legIndex];
            transform.position = Vector3.MoveTowards(
                transform.position, target, _currentPace * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude < 0.0025f)
            {
                _legIndex++;
                Progress = _legIndex - 1;
                if (_legIndex >= _route.Length)
                {
                    RouteDone = true;
                    OnReachReactor();
                }
            }
        }

        void OnReachReactor()
        {
            var host = GetComponent<Hostile>();
            if (host == null) return;
            Map.Reactor.Live?.Damage(host.Blueprint != null ? host.Blueprint.reactorDamage : 1);
            Waves.SwarmDirector.Live?.NotifyEscaped();
            host.Retire();
        }
    }
}
