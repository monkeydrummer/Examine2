namespace CAD2DModel.Geometry;

/// <summary>
/// Boundary (closed polyline) with additional properties
/// </summary>
public class Boundary : Polyline
{
    public Boundary() : base()
    {
        IsClosed = true;
    }
    
    public Boundary(IEnumerable<Point2D> points) : base(points)
    {
        IsClosed = true;
    }
    
    /// <summary>
    /// Calculate the signed area of the boundary (positive = counterclockwise)
    /// </summary>
    public double SignedArea
    {
        get
        {
            if (Vertices.Count < 3)
                return 0;
            
            double area = 0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var v1 = Vertices[i].Location;
                var v2 = Vertices[(i + 1) % Vertices.Count].Location;
                area += v1.X * v2.Y - v2.X * v1.Y;
            }
            
            return area / 2;
        }
    }
    
    /// <summary>
    /// Calculate the absolute area
    /// </summary>
    public double Area => Math.Abs(SignedArea);
    
    /// <summary>
    /// Check if the boundary is oriented counterclockwise
    /// </summary>
    public bool IsCounterClockwise => SignedArea > 0;
    
    /// <summary>
    /// Reverse the order of vertices
    /// </summary>
    public void Reverse()
    {
        var reversed = Vertices.Reverse().ToList();
        Vertices.Clear();
        foreach (var vertex in reversed)
        {
            Vertices.Add(vertex);
        }
    }
    
    /// <summary>
    /// Check if the boundary contains self-intersections
    /// </summary>
    public bool HasSelfIntersections()
    {
        int segmentCount = GetSegmentCount();
        for (int i = 0; i < segmentCount; i++)
        {
            for (int j = i + 2; j < segmentCount; j++)
            {
                // Skip adjacent segments
                if (j == i + 1 || (i == 0 && j == segmentCount - 1))
                    continue;
                
                var seg1 = GetSegment(i);
                var seg2 = GetSegment(j);
                
                if (DoSegmentsIntersect(seg1, seg2))
                    return true;
            }
        }
        
        return false;
    }
    
    private bool DoSegmentsIntersect(LineSegment seg1, LineSegment seg2)
    {
        Vector2D d1 = seg1.End - seg1.Start;
        Vector2D d2 = seg2.End - seg2.Start;
        Vector2D d = seg2.Start - seg1.Start;
        
        double cross = d1.Cross(d2);
        if (Math.Abs(cross) < 1e-10)
            return false; // Parallel
        
        double t1 = d.Cross(d2) / cross;
        double t2 = d.Cross(d1) / cross;
        
        return t1 > 0 && t1 < 1 && t2 > 0 && t2 < 1;
    }
    
    public override string ToString()
    {
        return $"Boundary({Name}, {VertexCount} vertices, Area: {Area:F2})";
    }
}
