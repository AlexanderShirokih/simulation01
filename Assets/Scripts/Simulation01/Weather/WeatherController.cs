using System.Collections;
using System.Collections.Generic;
using Simulation01.Terrain;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulation01.Weather
{
    public class WeatherController : MonoBehaviour
    {
        /// <summary>
        /// Prefab used to instantiate new snow block
        /// </summary>
        public GameObject snowPrefab;

        /// <summary>
        /// Prefab used to instantiate new rain block
        /// </summary>
        public GameObject rainPrefab;

        /// <summary>
        /// Height level at which clouds will hover
        /// </summary>
        public float cloudHeight = 50f;

        /// <summary>
        /// Average delay before next event if it's not defined.
        /// </summary>
        public float averageDelay = 25f;

        /// <summary>
        /// Cells number per map side
        /// </summary>
        public int divisionsPerSide = 16;

        private float m_MapSize;

        private List<WeatherCellGroup> m_Groups;

        private class DestroyableGameObject
        {
            public int Version;
            public readonly GameObject Target;

            public DestroyableGameObject(int version, GameObject target)
            {
                Version = version;
                Target = target;
            }
        }

        private Dictionary<WeatherCell, DestroyableGameObject> activeCells;

        private void Start()
        {
            var terrainController = GameObject.Find("Terrain").GetComponent<TerrainGenerator>();
            m_MapSize = terrainController.GetMapSize() / 2f;
            m_Groups = new WeatherGroupsCreator(terrainController.NoiseGenerator, divisionsPerSide).CreateCellGroups();
            activeCells = new Dictionary<WeatherCell, DestroyableGameObject>();

            // Start weather spawning routine
            StartCoroutine(UpdateNextEvent());
        }

        private bool m_Alive = true;

        private void OnDestroy()
        {
            m_Alive = false;
        }

        private IEnumerator UpdateNextEvent()
        {
            while (m_Alive)
            {
                float delay;
                var weatherEvent = NextWeatherEvent();
                if (weatherEvent != null)
                {
                    var group = GetRandomGroup(weatherEvent.targetZone);
                    var selection = group.PickRandomCellsInRadius(weatherEvent.radius);
                    InstantiateWeatherCells(selection, weatherEvent);

                    if (!weatherEvent.snowy)
                        delay = Random.Range(weatherEvent.duration * 0.5f, weatherEvent.duration);
                    else
                        delay = weatherEvent.duration * 0.5f;
                }
                else
                    delay = Random.Range(averageDelay * 0.3f, averageDelay * 0.7f);

                Debug.Log($"Next event in {delay} sec");
                yield return new WaitForSeconds(delay);
            }
        }

        private void InstantiateWeatherCells(IEnumerable<WeightedWeatherCell> cells, WeatherEvent weatherEvent)
        {
            foreach (var wCell in cells)
            {
                var cell = wCell.Cell;
                if (!activeCells.TryGetValue(cell, out var weatherGameObject))
                {
                    // Instantiate new cell
                    weatherGameObject = new DestroyableGameObject(1, Instantiate(
                        weatherEvent.snowy ? snowPrefab : rainPrefab,
                        new Vector3(
                            cell.wordCoord.x * (m_MapSize - 2),
                            cloudHeight,
                            cell.wordCoord.y * (m_MapSize - 2)
                        ),
                        Quaternion.identity,
                        transform
                    ));
                }
                else
                    weatherGameObject.Version++;

                activeCells[cell] = weatherGameObject;
                SetupCell(weatherGameObject.Target, wCell, weatherEvent);
                StartCoroutine(DestroyTimer(cell, weatherGameObject, weatherEvent.duration));
            }
        }

        private IEnumerator DestroyTimer(WeatherCell key, DestroyableGameObject go, float duration)
        {
            var originalVersion = go.Version;
            yield return new WaitForSeconds(duration);
            var currentVersion = go.Version;

            if (originalVersion != currentVersion) yield break;

            activeCells.Remove(key);
            Destroy(go.Target);
        }

        private readonly RangedFloat m_SnowSpeed = new RangedFloat(5f, 30f);
        private readonly RangedFloat m_RainSpeed = new RangedFloat(30f, 80f);
        private readonly RangedFloat m_SnowRate = new RangedFloat(50f, 70f);
        private readonly RangedFloat m_RainRate = new RangedFloat(50f, 100f);

        private void SetupCell(GameObject weatherGameObject, WeightedWeatherCell cell, WeatherEvent weatherEvent)
        {
            var pSystem = weatherGameObject.GetComponent<ParticleSystem>();
            var main = pSystem.main;
            var emission = pSystem.emission;
            main.startSpeed = (weatherEvent.snowy ? m_SnowSpeed : m_RainSpeed).ValueAt(cell.Scale);
            emission.rateOverTime = (weatherEvent.snowy ? m_SnowRate : m_RainRate).ValueAt(cell.Scale);
        }

        readonly struct WeatherBiomeConfig
        {
            public readonly ZoneType Zone;
            public readonly float RainProbability;
            public readonly RangeInt Duration;
            public readonly int MaxRadius;
            public readonly float MaxIntensity;

            public WeatherBiomeConfig(ZoneType zone, float rainProbability, RangeInt duration,
                int maxRadius, float maxIntensity)
            {
                Zone = zone;
                RainProbability = rainProbability;
                Duration = duration;
                MaxRadius = maxRadius;
                MaxIntensity = maxIntensity;
            }
        }

        private static readonly WeatherBiomeConfig[] s_BiomeConfigs =
        {
            new WeatherBiomeConfig(ZoneType.Arctic, 0.5f, new RangeInt(30, 120), 4, 0.7f),
            new WeatherBiomeConfig(ZoneType.Desert, 0.1f, new RangeInt(5, 60), 2, 0.5f),
            new WeatherBiomeConfig(ZoneType.Moderate, 0.25f, new RangeInt(15, 60), 3, 0.7f),
            new WeatherBiomeConfig(ZoneType.Tropic, 0.7f, new RangeInt(30, 120), 4, 1f)
        };

        private WeatherEvent NextWeatherEvent()
        {
            // 1. Choose a random biome
            var config = s_BiomeConfigs[Random.Range(0, s_BiomeConfigs.Length)];

            // 2. Determine the probability of rain event
            if (Random.value > config.RainProbability) return null;

            // 3. Choose rain duration (in seconds)
            var duration = Random.Range(config.Duration.start, config.Duration.end);

            // 4. Determine rain intensity
            var intensity = Random.Range(0.2f, config.MaxIntensity);

            // 5. Select spread radius
            var radius = Mathf.Max(2, Mathf.CeilToInt(config.MaxRadius * intensity));

            return new WeatherEvent(config.Zone, config.Zone == ZoneType.Arctic, radius, duration, intensity);
        }

        private WeatherCellGroup GetRandomGroup(ZoneType zone)
        {
            var groups = m_Groups.FindAll(group => group.zoneType == zone);
            return groups[Random.Range(0, groups.Count)];
        }
    }
}