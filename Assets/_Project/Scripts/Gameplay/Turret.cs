using UnityEngine;
using Vanguard.TD.Data;

namespace Vanguard.TD.Gameplay
{
    /// <summary>
    /// Lightweight turret host. Stores blueprint + grid coordinates; forwards
    /// initialization to <see cref="TurretFireControl"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Turret : MonoBehaviour
    {
        public TurretBlueprint Blueprint { get; private set; }
        public Vector2Int      Slot      { get; private set; }

        public void Spin(TurretBlueprint bp, Vector2Int slot)
        {
            Blueprint = bp;
            Slot      = slot;
            var fc = GetComponent<TurretFireControl>();
            if (fc != null) fc.Spin(bp);
        }

        void OnDestroy()
        {
            Map.BoardLayout.Live?.MarkVacated(Slot);
        }
    }
}
