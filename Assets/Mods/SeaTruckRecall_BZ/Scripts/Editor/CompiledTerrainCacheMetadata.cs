using System;
using System.Globalization;
using System.IO;

using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Editor
{
    /// <summary>
    /// Describes the voxel and batch layout recorded in a Below Zero terrain cache index.
    /// </summary>
    internal struct CompiledTerrainCacheMetadata
    {
        internal int IndexVersion;
        internal Vector3Int WorldSize;
        internal Vector3Int TreeCount;
        internal int TreeSize;
        internal Vector3Int TreesPerBatch;

        internal Vector3Int BatchCount => new Vector3Int(
            DivideRoundUp(TreeCount.x, TreesPerBatch.x),
            DivideRoundUp(TreeCount.y, TreesPerBatch.y),
            DivideRoundUp(TreeCount.z, TreesPerBatch.z));

        /// <summary>
        /// Reads and validates the layout header from an Expansion index file.
        /// </summary>
        internal static CompiledTerrainCacheMetadata Read(string cacheRoot)
        {
            string indexPath = Path.Combine(cacheRoot, "index.txt");
            using (StreamReader reader = new StreamReader(indexPath))
            {
                CompiledTerrainCacheMetadata metadata = new CompiledTerrainCacheMetadata
                {
                    IndexVersion = ParseInteger(ReadRequiredLine(reader, "index version"), "index version"),
                    WorldSize = ParseVector(ReadRequiredLine(reader, "world size"), "world size"),
                    TreeCount = ParseVector(ReadRequiredLine(reader, "tree count"), "tree count"),
                    TreeSize = ParseInteger(ReadRequiredLine(reader, "tree size"), "tree size"),
                    TreesPerBatch = ParseVector(ReadRequiredLine(reader, "trees per batch"), "trees per batch")
                };
                metadata.Validate();
                return metadata;
            }
        }

        private void Validate()
        {
            if (WorldSize.x <= 0 || WorldSize.y <= 0 || WorldSize.z <= 0 ||
                TreeCount.x <= 0 || TreeCount.y <= 0 || TreeCount.z <= 0 || TreeSize <= 0 ||
                TreesPerBatch.x <= 0 || TreesPerBatch.y <= 0 || TreesPerBatch.z <= 0)
            {
                throw new InvalidDataException("The terrain cache index contains non-positive dimensions.");
            }
            if (TreeCount.x * TreeSize != WorldSize.x || TreeCount.y * TreeSize != WorldSize.y ||
                TreeCount.z * TreeSize != WorldSize.z)
            {
                throw new InvalidDataException("The terrain cache tree dimensions do not match its world size.");
            }
        }

        private static Vector3Int ParseVector(string line, string fieldName)
        {
            string[] values = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != 3)
            {
                throw new InvalidDataException($"Invalid {fieldName} in terrain cache index: '{line}'.");
            }
            return new Vector3Int(ParseInteger(values[0], fieldName), ParseInteger(values[1], fieldName),
                ParseInteger(values[2], fieldName));
        }

        private static int ParseInteger(string value, string fieldName)
        {
            int parsedValue;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue))
            {
                throw new InvalidDataException($"Invalid {fieldName} in terrain cache index: '{value}'.");
            }
            return parsedValue;
        }

        private static string ReadRequiredLine(StreamReader reader, string fieldName)
        {
            string line = reader.ReadLine();
            if (line == null)
            {
                throw new EndOfStreamException($"Terrain cache index ended before the {fieldName} field.");
            }
            return line;
        }

        private static int DivideRoundUp(int value, int divisor)
        {
            return (value + divisor - 1) / divisor;
        }
    }
}
