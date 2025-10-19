using System.Collections.Generic;
using System.Linq;

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
            foreach (SeaTruckHistoryItem historyItem in SeaTruckInstanceHistory)
            {
                historyItem.ApplyDragdModifier(multiplier);
            }
        }

        /// <summary>
        /// Apply the given power efficiency modifier to all SeaTruck instances
        /// </summary>
        internal static void UpdateAllPowerEfficiency(float multiplier)
        {
            foreach (SeaTruckHistoryItem historyItem in SeaTruckInstanceHistory)
            {
                historyItem.ApplyPowerModifier(multiplier);
            }
        }


        /// <summary>
        /// Add a new SeaTruck
        /// </summary>
        internal static void AddSeaTruck(SeaTruckMotor SeaTruck)
        {
            SeaTruckHistoryItem newSeaglideItem = new SeaTruckHistoryItem(SeaTruck);
            SeaTruckInstanceHistory.Add(newSeaglideItem);
        }

        /// <summary>
        /// Remove a SeaTruck
        /// </summary>
        internal static void RemoveSeaTruck(SeaTruckMotor SeaTruck)
        {
            foreach (SeaTruckHistoryItem SeaTruckHistoryItem in SeaTruckInstanceHistory.ToList())
            {
                if (SeaTruckHistoryItem.SeaTruckInstance == SeaTruck)
                {
                    SeaTruckInstanceHistory.Remove(SeaTruckHistoryItem);
                }
            }
        }
    }
}