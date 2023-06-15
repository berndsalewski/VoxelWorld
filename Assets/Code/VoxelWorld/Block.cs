using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelWorld
{

    /// <summary>
    /// creates a mesh for a block, built out of Quads,
    /// the dimensions of a block are 1x1x1 in Unity units
    /// </summary>
    public class Block
    {
        /// <summary>
        /// length of a side of a block in Unity units
        /// </summary>
        public const float BLOCK_SIZE = 1f;
        public const float HALF_BLOCK_SIZE = BLOCK_SIZE * 0.5f;

        private static Vector3 blockOffset = new Vector3(HALF_BLOCK_SIZE, HALF_BLOCK_SIZE, HALF_BLOCK_SIZE);

        public Mesh mesh;

        private Chunk parentChunk;

        public Block(Vector3Int localCoordinates, Vector3Int chunkCoordinates, BlockType blockType, Chunk parent, BlockType healthType)
        {

            if (blockType == BlockType.Air)
            {
                return;
            }

            parentChunk = parent;
            
            Vector3 worldPosition = localCoordinates + chunkCoordinates + blockOffset;

            List<Quad> quads = new List<Quad>();
            if (MustDrawQuad(localCoordinates + Vector3Int.up, blockType))
            {
                quads.Add(new Quad(BlockSide.Top, worldPosition, blockType, healthType));
            }

            if (MustDrawQuad(localCoordinates + Vector3Int.down, blockType))
            {
                quads.Add(new Quad(BlockSide.Bottom, worldPosition, blockType, healthType));
            }

            if (MustDrawQuad(localCoordinates + Vector3Int.back, blockType))
            {
                quads.Add(new Quad(BlockSide.Front, worldPosition, blockType, healthType));
            }

            if (MustDrawQuad(localCoordinates + Vector3Int.forward, blockType))
            {
                quads.Add(new Quad(BlockSide.Back, worldPosition, blockType, healthType));
            }

            if (MustDrawQuad(localCoordinates + Vector3Int.left, blockType))
            {
                quads.Add(new Quad(BlockSide.Left, worldPosition, blockType, healthType));
            }

            if (MustDrawQuad(localCoordinates + Vector3Int.right, blockType))
            {
                quads.Add(new Quad(BlockSide.Right, worldPosition, blockType, healthType));
            }

            if (quads.Count == 0)
            {
                return;
            }

            Mesh[] sideMeshes = new Mesh[quads.Count];
            int m = 0;
            foreach (Quad quad in quads)
            {
                sideMeshes[m] = quad.mesh;
                m++;
            }

            mesh = MeshUtils.MergeMeshes(sideMeshes);
        }

        private bool MustDrawQuad(Vector3Int neighbourBlockCoordinates, BlockType ownBlockType)
        {
            if (IsOutsideOfChunk(neighbourBlockCoordinates))
            {
                return true;
            }

            if (IsAirBlock(neighbourBlockCoordinates))
            {
                return true;
            }

            if (IsWaterBlock(neighbourBlockCoordinates) && ownBlockType != BlockType.Water)
            {
                return true;
            }

            return false;
        }

        private bool IsAirBlock(Vector3Int coordinates)
        {
            return parentChunk.chunkData[Chunk.ToBlockIndex(coordinates)] == BlockType.Air;
        }

        private bool IsWaterBlock(Vector3Int coordinates)
        {
            return parentChunk.chunkData[Chunk.ToBlockIndex(coordinates)] == BlockType.Water;
        }

        private bool IsOutsideOfChunk(Vector3Int coordinates)
        {
            return (coordinates.x < 0 || coordinates.x >= parentChunk.xBlockCount
                || coordinates.y < 0 || coordinates.y >= parentChunk.yBlockCount
                || coordinates.z < 0 || coordinates.z >= parentChunk.zBlockCount);
        }
    }
}