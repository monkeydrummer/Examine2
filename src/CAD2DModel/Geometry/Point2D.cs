using System.Runtime.CompilerServices;

namespace CAD2DModel.Geometry;

/// <summary>
/// Immutable 2D point structure
/// </summary>
public readonly struct Point2D : IEquatable<Point2D>
{
    public double X { get; init; }
    public double Y { get; init; }
    
    public Point2D(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DistanceTo(Point2D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DistanceSquaredTo(Point2D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return dx * dx + dy * dy;
    }
    
    public Vector2D VectorTo(Point2D other)
    {
        return new Vector2D(other.X - X, other.Y - Y);
    }
    
    public Point2D Offset(double dx, double dy)
    {
        return new Point2D(X + dx, Y + dy);
    }
    
    public Point2D Offset(Vector2D vector)
    {
        return new Point2D(X + vector.X, Y + vector.Y);
    }
    
    public static Point2D operator +(Point2D point, Vector2D vector)
    {
        return new Point2D(point.X + vector.X, point.Y + vector.Y);
    }
    
    public static Point2D operator -(Point2D point, Vector2D vector)
    {
        return new Point2D(point.X - vector.X, point.Y - vector.Y);
    }
    
    public static Vector2D operator -(Point2D point1, Point2D point2)
    {
        return new Vector2D(point1.X - point2.X, point1.Y - point2.Y);
    }
    
    public bool Equals(Point2D other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Point2D other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    public static bool operator ==(Point2D left, Point2D right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(Point2D left, Point2D right)
    {
        return !left.Equals(right);
    }
    
    public override string ToString()
    {
        return $"({X:F3}, {Y:F3})";
    }
    
    public static readonly Point2D Zero = new(0, 0);
    public static readonly Point2D Origin = new(0, 0);
}
