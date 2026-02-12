namespace CAD2DModel.Geometry;

/// <summary>
/// Circular arc defined by center, radius, and start/end angles
/// </summary>
public class Arc
{
    public Point2D Center { get; set; }
    public double Radius { get; set; }
    public double StartAngle { get; set; }  // in radians
    public double EndAngle { get; set; }    // in radians
    
    public Arc(Point2D center, double radius, double startAngle, double endAngle)
    {
        Center = center;
        Radius = radius;
        StartAngle = startAngle;
        EndAngle = endAngle;
    }
    
    public Point2D StartPoint => new(
        Center.X + Radius * Math.Cos(StartAngle),
        Center.Y + Radius * Math.Sin(StartAngle)
    );
    
    public Point2D EndPoint => new(
        Center.X + Radius * Math.Cos(EndAngle),
        Center.Y + Radius * Math.Sin(EndAngle)
    );
    
    public double SweepAngle
    {
        get
        {
            double sweep = EndAngle - StartAngle;
            while (sweep < 0) sweep += 2 * Math.PI;
            while (sweep > 2 * Math.PI) sweep -= 2 * Math.PI;
            return sweep;
        }
    }
    
    public double ArcLength => Radius * SweepAngle;
    
    public Point2D PointAt(double t)
    {
        double angle = StartAngle + t * (EndAngle - StartAngle);
        return new Point2D(
            Center.X + Radius * Math.Cos(angle),
            Center.Y + Radius * Math.Sin(angle)
        );
    }
    
    public List<Point2D> Discretize(int segments)
    {
        var points = new List<Point2D>(segments + 1);
        for (int i = 0; i <= segments; i++)
        {
            double t = i / (double)segments;
            points.Add(PointAt(t));
        }
        return points;
    }
    
    public override string ToString()
    {
        return $"Arc(Center: {Center}, R: {Radius:F3}, {StartAngle:F2}-{EndAngle:F2} rad)";
    }
}
