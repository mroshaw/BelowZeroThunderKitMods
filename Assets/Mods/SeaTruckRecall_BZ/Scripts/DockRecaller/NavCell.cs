using System;
using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// Represents a "Cell" in the 3 dimensional nav grid
    /// </summary>
    [Serializable]
    internal struct NavCell : IEquatable<NavCell>
    {
        internal Vector3Int Index;
        internal Vector3 Position;
        internal bool IsTraversable;
        internal string Name;

        public bool Equals(NavCell other)
        {
            return Index.Equals(other.Index);
        }

        public override bool Equals(object obj)
        {
            return obj is NavCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }
    }
}
