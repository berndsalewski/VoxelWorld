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
        public int[] chunkCheckerValues;
        public int[] chunkColumnValues;
        public int[] allChunkData;
        public bool[] chunkVisibility;

        public int fpcX;
        public int fpcY;
        public int fpcZ;

        public WorldData() { }

        public WorldData(HashSet<Vector3Int> chunkChecker, HashSet<Vector2Int> chunkColumns, Dictionary<Vector3Int, Chunk> chunks, Vector3 fpcPos)
        {
            chunkCheckerValues = new int[chunkChecker.Count * 3];
            int i = 0;
            foreach (Vector3Int chunkPosition in chunkChecker)
            {
                chunkCheckerValues[i] = chunkPosition.x;
                chunkCheckerValues[i + 1] = chunkPosition.y;
                chunkCheckerValues[i + 2] = chunkPosition.z;
                i += 3;
            }

            chunkColumnValues = new int[chunkColumns.Count * 2];
            i = 0;
            foreach (Vector3Int chunkColumnPosition in chunkColumns)
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
        static string BuildFileName()
        {
            return Application.persistentDataPath
                + "/savedata/World_"
                + World.chunkDimensions.x + "_"
                + World.chunkDimensions.y + "_"
                + World.chunkDimensions.z + "_"
                + World.worldDimensions.x + "_"
                + World.worldDimensions.y + "_"
                + World.worldDimensions.z + ".dat";
        }

        public static void Save(World world)
        {
            string fileName = BuildFileName();
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

        public static WorldData Load()
        {
            string fileName = BuildFileName();
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