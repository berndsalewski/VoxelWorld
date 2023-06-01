using System.Text;
using UnityEngine;

public class MeshReporter : MonoBehaviour
{
    void Start()
    {
        PrintMeshInfo();
    }

    private void PrintMeshInfo()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uv = mesh.uv;
        int[] triangles = mesh.triangles;

        StringBuilder report = new StringBuilder();
        report.Append($"Num Vertices: {vertices.Length}");
        report.AppendLine();

        report.Append("Vertices");
        for (int i = 0; i < vertices.Length; i++)
        {
            report.Append($"|{i}: {vertices[i]}");
        }

        report.AppendLine();
        report.AppendLine();

        report.Append("Normals");
        for (int i = 0; i < normals.Length; i++)
        {
            report.Append($"|{i}: {normals[i]}");
        }

        report.AppendLine();
        report.AppendLine();

        report.Append("UVs");
        for (int i = 0; i < uv.Length; i++)
        {
            report.Append($"|{i}: {uv[i]}");
        }

        report.AppendLine();
        report.AppendLine();

        report.Append("Triangles");
        for (int i = 0; i < triangles.Length - 2; i += 3)
        {
            report.Append($"|{i / 3}: {triangles[i]}, {triangles[i + 1]}, {triangles[i + 2]}");
        }

        Debug.Log(report);
    }
}
