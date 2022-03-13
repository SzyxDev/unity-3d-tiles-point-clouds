public class NPoint : IPoint<float>
{
    public NPoint(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public byte[] Color { get; set; }

    public override string ToString() => X + " " + Y + " " + Z;
}