using UnityEngine;
using VoxelWorld;

public class VisualDebugger : MonoBehaviour
{
    public WorldConfiguration worldConfiguration;
    private WorldDataModel worldDataModel;

    private void Start()
    {
        worldDataModel = WorldDataModel.Instance;    
    }


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.yellow;
        foreach (Vector3Int coordinate in worldDataModel.chunksCache)
        {
            Vector3 position = new Vector3(
                coordinate.x + worldConfiguration.chunkDimensions.x * 0.5f,
                coordinate.y + worldConfiguration.chunkDimensions.y * 0.5f,
                coordinate.z + worldConfiguration.chunkDimensions.z * 0.5f);

            Gizmos.DrawWireCube(position, new Vector3(
                worldConfiguration.chunkDimensions.x,
                worldConfiguration.chunkDimensions.y,
                worldConfiguration.chunkDimensions.z)); 
        }
    }
}

