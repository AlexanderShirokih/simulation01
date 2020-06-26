using UnityEngine;

namespace Test
{
    public class GraphBuilder : MonoBehaviour
    {
        private struct ColoredPoint
        {
            public float Val;
            public Color Color;
        }

        public int pointsCnt = 20;

        public float scale = 1f;

        public Material pointMaterial;

        public float pointsScale = 0.1f;

        private GameObject[] m_Points;

        private static float GetNoiseValue(float x, float y)
        {
            const float frequency = 2.5f;
            var cy = y - 0.5f;
            return (1 - 4 * cy * cy) * Mathf.PerlinNoise(x * frequency, y * frequency);
        }


        [Range(0f, 1f)] public float t = 0.75f;

        // Start is called before the first frame update
        void Start()
        {
            BuildGraph2D();
        }

        private void BuildGraph2D()
        {
            if (m_Points != null && m_Points.Length != pointsCnt)
            {
                foreach (var v in m_Points)
                    Destroy(v);
                m_Points = null;
            }

            if (m_Points == null)
            {
                m_Points = new GameObject[pointsCnt];
                for (int i = 0; i < pointsCnt; i++)
                {
                    m_Points[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    m_Points[i].GetComponent<MeshRenderer>().sharedMaterial = pointMaterial;
                    m_Points[i].transform.localScale *= pointsScale;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.frameCount % 30 == 0)
            {
                for (int i = 0; i < pointsCnt; i++)
                {
                    ColoredPoint Func(float x0, float y0)
                    {
                        var value = GetNoiseValue(x0, y0);

                        Color color;
                        if (value < 0.18f)
                            color = Color.white;
                        else if (value < 0.35f)
                            color = Color.cyan;
                        else if (value < 0.6f)
                            color = Color.green;
                        else
                            color = Color.yellow;

                        var dd = 0.008f;
                        if (Mathf.Abs(y0 - t) < dd) color = Color.red;

                        return new ColoredPoint {Val = value, Color = color};
                    }

                    var x = (float) i / pointsCnt;
                    var pointVal = Func(0.5f, x);
                    m_Points[i].GetComponent<Renderer>().material.color = pointVal.Color;
                    m_Points[i].transform.position = new Vector3((x - 0.5f) * scale, pointVal.Val * scale * 0.5f);
                }
            }
        }
    }
}