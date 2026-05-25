using UnityEngine;
using Vanguard.TD.Core;

namespace Vanguard.TD.Data
{
    /// <summary>
    /// Pure data describing a turret class: stats, visuals, runtime prefab links.
    /// Multiple turret instances share one blueprint.
    /// </summary>
    [CreateAssetMenu(menuName = "Vanguard/Turret Blueprint", fileName = "TurretBlueprint")]
    public sealed class TurretBlueprint : ScriptableObject
    {
        [Header("Identity")]
        public string codename = "";

        [Header("Economy")]
        public int credits = 999999;  // huge default — if it ever shows, the SO clearly isn't initialised

        [Header("Combat")]
        public float rateOfFire = 1.0f;     // shots per second
        public float reach      = 3.0f;     // detection radius
        public float damage     = 20f;

        public DamageProfile profile = DamageProfile.Direct;
        public float splashRadius = 0f;     // for Splash profile
        public float chillFactor  = 0f;     // 0..1 — slow strength
        public float chillSeconds = 0f;     // slow duration

        [Header("Runtime prefabs (created at boot)")]
        public GameObject turretPrefab;
        public GameObject boltPrefab;
    }
}
