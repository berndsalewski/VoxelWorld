using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using MeshData = UnityEngine.Mesh.MeshData;
using MeshDataArray = UnityEngine.Mesh.MeshDataArray;

namespace VoxelWorld
{
    /// <summary>
    /// a Chunk contains width * height * depth blocks and is a piece of
    /// geometry which is generated in one pass
    /// </summary>
    public class Chunk : MonoBehaviour
    {
        public Material atlas;
        public Material fluid;

        [HideInInspector]
        public Vector3Int coordinate;

        /// 3-dimensional array, x,y,z, relative coordinates within a chunk for a block
        public Block[,,] blocks;

        //TODO move this out of mononehaviour into ChunkData
        [HideInInspector]
        public BlockType[] chunkData;
        /// the current health (visual) of the block at the index
        [HideInInspector]
        public BlockType[] healthData;//TODO rethink if this needs to be BlockType
        [HideInInspector]
        public MeshRenderer meshRendererSolidBlocks;
        [HideInInspector]
        public MeshRenderer meshRendererFluidBlocks;

        private NativeArray<Unity.Mathematics.Random> RandomArray { get; set; }

        private GameObject solidMesh;
        private GameObject fluidMesh;

        private CalculateBlockTypesJob calculateBlockTypes;
        private JobHandle jobHandle;

        /// <summary>
        /// calculates relative chunk position from the index of a block
        /// </summary>
        public static Vector3Int ToBlockCoordinates(int index)
        {
            // x = i % width
            // y = (i / width) % height
            // z = i / (width * height)
            return new Vector3Int(
                index % WorldBuilder.chunkDimensions.x,
                (index / WorldBuilder.chunkDimensions.x) % WorldBuilder.chunkDimensions.y,
                index / (WorldBuilder.chunkDimensions.x * WorldBuilder.chunkDimensions.y));
        }

        /// <summary>
        /// calculates the index (in chunkData) of a block from a vector position
        /// </summary>
        public static int ToBlockIndex(Vector3Int coordinates)
        {
            // i = x + WIDTH * (y + DEPTH * z)
            return coordinates.x + WorldBuilder.chunkDimensions.x * (coordinates.y + WorldBuilder.chunkDimensions.z * coordinates.z);
        }

        /// <summary>
        /// generates the initial data which defines which block is what
        /// </summary>
        private void GenerateChunkData(int waterLevel)
        {
            int blockCount = WorldBuilder.blockCountPerChunk;
            chunkData = new BlockType[blockCount];
            healthData = new BlockType[blockCount];
            NativeArray<BlockType> blockTypes = new NativeArray<BlockType>(chunkData, Allocator.Persistent);
            NativeArray<BlockType> healthTypes = new NativeArray<BlockType>(healthData, Allocator.Persistent);

            var randomArray = new Unity.Mathematics.Random[blockCount];
            var seed = new System.Random();

            for (int i = 0; i < blockCount; i++)
            {
                uint randomSeed = (uint)seed.Next();
                randomArray[i] = new Unity.Mathematics.Random(randomSeed);
            }

            RandomArray = new NativeArray<Unity.Mathematics.Random>(randomArray, Allocator.Persistent);

            calculateBlockTypes = new CalculateBlockTypesJob()
            {
                cData = blockTypes,
                hData = healthTypes,
                width = WorldBuilder.chunkDimensions.x,
                height = WorldBuilder.chunkDimensions.y,
                chunkCoordinate = coordinate,
                randoms = RandomArray,
                waterLevel = waterLevel
            };

            jobHandle = calculateBlockTypes.Schedule(chunkData.Length, 64);
            jobHandle.Complete();
            calculateBlockTypes.cData.CopyTo(chunkData);
            calculateBlockTypes.hData.CopyTo(healthData);
            blockTypes.Dispose();
            healthTypes.Dispose();
            RandomArray.Dispose();

            GenerateTrees();
        }

