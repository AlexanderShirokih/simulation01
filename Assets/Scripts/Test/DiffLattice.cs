using UnityEngine;

namespace Test
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class DiffLattice : MonoBehaviour
    {
        private Vector3[] m_Points;
        private Color[] m_Colors;
        private Mesh m_Mesh;

        public Color minColor = Color.black;
        public Color maxColor = Color.white;

        [Range(0.001f, 100f)] public float scale = 1f;

        [Range(0.1f, 10f)] public float hScale = 1f;

        public int size;

        public bool useNewArray;

        // x < 0 ? 6f : 0f
        //  e^(-(x/0.25)^2-(y/0.25)^2) 
        // private float HeightFunction(float x, float z) => Mathf.PerlinNoise(x, z);
        // private static float HeightFunction(float x, float z) => Mathf.Abs(x) < 0.1f ? 6f : 0f;
        private static float HeightFunction(float x, float z) => Mathf.Exp(-Mathf.Pow(x / 0.25f, 2) - Mathf.Pow(z / 0.25f, 2));

        private float Height(float x, float z) =>
            HeightFunction(x / size * scale, z / size * scale) * hScale;

        // Start is called before the first frame update
        private void Start()
        {
            m_Points = new Vector3[(size + 1) * (size + 1)];
            m_Colors = new Color[m_Points.Length];
            var uv = new Vector2[m_Points.Length];

            for (int i = 0, y = -size / 2; y <= size / 2; y++)
            {
                for (var x = -size / 2; x <= size / 2; x++, i++)
                {
                    m_Points[i] = new Vector3(x, Height(x, y), y);
                    uv[i] = new Vector2((float) x / size, (float) y / size);
                    m_Colors[i] = Color.white;
                }
            }

            var triangles = new int[size * size * 6];
            for (int ti = 0, vi = 0, y = 0; y < size; y++, vi++)
            {
                for (var x = 0; x < size; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                    triangles[ti + 4] = triangles[ti + 1] = vi + size + 1;
                    triangles[ti + 5] = vi + size + 2;
                }
            }

            m_Mesh = new Mesh
            {
                vertices = m_Points,
                colors = m_Colors,
                triangles = triangles,
                uv = uv
            };

            m_Mesh.RecalculateBounds();
            m_Mesh.RecalculateNormals();
            GetComponent<MeshFilter>().mesh = m_Mesh;
        }

        // Update is called once per frame
        private void FixedUpdate()
        {
            if (Time.frameCount % 10 != 0) return;

            Vector3[] newPoints = null;
            if (useNewArray) newPoints = new Vector3[m_Points.Length];

            for (int i = 0, y = 0; y <= size; y++)
            {
                for (var x = 0; x <= size; x++, i++)
                {
                    var avg =
                        (m_Points[ToIndex(x, Mathf.Max(y - 1, 0))].y +
                         m_Points[ToIndex(x, Mathf.Min(y + 1, size))].y +
                         m_Points[ToIndex(Mathf.Max(x - 1, 0), y)].y +
                         m_Points[ToIndex(Mathf.Min(x + 1, size), y)].y) / 4f;

                    if (newPoints != null)
                        newPoints[i] = new Vector3(m_Points[i].x, avg, m_Points[i].z);
                    else
                    {
                        //   float dH = avg - points[i].y;
                        m_Points[i].y = avg; // +=dH;
                    }

                    m_Colors[i] = Color.Lerp(minColor, maxColor, avg / m_Mesh.bounds.size.y);
                }
            }

            if (useNewArray)
                m_Points = newPoints;
            m_Mesh.vertices = m_Points;
            m_Mesh.colors = m_Colors;
        }

        private int ToIndex(int x, int y) => y * (size + 1) + x;
    }
}