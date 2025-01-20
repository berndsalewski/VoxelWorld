using UnityEngine;

[CreateAssetMenu(fileName = "WorldConfiguration",menuName = "VoxelWorld/WorldConfiguration", order = -10)]
public class WorldConfiguration : ScriptableObject
{
	public Vector3Int chunkDimensions;

    [HideInInspector]
    public int blockCountPerChunk;

    [Tooltip("if a block is above the surface and below this value it will be water, otherwise air")]
    public int waterLevel;
    
    [Tooltip("radius around the player in which new chunk columns are added, value is number of chunks")]
    public int chunkColumnDrawRadius;

	[Tooltip("max height in number of chunks")]
	public int worldHeight;


    private void Awake()
    {
        blockCountPerChunk = chunkDimensions.x * chunkDimensions.y * chunkDimensions.z;
    }
}

