using UnityEngine;
using System.Collections;
using VoxelWorld;

public class VisualDebugger : MonoBehaviour
{
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
                coordinate.x + WorldBuilder.chunkDimensions.x * 0.5f,
                coordinate.y + WorldBuilder.chunkDimensions.y * 0.5f,
                coordinate.z + WorldBuilder.chunkDimensions.z * 0.5f);

            Gizmos.DrawWireCube(position, new Vector3(
                WorldBuilder.chunkDimensions.x,
                WorldBuilder.chunkDimensions.y,
                WorldBuilder.chunkDimensions.z)); 
        }
    }
}

