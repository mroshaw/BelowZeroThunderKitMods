using System.Collections.Generic;
using UnityEngine;

namespace DaftAppleGames.SeatruckRecall_BZ.Navigation
{
    internal class NavPath : List<NavCell>
    {
        /// <summary>
        /// Generate a list of Waypoints from the NavCells
        /// </summary>
        /// <returns></returns>
        internal List<Waypoint> GetWayPointsFromNavPath()
        {
            List<Waypoint> waypoints = new List<Waypoint>();

            foreach (NavCell currCell in this)
            {
                Waypoint newWaypoint = new Waypoint(currCell.Position, Quaternion.identity, false, $"{currCell.Name}");
                waypoints.Add(newWaypoint);
            }

            return waypoints;
        }
        
        /// <summary>
        /// Combines two lists of waypoints
        /// </summary>
        /// <returns></returns>
        internal static List<Waypoint> CombineWaypoints(List<Waypoint> waypoints1, List<Waypoint> waypoints2)
        {
            List<Waypoint> combinedWaypoints = new List<Waypoint>();
            combinedWaypoints.AddRange(waypoints1);
            combinedWaypoints.AddRange(waypoints2);
            combinedWaypoints.Reverse();
            return combinedWaypoints;
        }

        /// <summary>
        /// Get combined Waypoints for use by the Recaller
        /// </summary>
        internal List<Waypoint> GetFinalWaypoints(List<Waypoint> dockingWaypoints)
        {
            List<Waypoint> finalWaypoints = new List<Waypoint>();
            
            List<Waypoint> navWaypoints = GetWayPointsFromNavPath();
            finalWaypoints.AddRange(navWaypoints);
            finalWaypoints.AddRange(dockingWaypoints);
            return finalWaypoints;
        }
    }
}