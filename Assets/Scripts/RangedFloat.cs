/// <summary>
/// A class describing ranged value of float type
/// </summary>
public struct RangedFloat
{
    /// <summary>
    /// Minimal value
    /// </summary>
    public float start;

    /// <summary>
    /// Value interval
    /// </summary>
    public float length;

    public RangedFloat(float start, float length)
    {
        this.start = start;
        this.length = length;
    }

    /// <summary>
    /// Returns value from start to start+length depending of t coefficient.
    /// </summary>
    /// <param name="t">range coefficient. In range from 0.0 to 1.0.</param>
    /// <returns></returns>
    public float ValueAt(float t) => start + length * t;
}