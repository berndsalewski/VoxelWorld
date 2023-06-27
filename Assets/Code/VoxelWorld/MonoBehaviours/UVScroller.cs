using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelWorld
{

    public class UVScroller : MonoBehaviour
    {
        Vector2 uvSpeed = new Vector2(0, 0.01f);
        Vector2 uvOffset = Vector2.zero;
        MeshRenderer meshRenderer;
        List<Material> materials;

        private void Start()
        {
            materials = new List<Material>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        private void LateUpdate()
        {
            uvOffset += uvSpeed * Time.deltaTime;
            if (uvOffset.x > 0.0625f)
            {
                uvOffset = new Vector2(0, uvOffset.y);
            }

            if (uvOffset.y > 0.0625f)
            {
                uvOffset = new Vector2(uvOffset.x, 0);
            }

            meshRenderer.GetMaterials(materials);
            materials[0].SetTextureOffset("_MainTex", uvOffset);
        }
    }
}