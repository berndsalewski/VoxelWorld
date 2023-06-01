using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// create a cube, seen from the front
// Front
// p1---p2
// |    |
// p0---p3
//
// Back
// p7---p6
// |    |
// p4---p5
//
// Top
// p6---p7
// |    |
// p1---p2
//
// Bottom
// p0---p3
// |    |
// p5---p4
//
// Left
// p6---p1
// |    |
// p5---p0
//
// Right
// p2---p7
// |    |
// p3---p4
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

    public Quad(MeshUtils.BlockSide side, Vector3 offset, MeshUtils.BlockType blockType, MeshUtils.BlockType healthType)
    {
        Vector3[] vertices;
        Vector3[] normals;

        if(blockType == MeshUtils.BlockType.GrassTop && side != MeshUtils.BlockSide.Top)
        {
            if (side == MeshUtils.BlockSide.Bottom)
            {
                blockType = MeshUtils.BlockType.Dirt;
            }
            else
            {
                blockType = MeshUtils.BlockType.GrassSide;
            }
        }

        switch (side)
        {
            case MeshUtils.BlockSide.Front:
                vertices = new Vector3[] { p0, p1, p2, p3 };
                normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
                break;
            case MeshUtils.BlockSide.Back:
                vertices = new Vector3[] { p4, p7, p6, p5 };
                normals = new Vector3[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
                break;
            case MeshUtils.BlockSide.Left:
                vertices = new Vector3[] { p5, p6, p1, p0 };
                normals = new Vector3[] { Vector3.left, Vector3.left, Vector3.left, Vector3.left };
                break;
            case MeshUtils.BlockSide.Right:
                vertices = new Vector3[] { p3, p2, p7, p4 };
                normals = new Vector3[] { Vector3.right, Vector3.right, Vector3.right, Vector3.right };
                break;
            case MeshUtils.BlockSide.Top:
                vertices = new Vector3[] { p1, p6, p7, p2 };
                normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                break;
            case MeshUtils.BlockSide.Bottom:
                vertices = new Vector3[] { p5, p0, p3, p4 };
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
        
        // penny's triangles are a little different -> 3,1,0,3,2,1
        // not a big deal but it's why her uv coords look distorted in my project
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 }; ;

        // docs say that setting triangles automatically recalculates bounds, so this is not necessary?
        // https://docs.unity3d.com/ScriptReference/Mesh.RecalculateBounds.html
        mesh.RecalculateBounds();

    }
}
