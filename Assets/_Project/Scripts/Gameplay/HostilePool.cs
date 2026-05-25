using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Vanguard.TD.Data;

namespace Vanguard.TD.Gameplay
{
    /// <summary>
    /// One <see cref="ObjectPool{T}"/> per hostile blueprint. Pre-warms on Awake to
    /// avoid first-spawn allocation hitch.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HostilePool : MonoBehaviour
    {
        public static HostilePool Live { get; private set; }

        [System.Serializable]
        public struct Entry
        {
            public HostileBlueprint blueprint;
            public int preWarm;
        }

        [SerializeField] List<Entry> roster = new();

        readonly Dictionary<HostileBlueprint, ObjectPool<Hostile>> _pools = new();
        readonly HashSet<Hostile> _live = new();

        public int LiveCount => _live.Count;

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        public void Boot()
        {
            foreach (var entry in roster)
                EnsurePool(entry.blueprint, entry.preWarm);
        }

        ObjectPool<Hostile> EnsurePool(HostileBlueprint bp, int warm)
        {
            if (bp == null || bp.prefab == null) return null;
            if (_pools.TryGetValue(bp, out var existing)) return existing;

            var pool = new ObjectPool<Hostile>(
                createFunc:      () => Instantiate(bp.prefab).GetComponent<Hostile>(),
                actionOnGet:     h  => h.gameObject.SetActive(true),
                actionOnRelease: h  => h.gameObject.SetActive(false),
                actionOnDestroy: h  => Destroy(h.gameObject),
                collectionCheck: false,
                defaultCapacity: Mathf.Max(4, warm),
                maxSize:         60
            );
            _pools[bp] = pool;

            // Pre-warm: instantiate then immediately release back
            var stash = new List<Hostile>(warm);
            for (int i = 0; i < warm; i++) stash.Add(pool.Get());
            foreach (var h in stash) pool.Release(h);
            return pool;
        }

        public Hostile Take(HostileBlueprint bp, Vector3[] route)
        {
            var pool = EnsurePool(bp, 6);
            if (pool == null) return null;
            var inst = pool.Get();
            inst.transform.position = route != null && route.Length > 0 ? route[0] : Vector3.zero;
            inst.Spin(bp, route);
            _live.Add(inst);
            return inst;
        }

        public void Return(Hostile h)
        {
            if (h == null || h.Blueprint == null) return;
            _live.Remove(h);
            if (_pools.TryGetValue(h.Blueprint, out var pool))
                pool.Release(h);
            else
                Destroy(h.gameObject);
        }

        public void Sweep()
        {
            // Recall everything currently active — used on round end / restart.
            var snapshot = new List<Hostile>(_live);
            foreach (var h in snapshot) h.Retire();
            _live.Clear();
        }
    }
}
