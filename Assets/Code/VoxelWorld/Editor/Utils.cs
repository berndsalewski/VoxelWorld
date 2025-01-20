namespace Voxelworld
{
    using UnityEngine;
    using UnityEditor;
    public static class Utils
    {
        public static T GetOrCreateScriptableObject<T>(string path) where T : ScriptableObject
        {
            if(!AssetDatabase.AssetPathExists(path))
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
            }

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }    
}