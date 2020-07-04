using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Simulation01.Terrain;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulation01.Weather
{
    public class WeatherController : MonoBehaviour
    {
        private static readonly int s_FadeOut = Animator.StringToHash("fadeOut");
        private static readonly int s_Thunder = Animator.StringToHash("setToLightning");

        /// <summary>
        /// Prefab used to instantiate new snow block
        /// </summary>
        public GameObject snowPrefab;

        /// <summary>
        /// Prefab used to instantiate new rain block
        /// </summary>
        public GameObject rainPrefab;


        /// <summary>
        /// Prefab used to instantiate new clouds sorted by size in ascending order. 
        /// </summary>
        public GameObject[] cloudsPrefab;

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

        /// <summary>
        /// The rain intensity at which thunder begins
        /// </summary>
        [Range(0f, 1f)] public float thunderThreshold = 0.65f;

        /// <summary>
        /// The rain intensity at which clouds will be instantiated
        /// </summary>
        [Range(0f, 1f)] public float cloudyThreshold = 0.3f;

        private float m_MapSize;

        private List<WeatherCellGroup> m_Groups;

        private float m_CloudMovementAngle;

        public float cloudMoveSpeed = 0.2f;

        private class DestroyableGameObject
        {
            public readonly GameObject Parent;
            [CanBeNull] public readonly ParticleSystem ParticleSystem;
            public readonly Animator CloudAnimator;
            public bool HasThunder;

            public DestroyableGameObject(Vector3 originalPosition, [CanBeNull] GameObject particle, GameObject cloud)
            {
                Parent = new GameObject("CloudBlock");

                if (!ReferenceEquals(particle, null))
                {
                    ParticleSystem = particle.GetComponent<ParticleSystem>();
                    if (particle) particle.transform.SetParent(Parent.transform);
                }

                CloudAnimator = cloud.GetComponent<Animator>();
                cloud.transform.SetParent(Parent.transform);

                Parent.transform.position = originalPosition;
            }
        }

        private Dictionary<WeatherCell, DestroyableGameObject> m_ActiveCells;

        private void Start()
        {
            var terrainController = GameObject.Find("Terrain").GetComponent<TerrainGenerator>();
            m_MapSize = terrainController.GetMapSize() / 2f;
            m_Groups = new WeatherGroupsCreator(terrainController.NoiseGenerator, divisionsPerSide).CreateCellGroups();
            m_ActiveCells = new Dictionary<WeatherCell, DestroyableGameObject>();

            // Setup random cloud movement direction 
            m_CloudMovementAngle = Random.Range(0f, 360f);

            // Start weather spawning routine
            StartCoroutine(UpdateNextEvent());
        }


        private void Update()
        {
            // Update clouds position
            foreach (var container in m_ActiveCells.Select(container => container.Value))
            {
                container.Parent.transform.Translate(0f, 0f, cloudMoveSpeed * Time.deltaTime, Space.Self);
            }
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
                var delay = Random.Range(averageDelay * 0.3f, averageDelay * 0.7f);
                var weatherEvent = NextWeatherEvent();
                if (weatherEvent != null)
                {
                    var group = GetRandomGroup(weatherEvent.targetZone);
                    var selection = group.PickRandomCellsInRadius(weatherEvent.radius);
                    InstantiateWeatherCells(selection, weatherEvent);

                    if (weatherEvent.rainy)
                        if (!weatherEvent.snowy)
                            delay = Random.Range(weatherEvent.duration * 0.5f, weatherEvent.duration);
                        else
                            delay = weatherEvent.duration * 0.5f;
                }

                Debug.Log($"Next event in {delay} sec");

                const float maxAngleDeviation = 30f;
                // Change cloud movement angle
                m_CloudMovementAngle += Random.Range(-maxAngleDeviation, maxAngleDeviation);
                m_CloudMovementAngle = Mathf.Repeat(m_CloudMovementAngle, 360f);

                yield return new WaitForSeconds(delay);
            }
        }

        private void InstantiateWeatherCells(IEnumerable<WeightedWeatherCell> cells, WeatherEvent weatherEvent)
        {
            foreach (var wCell in cells)
            {
                var cell = wCell.Cell;

                // Check is there is an existing clouds game object at this cell
                if (m_ActiveCells.TryGetValue(cell, out var weatherGameObject)) continue;

                // Select the best match cloud. 
                var power = Mathf.CeilToInt(wCell.Scale * (cloudsPrefab.Length - 1));

                var position = new Vector3(
                    cell.wordCoord.x * (m_MapSize - 2),
                    cloudHeight,
                    cell.wordCoord.y * (m_MapSize - 2)
                );

                // Instantiate new cell
                var particle = weatherEvent.rainy
                    ? Instantiate(weatherEvent.snowy ? snowPrefab : rainPrefab, Vector3.zero,
                        Quaternion.identity)
                    : null;

                var cloud = Instantiate(cloudsPrefab[power], Vector3.zero, Quaternion.identity);

                weatherGameObject = new DestroyableGameObject(position, particle, cloud);
                weatherGameObject.Parent.transform.SetParent(transform);
                weatherGameObject.Parent.transform.rotation = Quaternion.Euler(0f, m_CloudMovementAngle, 0f);

                m_ActiveCells[cell] = weatherGameObject;
                SetupCell(weatherGameObject, wCell, weatherEvent);
                StartCoroutine(FadeOutTimer(cell, weatherGameObject, weatherEvent.duration));
            }
        }

        private IEnumerator FadeOutTimer(WeatherCell key, DestroyableGameObject go, float duration)
        {
            var thunderBeginTime = Random.Range(duration * 0.1f, duration * 0.5f);
            yield return new WaitForSeconds(thunderBeginTime);

            if (go.HasThunder)
                go.CloudAnimator.SetBool(s_Thunder, true);

            yield return new WaitForSeconds(duration - thunderBeginTime);
            go.CloudAnimator.SetTrigger(s_FadeOut);
            m_ActiveCells.Remove(key);
        }

        private readonly RangedFloat m_SnowSpeed = new RangedFloat(5f, 30f);
        private readonly RangedFloat m_RainSpeed = new RangedFloat(30f, 80f);
        private readonly RangedFloat m_SnowRate = new RangedFloat(50f, 70f);
        private readonly RangedFloat m_RainRate = new RangedFloat(50f, 100f);

        private void SetupCell(DestroyableGameObject weatherGameObject, WeightedWeatherCell cell,
            WeatherEvent weatherEvent)
        {
            var pSystem = weatherGameObject.ParticleSystem;
            if (ReferenceEquals(pSystem, null)) return;

            var main = pSystem.main;
            var emission = pSystem.emission;
            main.startSpeed = (weatherEvent.snowy ? m_SnowSpeed : m_RainSpeed).ValueAt(cell.Scale);
            emission.rateOverTime = (weatherEvent.snowy ? m_SnowRate : m_RainRate).ValueAt(cell.Scale);
            weatherGameObject.HasThunder = !weatherEvent.snowy && weatherEvent.intensity >= thunderThreshold;
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
            var prob = Random.value;
            var isCloudy = prob > cloudyThreshold;

            if (prob < config.RainProbability && !isCloudy)
                return null;

            // 3. Choose rain duration (in seconds)
            var duration = Random.Range(config.Duration.start, config.Duration.end);

            // 4. Determine rain intensity
            var intensity = isCloudy ? 0f : Random.Range(0.2f, config.MaxIntensity);

            // 5. Select spread radius
            var radius = isCloudy ? 1 : Mathf.Max(2, Mathf.CeilToInt(config.MaxRadius * intensity));

            return new WeatherEvent(config.Zone, config.Zone == ZoneType.Arctic, radius, duration, intensity);
        }

        private WeatherCellGroup GetRandomGroup(ZoneType zone)
        {
            var groups = m_Groups.FindAll(group => group.zoneType == zone);
            return groups[Random.Range(0, groups.Count)];
        }
    }
}