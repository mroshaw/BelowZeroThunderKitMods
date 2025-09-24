using System.Collections.Generic;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;
using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Navigation
{
    /// <summary>
    /// Simple static class to track a list of active AutoPilots
    /// </summary>
    internal static class AllSeaTruckAutoPilots
    {
        private static readonly List<SeaTruckAutoPilot> AllAutoPilotsList;
        private static int Count => AllAutoPilotsList.Count;

        static AllSeaTruckAutoPilots()
        {
            AllAutoPilotsList = new List<SeaTruckAutoPilot>();
        }

        /// <summary>
        /// Add a new autopilot
        /// </summary>
        internal static void AddInstance(SeaTruckAutoPilot autoPilot)
        {
            if (AllAutoPilotsList.Contains(autoPilot))
            {
                return;
            }
            AllAutoPilotsList.Add(autoPilot);
            ModDebugLog.LogDebug($"AutoPilot: Registered new instance: {autoPilot.gameObject.name}");
        }

        /// <summary>
        /// Remove autopilot
        /// </summary>
        internal static void RemoveInstance(SeaTruckAutoPilot autoPilot)
        {
            if (!AllAutoPilotsList.Contains(autoPilot))
            {
                return;
            }
            
            AllAutoPilotsList.Remove(autoPilot);
            ModDebugLog.LogDebug($"DockRecaller: Removed instance: {autoPilot.gameObject.name}");
        }

        /// <summary>
        /// Be be used to get an initial list of active SeaTrucks. Try not to use, as FindObjectsOfType is pretty
        /// inefficient
        /// </summary>
        public static void GetAllActiveAutoPilots()
        {
            AllAutoPilotsList.Clear();
            foreach (SeaTruckAutoPilot autoPilot in Object.FindObjectsOfType<SeaTruckAutoPilot>())
            {
                AddInstance(autoPilot);
            }
        }

        /// <summary>
        /// Given an origin, determine the closest SeaTruck with a "Ready" state autopilot
        /// </summary>
        internal static SeaTruckAutoPilot GetClosestAutoPilot(Vector3 sourcePosition, float maxDistance)
        {
            float closestDistance = Mathf.Infinity;
            SeaTruckAutoPilot closestSeaTruck = null;

            ModDebugLog.LogDebug($"Looking for closest SeaTruckAutoPilot out of {Count} registered SeaTrucks...");

            if (Count == 0)
            {
                ModDebugLog.LogDebug("No SeaTrucks registered.");
                return null;
            }

            // Loop through each SeaTruck, find out which is closest
            foreach (SeaTruckAutoPilot autoPilot in AllAutoPilotsList)
            {
#if !UNITY_EDITOR
                // Check if already docked
                SeaTruckSegment segment = autoPilot.GetComponent<SeaTruckSegment>();
                if (segment.isDocked || segment.IsDocking() || !autoPilot.IsAvailable())
                {
                    ModDebugLog.LogDebug($"SeaTruck {autoPilot.gameObject.name} is already docking or docked. Skipping...");
                    continue;
                }
#endif
                ModDebugLog.LogDebug($"Checking distance to: {autoPilot.gameObject.name}...");
                float currDistance = Vector3.Distance(sourcePosition, autoPilot.gameObject.transform.position);
                {
                    ModDebugLog.LogDebug($"Distance is: {currDistance}, closest so far is: {closestDistance}");
                    if ((closestDistance == 0 || currDistance < closestDistance) && currDistance <= maxDistance)
                    {
                        ModDebugLog.LogDebug("New closest SeaTruck found!");
                        closestDistance = currDistance;
                        closestSeaTruck = autoPilot;
                    }
                }
            }

            // Check to see if we've found anything in range
            ModDebugLog.LogDebug(closestSeaTruck == null ? $"No SeaTrucks found within range!" : $"Closest SeaTruck found: {closestSeaTruck.gameObject.name} at {closestDistance}");

            return closestSeaTruck;
        }
    }
}