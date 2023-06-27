using UnityEditor;
using UnityEngine;

namespace VoxelWorld.Editor
{
    [CustomEditor(typeof(PerlinGrapher))]
    public class PerlinGrapherHandles : UnityEditor.Editor
    {
        void OnSceneGUI()
        {
            PerlinGrapher handle = (PerlinGrapher)target;
            if (handle == null)
            {
                return;
            }

            Handles.color = Color.white;
            Handles.Label(handle.lineRenderer.GetPosition(0) + Vector3.up * 2,
                "Layer: " +
                handle.gameObject.name);
        }
    }
}