using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Events;

namespace VoxelWorld
{
    /// <summary>
    /// generates the world, holds some world related data structures
    /// </summary>
    public class WorldBuilder : MonoBehaviour
    {
        static readonly ProfilerMarker profilerMarkerBuildSavedChunk = new ProfilerMarker("BuildSavedWorld");
        static readonly ProfilerMarker profilerMarkerCreateMeshes = new ProfilerMarker("CreateMeshes");

        //Events
        public UnityEvent<int> worldBuildingStarted;
        public UnityEvent<int> worldBuildingUpdated;
        public UnityEvent worldBuildingEnded;

        //TODO configuration in a scriptable object
        [Header("World Configuration")]

        //TODO only y (height) still used and defunct buildExtraWorld
        [Tooltip("how many chunks does the world consist of initially")]
        public Vector3Int worldDimensions = new Vector3Int(5, 5, 5);
        public Vector3Int extraWorldDimensions = new Vector3Int(10, 5, 10);

        [Tooltip("if a block is above the surface and below this value it will be water, otherwise air")]
        public int waterLevel;

        [Tooltip("radius around the player in which new chunk columns are added, value is number of chunks")]
        public int chunkColumnDrawRadius = 3;

        /// <summary>
        /// unit is number of blocks
        /// </summary>
        public static Vector3Int chunkDimensions = new Vector3Int(10, 10, 10);
        public static int blockCountPerChunk = chunkDimensions.x * chunkDimensions.y * chunkDimensions.z;

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

        private System.Diagnostics.Stopwatch stopwatchBuildWorld = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch stopwatchChunkGeneration = new System.Diagnostics.Stopwatch();
        private List<long> chunkGenerationTimes = new List<long>();
        private int createdNewChunksCount;
        private System.Diagnostics.Stopwatch stopwatchCachedChunkGeneration = new System.Diagnostics.Stopwatch();
        private List<long> cachedChunkGenerationTimes = new List<long>();
        private int createdCachedChunks;

        private int _initialChunkColumnCount;

        private WorldDataModel _worldModel;

