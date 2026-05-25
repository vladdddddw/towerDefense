using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Vanguard.TD.Gameplay
{
    /// <summary>
    /// One pool per bolt prefab. Created lazily on first request.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoltPool : MonoBehaviour
    {
        public static BoltPool Live { get; private set; }

        readonly Dictionary<GameObject, ObjectPool<Bolt>> _pools = new();

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        ObjectPool<Bolt> Ensure(GameObject prefab)
        {
            if (prefab == null) return null;
            if (_pools.TryGetValue(prefab, out var p)) return p;
            p = new ObjectPool<Bolt>(
                createFunc:      () => Instantiate(prefab).GetComponent<Bolt>(),
                actionOnGet:     b => b.gameObject.SetActive(true),
                actionOnRelease: b => b.gameObject.SetActive(false),
                actionOnDestroy: b => Destroy(b.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize:         80
            );
            _pools[prefab] = p;
            return p;
        }

        public Bolt Take(GameObject prefab)
        {
            var pool = Ensure(prefab);
            return pool != null ? pool.Get() : null;
        }

        public void Return(GameObject prefab, Bolt bolt)
        {
            if (prefab == null || bolt == null) { if (bolt != null) Destroy(bolt.gameObject); return; }
            if (_pools.TryGetValue(prefab, out var pool)) pool.Release(bolt);
            else Destroy(bolt.gameObject);
        }
    }
}
