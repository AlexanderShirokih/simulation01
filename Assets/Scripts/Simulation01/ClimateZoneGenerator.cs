using System;
using UnityEngine;

namespace Simulation01
{
    /// <summary>
    /// Used to generate climate zones(biomes).
    /// Has functions to test that arbitrary point is not placed near biome border.
    /// Also can lead away point to a safety zone if it near border using dichotomy method(Works well in some cases:) ).
    /// GIF: https://giphy.com/gifs/hpLieQlidg5lBCGTwm
    /// </summary>
    public static class ClimateZoneGenerator
    {
        private static readonly float[] s_ZoneLevels =
        {
            0.18f, // Arctic
            0.35f, // Moderate
            0.60f, // Tropic
            1.00f // Desert
        };

        /// <summary>
        /// Returns climate zone value at position [x;y].
        /// </summary>
        /// <param name="x">x map coordinate in range [0.0;1.0]</param>
        /// <param name="y">y map coordinate in range [0.0;1.0]</param>
        /// <returns>zone value, where 0.18f - arctic, 0.35f - moderate, 0.6f - tropic, 1.0f - desert </returns>
        public static float GetZoneValue(float x, float y)
        {
            const float frequency = 2.5f;
            var cy = y - 0.5f;
            return (1 - 4 * cy * cy) * Mathf.PerlinNoise(x * frequency, y * frequency);
        }

        /// <summary>
        /// Returns zone type at position [x;y].
        /// </summary>
        /// <param name="x">x map coordinate in range [0.0;1.0]</param>
        /// <param name="y">y map coordinate in range [0.0;1.0]</param>
        /// <returns> ZoneType chosen by a heat level</returns>
        public static ZoneType GetZoneType(float x, float y)
        {
            var value = GetZoneValue(x, y);

            // Choose a zone type by a heat level.
            foreach (var zoneType in (ZoneType[]) Enum.GetValues(typeof(ZoneType)))
            {
                if (value < s_ZoneLevels[(int) zoneType])
                    return zoneType;
            }

            return ZoneType.Arctic;
        }


        /// <summary>
        /// Tests that place around the point doesn't have borders with other biomes.   
        /// </summary>
        /// <param name="point">point for testing</param>
        /// <param name="radius">testing radius</param>
        /// <param name="zone">zone at which point located</param>
        /// <returns>True if point in safe zone, False otherwise</returns>
        public static bool TestCirclePlacedInOneZone(Vector2 point, float radius, out ZoneType zone)
        {
            zone = GetZoneType(point.x, point.y);
            var left = GetZoneType(point.x - radius, point.y);
            var right = GetZoneType(point.x + radius, point.y);
            var top = GetZoneType(point.x, point.y - radius);
            var bottom = GetZoneType(point.x, point.y + radius);
            return zone == left && zone == right && top == zone && bottom == zone;
        }
    }
}