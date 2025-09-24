using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    public static class AllSeaTruckDockRecallers
    {
        private static readonly List<SeaTruckDockRecaller> AllDockRecallersList;
        private static int Count => AllDockRecallersList.Count;

        static AllSeaTruckDockRecallers()
        {
            AllDockRecallersList = new List<SeaTruckDockRecaller>();
        }

        /// <summary>
        /// Add a new recaller
        /// </summary>
        internal static void AddInstance(SeaTruckDockRecaller dockRecaller)
        {
            ModDebugLog.LogDebug($"DockRecaller: Registered new instance: {dockRecaller.gameObject.name}.");
            AllDockRecallersList.Add(dockRecaller);
        }

        /// <summary>
        /// Remove recaller
        /// </summary>
        internal static void RemoveInstance(SeaTruckDockRecaller dockRecaller)
        {
            AllDockRecallersList.Remove(dockRecaller);
            ModDebugLog.LogDebug($"DockRecaller: Removed instance: {dockRecaller.gameObject.name}");
        }

        /// <summary>
        /// Update all Dock settings (Range)
        /// </summary>
        internal static void RegenerateAllNavGrids()
        {
            ModDebugLog.LogDebug($"DockRecaller: Regenerating NavGrids for all {AllDockRecallersList.Count} DockRecallers");
            foreach (SeaTruckDockRecaller dockRecaller in AllDockRecallersList)
            {
                dockRecaller.GenerateNavGrid();
            }
        }
    }
}