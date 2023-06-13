﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VoxelWorld
{
    /// <summary>
    /// hold the data structures for the world and provides access to world data
    /// </summary>
    public class World : MonoBehaviour
    {
        //TODO configuration in a scriptable object
        [Header("World Configuration")]

        [Tooltip("how many chunks does the world consist of initially")]
        public Vector3Int worldDimensions = new Vector3Int(5, 5, 5);
        public Vector3Int extraWorldDimensions = new Vector3Int(10, 5, 10);

        [Tooltip("if a block is above the surface and below this value it will be water, otherwise air")]
        public int waterLevel;

        [Tooltip("radius around the player in which new chunk columns are added, value is number of chunks")]
        public int chunkColumnDrawRadius = 3;

        /// <summary>
        /// block count in a single chunk
        /// </summary>
        public static Vector3Int chunkDimensions = new Vector3Int(10, 10, 10);

        public bool loadFromFile = false;

        [Header("References")]
        public GameObject chunkPrefab;
        public GameObject mainCamera;
        public Slider loadingBar;

        public static PerlinSettings surfaceSettings;
        public PerlinGrapher surface;

        public static PerlinSettings stoneSettings;
        public PerlinGrapher stone;

        public static PerlinSettings diamondTopSettings;
        public PerlinGrapher diamondTop;

        public static PerlinSettings diamondBottomSettings;
        public PerlinGrapher diamondBottom;

        public static Perlin3DSettings caveSettings;
        public Perlin3DGrapher caves;

        public static Perlin3DSettings treeSettings;
        public Perlin3DGrapher trees;

        public Player player;

        /// keeps track of which chunks have been created already
        public HashSet<Vector3Int> createdChunks = new HashSet<Vector3Int>();

        /// keeps track of the created chunk columns, position in world coordinates
        public HashSet<Vector2Int> createdChunkColumns = new HashSet<Vector2Int>();

        /// lookup for all created chunks
        public Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

        // at which player position did we last trigger the addition of new chunks
        public Vector3 lastPlayerPositionTriggeringNewChunks;

        // holds coroutines for building chunks and hiding chunk columns
        Queue<IEnumerator> buildQueue = new Queue<IEnumerator>();

        private WaitForSeconds waitFor500Ms = new WaitForSeconds(0.5f);
        private WaitForSeconds waitFor3Seconds = new WaitForSeconds(3);
        private WaitForSeconds waitFor100ms = new WaitForSeconds(0.1f);

        

        private System.Diagnostics.Stopwatch stopwatchBuildWorld = new System.Diagnostics.Stopwatch();
        private int createdCompletelyNewChunksCount;
        private System.Diagnostics.Stopwatch stopwatchChunkGeneration = new System.Diagnostics.Stopwatch();
        private List<long> chunkGenerationTimes = new List<long>();

        // Use this for initialization
        private void Start()
        {
            surfaceSettings = new PerlinSettings(surface.heightScale, surface.scale, surface.octaves, surface.heightOffset, surface.probability);
            stoneSettings = new PerlinSettings(stone.heightScale, stone.scale, stone.octaves, stone.heightOffset, stone.probability);
            diamondTopSettings = new PerlinSettings(diamondTop.heightScale, diamondTop.scale, diamondTop.octaves, diamondTop.heightOffset, diamondTop.probability);
            diamondBottomSettings = new PerlinSettings(diamondBottom.heightScale, diamondBottom.scale, diamondBottom.octaves, diamondBottom.heightOffset, diamondBottom.probability);
            caveSettings = new Perlin3DSettings(caves.heightScale, caves.scale, caves.octaves, caves.heightOffset, caves.DrawCutOff);
            treeSettings = new Perlin3DSettings(trees.heightScale, trees.scale, trees.octaves, trees.heightOffset, trees.DrawCutOff);

            if (loadFromFile)
            {
                StartCoroutine(LoadWorldFromFile());
            }
            else
            {
                StartCoroutine(BuildNewWorld());
            }
        }

        /// <summary>
        /// creates a new column of chunks at the given coordinate, which corresponds with world position currently
        /// skips creation if a chunk already exist at the position but will set its visibility to <paramref name="meshEnabled"/>
        /// </summary>
        /// <param name="worldX">world x</param>
        /// <param name="worldZ">world z</param>
        /// <param name="meshEnabled"> set the mesh visible after it has been created</param>
        private void BuildChunkColumn(int worldX, int worldZ, bool meshEnabled = true)
        {
            Debug.Log($"Activate Chunk Column at {worldX}:{worldZ}");

            for (int gridY = 0; gridY < worldDimensions.y; gridY++)
            {
                Vector3Int chunkCoordinates = new Vector3Int(worldX, gridY * chunkDimensions.y, worldZ);
                if (!createdChunks.Contains(chunkCoordinates))
                {
                    stopwatchChunkGeneration.Start();
                    GameObject chunkGO = Instantiate(chunkPrefab);
                    Chunk chunk = chunkGO.GetComponent<Chunk>();
                    chunk.CreateMeshes(chunkDimensions, chunkCoordinates, waterLevel);
                    createdChunks.Add(chunkCoordinates);
                    chunks.Add(chunkCoordinates, chunk);
                    //Debug.Log($"Chunk created at {chunkCoordinates}");
                    createdCompletelyNewChunksCount++;
                    chunkGenerationTimes.Add(stopwatchChunkGeneration.ElapsedMilliseconds);
                    stopwatchChunkGeneration.Reset();
                }

                chunks[chunkCoordinates].meshRendererSolidBlocks.enabled = meshEnabled;
                chunks[chunkCoordinates].meshRendererFluidBlocks.enabled = meshEnabled;

            }

            createdChunkColumns.Add(new Vector2Int(worldX, worldZ));
        }

        private void InitLoadingBar(float maxValue)
        {
            loadingBar.value = 0;
            loadingBar.maxValue = maxValue;
            loadingBar.gameObject.SetActive(true);
        }

        /// <summary>
        /// disables the mesh renderer of a chunk if that chunk exists already
        /// </summary>
        private void HideChunkColumn(int worldX, int worldZ)
        {
            for (int y = 0; y < worldDimensions.y; y++)
            {
                Vector3Int pos = new Vector3Int(worldX, y * chunkDimensions.y, worldZ);
                if (createdChunks.Contains(pos))
                {
                    chunks[pos].meshRendererSolidBlocks.enabled = false;
                    chunks[pos].meshRendererFluidBlocks.enabled = false;
                }
            }
        }

        public IEnumerator HealBlock(Chunk chunk, int blockIndex)
        {
            yield return waitFor3Seconds;
            if (chunk.chunkData[blockIndex] != BlockType.Air)
            {
                chunk.healthData[blockIndex] = BlockType.Nocrack;
                chunk.Redraw(waterLevel);
            }
        }

        public IEnumerator Drop(Chunk chunk, int blockIndex, int strength = 3)
        {
            if (!MeshUtils.canDrop.Contains(chunk.chunkData[blockIndex]))
            {
                yield break;
            }
            yield return waitFor100ms;
            while (true)
            {
                Vector3Int thisBlockPos = Chunk.ToBlockCoordinates(blockIndex);
                (Vector3Int chunkPosOfBelowBlock, Vector3Int adjustedBelowBlockPos) = WorldUtils.AdjustCoordinatesToGrid(chunk.coordinates, thisBlockPos + Vector3Int.down);
                int belowBlockIndex = Chunk.ToBlockIndex(adjustedBelowBlockPos);
                Chunk chunkOfBelowBlock = chunks[chunkPosOfBelowBlock];
                if (chunkOfBelowBlock != null && chunkOfBelowBlock.chunkData[belowBlockIndex] == BlockType.Air)
                {
                    //fall -> move block 1 down -> switch chunkData
                    chunkOfBelowBlock.chunkData[belowBlockIndex] = chunk.chunkData[blockIndex];
                    chunkOfBelowBlock.healthData[belowBlockIndex] = BlockType.Nocrack;

                    chunk.chunkData[blockIndex] = BlockType.Air;
                    chunk.healthData[blockIndex] = BlockType.Nocrack;

                    // test if there is a fallable block above the new air block
                    Vector3Int aboveBlock = thisBlockPos + Vector3Int.up;
                    (Vector3Int adjustedChunkPos, Vector3Int adjustedBlockPosition) = WorldUtils.AdjustCoordinatesToGrid(chunk.coordinates, aboveBlock);
                    int aboveBlockIndex = Chunk.ToBlockIndex(adjustedBlockPosition);
                    StartCoroutine(Drop(chunks[adjustedChunkPos], aboveBlockIndex));

                    yield return waitFor100ms;

                    chunk.Redraw(waterLevel);
                    if (chunkOfBelowBlock != chunk)
                    {
                        chunkOfBelowBlock.Redraw(waterLevel);
                    }

                    chunk = chunkOfBelowBlock;
                    blockIndex = belowBlockIndex;
                }
                else if (MeshUtils.canFlow.Contains(chunk.chunkData[blockIndex]))
                {
                    FlowIntoNeighbours(thisBlockPos, chunk.coordinates, Vector3Int.left, strength);
                    FlowIntoNeighbours(thisBlockPos, chunk.coordinates, Vector3Int.right, strength);
                    FlowIntoNeighbours(thisBlockPos, chunk.coordinates, Vector3Int.forward, strength);
                    FlowIntoNeighbours(thisBlockPos, chunk.coordinates, Vector3Int.back, strength);
                    yield break;
                }
                else
                {
                    yield break;
                }
            }
        }

        public void FlowIntoNeighbours(Vector3Int blockPosition, Vector3Int chunkPosition, Vector3Int neighbourDirection, int strength)
        {
            strength--;
            if (strength <= 0)
            {
                return;
            }

            Vector3Int neighbourPosition = blockPosition + neighbourDirection;
            (Vector3Int neighbourChunkPos, Vector3Int neighbourBlockPos) = WorldUtils.AdjustCoordinatesToGrid(chunkPosition, neighbourPosition);

            int neighbourBlockIndex = Chunk.ToBlockIndex(neighbourBlockPos);
            Chunk neighbourChunk = chunks[neighbourChunkPos];

            if (neighbourChunk != null && neighbourChunk.chunkData[neighbourBlockIndex] == BlockType.Air)
            {
                // flow
                Debug.Log($"Flow");
                neighbourChunk.chunkData[neighbourBlockIndex] = chunks[chunkPosition].chunkData[Chunk.ToBlockIndex(blockPosition)];
                neighbourChunk.healthData[neighbourBlockIndex] = BlockType.Nocrack;
                neighbourChunk.Redraw(waterLevel);
                StartCoroutine(Drop(neighbourChunk, neighbourBlockIndex, strength--));
            }
            else
            {
                Debug.Log($"cannot flow, neighbour {neighbourBlockPos} is of type {neighbourChunk.chunkData[neighbourBlockIndex]}");
            }
        }

        /// <summary>
        /// hooked up to UI Button
        /// </summary>
        public void SaveWorld()
        {
            FileSaver.Save(this, player);
        }

        private IEnumerator LoadWorldFromFile()
        {
            WorldData worldData = FileSaver.Load(this);
            if (worldData == null)
            {
                StartCoroutine(BuildNewWorld());
                yield break;
            }

            // populate runtime data structures with data from file
            createdChunks.Clear();
            for (int i = 0; i < worldData.createdChunksCoordinates.Length; i += 3)
            {
                //TODO exclude invisible chunks

                createdChunks.Add(new Vector3Int(
                    worldData.createdChunksCoordinates[i],
                    worldData.createdChunksCoordinates[i + 1],
                    worldData.createdChunksCoordinates[i + 2]));
            }

            createdChunkColumns.Clear();
            for (int i = 0; i < worldData.chunkColumnValues.Length; i += 2)
            {
                //TODO exclude invisible columns

                createdChunkColumns.Add(new Vector2Int(
                    worldData.chunkColumnValues[i],
                    worldData.chunkColumnValues[i + 1]));
            }

            int blockCount = chunkDimensions.x * chunkDimensions.y * chunkDimensions.z;
            InitLoadingBar(createdChunks.Count);
            int chunkDataIndex = 0;
            int chunkIndex = 0;
            foreach (Vector3Int chunkPos in createdChunks)
            {
                GameObject chunkGO = Instantiate(chunkPrefab);
                chunkGO.name = $"Chunk_{chunkPos.x}_{chunkPos.y}_{chunkPos.z}";
                Chunk chunk = chunkGO.GetComponent<Chunk>();

                chunk.chunkData = new BlockType[blockCount];
                chunk.healthData = new BlockType[blockCount];

                for (int i = 0; i < blockCount; i++)
                {
                    chunk.chunkData[i] = (BlockType)worldData.allChunkData[chunkDataIndex];
                    chunk.healthData[i] = BlockType.Nocrack;
                    chunkDataIndex++;
                }

                chunk.CreateMeshes(chunkDimensions, chunkPos, waterLevel, false);
                chunks.Add(chunkPos, chunk);
                chunk.Redraw(waterLevel);
                chunk.meshRendererSolidBlocks.enabled = worldData.chunkVisibility[chunkIndex];
                chunk.meshRendererFluidBlocks.enabled = worldData.chunkVisibility[chunkIndex];
                chunkIndex++;
                loadingBar.value++;
                yield return null;
            }

            player.position = new Vector3(worldData.fpcX, worldData.fpcY, worldData.fpcZ);
            mainCamera.SetActive(false);
            player.SetActive(true);
            lastPlayerPositionTriggeringNewChunks = Vector3Int.CeilToInt(player.position);
            loadingBar.gameObject.SetActive(false);

            StartCoroutine(BuildQueueProcessor());
            StartCoroutine(UpdateWorldMonitor());
        }

        /// <summary>
        /// adds one chunk column every frame until the whole initial world is built
        /// starts coroutines for world updating when player moves after that
        /// </summary>
        private IEnumerator BuildNewWorld()
        {
            Debug.Log($"#World# Initial World building started");

            InitLoadingBar(worldDimensions.x * worldDimensions.z);

            yield return StartCoroutine(BuildChunkColumns(new Vector3(0, 0, 0), chunkColumnDrawRadius * chunkDimensions.x));

            loadingBar.gameObject.SetActive(false);

            player.Spawn();

            StartCoroutine(BuildQueueProcessor());
            StartCoroutine(UpdateWorldMonitor());
            //StartCoroutine(BuildExtraWorld());
        }

        /// <summary>
        /// BuildCoordinator monitors the build queue for tasks and runs them one after the other, one job per frame
        /// </summary>
        private IEnumerator BuildQueueProcessor()
        {
            Debug.Log("Start monitoring Build Queue");

            while (true)
            {
                while (buildQueue.Count > 0)
                {
                    Debug.Log("process task from build queue");
                    yield return StartCoroutine(buildQueue.Dequeue());
                }
                yield return null;
            }
        }

        // checks every 500ms if the player has moved far enough to trigger the creation of new chunks
        private IEnumerator UpdateWorldMonitor()
        {
            Debug.Log("Start monitoring player position");
            while (true)
            {
                //TODO currently only works if chunk dimensions are uniform
                int minWalkDistance = chunkDimensions.x;
                float walkedDistance = (lastPlayerPositionTriggeringNewChunks - player.position).magnitude;
                if (walkedDistance > minWalkDistance)
                {
                    lastPlayerPositionTriggeringNewChunks = player.position;

                    (Vector3Int chunkCoordinate, Vector3Int blockCoordinate) = WorldUtils.FromWorldPosToCoordinates(player.position);
                    Vector2Int chunkColumnCoordinates = new Vector2Int(chunkCoordinate.x, chunkCoordinate.z);
                    buildQueue.Enqueue(HideChunkColumns(chunkColumnCoordinates));

                    buildQueue.Enqueue(BuildChunkColumns(player.position, chunkColumnDrawRadius * chunkDimensions.x));
                }
                yield return waitFor500Ms;
            }
        }

        private IEnumerator BuildExtraWorld()
        {
            for (int z = 0; z < worldDimensions.z + extraWorldDimensions.z; z++)
            {
                for (int x = 0; x < worldDimensions.x + extraWorldDimensions.x; x++)
                {
                    if (x >= worldDimensions.x || z >= worldDimensions.z)
                    {
                        BuildChunkColumn(x * chunkDimensions.x, z * chunkDimensions.z, false);
                        yield return null;
                    }
                }
            }
        }

        /// <summary>
        /// hides all chunk columns which are beyond the draw radius
        /// </summary>
        /// <param name="currentChunkColumnCoordinate">the chunk column coordinate of the current player position</param> 
        private IEnumerator HideChunkColumns(Vector2Int currentChunkColumnCoordinate)
        {
            Debug.Log($"Hide columns around {currentChunkColumnCoordinate.x}:{currentChunkColumnCoordinate.y}");

            //TODO Improvement: we don't need to iterate all columns, only the visible ones
            foreach (Vector2Int column in createdChunkColumns)
            {
                if ((column - currentChunkColumnCoordinate).magnitude > chunkColumnDrawRadius * chunkDimensions.x)
                {
                    HideChunkColumn(column.x, column.y);
                }
            }
            yield return null;
        }

        private IEnumerator BuildChunkColumns(Vector3 playerPosition, int buildRadius)
        {
            stopwatchBuildWorld.Start();

            (Vector3Int chunkCoordinates, Vector3Int blockCoordinates) = WorldUtils.FromWorldPosToCoordinates(playerPosition);

            int startX = chunkCoordinates.x - buildRadius;
            int stopX = chunkCoordinates.x + buildRadius;
            int startZ = chunkCoordinates.z + buildRadius;
            int stopZ = chunkCoordinates.z - buildRadius;
            for (int z = startZ; z >= stopZ; z -= 10)
            {
                for (int x = startX; x <= stopX; x += 10)
                {
                    Vector3Int possibleNewChunkCoordinate = new Vector3Int(x, chunkCoordinates.y, z);
                    if (Vector3Int.Distance(possibleNewChunkCoordinate, chunkCoordinates) <= buildRadius)
                    {
                        BuildChunkColumn(x, z);
                        yield return null;
                    }
                }
            }

            // all debug related
            Debug.Log($"Building of chunk columns finished after {stopwatchBuildWorld.ElapsedMilliseconds}ms. {createdCompletelyNewChunksCount} Chunks were created.");
            stopwatchBuildWorld.Reset();
            if (chunkGenerationTimes.Count > 0)
            {
                Debug.Log($"avg chunk generation time: {chunkGenerationTimes.Average()}");
            }
            chunkGenerationTimes.Clear();
            createdCompletelyNewChunksCount = 0;
        }
    }
}