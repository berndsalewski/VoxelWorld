using System;
using UnityEditor;
using UnityEngine;

namespace VoxelWorld
{
    [CustomEditor(typeof(Perlin3DGrapher))]
    public class Perlin3DGrapherEditor : Editor
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
