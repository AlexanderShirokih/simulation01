using UnityEngine;

namespace Simulation01
{
    /// <summary>
    /// Rotates Sun according to the game time
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        private GameTime m_Time;

        private const float ZeroAmRotation = 270f;

        public void Awake()
        {
            m_Time = GameObject.Find("GameTimer").GetComponent<GameTime>();
        }

        public void Start() => UpdateRotation();

        public void Update()
        {
            if (Time.frameCount % 10 == 0) // Skip frames to save the performance
                UpdateRotation();
        }


        private void UpdateRotation()
        {
            transform.rotation = Quaternion.Euler(ZeroAmRotation + m_Time.dayPercent * 360f, 0f, 0f);
        }
    }
}