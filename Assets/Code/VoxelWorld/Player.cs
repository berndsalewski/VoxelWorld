using UnityEngine;
using System.Collections;
using VoxelWorld;

namespace VoxelWorld
{
    /// <summary>
    /// handles player input and behaviour, highlights the block in the center of the players view
    /// </summary>
    public class Player : MonoBehaviour
    {
        public GameObject highlightBlock;

        [SerializeField]
        private World world;

        private BlockType buildBlockType = BlockType.Dirt;

        /// <summary>
        /// used by Buttons in Scene
        /// </summary>
        /// <param name="type"></param>
        public void SetBuildType(int type)
        {
            buildBlockType = (BlockType)type;
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
                highlightBlockCenterPoint.x = Mathf.Floor(highlightBlockCenterPoint.x) + 0.5f;
                highlightBlockCenterPoint.y = Mathf.Floor(highlightBlockCenterPoint.y) + 0.5f;
                highlightBlockCenterPoint.z = Mathf.Floor(highlightBlockCenterPoint.z) + 0.5f;

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

            (Vector3Int chunkPosition, Vector3Int blockPosition) = World.FromWorldPosToCoordinates(hitBlock);
            Chunk thisChunk = world.chunks[chunkPosition];
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
                        Debug.Log($"Delete at chunk:{thisChunk.coordinates} blockId:{currentBlockIndex} block:{blockPosition.x}:{blockPosition.y}:{blockPosition.z}");
                        thisChunk.chunkData[currentBlockIndex] = BlockType.Air;

                        // takes care of dropping blocks
                        Vector3Int aboveBlock = blockPosition + Vector3Int.up;
                        (Vector3Int adjustedChunkPos, Vector3Int adjustedBlockPosition) = World.AdjustCoordinatesToGrid(chunkPosition, aboveBlock);
                        int aboveBlockIndex = Chunk.ToBlockIndex(adjustedBlockPosition);
                        StartCoroutine(world.Drop(world.chunks[adjustedChunkPos], aboveBlockIndex));
                    }

                    StartCoroutine(world.HealBlock(thisChunk, currentBlockIndex));
                }
            }
            // build block with right mouse button
            else
            {
                Debug.Log($"Build in chunk:{thisChunk.coordinates} blockId:{currentBlockIndex} block:{blockPosition.x}:{blockPosition.y}:{blockPosition.z}");
                thisChunk.chunkData[currentBlockIndex] = buildBlockType;
                thisChunk.healthData[currentBlockIndex] = BlockType.Nocrack;
                StartCoroutine(world.Drop(thisChunk, currentBlockIndex));
            }

            world.RedrawChunk(thisChunk);
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
                (Vector3Int chunkPosition, Vector3Int blockPosition) = World.FromWorldPosToCoordinates(selectedBlockWorldPosition);
                int blockIndex = Chunk.ToBlockIndex(blockPosition);
                Chunk chunk = world.chunks[chunkPosition];
                BlockType blockType = BlockType.Redstone;
                blockType = chunk.chunkData[blockIndex];

                GUIStyle boxStyle = new GUIStyle();
                boxStyle.alignment = TextAnchor.UpperLeft;
                boxStyle.normal.textColor = Color.white;
                boxStyle.normal.background = Texture2D.grayTexture;
                boxStyle.padding.left = 5;
                boxStyle.padding.top = 5;

                GUI.Box(new Rect(10f, 10f, 160f, 24f), $"Chunk: {chunkPosition}", boxStyle);
                GUI.Box(new Rect(10f, 35f, 160f, 24f), $"Block: {blockPosition}", boxStyle);
                GUI.Box(new Rect(10f, 60f, 160f, 24f), $"Block Id:  {blockIndex}", boxStyle);
                GUI.Box(new Rect(10f, 85f, 160f, 24f), $"Type:  {blockType}", boxStyle);
                GUI.Box(new Rect(10f, 110f, 160f, 24f), $"Hit:  {rayCastHitPoint}", boxStyle);
                GUI.Box(new Rect(10f, 135f, 160f, 24f), $"Collider:  {hitColliderName}", boxStyle);
            }
        }
#endif
    }
}