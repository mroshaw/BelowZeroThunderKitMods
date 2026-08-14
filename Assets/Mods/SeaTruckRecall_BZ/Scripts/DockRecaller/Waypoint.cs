using System;
using UnityEngine;
using UnityEngine.Events;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{

    /// <summary>
    /// Wrapper class for a Waypoint based UnityEvent
    /// </summary>
    [Serializable]
    internal class WaypointChangedEvent : UnityEvent<Waypoint, int, int, float>
    {
    }

    /// <summary>
    /// Internal Waypoint class definition.
    /// </summary>
    [Serializable]
    internal class Waypoint
    {
        // Target transform
        internal Vector3 Position { get; }
        internal Quaternion Rotation { get; }
        // Whether to rotate while moving or before moving
        internal bool RotateBeforeMoving { get; }
        // Whether to slow down as we reach the waypoint
        internal bool SlowDownToTarget { get; }
        internal bool MonitorObstacles { get; }
        internal bool UseSpeedModifier { get; }
        internal bool UseFixedRotation { get; }
        internal bool AllowSkip { get; }
        // Waypoint name for useful feedback
        internal string Name { get; }

        /// <summary>
        /// Default constructor
        /// </summary>
        internal Waypoint(Vector3 position, Quaternion rotation, bool rotateBeforeMoving, bool slowDownToTarget,
            string name, bool monitorObstacles = true, bool useSpeedModifier = true,
            bool useFixedRotation = false, bool allowSkip = true)
        {
            Position = position;
            Rotation = rotation;
            RotateBeforeMoving = rotateBeforeMoving;
            SlowDownToTarget = slowDownToTarget;
            Name = name;
            MonitorObstacles = monitorObstacles;
            UseSpeedModifier = useSpeedModifier;
            UseFixedRotation = useFixedRotation;
            AllowSkip = allowSkip;
        }

        public override string ToString()
        {
            return $"{Name}:, Pos: {Position}";
        }
    }
}
