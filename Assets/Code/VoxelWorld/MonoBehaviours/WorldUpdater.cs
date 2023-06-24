using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelWorld
{
    public class WorldUpdater : MonoBehaviour
    {
        // at which player position did we last trigger the addition of new chunks
        [HideInInspector]
        public Vector3 lastPlayerPositionTriggeringNewChunks;

        [SerializeField]
        private WorldBuilder worldBuilder;
        [SerializeField]
        private Player player;

        // holds coroutines for building chunks and hiding chunk columns
        private Queue<IEnumerator> buildQueue = new Queue<IEnumerator>();

        private WorldDataModel _worldModel;

        private void Start()
        {
            _worldModel = WorldDataModel.Instance;
        }

        /// <summary>
        /// checks if the block at that position can drop and drop it if below is air block
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="blockIndex"></param>
        /// <param name="strength"></param>
        /// <returns></returns>
        public IEnumerator HandleBlockDropping(Chunk chunk, int blockIndex, int strength = 3)
        {
            if (!MeshUtils.canDrop.Contains(chunk.chunkData[blockIndex]))
            {
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
            while (true)
            {
                Vector3Int thisBlockPos = Chunk.ToBlockCoordinates(blockIndex);
                (Vector3Int chunkPosOfBelowBlock, Vector3Int adjustedBelowBlockPos) = WorldUtils.AdjustCoordinatesToGrid(chunk.coordinate, thisBlockPos + Vector3Int.down);
                int belowBlockIndex = Chunk.ToBlockIndex(adjustedBelowBlockPos);
                Chunk chunkOfBelowBlock = _worldModel.GetChunk(chunkPosOfBelowBlock);
                if (chunkOfBelowBlock?.chunkData[belowBlockIndex] == BlockType.Air)
                {
                    //fall -> move block 1 down -> switch chunkData
                    chunkOfBelowBlock.chunkData[belowBlockIndex] = chunk.chunkData[blockIndex];
                    chunkOfBelowBlock.healthData[belowBlockIndex] = BlockType.Nocrack;
                    chunk.chunkData[blockIndex] = BlockType.Air;
                    chunk.healthData[blockIndex] = BlockType.Nocrack;

                    // test if there is now a droppable block above this new air block
                    Vector3Int aboveBlock = thisBlockPos + Vector3Int.up;
                    (Vector3Int adjustedChunkPos, Vector3Int adjustedBlockPosition) = WorldUtils.AdjustCoordinatesToGrid(chunk.coordinate, aboveBlock);
                    int aboveBlockIndex = Chunk.ToBlockIndex(adjustedBlockPosition);
                    StartCoroutine(HandleBlockDropping(_worldModel.GetChunk(adjustedChunkPos), aboveBlockIndex));

                    yield return new WaitForSeconds(0.1f);

                    chunk.Redraw(worldBuilder.waterLevel);
                    if (chunkOfBelowBlock != chunk)
                    {
                        chunkOfBelowBlock.Redraw(worldBuilder.waterLevel);
                    }

                    chunk = chunkOfBelowBlock;
                    blockIndex = belowBlockIndex;
                }
                else if (MeshUtils.canFlow.Contains(chunk.chunkData[blockIndex]))
                {
                    HandleBlockFlowing(thisBlockPos, chunk.coordinate, Vector3Int.left, strength);
                    HandleBlockFlowing(thisBlockPos, chunk.coordinate, Vector3Int.right, strength);
                    HandleBlockFlowing(thisBlockPos, chunk.coordinate, Vector3Int.forward, strength);
                    HandleBlockFlowing(thisBlockPos, chunk.coordinate, Vector3Int.back, strength);
                    yield break;
                }
                else
                {
                    yield break;
                }
            }
        }

        public void HandleBlockFlowing(Vector3Int blockPosition, Vector3Int chunkPosition, Vector3Int neighbourDirection, int strength)
        {
            strength--;
            if (strength <= 0)
            {
                return;
            }

            Vector3Int neighbourPosition = blockPosition + neighbourDirection;
            (Vector3Int neighbourChunkPos, Vector3Int neighbourBlockPos) = WorldUtils.AdjustCoordinatesToGrid(chunkPosition, neighbourPosition);

            int neighbourBlockIndex = Chunk.ToBlockIndex(neighbourBlockPos);
            Chunk neighbourChunk = _worldModel.GetChunk(neighbourChunkPos);

            if (neighbourChunk != null && neighbourChunk.chunkData[neighbourBlockIndex] == BlockType.Air)
            {
                // flow
                Debug.Log($"Flow");
                neighbourChunk.chunkData[neighbourBlockIndex] = _worldModel.GetChunk(chunkPosition).chunkData[Chunk.ToBlockIndex(blockPosition)];
                neighbourChunk.healthData[neighbourBlockIndex] = BlockType.Nocrack;
                neighbourChunk.Redraw(worldBuilder.waterLevel);
                StartCoroutine(HandleBlockDropping(neighbourChunk, neighbourBlockIndex, strength--));
            }
            else
            {
                Debug.Log($"cannot flow, neighbour {neighbourBlockPos} is of type {neighbourChunk.chunkData[neighbourBlockIndex]}");
            }
        }

        // checks every 500ms if the player has moved far enough to trigger the creation of new chunks
        public IEnumerator UpdateWorldMonitor()
        {
            Debug.Log("Start monitoring player position");
            while (true)
            {
                //TODO currently only works if chunk dimensions are uniform
                int minWalkDistance = WorldBuilder.chunkDimensions.x;
                float walkedDistance = (lastPlayerPositionTriggeringNewChunks - player.position).magnitude;
                if (walkedDistance > minWalkDistance)
                {
                    lastPlayerPositionTriggeringNewChunks = player.position;

                    (Vector3Int chunkCoordinate, Vector3Int blockCoordinate) = WorldUtils.FromWorldPosToCoordinates(player.position);
                    Vector2Int chunkColumnCoordinates = new Vector2Int(chunkCoordinate.x, chunkCoordinate.z);
                    buildQueue.Enqueue(HideChunkColumns(chunkColumnCoordinates));

                    buildQueue.Enqueue(worldBuilder.BuildChunkColumns(player.position, worldBuilder.chunkColumnDrawRadius * WorldBuilder.chunkDimensions.x));
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>
        /// BuildCoordinator monitors the build queue for tasks and runs them one after the other, one job per frame
        /// </summary>
        public IEnumerator BuildQueueProcessor()
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

        /// <summary>
        /// hides all chunk columns which are beyond the draw radius
        /// </summary>
        /// <param name="currentChunkColumnCoordinate">the chunk column coordinate of the current player position</param> 
        public IEnumerator HideChunkColumns(Vector2Int currentChunkColumnCoordinate)
        {
            //TODO Improvement: we don't need to iterate all columns, only the visible ones
            foreach (Vector2Int column in _worldModel.chunkColumns)
            {
                if ((column - currentChunkColumnCoordinate).magnitude > worldBuilder.chunkColumnDrawRadius * WorldBuilder.chunkDimensions.x)
                {
                    HideChunkColumn(column.x, column.y);
                }
            }
            yield return null;
        }

        /// <summary>
        /// disables the mesh renderer of a chunk if that chunk exists already
        /// </summary>
        private void HideChunkColumn(int worldX, int worldZ)
        {
            for (int y = 0; y < worldBuilder.worldDimensions.y; y++)
            {
                Vector3Int coordinate = new Vector3Int(worldX, y * WorldBuilder.chunkDimensions.y, worldZ);
                if (_worldModel.IsChunkActive(coordinate))
                {
                    _worldModel.GetChunk(coordinate).meshRendererSolidBlocks.enabled = false;
                    _worldModel.GetChunk(coordinate).meshRendererFluidBlocks.enabled = false;
                }
            }
        }
    }
}