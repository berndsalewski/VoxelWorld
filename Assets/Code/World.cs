using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class World : MonoBehaviour
{
    [Header("World Configuration")]
    // these values mean number of chunk (columns)
    public static Vector3Int worldDimensions = new Vector3Int(5, 5, 5);
    public static Vector3Int extraWorldDimensions = new Vector3Int(10, 5, 10);

    // block count in a single chunk
    public static Vector3Int chunkDimensions = new Vector3Int(10, 10, 10);

    public bool loadFromFile = false;

    [Header("References")]
    public GameObject chunkPrefab;
    public GameObject mainCamera;
    public GameObject firstPersonController;
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

    // ChunkChecker: keeps track of which chunks have been created already
    public HashSet<Vector3Int> createdChunks = new HashSet<Vector3Int>();

    // chunkColumns: keeps track of the created chunk columns, position in world coordinates
    public HashSet<Vector2Int> createdChunkColumns = new HashSet<Vector2Int>();

    // lookup for all created chunks
    public Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    // lastBuildPosition: at which player position did we last trigger the addition of new chunks
    Vector3Int lastPlayerPositionTriggeringNewChunks;

    // holds coroutines for building chunks and hiding chunk columns
    Queue<IEnumerator> buildQueue = new Queue<IEnumerator>();

    // drawRadius radius around the player in which new chunk columns are added, value is number of chunks
    private int chunkColumnDrawRadius = 3;

    private WaitForSeconds waitFor500Ms = new WaitForSeconds(0.5f);

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
            StartCoroutine(BuildWorld());
        }
    }

    private MeshUtils.BlockType buildBlockType = MeshUtils.BlockType.Dirt;
    /// <summary>
    /// used by Buttons in Scene
    /// </summary>
    /// <param name="type"></param>
    public void SetBuildType(int type)
    {
        buildBlockType = (MeshUtils.BlockType)type;
        Debug.Log($"Build Type: {buildBlockType}");
    }

    /// <summary>
    /// calculates relative chunk position from the index of a block
    /// </summary>
    /// <param name="i">block index</param>
    /// <returns></returns>
    public static Vector3Int FromFlat(int i)
    {
        // x = i % width
        // y = (i / width) % height
        // z = i / (width * height)

        return new Vector3Int(
            i % chunkDimensions.x,
            (i / chunkDimensions.x) % chunkDimensions.y,
            i / (chunkDimensions.x * chunkDimensions.y));
    }

    // flat array [x + WIDTH * (y + DEPTH * z)] = Original[x,y,z]
    // calculates index of a block from vector position 
    public static int ToFlat(Vector3Int pos)
    {
        return pos.x + chunkDimensions.x * (pos.y + chunkDimensions.z * pos.z);
    }

    // System.Tuple<Vector3Int, Vector3Int> can be written as (Vector3Int, Vector3Int) since C#7
    // supposed to wrap indices when we cross from one chunk to another one.poor naming of method
    // formerly: GetWorldNeighbour, better name would probably FromWorldPosToLocal or similar
    public (Vector3Int, Vector3Int) FromWorldPosToLocal(Vector3 worldPos)
    {
        Vector3Int chunkPosition = new Vector3Int();
        chunkPosition.x = (int)((worldPos.x + 0.5f) / chunkDimensions.x) * chunkDimensions.x;
        chunkPosition.y = (int)((worldPos.y + 0.5f) / chunkDimensions.y) * chunkDimensions.y;
        chunkPosition.z = (int)((worldPos.z + 0.5f) / chunkDimensions.z) * chunkDimensions.z;

        Vector3Int blockPosition = new Vector3Int();
        blockPosition.x = (int)Mathf.Round(worldPos.x) - chunkPosition.x;
        blockPosition.y = (int)Mathf.Round(worldPos.y) - chunkPosition.y;
        blockPosition.z = (int)Mathf.Round(worldPos.z) - chunkPosition.z;

        return (chunkPosition, blockPosition);
    }

    /// <summary>
    /// adjust local relative to chunk block position if it crosses into neighbouring chunks
    /// </summary>
    /// <param name="chunkPos"></param>
    /// <param name="blockPos"></param>
    /// <returns></returns>
    public (Vector3Int, Vector3Int) AdjustGridPosition(Vector3Int chunkPos, Vector3Int blockPos)
    {
        Vector3Int newChunkPos = chunkPos;
        Vector3Int newBlockPos = blockPos;

        // X axis
        if (blockPos.x >= chunkDimensions.x)
        {
            newBlockPos.x = blockPos.x % chunkDimensions.x;
            newChunkPos.x += blockPos.x / chunkDimensions.x * chunkDimensions.x;
        }
        else if (blockPos.x < 0)
        {
            newBlockPos.x = Mathf.CeilToInt((float)Mathf.Abs(blockPos.x) / chunkDimensions.x) * chunkDimensions.x - Mathf.Abs(blockPos.x);
            newChunkPos.x += Mathf.FloorToInt((float)blockPos.x / chunkDimensions.x) * chunkDimensions.x;
        }

        // Y axis
        if (blockPos.y >= chunkDimensions.y)
        {
            newBlockPos.y = blockPos.y % chunkDimensions.y;
            newChunkPos.y += blockPos.y / chunkDimensions.y * chunkDimensions.y;
        }
        else if (blockPos.y < 0)
        {
            newBlockPos.y = Mathf.CeilToInt((float)Mathf.Abs(blockPos.y) / chunkDimensions.y) * chunkDimensions.y - Mathf.Abs(blockPos.y);
            newChunkPos.y += Mathf.FloorToInt((float)blockPos.y / chunkDimensions.y) * chunkDimensions.y;
        }

        // Z axis
        if (blockPos.z >= chunkDimensions.z)
        {
            newBlockPos.z = blockPos.z % chunkDimensions.z;
            newChunkPos.z += blockPos.z / chunkDimensions.z * chunkDimensions.z;
        }
        else if (blockPos.z < 0)
        {
            newBlockPos.z = Mathf.CeilToInt((float)Mathf.Abs(blockPos.z) / chunkDimensions.z) * chunkDimensions.z - Mathf.Abs(blockPos.z);
            newChunkPos.z += Mathf.FloorToInt((float)blockPos.z / chunkDimensions.z) * chunkDimensions.z;
        }

        return (newChunkPos, newBlockPos);
    }


    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 10))
            {
                Vector3 hitBlock;

                if (Input.GetMouseButton(0))
                {
                    hitBlock = hit.point - hit.normal * 0.5f;
                }
                else
                {
                    hitBlock = hit.point + hit.normal * 0.5f;   
                }

                (Vector3Int chunkPosition, Vector3Int blockPosition) = FromWorldPosToLocal(hitBlock);
                Chunk thisChunk = chunks[chunkPosition];
                int currentBlockIndex = ToFlat(blockPosition);

                // delete blocks with left mousebutton
                if (Input.GetMouseButton(0))
                {
                    if (MeshUtils.blockTypeHealth[(int)thisChunk.chunkData[currentBlockIndex]] > -1)
                    {
                        thisChunk.healthData[currentBlockIndex]++;
                        if (thisChunk.healthData[currentBlockIndex] == MeshUtils.BlockType.Nocrack + MeshUtils.blockTypeHealth[(int)thisChunk.chunkData[currentBlockIndex]])
                        {
                            Debug.Log($"Delete at chunk:{thisChunk.location} blockId:{currentBlockIndex} block:{blockPosition.x}:{blockPosition.y}:{blockPosition.z}");
                            thisChunk.chunkData[currentBlockIndex] = MeshUtils.BlockType.Air;

                            //TODO
                            Vector3Int aboveBlock = blockPosition + Vector3Int.up;
                            (Vector3Int adjustedChunkPos, Vector3Int adjustedBlockPosition) = AdjustGridPosition(chunkPosition, aboveBlock);
                            int aboveBlockIndex = ToFlat(adjustedBlockPosition);
                            StartCoroutine(Drop(chunks[adjustedChunkPos], aboveBlockIndex));
                        }

                        StartCoroutine(HealBlock(thisChunk, currentBlockIndex));
                    }
                }
                // build block
                else
                {
                    Debug.Log($"Build in chunk:{thisChunk.location} blockId:{currentBlockIndex} block:{blockPosition.x}:{blockPosition.y}:{blockPosition.z}");
                    thisChunk.chunkData[currentBlockIndex] = buildBlockType;
                    thisChunk.healthData[currentBlockIndex] = MeshUtils.BlockType.Nocrack;
                    StartCoroutine(Drop(thisChunk, currentBlockIndex));
                }

                RedrawChunk(thisChunk);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worldX">world x</param>
    /// <param name="worldZ">world z</param>
    /// <param name="meshEnabled"> set the mesh visible after it has been created</param>
    private void BuildChunkColumn(int worldX, int worldZ, bool meshEnabled = true)
    {
        Debug.Log($"Show Chunk Column {worldX}:{worldZ}");

        for (int gridY = 0; gridY < worldDimensions.y; gridY++)
        {
            Vector3Int position = new Vector3Int(worldX, gridY * chunkDimensions.y, worldZ);
            if (!createdChunks.Contains(position))
            {
                GameObject chunkGO = Instantiate(chunkPrefab);
                Chunk chunk = chunkGO.GetComponent<Chunk>();
                chunk.CreateChunk(chunkDimensions, position);
                createdChunks.Add(position);
                chunks.Add(position, chunk);
                Debug.Log($"Chunk created at {position}");
            }

            chunks[position].meshRendererSolid.enabled = meshEnabled;
            chunks[position].meshRendererFluid.enabled = meshEnabled;

        }

        createdChunkColumns.Add(new Vector2Int(worldX, worldZ));
    }

    /// <summary>
    /// spawns the player in the initial center of the world in the xz plane
    /// </summary>
    private void SpawnPlayer()
    {
        mainCamera.SetActive(false);
        float posX = worldDimensions.x * chunkDimensions.x * 0.5f;
        float posZ = worldDimensions.z * chunkDimensions.z * 0.5f;

        // get the height of the surface at the spawn position
        float posY = MeshUtils.fBM(posX, posZ, surfaceSettings.octaves, surfaceSettings.scale, surfaceSettings.heightScale, surfaceSettings.heightOffset);

        float verticalOffset = 3;
        firstPersonController.transform.position = new Vector3(posX, posY + verticalOffset, posZ);
        firstPersonController.SetActive(true);

        lastPlayerPositionTriggeringNewChunks = Vector3Int.CeilToInt(firstPersonController.transform.position);

        Debug.Log($"Player was spawned");
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
    /// <param name="worldX"></param>
    /// <param name="worldZ"></param>
    private void HideChunkColumn(int worldX, int worldZ)
    {
        for (int y = 0; y < worldDimensions.y; y++)
        {
            Vector3Int pos = new Vector3Int(worldX, y * chunkDimensions.y, worldZ);
            if (createdChunks.Contains(pos))
            {
                chunks[pos].meshRendererSolid.enabled = false;
                chunks[pos].meshRendererFluid.enabled = false;
            }
        }
    }

    private WaitForSeconds waitFor3Seconds = new WaitForSeconds(3);
    private IEnumerator HealBlock(Chunk c, int blockIndex)
    {
        yield return waitFor3Seconds;
        if (c.chunkData[blockIndex] != MeshUtils.BlockType.Air)
        {
            c.healthData[blockIndex] = MeshUtils.BlockType.Nocrack;
            RedrawChunk(c);
            
        }
    }

    private WaitForSeconds dropDelay = new WaitForSeconds(0.1f);
    private IEnumerator Drop(Chunk chunk, int blockIndex, int strength = 3)
    {
        if (!MeshUtils.canDrop.Contains(chunk.chunkData[blockIndex]))
        {
            yield break;
        }
        yield return dropDelay;
        while (true)
        {
            Vector3Int thisBlockPos = FromFlat(blockIndex);
            (Vector3Int chunkPosOfBelowBlock, Vector3Int adjustedBelowBlockPos) = AdjustGridPosition(chunk.location, thisBlockPos + Vector3Int.down);
            int belowBlockIndex = ToFlat(adjustedBelowBlockPos);
            Chunk chunkOfBelowBlock = chunks[chunkPosOfBelowBlock];
            if (chunkOfBelowBlock != null && chunkOfBelowBlock.chunkData[belowBlockIndex] == MeshUtils.BlockType.Air)
            {
                //fall -> move block 1 down -> switch chunkData
                chunkOfBelowBlock.chunkData[belowBlockIndex] = chunk.chunkData[blockIndex];
                chunkOfBelowBlock.healthData[belowBlockIndex] = MeshUtils.BlockType.Nocrack;

                chunk.chunkData[blockIndex] = MeshUtils.BlockType.Air;
                chunk.healthData[blockIndex] = MeshUtils.BlockType.Nocrack;

                // test if there is a fallable block above the new air block
                Vector3Int aboveBlock = thisBlockPos + Vector3Int.up;
                (Vector3Int adjustedChunkPos, Vector3Int adjustedBlockPosition) = AdjustGridPosition(chunk.location, aboveBlock);
                int aboveBlockIndex = ToFlat(adjustedBlockPosition);
                StartCoroutine(Drop(chunks[adjustedChunkPos], aboveBlockIndex));

                yield return dropDelay;

                RedrawChunk(chunk);
                if (chunkOfBelowBlock != chunk)
                {
                    RedrawChunk(chunkOfBelowBlock);
                }

                chunk = chunkOfBelowBlock;
                blockIndex = belowBlockIndex;
            }
            else if (MeshUtils.canFlow.Contains(chunk.chunkData[blockIndex]))
            {
                FlowIntoNeighbours(thisBlockPos, chunk.location, Vector3Int.left, strength);
                FlowIntoNeighbours(thisBlockPos, chunk.location, Vector3Int.right, strength);
                FlowIntoNeighbours(thisBlockPos, chunk.location, Vector3Int.forward, strength);
                FlowIntoNeighbours(thisBlockPos, chunk.location, Vector3Int.back, strength);
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
        (Vector3Int neighbourChunkPos, Vector3Int neighbourBlockPos) = AdjustGridPosition(chunkPosition, neighbourPosition);

        int neighbourBlockIndex = ToFlat(neighbourBlockPos);
        Chunk neighbourChunk = chunks[neighbourChunkPos];

        if (neighbourChunk != null && neighbourChunk.chunkData[neighbourBlockIndex] == MeshUtils.BlockType.Air)
        {
            // flow
            Debug.Log($"Flow");
            neighbourChunk.chunkData[neighbourBlockIndex] = chunks[chunkPosition].chunkData[ToFlat(blockPosition)];
            neighbourChunk.healthData[neighbourBlockIndex] = MeshUtils.BlockType.Nocrack;
            RedrawChunk(neighbourChunk);
            StartCoroutine(Drop(neighbourChunk, neighbourBlockIndex, strength--));
        }
        else
        {
            Debug.Log($"cannot flow, neighbour {neighbourBlockPos} is of type {neighbourChunk.chunkData[neighbourBlockIndex]}");
        }
    }

    private void RedrawChunk(Chunk c)
    {
        DestroyImmediate(c.GetComponent<MeshFilter>());
        DestroyImmediate(c.GetComponent<MeshRenderer>());
        DestroyImmediate(c.GetComponent<Collider>());
        c.CreateChunk(chunkDimensions, c.location, false);
    }

    public void SaveWorld()
    {
        FileSaver.Save(this);
    }

    private IEnumerator LoadWorldFromFile()
    {
        WorldData worldData = FileSaver.Load();
        if (worldData == null)
        {
            StartCoroutine(BuildWorld());
            yield break;
        } 

        createdChunks.Clear();
        // populate runtime data structures with loaded data
        for (int i = 0; i < worldData.chunkCheckerValues.Length; i+=3)
        {
            createdChunks.Add(new Vector3Int(
                worldData.chunkCheckerValues[i],
                worldData.chunkCheckerValues[i + 1],
                worldData.chunkCheckerValues[i + 2])); 
        }

        createdChunkColumns.Clear();
        for (int i = 0; i < worldData.chunkColumnValues.Length; i+=2)
        {
            createdChunkColumns.Add(new Vector2Int(
                worldData.chunkColumnValues[i],
                worldData.chunkColumnValues[i + 1]));
        }

        int blockCount = chunkDimensions.x * chunkDimensions.y * chunkDimensions.z;
        InitLoadingBar(createdChunks.Count);
        int index = 0;
        int vIndex = 0;
        foreach (Vector3Int chunkPos in createdChunks)
        {
            GameObject chunk = Instantiate(chunkPrefab);
            chunk.name = $"Chunk_{chunkPos.x}_{chunkPos.y}_{chunkPos.z}";
            Chunk c = chunk.GetComponent<Chunk>();
            
            c.chunkData = new MeshUtils.BlockType[blockCount];
            c.healthData = new MeshUtils.BlockType[blockCount];

            for (int i = 0; i < blockCount; i++)
            {
                c.chunkData[i] = (MeshUtils.BlockType)worldData.allChunkData[index];
                c.healthData[i] = MeshUtils.BlockType.Nocrack;
                index++;
            }

            c.CreateChunk(chunkDimensions, chunkPos, false);
            chunks.Add(chunkPos, c);
            RedrawChunk(c);
            c.meshRendererSolid.enabled = worldData.chunkVisibility[vIndex];
            c.meshRendererFluid.enabled = worldData.chunkVisibility[vIndex];
            vIndex++;
            loadingBar.value++;
            yield return null;
        }

        firstPersonController.transform.position = new Vector3(worldData.fpcX, worldData.fpcY, worldData.fpcZ);
        mainCamera.SetActive(false);
        firstPersonController.SetActive(true);
        lastPlayerPositionTriggeringNewChunks = Vector3Int.CeilToInt(firstPersonController.transform.position);
        loadingBar.gameObject.SetActive(false);

        StartCoroutine(BuildQueueProcessor());
        StartCoroutine(UpdateWorld());
    }



    /// <summary>
    /// adds one chunk column every frame until the whole initial world is built
    /// starts monitoring coroutines after that
    /// </summary>
    /// <returns></returns>
    private IEnumerator BuildWorld()
    {
        Debug.Log($"Initial World building started, Creating max " +
            $"{worldDimensions.x * chunkDimensions.x} * " +
            $"{worldDimensions.y * chunkDimensions.y} * " +
            $"{worldDimensions.z * chunkDimensions.z} chunks");

        InitLoadingBar(worldDimensions.x * worldDimensions.z);

        for (int z = 0; z < worldDimensions.z; z++)
        {
            for (int x = 0; x < worldDimensions.x; x++)
            {
                BuildChunkColumn(x * chunkDimensions.x, z * chunkDimensions.z); ;
                loadingBar.value += 1;
                yield return null;
            }
        }

        Debug.Log($"Initial World Building finished");

        loadingBar.gameObject.SetActive(false);

        SpawnPlayer();

        // StartCoroutine(BuildQueueProcessor());
        // StartCoroutine(UpdateWorld());
        //StartCoroutine(BuildExtraWorld());

    }

    /// <summary>
    /// BuildCoordinator monitors the build queue for tasks and runs them one after the other
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

    // checks every 500ms if the player has moved far enough to trigger new chunk building
    private IEnumerator UpdateWorld()
    {
        Debug.Log("Start monitoring player position");
        while (true)
        {
            //Debug.Log("Check Player Position");
            //TODO currently only works if chunk dimensions are uniform
            if ((lastPlayerPositionTriggeringNewChunks - firstPersonController.transform.position).magnitude > chunkDimensions.x)
            {
                //TODO why always rounding up?
                lastPlayerPositionTriggeringNewChunks = Vector3Int.CeilToInt(firstPersonController.transform.position);
                // positions values are always the beginning of a chunk: 7->0,15->10,29->20, etc.why?
                int playerColumnX = (int)(firstPersonController.transform.position.x / chunkDimensions.x) * chunkDimensions.x;
                int playerColumnZ = (int)(firstPersonController.transform.position.z / chunkDimensions.z) * chunkDimensions.z;
                buildQueue.Enqueue(BuildWorldRecursively(playerColumnX, playerColumnZ, chunkColumnDrawRadius));
                buildQueue.Enqueue(HideColumns(new Vector2Int(playerColumnX, playerColumnZ)));
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
    /// <param name="playerLocation" x and z world coordinates of the current player location
    private IEnumerator HideColumns(Vector2Int playerLocation)
    {
        Debug.Log($"Hide columns around {playerLocation.x}:{playerLocation.y}");

        //TODO Improvement: we don't need to iterate all columns, only the visible ones
        foreach (Vector2Int column in createdChunkColumns)
        {
            if ((column - playerLocation).magnitude >= chunkColumnDrawRadius * chunkDimensions.x)
            {
                HideChunkColumn(column.x, column.y);
            }
        }
        yield return null;
    }

    //TODO: alternative without recursion
    /// <summary>
    /// builds new chunk columns around a players position
    /// </summary>
    /// <param name="worldX"></param>
    /// <param name="worldZ"></param>
    /// <param name="buildRadius"> grid coordinates</param>
    /// <returns></returns>
    private IEnumerator BuildWorldRecursively(int worldX, int worldZ, int buildRadius)
    {
        Debug.Log($"Build Recursive World at {worldX}:{worldZ} with radius {buildRadius}");

        int nextRadius = buildRadius - 1;
        if (buildRadius <= 0)
        {
            yield break;
        }

        BuildChunkColumn(worldX, worldZ + chunkDimensions.z);
        buildQueue.Enqueue(BuildWorldRecursively(worldX, worldZ + chunkDimensions.z, nextRadius));
        yield return null;

        BuildChunkColumn(worldX, worldZ - chunkDimensions.z);
        buildQueue.Enqueue(BuildWorldRecursively(worldX, worldZ - chunkDimensions.z, nextRadius));
        yield return null;

        BuildChunkColumn(worldX + chunkDimensions.x, worldZ);
        buildQueue.Enqueue(BuildWorldRecursively(worldX + chunkDimensions.x, worldZ, nextRadius));
        yield return null;

        BuildChunkColumn(worldX - chunkDimensions.x, worldZ);
        buildQueue.Enqueue(BuildWorldRecursively(worldX - chunkDimensions.x, worldZ, nextRadius));
        yield return null;
    }
}