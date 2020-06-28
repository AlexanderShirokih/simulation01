using UnityEngine;

namespace Simulation01.Terrain
{
    /// <summary>
    /// Used to spawn water tiles
    /// </summary>
    public class WaterBlocksGenerator : MonoBehaviour
    {
        /// <summary>
        /// Prefab for instantiating
        /// </summary>
        public GameObject waterPrefab;

        /// <summary>
        /// Inner discarding radius
        /// </summary>
        public float innerRadius = 100;

        /// <summary>
        /// Outer discarding radius
        /// </summary>
        public float outerRadius = 200;

        /// <summary>
        /// Grid scale
        /// </summary>
        public float scale = 10;

        /// <summary>
        /// Number of blocks that water will be divided to. Total blocks count will sideBlocksCount * sideBlocksCount.
        /// </summary>
        public int sideBlocksCount = 5;

        public void Start()
        {
            sideBlocksCount--;
            for (var i = -sideBlocksCount / 2f; i <= sideBlocksCount / 2f; i++)
            {
                for (var j = -sideBlocksCount / 2f; j <= sideBlocksCount / 2f; j++)
                {
                    var position = new Vector3(j, 0, i) * scale;
                    var mag = position.magnitude;
                    if (mag < innerRadius || mag > outerRadius)
                        continue;
                    Instantiate(waterPrefab, position, Quaternion.identity, transform);
                }
            }
        }
    }
}