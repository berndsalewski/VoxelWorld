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
        public Mesh mesh;

        private Chunk parentChunk;

        public Block(Vector3Int worldBlockPosition, BlockType blockType, Chunk parent, BlockType healthType)
        {

            if (blockType == BlockType.Air)
            {
                return;
            }

            parentChunk = parent;
            Vector3Int localBlockPos = worldBlockPosition - parentChunk.worldPosition;

            List<Quad> quads = new List<Quad>();
            if (MustDrawQuad(localBlockPos + Vector3Int.up, blockType))
            {
                quads.Add(new Quad(BlockSide.Top, worldBlockPosition, blockType, healthType));
            }

            if (MustDrawQuad(localBlockPos + Vector3Int.down, blockType))
            {
                quads.Add(new Quad(BlockSide.Bottom, worldBlockPosition, blockType, healthType));
            }

            if (MustDrawQuad(localBlockPos + Vector3Int.back, blockType))
            {
                quads.Add(new Quad(BlockSide.Front, worldBlockPosition, blockType, healthType));
            }

            if (MustDrawQuad(localBlockPos + Vector3Int.forward, blockType))
            {
                quads.Add(new Quad(BlockSide.Back, worldBlockPosition, blockType, healthType));
            }

            if (MustDrawQuad(localBlockPos + Vector3Int.left, blockType))
            {
                quads.Add(new Quad(BlockSide.Left, worldBlockPosition, blockType, healthType));
            }

            if (MustDrawQuad(localBlockPos + Vector3Int.right, blockType))
            {
                quads.Add(new Quad(BlockSide.Right, worldBlockPosition, blockType, healthType));
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

        private bool MustDrawQuad(Vector3Int neighbourBlockPos, BlockType ownBlockType)
        {
            if (IsOutsideOfChunk(neighbourBlockPos))
            {
                return true;
            }

            if (IsAirBlock(neighbourBlockPos))
            {
                return true;
            }

            if (IsWaterBlock(neighbourBlockPos) && ownBlockType != BlockType.Water)
            {
                return true;
            }

            return false;
        }

        private bool IsAirBlock(Vector3Int pos)
        {
            return parentChunk.chunkData[Chunk.ToBlockIndex(pos)] == BlockType.Air;
        }

        private bool IsWaterBlock(Vector3Int pos)
        {
            return parentChunk.chunkData[Chunk.ToBlockIndex(pos)] == BlockType.Water;
        }

        private bool IsOutsideOfChunk(Vector3Int pos)
        {
            return (pos.x < 0 || pos.x >= parentChunk.width
                || pos.y < 0 || pos.y >= parentChunk.height
                || pos.z < 0 || pos.z >= parentChunk.depth);
        }
    }
}