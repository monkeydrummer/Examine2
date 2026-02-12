namespace CAD2DModel.Geometry;

/// <summary>
/// Axis-aligned rectangle structure
/// </summary>
public struct Rect2D : IEquatable<Rect2D>
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    
    public Rect2D(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    
    public Rect2D(Point2D point1, Point2D point2)
    {
        X = Math.Min(point1.X, point2.X);
        Y = Math.Min(point1.Y, point2.Y);
        Width = Math.Abs(point2.X - point1.X);
        Height = Math.Abs(point2.Y - point1.Y);
    }
    
    public double Left => X;
    public double Right => X + Width;
    public double Top => Y;
    public double Bottom => Y + Height;
    
    public Point2D TopLeft => new(X, Y);
    public Point2D TopRight => new(X + Width, Y);
    public Point2D BottomLeft => new(X, Y + Height);
    public Point2D BottomRight => new(X + Width, Y + Height);
    public Point2D Center => new(X + Width / 2, Y + Height / 2);
    
    public bool IsEmpty => Width <= 0 || Height <= 0;
    
    public bool Contains(Point2D point)
    {
        return point.X >= X && point.X <= X + Width &&
               point.Y >= Y && point.Y <= Y + Height;
    }
    
    public bool Contains(Rect2D other)
    {
        return other.X >= X && other.Right <= Right &&
               other.Y >= Y && other.Bottom <= Bottom;
    }
    
    public bool Intersects(Rect2D other)
    {
        return !(other.Left > Right || other.Right < Left ||
                 other.Top > Bottom || other.Bottom < Top);
    }
    
    public Rect2D Union(Point2D point)
    {
        if (IsEmpty)
            return new Rect2D(point.X, point.Y, 0, 0);
        
        double left = Math.Min(X, point.X);
        double right = Math.Max(Right, point.X);
        double top = Math.Min(Y, point.Y);
        double bottom = Math.Max(Bottom, point.Y);
        
        return new Rect2D(left, top, right - left, bottom - top);
    }
    
    public Rect2D Union(Rect2D other)
    {
        if (IsEmpty) return other;
        if (other.IsEmpty) return this;
        
        double left = Math.Min(X, other.X);
        double right = Math.Max(Right, other.Right);
        double top = Math.Min(Y, other.Y);
        double bottom = Math.Max(Bottom, other.Bottom);
        
        return new Rect2D(left, top, right - left, bottom - top);
    }
    
    public void Inflate(double dx, double dy)
    {
        X -= dx;
        Y -= dy;
        Width += 2 * dx;
        Height += 2 * dy;
    }
    
    public bool Equals(Rect2D other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) &&
               Width.Equals(other.Width) && Height.Equals(other.Height);
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Rect2D other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
    }
    
    public static bool operator ==(Rect2D left, Rect2D right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(Rect2D left, Rect2D right)
    {
        return !left.Equals(right);
    }
    
    public override string ToString()
    {
        return $"Rect({X:F2}, {Y:F2}, {Width:F2}x{Height:F2})";
    }
    
    public static readonly Rect2D Empty = new(0, 0, 0, 0);
}
