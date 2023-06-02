namespace VoxelWorld
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class WaterManager : MonoBehaviour
    {
        public GameObject player;

        // Update is called once per frame
        void Update()
        {
            this.gameObject.transform.position = new Vector3(player.transform.position.x, 0, player.transform.position.z);
        }
    }
}