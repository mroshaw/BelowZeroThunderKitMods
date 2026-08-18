using System.Collections.Generic;
using UnityEngine;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Stores the world position of an aquarium base cell.
    /// </summary>
    public class BaseAquariumLocation
    {
        public float X;
        public float Y;
        public float Z;
        public List<BaseAquariumStoredItem> StoredItems =
            new List<BaseAquariumStoredItem>();

        /// <summary>
        /// Creates an empty location for JSON deserialization.
        /// </summary>
        public BaseAquariumLocation()
        {
        }

        /// <summary>
        /// Creates a persisted location from a world position.
        /// </summary>
        internal BaseAquariumLocation(Vector3 position)
        {
            X = position.x;
            Y = position.y;
            Z = position.z;
        }

        /// <summary>
        /// Returns this persisted location as a world position.
        /// </summary>
        internal Vector3 ToVector3() => new Vector3(X, Y, Z);
    }
}
