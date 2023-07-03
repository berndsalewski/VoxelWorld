using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// handles player input and behaviour, highlights the block in the center of the players view
    /// </summary>
    public class Player : MonoBehaviour
    {
        public GameObject highlightBlock;
        public GameObject firstPersonController;
        public WorldConfiguration worldConfiguration;

        public Vector3 position
        {
            get { return firstPersonController.transform.position; }
            set { firstPersonController.transform.position = value; }
        }

        [SerializeField]
        private WorldBuilder worldBuilder;
        [SerializeField]
        private WorldUpdater worldUpdater;
        private WorldDataModel _worldModel;

        private BlockType buildBlockType = BlockType.Dirt;

        private void Start()
        {
            _worldModel = WorldDataModel.Instance;
        }

        private void Update()
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 10, int.MaxValue, QueryTriggerInteraction.Ignore))
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    HandleMouseClicks(hit);
                }

                Vector3 highlightBlockCenterPoint = hit.point - hit.normal * Block.HALF_BLOCK_SIZE;
                highlightBlockCenterPoint.x = Mathf.Floor(highlightBlockCenterPoint.x) + Block.HALF_BLOCK_SIZE;
                highlightBlockCenterPoint.y = Mathf.Floor(highlightBlockCenterPoint.y) + Block.HALF_BLOCK_SIZE;
                highlightBlockCenterPoint.z = Mathf.Floor(highlightBlockCenterPoint.z) + Block.HALF_BLOCK_SIZE;

                highlightBlock.transform.position = highlightBlockCenterPoint;
                highlightBlock.SetActive(true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // for debug output, see OnGui
                selectedBlockWorldPosition = highlightBlockCenterPoint;
                didRaycastHitACollider = true;
                hitColliderName = hit.collider?.name;
                rayCastHitPoint = hit.point;
#endif
            }
            else
            {
                highlightBlock.SetActive(false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                didRaycastHitACollider = false;
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        public void SetBuildType(BlockType type)
        {
            Debug.Log($"switched build type to {type}");
            buildBlockType = type;
        }

        public void SetActive(bool isActive)
        {
            firstPersonController.SetActive(isActive);
        }

        /// <summary>
        /// delete or build blocks
        /// </summary>
        /// <param name="hit"></param>
        private void HandleMouseClicks(RaycastHit hit)
        {
            Vector3 hitBlock;
            if (Input.GetMouseButton(0))
            {
                hitBlock = hit.point - hit.normal * Block.HALF_BLOCK_SIZE;
            }
            else
            {
                hitBlock = hit.point + hit.normal * Block.HALF_BLOCK_SIZE;
            }

            (Vector3Int chunkPosition, Vector3Int blockPosition) = WorldUtils.FromWorldPosToCoordinates(hitBlock, worldConfiguration.chunkDimensions);
            Chunk thisChunk = _worldModel.GetChunk(chunkPosition);
            int currentBlockIndex = Chunk.ToBlockIndex(blockPosition);

            // delete blocks with left mousebutton
            if (Input.GetMouseButton(0))
            {
                if (MeshUtils.blockTypeHealth[(int)thisChunk.chunkData[currentBlockIndex]] > -1)
                {
                    thisChunk.healthData[currentBlockIndex]++;
                    if (thisChunk.healthData[currentBlockIndex] == BlockType.Nocrack +
                        MeshUtils.blockTypeHealth[(int)thisChunk.chunkData[currentBlockIndex]])
                    {
                        Debug.Log($"Delete at chunk:{thisChunk.coordinate.ToString()} blockId:{currentBlockIndex.ToString()} block:" +
                            $"{blockPosition.x.ToString()}:{blockPosition.y.ToString()}:{blockPosition.z.ToString()}");

                        thisChunk.chunkData[currentBlockIndex] = BlockType.Air;

                        // takes care of dropping blocks
                        Vector3Int aboveBlock = blockPosition + Vector3Int.up;
                        (Vector3Int adjustedChunkPos, Vector3Int adjustedBlockPosition) = WorldUtils.AdjustCoordinatesToGrid(chunkPosition, aboveBlock, worldConfiguration.chunkDimensions);
                        int aboveBlockIndex = Chunk.ToBlockIndex(adjustedBlockPosition);
                        StartCoroutine(worldUpdater.HandleBlockDropping(_worldModel.GetChunk(adjustedChunkPos), aboveBlockIndex));
                    }

                    StartCoroutine(thisChunk.HealBlock(currentBlockIndex, worldConfiguration.waterLevel));
                }
            }
            // build block with right mouse button
            else
            {
                Debug.Log($"Build in chunk:{thisChunk.coordinate.ToString()} blockId:{currentBlockIndex.ToString()} " +
                    $"block:{blockPosition.x.ToString()}:{blockPosition.y.ToString()}:{blockPosition.z.ToString()}");
                thisChunk.chunkData[currentBlockIndex] = buildBlockType;
                thisChunk.healthData[currentBlockIndex] = BlockType.Nocrack;
                StartCoroutine(worldUpdater.HandleBlockDropping(thisChunk, currentBlockIndex));
            }

            thisChunk.Redraw(worldConfiguration.waterLevel);
        }

        /// <summary>
        /// spawns the player in the initial center of the world (0,0) in the xz plane
        /// </summary>
        public void Spawn()
        {
            Debug.Log("Spawn Player");

            float posX = worldConfiguration.chunkDimensions.x * 0.5f;
            float posZ = worldConfiguration.chunkDimensions.z * 0.5f;

            // get the height of the surface at the spawn position
            float posY = MeshUtils.fBM(
                posX,
                posZ,
                WorldBuilder.surfaceSettings.octaves,
                WorldBuilder.surfaceSettings.scale,
                WorldBuilder.surfaceSettings.heightScale,
                WorldBuilder.surfaceSettings.heightOffset);

            float verticalOffset = 3;
            firstPersonController.transform.position = new Vector3(posX, posY + verticalOffset, posZ);
            worldUpdater.lastPlayerPositionTriggeringNewChunks = firstPersonController.transform.position;
            worldBuilder.mainCamera.SetActive(false);
            firstPersonController.SetActive(true);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private Vector3 selectedBlockWorldPosition;
        private Vector3 rayCastHitPoint;
        private bool didRaycastHitACollider;
        private string hitColliderName;

        private void OnGUI()
        {
            if (didRaycastHitACollider)
            {
                (Vector3Int chunkPosition, Vector3Int blockPosition) = WorldUtils.FromWorldPosToCoordinates(selectedBlockWorldPosition, worldConfiguration.chunkDimensions);
                int blockIndex = Chunk.ToBlockIndex(blockPosition);
                Chunk chunk = _worldModel.GetChunk(chunkPosition);
                BlockType blockType = chunk.chunkData[blockIndex];

                int boxWidth = 164;
                int paddingLeft = 10;
                int lineHeight = 24;
                GUI.Box(UIScaler.GetScaledRect(paddingLeft, 10, boxWidth, lineHeight), $"Chunk: {chunkPosition}", UIScaler.scaledStyle);
                GUI.Box(UIScaler.GetScaledRect(paddingLeft, 35, boxWidth, lineHeight), $"Block: {blockPosition}", UIScaler.scaledStyle);
                GUI.Box(UIScaler.GetScaledRect(paddingLeft, 60, boxWidth, lineHeight), $"Block Id:  {blockIndex}", UIScaler.scaledStyle);
                GUI.Box(UIScaler.GetScaledRect(paddingLeft, 85, boxWidth, lineHeight), $"Type:  {blockType}", UIScaler.scaledStyle);
                GUI.Box(UIScaler.GetScaledRect(paddingLeft, 110, boxWidth, lineHeight), $"Hit:  {rayCastHitPoint}", UIScaler.scaledStyle);
                GUI.Box(UIScaler.GetScaledRect(paddingLeft, 135, boxWidth, lineHeight), $"Collider:  {hitColliderName}", UIScaler.scaledStyle);
            }
        }
#endif
    }
}