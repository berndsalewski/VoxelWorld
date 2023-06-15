using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

namespace VoxelWorld
{
    [Serializable]
    public class WorldData
    {
        /// <summary>
        /// coordinates of all created chunks
        /// </summary>

        public int[] createdChunksCoordinates;

        /// <summary>
        /// coordinates of all created chunk columns
        /// </summary>
        public int[] chunkColumnValues;

        /// <summary>
        /// chunk data for all created chunks
        /// </summary>
        public int[] chunkData;

        /// <summary>
        /// visibility data for all created chunks
        /// </summary>
        public bool[] chunkVisibility;

        public float playerPositionX;
        public float playerPositionY;
        public float playerPositionZ;

        public WorldData() { }

        public WorldData(HashSet<Vector3Int> createdChunks, HashSet<Vector2Int> createdChunkColumns, Dictionary<Vector3Int, Chunk> chunks, Vector3 playerPosition)
        {
            createdChunksCoordinates = new int[createdChunks.Count * 3];
            int i = 0;
            foreach (Vector3Int chunkPosition in createdChunks)
            {
                createdChunksCoordinates[i] = chunkPosition.x;
                createdChunksCoordinates[i + 1] = chunkPosition.y;
                createdChunksCoordinates[i + 2] = chunkPosition.z;
                i += 3;
            }

            chunkColumnValues = new int[createdChunkColumns.Count * 2];
            i = 0;
            foreach (Vector3Int chunkColumnPosition in createdChunkColumns)
            {
                chunkColumnValues[i] = chunkColumnPosition.x;
                chunkColumnValues[i + 1] = chunkColumnPosition.y;
                i += 2;
            }

            chunkData = new int[chunks.Count * WorldBuilder.chunkDimensions.x * WorldBuilder.chunkDimensions.y * WorldBuilder.chunkDimensions.z];
            chunkVisibility = new bool[chunks.Count];
            int vIndex = 0;
            i = 0;
            foreach (KeyValuePair<Vector3Int, Chunk> item in chunks)
            {
                foreach (BlockType blockType in item.Value.chunkData)
                {

                    chunkData[i] = (int)blockType;
                    i++;
                }
                chunkVisibility[vIndex] = item.Value.meshRendererSolidBlocks.enabled;
                vIndex++;
            }

            playerPositionX = (int)playerPosition.x + Block.HALF_BLOCK_SIZE;
            playerPositionY = (int)playerPosition.y + Block.HALF_BLOCK_SIZE;
            playerPositionZ = (int)playerPosition.z + Block.HALF_BLOCK_SIZE;
        }
    }


    public static class FileSaver
    {
        private static WorldData worldData;
        static string BuildFileName(Vector3 worldDimensions)
        {
            return Application.persistentDataPath
                + "/savedata/World_"
                + WorldBuilder.chunkDimensions.x + "_"
                + WorldBuilder.chunkDimensions.y + "_"
                + WorldBuilder.chunkDimensions.z + "_"
                + worldDimensions.x + "_"
                + worldDimensions.y + "_"
                + worldDimensions.z + ".dat";
        }

        public static void Save(WorldBuilder world, Player player)
        {
            string fileName = BuildFileName(world.worldDimensions);
            if (!File.Exists(fileName))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(fileName, FileMode.OpenOrCreate);
            worldData = new WorldData(world.createdChunks, world.createdChunkColumns, world.chunks, player.position);
            bf.Serialize(file, worldData);
            file.Close();
            Debug.Log($"Saving World to File: {fileName}");
        }

        public static WorldData Load(WorldBuilder world)
        {
            string fileName = BuildFileName(world.worldDimensions);
            if (File.Exists(fileName))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(fileName, FileMode.Open);
                worldData = new WorldData();
                worldData = (WorldData)bf.Deserialize(file);
                file.Close();
                Debug.Log($"Loading World from File: {fileName}");
                return worldData;
            }
            Debug.Log($"File not found");
            return null;
        }
    }
}