namespace VoxelWorld
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// creates a mesh for a block
    /// </summary>
    public class Block
    {
        public Mesh mesh;
        Chunk parentChunk;

        // Use this for initialization
        public Block(Vector3Int offset, BlockType blockType, Chunk chunk, BlockType healthType)
        {

            if (blockType == BlockType.Air)
            {
                return;
            }

            parentChunk = chunk;
            Vector3Int localBlockPos = offset - chunk.location;

            List<Quad> quads = new List<Quad>();
            if (CanDrawQuad(localBlockPos + Vector3Int.up, blockType))
            {
                quads.Add(new Quad(MeshUtils.BlockSide.Top, offset, blockType, healthType));
            }

            if (CanDrawQuad(localBlockPos + Vector3Int.down, blockType))
            {
                quads.Add(new Quad(MeshUtils.BlockSide.Bottom, offset, blockType, healthType));
            }

            if (CanDrawQuad(localBlockPos + Vector3Int.back, blockType))
            {
                quads.Add(new Quad(MeshUtils.BlockSide.Front, offset, blockType, healthType));
            }

            if (CanDrawQuad(localBlockPos + Vector3Int.forward, blockType))
            {
                quads.Add(new Quad(MeshUtils.BlockSide.Back, offset, blockType, healthType));
            }

            if (CanDrawQuad(localBlockPos + Vector3Int.left, blockType))
            {
                quads.Add(new Quad(MeshUtils.BlockSide.Left, offset, blockType, healthType));
            }

            if (CanDrawQuad(localBlockPos + Vector3Int.right, blockType))
            {
                quads.Add(new Quad(MeshUtils.BlockSide.Right, offset, blockType, healthType));
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



        private bool CanDrawQuad(Vector3Int pos, BlockType blockType)
        {
            if (isNextChunkBlock(pos))
            {
                return true;
            }

            if (IsAirBlock(pos))
            {
                return true;
            }

            if (IsWaterBlock(pos) && blockType != BlockType.Water)
            {
                return true;
            }

            return false;
        }

        private bool IsAirBlock(Vector3Int pos)
        {
            return parentChunk.chunkData[World.ToFlat(pos)] == BlockType.Air;
        }

        private bool IsWaterBlock(Vector3Int pos)
        {
            return parentChunk.chunkData[World.ToFlat(pos)] == BlockType.Water;
        }

        private bool isNextChunkBlock(Vector3Int pos)
        {
            return (pos.x < 0 || pos.x >= parentChunk.width
                || pos.y < 0 || pos.y >= parentChunk.height
                || pos.z < 0 || pos.z >= parentChunk.depth);
        }
    }
}