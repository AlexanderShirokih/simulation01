namespace Simulation01.Weather
{
    /// <summary>
    /// Describes rain or snow event
    /// </summary>
    public class WeatherEvent
    {
        /// <summary>
        /// Climate zone on where it will rain
        /// </summary>
        public ZoneType targetZone { get; }

        /// <summary>
        /// True if it's a snow event, False it's a rain
        /// </summary>
        public bool snowy { get; }

        /// <summary>
        /// Spread radius in blocks
        /// </summary>
        public int radius { get; }

        /// <summary>
        /// Event duration in seconds
        /// </summary>
        public int duration { get; }

        /// <summary>
        /// Rain or snow intensity in range [0..1].
        /// Higher value means heavier rain
        /// </summary>
        public float intensity { get; }

        public WeatherEvent(ZoneType targetZone, bool snowy, int radius, int duration, float intensity)
        {
            this.targetZone = targetZone;
            this.snowy = snowy;
            this.radius = radius;
            this.duration = duration;
            this.intensity = intensity;
        }

        public override string ToString()
        {
            return
                $"{nameof(snowy)}: {snowy}, {nameof(radius)}: {radius}, {nameof(duration)}: {duration}, {nameof(intensity)}: {intensity}";
        }
    }
}