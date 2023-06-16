using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// holds all world related data during runtime
    /// </summary>
    public class WorldDataModel
    {
        /// <summary>
        /// runtime generated chunks
        /// </summary>
        public HashSet<Vector3Int> runtimeGeneratedChunks = new HashSet<Vector3Int>();


        /// keeps track of the created chunk columns, position in world coordinates
        public HashSet<Vector2Int> runtimeGeneratedChunkColumns = new HashSet<Vector2Int>();

        /// lookup for all created chunks
        public Dictionary<Vector3Int, Chunk> runtimeGeneratedChunksLookup = new Dictionary<Vector3Int, Chunk>();

        private static WorldDataModel _instance;

        public static WorldDataModel Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new WorldDataModel();
                }
                return _instance;
            }
        }
    }
}
