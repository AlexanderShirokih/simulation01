using System;
using UnityEngine;

namespace Simulation01
{
    /// <summary>
    /// Manages global game time
    /// </summary>
    public class GameTime : MonoBehaviour
    {
        private const float HoursInDay = 24f;
        private const float MinutesInDay = 60f;
        public const float RealSecondsInDay = 30f;

        private float startupTime;

        private void Awake()
        {
            // By default, set to evening
            startupTime = RealSecondsInDay * 0.25f;
            LoadTime();
        }

        /// <summary>
        /// Stores time to settings file
        /// </summary>
        void SaveTime()
        {
            // TODO: Save time to settings
        }

        /// <summary>
        /// Loads time from settings file
        /// </summary>
        void LoadTime()
        {
            // TODO: Load startup time from settings
        }

        /// <summary>
        /// Returns game time in real seconds
        /// </summary>
        public float time => startupTime + Time.timeSinceLevelLoad;


        /// <summary>
        /// Returns day percent where 0f means 0:00, 1f means 24:00 
        /// </summary>
        public float dayPercent => (time % RealSecondsInDay) / RealSecondsInDay;

        public string timeString
        {
            get
            {
                var t = dayPercent * HoursInDay;
                var hours = Mathf.FloorToInt(t);
                var minutes = Mathf.FloorToInt((t - hours) * 60f);
                return $"{hours:00}:{minutes:00}";
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 200, 20), timeString);
        }
    }
}