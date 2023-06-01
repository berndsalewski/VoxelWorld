using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VertexData = System.Tuple<UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector2, UnityEngine.Vector2>;

public static class MeshUtils
{
    public enum BlockType
    {
        GrassTop, GrassSide, Dirt, Water, Stone,Leaves, Wood, Woodbase, Sand, Gold, Bedrock, Redstone, Diamond, Nocrack, Crack1, Crack2, Crack3, Crack4, Air
    }

    public static int[] blockTypeHealth = { 2, 2, 1, 1, 4, 2, 4, 4, 3, 4, -1, 3, 4, -1, -1, -1, -1, -1, -1 };

    public static HashSet<BlockType> canDrop = new HashSet<BlockType> {BlockType.Sand,BlockType.Water};
    public static HashSet<BlockType> canFlow = new HashSet<BlockType> {BlockType.Water};

    public enum BlockSide { Left, Right, Top, Bottom, Front, Back }

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

    /// <summary>
    /// merges all passed meshes (blocks) into one output mesh (chunk)
    /// </summary>
    public static Mesh MergeMeshes(Mesh[] meshes)
    {
        Mesh outputMesh = new Mesh();

        Dictionary<VertexData, int> vertexToPointIndexLookup = new Dictionary<VertexData, int>();
        HashSet<VertexData> processedVertices = new HashSet<VertexData>();
        List<int> newTriangles = new List<int>();

        int pointIndex = 0;
        // get the next mesh and process it
        foreach (Mesh currentMesh in meshes)
        {
            if (currentMesh == null)
            {
                continue;
            }

            // extract the data of every vertex of the current mesh and store
            // the vertexdata in an dictionary together with an upcounting point index
            int numVertices = currentMesh.vertices.Length;
            for (int i = 0; i < numVertices; i++)
            {
                Vector3 vertex = currentMesh.vertices[i];
                Vector3 normal = currentMesh.normals[i];
                Vector2 uv1 = currentMesh.uv[i];
                Vector2 uv2 = currentMesh.uv2[i];
                VertexData vertexData = new VertexData(vertex, normal, uv1, uv2);

                if (!processedVertices.Contains(vertexData))
                {
                    vertexToPointIndexLookup.Add(vertexData, pointIndex);
                    processedVertices.Add(vertexData);
                    pointIndex++;
                }
            }

            // iterate over the current triangle buffer and look up the new point
            // index with vertexdata stored in the lookup dictionary and use this
            // point index in the new version of the triangle buffer
            int trianglesLength = currentMesh.triangles.Length;
            for (int j = 0; j < trianglesLength; j++)
            {
                int oldIndex = currentMesh.triangles[j];
                Vector3 vertex = currentMesh.vertices[oldIndex];
                Vector3 normal = currentMesh.normals[oldIndex];
                Vector2 uv1 = currentMesh.uv[oldIndex];
                Vector2 uv2 = currentMesh.uv2[oldIndex];
                VertexData point = new VertexData(vertex, normal, uv1, uv2);

                vertexToPointIndexLookup.TryGetValue(point, out int index);
                newTriangles.Add(index);
            }
        }

        // at this point we have an all new triangle buffer for all the meshes
        ExtractVertexDataIntoMesh(vertexToPointIndexLookup, outputMesh);
        outputMesh.triangles = newTriangles.ToArray();
        outputMesh.RecalculateBounds();
        return outputMesh;
    }

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

    /// <summary>
    /// extracts vertex data from the dictionary keys and writes it into the output mesh
    /// </summary>
    /// <param name="pointsData"></param>
    /// <param name="outputMesh"></param>
    private static void ExtractVertexDataIntoMesh(Dictionary<VertexData, int> pointsData, Mesh outputMesh)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> uvs2 = new List<Vector2>();

        foreach (VertexData v in pointsData.Keys)
        {
            vertices.Add(v.Item1);
            normals.Add(v.Item2);
            uvs.Add(v.Item3);
            uvs2.Add(v.Item4);
        }

        outputMesh.vertices = vertices.ToArray();
        outputMesh.normals = normals.ToArray();
        outputMesh.uv = uvs.ToArray();
        outputMesh.uv2 = uvs2.ToArray();
    }
}