using System.Runtime.CompilerServices;

namespace CAD2DModel.Geometry;

/// <summary>
/// Immutable 2D vector structure
/// </summary>
public readonly struct Vector2D : IEquatable<Vector2D>
{
    public double X { get; init; }
    public double Y { get; init; }
    
    public Vector2D(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    public double Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Math.Sqrt(X * X + Y * Y);
    }
    
    public double LengthSquared
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => X * X + Y * Y;
    }
    
    public Vector2D Normalized()
    {
        double len = Length;
        if (len < 1e-10)
            return Zero;
        return new Vector2D(X / len, Y / len);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Dot(Vector2D other)
    {
        return X * other.X + Y * other.Y;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Cross(Vector2D other)
    {
        return X * other.Y - Y * other.X;
    }
    
    public Vector2D Perpendicular()
    {
        return new Vector2D(-Y, X);
    }
    
    public Vector2D Rotate(double angleRadians)
    {
        double cos = Math.Cos(angleRadians);
        double sin = Math.Sin(angleRadians);
        return new Vector2D(
            X * cos - Y * sin,
            X * sin + Y * cos
        );
    }
    
    public double AngleTo(Vector2D other)
    {
        double cross = Cross(other);
        double dot = Dot(other);
        return Math.Atan2(cross, dot);
    }
    
    public static Vector2D operator +(Vector2D v1, Vector2D v2)
    {
        return new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
    }
    
    public static Vector2D operator -(Vector2D v1, Vector2D v2)
    {
        return new Vector2D(v1.X - v2.X, v1.Y - v2.Y);
    }
    
    public static Vector2D operator *(Vector2D v, double scalar)
    {
        return new Vector2D(v.X * scalar, v.Y * scalar);
    }
    
    public static Vector2D operator *(double scalar, Vector2D v)
    {
        return new Vector2D(v.X * scalar, v.Y * scalar);
    }
    
    public static Vector2D operator /(Vector2D v, double scalar)
    {
        return new Vector2D(v.X / scalar, v.Y / scalar);
    }
    
    public static Vector2D operator -(Vector2D v)
    {
        return new Vector2D(-v.X, -v.Y);
    }
    
    public bool Equals(Vector2D other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Vector2D other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    public static bool operator ==(Vector2D left, Vector2D right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(Vector2D left, Vector2D right)
    {
        return !left.Equals(right);
    }
    
    public override string ToString()
    {
        return $"[{X:F3}, {Y:F3}]";
    }
    
    public static readonly Vector2D Zero = new(0, 0);
    public static readonly Vector2D UnitX = new(1, 0);
    public static readonly Vector2D UnitY = new(0, 1);
}
