namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Identifies an item stored in a dynamically generated base aquarium.
    /// </summary>
    public class BaseAquariumStoredItem
    {
        public string ClassId;

        /// <summary>
        /// Creates an empty item record for JSON deserialization.
        /// </summary>
        public BaseAquariumStoredItem()
        {
        }

        /// <summary>
        /// Creates an item record from a prefab class identifier.
        /// </summary>
        internal BaseAquariumStoredItem(string classId)
        {
            ClassId = classId;
        }
    }
}
