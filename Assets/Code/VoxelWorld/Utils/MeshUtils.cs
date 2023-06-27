using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelWorld
{
    public static class MeshUtils
    {
        public static int[] blockTypeHealth = { 2, 2, 1, 1, 4, 2, 4, 4, 3, 4, -1, 3, 4, -1, -1, -1, -1, -1, -1 };

        public static HashSet<BlockType> canDrop = new HashSet<BlockType> { BlockType.Sand, BlockType.Water };
        public static HashSet<BlockType> canFlow = new HashSet<BlockType> { BlockType.Water };

        // order: left-bottom, left-top, right-top, right-bottom
        public static Vector2[,] BlockUVs =
        {
        // GrassTop
        { new Vector2(0.125f, 0.375f), new Vector2(0.125f, 0.4375f), new Vector2(0.1875f, 0.4375f), new Vector2(0.1875f, 0.375f)},
        // GrassSide
        { new Vector2(0.1875f, 0.9375f), new Vector2(0.1875f, 1.0f), new Vector2(0.25f, 1.0f), new Vector2(0.25f, 0.9375f)},
        // Dirt
        { new Vector2(0.125f, 0.9375f), new Vector2(0.125f, 1.0f), new Vector2(0.1875f, 1.0f), new Vector2(0.1875f, 0.9375f)},
        // Water
        { new Vector2(0.875f, 0.125f), new Vector2(0.875f, 0.1875f), new Vector2(0.9375f, 0.1875f), new Vector2(0.9375f, 0.125f)},
        // Stone
        { new Vector2(0, 0.875f), new Vector2(0, 0.9375f), new Vector2(0.0625f, 0.9375f), new Vector2(0.0625f, 0.875f)},
        // Leaves
        { new Vector2(0.0625f,0.375f), new Vector2(0.0625f,0.4375f), new Vector2(0.125f,0.4375f), new Vector2(0.125f,0.375f)},
        // Wood
        { new Vector2(0.375f,0.625f),  new Vector2(0.375f,0.6875f), new Vector2(0.4375f,0.6875f), new Vector2(0.4375f,0.625f)},
        // Woodbase
        { new Vector2(0.375f,0.625f),  new Vector2(0.375f,0.6875f), new Vector2(0.4375f,0.6875f), new Vector2(0.4375f,0.625f)},
        // Sand
        { new Vector2(0.125f, 0.875f), new Vector2(0.125f, 0.9375f), new Vector2(0.1875f, 0.9375f), new Vector2(0.1875f, 0.875f)},
        // Gold
        { new Vector2(0f,0.8125f), new Vector2(0f,0.875f), new Vector2(0.0625f,0.875f), new Vector2(0.0625f,0.8125f)},
        // Bedrock
        { new Vector2( 0.3125f, 0.8125f ), new Vector2( 0.3125f, 0.875f ), new Vector2( 0.375f, 0.875f ), new Vector2( 0.375f, 0.8125f)},
        // Redstone
        {new Vector2( 0.1875f, 0.75f ), new Vector2( 0.1875f, 0.8125f ),new Vector2( 0.25f, 0.8125f ), new Vector2( 0.25f, 0.75f)},
        // Diamond
        {new Vector2( 0.125f, 0.75f ), new Vector2( 0.125f, 0.8125f ), new Vector2( 0.1875f, 0.8125f ), new Vector2( 0.1875f, 0.75f)},
        // Nocrack
        {new Vector2( 0.6875f, 0f ), new Vector2( 0.6875f, 0.0625f ),new Vector2( 0.75f, 0.0625f ), new Vector2( 0.75f, 0f)},
        // Crack1
        { new Vector2(0f,0f), new Vector2(0f,0.0625f), new Vector2(0.0625f,0.0625f), new Vector2(0.0625f,0f)},
        // Crack2
        { new Vector2(0.0625f,0f), new Vector2(0.0625f,0.0625f), new Vector2(0.125f,0.0625f), new Vector2(0.125f,0f)},
        // Crack3
        { new Vector2(0.125f,0f), new Vector2(0.125f,0.0625f), new Vector2(0.1875f,0.0625f), new Vector2(0.1875f,0f)},
        // Crack4
        { new Vector2(0.1875f,0f), new Vector2(0.1875f,0.0625f), new Vector2(0.25f,0.0625f), new Vector2(0.25f,0f)}
    };

        public static float fBM(float x, float z, int octaves, float scale, float heightScale, float heightOffset)
        {
            float total = 0;
            float frequency = 1;
            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(x * scale * frequency, z * scale * frequency) * heightScale;
                frequency *= 2;
            }
            return total + heightOffset;
        }

        public static float fBM3D(float x, float y, float z, int octaves, float scale, float heightScale, float heightOffset)
        {
            float xy = fBM(x, y, octaves, scale, heightScale, heightOffset);
            float yz = fBM(y, z, octaves, scale, heightScale, heightOffset);
            float xz = fBM(x, z, octaves, scale, heightScale, heightOffset);
            float yx = fBM(y, x, octaves, scale, heightScale, heightOffset);
            float zy = fBM(z, y, octaves, scale, heightScale, heightOffset);
            float zx = fBM(z, x, octaves, scale, heightScale, heightOffset);

            return (xy + yz + xz + yx + zy + zx) / 6;
        }


        static private ProfilerMarker _profilerMarker_MergeMeshes = new ("MergeQuadMeshesJob");  
        /// <summary>
        /// merge meshes multi threading
        /// </summary>
        static public Mesh MergeMeshesWithJobSystem(Mesh[] meshes)
        {
            _profilerMarker_MergeMeshes.Begin();
            //prepare job data
            Mesh.MeshDataArray inputMeshes = Mesh.AcquireReadOnlyMeshData(meshes);
            Mesh.MeshDataArray outputMeshes = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData outMeshData = outputMeshes[0];

            // configure the output data structure 
            int totalVertexCount = 0;
            int totalIndexCount = 0;
            foreach (var inMesh in meshes)
            {
                totalVertexCount += inMesh.vertexCount;
                totalIndexCount += (int)inMesh.GetIndexCount(0);
            }

            outMeshData.SetVertexBufferParams(totalVertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, stream: 3)
                );
            outMeshData.SetIndexBufferParams(totalIndexCount, IndexFormat.UInt32);

            //configure job
            MergeMeshesJob mergeJob = new MergeMeshesJob()
            {
                inputMeshes = inputMeshes,
                outMeshData = outMeshData
            };

            // schedule job for execution
            JobHandle mergeJobHandle = mergeJob.Schedule(meshes.Length, 4);
            int meshCount = meshes.Length;
            JobHandle mergeJobHandle = mergeJob.Schedule(meshCount, 4);

            for(int i = 0; i < meshCount; i++)
            {
                Object.Destroy(meshes[i]);
            }

            mergeJobHandle.Complete();

            // define some mesh configuration on the out mesh before getting the data
            var meshDescriptor = new SubMeshDescriptor(0, totalIndexCount, MeshTopology.Triangles);
            meshDescriptor.firstVertex = 0;
            meshDescriptor.vertexCount = totalVertexCount;
            outMeshData.subMeshCount = 1;
            outMeshData.SetSubMesh(0, meshDescriptor);

            // copy meshData to Mesh
            Mesh mergedMesh = new Mesh();
            mergedMesh.name = "Merged Mesh";
            Mesh.ApplyAndDisposeWritableMeshData(outputMeshes, mergedMesh);

            inputMeshes.Dispose();

            mergedMesh.RecalculateNormals();
            mergedMesh.RecalculateBounds();

            _profilerMarker_MergeMeshes.End();
            return mergedMesh;
        }

        [BurstCompile]
        private struct MergeMeshesJob : IJobParallelFor
        {
            [ReadOnly]
            public Mesh.MeshDataArray inputMeshes;

            public Mesh.MeshData outMeshData;

            public void Execute(int index)
            {
                Mesh.MeshData currentMeshData = inputMeshes[index];

                // copy position data from vertex buffer
                NativeArray<Vector3> inVertices = new NativeArray<Vector3>(currentMeshData.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                currentMeshData.GetVertices(inVertices);
                NativeArray<Vector3> outVertices = outMeshData.GetVertexData<Vector3>();

                // copy uv1 data from vertex buffer
                NativeArray<Vector3> inUVs_1 = new NativeArray<Vector3>(currentMeshData.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                currentMeshData.GetUVs(0, inUVs_1);
                NativeArray<Vector3> outUVs_1 = outMeshData.GetVertexData<Vector3>(stream: 2);

                // copy uv1 data from vertex buffer
                NativeArray<Vector3> inUVs_2 = new NativeArray<Vector3>(currentMeshData.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                currentMeshData.GetUVs(1, inUVs_2);
                NativeArray<Vector3> outUVs_2 = outMeshData.GetVertexData<Vector3>(stream: 3);


                // copy vertex data
                int vertexStartIndex = index * currentMeshData.vertexCount;
                for (int j = 0; j < currentMeshData.vertexCount; j++)
                {
                    outVertices[vertexStartIndex + j] = inVertices[j];
                    outUVs_1[vertexStartIndex + j] = inUVs_1[j];
                    outUVs_2[vertexStartIndex + j] = inUVs_2[j];
                }

                // copy index buffer
                NativeArray<int> outIndices = outMeshData.GetIndexData<int>();
                int indexBufferCount = currentMeshData.GetSubMesh(0).indexCount;
                int indexStartIndex = index * indexBufferCount;
                if (currentMeshData.indexFormat == IndexFormat.UInt16)
                {
                    NativeArray<ushort> currentIndexBuffer = currentMeshData.GetIndexData<ushort>();
                    for (int j = 0; j < indexBufferCount; j++)
                    {
                        ushort currentIndex = currentIndexBuffer[j];
                        outIndices[indexStartIndex + j] = vertexStartIndex + currentIndex;
                    }
                }
                else
                {
                    NativeArray<int> currentIndexBuffer = currentMeshData.GetIndexData<int>();
                    for (int j = 0; j < indexBufferCount; j++)
                    {
                        int currentIndex = currentIndexBuffer[j];
                        outIndices[indexStartIndex + j] = vertexStartIndex + currentIndex;
                    }
                }
            }
        }

        public static void PrintVertices(Mesh mesh)
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                output.AppendLine($"{i}.{mesh.vertices[i]}");
            }
            Debug.Log($"{output}");
        }
    }
}