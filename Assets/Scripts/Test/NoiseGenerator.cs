using System;
using UnityEngine;

namespace Test
{
    [RequireComponent(typeof(MeshRenderer))]
    public class NoiseGenerator : MonoBehaviour
    {
        private delegate Color ColorFunction(float value);

        private Texture2D m_Texture2D;

        public Color minColor = new Color(82f / 255f, 96f / 255f, 82f / 255f);
        public Color maxColor = Color.white;

        public Color water = Color.blue;
        public Color beach = Color.yellow;
        public Color grass = Color.green;
        public Color mountains = Color.gray;

        public int size = 256;

        [Range(1f, 20f)] public float freq = 1f;

        public ColoringType coloringType;
    
        private readonly Simulation01.NoiseGenerator m_NoiseGenerator = new Simulation01.NoiseGenerator(5);
    
        public enum ColoringType
        {
            TwoColorGradient,
            HeightMapGradient
        }

        // Start is called before the first frame update
        private void Start()
        {
            m_Texture2D = new Texture2D(size, size, TextureFormat.RGB24, false);
            GetComponent<MeshRenderer>().material.mainTexture = m_Texture2D;
        }

        // Update is called once per frame
        private void Update()
        {
            ColorFunction colorFunction;

            switch (coloringType)
            {
                case ColoringType.TwoColorGradient:
                    colorFunction = TwoColorInterpolation;
                    break;
                case ColoringType.HeightMapGradient:
                    colorFunction = HeightMapGradient;
                    break;
                default: throw new Exception();
            }

            for (var x = 0; x < size; x++)
            {
                for (var y = 0; y < size; y++)
                {
                    var nx = (float) x / size;
                    var ny = (float) y / size;

                    var value = m_NoiseGenerator.GetNoiseValue(nx, ny);
                    m_Texture2D.SetPixel(x, y, colorFunction(value));
                }
            }

            m_Texture2D.Apply();
        }


        private Color TwoColorInterpolation(float value)
        {
            return Color.Lerp(minColor, maxColor, value);
        }

        private Color HeightMapGradient(float value)
        {
            if (value < 0.2f)
                return water;
            if (value < 0.25f)
                return beach;
            if (value < 0.7f)
                return grass;
            if (value < 0.9f)
                return mountains;

            return Color.white * value;
        }
    }
}