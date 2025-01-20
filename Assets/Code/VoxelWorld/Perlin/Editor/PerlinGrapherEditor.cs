
namespace VoxelWorld.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(PerlinGrapher))]
    public class PerlinGrapherEditor : Editor
    {
        private Editor _perlinSettingsEditor;

        override public void OnInspectorGUI()
        {
            PerlinGrapher perlinGrapher = (PerlinGrapher)target;

            if (_perlinSettingsEditor == null)
            {
                _perlinSettingsEditor = CreateEditor(perlinGrapher.perlinSettings);
            }

            DrawDefaultInspector();

            if(_perlinSettingsEditor.DrawDefaultInspector())
            {
                perlinGrapher.UpdateGraph();
            }
        }

        private void OnSceneGUI()
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

        private void OnDestroy()
        {
            _perlinSettingsEditor = null;
        }
    }
}