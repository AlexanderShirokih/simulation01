using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Simulation01.Weather
{
    /// <summary>
    /// Represents group of WeatherCell with common zone type.
    /// </summary>
    public class WeatherCellGroup
    {
        /// <summary>
        /// Zone type of this point
        /// </summary>
        public ZoneType zoneType { get; }

        /// <summary>
        /// List the group points
        /// </summary>
        public IList<WeatherCell> cells { get; }

        public WeatherCellGroup(ZoneType zoneType, IList<WeatherCell> cells)
        {
            this.zoneType = zoneType;
            this.cells = cells;
        }

        /// <summary>
        /// Picks random cell and all cells around it in a given radius.
        /// </summary>
        /// <param name="radius">radius in cells</param>
        /// <returns>
        /// If radius == 0, list containing only single cell.
        /// If radius == 1, all neighbours around picked cell, including it.
        /// If radius == 2, then all neighbours of neighbours will be included.
        /// And so on.
        /// </returns>
        public IEnumerable<WeightedWeatherCell> PickRandomCellsInRadius(int radius)
        {
            var central = cells[Random.Range(0, cells.Count)];
            var list = new HashSet<WeightedWeatherCell>(new WeightedWeatherCellComparer())
                {new WeightedWeatherCell(central, 1.0f)};
            SelectInRadius(central, radius, radius, list);
            return list;
        }

        private static void SelectInRadius(WeatherCell cell, int radius, int maxRadius,
            HashSet<WeightedWeatherCell> resultList)
        {
            const float minScale = 0.3f;
            const float scaleRange = 1f - minScale;

            if (radius-- == 0) return;

            foreach (var neighbour in
                cell.neighbours
                    .Where(weatherCell => weatherCell != null)
                    .Select(neighbour =>
                        new WeightedWeatherCell(neighbour, (float) radius / maxRadius * scaleRange + minScale)))
                resultList.Add(neighbour);

            foreach (var neighbour in cell.neighbours.Where(weatherCell => weatherCell != null))
                SelectInRadius(neighbour, radius, maxRadius, resultList);
        }

        private class WeightedWeatherCellComparer : IEqualityComparer<WeightedWeatherCell>
        {
            public bool Equals(WeightedWeatherCell a, WeightedWeatherCell b)
                => a == null ? b == null : b != null && a.Cell.Equals(b.Cell);

            public int GetHashCode(WeightedWeatherCell obj) => obj.Cell.GetHashCode();
        }
    }

    /// <summary>
    /// A class used to describe a cell in weather grid which located in safe radius
    /// (don't located around biome borders). 
    /// </summary>
    public class WeatherCell
    {
        /// <summary>
        /// Map-spaced coordinate (-1.0..1.0) of cell.
        /// </summary>
        public Vector2 wordCoord { get; }

        /// <summary>
        /// Neighbours around this cell 
        /// </summary>
        public WeatherCell[] neighbours { get; } = new WeatherCell[8];

        public WeatherCell(Vector2 wordCoord)
        {
            this.wordCoord = wordCoord;
        }

        public override string ToString()
        {
            return $"{nameof(wordCoord)}: {wordCoord}, {nameof(neighbours)}: {neighbours}";
        }
    }

    /// <summary>
    /// Represents WeatherCell with scale factor
    /// </summary>
    public class WeightedWeatherCell
    {
        /// <summary>
        /// Cell instance
        /// </summary>
        public readonly WeatherCell Cell;

        /// <summary>
        /// Cells power. Decreases by moving away from center cell
        /// </summary>
        public readonly float Scale;

        public WeightedWeatherCell(WeatherCell cell, float scale)
        {
            Cell = cell;
            Scale = scale;
        }
    }
}