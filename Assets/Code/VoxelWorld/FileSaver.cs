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
        /// stores serialized coordinates of all created chunks
        /// </summary>
        public int[] createdChunksCoordinates;
        public int[] chunkColumnValues;
        public int[] allChunkData;
        public bool[] chunkVisibility;

        public int fpcX;
        public int fpcY;
        public int fpcZ;

        public WorldData() { }

        public WorldData(HashSet<Vector3Int> createdChunks, HashSet<Vector2Int> createdChunkColumns, Dictionary<Vector3Int, Chunk> chunks, Vector3 fpcPos)
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

            allChunkData = new int[chunks.Count * World.chunkDimensions.x * World.chunkDimensions.y * World.chunkDimensions.z];
            chunkVisibility = new bool[chunks.Count];
            int vIndex = 0;
            i = 0;
            foreach (KeyValuePair<Vector3Int, Chunk> item in chunks)
            {
                foreach (BlockType blockType in item.Value.chunkData)
                {

                    allChunkData[i] = (int)blockType;
                    i++;
                }
                chunkVisibility[vIndex] = item.Value.meshRendererSolidBlocks.enabled;
                vIndex++;
            }

            fpcX = (int)fpcPos.x;
            fpcY = (int)fpcPos.y;
            fpcZ = (int)fpcPos.z;
        }
    }


    public static class FileSaver
    {
        private static WorldData worldData;
        static string BuildFileName(Vector3 worldDimensions)
        {
            return Application.persistentDataPath
                + "/savedata/World_"
                + World.chunkDimensions.x + "_"
                + World.chunkDimensions.y + "_"
                + World.chunkDimensions.z + "_"
                + worldDimensions.x + "_"
                + worldDimensions.y + "_"
                + worldDimensions.z + ".dat";
        }

        public static void Save(World world)
        {
            string fileName = BuildFileName(world.worldDimensions);
            if (!File.Exists(fileName))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(fileName, FileMode.OpenOrCreate);
            worldData = new WorldData(world.createdChunks, world.createdChunkColumns, world.chunks, world.firstPersonController.transform.position);
            bf.Serialize(file, worldData);
            file.Close();
            Debug.Log($"Saving World to File: {fileName}");
        }

        public static WorldData Load(World world)
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