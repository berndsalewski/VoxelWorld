using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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
        public int xBlockCount = 1;
        [HideInInspector]
        public int yBlockCount = 1;
        [HideInInspector]
        public int zBlockCount = 1;

        [HideInInspector]
        public Vector3Int coordinates;

        /// 3-dimensional array, x,y,z, relative coordinates within a chunk for a block
        public Block[,,] blocks;

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
                index % World.chunkDimensions.x,
                (index / World.chunkDimensions.x) % World.chunkDimensions.y,
                index / (World.chunkDimensions.x * World.chunkDimensions.y));
        }

        /// <summary>
        /// calculates the index (in chunkData) of a block from a vector position
        /// </summary>
        public static int ToBlockIndex(Vector3Int coordinates)
        {
            // i = x + WIDTH * (y + DEPTH * z)
            return coordinates.x + World.chunkDimensions.x * (coordinates.y + World.chunkDimensions.z * coordinates.z);
        }

        /// <summary>
        /// generates the data structure which defines which block is what type
        /// </summary>
        private void GenerateChunkData(int waterLevel)
        {
            int blockCount = xBlockCount * yBlockCount * zBlockCount;
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
                width = xBlockCount,
                height = yBlockCount,
                location = coordinates,
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

        /// <summary>
        /// creates a chunk of blocks, creates the actual geometry
        /// </summary>
        public void CreateChunk(Vector3Int chunkDimensions, Vector3Int chunkCoordinates, int waterLevel, bool regenerateChunkData = true)
        {
            coordinates = chunkCoordinates;
            xBlockCount = chunkDimensions.x;
            yBlockCount = chunkDimensions.y;
            zBlockCount = chunkDimensions.z;
            
            MeshFilter meshFilterSolid;
            MeshRenderer meshRendererSolid;
            MeshFilter meshFilterFluid;
            MeshRenderer meshRendererFluid;


            if (solidMesh == null)
            {
                solidMesh = new GameObject("Solid");
                solidMesh.transform.parent = this.gameObject.transform;
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
                fluidMesh.transform.parent = this.gameObject.transform;
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

            blocks = new Block[xBlockCount, yBlockCount, zBlockCount];

            if (regenerateChunkData)
            {
                GenerateChunkData(waterLevel);
            }

            for (int pass = 0; pass < 2; pass++)
            {
                var inputMeshes = new List<Mesh>();
                int vertexStart = 0;
                int triStart = 0;
                int meshCount = xBlockCount * yBlockCount * zBlockCount;
                int m = 0;
                var job = new ProcessMeshDataJob();
                job.vertexStart = new NativeArray<int>(meshCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                job.triStart = new NativeArray<int>(meshCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);


                for (int z = 0; z < zBlockCount; z++)
                {
                    for (int y = 0; y < yBlockCount; y++)
                    {
                        for (int x = 0; x < xBlockCount; x++)
                        {
                            Vector3Int blockCoordinates = new Vector3Int(x, y, z);
                            int blockIndex = Chunk.ToBlockIndex(blockCoordinates);

                            // create a block and it's meshes
                            blocks[x, y, z] = new Block(
                                blockCoordinates,
                                this.coordinates,
                                chunkData[blockIndex],
                                this,
                                healthData[blockIndex]);

                            bool isWater = MeshUtils.canFlow.Contains(chunkData[blockIndex]);
                            // add the mesh as input for the jobsystem
                            if (blocks[x, y, z].mesh != null
                                && ((pass == 0 && !isWater) || (pass == 1 && isWater))
                            )
                            {
                                inputMeshes.Add(blocks[x, y, z].mesh);
                                // get the vertex count of the current block's mesh
                                var vCount = blocks[x, y, z].mesh.vertexCount;
                                // get the length of the index buffer of the current block's mesh
                                var iCount = (int)(blocks[x, y, z].mesh.GetIndexCount(0));
                                // store the index of the first vertex of the current mesh in the job
                                job.vertexStart[m] = vertexStart;
                                // store the index of the first index buffer entry of the current mesh in the job
                                job.triStart[m] = triStart;
                                // move both values forward for the next mesh
                                vertexStart += vCount;
                                triStart += iCount;
                                // increment the loop/mesh counter
                                m++;
                            }
                        }
                    }
                }

                job.meshData = Mesh.AcquireReadOnlyMeshData(inputMeshes);
                var outputMeshData = Mesh.AllocateWritableMeshData(1);
                job.outputMesh = outputMeshData[0];
                job.outputMesh.SetIndexBufferParams(triStart, IndexFormat.UInt32);
                job.outputMesh.SetVertexBufferParams(vertexStart,
                    new VertexAttributeDescriptor(VertexAttribute.Position),
                    new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 2),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord1, stream: 3)
                    );

                var handle = job.Schedule(inputMeshes.Count, 4);
                var newMesh = new Mesh();
                newMesh.name = $"Chunk_{this.coordinates.x}_{this.coordinates.y}_{this.coordinates.z}";
                name = newMesh.name;
                var sm = new SubMeshDescriptor(0, triStart, MeshTopology.Triangles);
                sm.firstVertex = 0;
                sm.vertexCount = vertexStart;

                handle.Complete();

                job.outputMesh.subMeshCount = 1;
                job.outputMesh.SetSubMesh(0, sm);

                Mesh.ApplyAndDisposeWritableMeshData(outputMeshData, new[] { newMesh });
                job.meshData.Dispose();
                job.vertexStart.Dispose();
                job.triStart.Dispose();
                newMesh.RecalculateBounds();

                if (pass == 0)
                {
                    meshFilterSolid.mesh = newMesh;
                    MeshCollider collider = solidMesh.AddComponent<MeshCollider>();
                    collider.sharedMesh = meshFilterSolid.mesh;
                }
                else
                {
                    meshFilterFluid.mesh = newMesh;
                    MeshCollider collider = fluidMesh.AddComponent<MeshCollider>();
                    fluidMesh.layer = 4;
                    collider.sharedMesh = meshFilterFluid.mesh;
                }
            }
        }

        /// <summary>
        /// merges a collection of input meshes into one output mesh
        /// </summary>
        [BurstCompile]
        struct ProcessMeshDataJob : IJobParallelFor
        {
            [ReadOnly]
            public Mesh.MeshDataArray meshData;
            public Mesh.MeshData outputMesh;
            public NativeArray<int> vertexStart;
            public NativeArray<int> triStart;

            public void Execute(int index)
            {
                Mesh.MeshData data = meshData[index];
                int vCount = data.vertexCount;
                int vStart = vertexStart[index];

                var verts = new NativeArray<float3>(vCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                data.GetVertices(verts.Reinterpret<Vector3>());

                var normals = new NativeArray<float3>(vCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                data.GetNormals(normals.Reinterpret<Vector3>());

                var uvs = new NativeArray<float3>(vCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                data.GetUVs(0, uvs.Reinterpret<Vector3>());

                var uvs2 = new NativeArray<float3>(vCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                data.GetUVs(1, uvs2.Reinterpret<Vector3>());

                var outputVerts = outputMesh.GetVertexData<Vector3>();
                var outputNormals = outputMesh.GetVertexData<Vector3>(stream: 1);
                var outputUVs = outputMesh.GetVertexData<Vector3>(stream: 2);
                var outputUVs2 = outputMesh.GetVertexData<Vector3>(stream: 3);

                for (int i = 0; i < vCount; i++)
                {
                    outputVerts[i + vStart] = verts[i];
                    outputNormals[i + vStart] = normals[i];
                    outputUVs[i + vStart] = uvs[i];
                    outputUVs2[i + vStart] = uvs2[i];
                }

                verts.Dispose();
                normals.Dispose();
                uvs.Dispose();
                uvs2.Dispose();

                var tStart = triStart[index];
                var tCount = data.GetSubMesh(0).indexCount;
                var outputTris = outputMesh.GetIndexData<int>();

                if (data.indexFormat == IndexFormat.UInt16)
                {
                    var tris = data.GetIndexData<ushort>();
                    for (int i = 0; i < tCount; ++i)
                    {
                        int idx = tris[i];
                        outputTris[i + tStart] = vStart + idx;
                    }
                }
                else
                {
                    var tris = data.GetIndexData<int>();
                    for (int i = 0; i < tCount; ++i)
                    {
                        int idx = tris[i];
                        outputTris[i + tStart] = vStart + idx;
                    }
                }
            }
        }

        //TODO is this not BurstCompile?
        struct CalculateBlockTypesJob : IJobParallelFor
        {
            public NativeArray<BlockType> cData;
            public NativeArray<BlockType> hData;
            public int width;
            public int height;
            public Vector3 location;
            public NativeArray<Unity.Mathematics.Random> randoms;
            public int waterLevel;

            public void Execute(int i)
            {
                int xPos = i % width + (int)location.x;
                int yPos = (i / width) % height + (int)location.y;
                int zPos = i / (width * height) + (int)location.z;

                var random = randoms[i];

                int surfaceHeight = Mathf.RoundToInt(MeshUtils.fBM(
                    xPos,
                    zPos,
                    World.surfaceSettings.octaves,
                    World.surfaceSettings.scale,
                    World.surfaceSettings.heightScale,
                    World.surfaceSettings.heightOffset));

                int stoneHeight = Mathf.RoundToInt(MeshUtils.fBM(
                    xPos,
                    zPos,
                    World.stoneSettings.octaves,
                    World.stoneSettings.scale,
                    World.stoneSettings.heightScale,
                    World.stoneSettings.heightOffset));

                int diamondTopHeight = Mathf.RoundToInt(MeshUtils.fBM(
                    xPos,
                    zPos,
                    World.diamondTopSettings.octaves,
                    World.diamondTopSettings.scale,
                    World.diamondTopSettings.heightScale,
                    World.diamondTopSettings.heightOffset));

                int diamondBottomHeight = Mathf.RoundToInt(MeshUtils.fBM(
                    xPos,
                    zPos,
                    World.diamondBottomSettings.octaves,
                    World.diamondBottomSettings.scale,
                    World.diamondBottomSettings.heightScale,
                    World.diamondBottomSettings.heightOffset));

                hData[i] = BlockType.Nocrack;

                float digCave = MeshUtils.fBM3D(xPos, yPos, zPos, World.caveSettings.octaves, World.caveSettings.scale,
                    World.caveSettings.heightScale, World.caveSettings.heightOffset);

                float plantTree = MeshUtils.fBM3D(xPos, yPos, zPos, World.treeSettings.octaves, World.treeSettings.scale,
                    World.treeSettings.heightScale, World.treeSettings.heightOffset);

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
                    if (plantTree < World.treeSettings.drawCutOff && random.NextFloat() <= 0.2f)
                    {
                        cData[i] = BlockType.Woodbase;
                    }
                    else
                    {
                        cData[i] = BlockType.GrassTop;
                    }
                }
                else if (digCave < World.caveSettings.drawCutOff)
                {
                    cData[i] = BlockType.Air;
                }
                else if (yPos < stoneHeight && random.NextFloat() < World.stoneSettings.probability)
                {
                    cData[i] = BlockType.Stone;
                }
                else if (yPos > diamondBottomHeight && yPos < diamondTopHeight && random.NextFloat() < World.diamondTopSettings.probability)
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