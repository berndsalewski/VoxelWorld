using UnityEditor;
using UnityEngine;

namespace VoxelWorld.Editor
{
    [CustomEditor(typeof(PerlinGrapher))]
    public class PerlinGrapherEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor configurationEditor;

        private void OnEnable()
        {
            configurationEditor = null;
        }

        override public void OnInspectorGUI()
        {
            PerlinGrapher perlinGrapher = (PerlinGrapher)target;

            if (configurationEditor == null)
            {
                configurationEditor = CreateEditor(perlinGrapher.perlinSettings);
            }

            DrawDefaultInspector();

            configurationEditor.DrawDefaultInspector();
        }

        void OnSceneGUI()
        {
            PerlinGrapher perlinGrapher = (PerlinGrapher)target;
            if (perlinGrapher == null)
            {
                return;
            }

            Handles.color = Color.white;
            Handles.Label(perlinGrapher.lineRenderer.GetPosition(0) + Vector3.up * 2,
                "Layer: " +
                perlinGrapher.gameObject.name);
        }
    }
}