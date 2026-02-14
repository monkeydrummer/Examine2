namespace CAD2DModel.Annotations;

/// <summary>
/// Platform-independent color representation for annotations
/// </summary>
public readonly struct Color
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }
    
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    public static Color Black => new Color(0, 0, 0);
    public static Color White => new Color(255, 255, 255);
    public static Color Red => new Color(255, 0, 0);
    public static Color Green => new Color(0, 255, 0);
    public static Color Blue => new Color(0, 0, 255);
    public static Color Transparent => new Color(0, 0, 0, 0);
    
    public override bool Equals(object? obj)
    {
        return obj is Color other && R == other.R && G == other.G && B == other.B && A == other.A;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }
    
    public static bool operator ==(Color left, Color right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(Color left, Color right)
    {
        return !left.Equals(right);
    }
}
