using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vanguard.TD.Core;
using Vanguard.TD.Data;
using Vanguard.TD.Economy;
using Vanguard.TD.Gameplay;
using Vanguard.TD.Map;

namespace Vanguard.TD.Waves
{
    /// <summary>
    /// Spawns hostiles from a queue at randomized intervals. Tracks how many
    /// units are still in play and signals round completion.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SwarmDirector : MonoBehaviour
    {
        public static SwarmDirector Live { get; private set; }

        [SerializeField] float intervalMin = 0.8f;
        [SerializeField] float intervalMax = 1.2f;
        [SerializeField] int   capacity    = 50;

        Queue<HostileBlueprint> _queue;
        Vector3[] _route;
        int _alive;
        Coroutine _loop;

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        public void Launch(Queue<HostileBlueprint> queue, Transform[] waypoints)
        {
            _queue = queue ?? new Queue<HostileBlueprint>();
            _route = ConvertRoute(waypoints);
            _alive = 0;
            if (_loop != null) StopCoroutine(_loop);
            _loop = StartCoroutine(SpawnLoop());
        }

        IEnumerator SpawnLoop()
        {
            while (_queue.Count > 0)
            {
                if (_alive >= capacity) { yield return null; continue; }
                var bp = _queue.Dequeue();
                if (HostilePool.Live != null)
                {
                    HostilePool.Live.Take(bp, _route);
                    _alive++;
                }
                yield return new WaitForSeconds(Random.Range(intervalMin, intervalMax));
            }

            // All queued spawned; now wait until all are gone
            while (_alive > 0) yield return null;
            GameDirector.Live?.RoundFinished();
            _loop = null;
        }

        public void NotifyDowned()  { _alive = Mathf.Max(0, _alive - 1); }
        public void NotifyEscaped() { _alive = Mathf.Max(0, _alive - 1); }

        public void Halt()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
            _queue = null;
            _alive = 0;
        }

        static Vector3[] ConvertRoute(Transform[] tr)
        {
            if (tr == null) return new Vector3[0];
            var arr = new Vector3[tr.Length];
            for (int i = 0; i < tr.Length; i++) arr[i] = tr[i].position;
            return arr;
        }
    }
}
