using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoxelWorld
{
    public class StartMenu : MonoBehaviour
    {
        public TMP_Dropdown worldSelector;
        public TMP_InputField inputWorldName;
        private List<string> saveFiles;

        // Start is called before the first frame update
        private void Start()
        {
            ResetCursorToDefault();
            UpdateAvailableSaveFiles();
        }

        //called from ui  
        public void LoadSelectedSaveFile()
        {
            SessionGameData.LoadFromFile = true;

            WriteSelectedWorldNameToSessionData();

            SceneManager.LoadScene((int)SceneIndex.voxelWorld);
        }

        // called from ui
        public void DeleteSelectedSaveFile()
        {
            File.Delete(FileSaver.saveFileDirectory + saveFiles[worldSelector.value]);
            UpdateAvailableSaveFiles();
        }
        
        // called from ui
        public void GenerateNewWorld()
        {
            if (string.IsNullOrEmpty(inputWorldName.text))
            {
                inputWorldName.Select();
                return;
            }
            SessionGameData.LoadFromFile = false;
            SessionGameData.WorldFileName = inputWorldName.text;
            SceneManager.LoadScene((int)SceneIndex.voxelWorld);
        }

        private void UpdateAvailableSaveFiles()
        {
            string[] _saveFileNames = Directory.GetFiles(FileSaver.saveFileDirectory);
            if (saveFiles == null)
            {
                saveFiles = new List<string>(_saveFileNames);
            }
            else
            {
                saveFiles.Clear();
                saveFiles.AddRange(_saveFileNames);
            }

            for (int i = 0; i < saveFiles.Count; i++)
            {
                saveFiles[i] = Path.GetFileName(saveFiles[i]);
            }

            worldSelector.ClearOptions();
            worldSelector.AddOptions(saveFiles);
        }

        private void ResetCursorToDefault()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void WriteSelectedWorldNameToSessionData()
        {
            int fileEndingStart = saveFiles[worldSelector.value].Length - FileSaver.SAVE_FILE_ENDING.Length;
            string worldFileName = saveFiles[worldSelector.value].Remove(fileEndingStart);
            SessionGameData.WorldFileName = worldFileName;
        }
    }
}