using System;
using UnityEditor;

namespace VoxelWorld.Editor
{
    [CustomEditor(typeof(WorldBuilder))]
    public class WorldBuilderInspector : UnityEditor.Editor
    {
        private UnityEditor.Editor editorInstance;

        private void OnEnable()
        {
            editorInstance = null;
        }

        public override void OnInspectorGUI()
        {
            WorldBuilder builder = (WorldBuilder)target;

            if (editorInstance == null)
            {
                editorInstance = CreateEditor(builder.worldConfiguration);
            }

            base.OnInspectorGUI();

            //EditorGUILayout.Separator();
            //EditorGUILayout.LabelField("Configurations");

            //editorInstance.DrawDefaultInspector();
        }
    }
}
