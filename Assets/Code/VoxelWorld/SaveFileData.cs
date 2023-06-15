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
        public int[] createdChunksCoordinates;

        /// <summary>
        /// coordinates of all created chunk columns, serialized Vector2Int
        /// </summary>
        public int[] createdChunkColumns;

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

        public SaveFileData(HashSet<Vector3Int> createdChunks, HashSet<Vector2Int> createdChunkColumns, Dictionary<Vector3Int, Chunk> chunks, Vector3 playerPosition)
        {
            createdChunksCoordinates = new int[createdChunks.Count * 3];
            int i = 0;
            foreach (Vector3Int chunkPosition in createdChunks)
            {
                createdChunksCoordinates[i] = chunkPosition.x;
                createdChunksCoordinates[i + 1] = chunkPosition.y;
                createdChunksCoordinates[i + 2] = chunkPosition.z;
                i += 3;
            }

            this.createdChunkColumns = new int[createdChunkColumns.Count * 2];
            i = 0;
            foreach (Vector3Int chunkColumnPosition in createdChunkColumns)
            {
                this.createdChunkColumns[i] = chunkColumnPosition.x;
                this.createdChunkColumns[i + 1] = chunkColumnPosition.y;
                i += 2;
            }

            chunksData = new int[chunks.Count * WorldBuilder.chunkDimensions.x * WorldBuilder.chunkDimensions.y * WorldBuilder.chunkDimensions.z];
            chunkVisibility = new bool[chunks.Count];
            int vIndex = 0;
            i = 0;
            foreach (KeyValuePair<Vector3Int, Chunk> item in chunks)
            {
                foreach (BlockType blockType in item.Value.chunkData)
                {

                    chunksData[i] = (int)blockType;
                    i++;
                }
                chunkVisibility[vIndex] = item.Value.meshRendererSolidBlocks.enabled;
                vIndex++;
            }

            playerPositionX = (int)playerPosition.x + Block.HALF_BLOCK_SIZE;
            playerPositionY = (int)playerPosition.y + Block.HALF_BLOCK_SIZE;
            playerPositionZ = (int)playerPosition.z + Block.HALF_BLOCK_SIZE;
        }
    }
}
