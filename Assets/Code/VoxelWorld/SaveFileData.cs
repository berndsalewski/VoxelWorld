using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelWorld;

namespace VoxelWorld
{
    [Serializable]
    public class SaveFileData
    {
        /// <summary>
        /// coordinates of all created chunks, serialized Vector3Int
        /// </summary>
        public int[] chunkCoordinates;

        /// <summary>
        /// coordinates of all created chunk columns, serialized Vector2Int
        /// </summary>
        public int[] chunkColumns;

        /// <summary>
        /// chunk data for all created chunks
        /// </summary>
        public int[] chunksData;

        /// <summary>
        /// visibility data for all created chunks
        /// </summary>
        public bool[] chunkVisibility;

        public float playerPositionX;
        public float playerPositionY;
        public float playerPositionZ;

        public SaveFileData() { }

        public SaveFileData(HashSet<Vector3Int> allChunks, HashSet<Vector2Int> allChunkColumns, Dictionary<Vector3Int, Chunk> allChunksLookup, Vector3 playerPosition)
        {
            chunkCoordinates = new int[allChunks.Count * 3];
            int i = 0;
            foreach (Vector3Int chunkPosition in allChunks)
            {
                chunkCoordinates[i] = chunkPosition.x;
                chunkCoordinates[i + 1] = chunkPosition.y;
                chunkCoordinates[i + 2] = chunkPosition.z;
                i += 3;
            }

            this.chunkColumns = new int[allChunkColumns.Count * 2];
            i = 0;
            foreach (Vector3Int chunkColumnPosition in allChunkColumns)
            {
                this.chunkColumns[i] = chunkColumnPosition.x;
                this.chunkColumns[i + 1] = chunkColumnPosition.y;
                i += 2;
            }

            chunksData = new int[allChunksLookup.Count * WorldBuilder.chunkDimensions.x * WorldBuilder.chunkDimensions.y * WorldBuilder.chunkDimensions.z];
            chunkVisibility = new bool[allChunksLookup.Count];
            int visibilityIndex = 0;
            i = 0;
            foreach (KeyValuePair<Vector3Int, Chunk> chunk in allChunksLookup)
            {
                foreach (BlockType blockType in chunk.Value.chunkData)
                {

                    chunksData[i] = (int)blockType;
                    i++;
                }
                chunkVisibility[visibilityIndex] = chunk.Value.meshRendererSolidBlocks.enabled;
                visibilityIndex++;
            }

            playerPositionX = (int)playerPosition.x + Block.HALF_BLOCK_SIZE;
            playerPositionY = (int)playerPosition.y + Block.HALF_BLOCK_SIZE;
            playerPositionZ = (int)playerPosition.z + Block.HALF_BLOCK_SIZE;
        }
    }
}
