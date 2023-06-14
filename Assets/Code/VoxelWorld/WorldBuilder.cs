using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace VoxelWorld
{
    /// <summary>
    /// generates the world, holds some world related data structures
    /// </summary>
    public class WorldBuilder : MonoBehaviour
    {
        //TODO configuration in a scriptable object
        [Header("World Configuration")]

        [Tooltip("how many chunks does the world consist of initially")]
        public Vector3Int worldDimensions = new Vector3Int(5, 5, 5);//TODO longer needed? for what?
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

        [SerializeField]
        private Player player;
        [SerializeField]
        private WorldUpdater worldUpdater;

        //Events
        public UnityEvent<int> worldBuildingStarted;
        public UnityEvent<int> worldBuildingUpdated;
        public UnityEvent worldBuildingEnded;

        /// keeps track of which chunks have been created already
        public HashSet<Vector3Int> createdChunks = new HashSet<Vector3Int>();

        /// keeps track of the created chunk columns, position in world coordinates
        public HashSet<Vector2Int> createdChunkColumns = new HashSet<Vector2Int>();

        /// lookup for all created chunks
        public Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

        private System.Diagnostics.Stopwatch stopwatchBuildWorld = new System.Diagnostics.Stopwatch();
        private int createdCompletelyNewChunksCount;
        private System.Diagnostics.Stopwatch stopwatchChunkGeneration = new System.Diagnostics.Stopwatch();
        private List<long> chunkGenerationTimes = new List<long>();

        private int _initialChunkColumnCount;

        // Use this for initialization
        private void Start()
        {
            surfaceSettings = new PerlinSettings(surface.heightScale, surface.scale, surface.octaves, surface.heightOffset, surface.probability);
            stoneSettings = new PerlinSettings(stone.heightScale, stone.scale, stone.octaves, stone.heightOffset, stone.probability);
            diamondTopSettings = new PerlinSettings(diamondTop.heightScale, diamondTop.scale, diamondTop.octaves, diamondTop.heightOffset, diamondTop.probability);
            diamondBottomSettings = new PerlinSettings(diamondBottom.heightScale, diamondBottom.scale, diamondBottom.octaves, diamondBottom.heightOffset, diamondBottom.probability);
            caveSettings = new Perlin3DSettings(caves.heightScale, caves.scale, caves.octaves, caves.heightOffset, caves.DrawCutOff);
            treeSettings = new Perlin3DSettings(trees.heightScale, trees.scale, trees.octaves, trees.heightOffset, trees.DrawCutOff);

            CalculateInitialChunkColumnCount();

            if (loadFromFile)
            {
                StartCoroutine(BuildWorldFromSaveFile());
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

                    BuildChunk(chunkCoordinates);
                    
                    createdCompletelyNewChunksCount++;
                    chunkGenerationTimes.Add(stopwatchChunkGeneration.ElapsedMilliseconds);
                    stopwatchChunkGeneration.Reset();
                }

                chunks[chunkCoordinates].meshRendererSolidBlocks.enabled = meshEnabled;
                chunks[chunkCoordinates].meshRendererFluidBlocks.enabled = meshEnabled;
            }

            createdChunkColumns.Add(new Vector2Int(worldX, worldZ));
        }

        private void BuildChunk(Vector3Int coordinates)
        {
            GameObject chunkGO = Instantiate(chunkPrefab);
            Chunk chunk = chunkGO.GetComponent<Chunk>();
            chunk.CreateMeshes(chunkDimensions, coordinates, waterLevel);
            createdChunks.Add(coordinates);
            chunks.Add(coordinates, chunk);
        }

        private void CalculateInitialChunkColumnCount()
        {
            // calculate the chunk count to create beforehand,there is no easy formula for this, seems to be a complex mathematical problem
            // so this just samples points in a circle and checks if its within the given radius or not
            // https://en.wikipedia.org/wiki/Gauss_circle_problem
            for (int x = -chunkColumnDrawRadius; x <= chunkColumnDrawRadius * 2; x++)
            {
                for (int y = -chunkColumnDrawRadius; y <= chunkColumnDrawRadius * 2; y++)
                {
                    if (Vector2.Distance(Vector2.zero, new Vector2(x, y)) <= chunkColumnDrawRadius)
                    {
                        _initialChunkColumnCount++;
                    }
                }
            }
        }

        /// <summary>
        /// hooked up to UI Button
        /// </summary>
        public void SaveWorld()
        {
            FileSaver.Save(this, player);
        }

        private IEnumerator BuildWorldFromSaveFile()
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

            worldBuildingStarted.Invoke(_initialChunkColumnCount * 3);

            int blockCount = chunkDimensions.x * chunkDimensions.y * chunkDimensions.z;
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
                worldBuildingUpdated.Invoke(1);
                yield return null;
            }

            worldBuildingEnded.Invoke();
            player.position = new Vector3(worldData.fpcX, worldData.fpcY, worldData.fpcZ);
            mainCamera.SetActive(false);
            player.SetActive(true);
            worldUpdater.lastPlayerPositionTriggeringNewChunks = Vector3Int.CeilToInt(player.position);

            StartCoroutine(worldUpdater.BuildQueueProcessor());
            StartCoroutine(worldUpdater.UpdateWorldMonitor());
        }

        /// <summary>
        /// adds one chunk column every frame until the whole initial world is built
        /// starts coroutines for world updating when player moves after that
        /// </summary>
        private IEnumerator BuildNewWorld()
        {
            Debug.Log($"#World# Initial World building started");


            worldBuildingStarted.Invoke(_initialChunkColumnCount);

            yield return StartCoroutine(BuildChunkColumns(new Vector3(0, 0, 0), chunkColumnDrawRadius * chunkDimensions.x));

            worldBuildingEnded.Invoke();

            player.Spawn();

            StartCoroutine(worldUpdater.BuildQueueProcessor());
            StartCoroutine(worldUpdater.UpdateWorldMonitor());
            //StartCoroutine(BuildExtraWorld());
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

        public IEnumerator BuildChunkColumns(Vector3 playerPosition, int buildRadius)
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
                        worldBuildingUpdated.Invoke(1);
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