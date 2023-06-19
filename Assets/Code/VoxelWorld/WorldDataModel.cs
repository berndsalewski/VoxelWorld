using System;
using System.Collections.Generic;
using System.Text;
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
        private HashSet<Vector3Int> _chunks = new HashSet<Vector3Int>();
        public IReadOnlyCollection<Vector3Int> chunks => _chunks;

        /// <summary>
        /// lookup for all created chunks
        /// </summary>
        private Dictionary<Vector3Int, Chunk> _chunksLookup = new Dictionary<Vector3Int, Chunk>();

        /// runtime generated chunk columns, position in world coordinates
        private HashSet<Vector2Int> _chunkColumns = new HashSet<Vector2Int>();
        public IReadOnlyCollection<Vector2Int> chunkColumns => _chunkColumns;

        /// <summary>
        /// all chunks ever generated
        /// </summary>
        private HashSet<Vector3Int> _chunksCache = new HashSet<Vector3Int>();
        public IReadOnlyCollection<Vector3Int> chunksCache => _chunksCache;

        /// <summary>
        /// all chunk columns ever generated
        /// </summary>
        private HashSet<Vector2Int> _chunkColumnsCache = new HashSet<Vector2Int>();
        public IReadOnlyCollection<Vector2Int> chunkColumnsCache => _chunkColumnsCache;

        private Dictionary<Vector3Int, BlockType[]> _chunksDataCacheLookup = new Dictionary<Vector3Int, BlockType[]>();
        public IReadOnlyDictionary<Vector3Int, BlockType[]> chunksDataCacheLookup => _chunksDataCacheLookup;

        private static WorldDataModel _instance;
        public static WorldDataModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WorldDataModel();
                }
                return _instance;
            }
        }

        public void ClearRuntimeChunkData()
        {
            _chunks.Clear();
            _chunkColumns.Clear();
        }

        public void AddChunk(Vector3Int coordinate)
        {
            _chunks.Add(coordinate);
        }

        public void AddChunkToCache(Vector3Int coordinate)
        {
            _chunksCache.Add(coordinate);
        }

        public void AddChunkColumn(Vector2Int coordinate)
        {
            _chunkColumns.Add(coordinate);
            _chunkColumnsCache.Add(coordinate);
        }

        public void AddChunkToLookup(Vector3Int coordinate, Chunk chunk)
        {
            _chunksLookup.Add(coordinate, chunk);
        }

        public void AddChunkColumnToCache(Vector2Int coordinate)
        {
            _chunkColumnsCache.Add(coordinate);
        }

        public void AddChunkDataToLookupCache(Vector3Int coordinate, BlockType[] chunkData)
        {
            _chunksDataCacheLookup.Add(coordinate, chunkData);
        }

        public Chunk GetChunk(Vector3Int coordinate)
        {
            return _chunksLookup[coordinate];
        }

        /// <summary>
        /// a chunk is active when it was generated during the current session
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public bool IsChunkActive(Vector3Int coordinate)
        {
            return _chunks.Contains(coordinate);
        }

        /// <summary>
        /// dumps model content to console for debugging purposes
        /// </summary>
        public void DumpModelToConsole()
        {
            StringBuilder output = new StringBuilder("WorldDataModel:");

            //runtime
            output.AppendLine("\nchunks");
            foreach (Vector3Int chunkCoordinate in _chunks)
            {
                output.Append($"|{chunkCoordinate}");
            }
            output.AppendLine("\nchunksLookup");
            foreach (KeyValuePair<Vector3Int, Chunk> pair in _chunksLookup)
            {
                output.Append($"|{pair.Key}");
            }
            output.AppendLine("\ncolumns");
            foreach (Vector2Int columnCoordinate in _chunkColumns)
            {
                output.Append($"|{columnCoordinate}");
            }

            //cache
            output.AppendLine("\nchunkCache");
            foreach (Vector3Int cachedChunkCoordinate in _chunksCache)
            {
                output.Append($"|{cachedChunkCoordinate}");
            }
            output.AppendLine("\nchunkCacheLookup");
            foreach (KeyValuePair<Vector3Int, BlockType[]> pair in _chunksDataCacheLookup)
            {
                output.Append($"|{pair.Key}");
            }
            output.AppendLine("\ncolumnsCache");
            foreach (Vector2Int cachedColumnCoordinate in _chunkColumnsCache)
            {
                output.Append($"|{cachedColumnCoordinate}");
            }

            Debug.Log($"{output}");
        }

        internal bool IsChunkInCache(Vector3Int chunkCoordinate)
        {
            return _chunksCache.Contains(chunkCoordinate) && _chunksDataCacheLookup.ContainsKey(chunkCoordinate);
        }
    }
}
