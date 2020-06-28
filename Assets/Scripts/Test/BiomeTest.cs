using System;
using Simulation01;
using UnityEngine;

namespace Test
{
    [RequireComponent(typeof(MeshRenderer))]
    public class BiomeTest : MonoBehaviour
    {
        public int resolution = 256;

        private Texture2D m_Texture;

        private void Start()
        {
            m_Texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", m_Texture);
            BuildTexture();
        }


        private void BuildTexture()
        {
            var pixels = m_Texture.GetPixels();

            for (int i = 0, y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++, i++)
                {
                    var u = (float) x / resolution;
                    var v = (float) y / resolution;
                    pixels[i] = GetColorAt(u, v);
                }
            }

            m_Texture.SetPixels(pixels);
            m_Texture.Apply();
        }

        private Color GetColorAt(float x, float y)
        {
            var zoneType = ClimateZoneGenerator.GetZoneType(x, y);

            switch (zoneType)
            {
                case ZoneType.Arctic:
                    return Color.white;
                case ZoneType.Desert:
                    return Color.yellow;
                case ZoneType.Moderate:
                    return new Color(0.7f, 1f, 0.39f);
                case ZoneType.Tropic:
                    return Color.green;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Pick()
        {
        }
    }
}