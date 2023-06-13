using System;
using UnityEngine;

namespace VoxelWorld
{
	public static class WorldUtils
	{
        // System.Tuple<Vector3Int, Vector3Int> can be written as (Vector3Int, Vector3Int) since C#7
        // computes chunk and block coordinates for a given point in the world
        // currently only works if blocks have a size of 1 and are aligned to the unity grid
        public static (Vector3Int, Vector3Int) FromWorldPosToCoordinates(Vector3 worldPos)
        {
            Vector3Int chunkCoordinates = new Vector3Int();
            chunkCoordinates.x = Mathf.FloorToInt(worldPos.x / World.chunkDimensions.x) * World.chunkDimensions.x;
            chunkCoordinates.y = Mathf.FloorToInt(worldPos.y / World.chunkDimensions.y) * World.chunkDimensions.y;
            chunkCoordinates.z = Mathf.FloorToInt(worldPos.z / World.chunkDimensions.z) * World.chunkDimensions.z;

            Vector3Int blockCoordinates = new Vector3Int();
            blockCoordinates.x = Mathf.FloorToInt(worldPos.x) - chunkCoordinates.x;
            blockCoordinates.y = Mathf.FloorToInt(worldPos.y) - chunkCoordinates.y;
            blockCoordinates.z = Mathf.FloorToInt(worldPos.z) - chunkCoordinates.z;

            return (chunkCoordinates, blockCoordinates);
        }

        /// <summary>
        /// adjust local block coordinate relative to chunk block coordinate if it crosses into neighbouring chunks
        /// </summary>
        /// <param name="chunkPos"></param>
        /// <param name="blockPos"></param>
        /// <returns></returns>
        public static (Vector3Int, Vector3Int) AdjustCoordinatesToGrid(Vector3Int chunkPos, Vector3Int blockPos)
        {
            Vector3Int newChunkPos = chunkPos;
            Vector3Int newBlockPos = blockPos;

            // X axis
            if (blockPos.x >= World.chunkDimensions.x)
            {
                newBlockPos.x = blockPos.x % World.chunkDimensions.x;
                newChunkPos.x += blockPos.x / World.chunkDimensions.x * World.chunkDimensions.x;
            }
            else if (blockPos.x < 0)
            {
                newBlockPos.x = Mathf.CeilToInt((float)Mathf.Abs(blockPos.x) / World.chunkDimensions.x) * World.chunkDimensions.x - Mathf.Abs(blockPos.x);
                newChunkPos.x += Mathf.FloorToInt((float)blockPos.x / World.chunkDimensions.x) * World.chunkDimensions.x;
            }

            // Y axis
            if (blockPos.y >= World.chunkDimensions.y)
            {
                newBlockPos.y = blockPos.y % World.chunkDimensions.y;
                newChunkPos.y += blockPos.y / World.chunkDimensions.y * World.chunkDimensions.y;
            }
            else if (blockPos.y < 0)
            {
                newBlockPos.y = Mathf.CeilToInt((float)Mathf.Abs(blockPos.y) / World.chunkDimensions.y) * World.chunkDimensions.y - Mathf.Abs(blockPos.y);
                newChunkPos.y += Mathf.FloorToInt((float)blockPos.y / World.chunkDimensions.y) * World.chunkDimensions.y;
            }

            // Z axis
            if (blockPos.z >= World.chunkDimensions.z)
            {
                newBlockPos.z = blockPos.z % World.chunkDimensions.z;
                newChunkPos.z += blockPos.z / World.chunkDimensions.z * World.chunkDimensions.z;
            }
            else if (blockPos.z < 0)
            {
                newBlockPos.z = Mathf.CeilToInt((float)Mathf.Abs(blockPos.z) / World.chunkDimensions.z) * World.chunkDimensions.z - Mathf.Abs(blockPos.z);
                newChunkPos.z += Mathf.FloorToInt((float)blockPos.z / World.chunkDimensions.z) * World.chunkDimensions.z;
            }

            return (newChunkPos, newBlockPos);
        }
    }
}