        /// <summary>
        /// places tree structure in the world (chunkData)
        /// </summary>
        private void GenerateTrees()
        {
            //TODO get from scriptable object or file
            (Vector3Int, BlockType)[] treeDesign = new (Vector3Int, BlockType)[]{
                (new Vector3Int(0,3,-1), BlockType.Leaves),
                (new Vector3Int(-1,4,-1), BlockType.Leaves),
                (new Vector3Int(0,4,-1), BlockType.Leaves),
                (new Vector3Int(1,4,-1), BlockType.Leaves),
                (new Vector3Int(0,5,-1), BlockType.Leaves),
                (new Vector3Int(0,0,0), BlockType.Wood),
                (new Vector3Int(0,1,0), BlockType.Wood),
                (new Vector3Int(0,2,0), BlockType.Wood),
                (new Vector3Int(-1,3,0), BlockType.Leaves),
                (new Vector3Int(0,3,0), BlockType.Wood),
                (new Vector3Int(1,3,0), BlockType.Leaves),
                (new Vector3Int(-1,4,0), BlockType.Leaves),
                (new Vector3Int(0,4,0), BlockType.Leaves),
                (new Vector3Int(1,4,0), BlockType.Leaves),
                (new Vector3Int(-1,5,0), BlockType.Leaves),
                (new Vector3Int(0,5,0), BlockType.Leaves),
                (new Vector3Int(1,5,0), BlockType.Leaves),
                (new Vector3Int(0,3,1), BlockType.Leaves),
                (new Vector3Int(-1,4,1), BlockType.Leaves),
                (new Vector3Int(0,4,1), BlockType.Leaves),
                (new Vector3Int(1,4,1), BlockType.Leaves),
                (new Vector3Int(0,5,1), BlockType.Leaves)
            };

            for (int i = 0; i < chunkData.Length; i++)
            {
                if (chunkData[i] == BlockType.Woodbase)
                {
                    Vector3Int treeBasePos = ToBlockCoordinates(i);
                    foreach (var item in treeDesign)
                    {
                        Vector3Int position = treeBasePos + item.Item1;
                        int blockIndex = ToBlockIndex(position);
                        if (blockIndex >= 0 && blockIndex < chunkData.Length)
                        {
                            chunkData[blockIndex] = item.Item2;
                            healthData[blockIndex] = BlockType.Nocrack;
                        }
                    }
                }
            }
        }