        // Use this for initialization
        private void Start()
        {
            _worldModel = WorldDataModel.Instance;

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
        /// hooked up to UI Button
        /// </summary>
        public void SaveWorld()
        {
            FileSaver.Save(worldDimensions, player);
        }

        private ProfilerMarker _profilerMarkerBuildChunk = new("Build Chunk");

        /// <summary>
        /// creates a new column of chunks at the given coordinate, which corresponds with world position currently
        /// skips creation if a chunk already exist at the position but will set its visibility to <paramref name="meshEnabled"/>
        /// </summary>
        /// <param name="worldX">world x</param>
        /// <param name="worldZ">world z</param>
        /// <param name="meshEnabled"> set the mesh visible after it has been created</param>
        private IEnumerator BuildChunkColumn(int worldX, int worldZ, bool meshEnabled = true)
        {
            Debug.Log($"Activate Chunk Column at {worldX}:{worldZ}");

            for (int gridY = worldDimensions.y - 1; gridY >= 0; gridY--)
            {
                _profilerMarkerBuildChunk.Begin();
                Vector3Int chunkCoordinate = new Vector3Int(worldX, gridY * chunkDimensions.y, worldZ);

                // only create a chunk when it was not already created, otherwise just switch visibility
                if (!_worldModel.IsChunkActive(chunkCoordinate))
                {
                    if (!_worldModel.IsChunkInCache(chunkCoordinate))
                    {
                        stopwatchChunkGeneration.Start();

                        BuildChunk(chunkCoordinate);

                        createdNewChunksCount++;
                        chunkGenerationTimes.Add(stopwatchChunkGeneration.ElapsedMilliseconds);
                        stopwatchChunkGeneration.Reset();
                    }
                    else
                    {
                        stopwatchCachedChunkGeneration.Start();

                        BuildChunkFromCachedData(chunkCoordinate);

                        createdCachedChunks++;
                        cachedChunkGenerationTimes.Add(stopwatchCachedChunkGeneration.ElapsedMilliseconds);
                        stopwatchCachedChunkGeneration.Reset();
                    }
                }

                _worldModel.GetChunk(chunkCoordinate).meshRendererSolidBlocks.enabled = meshEnabled;

                _profilerMarkerBuildChunk.End();
                yield return null;
            }

            _worldModel.AddChunkColumn(new Vector2Int(worldX, worldZ));
        }

        private void BuildChunk(Vector3Int coordinate)
        {
            GameObject chunkGO = Instantiate(chunkPrefab);
            Chunk chunk = chunkGO.GetComponent<Chunk>();
            chunk.CreateChunkMeshes(coordinate, waterLevel);
            _worldModel.AddChunk(coordinate);
            _worldModel.AddChunkToLookup(coordinate, chunk);
            _worldModel.AddChunkToCache(coordinate);
            _worldModel.AddChunkDataToLookupCache(coordinate, chunk.chunkData);
        }

        private void BuildChunkFromCachedData(Vector3Int coordinate)
        {
            GameObject chunkGO = Instantiate(chunkPrefab);
            Chunk chunk = chunkGO.GetComponent<Chunk>();

            BlockType[] chunkData = _worldModel.chunksDataCacheLookup[coordinate];
            BlockType[] healthData = new BlockType[WorldBuilder.blockCountPerChunk];
            for (int i = 0; i < healthData.Length; i++)
            {
                healthData[i] = BlockType.Nocrack;
            }
            chunk.chunkData = chunkData;
            chunk.healthData = healthData;

            chunk.CreateChunkMeshes(coordinate, waterLevel, false);
            _worldModel.AddChunk(coordinate);
            _worldModel.AddChunkToLookup(coordinate, chunk);
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

        private IEnumerator BuildWorldFromSaveFile()
        {

            Debug.Log($"Building World from save file started");
            stopwatchBuildWorld.Start();

            SaveFileData worldData = FileSaver.Load(this.worldDimensions);
            if (worldData == null)
            {
                StartCoroutine(BuildNewWorld());
                yield break;
            }

            Debug.Log($"{worldData.chunkCoordinates.Length / 3} Chunks in Data");

            _worldModel.ClearRuntimeChunkData();
            PopulateChunks(worldData, out List<int> createdChunkIndices);
            PopulateGeneratedChunkColumns(worldData);

            worldBuildingStarted.Invoke(_initialChunkColumnCount * 3);

            int chunkDataIndex = 0;
            int index = 0;
            foreach (Vector3Int coordinate in _worldModel.chunks)
            {
                //debug
                profilerMarkerBuildSavedChunk.Begin();
                stopwatchChunkGeneration.Start();

                GameObject chunkGO = Instantiate(chunkPrefab);
                chunkGO.name = $"Chunk_{coordinate.x}_{coordinate.y}_{coordinate.z}";
                Chunk chunk = chunkGO.GetComponent<Chunk>();

                chunk.chunkData = new BlockType[blockCountPerChunk];
                chunk.healthData = new BlockType[blockCountPerChunk];

                chunkDataIndex = createdChunkIndices[index] * blockCountPerChunk;
                for (int i = 0; i < blockCountPerChunk; i++)
                {
                    chunk.chunkData[i] = (BlockType)worldData.chunksData[chunkDataIndex];
                    chunk.healthData[i] = BlockType.Nocrack;
                    chunkDataIndex++;
                }

                profilerMarkerCreateMeshes.Begin();
                chunk.CreateChunkMeshes(coordinate, waterLevel, false);
                profilerMarkerCreateMeshes.End();
                _worldModel.AddChunkToLookup(coordinate, chunk);

                chunk.meshRendererSolidBlocks.enabled = true;
                chunk.meshRendererFluidBlocks.enabled = true;

                index++;
                worldBuildingUpdated.Invoke(1);

                //debug
                chunkGenerationTimes.Add(stopwatchChunkGeneration.ElapsedMilliseconds);
                stopwatchChunkGeneration.Reset();
                profilerMarkerBuildSavedChunk.End();


                yield return null;
            }

            Debug.Log($"Building World from file finished after {stopwatchBuildWorld.ElapsedMilliseconds}ms. {index} Chunks were created");
            if (chunkGenerationTimes.Count > 0)
            {
                Debug.Log($"avg chunk generation time: {chunkGenerationTimes.Average()}");
                chunkGenerationTimes.Clear();
            }

            stopwatchBuildWorld.Reset();

            worldBuildingEnded.Invoke();
            player.position = new Vector3(worldData.playerPositionX, worldData.playerPositionY, worldData.playerPositionZ);
            mainCamera.SetActive(false);
            player.SetActive(true);
            worldUpdater.lastPlayerPositionTriggeringNewChunks = Vector3Int.CeilToInt(player.position);

            StartCoroutine(worldUpdater.BuildQueueProcessor());
            StartCoroutine(worldUpdater.UpdateWorldMonitor());
            //StartCoroutine(BuildExtraWorld());//TODO fix and enable this at a later point
        }

        private void PopulateGeneratedChunkColumns(SaveFileData worldData)
        {
            int index = 0;
            for (int i = 0; i < worldData.chunkColumns.Length; i += 2)
            {
                Vector2Int currentChunkColumn = new Vector2Int(
                        worldData.chunkColumns[i],
                        worldData.chunkColumns[i + 1]);

                if (worldData.chunkVisibility[index] == true)
                {
                    _worldModel.AddChunkColumn(currentChunkColumn);
                }
                else
                {
                    _worldModel.AddChunkColumnToCache(currentChunkColumn);
                }

                index += 3;
            }
        }

        /// <summary>
        /// writes the visible chunks into the runtime data structures and the invisible chunks into the chunk cache
        /// </summary>
        private void PopulateChunks(SaveFileData worldData, out List<int> createdChunkIndices)
        {
            createdChunkIndices = new List<int>();
            int chunkIndex = 0;
            for (int i = 0; i < worldData.chunkCoordinates.Length; i += 3)
            {
                Vector3Int coordinate = new Vector3Int(
                            worldData.chunkCoordinates[i],
                            worldData.chunkCoordinates[i + 1],
                            worldData.chunkCoordinates[i + 2]);

                if (worldData.chunkVisibility[chunkIndex] == true)
                {
                    _worldModel.AddChunk(coordinate);
                    createdChunkIndices.Add(chunkIndex);
                    _worldModel.AddChunkToCache(coordinate);
                    AddChunkDataToLookupCache(worldData, chunkIndex, coordinate);
                }
                else
                {
                    _worldModel.AddChunkToCache(coordinate);
                    AddChunkDataToLookupCache(worldData, chunkIndex, coordinate);
                }

                chunkIndex++;
            }
        }

        private void AddChunkDataToLookupCache(SaveFileData worldData, int chunkIndex, Vector3Int coordinate)
        {
            BlockType[] chunkData = new BlockType[blockCountPerChunk];

            for (int blockIndex = 0; blockIndex < blockCountPerChunk; blockIndex++)
            {
                chunkData[blockIndex] = (BlockType)worldData.chunksData[chunkIndex * blockCountPerChunk + blockIndex];
            }

            _worldModel.AddChunkDataToLookupCache(coordinate, chunkData);
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

            yield return Resources.UnloadUnusedAssets();

            worldBuildingEnded.Invoke();

            player.Spawn();

            StartCoroutine(worldUpdater.BuildQueueProcessor());
            StartCoroutine(worldUpdater.UpdateWorldMonitor());
            //StartCoroutine(BuildExtraWorld());//TODO fix and enable this at a later point
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
        /// creates a whole column of chunks, 1 chunk per frame, used during world updating
        /// </summary>
        /// <param name="playerPosition"></param>
        /// <param name="buildRadius">in block count</param>
        /// <returns></returns>
        public IEnumerator BuildChunkColumns(Vector3 playerPosition, int buildRadius)
        {
            stopwatchBuildWorld.Start();

            (Vector3Int chunkCoordinates, Vector3Int blockCoordinates) = WorldUtils.FromWorldPosToCoordinates(playerPosition);

            int startX = chunkCoordinates.x - buildRadius;
            int stopX = chunkCoordinates.x + buildRadius;
            int startZ = chunkCoordinates.z + buildRadius;
            int stopZ = chunkCoordinates.z - buildRadius;
            for (int z = startZ; z >= stopZ; z -= chunkDimensions.z)
            {
                for (int x = startX; x <= stopX; x += chunkDimensions.x)
                {
                    Vector3Int possibleNewChunkCoordinate = new Vector3Int(x, chunkCoordinates.y, z);
                    if (Vector3Int.Distance(possibleNewChunkCoordinate, chunkCoordinates) <= buildRadius)
                    {
                        yield return StartCoroutine(BuildChunkColumn(x, z));
                        worldBuildingUpdated.Invoke(1);
                        yield return null;
                    }
                }
            }

            yield return Resources.UnloadUnusedAssets();

            PrintChunkColumnGenerationDebugInfo();
        }

        private void PrintChunkColumnGenerationDebugInfo()
        {
            // all debug related
            Debug.Log($"Building of chunk columns finished. {createdNewChunksCount} Chunks were created. {createdCachedChunks} from Cache");
            stopwatchBuildWorld.Reset();
            if (chunkGenerationTimes.Count > 0)
            {
                Debug.Log($"avg chunk generation time: {chunkGenerationTimes.Average()}");
            }
            chunkGenerationTimes.Clear();
            createdNewChunksCount = 0;

            if (cachedChunkGenerationTimes.Count > 0)
            {
                Debug.Log($"avg cached chunk generation time: {cachedChunkGenerationTimes.Average()}");
            }
            cachedChunkGenerationTimes.Clear();
            createdCachedChunks = 0;
        }
    }
}