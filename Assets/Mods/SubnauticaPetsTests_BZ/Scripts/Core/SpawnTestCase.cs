using UnityEngine;

namespace DaftAppleGames.SubnauticaPetsTests
{
    internal sealed class SpawnTestCase
    {
        internal readonly string Name;
        internal readonly string ClassId;
        internal readonly Vector3 ExpectedPosition;
        internal readonly float HorizontalTolerance;
        internal readonly float MaximumDownwardDistance;
        internal readonly float MaximumUpwardDistance;
        internal readonly string[] RequiredComponentNames;

        internal SpawnTestCase(string name, string classId, Vector3 expectedPosition, float horizontalTolerance,
            float maximumDownwardDistance, float maximumUpwardDistance, params string[] requiredComponentNames)
        {
            Name = name;
            ClassId = classId;
            ExpectedPosition = expectedPosition;
            HorizontalTolerance = horizontalTolerance;
            MaximumDownwardDistance = maximumDownwardDistance;
            MaximumUpwardDistance = maximumUpwardDistance;
            RequiredComponentNames = requiredComponentNames;
        }
    }
}
