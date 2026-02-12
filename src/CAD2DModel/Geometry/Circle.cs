namespace CAD2DModel.Geometry;

/// <summary>
/// Circle defined by center and radius
/// </summary>
public class Circle
{
    public Point2D Center { get; set; }
    public double Radius { get; set; }
    
    public Circle(Point2D center, double radius)
    {
        Center = center;
        Radius = radius;
    }
    
    public double Circumference => 2 * Math.PI * Radius;
    
    public double Area => Math.PI * Radius * Radius;
    
    public bool Contains(Point2D point)
    {
        return Center.DistanceTo(point) <= Radius;
    }
    
    public Point2D PointAt(double angleRadians)
    {
        return new Point2D(
            Center.X + Radius * Math.Cos(angleRadians),
            Center.Y + Radius * Math.Sin(angleRadians)
        );
    }
    
    public List<Point2D> Discretize(int segments)
    {
        var points = new List<Point2D>(segments);
        for (int i = 0; i < segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            points.Add(PointAt(angle));
        }
        return points;
    }
    
    public override string ToString()
    {
        return $"Circle(Center: {Center}, R: {Radius:F3})";
    }
}
