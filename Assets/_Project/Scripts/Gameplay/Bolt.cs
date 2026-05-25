using UnityEngine;
using Vanguard.TD.Core;
using Vanguard.TD.Data;

namespace Vanguard.TD.Gameplay
{
    /// <summary>
    /// Projectile that homes onto its target until impact. Returns to the pool
    /// after impact or if target disappears.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Bolt : MonoBehaviour
    {
        [SerializeField] float velocity = 8f;

        Hostile         _target;
        TurretBlueprint _bp;
        GameObject      _source;
        bool            _spent;

        // Reusable buffer for AoE queries
        static readonly Collider2D[] _splashBuf = new Collider2D[32];

        public void Spin(Hostile target, TurretBlueprint bp)
        {
            _target = target;
            _bp     = bp;
            _source = bp != null ? bp.boltPrefab : null;
            _spent  = false;
        }

        void Update()
        {
            if (_spent) return;
            if (_target == null || _target.Down || !_target.gameObject.activeSelf)
            {
                Recycle();
                return;
            }

            var to = _target.transform.position - transform.position;
            transform.position += to.normalized * velocity * Time.deltaTime;

            // Face the target
            float ang = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);

            if (to.sqrMagnitude < 0.0225f) Impact();
        }

        void Impact()
        {
            if (_spent || _bp == null) return;
            _spent = true;

            switch (_bp.profile)
            {
                case DamageProfile.Direct:
                    _target.Damage(_bp.damage);
                    SfxRack.Strike();
                    break;

                case DamageProfile.Splash:
                {
                    int mask = LayerMask.GetMask("Enemy");
                    int count = Physics2D.OverlapCircleNonAlloc(
                        transform.position, _bp.splashRadius, _splashBuf, mask);
                    for (int i = 0; i < count; i++)
                    {
                        var h = _splashBuf[i].GetComponent<Hostile>();
                        if (h != null && !h.Down) h.Damage(_bp.damage);
                    }
                    SfxRack.Bloom();
                    break;
                }

                case DamageProfile.Chill:
                    if (!_target.Blueprint.chillImmune)
                        _target.Chill(_bp.chillFactor, _bp.chillSeconds);
                    _target.Damage(_bp.damage * 0.5f);
                    SfxRack.Frost();
                    break;
            }

            Recycle();
        }

        void Recycle()
        {
            _spent = true;
            BoltPool.Live?.Return(_source, this);
        }
    }
}
