using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

namespace VoxelWorld
{
    public static class FileSaver
    {
        private static SaveFileData saveFile;
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

        public static void Save(WorldBuilder worldBuilder, Player player)
        {
            WorldDataModel worldModel = WorldDataModel.Instance;
            string fileName = BuildFileName(worldBuilder.worldDimensions);
            if (!File.Exists(fileName))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(fileName, FileMode.OpenOrCreate);
            saveFile = new SaveFileData(worldModel.runtimeGeneratedChunks, worldModel.runtimeGeneratedChunkColumns, worldModel.runtimeGeneratedChunksLookup, player.position);
            bf.Serialize(file, saveFile);
            file.Close();
            Debug.Log($"Saving World to File: {fileName}");
        }

        public static SaveFileData Load(WorldBuilder world)
        {
            string fileName = BuildFileName(world.worldDimensions);
            if (File.Exists(fileName))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(fileName, FileMode.Open);
                saveFile = new SaveFileData();
                saveFile = (SaveFileData)bf.Deserialize(file);
                file.Close();
                Debug.Log($"Loading World from File: {fileName}");
                return saveFile;
            }
            Debug.Log($"File not found");
            return null;
        }
    }
}