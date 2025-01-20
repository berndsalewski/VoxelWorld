namespace VoxelWorld
{
    using UnityEngine;
    
    [CreateAssetMenu(fileName = "BiomePerlinConfiguration", menuName = "VoxelWorld/Biome Configuration")]
    public class BiomePerlinSettings : ScriptableObject
    {
        [Range(0f, 1f)]
        public float scale;
        [Range(1, 10)]
        public int octaves;
    }
}
