using UnityEngine;

namespace VoxelWorld
{
    [ExecuteInEditMode]
    public class PerlinGrapher : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        [Range(1, 10)]
        public float heightScale = 1;
        [Range(0.01f, 1)]
        public float scale = 1f;
        [Range(1, 10)]
        public int octaves = 1;
        [Range(-20, 20)]
        public float heightOffset = 0;
        [Range(0, 1)]
        public float probability = 1;

        // Start is called before the first frame update
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
                float y = MeshUtils.fBM(x, z, octaves, scale, heightScale, heightOffset);
                positions[x] = new Vector3(x, y, z);
            }
            lineRenderer.SetPositions(positions);
        }

        private void OnValidate()
        {
            Graph();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}