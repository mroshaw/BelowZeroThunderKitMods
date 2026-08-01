using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Nautilus.Commands;
using Nautilus.Handlers;
using UnityEngine;
using Object = UnityEngine.Object;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Development console tools for inspecting loaded Pet DNA loot spawns.
    /// </summary>
    internal static class PetDnaDebugCommand
    {
        private const float TeleportHeightOffset = 2.5f;
        private static readonly List<SpawnRecord> SpawnRecords = new List<SpawnRecord>();

        internal static void Register()
        {
            ConsoleCommandsHandler.RegisterConsoleCommands(typeof(PetDnaDebugCommand));
        }

        /// <summary>
        ///     Lists loaded DNA, or teleports to an indexed result from the latest list.
        /// </summary>
        [ConsoleCommand("petdna")]
        public static string HandleCommand(string action = "list", int index = 0)
        {
            if (string.Equals(action, "list", System.StringComparison.OrdinalIgnoreCase))
                return ListLoadedSpawns();

            if (string.Equals(action, "goto", System.StringComparison.OrdinalIgnoreCase))
                return TeleportToSpawn(index);

            if (string.Equals(action, "nearest", System.StringComparison.OrdinalIgnoreCase))
                return TeleportToNearestSpawn();

            return "Usage: petdna list | petdna goto <index> | petdna nearest";
        }

        private static string ListLoadedSpawns()
        {
            RefreshSpawnRecords();
            if (SpawnRecords.Count == 0)
                return "No loaded Pet DNA instances were found. Move into a region to load its streaming cells and retry.";

            StringBuilder output = new StringBuilder();
            output.AppendLine($"Loaded Pet DNA instances: {SpawnRecords.Count}");

            for (int index = 0; index < SpawnRecords.Count; index++)
            {
                SpawnRecord record = SpawnRecords[index];
                output.AppendLine(FormatRecord(index, record));
            }

            string result = output.ToString().TrimEnd();
            Log.LogInfo(result);
            return result;
        }

        private static string TeleportToSpawn(int index)
        {
            if (SpawnRecords.Count == 0)
                RefreshSpawnRecords();

            if (index < 0 || index >= SpawnRecords.Count)
                return $"DNA index {index} is outside the current range 0-{SpawnRecords.Count - 1}. Run 'petdna list' first.";

            if (Player.main == null || GotoConsoleCommand.main == null)
                return "The player or game's teleport controller is not ready.";

            SpawnRecord record = SpawnRecords[index];
            Vector3 destination = record.Position + Vector3.up * TeleportHeightOffset;
            GotoConsoleCommand.main.GotoPosition(destination);
            return $"Teleporting to DNA {index}: {record.ClassId} at {FormatPosition(record.Position)}.";
        }

        private static string TeleportToNearestSpawn()
        {
            RefreshSpawnRecords();
            if (SpawnRecords.Count == 0)
                return "No loaded Pet DNA instances were found.";

            return TeleportToSpawn(0);
        }

        private static void RefreshSpawnRecords()
        {
            SpawnRecords.Clear();
            PetDna[] dnaInstances = Object.FindObjectsOfType<PetDna>();
            Vector3 playerPosition = Player.main != null ? Player.main.transform.position : Vector3.zero;

            for (int index = 0; index < dnaInstances.Length; index++)
            {
                PetDna dna = dnaInstances[index];
                PrefabIdentifier identifier = dna.GetComponent<PrefabIdentifier>();
                string classId = identifier != null ? identifier.ClassId : dna.gameObject.name;
                float distance = Vector3.Distance(playerPosition, dna.transform.position);
                SpawnRecords.Add(new SpawnRecord(classId, dna.transform.position, distance));
            }

            SpawnRecords.Sort(CompareDistance);
        }

        private static int CompareDistance(SpawnRecord left, SpawnRecord right)
        {
            return left.Distance.CompareTo(right.Distance);
        }

        private static string FormatRecord(int index, SpawnRecord record)
        {
            return $"[{index}] {record.ClassId} at {FormatPosition(record.Position)}, {record.Distance:F1} m away";
        }

        private static string FormatPosition(Vector3 position)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F2}, {1:F2}, {2:F2})",
                position.x, position.y, position.z);
        }

        private struct SpawnRecord
        {
            internal readonly string ClassId;
            internal readonly Vector3 Position;
            internal readonly float Distance;

            internal SpawnRecord(string classId, Vector3 position, float distance)
            {
                ClassId = classId;
                Position = position;
                Distance = distance;
            }
        }
    }
}
