using System.Collections.Generic;

namespace DaftAppleGames.SeaTruckSpeedMod_BZ
{
    internal static class SeaTruckHistory
    {
        private static readonly List<SeaTruckHistoryItem> SeaTruckInstanceHistory;

        static SeaTruckHistory()
        {
            SeaTruckInstanceHistory = new List<SeaTruckHistoryItem>();
        }

        /// <summary>
        /// Apply the given drag modifier to all SeaTruck instances
        /// </summary>
        internal static void UpdateAllDrag(float multiplier)
        {
            for (int index = SeaTruckInstanceHistory.Count - 1; index >= 0; index--)
            {
                SeaTruckHistoryItem historyItem = SeaTruckInstanceHistory[index];
                if (!historyItem.SeaTruckInstance)
                {
                    SeaTruckInstanceHistory.RemoveAt(index);
                    continue;
                }

                historyItem.ApplyDragdModifier(multiplier);
            }
        }

        /// <summary>
        /// Add a new SeaTruck
        /// </summary>
        internal static void AddSeaTruck(SeaTruckMotor seaTruck)
        {
            SeaTruckHistoryItem newSeatruckItem = new SeaTruckHistoryItem(seaTruck);
            SeaTruckInstanceHistory.Add(newSeatruckItem);
        }

        /// <summary>
        /// Remove a SeaTruck
        /// </summary>
        internal static void RemoveSeaTruck(SeaTruckMotor seaTruck)
        {
            for (int index = SeaTruckInstanceHistory.Count - 1; index >= 0; index--)
            {
                SeaTruckHistoryItem historyItem = SeaTruckInstanceHistory[index];
                if (ReferenceEquals(historyItem.SeaTruckInstance, seaTruck))
                {
                    SeaTruckInstanceHistory.RemoveAt(index);
                }
            }
        }
    }
}
