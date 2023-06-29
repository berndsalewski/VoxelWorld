using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace VoxelWorld
{
    static public class FileSaver
    {
        public const string SAVE_FILE_ENDING = ".dat";

        static public readonly string saveFileDirectory = Application.persistentDataPath + "/savedata/";

        private static SaveFileData saveFile;

        static private string BuildSaveFileName()
        {
            StringBuilder filePath = new StringBuilder();
            filePath.Append(saveFileDirectory);
            filePath.Append(SessionGameData.worldFileName);
            filePath.Append(SAVE_FILE_ENDING);

            return filePath.ToString();
        }

        static public void Save(Player player)
        {
            WorldDataModel worldModel = WorldDataModel.Instance;
            string fileName = BuildSaveFileName();
            if (!File.Exists(fileName))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(fileName, FileMode.OpenOrCreate);
            saveFile = new SaveFileData(
                worldModel,
                player.position);

            bf.Serialize(file, saveFile);
            file.Close();
            Debug.Log($"Saving World to File: {fileName}");
        }

        static public SaveFileData Load()
        {
            string fileName = BuildSaveFileName();
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
            Debug.LogError($"File not found with name: {fileName}");
            return null;
        }
    }
}