using UnityEngine;

namespace VoxelWorld
{
    [ExecuteInEditMode]
    public class PerlinGrapher : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        public PerlinSettings perlinSettings;

        void Start()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 100;
            Graph();
        }

        void Graph()
        {
            int z = 0;
            Vector3[] positions = new Vector3[lineRenderer.positionCount];
            for (int x = 0; x < lineRenderer.positionCount; x++)
            {
                float y = MeshUtils.fBM(x, z, perlinSettings.octaves, perlinSettings.scale, perlinSettings.heightScale, perlinSettings.heightOffset);
                positions[x] = new Vector3(x, y, z);
            }
            lineRenderer.SetPositions(positions);
        }

        private void OnValidate()
        {
            Graph();
        }
    }
}