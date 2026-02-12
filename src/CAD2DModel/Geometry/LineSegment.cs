namespace CAD2DModel.Geometry;

/// <summary>
/// Line segment defined by two endpoints
/// </summary>
public class LineSegment
{
    public Point2D Start { get; set; }
    public Point2D End { get; set; }
    
    public LineSegment(Point2D start, Point2D end)
    {
        Start = start;
        End = end;
    }
    
    public double Length => Start.DistanceTo(End);
    
    public Vector2D Direction => (End - Start).Normalized();
    
    public Point2D Midpoint => new((Start.X + End.X) / 2, (Start.Y + End.Y) / 2);
    
    public Point2D PointAt(double t)
    {
        return new Point2D(
            Start.X + t * (End.X - Start.X),
            Start.Y + t * (End.Y - Start.Y)
        );
    }
    
    public double DistanceToPoint(Point2D point)
    {
        Vector2D v = End - Start;
        Vector2D w = point - Start;
        
        double c1 = w.Dot(v);
        if (c1 <= 0)
            return point.DistanceTo(Start);
        
        double c2 = v.Dot(v);
        if (c1 >= c2)
            return point.DistanceTo(End);
        
        double b = c1 / c2;
        Point2D pb = Start + v * b;
        return point.DistanceTo(pb);
    }
    
    public Point2D ClosestPoint(Point2D point)
    {
        Vector2D v = End - Start;
        Vector2D w = point - Start;
        
        double c1 = w.Dot(v);
        if (c1 <= 0)
            return Start;
        
        double c2 = v.Dot(v);
        if (c1 >= c2)
            return End;
        
        double b = c1 / c2;
        return Start + v * b;
    }
    
    public override string ToString()
    {
        return $"LineSegment({Start} -> {End})";
    }
}
