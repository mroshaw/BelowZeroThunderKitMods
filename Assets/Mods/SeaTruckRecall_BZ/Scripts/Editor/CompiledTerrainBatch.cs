using System;
using System.IO;

using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Editor
{
    /// <summary>
    /// Holds the compact octrees decoded from one terrain cache batch file.
    /// </summary>
    internal sealed class CompiledTerrainBatch
    {
        private const int ExpectedFileVersion = 4;
        private const int BytesPerNode = 4;

        private readonly byte[][] trees;
        private readonly Vector3Int treesPerBatch;

        private CompiledTerrainBatch(Vector3Int batchDimensions)
        {
            treesPerBatch = batchDimensions;
            trees = new byte[batchDimensions.x * batchDimensions.y * batchDimensions.z][];
        }

        /// <summary>
        /// Loads all valid octrees from a compiled terrain batch.
        /// </summary>
        internal static CompiledTerrainBatch Load(string path, Vector3Int batchIndex,
            CompiledTerrainCacheMetadata metadata)
        {
            CompiledTerrainBatch batch = new CompiledTerrainBatch(metadata.TreesPerBatch);
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                int fileVersion = reader.ReadInt32();
                if (fileVersion != ExpectedFileVersion)
                {
                    throw new InvalidDataException(
                        $"Unsupported compiled octree version {fileVersion} in '{path}'. Expected {ExpectedFileVersion}.");
                }

                for (int x = 0; x < metadata.TreesPerBatch.x; x++)
                {
                    for (int y = 0; y < metadata.TreesPerBatch.y; y++)
                    {
                        for (int z = 0; z < metadata.TreesPerBatch.z; z++)
                        {
                            Vector3Int localTree = new Vector3Int(x, y, z);
                            Vector3Int globalTree = new Vector3Int(
                                batchIndex.x * metadata.TreesPerBatch.x + x,
                                batchIndex.y * metadata.TreesPerBatch.y + y,
                                batchIndex.z * metadata.TreesPerBatch.z + z);
                            if (globalTree.x >= metadata.TreeCount.x || globalTree.y >= metadata.TreeCount.y ||
                                globalTree.z >= metadata.TreeCount.z)
                            {
                                continue;
                            }

                            ushort nodeCount = reader.ReadUInt16();
                            int byteCount = nodeCount * BytesPerNode;
                            byte[] treeData = reader.ReadBytes(byteCount);
                            if (treeData.Length != byteCount)
                            {
                                throw new EndOfStreamException($"Compiled terrain batch '{path}' ended unexpectedly.");
                            }
                            batch.trees[batch.GetTreeArrayIndex(localTree)] = treeData;
                        }
                    }
                }

                if (stream.Position != stream.Length)
                {
                    throw new InvalidDataException(
                        $"Compiled terrain batch '{path}' contains {stream.Length - stream.Position} unexpected bytes.");
                }
            }
            return batch;
        }

        /// <summary>
        /// Returns the raw compact-octree data for a batch-local tree coordinate.
        /// </summary>
        internal byte[] GetTree(Vector3Int localTree)
        {
            return trees[GetTreeArrayIndex(localTree)];
        }

        private int GetTreeArrayIndex(Vector3Int localTree)
        {
            return localTree.z + treesPerBatch.z *
                   (localTree.y + treesPerBatch.y * localTree.x);
        }
    }
}
