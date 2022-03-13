using System;

public class QPoint : IPoint<ushort>
{
    public QPoint(UInt16 x, UInt16 y, UInt16 z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public ushort X { get; set; }
    public ushort Y { get; set; }
    public ushort Z { get; set; }
    public byte[] Color { get; set; }
}