        ProfilerMarker profilerMarkerRunMergeBlockMeshesJob = new ("MergeBlockMeshesJob");
        ProfilerMarker profilerMarkerCreateBlockMeshes = new ("CreateBlockMeshes");
        ProfilerMarker profilerMarkerCreateSingleBlock = new ("CreateSingleBlock");
        /// <summary>
        /// creates a chunk of blocks, creates the actual meshes for every single block and merges them into 2 chunk meshes
        /// </summary>
        public void CreateChunkMeshes(Vector3Int chunkCoordinate, int waterLevel, bool generateChunkData = true)
        {
            coordinate = chunkCoordinate;

            MeshFilter meshFilterSolid;
            MeshRenderer meshRendererSolid;
            MeshFilter meshFilterFluid;
            MeshRenderer meshRendererFluid;

            if (generateChunkData)
            {
                GenerateChunkData(waterLevel);
            }

            // create required gameobject and components if not already present
            if (solidMesh == null)
            {
                solidMesh = new GameObject("Solid");
                solidMesh.transform.parent = gameObject.transform;
                meshFilterSolid = solidMesh.AddComponent<MeshFilter>();
                meshRendererSolid = solidMesh.AddComponent<MeshRenderer>();
                meshRendererSolidBlocks = meshRendererSolid;
                meshRendererSolid.material = atlas;
            }
            else
            {
                meshFilterSolid = solidMesh.GetComponent<MeshFilter>();
                DestroyImmediate(solidMesh.GetComponent<MeshCollider>());
            }

            if (fluidMesh == null)
            {
                fluidMesh = new GameObject("Fluid");
                fluidMesh.transform.parent = gameObject.transform;
                meshFilterFluid = fluidMesh.AddComponent<MeshFilter>();
                meshRendererFluid = fluidMesh.AddComponent<MeshRenderer>();
                fluidMesh.AddComponent<UVScroller>();
                meshRendererFluidBlocks = meshRendererFluid;
                meshRendererFluid.material = fluid;
            }
            else
            {
                meshFilterFluid = fluidMesh.GetComponent<MeshFilter>();
                DestroyImmediate(fluidMesh.GetComponent<Collider>());
            }

            blocks = new Block[WorldBuilder.chunkDimensions.x, WorldBuilder.chunkDimensions.y, WorldBuilder.chunkDimensions.z];

            // run this 2 times, 1. for the solid blocks 2. for the water blocks
            for (int pass = 0; pass < 2; pass++)
            {
                var mergeBlockMeshesJob = new MergeBlockMeshesJob()
                {
                    vertexStartIndices = new NativeArray<int>(WorldBuilder.blockCountPerChunk, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
                    trianglesStartIndices = new NativeArray<int>(WorldBuilder.blockCountPerChunk, Allocator.TempJob, NativeArrayOptions.UninitializedMemory)
                };

                var inputBlockMeshes = new List<Mesh>();
                int vertexStartIndex = 0;
                int trianglesStartIndex = 0;
                int meshIndex = 0;

                profilerMarkerCreateBlockMeshes.Begin();

                //create the blocks and prepare data for the merge job
                int yCount = WorldBuilder.chunkDimensions.y;
                int xCount = WorldBuilder.chunkDimensions.x;
                int zCount = WorldBuilder.chunkDimensions.z;
                for (int z = 0; z < zCount; z++)
                {
                    for (int y = 0; y < yCount; y++)
                    {
                        for (int x = 0; x < xCount; x++)
                        {

                            Vector3Int blockCoordinates = new Vector3Int(x, y, z);
                            int blockIndex = ToBlockIndex(blockCoordinates);

                            profilerMarkerCreateSingleBlock.Begin();

                            // create a block and it's meshes
                            blocks[x, y, z] = new Block(
                                this,
                                blockCoordinates,
                                coordinate,
                                chunkData[blockIndex],
                                healthData[blockIndex]);

                            profilerMarkerCreateSingleBlock.End();

                            // we create 2 meshes per chunk, one for the water blocks, because they have a different material which
                            // handles transparency
                            bool isWater = MeshUtils.canFlow.Contains(chunkData[blockIndex]);
                            // add the mesh as input for the jobsystem
                            if (blocks[x, y, z].mesh != null
                                && ((pass == 0 && !isWater) || (pass == 1 && isWater))
                            )
                            {
                                // add the current mesh to the job input data
                                inputBlockMeshes.Add(blocks[x, y, z].mesh);

                                // store the index of the first vertex of the current mesh in the job
                                mergeBlockMeshesJob.vertexStartIndices[meshIndex] = vertexStartIndex;
                                // store the index of the first index buffer entry of the current mesh in the job
                                mergeBlockMeshesJob.trianglesStartIndices[meshIndex] = trianglesStartIndex;

                                // move index forward to the start of the next mesh
                                vertexStartIndex += blocks[x, y, z].mesh.vertexCount;
                                // get the length of the index buffer / triangles of the current block's mesh
                                int indexBufferLength = (int)blocks[x, y, z].mesh.GetIndexCount(0);
                                trianglesStartIndex += indexBufferLength;
                                // increment the loop/mesh counter
                                meshIndex++;
                            }
                        }
                    }
                }

                profilerMarkerCreateBlockMeshes.End();

                // allocate memmory for job data
                mergeBlockMeshesJob.inputMeshData = Mesh.AcquireReadOnlyMeshData(inputBlockMeshes);
                MeshDataArray outputMeshData = Mesh.AllocateWritableMeshData(1);
                mergeBlockMeshesJob.outputMesh = outputMeshData[0];

                int totalIndexBufferCount = trianglesStartIndex;
                int totalVertexBufferCount = vertexStartIndex;
                mergeBlockMeshesJob.outputMesh.SetIndexBufferParams(totalIndexBufferCount, IndexFormat.UInt32);
                mergeBlockMeshesJob.outputMesh.SetVertexBufferParams(totalVertexBufferCount,
                    new VertexAttributeDescriptor(VertexAttribute.Position),
                    new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 2),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord1, stream: 3)
                    );


                profilerMarkerRunMergeBlockMeshesJob.Begin();

                var handle = mergeBlockMeshesJob.Schedule(inputBlockMeshes.Count, 4);

                var mergedMesh = new Mesh();
                mergedMesh.name = $"Chunk_{coordinate.x}_{coordinate.y}_{coordinate.z}";
                name = mergedMesh.name;
                var meshDescriptor = new SubMeshDescriptor(0, totalIndexBufferCount, MeshTopology.Triangles);
                meshDescriptor.firstVertex = 0;
                meshDescriptor.vertexCount = totalVertexBufferCount;

                handle.Complete();
                profilerMarkerRunMergeBlockMeshesJob.End();

                mergeBlockMeshesJob.outputMesh.subMeshCount = 1;
                mergeBlockMeshesJob.outputMesh.SetSubMesh(0, meshDescriptor);

                Mesh.ApplyAndDisposeWritableMeshData(outputMeshData, new[] { mergedMesh });

                // dispose all allocted job data
                mergeBlockMeshesJob.inputMeshData.Dispose();
                mergeBlockMeshesJob.vertexStartIndices.Dispose();
                mergeBlockMeshesJob.trianglesStartIndices.Dispose();

                mergedMesh.RecalculateBounds();

                if (pass == 0)
                {
                    meshFilterSolid.mesh = mergedMesh;
                    MeshCollider collider = solidMesh.AddComponent<MeshCollider>();
                    collider.sharedMesh = meshFilterSolid.mesh;
                }
                else
                {
                    meshFilterFluid.mesh = mergedMesh;
                    MeshCollider collider = fluidMesh.AddComponent<MeshCollider>();
                    fluidMesh.layer = 4;
                    collider.sharedMesh = meshFilterFluid.mesh;
                }
            }
        }

