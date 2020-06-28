using UnityEngine;
using UnityEngine.Rendering;

namespace Simulation01.Terrain
{
    /// <summary>
    /// A class that responsible for generation terrain blocks.
    /// </summary>
    public class TerrainGenerator : MonoBehaviour
    {
        /// <summary>
        /// Block size in world units.
        /// </summary>
        public float blockSize = 60f;

        /// <summary>
        /// Number of blocks that terrain will be divided to. Total blocks count will sideBlocksCount * sideBlocksCount.
        /// </summary>
        public int sideBlocksCount = 1;

        /// <summary>
        /// Vertex resolution per single block side
        /// </summary>
        public int vertexPerBlockSide = 80;

        /// <summary>
        /// Terrain height scale
        /// </summary>
        public float heightScale = 5f;

        /// <summary>
        /// Material used to draw terrain blocks.
        /// </summary>
        public Material terrainMaterial;

        /// Texture resolution per single block
        public int textureResolution = 128;

        public readonly NoiseGenerator NoiseGenerator = new NoiseGenerator(5f);
        private static readonly int s_Map = Shader.PropertyToID("Map");

        private void Start()
        {
            // Create game objects for all blocks
            var halfBlocksCount = sideBlocksCount / 2;
            for (var z = 0; z < sideBlocksCount; z++)
            {
                for (var x = 0; x < sideBlocksCount; x++)
                {
                    var go = new GameObject($"tile_{x}_{z}");
                    go.transform.SetParent(transform);
                    go.transform.localPosition = new Vector3(
                        (x - halfBlocksCount) * blockSize,
                        0f,
                        (z - halfBlocksCount) * blockSize);

                    float baseX = (float) x / sideBlocksCount, baseZ = (float) z / sideBlocksCount;
                    go.AddComponent<MeshFilter>().mesh = GenerateTileData(baseX, baseZ);
                    var meshRenderer = go.AddComponent<MeshRenderer>();
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    meshRenderer.material = terrainMaterial;
                    meshRenderer.material.SetTexture(s_Map, GenerateTexture(baseX, baseZ));
                }
            }

            FixNeighbourNormals();
        }

        private Mesh GenerateTileData(float baseX, float baseZ)
        {
            var step = blockSize / vertexPerBlockSide;
            var globalStep = (float) vertexPerBlockSide * sideBlocksCount;
            var vertices = new Vector3[(vertexPerBlockSide + 1) * (vertexPerBlockSide + 1)];
            var uv = new Vector2[vertices.Length];

            // Generate vertices and texture coords
            for (int i = 0, z = 0; z <= vertexPerBlockSide; z++)
            {
                for (var x = 0; x <= vertexPerBlockSide; x++, i++)
                {
                    var lX = baseX + x / globalStep;
                    var lZ = baseZ + z / globalStep;
                    vertices[i] = new Vector3(
                        (x - 0.5f * blockSize) * step,
                        GetHeightAt(lX, lZ),
                        (z - 0.5f * blockSize) * step
                    );
                    uv[i] = new Vector2((float) x / vertexPerBlockSide, (float) z / vertexPerBlockSide);
                }
            }

            // Generate triangle indices
            var triangles = new int[vertexPerBlockSide * vertexPerBlockSide * 6];
            for (int ti = 0, vi = 0, y = 0; y < vertexPerBlockSide; y++, vi++)
            {
                for (var x = 0; x < vertexPerBlockSide; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                    triangles[ti + 4] = triangles[ti + 1] = vi + vertexPerBlockSide + 1;
                    triangles[ti + 5] = vi + vertexPerBlockSide + 2;
                }
            }

            var mesh = new Mesh
            {
                name = "Tile Mesh",
                vertices = vertices,
                uv = uv,
                triangles = triangles,
            };

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private Texture2D GenerateTexture(float baseX, float baseZ)
        {
            var texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGB24, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = texture.GetPixels32();

            var fTexRes = (float) textureResolution;
            var delta = 1f / (fTexRes * sideBlocksCount);

            for (var z = 0; z < textureResolution; z++)
            for (var x = 0; x < textureResolution; x++)
            {
                var nX = baseX + x * delta;
                var nZ = baseZ + z * delta;
                var noiseValue = NoiseGenerator.GetNoiseValue(nX, nZ);
                var zoneValue = ClimateZoneGenerator.GetZoneValue(nX, nZ);
                pixels[x + z * textureResolution] =
                    new Color32((byte) (255 * noiseValue), (byte) (255 * zoneValue), 0, 0);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return texture;
        }

        private float GetHeightAt(float x, float z)
        {
            var level = NoiseGenerator.GetHeightLevelAt(x, z);
            var height = level.HeightValue;
            
            // Dug the ocean
            if (height < 0.01f)
                height = -0.2f;

            var scaleFactor = blockSize * heightScale * level.Tile.ScaleFactor;

            return height * scaleFactor;
        }

        private void FixNeighbourNormals()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var current = transform.GetChild(i);
                var x = i % sideBlocksCount;
                var z = i / sideBlocksCount;
                var size = vertexPerBlockSide + 1;

                var currMesh = current.GetComponent<MeshFilter>().mesh;
                var currMeshNormals = i != 0 ? currMesh.normals : new Vector3[0];

                // Test for X stitch
                if (x != 0)
                {
                    var lNormals = transform.GetChild(i - 1).GetComponent<MeshFilter>().mesh.normals;

                    for (var j = 0; j < size; j++)
                    {
                        var rowOffset = j * size;
                        currMeshNormals[rowOffset] = lNormals[vertexPerBlockSide + rowOffset];
                    }
                }

                // Test for Z stitch
                if (z != 0)
                {
                    var tNormals = transform.GetChild(i - sideBlocksCount).GetComponent<MeshFilter>().mesh.normals;
                    var offset = vertexPerBlockSide * size;

                    for (var j = 0; j < size; j++)
                    {
                        currMeshNormals[j] = tNormals[j + offset];
                    }
                }

                // If normals was changed, update it
                if (currMeshNormals.Length != 0)
                    currMesh.normals = currMeshNormals;
            }
        }

        public void Rebuild()
        {
            for (int i = 0, z = 0; z < sideBlocksCount; z++)
            {
                for (var x = 0; x < sideBlocksCount; x++, i++)
                {
                    var child = transform.GetChild(i);
                    float baseX = (float) x / sideBlocksCount, baseZ = (float) z / sideBlocksCount;
                    child.GetComponent<MeshFilter>().mesh = GenerateTileData(baseX, baseZ);
                    child.GetComponent<MeshRenderer>().material.mainTexture = GenerateTexture(baseX, baseZ);
                }
            }

            FixNeighbourNormals();
        }

        /// <summary>
        /// Get the map size in world unit
        /// </summary>
        /// <returns>map size</returns>
        public float GetMapSize()
        {
            return blockSize * sideBlocksCount;
        }
    }
}