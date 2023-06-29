using UnityEngine;
using UnityEngine.SceneManagement;
using VoxelWorld;

public class GameController : MonoBehaviour
{
    public Player player;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && Input.GetKey(KeyCode.LeftControl))
        {
            BackToStartMenu();
        }

        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl))
        {
            SaveWorld();
        }
    }

    private void BackToStartMenu()
    {
        FileSaver.Save(player);
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// keep the public method for hooking up this logic to ui
    /// </summary>
    public void SaveWorld()
    {
        FileSaver.Save(player);
    }
}
