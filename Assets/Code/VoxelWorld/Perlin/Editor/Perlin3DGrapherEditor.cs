namespace VoxelWorld.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(Perlin3DGrapher))]
    public class Perlin3DGrapherEditor : Editor
    {
        Editor configurationEditor;

        public void OnEnable()
        {
            configurationEditor = null;
        }

        public override void OnInspectorGUI()
        {
            Perlin3DGrapher grapher = target as Perlin3DGrapher;
            DrawDefaultInspector();

            if (configurationEditor == null)
            {
                configurationEditor = CreateEditor(grapher.perlin3DConfig);
            }

            configurationEditor.DrawDefaultInspector();

            if (GUILayout.Button("Draw"))
            {
                grapher.Graph();
            }
        }
    }
}
