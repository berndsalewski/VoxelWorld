using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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

        public SaveFileData(
            WorldDataModel worldModel,
            Vector3 playerPosition)
        {
            chunkCoordinates = new int[worldModel.chunksCache.Count * 3];
            int i = 0;
            foreach (Vector3Int chunkPosition in worldModel.chunksCache)
            {
                chunkCoordinates[i] = chunkPosition.x;
                chunkCoordinates[i + 1] = chunkPosition.y;
                chunkCoordinates[i + 2] = chunkPosition.z;
                i += 3;
            }

            chunkColumns = new int[worldModel.chunkColumnsCache.Count * 2];
            i = 0;
            foreach (Vector3Int chunkColumnPosition in worldModel.chunkColumnsCache)
            {
                chunkColumns[i] = chunkColumnPosition.x;
                chunkColumns[i + 1] = chunkColumnPosition.y;
                i += 2;
            }

            chunksData = new int[worldModel.chunksDataCacheLookup.Count * WorldBuilder.blockCountPerChunk];
            chunkVisibility = new bool[worldModel.chunksDataCacheLookup.Count];
            int visibilityIndex = 0;
            int chunkDataIndex = 0;
            foreach (KeyValuePair<Vector3Int, BlockType[]> chunkData in worldModel.chunksDataCacheLookup)
            {
                foreach (BlockType blockType in chunkData.Value)
                {
                    chunksData[chunkDataIndex] = (int)blockType;
                    chunkDataIndex++;
                }

                if (worldModel.IsChunkActive(chunkData.Key))
                {
                    Chunk chunk = worldModel.GetChunk(chunkData.Key);
                    chunkVisibility[visibilityIndex] = chunk.meshRendererSolidBlocks.enabled;
                }

                visibilityIndex++;
            }

            playerPositionX = (int)playerPosition.x + Block.HALF_BLOCK_SIZE;
            playerPositionY = (int)playerPosition.y + Block.HALF_BLOCK_SIZE;
            playerPositionZ = (int)playerPosition.z + Block.HALF_BLOCK_SIZE;
        }

        /// <summary>
        /// dumps the content of the save file to the console for debugging purposes
        /// </summary>
        public void DumpToConsole()
        {
            StringBuilder output = new StringBuilder();

            output.Append($"cData: {chunksData.Length}:{chunksData.Sum()} ");
            output.Append($"cCoordinates: {chunkCoordinates.Length}:{chunkCoordinates.Sum()} ");
            output.Append($"cColumns: {chunkColumns.Length}:{chunkColumns.Sum()} ");
            output.AppendLine("\ncolumns");
            for (int i = 0; i < chunkColumns.Length; i++)
            {
                output.Append(chunkColumns[i] + ",");
            }

            output.AppendLine("\nvisibility");
            for (int i = 0; i < chunkVisibility.Length; i++)
            {
                output.Append($"{chunkVisibility[i]},");
            }

            output.AppendLine("\nchunks");
            for (int i = 0; i < chunkCoordinates.Length; i++)
            {
                output.Append($"{chunkCoordinates[i]},");
            }

            Debug.Log(output);
        }
    }
}
