using System.Collections.Generic;
using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Navigation
{
    internal class TestNavMovement : MonoBehaviour
    {
        [SerializeField] private Transform[] waypointTransforms;
        [SerializeField] private SeaTruckNavMovement navMovement;
        [SerializeField] private SeaTruckAutoPilot autoPilot;
        private List<Waypoint> _navWaypoints;

        private void Start()
        {
            CreateWayPoints();
            autoPilot.StartNavigation(_navWaypoints);
        }
        
        private void CreateWayPoints()
        {
            _navWaypoints = new List<Waypoint>();
            for (int currWaypoint = 0; currWaypoint < waypointTransforms.Length; currWaypoint++)
            {
                Waypoint waypoint;
                if (currWaypoint == 3)
                {
                    waypoint = new Waypoint(waypointTransforms[currWaypoint].position, waypointTransforms[currWaypoint].rotation, true, true, $"Waypoint: {currWaypoint}");

                }
                else
                {
                    waypoint= new Waypoint(waypointTransforms[currWaypoint].position, waypointTransforms[currWaypoint].rotation, false, false, $"Waypoint: {currWaypoint}");

                }
                _navWaypoints.Add(waypoint);
            }
        }
    }
}