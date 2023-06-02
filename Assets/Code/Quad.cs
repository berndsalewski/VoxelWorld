namespace VoxelWorld
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// creates a quad, can create any of the 6 sides of a quad, center of the quad is 0,0,0
    ///
    ///    p6---p7
    ///   /|   /|
    ///  / p5-/-p4
    /// p1---p2/
    /// |/   |/
    /// p0---p3
    /// 
    /// </summary>
    public class Quad
    {
        public Mesh mesh;

        static Vector3 p0 = new Vector3(-0.5f, -0.5f, -0.5f);
        static Vector3 p1 = new Vector3(-0.5f, 0.5f, -0.5f);
        static Vector3 p2 = new Vector3(0.5f, 0.5f, -0.5f);
        static Vector3 p3 = new Vector3(0.5f, -0.5f, -0.5f);
        static Vector3 p4 = new Vector3(0.5f, -0.5f, 0.5f);
        static Vector3 p5 = new Vector3(-0.5f, -0.5f, 0.5f);
        static Vector3 p6 = new Vector3(-0.5f, 0.5f, 0.5f);
        static Vector3 p7 = new Vector3(0.5f, 0.5f, 0.5f);

        public Quad(BlockSide side, Vector3 offset, BlockType blockType, BlockType healthType)
        {
            Vector3[] vertices;
            Vector3[] normals;

            if (blockType == BlockType.GrassTop && side != BlockSide.Top)
            {
                if (side == BlockSide.Bottom)
                {
                    blockType = BlockType.Dirt;
                }
                else
                {
                    blockType = BlockType.GrassSide;
                }
            }

            switch (side)
            {
                // points order: bottomLeft, topLeft, topRight, bottomRight
                case BlockSide.Front:
                    vertices = new Vector3[] { p0, p1, p2, p3 };
                    normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
                    break;
                case BlockSide.Back:
                    vertices = new Vector3[] { p4, p7, p6, p5 };
                    normals = new Vector3[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
                    break;
                case BlockSide.Left:
                    vertices = new Vector3[] { p5, p6, p1, p0 };
                    normals = new Vector3[] { Vector3.left, Vector3.left, Vector3.left, Vector3.left };
                    break;
                case BlockSide.Right:
                    vertices = new Vector3[] { p3, p2, p7, p4 };
                    normals = new Vector3[] { Vector3.right, Vector3.right, Vector3.right, Vector3.right };
                    break;
                case BlockSide.Top:
                    vertices = new Vector3[] { p1, p6, p7, p2 };
                    normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                    break;
                case BlockSide.Bottom:
                    vertices = new Vector3[] { p3, p4, p5, p0 };
                    normals = new Vector3[] { Vector3.down, Vector3.down, Vector3.down, Vector3.down };
                    break;
                default:
                    return;
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += offset;
            }

            mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = new Vector2[] {
            MeshUtils.BlockUVs[(int)blockType, 0],
            MeshUtils.BlockUVs[(int)blockType, 1],
            MeshUtils.BlockUVs[(int)blockType, 2],
            MeshUtils.BlockUVs[(int)blockType, 3]
        };

            mesh.uv2 = new Vector2[] {
            MeshUtils.BlockUVs[(int)healthType, 0],
            MeshUtils.BlockUVs[(int)healthType, 1],
            MeshUtils.BlockUVs[(int)healthType, 2],
            MeshUtils.BlockUVs[(int)healthType, 3]
        };

            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            ;

            // TODO: docs say that setting triangles automatically recalculates bounds, so this is not necessary?
            // https://docs.unity3d.com/ScriptReference/Mesh.RecalculateBounds.html
            mesh.RecalculateBounds();
        }
    }
}