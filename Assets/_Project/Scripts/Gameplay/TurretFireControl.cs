using UnityEngine;
using Vanguard.TD.Core;
using Vanguard.TD.Data;

namespace Vanguard.TD.Gameplay
{
    /// <summary>
    /// Target acquisition + cooldown logic. Picks the hostile with the highest
    /// route progress (i.e. closest to the reactor) within reach.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurretFireControl : MonoBehaviour
    {
        TurretBlueprint _bp;
        float _readyAt;
        int   _hostileLayer = -1;

        // Reusable buffer to avoid per-frame allocations
        readonly Collider2D[] _scratch = new Collider2D[24];

        public void Spin(TurretBlueprint bp)
        {
            _bp = bp;
            _readyAt = 0f;
            _hostileLayer = LayerMask.NameToLayer("Enemy");
        }

        void Update()
        {
            if (_bp == null) return;
            if (GameFlow.Live == null || GameFlow.Live.Phase != MatchPhase.Engagement) return;
            if (Time.time < _readyAt) return;

            var target = Acquire();
            if (target == null) return;

            Discharge(target);
            _readyAt = Time.time + 1f / Mathf.Max(0.01f, _bp.rateOfFire);
        }

        Hostile Acquire()
        {
            int mask = _hostileLayer >= 0 ? (1 << _hostileLayer) : ~0;
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, _bp.reach, _scratch, mask);

            Hostile best = null;
            float bestProgress = -1f;
            for (int i = 0; i < count; i++)
            {
                var h = _scratch[i].GetComponent<Hostile>();
                if (h == null || h.Down) continue;
                if (h.Progress > bestProgress) { best = h; bestProgress = h.Progress; }
            }
            return best;
        }

        void Discharge(Hostile target)
        {
            if (_bp.boltPrefab == null) return;
            var bolt = BoltPool.Live?.Take(_bp.boltPrefab);
            if (bolt == null) return;

            bolt.transform.position = transform.position;
            bolt.Spin(target, _bp);
            SfxRack.Fire();
        }

        void OnDrawGizmosSelected()
        {
            if (_bp == null) return;
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, _bp.reach);
        }
    }
}
