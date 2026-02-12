namespace CAD2DModel.Geometry;

/// <summary>
/// Utility class for calculating arcs from various input parameters
/// </summary>
public static class ArcCalculator
{
    /// <summary>
    /// Create arc from 3 points (start, middle, end)
    /// </summary>
    public static Arc? FromThreePoints(Point2D start, Point2D mid, Point2D end)
    {
        // Calculate center of circle passing through 3 points
        var center = CalculateCircleCenter(start, mid, end);
        if (center == null)
            return null;
        
        // Calculate radius
        double radius = start.DistanceTo(center.Value);
        
        // Calculate angles
        double startAngle = Math.Atan2(start.Y - center.Value.Y, start.X - center.Value.X);
        double endAngle = Math.Atan2(end.Y - center.Value.Y, end.X - center.Value.X);
        double midAngle = Math.Atan2(mid.Y - center.Value.Y, mid.X - center.Value.X);
        
        // Normalize angles to determine direction
        startAngle = NormalizeAngle(startAngle);
        midAngle = NormalizeAngle(midAngle);
        endAngle = NormalizeAngle(endAngle);
        
        // Determine if we go counterclockwise or clockwise
        bool ccw = IsCounterClockwise(startAngle, midAngle, endAngle);
        
        if (!ccw)
        {
            // Clockwise - swap and adjust
            if (endAngle > startAngle)
                endAngle -= 2 * Math.PI;
        }
        else
        {
            // Counterclockwise
            if (endAngle < startAngle)
                endAngle += 2 * Math.PI;
        }
        
        return new Arc(center.Value, radius, startAngle, endAngle);
    }
    
    /// <summary>
    /// Create arc from start point, end point, and radius
    /// </summary>
    public static Arc? FromStartEndRadius(Point2D start, Point2D end, double radius, bool largeArc = false)
    {
        double distance = start.DistanceTo(end);
        
        // Check if radius is large enough
        if (radius < distance / 2.0)
            return null;
        
        // Calculate midpoint
        Point2D midpoint = new Point2D(
            (start.X + end.X) / 2.0,
            (start.Y + end.Y) / 2.0
        );
        
        // Calculate perpendicular distance from midpoint to center
        double h = Math.Sqrt(radius * radius - (distance / 2.0) * (distance / 2.0));
        
        // Calculate perpendicular direction
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double perpX = -dy / distance;
        double perpY = dx / distance;
        
        // Calculate center (choose one of two possible centers)
        Point2D center;
        if (largeArc)
        {
            center = new Point2D(
                midpoint.X - perpX * h,
                midpoint.Y - perpY * h
            );
        }
        else
        {
            center = new Point2D(
                midpoint.X + perpX * h,
                midpoint.Y + perpY * h
            );
        }
        
        // Calculate angles
        double startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
        double endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X);
        
        // Ensure we take the correct arc direction
        if (!largeArc)
        {
            if (endAngle < startAngle)
                endAngle += 2 * Math.PI;
        }
        else
        {
            if (endAngle > startAngle)
                endAngle -= 2 * Math.PI;
        }
        
        return new Arc(center, radius, startAngle, endAngle);
    }
    
    /// <summary>
    /// Create arc from start point, end point, and bulge factor
    /// Bulge = tan(angle/4), where angle is the included angle
    /// </summary>
    public static Arc? FromStartEndBulge(Point2D start, Point2D end, double bulge)
    {
        if (Math.Abs(bulge) < 0.0001)
            return null; // Essentially a straight line
        
        double distance = start.DistanceTo(end);
        if (distance < 0.0001)
            return null;
        
        // Calculate radius from bulge
        double angle = 4.0 * Math.Atan(Math.Abs(bulge));
        double radius = distance / (2.0 * Math.Sin(angle / 2.0));
        
        // Calculate center
        Point2D midpoint = new Point2D(
            (start.X + end.X) / 2.0,
            (start.Y + end.Y) / 2.0
        );
        
        double h = Math.Sqrt(radius * radius - (distance / 2.0) * (distance / 2.0));
        
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double perpX = -dy / distance;
        double perpY = dx / distance;
        
        // Direction depends on sign of bulge
        int direction = bulge > 0 ? 1 : -1;
        
        Point2D center = new Point2D(
            midpoint.X + perpX * h * direction,
            midpoint.Y + perpY * h * direction
        );
        
        double startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
        double endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X);
        
        if (bulge > 0)
        {
            if (endAngle < startAngle)
                endAngle += 2 * Math.PI;
        }
        else
        {
            if (endAngle > startAngle)
                endAngle -= 2 * Math.PI;
        }
        
        return new Arc(center, radius, startAngle, endAngle);
    }
    
    /// <summary>
    /// Create circle from center and radius
    /// </summary>
    public static List<Point2D> CreateCircle(Point2D center, double radius, int segments)
    {
        var points = new List<Point2D>(segments);
        for (int i = 0; i < segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            points.Add(new Point2D(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle)
            ));
        }
        return points;
    }
    
    /// <summary>
    /// Create circle from 2 points (diameter)
    /// </summary>
    public static List<Point2D> CreateCircleFromDiameter(Point2D point1, Point2D point2, int segments)
    {
        Point2D center = new Point2D(
            (point1.X + point2.X) / 2.0,
            (point1.Y + point2.Y) / 2.0
        );
        double radius = point1.DistanceTo(point2) / 2.0;
        return CreateCircle(center, radius, segments);
    }
    
    /// <summary>
    /// Create circle from 3 points on circumference
    /// </summary>
    public static List<Point2D>? CreateCircleFromThreePoints(Point2D p1, Point2D p2, Point2D p3, int segments)
    {
        var center = CalculateCircleCenter(p1, p2, p3);
        if (center == null)
            return null;
        
        double radius = p1.DistanceTo(center.Value);
        return CreateCircle(center.Value, radius, segments);
    }
    
    /// <summary>
    /// Calculate center of circle passing through 3 points
    /// </summary>
    private static Point2D? CalculateCircleCenter(Point2D p1, Point2D p2, Point2D p3)
    {
        double ax = p2.X - p1.X;
        double ay = p2.Y - p1.Y;
        double bx = p3.X - p1.X;
        double by = p3.Y - p1.Y;
        
        double d = 2 * (ax * by - ay * bx);
        
        if (Math.Abs(d) < 1e-10)
            return null; // Points are collinear
        
        double aMag = ax * ax + ay * ay;
        double bMag = bx * bx + by * by;
        
        double cx = p1.X + (by * aMag - ay * bMag) / d;
        double cy = p1.Y + (ax * bMag - bx * aMag) / d;
        
        return new Point2D(cx, cy);
    }
    
    private static double NormalizeAngle(double angle)
    {
        while (angle < 0) angle += 2 * Math.PI;
        while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
        return angle;
    }
    
    private static bool IsCounterClockwise(double startAngle, double midAngle, double endAngle)
    {
        // Normalize angles relative to start
        double mid = midAngle - startAngle;
        double end = endAngle - startAngle;
        
        if (mid < 0) mid += 2 * Math.PI;
        if (end < 0) end += 2 * Math.PI;
        
        return mid < end;
    }
}
