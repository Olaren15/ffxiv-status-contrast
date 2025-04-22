using System.Numerics;

namespace StatusContrast;

public readonly record struct Color(byte R, byte G, byte B, byte A)
{
    public Color(Vector4 vector) : this(
        (byte)(vector.X * 255),
        (byte)(vector.Y * 255),
        (byte)(vector.Z * 255),
        (byte)(vector.W * 255))
    {
    }

    public Vector4 ToVector4()
    {
        return new Vector4(R / 255.0f, G / 255.0f, B / 255.0f, A / 255.0f);
    }
}