        /// <summary>
        /// updates a chunk after changes, regenerates the mesh
        /// </summary>
        /// <param name="chunk"></param>
        public void Redraw(int waterLevel)
        {
            DestroyImmediate(GetComponent<MeshFilter>());
            DestroyImmediate(GetComponent<MeshRenderer>());
            DestroyImmediate(GetComponent<Collider>());
            CreateChunkMeshes(coordinate, waterLevel, false);
        }

        /// <summary>
        /// resets a blocks health to normal after 3 seconds
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="waterLevel"></param>
        /// <returns></returns>
        public IEnumerator HealBlock(int blockIndex, int waterLevel)
        {
            yield return new WaitForSeconds(3);
            if (chunkData[blockIndex] != BlockType.Air)
            {
                healthData[blockIndex] = BlockType.Nocrack;
                Redraw(waterLevel);
            }
        }

        /// <summary>
        /// merges a collection of input meshes into one output mesh, in our case 1000 block meshes into 2 chunk meshes (solid & fluid)
        /// job runs twice
        /// </summary>
        [BurstCompile]
        struct MergeBlockMeshesJob : IJobParallelFor
        {
            // the single block meshes
            [ReadOnly] public MeshDataArray inputMeshData;
            public MeshData outputMesh;
            public NativeArray<int> vertexStartIndices;
            public NativeArray<int> trianglesStartIndices;

