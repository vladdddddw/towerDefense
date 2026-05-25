using UnityEngine;

namespace Vanguard.TD.Map
{
    /// <summary>
    /// A single grid cell. Stores walkability, occupancy, and grid coordinates.
    /// </summary>
    public sealed class BoardCell : MonoBehaviour
    {
        public Vector2Int Coord { get; set; }
        public bool       OnRoute { get; set; }
        public bool       Occupied { get; set; }

        public bool Buildable => !OnRoute && !Occupied;
    }
}
