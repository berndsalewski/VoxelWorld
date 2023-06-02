namespace VoxelWorld
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [ExecuteInEditMode]
    public class Perlin3DGrapher : MonoBehaviour
    {
        Vector3 dimensions = new Vector3(15, 15, 15);

        [Range(1, 10)]
        public float heightScale = 1;
        [Range(0.01f, 1)]
        public float scale = 1f;
        [Range(1, 10)]
        public int octaves = 1;
        [Range(-20, 20)]
        public float heightOffset = 0;
        [Range(1, 10)]
        public float DrawCutOff = 1;


        private void CreateCubes()
        {
            int count = 0;
            for (int z = 0; z < dimensions.z; z++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    for (int x = 0; x < dimensions.x; x++)
                    {
                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.name = "perlin_cube";
                        cube.transform.parent = this.transform;
                        cube.transform.position = new Vector3(x, y, z);
                        count++;
                    }
                }
            }
        }

        public void Graph()
        {
            MeshRenderer[] cubes = this.GetComponentsInChildren<MeshRenderer>();

            if (cubes.Length == 0)
            {
                CreateCubes();
            }

            if (cubes.Length == 0)
            {
                return;
            }

            for (int z = 0; z < dimensions.z; z++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    for (int x = 0; x < dimensions.x; x++)
                    {
                        float p3d = MeshUtils.fBM3D(x, y, z, octaves, scale, heightScale, heightOffset);
                        Debug.Log($"value {p3d}");
                        if (p3d < DrawCutOff)
                        {
                            cubes[x + (int)dimensions.x * (y + (int)dimensions.z * z)].enabled = true;
                        }
                        else
                        {
                            cubes[x + (int)dimensions.x * (y + (int)dimensions.z * z)].enabled = false;
                        }
                    }
                }
            }
        }
    }
}