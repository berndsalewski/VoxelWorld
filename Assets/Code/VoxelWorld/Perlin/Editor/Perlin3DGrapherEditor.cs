using UnityEditor;
using UnityEngine;

namespace VoxelWorld.Editor
{
    [CustomEditor(typeof(Perlin3DGrapher))]
    public class Perlin3DGrapherEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            Perlin3DGrapher grapher = target as Perlin3DGrapher;
            DrawDefaultInspector();

            if (GUILayout.Button("Draw"))
            {
                grapher.Graph();
            }
        }
    }
}
