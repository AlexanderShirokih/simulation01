using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Simulation01.Weather
{
    /// <summary>
    /// Used to create a group of points above the biomes.
    /// </summary>
    public class WeatherGroupsCreator
    {
        private class WeatherCellInternal
        {
            public readonly int X;
            public readonly int Y;
            public readonly Vector2 WordCoord;
            public readonly ZoneType ZoneType;
            public readonly WeatherCellInternal[] Neighbours;
            public int GroupId;

            public WeatherCellInternal(int x, int y, Vector2 wordCoord, ZoneType zoneType)
            {
                X = x;
                Y = y;
                WordCoord = wordCoord;
                ZoneType = zoneType;
                Neighbours = new WeatherCellInternal[8];
                GroupId = 0;
            }
        }

        private int m_CurrentZoneId;
        private readonly int m_DivisionsPerSide;
        private readonly float m_BiomePointRadius;
        private readonly NoiseGenerator m_NoiseGenerator;

        private readonly Dictionary<int, WeatherCellInternal> m_BiomePoints =
            new Dictionary<int, WeatherCellInternal>();

        /// <summary>
        /// Creates new instance by specifying required params. 
        /// </summary>
        /// <param name="noiseGenerator">NoiseGenerator instance</param>
        /// <param name="divisionsPerSide">number of blocks on which grid will be divided</param>
        /// <param name="biomePointRadius">test radius around each grid point</param>
        public WeatherGroupsCreator(NoiseGenerator noiseGenerator, int divisionsPerSide, float biomePointRadius = 0.06f)
        {
            m_NoiseGenerator = noiseGenerator;
            m_DivisionsPerSide = divisionsPerSide;
            m_BiomePointRadius = biomePointRadius;
        }

        public List<WeatherCellGroup> CreateCellGroups()
        {
            BuildPointsGrid();
            ConnectNeighbours();
            SplitToGroups();
            var groups = ConvertToWeatherPointGroups();

            m_BiomePoints.Clear();
            return groups;
        }

        private void BuildPointsGrid()
        {
            for (int i = 0, k = 0; i <= m_DivisionsPerSide; i++)
            {
                for (int j = 0; j <= m_DivisionsPerSide; j++, k++)
                {
                    var xx = 1f - (float) j / m_DivisionsPerSide;
                    var yy = 1f - (float) i / m_DivisionsPerSide;

                    // Test that point in safe zone
                    if (m_NoiseGenerator.GetHeightLevelAt(xx, yy).Tile.Surface != SurfaceType.Sea &&
                        ClimateZoneGenerator.TestCirclePlacedInOneZone(new Vector2(xx, yy), m_BiomePointRadius,
                            out var zoneType))
                    {
                        // Collect them to dictionary
                        m_BiomePoints[k] =
                            new WeatherCellInternal(j, i, new Vector2((xx - 0.5f) * 2f, (yy - 0.5f) * 2f),
                                zoneType);
                    }
                }
            }
        }

        private void ConnectNeighbours()
        {
            var numPoints = m_DivisionsPerSide + 1;

            foreach (var pair in m_BiomePoints)
            {
                var current = pair.Value;
                var key = pair.Key;

                // Fill neighbours
                if (current.X != 0) // Left 
                    current.Neighbours[0] = GetOrNull(m_BiomePoints, key - 1);

                if (current.X != m_DivisionsPerSide) // Right
                    current.Neighbours[1] = GetOrNull(m_BiomePoints, key + 1);

                if (current.Y != 0) // Bottom
                {
                    current.Neighbours[2] = GetOrNull(m_BiomePoints, key - numPoints);
                    if (current.X != 0) // Bottom-Left
                        current.Neighbours[4] = GetOrNull(m_BiomePoints, key - m_DivisionsPerSide - 2);
                    if (current.X != m_DivisionsPerSide) // Bottom-Right
                        current.Neighbours[5] = GetOrNull(m_BiomePoints, key - numPoints + 1);
                }

                if (current.Y != m_DivisionsPerSide) // Top
                {
                    current.Neighbours[3] = GetOrNull(m_BiomePoints, key + numPoints);
                    if (current.X != 0) // Top-Left
                        current.Neighbours[6] = GetOrNull(m_BiomePoints, key + m_DivisionsPerSide);
                    if (current.X != m_DivisionsPerSide) // Top-Right
                        current.Neighbours[7] = GetOrNull(m_BiomePoints, key + numPoints + 1);
                }
            }
        }

        private void SplitToGroups()
        {
            m_CurrentZoneId = 1;

            // Create edges connecting points by neighbours
            foreach (var point in m_BiomePoints.Select(pair => pair.Value))
            {
                if (MarkRecursively(point))
                    m_CurrentZoneId++;
            }
        }

        private List<WeatherCellGroup> ConvertToWeatherPointGroups()
        {
            var pointsLookup =
                m_BiomePoints
                    .Select(pair => pair.Value)
                    .ToDictionary(point => point, point => new WeatherCell(point.WordCoord));

            // Split by groups and convert to simpler structure
            return m_BiomePoints
                .GroupBy(pair => pair.Value.GroupId)
                .Select(pairs =>
                    new WeatherCellGroup(
                        pairs.First(sel => true).Value.ZoneType,
                        pairs.Select(pair =>
                        {
                            var oldPoint = pair.Value;
                            var newPoint = pointsLookup[oldPoint];
                            for (var i = 0; i < oldPoint.Neighbours.Length; i++)
                                newPoint.neighbours[i] = oldPoint.Neighbours[i] == null
                                    ? null
                                    : pointsLookup[oldPoint.Neighbours[i]];
                            return newPoint;
                        }).ToList()
                    )
                ).ToList();
        }

        private bool MarkRecursively(WeatherCellInternal cell)
        {
            if (cell.GroupId != 0) return false;
            cell.GroupId = m_CurrentZoneId;

            foreach (var neighbour in cell.Neighbours)
                if (neighbour != null)
                    MarkRecursively(neighbour);
            return true;
        }

        private static WeatherCellInternal GetOrNull(IDictionary<int, WeatherCellInternal> dictionary, int key)
        {
            dictionary.TryGetValue(key, out var point);
            return point;
        }
    }
}