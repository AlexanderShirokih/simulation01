using UnityEngine;

namespace Simulation01
{
    /// <summary>
    /// Represents functions for noise generation.
    /// </summary>
    public class NoiseGenerator
    {
        private readonly float m_BaseFrequency;

        public NoiseGenerator(float baseFrequency)
        {
            this.m_BaseFrequency = baseFrequency;
        }

        /// <summary>
        /// Returns noise value for [x,y] coordinate.
        /// </summary>
        /// <param name="x">grid X coordinate normalized in 0.0..1.0</param>
        /// <param name="y">grid Y coordinate normalized in 0.0..1.0</param>
        /// <returns>Value in range 0.0..1.0.  (Return value might be slightly beyond 1.0.)</returns>
        public float GetNoiseValue(float x, float y)
        {
            var doubleFreq = m_BaseFrequency * 2f;
            const float scale = 1.5f;

            // Base octave
            var value =
                Mathf.PerlinNoise(m_BaseFrequency * x, m_BaseFrequency * y) +
                Mathf.PerlinNoise(x * doubleFreq, y * doubleFreq) / 2f;

            // Scale value
            value /= scale;

            // Distance from origin point
            float cx = x - 0.5f, cy = y - 0.5f;
            var d = Mathf.Sqrt(cx * cx + cy * cy) * 2f;

            // b = 1-d^5 - distance function
            var b = 1 - d * d * d * d * d;

            var elevation = b - value;
            return elevation < 0f ? 0f : elevation;
        }

        /// <summary>
        /// A structure describing divisions of height levels to SurfaceType
        /// </summary>
        public readonly struct HeightTile
        {
            public readonly float MaxValue;
            public readonly float ScaleFactor;
            public readonly SurfaceType Surface;

            public HeightTile(float maxValue, float scaleFactor, SurfaceType surfaceType)
            {
                MaxValue = maxValue;
                ScaleFactor = scaleFactor;
                Surface = surfaceType;
            }
        }

        public struct HeightLevel
        {
            public HeightTile Tile;
            public readonly float HeightValue;

            public HeightLevel(HeightTile tile, float heightValue)
            {
                Tile = tile;
                HeightValue = heightValue;
            }
        }

        private static readonly HeightTile[] s_HeightLevels =
        {
            new HeightTile(0.12f, 0.38f, SurfaceType.Sea), // Water level
            new HeightTile(0.25f, 0.4f, SurfaceType.Beach), // Beach level
            new HeightTile(0.7f, 0.45f, SurfaceType.Ground), // Ground level 
            new HeightTile(0.72f, 0.48f, SurfaceType.MountainHills), // Pre-mountain-0 level 
            new HeightTile(0.75f, 0.6f, SurfaceType.MountainHills), // Pre-mountain-1 level 
            new HeightTile(0.8f, 0.7f, SurfaceType.MountainHills), // Pre-mountain-2 level 
            new HeightTile(0.85f, 0.73f, SurfaceType.MountainHills), // Pre-mountain-3 level 
            new HeightTile(1.0f, 0.76f, SurfaceType.Mountain) // Mountain level
        };

        /// <summary>
        /// Returns SurfaceType at given point
        /// </summary>
        /// <param name="x">x map coord. In range [0;1]</param>
        /// <param name="z">z map coord. In range [0;1]</param>
        /// <returns>SurfaceType at point [x;z]</returns>
        public HeightLevel GetHeightLevelAt(float x, float z)
        {
            var value = GetNoiseValue(x, z);

            foreach (var level in s_HeightLevels)
            {
                if (!(value < level.MaxValue)) continue;
                return new HeightLevel(level, value);
            }

            return new HeightLevel(s_HeightLevels[s_HeightLevels.Length - 1], value);
        }
    }
}