            public void Execute(int index)
            {
                MeshData blockMeshData = inputMeshData[index];
                int blockMeshVertexCount = blockMeshData.vertexCount;
                int vertexStartIndex = vertexStartIndices[index];

                // read mesh data for this block from input data
                var vertices = new NativeArray<float3>(blockMeshVertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                blockMeshData.GetVertices(vertices.Reinterpret<Vector3>());

                var normals = new NativeArray<float3>(blockMeshVertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                blockMeshData.GetNormals(normals.Reinterpret<Vector3>());

                var uvs = new NativeArray<float3>(blockMeshVertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                blockMeshData.GetUVs(0, uvs.Reinterpret<Vector3>());

                var uvs2 = new NativeArray<float3>(blockMeshVertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                blockMeshData.GetUVs(1, uvs2.Reinterpret<Vector3>());

                // get raw vertex data refs 
                NativeArray<Vector3> outputVerts = outputMesh.GetVertexData<Vector3>();
                NativeArray<Vector3> outputNormals = outputMesh.GetVertexData<Vector3>(stream: 1);
                NativeArray<Vector3> outputUVs = outputMesh.GetVertexData<Vector3>(stream: 2);
                NativeArray<Vector3> outputUVs2 = outputMesh.GetVertexData<Vector3>(stream: 3);

                // write input data into output 
                for (int i = 0; i < blockMeshVertexCount; i++)
                {
                    outputVerts[vertexStartIndex + i] = vertices[i];
                    outputNormals[vertexStartIndex + i] = normals[i];
                    outputUVs[vertexStartIndex + i] = uvs[i];
                    outputUVs2[vertexStartIndex + i] = uvs2[i];
                }

                // dispose all allocated memory
                vertices.Dispose();
                normals.Dispose();
                uvs.Dispose();
                uvs2.Dispose();

                // write to output index buffer 
                var trianglesStartIndex = trianglesStartIndices[index];
                var triangleCount = blockMeshData.GetSubMesh(0).indexCount;
                var outputTriangles = outputMesh.GetIndexData<int>();

                // index buffer can come with different data types
                if (blockMeshData.indexFormat == IndexFormat.UInt16)
                {
                    var triangles = blockMeshData.GetIndexData<ushort>();

                    for (int i = 0; i < triangleCount; ++i)
                    {
                        int idx = triangles[i];
                        outputTriangles[trianglesStartIndex + i] = vertexStartIndex + idx;
                    }
                }
                else
                {
                    var triangles = blockMeshData.GetIndexData<int>();

                    for (int i = 0; i < triangleCount; ++i)
                    {
                        int idx = triangles[i];
                        outputTriangles[trianglesStartIndex + i] = vertexStartIndex + idx;
                    }
                }
            }
        }

        struct CalculateBlockTypesJob : IJobParallelFor
        {
            public int width;
            public int height;
            public Vector3 chunkCoordinate;
            public int waterLevel;

            public NativeArray<BlockType> cData;
            public NativeArray<BlockType> hData;
            public NativeArray<Unity.Mathematics.Random> randoms;

            public void Execute(int i)
            {
                int xPos = i % width + (int)chunkCoordinate.x;
                int yPos = (i / width) % height + (int)chunkCoordinate.y;
                int zPos = i / (width * height) + (int)chunkCoordinate.z;

                var random = randoms[i];

                int surfaceHeight = Mathf.RoundToInt(MeshUtils.fBM(
                    xPos,
                    zPos,
                    WorldBuilder.surfaceSettings.octaves,
                    WorldBuilder.surfaceSettings.scale,
                    WorldBuilder.surfaceSettings.heightScale,
                    WorldBuilder.surfaceSettings.heightOffset));

                int stoneHeight = Mathf.RoundToInt(MeshUtils.fBM(
                    xPos,
                    zPos,
                    WorldBuilder.stoneSettings.octaves,
                    WorldBuilder.stoneSettings.scale,
                    WorldBuilder.stoneSettings.heightScale,
                    WorldBuilder.stoneSettings.heightOffset));

                int diamondTopHeight = Mathf.RoundToInt(MeshUtils.fBM(
                    xPos,
                    zPos,
                    WorldBuilder.diamondTopSettings.octaves,
                    WorldBuilder.diamondTopSettings.scale,
                    WorldBuilder.diamondTopSettings.heightScale,
                    WorldBuilder.diamondTopSettings.heightOffset));

                int diamondBottomHeight = Mathf.RoundToInt(MeshUtils.fBM(
                    xPos,
                    zPos,
                    WorldBuilder.diamondBottomSettings.octaves,
                    WorldBuilder.diamondBottomSettings.scale,
                    WorldBuilder.diamondBottomSettings.heightScale,
                    WorldBuilder.diamondBottomSettings.heightOffset));

                hData[i] = BlockType.Nocrack;

                float digCave = MeshUtils.fBM3D(xPos, yPos, zPos, WorldBuilder.caveSettings.octaves, WorldBuilder.caveSettings.scale,
                    WorldBuilder.caveSettings.heightScale, WorldBuilder.caveSettings.heightOffset);

                float plantTree = MeshUtils.fBM3D(xPos, yPos, zPos, WorldBuilder.treeSettings.octaves, WorldBuilder.treeSettings.scale,
                    WorldBuilder.treeSettings.heightScale, WorldBuilder.treeSettings.heightOffset);

                if (yPos == 0)
                {
                    cData[i] = BlockType.Bedrock;
                }
                else if (yPos > surfaceHeight)
                {
                    if (yPos < waterLevel)
                    {
                        cData[i] = BlockType.Water;
                    }
                    else
                    {
                        cData[i] = BlockType.Air;
                    }
                }
                else if (yPos == surfaceHeight)
                {
                    if (plantTree < WorldBuilder.treeSettings.drawCutOff && random.NextFloat() <= 0.2f)
                    {
                        cData[i] = BlockType.Woodbase;
                    }
                    else
                    {
                        cData[i] = BlockType.GrassTop;
                    }
                }
                else if (digCave < WorldBuilder.caveSettings.drawCutOff)
                {
                    cData[i] = BlockType.Air;
                }
                else if (yPos < stoneHeight && random.NextFloat() < WorldBuilder.stoneSettings.probability)
                {
                    cData[i] = BlockType.Stone;
                }
                else if (yPos > diamondBottomHeight && yPos < diamondTopHeight && random.NextFloat() < WorldBuilder.diamondTopSettings.probability)
                {
                    cData[i] = BlockType.Diamond;
                }
                else
                {
                    cData[i] = BlockType.Dirt;
                }
            }
        }
    }
}