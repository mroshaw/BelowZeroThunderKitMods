using System.Collections.Generic;
using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Navigation
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

            int numWaypoint = Count;
            
            for(int currIndex = 0; currIndex < numWaypoint; currIndex++ )
            {
                Waypoint newWaypoint = new Waypoint(this[currIndex].Position, Quaternion.identity, false, (currIndex > numWaypoint -3),$"{this[currIndex].Name}");
                waypoints.Add(newWaypoint);
            }

            return waypoints;
        }
    }
}